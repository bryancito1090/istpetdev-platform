namespace IstpetDev.Application.Imports;

public sealed record ParsedRow(int Row, IReadOnlyDictionary<string, object?> Values, IReadOnlySet<string>? SuppliedFields = null)
{
    public string? Text(string field) => Values.GetValueOrDefault(field) as string;
    public decimal? Decimal(string field) => Values.GetValueOrDefault(field) as decimal?;
    public int? Int(string field) => Values.GetValueOrDefault(field) as int?;
    public DateOnly? Date(string field) => Values.GetValueOrDefault(field) as DateOnly?;
    public TimeOnly? Time(string field) => Values.GetValueOrDefault(field) as TimeOnly?;
}

public static class RowParser
{
    // La fila 1 son los encabezados: los números de fila del reporte coinciden con los de una hoja de cálculo.
    private const int FirstDataRow = 2;

    public static List<ParsedRow> Parse(TabularData table, ImportEntity entity, ColumnResolution resolution,
        FileMapping? mapping, ParseSettings settings, ImportReport report)
    {
        var entityReport = report.Entity(entity);
        var rows = new List<ParsedRow>();
        for (var i = 0; i < table.Rows.Count; i++)
        {
            var rowNumber = table.RowNumbers?[i] ?? i + FirstDataRow;
            var values = new Dictionary<string, object?>();
            var supplied = new HashSet<string>();
            var valid = true;
            foreach (var field in CanonicalSchema.Fields[entity])
            {
                var text = resolution.Columns.TryGetValue(field.Name, out var source) ? table.Rows[i].GetValueOrDefault(source) : null;
                var constantUsed = mapping?.Constants?.TryGetValue(field.Name, out var constant) == true;
                if (constantUsed)
                {
                    text = mapping!.Constants![field.Name];
                    entityReport.Assumptions[field.Name] = new Assumption("Constante declarada en el perfil", entityReport.Assumptions.GetValueOrDefault(field.Name)?.Rows + 1 ?? 1);
                }
                if (!string.IsNullOrWhiteSpace(text)) supplied.Add(field.Name);
                if (string.IsNullOrWhiteSpace(text))
                {
                    if (field.DefaultValue is not null)
                    {
                        text = field.DefaultValue;
                        entityReport.CountDefault(field);
                    }
                    else if (field.Required)
                    {
                        report.Error(entity, rowNumber, field.Name, "required", $"'{field.Name}' es obligatorio y está vacío.");
                        valid = false;
                        continue;
                    }
                    else
                    {
                        if (field.MissingNote is not null) entityReport.CountDefault(field);
                        values[field.Name] = null;
                        continue;
                    }
                }
                if (field.IsCode) text = text.Trim().ToUpperInvariant();
                var cellSettings = !constantUsed && source is not null && table.NumericCells?[i].Contains(source) == true
                    ? settings with { DecimalSeparator = "." } : settings;
                if (field.Type is FieldType.Decimal or FieldType.Integer && cellSettings.DecimalSeparator is null && (text.Contains(',') || text.Contains('.')))
                {
                    var key = $"{field.Name}_decimal";
                    entityReport.Assumptions[key] = new Assumption("Separador decimal inferido; declárelo en el perfil si el texto representa miles", entityReport.Assumptions.GetValueOrDefault(key)?.Rows + 1 ?? 1);
                }
                if (!ValueParser.TryParse(text, field.Type, cellSettings, out var value))
                {
                    report.Error(entity, rowNumber, field.Name, "invalid_value", $"'{text}' no es un valor válido ({field.Type}) para '{field.Name}'.");
                    valid = false;
                    continue;
                }
                try
                {
                    if (mapping?.Scale?.TryGetValue(field.Name, out var factor) == true)
                    {
                        var key = $"{field.Name}_scale";
                        entityReport.Assumptions[key] = new Assumption($"Conversión por factor {factor.ToString(System.Globalization.CultureInfo.InvariantCulture)}", entityReport.Assumptions.GetValueOrDefault(key)?.Rows + 1 ?? 1);
                        if (value is decimal number) value = checked(number * factor);
                        else if (value is int integer && integer * factor == decimal.Truncate(integer * factor)) value = checked((int)(integer * factor));
                        else throw new OverflowException();
                    }
                    if (value is decimal quantity && Math.Abs(quantity) > ImportLimits.MaximumQuantity) throw new OverflowException();
                }
                catch (OverflowException)
                {
                    report.Error(entity, rowNumber, field.Name, "out_of_range", "La conversión supera el rango o produce un entero fraccionario; corrija quantity/scale.");
                    valid = false;
                }
                if (value is string word && word.Length > (field.IsCode ? 64 : 256))
                {
                    report.Error(entity, rowNumber, field.Name, "too_long", "El texto supera la longitud permitida; reduzca el valor.");
                    valid = false;
                }
                values[field.Name] = value;
            }
            if (valid) valid = RowRules.Check(entity, rowNumber, values, report);
            if (valid) rows.Add(new ParsedRow(rowNumber, values, supplied));
            else entityReport.Skipped++;
        }
        return rows;
    }
}

internal static class RowRules
{
    private static readonly string[] NonNegative =
        ["quantity", "max_weight", "max_volume", "weight_per_unit", "volume_per_unit", "cost_per_km", "lead_time_days"];

    public static bool Check(ImportEntity entity, int row, Dictionary<string, object?> values, ImportReport report)
    {
        var ok = true;
        if (values.ContainsKey("lat"))
        {
            var lat = values.GetValueOrDefault("lat") as decimal?;
            var lon = values.GetValueOrDefault("lon") as decimal?;
            if (lat is < -90m or > 90m) { report.Error(entity, row, "lat", "out_of_range", $"Latitud {lat} fuera de [-90, 90]."); ok = false; }
            if (lon is < -180m or > 180m) { report.Error(entity, row, "lon", "out_of_range", $"Longitud {lon} fuera de [-180, 180]."); ok = false; }
            if (ok && (lat is null) != (lon is null))
            {
                report.Error(entity, row, "lat", "incomplete_coordinates", "Informe lat y lon juntas, o deje ambas vacías.");
                ok = false;
            }
        }
        foreach (var field in NonNegative)
        {
            if (values.GetValueOrDefault(field) is not decimal value || value >= 0) continue;
            report.Error(entity, row, field, "negative", $"'{field}' no puede ser negativo ({value}).");
            ok = false;
        }
        if (values.GetValueOrDefault("max_weight") is decimal weight && weight == 0)
        {
            report.Error(entity, row, "max_weight", "zero_capacity", "La capacidad del vehículo no puede ser 0.");
            ok = false;
        }
        if (values.GetValueOrDefault("max_volume") is decimal volume && volume == 0)
        { report.Error(entity, row, "max_volume", "zero_capacity", "Informe un volumen positivo o deje max_volume vacío."); ok = false; }
        if (values.GetValueOrDefault("period_days") is int days && days < 1)
        {
            report.Error(entity, row, "period_days", "out_of_range", "period_days debe ser al menos 1.");
            ok = false;
        }
        if (values.GetValueOrDefault("window_start") is TimeOnly start && values.GetValueOrDefault("window_end") is TimeOnly end && end <= start)
        {
            report.Error(entity, row, "window_end", "invalid_window", "window_end debe ser posterior a window_start; corrija la ventana.");
            ok = false;
        }
        if ((values.GetValueOrDefault("window_start") is null) != (values.GetValueOrDefault("window_end") is null))
        { report.Error(entity, row, "window_start", "incomplete_window", "Informe ambos extremos de la ventana o deje ambos vacíos."); ok = false; }
        if (values.GetValueOrDefault("service_minutes") is int minutes && minutes < 0)
        { report.Error(entity, row, "service_minutes", "negative", "service_minutes no puede ser negativo."); ok = false; }
        return ok;
    }
}
