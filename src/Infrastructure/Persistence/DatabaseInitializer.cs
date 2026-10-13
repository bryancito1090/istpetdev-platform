using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IstpetDev.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task MigrateAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var opened = connection.State != ConnectionState.Open;
        if (opened) await connection.OpenAsync(cancellationToken);
        try { await connection.ReloadTypesAsync(); }
        finally { if (opened) await connection.CloseAsync(); }
    }
}
