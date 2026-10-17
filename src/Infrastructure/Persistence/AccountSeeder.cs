using IstpetDev.Application.Demo;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Infrastructure.Persistence;

public static class AccountSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, string? issuer, IEnumerable<DemoAccount> accounts, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(issuer)) return;
        var pendingSubjects = new HashSet<string>(StringComparer.Ordinal);
        foreach (var demo in accounts)
        {
            if (!Roles.IsValid(demo.Role))
                throw new InvalidOperationException($"Rol '{demo.Role}' no válido para la cuenta '{demo.Subject}'. Válidos: {string.Join(", ", Roles.All)}.");
            if (!pendingSubjects.Add(demo.Subject)) continue;
            var account = await db.Accounts.FirstOrDefaultAsync(a => a.Issuer == issuer && a.Subject == demo.Subject, cancellationToken);
            if (account is null)
            {
                if (string.IsNullOrWhiteSpace(demo.Subject) || demo.Kind is not ("human" or "technical") || (demo.Kind == "technical") != (demo.Role == Roles.Automatizacion))
                    throw new InvalidOperationException("Identidad/tipo/rol de cuenta demo inválidos.");
                db.Accounts.Add(new Account
                {
                    Issuer = issuer,
                    Subject = demo.Subject,
                    Kind = demo.Kind,
                    DisplayName = demo.DisplayName,
                    Role = demo.Role,
                    IsActive = true
                });
            }
            // Una cuenta existente no se reactiva ni se reescribe: una revocación o un cambio
            // de rol hechos por Gerencia deben sobrevivir al arranque del seeder de demo.
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
