using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace IstpetDev.Api.Authentication;

public static class CognitoJwtExtensions
{
    public static IServiceCollection AddCognitoAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<IConfiguration>((options, effective) =>
        {
            var authority = effective["Authentication:Authority"];
            if (string.IsNullOrWhiteSpace(authority))
            {
                authority = DevJwtSecurity.DefaultAuthority;
            }

            var isLocalDev = authority.StartsWith("https://identity.istpetdev.local", StringComparison.OrdinalIgnoreCase) || authority.Contains("localhost");

            options.MapInboundClaims = false;
            options.Authority = authority;

            if (isLocalDev)
            {
                var metadata = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration { Issuer = authority };
                metadata.SigningKeys.Add(DevJwtSecurity.DevKey);
                options.ConfigurationManager = new Microsoft.IdentityModel.Protocols.StaticConfigurationManager<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration>(metadata);
                options.RequireHttpsMetadata = false;
            }
            else
            {
                options.MetadataAddress = $"{authority.TrimEnd('/')}/.well-known/openid-configuration";
                options.RequireHttpsMetadata = true;
            }

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = authority,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = isLocalDev ? DevJwtSecurity.DevKey : null,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ClockSkew = TimeSpan.FromMinutes(1),
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var principal = context.Principal;
                    var client = principal?.FindFirst("client_id")?.Value;
                    var configuredAllowed = effective.GetSection("Authentication:AllowedClientIds").Get<string[]>() ?? [];
                    var defaultAllowed = new[] { "istpetdev-web-client", "istpetdev-mobile-client", "istpetdev-web-portal", "istpetdev-mobile-pwa" };
                    var allowed = configuredAllowed.Length > 0 ? configuredAllowed : defaultAllowed;
                    var technical = effective.GetSection("Authentication:TechnicalClientIds").Get<string[]>() ?? [];
                    var scopes = principal?.FindFirst("scope")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
                    if (principal?.FindFirst("token_use")?.Value != "access")
                    {
                        context.Fail("Only access tokens are authorized.");
                    }
                    else if (client is null || !allowed.Contains(client))
                    {
                        context.Fail("Unauthorized client_id.");
                    }
                    else
                    {
                        var isTechnical = technical.Contains(client);
                        if (isTechnical)
                        {
                            var technicalScope = effective["Authentication:TechnicalScope"] ?? effective["Authentication:TechnicalReadScope"] ?? "istpetdev-api/automation";
                            if (!scopes.Contains(technicalScope) && !scopes.Contains("istpetdev-api/automation") && !scopes.Contains("automation/risk.read"))
                            {
                                context.Fail("Required technical API scope is missing.");
                            }
                        }
                        else if (!scopes.Contains(effective["Authentication:HumanApiScope"] ?? "istpetdev-api/access"))
                        {
                            context.Fail("Required API scope is missing.");
                        }
                    }
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("CognitoJwtValidator")
                        .LogWarning("Authentication failed: {ErrorType} - {ErrorMessage}", context.Exception.GetType().Name, context.Exception.Message);
                    return Task.CompletedTask;
                },
            };
        });
        services.AddAuthorization();
        return services;
    }
}
