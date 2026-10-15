using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/receipts")]
[Authorize(Roles = Roles.Human)]
public sealed class ReceiptsController : ControllerBase
{
    [HttpGet("rejection-reasons")]
    public IActionResult GetRejectionReasons()
    {
        return Ok(new
        {
            version = ReceiptRejectionReasons.Version,
            items = ReceiptRejectionReasons.Items
        });
    }
}
