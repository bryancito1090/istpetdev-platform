using IstpetDev.Application.Accounts;

namespace IstpetDev.Infrastructure.Identity;

// Sin Authentication:Authority de Cognito no hay directorio: la gestión responde 503 y el login real no existe.
public sealed class UnconfiguredIdentityDirectory : IIdentityDirectory
{
    public string Issuer => string.Empty;
    public Task<DirectoryUser?> FindByEmailAsync(string email, CancellationToken ct) => Fail<DirectoryUser?>();
    public Task<DirectoryUser?> FindBySubjectAsync(string subject, CancellationToken ct) => Fail<DirectoryUser?>();
    public Task<DirectoryUser> CreateUserAsync(string email, CancellationToken ct) => Fail<DirectoryUser>();
    public Task<IReadOnlyList<DirectoryUser>> ListUsersAsync(CancellationToken ct) => Fail<IReadOnlyList<DirectoryUser>>();
    public Task DisableAsync(string subject, CancellationToken ct) => Fail<bool>();
    public Task EnableAsync(string subject, CancellationToken ct) => Fail<bool>();
    public Task SignOutEverywhereAsync(string subject, CancellationToken ct) => Fail<bool>();
    public Task ResendInvitationAsync(string email, CancellationToken ct) => Fail<bool>();

    private static Task<T> Fail<T>() =>
        Task.FromException<T>(new IdentityDirectoryUnavailableException("Authentication:Authority no apunta a un pool de Cognito."));
}
