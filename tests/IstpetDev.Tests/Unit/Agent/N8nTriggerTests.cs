using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IstpetDev.Application.Agent;
using IstpetDev.Infrastructure.Agent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IstpetDev.Tests.Unit.Agent;

[Trait("Category", "Unit")]
public sealed class N8nTriggerTests
{
    private const string Secret = "super-secret-hmac-key";

    private sealed class MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }

    private sealed class DelayedMockHttpHandler(TimeSpan delay) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Webhook_request_contains_expected_headers_and_valid_signature()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new MockHttpHandler(req =>
        {
            capturedRequest = req;
            capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = new HttpClient(handler);
        var options = Options.Create(new N8nOptions
        {
            BaseUrl = "http://localhost:5678",
            WebhookSecret = Secret,
            WatchWebhookPath = "/webhook/agent-watch",
            ApiBaseUrl = "http://localhost:5000",
            TimeoutSeconds = 10
        });

        var trigger = new N8nTrigger(client, options, NullLogger<N8nTrigger>.Instance);
        var runId = Guid.NewGuid();

        var result = await trigger.TriggerAsync(runId, AgentRunKind.Watch, null, CancellationToken.None);

        Assert.Equal(TriggerOutcome.Success, result);
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedBody);
        Assert.Equal("http://localhost:5678/webhook/agent-watch", capturedRequest.RequestUri?.ToString());

        // Validate timestamp header
        Assert.True(capturedRequest.Headers.Contains(N8nTrigger.TimestampHeader));
        var timestampStr = capturedRequest.Headers.GetValues(N8nTrigger.TimestampHeader).First();
        Assert.True(long.TryParse(timestampStr, out var timestamp));
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Assert.InRange(timestamp, now - 5, now + 5);

        // Validate signature header
        Assert.True(capturedRequest.Headers.Contains(N8nTrigger.SignatureHeader));
        var signature = capturedRequest.Headers.GetValues(N8nTrigger.SignatureHeader).First();
        Assert.StartsWith("sha256=", signature);

        var expectedSignature = N8nTrigger.ComputeSignature(timestampStr, capturedBody, Secret);
        Assert.Equal(expectedSignature, signature);

        // Validate body content
        using var doc = JsonDocument.Parse(capturedBody);
        var root = doc.RootElement;
        Assert.Equal(runId, root.GetProperty("runId").GetGuid());
        Assert.Equal("Watch", root.GetProperty("kind").GetString());
        Assert.Equal("http://localhost:5000", root.GetProperty("apiBaseUrl").GetString());
        Assert.True(root.TryGetProperty("deadlineUtc", out _));

        // Validate verification function accepts it
        Assert.True(N8nTrigger.VerifySignature(timestampStr, signature, capturedBody, Secret));
    }

    [Fact]
    public async Task Incident_webhook_uses_incident_path_and_payload()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new MockHttpHandler(req =>
        {
            capturedRequest = req;
            capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        var client = new HttpClient(handler);
        var options = Options.Create(new N8nOptions
        {
            BaseUrl = "http://localhost:5678",
            WebhookSecret = Secret,
            IncidentWebhookPath = "/webhook/agent-incident",
            ApiBaseUrl = "http://localhost:5000"
        });

        var trigger = new N8nTrigger(client, options, NullLogger<N8nTrigger>.Instance);
        var runId = Guid.NewGuid();
        var incidentId = Guid.NewGuid();

        var result = await trigger.TriggerAsync(runId, AgentRunKind.Incident, incidentId, CancellationToken.None);

        Assert.Equal(TriggerOutcome.Success, result);
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedBody);
        Assert.Equal("http://localhost:5678/webhook/agent-incident", capturedRequest.RequestUri?.ToString());

        using var doc = JsonDocument.Parse(capturedBody);
        Assert.Equal(incidentId, doc.RootElement.GetProperty("incidentId").GetGuid());
        Assert.Equal("Incident", doc.RootElement.GetProperty("kind").GetString());
    }

    [Fact]
    public async Task WebhookUrl_configuration_targets_watch_and_incident_suffixes()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = new HttpClient(handler);
        var options = Options.Create(new N8nOptions
        {
            WebhookUrl = "http://localhost:5678/webhook/istpetdev/agent",
            WebhookSecret = Secret,
            ApiBaseUrl = "http://localhost:5000"
        });

        var trigger = new N8nTrigger(client, options, NullLogger<N8nTrigger>.Instance);

        // Test Watch suffix
        await trigger.TriggerAsync(Guid.NewGuid(), AgentRunKind.Watch, null, CancellationToken.None);
        Assert.Equal("http://localhost:5678/webhook/istpetdev/agent/watch", capturedRequest?.RequestUri?.ToString());

        // Test Incident suffix
        await trigger.TriggerAsync(Guid.NewGuid(), AgentRunKind.Incident, Guid.NewGuid(), CancellationToken.None);
        Assert.Equal("http://localhost:5678/webhook/istpetdev/agent/incident", capturedRequest?.RequestUri?.ToString());
    }

    [Fact]
    public async Task TriggerAsync_with_explicit_deadline_uses_it_in_payload()
    {
        string? capturedBody = null;
        var handler = new MockHttpHandler(req =>
        {
            capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = new HttpClient(handler);
        var options = Options.Create(new N8nOptions { WebhookSecret = Secret });
        var trigger = new N8nTrigger(client, options, NullLogger<N8nTrigger>.Instance);

        var explicitDeadline = DateTime.SpecifyKind(new DateTime(2030, 1, 1, 12, 0, 0), DateTimeKind.Utc);
        await trigger.TriggerAsync(Guid.NewGuid(), AgentRunKind.Watch, null, explicitDeadline, CancellationToken.None);

        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody);
        Assert.Equal(explicitDeadline, doc.RootElement.GetProperty("deadlineUtc").GetDateTime());
    }

    [Fact]
    public async Task Expired_deadline_does_not_send_webhook()
    {
        var sent = false;
        var handler = new MockHttpHandler(_ =>
        {
            sent = true;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var trigger = new N8nTrigger(
            new HttpClient(handler),
            Options.Create(new N8nOptions { WebhookSecret = Secret }),
            NullLogger<N8nTrigger>.Instance);

        var outcome = await trigger.TriggerAsync(
            Guid.NewGuid(), AgentRunKind.Watch, null,
            DateTime.UtcNow.AddMilliseconds(-1), CancellationToken.None);

        Assert.Equal(TriggerOutcome.Error, outcome);
        Assert.False(sent);
    }

    [Fact]
    public async Task Missing_webhook_secret_does_not_send_webhook()
    {
        var sent = false;
        var handler = new MockHttpHandler(_ =>
        {
            sent = true;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var trigger = new N8nTrigger(
            new HttpClient(handler),
            Options.Create(new N8nOptions()),
            NullLogger<N8nTrigger>.Instance);

        var outcome = await trigger.TriggerAsync(Guid.NewGuid(), AgentRunKind.Watch, null, CancellationToken.None);

        Assert.Equal(TriggerOutcome.Error, outcome);
        Assert.False(sent);
    }

    [Fact]
    public void Tampered_body_fails_verification()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var body = "{\"runId\":\"1234\",\"kind\":\"Watch\"}";
        var signature = N8nTrigger.ComputeSignature(timestamp, body, Secret);

        Assert.True(N8nTrigger.VerifySignature(timestamp, signature, body, Secret));

        // Modified body
        var tamperedBody = "{\"runId\":\"1234\",\"kind\":\"Incident\"}";
        Assert.False(N8nTrigger.VerifySignature(timestamp, signature, tamperedBody, Secret));

        // Modified secret
        Assert.False(N8nTrigger.VerifySignature(timestamp, signature, body, "wrong-secret"));
    }

    [Fact]
    public void Expired_timestamp_fails_verification()
    {
        var expiredTimestamp = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 65).ToString();
        var body = "{\"test\":true}";
        var signature = N8nTrigger.ComputeSignature(expiredTimestamp, body, Secret);

        // Max age 60s -> should reject 65s old timestamp
        Assert.False(N8nTrigger.VerifySignature(expiredTimestamp, signature, body, Secret, maxAgeSeconds: 60));
    }

    [Fact]
    public async Task Non_2xx_response_returns_Error_outcome()
    {
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = new HttpClient(handler);
        var options = Options.Create(new N8nOptions { WebhookSecret = Secret });

        var trigger = new N8nTrigger(client, options, NullLogger<N8nTrigger>.Instance);
        var result = await trigger.TriggerAsync(Guid.NewGuid(), AgentRunKind.Watch, null, CancellationToken.None);

        Assert.Equal(TriggerOutcome.Error, result);
    }

    [Fact]
    public async Task Timeout_longer_than_2s_returns_Error_outcome()
    {
        // Delay of 3 seconds exceeds the 2s internal timeout
        var handler = new DelayedMockHttpHandler(TimeSpan.FromSeconds(3));
        var client = new HttpClient(handler);
        var options = Options.Create(new N8nOptions { WebhookSecret = Secret });

        var trigger = new N8nTrigger(client, options, NullLogger<N8nTrigger>.Instance);
        var result = await trigger.TriggerAsync(Guid.NewGuid(), AgentRunKind.Watch, null, CancellationToken.None);

        Assert.Equal(TriggerOutcome.Error, result);
    }
}
