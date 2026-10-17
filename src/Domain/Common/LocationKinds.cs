namespace IstpetDev.Domain.Common;

public static class LocationKinds
{
    public const string Warehouse = "Warehouse";
    public const string ServicePoint = "ServicePoint";
    public const string Vehicle = "Vehicle";
    public const string Quarantine = "Quarantine";

    public static readonly IReadOnlyList<string> All = [Warehouse, ServicePoint, Vehicle, Quarantine];
}
