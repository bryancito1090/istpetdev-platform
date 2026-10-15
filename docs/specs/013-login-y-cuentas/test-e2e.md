# 013 — Login real y gestión de cuentas por Gerencia: pruebas de punta a punta

> Recorridos completos como los vería una persona del equipo o el jurado, no pruebas de una función. El spec se cierra cuando todos los escenarios obligatorios pasan con evidencia guardada. Las cifras se marcan «Medido» solo si enlazan esta ejecución (Convenciones y estados).
>
> **Evidencia sin datos sensibles:** ninguna captura o archivo guardado muestra correos personales, tokens, códigos TOTP, contraseñas temporales ni el contenido de la invitación. Recortar o difuminar antes de guardar en `docs/specs/013-login-y-cuentas/evidencia/`.
>
> **Cierre (15-oct-2026):** pasaron E2E-00, E2E-01, E2E-02, E2E-03, E2E-05, E2E-06 y E2E-09 (ver «Registro de ejecuciones»). El spec se cerró con los escenarios restantes asignados: E2E-04, E2E-07 y E2E-10 en [#107](https://github.com/bryancito1090/istpetdev-platform/issues/107); E2E-11 a E2E-13 en [#108](https://github.com/bryancito1090/istpetdev-platform/issues/108); E2E-08 queda con Alexander (Conductor) y Joseph (Receptor) según [#55](https://github.com/bryancito1090/istpetdev-platform/issues/55).
>
> **Cuentas de la demo:** Jorge fijó contraseñas definitivas con `aws cognito-idp admin-set-user-password --permanent` después de `--provision-accounts`. Por eso las cuentas quedan en `CONFIRMED` (no `FORCE_CHANGE_PASSWORD`) y no dependen del correo de invitación.

## Preparación del entorno

### Infraestructura (una vez, Jorge — T10)

```bash
# Callbacks/cierres, marca, permiso y hop limit del host de la app (i-0e0a979f8c0885a4f) aplicados y verificados (T10 Steps 5–7)
pool=$(terraform -chdir=infra/terraform/environments/dev output -json cognito | jq -r '.issuer | split("/") | last')
aws cognito-idp describe-user-pool-client --user-pool-id "$pool" \
  --client-id "$(terraform -chdir=infra/terraform/environments/dev output -json cognito | jq -r .client_ids.web)" \
  --query 'UserPoolClient.[CallbackURLs,LogoutURLs]'
```

### API local contra el pool real

```bash
npm ci
npm run docker:up
export AWS_PROFILE=<perfil IAM personal con el permiso de NFR-02>
COGNITO=$(terraform -chdir=infra/terraform/environments/dev output -json cognito)
export Authentication__Authority=$(echo "$COGNITO" | jq -r .issuer)
export Authentication__AllowedClientIds__0=$(echo "$COGNITO" | jq -r .client_ids.web)
export Authentication__AllowedClientIds__1=$(echo "$COGNITO" | jq -r .client_ids.mobile)
dotnet run --project src/Api -- --migrate
dotnet run --project src/Api            # http://localhost:5000
npm run dev:web                          # http://localhost:4200
npm run dev:mobile                       # http://localhost:8100
```

### Archivo de cuentas (fuera del repositorio, G1)

Lo guarda la persona que decida PA-04. Ejemplo de forma (cada correo real lo escribe esa persona; nunca se sube):

```text
email,displayName,role
<correo de Gerencia inicial>,<nombre>,Gerencia
<correo de planificación>,<nombre>,Planificador
<correo de auditoría>,<nombre>,Auditor
<correo de bodega>,<nombre>,Bodega
<correo de conducción>,<nombre>,Conductor
<correo de recepción>,<nombre>,Receptor
```

Ruta sugerida: `~/istpetdev-cuentas-demo.csv` con `chmod 600`.

### Teléfonos y autenticador

- MFA desactivado en el pool desde el 15-oct-2026: el primer ingreso solo pide cambiar la contraseña temporal; ya no se registra TOTP.
- Las invitaciones caducan en **1 día**: crear o reenviar las cuentas de la demo el **15-oct** (riesgo del spec).

## Escenarios

| ID | Escenario | Cubre | Tipo | Obligatorio |
|---|---|---|---|---|
| E2E-00 | Suites automáticas verdes | CE-02, CE-03, CE-05, CE-07, NFR-01, NFR-04, NFR-05 | automatizado | sí |
| E2E-01 | Script de arranque crea la Gerencia y las cuentas de demo; segunda ejecución no duplica | H3, FR-11, CE-03 | manual | sí |
| E2E-02 | Planificador entra a la web con contraseña; la API recibe Bearer; ningún token persiste | H1-1, FR-01–FR-05, CE-02 | manual | sí |
| E2E-03 | Recargar una ruta privada vuelve autenticado sin pedir contraseña | H1-2, FR-03, FR-08 | manual | sí |
| E2E-04 | Cada rol solo entra a su plataforma | H1-3, FR-07, CE-01 | manual | sí |
| E2E-05 | Auditor ve todo y no puede escribir | H1-6, FR-21, CE-07 | manual | sí |
| E2E-06 | Identidad sin cuenta o con cuenta inactiva ve «Tu cuenta no está habilitada; contacta a Gerencia» | H1-4, FR-05 | manual | sí |
| E2E-07 | Recuperar la contraseña desde la página de Cognito | H1-5 | manual | sí |
| E2E-08 | Logout del conductor con capturas pendientes; otra cuenta no las ve ni las sincroniza | H2, FR-09, CE-04 | manual | sí |
| E2E-09 | Página de Cognito con la marca ISTPET | FR-20 | manual | sí |
| E2E-10 | Seis roles en el despliegue HTTPS de la demo | CE-01, #14 | manual | sí |
| E2E-11 | Gerencia crea un Conductor desde «Usuarios»; primer ingreso; ve solo sus entregas en la PWA | H4-1, FR-12, FR-13, FR-19, CE-06 | manual | P2 (sí si H4 entra) |
| E2E-12 | Gerencia desactiva al Conductor: 403 inmediato, sesión de Cognito revocada; reactivación | H4-2, FR-15, NFR-01, CE-05 | manual | P2 (sí si H4 entra) |
| E2E-13 | Reglas de Gerencia: propia cuenta, correo repetido, reenvío de invitación vencida | H4-3, H4-4, H4-5, FR-16, FR-17 | manual | P2 (sí si H4 entra) |

H4-6 (`AuditEvent`) no tiene escenario: FR-18 está pospuesto (ver [plan](plan.md#contradicciones-detectadas-al-planificar)).

### E2E-00 — Suites automáticas verdes

**Objetivo:** demostrar con el commit exacto que las reglas cubiertas por pruebas se cumplen.

**Pasos:**

1. `for c in Unit Integration E2E; do dotnet test tests/IstpetDev.Tests -c Release --filter "Category=$c" --logger "trx;LogFileName=$c.trx" --results-directory artifacts/spec013 || break; done`
2. `npm run test:frontend`
3. `npm run check:no-mock-login`
4. `grep -rnE "loginWithCredentials|useMockSession|jwt-mock" apps/ --include=*.ts --include=*.html; echo "grep=$?"`

**Resultado esperado:** 0 fallos en las tres categorías .NET (incluye `AuditorRoleTests`, `MeEndpointTests`, `AccountProvisioningTests`, `ProvisionAccountsCommandTests`, `AccountsApiTests`, `LeanSchemaTests` con 29 tablas); 0 fallos frontend; «Sin login simulado en apps»; `grep=1`.

**Evidencia a guardar:** `artifacts/spec013/*.trx` resumidos en `evidencia/e2e-00-resultados.txt` (totales por categoría y commit).

### E2E-01 — Script de arranque idempotente

**Objetivo:** que la Gerencia inicial y las cuentas de demo existan sin interfaz (plan B de H4).

**Pasos:**

1. Local: `dotnet run --project src/Api -- --provision-accounts ~/istpetdev-cuentas-demo.csv; echo "salida=$?"`.
   En el host de la demo, **dentro del contenedor que ya corre** (hereda `ConnectionStrings__Main` con la contraseña de RDS que exporta el deploy; un `docker compose run` nuevo arrancaría sin `DB_PASSWORD`):

   ```bash
   docker exec -i istpetdev-backend sh -c 'cat > /tmp/cuentas.csv' < ~/istpetdev-cuentas-demo.csv
   docker exec istpetdev-backend dotnet IstpetDev.Api.dll --provision-accounts /tmp/cuentas.csv; echo "salida=$?"
   docker exec istpetdev-backend rm -f /tmp/cuentas.csv
   ```

   Requiere el perfil IAM y el hop limit 2 del host (T10 Steps 4–6); sin ellos cada fila sale «rechazada: Cognito no está disponible.».
2. Repetir el mismo comando.
3. `aws cognito-idp list-users --user-pool-id "$pool" --query 'Users[].[Attributes[?Name==\`sub\`]|[0].Value,UserStatus,Enabled]' --output text`
4. En la base: `SELECT "Role", "IsActive", "Kind", "Subject" FROM "Account" WHERE "Kind" = 'human' ORDER BY "Role";`
5. Para una cuenta: `aws cognito-idp admin-get-user --user-pool-id "$pool" --username <sub de esa cuenta> --query Username` (comprueba que el `sub` sirve como `Username` en las llamadas Admin).

**Resultado esperado:** 1.ª ejecución: 6 líneas «creada; Cognito envió la invitación» y `salida=0`; cada persona recibe el correo de invitación. 2.ª ejecución: 6 líneas «ya existía; sin cambios», sin correos nuevos. Ninguna línea contiene «@» ni contraseñas. Cognito: 6 usuarios `FORCE_CHANGE_PASSWORD`, `True`. Base: 6 `Account` activas, `Kind = human`, `Subject` igual al `sub` de Cognito. Paso 5 devuelve el mismo `sub`.

**Evidencia a guardar:** salida de ambas ejecuciones (ya sin correos), conteo por rol de la consulta SQL (sin `Subject`), fecha de creación para el registro G8.

### E2E-02 — Login web del Planificador

**Objetivo:** acceso real con identidad administrada y sin tokens persistidos.

**Pasos:**

1. Navegador en ventana privada, DevTools abierto (Network con «Preserve log»). Ir a `http://localhost:4200/login`.
2. Pulsar «Iniciar sesión»: se abre `https://…amazoncognito.com/login?…code_challenge_method=S256…`.
3. Entrar con la cuenta de Planificador; en el primer ingreso, cambiar la contraseña temporal.
4. Al volver, comprobar la cabecera (nombre y rol) y abrir en Network `GET /api/v1/me` y otra llamada a `/api/v1/…`.
5. En Application: revisar `localStorage`, `sessionStorage` e IndexedDB del origen `localhost:4200`.
6. Buscar en la consola y en la URL final cualquier `access_token`, `id_token` o `refresh_token`.

**Resultado esperado:** la URL de Cognito incluye `code_challenge_method=S256` y `scope=openid+email+profile+istpetdev-api%2Faccess`. Tras volver, la URL queda en `/dashboard` sin `code` ni `state`. `/me` responde 200 con `role: "Planificador"`, `platform: "web"`; las llamadas a `/api/v1/…` llevan `Authorization: Bearer …`; la traza pública no. `localStorage` vacío; `sessionStorage` solo con `istpetdev.returnUrl` (si quedó) y la clave de configuración OIDC cuyo valor contiene únicamente `codeVerifier`, `authStateControl` y `authNonce`, o nada; IndexedDB sin tokens. Ningún token en la URL ni en la consola.

**Evidencia a guardar:** captura de la cabecera con nombre difuminado y rol visible; captura de `sessionStorage` (valores recortados); lista de cabeceras de una petición con el token sustituido por `Bearer <recortado>`.

### E2E-03 — Recarga sin contraseña

**Objetivo:** H1-2 con tokens solo en memoria.

**Pasos:**

1. Con la sesión de E2E-02, recargar `http://localhost:4200/dashboard` (F5).
2. Observar la secuencia en Network.

**Resultado esperado:** `/dashboard` → `/login?returnUrl=%2Fdashboard` → Cognito `authorize` → `/auth/callback` → `/dashboard`, sin mostrar el formulario de contraseña ni pedir TOTP mientras la sesión de Cognito siga vigente. Tiempo total ≤ 5 s con red normal.

**Evidencia a guardar:** captura de Network con la secuencia de redirecciones (sin parámetros `code`).

### E2E-04 — Cada rol en su plataforma

**Objetivo:** ADR-24 aplicado con `/me`.

**Pasos:**

1. Web (`:4200`) con la cuenta de Conductor.
2. Intentar abrir `http://localhost:4200/dashboard` escribiendo la URL.
3. PWA (`:8100`) con la cuenta de Gerencia; intentar `http://localhost:8100/home`.
4. Repetir 1–2 con Bodega y Receptor; 3 con Planificador y Auditor.

**Resultado esperado:** Bodega, Conductor y Receptor en la web ven «Tu cuenta opera en la app de campo» y el botón «Cerrar sesión»; `/dashboard` vuelve a `/login`. Gerencia, Planificador y Auditor en la PWA ven «Tu cuenta opera en la plataforma web»; `/home` vuelve a `/login`. Ninguna ruta privada muestra datos.

**Evidencia a guardar:** dos capturas (web con rol de campo, PWA con rol web).

### E2E-05 — Auditor de solo lectura

**Objetivo:** CE-07 con un token real.

**Pasos:**

1. Web con la cuenta de Auditor: recorrer panel, rutas, entregas, custodia y decisiones del agente.
2. Copiar el access token desde DevTools (Network → cabecera de una petición) a una variable de shell `T` sin guardarlo en archivo.
3. `for p in service-points risks route-plans agent-decisions autonomy/mode; do curl -s -o /dev/null -w "$p %{http_code}\n" -H "Authorization: Bearer $T" http://localhost:5000/api/v1/$p; done`
4. `for p in demo/reset imports route-plans incidents; do curl -s -o /dev/null -w "$p %{http_code}\n" -X POST -H "Authorization: Bearer $T" -H 'Content-Type: application/json' -d '{}' http://localhost:5000/api/v1/$p; done; curl -s -o /dev/null -w "autonomy PUT %{http_code}\n" -X PUT -H "Authorization: Bearer $T" -H 'Content-Type: application/json' -d '{"mode":"ProposeOnly"}' http://localhost:5000/api/v1/autonomy/mode`
5. `unset T`.

**Resultado esperado:** ningún botón de acción visible (aprobar, rechazar, publicar, reiniciar, cambiar modo, «Usuarios»). Paso 3: cinco `200`. Paso 4: todos `403`.

**Evidencia a guardar:** salida de los pasos 3–4; captura del panel del Auditor.

### E2E-06 — Identidad sin cuenta habilitada

**Objetivo:** H1-4 con el pool real.

**Pasos:**

1. Crear en Cognito un usuario de prueba sin `Account`: `aws cognito-idp admin-create-user --user-pool-id "$pool" --username <correo de prueba del equipo> --user-attributes Name=email,Value=<mismo correo> Name=email_verified,Value=true`.
2. Completar su primer ingreso en la web.
3. Crear su `Account` con el script (una fila con rol Planificador) y luego desactivarla: `UPDATE "Account" SET "IsActive" = false WHERE "Subject" = '<sub>';`. Recargar la web.
4. Limpiar: `aws cognito-idp admin-disable-user …` y dejar la `Account` inactiva (no se borran cuentas).

**Resultado esperado:** en 2 y en 3, «Tu cuenta no está habilitada; contacta a Gerencia» sin ningún dato; `/me` responde 403.

**Evidencia a guardar:** captura del mensaje; código de respuesta de `/me`.

### E2E-07 — Recuperar contraseña

**Objetivo:** H1-5 sin pantallas propias.

**Pasos:**

1. En `/login`, «Iniciar sesión» → en Cognito, «Forgot your password?».
2. Escribir el correo de una cuenta de demo; usar el código recibido para definir una contraseña nueva y entrar con ella. Solo funciona con una cuenta que ya completó su primer ingreso: Cognito no recupera la contraseña de una cuenta con la contraseña temporal pendiente (para esa, «Reenviar invitación»).

**Resultado esperado:** el correo llega a la dirección verificada; la nueva contraseña cumple la política (14 caracteres, mayúsculas, minúsculas, números, símbolos); el ingreso termina en `/dashboard`.

**Evidencia a guardar:** captura de la pantalla de recuperación con la marca (sin correo visible).

### E2E-08 — Logout del conductor con capturas pendientes

**Objetivo:** CE-04: nada se pierde ni se cruza entre cuentas.

**Pasos:**

1. PWA en el teléfono de la demo, sesión de Conductor; descargar la ruta.
2. Activar modo avión (o «Simular sin señal»). Registrar dos recepciones con foto y firma.
3. Pulsar «Cerrar sesión» (sigue sin señal): leer el aviso y confirmar.
4. Quitar el modo avión. Iniciar sesión con la cuenta de Receptor en el mismo teléfono; abrir la bandeja de sincronización.
5. Cerrar sesión e iniciar de nuevo con la cuenta de Conductor; abrir la bandeja y sincronizar.
6. En la base: `SELECT count(*) FROM "Receipt" WHERE "ActorAccountId" = '<id de la cuenta de Conductor>';` (o el endpoint de recepciones de 004 si está integrado).

**Resultado esperado:** paso 3: «Tienes 2 capturas pendientes. Se guardarán en este teléfono para tu cuenta y se enviarán cuando vuelvas a entrar.»; tras confirmar, la app vuelve a `/login`. Paso 4: la bandeja del Receptor está vacía y no se envía ninguna recepción (Network sin `POST` de recibos). Paso 5: aparecen las dos capturas del Conductor y se sincronizan. Paso 6: dos recepciones del Conductor (si el backend de 004 está integrado; si no, dejar constancia de que se verificó solo hasta la bandeja).

**Evidencia a guardar:** captura del aviso; captura de la bandeja vacía del Receptor; captura de la bandeja del Conductor con las dos capturas en «Completa».

**Límite declarado:** recargar la PWA **sin señal** deja la bandeja vacía hasta volver a iniciar sesión con señal (contradicción registrada en el plan). No forma parte de este escenario.

### E2E-09 — Marca en la página de Cognito

**Objetivo:** FR-20.

**Pasos:** abrir el login desde la web y desde la PWA en un teléfono.

**Resultado esperado:** banner `#00A7C4`, botón principal `#EA601F`, textos `#2D3748`, logo ISTPET si se cargó el PNG. La tipografía es la de Cognito: la UI clásica no admite fuentes web (limitación declarada, sin impacto en la demo).

**Evidencia a guardar:** dos capturas (escritorio y teléfono).

### E2E-10 — Seis roles en el despliegue HTTPS

**Objetivo:** CE-01 en el entorno de la demo.

**Pasos:**

1. Con las imágenes de `develop` desplegadas, abrir `https://istpetdev.doicela.dev/login` y `https://istpetdev.doicela.dev/campo/`.
2. Entrar con cada uno de los seis roles en su plataforma.
3. En el host: `docker compose -f infra/compose/docker-compose.prod.yml logs istpetdev-backend | grep -c "Authentication failed"` antes y después.

**Resultado esperado:** los seis entran con su contraseña y ven su nombre y rol; la URL de retorno es HTTPS exacta; ningún error `redirect_mismatch`; sin nuevos «Authentication failed».

**Evidencia a guardar:** tabla rol → plataforma → resultado (sin nombres); conteo de errores antes/después.

### E2E-11 — Alta de un Conductor desde «Usuarios» (P2)

**Objetivo:** CE-06.

**Pasos:**

1. Web con la cuenta de Gerencia → «Usuarios».
2. Crear correo de prueba del equipo, nombre y rol Conductor.
3. La persona completa su primer ingreso en la PWA (cambio de la contraseña temporal).
4. Gerencia recarga la lista.
5. Asignar a esa cuenta una entrega publicada (flujo de 002/004) y abrir la PWA del nuevo Conductor.

**Resultado esperado:** paso 2: fila con «Invitación pendiente» y llega la invitación. Paso 4: «Activa». Paso 5: ve solo su entrega.

**Evidencia a guardar:** captura de la lista en ambos estados (correos difuminados).

### E2E-12 — Desactivación con revocación inmediata (P2)

**Objetivo:** CE-05 y NFR-01 con Cognito real.

**Pasos:**

1. El Conductor de E2E-11 con sesión abierta en la PWA; copiar su token a `T` (DevTools remoto) o usar la PWA.
2. Gerencia pulsa «Desactivar».
3. Inmediatamente: `curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $T" http://<api>/api/v1/me` (o cualquier acción en la PWA).
4. `aws cognito-idp admin-get-user --user-pool-id "$pool" --username <sub> --query Enabled`.
5. Intentar iniciar sesión de nuevo con esa cuenta.
6. Gerencia pulsa «Reactivar»; la persona vuelve a entrar.

**Resultado esperado:** paso 3: `403` antes de que venza el token (≤ 15 min). Paso 4: `false`. Paso 5: Cognito rechaza el ingreso. Paso 6: entra y `/me` responde 200.

**Evidencia a guardar:** salida de los pasos 3–4 con hora; captura del rechazo de Cognito.

### E2E-13 — Reglas de Gerencia (P2)

**Objetivo:** H4-3, H4-4, H4-5.

**Pasos:**

1. Siendo la única Gerencia activa, buscar la propia fila.
2. Crear otra vez el correo del Conductor de E2E-11.
3. Crear una cuenta, esperar a que la invitación caduque (más de 1 día) o probar con una cuenta creada el día anterior; pulsar «Reenviar invitación».

**Resultado esperado:** paso 1: la propia fila no ofrece «Desactivar» ni selector de rol. Paso 2: «Ese correo ya tiene una cuenta.» y la lista no cambia. Paso 3: llega una nueva contraseña temporal y la persona completa su primer ingreso.

**Evidencia a guardar:** capturas de los mensajes.

## Registro de ejecuciones

| Fecha | Commit | Escenario | Resultado | Evidencia | Quién |
|---|---|---|---|---|---|
| 2026-10-14 | `29572c3` | E2E-00 | Pasa | [e2e-00-resultados.txt](evidencia/e2e-00-resultados.txt) | Joseph |
| 2026-10-15 | `2b8793f` | E2E-01 | Pasa (6 cuentas por rol; segunda ejecución sin duplicados) | Salida en poder de Jorge; CSV fuera del repositorio (G1) | Jorge |
| 2026-10-15 | `2b8793f` | E2E-02 | Pasa | Reportado en la sesión de cierre; sin captura en el repositorio | Equipo |
| 2026-10-15 | `2b8793f` | E2E-03 | Pasa | Reportado en la sesión de cierre; sin captura en el repositorio | Equipo |
| 2026-10-15 | `2b8793f` | E2E-05 | Pasa | Reportado en la sesión de cierre; sin captura en el repositorio | Joseph |
| 2026-10-15 | `2b8793f` | E2E-06 | Pasa | Reportado en la sesión de cierre; sin captura en el repositorio | Joseph |
| 2026-10-15 | `2b8793f` | E2E-09 | Pasa (banner `#00A7C4`, botón `#EA601F`; sin logo) | [escritorio](evidencia/e2e-09-marca-escritorio.png), [teléfono](evidencia/e2e-09-marca-telefono.png) (correo tapado) | Joseph |
| — | — | E2E-04, E2E-07, E2E-10 | Pendiente | [#107](https://github.com/bryancito1090/istpetdev-platform/issues/107) | Jorge |
| — | — | E2E-08 | Pendiente | [#55](https://github.com/bryancito1090/istpetdev-platform/issues/55) | Alexander y Joseph |
| — | — | E2E-11 a E2E-13 | Pendiente | [#108](https://github.com/bryancito1090/istpetdev-platform/issues/108) | Bryan |
