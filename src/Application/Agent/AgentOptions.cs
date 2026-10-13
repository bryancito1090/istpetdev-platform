namespace IstpetDev.Application.Agent;

public sealed class AgentOptions
{
    public bool Enabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 10;
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";
    public bool AutoOnIncident { get; set; } = false;
}
