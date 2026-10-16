using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Entities;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

// #197: orden de entregas del asistente por prioridad, fecha requerida y vecino más cercano.
[Trait("Category", "Unit")]
public sealed class DeliveryStopSequencerTests
{
    private static readonly GeoPoint Depot = new(-0.2295m, -78.5243m);
    private static readonly DateTime Day = new(2026, 10, 17, 15, 0, 0, DateTimeKind.Utc);

    private static DeliveryStopDraft Draft(string code, decimal lat, string priority, DateTime required) =>
        new(new RouteStopCandidate(0, RouteStopKind.Delivery, Guid.NewGuid(), null, code, code,
                new GeoPoint(lat, -78.5243m), -1m, 0m, new TimeOnly(8, 0), new TimeOnly(17, 0), 15),
            priority, required);

    private static string[] Codes(IEnumerable<DeliveryStopDraft> drafts) => drafts.Select(d => d.Stop.Code).ToArray();

    [Fact]
    public void Priority_levels_go_before_distance()
    {
        var drafts = new[]
        {
            Draft("LOW-NEAR", -0.2300m, "Low", Day),
            Draft("NORMAL-NEAR", -0.2310m, "Normal", Day),
            Draft("CRITICAL-FAR", -0.3000m, "Critical", Day),
            Draft("HIGH-MID", -0.2600m, "High", Day)
        };

        var ordered = DeliveryStopSequencer.Order(drafts, Depot);

        Assert.Equal(new[] { "CRITICAL-FAR", "HIGH-MID", "NORMAL-NEAR", "LOW-NEAR" }, Codes(ordered));
    }

    [Fact]
    public void Earlier_required_day_goes_first_within_same_priority()
    {
        var drafts = new[]
        {
            Draft("TOMORROW-NEAR", -0.2300m, "Normal", Day.AddDays(1)),
            Draft("TODAY-FAR", -0.3000m, "Normal", Day)
        };

        Assert.Equal(new[] { "TODAY-FAR", "TOMORROW-NEAR" }, Codes(DeliveryStopSequencer.Order(drafts, Depot)));
    }

    [Fact]
    public void Nearest_neighbor_chains_from_previous_stop_within_level()
    {
        // Por distancia a la bodega sería A, B, C; encadenando desde A, C (al norte) queda más cerca que B (al sur).
        var drafts = new[]
        {
            Draft("B", -0.2700m, "Critical", Day),
            Draft("C", -0.1700m, "Critical", Day),
            Draft("A", -0.2000m, "Critical", Day)
        };

        Assert.Equal(new[] { "A", "C", "B" }, Codes(DeliveryStopSequencer.Order(drafts, Depot)));
    }

    [Theory]
    [InlineData(new[] { "Normal", "Critical", "Low" }, "Critical")]
    [InlineData(new[] { "Low", "High" }, "High")]
    [InlineData(new[] { "Desconocida" }, "Desconocida")]
    public void Most_urgent_priority_wins_for_shared_destination(string[] priorities, string expected)
    {
        Assert.Equal(expected, DeliveryStopSequencer.MostUrgent(priorities));
    }

    [Fact]
    public void Unknown_priority_is_ranked_as_normal()
    {
        Assert.Equal(DeliveryStopSequencer.PriorityRank("Normal"), DeliveryStopSequencer.PriorityRank("Otra"));
        Assert.Equal(DeliveryStopSequencer.PriorityRank("Normal"), DeliveryStopSequencer.PriorityRank(null));
    }
}
