namespace EchoBook.ViewModels;

public class ReaderBundleViewModel
{
    public Guid BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public List<ReaderChapterSummary> Chapters { get; set; } = new();
    public ReaderProgressDto Progress { get; set; } = new();
    public ReaderSettingsDto Settings { get; set; } = new();
    public List<ReaderBookmarkDto> Bookmarks { get; set; } = new();
}

public class ReaderChapterSummary
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class ChapterContentDto
{
    public Guid ChapterId { get; set; }
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;
}

public class ReaderProgressDto
{
    public Guid? CurrentChapterId { get; set; }
    public int CurrentPage { get; set; }
    public int CurrentScrollOffset { get; set; }
    public int LinesPerPage { get; set; } = 25;
    public string? SelectedVoice { get; set; }
    public double ReadingSpeed { get; set; } = 1.0;
}

public class ReaderProgressUpdateDto
{
    public Guid? CurrentChapterId { get; set; }
    public int CurrentPage { get; set; }
    public int CurrentScrollOffset { get; set; }
    public int LinesPerPage { get; set; } = 25;
    public string? SelectedVoice { get; set; }
    public double ReadingSpeed { get; set; } = 1.0;
}

public class ReaderSettingsDto
{
    public bool DarkMode { get; set; } = true;
    public string Language { get; set; } = "en";
    public string Font { get; set; } = "Georgia, serif";
    public int FontSize { get; set; } = 18;
    public double LineHeight { get; set; } = 1.6;
    public double LetterSpacing { get; set; } = 0.0;
    public string? AiVoice { get; set; }
    public double ReadingSpeed { get; set; } = 1.0;
    public int LinesPerPage { get; set; } = 25;
}

public class ReaderBookmarkDto
{
    public Guid Id { get; set; }
    public Guid ChapterId { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public int LinesPerPage { get; set; }
    public string? PreviewText { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class BookmarkCreateDto
{
    public Guid ChapterId { get; set; }
    public int PageNumber { get; set; }
    public int LinesPerPage { get; set; }
    public string? PreviewText { get; set; }
}

public class SpeechRequestDto
{
    public string Text { get; set; } = string.Empty;
    public string Voice { get; set; } = "en-US-AriaNeural";
    public double Speed { get; set; } = 1.0;
}

public class SpeechResponseDto
{
    public Guid AudioId { get; set; }
    public string Url { get; set; } = string.Empty;
}
