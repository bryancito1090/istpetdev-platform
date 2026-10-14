using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public sealed class RebalancingAdvisorTests
{
    private sealed class FakeClock(DateOnly date) : IOperationalClock
    {
        public Task<DateOnly> TodayAsync(CancellationToken ct = default) => Task.FromResult(date);
        public Task<DateTime> WorkdayStartUtcAsync(CancellationToken ct = default) => Task.FromResult(date.ToDateTime(new TimeOnly(7, 0), DateTimeKind.Utc));
        public Task<DateTime> NowUtcAsync(CancellationToken ct = default) => Task.FromResult(DateTime.UtcNow);
    }

    private sealed class FakeAnalyzer(IReadOnlyList<WarehouseProductConsumption> consumptions) : IWarehouseConsumptionAnalyzer
    {
        public Task<IReadOnlyList<WarehouseProductConsumption>> AnalyzeAsync(CancellationToken ct = default) =>
            Task.FromResult(consumptions);

        public Task<WarehouseProductConsumption?> AnalyzeProductWarehouseAsync(Guid locationId, Guid productId, CancellationToken ct = default) =>
            Task.FromResult(consumptions.FirstOrDefault(c => c.LocationId == locationId && c.ProductId == productId));
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WhenDisparityExists_SuggestsBalancingTransfer()
    {
        // Caso Pinoklin:
        // Bodega A: 100 stock, consumo diario ~0.33 (10/mes). Cobertura ~300 días.
        // Bodega B: 50 stock, consumo diario ~2.67 (80/mes). Cobertura ~18.7 días.
        // Stock total = 150, consumo diario total = 3.0.
        // Cobertura equilibrada de red = 150 / 3.0 = 50 días.
        // Stock target Bodega A = 0.33 * 50 = 16.7 ~ 17 unidades. (Exceso = 100 - 17 = 83).
        // Stock target Bodega B = 2.67 * 50 = 133.5 ~ 133 unidades. (Déficit = 133 - 50 = 83).
        // Traslado sugerido: ~83 unidades de A hacia B.

        var options = Options.Create(new LogisticsOptions
        {
            Rebalancing = new RebalancingOptions
            {
                MinCoverageDifferenceDays = 15m,
                MinTransferThresholdPct = 5m
            }
        });

        var clock = new FakeClock(new DateOnly(2026, 10, 14));

        var prodId = Guid.NewGuid();
        var whAId = Guid.NewGuid();
        var whBId = Guid.NewGuid();

        var consumptions = new List<WarehouseProductConsumption>
        {
            new(whAId, "BOD-A", prodId, "PINOKLIN", "Pinoklin 1L", 30m, 90, 0.333m, 100m, 0m, 100m, 300m, "High"),
            new(whBId, "BOD-B", prodId, "PINOKLIN", "Pinoklin 1L", 240m, 90, 2.667m, 50m, 0m, 50m, 18.7m, "High")
        };

        var analyzer = new FakeAnalyzer(consumptions);

        var dbOptions = new DbContextOptionsBuilder<IstpetDev.Infrastructure.Persistence.ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new IstpetDev.Infrastructure.Persistence.ApplicationDbContext(dbOptions);

        var advisor = new RebalancingAdvisor(db, analyzer, clock, options);

        var suggestions = await advisor.GenerateSuggestionsAsync();

        Assert.NotEmpty(suggestions);
        var sug = Assert.Single(suggestions, s => s.ProductId == prodId);
        Assert.Equal(whAId, sug.SourceLocationId);
        Assert.Equal(whBId, sug.TargetLocationId);
        Assert.InRange(sug.SuggestedQuantity, 75m, 85m);
        Assert.Equal(TransferSuggestionStatus.Suggested, sug.Status);
        Assert.Contains("PINOKLIN", sug.Rationale);
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WhenBalanced_DoesNotSuggest()
    {
        var options = Options.Create(new LogisticsOptions
        {
            Rebalancing = new RebalancingOptions
            {
                MinCoverageDifferenceDays = 15m
            }
        });

        var clock = new FakeClock(new DateOnly(2026, 10, 14));

        var prodId = Guid.NewGuid();
        var whAId = Guid.NewGuid();
        var whBId = Guid.NewGuid();

        // Coberturas equilibradas
        var consumptions = new List<WarehouseProductConsumption>
        {
            new(whAId, "BOD-A", prodId, "JABON", "Jabón", 90m, 90, 1.0m, 30m, 0m, 30m, 30m, "High"),
            new(whBId, "BOD-B", prodId, "JABON", "Jabón", 90m, 90, 1.0m, 28m, 0m, 28m, 28m, "High")
        };

        var analyzer = new FakeAnalyzer(consumptions);

        var dbOptions = new DbContextOptionsBuilder<IstpetDev.Infrastructure.Persistence.ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new IstpetDev.Infrastructure.Persistence.ApplicationDbContext(dbOptions);

        var advisor = new RebalancingAdvisor(db, analyzer, clock, options);

        var suggestions = await advisor.GenerateSuggestionsAsync();

        Assert.Empty(suggestions);
    }
}
