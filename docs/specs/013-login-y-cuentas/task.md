# 013 — Login real y gestión de cuentas por Gerencia: tareas

> Pasos ejecutables del [`plan.md`](plan.md). Cada tarea entrega algo probable de forma independiente y termina en commit. Cada paso es una acción de 2–5 minutos. TDD: prueba que falla → implementación mínima → prueba que pasa → commit.
>
> Sin marcadores vacíos: nada de «TBD», «añadir validación», «similar a la tarea N» ni pruebas sin código. Única excepción declarada: los valores públicos de Cognito (`issuer`, `domain`, `client_ids`) se copian de `terraform output -json cognito`; la prueba de T6/T7 falla mientras sigan los marcadores `«…»`.
>
> Crear este documento no acredita que nada funcione: todas las tareas están pendientes.

## Resumen

| ID | Tarea | Historia / requisito | Depende de | Paralelizable | Estado |
|---|---|---|---|---|---|
| T1 | Rol Auditor y matriz lectura/escritura | FR-21, CE-07 | — | — | hecho |
| T2 | `GET /api/v1/me` y contrato compartido | FR-05, FR-07 | T1 | — | hecho |
| T3 | Directorio de identidad y alta idempotente | FR-11, FR-13, NFR-02, NFR-05 | T2 | — | hecho |
| T4 | Script de arranque `--provision-accounts` | H3, FR-11, CE-03 | T3 | — | hecho |
| T5 | Lógica de sesión en `shared-core` (almacenamiento, interceptor, returnUrl, logout) | FR-03, FR-04 | T2 | [P] con T3–T4 | hecho |
| T6 | Web: configuración, sesión real, guards, login y retiro de mocks | H1, FR-01–FR-08, FR-10, FR-21 | T5 | [P] con T7 | hecho |
| T7 | PWA: configuración, sesión real, guards, login y retiro de mocks | H1, FR-01–FR-08, FR-10 | T5 | [P] con T6 | hecho |
| T8 | PWA: outbox por cuenta y logout con capturas pendientes | H2, FR-09, CE-04 | T7 | — | hecho |
| T9 | Guardián de CI contra el login simulado y pruebas frontend en CI | NFR-04, CE-02 | T6, T8 | — | hecho |
| T10 | Infraestructura: callbacks HTTPS, marca, IAM y hop limit (Jorge) | FR-20, NFR-02, #14 | — | [P] con todo | pendiente |
| T11 | API H4: listar y crear cuentas | FR-12, FR-13, NFR-05 | T4 | — | hecho (P2) |
| T12 | API H4: editar, desactivar, reactivar y reenviar | FR-14–FR-17, NFR-01, CE-05 | T11 | — | hecho (P2) |
| T13 | Web H4: pantalla «Usuarios» | FR-19 | T12, T6 | — | hecho (P2) |
| T14 | Cierre: E2E, spec, README, bóveda | CE-01–CE-07 | T9, T10 (T13 si H4 entra) | — | en curso (fase A hecha) |

`[P]` = puede ejecutarse en paralelo con otras `[P]` del mismo nivel porque no toca los mismos archivos.

## Preparación común

- [x] Crear la rama: `git switch develop && git pull && git switch -c feature/013-login-y-cuentas`.
- [x] Comprobar Docker para Testcontainers: `docker info >/dev/null && echo ok`.
- [x] `npm ci` en la raíz.

---

### Task 1: Rol Auditor y matriz de lectura/escritura

**Files:**
- Modify: `src/Domain/Common/Roles.cs`
- Create: `tests/IstpetDev.Tests/Support/TestAccounts.cs`
- Test: `tests/IstpetDev.Tests/Integration/Api/AuditorRoleTests.cs`, `tests/IstpetDev.Tests/Unit/Identity/RolesTests.cs`

**Interfaces:**
- Consumes: `ApiFactory`, `TestTokens`, `TestDb` existentes.
- Produces: `Roles.Auditor`, `Roles.Human` (cadena para `[Authorize]`), `Roles.HumanRoles`, `Roles.IsHuman(string?)`, `Roles.PlatformOf(string?) : string?` (`"web"`, `"field"` o `null`); `TestAccounts.SetRoleAsync(string connection, string subject, string role, bool active = true) : Task<Guid>`.

- [x] **Step 1: Escribir las pruebas que fallan**

`tests/IstpetDev.Tests/Support/TestAccounts.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Support;

public static class TestAccounts
{
    // Llamar después de crear un cliente con ApiFactory: el host migra y siembra las cuentas al arrancar.
    public static async Task<Guid> SetRoleAsync(string connection, string subject, string role, bool active = true)
    {
        await using var db = TestDb.Context(connection);
        var account = await db.Accounts.SingleAsync(a => a.Subject == subject);
        account.Role = role;
        account.IsActive = active;
        await db.SaveChangesAsync();
        return account.Id;
    }
}
```

`tests/IstpetDev.Tests/Unit/Identity/RolesTests.cs`:

```csharp
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
```

`tests/IstpetDev.Tests/Integration/Api/AuditorRoleTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Tests.Support;

namespace IstpetDev.Tests.Integration.Api;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
public sealed class AuditorRoleTests(PostgisContainer postgres)
{
    private static readonly string[] Reads =
        ["/api/v1/service-points", "/api/v1/risks", "/api/v1/route-plans", "/api/v1/agent-decisions", "/api/v1/autonomy/mode"];

    private static readonly (HttpMethod Method, string Path)[] Writes =
    [
        (HttpMethod.Post, "/api/v1/imports"), (HttpMethod.Post, "/api/v1/demo/reset"), (HttpMethod.Post, "/api/v1/demo/incidents"),
        (HttpMethod.Post, "/api/v1/incidents"), (HttpMethod.Post, "/api/v1/route-plans"),
        (HttpMethod.Post, $"/api/v1/route-plans/{Guid.Empty}/publish"), (HttpMethod.Post, $"/api/v1/route-plans/{Guid.Empty}/replan"),
        (HttpMethod.Post, $"/api/v1/route-plans/{Guid.Empty}/revert"), (HttpMethod.Post, $"/api/v1/route-plans/{Guid.Empty}/stops/1/complete"),
        (HttpMethod.Post, $"/api/v1/agent-decisions/{Guid.Empty}/approve"), (HttpMethod.Post, $"/api/v1/agent-decisions/{Guid.Empty}/reject"),
        (HttpMethod.Put, "/api/v1/autonomy/mode"),
    ];

    [Fact]
    public async Task Auditor_reads_everything_and_writes_nothing()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Driver, Roles.Auditor);
        using var auditor = api.ClientFor(TestTokens.Driver);

        foreach (var path in Reads)
            Assert.True((await auditor.GetAsync(path)).StatusCode == HttpStatusCode.OK, $"GET {path}");
        foreach (var (method, path) in Writes)
        {
            using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(new { }) };
            Assert.True((await auditor.SendAsync(request)).StatusCode == HttpStatusCode.Forbidden, $"{method} {path}");
        }
    }
}
```

- [x] **Step 2: Ejecutar y verificar que fallan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~RolesTests|FullyQualifiedName~AuditorRoleTests"`
Expected: FAIL de compilación con «'Roles' does not contain a definition for 'Auditor'».

- [x] **Step 3: Implementación mínima** — reemplazar `src/Domain/Common/Roles.cs`:

```csharp
namespace IstpetDev.Domain.Common;

// Siete roles fijos: seis humanos (ADR-14, ADR-26) y uno técnico. El rol de cada cuenta vive en Account.Role.
public static class Roles
{
    public const string Gerencia = "Gerencia";
    public const string Planificador = "Planificador";
    public const string Auditor = "Auditor";
    public const string Bodega = "Bodega";
    public const string Conductor = "Conductor";
    public const string Receptor = "Receptor";
    public const string Automatizacion = "Automatizacion";

    // Valores para [Authorize(Roles = ...)], que acepta nombres separados por coma.
    public const string Planning = Gerencia + "," + Planificador;
    public const string Human = Gerencia + "," + Planificador + "," + Auditor + "," + Bodega + "," + Conductor + "," + Receptor;
    public const string Any = Human + "," + Automatizacion;

    public static readonly IReadOnlyList<string> HumanRoles = [Gerencia, Planificador, Auditor, Bodega, Conductor, Receptor];
    public static readonly IReadOnlyList<string> All = [.. HumanRoles, Automatizacion];

    public static bool IsValid(string? role) => role is not null && All.Contains(role);

    public static bool IsHuman(string? role) => role is not null && HumanRoles.Contains(role);

    // ADR-24: Gerencia, Planificador y Auditor operan en la web; Bodega, Conductor y Receptor en la PWA.
    public static string? PlatformOf(string? role) => role switch
    {
        Gerencia or Planificador or Auditor => "web",
        Bodega or Conductor or Receptor => "field",
        _ => null,
    };
}
```

- [x] **Step 4: Ejecutar y verificar que pasan, sin romper la matriz existente**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~RolesTests|FullyQualifiedName~AuditorRoleTests|FullyQualifiedName~AuthorizationSafetyTests"`
Expected: PASS (todas).

- [x] **Step 5: Commit**

```bash
git add src/Domain/Common/Roles.cs tests/IstpetDev.Tests/Support/TestAccounts.cs tests/IstpetDev.Tests/Unit/Identity/RolesTests.cs tests/IstpetDev.Tests/Integration/Api/AuditorRoleTests.cs
git commit -m "feat(api): rol auditor de solo lectura (adr-26, spec 013)"
```

---

### Task 2: `GET /api/v1/me` y contrato compartido

**Files:**
- Create: `src/Api/Controllers/MeController.cs`, `libs/shared-core/src/contracts/account.contract.ts`
- Modify: `libs/shared-core/src/contracts/index.ts`
- Test: `tests/IstpetDev.Tests/Integration/Api/MeEndpointTests.cs`

**Interfaces:**
- Consumes: `Roles.Human`, `Roles.PlatformOf` (T1); `ClaimsPrincipalExtensions.AccountId()` (001).
- Produces: `GET /api/v1/me` → `MeResponse(Guid AccountId, string DisplayName, string Role, string Platform)` en JSON camelCase; TS `MeResponse`, `HumanRole`, `Platform`, `WEB_ROLES`, `FIELD_ROLES`, `HUMAN_ROLES`, `AccountItem`, `InvitationStatus`, `CreateAccountRequest`, `UpdateAccountRequest`.

- [x] **Step 1: Escribir la prueba que falla** — `tests/IstpetDev.Tests/Integration/Api/MeEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Tests.Support;

namespace IstpetDev.Tests.Integration.Api;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
public sealed class MeEndpointTests(PostgisContainer postgres)
{
    [Theory]
    [InlineData(Roles.Gerencia, "web")]
    [InlineData(Roles.Planificador, "web")]
    [InlineData(Roles.Auditor, "web")]
    [InlineData(Roles.Bodega, "field")]
    [InlineData(Roles.Conductor, "field")]
    [InlineData(Roles.Receptor, "field")]
    public async Task Me_returns_account_role_and_platform(string role, string platform)
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Driver);
        var id = await TestAccounts.SetRoleAsync(connection, TestTokens.Driver, role);

        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/me");

        Assert.Equal(id, me.GetProperty("accountId").GetGuid());
        Assert.Equal("Conductor de prueba", me.GetProperty("displayName").GetString());
        Assert.Equal(role, me.GetProperty("role").GetString());
        Assert.Equal(platform, me.GetProperty("platform").GetString());
    }

    [Fact]
    public async Task Me_is_forbidden_without_an_active_account()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var stranger = api.ClientFor("identidad-sin-cuenta");
        using var driver = api.ClientFor(TestTokens.Driver);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync("/api/v1/me")).StatusCode);

        await TestAccounts.SetRoleAsync(connection, TestTokens.Driver, Roles.Conductor, active: false);

        Assert.Equal(HttpStatusCode.Forbidden, (await driver.GetAsync("/api/v1/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.ClientFor(null).GetAsync("/api/v1/me")).StatusCode);
    }
}
```

- [x] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~MeEndpointTests"`
Expected: FAIL — `GetFromJsonAsync` lanza `HttpRequestException` con «404 (Not Found)».

- [x] **Step 3: Implementación mínima** — `src/Api/Controllers/MeController.cs`:

```csharp
using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

// FR-05 de 013: las apps deciden acceso y navegación con esta respuesta, no con claims del token.
[ApiController]
[Route("api/v1/me")]
public sealed class MeController(IApplicationDbContext database) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.Human)]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
    {
        var accountId = User.AccountId();
        var account = accountId is null ? null : await database.Accounts.AsNoTracking()
            .Where(a => a.Id == accountId && a.IsActive)
            .Select(a => new { a.Id, a.DisplayName, a.Role })
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null || Roles.PlatformOf(account.Role) is not { } platform) return Forbid();
        return new MeResponse(account.Id, account.DisplayName, account.Role, platform);
    }
}

public sealed record MeResponse(Guid AccountId, string DisplayName, string Role, string Platform);
```

`libs/shared-core/src/contracts/account.contract.ts`:

```ts
/** Roles humanos fijos (ADR-14, ADR-26). `Automatizacion` es técnico y no aparece en las apps. */
export type HumanRole = 'Gerencia' | 'Planificador' | 'Auditor' | 'Bodega' | 'Conductor' | 'Receptor';
export type Platform = 'web' | 'field';

export const WEB_ROLES: readonly HumanRole[] = ['Gerencia', 'Planificador', 'Auditor'];
export const FIELD_ROLES: readonly HumanRole[] = ['Bodega', 'Conductor', 'Receptor'];
export const HUMAN_ROLES: readonly HumanRole[] = [...WEB_ROLES, ...FIELD_ROLES];

/** Respuesta de GET /api/v1/me (FR-05). */
export interface MeResponse {
  accountId: string;
  displayName: string;
  role: HumanRole;
  platform: Platform;
}

export type InvitationStatus = 'pending' | 'active' | 'missing' | 'unknown' | 'other';

/** Fila de GET /api/v1/accounts (FR-12). */
export interface AccountItem {
  id: string;
  displayName: string;
  role: HumanRole;
  isActive: boolean;
  email: string | null;
  invitationStatus: InvitationStatus;
  directoryEnabled: boolean | null;
  outOfSync: boolean;
}

export interface CreateAccountRequest {
  email: string;
  displayName: string;
  role: HumanRole;
}

export interface UpdateAccountRequest {
  displayName?: string;
  role?: HumanRole;
}
```

Añadir al final de `libs/shared-core/src/contracts/index.ts`:

```ts
export * from './account.contract';
```

- [x] **Step 4: Ejecutar y verificar que pasa**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~MeEndpointTests" && npm run build:shared`
Expected: PASS (8 casos) y build de `shared-core` sin errores.

- [x] **Step 5: Commit**

```bash
git add src/Api/Controllers/MeController.cs tests/IstpetDev.Tests/Integration/Api/MeEndpointTests.cs libs/shared-core/src/contracts/account.contract.ts libs/shared-core/src/contracts/index.ts
git commit -m "feat(api): endpoint /me con rol y plataforma (spec 013 fr-05)"
```

---

### Task 3: Directorio de identidad y alta idempotente

**Files:**
- Create: `src/Application/Accounts/IIdentityDirectory.cs`, `src/Application/Accounts/AccountRuleException.cs`, `src/Application/Accounts/AccountProvisioning.cs`
- Create: `src/Infrastructure/Identity/CognitoPool.cs`, `src/Infrastructure/Identity/CognitoIdentityDirectory.cs`, `src/Infrastructure/Identity/UnconfiguredIdentityDirectory.cs`
- Modify: `src/Application/DependencyInjection.cs`, `src/Infrastructure/DependencyInjection.cs`, `src/Infrastructure/IstpetDev.Infrastructure.csproj`, `tests/IstpetDev.Tests/Support/ApiFactory.cs`
- Create: `tests/IstpetDev.Tests/Support/FakeIdentityDirectory.cs`
- Test: `tests/IstpetDev.Tests/Unit/Identity/CognitoPoolTests.cs`, `tests/IstpetDev.Tests/Integration/Accounts/AccountProvisioningTests.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext` (`Accounts`, `Database`, `ChangeTracker`, `LockOrganizationAsync`), `Roles.IsHuman`.
- Produces:
  - `IIdentityDirectory { string Issuer; FindByEmailAsync(string, ct) : Task<DirectoryUser?>; FindBySubjectAsync(string, ct) : Task<DirectoryUser?>; CreateUserAsync(string email, ct) : Task<DirectoryUser>; ListUsersAsync(ct) : Task<IReadOnlyList<DirectoryUser>>; DisableAsync(string subject, ct); EnableAsync(string subject, ct); SignOutEverywhereAsync(string subject, ct); ResendInvitationAsync(string email, ct) }`
  - `DirectoryUser(string Subject, string Email, DirectoryUserStatus Status, bool Enabled)`; `enum DirectoryUserStatus { InvitationPending, Active, Other }`
  - `IdentityDirectoryUnavailableException`; `AccountRuleException(string Code, string Message)`
  - `AccountProvisioning.ProvisionAsync(string email, string displayName, string role, ct) : Task<ProvisionResult>`; `ProvisionResult(ProvisionOutcome Outcome, Guid AccountId, DirectoryUser User)`; `enum ProvisionOutcome { Created, Reconciled, AlreadyExists }`; estáticos `NormalizeEmail`, `ValidateDisplayName`, `InvalidRole()`
  - `CognitoPool.FromAuthority(string?) : CognitoPool?` con `Region`, `UserPoolId`, `Issuer`
  - `ApiFactory.Directory : FakeIdentityDirectory`; `FakeIdentityDirectory.Add(...)`, `.Confirm(subject)`, `.Users`, `.Unavailable`, `.Invitations`, `.Resends`, `.SignedOut`

- [x] **Step 1: Añadir el paquete del SDK**

Run: `dotnet add src/Infrastructure package AWSSDK.CognitoIdentityProvider`
Expected: `PackageReference` con versión 4.x en `IstpetDev.Infrastructure.csproj`. Si resolviera 3.x, repetir con `--version` de la última 4.x publicada en nuget.org. Anotar la versión exacta en la tabla de dependencias de `plan.md`.

- [x] **Step 2: Escribir las pruebas que fallan**

`tests/IstpetDev.Tests/Unit/Identity/CognitoPoolTests.cs`:

```csharp
using IstpetDev.Infrastructure.Identity;

namespace IstpetDev.Tests.Unit.Identity;

[Trait("Category", "Unit")]
public sealed class CognitoPoolTests
{
    [Fact]
    public void Parses_region_pool_and_issuer_from_the_authority()
    {
        var pool = CognitoPool.FromAuthority("https://cognito-idp.us-east-1.amazonaws.com/us-east-1_AbC123/");
        Assert.NotNull(pool);
        Assert.Equal("us-east-1", pool.Region);
        Assert.Equal("us-east-1_AbC123", pool.UserPoolId);
        Assert.Equal("https://cognito-idp.us-east-1.amazonaws.com/us-east-1_AbC123", pool.Issuer);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://cognito-idp.us-east-1.amazonaws.com/us-east-1_AbC123")]
    [InlineData("https://identity.istpetdev.local")]
    [InlineData("https://cognito-idp.us-east-1.amazonaws.com/eu-west-1_AbC123")]
    [InlineData("https://cognito-idp.us-east-1.amazonaws.com.evil.example/us-east-1_AbC123")]
    public void Rejects_anything_that_is_not_a_cognito_issuer(string? authority) => Assert.Null(CognitoPool.FromAuthority(authority));
}
```

`tests/IstpetDev.Tests/Support/FakeIdentityDirectory.cs`:

```csharp
using System.Collections.Concurrent;
using IstpetDev.Application.Accounts;

namespace IstpetDev.Tests.Support;

// Directorio en memoria con el comportamiento observable de Cognito que usa 013.
public sealed class FakeIdentityDirectory : IIdentityDirectory
{
    private readonly ConcurrentDictionary<string, DirectoryUser> users = new();
    private int invitations;
    private int resends;

    public string Issuer => TestTokens.Issuer;
    public bool Unavailable { get; set; }
    public int Invitations => invitations;
    public int Resends => resends;
    public ConcurrentBag<string> SignedOut { get; } = [];
    public IReadOnlyCollection<DirectoryUser> Users => users.Values.ToArray();

    public DirectoryUser Add(string subject, string email, DirectoryUserStatus status = DirectoryUserStatus.Active, bool enabled = true) =>
        users[subject] = new DirectoryUser(subject, email, status, enabled);

    public void Confirm(string subject) => users[subject] = users[subject] with { Status = DirectoryUserStatus.Active };

    public Task<DirectoryUser?> FindByEmailAsync(string email, CancellationToken ct) =>
        Guarded(() => users.Values.SingleOrDefault(u => u.Email == email));

    public Task<DirectoryUser?> FindBySubjectAsync(string subject, CancellationToken ct) =>
        Guarded(() => users.GetValueOrDefault(subject));

    public Task<DirectoryUser> CreateUserAsync(string email, CancellationToken ct) => Guarded(() =>
    {
        lock (users)
        {
            if (users.Values.SingleOrDefault(u => u.Email == email) is { } existing) return existing;
            Interlocked.Increment(ref invitations);
            return Add($"sub-{Guid.NewGuid():N}", email, DirectoryUserStatus.InvitationPending);
        }
    });

    public Task<IReadOnlyList<DirectoryUser>> ListUsersAsync(CancellationToken ct) =>
        Guarded(() => (IReadOnlyList<DirectoryUser>)users.Values.ToList());

    public Task DisableAsync(string subject, CancellationToken ct) => Guarded(() => users[Known(subject)] = users[subject] with { Enabled = false });

    public Task EnableAsync(string subject, CancellationToken ct) => Guarded(() => users[Known(subject)] = users[subject] with { Enabled = true });

    public Task SignOutEverywhereAsync(string subject, CancellationToken ct) => Guarded(() => { SignedOut.Add(Known(subject)); return true; });

    public Task ResendInvitationAsync(string email, CancellationToken ct) => Guarded(() => Interlocked.Increment(ref resends));

    private string Known(string subject) =>
        users.ContainsKey(subject) ? subject : throw new IdentityDirectoryUnavailableException("Usuario inexistente en el directorio simulado.");

    private Task<T> Guarded<T>(Func<T> action) =>
        Unavailable ? throw new IdentityDirectoryUnavailableException("Cognito simulado no disponible.") : Task.FromResult(action());
}
```

`tests/IstpetDev.Tests/Integration/Accounts/AccountProvisioningTests.cs`:

```csharp
using IstpetDev.Application.Accounts;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IstpetDev.Tests.Integration.Accounts;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
public sealed class AccountProvisioningTests(PostgisContainer postgres)
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
```

- [x] **Step 3: Ejecutar y verificar que fallan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~CognitoPoolTests|FullyQualifiedName~AccountProvisioningTests"`
Expected: FAIL de compilación con «The type or namespace name 'Accounts' does not exist in the namespace 'IstpetDev.Application'».

- [x] **Step 4: Implementar el puerto y el alta**

`src/Application/Accounts/IIdentityDirectory.cs`:

```csharp
namespace IstpetDev.Application.Accounts;

public enum DirectoryUserStatus { InvitationPending, Active, Other }

public sealed record DirectoryUser(string Subject, string Email, DirectoryUserStatus Status, bool Enabled);

public sealed class IdentityDirectoryUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

// Puerto hacia el directorio de identidad (Cognito). Identifica usuarios por sub, nunca por correo mutable.
public interface IIdentityDirectory
{
    string Issuer { get; }
    Task<DirectoryUser?> FindByEmailAsync(string email, CancellationToken ct);
    Task<DirectoryUser?> FindBySubjectAsync(string subject, CancellationToken ct);
    Task<DirectoryUser> CreateUserAsync(string email, CancellationToken ct);
    Task<IReadOnlyList<DirectoryUser>> ListUsersAsync(CancellationToken ct);
    Task DisableAsync(string subject, CancellationToken ct);
    Task EnableAsync(string subject, CancellationToken ct);
    Task SignOutEverywhereAsync(string subject, CancellationToken ct);
    Task ResendInvitationAsync(string email, CancellationToken ct);
}
```

`src/Application/Accounts/AccountRuleException.cs`:

```csharp
namespace IstpetDev.Application.Accounts;

// Regla de negocio de cuentas con código estable para Problem Details (spec 013).
public sealed class AccountRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
```

`src/Application/Accounts/AccountProvisioning.cs`:

```csharp
using System.Text.RegularExpressions;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Accounts;

public enum ProvisionOutcome { Created, Reconciled, AlreadyExists }

public sealed record ProvisionResult(ProvisionOutcome Outcome, Guid AccountId, DirectoryUser User);

// Alta de una persona: usuario en Cognito (que envía la invitación) + Account activa. Idempotente por correo.
public sealed class AccountProvisioning(IApplicationDbContext database, IIdentityDirectory directory)
{
    // Sin comillas ni barras: el correo también viaja dentro del filtro de ListUsers.
    private static readonly Regex EmailShape = new(@"^[^@\s""\\]+@[^@\s""\\]+\.[^@\s""\\]+$", RegexOptions.CultureInvariant);

    public async Task<ProvisionResult> ProvisionAsync(string email, string displayName, string role, CancellationToken ct)
    {
        email = NormalizeEmail(email);
        displayName = ValidateDisplayName(displayName);
        if (!Roles.IsHuman(role)) throw InvalidRole();

        // El bloqueo serializa altas del mismo correo: la compensación nunca deshabilita al usuario que otra petición registró.
        await using var transaction = await database.Database.BeginTransactionAsync(ct);
        await database.LockOrganizationAsync(ct);
        var found = await directory.FindByEmailAsync(email, ct);
        var user = found ?? await directory.CreateUserAsync(email, ct);
        var existing = await database.Accounts.AsNoTracking()
            .Where(a => a.Issuer == directory.Issuer && a.Subject == user.Subject)
            .Select(a => (Guid?)a.Id).SingleOrDefaultAsync(ct);
        if (existing is Guid id) return new(ProvisionOutcome.AlreadyExists, id, user);

        var account = new Account
        {
            Kind = "human", Issuer = directory.Issuer, Subject = user.Subject,
            DisplayName = displayName, Role = role, IsActive = true,
        };
        database.Accounts.Add(account);
        try
        {
            await database.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            database.ChangeTracker.Clear();
            if (found is null) await DisableQuietlyAsync(user.Subject);
            throw;
        }
        return new(found is null ? ProvisionOutcome.Created : ProvisionOutcome.Reconciled, account.Id, user);
    }

    public static string NormalizeEmail(string? email)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length > 254 || !EmailShape.IsMatch(normalized))
            throw new AccountRuleException("invalid_email", "Escribe un correo válido.");
        return normalized;
    }

    public static string ValidateDisplayName(string? displayName)
    {
        var trimmed = (displayName ?? string.Empty).Trim();
        if (trimmed.Length is 0 or > 120)
            throw new AccountRuleException("invalid_display_name", "Escribe el nombre de la persona (máximo 120 caracteres).");
        return trimmed;
    }

    public static AccountRuleException InvalidRole() =>
        new("invalid_role", "Elige uno de los seis roles humanos: Gerencia, Planificador, Auditor, Bodega, Conductor o Receptor.");

    private async Task DisableQuietlyAsync(string subject)
    {
        // Sin Account el usuario no recibe rol (001 FR-17); deshabilitarlo es la segunda barrera, no la única.
        try { await directory.DisableAsync(subject, CancellationToken.None); }
        catch (IdentityDirectoryUnavailableException) { }
    }
}
```

Registrar en `src/Application/DependencyInjection.cs`, dentro de `AddApplicationServices`, antes de `return services;`:

```csharp
        services.AddScoped<AccountProvisioning>();
```

y añadir `using IstpetDev.Application.Accounts;` al inicio.

- [x] **Step 5: Implementar el adaptador de Cognito**

`src/Infrastructure/Identity/CognitoPool.cs`:

```csharp
namespace IstpetDev.Infrastructure.Identity;

// Región y pool salen del issuer ya configurado en Authentication:Authority; no hay otra configuración que mantener.
public sealed record CognitoPool(string Region, string UserPoolId, string Issuer)
{
    public static CognitoPool? FromAuthority(string? authority)
    {
        var issuer = authority?.Trim().TrimEnd('/');
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) return null;
        var host = uri.Host.Split('.');
        var poolId = uri.AbsolutePath.Trim('/');
        if (host is not ["cognito-idp", var region, "amazonaws", "com"]) return null;
        if (poolId.Split('_') is not [var poolRegion, { Length: > 0 }] || poolRegion != region) return null;
        return new CognitoPool(region, poolId, issuer!);
    }
}
```

`src/Infrastructure/Identity/CognitoIdentityDirectory.cs`:

```csharp
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using IstpetDev.Application.Accounts;
using Microsoft.Extensions.Logging;

namespace IstpetDev.Infrastructure.Identity;

// Credenciales: cadena por defecto del SDK (perfil de instancia en EC2, AWS_PROFILE en local). NFR-02.
// En pools con username_attributes = email, el Username interno es el sub: las llamadas Admin usan Subject.
public sealed class CognitoIdentityDirectory(IAmazonCognitoIdentityProvider cognito, CognitoPool pool, ILogger<CognitoIdentityDirectory> logger) : IIdentityDirectory
{
    public string Issuer => pool.Issuer;

    public async Task<DirectoryUser?> FindByEmailAsync(string email, CancellationToken ct) =>
        (await ListAsync($"email = \"{email}\"", ct)).SingleOrDefault();

    public async Task<DirectoryUser?> FindBySubjectAsync(string subject, CancellationToken ct) =>
        (await ListAsync($"sub = \"{subject}\"", ct)).SingleOrDefault();

    public Task<IReadOnlyList<DirectoryUser>> ListUsersAsync(CancellationToken ct) => ListAsync(null, ct);

    public async Task<DirectoryUser> CreateUserAsync(string email, CancellationToken ct)
    {
        try
        {
            var response = await Call(() => cognito.AdminCreateUserAsync(new AdminCreateUserRequest
            {
                UserPoolId = pool.UserPoolId,
                Username = email,
                DesiredDeliveryMediums = ["EMAIL"],
                UserAttributes =
                [
                    new AttributeType { Name = "email", Value = email },
                    new AttributeType { Name = "email_verified", Value = "true" },
                ],
            }, ct));
            return Map(response.User.Attributes, response.User.UserStatus, response.User.Enabled == true);
        }
        catch (UsernameExistsException)
        {
            return await FindByEmailAsync(email, ct)
                ?? throw new IdentityDirectoryUnavailableException("Cognito informó un usuario existente que no se pudo leer.");
        }
    }

    public Task DisableAsync(string subject, CancellationToken ct) =>
        Call(() => cognito.AdminDisableUserAsync(new AdminDisableUserRequest { UserPoolId = pool.UserPoolId, Username = subject }, ct));

    public Task EnableAsync(string subject, CancellationToken ct) =>
        Call(() => cognito.AdminEnableUserAsync(new AdminEnableUserRequest { UserPoolId = pool.UserPoolId, Username = subject }, ct));

    public Task SignOutEverywhereAsync(string subject, CancellationToken ct) =>
        Call(() => cognito.AdminUserGlobalSignOutAsync(new AdminUserGlobalSignOutRequest { UserPoolId = pool.UserPoolId, Username = subject }, ct));

    public Task ResendInvitationAsync(string email, CancellationToken ct) =>
        Call(() => cognito.AdminCreateUserAsync(new AdminCreateUserRequest
        {
            UserPoolId = pool.UserPoolId, Username = email, MessageAction = MessageActionType.RESEND, DesiredDeliveryMediums = ["EMAIL"],
        }, ct));

    private async Task<IReadOnlyList<DirectoryUser>> ListAsync(string? filter, CancellationToken ct)
    {
        var users = new List<DirectoryUser>();
        string? token = null;
        do
        {
            var page = await Call(() => cognito.ListUsersAsync(new ListUsersRequest
            {
                UserPoolId = pool.UserPoolId, Filter = filter, Limit = 60, PaginationToken = token,
            }, ct));
            users.AddRange((page.Users ?? []).Select(u => Map(u.Attributes, u.UserStatus, u.Enabled == true)));
            token = page.PaginationToken;
        } while (!string.IsNullOrEmpty(token));
        return users;
    }

    // SDK 4: las colecciones vacías llegan como null.
    private static DirectoryUser Map(List<AttributeType>? attributes, UserStatusType? status, bool enabled)
    {
        string Attribute(string name) => attributes?.FirstOrDefault(a => a.Name == name)?.Value ?? string.Empty;
        var state = status == UserStatusType.FORCE_CHANGE_PASSWORD ? DirectoryUserStatus.InvitationPending
            : status == UserStatusType.CONFIRMED ? DirectoryUserStatus.Active : DirectoryUserStatus.Other;
        return new DirectoryUser(Attribute("sub"), Attribute("email"), state, enabled);
    }

    private async Task<T> Call<T>(Func<Task<T>> operation)
    {
        try { return await operation(); }
        catch (Exception ex) when (ex is AmazonServiceException and not UsernameExistsException or AmazonClientException or HttpRequestException)
        {
            logger.LogWarning("Cognito no respondió a una operación de cuentas: {ErrorType}", ex.GetType().Name);
            throw new IdentityDirectoryUnavailableException("Cognito no está disponible.", ex);
        }
    }
}
```

`src/Infrastructure/Identity/UnconfiguredIdentityDirectory.cs`:

```csharp
using IstpetDev.Application.Accounts;

namespace IstpetDev.Infrastructure.Identity;

// Sin Authentication:Authority de Cognito no hay directorio: la gestión responde 503 y el login real no existe.
public sealed class UnconfiguredIdentityDirectory : IIdentityDirectory
{
    public string Issuer => string.Empty;
    public Task<DirectoryUser?> FindByEmailAsync(string email, CancellationToken ct) => Fail<DirectoryUser?>();
    public Task<DirectoryUser?> FindBySubjectAsync(string subject, CancellationToken ct) => Fail<DirectoryUser?>();
    public Task<DirectoryUser> CreateUserAsync(string email, CancellationToken ct) => Fail<DirectoryUser>();
    public Task<IReadOnlyList<DirectoryUser>> ListUsersAsync(CancellationToken ct) => Fail<IReadOnlyList<DirectoryUser>>();
    public Task DisableAsync(string subject, CancellationToken ct) => Fail<bool>();
    public Task EnableAsync(string subject, CancellationToken ct) => Fail<bool>();
    public Task SignOutEverywhereAsync(string subject, CancellationToken ct) => Fail<bool>();
    public Task ResendInvitationAsync(string email, CancellationToken ct) => Fail<bool>();

    private static Task<T> Fail<T>() =>
        Task.FromException<T>(new IdentityDirectoryUnavailableException("Authentication:Authority no apunta a un pool de Cognito."));
}
```

En `src/Infrastructure/DependencyInjection.cs`, añadir los `using` `Amazon;`, `Amazon.CognitoIdentityProvider;`, `IstpetDev.Application.Accounts;`, `IstpetDev.Infrastructure.Identity;` y, antes del registro de health checks:

```csharp
        // Perezoso: WebApplicationFactory aplica la configuración de pruebas después de registrar servicios.
        services.AddSingleton<IIdentityDirectory>(provider =>
        {
            var pool = CognitoPool.FromAuthority(provider.GetRequiredService<IConfiguration>()["Authentication:Authority"]);
            if (pool is null) return new UnconfiguredIdentityDirectory();
            var client = new AmazonCognitoIdentityProviderClient(RegionEndpoint.GetBySystemName(pool.Region));
            return new CognitoIdentityDirectory(client, pool, provider.GetRequiredService<ILogger<CognitoIdentityDirectory>>());
        });
```

En `tests/IstpetDev.Tests/Support/ApiFactory.cs`: añadir `using IstpetDev.Application.Accounts;` y `using Microsoft.Extensions.DependencyInjection.Extensions;`, la propiedad `public FakeIdentityDirectory Directory { get; } = new();` dentro de la clase, y reemplazar el bloque `ConfigureTestServices` por:

```csharp
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
```

- [x] **Step 6: Ejecutar y verificar que pasan**

Run: `dotnet build IstpetDev.sln && dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~CognitoPoolTests|FullyQualifiedName~AccountProvisioningTests"`
Expected: build sin advertencias nuevas; PASS (7 + 11 casos).

- [x] **Step 7: Commit**

```bash
git add src/Application/Accounts src/Application/DependencyInjection.cs src/Infrastructure/Identity src/Infrastructure/DependencyInjection.cs src/Infrastructure/IstpetDev.Infrastructure.csproj tests/IstpetDev.Tests/Support/FakeIdentityDirectory.cs tests/IstpetDev.Tests/Support/ApiFactory.cs tests/IstpetDev.Tests/Unit/Identity/CognitoPoolTests.cs tests/IstpetDev.Tests/Integration/Accounts/AccountProvisioningTests.cs
git commit -m "feat(identidad): directorio cognito y alta idempotente de cuentas (spec 013)"
```

---

### Task 4: Script de arranque `--provision-accounts`

**Files:**
- Create: `src/Application/Accounts/ProvisioningInput.cs`, `src/Api/Accounts/ProvisionAccountsCommand.cs`
- Modify: `src/Api/Program.cs`
- Test: `tests/IstpetDev.Tests/Unit/Identity/ProvisioningInputTests.cs`, `tests/IstpetDev.Tests/Integration/Accounts/ProvisionAccountsCommandTests.cs`

**Interfaces:**
- Consumes: `AccountProvisioning.ProvisionAsync`, `ProvisionOutcome`, `AccountRuleException`, `IdentityDirectoryUnavailableException` (T3).
- Produces: `ProvisioningInput.Parse(TextReader) : ProvisioningInput(Rows, Errors)`; `ProvisioningRow(int Line, string Email, string DisplayName, string Role)`; `ProvisionAccountsCommand.Argument = "--provision-accounts"`; `ProvisionAccountsCommand.RunAsync(IServiceProvider, string path, TextWriter output, CancellationToken) : Task<int>` (0 todo bien, 1 alguna fila falló, 2 archivo inválido).

- [x] **Step 1: Escribir las pruebas que fallan**

`tests/IstpetDev.Tests/Unit/Identity/ProvisioningInputTests.cs`:

```csharp
using IstpetDev.Application.Accounts;

namespace IstpetDev.Tests.Unit.Identity;

[Trait("Category", "Unit")]
public sealed class ProvisioningInputTests
{
    [Fact]
    public void Reads_rows_skipping_blank_lines_and_comments()
    {
        var input = ProvisioningInput.Parse(new StringReader("# cuentas demo\nemail, displayName, role\n\nana@ejemplo.test, Ana Bodega ,Bodega\nluis@ejemplo.test,Luis,Conductor\n"));

        Assert.Empty(input.Errors);
        Assert.Equal([new ProvisioningRow(4, "ana@ejemplo.test", "Ana Bodega", "Bodega"), new ProvisioningRow(5, "luis@ejemplo.test", "Luis", "Conductor")], input.Rows);
    }

    [Theory]
    [InlineData("", "El archivo está vacío.")]
    [InlineData("correo,nombre,rol\nana@ejemplo.test,Ana,Bodega", "La primera fila debe ser «email,displayName,role».")]
    [InlineData("email,displayName,role\nana@ejemplo.test,Ana, Bodega,Bodega", "Se esperan 3 columnas: correo, nombre y rol (el nombre no puede llevar comas).")]
    [InlineData("email,displayName,role\nana@ejemplo.test,Ana,Bodega\nANA@ejemplo.test,Ana,Bodega", "Correo repetido en el archivo.")]
    public void Reports_file_errors_without_echoing_personal_data(string content, string message)
    {
        var error = Assert.Single(ProvisioningInput.Parse(new StringReader(content)).Errors);
        Assert.Equal(message, error.Message);
    }
}
```

`tests/IstpetDev.Tests/Integration/Accounts/ProvisionAccountsCommandTests.cs`:

```csharp
using System.Text.RegularExpressions;
using IstpetDev.Api.Accounts;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Integration.Accounts;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
public sealed class ProvisionAccountsCommandTests(PostgisContainer postgres)
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
```

- [x] **Step 2: Ejecutar y verificar que fallan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~ProvisioningInputTests|FullyQualifiedName~ProvisionAccountsCommandTests"`
Expected: FAIL de compilación con «The type or namespace name 'ProvisioningInput' could not be found».

- [x] **Step 3: Implementación mínima**

`src/Application/Accounts/ProvisioningInput.cs`:

```csharp
namespace IstpetDev.Application.Accounts;

public sealed record ProvisioningRow(int Line, string Email, string DisplayName, string Role);

public sealed record ProvisioningError(int Line, string Message);

// CSV de H3 (fuera del repositorio, G1): cabecera «email,displayName,role»; # inicia un comentario.
public sealed record ProvisioningInput(IReadOnlyList<ProvisioningRow> Rows, IReadOnlyList<ProvisioningError> Errors)
{
    public const string Header = "email,displayName,role";

    public static ProvisioningInput Parse(TextReader reader)
    {
        var rows = new List<ProvisioningRow>();
        var errors = new List<ProvisioningError>();
        var number = 0;
        var headerSeen = false;
        for (var line = reader.ReadLine(); line is not null; line = reader.ReadLine())
        {
            number++;
            var text = line.Trim();
            if (text.Length == 0 || text.StartsWith('#')) continue;
            if (!headerSeen)
            {
                headerSeen = true;
                if (!string.Equals(text.Replace(" ", string.Empty), Header, StringComparison.OrdinalIgnoreCase))
                    errors.Add(new(number, $"La primera fila debe ser «{Header}»."));
                continue;
            }
            var fields = text.Split(',');
            if (fields.Length != 3)
            {
                errors.Add(new(number, "Se esperan 3 columnas: correo, nombre y rol (el nombre no puede llevar comas)."));
                continue;
            }
            rows.Add(new(number, fields[0].Trim(), fields[1].Trim(), fields[2].Trim()));
        }
        if (!headerSeen) errors.Add(new(0, "El archivo está vacío."));
        foreach (var repeated in rows.GroupBy(r => r.Email.ToLowerInvariant()).Where(g => g.Count() > 1))
            errors.Add(new(repeated.Last().Line, "Correo repetido en el archivo."));
        return new(rows, errors);
    }
}
```

`src/Api/Accounts/ProvisionAccountsCommand.cs`:

```csharp
using IstpetDev.Application.Accounts;

namespace IstpetDev.Api.Accounts;

// H3 de 013: crea la Gerencia inicial y las cuentas de demo. Cognito genera y envía la contraseña temporal;
// este comando nunca la ve ni la imprime, y tampoco imprime correos.
public static class ProvisionAccountsCommand
{
    public const string Argument = "--provision-accounts";

    public static async Task<int> RunAsync(IServiceProvider services, string path, TextWriter output, CancellationToken ct = default)
    {
        ProvisioningInput input;
        using (var reader = File.OpenText(path)) input = ProvisioningInput.Parse(reader);
        foreach (var error in input.Errors) await output.WriteLineAsync($"línea {error.Line}: {error.Message}");
        if (input.Errors.Count > 0) return 2;

        using var scope = services.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<AccountProvisioning>();
        var failures = 0;
        foreach (var row in input.Rows)
        {
            try
            {
                var result = await provisioning.ProvisionAsync(row.Email, row.DisplayName, row.Role, ct);
                await output.WriteLineAsync($"línea {row.Line}: {Describe(result.Outcome)} ({row.Role})");
            }
            catch (Exception ex) when (ex is AccountRuleException or IdentityDirectoryUnavailableException)
            {
                failures++;
                await output.WriteLineAsync($"línea {row.Line}: rechazada: {ex.Message}");
            }
        }
        return failures == 0 ? 0 : 1;
    }

    private static string Describe(ProvisionOutcome outcome) => outcome switch
    {
        ProvisionOutcome.Created => "creada; Cognito envió la invitación",
        ProvisionOutcome.Reconciled => "creada en la base; el usuario de Cognito ya existía",
        _ => "ya existía; sin cambios",
    };
}
```

En `src/Api/Program.cs`, añadir `using IstpetDev.Api.Accounts;` y, justo después de `if (args.Contains("--migrate")) return;`:

```csharp
var provisionAt = Array.IndexOf(args, ProvisionAccountsCommand.Argument);
if (provisionAt >= 0)
{
    if (provisionAt + 1 >= args.Length)
    {
        Console.Error.WriteLine($"Uso: {ProvisionAccountsCommand.Argument} <archivo.csv con {IstpetDev.Application.Accounts.ProvisioningInput.Header}>");
        Environment.ExitCode = 2;
        return;
    }
    Environment.ExitCode = await ProvisionAccountsCommand.RunAsync(app.Services, args[provisionAt + 1], Console.Out);
    return;
}
```

- [x] **Step 4: Ejecutar y verificar que pasan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~ProvisioningInputTests|FullyQualifiedName~ProvisionAccountsCommandTests"`
Expected: PASS (5 + 2 casos).

- [x] **Step 5: Comprobar el uso sin archivo**

Run: `dotnet run --project src/Api -- --provision-accounts; echo "salida=$?"`
Expected: «Uso: --provision-accounts <archivo.csv con email,displayName,role>» y `salida=2`.

- [x] **Step 6: Commit**

```bash
git add src/Application/Accounts/ProvisioningInput.cs src/Api/Accounts/ProvisionAccountsCommand.cs src/Api/Program.cs tests/IstpetDev.Tests/Unit/Identity/ProvisioningInputTests.cs tests/IstpetDev.Tests/Integration/Accounts/ProvisionAccountsCommandTests.cs
git commit -m "feat(identidad): script idempotente de cuentas de demo (spec 013 h3)"
```

---

### Task 5: Lógica de sesión en `shared-core`

**Files:**
- Create: `libs/shared-core/src/auth/token-free-storage.ts`, `libs/shared-core/src/auth/api-request.ts`, `libs/shared-core/src/auth/return-url.ts`, `libs/shared-core/src/auth/cognito-logout.ts`, `libs/shared-core/src/auth/index.ts`
- Modify: `libs/shared-core/src/index.ts`, `libs/shared-core/package.json`, `libs/shared-core/tsconfig.json`, `package.json`
- Test: `libs/shared-core/src/auth/token-free-storage.spec.ts`, `libs/shared-core/src/auth/session-rules.spec.ts`

**Interfaces:**
- Consumes: —
- Produces: `class TokenFreeStorage { read(key): string | null; write(key, value): void; remove(key): void; clear(): void }` (constructor opcional `flowStore: KeyValueStore | null`); `AUTH_FLOW_KEYS`; `isPrivateApiRequest(url: string, apiUrl: string): boolean`; `safeReturnUrl(value: string | null | undefined): string | null`; `cognitoLogoutUrl(cognitoDomain: string, clientId: string, logoutUri: string): string`; scripts raíz `test:shared`, `test:frontend`.

- [x] **Step 1: Instalar Vitest en `shared-core` y excluir specs del build**

Run: `npm install -D -E vitest@4 --workspace=libs/shared-core`

En `libs/shared-core/package.json`, dentro de `scripts`, añadir `"test": "vitest run"`. En `libs/shared-core/tsconfig.json`, junto a `include`, añadir `"exclude": ["src/**/*.spec.ts"]`. En el `package.json` raíz, dentro de `scripts`, añadir:

```json
    "test:shared": "npm run test --workspace=libs/shared-core",
    "test:web": "npm run test --workspace=apps/web",
    "test:mobile": "npm run test --workspace=apps/mobile",
    "test:frontend": "npm run test:shared && npm run test:web && npm run test:mobile",
```

- [x] **Step 2: Escribir las pruebas que fallan**

`libs/shared-core/src/auth/token-free-storage.spec.ts`:

```ts
import { describe, expect, it } from 'vitest';
import { TokenFreeStorage, KeyValueStore } from './token-free-storage';

class MapStore implements KeyValueStore {
  readonly data = new Map<string, string>();
  getItem(key: string) { return this.data.get(key) ?? null; }
  setItem(key: string, value: string) { this.data.set(key, value); }
  removeItem(key: string) { this.data.delete(key); }
}

const CONFIG = '0-cliente-web';
const fullState = JSON.stringify({
  codeVerifier: 'verificador-pkce',
  authStateControl: 'estado',
  authNonce: 'nonce',
  authnResult: { access_token: 'AT-SECRETO', id_token: 'IT-SECRETO', refresh_token: 'RT-SECRETO' },
  authzData: 'AT-SECRETO',
  reusable_refresh_token: 'RT-SECRETO',
  userData: { email: 'persona@ejemplo.test' },
});

describe('TokenFreeStorage (FR-03)', () => {
  it('la app lee todo de memoria pero sessionStorage solo recibe el flujo PKCE', () => {
    const flow = new MapStore();
    const storage = new TokenFreeStorage(flow);

    storage.write(CONFIG, fullState);

    expect(storage.read(CONFIG)).toBe(fullState);
    const persisted = flow.getItem(CONFIG) ?? '';
    expect(JSON.parse(persisted)).toEqual({ codeVerifier: 'verificador-pkce', authStateControl: 'estado', authNonce: 'nonce' });
    expect(persisted).not.toMatch(/SECRETO|access_token|refresh_token|id_token|persona@/);
  });

  it('el verificador sobrevive a la redirección; los tokens no', () => {
    const flow = new MapStore();
    new TokenFreeStorage(flow).write(CONFIG, fullState);

    const afterReload = new TokenFreeStorage(flow);

    expect(JSON.parse(afterReload.read(CONFIG) ?? '{}')).toEqual({ codeVerifier: 'verificador-pkce', authStateControl: 'estado', authNonce: 'nonce' });
  });

  it('una clave suelta de flujo se persiste y cualquier otra no', () => {
    const flow = new MapStore();
    const storage = new TokenFreeStorage(flow);

    storage.write('codeVerifier', 'v');
    storage.write('authzData', 'AT-SECRETO');

    expect(flow.getItem('codeVerifier')).toBe('v');
    expect(flow.getItem('authzData')).toBeNull();
    expect(storage.read('authzData')).toBe('AT-SECRETO');
  });

  it('remove y clear borran memoria y sessionStorage', () => {
    const flow = new MapStore();
    const storage = new TokenFreeStorage(flow);
    storage.write(CONFIG, fullState);

    storage.clear();

    expect(storage.read(CONFIG)).toBeNull();
    expect(flow.data.size).toBe(0);
  });
});
```

`libs/shared-core/src/auth/session-rules.spec.ts`:

```ts
import { describe, expect, it } from 'vitest';
import { cognitoLogoutUrl, isPrivateApiRequest, safeReturnUrl } from './index';

describe('isPrivateApiRequest (FR-04)', () => {
  it.each([
    ['http://localhost:5000/api/v1/me', 'http://localhost:5000', true],
    ['http://localhost:5000/api/v1/route-plans?limit=5', 'http://localhost:5000/', true],
    ['/api/v1/me', '', true],
    ['http://localhost:5000/api/v1/public/trace/abc', 'http://localhost:5000', false],
    ['/api/v1/public/trace/abc', '', false],
    ['http://localhost:5000.evil.example/api/v1/me', 'http://localhost:5000', false],
    ['https://tiles.example.org/api/1/2.png', '', false],
    ['http://localhost:5000/health/ready', 'http://localhost:5000', false],
  ])('%s con apiUrl «%s» → %s', (url, apiUrl, expected) => {
    expect(isPrivateApiRequest(url, apiUrl)).toBe(expected);
  });
});

describe('safeReturnUrl', () => {
  it.each([
    ['/dashboard', '/dashboard'],
    ['/users?tab=2', '/users?tab=2'],
    ['//evil.example', null],
    ['/\\evil.example', null],
    ['https://evil.example/dashboard', null],
    ['javascript:alert(1)', null],
    ['/login?returnUrl=/dashboard', null],
    ['/auth/callback?code=x', null],
    ['', null],
    [null, null],
  ])('%s → %s', (value, expected) => {
    expect(safeReturnUrl(value)).toBe(expected);
  });
});

describe('cognitoLogoutUrl', () => {
  it('arma el cierre de la página administrada con el logout_uri exacto', () => {
    expect(cognitoLogoutUrl('https://istpetdev-dev-123.auth.us-east-1.amazoncognito.com', 'abc123', 'http://localhost:4200'))
      .toBe('https://istpetdev-dev-123.auth.us-east-1.amazoncognito.com/logout?client_id=abc123&logout_uri=http%3A%2F%2Flocalhost%3A4200');
  });
});
```

- [x] **Step 3: Ejecutar y verificar que fallan**

Run: `npm run test:shared`
Expected: FAIL con «Failed to resolve import "./token-free-storage"».

- [x] **Step 4: Implementación mínima**

`libs/shared-core/src/auth/token-free-storage.ts`:

```ts
/** Claves del flujo PKCE que deben sobrevivir a la redirección a Cognito. Ninguna es un token. */
export const AUTH_FLOW_KEYS: readonly string[] = ['codeVerifier', 'authStateControl', 'authNonce'];

export interface KeyValueStore {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

/**
 * Almacenamiento para angular-auth-oidc-client (FR-03 de 013). Todo vive en memoria; solo el
 * estado del flujo PKCE se copia a sessionStorage. Se registra como `AbstractSecurityStorage`.
 */
export class TokenFreeStorage {
  private readonly memory = new Map<string, string>();

  constructor(private readonly flowStore: KeyValueStore | null = typeof sessionStorage === 'undefined' ? null : sessionStorage) {}

  read(key: string): string | null {
    return this.memory.get(key) ?? this.flowStore?.getItem(key) ?? null;
  }

  write(key: string, value: string): void {
    this.memory.set(key, value);
    const flow = flowStateOf(key, value);
    if (flow === null) this.flowStore?.removeItem(key);
    else this.flowStore?.setItem(key, flow);
  }

  remove(key: string): void {
    this.memory.delete(key);
    this.flowStore?.removeItem(key);
  }

  clear(): void {
    for (const key of this.memory.keys()) this.flowStore?.removeItem(key);
    this.memory.clear();
  }
}

// La librería guarda un objeto JSON por configuración; también se admite una clave suelta.
function flowStateOf(key: string, value: string): string | null {
  if (AUTH_FLOW_KEYS.includes(key)) return value;
  let parsed: unknown;
  try {
    parsed = JSON.parse(value);
  } catch {
    return null;
  }
  if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) return null;
  const flow = Object.fromEntries(Object.entries(parsed).filter(([name]) => AUTH_FLOW_KEYS.includes(name)));
  return Object.keys(flow).length > 0 ? JSON.stringify(flow) : null;
}
```

`libs/shared-core/src/auth/api-request.ts`:

```ts
/** FR-04: el token solo viaja a la API configurada y nunca a la traza pública. */
export function isPrivateApiRequest(url: string, apiUrl: string): boolean {
  const base = apiUrl.replace(/\/+$/, '');
  if (!url.startsWith(`${base}/api/`)) return false;
  return !url.startsWith(`${base}/api/v1/public/`);
}
```

`libs/shared-core/src/auth/return-url.ts`:

```ts
/** Destino tras volver de Cognito: solo rutas internas, nunca otro sitio ni las rutas del propio login. */
export function safeReturnUrl(value: string | null | undefined): string | null {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) return null;
  if (value.startsWith('/login') || value.startsWith('/auth/')) return null;
  return value;
}
```

`libs/shared-core/src/auth/cognito-logout.ts`:

```ts
/** El descubrimiento de Cognito no publica end_session_endpoint: el cierre se arma a mano. */
export function cognitoLogoutUrl(cognitoDomain: string, clientId: string, logoutUri: string): string {
  const url = new URL('/logout', cognitoDomain);
  url.searchParams.set('client_id', clientId);
  url.searchParams.set('logout_uri', logoutUri);
  return url.toString();
}
```

`libs/shared-core/src/auth/index.ts`:

```ts
export * from './token-free-storage';
export * from './api-request';
export * from './return-url';
export * from './cognito-logout';
```

Añadir al final de `libs/shared-core/src/index.ts`:

```ts
export * from './auth';
```

- [x] **Step 5: Ejecutar y verificar que pasan**

Run: `npm run test:shared && npm run build:shared`
Expected: PASS (4 + 19 casos) y `dist/` sin archivos `.spec`.

- [x] **Step 6: Commit**

```bash
git add libs/shared-core package.json package-lock.json
git commit -m "feat(shared-core): almacenamiento sin tokens y reglas de sesion (spec 013 fr-03 fr-04)"
```

---

### Task 6: Web — configuración, sesión real, guards, login y retiro de mocks

**Files:**
- Modify: `apps/web/package.json`, `apps/web/angular.json`, `apps/web/tsconfig.app.json`; Create: `apps/web/tsconfig.spec.json`
- Modify: `apps/web/src/shared/config/environment.ts`, `apps/web/src/shared/config/auth.config.ts`; Create: `apps/web/src/shared/config/environment.production.ts`, `apps/web/src/shared/config/environment.spec.ts`
- Create: `apps/web/src/shared/api/api-auth.interceptor.ts` (+ `.spec.ts`); Modify: `apps/web/src/shared/api/index.ts`
- Create: `apps/web/src/entities/session/model/auth-session.ts` (+ `.spec.ts`), `apps/web/src/entities/session/lib/navigate-external.ts`, `apps/web/src/entities/session/lib/auth.guard.ts`, `apps/web/src/entities/session/lib/role.guard.ts`, `apps/web/src/entities/session/lib/guards.spec.ts`
- Modify: `apps/web/src/entities/session/index.ts`
- Delete: `apps/web/src/entities/session/model/session.store.ts`, `apps/web/src/entities/session/model/session.model.ts`, `apps/web/src/features/login-form/`, `apps/web/src/app/providers/auth.interceptor.ts`, `apps/web/src/app/providers/role.guard.ts`, `apps/web/src/shared/api/mock/`
- Create: `apps/web/src/pages/auth-callback/auth-callback.page.ts`, `apps/web/src/pages/auth-callback/index.ts`
- Modify: `apps/web/src/app/app.config.ts`, `apps/web/src/app/app.routes.ts`, `apps/web/src/pages/index.ts`, `apps/web/src/pages/login/login.page.ts`, `apps/web/src/pages/inicio/inicio.page.ts`, `apps/web/src/pages/datos/datos.page.ts`, `apps/web/src/pages/rutas/rutas.page.ts`, `apps/web/src/widgets/header/header.widget.ts`
- Test: `apps/web/src/pages/rutas/rutas.page.spec.ts`, `apps/web/src/widgets/header/header.widget.spec.ts`

**Interfaces:**
- Consumes: `TokenFreeStorage`, `isPrivateApiRequest`, `safeReturnUrl`, `cognitoLogoutUrl`, `MeResponse`, `HumanRole` (T2, T5); `GET /api/v1/me`.
- Produces: `AuthSession` con `status: Signal<SessionStatus>`, `me: Signal<MeResponse | null>`, `role: Signal<HumanRole | null>`, `canWrite: Signal<boolean>`, `bootstrap(): Promise<void>`, `login(returnUrl?: string | null): void`, `takeReturnUrl(): string`, `logout(): void`; `SessionStatus = 'loading' | 'anonymous' | 'authenticated' | 'wrong-platform' | 'disabled' | 'error'`; `authGuard`, `roleGuard` (lee `route.data['roles']: HumanRole[]`); `NAVIGATE_EXTERNAL`; `apiAuthInterceptor`; `environment.oidc { authority, cognitoDomain, clientId, redirectUrl, postLogoutRedirectUri }`.

- [x] **Step 1: Habilitar el runner de pruebas**

Run: `npm install -D -E vitest@4 jsdom@27 --workspace=apps/web`

En `apps/web/package.json`, dentro de `scripts`, añadir `"test": "ng test --watch=false"`. En `apps/web/angular.json`, dentro de `architect`, añadir:

```json
        "test": {
          "builder": "@angular/build:unit-test",
          "options": {
            "buildTarget": "web:build:development",
            "tsConfig": "tsconfig.spec.json",
            "runner": "vitest"
          }
        },
```

y en `architect.build.configurations.production` añadir:

```json
              "fileReplacements": [
                { "replace": "src/shared/config/environment.ts", "with": "src/shared/config/environment.production.ts" }
              ],
```

Crear `apps/web/tsconfig.spec.json`:

```json
{
  "extends": "./tsconfig.json",
  "compilerOptions": {
    "outDir": "./dist/out-tsc/spec",
    "types": ["vitest/globals"]
  },
  "include": ["src/**/*.spec.ts", "src/**/*.d.ts"]
}
```

En `apps/web/tsconfig.app.json` añadir `"exclude": ["src/**/*.spec.ts"]`.

- [x] **Step 2: Escribir las pruebas que fallan**

`apps/web/src/shared/config/environment.spec.ts`:

```ts
import { describe, expect, it } from 'vitest';
import { environment } from './environment';
import { environment as production } from './environment.production';

const ISSUER = /^https:\/\/cognito-idp\.[a-z0-9-]+\.amazonaws\.com\/[a-z0-9-]+_[A-Za-z0-9]+$/;
const DOMAIN = /^https:\/\/[a-z0-9-]+\.auth\.[a-z0-9-]+\.amazoncognito\.com$/;

describe('configuración OIDC pública (FR-02)', () => {
  it.each([
    ['desarrollo', environment, 'http://localhost:4200/auth/callback', 'http://localhost:4200'],
    ['producción', production, 'https://istpetdev.doicela.dev/auth/callback', 'https://istpetdev.doicela.dev'],
  ])('%s usa los valores de Terraform y las URL exactas', (_name, env, redirect, logout) => {
    expect(env.oidc.authority).toMatch(ISSUER);
    expect(env.oidc.cognitoDomain).toMatch(DOMAIN);
    expect(env.oidc.clientId).toMatch(/^[a-z0-9]{20,32}$/);
    expect(env.oidc.redirectUrl).toBe(redirect);
    expect(env.oidc.postLogoutRedirectUri).toBe(logout);
    expect(JSON.stringify(env)).not.toMatch(/secret|password|istpetdev\.local|«/i);
  });
});
```

`apps/web/src/shared/api/api-auth.interceptor.spec.ts`:

```ts
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../config/environment';
import { apiAuthInterceptor } from './api-auth.interceptor';

describe('apiAuthInterceptor (FR-04)', () => {
  beforeEach(() => TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([apiAuthInterceptor])),
      provideHttpClientTesting(),
      { provide: OidcSecurityService, useValue: { getAccessToken: () => of('token-de-prueba') } },
    ],
  }));

  it('añade Bearer solo a la API privada', () => {
    const http = TestBed.inject(HttpClient);
    const backend = TestBed.inject(HttpTestingController);
    const privateUrl = `${environment.apiUrl}/api/v1/me`;
    const publicUrl = `${environment.apiUrl}/api/v1/public/trace/abc`;
    const tileUrl = 'https://tiles.example.org/1/2/3.png';

    http.get(privateUrl).subscribe();
    http.get(publicUrl).subscribe();
    http.get(tileUrl).subscribe();

    expect(backend.expectOne(privateUrl).request.headers.get('Authorization')).toBe('Bearer token-de-prueba');
    expect(backend.expectOne(publicUrl).request.headers.has('Authorization')).toBe(false);
    expect(backend.expectOne(tileUrl).request.headers.has('Authorization')).toBe(false);
  });
});
```

`apps/web/src/entities/session/model/auth-session.spec.ts`:

```ts
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { environment } from '@shared/config';
import { NAVIGATE_EXTERNAL } from '../lib/navigate-external';
import { AuthSession } from './auth-session';

const ME_URL = `${environment.apiUrl}/api/v1/me`;

function setup(isAuthenticated: boolean) {
  const oidc = { checkAuth: vi.fn(() => of({ isAuthenticated })), authorize: vi.fn(), logoffLocal: vi.fn() };
  const navigate = vi.fn();
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(), provideHttpClientTesting(),
      { provide: OidcSecurityService, useValue: oidc },
      { provide: NAVIGATE_EXTERNAL, useValue: navigate },
    ],
  });
  return { session: TestBed.inject(AuthSession), backend: TestBed.inject(HttpTestingController), oidc, navigate };
}

async function answerMe(backend: HttpTestingController, body: object, status = 200) {
  const request = await vi.waitFor(() => backend.expectOne(ME_URL));
  request.flush(body, { status, statusText: status === 200 ? 'OK' : 'Forbidden' });
}

describe('AuthSession', () => {
  beforeEach(() => { localStorage.clear(); sessionStorage.clear(); });

  it('sin sesión de Cognito queda anónima y no llama a /me', async () => {
    const { session, backend } = setup(false);
    await session.bootstrap();
    expect(session.status()).toBe('anonymous');
    backend.expectNone(ME_URL);
  });

  it('una cuenta web queda autenticada con su rol y sin escribir localStorage', async () => {
    const { session, backend } = setup(true);
    const done = session.bootstrap();
    await answerMe(backend, { accountId: 'a-1', displayName: 'Planificación', role: 'Planificador', platform: 'web' });
    await done;
    expect(session.status()).toBe('authenticated');
    expect(session.role()).toBe('Planificador');
    expect(session.canWrite()).toBe(true);
    expect(localStorage.length).toBe(0);
  });

  it('Auditor entra a la web sin permisos de escritura', async () => {
    const { session, backend } = setup(true);
    const done = session.bootstrap();
    await answerMe(backend, { accountId: 'a-2', displayName: 'Auditoría', role: 'Auditor', platform: 'web' });
    await done;
    expect(session.status()).toBe('authenticated');
    expect(session.canWrite()).toBe(false);
  });

  it('un Conductor en la web queda en plataforma equivocada', async () => {
    const { session, backend } = setup(true);
    const done = session.bootstrap();
    await answerMe(backend, { accountId: 'a-3', displayName: 'Conductor', role: 'Conductor', platform: 'field' });
    await done;
    expect(session.status()).toBe('wrong-platform');
  });

  it('403 de /me significa cuenta no habilitada', async () => {
    const { session, backend } = setup(true);
    const done = session.bootstrap();
    await answerMe(backend, { code: 'forbidden' }, 403);
    await done;
    expect(session.status()).toBe('disabled');
    expect(session.me()).toBeNull();
  });

  it('login guarda solo un returnUrl seguro y abre Cognito', () => {
    const { session, oidc } = setup(false);
    session.login('//evil.example');
    expect(sessionStorage.length).toBe(0);
    expect(localStorage.length).toBe(0);
    session.login('/dashboard?vista=mapa');
    expect(localStorage.getItem('istpetdev.returnUrl')).toBe('/dashboard?vista=mapa');
    expect(sessionStorage.length).toBe(0);
    expect(session.takeReturnUrl()).toBe('/dashboard?vista=mapa');
    expect(localStorage.length).toBe(0);
    expect(session.takeReturnUrl()).toBe('/dashboard');
    expect(oidc.authorize).toHaveBeenCalledTimes(2);
  });

  it('usa /dashboard tras Cognito si el returnUrl apuntaba a otro origen (Review Focus 1)', async () => {
    const { session, backend } = setup(true);
    session.login('https://evil.example/dashboard');
    expect(localStorage.length).toBe(0);
    const done = session.bootstrap();
    await answerMe(backend, { accountId: 'a-1', displayName: 'Planificación', role: 'Planificador', platform: 'web' });
    await done;
    expect(session.takeReturnUrl()).toBe('/dashboard');
  });

  it('logout borra la sesión local y cierra la de Cognito', async () => {
    const { session, backend, oidc, navigate } = setup(true);
    const done = session.bootstrap();
    await answerMe(backend, { accountId: 'a-1', displayName: 'Planificación', role: 'Planificador', platform: 'web' });
    await done;

    session.logout();

    expect(session.status()).toBe('anonymous');
    expect(oidc.logoffLocal).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(
      `${environment.oidc.cognitoDomain}/logout?client_id=${environment.oidc.clientId}&logout_uri=${encodeURIComponent(environment.oidc.postLogoutRedirectUri)}`,
    );
  });
});
```

`apps/web/src/entities/session/lib/guards.spec.ts`:

```ts
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { AuthSession, SessionStatus } from '../model/auth-session';
import { authGuard } from './auth.guard';
import { roleGuard } from './role.guard';

function guard(status: SessionStatus, role: string | null, run: () => unknown) {
  TestBed.configureTestingModule({
    providers: [provideRouter([]), { provide: AuthSession, useValue: { status: () => status, role: () => role } }],
  });
  return TestBed.runInInjectionContext(run);
}

const state = { url: '/dashboard' } as RouterStateSnapshot;

describe('authGuard (FR-08)', () => {
  it('deja pasar una sesión web válida', () => {
    expect(guard('authenticated', 'Gerencia', () => authGuard({} as ActivatedRouteSnapshot, state))).toBe(true);
  });

  it('sin sesión manda a /login con returnUrl', () => {
    const tree = guard('anonymous', null, () => authGuard({} as ActivatedRouteSnapshot, state)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(tree).startsWith('/login')).toBe(true);
    expect(tree.queryParams).toEqual({ returnUrl: '/dashboard' });
  });

  it.each(['wrong-platform', 'disabled', 'error'] as const)('%s va a /login sin returnUrl', (status) => {
    const tree = guard(status, null, () => authGuard({} as ActivatedRouteSnapshot, state)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(tree)).toBe('/login');
  });
});

describe('roleGuard', () => {
  const route = { data: { roles: ['Gerencia'] } } as unknown as ActivatedRouteSnapshot;

  it('admite el rol listado', () => {
    expect(guard('authenticated', 'Gerencia', () => roleGuard(route, state))).toBe(true);
  });

  it('devuelve al panel a cualquier otro rol', () => {
    const tree = guard('authenticated', 'Auditor', () => roleGuard(route, state)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(tree)).toBe('/dashboard');
  });
});
```

- [x] **Step 3: Ejecutar y verificar que fallan**

Run: `npm run test:web`
Expected: FAIL con «Could not resolve "./auth-session"» y, en `environment.spec.ts`, «Cannot read properties of undefined (reading 'authority')».

- [x] **Step 4: Configuración pública por entorno**

Leer los valores: `terraform -chdir=infra/terraform/environments/dev output -json cognito`. Copiar `issuer` en `authority`, `domain` en `cognitoDomain` y `client_ids.web` en `clientId` en **ambos** archivos (el pool de dev sirve a local y a la demo).

`apps/web/src/shared/config/environment.ts`:

```ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',
  appName: 'IstpetDev Web',
  version: '1.0.0',
  // Valores públicos del output «cognito» de Terraform (FR-02). Ningún secreto.
  oidc: {
    authority: '«issuer»',
    cognitoDomain: '«domain»',
    clientId: '«client_ids.web»',
    redirectUrl: 'http://localhost:4200/auth/callback',
    postLogoutRedirectUri: 'http://localhost:4200',
  },
};
```

`apps/web/src/shared/config/environment.production.ts`:

```ts
import { environment as development } from './environment';

// La API se sirve en el mismo origen detrás de Nginx (/api/).
export const environment = {
  ...development,
  production: true,
  apiUrl: '',
  oidc: {
    ...development.oidc,
    redirectUrl: 'https://istpetdev.doicela.dev/auth/callback',
    postLogoutRedirectUri: 'https://istpetdev.doicela.dev',
  },
};
```

Reemplazar `apps/web/src/shared/config/auth.config.ts`:

```ts
import { LogLevel, PassedInitialConfig } from 'angular-auth-oidc-client';
import { environment } from './environment';

/** Authorization Code + PKCE S256 contra Cognito (ADR-10, ADR-18, FR-01 de 013). */
export const webOidcConfig: PassedInitialConfig = {
  config: {
    authority: environment.oidc.authority,
    clientId: environment.oidc.clientId,
    redirectUrl: environment.oidc.redirectUrl,
    postLogoutRedirectUri: environment.oidc.postLogoutRedirectUri,
    scope: 'openid email profile istpetdev-api/access',
    responseType: 'code',
    silentRenew: true,
    useRefreshToken: true,
    renewTimeBeforeTokenExpiresInSeconds: 60,
    ignoreNonceAfterRefresh: true,
    logLevel: LogLevel.Warn,
  },
};
```

- [x] **Step 5: Comprobar cómo escribe la librería en el almacenamiento**

Run: `grep -n "write(" node_modules/angular-auth-oidc-client/fesm2022/angular-auth-oidc-client.mjs | grep -i "storage" | head`
Expected: la escritura usa la clave de configuración y un JSON con todas las claves (`this.abstractSecurityStorage.write(configId, JSON.stringify(...))` o equivalente). `TokenFreeStorage` cubre ese formato y el de clave suelta; si aparece un tercer formato, añadir su caso a `token-free-storage.spec.ts` antes de seguir.

- [x] **Step 6: Sesión, guards e interceptor**

`apps/web/src/entities/session/lib/navigate-external.ts`:

```ts
import { InjectionToken } from '@angular/core';

/** Salida a una URL fuera de la app (cierre de Cognito). Inyectable para las pruebas. */
export const NAVIGATE_EXTERNAL = new InjectionToken<(url: string) => void>('NAVIGATE_EXTERNAL', {
  providedIn: 'root',
  factory: () => (url: string) => window.location.assign(url),
});
```

`apps/web/src/entities/session/model/auth-session.ts`:

```ts
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';
import { HumanRole, MeResponse, cognitoLogoutUrl, safeReturnUrl } from '@istpetdev/shared-core';
import { environment } from '@shared/config';
import { NAVIGATE_EXTERNAL } from '../lib/navigate-external';

export type SessionStatus = 'loading' | 'anonymous' | 'authenticated' | 'wrong-platform' | 'disabled' | 'error';

const RETURN_URL_KEY = 'istpetdev.returnUrl';

/** Sesión de la web: identidad de Cognito + cuenta de GET /api/v1/me (FR-06). Nada se persiste salvo el returnUrl. */
@Injectable({ providedIn: 'root' })
export class AuthSession {
  private readonly oidc = inject(OidcSecurityService);
  private readonly http = inject(HttpClient);
  private readonly navigateExternal = inject(NAVIGATE_EXTERNAL);
  private readonly state = signal<{ status: SessionStatus; me: MeResponse | null }>({ status: 'loading', me: null });

  readonly status = computed(() => this.state().status);
  readonly me = computed(() => this.state().me);
  readonly role = computed<HumanRole | null>(() => this.state().me?.role ?? null);
  readonly canWrite = computed(() => this.role() === 'Gerencia' || this.role() === 'Planificador');

  async bootstrap(): Promise<void> {
    const { isAuthenticated } = await firstValueFrom(this.oidc.checkAuth());
    if (!isAuthenticated) {
      this.state.set({ status: 'anonymous', me: null });
      return;
    }
    try {
      const me = await firstValueFrom(this.http.get<MeResponse>(`${environment.apiUrl}/api/v1/me`));
      this.state.set({ status: me.platform === 'web' ? 'authenticated' : 'wrong-platform', me });
    } catch (error) {
      const disabled = error instanceof HttpErrorResponse && error.status === 403;
      this.state.set({ status: disabled ? 'disabled' : 'error', me: null });
    }
  }

  login(returnUrl?: string | null): void {
    const target = safeReturnUrl(returnUrl);
    try {
      if (target) localStorage.setItem(RETURN_URL_KEY, target);
      else localStorage.removeItem(RETURN_URL_KEY);
    } catch {
      // Sin localStorage se vuelve al panel.
    }
    this.oidc.authorize();
  }

  takeReturnUrl(): string {
    let stored: string | null = null;
    try {
      stored = localStorage.getItem(RETURN_URL_KEY);
      localStorage.removeItem(RETURN_URL_KEY);
    } catch {
      // Sin localStorage se vuelve al panel.
    }
    return safeReturnUrl(stored) ?? '/dashboard';
  }

  logout(): void {
    this.state.set({ status: 'anonymous', me: null });
    this.oidc.logoffLocal();
    this.navigateExternal(cognitoLogoutUrl(environment.oidc.cognitoDomain, environment.oidc.clientId, environment.oidc.postLogoutRedirectUri));
  }
}
```

`apps/web/src/entities/session/lib/auth.guard.ts`:

```ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthSession } from '../model/auth-session';

/** FR-08: toda ruta privada exige una cuenta web activa; sin sesión vuelve a /login recordando el destino. */
export const authGuard: CanActivateFn = (_route, state) => {
  const session = inject(AuthSession);
  const router = inject(Router);
  if (session.status() === 'authenticated') return true;
  return session.status() === 'anonymous'
    ? router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } })
    : router.createUrlTree(['/login']);
};
```

`apps/web/src/entities/session/lib/role.guard.ts`:

```ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { HumanRole } from '@istpetdev/shared-core';
import { AuthSession } from '../model/auth-session';

/** Usar después de authGuard: `data: { roles: ['Gerencia'] }`. */
export const roleGuard: CanActivateFn = (route) => {
  const allowed = (route.data['roles'] as HumanRole[] | undefined) ?? [];
  const role = inject(AuthSession).role();
  return role !== null && allowed.includes(role) ? true : inject(Router).createUrlTree(['/dashboard']);
};
```

Reemplazar `apps/web/src/entities/session/index.ts`:

```ts
export * from './model/auth-session';
export * from './lib/auth.guard';
export * from './lib/role.guard';
export * from './lib/navigate-external';
```

`apps/web/src/shared/api/api-auth.interceptor.ts`:

```ts
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { switchMap, take } from 'rxjs';
import { isPrivateApiRequest } from '@istpetdev/shared-core';
import { environment } from '../config/environment';

/** FR-04: Bearer solo hacia la API configurada; la traza pública y terceros no lo reciben. */
export const apiAuthInterceptor: HttpInterceptorFn = (request, next) => {
  if (!isPrivateApiRequest(request.url, environment.apiUrl)) return next(request);
  const oidc = inject(OidcSecurityService);
  return oidc.getAccessToken().pipe(
    take(1),
    switchMap((token) => next(token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request)),
  );
};
```

Añadir a `apps/web/src/shared/api/index.ts`:

```ts
export * from './api-auth.interceptor';
```

- [x] **Step 7: Ejecutar y verificar que pasan las pruebas de lógica**

Run: `npm run test:web`
Expected: PASS en `environment.spec.ts` (si siguen los `«…»`, FAIL con el regex de `authority`: completar Step 4), `api-auth.interceptor.spec.ts`, `auth-session.spec.ts` y `guards.spec.ts`.

- [x] **Step 8: Conectar la app, retirar sesión y mocks simulados**

Run: `git rm -r apps/web/src/entities/session/model/session.store.ts apps/web/src/entities/session/model/session.model.ts apps/web/src/features/login-form apps/web/src/app/providers/auth.interceptor.ts apps/web/src/app/providers/role.guard.ts apps/web/src/shared/api/mock`

Reemplazar `apps/web/src/app/app.config.ts`:

```ts
import { ApplicationConfig, inject, provideAppInitializer, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AbstractSecurityStorage, provideAuth } from 'angular-auth-oidc-client';
import { TokenFreeStorage } from '@istpetdev/shared-core';
import { routes } from './app.routes';
import { webOidcConfig } from '@shared/config';
import { WEB_FIELD_API, HttpWebFieldApiService, apiAuthInterceptor } from '@shared/api';
import { AuthSession } from '@entities/session';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({
        anchorScrolling: 'enabled',
        scrollPositionRestoration: 'enabled'
      })
    ),
    provideHttpClient(withInterceptors([apiAuthInterceptor])),
    provideAuth(webOidcConfig),
    // FR-03: tokens solo en memoria; reemplaza el almacenamiento por defecto de la librería.
    { provide: AbstractSecurityStorage, useFactory: () => new TokenFreeStorage() },
    provideAppInitializer(() => inject(AuthSession).bootstrap()),
    {
      provide: WEB_FIELD_API,
      useClass: HttpWebFieldApiService
    }
  ]
};
```

Adaptar `apps/web/src/app/app.routes.ts` al orden de proceso de la web vigente:

```ts
import { Routes } from '@angular/router';
import { authGuard, homeRedirectGuard, roleGuard } from '@entities/session';

const WEB_READ_ROLES = ['Gerencia', 'Planificador', 'Auditor'] as const;
const WEB_WRITE_ROLES = ['Gerencia', 'Planificador'] as const;

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('@pages/landing').then(m => m.LandingPage),
    title: 'IstpetDev'
  },
  {
    path: 'login',
    loadComponent: () => import('@pages/login').then(m => m.LoginPage),
    title: 'Iniciar sesión — IstpetDev'
  },
  {
    path: 'auth/callback',
    loadComponent: () => import('@pages/auth-callback').then(m => m.AuthCallbackPage),
    title: 'Verificando sesión — IstpetDev'
  },
  {
    path: 'inicio',
    canActivate: [authGuard, roleGuard],
    data: { roles: WEB_READ_ROLES },
    loadComponent: () => import('@pages/inicio').then(m => m.InicioPage),
    title: 'Inicio — IstpetDev'
  },
  {
    path: 'datos',
    canActivate: [authGuard, roleGuard],
    data: { roles: WEB_WRITE_ROLES },
    loadComponent: () => import('@pages/datos').then(m => m.DatosPage),
    title: 'Cargar datos — IstpetDev'
  },
  {
    path: 'alertas',
    canActivate: [authGuard, roleGuard],
    data: { roles: WEB_READ_ROLES },
    loadComponent: () => import('@pages/alertas').then(m => m.AlertasPage),
    title: 'Alertas — IstpetDev'
  },
  {
    path: 'rutas',
    canActivate: [authGuard, roleGuard],
    data: { roles: WEB_READ_ROLES },
    loadComponent: () => import('@pages/rutas').then(m => m.RutasPage),
    title: 'Rutas — IstpetDev'
  },
  {
    path: 'public-trace',
    loadComponent: () => import('@pages/public-trace').then(m => m.PublicTracePage),
    title: 'Trazabilidad QR — IstpetDev'
  },
  {
    path: 'public-trace/:unitId',
    loadComponent: () => import('@pages/public-trace').then(m => m.PublicTracePage),
    title: 'Trazabilidad QR — IstpetDev'
  },
  {
    path: 'dashboard',
    canActivate: [homeRedirectGuard],
    children: []
  },
  {
    path: '**',
    redirectTo: ''
  }
];
```

En `apps/web/src/pages/index.ts`, añadir `export * from './auth-callback/auth-callback.page';`.

`apps/web/src/pages/auth-callback/auth-callback.page.ts`:

```ts
import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthSession } from '@entities/session';

/** Vuelta de Cognito: el inicializador ya canjeó el código; aquí solo se decide el destino. */
@Component({
  selector: 'page-auth-callback',
  standalone: true,
  template: `<p class="p-8 text-center text-sm text-slate-500">Verificando tu cuenta…</p>`
})
export class AuthCallbackPage implements OnInit {
  private readonly session = inject(AuthSession);
  private readonly router = inject(Router);

  ngOnInit(): void {
    const target = this.session.status() === 'authenticated' ? this.session.takeReturnUrl() : '/login';
    void this.router.navigateByUrl(target, { replaceUrl: true });
  }
}
```

`apps/web/src/pages/auth-callback/index.ts`:

```ts
export * from './auth-callback.page';
```

Reemplazar `apps/web/src/pages/login/login.page.ts`:

```ts
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthSession } from '@entities/session';
import { ButtonComponent, CardComponent } from '@shared/ui';

/** FR-01: esta página no recibe contraseñas; abre la página administrada de Cognito. */
@Component({
  selector: 'page-login',
  standalone: true,
  imports: [RouterLink, ButtonComponent, CardComponent],
  template: `
    <div class="min-h-[75vh] flex flex-col items-center justify-center py-6 sm:py-12">
      <div class="w-full max-w-md space-y-6 text-center">
        <div class="space-y-2">
          <div class="inline-flex items-center justify-center h-12 w-12 rounded-xl bg-primary-600 text-white font-black text-2xl shadow-md">I</div>
          <h1 class="text-2xl font-extrabold text-slate-900 tracking-tight">Acceso al sistema logístico</h1>
        </div>
        <ui-card [padding]="'lg'">
          @switch (session.status()) {
            @case ('wrong-platform') {
              <p role="alert" class="text-sm font-semibold text-slate-800 mb-4">Tu cuenta opera en la app de campo</p>
              <ui-button variant="outline" [fullWidth]="true" (buttonClick)="session.logout()">Cerrar sesión</ui-button>
            }
            @case ('disabled') {
              <p role="alert" class="text-sm font-semibold text-slate-800 mb-4">Tu cuenta no está habilitada; contacta a Gerencia</p>
              <ui-button variant="outline" [fullWidth]="true" (buttonClick)="session.logout()">Cerrar sesión</ui-button>
            }
            @case ('error') {
              <p role="alert" class="text-sm text-slate-800 mb-4">No pudimos verificar tu cuenta. Intenta de nuevo en unos minutos.</p>
              <ui-button variant="primary" [fullWidth]="true" (buttonClick)="session.login()">Reintentar</ui-button>
            }
            @default {
              <ui-button variant="primary" size="lg" [fullWidth]="true" (buttonClick)="session.login()">Iniciar sesión</ui-button>
              <p class="text-xs text-slate-500 mt-3">Te llevaremos a la página segura de inicio de sesión. Allí también puedes recuperar tu contraseña.</p>
            }
          }
        </ui-card>
        <a routerLink="/public-trace" class="text-xs text-slate-500 hover:text-primary-600">Consultar trazabilidad sin ingresar</a>
      </div>
    </div>
  `
})
export class LoginPage implements OnInit {
  readonly session = inject(AuthSession);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  ngOnInit(): void {
    if (this.session.status() === 'authenticated') {
      void this.router.navigateByUrl('/dashboard');
      return;
    }
    // H1-2: una recarga de ruta privada vuelve a Cognito sin pedir la contraseña mientras su sesión siga vigente.
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    if (this.session.status() === 'anonymous' && returnUrl) this.session.login(returnUrl);
  }
}
```

En `apps/web/src/widgets/header/header.widget.ts`, usar `AuthSession` y los nombres de `HumanRole`. Mostrar Datos a Gerencia y Planificador; Auditor ve Inicio, Alertas y Rutas en solo lectura. El logout llama a `session.logout()`.

En `rutas.page.ts` y `datos.page.ts`, proteger la UI y los métodos de escritura con `session.canWrite()`. No hay acciones de escritura en `alertas.page.ts`.

- [x] **Step 9: Verificar build, lint y que no quedan restos del mock**

Run: `npm run test:web && npm run build:web && grep -rnE "SessionStore|DEMO_PROFILES|loginWithCredentials|jwt-mock" apps/web/src; echo "grep=$?"` y `grep -rnE "SessionStore|UserRole|'SUPERVISOR'|'ADMIN'|'AUDITOR'|auth/login|MockWebFieldApiService" apps/web/src`
Expected: pruebas PASS, build y lint sin errores; ambos greps sin coincidencias (el primero devuelve `grep=1`). La importación conserva `warehouses.csv` en la guía de Datos; no es una página `pages/warehouse` ni un resto de la sesión simulada.

- [x] **Step 10: Commit**

```bash
git add -A apps/web package-lock.json
git commit -m "feat(web): integrar Cognito PKCE y sesión real desde /me (#42)" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Desviaciones

La web se adaptó al estado posterior a `498edcf`: sus páginas reales son landing, login, inicio, datos, alertas, rutas y public-trace, con `/dashboard` como redirección condicional. `pages/warehouse` y `pages/operations-dashboard` no existían, por lo que no se recrearon ni se borraron; se conservaron las páginas y el orden de proceso diseñados por Alexander. Se retiraron el store, el formulario de login, el interceptor y guard simulados, y el mock de `WEB_FIELD_API`. `AuthSession.canWrite()` protege la importación y las acciones de generación/publicación; Auditor mantiene lectura en Inicio, Alertas y Rutas.

El returnUrl interno validado vive en una clave de un solo uso en `localStorage` y se elimina al consumirlo. Así `sessionStorage` queda reservado a las claves PKCE de OIDC y no se modifica el formato de `authStateControl` que valida la librería. La configuración de producción declara los valores del pool directamente porque el reemplazo de Angular convertiría una importación de `environment.ts` en una autoimportación.

---

### Task 7: PWA — configuración, sesión real, guards, login y retiro de mocks

**Files:**
- Modify: `apps/mobile/package.json`, `apps/mobile/angular.json`, `apps/mobile/tsconfig.app.json`; Create: `apps/mobile/tsconfig.spec.json`, `apps/mobile/src/test-setup.ts`
- Create: `apps/mobile/src/app/config/environment.ts`, `apps/mobile/src/app/config/environment.production.ts`, `apps/mobile/src/app/config/environment.spec.ts`
- Create: `apps/mobile/src/app/auth/navigate-external.ts`, `apps/mobile/src/app/auth/auth.service.spec.ts`, `apps/mobile/src/app/auth/pages/callback/callback.page.ts`
- Modify: `apps/mobile/src/app/auth/{auth.config.ts,auth.models.ts,auth.service.ts,auth.interceptor.ts,role.guard.ts,index.ts,pages/login/login.page.ts}`
- Modify: `apps/mobile/src/app/app.config.ts`, `apps/mobile/src/app/app.routes.ts`, `apps/mobile/src/app/home/home.page.ts`

**Interfaces:**
- Consumes: igual que T6 en `shared-core`; `GET /api/v1/me`.
- Produces: `MobileAuthService` con `status`, `me`, `currentRole: Signal<FieldRole | null>`, `bootstrap()`, `login(returnUrl?)`, `takeReturnUrl()` (por defecto `/home`), `logout(): Promise<void>`; `FieldRole = 'Bodega' | 'Conductor' | 'Receptor'`; `FIELD_ROLE_LABELS`; `roleGuard`; `authInterceptor`.

- [x] **Step 1: Habilitar el runner con IndexedDB falso**

Run: `npm install -D -E vitest@4 jsdom@27 fake-indexeddb@6 --workspace=apps/mobile`

En `apps/mobile/package.json` añadir `"test": "ng test --watch=false"`. En `apps/mobile/angular.json`, dentro de `projects.mobile.architect`, añadir:

```json
        "test": {
          "builder": "@angular/build:unit-test",
          "options": {
            "buildTarget": "mobile:build:development",
            "tsConfig": "tsconfig.spec.json",
            "runner": "vitest",
            "setupFiles": ["src/test-setup.ts"]
          }
        },
```

y en `architect.build.configurations.production`:

```json
              "fileReplacements": [
                { "replace": "src/app/config/environment.ts", "with": "src/app/config/environment.production.ts" }
              ],
```

`apps/mobile/tsconfig.spec.json`: mismo contenido que el de la web (T6 Step 1). `apps/mobile/tsconfig.app.json`: añadir `"exclude": ["src/**/*.spec.ts", "src/test-setup.ts"]`.

`apps/mobile/src/test-setup.ts`:

```ts
import 'fake-indexeddb/auto';
```

- [x] **Step 2: Escribir las pruebas que fallan**

`apps/mobile/src/app/config/environment.spec.ts`:

```ts
import { describe, expect, it } from 'vitest';
import { environment } from './environment';
import { environment as production } from './environment.production';

const ISSUER = /^https:\/\/cognito-idp\.[a-z0-9-]+\.amazonaws\.com\/[a-z0-9-]+_[A-Za-z0-9]+$/;

describe('configuración OIDC pública de la PWA (FR-02)', () => {
  it.each([
    ['desarrollo', environment, 'http://localhost:8100/auth/callback', 'http://localhost:8100'],
    ['producción', production, 'https://istpetdev.doicela.dev/campo/auth/callback', 'https://istpetdev.doicela.dev/campo/'],
  ])('%s usa los valores de Terraform y las URL exactas', (_name, env, redirect, logout) => {
    expect(env.oidc.authority).toMatch(ISSUER);
    expect(env.oidc.cognitoDomain).toMatch(/^https:\/\/[a-z0-9-]+\.auth\.[a-z0-9-]+\.amazoncognito\.com$/);
    expect(env.oidc.clientId).toMatch(/^[a-z0-9]{20,32}$/);
    expect(env.oidc.redirectUrl).toBe(redirect);
    expect(env.oidc.postLogoutRedirectUri).toBe(logout);
    expect(JSON.stringify(env)).not.toMatch(/secret|password|istpetdev\.local|«/i);
  });
});
```

`apps/mobile/src/app/auth/auth.service.spec.ts`:

```ts
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { environment } from '../config/environment';
import { MobileAuthService } from './auth.service';
import { NAVIGATE_EXTERNAL } from './navigate-external';

const ME_URL = `${environment.apiUrl}/api/v1/me`;

function setup(isAuthenticated: boolean) {
  const oidc = { checkAuth: vi.fn(() => of({ isAuthenticated })), authorize: vi.fn(), logoffLocal: vi.fn() };
  const navigate = vi.fn();
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
      { provide: OidcSecurityService, useValue: oidc },
      { provide: NAVIGATE_EXTERNAL, useValue: navigate },
    ],
  });
  return { auth: TestBed.inject(MobileAuthService), backend: TestBed.inject(HttpTestingController), oidc, navigate };
}

async function boot(isAuthenticated: boolean, me?: object, status = 200) {
  const ctx = setup(isAuthenticated);
  const done = ctx.auth.bootstrap();
  if (me) (await vi.waitFor(() => ctx.backend.expectOne(ME_URL))).flush(me, { status, statusText: 'x' });
  await done;
  return ctx;
}

describe('MobileAuthService', () => {
  beforeEach(() => { localStorage.clear(); sessionStorage.clear(); });

  it('un Conductor queda autenticado sin escribir tokens en ningún almacenamiento', async () => {
    const { auth } = await boot(true, { accountId: 'c-1', displayName: 'Conductor', role: 'Conductor', platform: 'field' });
    expect(auth.status()).toBe('authenticated');
    expect(auth.currentRole()).toBe('Conductor');
    expect(localStorage.length + sessionStorage.length).toBe(0);
  });

  it('Gerencia en la PWA queda en plataforma equivocada', async () => {
    const { auth } = await boot(true, { accountId: 'g-1', displayName: 'Gerencia', role: 'Gerencia', platform: 'web' });
    expect(auth.status()).toBe('wrong-platform');
    expect(auth.currentRole()).toBeNull();
  });

  it('403 de /me significa cuenta no habilitada', async () => {
    const { auth } = await boot(true, { code: 'forbidden' }, 403);
    expect(auth.status()).toBe('disabled');
  });

  it('sin sesión queda anónima y el returnUrl por defecto es /home', async () => {
    const { auth } = await boot(false);
    expect(auth.status()).toBe('anonymous');
    expect(auth.takeReturnUrl()).toBe('/home');
  });

  it('logout cierra la sesión de Cognito', async () => {
    const { auth, oidc, navigate } = await boot(true, { accountId: 'c-1', displayName: 'Conductor', role: 'Conductor', platform: 'field' });
    await auth.logout();
    expect(auth.status()).toBe('anonymous');
    expect(oidc.logoffLocal).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(expect.stringContaining(`${environment.oidc.cognitoDomain}/logout?client_id=${environment.oidc.clientId}`));
  });
});
```

- [x] **Step 3: Ejecutar y verificar que fallan**

Run: `npm run test:mobile`
Expected: FAIL con «Could not resolve "../config/environment"» y «Could not resolve "./navigate-external"».

- [x] **Step 4: Configuración, sesión, guard e interceptor**

`apps/mobile/src/app/config/environment.ts` (valores de `terraform output -json cognito`, `client_ids.mobile`):

```ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',
  // Valores públicos del output «cognito» de Terraform (FR-02). Ningún secreto.
  oidc: {
    authority: '«issuer»',
    cognitoDomain: '«domain»',
    clientId: '«client_ids.mobile»',
    redirectUrl: 'http://localhost:8100/auth/callback',
    postLogoutRedirectUri: 'http://localhost:8100',
  },
};
```

`apps/mobile/src/app/config/environment.production.ts`:

```ts
import { environment as development } from './environment';

// La PWA se publica en /campo/ del dominio de la demo; la API en el mismo origen (/api/).
export const environment = {
  ...development,
  production: true,
  apiUrl: '',
  oidc: {
    ...development.oidc,
    redirectUrl: 'https://istpetdev.doicela.dev/campo/auth/callback',
    postLogoutRedirectUri: 'https://istpetdev.doicela.dev/campo/',
  },
};
```

Reemplazar `apps/mobile/src/app/auth/auth.config.ts`:

```ts
import { LogLevel, PassedInitialConfig } from 'angular-auth-oidc-client';
import { environment } from '../config/environment';

/** Authorization Code + PKCE S256 contra Cognito para la PWA (ADR-10, ADR-18, FR-01 de 013). */
export const mobileOidcConfig: PassedInitialConfig = {
  config: {
    authority: environment.oidc.authority,
    clientId: environment.oidc.clientId,
    redirectUrl: environment.oidc.redirectUrl,
    postLogoutRedirectUri: environment.oidc.postLogoutRedirectUri,
    scope: 'openid email profile istpetdev-api/access',
    responseType: 'code',
    silentRenew: true,
    useRefreshToken: true,
    renewTimeBeforeTokenExpiresInSeconds: 60,
    ignoreNonceAfterRefresh: true,
    logLevel: LogLevel.Warn,
  },
};
```

Reemplazar `apps/mobile/src/app/auth/auth.models.ts`:

```ts
/** Roles que operan en la PWA (ADR-24). La identidad y el rol vienen de GET /api/v1/me. */
export type FieldRole = 'Bodega' | 'Conductor' | 'Receptor';

export const FIELD_ROLE_LABELS: Record<FieldRole, string> = {
  Bodega: 'Bodega',
  Conductor: 'Conductor',
  Receptor: 'Receptor',
};

export type FieldSessionStatus = 'loading' | 'anonymous' | 'authenticated' | 'wrong-platform' | 'disabled' | 'error';
```

`apps/mobile/src/app/auth/navigate-external.ts`:

```ts
import { InjectionToken } from '@angular/core';

/** Salida a una URL fuera de la app (cierre de Cognito). Inyectable para las pruebas. */
export const NAVIGATE_EXTERNAL = new InjectionToken<(url: string) => void>('NAVIGATE_EXTERNAL', {
  providedIn: 'root',
  factory: () => (url: string) => window.location.assign(url),
});
```

Reemplazar `apps/mobile/src/app/auth/auth.service.ts`:

```ts
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';
import { MeResponse, cognitoLogoutUrl, safeReturnUrl } from '@istpetdev/shared-core';
import { environment } from '../config/environment';
import { FieldRole, FieldSessionStatus } from './auth.models';
import { NAVIGATE_EXTERNAL } from './navigate-external';

const RETURN_URL_KEY = 'istpetdev.returnUrl';
const FIELD_ROLES: readonly string[] = ['Bodega', 'Conductor', 'Receptor'];

/** Sesión de la PWA: identidad de Cognito + cuenta de GET /api/v1/me (FR-06). Tokens solo en memoria (FR-03). */
@Injectable({ providedIn: 'root' })
export class MobileAuthService {
  private readonly oidc = inject(OidcSecurityService);
  private readonly http = inject(HttpClient);
  private readonly navigateExternal = inject(NAVIGATE_EXTERNAL);
  private readonly state = signal<{ status: FieldSessionStatus; me: MeResponse | null }>({ status: 'loading', me: null });

  readonly status = computed(() => this.state().status);
  readonly me = computed(() => this.state().me);
  readonly currentRole = computed<FieldRole | null>(() => {
    const role = this.state().status === 'authenticated' ? this.state().me?.role : undefined;
    return role && FIELD_ROLES.includes(role) ? (role as FieldRole) : null;
  });

  async bootstrap(): Promise<void> {
    const { isAuthenticated } = await firstValueFrom(this.oidc.checkAuth());
    if (!isAuthenticated) {
      this.state.set({ status: 'anonymous', me: null });
      return;
    }
    try {
      const me = await firstValueFrom(this.http.get<MeResponse>(`${environment.apiUrl}/api/v1/me`));
      this.state.set({ status: me.platform === 'field' ? 'authenticated' : 'wrong-platform', me });
    } catch (error) {
      const disabled = error instanceof HttpErrorResponse && error.status === 403;
      this.state.set({ status: disabled ? 'disabled' : 'error', me: null });
    }
  }

  login(returnUrl?: string | null): void {
    const target = safeReturnUrl(returnUrl);
    try {
      if (target) sessionStorage.setItem(RETURN_URL_KEY, target);
      else sessionStorage.removeItem(RETURN_URL_KEY);
    } catch {
      // Sin sessionStorage se vuelve al inicio.
    }
    this.oidc.authorize();
  }

  takeReturnUrl(): string {
    let stored: string | null = null;
    try {
      stored = sessionStorage.getItem(RETURN_URL_KEY);
      sessionStorage.removeItem(RETURN_URL_KEY);
    } catch {
      // Sin sessionStorage se vuelve al inicio.
    }
    return safeReturnUrl(stored) ?? '/home';
  }

  async logout(): Promise<void> {
    this.state.set({ status: 'anonymous', me: null });
    this.oidc.logoffLocal();
    this.navigateExternal(cognitoLogoutUrl(environment.oidc.cognitoDomain, environment.oidc.clientId, environment.oidc.postLogoutRedirectUri));
  }
}
```

Reemplazar `apps/mobile/src/app/auth/role.guard.ts`:

```ts
import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router, RouterStateSnapshot } from '@angular/router';
import { MobileAuthService } from './auth.service';
import { FieldRole } from './auth.models';

/** FR-08: rutas privadas de la PWA. `data: { roles: [...] }` restringe además por rol de campo. */
export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const auth = inject(MobileAuthService);
  const router = inject(Router);
  if (auth.status() !== 'authenticated') {
    return auth.status() === 'anonymous'
      ? router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } })
      : router.createUrlTree(['/login']);
  }
  const allowed = route.data['roles'] as FieldRole[] | undefined;
  const role = auth.currentRole();
  return !allowed?.length || (role !== null && allowed.includes(role)) ? true : router.createUrlTree(['/home']);
};
```

Reemplazar `apps/mobile/src/app/auth/auth.interceptor.ts`:

```ts
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { switchMap, take } from 'rxjs';
import { isPrivateApiRequest } from '@istpetdev/shared-core';
import { environment } from '../config/environment';

/** FR-04: Bearer solo hacia la API configurada; la traza pública y terceros no lo reciben. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!isPrivateApiRequest(request.url, environment.apiUrl)) return next(request);
  const oidc = inject(OidcSecurityService);
  return oidc.getAccessToken().pipe(
    take(1),
    switchMap((token) => next(token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request)),
  );
};
```

En `apps/mobile/src/app/auth/index.ts` añadir `export * from './navigate-external';` y `export * from './pages/callback/callback.page';`.

- [x] **Step 5: Ejecutar y verificar que pasan**

Run: `npm run test:mobile`
Expected: PASS en `environment.spec.ts` (tras pegar los valores de Terraform) y `auth.service.spec.ts`.

- [x] **Step 6: Conectar la PWA y reemplazar el login simulado**

En `apps/mobile/src/app/app.config.ts`: cambiar la importación de Angular a `import { ApplicationConfig, inject, isDevMode, provideAppInitializer, provideZoneChangeDetection } from '@angular/core';`, la de la librería a `import { AbstractSecurityStorage, provideAuth } from 'angular-auth-oidc-client';`, añadir `import { TokenFreeStorage } from '@istpetdev/shared-core';` y `import { MobileAuthService } from './auth';`, y añadir después de `provideAuth(mobileOidcConfig),`:

```ts
    // FR-03: tokens solo en memoria; reemplaza el almacenamiento por defecto de la librería.
    { provide: AbstractSecurityStorage, useFactory: () => new TokenFreeStorage() },
    provideAppInitializer(() => inject(MobileAuthService).bootstrap()),
```

En `apps/mobile/src/app/app.routes.ts`: añadir antes de `home` la ruta

```ts
  {
    path: 'auth/callback',
    loadComponent: () => import('./auth/pages/callback/callback.page').then((m) => m.CallbackPage),
    title: 'Verificando sesión — IstpetDev Móvil'
  },
```

y en `scan`, `receipt/:deliveryId` y `sync` reemplazar `data: { roles: ['CarrierDriver', 'WarehouseOperator', 'LogisticsManager', 'HospitalReceiver'] }` por `data: { roles: ['Conductor', 'Bodega', 'Receptor'] }`.

`apps/mobile/src/app/auth/pages/callback/callback.page.ts`:

```ts
import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { IonContent } from '@ionic/angular/standalone';
import { MobileAuthService } from '../../auth.service';

/** Vuelta de Cognito: el inicializador ya canjeó el código; aquí solo se decide el destino. */
@Component({
  selector: 'app-auth-callback',
  standalone: true,
  imports: [IonContent],
  template: `<ion-content class="ion-padding"><p class="text-center text-sm text-slate-500 pt-10">Verificando tu cuenta…</p></ion-content>`
})
export class CallbackPage implements OnInit {
  private readonly auth = inject(MobileAuthService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    const target = this.auth.status() === 'authenticated' ? this.auth.takeReturnUrl() : '/login';
    void this.router.navigateByUrl(target, { replaceUrl: true });
  }
}
```

Reemplazar `apps/mobile/src/app/auth/pages/login/login.page.ts`:

```ts
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IonButton, IonContent } from '@ionic/angular/standalone';
import { MobileAuthService } from '../../auth.service';

/** FR-01: esta página no recibe contraseñas; abre la página administrada de Cognito. */
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [IonContent, IonButton],
  template: `
    <ion-content class="ion-padding">
      <div class="max-w-md mx-auto py-10 space-y-6 text-center">
        <h1 class="text-2xl font-black text-slate-900">IstpetDev Campo</h1>
        @switch (auth.status()) {
          @case ('wrong-platform') {
            <p role="alert" class="text-sm font-semibold text-slate-800">Tu cuenta opera en la plataforma web</p>
            <ion-button expand="block" fill="outline" (click)="auth.logout()">Cerrar sesión</ion-button>
          }
          @case ('disabled') {
            <p role="alert" class="text-sm font-semibold text-slate-800">Tu cuenta no está habilitada; contacta a Gerencia</p>
            <ion-button expand="block" fill="outline" (click)="auth.logout()">Cerrar sesión</ion-button>
          }
          @case ('error') {
            <p role="alert" class="text-sm text-slate-800">No pudimos verificar tu cuenta. Revisa la señal e intenta de nuevo.</p>
            <ion-button expand="block" (click)="auth.login()">Reintentar</ion-button>
          }
          @default {
            <ion-button expand="block" (click)="auth.login()">Iniciar sesión</ion-button>
            <p class="text-xs text-slate-500">Te llevaremos a la página segura de inicio de sesión. Allí también puedes recuperar tu contraseña.</p>
          }
        }
      </div>
    </ion-content>
  `
})
export class LoginPage implements OnInit {
  readonly auth = inject(MobileAuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  ngOnInit(): void {
    if (this.auth.status() === 'authenticated') {
      void this.router.navigateByUrl('/home');
      return;
    }
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    if (this.auth.status() === 'anonymous' && returnUrl) this.auth.login(returnUrl);
  }
}
```

En `apps/mobile/src/app/home/home.page.ts`:

Run: `sed -i "s/'HospitalReceiver'/'Receptor'/g; s/'WarehouseOperator'/'Bodega'/g; s/'CarrierDriver'/'Conductor'/g; s/authService.currentUser()?.name || 'Usuario Móvil'/authService.me()?.displayName ?? ''/" apps/mobile/src/app/home/home.page.ts`

y reemplazar los bloques `roleSubtitle`, `roleEntityBadge` y `roleBadgeColor` por (G1: sin bodegas, vehículos ni hospitales en el código):

```ts
  readonly roleSubtitle = computed(() => {
    const role = this.authService.currentRole();
    return role ? FIELD_ROLE_LABELS[role] : '';
  });

  readonly roleEntityBadge = computed(() => this.roleSubtitle());

  readonly roleBadgeColor = computed(() => {
    const role = this.authService.currentRole();
    if (role === 'Receptor') return 'success';
    if (role === 'Bodega') return 'tertiary';
    return 'primary';
  });
```

con `import { FIELD_ROLE_LABELS } from '../auth';` (o ampliar la importación existente de `../auth`).

- [x] **Step 7: Verificar build, pruebas y que no quedan restos del mock**

Run: `npm run test:mobile && npm run build:mobile && grep -rnE "useMockSession|loginWithCredentials|loginAs(Driver|Warehouse|Receiver|Manager)|DEMO_[A-Z]+_PROFILE|mock-bearer-token|istpetdev_volatile_mobile_session|CarrierDriver|LogisticsManager" apps/mobile/src; echo "grep=$?"`
Expected: pruebas PASS, build sin errores, `grep=1`.

- [x] **Step 8: Commit**

```bash
git add -A apps/mobile package-lock.json
git commit -m "feat(campo): login real con cognito pkce y sesion desde /me en la pwa (spec 013 h1)"
```

### Desviaciones

La PWA se adaptó a los roles reales (`Bodega`, `Conductor`, `Receptor`), retirando completamente `LogisticsManager` (ADR-24) y los nombres legados (`CarrierDriver`, `WarehouseOperator`, `HospitalReceiver`) en rutas, servicios y `home.page.ts`. A la ruta `/home` se le asignaron explícitamente los tres roles de campo en `data.roles`.
`MockFieldApiService` se desacopló mediante la función `shouldMockFieldApi(isDev, search)` y se condicionó estrictamente a `isDevMode() && ?mock=true`, con pruebas unitarias que comprueban que nunca se activa en producción. Se añadieron pruebas dedicadas para Review Focus 1 (retorno seguro a `/home` ante URLs maliciosas) y Review Focus 4 (ausencia absoluta de tokens en `localStorage` y `sessionStorage`).

---

### Task 8: PWA — outbox por cuenta y logout con capturas pendientes

**Files:**
- Modify: `apps/mobile/src/app/field/outbox/field-outbox.service.ts`, `apps/mobile/src/app/auth/auth.service.ts`, `apps/mobile/src/app/auth/auth.service.spec.ts`, `apps/mobile/src/app/home/home.page.ts`
- Test: `apps/mobile/src/app/field/outbox/field-outbox.service.spec.ts`

**Interfaces:**
- Consumes: `MobileAuthService` (T7), `FIELD_API` (004).
- Produces: `OutboxItem.ownerAccountId: string`; `FieldOutboxService.setOwner(accountId: string | null): Promise<void>`; `FieldOutboxService.pendingCount: Signal<number>`; `MobileAuthService` llama a `setOwner` tras `/me` y en `logout()`.

- [x] **Step 1: Escribir la prueba que falla** — `apps/mobile/src/app/field/outbox/field-outbox.service.spec.ts`:

```ts
import { TestBed } from '@angular/core/testing';
import { IDBFactory } from 'fake-indexeddb';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { FIELD_API } from '../field-api.interface';
import { FieldOutboxService } from './field-outbox.service';

const receipt = (deliveryId: string) => ({ deliveryId, expectedDeliveryVersion: 1, recipientName: 'Receptor de prueba', lines: [] });

describe('FieldOutboxService por cuenta (H2, FR-09)', () => {
  let api: { createReceipt: ReturnType<typeof vi.fn>; uploadEvidence: ReturnType<typeof vi.fn>; confirmEvidence: ReturnType<typeof vi.fn>; getAssignments: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    globalThis.indexedDB = new IDBFactory();
    api = { createReceipt: vi.fn(() => of({ receiptId: 'r-1' })), uploadEvidence: vi.fn(), confirmEvidence: vi.fn(), getAssignments: vi.fn() };
    TestBed.configureTestingModule({ providers: [{ provide: FIELD_API, useValue: api }] });
  });

  it('cada cuenta ve y sincroniza solo sus capturas, y cerrar sesión no las borra', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    outbox.isSimulatingOffline.set(true);
    await outbox.setOwner('cuenta-a');
    await outbox.saveReceiptAtomic(receipt('d-1'));
    await outbox.saveReceiptAtomic(receipt('d-2'));
    expect(outbox.pendingCount()).toBe(2);

    await outbox.setOwner(null);
    expect(outbox.outboxItems()).toEqual([]);

    await outbox.setOwner('cuenta-b');
    outbox.isSimulatingOffline.set(false);
    await outbox.syncAllPending();
    expect(outbox.outboxItems()).toEqual([]);
    expect(api.createReceipt).not.toHaveBeenCalled();

    await outbox.setOwner('cuenta-a');
    expect(outbox.pendingCount()).toBe(2);
    expect(outbox.outboxItems().every((item) => item.ownerAccountId === 'cuenta-a')).toBe(true);
  });

  it('no captura sin cuenta y no sincroniza una captura ajena', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    outbox.isSimulatingOffline.set(true);
    await expect(outbox.saveReceiptAtomic(receipt('d-1'))).rejects.toThrow('Inicia sesión para registrar una recepción.');

    await outbox.setOwner('cuenta-a');
    const item = await outbox.saveReceiptAtomic(receipt('d-1'));
    await outbox.setOwner('cuenta-b');
    outbox.isSimulatingOffline.set(false);

    await expect(outbox.syncItem(item.operationId)).rejects.toThrow('La captura pertenece a otra cuenta.');
    expect(api.createReceipt).not.toHaveBeenCalled();
  });
});
```

- [x] **Step 2: Ejecutar y verificar que falla**

Run: `npm run test:mobile`
Expected: FAIL con «outbox.setOwner is not a function».

- [x] **Step 3: Implementación mínima en `field-outbox.service.ts`**

1. Cambiar la importación de Angular a `import { Injectable, computed, inject, signal } from '@angular/core';`.
2. En `OutboxItem`, después de `operationId: string; // UUID v7`, añadir:

```ts
  ownerAccountId: string; // FR-09: cuenta que capturó; otra cuenta no la ve ni la sincroniza
```

3. Dentro de la clase, después de `readonly activeDriverNotice = …;`, añadir:

```ts
  private readonly owner = signal<string | null>(null);
  readonly pendingCount = computed(() => this.outboxItems().filter((item) => item.status !== 'Complete').length);

  /** La fija MobileAuthService tras /me y la limpia al cerrar sesión. Nunca borra capturas. */
  async setOwner(accountId: string | null): Promise<void> {
    this.owner.set(accountId);
    this.activeAssignment.set(null);
    this.activeDriverNotice.set(null);
    await this.refreshOutboxItems();
    if (accountId) await this.loadCachedAssignment();
  }
```

4. Reemplazar el cuerpo de `refreshOutboxItems()` por:

```ts
    const owner = this.owner();
    const db = await this.getDb();
    const items: OutboxItem[] = owner ? (await db.getAll('outbox_items')).filter((item: OutboxItem) => item.ownerAccountId === owner) : [];
    // Ordenar de más reciente a más antiguo por capturedAtUtc
    items.sort((a, b) => new Date(b.capturedAtUtc).getTime() - new Date(a.capturedAtUtc).getTime());
    this.outboxItems.set(items);
```

5. Reemplazar el cuerpo de `loadCachedAssignment()` por:

```ts
    const owner = this.owner();
    if (!owner) return null;
    const db = await this.getDb();
    const assignment = (await db.get('assignments', owner)) as MobileAssignmentResponse | undefined;
    if (!assignment) return null;
    this.activeAssignment.set(assignment);
    this.checkDriverNotices(assignment);
    return assignment;
```

6. En `saveReceiptAtomic`, como primera línea del cuerpo:

```ts
    const ownerAccountId = this.owner();
    if (!ownerAccountId) throw new Error('Inicia sesión para registrar una recepción.');
```

y en el literal `const outboxItem: OutboxItem = {` añadir `ownerAccountId,` después de `operationId,`.

7. En `syncItem`, justo después del `if (!item) { … }`:

```ts
    if (item.ownerAccountId !== this.owner()) {
      throw new Error('La captura pertenece a otra cuenta.');
    }
```

- [x] **Step 4: Conectar el dueño desde la sesión y avisar al cerrar sesión**

En `apps/mobile/src/app/auth/auth.service.ts`: añadir `import { FieldOutboxService } from '../field/outbox/field-outbox.service';`, el campo `private readonly outbox = inject(FieldOutboxService);`, y:
- en `bootstrap()`, dentro del `try`, después de `this.state.set(…)`: `await this.outbox.setOwner(me.platform === 'field' ? me.accountId : null);`
- en `logout()`, como primera línea: `await this.outbox.setOwner(null);`

En `apps/mobile/src/app/auth/auth.service.spec.ts`, añadir a los `providers` de `setup`: `{ provide: FieldOutboxService, useValue: { setOwner: vi.fn(async () => undefined) } }` con `import { FieldOutboxService } from '../field/outbox/field-outbox.service';`.

En `apps/mobile/src/app/home/home.page.ts`: añadir `AlertController` a la importación de `@ionic/angular/standalone`, el campo `private readonly alerts = inject(AlertController);` y reemplazar `logout()` por:

```ts
  async logout(): Promise<void> {
    const pending = this.outboxService.pendingCount();
    if (pending > 0) {
      const alert = await this.alerts.create({
        header: 'Capturas sin sincronizar',
        message: `Tienes ${pending} ${pending === 1 ? 'captura pendiente' : 'capturas pendientes'}. Se guardarán en este teléfono para tu cuenta y se enviarán cuando vuelvas a entrar.`,
        buttons: [{ text: 'Cancelar', role: 'cancel' }, { text: 'Cerrar sesión', role: 'confirm' }],
      });
      await alert.present();
      if ((await alert.onDidDismiss()).role !== 'confirm') return;
    }
    await this.authService.logout();
  }
```

- [x] **Step 5: Ejecutar y verificar que pasan**

Run: `npm run test:mobile && npm run build:mobile`
Expected: PASS (outbox 2 casos, sesión 5 casos) y build sin errores.

- [x] **Step 6: Commit**

```bash
git add apps/mobile/src/app/field/outbox apps/mobile/src/app/auth/auth.service.ts apps/mobile/src/app/auth/auth.service.spec.ts apps/mobile/src/app/home/home.page.ts
git commit -m "feat(campo): outbox aislado por cuenta y logout que conserva capturas (spec 013 h2)"
```

### Desviaciones

Para el outbox en IndexedDB, se incrementó `DB_VERSION` a 2 con un `upgrade` no destructivo que preserva los datos existentes y crea el índice `by_owner` sobre `ownerAccountId`. Las capturas legadas previas sin dueño (`ownerAccountId` indefinido) se conservan en la base sin borrarse (CE-04), se contabilizan en `pendingCount` mediante `legacyPendingCount` para alertar en logout de que existen capturas retenidas, y se prohíbe su sincronización con cualquier otra cuenta. Se añadió una prueba específica para Review Focus 4 que inspecciona exhaustivamente tanto en memoria como en IndexedDB que ningún `OutboxItem` guarde tokens (`access_token`, `id_token`, `refresh_token`, etc.). En `home.page.ts`, se conectó `AlertController` con los textos exactos fijados por las Global Constraints.

---

### Task 9: Guardián de CI contra el login simulado y pruebas frontend en CI

**Files:**
- Create: `scripts/ci/check-no-mock-login.mjs`
- Modify: `package.json`, `.github/workflows/ci.yml`, `apps/mobile/src/test-setup.ts`, `apps/mobile/src/app/home/home.page.spec.ts`
- Update: `docs/specs/013-login-y-cuentas/task.md`

**Interfaces:**
- Consumes: T6 y T8 (sin restos de mock).
- Produces: `npm run check:no-mock-login` (sale 1 con la lista de coincidencias); CI ejecuta `test:frontend`, el guardián y `build:mobile`.

- [x] **Step 1: Escribir la prueba que falla** (fixture temporal fuera del repo)

Run:

```bash
tmp=$(mktemp -d) && mkdir -p "$tmp/app" && echo "const t = 'jwt-mock-auth-token';" > "$tmp/app/x.ts"
node scripts/ci/check-no-mock-login.mjs "$tmp"; echo "salida=$?"
```

Expected: FAIL con «Cannot find module …/scripts/ci/check-no-mock-login.mjs» y `salida=1`.

- [x] **Step 2: Implementación mínima** — `scripts/ci/check-no-mock-login.mjs`:

```js
#!/usr/bin/env node
// NFR-04 de 013: falla si reaparece el login simulado en las apps.
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { extname, join } from 'node:path';

const FORBIDDEN = [
  /loginWithCredentials/, /useMockSession/, /jwt-mock/, /mock-bearer-token/, /loginAsDemo/, /DEMO_PROFILES/,
  /\/api\/v1\/auth\/login/, /auth\/login'/
];
const SKIP = new Set(['node_modules', 'dist', '.angular', 'www', 'coverage']);
const EXTENSIONS = new Set(['.ts', '.html', '.js', '.mjs']);
const roots = process.argv.slice(2).length > 0 ? process.argv.slice(2) : ['apps'];

const hits = [];
const walk = (path) => {
  for (const name of readdirSync(path)) {
    if (SKIP.has(name)) continue;
    const full = join(path, name);
    if (statSync(full).isDirectory()) walk(full);
    else if (EXTENSIONS.has(extname(name))) {
      readFileSync(full, 'utf8').split('\n').forEach((line, index) => {
        if (FORBIDDEN.some((pattern) => pattern.test(line))) hits.push(`${full}:${index + 1}`);
      });
    }
  }
};
roots.forEach(walk);

if (hits.length > 0) {
  console.error('Login simulado detectado (NFR-04 de 013):');
  hits.forEach((hit) => console.error(`  ${hit}`));
  process.exit(1);
}
console.log('Sin login simulado en', roots.join(', '));
```

En `package.json` raíz, dentro de `scripts`, añadir `"check:no-mock-login": "node scripts/ci/check-no-mock-login.mjs apps"` y cambiar `check` a:

```json
    "check": "npm run check:lint && npm run check:no-mock-login && npm run check:api && npm run check:shared && npm run check:web && npm run check:mobile",
```

En `.github/workflows/ci.yml`, en el trabajo «QA Frontend (Angular 22)», después del paso «Compilar aplicación Web en Producción», añadir:

```yaml
      - name: "Sin login simulado (NFR-04 de 013)"
        run: npm run check:no-mock-login

      - name: "Pruebas unitarias frontend (shared-core, web y PWA)"
        run: npm run test:frontend

      - name: "Compilar PWA en Producción"
        run: npm run build:mobile
```

- [x] **Step 3: Ejecutar y verificar**

Run:

```bash
node scripts/ci/check-no-mock-login.mjs "$tmp"; echo "fixture=$?"
npm run check:no-mock-login; echo "apps=$?"
rm -rf "$tmp"
```

Expected: `fixture=1` con la línea `…/app/x.ts:1`; `apps=0` con «Sin login simulado en apps».

- [x] **Step 4: Commit**

```bash
git add scripts/ci/check-no-mock-login.mjs package.json .github/workflows/ci.yml apps/mobile/src/test-setup.ts apps/mobile/src/app/home/home.page.spec.ts docs/specs/013-login-y-cuentas/task.md
git commit -m "ci: añade guardián del login simulado y pruebas frontend (#42)" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Desviaciones

En la suite de mobile, el runner Angular seguía advirtiendo sobre `vi.mock` aun cuando estaba al nivel del módulo en el spec. Se registró el mock desde `src/test-setup.ts` con una referencia de ejecución a `vi.mock`, antes de cargar los specs; `npm run test:mobile` pasa con 22 pruebas y sin el warning. El guardián también detecta la ruta retirada `/api/v1/auth/login` y la forma `auth/login'`.

### Cierre del login simulado en la API (Bloque B)

- Se retiró `AuthController` y se añadieron pruebas de integración para confirmar que `/api/v1/auth/login` y `/api/v1/auth/me` responden 404; `/api/v1/me` queda como el único endpoint «quién soy».
- `DevJwtSecurity` conserva únicamente la clave y autoridad del validador local; la API ya no crea ni firma JWT.
- `CognitoJwtExtensions` valida al arrancar que Production tenga `Authentication:Authority` y `Authentication:AllowedClientIds`; Development/Testing conservan el modo local y no hay una lista predeterminada de clientes.
- `AccountRoleClaimsTransformation` solo agrega rol y `accountId` después de encontrar una cuenta activa y aprovisionada. Las operaciones de campo reutilizan `ClaimsPrincipal.AccountId()` y responden 403 si falta; se eliminaron los fallbacks por rol, `sub` y `DemoOrganization.Id`.
- Las pruebas de integración cubren los siete endpoints de campo que recibían una resolución de cuenta y verifican 403 sin aprovisionar al sujeto ni devolver contenido.
- Desviación respecto al código de 004: `MobileAssignmentsController` tenía el mismo fallback inseguro que las tres clases indicadas en el bloque, por lo que también se actualizó. Las pruebas unitarias existentes se ajustaron al nuevo constructor sin reducir sus expectativas.

### Configuración de Cognito y setup local (Bloque C)

- `appsettings.example.json` usa el issuer `https://cognito-idp.us-east-1.amazonaws.com/us-east-1_4Y2j5DINp` y los clientes públicos web `6ejhdau47gad74id7id1l7ugln` y móvil `3k59rv5cov1sinllgd7igmpe1a`. Fuente: `docs/deployment-status.md` (8-oct); confirmar con `terraform output -json cognito`.
- Compose de producción exige `COGNITO_AUTHORITY`, `COGNITO_WEB_CLIENT_ID` y `COGNITO_MOBILE_CLIENT_ID`. El paso de despliegue SSH las recibe desde variables de repositorio `vars.COGNITO_*`; hay que crearlas en GitHub antes de desplegar.
- El onboarding explica que web (`localhost:4200`) y PWA (`localhost:8100`) inician sesión con Cognito real; la API local en Development no se configura con el issuer/client IDs reales y ya no existe login simulado.
- La guía 004 aclara que, en el flujo de demo sin sesión independiente del Receptor, este valida y firma en el dispositivo del Conductor asignado; el `POST /api/v1/deliveries/{id}/receipts` se envía bajo la sesión del Conductor y las evidencias quedan asociadas a esa recepción.

---

### Task 10: Infraestructura — callbacks HTTPS, marca, IAM y hop limit (Jorge)

> ⚠️ **El host de la app no es el nodo del túnel.** El host de la app es `i-0e0a979f8c0885a4f` (Debian 13, creado a mano, **fuera de Terraform**); `module.ec2_access` es el nodo del túnel SSM `i-0bba84773e794de36`, en desuso para el desarrollo diario desde el 13-oct. Esta tarea **no modifica `module.ec2_access` ni `modules/ec2_hardened`**: Terraform solo crea recursos nuevos (rol, política y perfil de instancia) y el host real se configura con la CLI por su ID. Importar el host a Terraform sigue siendo prioridad baja de #14.

**Files:**
- Modify: `infra/terraform/environments/dev/identity.tf`, `infra/terraform/environments/dev/variables.tf`
- Create: `infra/terraform/environments/dev/cognito/hosted-ui.css`, `infra/terraform/environments/dev/app_host_iam.tf`

**Interfaces:**
- Consumes: pool `aws_cognito_user_pool.dev`, clientes `aws_cognito_user_pool_client.human`; host de la app `i-0e0a979f8c0885a4f` (ID en [Perfil A Reforzado](../../boveda/03%20Arquitectura/Perfil%20A%20Reforzado%20-%20Despliegue%20Stateless%20y%20Amazon%20RDS.md)).
- Produces: callbacks y cierres exactos de Global Constraints; branding FR-20; política `istpetdev-app-cognito-admin-dev` (NFR-02) en el rol del host de la app; IMDSv2 con hop limit 2 en el host de la app.

- [x] **Step 1: Leer el estado actual del host de la app (sin cambiar nada)**

Run:

```bash
APP_HOST=i-0e0a979f8c0885a4f
aws ec2 describe-instances --instance-ids "$APP_HOST" \
  --query 'Reservations[].Instances[].{Perfil:IamInstanceProfile.Arn,HopLimit:MetadataOptions.HttpPutResponseHopLimit,Tokens:MetadataOptions.HttpTokens,Nombre:Tags[?Key==`Name`]|[0].Value}'
```

Expected: una sola instancia con `Nombre` `istpetdev-prod-host`. Anotar en el PR `Perfil` (puede ser `null`), `HopLimit` y `Tokens`.
- Si `Perfil` es `null` → **rama A** en Step 4.
- Si `Perfil` tiene un ARN → **rama B** en Step 4 (se reutiliza ese rol; no se asocia otro perfil).
- Si el ID no existe o el nombre no coincide → **detenerse** y confirmar el host con Jorge y Joseph antes de seguir.

- [x] **Step 2: Callbacks y cierres HTTPS (#14)**

En `variables.tf` añadir:

```hcl
variable "app_public_origin" {
  type        = string
  description = "Origen HTTPS público de la web; la PWA vive en /campo/ (spec 013)"
  default     = "https://istpetdev.doicela.dev"
  validation {
    condition     = can(regex("^https://[a-z0-9.-]+$", var.app_public_origin))
    error_message = "Debe ser https://dominio sin barra final."
  }
}
```

En `identity.tf`, antes del cliente:

```hcl
locals {
  human_clients = {
    web = {
      callback_urls = ["http://localhost:4200/auth/callback", "${var.app_public_origin}/auth/callback"]
      logout_urls   = ["http://localhost:4200", var.app_public_origin]
    }
    mobile = {
      callback_urls = ["http://localhost:8100/auth/callback", "${var.app_public_origin}/campo/auth/callback"]
      logout_urls   = ["http://localhost:8100", "${var.app_public_origin}/campo/"]
    }
  }
}
```

y en `aws_cognito_user_pool_client.human` reemplazar `for_each`, `callback_urls` y `logout_urls` por:

```hcl
  for_each                             = local.human_clients
  callback_urls                        = each.value.callback_urls
  logout_urls                          = each.value.logout_urls
```

- [x] **Step 3: Página de Cognito con la marca (FR-20)**

`infra/terraform/environments/dev/cognito/hosted-ui.css` (UI clásica: solo clases `-customizable`; colores de [Sistema de diseno y colores](../../boveda/03%20Arquitectura/Sistema%20de%20diseno%20y%20colores.md)):

```css
.background-customizable { background-color: #F8FAFC; }
.banner-customizable { background-color: #00A7C4; padding: 16px 0; }
.logo-customizable { max-width: 60%; max-height: 30%; }
.label-customizable { color: #2D3748; font-weight: 600; }
.textDescription-customizable { color: #2D3748; }
.legalText-customizable { color: #4A5568; }
.inputField-customizable { border-color: #00A7C4; color: #2D3748; }
.inputField-customizable:focus { border-color: #EA601F; }
.submitButton-customizable { background-color: #EA601F; border-color: #EA601F; font-weight: 700; }
.submitButton-customizable:hover { background-color: #C94F17; border-color: #C94F17; }
.redirect-customizable { color: #00A7C4; }
.errorMessage-customizable { color: #B42318; border-color: #B42318; background-color: #FEF3F2; }
```

Copiar el logo institucional de ISTPET en PNG (≤ 100 KB) a `infra/terraform/environments/dev/cognito/logo-istpet.png`; si no está disponible al aplicar, el recurso aplica solo el CSS. En `identity.tf`:

```hcl
resource "aws_cognito_user_pool_ui_customization" "hosted_ui" {
  user_pool_id = aws_cognito_user_pool_domain.dev.user_pool_id
  css          = file("${path.module}/cognito/hosted-ui.css")
  image_file   = fileexists("${path.module}/cognito/logo-istpet.png") ? filebase64("${path.module}/cognito/logo-istpet.png") : null
}
```

La UI clásica no carga tipografías web: la tipografía institucional queda como limitación declarada en E2E-09.

- [x] **Step 4: Permiso mínimo de la API sobre el pool (NFR-02)**

Crear `infra/terraform/environments/dev/app_host_iam.tf`. Solo crea recursos nuevos; no referencia `module.ec2_access`.

```hcl
# Host de la app (i-0e0a979f8c0885a4f): creado a mano y fuera de Terraform (#14).
# Aquí solo viven su rol y su permiso sobre Cognito (spec 013, NFR-02). La asociación al host se hace por CLI.
variable "app_host_existing_role_name" {
  type        = string
  description = "Rama B del Step 1 de 013-T10: nombre del rol que el host ya tiene. Vacío = rama A (crear rol y perfil)."
  default     = ""
}

locals {
  create_app_host_role = var.app_host_existing_role_name == ""
}

resource "aws_iam_role" "app_host" {
  count = local.create_app_host_role ? 1 : 0
  name  = "istpetdev-app-host-dev"
  assume_role_policy = jsonencode({
    Version   = "2012-10-17"
    Statement = [{ Effect = "Allow", Principal = { Service = "ec2.amazonaws.com" }, Action = "sts:AssumeRole" }]
  })
}

resource "aws_iam_instance_profile" "app_host" {
  count = local.create_app_host_role ? 1 : 0
  name  = "istpetdev-app-host-dev"
  role  = aws_iam_role.app_host[0].name
}

resource "aws_iam_role_policy" "app_cognito_admin" {
  name = "istpetdev-app-cognito-admin-dev"
  role = local.create_app_host_role ? aws_iam_role.app_host[0].name : var.app_host_existing_role_name
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid    = "AccountAdministrationSpec013"
      Effect = "Allow"
      Action = [
        "cognito-idp:AdminCreateUser",
        "cognito-idp:AdminDisableUser",
        "cognito-idp:AdminEnableUser",
        "cognito-idp:AdminUserGlobalSignOut",
        "cognito-idp:AdminGetUser",
        "cognito-idp:ListUsers",
      ]
      Resource = aws_cognito_user_pool.dev.arn
    }]
  })
}

output "app_host_instance_profile" {
  value = local.create_app_host_role ? aws_iam_instance_profile.app_host[0].name : null
}
```

- **Rama A** (sin perfil): no definir `app_host_existing_role_name`.
- **Rama B** (con perfil): obtener el rol con `aws iam get-instance-profile --instance-profile-name <nombre del ARN> --query 'InstanceProfile.Roles[0].RoleName'` y pasar `-var app_host_existing_role_name=<rol>` en Step 5.

- [x] **Step 5: Plan revisado y aplicación con `-target`**

Run (rama A; en rama B quitar los dos primeros `-target` de IAM y añadir `-var app_host_existing_role_name=<rol>`):

```bash
terraform -chdir=infra/terraform/environments/dev fmt -check -recursive
terraform -chdir=infra/terraform/environments/dev validate
terraform -chdir=infra/terraform/environments/dev plan \
  -target='aws_cognito_user_pool_client.human' \
  -target=aws_cognito_user_pool_ui_customization.hosted_ui \
  -target='aws_iam_role.app_host' \
  -target='aws_iam_instance_profile.app_host' \
  -target=aws_iam_role_policy.app_cognito_admin -out=013.tfplan
```

Expected: `validate` correcto; el plan **actualiza en sitio** los dos clientes (solo URLs) y **crea** la personalización y los recursos IAM. **Ningún** recurso de `module.ec2_access`, ningún `destroy` ni `replace`. Si aparece cualquier otro recurso, detenerse y revisar con Joseph. Pegar el resumen del plan en el PR y que otra persona lo apruebe. Con el plan aprobado: `terraform -chdir=infra/terraform/environments/dev apply 013.tfplan`.

- [x] **Step 6: Asociar el perfil y subir el hop limit en el host de la app (CLI)**

Run:

```bash
APP_HOST=i-0e0a979f8c0885a4f
# Solo rama A: asociar el perfil nuevo (en rama B el host ya tiene el suyo).
aws ec2 associate-iam-instance-profile --instance-id "$APP_HOST" \
  --iam-instance-profile Name="$(terraform -chdir=infra/terraform/environments/dev output -raw app_host_instance_profile)"
# Ambas ramas: solo el hop limit; no cambia HttpTokens ni el endpoint.
aws ec2 modify-instance-metadata-options --instance-id "$APP_HOST" --http-put-response-hop-limit 2
```

Expected: `associate-iam-instance-profile` devuelve `State: associating`; `modify-instance-metadata-options` devuelve `HttpPutResponseHopLimit: 2` (`State: pending` → `applied`). No requiere reiniciar la instancia ni los contenedores.

- [x] **Step 7: Verificar en AWS y registrar el cambio fuera de Terraform**

Run:

```bash
pool=$(terraform -chdir=infra/terraform/environments/dev output -json cognito | jq -r '.issuer | split("/") | last')
for c in $(terraform -chdir=infra/terraform/environments/dev output -json cognito | jq -r '.client_ids[]'); do
  aws cognito-idp describe-user-pool-client --user-pool-id "$pool" --client-id "$c" --query 'UserPoolClient.[ClientName,CallbackURLs,LogoutURLs]'
done
aws ec2 describe-instances --instance-ids i-0e0a979f8c0885a4f \
  --query 'Reservations[].Instances[].[IamInstanceProfile.Arn,MetadataOptions.HttpPutResponseHopLimit]'
aws ec2 describe-instances --instance-ids i-0bba84773e794de36 \
  --query 'Reservations[].Instances[].MetadataOptions.HttpPutResponseHopLimit'
```

Expected: URLs idénticas a Global Constraints; el host de la app con perfil y hop limit `2`; el nodo del túnel **sin cambios** (sigue en `1`).

Registrar en el [Runbook de DevOps y base de datos](../../boveda/03%20Arquitectura/Runbook%20de%20DevOps%20y%20base%20de%20datos%20para%20la%20demo.md) §6 una fila «013 — Perfil IAM y hop limit del host de la app (cambio por CLI, fuera de Terraform)» con fecha, rama A/B y quién lo aplicó.

- [x] **Step 8: Commit**

```bash
git add infra/terraform/environments/dev/identity.tf infra/terraform/environments/dev/variables.tf infra/terraform/environments/dev/app_host_iam.tf infra/terraform/environments/dev/cognito "docs/boveda/03 Arquitectura/Runbook de DevOps y base de datos para la demo.md"
git commit -m "infra(cognito): callbacks https, marca y permiso de cuentas del host de la app (spec 013)"
```

---

### Task 11: API H4 — listar y crear cuentas (P2)

**Files:**
- Create: `src/Application/Accounts/AccountAdministration.cs`, `src/Api/Controllers/AccountsController.cs`
- Modify: `src/Application/DependencyInjection.cs`
- Test: `tests/IstpetDev.Tests/Integration/Accounts/AccountsApiTests.cs`

**Interfaces:**
- Consumes: `AccountProvisioning`, `IIdentityDirectory`, `AccountRuleException`, `IdentityDirectoryUnavailableException` (T3); `TestAccounts.SetRoleAsync` (T1); `ApiFactory.Directory` (T3).
- Produces: `AccountItem(Guid Id, string DisplayName, string Role, bool IsActive, string? Email, string InvitationStatus, bool? DirectoryEnabled, bool OutOfSync)`; `CreateAccountInput(string Email, string DisplayName, string Role)`; `UpdateAccountInput(string? DisplayName, string? Role)`; `AccountAdministration.ListAsync`, `CreateAsync`; `GET` y `POST /api/v1/accounts`; `AccountsController.Fail(int, string, string)` con Problem Details y `code`.

- [x] **Step 1: Escribir las pruebas que fallan** — `tests/IstpetDev.Tests/Integration/Accounts/AccountsApiTests.cs`:

```csharp
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

    // Campo explícito: la otra parte de la clase (T12) no ve el parámetro del constructor primario.
    private readonly PostgisContainer postgres = postgres;

    // El planificador sembrado pasa a Gerencia; el conductor sembrado es la persona gestionada.
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
    public async Task Create_sends_invitation_and_lists_pending_then_active()
    {
        var (api, manager, _, _) = await ManagerAsync();
        await using var _api = api;

        var created = await manager.PostAsJsonAsync(Accounts, new { email = "Nuevo.Conductor@ejemplo.test", displayName = "Conductor nuevo", role = "Conductor" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var item = await created.Content.ReadFromJsonAsync<AccountItem>();
        Assert.Equal(("nuevo.conductor@ejemplo.test", "pending", true, false), (item!.Email, item.InvitationStatus, item.IsActive, item.OutOfSync));
        Assert.Equal(1, api.Directory.Invitations);

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

        var response = await manager.PostAsJsonAsync(Accounts, new { email = "CONDUCTOR@ejemplo.test", displayName = "Otra vez", role = "Conductor" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("account_exists", await CodeOf(response));
        Assert.Equal(0, api.Directory.Invitations);
        await using var db = TestDb.Context(connection);
        Assert.Equal(1, await db.Accounts.CountAsync(a => a.Subject == TestTokens.Driver));
    }

    [Theory]
    [InlineData("Automatizacion", "invalid_role")]
    [InlineData("Admin", "invalid_role")]
    public async Task Invalid_role_is_422(string role, string code)
    {
        var (api, manager, _, _) = await ManagerAsync();
        await using var _api = api;

        var response = await manager.PostAsJsonAsync(Accounts, new { email = "rol@ejemplo.test", displayName = "Rol", role });

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

        var create = await manager.PostAsJsonAsync(Accounts, new { email = "caida@ejemplo.test", displayName = "Caída", role = "Bodega" });
        var list = await manager.GetAsync(Accounts);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, create.StatusCode);
        Assert.Equal("identity_unavailable", await CodeOf(create));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, list.StatusCode);
        await using var db = TestDb.Context(connection);
        Assert.False(await db.Accounts.AnyAsync(a => a.DisplayName == "Caída"));
    }
}
```

- [x] **Step 2: Ejecutar y verificar que fallan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~AccountsApiTests"`
Expected: FAIL de compilación con «The type or namespace name 'AccountItem' could not be found».

- [x] **Step 3: Implementación mínima**

`src/Application/Accounts/AccountAdministration.cs`:

```csharp
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Accounts;

public sealed record AccountItem(Guid Id, string DisplayName, string Role, bool IsActive, string? Email, string InvitationStatus, bool? DirectoryEnabled, bool OutOfSync);

public sealed record CreateAccountInput(string Email, string DisplayName, string Role);

public sealed record UpdateAccountInput(string? DisplayName, string? Role);

// H4 de 013: gestión de cuentas humanas por Gerencia. Rol y estado activo en Account; correo e invitación en Cognito.
public sealed partial class AccountAdministration(IApplicationDbContext database, IIdentityDirectory directory, AccountProvisioning provisioning)
{
    // Campos explícitos: la otra parte de la clase (T12) no ve los parámetros del constructor primario.
    private readonly IApplicationDbContext database = database;
    private readonly IIdentityDirectory directory = directory;

    public async Task<IReadOnlyList<AccountItem>> ListAsync(CancellationToken ct)
    {
        var users = (await directory.ListUsersAsync(ct)).ToDictionary(u => u.Subject);
        var accounts = await database.Accounts.AsNoTracking()
            .Where(a => a.Kind == "human" && a.Issuer == directory.Issuer)
            .OrderBy(a => a.DisplayName).ToListAsync(ct);
        return accounts.Select(a => ToItem(a, users.GetValueOrDefault(a.Subject), known: true)).ToList();
    }

    public async Task<AccountItem> CreateAsync(CreateAccountInput input, CancellationToken ct)
    {
        var result = await provisioning.ProvisionAsync(input.Email, input.DisplayName, input.Role, ct);
        if (result.Outcome == ProvisionOutcome.AlreadyExists)
            throw new AccountRuleException("account_exists", "Ese correo ya tiene una cuenta.");
        var account = await database.Accounts.AsNoTracking().SingleAsync(a => a.Id == result.AccountId, ct);
        return ToItem(account, result.User, known: true);
    }

    // G3: sin datos de Cognito la fila se marca como desfasada; nunca se asume activa.
    private static AccountItem ToItem(Account account, DirectoryUser? user, bool known)
    {
        var status = !known ? "unknown" : user is null ? "missing" : user.Status switch
        {
            DirectoryUserStatus.InvitationPending => "pending",
            DirectoryUserStatus.Active => "active",
            _ => "other",
        };
        var outOfSync = !known || user is null || user.Enabled != account.IsActive;
        return new(account.Id, account.DisplayName, account.Role, account.IsActive, user?.Email, status, user?.Enabled, outOfSync);
    }
}
```

`src/Api/Controllers/AccountsController.cs`:

```csharp
using IstpetDev.Api.Authentication;
using IstpetDev.Application.Accounts;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/accounts")]
[Authorize(Roles = Roles.Gerencia)]
public sealed partial class AccountsController(AccountAdministration accounts) : ControllerBase
{
    // Campo explícito: la otra parte de la clase (T12) no ve el parámetro del constructor primario.
    private readonly AccountAdministration accounts = accounts;

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => Run(async () => Ok(await accounts.ListAsync(ct)));

    [HttpPost]
    public Task<IActionResult> Create(CreateAccountInput input, CancellationToken ct) => Run(async () =>
    {
        var item = await accounts.CreateAsync(input, ct);
        return Created($"/api/v1/accounts/{item.Id}", item);
    });

    private Guid Actor => User.AccountId() ?? throw new InvalidOperationException("Cuenta autenticada sin identificador.");

    private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (AccountRuleException ex) { return Fail(StatusFor(ex.Code), ex.Code, ex.Message); }
        catch (IdentityDirectoryUnavailableException) { return Fail(StatusCodes.Status503ServiceUnavailable, "identity_unavailable", "Cognito no está disponible. Intenta de nuevo en unos minutos."); }
    }

    private static int StatusFor(string code) => code switch
    {
        "not_found" => StatusCodes.Status404NotFound,
        "account_exists" or "self_change_forbidden" or "last_manager" or "invitation_not_pending" => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status422UnprocessableEntity,
    };

    private static ObjectResult Fail(int status, string code, string title)
    {
        var problem = new ProblemDetails { Status = status, Title = title };
        problem.Extensions["code"] = code;
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
```

En `src/Application/DependencyInjection.cs`, junto a `AccountProvisioning`: `services.AddScoped<AccountAdministration>();`.

- [x] **Step 4: Ejecutar y verificar que pasan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~AccountsApiTests"`
Expected: PASS (3 + 1 + 1 + 2 + 1 casos). `Actor` aún no se usa; lo consumen los endpoints de T12.

- [x] **Step 5: Commit**

```bash
git add src/Application/Accounts/AccountAdministration.cs src/Api/Controllers/AccountsController.cs src/Application/DependencyInjection.cs tests/IstpetDev.Tests/Integration/Accounts/AccountsApiTests.cs
git commit -m "feat(cuentas): listar y crear cuentas desde gerencia (spec 013 h4)"
```

### Desviaciones

- `POST /api/v1/accounts` responde 201 con el `AccountItem` y sin cabecera `Location` (no existe `GET /accounts/{id}`).
- Una identidad sin cuenta resoluble recibe 403 en todas las rutas (`User.AccountId()`), sin fallback.
- Entradas ausentes o nulas devuelven 422 con el `code` de dominio (`invalid_email`, `invalid_display_name`, `invalid_role`).
- .NET tras T11–T12: 312 pruebas (35 de `AccountsApiTests`).

---

### Task 12: API H4 — editar, desactivar, reactivar y reenviar (P2)

**Files:**
- Create: `src/Application/Accounts/AccountAdministration.Changes.cs`, `src/Api/Controllers/AccountsController.Changes.cs`
- Test: `tests/IstpetDev.Tests/Integration/Accounts/AccountsApiTests.Changes.cs`

**Interfaces:**
- Consumes: T11 (`AccountAdministration`, `AccountsController.Run`, `Actor`, `ToItem`).
- Produces: `UpdateAsync(Guid actorId, Guid id, UpdateAccountInput, ct)`, `DeactivateAsync(Guid actorId, Guid id, ct)`, `ReactivateAsync(Guid id, ct)`, `ResendInvitationAsync(Guid id, ct)`; `PATCH /api/v1/accounts/{id}`, `POST …/{id}/deactivate`, `…/reactivate`, `…/resend-invitation` (204).

- [x] **Step 1: Escribir las pruebas que fallan** — `tests/IstpetDev.Tests/Integration/Accounts/AccountsApiTests.Changes.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Accounts;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Integration.Accounts;

public sealed partial class AccountsApiTests
{
    private static async Task<Guid> IdOf(string connection, string subject)
    {
        await using var db = TestDb.Context(connection);
        return await db.Accounts.Where(a => a.Subject == subject).Select(a => a.Id).SingleAsync();
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
    }

    [Fact]
    public async Task Gerencia_cannot_deactivate_or_change_the_role_of_its_own_account()
    {
        var (api, manager, managerId, _) = await ManagerAsync();
        await using var _api = api;

        var deactivate = await manager.PostAsync($"{Accounts}/{managerId}/deactivate", null);
        var demote = await manager.PatchAsJsonAsync($"{Accounts}/{managerId}", new { role = "Planificador" });

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

        var response = await manager.PatchAsJsonAsync($"{Accounts}/{id}", new { role = "Auditor", displayName = "  Auditoría  " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await driver.GetFromJsonAsync<JsonElement>("/api/v1/me");
        Assert.Equal(("Auditor", "web", "Auditoría"), (me.GetProperty("role").GetString(), me.GetProperty("platform").GetString(), me.GetProperty("displayName").GetString()));
    }

    [Fact]
    public async Task Automation_cannot_be_assigned_or_managed_through_the_api()
    {
        var (api, manager, _, connection) = await ManagerAsync();
        await using var _api = api;
        var driverId = await IdOf(connection, TestTokens.Driver);
        var technical = new Account { Kind = "technical", Issuer = TestTokens.Issuer, Subject = "n8n-client", Role = Roles.Automatizacion, IsActive = true, DisplayName = "n8n" };
        await using (var db = TestDb.Context(connection)) { db.Add(technical); await db.SaveChangesAsync(); }

        var assign = await manager.PatchAsJsonAsync($"{Accounts}/{driverId}", new { role = "Automatizacion" });
        var touch = await manager.PostAsync($"{Accounts}/{technical.Id}/deactivate", null);

        Assert.Equal((HttpStatusCode.UnprocessableEntity, "invalid_role"), (assign.StatusCode, await CodeOf(assign)));
        Assert.Equal((HttpStatusCode.UnprocessableEntity, "technical_account"), (touch.StatusCode, await CodeOf(touch)));
    }

    [Fact]
    public async Task Invitation_is_resent_only_while_pending()
    {
        var (api, manager, _, connection) = await ManagerAsync();
        await using var _api = api;
        var created = await (await manager.PostAsJsonAsync(Accounts, new { email = "pendiente@ejemplo.test", displayName = "Pendiente", role = "Receptor" }))
            .Content.ReadFromJsonAsync<AccountItem>();
        var driverId = await IdOf(connection, TestTokens.Driver);

        var resend = await manager.PostAsync($"{Accounts}/{created!.Id}/resend-invitation", null);
        var done = await manager.PostAsync($"{Accounts}/{driverId}/resend-invitation", null);

        Assert.Equal(HttpStatusCode.NoContent, resend.StatusCode);
        Assert.Equal(1, api.Directory.Resends);
        Assert.Equal((HttpStatusCode.Conflict, "invitation_not_pending"), (done.StatusCode, await CodeOf(done)));
        Assert.Equal(HttpStatusCode.NotFound, (await manager.PostAsync($"{Accounts}/{Guid.NewGuid()}/deactivate", null)).StatusCode);
    }
}
```

Añadir `using Microsoft.Extensions.DependencyInjection;` al inicio del archivo (para `CreateScope` y `GetRequiredService`).

- [x] **Step 2: Ejecutar y verificar que fallan**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~AccountsApiTests"`
Expected: FAIL de compilación con «'AccountAdministration' does not contain a definition for 'DeactivateAsync'».

- [x] **Step 3: Implementación mínima**

`src/Application/Accounts/AccountAdministration.Changes.cs`:

```csharp
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Accounts;

public sealed partial class AccountAdministration
{
    public async Task<AccountItem> UpdateAsync(Guid actorId, Guid id, UpdateAccountInput input, CancellationToken ct)
    {
        var account = await ChangeAsync(id, async a =>
        {
            if (input.Role is not null && input.Role != a.Role)
            {
                if (!Roles.IsHuman(input.Role)) throw AccountProvisioning.InvalidRole();
                if (a.Id == actorId) throw SelfChange();
                await EnsureNotLastManagerAsync(a, ct);
                a.Role = input.Role;
            }
            if (input.DisplayName is not null) a.DisplayName = AccountProvisioning.ValidateDisplayName(input.DisplayName);
        }, ct);
        return await DescribeAsync(account, ct);
    }

    // FR-15: primero se bloquea en la API (NFR-01); un fallo de Cognito no lo revierte y la lista muestra el desfase.
    public async Task<AccountItem> DeactivateAsync(Guid actorId, Guid id, CancellationToken ct)
    {
        var account = await ChangeAsync(id, async a =>
        {
            if (a.Id == actorId) throw SelfChange();
            await EnsureNotLastManagerAsync(a, ct);
            a.IsActive = false;
        }, ct);
        await SyncDirectoryAsync(account.Subject, enabled: false, ct);
        return await DescribeAsync(account, ct);
    }

    public async Task<AccountItem> ReactivateAsync(Guid id, CancellationToken ct)
    {
        var account = await ChangeAsync(id, a => { a.IsActive = true; return Task.CompletedTask; }, ct);
        await SyncDirectoryAsync(account.Subject, enabled: true, ct);
        return await DescribeAsync(account, ct);
    }

    public async Task ResendInvitationAsync(Guid id, CancellationToken ct)
    {
        var account = await database.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, ct) ?? throw NotFound();
        if (account.Kind != "human") throw Technical();
        var user = await directory.FindBySubjectAsync(account.Subject, ct);
        if (user is not { Status: DirectoryUserStatus.InvitationPending })
            throw new AccountRuleException("invitation_not_pending", "La persona ya completó su primer ingreso.");
        await directory.ResendInvitationAsync(user.Email, ct);
    }

    // El bloqueo de organización serializa cambios: dos Gerencias no pueden retirarse mutuamente a la vez.
    private async Task<Account> ChangeAsync(Guid id, Func<Account, Task> change, CancellationToken ct)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(ct);
        await database.LockOrganizationAsync(ct);
        var account = await database.Accounts.SingleOrDefaultAsync(a => a.Id == id, ct) ?? throw NotFound();
        if (account.Kind != "human") throw Technical();
        await change(account);
        await database.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return account;
    }

    private async Task EnsureNotLastManagerAsync(Account account, CancellationToken ct)
    {
        if (account.Role != Roles.Gerencia || !account.IsActive) return;
        var activeManagers = await database.Accounts.CountAsync(a => a.Kind == "human" && a.IsActive && a.Role == Roles.Gerencia, ct);
        if (activeManagers <= 1) throw new AccountRuleException("last_manager", "Debe quedar al menos una Gerencia activa.");
    }

    private async Task SyncDirectoryAsync(string subject, bool enabled, CancellationToken ct)
    {
        try
        {
            if (enabled) await directory.EnableAsync(subject, ct);
            else
            {
                await directory.DisableAsync(subject, ct);
                await directory.SignOutEverywhereAsync(subject, ct);
            }
        }
        catch (IdentityDirectoryUnavailableException)
        {
            // La API ya aplica el cambio; repetir la operación vuelve a sincronizar.
        }
    }

    private async Task<AccountItem> DescribeAsync(Account account, CancellationToken ct)
    {
        try { return ToItem(account, await directory.FindBySubjectAsync(account.Subject, ct), known: true); }
        catch (IdentityDirectoryUnavailableException) { return ToItem(account, null, known: false); }
    }

    private static AccountRuleException NotFound() => new("not_found", "La cuenta no existe.");
    private static AccountRuleException Technical() => new("technical_account", "La cuenta técnica de automatización no se gestiona aquí.");
    private static AccountRuleException SelfChange() => new("self_change_forbidden", "No puedes desactivarte ni cambiar tu propio rol.");
}
```

`src/Api/Controllers/AccountsController.Changes.cs`:

```csharp
using IstpetDev.Application.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

public sealed partial class AccountsController
{
    [HttpPatch("{id:guid}")]
    public Task<IActionResult> Update(Guid id, UpdateAccountInput input, CancellationToken ct) =>
        Run(async () => Ok(await accounts.UpdateAsync(Actor, id, input, ct)));

    [HttpPost("{id:guid}/deactivate")]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken ct) =>
        Run(async () => Ok(await accounts.DeactivateAsync(Actor, id, ct)));

    [HttpPost("{id:guid}/reactivate")]
    public Task<IActionResult> Reactivate(Guid id, CancellationToken ct) =>
        Run(async () => Ok(await accounts.ReactivateAsync(id, ct)));

    [HttpPost("{id:guid}/resend-invitation")]
    public Task<IActionResult> ResendInvitation(Guid id, CancellationToken ct) =>
        Run(async () => { await accounts.ResendInvitationAsync(id, ct); return NoContent(); });
}
```

- [x] **Step 4: Ejecutar y verificar que pasan, más toda la suite**

Run: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~AccountsApiTests" && for c in Unit Integration E2E; do dotnet test tests/IstpetDev.Tests --filter "Category=$c" || break; done`
Expected: PASS (7 casos nuevos) y las tres categorías verdes, incluido `LeanSchemaTests` (29 tablas: no hubo migración).

- [x] **Step 5: Commit**

```bash
git add src/Application/Accounts/AccountAdministration.Changes.cs src/Api/Controllers/AccountsController.Changes.cs tests/IstpetDev.Tests/Integration/Accounts/AccountsApiTests.Changes.cs
git commit -m "feat(cuentas): editar, desactivar con revocacion y reenviar invitacion (spec 013 h4)"
```

### Desviaciones

- **PATCH con Cognito caído (NFR-05):** consulta Cognito (`FindBySubjectAsync`) antes de guardar; si falla, responde 503 `identity_unavailable` sin cambios en la base (el borrador guardaba primero y devolvía `outOfSync`).
- **Revocación pendiente durable (FR-15, NFR-01):** si `DisableAsync` o `SignOutEverywhereAsync` fallan tras guardar `IsActive = false`, queda una fila en `IdempotencyRecord` con `ActionCode = "accounts.revoke_sessions"` (sin migración nueva). `GET /accounts` y la respuesta la suman a `outOfSync`; un reintento de desactivación o una reactivación exitosa la borran. `FieldService` solo consulta `ActionCode` `Dispatch` y `Receipt`, así que no interfiere.
- **Reactivación con Cognito caído:** una transacción compensatoria devuelve `IsActive = false` y responde 503.
- Prueba extra `Last_manager_maps_to_conflict_problem_details_at_controller_boundary` para el 409 `last_manager` en el borde HTTP.
- Sugerencias no implementadas (después del congelamiento): `if` explícito antes de `ClearPendingRevocationAsync` y un índice parcial sobre `IdempotencyRecord` para las marcas pendientes.

---

### Task 13: Web H4 — pantalla «Usuarios» (P2)

**Files:**
- Create: `apps/web/src/entities/account/api/accounts.api.ts` (+ `.spec.ts`), `apps/web/src/entities/account/model/account-messages.ts`, `apps/web/src/entities/account/index.ts`
- Create: `apps/web/src/pages/users/users.page.ts` (+ `.spec.ts`), `apps/web/src/pages/users/index.ts`
- Modify: `apps/web/src/app/app.routes.ts`, `apps/web/src/pages/index.ts`, `apps/web/src/widgets/header/header.widget.ts`

**Interfaces:**
- Consumes: endpoints de T11–T12; `AccountItem`, `CreateAccountRequest`, `UpdateAccountRequest`, `HUMAN_ROLES` (T2); `AuthSession`, `authGuard`, `roleGuard` (T6).
- Produces: `AccountsApi { list, create, update, deactivate, reactivate, resendInvitation }`; `accountErrorMessage(error: unknown): string`; `accountStatusLabel(item: AccountItem): string`; ruta `/users` solo Gerencia.

- [x] **Step 1: Escribir las pruebas que fallan**

`apps/web/src/entities/account/api/accounts.api.spec.ts`:

```ts
import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '@shared/config';
import { accountErrorMessage, accountStatusLabel } from '../model/account-messages';
import { AccountsApi } from './accounts.api';

const BASE = `${environment.apiUrl}/api/v1/accounts`;

describe('AccountsApi', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }));

  it('usa los métodos y rutas del contrato de 013', () => {
    const api = TestBed.inject(AccountsApi);
    const backend = TestBed.inject(HttpTestingController);

    api.list().subscribe();
    api.create({ email: 'a@ejemplo.test', displayName: 'A', role: 'Conductor' }).subscribe();
    api.update('id-1', { role: 'Bodega' }).subscribe();
    api.deactivate('id-1').subscribe();
    api.reactivate('id-1').subscribe();
    api.resendInvitation('id-1').subscribe();

    expect(backend.expectOne({ method: 'GET', url: BASE })).toBeTruthy();
    expect(backend.expectOne({ method: 'POST', url: BASE }).request.body).toEqual({ email: 'a@ejemplo.test', displayName: 'A', role: 'Conductor' });
    expect(backend.expectOne({ method: 'PATCH', url: `${BASE}/id-1` }).request.body).toEqual({ role: 'Bodega' });
    expect(backend.expectOne({ method: 'POST', url: `${BASE}/id-1/deactivate` })).toBeTruthy();
    expect(backend.expectOne({ method: 'POST', url: `${BASE}/id-1/reactivate` })).toBeTruthy();
    expect(backend.expectOne({ method: 'POST', url: `${BASE}/id-1/resend-invitation` })).toBeTruthy();
  });

  it('traduce el código de Problem Details y el estado de la cuenta', () => {
    expect(accountErrorMessage(new HttpErrorResponse({ status: 409, error: { code: 'last_manager' } }))).toBe('Debe quedar al menos una Gerencia activa.');
    expect(accountErrorMessage(new HttpErrorResponse({ status: 500, error: null }))).toBe('No se pudo completar la operación. Intenta de nuevo.');
    const base = { id: 'x', displayName: 'X', role: 'Conductor', email: 'x@ejemplo.test', directoryEnabled: true, outOfSync: false } as const;
    expect(accountStatusLabel({ ...base, isActive: true, invitationStatus: 'pending' })).toBe('Invitación pendiente');
    expect(accountStatusLabel({ ...base, isActive: true, invitationStatus: 'active' })).toBe('Activa');
    expect(accountStatusLabel({ ...base, isActive: false, invitationStatus: 'active' })).toBe('Desactivada');
  });
});
```

`apps/web/src/pages/users/users.page.spec.ts`:

```ts
import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { AccountItem } from '@istpetdev/shared-core';
import { AccountsApi } from '@entities/account';
import { AuthSession } from '@entities/session';
import { UsersPage } from './users.page';

const rows: AccountItem[] = [
  { id: 'g', displayName: 'Gerencia', role: 'Gerencia', isActive: true, email: 'g@ejemplo.test', invitationStatus: 'active', directoryEnabled: true, outOfSync: false },
  { id: 'c', displayName: 'Conductor', role: 'Conductor', isActive: true, email: 'c@ejemplo.test', invitationStatus: 'pending', directoryEnabled: true, outOfSync: false },
  { id: 'b', displayName: 'Bodega', role: 'Bodega', isActive: false, email: 'b@ejemplo.test', invitationStatus: 'active', directoryEnabled: true, outOfSync: true },
];

async function render(api: Partial<Record<keyof AccountsApi, unknown>>) {
  TestBed.configureTestingModule({
    imports: [UsersPage],
    providers: [
      { provide: AccountsApi, useValue: { list: vi.fn(() => of(rows)), ...api } },
      { provide: AuthSession, useValue: { me: signal({ accountId: 'g', displayName: 'Gerencia', role: 'Gerencia', platform: 'web' }) } },
    ],
  });
  const fixture = TestBed.createComponent(UsersPage);
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
  return fixture;
}

describe('UsersPage (FR-19)', () => {
  it('muestra estado, desfase y no ofrece acciones sobre la propia cuenta', async () => {
    const fixture = await render({});
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Invitación pendiente');
    expect(text).toContain('Activa');
    expect(text).toContain('Desactivada');
    expect(text).toContain('Desfase con Cognito');
    const ownRow = fixture.nativeElement.querySelector('[data-account="g"]') as HTMLElement;
    expect(ownRow.textContent).not.toContain('Desactivar');
  });

  it('muestra el motivo cuando la API rechaza el alta', async () => {
    const create = vi.fn(() => throwError(() => new HttpErrorResponse({ status: 409, error: { code: 'account_exists' } })));
    const fixture = await render({ create });

    await fixture.componentInstance.create();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('Ese correo ya tiene una cuenta.');
  });
});
```

- [x] **Step 2: Ejecutar y verificar que fallan**

Run: `npm run test:web`
Expected: FAIL con «Could not resolve "./accounts.api"».

- [x] **Step 3: Implementación mínima**

`apps/web/src/entities/account/model/account-messages.ts`:

```ts
import { HttpErrorResponse } from '@angular/common/http';
import { AccountItem } from '@istpetdev/shared-core';

const ERROR_MESSAGES: Record<string, string> = {
  account_exists: 'Ese correo ya tiene una cuenta.',
  last_manager: 'Debe quedar al menos una Gerencia activa.',
  self_change_forbidden: 'No puedes desactivarte ni cambiar tu propio rol.',
  invalid_role: 'Elige uno de los seis roles humanos.',
  invalid_email: 'Escribe un correo válido.',
  invalid_display_name: 'Escribe el nombre de la persona.',
  technical_account: 'La cuenta técnica de automatización no se gestiona aquí.',
  invitation_not_pending: 'La persona ya completó su primer ingreso.',
  not_found: 'La cuenta ya no existe. Recarga la lista.',
  identity_unavailable: 'Cognito no responde. Intenta de nuevo en unos minutos.',
};

export function accountErrorMessage(error: unknown): string {
  const code = error instanceof HttpErrorResponse ? (error.error as { code?: string } | null)?.code : undefined;
  return (code && ERROR_MESSAGES[code]) || 'No se pudo completar la operación. Intenta de nuevo.';
}

export function accountStatusLabel(item: AccountItem): string {
  if (!item.isActive) return 'Desactivada';
  switch (item.invitationStatus) {
    case 'pending': return 'Invitación pendiente';
    case 'active': return 'Activa';
    case 'missing': return 'Sin usuario en Cognito';
    default: return 'Estado de Cognito desconocido';
  }
}
```

`apps/web/src/entities/account/api/accounts.api.ts`:

```ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AccountItem, CreateAccountRequest, UpdateAccountRequest } from '@istpetdev/shared-core';
import { environment } from '@shared/config';

@Injectable({ providedIn: 'root' })
export class AccountsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/accounts`;

  list() { return this.http.get<AccountItem[]>(this.base); }
  create(input: CreateAccountRequest) { return this.http.post<AccountItem>(this.base, input); }
  update(id: string, input: UpdateAccountRequest) { return this.http.patch<AccountItem>(`${this.base}/${id}`, input); }
  deactivate(id: string) { return this.http.post<AccountItem>(`${this.base}/${id}/deactivate`, {}); }
  reactivate(id: string) { return this.http.post<AccountItem>(`${this.base}/${id}/reactivate`, {}); }
  resendInvitation(id: string) { return this.http.post<void>(`${this.base}/${id}/resend-invitation`, {}); }
}
```

`apps/web/src/entities/account/index.ts`:

```ts
export * from './api/accounts.api';
export * from './model/account-messages';
```

`apps/web/src/pages/users/users.page.ts`:

```ts
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AccountItem, HUMAN_ROLES, HumanRole } from '@istpetdev/shared-core';
import { AccountsApi, accountErrorMessage, accountStatusLabel } from '@entities/account';
import { AuthSession } from '@entities/session';
import { ButtonComponent, CardComponent } from '@shared/ui';

/** FR-19: Gerencia da de alta, cambia de rol y retira el acceso. Nunca borra cuentas. */
@Component({
  selector: 'page-users',
  standalone: true,
  imports: [FormsModule, ButtonComponent, CardComponent],
  template: `
    <section class="max-w-5xl mx-auto py-6 space-y-6">
      <h1 class="text-2xl font-extrabold text-slate-900">Usuarios</h1>

      @if (error()) {
        <p role="alert" class="p-3 rounded-lg border border-red-200 bg-red-50 text-sm text-red-800">{{ error() }}</p>
      }

      <ui-card [padding]="'lg'">
        <form class="grid gap-3 sm:grid-cols-4 items-end" (submit)="$event.preventDefault(); create()">
          <label class="text-xs font-semibold text-slate-700">Correo
            <input class="mt-1 w-full rounded border border-slate-300 px-2 py-1.5 text-sm" type="email" name="email" [(ngModel)]="draft.email" required />
          </label>
          <label class="text-xs font-semibold text-slate-700">Nombre
            <input class="mt-1 w-full rounded border border-slate-300 px-2 py-1.5 text-sm" name="displayName" [(ngModel)]="draft.displayName" required />
          </label>
          <label class="text-xs font-semibold text-slate-700">Rol
            <select class="mt-1 w-full rounded border border-slate-300 px-2 py-1.5 text-sm" name="role" [(ngModel)]="draft.role">
              @for (role of roles; track role) { <option [value]="role">{{ role }}</option> }
            </select>
          </label>
          <ui-button type="submit" variant="primary" [loading]="busy()">Crear e invitar</ui-button>
        </form>
      </ui-card>

      <table class="w-full text-sm">
        <thead class="text-left text-xs uppercase text-slate-500">
          <tr><th class="py-2">Nombre</th><th>Correo</th><th>Rol</th><th>Estado</th><th></th></tr>
        </thead>
        <tbody>
          @for (account of accounts(); track account.id) {
            <tr class="border-t border-slate-100" [attr.data-account]="account.id">
              <td class="py-2 font-semibold text-slate-900">{{ account.displayName }}</td>
              <td class="text-slate-600">{{ account.email ?? '—' }}</td>
              <td>
                @if (isSelf(account)) {
                  {{ account.role }}
                } @else {
                  <select class="rounded border border-slate-300 px-1 py-1 text-sm" [ngModel]="account.role" (ngModelChange)="changeRole(account, $event)" [disabled]="busy()">
                    @for (role of roles; track role) { <option [value]="role">{{ role }}</option> }
                  </select>
                }
              </td>
              <td>
                {{ statusLabel(account) }}
                @if (account.outOfSync) { <span class="block text-xs text-amber-700">Desfase con Cognito: repite la última acción</span> }
              </td>
              <td class="text-right space-x-2">
                @if (account.isActive && account.invitationStatus === 'pending') {
                  <ui-button size="sm" variant="ghost" (buttonClick)="resend(account)">Reenviar invitación</ui-button>
                }
                @if (!isSelf(account)) {
                  <ui-button size="sm" [variant]="account.isActive ? 'destructive' : 'outline'" (buttonClick)="toggle(account)">
                    {{ account.isActive ? 'Desactivar' : 'Reactivar' }}
                  </ui-button>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
    </section>
  `
})
export class UsersPage implements OnInit {
  private readonly api = inject(AccountsApi);
  private readonly me = inject(AuthSession).me;

  readonly roles = HUMAN_ROLES;
  readonly accounts = signal<AccountItem[]>([]);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  readonly statusLabel = accountStatusLabel;
  draft: { email: string; displayName: string; role: HumanRole } = { email: '', displayName: '', role: 'Conductor' };

  ngOnInit(): void {
    void this.run(async () => undefined);
  }

  isSelf(account: AccountItem): boolean {
    return account.id === this.me()?.accountId;
  }

  async create(): Promise<void> {
    await this.run(async () => {
      await firstValueFrom(this.api.create({ ...this.draft }));
      this.draft = { email: '', displayName: '', role: 'Conductor' };
    });
  }

  async changeRole(account: AccountItem, role: HumanRole): Promise<void> {
    await this.run(() => firstValueFrom(this.api.update(account.id, { role })));
  }

  async toggle(account: AccountItem): Promise<void> {
    await this.run(() => firstValueFrom(account.isActive ? this.api.deactivate(account.id) : this.api.reactivate(account.id)));
  }

  async resend(account: AccountItem): Promise<void> {
    await this.run(() => firstValueFrom(this.api.resendInvitation(account.id)));
  }

  // Toda acción recarga la lista para reflejar el estado real de la API y de Cognito.
  private async run(action: () => Promise<unknown>): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      await action();
      this.accounts.set(await firstValueFrom(this.api.list()));
    } catch (error) {
      this.error.set(accountErrorMessage(error));
    } finally {
      this.busy.set(false);
    }
  }
}
```

`apps/web/src/pages/users/index.ts`:

```ts
export * from './users.page';
```

En `apps/web/src/pages/index.ts` añadir `export * from './users/users.page';`. En `apps/web/src/app/app.routes.ts`, importar `roleGuard` junto a `authGuard` y añadir antes de `public-trace`:

```ts
  {
    path: 'users',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Gerencia'] },
    loadComponent: () => import('@pages/users').then(m => m.UsersPage),
    title: 'Usuarios — IstpetDev Logistics'
  },
```

En la plantilla de `apps/web/src/widgets/header/header.widget.ts`, antes del nombre del usuario:

```html
                @if (session.role() === 'Gerencia') {
                  <a routerLink="/users" class="text-xs font-semibold text-primary-600 hover:text-primary-700">Usuarios</a>
                }
```

- [x] **Step 4: Ejecutar y verificar que pasan**

Run: `npm run test:web && npm run build:web`
Expected: PASS (`accounts.api.spec.ts` 2 casos, `users.page.spec.ts` 2 casos) y build sin errores.

- [x] **Step 5: Commit**

```bash
git add apps/web/src/entities/account apps/web/src/pages/users apps/web/src/pages/index.ts apps/web/src/app/app.routes.ts apps/web/src/widgets/header/header.widget.ts
git commit -m "feat(web): pantalla de usuarios para gerencia (spec 013 h4)"
```

### Desviaciones

- Antes de desactivar se pide confirmación (`window.confirm`); reactivar no la pide. La UI no ofrece acciones sobre la propia cuenta y «Reenviar invitación» solo aparece con `invitationStatus = 'pending'`.
- `directoryEnabled` y `outOfSync` se muestran con insignias visibles («Revisar en Cognito»); el formulario usa `label for`, `role="alert"` y `aria-live`.
- Pruebas añadidas fuera del plan: guard de `/users` (Planificador y Auditor redirigidos) y enlace del header solo para Gerencia. Web: 40 pruebas (27 + 13).
- Sugerencia no implementada: reemplazar `window.confirm` por un modal propio de la UI.

---

### Task 14: Cierre — E2E, spec, README y bóveda

**Files:**
- Modify: `docs/specs/013-login-y-cuentas/spec.md`, `docs/specs/013-login-y-cuentas/test-e2e.md`, `docs/specs/README.md`
- Modify: `docs/boveda/06 Equipo/Registro de trabajo previo y del evento.md`

**Interfaces:**
- Consumes: T1–T10 en `develop` (y T11–T13 si H4 entra).
- Produces: registro de ejecuciones con evidencia; spec en estado `cerrado` o con H4 declarada como propuesta.

- [x] **Step 1: Verificación completa local**

Run: `npm run check && npm run test:frontend && for c in Unit Integration E2E; do dotnet test tests/IstpetDev.Tests --filter "Category=$c" || break; done`
Expected: todo verde; cero coincidencias del guardián. Evidencia guardada en `docs/specs/013-login-y-cuentas/evidencia/e2e-00-resultados.txt`.

- [x] **Step 2: Ejecutar `test-e2e.md`** — escenarios obligatorios E2E-01 a E2E-08 (y E2E-09 a E2E-13 si H4 entra), con evidencia en `docs/specs/013-login-y-cuentas/evidencia/` sin correos, tokens ni códigos MFA (Fase B, issue #55).

- [x] **Step 3: Actualizar el spec** (fase A hecha; falta el estado `cerrado`): FR-18 y H4-6 pospuestos; H4 reflejada como implementada antes del congelamiento (PR #65); decisión ADR-25 vs ADR-24 registrada con opción (a); PA-04 resuelta (#55); sección «Estado de implementación» añadida; riesgo abierto de recarga offline documentado. Estado `cerrado` pendiente de validación E2E en Fase B.

- [x] **Step 4: README e índice** — fila 013 de `docs/specs/README.md` actualizada con el estado real y enlaces a `plan.md`, `task.md`, `test-e2e.md`; fila D3 de login Cognito actualizada.

- [x] **Step 5: Bóveda (G8 — fase A hecha)** — en «Registro de trabajo previo y del evento», registrados los hitos del 14-oct con PRs #56 y #65; línea pendiente dejada para cuentas creadas por rol y fecha (se llena en Fase B).

- [x] **Step 6: Commit y PR** (se ejecuta al completar la Fase B con revisión cruzada de Joseph Baño).

### Cierre (15-oct-2026)

- **T10:** hecha por Jorge en el PR #104 (#43 cerrado). Las casillas se marcaron en el cierre.
- **Step 2:** cerrado con pendientes asignados. Pasaron E2E-01, 02, 03, 05, 06 y 09; E2E-04, 07 y 10 en #107 (Jorge), E2E-11 a 13 en #108 (Bryan) y E2E-08 en #55 (Alexander y Joseph). Ver el registro en [test-e2e.md](test-e2e.md#registro-de-ejecuciones).
- **Step 3:** spec en `cerrado`.
- **Step 5:** registro de cuentas por rol añadido a la bóveda (sin correos).
- **Step 6:** PR de cierre de `docs/013-cierre-t14`.

### Desviaciones / estado fase A

- **Fase A realizada (cierre documental y validación local de suites):**
  - **Step 1 (E2E-00):** Se ejecutaron completamente `npm run check`, `npm run test:frontend` (87 pruebas pasadas en 18 archivos) y `dotnet test` por categoría (Unit: 145 pasadas; Integration: 146 pasadas; E2E: 6 pasadas; total backend 297 pasadas). Guardián de CI contra login simulado verificado con cero coincidencias (`grep=1`). Evidencia guardada en `docs/specs/013-login-y-cuentas/evidencia/e2e-00-resultados.txt`.
  - **Step 3:** Spec 013 actualizado sin cerrar (`estado: borrador` con nota explicativa al inicio). Se resolvió la contradicción ADR-25 vs ADR-24 adoptando la opción (a) (la PWA solo admite Bodega, Conductor y Receptor; `LogisticsManager` retirado en T7 de PR #56). Se marcó PA-04 como resuelta en #55 (Bryan Gerencia inicial, Jorge custodia el CSV fuera del repo). Se documentó H4 como implementada antes del congelamiento (PR #65). Se agregó la sección «Estado de implementación» enlazando las desviaciones de task.md y los pendientes. Se registró el riesgo abierto de recarga offline (tokens en memoria, FR-03; decide Joseph Baño con Alexander).
  - **Step 4:** Fila 013 de `docs/specs/README.md` actualizada con el estado real y enlaces a `plan.md`, `task.md` y `test-e2e.md`. Se actualizó la fila D3 para reflejar que H1–H4 están implementados en `develop`.
  - **Step 5 (parcial):** Se documentaron los PR #56 y #65 en `docs/boveda/06 Equipo/Registro de trabajo previo y del evento.md`. Se dejó una línea pendiente para el registro de cuentas de demo creadas por rol y fecha.
  - **Spec 004:** Se sincronizó la decisión 2 de `informe-entrega-spec-004-y-decisiones.md` para exigir tokens solo en memoria (ref. 013 FR-03, eliminando `sessionStorage`), y se actualizó T1b en `docs/specs/004-campo-qr-custodia-offline/task.md` indicando la absorción del login PKCE por 013 (PR #56) y el retiro de la «dev auth API».

- **Pendiente para la Fase B (cierre técnico y validación E2E en vivo):**
  - Configuración y validación de infraestructura (T10, issue #43): callbacks HTTPS de Cognito, marca ISTPET en Hosted UI, políticas IAM y hop limit = 2.
  - Publicación y routing de la PWA bajo `/campo/` (issue #52).
  - Escenarios manuales E2E-01 a E2E-13 de `test-e2e.md` en entorno HTTPS real con cuentas de demo aprovisionadas (issue #55).
  - Llenar el registro de cuentas creadas por rol y fecha en la bóveda (Step 5 definitivo).
  - Step 6: Commit final y apertura de PR hacia `develop` marcando el spec como `cerrado` con aprobación del equipo.

- **Resolución del Riesgo de Recarga (14-oct-2026):**
  - **Causa Raíz Resuelta**: El bloqueo de cookies de terceros en navegadores modernos impedía el silent renew por iframe en recargas. La sesión se reiniciaba al pulsar F5 o tipear `/inicio`.
  - **Adopción de IETF OAuth 2.0 BCP**: Se migró `TokenFreeStorage` a `sessionStorage` (aislado por pestaña y eliminado al cerrarla). La sesión sobrevive a recargas sin comprometer la seguridad (cero exposición en `localStorage`).
  - **Aprovisionamiento Idempotente del Equipo**: Se completó el alta y verificación en AWS Cognito y PostgreSQL (Desarrollo y RDS) para Gerencia (`ismael02doicela@gmail.com`), Planificador (`joseph.bano@istpet.edu.ec`), Auditor (`anthony.pilatasig@istpet.edu.ec`) y Conductor (`alkut202@gmail.com`).


