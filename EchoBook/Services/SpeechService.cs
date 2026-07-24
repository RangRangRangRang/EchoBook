using System.Security.Cryptography;
using System.Text;
using EchoBook.Models;
using EchoBook.Repositories.Interfaces;
using EchoBook.Services.Interfaces;

namespace EchoBook.Services;

public class SpeechService : ISpeechService
{
    private const string DefaultVoice = "en-US-AriaNeural";

    private readonly IAudioCacheRepository _audioCacheRepository;
    private readonly ITextToSpeechClient _ttsClient;
    private readonly IFileStorageService _fileStorageService;

    public SpeechService(
        IAudioCacheRepository audioCacheRepository,
        ITextToSpeechClient ttsClient,
        IFileStorageService fileStorageService)
    {
        _audioCacheRepository = audioCacheRepository;
        _ttsClient = ttsClient;
        _fileStorageService = fileStorageService;
    }

    public async Task<Guid> GetOrSynthesizeAsync(string text, string voice, double speed)
    {
        var normalizedText = text.Trim();
        var normalizedVoice = string.IsNullOrWhiteSpace(voice) ? DefaultVoice : voice;
        var normalizedSpeed = Math.Round(speed, 2);

        var hash = ComputeHash(normalizedText, normalizedVoice, normalizedSpeed);

        var existing = await _audioCacheRepository.GetAsync(hash, normalizedVoice, normalizedSpeed);
        if (existing is not null)
        {
            return existing.Id;
        }

        var mp3Bytes = await _ttsClient.SynthesizeAsync(normalizedText, normalizedVoice, normalizedSpeed);

        var id = Guid.NewGuid();
        var fileName = $"{id}.mp3";
        var relativePath = await _fileStorageService.SaveAudioAsync(fileName, mp3Bytes);

        var cacheEntry = new AudioCache
        {
            Id = id,
            ChunkHash = hash,
            Voice = normalizedVoice,
            Speed = normalizedSpeed,
            AudioFilePath = relativePath,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _audioCacheRepository.AddAsync(cacheEntry);
        return id;
    }

    public async Task<(byte[] Bytes, string ContentType)?> GetAudioAsync(Guid audioId)
    {
        var entry = await _audioCacheRepository.GetByIdAsync(audioId);
        if (entry is null) return null;

        var absolutePath = _fileStorageService.GetAudioAbsolutePath(entry.AudioFilePath);
        if (!File.Exists(absolutePath)) return null;

        var bytes = await File.ReadAllBytesAsync(absolutePath);
        return (bytes, "audio/mpeg");
    }

    private static string ComputeHash(string text, string voice, double speed)
    {
        var input = $"{text}|{voice}|{speed:0.00}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }
}
