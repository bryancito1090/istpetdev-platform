using System.Text.RegularExpressions;

namespace IstpetDev.Application.Logistics;

public interface IExplanationTemplates
{
    string Explain(string kind, IReadOnlyList<Fact> facts);
}

public sealed partial class ExplanationTemplates : IExplanationTemplates
{
    [GeneratedRegex(@"\{fact:([a-z_]+):([^}]+)\}")]
    private static partial Regex FactPlaceholderRegex();

    public string Explain(string kind, IReadOnlyList<Fact> facts)
    {
        var factsMap = facts.GroupBy(f => $"{f.Code}:{f.Subject}").ToDictionary(g => g.Key, g => g.First());

        string template = kind.ToLowerInvariant() switch
        {
            "risk" => "El punto crítico presenta cobertura estimada de {fact:coverage_days:critical_item}.",
            "plan" => "Ruta planificada con recorrido de {fact:route_km:total} y costo estimado de {fact:route_cost:total}.",
            "replan" => "Ruta recalculada con incremento de costo del {fact:cost_increase_pct:route} y desvío de {fact:max_delay_minutes:route}.",
            "escalation" => "Decisión escalada a operador debido a incremento de costo de {fact:cost_increase_pct:route}.",
            _ => throw new ArgumentException($"Tipo de plantilla desconocido: {kind}", nameof(kind))
        };

        return FactPlaceholderRegex().Replace(template, match =>
        {
            string code = match.Groups[1].Value;
            string subject = match.Groups[2].Value;
            string key = $"{code}:{subject}";

            if (factsMap.TryGetValue(key, out var fact))
            {
                return $"{fact.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} {fact.Unit}".Trim();
            }

            var byCode = facts.FirstOrDefault(f => f.Code == code);
            if (byCode != null)
            {
                return $"{byCode.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} {byCode.Unit}".Trim();
            }

            // Si el fact no existe, no inventar valor
            return "[DATO_NO_DISPONIBLE]";
        });
    }
}
