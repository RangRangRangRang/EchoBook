using EchoBook.Models;
using EchoBook.Repositories.Interfaces;
using EchoBook.Services.Interfaces;

namespace EchoBook.Services;

public class CurrentRecoveryKeyAccessor : ICurrentRecoveryKeyAccessor
{
    public const string CookieName = "EchoBookActiveKey";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRecoveryKeyRepository _repository;

    public CurrentRecoveryKeyAccessor(IHttpContextAccessor httpContextAccessor, IRecoveryKeyRepository repository)
    {
        _httpContextAccessor = httpContextAccessor;
        _repository = repository;
    }

    public async Task<RecoveryKey?> GetCurrentAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null) return null;

        if (!context.Request.Cookies.TryGetValue(CookieName, out var raw) || !Guid.TryParse(raw, out var id))
        {
            return null;
        }

        return await _repository.GetByIdAsync(id);
    }

    public void SetActiveKeyCookie(Guid recoveryKeyId)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null) return;

        context.Response.Cookies.Append(CookieName, recoveryKeyId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddYears(2),
            IsEssential = true
        });
    }

    public void ClearActiveKeyCookie()
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete(CookieName);
    }
}
