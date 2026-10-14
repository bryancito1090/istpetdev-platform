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
        Assert.Equal(2, files.Length);

        var violations = new List<string>();
        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            Inspect(document.RootElement, Path.GetFileName(file), violations);
        }

        Assert.Empty(violations);
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
