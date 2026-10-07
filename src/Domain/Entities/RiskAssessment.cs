using System;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Enums;

namespace IstpetDev.Domain.Entities;

public class RiskAssessment : BaseEntity
{
    public Guid ServicePointId { get; set; }
    public ServicePoint? ServicePoint { get; set; }
    public double Score { get; set; }
    public RiskLevel Level { get; set; } = RiskLevel.Low;
    public string Summary { get; set; } = string.Empty;
    public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
}
