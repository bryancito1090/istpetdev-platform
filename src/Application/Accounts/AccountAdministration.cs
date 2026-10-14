using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Accounts;

public sealed record AccountItem(Guid Id, string DisplayName, string Role, bool IsActive, string? Email, string InvitationStatus, bool? DirectoryEnabled, bool OutOfSync);

public sealed record CreateAccountInput(string? Email, string? DisplayName, string? Role);

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
        var email = AccountProvisioning.NormalizeEmail(input.Email);
        var displayName = AccountProvisioning.ValidateDisplayName(input.DisplayName);
        if (!Roles.IsHuman(input.Role)) throw AccountProvisioning.InvalidRole();
        var role = input.Role!;
        var result = await provisioning.ProvisionAsync(email, displayName, role, ct);
        if (result.Outcome == ProvisionOutcome.AlreadyExists)
            throw new AccountRuleException("account_exists", "Ese correo ya tiene una cuenta.");
        return ToItem(result.AccountId, displayName, role, true, result.User);
    }

    private static AccountItem ToItem(Account account, DirectoryUser? user) =>
        ToItem(account.Id, account.DisplayName, account.Role, account.IsActive, user);

    private static AccountItem ToItem(Guid id, string displayName, string role, bool isActive, DirectoryUser? user)
    {
        var status = user is null ? "missing" : user.Status switch
        {
            DirectoryUserStatus.InvitationPending => "pending",
            DirectoryUserStatus.Active => "active",
            _ => "other",
        };
        var outOfSync = user is null || user.Enabled != isActive;
        return new(id, displayName, role, isActive, user?.Email, status, user?.Enabled, outOfSync);
    }
}
