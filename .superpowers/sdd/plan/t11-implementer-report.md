# Reporte de implementación — Task 11

## Alcance y resultado

Rama `feat/013-h4-cuentas`, base `029c823f249b42ebef9a2968af47b0316fd94b3b`. Se implementaron `GET` y `POST /api/v1/accounts` para Gerencia, con `AccountAdministration` sobre los contratos existentes de aprovisionamiento y directorio. La respuesta de creación incluye solo el estado y los datos públicos de la cuenta; no contiene contraseña temporal, token ni secreto. El controlador usa `User.AccountId()` y responde 403 cuando no se resuelve el actor.

## Evidencia RED

Comando: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~AccountsApiTests" -m:1`

Salida relevante, código de salida 1:

```text
tests/IstpetDev.Tests/Integration/Accounts/AccountsApiTests.cs(66,60): error CS0246: The type or namespace name 'AccountItem' could not be found
tests/IstpetDev.Tests/Integration/Accounts/AccountsApiTests.cs(75,51): error CS0246: The type or namespace name 'AccountItem' could not be found
```

También apareció `NU1900` porque NuGet no pudo consultar `https://api.nuget.org/v3/index.json` para datos de vulnerabilidades. La compilación alcanzó el error esperado del contrato ausente.

## Evidencia GREEN

Primer intento: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~AccountsApiTests" -m:1 --no-restore` compiló, pero el sandbox denegó el socket local de VSTest (`System.Net.Sockets.SocketException (13): Permission denied`); ejecución abortada. Se repitió el mismo comando con autorización de ejecución fuera del sandbox.

Salida final, código de salida 0:

```text
Passed!  - Failed:     0, Passed:    11, Skipped:     0, Total:    11, Duration: 27 s - IstpetDev.Tests.dll (net8.0)
```

Persistieron las advertencias `NU1900` por la consulta de vulnerabilidades de NuGet. No hubo errores ni advertencias de compilación del código nuevo en la ejecución final.

## Suite completa

Comando ejecutado una vez antes del commit: `dotnet test -m:1`, autorizado fuera del sandbox por el requisito de socket local de VSTest.

Salida final, código de salida 0:

```text
Passed!  - Failed:     0, Passed:   288, Skipped:     0, Total:   288, Duration: 5 m 45 s - IstpetDev.Tests.dll (net8.0)
```

La restauración emitió `NU1900` al no poder consultar los datos de vulnerabilidades de NuGet. No se modificó infraestructura ni se añadieron dependencias.

## Pruebas añadidas

- `Only_gerencia_manages_accounts`: tres casos, Planificador, Auditor y Conductor; GET y POST dan 403.
- `Actor_without_account_cannot_manage_accounts`: GET y POST dan 403 con un sujeto sin cuenta.
- `Create_sends_invitation_and_lists_pending_then_active`: creación 201, correo normalizado, invitación pendiente, transición a activa, Gerencia en lista y ausencia de claves de contraseña, token y secreto en la respuesta.
- `Existing_email_is_rejected_without_creating_anything`: 409 `account_exists`, sin segunda invitación ni segunda cuenta.
- `Invalid_role_is_422`: dos casos, `Automatizacion` y rol desconocido; 422 `invalid_role`.
- `Invalid_input_has_problem_code`: correo con comilla (`invalid_email`) y nombre vacío (`invalid_display_name`), ambos 422.
- `Cognito_down_is_503_and_leaves_no_changes`: creación y lista con 503 `identity_unavailable`, sin persistencia parcial ni mensaje crudo del directorio.

## Archivos y revisión

- `src/Application/Accounts/AccountAdministration.cs`: DTOs, lista combinada de Account y directorio, creación delegada a `AccountProvisioning`.
- `src/Api/Controllers/AccountsController.cs`: autorización de Gerencia, GET/POST, validación de actor y Problem Details con `code`.
- `src/Application/DependencyInjection.cs`: registro scoped de `AccountAdministration`.
- `tests/IstpetDev.Tests/Integration/Accounts/AccountsApiTests.cs`: once casos de integración.
- `.superpowers/sdd/plan/t11-implementer-report.md`: este reporte.

`git diff --check` no señaló errores. La revisión del diff mostró únicamente estos archivos y ningún cambio en Task 12, rutas prohibidas, migraciones, infraestructura, dependencias o registros de correo/nombre. `superpowers:subagent-driven-development` no está instalado en las rutas de skills disponibles; se cumplió su requisito explícito de ejecutar la suite completa una vez antes del commit.

## Corrección tras revisión independiente

Se atendieron los tres hallazgos de `t11-code-review.md`. `CreateAsync` valida y normaliza correo, nombre y rol antes de invocar `ProvisionAsync` y construye el `AccountItem` con el identificador, usuario de directorio y datos validados que ya están disponibles al confirmar la cuenta. Se eliminó la consulta `SingleAsync` posterior al commit. El POST conserva 201 y deja `Location` ausente, ya que no existe GET por ID. Campos de correo, nombre o rol ausentes o `null` producen 422 Problem Details con `invalid_email`, `invalid_display_name` o `invalid_role`, sin invitar ni llamar al directorio.

### RED de la corrección

Comando: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~AccountsApiTests" -m:1 --no-restore`, autorizado fuera del sandbox por el socket local de VSTest. Código de salida 1:

```text
Failed!  - Failed:     7, Passed:    10, Skipped:     0, Total:    17, Duration: 38 s - IstpetDev.Tests.dll (net8.0)
```

Las seis variantes de campos ausentes o `null` obtuvieron `BadRequest` en vez de `UnprocessableEntity`; la prueba de creación encontró `Location: /api/v1/accounts/{id}` cuando esperaba ausencia de cabecera. Son fallas por los comportamientos descritos en la revisión.

### GREEN de la corrección

Mismo comando, código de salida 0:

```text
Passed!  - Failed:     0, Passed:    17, Skipped:     0, Total:    17, Duration: 38 s - IstpetDev.Tests.dll (net8.0)
```

Pruebas añadidas: seis casos de `Missing_or_null_fields_have_domain_problem_code`; la prueba de creación comprueba ausencia de `Location`, nombre normalizado, coincidencia del DTO con la fila persistida y el resto del comportamiento anterior.

No se simuló un fallo de lectura de base de datos inmediatamente después del commit: los dobles de integración actuales no permiten inyectar un fallo solo en esa fase sin sustituir el contexto de EF o añadir instrumentación de infraestructura, fuera de la corrección mínima. La comprobación estática del flujo confirma que ya no hay lectura de base de datos después de `ProvisionAsync`, y la prueba de creación verifica la respuesta junto con la cuenta persistida.

### Suite completa de la corrección

`dotnet test -m:1`, ejecutado una vez antes del commit con autorización para el socket local, código de salida 0:

```text
Passed!  - Failed:     0, Passed:   294, Skipped:     0, Total:   294, Duration: 5 m 16 s - IstpetDev.Tests.dll (net8.0)
```

Solo aparecieron las advertencias `NU1900` de consulta de datos de vulnerabilidades a NuGet. El diff de corrección contiene `AccountAdministration.cs`, `AccountsController.cs`, `AccountsApiTests.cs` y este reporte; no cambia rutas ni archivos prohibidos.
