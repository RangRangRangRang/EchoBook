namespace EchoBook.Services.Interfaces;

/// <summary>
/// Resolves absolute paths for uploaded epubs, extracted covers, and generated audio,
/// and performs the actual disk writes/deletes. Keeps physical storage layout out of controllers.
/// </summary>
public interface IFileStorageService
{
    string UploadsRoot { get; }
    string AudioCacheRoot { get; }

    /// <summary>Saves a stream under Uploads/{recoveryKeyId}/{bookId}/{fileName} and returns the relative path.</summary>
    Task<string> SaveUploadAsync(Guid recoveryKeyId, Guid bookId, string fileName, Stream content);

    /// <summary>Saves cover bytes under Uploads/{recoveryKeyId}/{bookId}/cover.{ext} and returns the relative path.</summary>
    Task<string> SaveCoverAsync(Guid recoveryKeyId, Guid bookId, byte[] imageBytes, string extension);

    /// <summary>Saves a synthesized TTS clip under AudioCache/{fileName} and returns the relative path.</summary>
    Task<string> SaveAudioAsync(string fileName, byte[] mp3Bytes);

    string GetAbsolutePath(string relativePath);

    string GetAudioAbsolutePath(string relativePath);

    void DeleteBookFiles(Guid recoveryKeyId, Guid bookId);
}
