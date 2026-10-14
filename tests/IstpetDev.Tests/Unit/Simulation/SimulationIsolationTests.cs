using IstpetDev.Infrastructure.Simulation;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

public sealed class SimulationIsolationTests
{
    [Fact]
    public void Same_Connection_String_Throws_Simulation_Database_Not_Isolated()
    {
        var opConn = "Host=localhost;Port=5432;Database=istpetdev_demo;Username=postgres;Password=secret";
        var simConn = "Host=localhost;Port=5432;Database=istpetdev_demo;Username=postgres;Password=secret";

        var ex = Assert.Throws<SimulationIsolationException>(() =>
            SimulationDatabase.ValidateIsolation(opConn, simConn));

        Assert.Contains(SimulationDatabase.NotIsolatedErrorCode, ex.Message);
    }

    [Fact]
    public void Same_Database_With_Whitespace_Or_Case_Difference_Still_Throws()
    {
        var opConn = "Host=localhost;Port=5432;Database=istpetdev_demo;Username=postgres;Password=secret";
        var simConn = "Host=localhost;Port=5432;Database=ISTPETDEV_DEMO;Username=postgres;Password=secret  ";

        var ex = Assert.Throws<SimulationIsolationException>(() =>
            SimulationDatabase.ValidateIsolation(opConn, simConn));

        Assert.Contains(SimulationDatabase.NotIsolatedErrorCode, ex.Message);
    }

    [Fact]
    public void Empty_Simulation_Connection_Throws_Simulation_Database_Not_Isolated()
    {
        var opConn = "Host=localhost;Port=5432;Database=istpetdev_demo;Username=postgres;Password=secret";

        var ex = Assert.Throws<SimulationIsolationException>(() =>
            SimulationDatabase.ValidateIsolation(opConn, null));

        Assert.Contains(SimulationDatabase.NotIsolatedErrorCode, ex.Message);
    }

    [Fact]
    public void Distinct_Database_Passes_Isolation_Validation()
    {
        var opConn = "Host=localhost;Port=5432;Database=istpetdev_demo;Username=postgres;Password=secret";
        var simConn = "Host=localhost;Port=5432;Database=istpetdev_simulation;Username=postgres;Password=secret";

        // No debe lanzar excepción
        SimulationDatabase.ValidateIsolation(opConn, simConn);
    }
}
