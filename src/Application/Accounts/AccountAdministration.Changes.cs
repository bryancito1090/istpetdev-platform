using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Accounts;

public sealed partial class AccountAdministration
{
    // El directorio es singleton en el API. La tabla débil no retiene instancias de pruebas.
    private static readonly ConditionalWeakTable<IIdentityDirectory, ConcurrentDictionary<string, byte>> PendingRevocations = new();
    private ConcurrentDictionary<string, byte> PendingForDirectory => PendingRevocations.GetValue(directory, _ => new());

    public async Task<AccountItem> UpdateAsync(Guid actorId, Guid id, UpdateAccountInput input, CancellationToken ct)
    {
        DirectoryUser? user = null;
        var account = await ChangeAsync(id, async a =>
        {
            if (input.Role is not null && !Roles.IsHuman(input.Role)) throw AccountProvisioning.InvalidRole();
            var displayName = input.DisplayName is null ? null : AccountProvisioning.ValidateDisplayName(input.DisplayName);
            if (input.Role is not null && input.Role != a.Role)
            {
                if (a.Id == actorId) throw SelfChange();
                await EnsureNotLastManagerAsync(a, ct);
            }
            user = await directory.FindBySubjectAsync(a.Subject, ct);
            if (input.Role is not null) a.Role = input.Role;
            if (displayName is not null) a.DisplayName = displayName;
        }, ct);
        return ToManagedItem(account, user);
    }

    // Persiste el bloqueo antes de tocar Cognito; el desfase se puede reintentar.
    public async Task<AccountItem> DeactivateAsync(Guid actorId, Guid id, CancellationToken ct)
    {
        var account = await ChangeAsync(id, async a =>
        {
            if (a.Id == actorId) throw SelfChange();
            await EnsureNotLastManagerAsync(a, ct);
            a.IsActive = false;
        }, ct);
        PendingForDirectory[account.Subject] = 0;
        try
        {
            await directory.DisableAsync(account.Subject, ct);
            await directory.SignOutEverywhereAsync(account.Subject, ct);
            PendingForDirectory.TryRemove(account.Subject, out _);
        }
        catch (IdentityDirectoryUnavailableException)
        {
            // Account.IsActive ya niega la siguiente petición incluso con el mismo token.
        }
        return await DescribeAsync(account, ct);
    }

    public async Task<AccountItem> ReactivateAsync(Guid id, CancellationToken ct)
    {
        var account = await ChangeAsync(id, a => { a.IsActive = true; return Task.CompletedTask; }, ct);
        try
        {
            await directory.EnableAsync(account.Subject, ct);
            await directory.SignOutEverywhereAsync(account.Subject, ct);
            PendingForDirectory.TryRemove(account.Subject, out _);
        }
        catch (IdentityDirectoryUnavailableException)
        {
            await ChangeAsync(id, a => { a.IsActive = false; return Task.CompletedTask; }, ct);
            throw;
        }
        return await DescribeAsync(account, ct);
    }

    public async Task ResendInvitationAsync(Guid id, CancellationToken ct)
    {
        var account = await database.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, ct) ?? throw NotFound();
        if (account.Kind != "human") throw Technical();
        var user = await directory.FindBySubjectAsync(account.Subject, ct);
        if (user is not { Status: DirectoryUserStatus.InvitationPending })
            throw new AccountRuleException("invitation_not_pending", "La persona ya completó su primer ingreso.");
        await directory.ResendInvitationAsync(user.Email, ct);
    }

    private async Task<Account> ChangeAsync(Guid id, Func<Account, Task> change, CancellationToken ct)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(ct);
        await database.LockOrganizationAsync(ct);
        var account = await database.Accounts.SingleOrDefaultAsync(a => a.Id == id, ct) ?? throw NotFound();
        if (account.Kind != "human") throw Technical();
        await change(account);
        await database.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return account;
    }

    private async Task EnsureNotLastManagerAsync(Account account, CancellationToken ct)
    {
        if (account.Role != Roles.Gerencia || !account.IsActive) return;
        var activeManagers = await database.Accounts.CountAsync(a => a.Kind == "human" && a.IsActive && a.Role == Roles.Gerencia, ct);
        if (activeManagers <= 1) throw new AccountRuleException("last_manager", "Debe quedar al menos una Gerencia activa.");
    }

    private async Task<AccountItem> DescribeAsync(Account account, CancellationToken ct)
    {
        try { return ToManagedItem(account, await directory.FindBySubjectAsync(account.Subject, ct)); }
        catch (IdentityDirectoryUnavailableException)
        {
            return new AccountItem(account.Id, account.DisplayName, account.Role, account.IsActive, null, "unknown", null, true);
        }
    }

    private AccountItem ToManagedItem(Account account, DirectoryUser? user)
    {
        var item = ToItem(account, user);
        return PendingForDirectory.ContainsKey(account.Subject) ? item with { OutOfSync = true } : item;
    }

    private static AccountRuleException NotFound() => new("not_found", "La cuenta no existe.");
    private static AccountRuleException Technical() => new("technical_account", "La cuenta técnica de automatización no se gestiona aquí.");
    private static AccountRuleException SelfChange() => new("self_change_forbidden", "No puedes desactivarte ni cambiar tu propio rol.");
}
