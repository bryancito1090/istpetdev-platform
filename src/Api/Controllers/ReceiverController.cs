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
