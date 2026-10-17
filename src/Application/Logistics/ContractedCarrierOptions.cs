namespace IstpetDev.Application.Logistics;

public sealed class ContractedCarrierOptions
{
    public const string SectionName = "Logistics:ContractedCarrier";

    public decimal BaseFeeUsd { get; set; } = 25.00m;
    public decimal PerKmUsd { get; set; } = 0.65m;
    public decimal PerKgUsd { get; set; } = 0.05m;
    public int ResponseHours { get; set; } = 4;
    public string Name { get; set; } = "Operador Logístico Contratado";
    public bool IsEnabled { get; set; } = true;

    public bool IsConfigured => IsEnabled &&
                                !string.IsNullOrWhiteSpace(Name) &&
                                BaseFeeUsd >= 0m &&
                                PerKmUsd > 0m &&
                                PerKgUsd >= 0m &&
                                ResponseHours >= 0;
}
