# 014 — Recepción real del Receptor en la PWA (plan por commits)

> Rama: `feat/receptor-recepcion-real` (desde `develop`). Plazo: congelamiento 15-oct 14:00.
> Decisión de Joseph Baño (15-oct): el Receptor registra la recepción con su propia cuenta, contra la API real.
> Pruebas mínimas por el tiempo: solo `ReceiverReceiptHttpTests` (ya escrito en el commit 0).

## Problema

- `apps/mobile/src/app/home/home.page.ts` muestra al Receptor tres bultos fijos (`receiverSampleBoxes`: Cloruro, Oxígeno, Alcohol) y un aviso fijo «Datos simulados (Fase Frontend-First) · useMockApi» que **ven todos los roles**, aunque Conductor y Bodega usan la API real.
- «Confirmar recepción» guarda en el outbox un `deliveryId` inventado (`hu-2026-med-042`), y la sincronización falla.
- La API no tiene nada para el Receptor: `POST /deliveries/{id}/receipts` solo admite `Conductor,Gerencia` y exige el conductor asignado. El spec 004 dejó fuera la sesión del Receptor, y el ADR-24 y el spec 013 le dieron cuenta. Nadie cerró ese hueco.

## Diseño

**Prueba de posesión: el QR del bulto.** El QR lleva `https://istpetdev.doicela.dev/public-trace/{unitId}#t={token}`. El Receptor manda `unitId` en la ruta y el token en el header `X-Trace-Token`, el mismo que usa la traza pública. Se valida igual que en `GetPublicTraceAsync`: hash SHA-256, sin revocar y sin vencer. Si falla, se responde un 404 uniforme. Así no hace falta ninguna migración (NFR-03) ni asignar un receptor a cada punto.

| Endpoint | Roles | Qué hace |
|---|---|---|
| `GET /api/v1/receiver/handling-units/{unitId}` + `X-Trace-Token` | Receptor, Gerencia | Devuelve qué se recibe: unidad, entrega, versión, destino y líneas (producto, lote, cantidad, unidad) |
| `POST /api/v1/receiver/handling-units/{unitId}/receipt` + `X-Trace-Token` + `Idempotency-Key`, body `CreateReceiptRequest` | Receptor, Gerencia | Valida el QR y reutiliza la lógica de `CreateReceiptAsync` **sin** el chequeo de conductor asignado |

Reglas: la unidad debe pertenecer a la entrega. Se aceptan repeticiones idempotentes: el mismo `OperationId` devuelve la misma respuesta. Una segunda recepción con otro `OperationId` choca con `QUANTITY_EXCEEDED` (409), porque `CreateReceiptAsync` ya controla la cantidad despachada.

La PWA del Receptor trabaja **en línea**, sin outbox: escanea el QR, ve el contenido real, marca «Todo conforme» o «Pasó algo malo» y confirma. Sin red muestra «Necesitas señal para confirmar».

---

## Commit 0 — prueba de integración (ya hecho en la rama)

`tests/IstpetDev.Tests/Integration/Field/ReceiverReceiptHttpTests.cs`: 5 casos. Ver QR, token malo o ausente da 404, Planificador da 403, la recepción deja la entrega `Delivered` y la unidad `Received`, la repetición es idempotente y un segundo recibo da 409, y un token falso no cambia nada.

Correr: `dotnet test --filter "FullyQualifiedName~ReceiverReceiptHttpTests"` (hoy fallan: los endpoints no existen).

## Commit 1 — `feat(api): recepción del Receptor validada con el QR del bulto`

**1a. `src/Application/Field/Dtos/FieldContracts.cs`**, al final:

```csharp
// 014: lo que ve el Receptor al escanear el QR del bulto.
public sealed record ReceiverUnitLineDto(Guid DeliveryLineId, Guid LotId, string LotCode, string Sku, string ProductName, decimal Quantity, string Unit);

public sealed record ReceiverUnitResponse(
    Guid HandlingUnitId, string Code, string Status, Guid DeliveryId, long DeliveryVersion,
    string DestinationName, IReadOnlyList<ReceiverUnitLineDto> Lines);
```

**1b. `src/Application/Field/IFieldService.cs`**: añadir

```csharp
Task<ReceiverUnitResponse> GetReceiverUnitAsync(Guid unitId, string? traceToken, CancellationToken ct = default);
Task<ReceiptResponse> CreateReceiverReceiptAsync(Guid unitId, string? traceToken, CreateReceiptRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default);
```

Implementar ambos como `throw new NotImplementedException()` en el `FakeFieldService` de `tests/IstpetDev.Tests/Unit/Field/FieldUnitTests.cs`, o no compila.

**1c. `src/Application/Field/FieldService.cs`**

1. Extraer la validación del token de `GetPublicTraceAsync` a un helper y usarlo ahí también:

```csharp
private async Task<HandlingUnit> RequireUnitWithTokenAsync(Guid unitId, string? traceToken, CancellationToken ct)
{
    var notFound = new FieldNotFoundException(FieldErrorCodes.UnitNotFound, "Unit not found.");
    if (string.IsNullOrWhiteSpace(traceToken)) throw notFound;
    var hu = await _db.HandlingUnits.FirstOrDefaultAsync(h => h.Id == unitId && h.OrganizationId == DemoOrganization.Id, ct) ?? throw notFound;
    var tokenHash = ComputeSha256(traceToken.Trim());
    var valid = await _db.QrAccessTokens.AnyAsync(t => t.HandlingUnitId == hu.Id && t.TokenHash == tokenHash
        && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow, ct);
    return valid ? hu : throw notFound;
}
```

2. En `CreateReceiptAsync`, mover todo el cuerpo a `private async Task<ReceiptResponse> CreateReceiptCoreAsync(Guid deliveryId, CreateReceiptRequest request, Guid actorAccountId, string idempotencyKey, bool requireAssignedDriver, CancellationToken ct)`. Hacer que `CreateReceiptAsync` llame al núcleo con `requireAssignedDriver: true`. En el paso «2. Autorización por asignación de conductor», condicionar el `if` con `requireAssignedDriver &&`.

3. Métodos nuevos:

```csharp
public async Task<ReceiverUnitResponse> GetReceiverUnitAsync(Guid unitId, string? traceToken, CancellationToken ct = default)
{
    var hu = await RequireUnitWithTokenAsync(unitId, traceToken, ct);
    if (hu.DeliveryId is not Guid deliveryId) throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, "Unit not found.");
    var delivery = await _db.Deliveries.SingleAsync(d => d.Id == deliveryId, ct);
    var point = await _db.ServicePoints.FirstOrDefaultAsync(p => p.Id == delivery.ServicePointId, ct);
    var deliveryLines = await _db.DeliveryLines.Where(l => l.DeliveryId == deliveryId).ToListAsync(ct);
    var unitLines = await _db.HandlingUnitLines.Where(l => l.HandlingUnitId == hu.Id).ToListAsync(ct);
    var lotIds = unitLines.Select(l => l.LotId).ToList();
    var lots = await _db.Lots.Where(l => lotIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);
    var productIds = lots.Values.Select(l => l.ProductId).ToList();
    var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

    var lines = unitLines.Select(l =>
    {
        // La línea de la caja puede no traer DeliveryLineId: se resuelve por lote dentro de la entrega.
        var deliveryLineId = l.DeliveryLineId ?? deliveryLines.First(dl => dl.LotId == l.LotId).Id;
        var lot = lots[l.LotId];
        var product = products[lot.ProductId];
        return new ReceiverUnitLineDto(deliveryLineId, lot.Id, lot.Code, product.Sku, product.Name, l.Quantity, product.BaseUnit);
    }).ToList();

    return new ReceiverUnitResponse(hu.Id, hu.Code, hu.Status, deliveryId, delivery.Version, point?.Name ?? "", lines);
}

public async Task<ReceiptResponse> CreateReceiverReceiptAsync(Guid unitId, string? traceToken, CreateReceiptRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default)
{
    var hu = await RequireUnitWithTokenAsync(unitId, traceToken, ct);
    if (hu.DeliveryId is not Guid deliveryId) throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, "Unit not found.");
    // El QR prueba que el Receptor tiene el bulto en la mano; no hay conductor asignado que validar.
    return await CreateReceiptCoreAsync(deliveryId, request, actorAccountId, idempotencyKey, requireAssignedDriver: false, ct);
}
```

Verificar los nombres reales de las propiedades antes de compilar: `Lot.Code`, `Product.Sku`, `Product.Name`, `Product.BaseUnit`, `ServicePoint.Name` y `_db.ServicePoints`. Todos aparecen en `tests/IstpetDev.Tests/Support/FieldSeed.cs`.

**1d. `src/Api/Controllers/ReceiverController.cs`** (nuevo):

```csharp
using IstpetDev.Api.Authentication;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

// 014: el Receptor del hospital recibe con su cuenta; el token del QR prueba que tiene el bulto.
[ApiController]
[Route("api/v1/receiver/handling-units")]
[Authorize(Roles = $"{Roles.Receptor},{Roles.Gerencia}")]
public sealed class ReceiverController(IFieldService fieldService) : ControllerBase
{
    [HttpGet("{unitId:guid}")]
    public Task<IActionResult> Get(Guid unitId, [FromHeader(Name = "X-Trace-Token")] string? token, CancellationToken ct) =>
        Run(async () => Ok(await fieldService.GetReceiverUnitAsync(unitId, token, ct)));

    [HttpPost("{unitId:guid}/receipt")]
    public Task<IActionResult> Receive(Guid unitId, [FromHeader(Name = "X-Trace-Token")] string? token,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, [FromBody] CreateReceiptRequest request, CancellationToken ct) =>
        Run(async () =>
        {
            if (User.AccountId() is not Guid actor) return Forbid();
            var key = !string.IsNullOrWhiteSpace(idempotencyKey) ? idempotencyKey : request.OperationId.ToString();
            return Ok(await fieldService.CreateReceiverReceiptAsync(unitId, token, request, actor, key, ct));
        });

    private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (FieldException ex) { return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code); }
    }
}
```

Validar: `dotnet test --filter "FullyQualifiedName~ReceiverReceiptHttpTests|FullyQualifiedName~Field|FullyQualifiedName~AuthorizationSafety"`. Si `AuthorizationSafetyTests` lista los endpoints con sus roles, añadir los dos nuevos ahí.

## Commit 2 — `feat(mobile): el Receptor escanea el QR real y recibe contra la API`

**2a. Contrato y servicio HTTP.** En `apps/mobile/src/app/field/field-api.interface.ts`, añadir los tipos `ReceiverUnitLine` y `ReceiverUnit`, espejo de los DTO, y dos métodos:

```ts
getReceiverUnit(unitId: string, token: string): Observable<ReceiverUnit>;
receiveAsReceiver(unitId: string, token: string, req: CreateReceiptRequest, idempotencyKey: string): Observable<ReceiptResponse>;
```

Implementarlos en `http-field-api.service.ts` copiando el patrón de `getPublicTrace` (header `X-Trace-Token`) y de `createReceipt` (header `Idempotency-Key`). En `mock/mock-field-api.service.ts`, poner `throwError(() => new Error('no disponible en mock'))`.

**2b. Leer el QR.** Función pura en `apps/mobile/src/app/field/receiver-qr.ts`:

```ts
/** Acepta la URL del QR (`…/public-trace/{unitId}#t={token}`) y devuelve unidad y token. */
export function parseUnitQr(raw: string): { unitId: string; token: string } | null {
  const match = raw.trim().match(/public-trace\/([0-9a-f-]{36})#t=([0-9a-f]+)/i);
  return match ? { unitId: match[1], token: match[2] } : null;
}
```

**2c. Pantalla del Receptor (`home.page.ts`).**
- Borrar el bloque `<!-- Insignia fija: Datos Simulados -->`. Si se quiere conservar en desarrollo, envolverlo en `@if (usingMock)` con `usingMock = inject(FIELD_API) instanceof MockFieldApiService`.
- Borrar `receiverSampleBoxes`, `sampleBoxIndex`, `cycleSampleBox()` y el botón «Simular bulto».
- `receiverActiveBox` pasa a `signal<ReceiverUnit | null>(null)`. Añadir `receiverQr = signal<{ unitId: string; token: string } | null>(null)`, `receiverError = signal<string | null>(null)` y `receiverLoading = signal(false)`.
- «Escanear QR con cámara» abre el escáner. Para no tocar `scan.page.ts`, que resuelve contra la asignación del conductor, el Receptor navega a `/scan?mode=receiver`. En `scan.page.ts`, si `mode=receiver`, se hace `parseUnitQr(code)` y `router.navigate(['/home'], { state: { receiverQr } })`. Si es más rápido, basta un `<ion-input>` en la tarjeta del Receptor donde se pega o escanea el código, con un botón «Buscar».
- `loadReceiverUnit(qr)`: llama a `fieldApi.getReceiverUnit`. Si responde bien, `receiverActiveBox.set(unit)`. Un 404 muestra «QR no válido o vencido» y un `status !== 'Dispatched'` muestra «Este bulto ya fue recibido».
- Adaptar la ficha: `box.code`, `box.destinationName` y un `@for` sobre `box.lines` con `productName`, `lotCode`, `quantity` y `unit`. Quitar «Vehículo», que no viene en el DTO.
- `confirmReceiverAction()`: reemplazar `saveReceiptAtomic` por una llamada directa:

```ts
const ok = this.receiverStatus() === 'conforme';
const req: CreateReceiptRequest = {
  operationId: crypto.randomUUID(),
  expectedDeliveryVersion: box.deliveryVersion,
  occurredAtDevice: new Date().toISOString(),
  lines: box.lines.map(l => ({
    deliveryLineId: l.deliveryLineId, lotId: l.lotId, unit: l.unit,
    quantityAccepted: ok ? l.quantity : 0,
    quantityRejected: ok ? 0 : l.quantity,
    rejectionReason: ok ? undefined : this.receiverSelectedIssues().join(', ') || 'Novedad',
  })),
};
const receipt = await firstValueFrom(this.fieldApi.receiveAsReceiver(qr.unitId, qr.token, req, req.operationId));
this.receiverConfirmedMessage.set(`Recepción registrada: entrega ${receipt.deliveryStatus === 'Delivered' ? 'completa' : 'con novedades'}.`);
```

  Si falla, `receiverError` muestra: sin red, «Necesitas señal para confirmar»; 409, «Este bulto ya fue recibido».
- Mantener «Todo conforme / Pasó algo malo» y los chips de novedad como están.

Validar: `npm run test:mobile && npm run build:mobile && npm run check:no-mock-login`.

## Commit 3 — `docs(004,013): el Receptor recibe con su cuenta (spec 014)`

- `docs/specs/004-campo-qr-custodia-offline/spec.md` «Fuera de alcance»: tachar la línea de la sesión independiente del Receptor y enlazar este plan.
- `docs/boveda/03 Arquitectura/Decisiones de arquitectura.md` (línea ~389): corregir la afirmación de que el Receptor «transmite de forma idempotente al backend» con `saveReceiptAtomic`. Ahora usa `POST /api/v1/receiver/handling-units/{unitId}/receipt`, en línea.
- `docs/specs/013-login-y-cuentas/test-e2e.md`: añadir E2E-14. Bodega prepara y despacha un bulto, el Receptor escanea su QR, ve el producto real y confirma; la entrega queda `Delivered` en la web.

## Cierre

PR `feat/receptor-recepcion-real → develop`, revisión de otra persona, y después PR `develop → main` (CD).

**Prueba manual en producción** (sustituye a las pruebas automáticas de la PWA):
1. Bodega crea el bulto de una entrega y la despacha.
2. Abrir la URL del QR (`/public-trace/{id}#t=…`) para comprobar el token.
3. El Receptor escanea o pega el QR y ve producto, lote y cantidad.
4. Confirma «Todo conforme».
5. En la web la entrega aparece «Entregada».

**Plan B si no llega a las 14:00:** quitar solo el aviso «Datos simulados» y ocultar la tarjeta del Receptor tras el mensaje «La recepción se registra en el teléfono del conductor», como dice el spec 004. En la demo no se usa la cuenta Receptor.
