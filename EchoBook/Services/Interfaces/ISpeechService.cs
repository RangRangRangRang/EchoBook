namespace EchoBook.Services.Interfaces;

public interface ISpeechService
{
    /// <summary>
    /// Returns a cached clip if this exact text/voice/speed was synthesized before, otherwise
    /// synthesizes it via edge-tts, caches it, and returns the new clip's id.
    /// </summary>
    Task<Guid> GetOrSynthesizeAsync(string text, string voice, double speed);

    Task<(byte[] Bytes, string ContentType)?> GetAudioAsync(Guid audioId);
}
