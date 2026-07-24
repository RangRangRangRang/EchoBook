using EchoBook.Data;
using EchoBook.Models;
using EchoBook.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EchoBook.Repositories;

public class BookmarkRepository : IBookmarkRepository
{
    private readonly AppDbContext _db;

    public BookmarkRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Bookmark>> GetByBookIdAsync(Guid bookId)
    {
        return await _db.Bookmarks
            .Include(b => b.Chapter)
            .Where(b => b.BookId == bookId)
            .OrderBy(b => b.Chapter.Order).ThenBy(b => b.PageNumber)
            .ToListAsync();
    }

    public async Task<Bookmark?> GetByIdAsync(Guid bookmarkId)
    {
        return await _db.Bookmarks.FirstOrDefaultAsync(b => b.Id == bookmarkId);
    }

    public async Task AddAsync(Bookmark bookmark)
    {
        _db.Bookmarks.Add(bookmark);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Bookmark bookmark)
    {
        _db.Bookmarks.Remove(bookmark);
        await _db.SaveChangesAsync();
    }
}
