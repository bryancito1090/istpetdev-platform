using IstpetDev.Api.Authentication;
using IstpetDev.Application.Accounts;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/accounts")]
[Authorize(Roles = Roles.Gerencia)]
public sealed partial class AccountsController(AccountAdministration accounts) : ControllerBase
{
    private readonly AccountAdministration accounts = accounts;

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => Run(async () => Ok(await accounts.ListAsync(ct)));

    [HttpPost]
    public Task<IActionResult> Create(CreateAccountInput input, CancellationToken ct) => Run(async () =>
    {
        var item = await accounts.CreateAsync(input, ct);
        return StatusCode(StatusCodes.Status201Created, item);
    });

    private Guid Actor => User.AccountId() ?? throw new InvalidOperationException("Cuenta autenticada sin identificador.");

    private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        if (User.AccountId() is null) return Forbid();
        try { return await action(); }
        catch (AccountRuleException ex) { return Fail(StatusFor(ex.Code), ex.Code, ex.Message); }
        catch (IdentityDirectoryUnavailableException) { return Fail(StatusCodes.Status503ServiceUnavailable, "identity_unavailable", "Cognito no está disponible. Intenta de nuevo en unos minutos."); }
    }

    private static int StatusFor(string code) => code switch
    {
        "not_found" => StatusCodes.Status404NotFound,
        "account_exists" or "self_change_forbidden" or "last_manager" or "invitation_not_pending" => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status422UnprocessableEntity,
    };

    private static ObjectResult Fail(int status, string code, string title)
    {
        var problem = new ProblemDetails { Status = status, Title = title };
        problem.Extensions["code"] = code;
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
