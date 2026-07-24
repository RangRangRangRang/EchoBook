namespace EchoBook.Services.Interfaces;

/// <summary>
/// Synthesizes speech using Microsoft Edge's free "Read Aloud" service - the same backend
/// the community-maintained edge-tts Python package talks to. There is no first-party SDK;
/// this is a native .NET WebSocket client speaking its (reverse-engineered) protocol directly,
/// which avoids running a second Python runtime alongside the .NET app.
/// </summary>
public interface ITextToSpeechClient
{
    /// <param name="text">Plain text to speak (a single sentence/chunk).</param>
    /// <param name="voiceName">Edge neural voice name, e.g. "en-US-AriaNeural".</param>
    /// <param name="speed">Playback speed multiplier, e.g. 1.0 = normal, 1.5 = 50% faster.</param>
    Task<byte[]> SynthesizeAsync(string text, string voiceName, double speed, CancellationToken cancellationToken = default);
}
