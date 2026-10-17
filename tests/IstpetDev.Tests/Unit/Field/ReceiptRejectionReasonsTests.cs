using IstpetDev.Domain.Common;

namespace IstpetDev.Tests.Unit.Field;

// #236: catálogo V2 con ErrorRotulado; los motivos de V1 no cambian.
[Trait("Category", "Unit")]
public sealed class ReceiptRejectionReasonsTests
{
    [Theory]
    [InlineData("mislabeled")]
    [InlineData("wrong_label")]
    [InlineData("error_rotulado")]
    [InlineData("ErrorRotulado")]
    [InlineData("  MISLABELED  ")]
    public void Synonyms_normalize_to_ErrorRotulado(string input)
    {
        Assert.True(ReceiptRejectionReasons.TryNormalize(input, out var canonical));
        Assert.Equal(ReceiptRejectionReasons.ErrorRotulado, canonical);
        Assert.True(ReceiptRejectionReasons.IsValid(input));
    }

    [Theory]
    [InlineData("shortage", ReceiptRejectionReasons.Faltante)]
    [InlineData("surplus", ReceiptRejectionReasons.Sobrante)]
    [InlineData("dano", ReceiptRejectionReasons.Daño)]
    [InlineData("deteriorado", ReceiptRejectionReasons.Daño)]
    [InlineData("broken_seal", ReceiptRejectionReasons.PrecintoViolado)]
    [InlineData("wrong_lot", ReceiptRejectionReasons.LoteNoCoincide)]
    [InlineData("temperatura", ReceiptRejectionReasons.CadenaDeFrio)]
    public void Previous_reasons_keep_their_normalization(string input, string expected)
    {
        Assert.True(ReceiptRejectionReasons.TryNormalize(input, out var canonical));
        Assert.Equal(expected, canonical);
    }

    [Fact]
    public void Catalog_is_V2_and_keeps_V1_codes_in_order_with_ErrorRotulado_last()
    {
        Assert.Equal("V2", ReceiptRejectionReasons.Version);
        Assert.Equal(
            ["Faltante", "Sobrante", "Daño", "PrecintoViolado", "LoteNoCoincide", "CadenaDeFrio", "ErrorRotulado"],
            ReceiptRejectionReasons.All);
        Assert.Equal(
            "El rótulo o la identificación del empaque no coincide con el producto o el despacho",
            ReceiptRejectionReasons.GetDescription(ReceiptRejectionReasons.ErrorRotulado));
    }

    [Fact]
    public void Unknown_text_is_not_a_catalog_reason()
    {
        Assert.False(ReceiptRejectionReasons.TryNormalize("etiqueta rara", out _));
        Assert.False(ReceiptRejectionReasons.IsValid("etiqueta rara"));
    }
}
