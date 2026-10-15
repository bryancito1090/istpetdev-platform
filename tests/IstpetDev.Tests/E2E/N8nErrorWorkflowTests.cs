using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DotNet.Testcontainers.Builders;
using IstpetDev.Application.Agent;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace IstpetDev.Tests.E2E;

[Collection(PostgisCollection.Name)]
[Trait("Category", "E2E")]
public sealed class N8nErrorWorkflowTests(PostgisContainer postgres, ITestOutputHelper output)
{
    private const string ClientId = "n8n-runtime-test";
    private const string M2mReference = "8e186867-4916-54b1-b968-c1c273f79a11";
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Real_error_trigger_traces_both_workflows_and_preserves_unreachable_and_timeout_fallbacks()
    {
        // Linux host networking keeps the API bridge and n8n on loopback only.
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        await using var bridge = builder.Build();
        var secret = Guid.NewGuid().ToString("N");
        var basic = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(ClientId + ":" + secret));
        var oauthRequests = 0;
        var failures = new ConcurrentBag<string>();
        HttpClient callbacks = null!;
        bridge.MapPost("/oauth/token", (HttpContext context) =>
        {
            if (context.Request.Headers.Authorization != basic) return Results.Unauthorized();
            Interlocked.Increment(ref oauthRequests);
            return Results.Json(new { access_token = AutomationToken(), token_type = "Bearer", expires_in = 600 });
        });
        bridge.Map("/api/{**path}", async (HttpContext context) =>
        {
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
            if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
            {
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                if (context.Request.Path.Value!.EndsWith("/failure")) failures.Add(body);
            }
            request.Headers.TryAddWithoutValidation("Authorization", context.Request.Headers.Authorization.ToString());
            using var response = await callbacks.SendAsync(request);
            context.Response.StatusCode = (int)response.StatusCode;
            context.Response.ContentType = response.Content.Headers.ContentType?.ToString();
            await response.Content.CopyToAsync(context.Response.Body);
        });
        await bridge.StartAsync();
        var apiUrl = bridge.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var n8nPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var n8nUrl = $"http://127.0.0.1:{n8nPort}";
        var connection = postgres.NewDatabase();
        var settings = new Dictionary<string, string?>
        {
            ["Agent:Enabled"] = "true", ["Agent:TimeoutSeconds"] = "10", ["Agent:AutoOnIncident"] = "false",
            ["Agent:N8n:WebhookUrl"] = n8nUrl + "/webhook/istpetdev/agent",
            ["Agent:N8n:ApiBaseUrl"] = apiUrl + "/api/v1", ["Agent:N8n:WebhookSecret"] = secret,
            ["Authentication:AllowedClientIds:1"] = ClientId, ["Authentication:TechnicalClientIds:0"] = ClientId
        };
        await using var api = new ApiFactory(connection, overrides: settings);
        using var planner = api.ClientFor(TestTokens.Planner);
        callbacks = api.CreateClient();
        using var callbackClient = callbacks;
        await ResetAsync(planner, connection);

        var root = Path.GetFullPath(Path.Combine(Fixtures.Folder("demo-base"), "../../.."));
        var credentials = JsonSerializer.SerializeToUtf8Bytes(new[]
        {
            new { id = M2mReference, name = "IstpetDev M2M", type = "oAuth2Api", data = new
            {
                grantType = "clientCredentials", accessTokenUrl = apiUrl + "/oauth/token", clientId = ClientId,
                clientSecret = secret, scope = "istpetdev-api/automation", authentication = "header"
            } }
        });
        var containerBuilder = new ContainerBuilder().WithImage("docker.n8n.io/n8nio/n8n:2.39.5")
            .WithCreateParameterModifier(parameters => parameters.HostConfig.NetworkMode = "host")
            .WithEntrypoint("/bin/sh", "-c")
            .WithCommand("n8n import:credentials --input=/probe/credentials.json && node /probe/bootstrap.cjs && exec n8n start")
            .WithEnvironment("N8N_PORT", n8nPort.ToString()).WithEnvironment("N8N_LISTEN_ADDRESS", "127.0.0.1")
            .WithEnvironment("N8N_ENCRYPTION_KEY", secret).WithEnvironment("N8N_SECURE_COOKIE", "false")
            .WithEnvironment("N8N_BLOCK_ENV_ACCESS_IN_NODE", "false").WithEnvironment("NODE_FUNCTION_ALLOW_BUILTIN", "crypto")
            .WithEnvironment("N8N_DEFAULT_BINARY_DATA_MODE", "filesystem")
            .WithEnvironment("N8N_DIAGNOSTICS_ENABLED", "false").WithEnvironment("N8N_VERSION_NOTIFICATIONS_ENABLED", "false")
            .WithEnvironment("ISTPETDEV_API_BASE_URL", apiUrl + "/api/v1").WithEnvironment("ISTPETDEV_WEBHOOK_SECRET", secret)
            .WithResourceMapping(credentials, "/probe/credentials.json")
            .WithResourceMapping(Encoding.UTF8.GetBytes(Bootstrap), "/probe/bootstrap.cjs")
            .WithResourceMapping(File.ReadAllBytes(Path.Combine(root, "automation/n8n/workflows/error-agente.json")), "/probe/error-agente.json")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Editor is now accessible via"));
        foreach (var name in new[] { "vigia", "incidente", "silent" })
        {
            var workflow = JsonNode.Parse(File.ReadAllText(Path.Combine(root, $"automation/n8n/workflows/{(name == "silent" ? "vigia" : name)}.json")))!;
            // Inject one controlled fault after the genuine HMAC/registration/202 path; no LLM calls.
            var prepare = workflow["nodes"]!.AsArray().Single(node => node!["name"]!.GetValue<string>() == "Prepare Result")!;
            prepare["parameters"]!["jsCode"] = name == "silent" ? "return { json: {} };" : "throw new Error('ESC-19 controlled node exception');";
            workflow["connections"]!["Respond Accepted"]!["main"] = JsonSerializer.SerializeToNode(new[] { new[] { new { node = "Prepare Result", type = "main", index = 0 } } });
            if (name == "silent")
            {
                workflow["id"] = Guid.NewGuid().ToString();
                workflow["name"] = "IstpetDev Agent - timeout probe";
                workflow["nodes"]!.AsArray().Single(node => node!["name"]!.GetValue<string>() == "Webhook")!["parameters"]!["path"] = "istpetdev/silent/watch";
                workflow["connections"]!.AsObject().Remove("Prepare Result");
            }
            containerBuilder = containerBuilder.WithResourceMapping(Encoding.UTF8.GetBytes(workflow.ToJsonString()), $"/probe/{name}.json");
        }
        await using var n8n = containerBuilder.Build();
        try
        {
            using var startupTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
            await n8n.StartAsync(startupTimeout.Token);
            Assert.Equal("2.39.5", (await n8n.ExecAsync(["n8n", "--version"])).Stdout.Trim());
            var links = await n8n.ExecAsync(["cat", "/tmp/link-check.json"]);
            Assert.Equal("{\"serverAssigned\":true,\"linked\":3}", links.Stdout.Trim());
            using var webhooks = new HttpClient();
            Assert.Equal(HttpStatusCode.Unauthorized, (await webhooks.PostAsJsonAsync(n8nUrl + "/webhook/istpetdev/agent/watch", new { })).StatusCode);
            var results = new List<object>();
            foreach (var kind in new[] { AgentRunKind.Watch, AgentRunKind.Incident })
            {
                Guid? incidentId = null;
                if (kind == AgentRunKind.Incident)
                {
                    await using var db = TestDb.Context(connection);
                    foreach (var id in await db.RoutePlans.Where(plan => plan.Status == "Proposed").Select(plan => plan.Id).ToListAsync())
                        (await planner.PostAsJsonAsync($"/api/v1/route-plans/{id}/publish", new { })).EnsureSuccessStatusCode();
                    using var incident = await planner.PostAsJsonAsync("/api/v1/incidents", Fixtures.IncidentPreset("demo-region", "cierre-norte"));
                    incident.EnsureSuccessStatusCode();
                    incidentId = (await incident.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("incidentId").GetGuid();
                }
                var run = await StartAsync(planner, kind, incidentId);
                Assert.Equal(AgentRunStatus.Fallback, run.Status);
                Assert.Equal("timeout", run.FallbackReason);
                var failure = Assert.Single(run.ToolCalls, trace => trace.Event == "falló el nodo Prepare Result");
                Assert.StartsWith("ESC-19 controlled node exception", failure.Message);
                Assert.Equal(run.N8nExecutionId, failure.ExecutionId);
                Assert.Equal(kind == AgentRunKind.Watch ? "IstpetDev Agent - vigia" : "IstpetDev Agent - incidente", failure.Workflow);
                using var automation = api.CreateClient();
                automation.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AutomationToken());
                var payload = new AgentFailureRequest(run.RunId, failure.ExecutionId!, failure.Workflow!, failure.Node!, failure.Message!);
                Assert.Equal(HttpStatusCode.Forbidden, (await planner.PostAsJsonAsync($"/api/v1/agent/runs/{run.RunId}/failure", payload)).StatusCode);
                var retries = await Task.WhenAll(automation.PostAsJsonAsync($"/api/v1/agent/runs/{run.RunId}/failure", payload), automation.PostAsJsonAsync($"/api/v1/agent/runs/{run.RunId}/failure", payload));
                Assert.All(retries, retry => Assert.Equal(HttpStatusCode.Accepted, retry.StatusCode));
                foreach (var retry in retries) retry.Dispose();
                var reread = (await planner.GetFromJsonAsync<AgentRunView>($"/api/v1/agent/runs/{run.RunId}", Json))!;
                Assert.Single(reread.ToolCalls, trace => trace.ExecutionId == run.N8nExecutionId);
                results.Add(new { scenario = "ESC-19", kind, run.RunId, run.N8nExecutionId, failure.Node, failure.Message, traceCount = 1 });
            }
            Assert.Equal(2, failures.Count);
            foreach (var body in failures)
            {
                using var document = JsonDocument.Parse(body);
                Assert.Equal(new[] { "executionId", "message", "node", "runId", "workflow" }, document.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray());
            }
            Assert.True(oauthRequests > 0);

            await ResetAsync(planner, connection);
            await using var silentApi = new ApiFactory(connection, overrides: new Dictionary<string, string?>(settings)
            {
                ["Agent:N8n:WebhookUrl"] = n8nUrl + "/webhook/istpetdev/silent"
            });
            using var silentPlanner = silentApi.ClientFor(TestTokens.Planner);
            var timeout = await StartAsync(silentPlanner, AgentRunKind.Watch);
            Assert.Equal(AgentRunStatus.Fallback, timeout.Status);
            Assert.Equal("timeout", timeout.FallbackReason);
            Assert.InRange(timeout.LatencyMs, 9900, 11000);
            Assert.DoesNotContain(timeout.ToolCalls, trace => trace.Event?.StartsWith("falló el nodo") == true);
            Assert.NotEmpty(timeout.Decisions);
            Assert.All(timeout.Decisions, decision => Assert.Equal(ExplanationSource.Template, decision.Source));
            results.Add(new { scenario = "ESC-17", timeout.Status, timeout.FallbackReason, timeout.LatencyMs });

            await n8n.StopAsync();
            await ResetAsync(planner, connection);
            var down = await StartAsync(planner, AgentRunKind.Watch);
            Assert.Equal(AgentRunStatus.Fallback, down.Status);
            Assert.Equal("n8n_unreachable", down.FallbackReason);
            Assert.InRange(down.LatencyMs, 0, 11000);
            Assert.NotEmpty(down.Decisions);
            Assert.All(down.Decisions, decision => Assert.Equal(ExplanationSource.Template, decision.Source));
            results.Add(new { scenario = "ESC-16", down.Status, down.FallbackReason, down.LatencyMs });
            output.WriteLine(JsonSerializer.Serialize(new { n8n = "2.39.5", serverAssignedErrorWorkflow = true, g1Fields = 5, oauthRequests, results }, Json));
        }
        catch
        {
            var logs = await n8n.GetLogsAsync();
            output.WriteLine(string.Join('\n', (logs.Stdout + logs.Stderr).Split('\n').TakeLast(80)));
            throw;
        }
    }

    private static string AutomationToken() => TestTokens.Create(ClientId, clientId: ClientId, scope: "istpetdev-api/automation", role: Roles.Automatizacion);

    private static async Task ResetAsync(HttpClient planner, string connection)
    {
        (await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null)).EnsureSuccessStatusCode();
        await using var db = TestDb.Context(connection);
        if (!await db.Accounts.AnyAsync(account => account.Subject == ClientId))
        {
            db.Accounts.Add(new Account { Kind = "technical", Issuer = TestTokens.Issuer, Subject = ClientId, Role = Roles.Automatizacion, IsActive = true });
            await db.SaveChangesAsync();
        }
    }

    private static async Task<AgentRunView> StartAsync(HttpClient planner, AgentRunKind kind, Guid? incidentId = null)
    {
        using var response = await planner.PostAsJsonAsync("/api/v1/agent/runs", new StartRunRequest(kind, incidentId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AgentRunView>(Json))!;
    }

    private const string Bootstrap = """
        const fs = require('fs');
        const {execFileSync: run} = require('child_process');
        const cli = args => run('n8n', args, {stdio: 'inherit'});
        const imported = JSON.parse(fs.readFileSync('/probe/error-agente.json'));
        if (imported.id) throw new Error('Error workflow ID must not be committed');
        imported.id = require('crypto').randomUUID();
        fs.writeFileSync('/tmp/error-assigned.json', JSON.stringify(imported));
        cli(['import:workflow', '--input=/tmp/error-assigned.json']);
        cli(['export:workflow', '--all', '--output=/tmp/error-workflow.json']);
        const error = JSON.parse(fs.readFileSync('/tmp/error-workflow.json'))[0];
        if (!error.id || error.id === '{{ERROR_WORKFLOW_ID}}') throw new Error('Missing server-assigned workflow ID');
        fs.mkdirSync('/tmp/linked');
        for (const name of ['vigia', 'incidente', 'silent']) {
          const workflow = JSON.parse(fs.readFileSync('/probe/' + name + '.json'));
          if (workflow.settings.errorWorkflow !== '{{ERROR_WORKFLOW_ID}}') throw new Error('Unexpected workflow reference');
          workflow.settings.errorWorkflow = error.id;
          fs.writeFileSync('/tmp/linked/' + name + '.json', JSON.stringify(workflow));
        }
        cli(['import:workflow', '--separate', '--input=/tmp/linked']);
        cli(['publish:workflow', '--id=' + error.id]);
        for (const name of ['vigia', 'incidente', 'silent']) {
          const workflow = JSON.parse(fs.readFileSync('/tmp/linked/' + name + '.json'));
          cli(['publish:workflow', '--id=' + workflow.id]);
        }
        cli(['export:workflow', '--all', '--output=/tmp/linked-export.json']);
        const linked = JSON.parse(fs.readFileSync('/tmp/linked-export.json')).filter(w => w.id !== error.id);
        if (linked.length !== 3 || linked.some(w => w.settings.errorWorkflow !== error.id)) throw new Error('Error workflow not linked');
        fs.writeFileSync('/tmp/link-check.json', JSON.stringify({serverAssigned: true, linked: linked.length}));
        """;
}
