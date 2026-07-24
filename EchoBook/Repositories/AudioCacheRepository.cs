using EchoBook.Data;
using EchoBook.Models;
using EchoBook.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EchoBook.Repositories;

public class AudioCacheRepository : IAudioCacheRepository
{
    private readonly AppDbContext _db;

    public AudioCacheRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AudioCache?> GetAsync(string chunkHash, string voice, double speed)
    {
        return await _db.AudioCaches.FirstOrDefaultAsync(a =>
            a.ChunkHash == chunkHash && a.Voice == voice && a.Speed == speed);
    }

    public async Task<AudioCache?> GetByIdAsync(Guid id)
    {
        return await _db.AudioCaches.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task AddAsync(AudioCache audioCache)
    {
        _db.AudioCaches.Add(audioCache);
        await _db.SaveChangesAsync();
    }
}
