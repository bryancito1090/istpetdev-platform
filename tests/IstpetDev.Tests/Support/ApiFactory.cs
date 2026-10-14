using System.Net.Http.Headers;
using IstpetDev.Application.Accounts;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace IstpetDev.Tests.Support;

public sealed class ApiFactory(string connectionString, bool demoEnabled = true, IReadOnlyDictionary<string, string?>? overrides = null) : WebApplicationFactory<Program>
{
    public FakeIdentityDirectory Directory { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Main"] = connectionString,
            ["Authentication:Authority"] = TestTokens.Issuer,
            ["Authentication:AllowedClientIds:0"] = TestTokens.ClientId,
            ["ApplyMigrations"] = "true",
            ["Demo:Enabled"] = demoEnabled.ToString(),
            ["Demo:FixturePath"] = Fixtures.Folder("demo-base"),
            ["Demo:Accounts:0:Subject"] = TestTokens.Planner,
            ["Demo:Accounts:0:DisplayName"] = "Planificación de prueba",
            ["Demo:Accounts:0:Role"] = Roles.Planificador,
            ["Demo:Accounts:1:Subject"] = TestTokens.Driver,
            ["Demo:Accounts:1:DisplayName"] = "Conductor de prueba",
            ["Demo:Accounts:1:Role"] = Roles.Conductor,
            ["Demo:Accounts:4:Subject"] = "driver-fixture-duplicate",
        }).AddInMemoryCollection(overrides ?? new Dictionary<string, string?>()));
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Sustituye solo la descarga de metadatos de Cognito; se conserva toda la validación de producción.
                var metadata = new OpenIdConnectConfiguration { Issuer = TestTokens.Issuer };
                metadata.SigningKeys.Add(TestTokens.Key);
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
            });
            // Ninguna prueba llama a AWS: el authority de pruebas tiene forma de Cognito real.
            services.RemoveAll<IIdentityDirectory>();
            services.AddSingleton<IIdentityDirectory>(Directory);
        });
    }

    public HttpClient ClientFor(string? subject, string tokenUse = "access")
    {
        var client = CreateClient();
        if (subject is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Create(subject, tokenUse));
        return client;
    }
}
