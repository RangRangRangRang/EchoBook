using EchoBook.Services.Interfaces;
using EchoBook.ViewModels;
using VersOne.Epub;

namespace EchoBook.Services;

public class EpubParsingService : IEpubParsingService
{
    public async Task<ParsedEpubResult> ParseAsync(string absoluteEpubFilePath)
    {
        var epubBook = await EpubReader.ReadBookAsync(absoluteEpubFilePath);

        var result = new ParsedEpubResult
        {
            Title = string.IsNullOrWhiteSpace(epubBook.Title) ? Path.GetFileNameWithoutExtension(absoluteEpubFilePath) : epubBook.Title,
            Author = epubBook.AuthorList is { Count: > 0 } ? string.Join(", ", epubBook.AuthorList) : epubBook.Author
        };

        if (epubBook.CoverImage is { Length: > 0 })
        {
            result.CoverImageBytes = epubBook.CoverImage;
            result.CoverImageExtension = DetectImageExtension(epubBook.CoverImage);
        }

        result.Chapters = BuildChapterList(epubBook);

        return result;
    }

    public async Task<string> GetChapterHtmlAsync(string absoluteEpubFilePath, string epubItemHref)
    {
        var epubBook = await EpubReader.ReadBookAsync(absoluteEpubFilePath);

        var item = epubBook.ReadingOrder.FirstOrDefault(f => f.FilePath.EndsWith(epubItemHref, StringComparison.OrdinalIgnoreCase))
                   ?? epubBook.Content.Html.Local.FirstOrDefault(f => f.FilePath.EndsWith(epubItemHref, StringComparison.OrdinalIgnoreCase));

        if (item is null)
        {
            throw new FileNotFoundException($"Chapter content not found for href '{epubItemHref}'.");
        }

        return item.Content;
    }

    private static List<ParsedChapter> BuildChapterList(EpubBook epubBook)
    {
        var chapters = new List<ParsedChapter>();

        if (epubBook.Navigation is { Count: > 0 })
        {
            var order = 0;
            void Walk(IEnumerable<EpubNavigationItem> items)
            {
                foreach (var navItem in items)
                {
                    if (navItem.Link is not null && !string.IsNullOrWhiteSpace(navItem.Link.ContentFilePath))
                    {
                        chapters.Add(new ParsedChapter
                        {
                            Order = order++,
                            Title = string.IsNullOrWhiteSpace(navItem.Title) ? $"Chapter {order}" : navItem.Title,
                            EpubItemHref = navItem.Link.ContentFilePath
                        });
                    }

                    if (navItem.NestedItems is { Count: > 0 })
                    {
                        Walk(navItem.NestedItems);
                    }
                }
            }
            Walk(epubBook.Navigation);
        }

        // Fallback: no usable navigation/TOC - derive chapters directly from spine reading order.
        if (chapters.Count == 0)
        {
            var order = 0;
            foreach (var spineItem in epubBook.ReadingOrder)
            {
                chapters.Add(new ParsedChapter
                {
                    Order = order,
                    Title = $"Chapter {order + 1}",
                    EpubItemHref = spineItem.FilePath
                });
                order++;
            }
        }

        return chapters;
    }

    public async Task<(byte[] Bytes, string ContentType)?> GetAssetAsync(string absoluteEpubFilePath, string chapterHref, string assetRelativeSrc)
    {
        var cleanSrc = assetRelativeSrc.Split('#')[0].Split('?')[0];
        if (string.IsNullOrWhiteSpace(cleanSrc)) return null;

        var resolvedPath = ResolveRelativePath(chapterHref, cleanSrc);

        var epubBook = await EpubReader.ReadBookAsync(absoluteEpubFilePath);
        var image = epubBook.Content.Images.Local
            .FirstOrDefault(f => f.FilePath.EndsWith(resolvedPath, StringComparison.OrdinalIgnoreCase))
            ?? epubBook.Content.Images.Local
            .FirstOrDefault(f => f.FilePath.EndsWith(Path.GetFileName(resolvedPath), StringComparison.OrdinalIgnoreCase));

        if (image is null) return null;

        var bytes = image.Content;
        var contentType = string.IsNullOrWhiteSpace(image.ContentMimeType) ? "application/octet-stream" : image.ContentMimeType;
        return (bytes, contentType);
    }

    private static string ResolveRelativePath(string basePath, string relative)
    {
        if (relative.StartsWith('/')) return relative.TrimStart('/');

        var baseDir = basePath.Contains('/') ? basePath[..basePath.LastIndexOf('/')] : string.Empty;
        var segments = string.IsNullOrEmpty(baseDir)
            ? new List<string>()
            : baseDir.Split('/').ToList();

        foreach (var segment in relative.Split('/'))
        {
            if (segment is "." or "") continue;
            if (segment == "..")
            {
                if (segments.Count > 0) segments.RemoveAt(segments.Count - 1);
            }
            else
            {
                segments.Add(segment);
            }
        }

        return string.Join('/', segments);
    }

    private static string DetectImageExtension(byte[] bytes)
    {
        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return ".png";
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8)
            return ".jpg";
        if (bytes.Length >= 6 && bytes[0] == 'G' && bytes[1] == 'I' && bytes[2] == 'F')
            return ".gif";
        return ".jpg";
    }
}
