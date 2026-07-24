using System.Security.Cryptography;
using EchoBook.Models;
using EchoBook.Repositories.Interfaces;
using EchoBook.Services.Interfaces;

namespace EchoBook.Services;

public class RecoveryKeyService : IRecoveryKeyService
{
    private readonly IRecoveryKeyRepository _repository;

    // Crockford-ish charset with ambiguous characters (0/O, 1/I) removed to avoid transcription errors.
    private const string CharSet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int GroupCount = 3;
    private const int GroupLength = 4;

    public RecoveryKeyService(IRecoveryKeyRepository repository)
    {
        _repository = repository;
    }

    public async Task<RecoveryKey> GenerateNewKeyAsync()
    {
        string code;
        do
        {
            code = GenerateCode();
        } while (await _repository.CodeExistsAsync(code));

        var recoveryKey = new RecoveryKey
        {
            Id = Guid.NewGuid(),
            Code = code,
            CreatedAtUtc = DateTime.UtcNow,
            LastAccessedAtUtc = DateTime.UtcNow
        };

        await _repository.AddAsync(recoveryKey);
        return recoveryKey;
    }

    public async Task<RecoveryKey?> ValidateAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var key = await _repository.GetByCodeAsync(code);
        if (key is null) return null;

        await _repository.TouchLastAccessedAsync(key.Id);
        return key;
    }

    private static string GenerateCode()
    {
        var groups = new string[GroupCount];
        for (var g = 0; g < GroupCount; g++)
        {
            var chars = new char[GroupLength];
            for (var i = 0; i < GroupLength; i++)
            {
                chars[i] = CharSet[RandomNumberGenerator.GetInt32(CharSet.Length)];
            }
            groups[g] = new string(chars);
        }
        return string.Join('-', groups);
    }
}
