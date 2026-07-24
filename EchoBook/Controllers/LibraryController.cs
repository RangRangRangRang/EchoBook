using EchoBook.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EchoBook.Controllers;

public class LibraryController : Controller
{
    private const long MaxUploadBytes = 200 * 1024 * 1024; // 200 MB

    private readonly IBookService _bookService;
    private readonly ICurrentRecoveryKeyAccessor _currentRecoveryKeyAccessor;

    public LibraryController(IBookService bookService, ICurrentRecoveryKeyAccessor currentRecoveryKeyAccessor)
    {
        _bookService = bookService;
        _currentRecoveryKeyAccessor = currentRecoveryKeyAccessor;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var recoveryKey = await _currentRecoveryKeyAccessor.GetCurrentAsync();
        if (recoveryKey is null)
        {
            return RedirectToAction("Index", "Home");
        }

        var books = await _bookService.GetLibraryAsync(recoveryKey.Id);
        ViewBag.RecoveryKeyCode = recoveryKey.Code;
        return View(books);
    }

    [HttpGet]
    public async Task<IActionResult> Upload()
    {
        // Upload is reachable even with no active key yet - a key is minted automatically
        // on first successful upload, per spec.
        var recoveryKey = await _currentRecoveryKeyAccessor.GetCurrentAsync();
        ViewBag.HasExistingKey = recoveryKey is not null;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Upload(IFormFile epubFile)
    {
        if (epubFile is null || epubFile.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Choose an .epub file to upload.");
            return View();
        }

        if (!Path.GetExtension(epubFile.FileName).Equals(".epub", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Only .epub files are supported.");
            return View();
        }

        if (epubFile.Length > MaxUploadBytes)
        {
            ModelState.AddModelError(string.Empty, "File is too large (200 MB max).");
            return View();
        }

        var existingKey = await _currentRecoveryKeyAccessor.GetCurrentAsync();

        await using var stream = epubFile.OpenReadStream();
        var (_, recoveryKey) = await _bookService.UploadEpubAsync(
            existingKey?.Id, epubFile.FileName, epubFile.Length, stream);

        // Ensure the browser is now tied to this key, whether it was just created or already existed.
        _currentRecoveryKeyAccessor.SetActiveKeyCookie(recoveryKey.Id);

        TempData["JustCreatedKey"] = existingKey is null ? recoveryKey.Code : null;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var recoveryKey = await _currentRecoveryKeyAccessor.GetCurrentAsync();
        if (recoveryKey is null)
        {
            return RedirectToAction("Index", "Home");
        }

        await _bookService.DeleteBookAsync(recoveryKey.Id, id);
        return RedirectToAction("Index");
    }
}
