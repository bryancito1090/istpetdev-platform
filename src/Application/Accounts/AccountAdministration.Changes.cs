using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Accounts;

public sealed partial class AccountAdministration
{
    public async Task<AccountItem> UpdateAsync(Guid actorId, Guid id, UpdateAccountInput input, CancellationToken ct)
    {
        var account = await ChangeAsync(id, async a =>
        {
            if (input.Role is not null && input.Role != a.Role)
            {
                if (!Roles.IsHuman(input.Role)) throw AccountProvisioning.InvalidRole();
                if (a.Id == actorId) throw SelfChange();
                await EnsureNotLastManagerAsync(a, ct);
                a.Role = input.Role;
            }
            if (input.DisplayName is not null) a.DisplayName = AccountProvisioning.ValidateDisplayName(input.DisplayName);
        }, ct);
        return await DescribeAsync(account, ct);
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
        try
        {
            await directory.DisableAsync(account.Subject, ct);
            await directory.SignOutEverywhereAsync(account.Subject, ct);
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
        try { return ToItem(account, await directory.FindBySubjectAsync(account.Subject, ct)); }
        catch (IdentityDirectoryUnavailableException)
        {
            return new AccountItem(account.Id, account.DisplayName, account.Role, account.IsActive, null, "unknown", null, true);
        }
    }

    private static AccountRuleException NotFound() => new("not_found", "La cuenta no existe.");
    private static AccountRuleException Technical() => new("technical_account", "La cuenta técnica de automatización no se gestiona aquí.");
    private static AccountRuleException SelfChange() => new("self_change_forbidden", "No puedes desactivarte ni cambiar tu propio rol.");
}
