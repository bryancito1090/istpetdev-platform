using IstpetDev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace IstpetDev.Tests.Support;

/// <summary>
/// Fixture de clase: todas las clases comparten un único contenedor PostGIS por proceso de pruebas
/// y cada prueba trabaja en su propia base clonada, así que las clases pueden ejecutarse en paralelo.
/// </summary>
public sealed class PostgisContainer : IAsyncLifetime
{
    // Se arranca una vez por proceso; Testcontainers (Ryuk) retira el contenedor al terminar el proceso.
    private static readonly Lazy<Task<PostgisServer>> Shared = new(PostgisServer.StartAsync);
    private PostgisServer _server = null!;

    public async Task InitializeAsync() => _server = await Shared.Value;

    public string NewDatabase() => _server.NewDatabase();

    public Task<string> NewEmptyDatabaseAsync() => _server.NewEmptyDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}

internal sealed class PostgisServer
{
    // Las migraciones bloquean cada tabla del esquema; varias en paralelo agotan la tabla de locks por defecto (64).
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder().WithImage("postgis/postgis:16-3.4-alpine")
        .WithCommand("-c", "max_locks_per_transaction=1024").Build();
    private readonly string _password = Guid.NewGuid().ToString("N");

    public static async Task<PostgisServer> StartAsync()
    {
        var server = new PostgisServer();
        await server.InitializeAsync();
        return server;
    }

    private async Task InitializeAsync()
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

    // Nombre canónico fijo: solo puede usarlo una prueba por proceso (ver PostgisCollection).
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
}

/// <summary>
/// Colección serial para pruebas que miden tiempos o usan la base canónica fija istpetdev_dev_02.
/// xUnit la ejecuta sola, después de las clases paralelas, para que la carga no altere sus mediciones.
/// El resto de clases usa IClassFixture&lt;PostgisContainer&gt; y corre en paralelo.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgisCollection : ICollectionFixture<PostgisContainer>
{
    public const string Name = "postgis";
}
