using System.Text.Json;
using System.Text.Json.Serialization;

namespace IstpetDev.Application.Imports;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        MaxDepth = 16,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() },
    };
}

public sealed class FileMapping
{
    public string? File { get; set; }
    public string? Sheet { get; set; }
    public Dictionary<string, string>? Columns { get; set; }
    public Dictionary<string, string>? Constants { get; set; }
    public Dictionary<string, decimal>? Scale { get; set; }
}

public sealed class MappingProfile
{
    public string Name { get; set; } = "auto";
    public char? Delimiter { get; set; }
    public string? DecimalSeparator { get; set; }
    public string[]? DateFormats { get; set; }
    public Dictionary<string, FileMapping> Files { get; set; } = new();

    public static MappingProfile Auto() => new();

    public FileMapping? For(ImportEntity entity) =>
        Files.FirstOrDefault(pair => ImportEntityKeys.TryParse(pair.Key, out var parsed) && parsed == entity).Value;

    public ImportEntity? EntityForFile(string fileName)
    {
        foreach (var (key, mapping) in Files)
            if (string.Equals(mapping.File, fileName, StringComparison.OrdinalIgnoreCase) && ImportEntityKeys.TryParse(key, out var entity))
                return entity;
        return null;
    }

    public ParseSettings ToParseSettings() =>
        new(DecimalSeparator, (DateFormats ?? []).Concat(ParseSettings.DefaultDateFormats).Distinct().ToList());

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (Files is null) return ["files no puede ser nulo."];
        if (Delimiter is not null && !",;\t|".Contains(Delimiter.Value)) errors.Add("delimiter debe ser coma, punto y coma, tabulador o barra vertical.");
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 256) errors.Add("name debe contener entre 1 y 256 caracteres.");
        var entities = new HashSet<ImportEntity>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, file) in Files)
        {
            if (!ImportEntityKeys.TryParse(key, out var entity))
            {
                errors.Add($"Entidad desconocida '{key}'. Válidas: {string.Join(", ", ImportEntityKeys.All)}.");
                continue;
            }
            var valid = CanonicalSchema.Fields[entity].Select(f => f.Name).ToHashSet();
            if (!entities.Add(entity)) errors.Add($"Hay dos perfiles para '{entity.Key()}'; use una sola clave.");
            if (file is null) { errors.Add($"'{key}' no puede ser nulo."); continue; }
            if (file.File is not null && (Path.GetFileName(file.File) != file.File || !names.Add(file.File)))
                errors.Add($"'{key}.file' debe ser un nombre único sin ruta.");
            foreach (var (field, factor) in file.Scale ?? [])
                if (factor <= 0 || !CanonicalSchema.Fields[entity].Any(f => f.Name == field && f.Type is FieldType.Decimal or FieldType.Integer))
                    errors.Add($"'{key}.{field}': la escala debe ser positiva y aplicarse a un campo numérico.");
            foreach (var (field, value) in file.Columns ?? [])
                if (string.IsNullOrWhiteSpace(value) || value.Length > ImportLimits.CellCharacters) errors.Add($"Columna origen de '{key}.{field}' inválida.");
            foreach (var (field, value) in file.Constants ?? [])
                if (value is null || value.Length > ImportLimits.CellCharacters) errors.Add($"Constante '{key}.{field}' inválida o demasiado larga.");
            var used = (file.Columns?.Keys ?? Enumerable.Empty<string>())
                .Concat(file.Constants?.Keys ?? Enumerable.Empty<string>())
                .Concat(file.Scale?.Keys ?? Enumerable.Empty<string>());
            foreach (var field in used.Where(f => !valid.Contains(f)).Distinct())
                errors.Add($"'{key}.{field}' no es un campo canónico. Válidos: {string.Join(", ", valid)}.");
        }
        if (DecimalSeparator is not null and not ("," or "."))
            errors.Add("decimalSeparator debe ser \",\" o \".\".");
        return errors;
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonDefaults.Options);

    public static MappingProfile Parse(string json)
    {
        MappingProfile? profile;
        if (System.Text.Encoding.UTF8.GetByteCount(json) > ImportLimits.ProfileBytes)
            throw new MappingProfileException(["El perfil supera 64 KiB; reduzca su contenido."]);
        try { profile = JsonSerializer.Deserialize<MappingProfile>(json, JsonDefaults.Options); }
        catch (JsonException ex) { throw new MappingProfileException([$"JSON inválido o propiedades/tipos no admitidos; revise {ex.Path ?? "la estructura"}."]); }
        if (profile is null) throw new MappingProfileException(["El perfil está vacío."]);
        var errors = profile.Validate();
        if (errors.Count > 0) throw new MappingProfileException(errors);
        return profile;
    }
}

public sealed class MappingProfileException(IReadOnlyList<string> errors) : Exception(string.Join(" ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
