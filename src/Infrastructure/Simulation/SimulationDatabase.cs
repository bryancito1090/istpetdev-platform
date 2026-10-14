using System.Data.Common;
using Npgsql;

namespace IstpetDev.Infrastructure.Simulation;

public sealed class SimulationIsolationException : InvalidOperationException
{
    public SimulationIsolationException(string message) : base(message) { }
}

public static class SimulationDatabase
{
    public const string NotIsolatedErrorCode = "SIMULATION_DATABASE_NOT_ISOLATED";

    public static void ValidateIsolation(string operationalConnectionString, string? simulationConnectionString)
    {
        if (string.IsNullOrWhiteSpace(simulationConnectionString))
        {
            throw new SimulationIsolationException($"{NotIsolatedErrorCode}: La cadena de conexión de simulación no está configurada.");
        }

        if (string.Equals(operationalConnectionString.Trim(), simulationConnectionString.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new SimulationIsolationException($"{NotIsolatedErrorCode}: La base de simulación no puede ser la misma que la base operativa.");
        }

        try
        {
            var opBuilder = new NpgsqlConnectionStringBuilder(operationalConnectionString);
            var simBuilder = new NpgsqlConnectionStringBuilder(simulationConnectionString);

            var sameHost = string.Equals(opBuilder.Host, simBuilder.Host, StringComparison.OrdinalIgnoreCase);
            var samePort = opBuilder.Port == simBuilder.Port;
            var sameDatabase = string.Equals(opBuilder.Database, simBuilder.Database, StringComparison.OrdinalIgnoreCase);

            if (sameHost && samePort && sameDatabase)
            {
                throw new SimulationIsolationException($"{NotIsolatedErrorCode}: La base de simulación '{simBuilder.Database}' apunta a la misma base física que la operativa.");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Si el formato de conexión falla, evitar arrancar en falso
            throw new InvalidOperationException($"{NotIsolatedErrorCode}: Error al parsear cadenas de conexión: {ex.Message}", ex);
        }
    }
}
