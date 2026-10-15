---
id: "013"
titulo: Login real y gestión de cuentas por Gerencia
estado: borrador
creado: 2026-10-13
actualizado: 2026-10-14
responsable: Alexander Martínez y Joseph Baño   # asignación de Joseph Baño, 13-oct
aprobado_por: PENDIENTE     # diseño acordado con Joseph Baño en la sesión del 13-oct (Auditor, H4 en P2 y revisor); PA-04 abierta
revisor: revisión cruzada (Alexander revisa los PR de Joseph y Joseph los de Alexander; nadie aprueba su propio PR)
depende_de: ["001"]
boveda:
  - 03 Arquitectura/Identidad OIDC y sesiones.md
  - 03 Arquitectura/Decisiones de arquitectura.md
  - 03 Arquitectura/Seguridad y evidencias.md
  - 03 Arquitectura/Runbook de DevOps y base de datos para la demo.md
  - 00 Inicio/Hechos canonicos.md
---

# 013 — Login real y gestión de cuentas por Gerencia

> **Nota de estado (14-oct-2026):** Implementado en `develop` mediante PR #56 (T1–T9) y PR #65 (T11–T13). Pendiente de validación E2E en entorno HTTPS con cuentas reales (E2E-01 a E2E-13, issue #55) y tareas de infraestructura (T10, issue #43). Spec no cerrado.

## Contexto

La identidad del sistema está decidida y vigente: **Amazon Cognito** (ADR-10, 7-oct) con **Authorization Code + PKCE S256** desde Angular (ADR-18, 11-oct), cuentas creadas por administración y sin registro público ([Identidad OIDC y sesiones](../../boveda/03%20Arquitectura/Identidad%20OIDC%20y%20sesiones.md)). El pool `istpetdev-dev` ya existe en Terraform con `allow_admin_create_user_only = true`, MFA TOTP obligatorio (`mfa_configuration = "ON"`), contraseña temporal de 1 día y recuperación por correo verificado. La API ya valida los tokens de Cognito y resuelve el rol fijo desde `Account` por `(issuer, subject)` (spec 001, FR-17); una cuenta inexistente o inactiva no recibe rol.

El frontend no usa nada de eso. La web (`apps/web/src/entities/session/model/session.store.ts`) acepta **cualquier correo con una contraseña de 4 o más caracteres**, inventa el token `jwt-mock-auth-token` y guarda la sesión en `localStorage`. La PWA arranca con `useMockSession = true` y perfiles demo. La configuración OIDC de ambas apps usa valores de ejemplo (`https://identity.istpetdev.local`, `clientId` y scopes que no existen en Cognito). `/dashboard` y `/warehouse` no tienen guard. Por eso ninguna app puede llamar a la API real, lo que bloquea la integración 002–004 (#33) y el panel 005.

No existe forma de dar de alta a una persona: no hay usuarios de demo documentados en Cognito ni una interfaz para crearlos. Este spec cierra ese hueco: login real en web y PWA y una pantalla de Gerencia para crear, cambiar de rol y desactivar cuentas, sobre los roles fijos de 001 más el rol Auditor de [ADR-26](../../boveda/03%20Arquitectura/Decisiones%20de%20arquitectura.md).

**Prioridad:** 013 se planifica después de 001–006, con una excepción: H1–H3 son el **paso previo** de la integración 002–004 (#33) y del panel 005, porque sin token real la PWA y el panel no pueden llamar a la API. H4 queda al final de la cola (P2).

## Objetivo y aporte a la nota

**Objetivo:** que cada rol de la demo entre con su propia cuenta real a la plataforma que le corresponde (ADR-24) y que la API reciba tokens válidos, y que Gerencia pueda dar de alta y retirar el acceso de una persona sin intervención técnica.

| Criterio | Peso | Cómo lo sostiene este spec |
|---|---:|---|
| Viabilidad técnica | 15% | Identidad administrada con MFA y revocación inmediata; ante una pregunta del jurado se muestra que el acceso no es simulado |
| Prototipo/demo | 20% | Habilita el recorrido real de 004 y 005 (sin login real la PWA y el panel no hablan con la API) |
| Presentación | 10% | Cada dispositivo de la demo muestra el nombre y rol reales; pantalla de Gerencia disponible para preguntas |

## Historias de usuario

### H1 — Entrar con mi cuenta a la plataforma que me corresponde (P1)

**Como** integrante de cualquier rol humano, **quiero** iniciar sesión con mi correo, contraseña y código MFA, **para** operar con mi identidad real en la web (Gerencia, Planificador, Auditor) o en la PWA (Bodega, Conductor, Receptor).

**Por qué esta prioridad:** sin ella ninguna llamada a la API funciona; bloquea 004 y 005.

**Escenarios de aceptación:**

1. **Dado** una cuenta activa de Planificador, **cuando** pulso «Iniciar sesión» en `/login` de la web y completo la página de Cognito con contraseña y código MFA, **entonces** vuelvo a la web autenticado, veo mi nombre y rol obtenidos de la API y las llamadas llevan `Authorization: Bearer`.
2. **Dado** una sesión iniciada, **cuando** recargo la página, **entonces** la app vuelve a Cognito y regresa autenticada sin pedirme la contraseña mientras la sesión de Cognito siga vigente.
3. **Dado** una cuenta de Conductor, **cuando** inicia sesión en la web, **entonces** ve «Tu cuenta opera en la app de campo» y no accede a ninguna ruta privada de la web; el caso inverso aplica a Gerencia, Planificador y Auditor en la PWA.
6. **Dado** una cuenta de Auditor, **cuando** entra a la web, **entonces** ve el panel, rutas, entregas, custodia y decisiones del agente sin ningún botón de acción, y cualquier escritura que intente por la API responde 403.
4. **Dado** un usuario autenticado en Cognito sin `Account` o con `Account` inactiva, **cuando** la app consulta su cuenta, **entonces** muestra «Tu cuenta no está habilitada; contacta a Gerencia» y no muestra datos.
5. **Dado** que olvidé mi contraseña, **cuando** uso «¿Olvidaste tu contraseña?» en la página de Cognito, **entonces** recibo un código en mi correo verificado y puedo definir una nueva.

### H2 — Cerrar sesión sin perder capturas pendientes (P1)

**Como** conductor, **quiero** cerrar sesión aunque tenga recepciones sin sincronizar, **para** entregar el teléfono sin perder lo capturado.

**Por qué esta prioridad:** la PWA offline de 004 conserva capturas en el outbox; un logout no puede borrarlas ni exponerlas a otra cuenta ([Identidad OIDC y sesiones](../../boveda/03%20Arquitectura/Identidad%20OIDC%20y%20sesiones.md), sección «Sesión web, móvil y offline»).

**Escenarios de aceptación:**

1. **Dado** dos capturas pendientes en el outbox, **cuando** pulso «Cerrar sesión», **entonces** la app me advierte, cierra la sesión en Cognito y en memoria, y las capturas siguen en el dispositivo asociadas a mi cuenta.
2. **Dado** que otra cuenta inicia sesión en ese teléfono, **cuando** abre la bandeja de sincronización, **entonces** no ve ni sincroniza las capturas de la cuenta anterior.

### H3 — Preparar las cuentas de la demo sin interfaz (P1)

**Como** equipo, **queremos** crear la primera cuenta de Gerencia y las cuentas de cada rol de la demo con un script, **para** tener sesiones reales iniciadas en los dispositivos antes del pitch aunque la pantalla de H4 no esté lista.

**Por qué esta prioridad:** es el plan B de H4 y el arranque del sistema: nadie puede usar la pantalla de Gerencia si todavía no existe una Gerencia.

**Escenarios de aceptación:**

1. **Dado** un pool sin usuarios, **cuando** se ejecuta el script con correo, nombre y rol de cada persona, **entonces** cada una recibe la invitación de Cognito y existe su `Account` activa con ese rol y `Subject` igual a su `sub` de Cognito.
2. **Dado** que el script se ejecuta dos veces con los mismos datos, **cuando** termina, **entonces** no duplica usuarios ni cuentas y reporta los que ya existían.

### H4 — Dar de alta, cambiar de rol y retirar el acceso de una persona (P2)

**Como** Gerencia, **quiero** gestionar las cuentas desde la web, **para** incorporar o retirar personal sin pedir ayuda técnica.

**Por qué esta prioridad:** el jurado no la ve en el recorrido principal (filtro 1 del objetivo norte); se implementó antes del congelamiento (PR #65). FR-18 y H4-6 siguen pospuestos hasta después del 17-oct.

**Escenarios de aceptación:**

1. **Dado** que soy Gerencia, **cuando** creo una cuenta con correo, nombre y rol Conductor, **entonces** la persona recibe la invitación de Cognito con contraseña temporal, aparece en la lista con estado «Invitación pendiente» y, tras su primer ingreso (cambio de contraseña y MFA), con estado «Activa».
2. **Dado** una cuenta de Conductor, **cuando** la desactivo, **entonces** su siguiente llamada a la API responde 403 aunque su token no haya vencido, su sesión de Cognito queda revocada y no puede volver a entrar hasta que la reactive.
3. **Dado** que soy la única Gerencia activa, **cuando** intento desactivarme o cambiar mi rol, **entonces** la operación se rechaza con un motivo claro.
4. **Dado** un correo que ya tiene cuenta, **cuando** intento crearla otra vez, **entonces** se rechaza sin crear nada nuevo. Si la pido con otro rol, el mensaje dice qué rol tiene ya: **un correo = un rol** (índice único `Issuer`+`Subject` en `Account`). Para cambiarlo se edita la cuenta; quien necesite dos roles en la demo usa dos correos (por ejemplo, alias `nombre+conductor@gmail.com`).
5. **Dado** una invitación vencida (más de 1 día), **cuando** pulso «Reenviar invitación», **entonces** la persona recibe una nueva contraseña temporal.
6. ~~**Dado** cualquier alta o cambio, **cuando** consulto la auditoría en la base, **entonces** existe un `AuditEvent` con quién, qué, cuándo, antes y después.~~ **Pospuesto hasta después del 17-oct** (decisión de Joseph Baño, 13-oct): la tabla `AuditEvent` no existe en el esquema de la demo (ver FR-18).

## Requisitos

### Funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| FR-01 | La web y la PWA DEBEN autenticarse con Cognito mediante Authorization Code + PKCE S256 usando `angular-auth-oidc-client`, con la página administrada de Cognito como pantalla de credenciales. Nuestra `/login` muestra la marca y un botón que la abre; ninguna pantalla propia recibe contraseñas. | ADR-10, ADR-18 |
| FR-02 | `authority`, `clientId`, `scope` (`openid email profile istpetdev-api/access`) y las URL de retorno y cierre DEBEN venir de la configuración pública por entorno (`environment.*.ts`), con los valores del output `cognito` de Terraform. Ningún secreto en el frontend. | Identidad OIDC y sesiones |
| FR-03 | La sesión OIDC se guarda según la decisión D-013-S1 (15-oct): en la web, en `sessionStorage` (sobrevive a F5 y muere al cerrar la pestaña); en la PWA, en `localStorage` (sobrevive a que el sistema cierre la app sin señal). Nunca en IndexedDB, el outbox, URLs ni logs. Al arrancar con el access token vencido se renueva con el refresh token antes de pedir login; un 401 con la sesión iniciada devuelve a Cognito sin soltar el outbox. | Identidad OIDC y sesiones; NFR-03 de 004 |
| FR-04 | Un interceptor DEBE añadir `Authorization: Bearer` únicamente a las llamadas dirigidas a la API configurada. | ADR-18 |
| FR-05 | `GET /api/v1/me` DEBE devolver `accountId`, `displayName`, `role` y `platform` (`web` o `field`) de la cuenta activa del token; sin `Account` activa responde 403. | 001 FR-17 |
| FR-06 | Las apps DEBEN decidir acceso y navegación con la respuesta de `GET /me`, no con claims del token ni con datos guardados localmente. | 001 FR-17 |
| FR-07 | La web DEBE admitir solo Gerencia, Planificador y Auditor; la PWA solo Bodega, Conductor y Receptor. Un rol en la plataforma equivocada ve un mensaje que indica su plataforma y no accede a rutas privadas. | ADR-24 |
| FR-08 | Toda ruta privada de la web y de la PWA DEBE tener guard; sin sesión redirige a `/login`. `/warehouse` se retira de la web (ADR-24). | ADR-24, RNF-01 |
| FR-09 | El logout DEBE cerrar la sesión de Cognito y la de memoria. En la PWA, si hay capturas pendientes, advierte y las conserva asociadas a la cuenta; otra cuenta no las ve ni las sincroniza. | Identidad OIDC y sesiones |
| FR-10 | DEBE eliminarse todo login simulado: `loginWithCredentials`, `useMockSession`, perfiles demo con contraseña, `jwt-mock-auth-token` y la sesión en `localStorage`. | RNF-14 |
| FR-11 | Un script de arranque DEBE crear usuarios en Cognito y sus `Account` (`Kind = human`, `Subject = sub`, rol, activa) a partir de una lista de correo, nombre y rol; es idempotente y no imprime contraseñas. | Identidad OIDC y sesiones (registro por administración) |
| FR-12 | `GET /api/v1/accounts` (Gerencia) DEBE listar cuentas humanas con nombre, rol y estado activo desde `Account`, y correo y estado de invitación desde Cognito. | — |
| FR-13 | `POST /api/v1/accounts` (Gerencia) DEBE crear el usuario en Cognito (que envía la invitación) y su `Account` con uno de los seis roles humanos (Gerencia, Planificador, Auditor, Bodega, Conductor, Receptor). Si la base falla después de crear el usuario, DEBE deshabilitarlo en Cognito. Reintentar con el mismo correo reconcilia sin duplicar. | ADR-21 (roles fijos) |
| FR-14 | `PATCH /api/v1/accounts/{id}` (Gerencia) DEBE permitir cambiar nombre y rol entre los seis humanos. `Automatizacion` NO DEBE asignarse ni retirarse por esta vía. | ADR-21, 003 FR-05 |
| FR-15 | `POST /api/v1/accounts/{id}/deactivate` y `/reactivate` (Gerencia) DEBEN cambiar `Account.IsActive` primero y luego deshabilitar/habilitar el usuario en Cognito y revocar sus sesiones (`AdminUserGlobalSignOut`). Si Cognito falla, la cuenta queda igualmente bloqueada en la API y la lista muestra el desfase para reintentar. | RNF-01 (revocación comprobada) |
| FR-16 | `POST /api/v1/accounts/{id}/resend-invitation` (Gerencia) DEBE reenviar la invitación solo a usuarios que no completaron su primer ingreso. | — |
| FR-17 | Una Gerencia NO DEBE poder desactivarse ni cambiar su propio rol, y la última Gerencia activa no puede perder ese rol ni desactivarse. | — |
| FR-18 | ~~Cada alta, cambio de rol o nombre, desactivación, reactivación y reenvío DEBE registrar un `AuditEvent` con actor, acción, recurso, resultado, antes y después.~~ **Pospuesto** (Joseph Baño, 13-oct): la migración `20261012223838_DemoCore` elimina la tabla `AuditEvent` y crearla exige una migración, que NFR-03 prohíbe. Mientras tanto, los cambios de cuentas solo dejan trazas en los logs estructurados de la API, sin correos. | Seguridad y evidencias |
| FR-19 | La web DEBE ofrecer a Gerencia una pantalla «Usuarios» con lista, alta, edición, desactivación, reactivación y reenvío, mostrando los errores de FR-13 a FR-17 con su motivo. | — |
| FR-20 | La página administrada de Cognito DEBE mostrar logo, colores y tipografía institucionales de ISTPET. | Sistema de diseno y colores |
| FR-21 | `Roles.cs` DEBE incluir `Auditor` como rol humano. Los endpoints de lectura que hoy admiten Gerencia o Planificador lo admiten también; ningún endpoint de escritura lo admite (403). La web oculta al Auditor toda acción de escritura. | ADR-26 |

### No funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| NFR-01 | Tras desactivar una cuenta, su siguiente petición a la API responde 403 aunque el access token siga vigente. | RNF-01 |
| NFR-02 | La API accede a Cognito con credenciales del entorno (perfil de instancia en producción, credenciales IAM personales en local); ninguna clave de AWS en configuración ni repositorio. El permiso se limita a `AdminCreateUser`, `AdminDisableUser`, `AdminEnableUser`, `AdminUserGlobalSignOut`, `AdminGetUser` y `ListUsers` sobre el ARN del pool. | Credenciales y acceso del equipo |
| NFR-03 | Este spec NO DEBE añadir migraciones: correo y estado de invitación se leen de Cognito; rol y estado activo, de `Account`. | Runbook §3 (congelamiento de migraciones) |
| NFR-04 | Una prueba de CI DEBE fallar si reaparecen `loginWithCredentials`, `useMockSession` o `jwt-mock` en `apps/`. | RNF-14 |
| NFR-05 | Si Cognito no está disponible, los endpoints de gestión responden 503 sin dejar cambios parciales en la base, y las sesiones ya válidas siguen operando. | Identidad OIDC y sesiones |

## Entidades y contratos

**Entidades:** sin cambios de esquema. Usa `Account` (`Kind`, `Issuer`, `Subject`, `DisplayName`, `Role`, `IsActive`) del núcleo 001. `AuditEvent` **no** existe en el esquema de la demo (la elimina `DemoCore`), por eso FR-18 está pospuesto. Roles válidos para cuentas humanas: `Gerencia`, `Planificador`, `Auditor` (ADR-26), `Bodega`, `Conductor`, `Receptor` (`src/Domain/Common/Roles.cs`). `Auditor` no requiere migración: `Account.Role` es texto sin restricción en la base.

**Endpoints nuevos:**

| Método y ruta | Rol | Función |
|---|---|---|
| `GET /api/v1/me` | Cualquier cuenta activa | Cuenta, nombre, rol y plataforma |
| `GET /api/v1/accounts` | Gerencia | Lista de cuentas humanas con estado de Cognito |
| `POST /api/v1/accounts` | Gerencia | Alta e invitación |
| `PATCH /api/v1/accounts/{id}` | Gerencia | Nombre y rol |
| `POST /api/v1/accounts/{id}/deactivate` | Gerencia | Retira el acceso |
| `POST /api/v1/accounts/{id}/reactivate` | Gerencia | Restituye el acceso |
| `POST /api/v1/accounts/{id}/resend-invitation` | Gerencia | Nueva contraseña temporal |

Errores en Problem Details: 409 correo existente o regla de última Gerencia/propia cuenta; 422 rol inválido o `Automatizacion`; 503 Cognito no disponible; 403 sin cuenta activa.

**Consumidores:** 004 (PWA: login, guards, interceptor, logout con outbox) y 005 (panel: login, guards por rol). Ambos dejan de implementar su propio login. La dependencia es parcial: solo consumen H1–H3.

**Dependencias de infraestructura (Jorge; Terraform solo con `-target` y plan revisado, ver #14):** URL HTTPS de retorno y cierre en los clientes web y móvil (#14, prioridad Alta); personalización de la página administrada de Cognito; perfil de instancia del host de la app con el permiso de NFR-02 y `HttpPutResponseHopLimit = 2` en IMDSv2 para que el contenedor obtenga credenciales.

## Guardarraíles aplicados

| Guardarraíl | Aplicación |
|---|---|
| G1 | Ningún correo, nombre ni rol de persona en el código: las cuentas de demo vienen del archivo de entrada del script, fuera del repositorio. |
| G3 | Una cuenta sin `Account` o con desfase con Cognito se muestra como tal; nunca se asume activa. |
| G7 | Los roles son los genéricos de 001; nada del relato del hospital en la gestión de cuentas. |
| G8 | Las cuentas creadas antes del 16-oct se registran en [Registro de trabajo previo y del evento](../../boveda/06%20Equipo/Registro%20de%20trabajo%20previo%20y%20del%20evento.md) sin correos personales. |

## Fuera de alcance

- Registro público o autoregistro (el pool lo impide y la bóveda lo prohíbe).
- Formularios propios de contraseña, MFA o recuperación: los resuelve la página de Cognito.
- Crear roles, permisos editables o ámbitos por organización (RF-17, P2 por ADR-21).
- Borrar cuentas: solo se desactivan, para conservar la trazabilidad de custodia.
- Cuentas para el jurado: el jurado usa la traza pública por QR (004).
- Gestión de la cuenta técnica de n8n (`Automatizacion`): la crea #38/003.
- Cambiar la configuración de MFA del pool.
- Login con proveedores externos (Google, Microsoft).
- Auditoría de cambios de cuentas en `AuditEvent` (FR-18, H4-6): pospuesta hasta después del 17-oct porque exige una migración.

## Contradicciones con la bóveda

> **✅ Resuelta (13-oct)** — Login visible frente a D3. D3 de [docs/specs/README.md](../README.md#fuera-de-alcance-hasta-el-17-oct-d3) cortó «Login de Cognito visible en la demo» por carga del equipo, no por una razón técnica. Joseph Baño acordó: H1–H3 obligatorias como requisito de las sesiones ya iniciadas y de 004/005; mostrar el login en el pitch solo si 013 H1–H3 cierra antes del congelamiento; H4 en P2. La fila D3 del README se actualizó en el mismo cambio.

> **✅ Resuelta (13-oct)** — Rol Auditor. ADR-24 y el frontend lo nombraban y la API no lo tenía. [ADR-26](../../boveda/03%20Arquitectura/Decisiones%20de%20arquitectura.md) (aceptada por Joseph Baño) lo agrega como séptimo rol fijo, humano y de solo lectura total en la web, sin migración.

> **✅ Resuelta (13-oct)** — Dueño del login. [spec 004](../004-campo-qr-custodia-offline/spec.md) FR-17 (y su T1b) y [spec 005](../005-panel-simulador/spec.md) FR-05 asignaban el login a 004. Ambos FR se actualizaron para depender de 013, y los dos specs listan 013 en `depende_de` como **dependencia parcial** (H1–H3, solo su frontend conectado a la API): la API de campo, el simulador, los KPI y la exportación no esperan a 013, y H4 no bloquea a nadie. Queda por ajustar `task.md` de 004 (T1b) en su próxima revisión.

> **✅ Resuelta (14-oct)** — Tokens solo en memoria frente al [informe de entrega de 004](../004-campo-qr-custodia-offline/informe-entrega-spec-004-y-decisiones.md) (decisión 2), que admitía `sessionStorage`. Se siguió la bóveda ([Identidad OIDC y sesiones](../../boveda/03%20Arquitectura/Identidad%20OIDC%20y%20sesiones.md)): la decisión 2 del informe ahora dice «solo en memoria» con referencia a FR-03.

> **✅ Resuelta (15-oct, D-013-S1)** — «Tokens solo en memoria» rompía la recarga (F5) y el reinicio de la PWA, y la sesión moría a los 15 minutos aunque el refresh token siguiera vigente. Decisión de Joseph Baño: web en `sessionStorage`, PWA en `localStorage` con `useRefreshTokenLock`, renovación con refresh token al arrancar y vuelta a Cognito ante un 401. Se descartaron las cookies `HttpOnly`: exigen un backend-for-frontend que haga el canje de tokens y no funcionan sin red en la PWA. Riesgo aceptado: un XSS podría leer el refresh token; se mitiga con su rotación en cada uso, su vida de 7 días y la revocación al desactivar la cuenta. Pendiente después del evento: una CSP en nginx.

> **✅ Resuelta (14-oct)** — ADR-25 frente a ADR-24 (roles en PWA). Se aplicó la opción (a): la PWA solo admite Bodega, Conductor y Receptor, alineada con ADR-24 y ADR-26. El perfil `LogisticsManager` se retiró en T7 (PR #56). Gerencia y Planificador operan exclusivamente en la plataforma web.

Notas revisadas sin otra contradicción: Hechos canonicos (identidad Cognito ✅), RBAC y configuracion del sistema (roles fijos dentro del diseño diferido), Seguridad y evidencias, Runbook de DevOps y base de datos (sin migraciones nuevas), Credenciales y acceso del equipo.

## Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Alexander no llega con H1–H3 a H3 (14-oct 20:00) por su carga en 004 y 005 | Alta | Alto | H1–H3 antes del #33 y H4 al final; revisor fijo: Joseph Baño; H3 como plan B |
| Las URL HTTPS de retorno no están en Cognito a tiempo | Media | Alto | Prioridad Alta en #14; probar en local con `localhost` mientras tanto |
| La contraseña temporal vence en 1 día y la invitación caduca antes del evento | Alta | Medio | Crear o reenviar las cuentas de demo el 15-oct; reenviar desde H4 o el script |
| El contenedor no obtiene credenciales del perfil de instancia (hop limit 1) | Media | Medio | `HttpPutResponseHopLimit = 2`; probar `GET /accounts` en producción antes de H4 |
| Cognito envía como máximo unos 50 correos al día con su configuración por defecto | Baja | Medio | Evitar reenvíos masivos; crear las cuentas de demo en un solo lote |
| El MFA obligatorio frena al conductor en la demo | Media | Medio | MFA configurado antes del evento en los teléfonos de la demo; sesiones ya iniciadas (D3) |
| Reinicio de la PWA sin señal con el access token vencido (más de 15 min): el refresh token no se puede usar sin red y la app pide login. Con D-013-S1 la sesión ya no se pierde al cerrar la app; solo falta red para renovarla. Las capturas ya guardadas no se pierden (CE-04). | Baja | Medio | Abrir la PWA con red antes de la escena offline; en la demo no se reinicia en modo avión. Decide: Joseph Baño |
| Se presenta H4 como funcionando sin estarlo | Baja | Alto | Regla del 006: solo se muestra lo probado en `test-e2e.md` |

## Criterios de éxito

- CE-01: en la web y la PWA desplegadas, cada uno de los seis roles humanos entra con su cuenta real y MFA, y `GET /me` devuelve su rol; ningún rol entra a la plataforma que no le corresponde.
- CE-02: `grep` en `apps/` no encuentra `loginWithCredentials`, `useMockSession` ni `jwt-mock`, y ninguna prueba encuentra tokens en `localStorage` ni IndexedDB.
- CE-03: el script de H3 crea la Gerencia inicial y las cuentas de demo, y una segunda ejecución no duplica nada.
- CE-04: un logout con capturas pendientes las conserva y otra cuenta no las ve.
- CE-05 (H4): una cuenta desactivada recibe 403 en su siguiente llamada, sin esperar a que venza su token.
- CE-06 (H4): Gerencia crea un Conductor desde la web y esa persona completa su primer ingreso y ve solo sus entregas en la PWA.
- CE-07: con un token de Auditor, todos los endpoints de lectura de la web responden 200 y todos los de escritura responden 403.

## Estado de implementación

- **PR #56** (T1–T9): implementación de roles (`Auditor`), `GET /api/v1/me`, script de aprovisionamiento idempotente (`--provision-accounts`), lógica de sesión OIDC en `shared-core`, integración real en web y PWA (con retiro de mocks y guards) y guardián de CI.
- **PR #65** (T11–T13): API y web de gestión de cuentas por Gerencia (H4) — listado, alta, edición, desactivación/reactivación y reenvío de invitación, junto con la pantalla «Usuarios».
- **Desviaciones relevantes:** documentadas en las secciones «Desviaciones» de las Tasks 6–9 y 11–13 de [task.md](task.md).
- **Pendiente para cierre completo:**
  - T10: configuración de infraestructura en AWS/Cognito (callbacks HTTPS, marca, IAM y hop limit, issue [#43](https://github.com/bryancito1090/istpetdev-platform/issues/43)).
  - PWA en `/campo/`: despliegue y validación de la app móvil en subdirectorio (issue [#52](https://github.com/bryancito1090/istpetdev-platform/issues/52)).
  - E2E-01 a E2E-13: ejecución de pruebas de punta a punta en entorno HTTPS con cuentas reales (issue [#55](https://github.com/bryancito1090/istpetdev-platform/issues/55)).

## Preguntas abiertas

Ninguna abierta.

Resueltas:
- PA-01 (13-oct, Joseph Baño): rol Auditor → se agrega, ADR-26.
- PA-02 (13-oct, Joseph Baño): H4 en P2 → aceptado.
- PA-03 (13-oct, Joseph Baño): revisor → Joseph Baño.
- PA-04 (14-oct, en #55): Gerencia inicial: Bryan; el CSV de cuentas lo guarda Jorge fuera del repositorio (G1).

