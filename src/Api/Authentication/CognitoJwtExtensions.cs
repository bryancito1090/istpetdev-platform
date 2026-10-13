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
            options.MapInboundClaims = false;
            if (string.IsNullOrWhiteSpace(authority))
            {
                options.Events = new JwtBearerEvents { OnMessageReceived = context => { context.Fail("Authentication is not configured."); return Task.CompletedTask; } };
                return;
            }
            options.Authority = authority;
            options.MetadataAddress = $"{authority.TrimEnd('/')}/.well-known/openid-configuration";
            options.RequireHttpsMetadata = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = authority, ValidateAudience = false,
                ValidateLifetime = true, ValidateIssuerSigningKey = true,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256], ClockSkew = TimeSpan.FromMinutes(1),
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var principal = context.Principal;
                    var client = principal?.FindFirst("client_id")?.Value;
                    var allowed = effective.GetSection("Authentication:AllowedClientIds").Get<string[]>() ?? [];
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
                        .LogWarning("Authentication failed: {ErrorType}", context.Exception.GetType().Name);
                    return Task.CompletedTask;
                },
            };
        });
        services.AddAuthorization();
        return services;
    }
}
