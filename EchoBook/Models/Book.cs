using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoBook.Models;

public class Book
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RecoveryKeyId { get; set; }

    [ForeignKey(nameof(RecoveryKeyId))]
    public RecoveryKey RecoveryKey { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Author { get; set; }

    /// <summary>
    /// Relative path (under Storage:UploadsPath) to the original .epub file.
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string EpubFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Relative path (under Storage:UploadsPath) to the extracted cover image, or null if the epub had none.
    /// </summary>
    [MaxLength(1000)]
    public string? CoverImagePath { get; set; }

    public long FileSizeBytes { get; set; }

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();

    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    public ReadingProgress? ReadingProgress { get; set; }
}
