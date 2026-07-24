using EchoBook.ViewModels;

namespace EchoBook.Services.Interfaces;

public interface IEpubParsingService
{
    Task<ParsedEpubResult> ParseAsync(string absoluteEpubFilePath);

    /// <summary>
    /// Extracts the raw (X)HTML content of a single chapter/spine item by its href, for lazy on-demand loading.
    /// </summary>
    Task<string> GetChapterHtmlAsync(string absoluteEpubFilePath, string epubItemHref);

    /// <summary>
    /// Resolves an image (or other binary asset) referenced by a chapter, given the chapter's own href
    /// and the (possibly relative) src/href written inside that chapter's markup.
    /// </summary>
    Task<(byte[] Bytes, string ContentType)?> GetAssetAsync(string absoluteEpubFilePath, string chapterHref, string assetRelativeSrc);
}
