namespace IstpetDev.Application.Imports;

public sealed record EntityDetection(ImportEntity? Entity, string Explanation);

// Reconoce la entidad de un archivo sin pedir que el usuario lo renombre: primero por el nombre
// del archivo y, si no basta, por sus columnas. Nunca adivina: ante empate o falta de columnas
// obligatorias devuelve null con la explicación para el usuario.
public static class ImportEntityDetector
{
    // "consumptions.csv", "Consumos Septiembre 2026.xlsx", "bodegas-quito.csv".
    public static ImportEntity? FromFileName(string? fileName)
    {
        var stem = HeaderNormalizer.Normalize(Path.GetFileNameWithoutExtension(fileName ?? string.Empty));
        if (stem.Length == 0) return null;
        if (ImportEntityKeys.TryParse(stem, out var exact)) return exact;

        var padded = $"_{stem}_";
        var matches = ImportEntityKeys.Names()
            .Where(n => padded.Contains($"_{n.Name}_", StringComparison.Ordinal))
            .Select(n => n.Entity)
            .Distinct()
            .ToList();
        return matches.Count == 1 ? matches[0] : null;
    }

    public static EntityDetection FromHeaders(IReadOnlyList<string> headers)
    {
        var results = new List<(ImportEntity Entity, int Resolved, int Ignored, List<string> Missing)>();
        foreach (var entity in CanonicalSchema.Order)
        {
            try
            {
                var resolution = ColumnResolver.Resolve(headers, entity, null);
                results.Add((entity, resolution.Columns.Count, resolution.Ignored.Count, resolution.MissingRequired));
            }
            catch (MappingProfileException)
            {
                // Varias columnas compiten por un campo de esta entidad: no se usa para detectar.
            }
        }

        var complete = results.Where(r => r.Missing.Count == 0)
            .OrderByDescending(r => r.Resolved).ThenBy(r => r.Ignored).ToList();
        if (complete.Count == 1 || complete.Count > 1 && (complete[0].Resolved, complete[0].Ignored) != (complete[1].Resolved, complete[1].Ignored))
            return new(complete[0].Entity, $"Entidad '{complete[0].Entity.Key()}' detectada por sus columnas.");

        var columns = string.Join(", ", headers);
        if (complete.Count > 1)
            return new(null, $"Las columnas ({columns}) encajan igual en '{complete[0].Entity.Key()}' y '{complete[1].Entity.Key()}'. " +
                             "Ponga la entidad en el nombre del archivo (por ejemplo bodegas.csv) o declárela en un perfil de mapeo.");

        var closest = results.OrderBy(r => r.Missing.Count).ThenByDescending(r => r.Resolved).FirstOrDefault();
        var hint = closest.Missing is { Count: > 0 }
            ? $" La más cercana es '{closest.Entity.Key()}', a la que le faltan: {string.Join(", ", closest.Missing)}."
            : string.Empty;
        return new(null, $"No se reconoce a qué entidad pertenecen las columnas ({columns}).{hint} " +
                         "Si sus columnas usan otros nombres, declárelos en un perfil de mapeo (columns). " +
                         $"Entidades importables: {string.Join(", ", ImportEntityKeys.All)}.");
    }
}
