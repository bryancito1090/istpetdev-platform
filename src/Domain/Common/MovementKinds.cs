namespace IstpetDev.Domain.Common;

public static class MovementKinds
{
    public const string ImportAdjustment = "ImportAdjustment";

    // Diferencia entre el stock en percha declarado al recibir y el saldo del punto (#111).
    public const string FieldAdjustment = "FieldAdjustment";

    // Devolución de bultos o saldo no entregado a la bodega de origen (#247).
    public const string Return = "Return";
}
