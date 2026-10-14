using IstpetDev.Application.Accounts;

namespace IstpetDev.Tests.Unit.Identity;

[Trait("Category", "Unit")]
public sealed class ProvisioningInputTests
{
    [Fact]
    public void Reads_rows_skipping_blank_lines_and_comments()
    {
        var input = ProvisioningInput.Parse(new StringReader("# cuentas demo\nemail, displayName, role\n\nana@ejemplo.test, Ana Bodega ,Bodega\nluis@ejemplo.test,Luis,Conductor\n"));

        Assert.Empty(input.Errors);
        Assert.Equal([new ProvisioningRow(4, "ana@ejemplo.test", "Ana Bodega", "Bodega"), new ProvisioningRow(5, "luis@ejemplo.test", "Luis", "Conductor")], input.Rows);
    }

    [Theory]
    [InlineData("", "El archivo está vacío.")]
    [InlineData("correo,nombre,rol\nana@ejemplo.test,Ana,Bodega", "La primera fila debe ser «email,displayName,role».")]
    [InlineData("email,displayName,role\nana@ejemplo.test,Ana, Bodega,Bodega", "Se esperan 3 columnas: correo, nombre y rol (el nombre no puede llevar comas).")]
    [InlineData("email,displayName,role\nana@ejemplo.test,Ana,Bodega\nANA@ejemplo.test,Ana,Bodega", "Correo repetido en el archivo.")]
    public void Reports_file_errors_without_echoing_personal_data(string content, string message)
    {
        var error = Assert.Single(ProvisioningInput.Parse(new StringReader(content)).Errors);
        Assert.Equal(message, error.Message);
    }
}
