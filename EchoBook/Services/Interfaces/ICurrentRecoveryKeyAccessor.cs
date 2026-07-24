using EchoBook.Models;

namespace EchoBook.Services.Interfaces;

/// <summary>
/// Resolves which RecoveryKey the current visitor is browsing as, based on a cookie
/// set after they successfully open their library. There are no logins/sessions -
/// the cookie IS the credential, mirroring the recovery key itself.
/// </summary>
public interface ICurrentRecoveryKeyAccessor
{
    Task<RecoveryKey?> GetCurrentAsync();
    void SetActiveKeyCookie(Guid recoveryKeyId);
    void ClearActiveKeyCookie();
}
