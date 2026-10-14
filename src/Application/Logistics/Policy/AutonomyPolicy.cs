using IstpetDev.Application.Logistics.Planning;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics.Policy;

public enum PolicyOutcome
{
    AutoExecuted,
    Escalated
}

public sealed record PolicyDecision(
    PolicyOutcome Outcome,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<Fact> Facts);

public interface IAutonomyPolicy
{
    PolicyDecision Classify(
        VehicleRoute previous,
        VehicleRoute proposed,
        IReadOnlyList<Unserved> unservedMandatory,
        AutonomyMode mode);

    PolicyDecision ClassifyInitialPlan(
        PlanResult planResult,
        AutonomyMode mode);
}

public sealed class AutonomyPolicy(IOptions<LogisticsOptions> options) : IAutonomyPolicy
{
    private readonly PolicyOptions _policy = options.Value.Policy;

    public PolicyDecision Classify(
        VehicleRoute previous,
        VehicleRoute proposed,
        IReadOnlyList<Unserved> unservedMandatory,
        AutonomyMode mode)
    {
        var reasons = new List<string>();
        var facts = new List<Fact>();

        // Si el modo es ProposeOnly, siempre escala
        if (mode == AutonomyMode.ProposeOnly)
        {
            reasons.Add("mode_propose_only");
            facts.Add(new Fact("policy_mode", "mode", 0, "ProposeOnly"));
            return new PolicyDecision(PolicyOutcome.Escalated, reasons, facts);
        }

        // 1. Obligatorios desatendidos
        if (unservedMandatory.Count > 0)
        {
            reasons.Add("dropped_mandatory");
            facts.Add(new Fact("dropped_mandatory_count", proposed.VehicleCode, unservedMandatory.Count, "paradas"));
        }

        // 2. Ventanas horarias incumplidas (LateMinutes > 0)
        var lateStops = proposed.Stops.Where(s => s.LateMinutes.HasValue && s.LateMinutes.Value > 0).ToList();
        if (lateStops.Count > 0)
        {
            reasons.Add("missed_window");
            foreach (var ls in lateStops)
            {
                facts.Add(new Fact("missed_window_minutes", ls.PointCode, ls.LateMinutes!.Value, "min"));
            }
        }

        // 3. Incremento de costo (cost_increase_pct <= MaxCostIncreasePct)
        decimal costDeltaPct = 0m;
        if (previous.Cost > 0)
        {
            costDeltaPct = Math.Round(((proposed.Cost - previous.Cost) / previous.Cost) * 100m, 2);
        }
        else if (proposed.Cost > 0)
        {
            costDeltaPct = 100m;
        }

        facts.Add(new Fact("cost_increase_pct", proposed.VehicleCode, costDeltaPct, "%"));

        if (costDeltaPct > _policy.MaxCostIncreasePct)
        {
            reasons.Add("cost_increase_pct");
        }

        // 4. Retraso máximo de llegada de paradas (max_delay_minutes <= MaxDelayMinutes)
        decimal maxDelay = 0m;
        var prevArrivals = previous.Stops.ToDictionary(s => s.PointCode, s => s.ArrivalUtc);

        foreach (var stop in proposed.Stops)
        {
            if (prevArrivals.TryGetValue(stop.PointCode, out var prevArr))
            {
                var delay = (decimal)(stop.ArrivalUtc - prevArr).TotalMinutes;
                if (delay > maxDelay)
                {
                    maxDelay = delay;
                }
            }
        }

        facts.Add(new Fact("max_delay_minutes", proposed.VehicleCode, maxDelay, "min"));

        if (maxDelay > _policy.MaxDelayMinutes)
        {
            reasons.Add("max_delay_minutes");
        }

        var outcome = reasons.Count == 0 ? PolicyOutcome.AutoExecuted : PolicyOutcome.Escalated;
        return new PolicyDecision(outcome, reasons, facts);
    }

    public PolicyDecision ClassifyInitialPlan(PlanResult planResult, AutonomyMode mode)
    {
        var reasons = new List<string>();
        var facts = new List<Fact>();

        if (mode == AutonomyMode.ProposeOnly || !_policy.AutoPublishInitialPlans)
        {
            reasons.Add("initial_plan_approval_required");
            facts.Add(new Fact("auto_publish_initial_plans", "policy", _policy.AutoPublishInitialPlans ? 1 : 0, "flag"));
            return new PolicyDecision(PolicyOutcome.Escalated, reasons, facts);
        }

        // Si tiene obligatorios sin servir
        if (planResult.Unserved.Count > 0)
        {
            reasons.Add("dropped_mandatory");
        }

        var outcome = reasons.Count == 0 ? PolicyOutcome.AutoExecuted : PolicyOutcome.Escalated;
        return new PolicyDecision(outcome, reasons, facts);
    }
}
