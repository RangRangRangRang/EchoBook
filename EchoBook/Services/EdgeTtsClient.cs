using System.Globalization;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using EchoBook.Services.Interfaces;

namespace EchoBook.Services;

/// <summary>
/// Implements the edge-tts WebSocket protocol: connect, send a speech.config control message,
/// send the SSML to synthesize, then collect binary "Path:audio" frames until "Path:turn.end".
///
/// This talks to an undocumented Microsoft endpoint (the same one Edge's built-in Read Aloud
/// feature and the community edge-tts tools use). It requires no API key, which is exactly why
/// it can also change without notice - if synthesis starts failing, this is the first place to check.
/// </summary>
public class EdgeTtsClient : ITextToSpeechClient
{
    private const string TrustedClientToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";
    private const string WssEndpoint = "wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1";
    private const string ClientVersion = "130.0.2849.68";
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/" +
        ClientVersion + " Safari/537.36 Edg/" + ClientVersion;

    public async Task<byte[]> SynthesizeAsync(string text, string voiceName, double speed, CancellationToken cancellationToken = default)
    {
        var url = BuildConnectionUrl();

        using var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("Pragma", "no-cache");
        ws.Options.SetRequestHeader("Cache-Control", "no-cache");
        ws.Options.SetRequestHeader("User-Agent", UserAgent);
        ws.Options.SetRequestHeader("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");

        await ws.ConnectAsync(new Uri(url), cancellationToken);

        var timestamp = FormatTimestamp();
        await SendTextAsync(ws, BuildSpeechConfigMessage(timestamp), cancellationToken);
        await SendTextAsync(ws, BuildSsmlMessage(text, voiceName, speed, timestamp), cancellationToken);

        var audio = await ReceiveAudioAsync(ws, cancellationToken);

        if (ws.State == WebSocketState.Open)
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        }

        return audio;
    }

    private static string BuildConnectionUrl()
    {
        var connectionId = Guid.NewGuid().ToString("N");
        var secMsGec = GenerateSecMsGec();
        return $"{WssEndpoint}?TrustedClientToken={TrustedClientToken}" +
               $"&Sec-MS-GEC={secMsGec}&Sec-MS-GEC-Version=1-{ClientVersion}&ConnectionId={connectionId}";
    }

    /// <summary>
    /// Microsoft added an anti-abuse token derived from the current time, rounded down to the
    /// nearest 5-minute window and expressed as a Windows FILETIME, hashed together with the
    /// trusted client token. Reverse-engineered from the public edge-tts client implementations.
    /// </summary>
    private static string GenerateSecMsGec()
    {
        var unixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowStart = unixSeconds - (unixSeconds % 300);
        var windowsFileTimeTicks = (windowStart + 11644473600L) * 10_000_000L;
        var toHash = windowsFileTimeTicks.ToString(CultureInfo.InvariantCulture) + TrustedClientToken;
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(toHash));
        return Convert.ToHexString(hash); // uppercase hex
    }

    private static string FormatTimestamp() =>
        DateTime.UtcNow.ToString("ddd MMM dd yyyy HH:mm:ss 'GMT+0000 (Coordinated Universal Time)'", CultureInfo.InvariantCulture);

    private static string BuildSpeechConfigMessage(string timestamp) =>
        $"X-Timestamp:{timestamp}\r\n" +
        "Content-Type:application/json; charset=utf-8\r\n" +
        "Path:speech.config\r\n\r\n" +
        "{\"context\":{\"synthesis\":{\"audio\":{" +
        "\"metadataoptions\":{\"sentenceBoundaryEnabled\":false,\"wordBoundaryEnabled\":false}," +
        "\"outputFormat\":\"audio-24khz-48kbitrate-mono-mp3\"}}}}";

    private static string BuildSsmlMessage(string text, string voiceName, double speed, string timestamp)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var ratePercent = (int)Math.Round((speed - 1.0) * 100.0);
        var rateAttribute = (ratePercent >= 0 ? "+" : "") + ratePercent.ToString(CultureInfo.InvariantCulture) + "%";
        var escapedText = EscapeXml(text);

        var ssml =
            "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" +
            $"<voice name='{voiceName}'>" +
            $"<prosody rate='{rateAttribute}' pitch='+0Hz'>{escapedText}</prosody>" +
            "</voice></speak>";

        return $"X-RequestId:{requestId}\r\n" +
               "Content-Type:application/ssml+xml\r\n" +
               $"X-Timestamp:{timestamp}\r\n" +
               "Path:ssml\r\n\r\n" + ssml;
    }

    private static string EscapeXml(string text) =>
        text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");

    private static async Task SendTextAsync(ClientWebSocket ws, string message, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
    }

    private static async Task<byte[]> ReceiveAudioAsync(ClientWebSocket ws, CancellationToken cancellationToken)
    {
        using var audioStream = new MemoryStream();
        var buffer = new byte[16 * 1024];

        while (ws.State == WebSocketState.Open)
        {
            using var frame = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close) break;
                frame.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close) break;

            var payload = frame.ToArray();

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var text = Encoding.UTF8.GetString(payload);
                if (text.Contains("Path:turn.end", StringComparison.Ordinal))
                {
                    break;
                }
                // Path:turn.start / Path:response / Path:audio.metadata carry no data we need
                // since sentence-level highlighting is handled client-side per audio chunk.
                continue;
            }

            // Binary frame: first 2 bytes (big-endian) = header text length, then header, then raw mp3 bytes.
            if (payload.Length < 2) continue;
            var headerLength = (payload[0] << 8) | payload[1];
            var audioStart = 2 + headerLength;
            if (audioStart < payload.Length)
            {
                audioStream.Write(payload, audioStart, payload.Length - audioStart);
            }
        }

        return audioStream.ToArray();
    }
}
