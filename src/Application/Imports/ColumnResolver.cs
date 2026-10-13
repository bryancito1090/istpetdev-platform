namespace IstpetDev.Application.Imports;

public sealed record ColumnResolution(Dictionary<string, string> Columns, List<string> MissingRequired, List<string> Ignored);

public static class ColumnResolver
{
    public static ColumnResolution Resolve(IReadOnlyList<string> headers, ImportEntity entity, FileMapping? mapping)
    {
        var byNormalized = new Dictionary<string, string>();
        foreach (var header in headers) byNormalized.TryAdd(HeaderNormalizer.Normalize(header), header);

        var columns = new Dictionary<string, string>();
        var used = new HashSet<string>();
        var missing = new List<string>();
        foreach (var field in CanonicalSchema.Fields[entity])
        {
            string? source;
            if (mapping?.Columns?.TryGetValue(field.Name, out var declared) == true)
                source = headers.FirstOrDefault(h => h == declared) ?? byNormalized.GetValueOrDefault(HeaderNormalizer.Normalize(declared));
            else
            {
                var candidates = field.Candidates().Select(c => byNormalized.GetValueOrDefault(c))
                    .Where(h => h is not null && !used.Contains(h)).Distinct().ToList();
                if (candidates.Count > 1)
                    throw new MappingProfileException([$"Varias columnas corresponden a '{field.Name}'; declare columns en el perfil."]);
                source = candidates.FirstOrDefault();
            }

            if (mapping?.Columns?.ContainsKey(field.Name) == true && source is null)
                throw new MappingProfileException([$"La columna declarada para '{field.Name}' no existe en el archivo."]);

            if (source is not null && used.Add(source)) columns[field.Name] = source;
            else if (field.Required && mapping?.Constants?.ContainsKey(field.Name) != true) missing.Add(field.Name);
        }
        return new ColumnResolution(columns, missing, headers.Where(h => !used.Contains(h)).ToList());
    }
}
