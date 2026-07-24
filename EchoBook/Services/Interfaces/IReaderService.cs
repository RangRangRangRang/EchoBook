using EchoBook.ViewModels;

namespace EchoBook.Services.Interfaces;

public interface IReaderService
{
    /// <summary>Returns null if the book does not exist or does not belong to the given recovery key.</summary>
    Task<ReaderBundleViewModel?> GetReaderBundleAsync(Guid bookId, Guid recoveryKeyId);

    Task<ChapterContentDto?> GetChapterContentAsync(Guid bookId, Guid chapterId, Guid recoveryKeyId);

    Task<(byte[] Bytes, string ContentType)?> GetChapterAssetAsync(Guid bookId, Guid chapterId, Guid recoveryKeyId, string src);

    Task<bool> SaveProgressAsync(Guid bookId, Guid recoveryKeyId, ReaderProgressUpdateDto dto);

    Task<bool> SaveSettingsAsync(Guid recoveryKeyId, ReaderSettingsDto dto);

    Task<ReaderBookmarkDto?> AddBookmarkAsync(Guid bookId, Guid recoveryKeyId, BookmarkCreateDto dto);

    Task<bool> DeleteBookmarkAsync(Guid bookId, Guid recoveryKeyId, Guid bookmarkId);
}
