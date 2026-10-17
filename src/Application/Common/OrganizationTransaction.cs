using IstpetDev.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace IstpetDev.Application.Common;

public static class OrganizationTransaction
{
    // ponytail: serialización por organización para el volumen de la demo; particionar por recursos
    // solo si se necesita más rendimiento, manteniendo un orden común para rutas, pedidos y saldos.
    public static async Task<IDbContextTransaction?> BeginAsync(IApplicationDbContext db, CancellationToken ct)
    {
        if (!db.Database.IsRelational()) return null;
        var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            await db.LockOrganizationAsync(ct);
            return transaction;
        }
        catch
        {
            if (transaction is not null) await transaction.DisposeAsync();
            throw;
        }
    }
}
