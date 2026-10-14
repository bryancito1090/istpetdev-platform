using System.Text.RegularExpressions;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Accounts;

public enum ProvisionOutcome { Created, Reconciled, AlreadyExists }

public sealed record ProvisionResult(ProvisionOutcome Outcome, Guid AccountId, DirectoryUser User);

// Alta de una persona: usuario en Cognito (que envía la invitación) + Account activa. Idempotente por correo.
public sealed class AccountProvisioning(IApplicationDbContext database, IIdentityDirectory directory)
{
    // Sin comillas ni barras: el correo también viaja dentro del filtro de ListUsers.
    private static readonly Regex EmailShape = new(@"^[^@\s""\\]+@[^@\s""\\]+\.[^@\s""\\]+$", RegexOptions.CultureInvariant);

    public async Task<ProvisionResult> ProvisionAsync(string email, string displayName, string role, CancellationToken ct)
    {
        email = NormalizeEmail(email);
        displayName = ValidateDisplayName(displayName);
        if (!Roles.IsHuman(role)) throw InvalidRole();

        // El bloqueo serializa altas del mismo correo: la compensación nunca deshabilita al usuario que otra petición registró.
        await using var transaction = await database.Database.BeginTransactionAsync(ct);
        await database.LockOrganizationAsync(ct);
        var found = await directory.FindByEmailAsync(email, ct);
        var user = found ?? await directory.CreateUserAsync(email, ct);
        var existing = await database.Accounts.AsNoTracking()
            .Where(a => a.Issuer == directory.Issuer && a.Subject == user.Subject)
            .Select(a => (Guid?)a.Id).SingleOrDefaultAsync(ct);
        if (existing is Guid id) return new(ProvisionOutcome.AlreadyExists, id, user);

        var account = new Account
        {
            Kind = "human", Issuer = directory.Issuer, Subject = user.Subject,
            DisplayName = displayName, Role = role, IsActive = true,
        };
        database.Accounts.Add(account);
        try
        {
            await database.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            database.ChangeTracker.Clear();
            if (found is null) await DisableQuietlyAsync(user.Subject);
            throw;
        }
        return new(found is null ? ProvisionOutcome.Created : ProvisionOutcome.Reconciled, account.Id, user);
    }

    public static string NormalizeEmail(string? email)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length > 254 || !EmailShape.IsMatch(normalized))
            throw new AccountRuleException("invalid_email", "Escribe un correo válido.");
        return normalized;
    }

    public static string ValidateDisplayName(string? displayName)
    {
        var trimmed = (displayName ?? string.Empty).Trim();
        if (trimmed.Length is 0 or > 120)
            throw new AccountRuleException("invalid_display_name", "Escribe el nombre de la persona (máximo 120 caracteres).");
        return trimmed;
    }

    public static AccountRuleException InvalidRole() =>
        new("invalid_role", "Elige uno de los seis roles humanos: Gerencia, Planificador, Auditor, Bodega, Conductor o Receptor.");

    private async Task DisableQuietlyAsync(string subject)
    {
        // Sin Account el usuario no recibe rol (001 FR-17); deshabilitarlo es la segunda barrera, no la única.
        try { await directory.DisableAsync(subject, CancellationToken.None); }
        catch (IdentityDirectoryUnavailableException) { }
    }
}
