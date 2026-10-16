using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNet.Testcontainers.Builders;
using IstpetDev.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace IstpetDev.Tests.E2E;

/// <summary>
/// Runs the generated supply-tool subworkflows in a real n8n 2.39.5 container against a
/// recording stub. The tool payloads, guards and M2M headers are the ones shipped in
/// automation/n8n/workflows, executed by the real runtime. A real DeepSeek Watch is
/// exercised separately in the manual local ensayo; this test is CI-safe and deterministic.
/// </summary>
[Collection(PostgisCollection.Name)]
[Trait("Category", "E2E")]
public sealed class N8nSupplyToolsTests(ITestOutputHelper output)
{
    private const string ClientId = "n8n-supply-test";
    private const string M2mReference = "8e186867-4916-54b1-b968-c1c273f79a11";
    private const string ProductId = "11111111-1111-4111-8111-111111111111";
    private const string WarehouseId = "22222222-2222-4222-8222-222222222222";
    private const string PointId = "33333333-3333-4333-8333-333333333333";
    private static readonly string RunId = Guid.NewGuid().ToString();

    [Fact]
    public async Task Real_n8n_supply_tool_subworkflows_validate_arguments_and_call_api_with_run_header()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        await using var stub = builder.Build();
        var secret = Guid.NewGuid().ToString("N");
        var basic = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(ClientId + ":" + secret));
        var oauthRequests = 0;
        var calls = new ConcurrentBag<RecordedCall>();
        stub.MapPost("/oauth/token", (HttpContext context) =>
        {
            if (context.Request.Headers.Authorization != basic) return Results.Unauthorized();
            Interlocked.Increment(ref oauthRequests);
            return Results.Json(new { access_token = "stub-token-" + Guid.NewGuid().ToString("N"), token_type = "Bearer", expires_in = 600 });
        });
        stub.Map("/api/{**path}", async (HttpContext context) =>
        {
            var body = context.Request.ContentLength > 0
                ? await new StreamReader(context.Request.Body).ReadToEndAsync()
                : string.Empty;
            calls.Add(new RecordedCall(
                context.Request.Method,
                context.Request.Path.Value!,
                context.Request.QueryString.Value ?? string.Empty,
                body,
                context.Request.Headers["X-Agent-Run-Id"].ToString()));
            return Results.Json(new { ok = true });
        });
        await stub.StartAsync();
        var stubUrl = stub.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();

        var root = Path.GetFullPath(Path.Combine(Fixtures.Folder("demo-base"), "../../.."));
        var n8nPort = FreePort();
        var n8nUrl = $"http://127.0.0.1:{n8nPort}";
        var credentials = JsonSerializer.SerializeToUtf8Bytes(new[]
        {
            new { id = M2mReference, name = "IstpetDev M2M", type = "oAuth2Api", data = new
            {
                grantType = "clientCredentials", accessTokenUrl = stubUrl + "/oauth/token", clientId = ClientId,
                clientSecret = secret, scope = "istpetdev-api/automation", authentication = "header"
            } }
        });

        var workflows = new Dictionary<string, string>();
        foreach (var tool in new[] { "find_stock", "plan_sourcing", "vehicle_availability" })
        {
            var workflow = BuildStandaloneToolWorkflow(tool, SignedContext(DateTime.UtcNow.AddMinutes(10)));
            workflow["id"] = Guid.NewGuid().ToString();
            workflow["name"] = "Supply tool " + tool;
            workflows[tool] = workflow.ToJsonString();
        }
        var expired = BuildStandaloneToolWorkflow("find_stock", SignedContext(DateTime.UtcNow.AddSeconds(-5)));
        expired["id"] = Guid.NewGuid().ToString();
        expired["name"] = "Supply tool expired";
        expired["nodes"]!.AsArray().Single(node => node!["name"]!.GetValue<string>() == "Webhook")!["parameters"]!["path"] = "istpetdev/tools/expired";
        workflows["expired"] = expired.ToJsonString();

        var containerBuilder = new ContainerBuilder().WithImage("docker.n8n.io/n8nio/n8n:2.39.5")
            .WithCreateParameterModifier(parameters => parameters.HostConfig.NetworkMode = "host")
            .WithEntrypoint("/bin/sh", "-c")
            .WithCommand("n8n import:credentials --input=/probe/credentials.json && node /probe/bootstrap.cjs && exec n8n start")
            .WithEnvironment("N8N_PORT", n8nPort.ToString())
            .WithEnvironment("N8N_LISTEN_ADDRESS", "127.0.0.1")
            .WithEnvironment("N8N_RUNNERS_BROKER_PORT", FreePort().ToString())
            .WithEnvironment("N8N_ENCRYPTION_KEY", secret)
            .WithEnvironment("N8N_SECURE_COOKIE", "false")
            .WithEnvironment("N8N_BLOCK_ENV_ACCESS_IN_NODE", "false")
            .WithEnvironment("NODE_FUNCTION_ALLOW_BUILTIN", "crypto")
            .WithEnvironment("N8N_DIAGNOSTICS_ENABLED", "false")
            .WithEnvironment("N8N_VERSION_NOTIFICATIONS_ENABLED", "false")
            .WithEnvironment("ISTPETDEV_API_BASE_URL", stubUrl + "/api/v1")
            .WithResourceMapping(credentials, "/probe/credentials.json")
            .WithResourceMapping(Encoding.UTF8.GetBytes(Bootstrap), "/probe/bootstrap.cjs")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Editor is now accessible via"));
        foreach (var (name, content) in workflows)
        {
            containerBuilder = containerBuilder.WithResourceMapping(Encoding.UTF8.GetBytes(content), $"/probe/{name}.json");
        }

        await using var n8n = containerBuilder.Build();
        try
        {
            using var startupTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
            await n8n.StartAsync(startupTimeout.Token);
            Assert.Equal("2.39.5", (await n8n.ExecAsync(["n8n", "--version"])).Stdout.Trim());
            var links = await n8n.ExecAsync(["cat", "/tmp/link-check.json"]);
            Assert.Equal("{\"published\":4}", links.Stdout.Trim());

            using var webhooks = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            using var stock = await webhooks.PostAsJsonAsync(n8nUrl + "/webhook/istpetdev/tools/find-stock",
                new { productId = ProductId, warehouseId = WarehouseId });
            Assert.Equal(HttpStatusCode.OK, stock.StatusCode);
            var stockCall = Assert.Single(calls, call => call.Path == "/api/v1/logistics/stock");
            Assert.Equal("GET", stockCall.Method);
            Assert.Contains("productId=" + ProductId, stockCall.Query);
            Assert.Contains("warehouseId=" + WarehouseId, stockCall.Query);
            Assert.Equal(RunId, stockCall.RunHeader);

            using var sourcing = await webhooks.PostAsJsonAsync(n8nUrl + "/webhook/istpetdev/tools/plan-sourcing",
                new { servicePointIds = new[] { PointId, PointId }, productId = ProductId });
            Assert.Equal(HttpStatusCode.OK, sourcing.StatusCode);
            var sourcingCall = Assert.Single(calls, call => call.Path == "/api/v1/logistics/sourcing");
            Assert.Equal("POST", sourcingCall.Method);
            using var sourcingBody = JsonDocument.Parse(sourcingCall.Body);
            Assert.Equal([PointId], sourcingBody.RootElement.GetProperty("servicePointIds").EnumerateArray().Select(item => item.GetString()!).ToArray());
            Assert.Equal(ProductId, sourcingBody.RootElement.GetProperty("productId").GetString());
            Assert.Equal(RunId, sourcingCall.RunHeader);

            using var vehicles = await webhooks.PostAsJsonAsync(n8nUrl + "/webhook/istpetdev/tools/vehicle-availability",
                new { date = "2099-12-31" });
            Assert.Equal(HttpStatusCode.OK, vehicles.StatusCode);
            var vehicleCall = Assert.Single(calls, call => call.Path == "/api/v1/logistics/vehicles/available");
            Assert.Equal("GET", vehicleCall.Method);
            Assert.Equal(string.Empty, vehicleCall.Body);
            Assert.Equal(RunId, vehicleCall.RunHeader);
            Assert.Equal(3, calls.Count);

            using var invalid = await webhooks.PostAsJsonAsync(n8nUrl + "/webhook/istpetdev/tools/find-stock",
                new { productId = "not-a-uuid" });
            Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
            using var invalidBody = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
            Assert.True(invalidBody.RootElement.GetProperty("skipped").GetBoolean());
            Assert.Equal("invalid_arguments", invalidBody.RootElement.GetProperty("reason").GetString());

            using var expiredResponse = await webhooks.PostAsJsonAsync(n8nUrl + "/webhook/istpetdev/tools/expired",
                new { productId = ProductId });
            Assert.Equal(HttpStatusCode.OK, expiredResponse.StatusCode);
            using var expiredBody = JsonDocument.Parse(await expiredResponse.Content.ReadAsStringAsync());
            Assert.True(expiredBody.RootElement.GetProperty("skipped").GetBoolean());
            Assert.Equal("deadline_too_short", expiredBody.RootElement.GetProperty("reason").GetString());
            Assert.Equal(3, calls.Count);
            Assert.True(oauthRequests > 0, "n8n debió obtener token M2M del stub");
            output.WriteLine(JsonSerializer.Serialize(new { n8n = "2.39.5", runId = RunId, calls = calls.Count, oauthRequests }));
        }
        catch
        {
            var logs = await n8n.GetLogsAsync();
            output.WriteLine(string.Join('\n', (logs.Stdout + logs.Stderr).Split('\n').TakeLast(80)));
            throw;
        }
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string SignedContext(DateTime deadlineUtc)
        => JsonSerializer.Serialize(new
        {
            runId = RunId,
            incidentId = (string?)null,
            kind = "Watch",
            apiBaseUrl = (string?)null,
            deadlineUtc = deadlineUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        });

    private static JsonObject BuildStandaloneToolWorkflow(string toolName, string contextJson)
    {
        var root = Path.GetFullPath(Path.Combine(Fixtures.Folder("demo-base"), "../../.."));
        using var generated = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "automation/n8n/workflows/vigia.json")));
        var expression = generated.RootElement.GetProperty("nodes").EnumerateArray()
            .Single(node => node.GetProperty("name").GetString() == toolName)
            .GetProperty("parameters").GetProperty("workflowJson").GetString()!;
        const string prefix = "={{ ";
        var end = expression.IndexOf("\".replace('__SIGNED_CONTEXT__'", StringComparison.Ordinal);
        using var literal = JsonDocument.Parse(expression[prefix.Length..(end + 1)]);
        var workflow = JsonNode.Parse(literal.RootElement.GetString()!)!.AsObject();

        var nodes = workflow["nodes"]!.AsArray();
        var trigger = nodes.Single(node => node!["name"]!.GetValue<string>() == "When Executed by Another Workflow")!;
        var triggerPosition = trigger["position"]!.DeepClone();
        nodes.Remove(trigger);
        nodes.Insert(0, new JsonObject
        {
            ["parameters"] = new JsonObject
            {
                ["httpMethod"] = "POST",
                ["path"] = $"istpetdev/tools/{toolName.Replace('_', '-')}",
                ["responseMode"] = "lastNode",
                ["options"] = new JsonObject()
            },
            ["id"] = Guid.NewGuid().ToString(),
            ["name"] = "Webhook",
            ["type"] = "n8n-nodes-base.webhook",
            ["typeVersion"] = 2.1,
            ["webhookId"] = Guid.NewGuid().ToString(),
            ["position"] = triggerPosition
        });
        nodes.Insert(1, new JsonObject
        {
            ["parameters"] = new JsonObject
            {
                ["mode"] = "runOnceForEachItem",
                ["jsCode"] = "return { json: Object.assign({}, $json.body, {}) };"
            },
            ["id"] = Guid.NewGuid().ToString(),
            ["name"] = "Flatten Body",
            ["type"] = "n8n-nodes-base.code",
            ["typeVersion"] = 2,
            ["position"] = new JsonArray(350, 280)
        });
        foreach (var node in nodes)
        {
            var code = node!["parameters"]?["jsCode"]?.GetValue<string>();
            if (code is not null && code.Contains("__SIGNED_CONTEXT__"))
            {
                node["parameters"]!["jsCode"] = code.Replace("__SIGNED_CONTEXT__", contextJson);
            }
        }

        var connections = workflow["connections"]!.AsObject();
        connections.Remove("When Executed by Another Workflow");
        connections["Webhook"] = new JsonObject
        {
            ["main"] = new JsonArray(new JsonArray(new JsonObject { ["node"] = "Flatten Body", ["type"] = "main", ["index"] = 0 }))
        };
        connections["Flatten Body"] = new JsonObject
        {
            ["main"] = new JsonArray(new JsonArray(new JsonObject { ["node"] = "Check Deadline", ["type"] = "main", ["index"] = 0 }))
        };
        return workflow;
    }

    private sealed record RecordedCall(string Method, string Path, string Query, string Body, string RunHeader);

    private const string Bootstrap = """
        const fs = require('fs');
        const {execFileSync: run} = require('child_process');
        const cli = args => run('n8n', args, {stdio: 'inherit'});
        const names = ['find_stock', 'plan_sourcing', 'vehicle_availability', 'expired'];
        fs.mkdirSync('/tmp/import');
        for (const name of names) {
          const workflow = JSON.parse(fs.readFileSync('/probe/' + name + '.json'));
          if (!workflow.id) throw new Error('Missing workflow id for ' + name);
          fs.writeFileSync('/tmp/import/' + name + '.json', JSON.stringify(workflow));
        }
        cli(['import:workflow', '--separate', '--input=/tmp/import']);
        cli(['export:workflow', '--all', '--output=/tmp/exported.json']);
        const exported = JSON.parse(fs.readFileSync('/tmp/exported.json'));
        if (exported.length !== names.length) throw new Error('Unexpected workflow count: ' + exported.length);
        for (const workflow of exported) cli(['publish:workflow', '--id=' + workflow.id]);
        fs.writeFileSync('/tmp/link-check.json', JSON.stringify({published: exported.length}));
        """;
}
