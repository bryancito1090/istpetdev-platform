namespace IstpetDev.Application.Accounts;

public sealed record ProvisioningRow(int Line, string Email, string DisplayName, string Role);

public sealed record ProvisioningError(int Line, string Message);

// CSV de H3 (fuera del repositorio, G1): cabecera «email,displayName,role»; # inicia un comentario.
public sealed record ProvisioningInput(IReadOnlyList<ProvisioningRow> Rows, IReadOnlyList<ProvisioningError> Errors)
{
    public const string Header = "email,displayName,role";

    public static ProvisioningInput Parse(TextReader reader)
    {
        var rows = new List<ProvisioningRow>();
        var errors = new List<ProvisioningError>();
        var number = 0;
        var headerSeen = false;
        for (var line = reader.ReadLine(); line is not null; line = reader.ReadLine())
        {
            number++;
            var text = line.Trim();
            if (text.Length == 0 || text.StartsWith('#')) continue;
            if (!headerSeen)
            {
                headerSeen = true;
                if (!string.Equals(text.Replace(" ", string.Empty), Header, StringComparison.OrdinalIgnoreCase))
                    errors.Add(new(number, $"La primera fila debe ser «{Header}»."));
                continue;
            }
            var fields = text.Split(',');
            if (fields.Length != 3)
            {
                errors.Add(new(number, "Se esperan 3 columnas: correo, nombre y rol (el nombre no puede llevar comas)."));
                continue;
            }
            rows.Add(new(number, fields[0].Trim(), fields[1].Trim(), fields[2].Trim()));
        }
        if (!headerSeen) errors.Add(new(0, "El archivo está vacío."));
        foreach (var repeated in rows.GroupBy(r => r.Email.ToLowerInvariant()).Where(g => g.Count() > 1))
            errors.Add(new(repeated.Last().Line, "Correo repetido en el archivo."));
        return new(rows, errors);
    }
}
