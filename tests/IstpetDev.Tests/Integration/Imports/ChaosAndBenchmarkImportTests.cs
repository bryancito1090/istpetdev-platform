using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Application.Imports;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Integration.Imports;

[Trait("Category", "Integration")]
[Trait("Category", "Chaos")]
[Trait("Category", "Benchmark")]
public sealed class ChaosAndBenchmarkImportTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static MultipartFormDataContent UploadFile(byte[] bytes, string entity, string fileName, string? profile = null)
    {
        var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(bytes), entity, fileName }
        };
        if (profile is not null)
        {
            content.Add(new StringContent(profile), "profile");
        }
        return content;
    }

    private static MultipartFormDataContent UploadCsv(string csv, string entity = "products", string? profile = null) =>
        UploadFile(Encoding.UTF8.GetBytes(csv), entity, "data.csv", profile);

    /// <summary>
    /// ESC-21: CSV corrupto (codificación mixta o columnas desplazadas) -> 422 con fila y campo; nada aplicado.
    /// </summary>
    [Fact]
    public async Task ESC21_Corrupted_csv_returns_422_with_row_and_field_details_and_zero_persisted_entities()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Planner);

        var corruptFile = Path.Combine(Fixtures.Folder("demo-region-caos"), "corrupto.csv");
        var corruptBytes = await File.ReadAllBytesAsync(corruptFile);

        using var form = UploadFile(corruptBytes, "products", "corrupto.csv");
        var response = await client.PostAsync("/api/v1/imports", form);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("applied").GetBoolean());
        Assert.True(body.GetProperty("errorCount").GetInt32() > 0);

        var issues = body.GetProperty("issues").EnumerateArray().ToList();
        Assert.NotEmpty(issues);

        // Verifica que se identifiquen el archivo corrupto, el código de error y la fila afectada
        var first = issues[0];
        Assert.Equal("file_unreadable", first.GetProperty("code").GetString());
        Assert.Contains("Fila 2", first.GetProperty("message").GetString());
        Assert.Equal("corrupto.csv", first.GetProperty("file").GetString());

        // Verifica integridad: nada fue aplicado en base de datos
        await using var db = TestDb.Context(connection);
        var productsCount = await db.Products.CountAsync(p => p.Sku.StartsWith("CORR-"));
        Assert.Equal(0, productsCount);
    }

    /// <summary>
    /// ESC-22: Archivo masivo (10 000 filas de consumo) -> < 30 s (001 NFR-01); por encima del límite, 413 claro.
    /// </summary>
    [Fact]
    public async Task ESC22_Benchmark_10k_consumptions_under_30s_and_file_limit_returns_413()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Planner);

        // 1. Sembrar estructura base (bodegas, puntos, productos) para validar referencias de consumos
        var baseSeed = Fixtures.Request("demo-region");
        using (var scope = api.Services.CreateScope())
        {
            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
            var seedReport = await importService.RunAsync(baseSeed, CancellationToken.None);
            Assert.True(seedReport.Applied);
        }

        // 2. Cargar archivo masivo de 10 000 filas de consumo generado
        var massConsumptionPath = Path.Combine(Fixtures.Folder("demo-region-caos"), "consumos_10k.csv");
        Assert.True(File.Exists(massConsumptionPath), $"El dataset {massConsumptionPath} debe existir.");

        var massBytes = await File.ReadAllBytesAsync(massConsumptionPath);

        using var form = UploadFile(massBytes, "consumptions", "consumos_10k.csv");

        var stopwatch = Stopwatch.StartNew();
        var response = await client.PostAsync("/api/v1/imports?dryRun=false", form);
        stopwatch.Stop();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("applied").GetBoolean());

        // 001 NFR-01: El tiempo de importación para 10 000 filas debe ser estrictamente menor a 30 segundos
        Assert.True(stopwatch.Elapsed.TotalSeconds < 30.0,
            $"Tiempo de importación superó la cota NFR-01 (< 30s): {stopwatch.Elapsed.TotalSeconds:F2} s.");

        // Verificar que los consumos fueron persistidos
        await using var db = TestDb.Context(connection);
        var insertedCount = await db.Consumptions.CountAsync();
        Assert.True(insertedCount >= 10000, $"Se esperaban al menos 10000 consumos, encontrados: {insertedCount}");

        // 3. Probar límite de archivo (> ImportLimits.FileBytes -> 413 Payload Too Large)
        using var oversizedContent = new MultipartFormDataContent
        {
            { new ByteArrayContent(new byte[ImportLimits.FileBytes + 1]), "consumptions", "huge.csv" }
        };
        var limitResponse = await client.PostAsync("/api/v1/imports", oversizedContent);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, limitResponse.StatusCode);
    }

    /// <summary>
    /// ESC-28: Reimportación de corte de stock posterior a recepción en campo preserva trazabilidad y audita ajuste.
    /// </summary>
    [Fact]
    public async Task ESC28_Stock_reimport_preserves_operational_receipt_audit_trail_and_adjusts_balance()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);

        // 1. Sembrar despacho y ejecutar recepción operativa en campo por el Receptor (#162)
        const string unitToken = "token-e28-0123456789abcdef0123456789abcdef";
        var dispatched = await FieldSeed.DispatchedDeliveryAsync(api, 100m, "E28", withHandlingUnit: true, unitToken: unitToken);
        await FieldSeed.DeliverUnitAsync(api, dispatched);
        using var receiverClient = api.ClientFor(TestTokens.Receiver);

        var receiptReq = new CreateReceiptRequest(
            OperationId: Guid.NewGuid(),
            ExpectedDeliveryVersion: 0, // la entrega avanzó con pickup/deliver; el recibo valida el estado vigente
            OccurredAtDevice: DateTime.UtcNow,
            Lines:
            [
                new ReceiptLineRequest(
                    DeliveryLineId: dispatched.DeliveryLineId,
                    LotId: dispatched.LotId,
                    QuantityAccepted: 60m,
                    QuantityRejected: 40m,
                    Unit: "L",
                    RejectionReason: ReceiptRejectionReasons.Daño,
                    RemainingQuantity: 10m)
            ]);

        using var receiptHttp = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/receiver/handling-units/{dispatched.UnitId}/receipt")
        {
            Content = JsonContent.Create(receiptReq),
        };
        receiptHttp.Headers.Add("X-Trace-Token", unitToken);
        receiptHttp.Headers.Add("Idempotency-Key", receiptReq.OperationId.ToString());
        var receiptResponse = await receiverClient.SendAsync(receiptHttp);
        Assert.Equal(HttpStatusCode.OK, receiptResponse.StatusCode);

        // Saldo después de recepción: remaining (10) + accepted (60) = 70
        var postReceiptBalance = await FieldSeed.OnHandAsync(api, dispatched.PointLocationId, dispatched.LotId);
        Assert.Equal(70m, postReceiptBalance);

        // 2. Simular reimportación de corte de stock previo por parte del Planificador
        using var plannerClient = api.ClientFor(TestTokens.Planner);

        var stockCsv = $"location_kind,location_code,sku,lot_code,quantity\nservice_point,P-E28,SKU-E28,LOTE-E28,25\n";
        using var stockForm = UploadCsv(stockCsv, "stock");

        var importResponse = await plannerClient.PostAsync("/api/v1/imports?dryRun=false", stockForm);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        // 3. Verificar que los movimientos de recepción no fueron borrados y que se auditó un movimiento de ajuste
        await using var db = TestDb.Context(connection);

        var movements = await db.InventoryMovements
            .Where(m => m.LotId == dispatched.LotId)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync();

        // Debe coexistir el movimiento operativo de tipo 'Receipt' y el movimiento 'ImportAdjustment'
        Assert.Contains(movements, m => m.Kind == "Receipt" && m.Quantity == 60m);
        Assert.Contains(movements, m => m.Kind == MovementKinds.ImportAdjustment);

        // El saldo final refleja el corte ajustado de manera auditable
        var finalBalance = await FieldSeed.OnHandAsync(api, dispatched.PointLocationId, dispatched.LotId);
        Assert.Equal(25m, finalBalance);
    }

    /// <summary>
    /// Catálogo de motivos de rechazo versionado V1 (Issue #117).
    /// </summary>
    [Fact]
    public async Task Receipt_rejection_reasons_catalog_returns_version_v1_and_normalizes_aliases()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Driver);

        var catalogResponse = await client.GetAsync("/api/v1/deliveries/rejection-reasons");
        Assert.Equal(HttpStatusCode.OK, catalogResponse.StatusCode);

        var catalog = await catalogResponse.Content.ReadFromJsonAsync<RejectionReasonsCatalogResponse>();
        Assert.NotNull(catalog);
        Assert.Equal(ReceiptRejectionReasons.Version, catalog.Version);
        Assert.Equal(6, catalog.Reasons.Count);

        var codes = catalog.Reasons.Select(r => r.Code).ToList();
        Assert.Contains(ReceiptRejectionReasons.Faltante, codes);
        Assert.Contains(ReceiptRejectionReasons.Sobrante, codes);
        Assert.Contains(ReceiptRejectionReasons.Daño, codes);
        Assert.Contains(ReceiptRejectionReasons.PrecintoViolado, codes);
        Assert.Contains(ReceiptRejectionReasons.LoteNoCoincide, codes);
        Assert.Contains(ReceiptRejectionReasons.CadenaDeFrio, codes);

        // Prueba de normalización de alias
        Assert.True(ReceiptRejectionReasons.TryNormalize("cadena-de-frio", out var c1));
        Assert.Equal(ReceiptRejectionReasons.CadenaDeFrio, c1);

        Assert.True(ReceiptRejectionReasons.TryNormalize("dano", out var c2));
        Assert.Equal(ReceiptRejectionReasons.Daño, c2);

        Assert.True(ReceiptRejectionReasons.TryNormalize("broken_seal", out var c3));
        Assert.Equal(ReceiptRejectionReasons.PrecintoViolado, c3);
    }
}
