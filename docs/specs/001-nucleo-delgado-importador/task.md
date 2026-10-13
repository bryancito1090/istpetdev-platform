# 001 — Núcleo delgado e importador genérico: tareas

> Pasos ejecutables del [`plan.md`](plan.md). Cada tarea termina en commit y deja la solución compilando. TDD: prueba que falla → implementación mínima → prueba que pasa → commit. Todos los comandos se ejecutan desde la raíz del repositorio.

## Resumen

| ID | Tarea | Depende de | Paralelizable | Estado |
|---|---|---|---|---|
| T0 | Preparación: aprobación, rama, tag de archivo, entorno | — | — | pendiente |
| T1 | Proyecto de pruebas y primitivas del importador | T0 | [P] con T4 | pendiente |
| T2 | Perfil de mapeo, resolución de columnas, parser de filas y reporte | T1 | [P] con T3, T4 | pendiente |
| T3 | Lectores CSV y XLSX | T1 | [P] con T2, T4 | pendiente |
| T4 | Esquema delgado de 27 tablas y migración `DemoCore` | T0 | [P] con T1–T3 | pendiente |
| T5 | `ImportService`, escritor canónico y fixtures | T2, T3, T4 | — | pendiente |
| T6 | Roles fijos, cuentas de demo y autorización | T4, T5 | — | pendiente |
| T7 | Endpoints de importación y reset de demo; empaquetado | T5, T6 | — | pendiente |
| T8 | Pruebas E2E del recorrido de datos | T7 | — | pendiente |
| T9 | Registro en la bóveda (ADR-21 y contradicciones) | T0 | [P] | pendiente |

T1–T3 tocan `src/Application/Imports`, `src/Infrastructure/Imports` y `tests/`; T4 toca `src/Domain`, `src/Infrastructure/Persistence`, `src/Api` y CI. Si se ejecutan en paralelo, T4 añade `tests/IstpetDev.Tests/Support/` y T1 crea el `.csproj`: quien termine segundo rebasa sobre el primero.

---

### Task 0: Preparación

**Files:** ninguno (operaciones de Git y entorno).

**Interfaces:**
- Consumes: —
- Produces: rama `feature/001-nucleo-delgado-importador`; tag `schema95-archivo` sobre el `develop` previo al cambio.

- [ ] **Step 1: Confirmar la aprobación del spec**

Abrir `docs/specs/001-nucleo-delgado-importador/spec.md` y comprobar que `aprobado_por` tiene nombres y fecha, y que PA-01 está resuelta. Si no, **detenerse**: archivar Schema95 sin acuerdo del equipo contradice ADR-16.

- [ ] **Step 2: Crear la rama desde `develop` actualizado**

```bash
git fetch origin
git switch develop
git pull --ff-only
git switch -c feature/001-nucleo-delgado-importador
```

- [ ] **Step 3: Etiquetar el estado de Schema95 antes de borrarlo**

```bash
git tag -a schema95-archivo -m "Último develop con Schema95 (95 tablas, RLS, RBAC administrable) antes del spec 001" develop
git push origin schema95-archivo   # publica el tag; confirmar con el equipo antes de ejecutarlo
```

Expected: `git show schema95-archivo --stat | head -3` muestra el commit de `develop`.

- [ ] **Step 4: Comprobar SDK, herramientas y Docker**

```bash
dotnet --list-sdks | grep 8.0.424 || curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version 8.0.424
dotnet tool restore
dotnet ef --version
docker info --format '{{.ServerVersion}}'
```

Expected: SDK `8.0.424` listado, `Entity Framework Core .NET Command-line Tools 8.0.11` y una versión de Docker.

---

### Task 1: Proyecto de pruebas y primitivas del importador

**Files:**
- Create: `tests/IstpetDev.Tests/IstpetDev.Tests.csproj`
- Create: `src/Application/Imports/HeaderNormalizer.cs`
- Create: `src/Application/Imports/ImportEntity.cs`
- Create: `src/Application/Imports/CanonicalSchema.cs`
- Create: `src/Application/Imports/ValueParser.cs`
- Test: `tests/IstpetDev.Tests/Unit/Imports/PrimitivesTests.cs`
- Modify: `IstpetDev.sln` (añadir el proyecto de pruebas)

**Interfaces:**
- Consumes: —
- Produces:
  - `HeaderNormalizer.Normalize(string? header) : string`
  - `enum ImportEntity { Warehouses, ServicePoints, Products, Vehicles, Lots, Stock, Consumptions }`
  - `ImportEntityKeys.Key(this ImportEntity) : string`, `ImportEntityKeys.TryParse(string?, out ImportEntity) : bool`, `ImportEntityKeys.All : IReadOnlyCollection<string>`
  - `enum FieldType { Text, Decimal, Integer, Date, Time }`
  - `record CanonicalField(string Name, FieldType Type, bool Required, string[] Synonyms, string? DefaultValue = null, string? MissingNote = null, bool IsCode = false)` con `Candidates() : IEnumerable<string>`
  - `CanonicalSchema.Order : ImportEntity[]`, `CanonicalSchema.Fields : IReadOnlyDictionary<ImportEntity, IReadOnlyList<CanonicalField>>`
  - `record ParseSettings(string? DecimalSeparator, IReadOnlyList<string> DateFormats)` con `ParseSettings.DefaultDateFormats`
  - `ValueParser.TryParse(string text, FieldType type, ParseSettings settings, out object? value) : bool`

- [ ] **Step 1: Crear el proyecto de pruebas**

`tests/IstpetDev.Tests/IstpetDev.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.11" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="3.10.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Api\IstpetDev.Api.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

```bash
dotnet sln IstpetDev.sln add tests/IstpetDev.Tests/IstpetDev.Tests.csproj --solution-folder tests
```

- [ ] **Step 2: Escribir las pruebas que fallan**

`tests/IstpetDev.Tests/Unit/Imports/PrimitivesTests.cs`:

```csharp
using System.Globalization;
using IstpetDev.Application.Imports;

namespace IstpetDev.Tests.Unit.Imports;

public class PrimitivesTests
{
    private static readonly ParseSettings Auto = new(null, ParseSettings.DefaultDateFormats);

    [Theory]
    [InlineData("Cód. Cliente", "cod_cliente")]
    [InlineData("  CÓDIGO  ", "codigo")]
    [InlineData("Razón Social", "razon_social")]
    [InlineData("lead-time (días)", "lead_time_dias")]
    [InlineData("Año__Mes", "ano_mes")]
    [InlineData("", "")]
    public void Normalize_strips_accents_case_and_symbols(string header, string expected)
        => Assert.Equal(expected, HeaderNormalizer.Normalize(header));

    [Theory]
    [InlineData("service_points", ImportEntity.ServicePoints)]
    [InlineData("Puntos", ImportEntity.ServicePoints)]
    [InlineData("inventario", ImportEntity.Stock)]
    [InlineData("Vehículos", ImportEntity.Vehicles)]
    public void Entity_keys_accept_canonical_names_and_spanish_aliases(string text, ImportEntity expected)
    {
        Assert.True(ImportEntityKeys.TryParse(text, out var entity));
        Assert.Equal(expected, entity);
    }

    [Fact]
    public void Entity_keys_reject_unknown_names() => Assert.False(ImportEntityKeys.TryParse("pedidos", out _));

    [Fact]
    public void Every_entity_has_required_fields()
    {
        foreach (var entity in CanonicalSchema.Order)
            Assert.Contains(CanonicalSchema.Fields[entity], field => field.Required);
    }

    [Fact]
    public void Header_candidates_do_not_collide_within_an_entity()
    {
        foreach (var entity in CanonicalSchema.Order)
        {
            var candidates = CanonicalSchema.Fields[entity].SelectMany(f => f.Candidates().Distinct()).ToList();
            Assert.Equal(candidates.Count, candidates.Distinct().Count());
        }
    }

    [Theory]
    [InlineData("12.5", "12.5")]
    [InlineData("12,5", "12.5")]
    [InlineData("1.234,56", "1234.56")]
    [InlineData("1,234.56", "1234.56")]
    [InlineData("1.234.567", "1234567")]
    [InlineData("1,234,567", "1234567")]
    [InlineData("-3", "-3")]
    [InlineData("1.2E+3", "1200")]
    public void Decimal_infers_the_separator(string text, string expected)
    {
        Assert.True(ValueParser.TryParse(text, FieldType.Decimal, Auto, out var value));
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), value);
    }

    [Fact]
    public void Decimal_respects_a_declared_separator()
    {
        var comma = new ParseSettings(",", ParseSettings.DefaultDateFormats);
        Assert.True(ValueParser.TryParse("1.250", FieldType.Decimal, comma, out var value));
        Assert.Equal(1250m, value);
    }

    [Theory]
    [InlineData("2026-10-05", 2026, 10, 5)]
    [InlineData("05/10/2026", 2026, 10, 5)]
    [InlineData("2026-09", 2026, 9, 1)]
    [InlineData("46300", 2026, 10, 5)]
    public void Date_accepts_common_formats_and_excel_serials(string text, int year, int month, int day)
    {
        Assert.True(ValueParser.TryParse(text, FieldType.Date, Auto, out var value));
        Assert.Equal(new DateOnly(year, month, day), value);
    }

    [Theory]
    [InlineData("07:00", 7, 0)]
    [InlineData("7:30", 7, 30)]
    [InlineData("0.5", 12, 0)]
    public void Time_accepts_clock_text_and_excel_fractions(string text, int hour, int minute)
    {
        Assert.True(ValueParser.TryParse(text, FieldType.Time, Auto, out var value));
        Assert.Equal(new TimeOnly(hour, minute), value);
    }

    [Theory]
    [InlineData("abc", FieldType.Decimal)]
    [InlineData("15.5", FieldType.Integer)]
    [InlineData("31/02/2026", FieldType.Date)]
    [InlineData("25:00", FieldType.Time)]
    public void Invalid_values_are_rejected(string text, FieldType type)
        => Assert.False(ValueParser.TryParse(text, type, Auto, out _));
}
```

- [ ] **Step 3: Ejecutar las pruebas y verificar que fallan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~PrimitivesTests"`
Expected: FAIL de compilación: `The type or namespace name 'Imports' does not exist in the namespace 'IstpetDev.Application'`.

- [ ] **Step 4: Implementar `HeaderNormalizer`**

`src/Application/Imports/HeaderNormalizer.cs`:

```csharp
using System.Text;

namespace IstpetDev.Application.Imports;

// Normaliza encabezados y alias sin depender de ICU: el contenedor corre con globalización invariante.
public static class HeaderNormalizer
{
    private const string Accented = "áàäâãéèëêíìïîóòöôõúùüûñçÁÀÄÂÃÉÈËÊÍÌÏÎÓÒÖÔÕÚÙÜÛÑÇ";
    private const string Plain = "aaaaaeeeeiiiiooooouuuuncaaaaaeeeeiiiiooooouuuunc";

    public static string Normalize(string? header)
    {
        if (string.IsNullOrWhiteSpace(header)) return string.Empty;
        var builder = new StringBuilder(header.Length);
        foreach (var raw in header.Trim())
        {
            var index = Accented.IndexOf(raw);
            var c = char.ToLowerInvariant(index >= 0 ? Plain[index] : raw);
            builder.Append(c is >= 'a' and <= 'z' or >= '0' and <= '9' ? c : '_');
        }
        var collapsed = builder.ToString();
        while (collapsed.Contains("__")) collapsed = collapsed.Replace("__", "_");
        return collapsed.Trim('_');
    }
}
```

- [ ] **Step 5: Implementar `ImportEntity`**

`src/Application/Imports/ImportEntity.cs`:

```csharp
namespace IstpetDev.Application.Imports;

// El orden del enum es el orden de escritura: cada entidad solo referencia entidades anteriores.
public enum ImportEntity { Warehouses, ServicePoints, Products, Vehicles, Lots, Stock, Consumptions }

public static class ImportEntityKeys
{
    private static readonly Dictionary<ImportEntity, string> Keys = new()
    {
        [ImportEntity.Warehouses] = "warehouses",
        [ImportEntity.ServicePoints] = "service_points",
        [ImportEntity.Products] = "products",
        [ImportEntity.Vehicles] = "vehicles",
        [ImportEntity.Lots] = "lots",
        [ImportEntity.Stock] = "stock",
        [ImportEntity.Consumptions] = "consumptions",
    };

    private static readonly Dictionary<string, ImportEntity> Aliases = new()
    {
        ["bodegas"] = ImportEntity.Warehouses, ["almacenes"] = ImportEntity.Warehouses,
        ["puntos"] = ImportEntity.ServicePoints, ["clientes"] = ImportEntity.ServicePoints, ["puntos_de_servicio"] = ImportEntity.ServicePoints,
        ["productos"] = ImportEntity.Products, ["catalogo"] = ImportEntity.Products,
        ["vehiculos"] = ImportEntity.Vehicles, ["flota"] = ImportEntity.Vehicles,
        ["lotes"] = ImportEntity.Lots,
        ["inventario"] = ImportEntity.Stock, ["saldos"] = ImportEntity.Stock,
        ["consumos"] = ImportEntity.Consumptions, ["demanda"] = ImportEntity.Consumptions,
    };

    public static IReadOnlyCollection<string> All => Keys.Values;

    public static string Key(this ImportEntity entity) => Keys[entity];

    public static bool TryParse(string? text, out ImportEntity entity)
    {
        var normalized = HeaderNormalizer.Normalize(text);
        foreach (var (candidate, key) in Keys)
        {
            if (key != normalized) continue;
            entity = candidate;
            return true;
        }
        return Aliases.TryGetValue(normalized, out entity);
    }
}
```

- [ ] **Step 6: Implementar `CanonicalSchema`**

`src/Application/Imports/CanonicalSchema.cs`:

```csharp
namespace IstpetDev.Application.Imports;

public enum FieldType { Text, Decimal, Integer, Date, Time }

// DefaultValue se aplica cuando falta el dato; MissingNote explica el supuesto en el reporte (guardarraíl G3).
public sealed record CanonicalField(
    string Name, FieldType Type, bool Required, string[] Synonyms,
    string? DefaultValue = null, string? MissingNote = null, bool IsCode = false)
{
    public IEnumerable<string> Candidates() => Synonyms.Prepend(Name).Select(HeaderNormalizer.Normalize);
}

public static class CanonicalSchema
{
    public static readonly ImportEntity[] Order = Enum.GetValues<ImportEntity>();

    private static readonly string[] Lat = ["latitud", "latitude", "y"];
    private static readonly string[] Lon = ["longitud", "longitude", "lng", "long", "x"];
    private static readonly string[] Sku = ["codigo_producto", "producto", "cod_producto"];

    private static CanonicalField Code(string name, params string[] synonyms) => new(name, FieldType.Text, true, synonyms, IsCode: true);

    public static readonly IReadOnlyDictionary<ImportEntity, IReadOnlyList<CanonicalField>> Fields =
        new Dictionary<ImportEntity, IReadOnlyList<CanonicalField>>
        {
            [ImportEntity.Warehouses] =
            [
                Code("code", "codigo", "cod", "id", "bodega", "codigo_bodega"),
                new("name", FieldType.Text, true, ["nombre", "descripcion"]),
                new("lat", FieldType.Decimal, true, Lat),
                new("lon", FieldType.Decimal, true, Lon),
            ],
            [ImportEntity.ServicePoints] =
            [
                Code("code", "codigo", "cod", "id", "punto", "codigo_punto", "cod_cliente", "codigo_cliente", "cliente_id"),
                new("name", FieldType.Text, true, ["nombre", "cliente", "razon_social", "punto_nombre"]),
                new("lat", FieldType.Decimal, false, Lat, MissingNote: "sin coordenadas: el punto no será ruteable"),
                new("lon", FieldType.Decimal, false, Lon),
                new("region", FieldType.Text, false, ["provincia", "ciudad", "zona"]),
                new("category", FieldType.Text, false, ["tipo", "categoria", "segmento"]),
                new("criticality", FieldType.Text, false, ["criticidad", "prioridad"], DefaultValue: "normal"),
                new("window_start", FieldType.Time, false, ["hora_inicio", "apertura", "desde"], MissingNote: "se usará la ventana global por defecto"),
                new("window_end", FieldType.Time, false, ["hora_fin", "cierre", "hasta"]),
                new("service_minutes", FieldType.Integer, false, ["tiempo_servicio", "minutos_servicio"], MissingNote: "se usará el tiempo de servicio global"),
                new("lead_time_days", FieldType.Decimal, false, ["lead_time", "tiempo_reposicion", "dias_entrega"], MissingNote: "se usará el lead time global"),
            ],
            [ImportEntity.Products] =
            [
                Code("sku", "codigo", "cod_producto", "producto_id", "item", "codigo_producto"),
                new("name", FieldType.Text, true, ["nombre", "descripcion", "producto"]),
                new("base_unit", FieldType.Text, true, ["unidad", "um", "unidad_medida", "unidad_base"]),
                new("weight_per_unit", FieldType.Decimal, false, ["peso", "peso_unitario", "kg"], DefaultValue: "0", MissingNote: "capacidad por peso no evaluable"),
                new("volume_per_unit", FieldType.Decimal, false, ["volumen", "volumen_unitario", "m3"], DefaultValue: "0", MissingNote: "capacidad por volumen no evaluable"),
            ],
            [ImportEntity.Vehicles] =
            [
                Code("code", "codigo", "placa", "vehiculo"),
                Code("depot_code", "bodega", "deposito", "base", "codigo_bodega"),
                new("max_weight", FieldType.Decimal, true, ["capacidad_kg", "capacidad", "carga_maxima", "peso_maximo"]),
                new("max_volume", FieldType.Decimal, false, ["capacidad_m3", "volumen_maximo"], MissingNote: "sin límite de volumen"),
                new("cost_per_km", FieldType.Decimal, false, ["costo_km", "costo_por_km"], MissingNote: "se usará el costo por km global"),
            ],
            [ImportEntity.Lots] =
            [
                Code("sku", Sku),
                Code("lot_code", "lote", "codigo_lote"),
                new("expires_on", FieldType.Date, false, ["vencimiento", "fecha_vencimiento", "caducidad"]),
                new("origin", FieldType.Text, false, ["origen", "proveedor"]),
            ],
            [ImportEntity.Stock] =
            [
                new("location_kind", FieldType.Text, false, ["tipo_ubicacion", "tipo"]),
                Code("location_code", "ubicacion", "codigo_ubicacion", "bodega", "punto"),
                Code("sku", Sku),
                new("lot_code", FieldType.Text, false, ["lote", "codigo_lote"], MissingNote: "se usa el lote GENERICO-{sku}", IsCode: true),
                new("quantity", FieldType.Decimal, true, ["cantidad", "stock", "existencia", "saldo"]),
            ],
            [ImportEntity.Consumptions] =
            [
                Code("point_code", "punto", "codigo_punto", "cliente", "cod_cliente"),
                Code("sku", Sku),
                new("date", FieldType.Date, true, ["fecha", "dia", "mes", "periodo"]),
                new("quantity", FieldType.Decimal, true, ["cantidad", "consumo", "unidades"]),
                new("period_days", FieldType.Integer, false, ["dias_periodo", "periodo_dias"], DefaultValue: "1"),
            ],
        };
}
```

- [ ] **Step 7: Implementar `ValueParser`**

`src/Application/Imports/ValueParser.cs`:

```csharp
using System.Globalization;

namespace IstpetDev.Application.Imports;

public sealed record ParseSettings(string? DecimalSeparator, IReadOnlyList<string> DateFormats)
{
    public static readonly IReadOnlyList<string> DefaultDateFormats =
        ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy/MM/dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM"];
}

public static class ValueParser
{
    private static readonly string[] TimeFormats = ["HH:mm", "H:mm", "HH:mm:ss", "H:mm:ss"];

    public static bool TryParse(string text, FieldType type, ParseSettings settings, out object? value)
    {
        value = null;
        switch (type)
        {
            case FieldType.Text:
                value = text.Trim();
                return true;
            case FieldType.Decimal when TryDecimal(text, settings.DecimalSeparator, out var number):
                value = number;
                return true;
            case FieldType.Integer when TryDecimal(text, settings.DecimalSeparator, out var whole)
                                        && whole == decimal.Truncate(whole) && whole >= int.MinValue && whole <= int.MaxValue:
                value = (int)whole;
                return true;
            case FieldType.Date when TryDate(text, settings, out var date):
                value = date;
                return true;
            case FieldType.Time when TryTime(text, out var time):
                value = time;
                return true;
            default:
                return false;
        }
    }

    public static bool TryDecimal(string text, string? decimalSeparator, out decimal value)
    {
        var cleaned = text.Trim().Replace(" ", "").Replace(" ", "");
        var separator = decimalSeparator ?? InferDecimalSeparator(cleaned);
        cleaned = cleaned.Replace(separator == "," ? "." : ",", "");
        if (separator == ",") cleaned = cleaned.Replace(',', '.');
        return decimal.TryParse(cleaned, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture, out value);
    }

    // Un único separador se toma como decimal (convención es-EC); varios del mismo tipo son miles.
    private static string InferDecimalSeparator(string text)
    {
        int comma = text.LastIndexOf(','), dot = text.LastIndexOf('.');
        if (comma >= 0 && dot >= 0) return comma > dot ? "," : ".";
        if (comma >= 0) return text.Count(c => c == ',') == 1 ? "," : ".";
        if (dot >= 0 && text.Count(c => c == '.') > 1) return ",";
        return ".";
    }

    private static bool TryDate(string text, ParseSettings settings, out DateOnly date)
    {
        var trimmed = text.Trim();
        if (DateTime.TryParseExact(trimmed, settings.DateFormats.ToArray(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            date = DateOnly.FromDateTime(parsed);
            return true;
        }
        // Serial de fecha de Excel (días desde 1899-12-30), acotado a 1954–2119.
        if (double.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var serial) && serial >= 20000 && serial <= 80000)
        {
            date = DateOnly.FromDateTime(DateTime.FromOADate(Math.Floor(serial)));
            return true;
        }
        date = default;
        return false;
    }

    private static bool TryTime(string text, out TimeOnly time)
    {
        var trimmed = text.Trim();
        if (TimeOnly.TryParseExact(trimmed, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out time)) return true;
        // Fracción de día de Excel (0.5 = 12:00).
        if (double.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fraction) && fraction >= 0 && fraction < 1)
        {
            time = TimeOnly.FromTimeSpan(TimeSpan.FromDays(fraction));
            return true;
        }
        return false;
    }
}
```

- [ ] **Step 8: Ejecutar las pruebas y verificar que pasan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~PrimitivesTests"`
Expected: PASS (todas las pruebas de `PrimitivesTests`).

- [ ] **Step 9: Commit**

```bash
git add IstpetDev.sln tests/IstpetDev.Tests src/Application/Imports
git commit -m "feat(importador): esquema canónico, normalizador y parser de valores"
```

---

### Task 2: Perfil de mapeo, resolución de columnas, parser de filas y reporte

**Files:**
- Create: `src/Application/Imports/TabularData.cs`
- Create: `src/Application/Imports/MappingProfile.cs`
- Create: `src/Application/Imports/ColumnResolver.cs`
- Create: `src/Application/Imports/ImportReport.cs`
- Create: `src/Application/Imports/RowParser.cs`
- Test: `tests/IstpetDev.Tests/Unit/Imports/MappingAndParsingTests.cs`

**Interfaces:**
- Consumes: todo lo producido por Task 1.
- Produces:
  - `record TabularData(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyDictionary<string, string>> Rows, bool InvariantNumbers = false)` con `static string[] UniqueHeaders(IEnumerable<string?>)`
  - `interface ITabularReader { TabularData Read(Stream content, string fileName, char? delimiter, string? sheet); }`
  - `class UnsupportedFileException(string message) : Exception`
  - `JsonDefaults.Options : JsonSerializerOptions`
  - `class FileMapping { string? File; string? Sheet; Dictionary<string,string>? Columns; Dictionary<string,string>? Constants; Dictionary<string,decimal>? Scale }`
  - `class MappingProfile { string Name; char? Delimiter; string? DecimalSeparator; string[]? DateFormats; Dictionary<string, FileMapping> Files; static Auto(); For(ImportEntity) : FileMapping?; EntityForFile(string) : ImportEntity?; ToParseSettings() : ParseSettings; Validate() : IReadOnlyList<string>; ToJson() : string; static Parse(string json) : MappingProfile }`
  - `class MappingProfileException(IReadOnlyList<string> errors)` con `Errors`
  - `record ColumnResolution(Dictionary<string,string> Columns, List<string> MissingRequired, List<string> Ignored)`; `ColumnResolver.Resolve(IReadOnlyList<string> headers, ImportEntity entity, FileMapping? mapping) : ColumnResolution`
  - `enum IssueSeverity { Warning, Error }`, `record ImportIssue(string Entity, int Row, string? Field, IssueSeverity Severity, string Code, string Message)`, `record Assumption(string Note, int Rows)`
  - `class EntityImportReport` (Entity, File, RowsRead, Created, Updated, Skipped, ResolvedColumns, IgnoredColumns, Assumptions, `CountDefault(CanonicalField)`)
  - `class ImportReport(Guid batchId, string profile, bool dryRun)` con `MaxIssues = 500`, `Applied`, `ErrorCount`, `WarningCount`, `OmittedIssues`, `HasErrors`, `Entities`, `Issues`, `Entity(ImportEntity)`, `Error(...)`, `Warn(...)`, `ToJson()`
  - `record ParsedRow(int Row, IReadOnlyDictionary<string, object?> Values)` con `Text/Decimal/Int/Date/Time(string field)`
  - `RowParser.Parse(TabularData table, ImportEntity entity, ColumnResolution resolution, FileMapping? mapping, ParseSettings settings, ImportReport report) : List<ParsedRow>`

- [ ] **Step 1: Escribir las pruebas que fallan**

`tests/IstpetDev.Tests/Unit/Imports/MappingAndParsingTests.cs`:

```csharp
using IstpetDev.Application.Imports;

namespace IstpetDev.Tests.Unit.Imports;

public class MappingAndParsingTests
{
    private static TabularData Table(params string[] lines)
    {
        var headers = lines[0].Split(',');
        var rows = lines.Skip(1)
            .Select(line => (IReadOnlyDictionary<string, string>)headers.Zip(line.Split(',')).ToDictionary(p => p.First, p => p.Second))
            .ToList();
        return new TabularData(headers, rows);
    }

    private static (List<ParsedRow> Rows, ImportReport Report) Parse(ImportEntity entity, TabularData table, FileMapping? mapping = null)
    {
        var report = new ImportReport(Guid.NewGuid(), "prueba", dryRun: true);
        var resolution = ColumnResolver.Resolve(table.Headers, entity, mapping);
        var rows = RowParser.Parse(table, entity, resolution, mapping, MappingProfile.Auto().ToParseSettings(), report);
        return (rows, report);
    }

    [Fact]
    public void Profile_with_unknown_entity_or_field_is_rejected_with_all_errors()
    {
        var ex = Assert.Throws<MappingProfileException>(() => MappingProfile.Parse("""
            { "files": { "pedidos": { "file": "p.csv" }, "puntos": { "columns": { "latitude": "Lat" } } } }
            """));
        Assert.Equal(2, ex.Errors.Count);
        Assert.Contains(ex.Errors, e => e.Contains("pedidos"));
        Assert.Contains(ex.Errors, e => e.Contains("puntos.latitude"));
    }

    [Fact]
    public void Profile_finds_entities_by_alias_and_file_name()
    {
        var profile = MappingProfile.Parse("""{ "name": "x", "files": { "puntos": { "file": "Clientes.CSV" } } }""");
        Assert.NotNull(profile.For(ImportEntity.ServicePoints));
        Assert.Equal(ImportEntity.ServicePoints, profile.EntityForFile("clientes.csv"));
        Assert.Null(profile.EntityForFile("otro.csv"));
    }

    [Fact]
    public void Profile_date_formats_come_before_defaults()
    {
        var settings = MappingProfile.Parse("""{ "dateFormats": ["MM/dd/yyyy"] }""").ToParseSettings();
        Assert.Equal("MM/dd/yyyy", settings.DateFormats[0]);
        Assert.Contains("yyyy-MM-dd", settings.DateFormats);
    }

    [Fact]
    public void Resolver_matches_synonyms_and_reports_ignored_and_missing_columns()
    {
        var resolution = ColumnResolver.Resolve(["Código", "Razón Social", "Latitud", "Notas"], ImportEntity.ServicePoints, null);
        Assert.Equal("Código", resolution.Columns["code"]);
        Assert.Equal("Razón Social", resolution.Columns["name"]);
        Assert.Equal("Latitud", resolution.Columns["lat"]);
        Assert.Equal(new[] { "Notas" }, resolution.Ignored);
        Assert.Empty(resolution.MissingRequired);

        var missing = ColumnResolver.Resolve(["Nombre"], ImportEntity.ServicePoints, null);
        Assert.Equal(new[] { "code" }, missing.MissingRequired);
    }

    [Fact]
    public void Resolver_uses_declared_columns_and_constants_satisfy_required_fields()
    {
        var mapping = new FileMapping
        {
            Columns = new() { ["sku"] = "Cód. Producto", ["name"] = "Descripción" },
            Constants = new() { ["base_unit"] = "L" },
        };
        var resolution = ColumnResolver.Resolve(["Cod Producto", "Descripción"], ImportEntity.Products, mapping);
        Assert.Equal("Cod Producto", resolution.Columns["sku"]);
        Assert.Empty(resolution.MissingRequired);
    }

    [Fact]
    public void Parser_applies_defaults_constants_scale_and_code_normalization()
    {
        var mapping = new FileMapping { Constants = new() { ["period_days"] = "30" }, Scale = new() { ["quantity"] = 3.78541m } };
        var (rows, report) = Parse(ImportEntity.Consumptions, Table("point_code,sku,date,quantity", " hos-001 ,des-01,2026-09,95"), mapping);
        var row = Assert.Single(rows);
        Assert.Equal("HOS-001", row.Text("point_code"));
        Assert.Equal("DES-01", row.Text("sku"));
        Assert.Equal(95m * 3.78541m, row.Decimal("quantity"));
        Assert.Equal(30, row.Int("period_days"));
        Assert.False(report.HasErrors);
    }

    [Fact]
    public void Parser_counts_assumptions_for_missing_optional_values()
    {
        var (rows, report) = Parse(ImportEntity.ServicePoints, Table("code,name", "P1,Uno", "P2,Dos"));
        Assert.Equal(2, rows.Count);
        var assumptions = report.Entity(ImportEntity.ServicePoints).Assumptions;
        Assert.Equal(2, assumptions["criticality"].Rows);
        Assert.Equal(2, assumptions["lat"].Rows);
        Assert.Equal("normal", rows[0].Text("criticality"));
    }

    [Fact]
    public void Parser_reports_row_numbers_and_skips_invalid_rows()
    {
        var (rows, report) = Parse(ImportEntity.Warehouses, Table("code,name,lat,lon", "B1,Bodega,-0.2,-78.5", "B2,,-0.2,-78.5", "B3,Mala,-95,-78.5"));
        Assert.Single(rows);
        Assert.Equal(2, report.ErrorCount);
        Assert.Contains(report.Issues, i => i.Row == 3 && i.Field == "name" && i.Code == "required");
        Assert.Contains(report.Issues, i => i.Row == 4 && i.Field == "lat" && i.Code == "out_of_range");
        Assert.Equal(2, report.Entity(ImportEntity.Warehouses).Skipped);
    }

    [Fact]
    public void Parser_drops_incomplete_coordinates_with_a_warning()
    {
        var (rows, report) = Parse(ImportEntity.ServicePoints, Table("code,name,lat,lon", "P1,Uno,-0.2,"));
        Assert.Null(Assert.Single(rows).Decimal("lat"));
        Assert.Equal(1, report.WarningCount);
        Assert.False(report.HasErrors);
    }

    [Fact]
    public void Report_keeps_counting_after_the_issue_cap()
    {
        var report = new ImportReport(Guid.NewGuid(), "prueba", dryRun: false);
        for (var i = 0; i < 1200; i++) report.Error(ImportEntity.Stock, i + 2, "quantity", "negative", "negativo");
        Assert.Equal(ImportReport.MaxIssues, report.Issues.Count);
        Assert.Equal(1200, report.ErrorCount);
        Assert.Equal(700, report.OmittedIssues);
        Assert.True(report.HasErrors);
    }

    [Fact]
    public void Unique_headers_name_blanks_and_suffix_duplicates()
        => Assert.Equal(new[] { "a", "columna_2", "a_2" }, TabularData.UniqueHeaders(new[] { "a", " ", "a" }));
}
```

- [ ] **Step 2: Ejecutar las pruebas y verificar que fallan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~MappingAndParsingTests"`
Expected: FAIL de compilación: `The type or namespace name 'TabularData' could not be found`.

- [ ] **Step 3: Implementar `TabularData`**

`src/Application/Imports/TabularData.cs`:

```csharp
namespace IstpetDev.Application.Imports;

// InvariantNumbers = true cuando los números vienen tipados (XLSX): no se reinterpretan con el separador del perfil.
public sealed record TabularData(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Rows,
    bool InvariantNumbers = false)
{
    public static string[] UniqueHeaders(IEnumerable<string?> raw)
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        return raw.Select((header, index) =>
        {
            var name = string.IsNullOrWhiteSpace(header) ? $"columna_{index + 1}" : header.Trim();
            if (!seen.TryAdd(name, 1)) name = $"{name}_{++seen[name]}";
            return name;
        }).ToArray();
    }
}

public interface ITabularReader
{
    TabularData Read(Stream content, string fileName, char? delimiter, string? sheet);
}

public sealed class UnsupportedFileException(string message) : Exception(message);
```

- [ ] **Step 4: Implementar `MappingProfile`**

`src/Application/Imports/MappingProfile.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IstpetDev.Application.Imports;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
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
        foreach (var (key, file) in Files)
        {
            if (!ImportEntityKeys.TryParse(key, out var entity))
            {
                errors.Add($"Entidad desconocida '{key}'. Válidas: {string.Join(", ", ImportEntityKeys.All)}.");
                continue;
            }
            var valid = CanonicalSchema.Fields[entity].Select(f => f.Name).ToHashSet();
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
        try { profile = JsonSerializer.Deserialize<MappingProfile>(json, JsonDefaults.Options); }
        catch (JsonException ex) { throw new MappingProfileException([$"JSON inválido: {ex.Message}"]); }
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
```

- [ ] **Step 5: Implementar `ColumnResolver`**

`src/Application/Imports/ColumnResolver.cs`:

```csharp
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
                source = field.Candidates().Select(c => byNormalized.GetValueOrDefault(c)).FirstOrDefault(h => h is not null && !used.Contains(h));

            if (source is not null && used.Add(source)) columns[field.Name] = source;
            else if (field.Required && mapping?.Constants?.ContainsKey(field.Name) != true) missing.Add(field.Name);
        }
        return new ColumnResolution(columns, missing, headers.Where(h => !used.Contains(h)).ToList());
    }
}
```

- [ ] **Step 6: Implementar `ImportReport`**

`src/Application/Imports/ImportReport.cs`:

```csharp
using System.Text.Json;

namespace IstpetDev.Application.Imports;

public enum IssueSeverity { Warning, Error }

public sealed record ImportIssue(string Entity, int Row, string? Field, IssueSeverity Severity, string Code, string Message);

public sealed record Assumption(string Note, int Rows);

public sealed class EntityImportReport
{
    public required string Entity { get; init; }
    public string? File { get; set; }
    public int RowsRead { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public Dictionary<string, string> ResolvedColumns { get; set; } = new();
    public List<string> IgnoredColumns { get; set; } = new();
    public Dictionary<string, Assumption> Assumptions { get; } = new();

    public void CountDefault(CanonicalField field)
    {
        var note = field.DefaultValue is null
            ? field.MissingNote!
            : $"vacío → '{field.DefaultValue}'" + (field.MissingNote is null ? "" : $" ({field.MissingNote})");
        Assumptions[field.Name] = new Assumption(note, Assumptions.TryGetValue(field.Name, out var previous) ? previous.Rows + 1 : 1);
    }
}

public sealed class ImportReport(Guid batchId, string profile, bool dryRun)
{
    public const int MaxIssues = 500;

    public Guid BatchId { get; } = batchId;
    public string Profile { get; } = profile;
    public bool DryRun { get; } = dryRun;
    public bool Applied { get; set; }
    public int ErrorCount { get; private set; }
    public int WarningCount { get; private set; }
    public int OmittedIssues { get; private set; }
    public bool HasErrors => ErrorCount > 0;
    public List<EntityImportReport> Entities { get; } = new();
    public List<ImportIssue> Issues { get; } = new();

    public EntityImportReport Entity(ImportEntity entity)
    {
        var key = entity.Key();
        var report = Entities.FirstOrDefault(e => e.Entity == key);
        if (report is null) Entities.Add(report = new EntityImportReport { Entity = key });
        return report;
    }

    public void Error(ImportEntity entity, int row, string? field, string code, string message)
    {
        ErrorCount++;
        Add(new ImportIssue(entity.Key(), row, field, IssueSeverity.Error, code, message));
    }

    public void Warn(ImportEntity entity, int row, string? field, string code, string message)
    {
        WarningCount++;
        Add(new ImportIssue(entity.Key(), row, field, IssueSeverity.Warning, code, message));
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonDefaults.Options);

    private void Add(ImportIssue issue)
    {
        if (Issues.Count < MaxIssues) Issues.Add(issue);
        else OmittedIssues++;
    }
}
```

- [ ] **Step 7: Implementar `RowParser`**

`src/Application/Imports/RowParser.cs`:

```csharp
namespace IstpetDev.Application.Imports;

public sealed record ParsedRow(int Row, IReadOnlyDictionary<string, object?> Values)
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
            var rowNumber = i + FirstDataRow;
            var values = new Dictionary<string, object?>();
            var valid = true;
            foreach (var field in CanonicalSchema.Fields[entity])
            {
                var text = resolution.Columns.TryGetValue(field.Name, out var source) ? table.Rows[i].GetValueOrDefault(source) : null;
                if (string.IsNullOrWhiteSpace(text) && mapping?.Constants?.TryGetValue(field.Name, out var constant) == true) text = constant;
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
                if (!ValueParser.TryParse(text, field.Type, settings, out var value))
                {
                    report.Error(entity, rowNumber, field.Name, "invalid_value", $"'{text}' no es un valor válido ({field.Type}) para '{field.Name}'.");
                    valid = false;
                    continue;
                }
                if (value is decimal number && mapping?.Scale?.TryGetValue(field.Name, out var factor) == true) value = number * factor;
                values[field.Name] = value;
            }
            if (valid) valid = RowRules.Check(entity, rowNumber, values, report);
            if (valid) rows.Add(new ParsedRow(rowNumber, values));
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
                report.Warn(entity, row, "lat", "incomplete_coordinates", "Solo hay una coordenada; el punto queda sin ubicación.");
                values["lat"] = values["lon"] = null;
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
        if (values.GetValueOrDefault("period_days") is int days && days < 1)
        {
            report.Error(entity, row, "period_days", "out_of_range", "period_days debe ser al menos 1.");
            ok = false;
        }
        if (values.GetValueOrDefault("window_start") is TimeOnly start && values.GetValueOrDefault("window_end") is TimeOnly end && end <= start)
        {
            report.Warn(entity, row, "window_end", "invalid_window", $"La ventana {start}–{end} no es válida; se usará la ventana global.");
            values["window_start"] = values["window_end"] = null;
        }
        return ok;
    }
}
```

- [ ] **Step 8: Ejecutar las pruebas y verificar que pasan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~Unit.Imports"`
Expected: PASS (`PrimitivesTests` y `MappingAndParsingTests`).

- [ ] **Step 9: Commit**

```bash
git add src/Application/Imports tests/IstpetDev.Tests/Unit
git commit -m "feat(importador): perfil de mapeo, resolución de columnas, parser de filas y reporte"
```

---

### Task 3: Lectores CSV y XLSX

**Files:**
- Modify: `src/Infrastructure/IstpetDev.Infrastructure.csproj` (paquetes)
- Create: `src/Infrastructure/Imports/CsvTabularReader.cs`
- Create: `src/Infrastructure/Imports/XlsxTabularReader.cs`
- Create: `src/Infrastructure/Imports/TabularReader.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs` (registrar `ITabularReader`)
- Test: `tests/IstpetDev.Tests/Unit/Imports/TabularReaderTests.cs`

**Interfaces:**
- Consumes: `TabularData`, `ITabularReader`, `UnsupportedFileException` (Task 2).
- Produces: `TabularReader : ITabularReader` (singleton en DI); `CsvTabularReader.Read(Stream, char?)`, `CsvTabularReader.Decode(byte[])`, `CsvTabularReader.DetectDelimiter(string)`; `XlsxTabularReader.Read(Stream, string?)`.

- [ ] **Step 1: Añadir paquetes**

```bash
dotnet add src/Infrastructure package CsvHelper --version 33.0.1
dotnet add src/Infrastructure package ClosedXML --version 0.104.1
```

- [ ] **Step 2: Escribir las pruebas que fallan**

`tests/IstpetDev.Tests/Unit/Imports/TabularReaderTests.cs`:

```csharp
using System.Text;
using ClosedXML.Excel;
using IstpetDev.Application.Imports;
using IstpetDev.Infrastructure.Imports;

namespace IstpetDev.Tests.Unit.Imports;

public class TabularReaderTests
{
    private static TabularData ReadCsv(byte[] bytes, char? delimiter = null) =>
        new TabularReader().Read(new MemoryStream(bytes), "datos.csv", delimiter, null);

    [Fact]
    public void Csv_detects_comma_and_keeps_quoted_commas()
    {
        var table = ReadCsv(Encoding.UTF8.GetBytes("code,name\nP1,\"Clínica, Sur\"\n\nP2,Norte\n"));
        Assert.Equal(new[] { "code", "name" }, table.Headers);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Clínica, Sur", table.Rows[0]["name"]);
        Assert.False(table.InvariantNumbers);
    }

    [Fact]
    public void Csv_detects_semicolon_and_pads_short_rows()
    {
        var table = ReadCsv(Encoding.UTF8.GetBytes("a;b;c\n1;2\n"));
        Assert.Equal("", table.Rows[0]["c"]);
    }

    [Fact]
    public void Csv_reads_latin1_files_exported_by_excel()
    {
        var table = ReadCsv(Encoding.Latin1.GetBytes("Código;Descripción\nDES-01;Desinfectante\n"));
        Assert.Equal(new[] { "Código", "Descripción" }, table.Headers);
    }

    [Fact]
    public void Csv_strips_utf8_bom()
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("sku,name\nA,B\n")).ToArray();
        Assert.Equal("sku", ReadCsv(bytes).Headers[0]);
    }

    [Fact]
    public void Xlsx_reads_typed_cells_in_invariant_format()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Productos");
        sheet.Cell(1, 1).Value = "sku";
        sheet.Cell(1, 2).Value = "peso";
        sheet.Cell(1, 3).Value = "vence";
        sheet.Cell(1, 4).Value = "apertura";
        sheet.Cell(2, 1).Value = "DES-01";
        sheet.Cell(2, 2).Value = 0.45;
        sheet.Cell(2, 3).Value = new DateTime(2026, 10, 5);
        sheet.Cell(2, 4).Value = TimeSpan.FromHours(7.5);
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var table = new TabularReader().Read(stream, "catalogo.xlsx", null, "Productos");

        Assert.True(table.InvariantNumbers);
        Assert.Equal("0.45", table.Rows[0]["peso"]);
        Assert.Equal("2026-10-05", table.Rows[0]["vence"]);
        Assert.Equal("07:30", table.Rows[0]["apertura"]);
    }

    [Fact]
    public void Unsupported_extensions_are_rejected_with_guidance()
    {
        var ex = Assert.Throws<UnsupportedFileException>(() => new TabularReader().Read(new MemoryStream(), "datos.pdf", null, null));
        Assert.Contains(".csv o .xlsx", ex.Message);
    }
}
```

- [ ] **Step 3: Ejecutar las pruebas y verificar que fallan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~TabularReaderTests"`
Expected: FAIL de compilación: `The type or namespace name 'TabularReader' could not be found`.

- [ ] **Step 4: Implementar el lector CSV**

`src/Infrastructure/Imports/CsvTabularReader.cs`:

```csharp
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using IstpetDev.Application.Imports;

namespace IstpetDev.Infrastructure.Imports;

public sealed class CsvTabularReader
{
    private static readonly char[] CandidateDelimiters = [';', ',', '\t', '|'];

    public TabularData Read(Stream content, char? delimiter)
    {
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        var text = Decode(buffer.ToArray());
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = (delimiter ?? DetectDelimiter(text)).ToString(),
            HasHeaderRecord = false,
            BadDataFound = null,
            MissingFieldFound = null,
            DetectColumnCountChanges = false,
            TrimOptions = TrimOptions.Trim,
        };
        using var parser = new CsvParser(new StringReader(text), configuration);
        string[]? headers = null;
        var rows = new List<IReadOnlyDictionary<string, string>>();
        while (parser.Read())
        {
            var record = parser.Record ?? [];
            if (headers is null)
            {
                headers = TabularData.UniqueHeaders(record);
                continue;
            }
            if (record.All(string.IsNullOrWhiteSpace)) continue;
            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < headers.Length; i++) row[headers[i]] = i < record.Length ? record[i] : string.Empty;
            rows.Add(row);
        }
        return new TabularData(headers ?? [], rows);
    }

    // Excel en Windows exporta CSV en Windows-1252; si el texto no es UTF-8 válido se lee como Latin-1.
    public static string Decode(byte[] bytes)
    {
        try { return new UTF8Encoding(false, true).GetString(bytes).TrimStart('﻿'); }
        catch (DecoderFallbackException) { return Encoding.Latin1.GetString(bytes); }
    }

    public static char DetectDelimiter(string text)
    {
        var firstLine = text.Split('\n', 2)[0];
        return CandidateDelimiters.OrderByDescending(c => firstLine.Count(x => x == c)).First();
    }
}
```

- [ ] **Step 5: Implementar el lector XLSX**

`src/Infrastructure/Imports/XlsxTabularReader.cs`:

```csharp
using System.Globalization;
using ClosedXML.Excel;
using IstpetDev.Application.Imports;

namespace IstpetDev.Infrastructure.Imports;

public sealed class XlsxTabularReader
{
    public TabularData Read(Stream content, string? sheet)
    {
        using var workbook = new XLWorkbook(content);
        var worksheet = sheet is null ? workbook.Worksheets.First() : workbook.Worksheet(sheet);
        var range = worksheet.RangeUsed();
        if (range is null) return new TabularData([], [], InvariantNumbers: true);

        var width = range.ColumnCount();
        var rows = range.RowsUsed().ToList();
        var headers = TabularData.UniqueHeaders(rows[0].Cells(1, width).Select(c => c.GetString()));
        var data = new List<IReadOnlyDictionary<string, string>>();
        foreach (var row in rows.Skip(1))
        {
            var values = row.Cells(1, width).Select(CellText).ToArray();
            if (values.All(string.IsNullOrWhiteSpace)) continue;
            data.Add(headers.Select((header, i) => (header, value: values[i])).ToDictionary(p => p.header, p => p.value));
        }
        return new TabularData(headers, data, InvariantNumbers: true);
    }

    private static string CellText(IXLCell cell)
    {
        var value = cell.Value;
        if (value.IsDateTime)
        {
            // Excel guarda las horas como fracción de día con formato de fecha: al leerlas llegan como 1899-12-30 hh:mm.
            var moment = value.GetDateTime();
            if (moment.Date <= new DateTime(1900, 1, 1)) return moment.ToString("HH:mm", CultureInfo.InvariantCulture);
            return moment.TimeOfDay == TimeSpan.Zero
                ? moment.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : moment.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
        if (value.IsTimeSpan) return value.GetTimeSpan().ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        if (value.IsNumber) return value.GetNumber().ToString(CultureInfo.InvariantCulture);
        return cell.GetString();
    }
}
```

- [ ] **Step 6: Implementar el lector compuesto y registrarlo**

`src/Infrastructure/Imports/TabularReader.cs`:

```csharp
using IstpetDev.Application.Imports;

namespace IstpetDev.Infrastructure.Imports;

public sealed class TabularReader : ITabularReader
{
    public TabularData Read(Stream content, string fileName, char? delimiter, string? sheet) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".xlsx" or ".xlsm" => new XlsxTabularReader().Read(content, sheet),
            ".tsv" => new CsvTabularReader().Read(content, delimiter ?? '\t'),
            ".csv" or ".txt" or "" => new CsvTabularReader().Read(content, delimiter),
            var other => throw new UnsupportedFileException($"Formato '{other}' no soportado. Guarde el archivo como .csv o .xlsx."),
        };
}
```

En `src/Infrastructure/DependencyInjection.cs`, añadir `using IstpetDev.Application.Imports;` y `using IstpetDev.Infrastructure.Imports;`, y antes de `services.AddHealthChecks()`:

```csharp
        services.AddSingleton<ITabularReader, TabularReader>();
```

- [ ] **Step 7: Ejecutar las pruebas y verificar que pasan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~Unit.Imports"`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add src/Infrastructure tests/IstpetDev.Tests/Unit
git commit -m "feat(importador): lectores CSV (UTF-8/Latin-1, separador detectado) y XLSX"
```

---

### Task 4: Esquema delgado de 27 tablas y migración `DemoCore`

**Files:**
- Delete: `src/Domain/Entities/ConfigurationEntities.cs`, `src/Domain/Entities/MessagingEntities.cs`, `src/Infrastructure/Persistence/CompleteSchema.cs`, `src/Infrastructure/Persistence/Migrations/` (todo), `src/Api/Authentication/OrganizationContextFilter.cs`, `tools/` (todo), `scripts/db/schema95-check.sql`, `scripts/db/schema95-integrity-check.sql`, `scripts/db/check-schema95-runtime.py`
- Create: `src/Domain/Common/DemoOrganization.cs`, `src/Domain/Common/LocationKinds.cs`, `src/Domain/Common/MovementKinds.cs`, `src/Domain/Entities/OperationsEntities.cs`
- Modify (reemplazo completo): `src/Domain/Entities/{IdentityEntities,CatalogEntities,InventoryEntities,LogisticsEntities,RiskEntities,SimulationEntities}.cs`
- Create: `src/Infrastructure/Persistence/LeanSchema.cs`, `src/Infrastructure/Persistence/DemoGrants.cs`, `scripts/db/reset-to-demo-core.sql`
- Modify (reemplazo completo): `src/Infrastructure/Persistence/ApplicationDbContext.cs`, `src/Application/Common/Interfaces/IApplicationDbContext.cs`, `src/Application/ServicePoints/Services/ServicePointsService.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`, `src/Infrastructure/IstpetDev.Infrastructure.csproj`, `src/Api/Program.cs`, `src/Api/Controllers/ServicePointsController.cs`, `.github/workflows/deploy.yml`, `scripts/db/migrate-personal.sh`
- Create (generado): `src/Infrastructure/Persistence/Migrations/<timestamp>_DemoCore.cs`, `.Designer.cs`, `ApplicationDbContextModelSnapshot.cs`
- Test: `tests/IstpetDev.Tests/Support/PostgisContainer.cs`, `tests/IstpetDev.Tests/Support/TestDb.cs`, `tests/IstpetDev.Tests/Integration/Persistence/LeanSchemaTests.cs`

**Interfaces:**
- Consumes: —
- Produces:
  - Entidades del núcleo (ver código), `DemoOrganization.Id`, `LocationKinds.{Warehouse, ServicePoint, Vehicle, All}`, `MovementKinds.ImportAdjustment`
  - `ApplicationDbContext` con 27 `DbSet` (nombres en plural: `Organizations`, `Accounts`, `ServicePoints`, `Locations`, `Vehicles`, `Products`, `Lots`, `HandlingUnits`, `HandlingUnitLines`, `InventoryMovements`, `StockBalances`, `Consumptions`, `RoutePlans`, `RouteStops`, `Deliveries`, `DeliveryLines`, `Receipts`, `ReceiptLines`, `Evidences`, `CustodyEvents`, `RiskSnapshots`, `Incidents`, `IdempotencyRecords`, `ImportBatches`, `AgentDecisions`, `SimulationRuns`, `SimulationMetrics`)
  - `IApplicationDbContext` con esos `DbSet`, `Database`, `ChangeTracker` y `SaveChangesAsync`
  - `DemoGrants.Sql` (constante SQL para conceder permisos a los roles de aplicación de RDS; toda migración futura que cree tablas termina con `migrationBuilder.Sql(DemoGrants.Sql)`)
  - Pruebas: `PostgisContainer.NewDatabase() : string`, `PostgisCollection.Name`, `TestDb.CreateMigratedAsync(string) : Task<ApplicationDbContext>`

- [ ] **Step 1: Eliminar Schema95 y sus verificaciones**

```bash
git rm -r -q src/Infrastructure/Persistence/Migrations src/Infrastructure/Persistence/CompleteSchema.cs \
  src/Domain/Entities/ConfigurationEntities.cs src/Domain/Entities/MessagingEntities.cs \
  src/Api/Authentication/OrganizationContextFilter.cs tools \
  scripts/db/schema95-check.sql scripts/db/schema95-integrity-check.sql scripts/db/check-schema95-runtime.py
```

En `src/Infrastructure/IstpetDev.Infrastructure.csproj`, borrar la línea:

```xml
    <EmbeddedResource Include="Persistence/Migrations/Sql/Schema95*.sql" />
```

(y su `<ItemGroup>` si queda vacío).

- [ ] **Step 2: Escribir la infraestructura de pruebas y las pruebas que fallan**

`tests/IstpetDev.Tests/Support/PostgisContainer.cs`:

```csharp
using Npgsql;
using Testcontainers.PostgreSql;

namespace IstpetDev.Tests.Support;

// Un contenedor por ejecución; cada prueba crea su propia base para no compartir estado.
public sealed class PostgisContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder().WithImage("postgis/postgis:16-3.4-alpine").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public string NewDatabase() =>
        new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = $"t_{Guid.NewGuid():N}" }.ConnectionString;
}

[CollectionDefinition(Name)]
public sealed class PostgisCollection : ICollectionFixture<PostgisContainer>
{
    public const string Name = "postgis";
}
```

`tests/IstpetDev.Tests/Support/TestDb.cs`:

```csharp
using IstpetDev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IstpetDev.Tests.Support;

public static class TestDb
{
    public static async Task<ApplicationDbContext> CreateMigratedAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
        // La migración crea postgis; sin recargar tipos, Npgsql no sabe escribir geometry en esta fuente de datos.
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await connection.OpenAsync();
        connection.ReloadTypes();
        await connection.CloseAsync();
        return db;
    }
}
```

`tests/IstpetDev.Tests/Integration/Persistence/LeanSchemaTests.cs`:

```csharp
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace IstpetDev.Tests.Integration.Persistence;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class LeanSchemaTests(PostgisContainer postgres)
{
    private static readonly string[] CoreTables =
    [
        "Organization", "Account", "ServicePoint", "Location", "Vehicle", "Product", "Lot", "HandlingUnit", "HandlingUnitLine",
        "InventoryMovement", "StockBalance", "Consumption", "RoutePlan", "RouteStop", "Delivery", "DeliveryLine", "Receipt",
        "ReceiptLine", "Evidence", "CustodyEvent", "RiskSnapshot", "Incident", "IdempotencyRecord", "ImportBatch",
        "AgentDecision", "SimulationRun", "SimulationMetric",
    ];

    // Tablas de Schema95 que el núcleo delgado elimina; ninguna debe volver por una migración posterior.
    private static readonly string[] RemovedSchema95Tables =
    [
        "Permission", "Role", "RolePermission", "RoleFieldPermission", "OrganizationAccess", "AccessRole", "AccessDelegation",
        "ConfigurationDefinition", "ConfigurationVersion", "Template", "TemplateVersion", "WorkflowDefinition", "WorkflowVersion",
        "FormSubmission", "Conversation", "Message", "Notification", "OutboxEvent", "AuditEvent", "SyncBatch",
    ];

    // Los specs 002–004 añaden sus tablas con migraciones propias: la prueba exige el núcleo, no un conteo cerrado.
    [Fact]
    public async Task Migration_creates_the_core_tables_without_schema95_and_the_demo_organization()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        var tables = await db.Database.SqlQueryRaw<string>(
            "SELECT table_name AS \"Value\" FROM information_schema.tables " +
            "WHERE table_schema = 'public' AND table_type = 'BASE TABLE' " +
            "AND table_name NOT IN ('__EFMigrationsHistory', 'spatial_ref_sys')").ToListAsync();

        Assert.Empty(CoreTables.Except(tables));
        Assert.Empty(RemovedSchema95Tables.Intersect(tables));
        Assert.True(await db.Organizations.AnyAsync(o => o.Id == DemoOrganization.Id));
    }

    [Fact]
    public async Task Service_point_position_round_trips_and_organization_defaults_to_demo()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        db.ServicePoints.Add(new ServicePoint { Code = "P-1", Name = "Punto", Criticality = "normal", Position = new Point(-78.5, -0.2) { SRID = 4326 }, IsActive = true });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var point = await db.ServicePoints.SingleAsync();
        Assert.Equal(DemoOrganization.Id, point.OrganizationId);
        Assert.Equal(-0.2, point.Position!.Y, 6);
        Assert.Equal(-78.5, point.Position.X, 6);
        Assert.Equal(1, point.Version);
    }

    [Fact]
    public async Task Stock_balance_cannot_be_negative()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        var product = new Product { Sku = "S", Name = "S", BaseUnit = "L", IsActive = true };
        var lot = new Lot { ProductId = product.Id, Code = "L1" };
        var location = new Location { Code = "B", Name = "B", Kind = LocationKinds.Warehouse, IsActive = true };
        db.AddRange(product, lot, location, new StockBalance { LocationId = location.Id, LotId = lot.Id, QuantityOnHand = -1 });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
```

- [ ] **Step 3: Ejecutar las pruebas y verificar que fallan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~LeanSchemaTests"`
Expected: FAIL de compilación (por ejemplo, `The name 'CompleteSchema' does not exist in the current context` en `ApplicationDbContext.cs`, y `DemoOrganization` no encontrado).

- [ ] **Step 4: Escribir las constantes de dominio**

`src/Domain/Common/DemoOrganization.cs`:

```csharp
namespace IstpetDev.Domain.Common;

// Organización única de la demo (spec 001). OrganizationId se conserva para la versión multiempresa.
public static class DemoOrganization
{
    public static readonly Guid Id = Guid.Parse("01927a00-0000-7000-8000-000000000001");
}
```

`src/Domain/Common/LocationKinds.cs`:

```csharp
namespace IstpetDev.Domain.Common;

public static class LocationKinds
{
    public const string Warehouse = "Warehouse";
    public const string ServicePoint = "ServicePoint";
    public const string Vehicle = "Vehicle";

    public static readonly IReadOnlyList<string> All = [Warehouse, ServicePoint, Vehicle];
}
```

`src/Domain/Common/MovementKinds.cs`:

```csharp
namespace IstpetDev.Domain.Common;

public static class MovementKinds
{
    public const string ImportAdjustment = "ImportAdjustment";
}
```

- [ ] **Step 5: Reemplazar las entidades**

`src/Domain/Entities/IdentityEntities.cs`:

```csharp
using IstpetDev.Domain.Common;

namespace IstpetDev.Domain.Entities;

public sealed class Organization : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

// Identidad externa (issuer, sub). Las cuentas técnicas de Cognito (client_credentials) usan sub = client_id.
public sealed class Account : BaseEntity
{
    public string Issuer { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
```

`src/Domain/Entities/CatalogEntities.cs`:

```csharp
using IstpetDev.Domain.Common;
using NetTopologySuite.Geometries;

namespace IstpetDev.Domain.Entities;

public sealed class ServicePoint : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Region { get; set; }
    public string Criticality { get; set; } = string.Empty;
    public Point? Position { get; set; }
    public TimeOnly? WindowStartLocal { get; set; }
    public TimeOnly? WindowEndLocal { get; set; }
    public int? ServiceMinutes { get; set; }
    public decimal? LeadTimeDays { get; set; }
    public bool IsActive { get; set; }
}

// Bodega, punto de servicio o vehículo: todo lugar que puede tener saldo.
public sealed class Location : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public Guid? ServicePointId { get; set; }
    public Guid? VehicleId { get; set; }
    public Point? Position { get; set; }
    public bool IsActive { get; set; }
}

public sealed class Vehicle : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid DepotLocationId { get; set; }
    public decimal MaxWeight { get; set; }
    public decimal? MaxVolume { get; set; }
    public decimal? CostPerKm { get; set; }
    public bool IsActive { get; set; }
}

public sealed class Product : OrganizationEntity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = string.Empty;
    public decimal WeightPerBaseUnit { get; set; }
    public decimal VolumePerBaseUnit { get; set; }
    public bool IsActive { get; set; }
}

public sealed class Lot : OrganizationEntity
{
    public Guid ProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Origin { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public bool IsBlocked { get; set; }
}

public sealed class HandlingUnit : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SealedAtUtc { get; set; }
}

public sealed class HandlingUnitLine : OrganizationEntity
{
    public Guid HandlingUnitId { get; set; }
    public Guid LotId { get; set; }
    public decimal Quantity { get; set; }
}
```

`src/Domain/Entities/InventoryEntities.cs`:

```csharp
using IstpetDev.Domain.Common;

namespace IstpetDev.Domain.Entities;

public sealed class InventoryMovement : OrganizationEntity
{
    public string Kind { get; set; } = string.Empty;
    public Guid LotId { get; set; }
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? ActorAccountId { get; set; }
    public Guid OperationId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid? DeliveryLineId { get; set; }
    public Guid? ReceiptLineId { get; set; }
    public Guid? ImportBatchId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class StockBalance : OrganizationEntity
{
    public Guid LocationId { get; set; }
    public Guid LotId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
}

// Consumo de un punto y SKU; PeriodDays permite cargar totales semanales o mensuales sin desagregarlos.
public sealed class Consumption : OrganizationEntity
{
    public Guid ServicePointId { get; set; }
    public Guid ProductId { get; set; }
    public DateOnly OccurredOn { get; set; }
    public decimal Quantity { get; set; }
    public int PeriodDays { get; set; } = 1;
    public string SourceKind { get; set; } = string.Empty;
    public Guid? ImportBatchId { get; set; }
}
```

`src/Domain/Entities/LogisticsEntities.cs`:

```csharp
using IstpetDev.Domain.Common;

namespace IstpetDev.Domain.Entities;

public sealed class RoutePlan : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public int Revision { get; set; }
    public Guid? SupersedesRoutePlanId { get; set; }
    public Guid VehicleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ProposedBy { get; set; } = string.Empty;
    public DateTime PlannedStartUtc { get; set; }
    public DateTime? PlannedEndUtc { get; set; }
    public decimal? TotalKm { get; set; }
    public decimal? TotalMinutes { get; set; }
    public decimal? TotalCost { get; set; }
    public string AlgorithmVersion { get; set; } = string.Empty;
    public string InputJson { get; set; } = "{}";
    public Guid? PublishedByAccountId { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}

public sealed class RouteStop : OrganizationEntity
{
    public Guid RoutePlanId { get; set; }
    public Guid ServicePointId { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime? PlannedArrivalUtc { get; set; }
    public DateTime? PlannedDepartureUtc { get; set; }
    public DateTime? ActualArrivalUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PendingReason { get; set; }
}

public sealed class Delivery : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid? RouteStopId { get; set; }
    public Guid OriginLocationId { get; set; }
    public Guid ServicePointId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? AssignedAccountId { get; set; }
    public DateTime? DispatchedAtUtc { get; set; }
}

public sealed class DeliveryLine : OrganizationEntity
{
    public Guid DeliveryId { get; set; }
    public Guid LotId { get; set; }
    public Guid? HandlingUnitId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal DispatchedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
}

public sealed class Receipt : OrganizationEntity
{
    public Guid DeliveryId { get; set; }
    public Guid ActorAccountId { get; set; }
    public Guid OperationId { get; set; }
    public DateTime? OccurredAtDevice { get; set; }
    public DateTime ReceivedAtServer { get; set; }
}

public sealed class ReceiptLine : OrganizationEntity
{
    public Guid ReceiptId { get; set; }
    public Guid DeliveryLineId { get; set; }
    public decimal QuantityAccepted { get; set; }
    public decimal QuantityRejected { get; set; }
    public string? RejectionReason { get; set; }
}

public sealed class Evidence : OrganizationEntity
{
    public Guid ReceiptId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string? Sha256 { get; set; }
    public Guid UploadedByAccountId { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
}

public sealed class CustodyEvent : OrganizationEntity
{
    public Guid HandlingUnitId { get; set; }
    public Guid? DeliveryId { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public Guid ActorAccountId { get; set; }
    public string EventKind { get; set; } = string.Empty;
    public Guid OperationId { get; set; }
    public DateTime? OccurredAtDevice { get; set; }
    public DateTime ReceivedAtServer { get; set; }
}
```

`src/Domain/Entities/RiskEntities.cs`:

```csharp
using IstpetDev.Domain.Common;
using NetTopologySuite.Geometries;

namespace IstpetDev.Domain.Entities;

public sealed class RiskSnapshot : OrganizationEntity
{
    public Guid ServicePointId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
    public string FormulaVersion { get; set; } = string.Empty;
    public decimal AvailableQuantity { get; set; }
    public decimal? DailyDemand { get; set; }
    public decimal? CoverageDays { get; set; }
    public decimal LeadTimeDays { get; set; }
    public decimal SafetyMarginDays { get; set; }
    public decimal SuggestedQuantity { get; set; }
    public string RiskCode { get; set; } = string.Empty;
    public string InputSnapshotJson { get; set; } = "{}";
    public string? UncertaintyJson { get; set; }
}

public sealed class Incident : OrganizationEntity
{
    public string Kind { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ServicePointId { get; set; }
    public Guid? RoutePlanId { get; set; }
    public Point? Position { get; set; }
    public decimal? RadiusKm { get; set; }
    public bool IsSynthetic { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ReportedByAccountId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
```

`src/Domain/Entities/OperationsEntities.cs`:

```csharp
using IstpetDev.Domain.Common;

namespace IstpetDev.Domain.Entities;

public sealed class IdempotencyRecord : OrganizationEntity
{
    public Guid AccountId { get; set; }
    public Guid OperationId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public int? ResponseStatus { get; set; }
    public string? ResponseJson { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class ImportBatch : OrganizationEntity
{
    public string ProfileName { get; set; } = string.Empty;
    public bool DryRun { get; set; }
    public string Status { get; set; } = string.Empty;
    public string InputHash { get; set; } = string.Empty;
    public string ProfileJson { get; set; } = "{}";
    public string ReportJson { get; set; } = "{}";
    public Guid? CreatedByAccountId { get; set; }
}

// Propuesta del agente logístico (spec 003). El motor de reglas de autonomía (spec 002, D5) decide si se ejecuta sola o espera aprobación.
public sealed class AgentDecision : OrganizationEntity
{
    public string Kind { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TriggerJson { get; set; } = "{}";
    public string ProposalJson { get; set; } = "{}";
    public string ToolCallsJson { get; set; } = "[]";
    public string Rationale { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? PromptVersion { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid? DecidedByAccountId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public DateTime? ExecutedAtUtc { get; set; }
    public string? FailureReason { get; set; }
}
```

`src/Domain/Entities/SimulationEntities.cs`:

```csharp
using IstpetDev.Domain.Common;

namespace IstpetDev.Domain.Entities;

public sealed class SimulationRun : OrganizationEntity
{
    public int Seed { get; set; }
    public string DatasetHash { get; set; } = string.Empty;
    public string PolicyCode { get; set; } = string.Empty;
    public string ParametersJson { get; set; } = "{}";
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class SimulationMetric : OrganizationEntity
{
    public Guid SimulationRunId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string PolicyCode { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public decimal Numerator { get; set; }
    public decimal? Denominator { get; set; }
    public decimal? Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? NotApplicableReason { get; set; }
}
```

- [ ] **Step 6: Escribir el esquema EF y los permisos para RDS**

`src/Infrastructure/Persistence/LeanSchema.cs`:

```csharp
using System.Linq.Expressions;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IstpetDev.Infrastructure.Persistence;

// Esquema de la demo (spec 001): sin RLS ni configuración dinámica; OrganizationId apunta a la organización demo.
internal static class LeanSchema
{
    private const string PointType = "geometry(Point,4326)";

    private static readonly HashSet<string> LongText =
        ["Description", "Rationale", "FailureReason", "PendingReason", "Reason", "RejectionReason"];

    public static void Configure(ModelBuilder model)
    {
        model.HasPostgresExtension("postgis");

        model.Entity<Organization>(e =>
        {
            e.HasIndex(o => o.Code).IsUnique();
            e.HasData(new Organization
            {
                Id = DemoOrganization.Id, Code = "demo", Name = "Operación demo", TimeZone = "America/Guayaquil",
                IsActive = true, CreatedAtUtc = new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc), Version = 1,
            });
        });
        model.Entity<Account>(e =>
        {
            e.Property(a => a.Issuer).HasMaxLength(512);
            e.Property(a => a.Subject).HasMaxLength(512);
            e.HasIndex(a => new { a.Issuer, a.Subject }).IsUnique();
        });

        model.Entity<ServicePoint>(e =>
        {
            e.HasIndex(p => new { p.OrganizationId, p.Code }).IsUnique();
            e.Property(p => p.Position).HasColumnType(PointType);
        });
        model.Entity<Location>(e =>
        {
            e.HasIndex(l => new { l.OrganizationId, l.Kind, l.Code }).IsUnique();
            e.Property(l => l.Position).HasColumnType(PointType);
            Fk<Location, ServicePoint>(e, l => l.ServicePointId);
            Fk<Location, Vehicle>(e, l => l.VehicleId);
        });
        model.Entity<Vehicle>(e =>
        {
            e.HasIndex(v => new { v.OrganizationId, v.Code }).IsUnique();
            Fk<Vehicle, Location>(e, v => v.DepotLocationId);
        });
        model.Entity<Product>(e => e.HasIndex(p => new { p.OrganizationId, p.Sku }).IsUnique());
        model.Entity<Lot>(e =>
        {
            e.HasIndex(l => new { l.ProductId, l.Code }).IsUnique();
            Fk<Lot, Product>(e, l => l.ProductId);
        });
        model.Entity<HandlingUnit>(e =>
        {
            e.HasIndex(h => new { h.OrganizationId, h.Code }).IsUnique();
            Fk<HandlingUnit, Location>(e, h => h.LocationId);
        });
        model.Entity<HandlingUnitLine>(e =>
        {
            Fk<HandlingUnitLine, HandlingUnit>(e, l => l.HandlingUnitId);
            Fk<HandlingUnitLine, Lot>(e, l => l.LotId);
        });

        model.Entity<InventoryMovement>(e =>
        {
            e.HasIndex(m => new { m.OrganizationId, m.OperationId });
            Fk<InventoryMovement, Lot>(e, m => m.LotId);
            Fk<InventoryMovement, Location>(e, m => m.FromLocationId);
            Fk<InventoryMovement, Location>(e, m => m.ToLocationId);
            Fk<InventoryMovement, ImportBatch>(e, m => m.ImportBatchId);
            e.ToTable(t => t.HasCheckConstraint("CK_InventoryMovement_Positive", "\"Quantity\" > 0"));
        });
        model.Entity<StockBalance>(e =>
        {
            e.HasIndex(b => new { b.LocationId, b.LotId }).IsUnique();
            Fk<StockBalance, Location>(e, b => b.LocationId);
            Fk<StockBalance, Lot>(e, b => b.LotId);
            e.ToTable(t => t.HasCheckConstraint("CK_StockBalance_NonNegative", "\"QuantityOnHand\" >= 0 AND \"QuantityReserved\" >= 0"));
        });
        model.Entity<Consumption>(e =>
        {
            e.HasIndex(c => new { c.ServicePointId, c.ProductId, c.OccurredOn }).IsUnique();
            Fk<Consumption, ServicePoint>(e, c => c.ServicePointId);
            Fk<Consumption, Product>(e, c => c.ProductId);
            Fk<Consumption, ImportBatch>(e, c => c.ImportBatchId);
            e.ToTable(t => t.HasCheckConstraint("CK_Consumption_Valid", "\"PeriodDays\" >= 1 AND \"Quantity\" >= 0"));
        });

        model.Entity<RiskSnapshot>(e =>
        {
            e.HasIndex(r => new { r.ServicePointId, r.ProductId, r.CalculatedAtUtc });
            Fk<RiskSnapshot, ServicePoint>(e, r => r.ServicePointId);
            Fk<RiskSnapshot, Product>(e, r => r.ProductId);
        });
        model.Entity<Incident>(e =>
        {
            e.Property(i => i.Position).HasColumnType(PointType);
            Fk<Incident, ServicePoint>(e, i => i.ServicePointId);
            Fk<Incident, RoutePlan>(e, i => i.RoutePlanId);
        });

        model.Entity<RoutePlan>(e =>
        {
            e.HasIndex(r => new { r.OrganizationId, r.Code, r.Revision }).IsUnique();
            Fk<RoutePlan, Vehicle>(e, r => r.VehicleId);
            Fk<RoutePlan, RoutePlan>(e, r => r.SupersedesRoutePlanId);
        });
        model.Entity<RouteStop>(e =>
        {
            e.HasIndex(s => new { s.RoutePlanId, s.SequenceNumber }).IsUnique();
            Fk<RouteStop, RoutePlan>(e, s => s.RoutePlanId);
            Fk<RouteStop, ServicePoint>(e, s => s.ServicePointId);
        });
        model.Entity<Delivery>(e =>
        {
            e.HasIndex(d => new { d.OrganizationId, d.Code }).IsUnique();
            Fk<Delivery, RouteStop>(e, d => d.RouteStopId);
            Fk<Delivery, Location>(e, d => d.OriginLocationId);
            Fk<Delivery, ServicePoint>(e, d => d.ServicePointId);
            Fk<Delivery, Account>(e, d => d.AssignedAccountId);
        });
        model.Entity<DeliveryLine>(e =>
        {
            Fk<DeliveryLine, Delivery>(e, l => l.DeliveryId);
            Fk<DeliveryLine, Lot>(e, l => l.LotId);
            Fk<DeliveryLine, HandlingUnit>(e, l => l.HandlingUnitId);
        });
        model.Entity<Receipt>(e =>
        {
            e.HasIndex(r => new { r.OrganizationId, r.OperationId }).IsUnique();
            Fk<Receipt, Delivery>(e, r => r.DeliveryId);
            Fk<Receipt, Account>(e, r => r.ActorAccountId);
        });
        model.Entity<ReceiptLine>(e =>
        {
            Fk<ReceiptLine, Receipt>(e, l => l.ReceiptId);
            Fk<ReceiptLine, DeliveryLine>(e, l => l.DeliveryLineId);
        });
        model.Entity<Evidence>(e => Fk<Evidence, Receipt>(e, x => x.ReceiptId));
        model.Entity<CustodyEvent>(e =>
        {
            e.HasIndex(c => new { c.OrganizationId, c.OperationId }).IsUnique();
            Fk<CustodyEvent, HandlingUnit>(e, c => c.HandlingUnitId);
            Fk<CustodyEvent, Account>(e, c => c.ActorAccountId);
        });

        model.Entity<IdempotencyRecord>(e => e.HasIndex(r => new { r.AccountId, r.OperationId }).IsUnique());
        model.Entity<ImportBatch>();
        model.Entity<AgentDecision>(e => e.HasIndex(d => new { d.OrganizationId, d.Status }));
        model.Entity<SimulationRun>();
        model.Entity<SimulationMetric>(e => Fk<SimulationMetric, SimulationRun>(e, m => m.SimulationRunId));

        foreach (var entity in model.Model.GetEntityTypes().ToList())
        {
            entity.SetTableName(entity.ClrType.Name);
            if (typeof(OrganizationEntity).IsAssignableFrom(entity.ClrType))
                model.Entity(entity.ClrType).HasOne(typeof(Organization)).WithMany()
                    .HasForeignKey(nameof(OrganizationEntity.OrganizationId)).OnDelete(DeleteBehavior.NoAction);
            foreach (var property in entity.GetProperties())
            {
                if (property.Name.EndsWith("Json", StringComparison.Ordinal)) { property.SetColumnType("jsonb"); property.SetMaxLength(null); }
                else if (LongText.Contains(property.Name)) { property.SetColumnType("text"); property.SetMaxLength(null); }
            }
        }
    }

    // NO ACTION (no RESTRICT): PostgreSQL valida al final de la sentencia, lo que permite borrar cadenas de revisiones en bloque.
    private static void Fk<TEntity, TPrincipal>(EntityTypeBuilder<TEntity> entity, Expression<Func<TEntity, object?>> key)
        where TEntity : class where TPrincipal : class
        => entity.HasOne<TPrincipal>().WithMany().HasForeignKey(key).OnDelete(DeleteBehavior.NoAction);
}
```

`src/Infrastructure/Persistence/DemoGrants.cs`:

```csharp
namespace IstpetDev.Infrastructure.Persistence;

// Permisos para los roles de aplicación de RDS (user_dev_0N en bases personales, user_integ_dev_0N en integración).
// No hace nada en bases sin esos roles (pruebas, Docker local). Toda migración que cree tablas termina con este SQL.
public static class DemoGrants
{
    public const string Sql = """
        DO $grants$
        DECLARE role_name text;
        BEGIN
            FOR role_name IN
                SELECT rolname FROM pg_roles
                WHERE (current_database() = 'istpetdev_integration' AND rolname ~ '^user_integ_dev_0[1-5]$')
                   OR (current_database() ~ '^istpetdev_dev_0[1-5]$' AND rolname = 'user_' || substring(current_database() from 11))
            LOOP
                EXECUTE format('GRANT USAGE ON SCHEMA public TO %I', role_name);
                EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO %I', role_name);
                EXECUTE format('REVOKE INSERT, UPDATE, DELETE ON TABLE public."__EFMigrationsHistory" FROM %I', role_name);
            END LOOP;
        END $grants$;
        """;
}
```

- [ ] **Step 7: Reemplazar el contexto y su interfaz**

`src/Infrastructure/Persistence/ApplicationDbContext.cs`:

```csharp
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ServicePoint> ServicePoints => Set<ServicePoint>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<HandlingUnit> HandlingUnits => Set<HandlingUnit>();
    public DbSet<HandlingUnitLine> HandlingUnitLines => Set<HandlingUnitLine>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<Consumption> Consumptions => Set<Consumption>();
    public DbSet<RoutePlan> RoutePlans => Set<RoutePlan>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryLine> DeliveryLines => Set<DeliveryLine>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLine> ReceiptLines => Set<ReceiptLine>();
    public DbSet<Evidence> Evidences => Set<Evidence>();
    public DbSet<CustodyEvent> CustodyEvents => Set<CustodyEvent>();
    public DbSet<RiskSnapshot> RiskSnapshots => Set<RiskSnapshot>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<AgentDecision> AgentDecisions => Set<AgentDecision>();
    public DbSet<SimulationRun> SimulationRuns => Set<SimulationRun>();
    public DbSet<SimulationMetric> SimulationMetrics => Set<SimulationMetric>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configuration)
    {
        configuration.Properties<decimal>().HavePrecision(18, 3);
        configuration.Properties<string>().HaveMaxLength(256);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) => LeanSchema.Configure(modelBuilder);

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.Version = 1;
                if (entry.Entity is OrganizationEntity { OrganizationId: var organization } scoped && organization == Guid.Empty)
                    scoped.OrganizationId = DemoOrganization.Id;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.Version++;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
```

`src/Application/Common/Interfaces/IApplicationDbContext.cs`:

```csharp
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IstpetDev.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<Account> Accounts { get; }
    DbSet<ServicePoint> ServicePoints { get; }
    DbSet<Location> Locations { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<Product> Products { get; }
    DbSet<Lot> Lots { get; }
    DbSet<HandlingUnit> HandlingUnits { get; }
    DbSet<HandlingUnitLine> HandlingUnitLines { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }
    DbSet<StockBalance> StockBalances { get; }
    DbSet<Consumption> Consumptions { get; }
    DbSet<RoutePlan> RoutePlans { get; }
    DbSet<RouteStop> RouteStops { get; }
    DbSet<Delivery> Deliveries { get; }
    DbSet<DeliveryLine> DeliveryLines { get; }
    DbSet<Receipt> Receipts { get; }
    DbSet<ReceiptLine> ReceiptLines { get; }
    DbSet<Evidence> Evidences { get; }
    DbSet<CustodyEvent> CustodyEvents { get; }
    DbSet<RiskSnapshot> RiskSnapshots { get; }
    DbSet<Incident> Incidents { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }
    DbSet<ImportBatch> ImportBatches { get; }
    DbSet<AgentDecision> AgentDecisions { get; }
    DbSet<SimulationRun> SimulationRuns { get; }
    DbSet<SimulationMetric> SimulationMetrics { get; }
    DatabaseFacade Database { get; }
    ChangeTracker ChangeTracker { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 8: Adaptar servicios, DI, controlador y `Program`**

`src/Application/ServicePoints/Services/ServicePointsService.cs` — reemplazar `GetAllAsync`:

```csharp
    public async Task<IReadOnlyList<ServicePointDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var points = await _context.ServicePoints
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new { p.Id, p.Code, p.Name, p.Category, p.Position, p.IsActive })
            .ToListAsync(cancellationToken);

        // Npgsql materializa el Point de NTS; X = longitud, Y = latitud.
        return points.Select(p => new ServicePointDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Type = p.Category ?? string.Empty,
            Latitude = p.Position?.Y,
            Longitude = p.Position?.X,
            IsActive = p.IsActive
        }).ToList();
    }
```

`src/Infrastructure/DependencyInjection.cs` — reemplazar el bloque de la cadena de conexión y `AddDbContext` por:

```csharp
        services.AddDbContext<ApplicationDbContext>((provider, options) =>
        {
            // Se lee al resolver el contexto, no al registrarlo, para que las pruebas puedan sustituirla.
            var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString("Main");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Placeholder connection string to allow process startup when DB is not yet provisioned/configured
                connectionString = "Host=localhost;Port=15432;Database=istpetdev_dev;Username=placeholder;Password=placeholder;";
            }
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.UseNetTopologySuite();
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.CommandTimeout(30);
            });
        });
```

`src/Api/Controllers/ServicePointsController.cs` — borrar `using IstpetDev.Api.Authentication;` y la línea `[ServiceFilter(typeof(OrganizationContextFilter))]`. Se mantiene `[Authorize]` (los roles llegan en Task 6).

`src/Api/Program.cs`:
- Borrar `builder.Services.AddScoped<IstpetDev.Api.Authentication.OrganizationContextFilter>();`.
- Añadir `using Npgsql;` y, justo después de `db.Database.Migrate();`:

```csharp
        // La migración puede crear la extensión postgis: recargar tipos para escribir geometry sin reiniciar.
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        connection.Open();
        connection.ReloadTypes();
        connection.Close();
```

- Al final del archivo, después de `app.Run();`:

```csharp
public partial class Program { }
```

- [ ] **Step 9: Generar la migración y añadir los permisos**

```bash
dotnet build
dotnet ef migrations add DemoCore --project src/Infrastructure --startup-project src/Api --output-dir Persistence/Migrations
```

Expected: `Done.` y tres archivos nuevos en `src/Infrastructure/Persistence/Migrations/`.

En `src/Infrastructure/Persistence/Migrations/<timestamp>_DemoCore.cs`, añadir como **última línea** de `Up(...)`:

```csharp
            migrationBuilder.Sql(DemoGrants.Sql);
```

Comprobar que el archivo generado contiene `.Annotation("Npgsql:PostgresExtension:postgis", ",,")` y 27 llamadas a `migrationBuilder.CreateTable(`:

```bash
grep -c "migrationBuilder.CreateTable(" src/Infrastructure/Persistence/Migrations/*_DemoCore.cs
```

Expected: `27`.

- [ ] **Step 10: Ejecutar las pruebas y verificar que pasan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~LeanSchemaTests"`
Expected: PASS (3 pruebas). La primera ejecución descarga `postgis/postgis:16-3.4-alpine`.

- [ ] **Step 11: Reemplazar Schema95Check en CI y en el script personal**

`.github/workflows/deploy.yml` — reemplazar el paso:

```yaml
      - name: "Verificar integridad del modelo relacional (Schema 95 & RLS)"
        run: dotnet run --project tools/Schema95Check -- "$PWD"
```

por:

```yaml
      - name: "Ejecutar pruebas (unitarias, integración y E2E)"
        run: dotnet test IstpetDev.sln -c Release --no-build --verbosity normal
```

`scripts/db/migrate-personal.sh` — borrar la línea `dotnet run --project tools/Schema95Check -- "$repo_dir"`.

```bash
grep -rn "Schema95\|tools/" .github scripts src --include=*.yml --include=*.sh --include=*.cs --include=*.csproj
```

Expected: sin resultados.

- [ ] **Step 12: Script para recrear bases que tienen Schema95**

`scripts/db/reset-to-demo-core.sql`:

```sql
-- Borra las tablas y funciones de Schema95 que pertenecen al rol actual (el migrador) para aplicar DemoCore.
-- DESTRUCTIVO. Ejecutar solo con confirmación del equipo, con el rol migrador de la base indicada:
--   psql "<cadena del migrador>" -v ON_ERROR_STOP=1 -f scripts/db/reset-to-demo-core.sql
DO $reset$
DECLARE item record;
BEGIN
    FOR item IN SELECT tablename FROM pg_tables
                WHERE schemaname = 'public' AND tableowner = current_user AND tablename <> 'spatial_ref_sys' LOOP
        EXECUTE format('DROP TABLE IF EXISTS public.%I CASCADE', item.tablename);
    END LOOP;
    FOR item IN SELECT p.oid::regprocedure AS fn FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
                WHERE n.nspname = 'public' AND p.proname LIKE 'istpetdev\_%' LOOP
        EXECUTE format('DROP FUNCTION IF EXISTS %s CASCADE', item.fn);
    END LOOP;
END $reset$;
```

No se ejecuta en esta tarea. Las bases personales y `istpetdev_integration` se recrean con este script **antes** de aplicar `DemoCore` (`scripts/db/migrate-personal.sh` para las personales; el despliegue aplica la de integración al arrancar). Pedir confirmación explícita a quien administra cada base: borra datos.

- [ ] **Step 13: Ejecutar toda la suite y hacer commit**

Run: `dotnet build && dotnet test tests/IstpetDev.Tests`
Expected: compilación sin errores; PASS de unitarias y `LeanSchemaTests`.

```bash
git add -A src tests scripts .github
git commit -m "refactor(datos): esquema delgado de 27 tablas sin RLS ni configuración dinámica (spec 001)"
```

---

### Task 5: `ImportService`, escritor canónico y fixtures

**Files:**
- Create: `src/Application/Imports/ImportContext.cs`
- Create: `src/Application/Imports/CanonicalWriter.cs`
- Create: `src/Application/Imports/ImportService.cs`
- Modify: `src/Application/DependencyInjection.cs`
- Create: `data/fixtures/demo-base/{mapping.json,bodegas.csv,puntos.csv,productos.csv,vehiculos.csv,lotes.csv,inventario.csv,consumos.csv}`
- Create: `data/fixtures/ejemplo-externo/{mapping.json,clientes.csv,catalogo.csv,consumo_mensual.csv}`
- Test: `tests/IstpetDev.Tests/Support/Fixtures.cs`, `tests/IstpetDev.Tests/Integration/Imports/ImportServiceTests.cs`

**Interfaces:**
- Consumes: Task 2 (perfil, resolver, parser, reporte), Task 3 (`TabularReader`), Task 4 (entidades, `IApplicationDbContext`, `LocationKinds`, `MovementKinds`).
- Produces:
  - `record ImportFile(ImportEntity Entity, string FileName, Stream Content)`
  - `record ImportRequest(IReadOnlyList<ImportFile> Files, MappingProfile Profile, bool DryRun, bool AllowPartial, Guid? ActorAccountId)`
  - `interface IImportService { Task<ImportReport> RunAsync(ImportRequest request, CancellationToken cancellationToken = default); }` registrado como scoped
  - Fixtures `data/fixtures/demo-base` y `data/fixtures/ejemplo-externo`, cada uno con `mapping.json`
  - Pruebas: `Fixtures.Folder(string name) : string`, `Fixtures.Request(string name, bool dryRun = false, bool allowPartial = false) : ImportRequest`

- [ ] **Step 1: Crear el fixture `demo-base`**

Coordenadas y nombres **sintéticos** (no representan clientes reales). Caso del hospital: 60 L disponibles y 84 L en 7 días = 12 L/día ([Hechos canonicos](../../boveda/00%20Inicio/Hechos%20canonicos.md)).

`data/fixtures/demo-base/mapping.json`:

```json
{
  "name": "demo-base",
  "files": {
    "warehouses": { "file": "bodegas.csv" },
    "service_points": { "file": "puntos.csv" },
    "products": { "file": "productos.csv" },
    "vehicles": { "file": "vehiculos.csv" },
    "lots": { "file": "lotes.csv" },
    "stock": { "file": "inventario.csv" },
    "consumptions": { "file": "consumos.csv" }
  }
}
```

`data/fixtures/demo-base/bodegas.csv`:

```csv
code,name,lat,lon
BOD-UIO,Bodega central Quito (sintética),-0.2295,-78.5243
```

`data/fixtures/demo-base/puntos.csv`:

```csv
code,name,lat,lon,region,category,criticality,window_start,window_end,service_minutes,lead_time_days
HOS-001,Hospital Norte (sintético),-0.1700,-78.4800,Pichincha,hospital,alta,07:00,15:00,20,2
CLI-002,Clínica Centro (sintética),-0.2100,-78.5000,Pichincha,clinica,alta,08:00,16:00,15,
OFI-003,Oficina Corporativa (sintética),-0.1900,-78.4850,Pichincha,oficina,normal,08:00,17:00,10,
ESC-004,Unidad Educativa (sintética),-0.2600,-78.5300,Pichincha,educacion,normal,07:00,13:00,10,
CEN-005,Centro Comercial (sintético),-0.1750,-78.4900,Pichincha,comercial,media,09:00,20:00,15,
HOS-006,Hospital Sur (sintético),-0.3000,-78.5500,Pichincha,hospital,alta,07:00,15:00,20,2
```

`data/fixtures/demo-base/productos.csv`:

```csv
sku,name,base_unit,weight_per_unit,volume_per_unit
DES-01,Desinfectante hospitalario,L,1.05,0.001
PAP-01,Papel higiénico industrial,UN,0.80,0.004
```

`data/fixtures/demo-base/vehiculos.csv`:

```csv
code,depot_code,max_weight,max_volume,cost_per_km
VEH-01,BOD-UIO,1200,8,0.45
```

`data/fixtures/demo-base/lotes.csv`:

```csv
sku,lot_code,expires_on,origin
DES-01,L-DES-2610,2027-04-30,Proveedor sintético A
PAP-01,L-PAP-2610,,Proveedor sintético B
```

`data/fixtures/demo-base/inventario.csv`:

```csv
location_kind,location_code,sku,lot_code,quantity
warehouse,BOD-UIO,DES-01,L-DES-2610,800
warehouse,BOD-UIO,PAP-01,L-PAP-2610,1500
service_point,HOS-001,DES-01,L-DES-2610,60
service_point,HOS-001,PAP-01,L-PAP-2610,120
service_point,CLI-002,DES-01,L-DES-2610,90
service_point,OFI-003,PAP-01,L-PAP-2610,200
service_point,ESC-004,PAP-01,L-PAP-2610,40
service_point,CEN-005,DES-01,L-DES-2610,35
service_point,HOS-006,DES-01,L-DES-2610,150
```

`data/fixtures/demo-base/consumos.csv`:

```csv
point_code,sku,date,quantity,period_days
HOS-001,DES-01,2026-10-05,84,7
HOS-001,PAP-01,2026-10-05,70,7
CLI-002,DES-01,2026-10-05,42,7
OFI-003,PAP-01,2026-10-05,35,7
ESC-004,PAP-01,2026-10-05,56,7
CEN-005,DES-01,2026-10-05,49,7
HOS-006,DES-01,2026-10-05,63,7
```

- [ ] **Step 2: Crear el fixture `ejemplo-externo`**

Simula un dataset ajeno: separador `;`, decimales con coma, encabezados con tildes, consumo mensual en galones.

`data/fixtures/ejemplo-externo/mapping.json`:

```json
{
  "name": "ejemplo-externo",
  "delimiter": ";",
  "decimalSeparator": ",",
  "dateFormats": ["yyyy-MM"],
  "files": {
    "service_points": {
      "file": "clientes.csv",
      "columns": { "code": "Cód. Cliente", "name": "Razón Social", "lat": "Latitud", "lon": "Longitud", "region": "Provincia" },
      "constants": { "category": "cliente" }
    },
    "products": {
      "file": "catalogo.csv",
      "columns": { "sku": "Código", "name": "Descripción" },
      "constants": { "base_unit": "L" }
    },
    "consumptions": {
      "file": "consumo_mensual.csv",
      "columns": { "point_code": "Cliente", "sku": "Producto", "date": "Mes", "quantity": "Galones" },
      "constants": { "period_days": "30" },
      "scale": { "quantity": 3.78541 }
    }
  }
}
```

`data/fixtures/ejemplo-externo/clientes.csv`:

```csv
Cód. Cliente;Razón Social;Latitud;Longitud;Provincia
C-100;Hospital Provincial (sintético);-2,1700;-79,9000;Guayas
C-200;Clínica del Río (sintética);-2,1900;-79,8800;Guayas
```

`data/fixtures/ejemplo-externo/catalogo.csv`:

```csv
Código;Descripción
DES-GAL;Desinfectante (galón)
```

`data/fixtures/ejemplo-externo/consumo_mensual.csv`:

```csv
Cliente;Producto;Mes;Galones
C-100;DES-GAL;2026-09;95
C-200;DES-GAL;2026-09;40
```

- [ ] **Step 3: Escribir las pruebas que fallan**

`tests/IstpetDev.Tests/Support/Fixtures.cs`:

```csharp
using IstpetDev.Application.Imports;

namespace IstpetDev.Tests.Support;

public static class Fixtures
{
    public static string Folder(string name)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "IstpetDev.sln"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("No se encontró IstpetDev.sln por encima del directorio de pruebas.");
        return Path.Combine(directory.FullName, "data", "fixtures", name);
    }

    public static ImportRequest Request(string name, bool dryRun = false, bool allowPartial = false)
    {
        var folder = Folder(name);
        var profile = MappingProfile.Parse(File.ReadAllText(Path.Combine(folder, "mapping.json")));
        var files = new List<ImportFile>();
        foreach (var (key, mapping) in profile.Files)
        {
            Assert.True(ImportEntityKeys.TryParse(key, out var entity));
            files.Add(new ImportFile(entity, mapping.File!, File.OpenRead(Path.Combine(folder, mapping.File!))));
        }
        return new ImportRequest(files, profile, dryRun, allowPartial, null);
    }
}
```

`tests/IstpetDev.Tests/Integration/Imports/ImportServiceTests.cs`:

```csharp
using System.Text;
using ClosedXML.Excel;
using IstpetDev.Application.Imports;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Imports;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Integration.Imports;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class ImportServiceTests(PostgisContainer postgres)
{
    private static async Task<ImportReport> Import(ApplicationDbContext db, ImportRequest request)
    {
        var report = await new ImportService(db, new TabularReader()).RunAsync(request);
        db.ChangeTracker.Clear();
        return report;
    }

    private static ImportFile Csv(ImportEntity entity, string content) =>
        new(entity, $"{entity.Key()}.csv", new MemoryStream(Encoding.UTF8.GetBytes(content)));

    private static string Describe(ImportReport report) => string.Join("\n", report.Issues.Select(i => $"{i.Entity}:{i.Row}:{i.Field} {i.Message}"));

    [Fact]
    public async Task Demo_base_fixture_loads_the_expected_network()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var report = await Import(db, Fixtures.Request("demo-base"));

        Assert.True(report.Applied, Describe(report));
        Assert.Equal(0, report.ErrorCount);
        Assert.Equal(6, await db.ServicePoints.CountAsync());
        Assert.Equal(8, await db.Locations.CountAsync());
        Assert.Equal(2, await db.Products.CountAsync());
        Assert.Equal(1, await db.Vehicles.CountAsync());
        Assert.Equal(2, await db.Lots.CountAsync());
        Assert.Equal(9, await db.StockBalances.CountAsync());
        Assert.Equal(7, await db.Consumptions.CountAsync());
        Assert.Equal(9, await db.InventoryMovements.CountAsync(m => m.Kind == MovementKinds.ImportAdjustment));
        var hospitalDisinfectant = await (
            from balance in db.StockBalances
            join location in db.Locations on balance.LocationId equals location.Id
            join lot in db.Lots on balance.LotId equals lot.Id
            where location.Kind == LocationKinds.ServicePoint && location.Code == "HOS-001" && lot.Code == "L-DES-2610"
            select balance.QuantityOnHand).SingleAsync();
        Assert.Equal(60m, hospitalDisinfectant);
        Assert.Equal("Applied", (await db.ImportBatches.SingleAsync()).Status);
    }

    [Fact]
    public async Task Dry_run_validates_everything_without_writing_operational_data()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var report = await Import(db, Fixtures.Request("demo-base", dryRun: true));

        Assert.False(report.Applied);
        Assert.Equal(0, report.ErrorCount);
        Assert.Equal(6, report.Entities.Single(e => e.Entity == "service_points").Created);
        Assert.Equal(0, await db.ServicePoints.CountAsync());
        Assert.Equal("DryRun", (await db.ImportBatches.SingleAsync()).Status);
    }

    [Fact]
    public async Task Reimporting_the_same_files_creates_nothing_new()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        await Import(db, Fixtures.Request("demo-base"));

        var second = await Import(db, Fixtures.Request("demo-base"));

        Assert.True(second.Applied, Describe(second));
        Assert.All(second.Entities, e => Assert.Equal(0, e.Created));
        Assert.Equal(6, await db.ServicePoints.CountAsync());
        Assert.Equal(9, await db.InventoryMovements.CountAsync());
    }

    [Fact]
    public async Task External_dataset_loads_with_its_mapping_profile()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var report = await Import(db, Fixtures.Request("ejemplo-externo"));

        Assert.True(report.Applied, Describe(report));
        var point = await db.ServicePoints.SingleAsync(p => p.Code == "C-100");
        Assert.Equal(-2.17, point.Position!.Y, 4);
        Assert.Equal("cliente", point.Category);
        Assert.Equal("Guayas", point.Region);
        var consumption = await db.Consumptions.SingleAsync(c => c.ServicePointId == point.Id);
        Assert.Equal(359.614m, consumption.Quantity);
        Assert.Equal(30, consumption.PeriodDays);
        Assert.Equal(new DateOnly(2026, 9, 1), consumption.OccurredOn);
        Assert.Equal("L", (await db.Products.SingleAsync()).BaseUnit);
        Assert.Contains("weight_per_unit", report.Entities.Single(e => e.Entity == "products").Assumptions.Keys);
    }

    [Fact]
    public async Task Errors_reject_the_whole_import_unless_partial_is_allowed()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        const string csv = "code,name,lat,lon\nB-1,Bodega,-0.2,-78.5\nB-2,Bodega mala,-200,-78.5\n";
        ImportRequest Request(bool partial) => new([Csv(ImportEntity.Warehouses, csv)], MappingProfile.Auto(), false, partial, null);

        var rejected = await Import(db, Request(partial: false));
        Assert.False(rejected.Applied);
        Assert.Equal(3, Assert.Single(rejected.Issues).Row);
        Assert.Equal(0, await db.Locations.CountAsync());
        Assert.Equal("Rejected", (await db.ImportBatches.SingleAsync()).Status);

        var partial = await Import(db, Request(partial: true));
        Assert.True(partial.Applied);
        Assert.Equal(1, await db.Locations.CountAsync());
    }

    [Fact]
    public async Task Unknown_references_are_reported_with_row_and_field()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var report = await Import(db, new ImportRequest(
            [Csv(ImportEntity.Consumptions, "point_code,sku,date,quantity\nNOPE,DES-01,2026-10-05,1\n")],
            MappingProfile.Auto(), true, false, null));

        var issue = Assert.Single(report.Issues, i => i.Severity == IssueSeverity.Error);
        Assert.Equal("unknown_reference", issue.Code);
        Assert.Equal(2, issue.Row);
        Assert.Equal("point_code", issue.Field);
    }

    [Fact]
    public async Task Codes_match_across_files_regardless_of_case_and_spaces()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var report = await Import(db, new ImportRequest(
        [
            Csv(ImportEntity.ServicePoints, "code,name\nHOS-001,Hospital\n"),
            Csv(ImportEntity.Products, "sku,name,base_unit\nDES-01,Desinfectante,L\n"),
            Csv(ImportEntity.Consumptions, "point_code,sku,date,quantity\n\" hos-001 \",des-01,2026-10-05,12\n"),
        ], MappingProfile.Auto(), false, false, null));

        Assert.True(report.Applied, Describe(report));
        Assert.Equal(1, await db.Consumptions.CountAsync());
    }

    [Fact]
    public async Task Duplicate_consumption_rows_are_summed_with_a_warning()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var report = await Import(db, new ImportRequest(
        [
            Csv(ImportEntity.ServicePoints, "code,name\nP1,Punto\n"),
            Csv(ImportEntity.Products, "sku,name,base_unit\nS1,Producto,L\n"),
            Csv(ImportEntity.Consumptions, "point_code,sku,date,quantity\nP1,S1,2026-10-05,5\nP1,S1,2026-10-05,7\n"),
        ], MappingProfile.Auto(), false, false, null));

        Assert.True(report.Applied, Describe(report));
        Assert.Equal(12m, (await db.Consumptions.SingleAsync()).Quantity);
        Assert.Contains(report.Issues, i => i.Code == "duplicate" && i.Severity == IssueSeverity.Warning);
    }

    [Fact]
    public async Task Xlsx_numbers_ignore_the_profile_decimal_separator()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Productos");
        sheet.Cell(1, 1).Value = "sku";
        sheet.Cell(1, 2).Value = "name";
        sheet.Cell(1, 3).Value = "base_unit";
        sheet.Cell(1, 4).Value = "weight_per_unit";
        sheet.Cell(2, 1).Value = "DES-01";
        sheet.Cell(2, 2).Value = "Desinfectante";
        sheet.Cell(2, 3).Value = "L";
        sheet.Cell(2, 4).Value = 0.45;
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        var profile = MappingProfile.Parse("""{ "name": "coma", "decimalSeparator": "," }""");

        var report = await Import(db, new ImportRequest([new ImportFile(ImportEntity.Products, "productos.xlsx", stream)], profile, false, false, null));

        Assert.True(report.Applied, Describe(report));
        Assert.Equal(0.45m, (await db.Products.SingleAsync()).WeightPerBaseUnit);
    }
}
```

- [ ] **Step 4: Ejecutar las pruebas y verificar que fallan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~ImportServiceTests"`
Expected: FAIL de compilación: `The type or namespace name 'ImportService' could not be found`.

- [ ] **Step 5: Implementar `ImportContext`**

`src/Application/Imports/ImportContext.cs`:

```csharp
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Imports;

// Entidades existentes indexadas por clave natural; el escritor añade aquí lo que crea en el mismo lote.
internal sealed class ImportContext
{
    public Dictionary<(string Kind, string Code), Location> Locations { get; } = new();
    public Dictionary<string, ServicePoint> ServicePoints { get; } = new();
    public Dictionary<string, Product> Products { get; } = new();
    public Dictionary<string, Vehicle> Vehicles { get; } = new();
    public Dictionary<(Guid ProductId, string Code), Lot> Lots { get; } = new();
    public Dictionary<(Guid LocationId, Guid LotId), StockBalance> Balances { get; } = new();
    public Dictionary<(Guid ServicePointId, Guid ProductId, DateOnly Date), Consumption> Consumptions { get; } = new();

    public static async Task<ImportContext> LoadAsync(IApplicationDbContext db, CancellationToken cancellationToken)
    {
        var context = new ImportContext();
        foreach (var x in await db.Locations.ToListAsync(cancellationToken)) context.Locations[(x.Kind, x.Code)] = x;
        foreach (var x in await db.ServicePoints.ToListAsync(cancellationToken)) context.ServicePoints[x.Code] = x;
        foreach (var x in await db.Products.ToListAsync(cancellationToken)) context.Products[x.Sku] = x;
        foreach (var x in await db.Vehicles.ToListAsync(cancellationToken)) context.Vehicles[x.Code] = x;
        foreach (var x in await db.Lots.ToListAsync(cancellationToken)) context.Lots[(x.ProductId, x.Code)] = x;
        foreach (var x in await db.StockBalances.ToListAsync(cancellationToken)) context.Balances[(x.LocationId, x.LotId)] = x;
        foreach (var x in await db.Consumptions.ToListAsync(cancellationToken)) context.Consumptions[(x.ServicePointId, x.ProductId, x.OccurredOn)] = x;
        return context;
    }
}
```

- [ ] **Step 6: Implementar `CanonicalWriter`**

`src/Application/Imports/CanonicalWriter.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using NetTopologySuite.Geometries;

namespace IstpetDev.Application.Imports;

// Inserta o actualiza por clave natural. Devuelve false cuando una fila no se aplica (ya registrada en el reporte).
internal sealed class CanonicalWriter(IApplicationDbContext db, ImportContext context, ImportReport report, Guid batchId)
{
    private readonly HashSet<string> _seen = new();

    public void Write(ImportEntity entity, IReadOnlyList<ParsedRow> rows)
    {
        var entityReport = report.Entity(entity);
        foreach (var row in rows)
        {
            var applied = entity switch
            {
                ImportEntity.Warehouses => Warehouse(row, entityReport),
                ImportEntity.ServicePoints => ServicePoint(row, entityReport),
                ImportEntity.Products => Product(row, entityReport),
                ImportEntity.Vehicles => Vehicle(row, entityReport),
                ImportEntity.Lots => Lot(row, entityReport),
                ImportEntity.Stock => Stock(row, entityReport),
                ImportEntity.Consumptions => Consumption(row, entityReport),
                _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, null),
            };
            if (!applied) entityReport.Skipped++;
        }
    }

    private bool Warehouse(ParsedRow row, EntityImportReport r)
    {
        var code = row.Text("code")!;
        FirstInBatch(ImportEntity.Warehouses, code, row, "La bodega");
        UpsertLocation(LocationKinds.Warehouse, code, row.Text("name")!, Position(row), r);
        return true;
    }

    private bool ServicePoint(ParsedRow row, EntityImportReport r)
    {
        var code = row.Text("code")!;
        FirstInBatch(ImportEntity.ServicePoints, code, row, "El punto");
        if (!context.ServicePoints.TryGetValue(code, out var point))
        {
            point = new ServicePoint { Code = code };
            db.ServicePoints.Add(point);
            context.ServicePoints[code] = point;
            r.Created++;
        }
        else r.Updated++;
        point.Name = row.Text("name")!;
        point.Position = Position(row);
        point.Region = row.Text("region");
        point.Category = row.Text("category");
        point.Criticality = row.Text("criticality")!.ToLowerInvariant();
        point.WindowStartLocal = row.Time("window_start");
        point.WindowEndLocal = row.Time("window_end");
        point.ServiceMinutes = row.Int("service_minutes");
        point.LeadTimeDays = row.Decimal("lead_time_days");
        point.IsActive = true;
        UpsertLocation(LocationKinds.ServicePoint, code, point.Name, point.Position, null, servicePointId: point.Id);
        return true;
    }

    private bool Product(ParsedRow row, EntityImportReport r)
    {
        var sku = row.Text("sku")!;
        FirstInBatch(ImportEntity.Products, sku, row, "El SKU");
        if (!context.Products.TryGetValue(sku, out var product))
        {
            product = new Product { Sku = sku };
            db.Products.Add(product);
            context.Products[sku] = product;
            r.Created++;
        }
        else r.Updated++;
        product.Name = row.Text("name")!;
        product.BaseUnit = row.Text("base_unit")!.ToUpperInvariant();
        product.WeightPerBaseUnit = row.Decimal("weight_per_unit") ?? 0;
        product.VolumePerBaseUnit = row.Decimal("volume_per_unit") ?? 0;
        product.IsActive = true;
        return true;
    }

    private bool Vehicle(ParsedRow row, EntityImportReport r)
    {
        var code = row.Text("code")!;
        var depotCode = row.Text("depot_code")!;
        if (!context.Locations.TryGetValue((LocationKinds.Warehouse, depotCode), out var depot))
        {
            report.Error(ImportEntity.Vehicles, row.Row, "depot_code", "unknown_reference",
                $"La bodega '{depotCode}' no existe. Inclúyala en 'warehouses' en esta importación o en una anterior.");
            return false;
        }
        FirstInBatch(ImportEntity.Vehicles, code, row, "El vehículo");
        if (!context.Vehicles.TryGetValue(code, out var vehicle))
        {
            vehicle = new Vehicle { Code = code };
            db.Vehicles.Add(vehicle);
            context.Vehicles[code] = vehicle;
            r.Created++;
        }
        else r.Updated++;
        vehicle.DepotLocationId = depot.Id;
        vehicle.MaxWeight = row.Decimal("max_weight")!.Value;
        vehicle.MaxVolume = row.Decimal("max_volume");
        vehicle.CostPerKm = row.Decimal("cost_per_km");
        vehicle.IsActive = true;
        UpsertLocation(LocationKinds.Vehicle, code, $"Vehículo {code}", null, null, vehicleId: vehicle.Id);
        return true;
    }

    private bool Lot(ParsedRow row, EntityImportReport r)
    {
        var sku = row.Text("sku")!;
        var lotCode = row.Text("lot_code")!;
        if (!TryProduct(ImportEntity.Lots, row, sku, out var product)) return false;
        FirstInBatch(ImportEntity.Lots, $"{sku}/{lotCode}", row, "El lote");
        var lot = FindOrCreateLot(product, lotCode, r);
        lot.ExpiresOn = row.Date("expires_on");
        lot.Origin = row.Text("origin");
        return true;
    }

    private bool Stock(ParsedRow row, EntityImportReport r)
    {
        var code = row.Text("location_code")!;
        var sku = row.Text("sku")!;
        if (!TryProduct(ImportEntity.Stock, row, sku, out var product)) return false;
        var location = ResolveLocation(row, code);
        if (location is null) return false;

        var lotCode = row.Text("lot_code") ?? $"GENERICO-{sku}";
        if (!context.Lots.ContainsKey((product.Id, lotCode)) && row.Text("lot_code") is not null)
            report.Warn(ImportEntity.Stock, row.Row, "lot_code", "lot_created", $"El lote '{lotCode}' no estaba en 'lots'; se crea sin vencimiento.");
        var lot = FindOrCreateLot(product, lotCode, null);

        var first = FirstInBatch(ImportEntity.Stock, $"{location.Kind}:{code}/{sku}/{lotCode}", row, "El saldo");
        if (!context.Balances.TryGetValue((location.Id, lot.Id), out var balance))
        {
            balance = new StockBalance { LocationId = location.Id, LotId = lot.Id };
            db.StockBalances.Add(balance);
            context.Balances[(location.Id, lot.Id)] = balance;
            r.Created++;
        }
        else if (first) r.Updated++;

        var quantity = row.Decimal("quantity")!.Value;
        var target = first ? quantity : balance.QuantityOnHand + quantity;
        var delta = target - balance.QuantityOnHand;
        if (delta != 0)
        {
            db.InventoryMovements.Add(new InventoryMovement
            {
                Kind = MovementKinds.ImportAdjustment,
                LotId = lot.Id,
                FromLocationId = delta < 0 ? location.Id : null,
                ToLocationId = delta > 0 ? location.Id : null,
                Quantity = Math.Abs(delta),
                OperationId = BaseEntity.NewId(),
                OccurredAtUtc = DateTime.UtcNow,
                ImportBatchId = batchId,
                Reason = "Ajuste por importación",
            });
        }
        balance.QuantityOnHand = target;
        return true;
    }

    private bool Consumption(ParsedRow row, EntityImportReport r)
    {
        var pointCode = row.Text("point_code")!;
        var sku = row.Text("sku")!;
        if (!context.ServicePoints.TryGetValue(pointCode, out var point))
        {
            report.Error(ImportEntity.Consumptions, row.Row, "point_code", "unknown_reference",
                $"El punto '{pointCode}' no existe. Inclúyalo en 'service_points'.");
            return false;
        }
        if (!TryProduct(ImportEntity.Consumptions, row, sku, out var product)) return false;

        var date = row.Date("date")!.Value;
        var first = FirstInBatch(ImportEntity.Consumptions, $"{pointCode}/{sku}/{date:yyyy-MM-dd}", row, "El consumo");
        if (!context.Consumptions.TryGetValue((point.Id, product.Id, date), out var consumption))
        {
            consumption = new Consumption { ServicePointId = point.Id, ProductId = product.Id, OccurredOn = date, SourceKind = "import" };
            db.Consumptions.Add(consumption);
            context.Consumptions[(point.Id, product.Id, date)] = consumption;
            r.Created++;
        }
        else if (first) r.Updated++;

        var quantity = row.Decimal("quantity")!.Value;
        consumption.Quantity = first ? quantity : consumption.Quantity + quantity;
        consumption.PeriodDays = row.Int("period_days") ?? 1;
        consumption.ImportBatchId = batchId;
        return true;
    }

    private bool FirstInBatch(ImportEntity entity, string key, ParsedRow row, string subject)
    {
        if (_seen.Add($"{entity}|{key}")) return true;
        var action = entity is ImportEntity.Stock or ImportEntity.Consumptions ? "se suma" : "gana la última fila";
        report.Warn(entity, row.Row, null, "duplicate", $"{subject} '{key}' aparece más de una vez en el archivo; {action}.");
        return false;
    }

    private bool TryProduct(ImportEntity entity, ParsedRow row, string sku, [NotNullWhen(true)] out Product? product)
    {
        if (context.Products.TryGetValue(sku, out product)) return true;
        report.Error(entity, row.Row, "sku", "unknown_reference", $"El SKU '{sku}' no existe. Inclúyalo en 'products'.");
        return false;
    }

    private Lot FindOrCreateLot(Product product, string code, EntityImportReport? r)
    {
        if (context.Lots.TryGetValue((product.Id, code), out var lot))
        {
            if (r is not null) r.Updated++;
            return lot;
        }
        lot = new Lot { ProductId = product.Id, Code = code };
        db.Lots.Add(lot);
        context.Lots[(product.Id, code)] = lot;
        if (r is not null) r.Created++;
        return lot;
    }

    private Location? ResolveLocation(ParsedRow row, string code)
    {
        var kindText = row.Text("location_kind");
        if (kindText is not null)
        {
            var kind = HeaderNormalizer.Normalize(kindText) switch
            {
                "warehouse" or "bodega" or "almacen" or "deposito" => LocationKinds.Warehouse,
                "service_point" or "punto" or "cliente" or "punto_de_servicio" => LocationKinds.ServicePoint,
                "vehicle" or "vehiculo" or "camion" => LocationKinds.Vehicle,
                _ => null,
            };
            if (kind is null)
            {
                report.Error(ImportEntity.Stock, row.Row, "location_kind", "invalid_value",
                    $"Tipo de ubicación '{kindText}' desconocido. Use warehouse/bodega, service_point/punto o vehicle/vehiculo.");
                return null;
            }
            if (context.Locations.TryGetValue((kind, code), out var exact)) return exact;
            report.Error(ImportEntity.Stock, row.Row, "location_code", "unknown_reference", $"No existe {kindText} '{code}'.");
            return null;
        }
        var matches = LocationKinds.All.Where(k => context.Locations.ContainsKey((k, code))).ToList();
        if (matches.Count == 1) return context.Locations[(matches[0], code)];
        report.Error(ImportEntity.Stock, row.Row, "location_code", matches.Count == 0 ? "unknown_reference" : "ambiguous_reference",
            matches.Count == 0
                ? $"La ubicación '{code}' no existe como bodega, punto ni vehículo."
                : $"'{code}' existe como {string.Join(" y ", matches)}; añada la columna location_kind.");
        return null;
    }

    private Location UpsertLocation(string kind, string code, string name, Point? position, EntityImportReport? r,
        Guid? servicePointId = null, Guid? vehicleId = null)
    {
        if (context.Locations.TryGetValue((kind, code), out var location))
        {
            location.Name = name;
            location.Position = position ?? location.Position;
            location.IsActive = true;
            if (r is not null) r.Updated++;
            return location;
        }
        location = new Location
        {
            Kind = kind, Code = code, Name = name, Position = position,
            ServicePointId = servicePointId, VehicleId = vehicleId, IsActive = true,
        };
        db.Locations.Add(location);
        context.Locations[(kind, code)] = location;
        if (r is not null) r.Created++;
        return location;
    }

    private static Point? Position(ParsedRow row) =>
        row.Decimal("lat") is { } lat && row.Decimal("lon") is { } lon ? new Point((double)lon, (double)lat) { SRID = 4326 } : null;
}
```

- [ ] **Step 7: Implementar `ImportService` y registrarlo**

`src/Application/Imports/ImportService.cs`:

```csharp
using System.Security.Cryptography;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Entities;

namespace IstpetDev.Application.Imports;

public sealed record ImportFile(ImportEntity Entity, string FileName, Stream Content);

public sealed record ImportRequest(IReadOnlyList<ImportFile> Files, MappingProfile Profile, bool DryRun, bool AllowPartial, Guid? ActorAccountId);

public interface IImportService
{
    Task<ImportReport> RunAsync(ImportRequest request, CancellationToken cancellationToken = default);
}

public sealed class ImportService(IApplicationDbContext db, ITabularReader reader) : IImportService
{
    public async Task<ImportReport> RunAsync(ImportRequest request, CancellationToken cancellationToken = default)
    {
        var batch = new ImportBatch
        {
            ProfileName = request.Profile.Name, DryRun = request.DryRun,
            ProfileJson = request.Profile.ToJson(), CreatedByAccountId = request.ActorAccountId,
        };
        var report = new ImportReport(batch.Id, request.Profile.Name, request.DryRun);
        var baseSettings = request.Profile.ToParseSettings();
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var parsed = new Dictionary<ImportEntity, List<ParsedRow>>();

        foreach (var file in request.Files.OrderBy(f => f.Entity))
        {
            var entityReport = report.Entity(file.Entity);
            if (entityReport.File is not null)
            {
                report.Error(file.Entity, 0, null, "duplicate_file", $"Hay dos archivos para '{file.Entity.Key()}': '{entityReport.File}' y '{file.FileName}'.");
                continue;
            }
            entityReport.File = file.FileName;
            using var buffer = new MemoryStream();
            await file.Content.CopyToAsync(buffer, cancellationToken);
            hash.AppendData(buffer.GetBuffer(), 0, (int)buffer.Length);
            buffer.Position = 0;

            var mapping = request.Profile.For(file.Entity);
            TabularData table;
            try { table = reader.Read(buffer, file.FileName, request.Profile.Delimiter, mapping?.Sheet); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                report.Error(file.Entity, 0, null, "file_unreadable", $"No se pudo leer '{file.FileName}': {ex.Message}");
                continue;
            }

            var resolution = ColumnResolver.Resolve(table.Headers, file.Entity, mapping);
            entityReport.RowsRead = table.Rows.Count;
            entityReport.ResolvedColumns = resolution.Columns;
            entityReport.IgnoredColumns = resolution.Ignored;
            foreach (var missing in resolution.MissingRequired)
                report.Error(file.Entity, 1, missing, "missing_column",
                    $"Falta la columna obligatoria '{missing}' o su mapeo. Columnas recibidas: {string.Join(", ", table.Headers)}.");
            if (resolution.MissingRequired.Count > 0)
            {
                entityReport.Skipped = table.Rows.Count;
                continue;
            }
            var settings = table.InvariantNumbers ? baseSettings with { DecimalSeparator = "." } : baseSettings;
            parsed[file.Entity] = RowParser.Parse(table, file.Entity, resolution, mapping, settings, report);
        }
        batch.InputHash = Convert.ToHexString(hash.GetHashAndReset());

        var context = await ImportContext.LoadAsync(db, cancellationToken);
        var writer = new CanonicalWriter(db, context, report, batch.Id);
        foreach (var entity in CanonicalSchema.Order)
            if (parsed.TryGetValue(entity, out var rows)) writer.Write(entity, rows);

        report.Applied = !request.DryRun && parsed.Count > 0 && (!report.HasErrors || request.AllowPartial);
        if (!report.Applied) db.ChangeTracker.Clear();
        batch.Status = report.Applied ? "Applied" : request.DryRun ? "DryRun" : "Rejected";
        batch.ReportJson = report.ToJson();
        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync(cancellationToken);
        return report;
    }
}
```

`src/Application/DependencyInjection.cs` — añadir `using IstpetDev.Application.Imports;` y dentro de `AddApplicationServices`:

```csharp
        services.AddScoped<IImportService, ImportService>();
```

- [ ] **Step 8: Ejecutar las pruebas y verificar que pasan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~ImportServiceTests"`
Expected: PASS (9 pruebas).

- [ ] **Step 9: Commit**

```bash
git add src/Application data tests
git commit -m "feat(importador): servicio de importación con upsert por clave natural, fixtures demo-base y ejemplo-externo"
```

---

### Task 6: Roles fijos, cuentas de demo y autorización

**Files:**
- Create: `src/Domain/Common/Roles.cs`
- Create: `src/Application/Demo/DemoOptions.cs`
- Create: `src/Infrastructure/Persistence/AccountSeeder.cs`
- Create: `src/Api/Authentication/AccountRoleClaimsTransformation.cs`
- Create: `src/Api/Authentication/ClaimsPrincipalExtensions.cs`
- Modify (reemplazo completo): `src/Api/Program.cs`
- Modify: `src/Api/Controllers/ServicePointsController.cs`, `src/Api/appsettings.json`, `src/Api/appsettings.Development.json`
- Test: `tests/IstpetDev.Tests/Support/TestTokens.cs`, `tests/IstpetDev.Tests/Support/ApiFactory.cs`, `tests/IstpetDev.Tests/Integration/Api/AuthorizationTests.cs`

**Interfaces:**
- Consumes: `ApplicationDbContext.Accounts` (Task 4), `ImportReport` (Task 2).
- Produces:
  - `Roles.{Gerencia, Planificador, Bodega, Conductor, Receptor, Automatizacion, Planning, Any, All, IsValid(string?)}`
  - `DemoOptions { const Section = "Demo"; bool Enabled; string FixturePath; List<DemoAccount> Accounts }`, `DemoAccount { Subject, DisplayName, Role }`, `interface IDemoResetService { Task<ImportReport> ResetAsync(CancellationToken) }`
  - `AccountSeeder.SeedAsync(ApplicationDbContext, string? issuer, IEnumerable<DemoAccount>, CancellationToken)`
  - `AccountRoleClaimsTransformation.AccountIdClaim = "account_id"`; `ClaimsPrincipal.AccountId() : Guid?`
  - Pruebas: `TestTokens.{Issuer, ClientId, Planner, Driver, Create(subject, tokenUse, scope)}`, `ApiFactory(string connectionString, bool demoEnabled = true)` con `ClientFor(string? subject, string tokenUse = "access") : HttpClient`

- [ ] **Step 1: Escribir los helpers de prueba y las pruebas que fallan**

`tests/IstpetDev.Tests/Support/TestTokens.cs`:

```csharp
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IstpetDev.Tests.Support;

// Tokens con la forma de un access token de Cognito, firmados con una clave RSA solo de pruebas.
public static class TestTokens
{
    public const string Issuer = "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_TESTPOOL";
    public const string ClientId = "istpetdev-test-client";
    public const string Planner = "planner-1";
    public const string Driver = "driver-1";

    public static readonly RsaSecurityKey Key = new(RSA.Create(2048)) { KeyId = "istpetdev-test" };

    public static string Create(string subject, string tokenUse = "access", string scope = "istpetdev-api/access") =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow.AddSeconds(-5),
            Expires = DateTime.UtcNow.AddMinutes(10),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = subject, ["client_id"] = ClientId, ["token_use"] = tokenUse, ["scope"] = scope,
            },
            SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.RsaSha256),
        });
}
```

`tests/IstpetDev.Tests/Support/ApiFactory.cs`:

```csharp
using System.Net.Http.Headers;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace IstpetDev.Tests.Support;

public sealed class ApiFactory(string connectionString, bool demoEnabled = true) : WebApplicationFactory<Program>
{
    static ApiFactory()
    {
        // Program lee estos valores al registrar servicios (antes de Build); por eso van como variables de entorno.
        // Son constantes, así que compartirlas entre pruebas paralelas no causa interferencias.
        Environment.SetEnvironmentVariable("Authentication__Authority", TestTokens.Issuer);
        Environment.SetEnvironmentVariable("Authentication__AllowedClientIds__0", TestTokens.ClientId);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Main"] = connectionString,
            ["ApplyMigrations"] = "true",
            ["Demo:Enabled"] = demoEnabled.ToString(),
            ["Demo:FixturePath"] = Fixtures.Folder("demo-base"),
            ["Demo:Accounts:0:Subject"] = TestTokens.Planner,
            ["Demo:Accounts:0:DisplayName"] = "Planificación de prueba",
            ["Demo:Accounts:0:Role"] = Roles.Planificador,
            ["Demo:Accounts:1:Subject"] = TestTokens.Driver,
            ["Demo:Accounts:1:DisplayName"] = "Conductor de prueba",
            ["Demo:Accounts:1:Role"] = Roles.Conductor,
        }));
        builder.ConfigureTestServices(services => services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            // Sustituye solo la descarga de metadatos de Cognito; se conserva toda la validación de producción.
            var metadata = new OpenIdConnectConfiguration { Issuer = TestTokens.Issuer };
            metadata.SigningKeys.Add(TestTokens.Key);
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
        }));
    }

    public HttpClient ClientFor(string? subject, string tokenUse = "access")
    {
        var client = CreateClient();
        if (subject is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Create(subject, tokenUse));
        return client;
    }
}
```

`tests/IstpetDev.Tests/Integration/Api/AuthorizationTests.cs`:

```csharp
using System.Net;
using IstpetDev.Tests.Support;

namespace IstpetDev.Tests.Integration.Api;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class AuthorizationTests(PostgisContainer postgres)
{
    [Theory]
    [InlineData(null, "access", HttpStatusCode.Unauthorized)]
    [InlineData("identidad-sin-cuenta", "access", HttpStatusCode.Forbidden)]
    [InlineData(TestTokens.Planner, "id", HttpStatusCode.Unauthorized)]
    [InlineData(TestTokens.Planner, "access", HttpStatusCode.OK)]
    [InlineData(TestTokens.Driver, "access", HttpStatusCode.OK)]
    public async Task Service_points_require_a_registered_role(string? subject, string tokenUse, HttpStatusCode expected)
    {
        await using var api = new ApiFactory(postgres.NewDatabase());

        var response = await api.ClientFor(subject, tokenUse).GetAsync("/api/v1/service-points");

        Assert.Equal(expected, response.StatusCode);
    }
}
```

- [ ] **Step 2: Ejecutar las pruebas y verificar que fallan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~AuthorizationTests"`
Expected: FAIL de compilación: `The name 'Roles' does not exist in the current context`.

- [ ] **Step 3: Implementar roles, opciones de demo y siembra de cuentas**

`src/Domain/Common/Roles.cs`:

```csharp
namespace IstpetDev.Domain.Common;

// Seis roles funcionales de ADR-14, fijos (spec 001). El rol de cada cuenta vive en Account.Role.
public static class Roles
{
    public const string Gerencia = "Gerencia";
    public const string Planificador = "Planificador";
    public const string Bodega = "Bodega";
    public const string Conductor = "Conductor";
    public const string Receptor = "Receptor";
    public const string Automatizacion = "Automatizacion";

    // Valores para [Authorize(Roles = ...)], que acepta nombres separados por coma.
    public const string Planning = Gerencia + "," + Planificador;
    public const string Any = Gerencia + "," + Planificador + "," + Bodega + "," + Conductor + "," + Receptor + "," + Automatizacion;

    public static readonly IReadOnlyList<string> All = [Gerencia, Planificador, Bodega, Conductor, Receptor, Automatizacion];

    public static bool IsValid(string? role) => role is not null && All.Contains(role);
}
```

`src/Application/Demo/DemoOptions.cs`:

```csharp
using IstpetDev.Application.Imports;

namespace IstpetDev.Application.Demo;

public sealed class DemoOptions
{
    public const string Section = "Demo";

    public bool Enabled { get; set; }
    public string FixturePath { get; set; } = "data/fixtures/demo-base";
    public List<DemoAccount> Accounts { get; set; } = new();
}

public sealed class DemoAccount
{
    public string Subject { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public interface IDemoResetService
{
    Task<ImportReport> ResetAsync(CancellationToken cancellationToken = default);
}
```

`src/Infrastructure/Persistence/AccountSeeder.cs`:

```csharp
using IstpetDev.Application.Demo;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Infrastructure.Persistence;

public static class AccountSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, string? issuer, IEnumerable<DemoAccount> accounts, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(issuer)) return;
        foreach (var demo in accounts)
        {
            if (!Roles.IsValid(demo.Role))
                throw new InvalidOperationException($"Rol '{demo.Role}' no válido para la cuenta '{demo.Subject}'. Válidos: {string.Join(", ", Roles.All)}.");
            var account = await db.Accounts.FirstOrDefaultAsync(a => a.Issuer == issuer && a.Subject == demo.Subject, cancellationToken);
            if (account is null) db.Accounts.Add(account = new Account { Issuer = issuer, Subject = demo.Subject });
            account.DisplayName = demo.DisplayName;
            account.Role = demo.Role;
            account.IsActive = true;
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Implementar la transformación de claims**

`src/Api/Authentication/AccountRoleClaimsTransformation.cs`:

```csharp
using System.Security.Claims;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Authentication;

// El rol sale de Account en la base, nunca de un claim enviado por el cliente (Identidad OIDC y sesiones).
public sealed class AccountRoleClaimsTransformation(ApplicationDbContext database) : IClaimsTransformation
{
    public const string AccountIdClaim = "account_id";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity || principal.HasClaim(c => c.Type == AccountIdClaim))
            return principal;
        var issuer = principal.FindFirst("iss")?.Value;
        var subject = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (issuer is null || subject is null) return principal;

        var account = await database.Accounts.AsNoTracking()
            .Where(a => a.IsActive && a.Issuer == issuer && a.Subject == subject)
            .Select(a => new { a.Id, a.Role })
            .FirstOrDefaultAsync();
        if (account is null) return principal;

        identity.AddClaim(new Claim(AccountIdClaim, account.Id.ToString()));
        identity.AddClaim(new Claim(identity.RoleClaimType, account.Role));
        return principal;
    }
}
```

`src/Api/Authentication/ClaimsPrincipalExtensions.cs`:

```csharp
using System.Security.Claims;

namespace IstpetDev.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static Guid? AccountId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst(AccountRoleClaimsTransformation.AccountIdClaim)?.Value, out var id) ? id : null;
}
```

- [ ] **Step 5: Reemplazar `Program.cs`**

`src/Api/Program.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using IstpetDev.Api.Authentication;
using IstpetDev.Application;
using IstpetDev.Application.Demo;
using IstpetDev.Infrastructure;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Safe logging: ensure no headers/tokens are leaked in logs
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Cognito valida la identidad (ADR-10); el rol se lee de Account (spec 001).
builder.Services.AddCognitoAuthentication(builder.Configuration);
builder.Services.AddScoped<IClaimsTransformation, AccountRoleClaimsTransformation>();

builder.Services.Configure<DemoOptions>(builder.Configuration.GetSection(DemoOptions.Section));
// Rutas relativas de fixtures se resuelven contra la raíz de contenido (src/Api en desarrollo, /app en Docker).
builder.Services.PostConfigure<DemoOptions>(options =>
    options.FixturePath = Path.GetFullPath(options.FixturePath, builder.Environment.ContentRootPath));

// CORS configuration for local Angular web (4200) and Ionic mobile (8100)
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ??
    new[] { "http://localhost:4200", "http://localhost:8100" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("IstpetDevCors", policy =>
    {
        policy.WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await PrepareDatabaseAsync(app, args);

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger", true))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("IstpetDevCors");

app.UseAuthentication();
app.UseAuthorization();

// 1. Process Liveness: returns 200 OK as long as the process is alive, regardless of cloud/DB
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = "Live",
            timestamp = DateTime.UtcNow,
            service = "IstpetDev API"
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

// 2. Database Readiness: verifies PostgreSQL connection, reports Unhealthy/503 if unreachable
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var isHealthy = report.Status == HealthStatus.Healthy;
        context.Response.StatusCode = isHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            entries = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

app.MapControllers();

app.Run();

// Migraciones opcionales y cuentas de demo. Un fallo se registra y la API arranca; /health/ready lo reporta.
static async Task PrepareDatabaseAsync(WebApplication app, string[] args)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var migrateRequested = args.Contains("--migrate");
    try
    {
        if (app.Configuration.GetValue<bool>("ApplyMigrations") || migrateRequested)
        {
            app.Logger.LogInformation("Verificando y aplicando migraciones pendientes de EF Core...");
            await db.Database.MigrateAsync();
            // La migración puede crear la extensión postgis: recargar tipos para escribir geometry sin reiniciar.
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await connection.OpenAsync();
            connection.ReloadTypes();
            await connection.CloseAsync();
            app.Logger.LogInformation("Migraciones de base de datos aplicadas exitosamente.");
        }
        var demo = scope.ServiceProvider.GetRequiredService<IOptions<DemoOptions>>().Value;
        if (demo.Accounts.Count > 0)
            await AccountSeeder.SeedAsync(db, app.Configuration["Authentication:Authority"], demo.Accounts);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error al preparar la base de datos en el arranque.");
        if (migrateRequested) throw;
    }
}

public partial class Program { }
```

- [ ] **Step 6: Exigir rol en `ServicePointsController` y configurar `Demo`**

`src/Api/Controllers/ServicePointsController.cs` — añadir `using IstpetDev.Domain.Common;` y reemplazar `[Authorize]` por:

```csharp
[Authorize(Roles = Roles.Any)]
```

`src/Api/appsettings.json` — borrar `"HumanApiScope": "istpetdev/api"` (no se usa; el scope exigido es `istpetdev-api/access`) y añadir al objeto raíz:

```json
  "Demo": {
    "Enabled": false,
    "FixturePath": "data/fixtures/demo-base",
    "Accounts": []
  }
```

`src/Api/appsettings.Development.json` — borrar `"HumanApiScope": "istpetdev/api"` y añadir:

```json
  "Demo": {
    "Enabled": true,
    "FixturePath": "../../data/fixtures/demo-base",
    "Accounts": []
  }
```

Cada integrante registra su cuenta con user-secrets (el `sub` sale de su access token de Cognito):

```bash
dotnet user-secrets --project src/Api set "Demo:Accounts:0:Subject" "<sub de Cognito>"
dotnet user-secrets --project src/Api set "Demo:Accounts:0:DisplayName" "<nombre>"
dotnet user-secrets --project src/Api set "Demo:Accounts:0:Role" "Planificador"
```

- [ ] **Step 7: Ejecutar las pruebas y verificar que pasan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~AuthorizationTests"`
Expected: PASS (5 casos).

Si el caso `planner-1/access` devuelve 401 en lugar de 200, el emisor de prueba no llegó a `AddCognitoAuthentication`: comprobar que el constructor estático de `ApiFactory` se ejecuta antes de `CreateClient()` (lo garantiza el lenguaje) y que `appsettings.Testing.json` no existe con otro `Authority`.

- [ ] **Step 8: Commit**

```bash
git add src tests
git commit -m "feat(auth): roles fijos por cuenta y siembra de cuentas de demo (spec 001)"
```

---

### Task 7: Endpoints de importación y reset de demo; empaquetado

**Files:**
- Create: `src/Api/Controllers/ImportsController.cs`
- Create: `src/Api/Controllers/DemoController.cs`
- Create: `src/Infrastructure/Demo/DemoResetService.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Modify: `infra/docker/backend/Dockerfile`, `infra/compose/docker-compose.prod.yml`
- Test: `tests/IstpetDev.Tests/Integration/Api/ImportsApiTests.cs`

**Interfaces:**
- Consumes: `IImportService`, `ImportRequest`, `ImportFile`, `MappingProfile`, `MappingProfileException` (Tasks 2 y 5); `IDemoResetService`, `DemoOptions`, `Roles`, `ClaimsPrincipal.AccountId()` (Task 6).
- Produces: `POST /api/v1/imports?dryRun&allowPartial`, `POST /api/v1/demo/reset`; `DemoResetService : IDemoResetService`.

- [ ] **Step 1: Escribir las pruebas que fallan**

`tests/IstpetDev.Tests/Integration/Api/ImportsApiTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Imports;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Integration.Api;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class ImportsApiTests(PostgisContainer postgres)
{
    private static MultipartFormDataContent Upload(string fixture, bool includeProfile = true)
    {
        var folder = Fixtures.Folder(fixture);
        var profileJson = File.ReadAllText(Path.Combine(folder, "mapping.json"));
        var content = new MultipartFormDataContent();
        if (includeProfile) content.Add(new StringContent(profileJson), "profile");
        foreach (var file in MappingProfile.Parse(profileJson).Files.Values)
            content.Add(new ByteArrayContent(File.ReadAllBytes(Path.Combine(folder, file.File!))), "files", file.File!);
        return content;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>())!;

    [Fact]
    public async Task Demo_reset_loads_the_fixture_on_a_brand_new_database()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);

        var reset = await planner.PostAsync("/api/v1/demo/reset", null);

        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.True((await Json(reset)).GetProperty("applied").GetBoolean());
        var points = await Json(await planner.GetAsync("/api/v1/service-points"));
        Assert.Equal(6, points.GetArrayLength());
        var hospital = points.EnumerateArray().Single(p => p.GetProperty("code").GetString() == "HOS-001");
        Assert.Equal("hospital", hospital.GetProperty("type").GetString());
        Assert.Equal(-0.17, hospital.GetProperty("latitude").GetDouble(), 4);
    }

    [Fact]
    public async Task Demo_reset_is_hidden_when_demo_is_disabled_and_forbidden_for_drivers()
    {
        await using var disabled = new ApiFactory(postgres.NewDatabase(), demoEnabled: false);
        Assert.Equal(HttpStatusCode.NotFound, (await disabled.ClientFor(TestTokens.Planner).PostAsync("/api/v1/demo/reset", null)).StatusCode);

        await using var enabled = new ApiFactory(postgres.NewDatabase());
        Assert.Equal(HttpStatusCode.Forbidden, (await enabled.ClientFor(TestTokens.Driver).PostAsync("/api/v1/demo/reset", null)).StatusCode);
    }

    [Fact]
    public async Task Import_defaults_to_dry_run_and_applies_when_asked()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);

        var dryRun = await planner.PostAsync("/api/v1/imports", Upload("ejemplo-externo"));
        Assert.Equal(HttpStatusCode.OK, dryRun.StatusCode);
        Assert.False((await Json(dryRun)).GetProperty("applied").GetBoolean());
        Assert.Equal(0, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());

        var applied = await planner.PostAsync("/api/v1/imports?dryRun=false", Upload("ejemplo-externo"));
        Assert.Equal(HttpStatusCode.OK, applied.StatusCode);
        Assert.Equal(2, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());
    }

    [Fact]
    public async Task Import_rejects_bad_requests_with_explanations()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);

        var badProfile = new MultipartFormDataContent { { new StringContent("""{ "files": { "pedidos": {} } }"""), "profile" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await planner.PostAsync("/api/v1/imports", badProfile)).StatusCode);

        var unknownFile = new MultipartFormDataContent { { new ByteArrayContent("a,b\n1,2\n"u8.ToArray()), "files", "misterio.csv" } };
        var unknown = await planner.PostAsync("/api/v1/imports", unknownFile);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Contains("misterio.csv", await unknown.Content.ReadAsStringAsync());

        var invalidRows = new MultipartFormDataContent { { new ByteArrayContent("code,name,lat,lon\nB1,Bodega,-200,-78\n"u8.ToArray()), "bodegas", "b.csv" } };
        var rejected = await planner.PostAsync("/api/v1/imports?dryRun=false", invalidRows);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
        Assert.Equal(1, (await Json(rejected)).GetProperty("errorCount").GetInt32());
    }

    [Fact]
    public async Task Drivers_cannot_import()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var response = await api.ClientFor(TestTokens.Driver).PostAsync("/api/v1/imports", Upload("ejemplo-externo"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Demo_reset_keeps_completed_simulation_evidence_for_spec_005()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);
        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var organizationId = await db.Organizations.Select(x => x.Id).SingleAsync();
        var run = new SimulationRun
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, Seed = 11,
            DatasetHash = "fixture-hash", PolicyCode = "fixed7-v1",
            Status = "Completed", StartedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        };
        db.SimulationRuns.Add(run);
        db.SimulationMetrics.Add(new SimulationMetric
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId,
            SimulationRunId = run.Id, Code = "km_total",
            PolicyCode = "fixed7-v1", Numerator = 10m,
            Value = 10m, Unit = "km"
        });
        await db.SaveChangesAsync();

        Assert.Equal(HttpStatusCode.OK,
            (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);
        Assert.Equal(1, await db.SimulationRuns.CountAsync(x => x.Id == run.Id));
        Assert.Equal(1, await db.SimulationMetrics.CountAsync(x => x.SimulationRunId == run.Id));
    }
}
```

- [ ] **Step 2: Ejecutar las pruebas y verificar que fallan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~ImportsApiTests"`
Expected: FAIL: las peticiones a `/api/v1/demo/reset` y `/api/v1/imports` devuelven `404 NotFound` en vez de los códigos esperados.

- [ ] **Step 3: Implementar `DemoResetService` y registrarlo**

`src/Infrastructure/Demo/DemoResetService.cs`:

```csharp
using IstpetDev.Application.Demo;
using IstpetDev.Application.Imports;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Infrastructure.Demo;

// Borra los datos operativos y vuelve a importar el fixture con el importador público (guardarraíl G2), en una transacción.
public sealed class DemoResetService(ApplicationDbContext db, IImportService importer, IOptions<DemoOptions> options) : IDemoResetService
{
    public async Task<ImportReport> ResetAsync(CancellationToken cancellationToken = default)
    {
        var folder = options.Value.FixturePath;
        var profile = MappingProfile.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "mapping.json"), cancellationToken));
        var files = new List<ImportFile>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await DeleteOperationalDataAsync(cancellationToken);
            foreach (var (key, mapping) in profile.Files)
                if (mapping.File is not null && ImportEntityKeys.TryParse(key, out var entity))
                    files.Add(new ImportFile(entity, mapping.File, File.OpenRead(Path.Combine(folder, mapping.File))));

            var report = await importer.RunAsync(new ImportRequest(files, profile, DryRun: false, AllowPartial: false, ActorAccountId: null), cancellationToken);
            if (report.Applied) await transaction.CommitAsync(cancellationToken);
            else await transaction.RollbackAsync(cancellationToken);
            return report;
        }
        finally
        {
            foreach (var file in files) await file.Content.DisposeAsync();
        }
    }

    // Orden inverso a las dependencias. Se conservan Organization, Account, ImportBatch
    // y SimulationRun/SimulationMetric completados: el reset operativo no borra evidencia de 005.
    private async Task DeleteOperationalDataAsync(CancellationToken ct)
    {
        await db.CustodyEvents.ExecuteDeleteAsync(ct);
        await db.Evidences.ExecuteDeleteAsync(ct);
        await db.ReceiptLines.ExecuteDeleteAsync(ct);
        await db.Receipts.ExecuteDeleteAsync(ct);
        await db.InventoryMovements.ExecuteDeleteAsync(ct);
        await db.DeliveryLines.ExecuteDeleteAsync(ct);
        await db.Deliveries.ExecuteDeleteAsync(ct);
        await db.Incidents.ExecuteDeleteAsync(ct);
        await db.RouteStops.ExecuteDeleteAsync(ct);
        await db.RoutePlans.ExecuteDeleteAsync(ct);
        await db.AgentDecisions.ExecuteDeleteAsync(ct);
        await db.RiskSnapshots.ExecuteDeleteAsync(ct);
        await db.IdempotencyRecords.ExecuteDeleteAsync(ct);
        await db.StockBalances.ExecuteDeleteAsync(ct);
        await db.HandlingUnitLines.ExecuteDeleteAsync(ct);
        await db.HandlingUnits.ExecuteDeleteAsync(ct);
        await db.Consumptions.ExecuteDeleteAsync(ct);
        await db.Lots.ExecuteDeleteAsync(ct);
        await db.Locations.Where(l => l.Kind == LocationKinds.Vehicle).ExecuteDeleteAsync(ct);
        await db.Vehicles.ExecuteDeleteAsync(ct);
        await db.Locations.ExecuteDeleteAsync(ct);
        await db.ServicePoints.ExecuteDeleteAsync(ct);
        await db.Products.ExecuteDeleteAsync(ct);
    }
}
```

`src/Infrastructure/DependencyInjection.cs` — añadir `using IstpetDev.Application.Demo;` y `using IstpetDev.Infrastructure.Demo;`, y junto al registro de `ITabularReader`:

```csharp
        services.AddScoped<IDemoResetService, DemoResetService>();
```

- [ ] **Step 4: Implementar los controladores**

`src/Api/Controllers/DemoController.cs`:

```csharp
using IstpetDev.Application.Demo;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/demo")]
[Authorize(Roles = Roles.Planning)]
public sealed class DemoController(IDemoResetService reset, IOptions<DemoOptions> options) : ControllerBase
{
    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled) return NotFound();
        var report = await reset.ResetAsync(cancellationToken);
        return report.Applied ? Ok(report) : UnprocessableEntity(report);
    }
}
```

`src/Api/Controllers/ImportsController.cs`:

```csharp
using IstpetDev.Api.Authentication;
using IstpetDev.Application.Imports;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/imports")]
[Authorize(Roles = Roles.Planning)]
public sealed class ImportsController(IImportService importer) : ControllerBase
{
    // dryRun es true por defecto: aplicar datos exige pedirlo explícitamente.
    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Import([FromQuery] bool dryRun = true, [FromQuery] bool allowPartial = false, CancellationToken cancellationToken = default)
    {
        if (!Request.HasFormContentType)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Se esperaba multipart/form-data con archivos.");
        var form = await Request.ReadFormAsync(cancellationToken);

        MappingProfile profile;
        try
        {
            profile = form.TryGetValue("profile", out var json) && !string.IsNullOrWhiteSpace(json)
                ? MappingProfile.Parse(json.ToString())
                : MappingProfile.Auto();
        }
        catch (MappingProfileException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Perfil de mapeo inválido", detail: string.Join(" ", ex.Errors));
        }

        var files = new List<ImportFile>();
        var unknown = new List<string>();
        foreach (var file in form.Files)
        {
            var entity = profile.EntityForFile(file.FileName)
                ?? (ImportEntityKeys.TryParse(file.Name, out var byField) ? byField : (ImportEntity?)null);
            if (entity is null) unknown.Add(file.FileName);
            else files.Add(new ImportFile(entity.Value, file.FileName, file.OpenReadStream()));
        }
        if (unknown.Count > 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Archivos sin entidad",
                detail: $"No se pudo asociar: {string.Join(", ", unknown)}. Use como nombre de campo una entidad " +
                        $"({string.Join(", ", ImportEntityKeys.All)}) o declare el archivo en el perfil.");
        if (files.Count == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "No se recibieron archivos.");

        var report = await importer.RunAsync(new ImportRequest(files, profile, dryRun, allowPartial, User.AccountId()), cancellationToken);
        return !dryRun && !report.Applied ? UnprocessableEntity(report) : Ok(report);
    }
}
```

- [ ] **Step 5: Ejecutar las pruebas y verificar que pasan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~ImportsApiTests"`
Expected: PASS (6 pruebas). `Demo_reset_loads_the_fixture_on_a_brand_new_database` cubre el foco de revisión 4: la API crea PostGIS e inserta puntos sin reiniciar. `Demo_reset_keeps_completed_simulation_evidence_for_spec_005` fija la conservación del resultado al reiniciar la demo.

- [ ] **Step 6: Empaquetar fixtures y activar la demo en el despliegue**

`infra/docker/backend/Dockerfile` — en la etapa `runtime`, después de `COPY --from=build /app/publish .`:

```dockerfile
# Fixtures del reset de demo (spec 001); el contexto de build es la raíz del repositorio
COPY data/fixtures ./data/fixtures
```

`infra/compose/docker-compose.prod.yml` — en `istpetdev-backend.environment`, añadir:

```yaml
      Demo__Enabled: "true"
      Demo__FixturePath: /app/data/fixtures/demo-base
```

Las cuentas del entorno desplegado (`Demo__Accounts__0__Subject`, `Demo__Accounts__0__DisplayName`, `Demo__Accounts__0__Role`, …) se definen en el `.env` del host, que no se versiona.

```bash
docker build -f infra/docker/backend/Dockerfile -t istpetdev-backend:spec001 .
docker run --rm --entrypoint ls istpetdev-backend:spec001 /app/data/fixtures/demo-base
```

Expected: lista `bodegas.csv consumos.csv inventario.csv lotes.csv mapping.json productos.csv puntos.csv vehiculos.csv`.

- [ ] **Step 7: Commit**

```bash
git add src tests infra
git commit -m "feat(api): endpoints de importación y reset de demo; fixtures en la imagen"
```

---

### Task 8: Pruebas E2E del recorrido de datos

**Files:**
- Test: `tests/IstpetDev.Tests/E2E/DatasetJourneyTests.cs`

**Interfaces:**
- Consumes: `ApiFactory`, `TestTokens`, `Fixtures` (Tasks 5–6); endpoints de Task 7.
- Produces: escenarios E2E-01 y E2E-02 de [`test-e2e.md`](test-e2e.md) automatizados.

- [ ] **Step 1: Escribir la prueba E2E**

`tests/IstpetDev.Tests/E2E/DatasetJourneyTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Imports;
using IstpetDev.Tests.Support;

namespace IstpetDev.Tests.E2E;

[Collection(PostgisCollection.Name)]
[Trait("Category", "E2E")]
public class DatasetJourneyTests(PostgisContainer postgres)
{
    private static MultipartFormDataContent Upload(string fixture)
    {
        var folder = Fixtures.Folder(fixture);
        var profileJson = File.ReadAllText(Path.Combine(folder, "mapping.json"));
        var content = new MultipartFormDataContent { { new StringContent(profileJson), "profile" } };
        foreach (var file in MappingProfile.Parse(profileJson).Files.Values)
            content.Add(new ByteArrayContent(File.ReadAllBytes(Path.Combine(folder, file.File!))), "files", file.File!);
        return content;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<JsonElement>())!;
    }

    // E2E-01 + E2E-02: demo lista → dataset ajeno validado en seco → aplicado → reimportado sin duplicar.
    [Fact]
    public async Task Planner_resets_demo_then_adds_an_external_dataset_without_code_changes()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);

        var reset = await Json(await planner.PostAsync("/api/v1/demo/reset", null));
        Assert.True(reset.GetProperty("applied").GetBoolean());
        Assert.Equal(6, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());

        var dryRun = await Json(await planner.PostAsync("/api/v1/imports", Upload("ejemplo-externo")));
        Assert.Equal(0, dryRun.GetProperty("errorCount").GetInt32());
        Assert.Equal(6, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());

        var applied = await Json(await planner.PostAsync("/api/v1/imports?dryRun=false", Upload("ejemplo-externo")));
        Assert.True(applied.GetProperty("applied").GetBoolean());
        Assert.Equal(8, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());

        var again = await Json(await planner.PostAsync("/api/v1/imports?dryRun=false", Upload("ejemplo-externo")));
        Assert.All(again.GetProperty("entities").EnumerateArray(), e => Assert.Equal(0, e.GetProperty("created").GetInt32()));
        Assert.Equal(8, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());

        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);
        Assert.Equal(6, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());
    }
}
```

- [ ] **Step 2: Ejecutar la prueba**

Run: `dotnet test tests/IstpetDev.Tests --filter "Category=E2E"`
Expected: PASS. Esta prueba no falla primero: cubre un recorrido compuesto por piezas ya probadas en Tasks 5–7. Si falla, la causa está en la integración entre ellas (por ejemplo, el reset no borra los puntos importados después).

- [ ] **Step 3: Ejecutar la suite completa**

Run: `dotnet test IstpetDev.sln`
Expected: PASS de todas las categorías.

- [ ] **Step 4: Commit**

```bash
git add tests
git commit -m "test(e2e): recorrido de reset de demo e importación de dataset externo"
```

---

### Task 9: Registro en la bóveda (ADR-21 y contradicciones)

**Files:**
- Modify: `docs/boveda/03 Arquitectura/Decisiones de arquitectura.md`
- Modify: `docs/boveda/00 Inicio/Hechos canonicos.md`
- Modify: `docs/boveda/06 Equipo/Registro de trabajo previo y del evento.md`
- Modify: `docs/specs/001-nucleo-delgado-importador/spec.md` (estado)

**Interfaces:**
- Consumes: decisión del equipo de Task 0 (Deciders y fecha).
- Produces: ADR-21 registrada; contradicciones visibles en Hechos canonicos.

- [ ] **Step 1: Actualizar ADR-21 en `Decisiones de arquitectura.md`**

ADR-21 ya está registrada como **propuesta** desde el 12-oct (fila de la tabla y sección «ADR-21 — núcleo delgado para la demo»). Con la decisión de Task 0:

- En la fila `| ADR-21 |`, reemplazar «**Propuesta, 2026-10-12** (Deciders PENDIENTE; …)» por «**Aceptada por <Deciders>, <fecha>**» y conservar el resto del texto.
- En la sección, cambiar `**Estado:** propuesta` por `**Estado:** aceptado` y `**Deciders:** PENDIENTE (…)` por los nombres reales.
- Si el equipo no la acepta, marcarla `reemplazado` o dejarla como propuesta con el motivo, y **no** ejecutar este spec.

- [ ] **Step 2: Cerrar las contradicciones en `Hechos canonicos.md`**

Las filas ya existen desde el 12-oct. Con la decisión tomada:

- En «Estado de las decisiones (ADR)», actualizar la fila `Núcleo delgado para la demo | ADR-21` con el estado y los Deciders.
- En «Contradicciones abiertas», quitar la fila «Alcance de RBAC/configuración para la demo» y anotar la resolución al final de la sección («Cerrada el <fecha> por ADR-21»), como se hizo con las del 7 de octubre.
- En «Nombres en código», quitar «⚠️ propuesta» de `ImportBatch` y `AgentDecision`.
- Actualizar `actualizado:` en la cabecera.

- [ ] **Step 3: Registrar el trabajo en `Registro de trabajo previo y del evento.md`**

Añadir al final:

```markdown
## Registro de preparación — 13 de octubre de 2026

| Fecha | Fase | Resultado | Evidencia | Estado |
|---|---|---|---|---|
| 2026-10-13 | Preparación | Núcleo delgado de 27 tablas, roles fijos e importador canónico con reset de demo (spec 001) | PR de `feature/001-nucleo-delgado-importador`, tag `schema95-archivo`, ejecución de [test-e2e](../../specs/001-nucleo-delgado-importador/test-e2e.md) | <completar con el resultado> |
```

- [ ] **Step 4: Verificar la bóveda**

Run: `python docs/boveda/.github/scripts/check_vault.py`
Expected: termina sin errores.

- [ ] **Step 5: Marcar el spec y hacer commit**

En `docs/specs/001-nucleo-delgado-importador/spec.md`, cambiar `estado:` a `cerrado` solo si `test-e2e.md` tiene todas las filas obligatorias aprobadas; si no, `en curso`.

```bash
git add docs
git commit -m "docs(boveda): registrar ADR-21 núcleo delgado y contradicciones del spec 001"
```

- [ ] **Step 6: Publicar y abrir el PR a `develop`**

Según [CONTRIBUTING.md](../../../CONTRIBUTING.md): antes de pedir revisión, `npm run check` y `dotnet test IstpetDev.sln` en local; si `develop` avanzó, `git pull --rebase origin develop`.

```bash
git push -u origin feature/001-nucleo-delgado-importador
gh pr create --base develop --title "feat(api): núcleo delgado e importador genérico (spec 001)" \
  --body "Implementa docs/specs/001-nucleo-delgado-importador. Pruebas: dotnet test IstpetDev.sln (unitarias, integración y E2E). Evidencia en test-e2e.md."
```

Expected: el check **Validar rama de origen** en verde. Tras el merge, borrar la rama a mano (CONTRIBUTING.md §6).
