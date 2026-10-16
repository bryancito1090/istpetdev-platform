namespace IstpetDev.Domain.Common;

// #162: cadena de custodia Bodega → Conductor → Receptor.
// El estado del bulto y de la entrega avanza en un solo sentido; cada transición
// la ejecuta un rol distinto y se registra en CustodyEvent.
public static class DeliveryStates
{
    public const string Draft = "Draft";
    public const string Dispatched = "Dispatched";
    public const string InTransit = "InTransit";
    public const string Delivered = "Delivered";
    public const string Received = "Received";
    public const string Partial = "Partial";
    public const string Cancelled = "Cancelled";

    // Recibo final previo o estado terminal: no admite una segunda recepción.
    public static bool IsReceived(string status) => status is Received;
}

public static class HandlingUnitStates
{
    public const string Sealed = "Sealed";

    // Legado anterior a #162: la preparación se guardaba como "Prepared".
    // Se acepta en guardas y se normaliza a Sealed en la siguiente transición.
    public const string Prepared = "Prepared";

    public const string Dispatched = "Dispatched";
    public const string InTransit = "InTransit";
    public const string Delivered = "Delivered";
    public const string Received = "Received";
    public const string Partial = "Partial";

    public static bool IsPrepared(string status) => status is Sealed or Prepared;
    public static bool IsReceived(string status) => status is Received or Partial;
}

// Tipos de evento de custodia persistidos en CustodyEvent.EventKind.
public static class CustodyEventKinds
{
    public const string Dispatched = "Dispatched";
    public const string PickedUp = "PickedUp";
    public const string Delivered = "Delivered";
    public const string Received = "Received";
    public const string Partial = "Partial";
}
