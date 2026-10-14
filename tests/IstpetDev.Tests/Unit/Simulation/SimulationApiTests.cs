using System.Text;
using IstpetDev.Api.Controllers;
using IstpetDev.Application.Simulation;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Simulation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

[Trait("Category", "Unit")]
public sealed class SimulationApiTests
{
    private sealed class FakeSimulationService : ISimulationService
    {
        public bool SimulateIsolationFailure { get; set; }
        public string ComparisonStatus { get; set; } = "Completed";

        public Task<SimulationComparisonResponse> QueueComparisonAsync(SimulationComparisonRequest request, CancellationToken ct = default)
        {
            if (SimulateIsolationFailure)
            {
                throw new SimulationIsolationException("SIMULATION_DATABASE_NOT_ISOLATED: La base de simulación no puede ser la misma que la operativa.");
            }

            return Task.FromResult(new SimulationComparisonResponse(
                ComparisonId: Guid.NewGuid(),
                Status: "Queued",
                CreatedAtUtc: DateTime.UtcNow));
        }

        public Task<IReadOnlyList<SimulationComparisonSummary>> ListComparisonsAsync(int limit = 50, CancellationToken ct = default)
        {
            IReadOnlyList<SimulationComparisonSummary> list =
            [
                new(Guid.NewGuid(), "Completed", "national-v1", DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow, 100, "computed")
            ];
            return Task.FromResult(list);
        }

        public Task<SimulationComparisonDetail?> GetComparisonDetailAsync(Guid comparisonId, CancellationToken ct = default)
        {
            if (comparisonId == Guid.Empty)
            {
                return Task.FromResult<SimulationComparisonDetail?>(null);
            }

            var detail = new SimulationComparisonDetail(
                ComparisonId: comparisonId,
                Status: ComparisonStatus,
                DatasetVersion: "national-v1",
                ProgressPercentage: ComparisonStatus == "Completed" ? 100 : 45,
                CreatedAtUtc: DateTime.UtcNow.AddMinutes(-5),
                CompletedAtUtc: ComparisonStatus == "Completed" ? DateTime.UtcNow : null,
                Origin: "computed",
                HostName: "test-host",
                CommitHash: "test-commit",
                FailureReason: null,
                Assumptions: ["Matriz vial precargada / aproximación euclidiana etiquetada"],
                Runs:
                [
                    new(Guid.NewGuid(), 42, "base", "reactive-v1", ComparisonStatus, 100, null),
                    new(Guid.NewGuid(), 42, "base", "fixed7-v1", ComparisonStatus, 100, null),
                    new(Guid.NewGuid(), 42, "base", "istpetdev-v1", ComparisonStatus, 100, null)
                ],
                Aggregated:
                [
                    new("istpetdev-v1", "km_total", 100m, 120m, 150m, 180m)
                ],
                Metrics:
                [
                    new("km_total", "istpetdev-v1", null, 120m, null, 120m, "km", null)
                ]);

            return Task.FromResult<SimulationComparisonDetail?>(detail);
        }

        public Task<ExportPackage?> ExportComparisonAsync(Guid comparisonId, CancellationToken ct = default)
        {
            if (ComparisonStatus != "Completed")
            {
                return Task.FromResult<ExportPackage?>(null);
            }

            var fakeZip = Encoding.UTF8.GetBytes("fake-zip-data");
            var pkg = new ExportPackage(fakeZip, "\"hash123\"", "hash123", new Dictionary<string, string>());
            return Task.FromResult<ExportPackage?>(pkg);
        }

        public Task<SimulationImportResult> ImportComparisonAsync(byte[] zipBytes, string? hostName = null, string? commitHash = null, CancellationToken ct = default)
        {
            var text = Encoding.UTF8.GetString(zipBytes);
            if (text.Contains("corrupt"))
            {
                return Task.FromResult(new SimulationImportResult(false, null, 0, "Hash mismatch for metrics.json"));
            }

            return Task.FromResult(new SimulationImportResult(true, Guid.NewGuid(), 12, null));
        }
    }

    [Fact]
    public async Task Post_Comparison_Returns_202_Accepted_With_Queued_Status()
    {
        var fake = new FakeSimulationService();
        var controller = new SimulationsController(fake);

        var request = new SimulationComparisonRequest(
            DatasetVersion: "national-v1",
            Seeds: [42, 101, 2024],
            IncludeAdverse: true);

        var result = await controller.CreateComparison(request, CancellationToken.None);

        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        var response = Assert.IsType<SimulationComparisonResponse>(acceptedResult.Value);
        Assert.Equal("Queued", response.Status);
        Assert.NotEqual(Guid.Empty, response.ComparisonId);
    }

    [Fact]
    public async Task Post_Comparison_Returns_422_When_Database_Not_Isolated()
    {
        var fake = new FakeSimulationService { SimulateIsolationFailure = true };
        var controller = new SimulationsController(fake);

        var request = new SimulationComparisonRequest();
        var result = await controller.CreateComparison(request, CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
        Assert.NotNull(unprocessable.Value);
    }

    [Fact]
    public async Task Get_Comparisons_Returns_200_List()
    {
        var fake = new FakeSimulationService();
        var controller = new SimulationsController(fake);

        var result = await controller.ListComparisons(50, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<SimulationComparisonSummary>>(okResult.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task Get_Comparison_Detail_Returns_200_And_404_When_Missing()
    {
        var fake = new FakeSimulationService();
        var controller = new SimulationsController(fake);

        var okResult = await controller.GetComparison(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(okResult);

        var notFoundResult = await controller.GetComparison(Guid.Empty, CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(notFoundResult);
    }

    [Fact]
    public async Task Export_Returns_409_When_In_Progress()
    {
        var fake = new FakeSimulationService { ComparisonStatus = "Running" };
        var controller = new SimulationsController(fake);

        var result = await controller.ExportComparison(Guid.NewGuid(), CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflict.Value);
    }

    [Fact]
    public async Task Export_Returns_Zip_File_And_ETag_When_Completed()
    {
        var fake = new FakeSimulationService { ComparisonStatus = "Completed" };
        var controller = new SimulationsController(fake);

        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var id = Guid.NewGuid();
        var result = await controller.ExportComparison(id, CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/zip", fileResult.ContentType);
        Assert.Equal($"simulation-{id}.zip", fileResult.FileDownloadName);
        Assert.Equal("\"hash123\"", httpContext.Response.Headers.ETag.ToString());
    }

    [Fact]
    public async Task Import_Returns_422_On_Hash_Mismatch()
    {
        var fake = new FakeSimulationService();
        var controller = new SimulationsController(fake);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("corrupt-data"));
        httpContext.Request.ContentLength = 12;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.ImportComparison(null, CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
        Assert.NotNull(unprocessable.Value);
    }

    [Fact]
    public async Task Import_Returns_200_On_Valid_Zip()
    {
        var fake = new FakeSimulationService();
        var controller = new SimulationsController(fake);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("valid-data"));
        httpContext.Request.ContentLength = 10;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.ImportComparison(null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void All_Comparison_Endpoints_Are_Protected_With_Planning_Role()
    {
        var controllerType = typeof(SimulationsController);

        var methods = new[]
        {
            nameof(SimulationsController.CreateComparison),
            nameof(SimulationsController.ListComparisons),
            nameof(SimulationsController.GetComparison),
            nameof(SimulationsController.ExportComparison),
            nameof(SimulationsController.ImportComparison)
        };

        foreach (var name in methods)
        {
            var method = controllerType.GetMethod(name);
            Assert.NotNull(method);
            var auth = (AuthorizeAttribute?)Attribute.GetCustomAttribute(method, typeof(AuthorizeAttribute));
            Assert.NotNull(auth);
            Assert.Equal(Roles.Planning, auth.Roles);
            Assert.Contains(Roles.Gerencia, auth.Roles);
            Assert.Contains(Roles.Planificador, auth.Roles);
            Assert.DoesNotContain(Roles.Conductor, auth.Roles);
            Assert.DoesNotContain(Roles.Automatizacion, auth.Roles);
        }
    }
}
