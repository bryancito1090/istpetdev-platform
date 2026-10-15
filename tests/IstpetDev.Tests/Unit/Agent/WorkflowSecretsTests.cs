using System.Text.Json;
using System.Text.RegularExpressions;
using IstpetDev.Tests.Support;

namespace IstpetDev.Tests.Unit.Agent;

[Trait("Category", "Unit")]
public sealed class WorkflowSecretsTests
{
    private static readonly HashSet<string> SecretFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "apikey", "password", "secret", "clientsecret", "token", "authorization"
    };

    private static readonly Regex[] SecretPatterns =
    [
        new(@"\bBearer\s+[A-Za-z0-9._~+/-]{12,}", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\b", RegexOptions.Compiled),
        new(@"\bsk-[A-Za-z0-9_-]{16,}\b", RegexOptions.Compiled),
        new(@"\bAKIA[A-Z0-9]{16}\b", RegexOptions.Compiled)
    ];

    [Fact]
    public void Exported_workflows_have_no_secret_fields_or_credential_values()
    {
        var directory = Path.GetFullPath(Path.Combine(Fixtures.Folder("demo-base"), "../../../automation/n8n/workflows"));
        var files = Directory.GetFiles(directory, "*.json");
        Assert.Equal(["error-agente.json", "incidente.json", "vigia.json"], files.Select(file => Path.GetFileName(file)!).Order().ToArray());

        var violations = new List<string>();
        var documents = new Dictionary<string, JsonDocument>();
        foreach (var file in files)
        {
            var document = JsonDocument.Parse(File.ReadAllText(file));
            documents.Add(Path.GetFileName(file), document);
            Inspect(document.RootElement, Path.GetFileName(file), violations);
        }

        Assert.Empty(violations);
        try
        {
            foreach (var name in new[] { "vigia.json", "incidente.json" })
            {
                var workflow = documents[name].RootElement;
                Assert.Equal("{{ERROR_WORKFLOW_ID}}", workflow.GetProperty("settings").GetProperty("errorWorkflow").GetString());
                var ack = workflow.GetProperty("nodes").EnumerateArray().Single(node => node.GetProperty("name").GetString() == "Respond Accepted");
                Assert.Contains("$execution.id", ack.GetProperty("parameters").GetProperty("responseBody").GetString());
                var register = workflow.GetProperty("nodes").EnumerateArray().Single(node => node.GetProperty("name").GetString() == "Register Execution");
                Assert.Contains("/execution", register.GetProperty("parameters").GetProperty("url").GetString());
                Assert.Equal("Register Execution", workflow.GetProperty("connections").GetProperty("Valid Signature?").GetProperty("main")[0][0].GetProperty("node").GetString());
                Assert.Equal("Respond Accepted", workflow.GetProperty("connections").GetProperty("Register Execution").GetProperty("main")[0][0].GetProperty("node").GetString());
            }

            var errorWorkflow = documents["error-agente.json"].RootElement;
            Assert.False(errorWorkflow.TryGetProperty("id", out _));
            Assert.Equal("n8n-nodes-base.errorTrigger", errorWorkflow.GetProperty("nodes")[0].GetProperty("type").GetString());
            var m2mReferences = errorWorkflow.GetProperty("nodes").EnumerateArray()
                .Where(node => node.TryGetProperty("credentials", out _))
                .SelectMany(node => node.GetProperty("credentials").EnumerateObject())
                .Select(property => property.Value.GetProperty("id").GetString()!)
                .Distinct()
                .ToArray();
            Assert.Equal(["8e186867-4916-54b1-b968-c1c273f79a11"], m2mReferences);
        }
        finally
        {
            foreach (var document in documents.Values) document.Dispose();
        }
    }

    private static void Inspect(JsonElement value, string path, List<string> violations)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                var normalized = Regex.Replace(property.Name, "[^a-z0-9]", "", RegexOptions.IgnoreCase);
                if (SecretFields.Contains(normalized)) violations.Add($"{path}.{property.Name}");
                Inspect(property.Value, $"{path}.{property.Name}", violations);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in value.EnumerateArray()) Inspect(item, $"{path}[{index++}]", violations);
        }
        else if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString() ?? string.Empty;
            if (SecretPatterns.Any(pattern => pattern.IsMatch(text))) violations.Add($"{path} contains a credential value");
            if (text.StartsWith('{'))
            {
                try
                {
                    using var nested = JsonDocument.Parse(text);
                    Inspect(nested.RootElement, path, violations);
                }
                catch (JsonException)
                {
                    // Ordinary node code and expressions are strings, not embedded JSON.
                }
            }
        }
    }
}
