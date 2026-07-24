using EchoBook.Data;
using EchoBook.Models;
using EchoBook.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EchoBook.Repositories;

public class ReadingProgressRepository : IReadingProgressRepository
{
    private readonly AppDbContext _db;

    public ReadingProgressRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReadingProgress?> GetByBookIdAsync(Guid bookId)
    {
        return await _db.ReadingProgresses.FirstOrDefaultAsync(p => p.BookId == bookId);
    }

    public async Task AddAsync(ReadingProgress progress)
    {
        _db.ReadingProgresses.Add(progress);
        await _db.SaveChangesAsync();
    }

    public async Task SaveAsync(ReadingProgress progress)
    {
        progress.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
