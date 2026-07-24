using EchoBook.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EchoBook.Services;

public class FileStorageService : IFileStorageService
{
    public string UploadsRoot { get; }
    public string AudioCacheRoot { get; }

    public FileStorageService(IWebHostEnvironment env, IConfiguration configuration)
    {
        var uploadsPath = configuration["Storage:UploadsPath"] ?? "Uploads";
        var audioPath = configuration["Storage:AudioCachePath"] ?? "AudioCache";

        UploadsRoot = Path.IsPathRooted(uploadsPath)
            ? uploadsPath
            : Path.Combine(env.ContentRootPath, uploadsPath);

        AudioCacheRoot = Path.IsPathRooted(audioPath)
            ? audioPath
            : Path.Combine(env.ContentRootPath, audioPath);

        Directory.CreateDirectory(UploadsRoot);
        Directory.CreateDirectory(AudioCacheRoot);
    }

    public async Task<string> SaveUploadAsync(Guid recoveryKeyId, Guid bookId, string fileName, Stream content)
    {
        var relativeDir = Path.Combine(recoveryKeyId.ToString(), bookId.ToString());
        var absoluteDir = Path.Combine(UploadsRoot, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var safeFileName = Path.GetFileName(fileName);
        var relativePath = Path.Combine(relativeDir, safeFileName);
        var absolutePath = Path.Combine(absoluteDir, safeFileName);

        await using var fileStream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fileStream);

        return relativePath.Replace('\\', '/');
    }

    public async Task<string> SaveCoverAsync(Guid recoveryKeyId, Guid bookId, byte[] imageBytes, string extension)
    {
        var relativeDir = Path.Combine(recoveryKeyId.ToString(), bookId.ToString());
        var absoluteDir = Path.Combine(UploadsRoot, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var fileName = $"cover{extension}";
        var relativePath = Path.Combine(relativeDir, fileName);
        var absolutePath = Path.Combine(absoluteDir, fileName);

        await File.WriteAllBytesAsync(absolutePath, imageBytes);

        return relativePath.Replace('\\', '/');
    }

    public string GetAbsolutePath(string relativePath)
    {
        return Path.Combine(UploadsRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public async Task<string> SaveAudioAsync(string fileName, byte[] mp3Bytes)
    {
        var absolutePath = Path.Combine(AudioCacheRoot, fileName);
        await File.WriteAllBytesAsync(absolutePath, mp3Bytes);
        return fileName;
    }

    public string GetAudioAbsolutePath(string relativePath)
    {
        return Path.Combine(AudioCacheRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public void DeleteBookFiles(Guid recoveryKeyId, Guid bookId)
    {
        var absoluteDir = Path.Combine(UploadsRoot, recoveryKeyId.ToString(), bookId.ToString());
        if (Directory.Exists(absoluteDir))
        {
            Directory.Delete(absoluteDir, recursive: true);
        }
    }
}
