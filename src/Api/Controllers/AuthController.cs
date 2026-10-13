using System.Security.Claims;
using IstpetDev.Api.Authentication;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Inicia sesión emitiendo un Access Token JWT con el rol institucional solicitado o validado.
    /// Soporta inicio con credenciales o selección de perfiles de demostración (Expo Clean Ecuador 2026).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var requestedRole = request.Role?.Trim();

        string resolvedRole;
        string subject;
        string displayName;

        // 1. Detección por rol directo solicitado (Demostración / Pruebas rápidas)
        if (!string.IsNullOrEmpty(requestedRole))
        {
            var normalizedRole = NormalizeRoleName(requestedRole);
            if (!Roles.IsValid(normalizedRole))
            {
                return BadRequest(new { message = $"Rol '{requestedRole}' no reconocido. Válidos: {string.Join(", ", Roles.All)}" });
            }

            resolvedRole = normalizedRole;
            subject = GetSubjectForRole(resolvedRole);
            displayName = GetDisplayNameForRole(resolvedRole);
        }
        // 2. Detección por correo institucional
        else if (!string.IsNullOrEmpty(email))
        {
            if (email.Contains("supervis") || email.Contains("planificad") || email.Contains("andrea"))
            {
                resolvedRole = Roles.Planificador;
                subject = "planner-1";
                displayName = "Dra. Andrea Morales (Planificación)";
            }
            else if (email.Contains("admin") || email.Contains("gerenc") || email.Contains("bryan"))
            {
                resolvedRole = Roles.Gerencia;
                subject = "admin-1";
                displayName = "Bryan L. (Gerencia y Plataforma)";
            }
            else if (email.Contains("bodega") || email.Contains("warehouse") || email.Contains("elena"))
            {
                resolvedRole = Roles.Bodega;
                subject = "warehouse-1";
                displayName = "Elena Bodega (Operador de Almacén)";
            }
            else if (email.Contains("receptor") || email.Contains("hospital") || email.Contains("laura"))
            {
                resolvedRole = Roles.Receptor;
                subject = "receiver-1";
                displayName = "Receptor Hospital Laura (Farmacia)";
            }
            else
            {
                // Conductor de Ruta por defecto para personal de transporte
                resolvedRole = Roles.Conductor;
                subject = "driver-1";
                displayName = "Carlos Mendoza (Conductor C-01)";
            }
        }
        else
        {
            return BadRequest(new { message = "Se requiere proporcionar email o rol institucional." });
        }

        var issuer = configuration["Authentication:Authority"] ?? DevJwtSecurity.DefaultAuthority;
        var token = DevJwtSecurity.CreateToken(
            subject: subject,
            role: resolvedRole,
            clientId: "istpetdev-web-client",
            issuer: issuer
        );

        return Ok(new LoginResponse(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: 28800, // 8 horas
            User: new AuthUserDto(
                Id: subject,
                Email: string.IsNullOrEmpty(email) ? $"{subject}@istpetdev.ec" : email,
                Name: displayName,
                Role: resolvedRole,
                RoleLabel: GetRoleLabel(resolvedRole),
                Facility: GetFacilityForRole(resolvedRole)
            )
        ));
    }

    /// <summary>
    /// Consulta el perfil de la cuenta actualmente autenticada mediante su JWT Bearer.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Roles = Roles.Any)]
    public IActionResult GetCurrentUser()
    {
        var subject = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var accountId = User.AccountId();

        return Ok(new
        {
            id = accountId?.ToString() ?? subject,
            subject = subject,
            role = role,
            roleLabel = GetRoleLabel(role ?? string.Empty),
            clientId = User.FindFirst("client_id")?.Value
        });
    }

    /// <summary>
    /// Cierra la sesión activa.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        return Ok(new { success = true, message = "Sesión finalizada exitosamente." });
    }

    private static string NormalizeRoleName(string role)
    {
        var r = role.Trim();
        if (string.Equals(r, "SUPERVISOR", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "PLANNER", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "Planificador", StringComparison.OrdinalIgnoreCase))
            return Roles.Planificador;

        if (string.Equals(r, "ADMIN", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "Gerencia", StringComparison.OrdinalIgnoreCase))
            return Roles.Gerencia;

        if (string.Equals(r, "WarehouseOperator", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "OPERADOR", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "Bodega", StringComparison.OrdinalIgnoreCase))
            return Roles.Bodega;

        if (string.Equals(r, "CarrierDriver", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "CHOFER", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "Conductor", StringComparison.OrdinalIgnoreCase))
            return Roles.Conductor;

        if (string.Equals(r, "HospitalReceiver", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "RECEPTOR", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "Receptor", StringComparison.OrdinalIgnoreCase))
            return Roles.Receptor;

        return r;
    }

    private static string GetSubjectForRole(string role) => role switch
    {
        Roles.Planificador => "planner-1",
        Roles.Gerencia => "admin-1",
        Roles.Bodega => "warehouse-1",
        Roles.Conductor => "driver-1",
        Roles.Receptor => "receiver-1",
        _ => "user-1"
    };

    private static string GetDisplayNameForRole(string role) => role switch
    {
        Roles.Planificador => "Dra. Andrea Morales (Planificación)",
        Roles.Gerencia => "Bryan L. (Gerencia y Plataforma)",
        Roles.Bodega => "Elena Bodega (Operador de Almacén)",
        Roles.Conductor => "Carlos Mendoza (Conductor C-01)",
        Roles.Receptor => "Receptor Hospital Laura (Farmacia)",
        _ => "Usuario Institucional"
    };

    private static string GetRoleLabel(string role) => role switch
    {
        Roles.Planificador => "Planificador Logístico",
        Roles.Gerencia => "Gerencia General",
        Roles.Bodega => "Operador de Bodega",
        Roles.Conductor => "Conductor de Custodia en Ruta",
        Roles.Receptor => "Receptor Hospitalario",
        Roles.Automatizacion => "Agente Autónomo M2M",
        _ => role
    };

    private static string GetFacilityForRole(string role) => role switch
    {
        Roles.Bodega => "Centro Logístico Principal Quito (HUB-UIO)",
        Roles.Planificador => "Torre de Control de Operaciones",
        Roles.Conductor => "Flota de Transporte C-01 (Mercedes Sprinter)",
        Roles.Receptor => "Hospital General Docente Ambato",
        _ => "IstpetDev Central"
    };
}

public sealed record LoginRequest(string? Email, string? Password, string? Role = null);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    AuthUserDto User
);

public sealed record AuthUserDto(
    string Id,
    string Email,
    string Name,
    string Role,
    string RoleLabel,
    string Facility
);
