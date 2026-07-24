using EchoBook.Models;

namespace EchoBook.Services.Interfaces;

public interface IRecoveryKeyService
{
    /// <summary>
    /// Generates a brand new, guaranteed-unique recovery key and persists it.
    /// Called the first time a visitor uploads a book with no existing key.
    /// </summary>
    Task<RecoveryKey> GenerateNewKeyAsync();

    /// <summary>
    /// Validates a user-entered recovery key code. Returns null if it does not exist.
    /// </summary>
    Task<RecoveryKey?> ValidateAsync(string code);
}
