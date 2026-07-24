using EchoBook.Models;
using EchoBook.Repositories.Interfaces;
using EchoBook.Services.Interfaces;

namespace EchoBook.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IRecoveryKeyRepository _recoveryKeyRepository;
    private readonly IRecoveryKeyService _recoveryKeyService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IEpubParsingService _epubParsingService;

    public BookService(
        IBookRepository bookRepository,
        IRecoveryKeyRepository recoveryKeyRepository,
        IRecoveryKeyService recoveryKeyService,
        IFileStorageService fileStorageService,
        IEpubParsingService epubParsingService)
    {
        _bookRepository = bookRepository;
        _recoveryKeyRepository = recoveryKeyRepository;
        _recoveryKeyService = recoveryKeyService;
        _fileStorageService = fileStorageService;
        _epubParsingService = epubParsingService;
    }

    public async Task<(Book Book, RecoveryKey RecoveryKey)> UploadEpubAsync(Guid? recoveryKeyId, string originalFileName, long fileSizeBytes, Stream fileContent)
    {
        RecoveryKey? recoveryKey = recoveryKeyId.HasValue
            ? await _recoveryKeyRepository.GetByIdAsync(recoveryKeyId.Value)
            : null;

        recoveryKey ??= await _recoveryKeyService.GenerateNewKeyAsync();

        var bookId = Guid.NewGuid();

        // Save the raw file first so the parser can read it from a stable path on disk.
        var relativeEpubPath = await _fileStorageService.SaveUploadAsync(recoveryKey.Id, bookId, originalFileName, fileContent);
        var absoluteEpubPath = _fileStorageService.GetAbsolutePath(relativeEpubPath);

        var parsed = await _epubParsingService.ParseAsync(absoluteEpubPath);

        string? relativeCoverPath = null;
        if (parsed.CoverImageBytes is { Length: > 0 })
        {
            relativeCoverPath = await _fileStorageService.SaveCoverAsync(
                recoveryKey.Id, bookId, parsed.CoverImageBytes, parsed.CoverImageExtension ?? ".jpg");
        }

        var book = new Book
        {
            Id = bookId,
            RecoveryKeyId = recoveryKey.Id,
            Title = parsed.Title,
            Author = parsed.Author,
            EpubFilePath = relativeEpubPath,
            CoverImagePath = relativeCoverPath,
            FileSizeBytes = fileSizeBytes,
            UploadedAtUtc = DateTime.UtcNow,
            Chapters = parsed.Chapters.Select(c => new Chapter
            {
                Id = Guid.NewGuid(),
                Order = c.Order,
                Title = c.Title,
                EpubItemHref = c.EpubItemHref
            }).ToList()
        };

        await _bookRepository.AddAsync(book);

        return (book, recoveryKey);
    }

    public Task<List<Book>> GetLibraryAsync(Guid recoveryKeyId)
    {
        return _bookRepository.GetByRecoveryKeyAsync(recoveryKeyId);
    }

    public async Task<bool> DeleteBookAsync(Guid recoveryKeyId, Guid bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book is null || book.RecoveryKeyId != recoveryKeyId)
        {
            return false;
        }

        await _bookRepository.DeleteAsync(book);
        _fileStorageService.DeleteBookFiles(recoveryKeyId, bookId);
        return true;
    }
}
