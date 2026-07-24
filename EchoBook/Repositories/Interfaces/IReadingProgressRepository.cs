using EchoBook.Models;

namespace EchoBook.Repositories.Interfaces;

public interface IReadingProgressRepository
{
    Task<ReadingProgress?> GetByBookIdAsync(Guid bookId);
    Task AddAsync(ReadingProgress progress);
    Task SaveAsync(ReadingProgress progress);
}
