using System.Collections.Concurrent;
using IstpetDev.Application.Accounts;

namespace IstpetDev.Tests.Support;

// Directorio en memoria con el comportamiento observable de Cognito que usa 013.
public sealed class FakeIdentityDirectory : IIdentityDirectory
{
    private readonly ConcurrentDictionary<string, DirectoryUser> users = new();
    private int invitations;
    private int resends;

    public string Issuer => TestTokens.Issuer;
    public bool Unavailable { get; set; }
    public bool SignOutUnavailable { get; set; }
    public int Invitations => invitations;
    public int Resends => resends;
    public ConcurrentBag<string> SignedOut { get; } = [];
    public IReadOnlyCollection<DirectoryUser> Users => users.Values.ToArray();

    public DirectoryUser Add(string subject, string email, DirectoryUserStatus status = DirectoryUserStatus.Active, bool enabled = true) =>
        users[subject] = new DirectoryUser(subject, email, status, enabled);

    public void Confirm(string subject) => users[subject] = users[subject] with { Status = DirectoryUserStatus.Active };

    public Task<DirectoryUser?> FindByEmailAsync(string email, CancellationToken ct) =>
        Guarded(() => users.Values.SingleOrDefault(u => u.Email == email));

    public Task<DirectoryUser?> FindBySubjectAsync(string subject, CancellationToken ct) =>
        Guarded(() => users.GetValueOrDefault(subject));

    public Task<DirectoryUser> CreateUserAsync(string email, CancellationToken ct) => Guarded(() =>
    {
        lock (users)
        {
            if (users.Values.SingleOrDefault(u => u.Email == email) is { } existing) return existing;
            Interlocked.Increment(ref invitations);
            return Add($"sub-{Guid.NewGuid():N}", email, DirectoryUserStatus.InvitationPending);
        }
    });

    public Task<IReadOnlyList<DirectoryUser>> ListUsersAsync(CancellationToken ct) =>
        Guarded(() => (IReadOnlyList<DirectoryUser>)users.Values.ToList());

    public Task DisableAsync(string subject, CancellationToken ct) => Guarded(() => users[Known(subject)] = users[subject] with { Enabled = false });

    public Task EnableAsync(string subject, CancellationToken ct) => Guarded(() => users[Known(subject)] = users[subject] with { Enabled = true });

    public Task SignOutEverywhereAsync(string subject, CancellationToken ct) =>
        SignOutUnavailable ? throw new IdentityDirectoryUnavailableException("Cognito simulado no revocó sesiones.")
            : Guarded(() => { SignedOut.Add(Known(subject)); return true; });

    public Task ResendInvitationAsync(string email, CancellationToken ct) => Guarded(() => Interlocked.Increment(ref resends));

    private string Known(string subject) =>
        users.ContainsKey(subject) ? subject : throw new IdentityDirectoryUnavailableException("Usuario inexistente en el directorio simulado.");

    private Task<T> Guarded<T>(Func<T> action) =>
        Unavailable ? throw new IdentityDirectoryUnavailableException("Cognito simulado no disponible.") : Task.FromResult(action());
}
