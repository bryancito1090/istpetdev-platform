using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Support;

// Siembra directa en la base para probar recepción y consulta pública sin depender del recorrido de 002.
public static class FieldSeed
{
    public sealed record Dispatched(
        Guid DeliveryId,
        Guid DeliveryLineId,
        Guid LotId,
        Guid PointLocationId,
        Guid VehicleLocationId,
        long DeliveryVersion,
        Guid? UnitId = null,
        Guid DriverAccountId = default,
        string? UnitToken = null);

    // suffix hace únicos los códigos cuando una prueba siembra varias entregas en la misma base.
    // withHandlingUnit crea un bulto ya despachado en el vehículo; unitToken emite su token QR (#162).
    public static async Task<Dispatched> DispatchedDeliveryAsync(
        ApiFactory api,
        decimal quantity,
        string suffix = "T",
        bool withHandlingUnit = false,
        string? unitToken = null)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var driver = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
        var product = new Product { OrganizationId = DemoOrganization.Id, Sku = $"SKU-{suffix}", Name = "Producto de prueba", BaseUnit = "L", IsActive = true };
        var lot = new Lot { OrganizationId = DemoOrganization.Id, ProductId = product.Id, Code = $"LOTE-{suffix}" };
        var point = new ServicePoint { OrganizationId = DemoOrganization.Id, Code = $"P-{suffix}", Name = "Punto de prueba", Criticality = "normal", IsActive = true };
        var warehouse = new Location { OrganizationId = DemoOrganization.Id, Code = $"B-{suffix}", Name = "Bodega de prueba", Kind = LocationKinds.Warehouse, IsActive = true };
        var pointLocation = new Location { OrganizationId = DemoOrganization.Id, Code = $"P-{suffix}", Name = "Punto de prueba", Kind = LocationKinds.ServicePoint, ServicePointId = point.Id, IsActive = true };
        var vehicle = new Vehicle { OrganizationId = DemoOrganization.Id, Code = $"V-{suffix}", DepotLocationId = warehouse.Id, MaxWeight = 1000, IsActive = true };
        var vehicleLocation = new Location { OrganizationId = DemoOrganization.Id, Code = $"V-{suffix}", Name = "Vehículo de prueba", Kind = LocationKinds.Vehicle, VehicleId = vehicle.Id, IsActive = true };
        var delivery = new Delivery
        {
            OrganizationId = DemoOrganization.Id,
            Code = $"D-{suffix}",
            OriginLocationId = warehouse.Id,
            ServicePointId = point.Id,
            Status = DeliveryStates.Dispatched,
            AssignedAccountId = driver.Id,
            DispatchedAtUtc = DateTime.UtcNow,
        };
        var line = new DeliveryLine { OrganizationId = DemoOrganization.Id, DeliveryId = delivery.Id, LotId = lot.Id, PlannedQuantity = quantity, DispatchedQuantity = quantity };
        db.AddRange(product, lot, point, warehouse, pointLocation, vehicle, vehicleLocation, delivery, line,
            new StockBalance { OrganizationId = DemoOrganization.Id, LocationId = vehicleLocation.Id, LotId = lot.Id, QuantityOnHand = quantity });

        HandlingUnit? unit = null;
        if (withHandlingUnit)
        {
            unit = new HandlingUnit
            {
                OrganizationId = DemoOrganization.Id,
                Code = $"HU-{suffix}",
                DeliveryId = delivery.Id,
                LocationId = vehicleLocation.Id,
                Status = HandlingUnitStates.Dispatched,
            };
            db.Add(unit);
            db.Add(new HandlingUnitLine { OrganizationId = DemoOrganization.Id, HandlingUnitId = unit.Id, DeliveryLineId = line.Id, LotId = lot.Id, Quantity = quantity });
            if (unitToken is not null)
            {
                db.Add(new QrAccessToken
                {
                    OrganizationId = DemoOrganization.Id,
                    HandlingUnitId = unit.Id,
                    TokenHash = HashToken(unitToken),
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                    CreatedByAccountId = driver.Id,
                });
            }
        }

        await db.SaveChangesAsync();
        return new Dispatched(delivery.Id, line.Id, lot.Id, pointLocation.Id, vehicleLocation.Id, delivery.Version, unit?.Id, driver.Id, unit is null ? null : unitToken);
    }

    // #162: lleva la entrega sembrada al estado Delivered con los comandos reales del conductor asignado.
    public static async Task DeliverUnitAsync(ApiFactory api, Dispatched d)
    {
        using var scope = api.Services.CreateScope();
        var field = scope.ServiceProvider.GetRequiredService<IFieldService>();
        await field.PickupAsync(d.DeliveryId, new CustodyTransitionRequest(BaseEntity.NewId(), DateTime.UtcNow), d.DriverAccountId, $"pickup-{Guid.NewGuid()}", CancellationToken.None);
        await field.DeliverAsync(d.DeliveryId, new CustodyTransitionRequest(BaseEntity.NewId(), DateTime.UtcNow), d.DriverAccountId, $"deliver-{Guid.NewGuid()}", CancellationToken.None);
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    public static async Task<decimal> OnHandAsync(ApiFactory api, Guid locationId, Guid lotId)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.StockBalances.Where(b => b.LocationId == locationId && b.LotId == lotId)
            .Select(b => b.QuantityOnHand).SingleOrDefaultAsync();
    }
}
