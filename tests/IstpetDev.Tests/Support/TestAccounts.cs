using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Support;

public static class TestAccounts
{
    // Llamar después de crear un cliente con ApiFactory: el host migra y siembra las cuentas al arrancar.
    public static async Task<Guid> SetRoleAsync(string connection, string subject, string role, bool active = true)
    {
        await using var db = TestDb.Context(connection);
        var account = await db.Accounts.SingleAsync(a => a.Subject == subject);
        account.Role = role;
        account.IsActive = active;
        await db.SaveChangesAsync();
        return account.Id;
    }
}
