# Task 12 — informe de implementación

## Alcance

Se añadieron `PATCH /api/v1/accounts/{id}`, `POST .../{id}/deactivate`, `.../reactivate` y `.../resend-invitation`, con sus métodos de administración y pruebas de integración. Los cuatro endpoints heredan `[Authorize(Roles = Roles.Gerencia)]` y ejecutan `Run`, que exige `User.AccountId()`; no hay fallback del actor.

## Decisiones de comportamiento

- La desactivación confirma `Account.IsActive=false` antes de llamar a Cognito. Si falla el directorio, devuelve 200 con `InvitationStatus=unknown` y `OutOfSync=true`; el mismo token recibe 403 en `/api/v1/me`. Repetir la operación sincroniza la cuenta y revoca sesiones.
- La reactivación cambia el estado local primero, llama a `EnableAsync` y `SignOutEverywhereAsync`, y ante `IdentityDirectoryUnavailableException` restaura `IsActive=false`. El controlador devuelve 503 `identity_unavailable` sin el detalle del directorio.
- El reenvío exige estado `InvitationPending`, devuelve 204 vacío y no muestra contraseña temporal ni token.
- `last_manager` se comprueba bajo bloqueo de organización en dominio. Con un único Gerencia activo, una petición HTTP válida para retirarlo es necesariamente un autocambio y se rechaza antes con `self_change_forbidden`. El test de dominio demuestra `last_manager`; el controlador ya mapea ese código a 409 Problem Details, pero no se debilitó la autorización para hacerlo alcanzable por HTTP.
- La respuesta de gestión usa `AccountItem`; ante directorio no disponible al describir una desactivación, `unknown` y `OutOfSync=true` muestran el desfase. No se añadieron dependencias ni migraciones.

## TDD y verificación

1. **RED** — `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~AccountsApiTests' -m:1`: exit 1 por `CS1061`, `AccountAdministration` no contenía `DeactivateAsync` (`AccountsApiTests.Changes.cs:131`). También hubo `NU1900` porque no se pudo consultar `api.nuget.org`.
2. **GREEN, primer intento** — mismo filtro con `--no-restore` dentro del sandbox: compiló, pero VSTest abortó con `System.Net.Sockets.SocketException (13): Permission denied` al abrir su socket local. Se repitió el mismo comando con autorización de ejecución fuera del sandbox.
3. **GREEN, ejecución autorizada** — `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~AccountsApiTests' -m:1 --no-restore`: exit 0; `Failed: 0, Passed: 31, Skipped: 0, Total: 31, Duration: 1 m 1 s`.
4. **Suite completa, ejecución autorizada** — `dotnet test -m:1`: exit 0; `Failed: 0, Passed: 308, Skipped: 0, Total: 308, Duration: 5 m 28 s`. Persistieron avisos `NU1900` por consulta de vulnerabilidades a NuGet no disponible; no hubo fallos de compilación ni pruebas.

## Pruebas nuevas

- `Only_gerencia_can_call_change_endpoints_without_side_effects` (Planificador, Auditor, Conductor; 4 rutas; estado local y directorio intactos).
- `Deactivation_blocks_the_next_request_and_revokes_cognito_sessions`.
- `Cognito_down_during_deactivation_still_blocks_and_shows_the_gap`.
- `Cognito_down_during_reactivation_restores_inactive_account` (`identity_unavailable`).
- `Gerencia_cannot_deactivate_or_change_the_role_of_its_own_account` (`self_change_forbidden`).
- `The_last_active_gerencia_keeps_its_role` (`last_manager`, dominio).
- `Role_change_moves_the_person_to_the_other_platform`.
- `Invalid_patch_name_has_problem_code_and_preserves_account` (`invalid_display_name`).
- `Automation_cannot_be_assigned_or_managed_through_the_api` (`invalid_role`, `technical_account`).
- `Invitation_is_resent_only_while_pending` (`invitation_not_pending`, 204 vacío).
- `Missing_account_has_not_found_problem_code_on_each_change_route` (`not_found`).
- `Resend_with_unavailable_directory_is_503_without_raw_error` (`identity_unavailable`).

Las pruebas existentes de T11 cubren GET/POST solo Gerencia, actor sin cuenta, correo duplicado, rol/correo/nombre inválidos y Cognito no disponible. La suite completa incluye `LeanSchemaTests`; no se modificó el esquema.
