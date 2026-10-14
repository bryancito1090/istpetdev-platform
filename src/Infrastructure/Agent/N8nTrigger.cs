using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IstpetDev.Application.Agent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IstpetDev.Infrastructure.Agent;

public sealed class N8nOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5678";
    public string? WebhookUrl { get; set; }
    public string WebhookSecret { get; set; } = string.Empty;
    public string WatchWebhookPath { get; set; } = "/webhook/agent-watch";
    public string IncidentWebhookPath { get; set; } = "/webhook/agent-incident";
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";
    public int TimeoutSeconds { get; set; } = 10;
}

public sealed class N8nTrigger(
    HttpClient httpClient,
    IOptions<N8nOptions> options,
    ILogger<N8nTrigger> logger) : IN8nTrigger
{
    public const string TimestampHeader = "X-IstpetDev-Timestamp";
    public const string SignatureHeader = "X-IstpetDev-Signature";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public Task<TriggerOutcome> TriggerAsync(Guid runId, AgentRunKind kind, Guid? incidentId, CancellationToken ct)
        => TriggerAsync(runId, kind, incidentId, null, ct);

    public async Task<TriggerOutcome> TriggerAsync(Guid runId, AgentRunKind kind, Guid? incidentId, DateTime? deadlineUtc, CancellationToken ct)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.WebhookSecret))
        {
            logger.LogWarning("n8n webhook secret is not configured for run {RunId}", runId);
            return TriggerOutcome.Error;
        }

        string targetUrl;
        if (!string.IsNullOrWhiteSpace(opts.WebhookUrl))
        {
            var suffix = kind == AgentRunKind.Incident ? "incident" : "watch";
            targetUrl = $"{opts.WebhookUrl.TrimEnd('/')}/{suffix}";
        }
        else
        {
            var path = kind == AgentRunKind.Incident
                ? opts.IncidentWebhookPath
                : opts.WatchWebhookPath;

            targetUrl = string.IsNullOrWhiteSpace(opts.BaseUrl)
                ? path
                : $"{opts.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }

        var deadline = deadlineUtc ?? DateTime.UtcNow.AddSeconds(opts.TimeoutSeconds);

        var payload = new
        {
            runId,
            kind = kind.ToString(),
            incidentId,
            apiBaseUrl = opts.ApiBaseUrl,
            deadlineUtc = deadline
        };

        var bodyJson = JsonSerializer.Serialize(payload, JsonOpts);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = ComputeSignature(timestamp, bodyJson, opts.WebhookSecret);

        var remaining = deadline - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            logger.LogWarning("Agent run {RunId} reached its deadline before the n8n webhook call", runId);
            return TriggerOutcome.Error;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl)
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };

        request.Headers.Add(TimestampHeader, timestamp);
        request.Headers.Add(SignatureHeader, signature);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var requestTimeout = remaining < TimeSpan.FromSeconds(2) ? remaining : TimeSpan.FromSeconds(2);
        timeoutCts.CancelAfter(requestTimeout);

        try
        {
            var response = await httpClient.SendAsync(request, timeoutCts.Token);
            if (response.IsSuccessStatusCode)
            {
                return TriggerOutcome.Success;
            }

            logger.LogWarning("n8n webhook returned non-success status code {StatusCode} for run {RunId}", (int)response.StatusCode, runId);
            return TriggerOutcome.Error;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("n8n webhook timed out after {TimeoutMs}ms for run {RunId}", (int)requestTimeout.TotalMilliseconds, runId);
            return TriggerOutcome.Error;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to call n8n webhook for run {RunId}", runId);
            return TriggerOutcome.Error;
        }
    }

    public static string ComputeSignature(string timestamp, string body, string secret)
    {
        var messageToSign = Encoding.UTF8.GetBytes($"{timestamp}.{body}");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(messageToSign);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool VerifySignature(string timestampHeader, string signatureHeader, string body, string secret, int maxAgeSeconds = 60)
    {
        if (string.IsNullOrWhiteSpace(timestampHeader) || string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        if (!long.TryParse(timestampHeader, out var timestampSeconds))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - timestampSeconds) > maxAgeSeconds)
        {
            return false;
        }

        var expectedSignature = ComputeSignature(timestampHeader, body, secret);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
        var actualBytes = Encoding.UTF8.GetBytes(signatureHeader);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
