using System.Text.RegularExpressions;
using IstpetDev.Api.Accounts;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Integration.Accounts;

[Trait("Category", "Integration")]
public sealed class ProvisionAccountsCommandTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    [Fact]
    public async Task Running_twice_creates_once_and_reports_existing_accounts()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        var file = Path.Combine(Path.GetTempPath(), $"cuentas-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(file, "email,displayName,role\ngerencia@ejemplo.test,Gerencia inicial,Gerencia\nconductor@ejemplo.test,Conductor demo,Conductor\n");
        try
        {
            var first = new StringWriter();
            var second = new StringWriter();

            Assert.Equal(0, await ProvisionAccountsCommand.RunAsync(api.Services, file, first));
            Assert.Equal(0, await ProvisionAccountsCommand.RunAsync(api.Services, file, second));

            Assert.Equal(2, Regex.Matches(first.ToString(), "creada").Count);
            Assert.Equal(2, Regex.Matches(second.ToString(), "ya existía").Count);
            Assert.DoesNotContain("@", first + second.ToString());
            Assert.Equal(2, api.Directory.Invitations);
            var subjects = api.Directory.Users.Select(u => u.Subject).ToArray();
            await using var db = TestDb.Context(connection);
            var accounts = await db.Accounts.Where(a => subjects.Contains(a.Subject)).ToListAsync();
            Assert.Equal(2, accounts.Count);
            Assert.All(accounts, a => Assert.True(a.IsActive && a.Kind == "human" && a.Issuer == TestTokens.Issuer));
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task Invalid_file_creates_nothing()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var file = Path.Combine(Path.GetTempPath(), $"cuentas-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(file, "email,displayName,role\nana@ejemplo.test,Ana,Bodega,extra\n");
        try
        {
            var output = new StringWriter();
            Assert.Equal(2, await ProvisionAccountsCommand.RunAsync(api.Services, file, output));
            Assert.Contains("línea 2", output.ToString());
            Assert.Empty(api.Directory.Users);
        }
        finally { File.Delete(file); }
    }
}
