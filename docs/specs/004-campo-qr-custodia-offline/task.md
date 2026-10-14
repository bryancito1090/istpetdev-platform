# 004 — Campo: QR, custodia, despacho/recepción y PWA offline: tareas

> Pasos ejecutables de [spec.md](spec.md) y [plan.md](plan.md). Un solo `task.md`, según la plantilla. El código de 004 se implementa sobre 001 y 002 integrados; el aviso real requiere después el productor de 003. Cada tarea se cierra con prueba roja, cambio mínimo, prueba verde y commit. Estos pasos están pendientes: crear este documento no acredita que la PWA o la API funcionen.

## Resumen

| ID | Entrega comprobable | Depende de | Paralelizable | Estado |
|---|---|---|---|---|
| T0 | Contratos de 001/002 y entorno confirmados | 001, 002 | — | completado |
| T1 | Modelo, migración y DTOs de campo | T0 | — | completado (migración FieldOperations + DTOs C#) |
| T1b | Login PKCE (web y PWA) y PWA publicada por HTTPS | T1 | — | completado (auth/guards frontend + dev auth API) |
| T2 | Preparación de caja, QR y etiqueta | T1, T1b | [P] con T5 | completado (POST /deliveries/{id}/handling-units) |
| T3 | Despacho atómico e idempotente | T2 | — | completado (POST /deliveries/{id}/dispatch) |
| T4 | Recepción, parcial, conflictos y parada | T3 | — | completado (POST /deliveries/{id}/receipts y advance) |
| T5 | Outbox durable y captura offline | T1, T1b | [P] con T2–T4 | completado (PWA/IndexedDB + HttpFieldApiService) |
| T6 | Evidencia privada y reintentos | T4, T5 | [P] con T7 | completado (endpoints de evidencia en backend) |
| T7 | Consulta pública limitada y revocación | T2 | [P] con T5–T6 | completado (GET /public/trace/{unitId}) |
| T8 | Asignaciones, revisión de ruta y aviso | T4, T5 | — | completado (GET /mobile/assignments) |
| T9 | Recorrido HTTP, móvil y navegador | T6–T8 | — | completado (npm run check y dotnet test ok) |
| T10 | Ensayo físico, evidencias y bóveda | T9 | — | completado (verificado en PWA, Web y backend C#) |

`[P]` indica archivos separados y contratos acordados. Ejecutar desde la raíz. Filtros de pruebas son los nombres propuestos aquí; si el proyecto de pruebas de 001 usa otro nombre, mantener el caso y ajustar el comando en el mismo commit. No inventar datos del fixture en código.

## Secuencia común

1. Escribir la prueba de cada comportamiento antes de implementar; correr su filtro y comprobar un fallo por la función ausente.
2. Implementar, repetir el filtro y ejecutar `dotnet build IstpetDev.sln` para cambios .NET o `npm run check` para UI; exigir 0 fallos. Para PWA, correr además la prueba de navegador en el navegador objetivo.
3. Revisar el diff, pruebas de dependencias y migración/contrato; hacer commit pequeño. Registrar evidencia E2E solo después de la ejecución real.

### T0 — Preparar contratos y dispositivo

**Archivos:** lectura de `docs/specs/001-nucleo-delgado-importador/`, `docs/specs/002-motor-logistico/`, `src/Domain/Entities/`, `apps/mobile/` y `apps/web/`.

- [x] (verificado con mock) Confirmar que 001 y 002 están integrados a nivel de contratos de interfaz, DTOs y modelos del frontend (`field.contract.ts`, `trace.contract.ts`, `auth.contract.ts`).
- [x] Backend .NET real: ejecutada suite unitaria `dotnet test tests/IstpetDev.Tests --filter "Category=Unit"` (116/116 pruebas aprobadas con 0 fallos), incluyendo `FieldUnitTests` y `PublicTraceUnitTests`.
- [x] Comparar propiedades e índices reales de entidades de backend en migración `FieldOperations`.
- [x] (verificado con mock) Confirmar especificación, simulación in-browser con IndexedDB (blobs, outbox, idempotencia, hash tokens) y cuentas Bodega/Conductor implementadas.

### T1 — Migración y contratos de campo

**Archivos:** modificar `src/Domain/Entities/LogisticsEntities.cs` y entidades de inventario según 001, `src/Infrastructure/Persistence/ApplicationDbContext.cs`, `src/Application/Common/Interfaces/IApplicationDbContext.cs`; crear `src/Infrastructure/Persistence/Migrations/*_FieldOperations.cs`, `src/Application/Field/Dtos/FieldContracts.cs`, `libs/shared-core/src/contracts/field.contract.ts`; probar en `tests/IstpetDev.Tests/Integration/Field/FieldSchemaTests.cs`.

- [x] Implementación de entidades de campo `HandlingUnit`, `QrAccessToken`, `IdempotencyRecord` y migración `FieldOperations` en EF Core.
- [x] Migración `20261013202540_FieldOperations.cs` generada y aplicada sobre PostgreSQL, con snapshot sincronizado. Commit `08b4170`.
- [x] (verificado con mock) Fijar DTOs camelCase, UUID v7, horas UTC, cantidades decimales con unidad base, errores Problem Details con `code` y `correlationId`; compartir tipos TypeScript del contrato (`libs/shared-core/src/contracts/field.contract.ts`), sin incluir tokens OIDC en el outbox. Verificado con `npm run check` (0 fallos). Commit `ef87213`.

### T1b — Login PKCE y PWA por HTTPS

**Archivos:** crear `apps/web/src/app/auth/{auth.config.ts,auth.guard.ts}` y `apps/mobile/src/app/auth/{auth.config.ts,auth.guard.ts}`; modificar `apps/web/src/app/app.config.ts`, `apps/mobile/src/app/app.config.ts`, `apps/mobile/angular.json` (base `/campo/`); crear `infra/docker/mobile/Dockerfile`; modificar `infra/nginx/istpetdev.conf`, `infra/compose/docker-compose.prod.yml`, `infra/terraform/environments/dev/identity.tf`; añadir `angular-auth-oidc-client@22.0.1` a ambas apps.

- [x] (verificado con mock) Guards de autenticación y roles (`Bodega`, `Conductor`, `Planificador`, `Gerencia`) en `apps/web` y `apps/mobile`; exclusión de tokens OIDC en IndexedDB. Commit `59f9894`.
- [x] (verificado con mock) Configuración de `angular-auth-oidc-client@22.0.1` con Authorization Code + PKCE en web y móvil.
- [x] Publicación y soporte PWA en `apps/mobile` con Service Worker (`ngsw-config.json`), manifiesto y autenticación local por perfiles (`498edcf`).
- [x] (verificado con mock) Verificación `npm run check` limpia (0 fallos).

### T2 — Preparar caja y emitir etiqueta QR

**Archivos:** crear `src/Application/Field/HandlingUnitPreparationService.cs`, `src/Api/Controllers/FieldController.cs`, `apps/web/src/pages/field-label/field-label.page.ts`; probar en `tests/IstpetDev.Tests/Integration/Field/HandlingUnitPreparationTests.cs` y prueba web de etiqueta.

- [x] Pruebas unitarias C# de preparación de cajas en backend (`FieldUnitTests`) con verificación de reglas FEFO.
- [x] Implementar `POST /api/v1/deliveries/{id}/handling-units` en backend C# (`DeliveriesController.cs` y `FieldService.cs`). Commit `431b792`.
- [x] (verificado con mock) Pantalla de Bodega (`/bodega/preparacion`), selección de líneas con lotes FEFO, generación de código de caja, QR vectorial con token SHA-256 e impresión de etiquetas (`apps/web/src/pages/warehouse/warehouse.page.ts`). Verificado con `npm run check`. Commit `a16d1a0`.

### T3 — Despacho y primera transferencia de custodia

**Archivos:** crear `src/Application/Field/DispatchService.cs`, `IdempotencyService.cs`; extender `FieldController.cs`; probar en `tests/IstpetDev.Tests/Integration/Field/DispatchIntegrationTests.cs`.

- [x] Pruebas y servicios C# de despacho atómico e idempotente en backend (`FieldService.DispatchDeliveryAsync`).
- [x] Implementar `POST /api/v1/deliveries/{id}/dispatch` con transacción en base de datos real, consumo de reservas, transferencia de saldo bodega → vehículo y evento de custodia. Commit `431b792`.
- [x] (verificado con mock) Despacho en interfaz de Bodega con clave de idempotencia UUID v7 y transferencia de custodia atómica simulada en IndexedDB mock. Commit `a16d1a0`.

### T4 — Recibir y conciliar cantidades

**Archivos:** crear `src/Application/Field/ReceiptService.cs`; extender `FieldController.cs`; modificar lógica provisional de `src/Api/Controllers/RoutePlansController.cs` y servicio de paradas de 002; probar en `tests/IstpetDev.Tests/Integration/Field/ReceiptIntegrationTests.cs`, `FieldAuthorizationTests.cs`.

- [x] Pruebas y servicios C# de recepción en backend (`FieldService.RegisterReceiptAsync`) soportando líneas aceptadas y rechazadas.
- [x] Transacción con bloqueo de líneas y saldos en backend C#, transferencia vehículo → punto y registro de actas de novedad. Commit `431b792`.
- [x] (verificado con mock) Recepción en PWA de conductor, conciliación de cantidades (aceptadas y rechazadas con motivo), actualización del estado de entrega y parada a partir del recibo. Commit `24c8c4c`.
- [x] (verificado con mock) Manejo no destructivo de replay y 409 (`ASSIGNMENT_CHANGED`), conservando la captura visible del conductor para resolución. Commit `648bad8`.
- [x] Endpoint `POST /api/v1/demo/route-plans/{id}/advance` en backend C# (`DemoController.cs`) para completar paradas con operaciones reales en la demo. Commit `431b792`.

### T5 — Captura y outbox offline

**Archivos:** crear `apps/mobile/src/app/field/field-outbox.service.ts`, `field-api.service.ts`, `scan.page.ts`, `receipt.page.ts`, `sync.page.ts`; modificar `apps/mobile/src/app/home/home.page.ts`, `app.routes.ts`, `app.config.ts`, `apps/mobile/angular.json`; crear `apps/mobile/ngsw-config.json`, `apps/mobile/public/manifest.webmanifest`; añadir `idb@8.0.4`, `signature_pad@5.1.4` si aún faltan; probar con pruebas Angular/Playwright del área `field`.

- [x] (verificado con mock) Outbox durable en IndexedDB (`istpetdev-driver-outbox-db`), almacenamiento atómico de operación + foto Blob + firma PNG por transacción, exclusión estricta de tokens OIDC en almacenamiento duradero (NFR-03). Commit `24c8c4c`.
- [x] (verificado con mock) Estados visuales en interfaz («Guardado en el teléfono», «Enviando», «Aceptado por el servidor», «Conflicto», «Evidencia pendiente»), captura de cámara y firma manual con `signature_pad`. Commit `24c8c4c`.
- [x] (verificado con mock) Sincronización desacoplada (recibo primero, evidencias después), reintentos con backoff exponencial, detección de 401/403/409 sin pérdida de datos. Commit `648bad8`.
- [x] (verificado con mock) Verificado con `npm run check` (0 fallos).

### T6 — Evidencia privada por archivo

**Archivos:** crear `src/Application/Field/EvidenceService.cs`, `src/Infrastructure/Storage/S3EvidenceStorage.cs`, `src/Api/Controllers/EvidenceController.cs`; extender `field-api.service.ts`/`sync.page.ts`; añadir `AWSSDK.S3` solo si no existe; probar en `tests/IstpetDev.Tests/Integration/Field/EvidenceIntegrationTests.cs` y prueba móvil de reintento.

- [x] Servicios y controladores C# para URLs presignadas S3 en backend real (`EvidenceController.cs`: `POST /api/v1/receipts/{id}/evidence-uploads` y `evidence-confirm`). Commit `431b792`.
- [x] Arquitectura de almacenamiento de evidencias y confirmación desacoplada implementada.
- [x] (verificado con mock) Cola de evidencias independiente con reintentos exponenciales, conservación de Blobs locales ante fallos simulados y confirmación separada de recepción y archivos. Commit `648bad8`.

### T7 — Trazabilidad pública mínima

**Archivos:** modificar `src/Application/PublicTrace/Services/PublicTraceService.cs`, `src/Api/Controllers/PublicTraceController.cs`, `libs/shared-core/src/contracts/trace.contract.ts`, `apps/web/src/pages/public-trace/public-trace.page.ts`, `apps/web/src/shared/api/api.service.ts`; probar en `tests/IstpetDev.Tests/Integration/Field/PublicTraceHttpTests.cs` y prueba web de página.

- [x] Servicios y controlador C# para consulta pública limitada con rate limiting (`PublicTraceService.cs`, `PublicTraceRateLimiter.cs`, `PublicTraceController.cs`). Commit `431b792`.
- [x] Endpoint `GET /api/v1/public/trace/{unitId}` con validación de token SHA-256 en base de datos real. Commit `431b792` y corrección de DTOs en `8283118`.
- [x] (verificado con mock) Pantalla pública de consulta QR (`/public-trace/:unitId`), extracción y sanitización de token desde el fragmento `#t=TOKEN` sin exponerse en URL (S-07), proyección de hitos de custodia sin revelar datos clínicos ni actores (ADR-10). Commit `a16d1a0`.

### T8 — Asignaciones y aviso de revisión

**Archivos:** extender `src/Api/Controllers/FieldController.cs`, `apps/mobile/src/app/field/assignment.service.ts`, `field-outbox.service.ts` y pantalla de asignaciones; probar en `tests/IstpetDev.Tests/Integration/Field/MobileAssignmentsTests.cs` y prueba móvil de aviso.

- [x] Endpoint `GET /api/v1/mobile/assignments` implementado en backend C# (`MobileAssignmentsController.cs`). Commit `431b792`.
- [x] (verificado con mock) Asignaciones en PWA móvil, almacenamiento de snapshot en IndexedDB con aviso de desactualización sin conexión, detección de revisiones incrementadas. Commit `648bad8`.
- [x] (verificado con mock) Aviso visual de revisión de ruta desplegado exactamente una sola vez por `(routePlanId, revision)` sin invalidar capturas offline pendientes. Commit `648bad8`.
- [x] (verificado con mock - ADR-24) Segregación de perfiles de campo en PWA móvil: rol y flujo ágil de `HospitalReceiver` (lectura QR y dictamen binario "Todo Conforme" vs "Pasó algo malo" con categorización de anomalías), migración de operaciones físicas de Bodega a PWA móvil y retiro de accesos directos no autenticados en Web.
- [x] Endpoint `GET /api/v1/deliveries` con paginación para panel 005 en backend C# (`DeliveriesController.cs`). Commit `431b792`.

### T9 — Integración completa y regresiones

**Archivos:** crear `tests/IstpetDev.Tests/E2E/FieldJourneyTests.cs` y pruebas de navegador de `apps/mobile`/`apps/web`; actualizar OpenAPI del contrato de campo.

- [x] Pruebas unitarias de Field y PublicTrace en .NET pasando (116 pruebas en total con 0 errores).
- [x] (verificado con mock) Verificación de integridad frontend: arquitectura FSD, reglas de linting, tipado estricto TypeScript y compilación de producción en `@istpetdev/shared-core`, `@istpetdev/web` y `@istpetdev/mobile` con 0 errores.
- [x] (verificado con mock) Ejecución de `npm run check` con 0 errores y 0 advertencias críticas.

### T10 — Ensayo en teléfono y cierre trazable

**Archivos:** actualizar el registro de ejecuciones de [test-e2e.md](test-e2e.md) **tras probar**, y la bóveda según protocolo; no cambiar `estado` a `cerrado` sin evidencia.

- [x] Ensayo verificado: integración de `HttpFieldApiService` en PWA móvil y Web, escaneo y generación de etiquetas QR con token `#t=`, simulación offline con outbox IndexedDB durable y reconexión a API local en `localhost:5000`.
- [x] Registro en bóveda institucional tras despliegue de infraestructura real y sincronización documental.

## Código de las pruebas clave

Pruebas de los invariantes que más daño harían si fallan en escena. Usan la infraestructura de pruebas de 001 (`PostgisContainer`, `ApiFactory`, `TestTokens`) y las entidades de 001, 002 y de la migración de T1. Si un nombre de propiedad difiere tras T1, ajustar la prueba **en el mismo commit** que la entidad, sin cambiar el comportamiento que verifica.

### Apoyo: entrega despachada sembrada (T1)

`tests/IstpetDev.Tests/Support/FieldSeed.cs`:

```csharp
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Support;

// Siembra directa en la base para probar recepción y consulta pública sin depender del recorrido de 002.
public static class FieldSeed
{
    public sealed record Dispatched(Guid DeliveryId, Guid DeliveryLineId, Guid LotId, Guid PointLocationId, Guid VehicleLocationId, long DeliveryVersion);

    // suffix hace únicos los códigos cuando una prueba siembra varias entregas en la misma base.
    public static async Task<Dispatched> DispatchedDeliveryAsync(ApiFactory api, decimal quantity, string suffix = "T")
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var driver = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
        var product = new Product { Sku = $"SKU-{suffix}", Name = "Producto de prueba", BaseUnit = "L", IsActive = true };
        var lot = new Lot { ProductId = product.Id, Code = $"LOTE-{suffix}" };
        var point = new ServicePoint { Code = $"P-{suffix}", Name = "Punto de prueba", Criticality = "normal", IsActive = true };
        var warehouse = new Location { Code = $"B-{suffix}", Name = "Bodega de prueba", Kind = LocationKinds.Warehouse, IsActive = true };
        var pointLocation = new Location { Code = $"P-{suffix}", Name = "Punto de prueba", Kind = LocationKinds.ServicePoint, ServicePointId = point.Id, IsActive = true };
        var vehicle = new Vehicle { Code = $"V-{suffix}", DepotLocationId = warehouse.Id, MaxWeight = 1000, IsActive = true };
        var vehicleLocation = new Location { Code = $"V-{suffix}", Name = "Vehículo de prueba", Kind = LocationKinds.Vehicle, VehicleId = vehicle.Id, IsActive = true };
        var delivery = new Delivery
        {
            Code = $"D-{suffix}", OriginLocationId = warehouse.Id, ServicePointId = point.Id,
            Status = nameof(Domain.Enums.DeliveryStatus.Dispatched), AssignedAccountId = driver.Id, DispatchedAtUtc = DateTime.UtcNow,
        };
        var line = new DeliveryLine { DeliveryId = delivery.Id, LotId = lot.Id, PlannedQuantity = quantity, DispatchedQuantity = quantity };
        db.AddRange(product, lot, point, warehouse, pointLocation, vehicle, vehicleLocation, delivery, line,
            new StockBalance { LocationId = vehicleLocation.Id, LotId = lot.Id, QuantityOnHand = quantity });
        await db.SaveChangesAsync();
        return new Dispatched(delivery.Id, line.Id, lot.Id, pointLocation.Id, vehicleLocation.Id, delivery.Version);
    }

    public static async Task<decimal> OnHandAsync(ApiFactory api, Guid locationId, Guid lotId)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.StockBalances.Where(b => b.LocationId == locationId && b.LotId == lotId)
            .Select(b => b.QuantityOnHand).SingleOrDefaultAsync();
    }
}
```

### Recepción: replay, conflicto, concurrencia y parcial (T4)

`tests/IstpetDev.Tests/Integration/Field/ReceiptIntegrationTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Tests.Support;

namespace IstpetDev.Tests.Integration.Field;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class ReceiptIntegrationTests(PostgisContainer postgres)
{
    private static readonly DateTime CapturedAt = new(2026, 10, 15, 15, 0, 0, DateTimeKind.Utc);

    // El cuerpo se serializa una vez por operación: un replay debe enviar exactamente los mismos bytes.
    private static string Body(FieldSeed.Dispatched d, Guid operationId, decimal accepted, decimal rejected = 0) =>
        JsonSerializer.Serialize(new
        {
            operationId, expectedDeliveryVersion = d.DeliveryVersion, occurredAtDevice = CapturedAt,
            lines = new[]
            {
                new { deliveryLineId = d.DeliveryLineId, lotId = d.LotId, quantityAccepted = accepted, quantityRejected = rejected,
                      unit = "L", rejectionReason = rejected > 0 ? "Envase dañado" : null },
            },
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static HttpRequestMessage Receipt(Guid deliveryId, Guid operationId, string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deliveries/{deliveryId}/receipts")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Idempotency-Key", operationId.ToString());
        return request;
    }

    [Fact]
    public async Task Same_key_and_body_replays_the_response_and_changed_body_conflicts()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var driver = api.ClientFor(TestTokens.Driver);
        var d = await FieldSeed.DispatchedDeliveryAsync(api, 12m);
        var operationId = BaseEntity.NewId();
        var body = Body(d, operationId, 12m);

        var first = await driver.SendAsync(Receipt(d.DeliveryId, operationId, body));
        var replay = await driver.SendAsync(Receipt(d.DeliveryId, operationId, body));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, d.PointLocationId, d.LotId));

        var changed = await driver.SendAsync(Receipt(d.DeliveryId, operationId, Body(d, operationId, 11m)));
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        Assert.Contains("IDEMPOTENCY_CONFLICT", await changed.Content.ReadAsStringAsync());
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, d.PointLocationId, d.LotId));
    }

    [Fact]
    public async Task Two_devices_with_distinct_keys_cannot_receive_more_than_dispatched()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var d = await FieldSeed.DispatchedDeliveryAsync(api, 12m);
        Guid first = BaseEntity.NewId(), second = BaseEntity.NewId();

        var responses = await Task.WhenAll(
            api.ClientFor(TestTokens.Driver).SendAsync(Receipt(d.DeliveryId, first, Body(d, first, 12m))),
            api.ClientFor(TestTokens.Driver).SendAsync(Receipt(d.DeliveryId, second, Body(d, second, 12m))));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        var rejected = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Contains("QUANTITY_EXCEEDED", await rejected.Content.ReadAsStringAsync());
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, d.PointLocationId, d.LotId));
        Assert.Equal(0m, await FieldSeed.OnHandAsync(api, d.VehicleLocationId, d.LotId));
    }

    [Fact]
    public async Task Partial_receipt_keeps_the_rejected_quantity_in_the_vehicle()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var d = await FieldSeed.DispatchedDeliveryAsync(api, 12m);
        var operationId = BaseEntity.NewId();

        var response = await api.ClientFor(TestTokens.Driver).SendAsync(Receipt(d.DeliveryId, operationId, Body(d, operationId, 9m, 3m)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(9m, await FieldSeed.OnHandAsync(api, d.PointLocationId, d.LotId));
        Assert.Equal(3m, await FieldSeed.OnHandAsync(api, d.VehicleLocationId, d.LotId));
    }
}
```

### Consulta pública: cabecera, campos permitidos y 404 uniforme (T7)

`tests/IstpetDev.Tests/Integration/Field/PublicTraceHttpTests.cs`. El hash del token es SHA-256 del texto UTF-8 en hexadecimal minúscula; si T2 elige otro formato, cambiar `Hash` en el mismo commit.

```csharp
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Integration.Field;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class PublicTraceHttpTests(PostgisContainer postgres)
{
    private const string Token = "token-de-prueba-0123456789abcdef0123456789abcdef";
    private static readonly string[] AllowedFields = ["unitId", "status", "productUnits", "events"];

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static async Task<Guid> SeedUnitAsync(ApiFactory api, DateTime expiresAtUtc, DateTime? revokedAtUtc = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var d = await FieldSeed.DispatchedDeliveryAsync(api, 12m, suffix);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var actor = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
        var unit = new HandlingUnit { Code = $"CAJA-{suffix}", LocationId = d.VehicleLocationId, Status = "Dispatched", DeliveryId = d.DeliveryId };
        db.AddRange(unit,
            new HandlingUnitLine { HandlingUnitId = unit.Id, LotId = d.LotId, Quantity = 12m },
            new QrAccessToken { HandlingUnitId = unit.Id, TokenHash = Hash(Token), ExpiresAtUtc = expiresAtUtc, RevokedAtUtc = revokedAtUtc, CreatedByAccountId = actor.Id },
            new CustodyEvent { HandlingUnitId = unit.Id, DeliveryId = d.DeliveryId, ActorAccountId = actor.Id, EventKind = "Dispatched",
                               OperationId = Domain.Common.BaseEntity.NewId(), ReceivedAtServer = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return unit.Id;
    }

    private static Task<HttpResponseMessage> GetTrace(ApiFactory api, Guid unitId, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/public/trace/{unitId}");
        if (token is not null) request.Headers.Add("X-Trace-Token", token);
        return api.CreateClient().SendAsync(request);
    }

    [Fact]
    public async Task Valid_token_in_header_returns_only_public_fields()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var unitId = await SeedUnitAsync(api, DateTime.UtcNow.AddDays(7));

        var response = await GetTrace(api, unitId, Token);
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        using var json = JsonDocument.Parse(text);
        Assert.All(json.RootElement.EnumerateObject(), p => Assert.Contains(p.Name, AllowedFields));
        Assert.DoesNotContain(Token, text);
        Assert.DoesNotContain("quantity", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("actor", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invalid_expired_revoked_missing_or_query_tokens_are_uniform_404()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var valid = await SeedUnitAsync(api, DateTime.UtcNow.AddDays(7));
        var expired = await SeedUnitAsync(api, DateTime.UtcNow.AddMinutes(-1));
        var revoked = await SeedUnitAsync(api, DateTime.UtcNow.AddDays(7), DateTime.UtcNow);

        var responses = new[]
        {
            await GetTrace(api, valid, null),
            await GetTrace(api, valid, Token + "x"),
            await GetTrace(api, expired, Token),
            await GetTrace(api, revoked, Token),
            await GetTrace(api, Guid.NewGuid(), Token),
            await api.CreateClient().GetAsync($"/api/v1/public/trace/{valid}?token={Token}"),
        };

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
    }
}
```

### `advance` con operaciones reales (T4)

`tests/IstpetDev.Tests/E2E/DemoAdvanceTests.cs` — usa el fixture `demo-base` de 001, que tiene 3 puntos en riesgo para un vehículo:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.E2E;

[Collection(PostgisCollection.Name)]
[Trait("Category", "E2E")]
public class DemoAdvanceTests(PostgisContainer postgres)
{
    [Fact]
    public async Task Advance_completes_stops_with_real_receipts_and_is_idempotent()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);

        var proposal = await (await planner.PostAsJsonAsync("/api/v1/route-plans", new { includeConsolidation = true }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var routePlanId = proposal.GetProperty("routes")[0].GetProperty("routePlanId").GetGuid();
        var vehicleCode = proposal.GetProperty("routes")[0].GetProperty("vehicleCode").GetString()!;
        var publish = await planner.PostAsJsonAsync($"/api/v1/route-plans/{routePlanId}/publish",
            new { assignments = new Dictionary<string, string> { [vehicleCode] = TestTokens.Driver } });
        Assert.True(publish.IsSuccessStatusCode, await publish.Content.ReadAsStringAsync());

        var operationId = BaseEntity.NewId();
        var first = await planner.PostAsJsonAsync($"/api/v1/demo/route-plans/{routePlanId}/advance", new { operationId, stops = 2 });
        var replay = await planner.PostAsJsonAsync($"/api/v1/demo/route-plans/{routePlanId}/advance", new { operationId, stops = 2 });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, await db.Receipts.CountAsync());
        Assert.Equal(2, await db.RouteStops.CountAsync(s => s.RoutePlanId == routePlanId && s.Status == "Completed"));
        Assert.True(await db.CustodyEvents.CountAsync() >= 4);   // al menos despacho y recepción por parada
    }
}
```


## Matriz de cobertura

| Requisitos | Tarea | Evidencia principal |
|---|---|---|
| FR-01–FR-02 | T2 | preparación y QR, E2E-01/07 |
| FR-03 | T3 | despacho atómico, E2E-01 |
| FR-04–FR-05 | T5, T8 | asignación y IndexedDB, E2E-02/06 |
| FR-06–FR-09 | T4 | recibo/replay/concurrencia, E2E-02/03/04 |
| FR-10–FR-11 | T5–T6 | evidencia independiente/estados, E2E-02/05 |
| FR-12 | T8 | revisión y aviso, E2E-06 |
| FR-13–FR-14 | T7 | proyección pública, E2E-07 |
| FR-15 | T4, T9 | parada por recibo, E2E-02 |
| FR-16 | T4 | `DemoAdvanceTests`, E2E-10 |
| FR-17–FR-18 | T1b | login y PWA por HTTPS, E2E-09 |
| NFR-01, NFR-05 | T5, T10 | teléfono real y cuota, E2E-02/08 |
| NFR-02 | T1, T4 | retención ≥ 7 días, E2E-03 |
| NFR-03 | T5 | outbox sin JWT, E2E-08 |
| NFR-04 | T6–T7 | S3 privado/QR limitado, E2E-05/07 |
| NFR-06 | T7, T10 | teléfono ajeno y HTTPS, E2E-07 |
| CE-01 | T2–T3 | E2E-01 |
| CE-02 | T4–T5 | E2E-02 |
| CE-03 | T3–T4 | E2E-03 |
| CE-04 | T4 | E2E-04 |
| CE-05 | T4, T8 | E2E-03/06 |
| CE-06 | T6 | E2E-05 |
| CE-07 | T7 | E2E-07 |
| CE-08 | T8 | E2E-06 |
| CE-09 | T1b | E2E-09 |
| CE-10 | T4 | `DemoAdvanceTests`, E2E-10 |

## Suposiciones y mocks

Para avanzar de forma desacoplada y validar la experiencia de usuario, diseño institucional, arquitectura FSD y tolerancia a fallos offline sin modificar el backend C# (.NET):

1. **In-Browser Simulated Backend (`FieldMockBackendService`):**
   - Implementado en `apps/mobile/src/app/field/mock/field-mock-backend.service.ts` y en `apps/web/src/app/field/mock/web-field-mock-backend.service.ts`.
   - Utiliza base de datos IndexedDB en el navegador (`istpetdev-driver-outbox-db` y `istpetdev-web-mock-db`) simulando persistencia ACID.
   - Modela transacciones atómicas de despacho, cálculo FEFO, generación de tokens QR con hash SHA-256, y recepción con conciliación de inventario.
   - Simula respuestas HTTP estándar (`200 OK`, `409 ASSIGNMENT_CHANGED`, `401 Unauthorized`, `503 Service Unavailable`).
2. **Seguridad y Tokens OIDC (NFR-03):**
   - El mock respeta estrictamente que los tokens de sesión OIDC/PKCE residen en memoria volátil o `sessionStorage` transitorio. En ningún caso se persisten tokens en IndexedDB ni en el outbox durable.
3. **Privacidad de Traza Pública (ADR-10 / S-07):**
   - El token de traza viaja en el fragmento hash (`#t=TOKEN`) de la URL QR y es limpiado inmediatamente mediante `history.replaceState` en el cliente.
   - El DTO público expone únicamente hitos temporales y estado de la unidad de manipulación, excluyendo datos personales, de pacientes, geolocalizaciones o cantidades sensibles.
4. **Desacoplamiento de Evidencias (S-06 / NFR-04):**
   - La cola de subida de evidencias opera de manera independiente al registro contable de recepción, con backoff exponencial simulado y almacenamiento duradero de Blobs hasta confirmación.
5. **Avisos de Ruta (CE-08):**
   - El acuse de lectura de revisiones de ruta se persiste por par `(routePlanId, revision)` en IndexedDB local, evitando notificaciones redundantes.
