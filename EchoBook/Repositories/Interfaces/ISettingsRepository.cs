using EchoBook.Models;

namespace EchoBook.Repositories.Interfaces;

public interface ISettingsRepository
{
    Task<Settings?> GetByRecoveryKeyIdAsync(Guid recoveryKeyId);
    Task AddAsync(Settings settings);
    Task SaveAsync(Settings settings);
}
