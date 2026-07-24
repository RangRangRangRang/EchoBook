using EchoBook.Models;

namespace EchoBook.Repositories.Interfaces;

public interface IAudioCacheRepository
{
    Task<AudioCache?> GetAsync(string chunkHash, string voice, double speed);
    Task<AudioCache?> GetByIdAsync(Guid id);
    Task AddAsync(AudioCache audioCache);
}
