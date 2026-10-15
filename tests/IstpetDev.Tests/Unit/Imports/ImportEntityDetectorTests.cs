using IstpetDev.Application.Imports;
using Xunit;

namespace IstpetDev.Tests.Unit.Imports;

[Trait("Category", "Unit")]
public class ImportEntityDetectorTests
{
    [Theory]
    [InlineData("consumptions.csv", ImportEntity.Consumptions)]
    [InlineData("service_points.csv", ImportEntity.ServicePoints)]
    [InlineData("Consumos Septiembre 2026.xlsx", ImportEntity.Consumptions)]
    [InlineData("bodegas-quito.csv", ImportEntity.Warehouses)]
    [InlineData("Puntos de Servicio.csv", ImportEntity.ServicePoints)]
    [InlineData("road_segments.csv", ImportEntity.RoadSegments)]
    public void Recognizes_the_entity_from_the_file_name(string fileName, ImportEntity expected) =>
        Assert.Equal(expected, ImportEntityDetector.FromFileName(fileName));

    [Theory]
    [InlineData("datos.csv")]
    [InlineData("inventario_lotes.csv")] // dos entidades posibles: no adivina
    [InlineData("")]
    public void Does_not_guess_when_the_name_is_unknown_or_ambiguous(string fileName) =>
        Assert.Null(ImportEntityDetector.FromFileName(fileName));

    [Fact]
    public void Recognizes_consumptions_from_spanish_headers()
    {
        var detection = ImportEntityDetector.FromHeaders(["Código punto", "SKU", "Fecha", "Cantidad"]);
        Assert.Equal(ImportEntity.Consumptions, detection.Entity);
    }

    [Fact]
    public void Prefers_the_entity_that_explains_more_columns()
    {
        var detection = ImportEntityDetector.FromHeaders(
            ["code", "name", "lat", "lon", "region", "category", "criticality", "window_start", "window_end"]);
        Assert.Equal(ImportEntity.ServicePoints, detection.Entity);
    }

    [Fact]
    public void Reports_ambiguity_instead_of_guessing()
    {
        // Código, nombre y coordenadas sirven tanto para una bodega como para un punto de servicio.
        var detection = ImportEntityDetector.FromHeaders(["code", "name", "lat", "lon"]);
        Assert.Null(detection.Entity);
        Assert.Contains("encajan igual", detection.Explanation);
    }

    [Fact]
    public void Explains_what_is_missing_when_no_entity_fits()
    {
        var detection = ImportEntityDetector.FromHeaders(
            ["provincia", "canton", "tipo_establecimiento", "categoria", "establecimientos", "permisos"]);
        Assert.Null(detection.Entity);
        Assert.Contains("No se reconoce", detection.Explanation);
        Assert.Contains("perfil de mapeo", detection.Explanation);
    }
}
