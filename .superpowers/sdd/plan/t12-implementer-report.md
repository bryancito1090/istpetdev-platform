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

## Corrección tras revisión del spec

La revisión `.superpowers/sdd/plan/t12-spec-review.md` detectó dos fallos ante Cognito no disponible y pidió comprobar el borde del controlador para `last_manager`.

- PATCH valida rol y nombre, obtiene la cuenta humana bajo el bloqueo de organización y consulta `FindBySubjectAsync` **antes** de cambiar valores y guardar. Si el directorio falla, `Run` devuelve 503 `identity_unavailable` y la transacción no persiste cambios. La respuesta usa el usuario consultado antes de guardar; no hace otra lectura del directorio después del commit.
- Una revocación pendiente se guarda en una tabla concurrente en memoria, asociada a la instancia singleton de `IIdentityDirectory`. Se marca después de persistir `IsActive=false` y se borra solo tras completar `DisableAsync` y `SignOutEverywhereAsync`. La respuesta y `GET /accounts` suman esta señal al cálculo de `OutOfSync`. Una segunda desactivación reintenta ambos pasos y limpia el desfase al completar la revocación.
- **Límite de duración:** la marca de revocación pendiente existe solo mientras vive el proceso del API. Si el proceso reinicia después de que `DisableAsync` tuvo éxito pero `SignOutEverywhereAsync` falló, la marca se pierde y la lista puede mostrar `OutOfSync=false`; un reintento manual de desactivación sigue revocando sesiones. Persistir esa marca entre reinicios requeriría esquema adicional, excluido por NFR-03. El bloqueo de `/me` sí persiste en `Account.IsActive=false`.
- La prueba controlada de `AccountsController.Deactivate` con `AccountId` distinto del último Gerencia comprueba `ObjectResult` 409, `application/problem+json` y `code=last_manager`. La autorización real no permite tal actor en ese estado; la prueba evita relajar `[Authorize(Roles=Gerencia)]` y conserva el test de dominio.

### Evidencia RED/GREEN de la corrección

- **RED:** `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~AccountsApiTests' -m:1 --no-restore`: exit 1; 2 fallos, 32 aprobadas, 34 en total. `Failed_sign_out_after_disable_is_visible_in_response_and_list_until_retry` esperaba `OutOfSync=true` y recibió `false`; `Patch_with_unavailable_directory_is_503_and_keeps_original_values` no encontró `code` porque recibió la respuesta anterior 200. `Last_manager_maps_to_conflict_problem_details_at_controller_boundary` pasó.
- **GREEN:** mismo comando: exit 0; `Failed: 0, Passed: 34, Skipped: 0, Total: 34, Duration: 1 m 4 s`.
- **Suite completa tras corrección:** `dotnet test -m:1`, exit 0; `Failed: 0, Passed: 311, Skipped: 0, Total: 311, Duration: 5 m 17 s`. Persistieron avisos `NU1900` por acceso a NuGet para datos de vulnerabilidades.
