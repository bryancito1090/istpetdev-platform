using IstpetDev.Domain.Common;

namespace IstpetDev.Tests.Unit.Identity;

[Trait("Category", "Unit")]
public sealed class RolesTests
{
    [Theory]
    [InlineData(Roles.Gerencia, "web")]
    [InlineData(Roles.Planificador, "web")]
    [InlineData(Roles.Auditor, "web")]
    [InlineData(Roles.Bodega, "field")]
    [InlineData(Roles.Conductor, "field")]
    [InlineData(Roles.Receptor, "field")]
    [InlineData(Roles.Automatizacion, null)]
    [InlineData("Admin", null)]
    [InlineData(null, null)]
    public void Platform_follows_adr_24(string? role, string? platform) => Assert.Equal(platform, Roles.PlatformOf(role));

    [Fact]
    public void Six_human_roles_and_automation_is_technical()
    {
        Assert.Equal(6, Roles.HumanRoles.Count);
        Assert.True(Roles.IsHuman(Roles.Auditor));
        Assert.False(Roles.IsHuman(Roles.Automatizacion));
        Assert.True(Roles.IsValid(Roles.Auditor));
        Assert.Contains(Roles.Auditor, Roles.Any.Split(','));
        Assert.DoesNotContain(Roles.Auditor, Roles.Planning.Split(','));
    }
}
