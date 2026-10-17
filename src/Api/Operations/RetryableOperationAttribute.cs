using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace IstpetDev.Api.Operations;

// Guarda efectos y respuesta en el mismo commit. Los filtros de autorización corren primero.
[AttributeUsage(AttributeTargets.Method)]
public sealed class RetryableOperationAttribute(string action) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var ct = http.RequestAborted;
        var db = http.RequestServices.GetRequiredService<IApplicationDbContext>();
        var json = http.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
        if (http.User.AccountId() is not Guid actor)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var arguments = context.ActionArguments.Where(a => a.Value is not CancellationToken)
            .ToDictionary(a => a.Key, a => a.Value);
        var payload = JsonSerializer.SerializeToElement(arguments, json);
        var body = payload.EnumerateObject().Select(p => p.Value).FirstOrDefault(v => v.ValueKind == JsonValueKind.Object);
        string? bodyKey = body.ValueKind == JsonValueKind.Object && body.TryGetProperty("operationId", out var operation)
            && operation.ValueKind != JsonValueKind.Null ? operation.GetString() : null;
        var headerKey = http.Request.Headers["Idempotency-Key"].ToString();
        if ((bodyKey is not null && (!Guid.TryParse(bodyKey, out var bodyId) || bodyId == Guid.Empty)) ||
            (http.Request.Headers.ContainsKey("Idempotency-Key") && (!Guid.TryParse(headerKey, out var headerId) || headerId == Guid.Empty)) ||
            (bodyKey is not null && headerKey.Length > 0 && Guid.Parse(bodyKey) != Guid.Parse(headerKey)))
        {
            context.Result = Problem(422, "INVALID_OPERATION_ID", "Use un UUID no vacío; cuerpo y cabecera deben coincidir.");
            return;
        }

        var resource = context.ActionArguments.TryGetValue("id", out var id) ? id?.ToString() : "";
        var fingerprint = Hash($"{action}|{resource}|{http.Request.Headers["X-Agent-Run-Id"]}|{Canonical(payload)}");
        Guid key;
        if (bodyKey is not null || headerKey.Length > 0)
            key = Guid.Parse(bodyKey ?? headerKey);
        else if (!string.IsNullOrEmpty(resource))
            key = NaturalKey($"{action}|{resource}");
        else if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("proposalId", out var proposal)
                 && proposal.ValueKind == JsonValueKind.String && proposal.TryGetGuid(out var proposalId) && proposalId != Guid.Empty)
            key = NaturalKey($"{action}|{proposalId:D}");
        else
        {
            // Compatibilidad: una petición sin clave representa una operación por contenido y día operativo.
            // Para repetir intencionalmente el mismo contenido, el cliente envía un OperationId nuevo.
            var today = await http.RequestServices.GetRequiredService<IOperationalClock>().TodayAsync(ct);
            key = NaturalKey($"{action}|legacy|{today:yyyy-MM-dd}|{fingerprint}");
        }

        try
        {
            await using var transaction = await OrganizationTransaction.BeginAsync(db, ct);
            var previous = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(r =>
                r.OrganizationId == DemoOrganization.Id && r.AccountId == actor && r.OperationId == key, ct);
            if (previous is not null)
            {
                context.Result = previous.ActionCode != action || previous.RequestHash != fingerprint
                    ? Problem(409, "IDEMPOTENCY_CONFLICT", "La operación ya existe con otra acción, recurso o contenido.")
                    : new ContentResult { StatusCode = previous.ResponseStatus, ContentType = "application/json", Content = previous.ResponseJson };
                return;
            }

            var executed = await next();
            if (executed.Exception is not null && !executed.ExceptionHandled)
            {
                if (IsRecoverable(executed.Exception))
                {
                    executed.ExceptionHandled = true;
                    executed.Result = Problem(409, "CONCURRENT_OPERATION", "El recurso cambió; consulte su estado y reintente con la misma clave.");
                }
                return;
            }
            if (executed.Result is not ObjectResult result || (result.StatusCode ?? 200) is < 200 or >= 300) return;

            var response = JsonSerializer.Serialize(result.Value, json);
            db.IdempotencyRecords.Add(new IdempotencyRecord
            {
                OrganizationId = DemoOrganization.Id, AccountId = actor, OperationId = key,
                ActionCode = action, RequestHash = fingerprint, ResponseStatus = result.StatusCode ?? 200,
                ResponseJson = response, CompletedAtUtc = DateTime.UtcNow
                // Sin borrado automático: expirar la evidencia permitiría repetir efectos históricos.
            });
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
        }
        catch (Exception error) when (IsRecoverable(error))
        {
            context.Result = Problem(409, "CONCURRENT_OPERATION", "El recurso cambió; consulte su estado y reintente con la misma clave.");
        }
    }

    private static bool IsRecoverable(Exception error) => error is DbUpdateConcurrencyException or ReplanConflictException ||
        error is DbUpdateException { InnerException: PostgresException { SqlState: "23505" or "40001" or "40P01" or "55P03" } } ||
        error is PostgresException { SqlState: "40001" or "40P01" or "55P03" };

    private static ObjectResult Problem(int status, string code, string detail) => new(new ProblemDetails
        { Status = status, Title = code, Detail = detail }) { StatusCode = status };

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static Guid NaturalKey(string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, 16));

    private static string Canonical(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(",", value.EnumerateObject()
            .Where(p => p.Name != "operationId").OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => JsonSerializer.Serialize(p.Name) + ":" + Canonical(p.Value))) + "}",
        JsonValueKind.Array => "[" + string.Join(",", value.EnumerateArray().Select(Canonical)) + "]",
        _ => value.GetRawText()
    };
}
