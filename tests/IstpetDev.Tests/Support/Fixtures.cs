using System.Text.Json;
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

    public static JsonElement IncidentPreset(string fixture, string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Folder(fixture), "incidentes.json")));
        return document.RootElement.GetProperty(name).Clone();
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
