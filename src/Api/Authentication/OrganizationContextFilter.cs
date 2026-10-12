using System.Data;
using System.Security.Claims;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace IstpetDev.Api.Authentication;

// Apply after [Authorize]. An organization header selects a context, never grants access.
public sealed class OrganizationContextFilter(ApplicationDbContext database) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext;
        var cancellation = request.RequestAborted;
        var issuer = request.User.FindFirst("iss")?.Value;
        var subject = request.User.FindFirst("sub")?.Value
            ?? request.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (request.User.Identity?.IsAuthenticated != true || issuer is null || subject is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        if (!Guid.TryParse(request.Request.Headers["X-Organization-Id"], out var organizationId))
        {
            context.Result = new BadRequestObjectResult(new { code = "organization_context_required" });
            return;
        }

        await using var transaction = await database.Database.BeginTransactionAsync(cancellation);
        await using var command = database.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT public.istpetdev_account_id(@issuer, @subject)";
        AddParameter("issuer", issuer);
        AddParameter("subject", subject);
        var identity = await command.ExecuteScalarAsync(cancellation);
        if (identity is not Guid accountId)
        {
            context.Result = new ForbidResult();
            return;
        }
        await database.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.organization_id', {organizationId.ToString()}, true), set_config('app.account_id', {accountId.ToString()}, true)", cancellation);
        command.Parameters.Clear();
        command.CommandText = "SELECT public.istpetdev_is_member(@organization) AND public.istpetdev_has_permission(@organization, 'service-point.read')";
        AddParameter("organization", organizationId);
        if (await command.ExecuteScalarAsync(cancellation) is not true)
        {
            context.Result = new ForbidResult();
            return;
        }
        var result = await next();
        var status = (result.Result as IStatusCodeActionResult)?.StatusCode ?? request.Response.StatusCode;
        if (result.Exception is null && status < 400)
            await transaction.CommitAsync(cancellation);
        // Disposal rolls back every rejected/failed path; SET LOCAL context cannot survive it.

        void AddParameter(string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }
    }
}
