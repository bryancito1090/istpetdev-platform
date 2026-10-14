using IstpetDev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace IstpetDev.Tests.Support;

public sealed class PostgisContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder().WithImage("postgis/postgis:16-3.4-alpine").Build();
    private readonly string _password = Guid.NewGuid().ToString("N");

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var admin = new NpgsqlConnection(_container.GetConnectionString());
        await admin.OpenAsync();
        foreach (var number in new[] { "01", "02" })
        {
            await Execute(admin, $"CREATE ROLE migrator_dev_{number} LOGIN PASSWORD '{_password}' NOSUPERUSER NOBYPASSRLS; CREATE ROLE user_dev_{number} LOGIN PASSWORD '{_password}' NOSUPERUSER NOBYPASSRLS;");
        }
        await Execute(admin, "CREATE DATABASE istpetdev_dev_01 OWNER migrator_dev_01");
        await PrepareExtensionAsync("istpetdev_dev_01");
        // Historical guard requires the genuine limited migrator and canonical DB name.
        var template = Connection("istpetdev_dev_01", "01", pooling: false);
        await using var db = TestDb.Context(template);
        await db.GetService<IMigrator>().MigrateAsync("20261010023200_CompleteSystemSchema95");
    }

    private async Task PrepareExtensionAsync(string database)
    {
        await using var admin = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = database, Pooling = false }.ConnectionString);
        await admin.OpenAsync();
        await Execute(admin, "CREATE EXTENSION IF NOT EXISTS postgis");
    }

    public string NewDatabase()
    {
        var name = $"t_{Guid.NewGuid():N}";
        using var admin = new NpgsqlConnection(_container.GetConnectionString()); admin.Open();
        using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\" TEMPLATE istpetdev_dev_01 OWNER migrator_dev_01", admin);
        command.ExecuteNonQuery();
        return Connection(name, "01", pooling: false);
    }

    public async Task<string> NewEmptyDatabaseAsync()
    {
        await using var admin = new NpgsqlConnection(_container.GetConnectionString()); await admin.OpenAsync();
        await Execute(admin, "CREATE DATABASE istpetdev_dev_02 OWNER migrator_dev_02");
        await PrepareExtensionAsync("istpetdev_dev_02");
        return Connection("istpetdev_dev_02", "02");
    }

    private string Connection(string database, string number, bool pooling = true) => new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
    { Database = database, Username = $"migrator_dev_{number}", Password = _password, Pooling = pooling }.ConnectionString;
    private static Task<int> Execute(NpgsqlConnection connection, string sql) => new NpgsqlCommand(sql, connection).ExecuteNonQueryAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgisCollection : ICollectionFixture<PostgisContainer>
{
    public const string Name = "postgis";
}
