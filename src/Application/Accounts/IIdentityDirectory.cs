namespace IstpetDev.Application.Accounts;

public enum DirectoryUserStatus { InvitationPending, Active, Other }

public sealed record DirectoryUser(string Subject, string Email, DirectoryUserStatus Status, bool Enabled);

public sealed class IdentityDirectoryUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

// Puerto hacia el directorio de identidad (Cognito). Identifica usuarios por sub, nunca por correo mutable.
public interface IIdentityDirectory
{
    string Issuer { get; }
    Task<DirectoryUser?> FindByEmailAsync(string email, CancellationToken ct);
    Task<DirectoryUser?> FindBySubjectAsync(string subject, CancellationToken ct);
    Task<DirectoryUser> CreateUserAsync(string email, CancellationToken ct);
    Task<IReadOnlyList<DirectoryUser>> ListUsersAsync(CancellationToken ct);
    Task DisableAsync(string subject, CancellationToken ct);
    Task EnableAsync(string subject, CancellationToken ct);
    Task SignOutEverywhereAsync(string subject, CancellationToken ct);
    Task ResendInvitationAsync(string email, CancellationToken ct);
}
