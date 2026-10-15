using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IstpetDev.Tests.Integration.Field;

[Trait("Category", "Integration")]
public class DeliveryDetailHttpTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    [Fact]
    public async Task Bodega_receives_200_with_lines_and_conductor_receives_403()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var dispatched = await FieldSeed.DispatchedDeliveryAsync(api, 25m, suffix);

        // 1. Conductor intenta consultar GET /api/v1/deliveries/{id} -> 403 Forbidden
        using var driverClient = api.ClientFor(TestTokens.Driver);
        var forbiddenResponse = await driverClient.GetAsync($"/api/v1/deliveries/{dispatched.DeliveryId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        // 2. Modificar rol a Bodega
        await using (var db = TestDb.Context(connection))
        {
            var account = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
            account.Role = Roles.Bodega;
            await db.SaveChangesAsync();
        }

        // 3. Bodega consulta GET /api/v1/deliveries/{id} -> 200 OK con líneas
        using var bodegaClient = api.ClientFor(TestTokens.Driver);
        var okResponse = await bodegaClient.GetAsync($"/api/v1/deliveries/{dispatched.DeliveryId}");
        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);

        var content = await okResponse.Content.ReadAsStringAsync();
        var detail = JsonSerializer.Deserialize<DeliveryDetailResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(detail);
        Assert.Equal(dispatched.DeliveryId, detail.DeliveryId);
        Assert.NotEmpty(detail.Lines);
        Assert.Equal(dispatched.LotId, detail.Lines[0].LotId);
        Assert.Equal(25m, detail.Lines[0].PlannedQuantity);
        Assert.Equal("Punto de prueba", detail.PointName);
    }
}
