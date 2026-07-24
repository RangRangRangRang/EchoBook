using System.ComponentModel.DataAnnotations;

namespace EchoBook.Models;

/// <summary>
/// Caches generated TTS MP3 files so identical chunk text + voice + speed is never re-synthesized.
/// </summary>
public class AudioCache
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// SHA-256 hash of (chunk text + voice + speed), used as the cache lookup key.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string ChunkHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Voice { get; set; } = string.Empty;

    public double Speed { get; set; } = 1.0;

    /// <summary>
    /// Relative path (under Storage:AudioCachePath) to the cached MP3 file.
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string AudioFilePath { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
