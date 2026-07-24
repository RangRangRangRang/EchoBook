using EchoBook.Services.Interfaces;
using EchoBook.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EchoBook.Controllers;

[Route("Reader")]
public class ReaderController : Controller
{
    private readonly IReaderService _readerService;
    private readonly ICurrentRecoveryKeyAccessor _currentRecoveryKeyAccessor;
    private readonly ISpeechService _speechService;

    public ReaderController(
        IReaderService readerService,
        ICurrentRecoveryKeyAccessor currentRecoveryKeyAccessor,
        ISpeechService speechService)
    {
        _readerService = readerService;
        _currentRecoveryKeyAccessor = currentRecoveryKeyAccessor;
        _speechService = speechService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Index(Guid id)
    {
        var recoveryKey = await _currentRecoveryKeyAccessor.GetCurrentAsync();
        if (recoveryKey is null)
        {
            return RedirectToAction("Index", "Home");
        }

        var bundle = await _readerService.GetReaderBundleAsync(id, recoveryKey.Id);
        if (bundle is null)
        {
            return NotFound();
        }

        return View(bundle);
    }

    [HttpGet("{id:guid}/Chapter/{chapterId:guid}")]
    public async Task<IActionResult> Chapter(Guid id, Guid chapterId)
    {
        var recoveryKey = await _currentRecoveryKeyAccessor.GetCurrentAsync();
        if (recoveryKey is null) return Unauthorized();

        var content = await _readerService.GetChapterContentAsync(id, chapterId, recoveryKey.Id);
        if (content is null) return NotFound();

        return Json(content);
    }

    [HttpGet("{id:guid}/Asset")]
    public async Task<IActionResult> Asset(Guid id, [FromQuery] Guid chapterId, [FromQuery] string src)
    {
        var recoveryKey = await _currentRecoveryKeyAccessor.GetCurrentAsync();
        if (recoveryKey is null) return Unauthorized();

        var asset = await _readerService.GetChapterAssetAsync(id, chapterId, recoveryKey.Id, src);
        if (asset is null) return NotFound();

        return File(asset.Value.Bytes, asset.Value.ContentType);
    }

    [HttpPost("{id:guid}/Progress")]
    public async Task<IActionResult> SaveProgress(Guid id, [FromBody] ReaderProgressUpdateDto dto)
    {
        var recoveryKey = await _currentRecoveryKeyAccessor.GetCurrentAsync();
        if (recoveryKey is null) return Unauthorized();

        var ok = await _readerService.SaveProgressAsync(id, recoveryKey.Id, dto);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/Settings")]
    public async Task<IActionResult> SaveSettings(Guid id, [FromBody] ReaderSettingsDto dto)
    {
        var recoveryKey = await _currentRecoveryKeyAccessor.GetCurrentAsync();
        if (recoveryKey is null) return Unauthorized();

        var ok = await _readerService.SaveSettingsAsync(recoveryKey.Id, dto);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/Bookmarks")]
    public async Task<IActionResult> AddBookmark(Guid id, [FromBody] BookmarkCreateDto dto)
    {
        var recoveryKey = await _currentRecoveryKeyAccessor.GetCurrentAsync();
        if (recoveryKey is null) return Unauthorized();

        var bookmark = await _readerService.AddBookmarkAsync(id, recoveryKey.Id, dto);
        return bookmark is null ? NotFound() : Json(bookmark);
    }

    [HttpPost("{id:guid}/Bookmarks/{bookmarkId:guid}/Delete")]
    public async Task<IActionResult> DeleteBookmark(Guid id, Guid bookmarkId)
    {
        var recoveryKey = await _currentRecoveryKeyAccessor.GetCurrentAsync();
        if (recoveryKey is null) return Unauthorized();

        var ok = await _readerService.DeleteBookmarkAsync(id, recoveryKey.Id, bookmarkId);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/Speech")]
    public async Task<IActionResult> SynthesizeSpeech(Guid id, [FromBody] SpeechRequestDto dto)
    {
        var recoveryKey = await _currentRecoveryKeyAccessor.GetCurrentAsync();
        if (recoveryKey is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Text)) return BadRequest();

        var audioId = await _speechService.GetOrSynthesizeAsync(dto.Text, dto.Voice, dto.Speed);
        return Json(new SpeechResponseDto { AudioId = audioId, Url = $"/Reader/{id}/Speech/{audioId}" });
    }

    [HttpGet("{id:guid}/Speech/{audioId:guid}")]
    public async Task<IActionResult> GetSpeech(Guid id, Guid audioId)
    {
        var recoveryKey = await _currentRecoveryKeyAccessor.GetCurrentAsync();
        if (recoveryKey is null) return Unauthorized();

        var audio = await _speechService.GetAudioAsync(audioId);
        if (audio is null) return NotFound();

        return File(audio.Value.Bytes, audio.Value.ContentType);
    }
}
