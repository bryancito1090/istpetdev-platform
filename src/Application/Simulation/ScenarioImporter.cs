using IstpetDev.Application.Imports;

namespace IstpetDev.Application.Simulation;

public sealed class ScenarioImporter : IScenarioImporter
{
    private readonly IImportService _importService;

    public ScenarioImporter(IImportService importService)
    {
        _importService = importService;
    }

    public async Task ImportInitialStateAsync(
        string canonicalDirectory,
        string simulationConnection,
        CancellationToken cancellationToken)
    {
        // 1. Validar que la cadena de conexión exista
        if (string.IsNullOrWhiteSpace(simulationConnection))
        {
            throw new ArgumentException("simulationConnection cannot be empty", nameof(simulationConnection));
        }

        // 2. Cargar mapping profile
        var mappingPath = Path.Combine(canonicalDirectory, "mapping.json");
        MappingProfile profile;
        if (File.Exists(mappingPath))
        {
            var mappingJson = await File.ReadAllTextAsync(mappingPath, cancellationToken);
            profile = MappingProfile.Parse(mappingJson);
        }
        else
        {
            profile = new MappingProfile
            {
                Name = "simulation-canonical",
                Delimiter = ',',
                Files = new Dictionary<string, FileMapping>
                {
                    ["warehouses"] = new() { File = "warehouses.csv" },
                    ["service_points"] = new() { File = "service_points.csv" },
                    ["products"] = new() { File = "products.csv" },
                    ["vehicles"] = new() { File = "vehicles.csv" },
                    ["lots"] = new() { File = "lots.csv" },
                    ["stock"] = new() { File = "stock.csv" },
                    ["consumptions"] = new() { File = "consumptions.csv" },
                    ["road_segments"] = new() { File = "road_segments.csv" }
                }
            };
        }

        // 3. Preparar los archivos canónicos para el importador
        var files = new List<ImportFile>();
        foreach (var (key, fileMapping) in profile.Files)
        {
            if (fileMapping.File is not null && ImportEntityKeys.TryParse(key, out var entity))
            {
                var filePath = Path.Combine(canonicalDirectory, fileMapping.File);
                if (File.Exists(filePath))
                {
                    var fileStream = File.OpenRead(filePath);
                    files.Add(new ImportFile(entity, fileMapping.File, fileStream));
                }
            }
        }

        if (files.Count < 7)
        {
            foreach (var f in files) f.Content.Dispose();
            throw new InvalidOperationException($"El directorio canónico {canonicalDirectory} debe incluir al menos las siete entidades básicas.");
        }

        try
        {
            var report = await _importService.RunAsync(
                new ImportRequest(files, profile, DryRun: false, AllowPartial: false, ActorAccountId: null),
                cancellationToken);

            if (!report.Applied)
            {
                throw new InvalidOperationException($"La importación del estado inicial falló en {canonicalDirectory}");
            }
        }
        finally
        {
            foreach (var f in files)
            {
                f.Content.Dispose();
            }
        }
    }
}
