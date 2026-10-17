using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Imports;
using IstpetDev.Application.Simulation;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IstpetDev.Tests.Integration.Simulation;

// Issue #319: el simulador debe leer el dataset importado en la base, no constantes del worker,
// y debe funcionar con Demo:Enabled=false (sin reset ni fixture en disco como fuente).
[Trait("Category", "Integration")]
public sealed class SimulationQuitoDatasetTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static string QuitoConfigFile =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../data/fixtures/demo-quito-planificacion/appsettings.demo-quito.json"));

    [Fact]
    public async Task Imported_Quito_Dataset_Produces_Real_Metrics_And_Respects_Lead_Time_Without_Demo()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Simulation"] = postgres.NewDatabase(),
            ["LOGISTICS_CONFIG_FILE"] = QuitoConfigFile,
            ["Demo:EnableQuitoConfig"] = "true"
        };
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: false, overrides: overrides);
        using var client = api.ClientFor(TestTokens.Planner);
        var health = await client.GetAsync("/health/live");
        Assert.True(health.IsSuccessStatusCode, await health.Content.ReadAsStringAsync());

        using var scope = api.Services.CreateScope();
        var importer = scope.ServiceProvider.GetRequiredService<IImportService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Accounts.Add(new Account
        {
            OrganizationId = DemoOrganization.Id,
            Kind = "human",
            Issuer = TestTokens.Issuer,
            Subject = TestTokens.Planner,
            DisplayName = "Planificación de prueba",
            Role = Roles.Planificador,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();
        var request = Fixtures.Request("demo-quito-planificacion");
        try
        {
            var report = await importer.RunAsync(request);
            Assert.True(report.Applied, string.Join("\n", report.Issues.Select(i => $"{i.Entity}:{i.Row}:{i.Field} {i.Message}")));
        }
        finally
        {
            foreach (var file in request.Files) await file.Content.DisposeAsync();
        }

        var service = scope.ServiceProvider.GetRequiredService<ISimulationService>();
        var queued = await service.QueueComparisonAsync(new SimulationComparisonRequest(
            DatasetVersion: null,
            Seeds: [42, 101, 2024],
            IncludeAdverse: true));

        SimulationComparisonDetail? detail = null;
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        string? lastBody = null;
        HttpStatusCode lastStatus = 0;
        for (var attempt = 0; attempt < 600; attempt++)
        {
            var response = await client.GetAsync($"/api/v1/simulations/comparisons/{queued.ComparisonId}");
            lastStatus = response.StatusCode;
            lastBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                detail = JsonSerializer.Deserialize<SimulationComparisonDetail>(lastBody, jsonOptions);
                if (detail is not null && detail.Status is "Completed" or "Failed" or "Interrupted") break;
            }
            await Task.Delay(500);
        }

        Assert.True(detail is not null, $"HTTP {(int)lastStatus}: {lastBody}");
        Assert.True(detail.Status == "Completed", detail.FailureReason ?? $"Estado: {detail.Status}");
        Assert.Equal("demo-quito-planificacion", detail.DatasetVersion);
        Assert.False(string.IsNullOrWhiteSpace(detail.DatasetHash));
        Assert.Equal(12, detail.Runs.Count);
        Assert.Equal(12, detail.Runs.Count(r => r.Status == "Completed"));

        var reactive = Assert.Single(detail.Runs, r => r.PolicyCode == ReactivePolicy.Version && r.Seed == 42 && r.ScenarioCode == "base");
        var stockouts = Assert.Single(detail.Metrics, m => m.Code == "quiebres_total" && m.Seed == 42 && m.PolicyCode == ReactivePolicy.Version);
        Assert.True(stockouts.Value > 0m, "La corrida reactiva debe tener al menos un quiebre real con el dataset de Quito.");

        var unattended = detail.Metrics.Where(m => m.Code == "demanda_no_atendida").ToList();
        Assert.NotEmpty(unattended);
        Assert.All(unattended, m => Assert.StartsWith("PRD-", m.Sku));
        Assert.DoesNotContain(detail.Metrics, m => m.Sku == "SKU-01" || m.Sku == "SKU-02");

        var punctuality = Assert.Single(detail.Metrics, m => m.Code == "puntualidad" && m.PolicyCode == ReactivePolicy.Version && m.Seed == 42);
        Assert.NotNull(punctuality.Value);
        Assert.NotNull(punctuality.Denominator);
        Assert.True(punctuality.Numerator <= punctuality.Denominator);

        var km = Assert.Single(detail.Metrics, m => m.Code == "km_total" && m.PolicyCode == IstpetDevPolicy.Version && m.Seed == 42);
        Assert.True(km.Value > 0m);

        using var verifyScope = api.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ordered = await db.SimulationEvents
            .Where(e => e.SimulationRunId == reactive.Id && e.Kind == "delivery_ordered")
            .OrderBy(e => e.Sequence)
            .FirstOrDefaultAsync();
        Assert.NotNull(ordered);

        var leadTime = Assert.Single(detail.Metrics, m =>
            m.Code == "lead_time_minimo_dias" && m.PolicyCode == ReactivePolicy.Version && m.Seed == 42);
        Assert.NotNull(leadTime.Value);
        Assert.True(leadTime.Value >= 1m,
            $"La reposición reactiva debe programarse con al menos un día laborable de lead time; valor: {leadTime.Value}.");

        // Evidencia reproducible de la corrida completa (12 runs) para #319.
        var evidenceDir = Path.Combine(RepoRoot(), "docs", "evidence", "simulation-quito");
        Directory.CreateDirectory(evidenceDir);
        await File.WriteAllTextAsync(
            Path.Combine(evidenceDir, $"comparison-{queued.ComparisonId}.json"),
            lastBody ?? "{}");
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "IstpetDev.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? Directory.GetCurrentDirectory();
    }
}
