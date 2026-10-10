using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql("Host=localhost;Database=design_time;Username=unused", x => x.UseNetTopologySuite()).Options;
using var database = new ApplicationDbContext(options);
var model = database.GetService<IDesignTimeModel>().Model;
var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(args[0], "docs/schema95.json")));
var entities = model.GetEntityTypes().ToDictionary(e => e.GetTableName()!);
Check(entities.Count == 95, "Expected exactly 95 domain tables");
foreach (var entry in expected.RootElement.EnumerateArray())
{
    var name = entry.GetProperty("name").GetString()!;
    var tenant = entry.GetProperty("scope").GetString() == "T";
    Check(entities.TryGetValue(name, out var possibleEntity), "Missing table: " + name);
    var entity = possibleEntity ?? throw new InvalidOperationException(name);
    foreach (var column in entry.GetProperty("props").EnumerateArray())
    {
        var property = entity.FindProperty(column.GetProperty("name").GetString()!)
            ?? throw new InvalidOperationException("Missing column in " + name);
        Check(property.GetColumnType() == column.GetProperty("sql").GetString(), "Wrong type: " + property.Name);
        Check(property.IsNullable == column.GetProperty("nullable").GetBoolean(), "Wrong nullability: " + property.Name);
    }
    Check(entity.FindProperty("Version")!.IsConcurrencyToken, "Missing optimistic concurrency in " + name);
    foreach (var fk in entity.GetForeignKeys())
    {
        Check(fk.DeleteBehavior == DeleteBehavior.Restrict, "Unexpected cascading deletion in " + name);
        if (fk.PrincipalEntityType.FindProperty("OrganizationId") is null) continue;
        Check(tenant && fk.Properties[0].Name == "OrganizationId" && fk.PrincipalKey.Properties[0].Name == "OrganizationId", "Cross-tenant FK possible in " + name);
    }
    if (tenant)
        Check(entity.GetKeys().Any(k => k.Properties.Select(p => p.Name).SequenceEqual(new[] { "OrganizationId", "Id" })), "Missing tenant alternate key in " + name);
}
Check(entities.Values.Count(e => e.FindProperty("OrganizationId") is not null) == 90, "Expected 90 tenant tables");
Check(!database.Database.HasPendingModelChanges(), "Model and migration snapshot differ");
var migrationAssembly = database.GetService<IMigrationsAssembly>();
Check(migrationAssembly.Migrations.Count == 2 && migrationAssembly.Migrations.Keys.First() == "20261008131600_InitialSchema", "Initial migration must be preserved");
var sql = database.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
Check(sql.IndexOf("Schema95 stopped:", StringComparison.Ordinal) < sql.IndexOf("DROP TABLE", StringComparison.Ordinal), "Data-loss guard must precede legacy DDL");
Check(sql.Split("FORCE ROW LEVEL SECURITY", StringSplitOptions.None).Length - 1 == 95, "Missing FORCE RLS in generated SQL");
Check(sql.Contains("SECURITY DEFINER SET search_path = pg_catalog"), "Identity lookup search_path must be fixed");
for (var i = 0; i < 1000; i++)
{
    var id = BaseEntity.NewId();
    Check(id.ToString()[14] == '7' && "89ab".Contains(id.ToString()[19]), "Invalid UUID v7/variant");
}
Console.WriteLine("PASS: 95 tables, 90 tenant tables, FK isolation, types, concurrency, snapshot, guards, FORCE RLS, UUID v7");

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
