using IstpetDev.Application.Accounts;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IstpetDev.Tests.Integration.Accounts;

[Trait("Category", "Integration")]
public sealed class AccountProvisioningTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private async Task<string> MigratedAsync()
    {
        var connection = postgres.NewDatabase();
        await using var _ = await TestDb.CreateMigratedAsync(connection);
        return connection;
    }

    [Fact]
    public async Task Creates_the_directory_user_and_an_active_human_account()
    {
        var connection = await MigratedAsync();
        var directory = new FakeIdentityDirectory();
        await using var db = TestDb.Context(connection);

        var result = await new AccountProvisioning(db, directory).ProvisionAsync(" Ana@Ejemplo.TEST ", " Ana Bodega ", Roles.Bodega, default);

        Assert.Equal(ProvisionOutcome.Created, result.Outcome);
        var user = Assert.Single(directory.Users);
        Assert.Equal("ana@ejemplo.test", user.Email);
        var account = await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == result.AccountId);
        Assert.Equal(("human", TestTokens.Issuer, user.Subject, "Ana Bodega", Roles.Bodega, true),
            (account.Kind, account.Issuer, account.Subject, account.DisplayName, account.Role, account.IsActive));
    }

    [Fact]
    public async Task Same_email_in_any_case_is_reported_as_existing_without_a_second_invitation()
    {
        var connection = await MigratedAsync();
        var directory = new FakeIdentityDirectory();
        await using var db = TestDb.Context(connection);
        var provisioning = new AccountProvisioning(db, directory);

        await provisioning.ProvisionAsync("ana@ejemplo.test", "Ana", Roles.Bodega, default);
        var again = await provisioning.ProvisionAsync("ANA@ejemplo.test", "Ana", Roles.Bodega, default);

        Assert.Equal(ProvisionOutcome.AlreadyExists, again.Outcome);
        Assert.Equal(1, directory.Invitations);
        Assert.Equal(1, await db.Accounts.CountAsync(a => a.Kind == "human" && a.Issuer == TestTokens.Issuer));
    }

    [Fact]
    public async Task Same_email_with_another_role_is_rejected_and_keeps_the_original_role()
    {
        var connection = await MigratedAsync();
        var directory = new FakeIdentityDirectory();
        await using var db = TestDb.Context(connection);
        var provisioning = new AccountProvisioning(db, directory);
        await provisioning.ProvisionAsync("ana@ejemplo.test", "Ana", Roles.Bodega, default);

        var error = await Assert.ThrowsAsync<AccountRuleException>(
            () => provisioning.ProvisionAsync("ana@ejemplo.test", "Ana", Roles.Conductor, default));

        Assert.Equal("role_conflict", error.Code);
        Assert.Contains(Roles.Bodega, error.Message);
        Assert.Equal(Roles.Bodega, (await db.Accounts.AsNoTracking().SingleAsync(a => a.Kind == "human")).Role);
    }

    [Fact]
    public async Task Directory_user_without_account_is_reconciled_without_a_new_invitation()
    {
        var connection = await MigratedAsync();
        var directory = new FakeIdentityDirectory();
        directory.Add("sub-previo", "previo@ejemplo.test", DirectoryUserStatus.InvitationPending);
        await using var db = TestDb.Context(connection);

        var result = await new AccountProvisioning(db, directory).ProvisionAsync("previo@ejemplo.test", "Previo", Roles.Receptor, default);

        Assert.Equal(ProvisionOutcome.Reconciled, result.Outcome);
        Assert.Equal(0, directory.Invitations);
        Assert.True(await db.Accounts.AnyAsync(a => a.Subject == "sub-previo" && a.IsActive));
    }

    [Theory]
    [InlineData("ana\"x@ejemplo.test", "Ana", Roles.Bodega, "invalid_email")]
    [InlineData("sin-arroba", "Ana", Roles.Bodega, "invalid_email")]
    [InlineData("ana@ejemplo.test", "   ", Roles.Bodega, "invalid_display_name")]
    [InlineData("ana@ejemplo.test", "Ana", Roles.Automatizacion, "invalid_role")]
    [InlineData("ana@ejemplo.test", "Ana", "Admin", "invalid_role")]
    public async Task Invalid_input_is_rejected_before_touching_the_directory(string email, string name, string role, string code)
    {
        var connection = await MigratedAsync();
        var directory = new FakeIdentityDirectory();
        await using var db = TestDb.Context(connection);

        var error = await Assert.ThrowsAsync<AccountRuleException>(() => new AccountProvisioning(db, directory).ProvisionAsync(email, name, role, default));

        Assert.Equal(code, error.Code);
        Assert.Empty(directory.Users);
    }

    [Fact]
    public async Task Database_failure_after_creating_the_user_disables_it()
    {
        var connection = await MigratedAsync();
        var directory = new FakeIdentityDirectory();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connection, npgsql => npgsql.UseNetTopologySuite())
            .AddInterceptors(new FailingSave()).Options;
        await using var db = new ApplicationDbContext(options);

        await Assert.ThrowsAsync<DbUpdateException>(() => new AccountProvisioning(db, directory).ProvisionAsync("falla@ejemplo.test", "Falla", Roles.Conductor, default));

        Assert.False(Assert.Single(directory.Users).Enabled);
    }

    [Fact]
    public async Task Directory_down_leaves_no_account()
    {
        var connection = await MigratedAsync();
        var directory = new FakeIdentityDirectory { Unavailable = true };
        await using var db = TestDb.Context(connection);

        await Assert.ThrowsAsync<IdentityDirectoryUnavailableException>(() => new AccountProvisioning(db, directory).ProvisionAsync("caida@ejemplo.test", "Caída", Roles.Conductor, default));

        Assert.False(await db.Accounts.AnyAsync(a => a.Kind == "human" && a.DisplayName == "Caída"));
    }

    [Fact]
    public async Task Concurrent_requests_for_the_same_email_create_one_user_and_one_account()
    {
        var connection = await MigratedAsync();
        var directory = new FakeIdentityDirectory();
        async Task<ProvisionOutcome> Provision()
        {
            await using var db = TestDb.Context(connection);
            return (await new AccountProvisioning(db, directory).ProvisionAsync("doble@ejemplo.test", "Doble", Roles.Bodega, default)).Outcome;
        }

        var outcomes = await Task.WhenAll(Provision(), Provision());

        Assert.Equal([ProvisionOutcome.Created, ProvisionOutcome.AlreadyExists], outcomes.Order());
        var user = Assert.Single(directory.Users);
        Assert.True(user.Enabled);
        await using var check = TestDb.Context(connection);
        Assert.Equal(1, await check.Accounts.CountAsync(a => a.Subject == user.Subject));
    }

    private sealed class FailingSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("Fallo simulado de la base.");
    }
}
