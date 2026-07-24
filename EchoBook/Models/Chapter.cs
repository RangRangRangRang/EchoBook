using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoBook.Models;

public class Chapter
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid BookId { get; set; }

    [ForeignKey(nameof(BookId))]
    public Book Book { get; set; } = null!;

    /// <summary>
    /// Zero-based position of this chapter within the book's spine / table of contents.
    /// </summary>
    public int Order { get; set; }

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The epub internal file path (spine item href), used to re-extract HTML content lazily on demand
    /// rather than duplicating full chapter HTML in the database.
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string EpubItemHref { get; set; } = string.Empty;

    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
}
