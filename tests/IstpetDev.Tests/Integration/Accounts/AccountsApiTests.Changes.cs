using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Accounts;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Integration.Accounts;

public sealed partial class AccountsApiTests
{
    private static async Task<Guid> IdOf(string connection, string subject)
    {
        await using var db = TestDb.Context(connection);
        return await db.Accounts.Where(a => a.Subject == subject).Select(a => a.Id).SingleAsync();
    }

    [Theory]
    [InlineData(Roles.Planificador)]
    [InlineData(Roles.Auditor)]
    [InlineData(Roles.Conductor)]
    public async Task Only_gerencia_can_call_change_endpoints_without_side_effects(string role)
    {
        var (api, manager, _, connection) = await ManagerAsync();
        await using var _api = api;
        using var caller = api.ClientFor(TestTokens.Driver);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Driver, role);
        var id = await IdOf(connection, TestTokens.Planner);
        var original = api.Directory.Users.Single(u => u.Subject == TestTokens.Planner);

        Assert.Equal(HttpStatusCode.Forbidden, (await caller.PatchAsJsonAsync($"{Accounts}/{id}", new { displayName = "Alterado" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await caller.PostAsync($"{Accounts}/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await caller.PostAsync($"{Accounts}/{id}/reactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await caller.PostAsync($"{Accounts}/{id}/resend-invitation", null)).StatusCode);
        await using var db = TestDb.Context(connection);
        var stored = await db.Accounts.SingleAsync(a => a.Id == id);
        Assert.Equal("Planificación de prueba", stored.DisplayName);
        Assert.True(stored.IsActive);
        Assert.Equal(original, api.Directory.Users.Single(u => u.Subject == TestTokens.Planner));
        Assert.Empty(api.Directory.SignedOut);
        Assert.Equal(0, api.Directory.Resends);
    }

    [Fact]
    public async Task Deactivation_blocks_the_next_request_and_revokes_cognito_sessions()
    {
        var (api, manager, _, connection) = await ManagerAsync();
        await using var _api = api;
        using var driver = api.ClientFor(TestTokens.Driver);
        Assert.Equal(HttpStatusCode.OK, (await driver.GetAsync("/api/v1/me")).StatusCode);
        var id = await IdOf(connection, TestTokens.Driver);

        var response = await manager.PostAsync($"{Accounts}/{id}/deactivate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await driver.GetAsync("/api/v1/me")).StatusCode);
        Assert.False(api.Directory.Users.Single(u => u.Subject == TestTokens.Driver).Enabled);
        Assert.Contains(TestTokens.Driver, api.Directory.SignedOut);
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsync($"{Accounts}/{id}/reactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await driver.GetAsync("/api/v1/me")).StatusCode);
        Assert.True(api.Directory.Users.Single(u => u.Subject == TestTokens.Driver).Enabled);
    }

    [Fact]
    public async Task Cognito_down_during_deactivation_still_blocks_and_shows_the_gap()
    {
        var (api, manager, _, connection) = await ManagerAsync();
        await using var _api = api;
        using var driver = api.ClientFor(TestTokens.Driver);
        var id = await IdOf(connection, TestTokens.Driver);
        api.Directory.Unavailable = true;

        var response = await manager.PostAsync($"{Accounts}/{id}/deactivate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<AccountItem>();
        Assert.Equal((false, "unknown", true), (item!.IsActive, item.InvitationStatus, item.OutOfSync));
        Assert.Equal(HttpStatusCode.Forbidden, (await driver.GetAsync("/api/v1/me")).StatusCode);
        api.Directory.Unavailable = false;
        var retried = await (await manager.PostAsync($"{Accounts}/{id}/deactivate", null)).Content.ReadFromJsonAsync<AccountItem>();
        Assert.False(retried!.OutOfSync);
        Assert.False(api.Directory.Users.Single(u => u.Subject == TestTokens.Driver).Enabled);
        Assert.Contains(TestTokens.Driver, api.Directory.SignedOut);
    }

    [Fact]
    public async Task Cognito_down_during_reactivation_restores_inactive_account()
    {
        var (api, manager, _, connection) = await ManagerAsync();
        await using var _api = api;
        using var driver = api.ClientFor(TestTokens.Driver);
        var id = await IdOf(connection, TestTokens.Driver);
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsync($"{Accounts}/{id}/deactivate", null)).StatusCode);
        api.Directory.Unavailable = true;

        var response = await manager.PostAsync($"{Accounts}/{id}/reactivate", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("identity_unavailable", await CodeOf(response));
        Assert.DoesNotContain("Cognito simulado", await response.Content.ReadAsStringAsync());
        await using var db = TestDb.Context(connection);
        Assert.False((await db.Accounts.SingleAsync(a => a.Id == id)).IsActive);
        Assert.Equal(HttpStatusCode.Forbidden, (await driver.GetAsync("/api/v1/me")).StatusCode);
        api.Directory.Unavailable = false;
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsync($"{Accounts}/{id}/reactivate", null)).StatusCode);
    }

    [Fact]
    public async Task Gerencia_cannot_deactivate_or_change_the_role_of_its_own_account()
    {
        var (api, manager, managerId, _) = await ManagerAsync();
        await using var _api = api;
        var deactivate = await manager.PostAsync($"{Accounts}/{managerId}/deactivate", null);
        var demote = await manager.PatchAsJsonAsync($"{Accounts}/{managerId}", new { role = Roles.Planificador });
        Assert.Equal((HttpStatusCode.Conflict, "self_change_forbidden"), (deactivate.StatusCode, await CodeOf(deactivate)));
        Assert.Equal((HttpStatusCode.Conflict, "self_change_forbidden"), (demote.StatusCode, await CodeOf(demote)));
    }

    [Fact]
    public async Task The_last_active_gerencia_keeps_its_role()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var _ = api.ClientFor(TestTokens.Planner);
        var managerId = await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, Roles.Gerencia);
        using var scope = api.Services.CreateScope();
        var administration = scope.ServiceProvider.GetRequiredService<AccountAdministration>();
        var error = await Assert.ThrowsAsync<AccountRuleException>(() => administration.DeactivateAsync(Guid.NewGuid(), managerId, default));
        Assert.Equal("last_manager", error.Code);
    }

    [Fact]
    public async Task Role_change_moves_the_person_to_the_other_platform()
    {
        var (api, manager, _, connection) = await ManagerAsync();
        await using var _api = api;
        using var driver = api.ClientFor(TestTokens.Driver);
        var id = await IdOf(connection, TestTokens.Driver);
        var response = await manager.PatchAsJsonAsync($"{Accounts}/{id}", new { role = Roles.Auditor, displayName = "  Auditoría  " });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await driver.GetFromJsonAsync<JsonElement>("/api/v1/me");
        Assert.Equal((Roles.Auditor, "web", "Auditoría"), (me.GetProperty("role").GetString(), me.GetProperty("platform").GetString(), me.GetProperty("displayName").GetString()));
    }

    [Fact]
    public async Task Invalid_patch_name_has_problem_code_and_preserves_account()
    {
        var (api, manager, _, connection) = await ManagerAsync();
        await using var _api = api;
        var id = await IdOf(connection, TestTokens.Driver);
        var response = await manager.PatchAsJsonAsync($"{Accounts}/{id}", new { displayName = "   " });
        Assert.Equal((HttpStatusCode.UnprocessableEntity, "invalid_display_name"), (response.StatusCode, await CodeOf(response)));
        await using var db = TestDb.Context(connection);
        Assert.Equal("Conductor de prueba", (await db.Accounts.SingleAsync(a => a.Id == id)).DisplayName);
    }

    [Fact]
    public async Task Automation_cannot_be_assigned_or_managed_through_the_api()
    {
        var (api, manager, _, connection) = await ManagerAsync();
        await using var _api = api;
        var driverId = await IdOf(connection, TestTokens.Driver);
        var technical = new Account { Kind = "technical", Issuer = TestTokens.Issuer, Subject = "n8n-client", Role = Roles.Automatizacion, IsActive = true, DisplayName = "n8n" };
        await using (var db = TestDb.Context(connection)) { db.Add(technical); await db.SaveChangesAsync(); }
        var assign = await manager.PatchAsJsonAsync($"{Accounts}/{driverId}", new { role = Roles.Automatizacion });
        var touch = await manager.PostAsync($"{Accounts}/{technical.Id}/deactivate", null);
        Assert.Equal((HttpStatusCode.UnprocessableEntity, "invalid_role"), (assign.StatusCode, await CodeOf(assign)));
        Assert.Equal((HttpStatusCode.UnprocessableEntity, "technical_account"), (touch.StatusCode, await CodeOf(touch)));
    }

    [Fact]
    public async Task Invitation_is_resent_only_while_pending()
    {
        var (api, manager, _, connection) = await ManagerAsync();
        await using var _api = api;
        var created = await (await manager.PostAsJsonAsync(Accounts, new { email = "pendiente@ejemplo.test", displayName = "Pendiente", role = Roles.Receptor }))
            .Content.ReadFromJsonAsync<AccountItem>();
        var driverId = await IdOf(connection, TestTokens.Driver);
        var resend = await manager.PostAsync($"{Accounts}/{created!.Id}/resend-invitation", null);
        var done = await manager.PostAsync($"{Accounts}/{driverId}/resend-invitation", null);
        Assert.Equal(HttpStatusCode.NoContent, resend.StatusCode);
        Assert.Equal(string.Empty, await resend.Content.ReadAsStringAsync());
        Assert.Equal(1, api.Directory.Resends);
        Assert.Equal((HttpStatusCode.Conflict, "invitation_not_pending"), (done.StatusCode, await CodeOf(done)));
    }

    [Fact]
    public async Task Missing_account_has_not_found_problem_code_on_each_change_route()
    {
        var (api, manager, _, _) = await ManagerAsync();
        await using var _api = api;
        var id = Guid.NewGuid();
        var responses = new[]
        {
            await manager.PatchAsJsonAsync($"{Accounts}/{id}", new { displayName = "Persona" }),
            await manager.PostAsync($"{Accounts}/{id}/deactivate", null),
            await manager.PostAsync($"{Accounts}/{id}/reactivate", null),
            await manager.PostAsync($"{Accounts}/{id}/resend-invitation", null),
        };
        foreach (var response in responses)
            Assert.Equal((HttpStatusCode.NotFound, "not_found"), (response.StatusCode, await CodeOf(response)));
    }

    [Fact]
    public async Task Resend_with_unavailable_directory_is_503_without_raw_error()
    {
        var (api, manager, _, connection) = await ManagerAsync();
        await using var _api = api;
        var id = await IdOf(connection, TestTokens.Driver);
        api.Directory.Unavailable = true;
        var response = await manager.PostAsync($"{Accounts}/{id}/resend-invitation", null);
        Assert.Equal((HttpStatusCode.ServiceUnavailable, "identity_unavailable"), (response.StatusCode, await CodeOf(response)));
        Assert.DoesNotContain("Cognito simulado", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, api.Directory.Resends);
    }
}
