using System.Text.Json;
using IstpetDev.Application.Field;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

var connection = Environment.GetEnvironmentVariable("FIELD_SEED_CONNECTION") ?? throw new InvalidOperationException("FIELD_SEED_CONNECTION requerida.");
var expectedDatabase = Environment.GetEnvironmentVariable("FIELD_SEED_DATABASE");
var target = new NpgsqlConnectionStringBuilder(connection);
if (expectedDatabase is not ("istpetdev_dev_01" or "istpetdev_integration") || target.Database != expectedDatabase)
    throw new InvalidOperationException("Identidad de base de datos inesperada.");
if (target.Database == "istpetdev_integration" && target.SslMode != SslMode.VerifyFull)
    throw new InvalidOperationException("Producción exige TLS VerifyFull.");
var apply = args.Contains("--apply");
var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
var options = Options.Create(new LogisticsOptions { Clock = new ClockOptions { Mode = ClockMode.Real } });
await using var db = new SeedDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql(connection, o => o.UseNetTopologySuite()).Options);
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
var distances = new DistanceProvider(db, http, options, NullLogger<DistanceProvider>.Instance);
var field = new FieldService(db, configuration, NullLogger<FieldService>.Instance, options);
var publisher = new RoutePublisher(db, new OperationalClock(db, options), options);
var evaluator = new RouteEvaluator(distances, options, db: db);
var report = await new FieldActivitySeed(db, field, publisher, evaluator).RunAsync(apply);
Console.WriteLine(JsonSerializer.Serialize(new { database = expectedDatabase, mode = apply ? "apply" : "rehearsal-rolled-back", report }));

// La herramienta no lee ni modifica metadatos de catálogo; funciona antes y después de esa migración aditiva.
sealed class SeedDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Product>().Ignore(p => p.MetadataJson);
        modelBuilder.Entity<Vehicle>().Ignore(v => v.MetadataJson);
    }
}
