using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Application.Logistics.Policy;
using Microsoft.Extensions.Options;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public sealed class AutonomyPolicyTests
{
    private static IOptions<LogisticsOptions> CreateOptions(
        decimal maxCostIncreasePct = 15m,
        decimal maxDelayMinutes = 60m,
        bool autoPublishInitial = false,
        string mode = "AutoWithLimits") =>
        Options.Create(new LogisticsOptions
        {
            Policy = new PolicyOptions
            {
                MaxCostIncreasePct = maxCostIncreasePct,
                MaxDelayMinutes = maxDelayMinutes,
                AutoPublishInitialPlans = autoPublishInitial,
                Mode = mode
            }
        });

    [Fact]
    public void Classify_auto_executes_when_all_limits_and_rules_are_respected()
    {
        var policy = new AutonomyPolicy(CreateOptions());

        var prevStops = new[]
        {
            CreatePlannedStop("HOS-01", new DateTime(2026, 10, 12, 14, 0, 0, DateTimeKind.Utc))
        };
        var propStops = new[]
        {
            CreatePlannedStop("HOS-01", new DateTime(2026, 10, 12, 14, 30, 0, DateTimeKind.Utc)) // 30 min retraso <= 60 min
        };

        var prev = CreateVehicleRoute("VEH-01", 100m, prevStops);
        var prop = CreateVehicleRoute("VEH-01", 110m, propStops); // 10% incremento <= 15%

        var decision = policy.Classify(prev, prop, Array.Empty<Unserved>(), AutonomyMode.AutoWithLimits);

        Assert.Equal(PolicyOutcome.AutoExecuted, decision.Outcome);
        Assert.Empty(decision.Reasons);
        Assert.Contains(decision.Facts, f => f.Code == "cost_increase_pct" && f.Value == 10m);
        Assert.Contains(decision.Facts, f => f.Code == "max_delay_minutes" && f.Value == 30m);
    }

    [Fact]
    public void Classify_accepts_exact_boundary_limits()
    {
        var policy = new AutonomyPolicy(CreateOptions(maxCostIncreasePct: 15m, maxDelayMinutes: 60m));

        var prevStops = new[]
        {
            CreatePlannedStop("HOS-01", new DateTime(2026, 10, 12, 14, 0, 0, DateTimeKind.Utc))
        };
        var propStops = new[]
        {
            CreatePlannedStop("HOS-01", new DateTime(2026, 10, 12, 15, 0, 0, DateTimeKind.Utc)) // Exactamente 60 min retraso
        };

        var prev = CreateVehicleRoute("VEH-01", 100m, prevStops);
        var prop = CreateVehicleRoute("VEH-01", 115m, propStops); // Exactamente 15% incremento

        var decision = policy.Classify(prev, prop, Array.Empty<Unserved>(), AutonomyMode.AutoWithLimits);

        Assert.Equal(PolicyOutcome.AutoExecuted, decision.Outcome);
        Assert.Empty(decision.Reasons);
    }

    [Theory]
    [InlineData(15.1, 0, "cost_increase_pct")]
    [InlineData(10, 61, "max_delay_minutes")]
    public void Classify_escalates_when_cost_or_delay_exceeds_limits(decimal costPct, decimal delayMin, string expectedReason)
    {
        var policy = new AutonomyPolicy(CreateOptions(maxCostIncreasePct: 15m, maxDelayMinutes: 60m));

        var prevStops = new[]
        {
            CreatePlannedStop("HOS-01", new DateTime(2026, 10, 12, 14, 0, 0, DateTimeKind.Utc))
        };
        var propStops = new[]
        {
            CreatePlannedStop("HOS-01", new DateTime(2026, 10, 12, 14, 0, 0, DateTimeKind.Utc).AddMinutes((double)delayMin))
        };

        decimal prevCost = 100m;
        decimal propCost = prevCost * (1m + costPct / 100m);

        var prev = CreateVehicleRoute("VEH-01", prevCost, prevStops);
        var prop = CreateVehicleRoute("VEH-01", propCost, propStops);

        var decision = policy.Classify(prev, prop, Array.Empty<Unserved>(), AutonomyMode.AutoWithLimits);

        Assert.Equal(PolicyOutcome.Escalated, decision.Outcome);
        Assert.Contains(expectedReason, decision.Reasons);
    }

    [Fact]
    public void Classify_escalates_when_unserved_mandatory_stops_exist()
    {
        var policy = new AutonomyPolicy(CreateOptions());
        var prev = CreateVehicleRoute("VEH-01", 100m, Array.Empty<PlannedStop>());
        var prop = CreateVehicleRoute("VEH-01", 100m, Array.Empty<PlannedStop>());

        var unserved = new[] { new Unserved("HOS-01", "capacity", Array.Empty<Fact>()) };
        var decision = policy.Classify(prev, prop, unserved, AutonomyMode.AutoWithLimits);

        Assert.Equal(PolicyOutcome.Escalated, decision.Outcome);
        Assert.Contains("dropped_mandatory", decision.Reasons);
    }

    [Fact]
    public void Classify_escalates_when_time_window_is_violated()
    {
        var policy = new AutonomyPolicy(CreateOptions());
        var prev = CreateVehicleRoute("VEH-01", 100m, Array.Empty<PlannedStop>());
        var lateStop = CreatePlannedStop("HOS-01", DateTime.UtcNow, lateMinutes: 15);
        var prop = CreateVehicleRoute("VEH-01", 100m, new[] { lateStop });

        var decision = policy.Classify(prev, prop, Array.Empty<Unserved>(), AutonomyMode.AutoWithLimits);

        Assert.Equal(PolicyOutcome.Escalated, decision.Outcome);
        Assert.Contains("missed_window", decision.Reasons);
    }

    [Fact]
    public void Classify_always_escalates_under_ProposeOnly_mode()
    {
        var policy = new AutonomyPolicy(CreateOptions());
        var prev = CreateVehicleRoute("VEH-01", 100m, Array.Empty<PlannedStop>());
        var prop = CreateVehicleRoute("VEH-01", 100m, Array.Empty<PlannedStop>());

        var decision = policy.Classify(prev, prop, Array.Empty<Unserved>(), AutonomyMode.ProposeOnly);

        Assert.Equal(PolicyOutcome.Escalated, decision.Outcome);
        Assert.Contains("mode_propose_only", decision.Reasons);
    }

    [Fact]
    public void AutonomyState_supports_thread_safe_read_write_and_reset()
    {
        var state = new AutonomyState(CreateOptions(mode: "AutoWithLimits"));
        Assert.Equal(AutonomyMode.AutoWithLimits, state.CurrentMode);

        state.CurrentMode = AutonomyMode.ProposeOnly;
        Assert.Equal(AutonomyMode.ProposeOnly, state.CurrentMode);

        state.ResetToConfigured();
        Assert.Equal(AutonomyMode.AutoWithLimits, state.CurrentMode);
    }

    private static PlannedStop CreatePlannedStop(string code, DateTime arrivalUtc, int? lateMinutes = null) =>
        new(1, code, Guid.NewGuid(), arrivalUtc, arrivalUtc.AddMinutes(15), TimeSpan.Zero,
            Array.Empty<StopLoad>(), false, lateMinutes);

    private static VehicleRoute CreateVehicleRoute(string vehicleCode, decimal cost, IReadOnlyList<PlannedStop> stops) =>
        new(vehicleCode, Guid.NewGuid(), stops, 10m, 60m, cost, 100m, 1m, false, Array.Empty<Leg>());
}
