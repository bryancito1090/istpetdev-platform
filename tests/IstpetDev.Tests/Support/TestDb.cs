using IstpetDev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IstpetDev.Tests.Support;

public static class TestDb
{
    public static ApplicationDbContext Context(string connectionString) => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite()).Options);

    public static async Task<ApplicationDbContext> CreateMigratedAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        var db = new ApplicationDbContext(options);
        await DatabaseInitializer.MigrateAsync(db);
        return db;
    }
}
