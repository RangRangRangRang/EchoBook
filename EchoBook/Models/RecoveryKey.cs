using System.ComponentModel.DataAnnotations;

namespace EchoBook.Models;

/// <summary>
/// A passwordless account replacement. Every uploaded book belongs to one RecoveryKey.
/// </summary>
public class RecoveryKey
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-friendly key shown to the user, e.g. "K8AF-HX32-JM9Q".
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime LastAccessedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Book> Books { get; set; } = new List<Book>();

    public Settings? Settings { get; set; }
}
