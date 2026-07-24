using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoBook.Models;

public class Bookmark
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid BookId { get; set; }

    [ForeignKey(nameof(BookId))]
    public Book Book { get; set; } = null!;

    [Required]
    public Guid ChapterId { get; set; }

    [ForeignKey(nameof(ChapterId))]
    public Chapter Chapter { get; set; } = null!;

    /// <summary>
    /// Page number within the chapter at the line-count setting active when the bookmark was created.
    /// Combined with LinesPerPage so the bookmark can be re-resolved if pagination settings change.
    /// </summary>
    public int PageNumber { get; set; }

    public int LinesPerPage { get; set; }

    [MaxLength(300)]
    public string? PreviewText { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
