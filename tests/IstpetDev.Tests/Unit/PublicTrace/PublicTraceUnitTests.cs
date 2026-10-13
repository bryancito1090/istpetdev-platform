using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Api.Controllers;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.PublicTrace.Dtos;
using IstpetDev.Application.PublicTrace.Services;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace IstpetDev.Tests.Unit.PublicTrace;

public class PublicTraceUnitTests
{
    private sealed class FakeRateLimiter(bool allow = true) : IPublicTraceRateLimiter
    {
        public bool IsAllowed(string tokenHash) => allow;
    }

    private sealed class FakePublicTraceService(PublicTraceResult resultToReturn) : IPublicTraceService
    {
        public Task<PublicTraceResult> GetTraceAsync(string unitId, string? token, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(resultToReturn);
        }
    }

    [Fact]
    public void RateLimiter_enforces_limit_per_token_hash()
    {
        var limiter = new PublicTraceRateLimiter(maxRequestsPerWindow: 3, window: TimeSpan.FromMinutes(1));
        const string hash1 = "hash-token-1";
        const string hash2 = "hash-token-2";

        // hash1: primeros 3 permitidos
        Assert.True(limiter.IsAllowed(hash1));
        Assert.True(limiter.IsAllowed(hash1));
        Assert.True(limiter.IsAllowed(hash1));
        // hash1: cuarto bloqueado (429)
        Assert.False(limiter.IsAllowed(hash1));

        // hash2: no afectado por hash1
        Assert.True(limiter.IsAllowed(hash2));
    }

    [Fact]
    public async Task Controller_returns_404_when_token_in_query_string()
    {
        var service = new FakePublicTraceService(new PublicTraceResult.Success(new PublicTraceResponseDto()));
        var controller = new PublicTraceController(service);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?token=secreto123");
        httpContext.Request.Headers["X-Trace-Token"] = "secreto123";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var actionResult = await controller.GetTrace("unit-123", CancellationToken.None);

        Assert.IsType<NotFoundResult>(actionResult);
        Assert.Equal("no-store", httpContext.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Controller_returns_404_when_header_is_missing()
    {
        var service = new FakePublicTraceService(new PublicTraceResult.Success(new PublicTraceResponseDto()));
        var controller = new PublicTraceController(service);

        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var actionResult = await controller.GetTrace("unit-123", CancellationToken.None);

        Assert.IsType<NotFoundResult>(actionResult);
        Assert.Equal("no-store", httpContext.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Controller_returns_429_when_rate_limited()
    {
        var service = new FakePublicTraceService(new PublicTraceResult.RateLimited());
        var controller = new PublicTraceController(service);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Trace-Token"] = "token-valido";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var actionResult = await controller.GetTrace("unit-123", CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
        Assert.Equal("no-store", httpContext.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Controller_returns_200_with_trace_and_no_store()
    {
        var dto = new PublicTraceResponseDto
        {
            UnitId = "CAJA-001",
            Status = "Dispatched",
            ProductUnits = [new PublicTraceProductUnitDto { Sku = "SKU-1", Unit = "L" }],
            Events = [new PublicTraceEventDto { Stage = "Dispatched", Timestamp = DateTime.UtcNow }]
        };
        var service = new FakePublicTraceService(new PublicTraceResult.Success(dto));
        var controller = new PublicTraceController(service);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Trace-Token"] = "token-valido";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var actionResult = await controller.GetTrace("unit-123", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(dto, okResult.Value);
        Assert.Equal("no-store", httpContext.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public void HandlingUnitsController_revoke_has_authorize_roles_bodega_and_gerencia()
    {
        var method = typeof(HandlingUnitsController).GetMethod(nameof(HandlingUnitsController.RevokePublicToken));
        Assert.NotNull(method);
        var authAttr = (AuthorizeAttribute?)Attribute.GetCustomAttribute(method, typeof(AuthorizeAttribute));
        Assert.NotNull(authAttr);
        Assert.Contains(Roles.Bodega, authAttr.Roles);
        Assert.Contains(Roles.Gerencia, authAttr.Roles);
    }
}
