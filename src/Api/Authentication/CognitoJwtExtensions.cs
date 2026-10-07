using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace IstpetDev.Api.Authentication;

public static class CognitoJwtExtensions
{
    public static IServiceCollection AddCognitoAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authority = configuration["Authentication:Authority"];
        var allowedClientIds = configuration.GetSection("Authentication:AllowedClientIds").Get<string[]>() ?? Array.Empty<string>();

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });

        if (!string.IsNullOrWhiteSpace(authority))
        {
            authBuilder.AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.MetadataAddress = $"{authority.TrimEnd('/')}/.well-known/openid-configuration";
                options.RequireHttpsMetadata = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authority,
                    // Cognito access tokens do not include audience claim 'aud', client_id is used instead
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("CognitoJwtValidator");

                        var principal = context.Principal;
                        if (principal == null)
                        {
                            context.Fail("Missing security principal.");
                            return Task.CompletedTask;
                        }

                        // ADR-10 Rule: Ensure token_use is "access" (reject ID tokens)
                        var tokenUseClaim = principal.FindFirst("token_use")?.Value;
                        if (tokenUseClaim != "access")
                        {
                            logger.LogWarning("Rejected token: token_use is '{TokenUse}', expected 'access'", tokenUseClaim);
                            context.Fail("Invalid token_use. Only access tokens are authorized for API access.");
                            return Task.CompletedTask;
                        }

                        // ADR-10 Rule: Validate client_id against allowed list
                        var clientIdClaim = principal.FindFirst("client_id")?.Value;
                        if (allowedClientIds.Length > 0 && !allowedClientIds.Contains(clientIdClaim))
                        {
                            logger.LogWarning("Rejected token: client_id '{ClientId}' not in allowed list", clientIdClaim);
                            context.Fail("Unauthorized client_id.");
                            return Task.CompletedTask;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("CognitoJwtValidator");
                        logger.LogWarning("Authentication failed: {Message}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });
        }
        else
        {
            // If Authority is not yet configured, configure fallback JwtBearer so DI does not fail
            authBuilder.AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false
                };
            });
        }

        services.AddAuthorization();

        return services;
    }
}
