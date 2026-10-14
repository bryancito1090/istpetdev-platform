using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Accounts;
using IstpetDev.Domain.Common;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Integration.Accounts;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
public sealed partial class AccountsApiTests(PostgisContainer postgres)
{
    private const string Accounts = "/api/v1/accounts";
    private readonly PostgisContainer postgres = postgres;

    private async Task<(ApiFactory Api, HttpClient Manager, Guid ManagerId, string Connection)> ManagerAsync()
    {
        var connection = postgres.NewDatabase();
        var api = new ApiFactory(connection);
        var manager = api.ClientFor(TestTokens.Planner);
        var managerId = await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, Roles.Gerencia);
        api.Directory.Add(TestTokens.Planner, "gerencia@ejemplo.test");
        api.Directory.Add(TestTokens.Driver, "conductor@ejemplo.test");
        return (api, manager, managerId, connection);
    }

    private static async Task<string?> CodeOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    [Theory]
    [InlineData(Roles.Planificador)]
    [InlineData(Roles.Auditor)]
    [InlineData(Roles.Conductor)]
    public async Task Only_gerencia_manages_accounts(string role)
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Driver);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Driver, role);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Accounts)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Accounts, new { email = "x@ejemplo.test", displayName = "X", role = "Conductor" })).StatusCode);
    }

    [Fact]
    public async Task Actor_without_account_cannot_manage_accounts()
    {
        var (api, _, _, _) = await ManagerAsync();
        await using var _api = api;
        using var client = api.ClientFor("unknown-subject");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Accounts)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Accounts, new { email = "x@ejemplo.test", displayName = "X", role = Roles.Conductor })).StatusCode);
    }

    [Fact]
    public async Task Create_sends_invitation_and_lists_pending_then_active()
    {
        var (api, manager, _, _) = await ManagerAsync();
        await using var _api = api;

        var created = await manager.PostAsJsonAsync(Accounts, new { email = "Nuevo.Conductor@ejemplo.test", displayName = "Conductor nuevo", role = Roles.Conductor });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var item = await created.Content.ReadFromJsonAsync<AccountItem>();
        Assert.Equal(("nuevo.conductor@ejemplo.test", "pending", true, false), (item!.Email, item.InvitationStatus, item.IsActive, item.OutOfSync));
        Assert.Equal(1, api.Directory.Invitations);
        var json = await created.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);

        api.Directory.Confirm(api.Directory.Users.Single(u => u.Email == "nuevo.conductor@ejemplo.test").Subject);
        var list = await manager.GetFromJsonAsync<AccountItem[]>(Accounts);
        Assert.Equal("active", list!.Single(a => a.Id == item.Id).InvitationStatus);
        Assert.True(list!.Single(a => a.Role == Roles.Gerencia).IsActive);
    }

    [Fact]
    public async Task Existing_email_is_rejected_without_creating_anything()
    {
        var (api, manager, _, connection) = await ManagerAsync();
        await using var _api = api;

        var response = await manager.PostAsJsonAsync(Accounts, new { email = "CONDUCTOR@ejemplo.test", displayName = "Otra vez", role = Roles.Conductor });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("account_exists", await CodeOf(response));
        Assert.Equal(0, api.Directory.Invitations);
        await using var db = TestDb.Context(connection);
        Assert.Equal(1, await db.Accounts.CountAsync(a => a.Subject == TestTokens.Driver));
    }

    [Theory]
    [InlineData(Roles.Automatizacion)]
    [InlineData("Admin")]
    public async Task Invalid_role_is_422(string role)
    {
        var (api, manager, _, _) = await ManagerAsync();
        await using var _api = api;

        var response = await manager.PostAsJsonAsync(Accounts, new { email = "rol@ejemplo.test", displayName = "Rol", role });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("invalid_role", await CodeOf(response));
        Assert.Equal(0, api.Directory.Invitations);
    }

    [Theory]
    [InlineData("x\"@ejemplo.test", "Nombre", "invalid_email")]
    [InlineData("correo@ejemplo.test", "   ", "invalid_display_name")]
    public async Task Invalid_input_has_problem_code(string email, string displayName, string code)
    {
        var (api, manager, _, _) = await ManagerAsync();
        await using var _api = api;

        var response = await manager.PostAsJsonAsync(Accounts, new { email, displayName, role = Roles.Conductor });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(code, await CodeOf(response));
        Assert.Equal(0, api.Directory.Invitations);
    }

    [Fact]
    public async Task Cognito_down_is_503_and_leaves_no_changes()
    {
        var (api, manager, _, connection) = await ManagerAsync();
        await using var _api = api;
        api.Directory.Unavailable = true;

        var create = await manager.PostAsJsonAsync(Accounts, new { email = "caida@ejemplo.test", displayName = "Caída", role = Roles.Bodega });
        var list = await manager.GetAsync(Accounts);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, create.StatusCode);
        Assert.Equal("identity_unavailable", await CodeOf(create));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, list.StatusCode);
        Assert.Equal("identity_unavailable", await CodeOf(list));
        Assert.DoesNotContain("Cognito simulado", await create.Content.ReadAsStringAsync());
        await using var db = TestDb.Context(connection);
        Assert.False(await db.Accounts.AnyAsync(a => a.DisplayName == "Caída"));
    }
}
