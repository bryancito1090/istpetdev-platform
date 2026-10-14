using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Accounts;

public sealed record AccountItem(Guid Id, string DisplayName, string Role, bool IsActive, string? Email, string InvitationStatus, bool? DirectoryEnabled, bool OutOfSync);

public sealed record CreateAccountInput(string Email, string DisplayName, string Role);

public sealed record UpdateAccountInput(string? DisplayName, string? Role);

// Rol y estado activo desde Account; correo e invitación desde el directorio de identidad.
public sealed partial class AccountAdministration(IApplicationDbContext database, IIdentityDirectory directory, AccountProvisioning provisioning)
{
    private readonly IApplicationDbContext database = database;
    private readonly IIdentityDirectory directory = directory;

    public async Task<IReadOnlyList<AccountItem>> ListAsync(CancellationToken ct)
    {
        var users = (await directory.ListUsersAsync(ct)).ToDictionary(u => u.Subject);
        var accounts = await database.Accounts.AsNoTracking()
            .Where(a => a.Kind == "human" && a.Issuer == directory.Issuer)
            .OrderBy(a => a.DisplayName).ToListAsync(ct);
        return accounts.Select(a => ToItem(a, users.GetValueOrDefault(a.Subject))).ToList();
    }

    public async Task<AccountItem> CreateAsync(CreateAccountInput input, CancellationToken ct)
    {
        var result = await provisioning.ProvisionAsync(input.Email, input.DisplayName, input.Role, ct);
        if (result.Outcome == ProvisionOutcome.AlreadyExists)
            throw new AccountRuleException("account_exists", "Ese correo ya tiene una cuenta.");
        var account = await database.Accounts.AsNoTracking().SingleAsync(a => a.Id == result.AccountId, ct);
        return ToItem(account, result.User);
    }

    private static AccountItem ToItem(Account account, DirectoryUser? user)
    {
        var status = user is null ? "missing" : user.Status switch
        {
            DirectoryUserStatus.InvitationPending => "pending",
            DirectoryUserStatus.Active => "active",
            _ => "other",
        };
        var outOfSync = user is null || user.Enabled != account.IsActive;
        return new(account.Id, account.DisplayName, account.Role, account.IsActive, user?.Email, status, user?.Enabled, outOfSync);
    }
}
