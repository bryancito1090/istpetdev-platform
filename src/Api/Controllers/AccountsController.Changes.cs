using IstpetDev.Application.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

public sealed partial class AccountsController
{
    [HttpPatch("{id:guid}")]
    public Task<IActionResult> Update(Guid id, UpdateAccountInput input, CancellationToken ct) =>
        Run(async () => Ok(await accounts.UpdateAsync(Actor, id, input, ct)));

    [HttpPost("{id:guid}/deactivate")]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken ct) =>
        Run(async () => Ok(await accounts.DeactivateAsync(Actor, id, ct)));

    [HttpPost("{id:guid}/reactivate")]
    public Task<IActionResult> Reactivate(Guid id, CancellationToken ct) =>
        Run(async () => Ok(await accounts.ReactivateAsync(id, ct)));

    [HttpPost("{id:guid}/resend-invitation")]
    public Task<IActionResult> ResendInvitation(Guid id, CancellationToken ct) =>
        Run(async () => { await accounts.ResendInvitationAsync(id, ct); return NoContent(); });
}
