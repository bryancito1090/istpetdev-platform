---
id: "013"
titulo: Login real y gestión de cuentas por Gerencia
estado: borrador
creado: 2026-10-13
actualizado: 2026-10-13
responsable: Alexander Martínez
aprobado_por: PENDIENTE     # diseño acordado con Andres en sesión de brainstorming del 13-oct; falta confirmación del equipo (ver Preguntas abiertas)
depende_de: ["001"]
boveda:
  - 03 Arquitectura/Identidad OIDC y sesiones.md
  - 03 Arquitectura/Decisiones de arquitectura.md
  - 03 Arquitectura/Seguridad y evidencias.md
  - 03 Arquitectura/Runbook de DevOps y base de datos para la demo.md
  - 00 Inicio/Hechos canonicos.md
---

# 013 — Login real y gestión de cuentas por Gerencia

## Contexto

La identidad del sistema está decidida y vigente: **Amazon Cognito** (ADR-10, 7-oct) con **Authorization Code + PKCE S256** desde Angular (ADR-18, 11-oct), cuentas creadas por administración y sin registro público ([Identidad OIDC y sesiones](../../boveda/03%20Arquitectura/Identidad%20OIDC%20y%20sesiones.md)). El pool `istpetdev-dev` ya existe en Terraform con `allow_admin_create_user_only = true`, MFA TOTP obligatorio (`mfa_configuration = "ON"`), contraseña temporal de 1 día y recuperación por correo verificado. La API ya valida los tokens de Cognito y resuelve el rol fijo desde `Account` por `(issuer, subject)` (spec 001, FR-17); una cuenta inexistente o inactiva no recibe rol.

El frontend no usa nada de eso. La web (`apps/web/src/entities/session/model/session.store.ts`) acepta **cualquier correo con una contraseña de 4 o más caracteres**, inventa el token `jwt-mock-auth-token` y guarda la sesión en `localStorage`. La PWA arranca con `useMockSession = true` y perfiles demo. La configuración OIDC de ambas apps usa valores de ejemplo (`https://identity.istpetdev.local`, `clientId` y scopes que no existen en Cognito). `/dashboard` y `/warehouse` no tienen guard. Por eso ninguna app puede llamar a la API real, lo que bloquea la integración 002–004 (#33) y el panel 005.

No existe forma de dar de alta a una persona: no hay usuarios de demo documentados en Cognito ni una interfaz para crearlos. Este spec cierra ese hueco: login real en web y PWA y una pantalla de Gerencia para crear, cambiar de rol y desactivar cuentas, sobre los seis roles fijos de 001.

## Objetivo y aporte a la nota

**Objetivo:** que cada rol de la demo entre con su propia cuenta real a la plataforma que le corresponde (ADR-24) y que la API reciba tokens válidos, y que Gerencia pueda dar de alta y retirar el acceso de una persona sin intervención técnica.

| Criterio | Peso | Cómo lo sostiene este spec |
|---|---:|---|
| Viabilidad técnica | 15% | Identidad administrada con MFA, revocación inmediata y auditoría; ante una pregunta del jurado se muestra que el acceso no es simulado |
| Prototipo/demo | 20% | Habilita el recorrido real de 004 y 005 (sin login real la PWA y el panel no hablan con la API) |
| Presentación | 10% | Cada dispositivo de la demo muestra el nombre y rol reales; pantalla de Gerencia disponible para preguntas |

## Historias de usuario

### H1 — Entrar con mi cuenta a la plataforma que me corresponde (P1)

**Como** integrante de cualquier rol humano, **quiero** iniciar sesión con mi correo, contraseña y código MFA, **para** operar con mi identidad real en la web (Gerencia, Planificador) o en la PWA (Bodega, Conductor, Receptor).

**Por qué esta prioridad:** sin ella ninguna llamada a la API funciona; bloquea 004 y 005.

**Escenarios de aceptación:**

1. **Dado** una cuenta activa de Planificador, **cuando** pulso «Iniciar sesión» en `/login` de la web y completo la página de Cognito con contraseña y código MFA, **entonces** vuelvo a la web autenticado, veo mi nombre y rol obtenidos de la API y las llamadas llevan `Authorization: Bearer`.
2. **Dado** una sesión iniciada, **cuando** recargo la página, **entonces** la app vuelve a Cognito y regresa autenticada sin pedirme la contraseña mientras la sesión de Cognito siga vigente.
3. **Dado** una cuenta de Conductor, **cuando** inicia sesión en la web, **entonces** ve «Tu cuenta opera en la app de campo» y no accede a ninguna ruta privada de la web; el caso inverso aplica a Gerencia y Planificador en la PWA.
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

**Por qué esta prioridad:** el jurado no la ve en el recorrido principal (filtro 1 del objetivo norte); se construye después de H1–H3 y, si no está lista en H4 del plan, se presenta como propuesta (ver Contradicciones).

**Escenarios de aceptación:**

1. **Dado** que soy Gerencia, **cuando** creo una cuenta con correo, nombre y rol Conductor, **entonces** la persona recibe la invitación de Cognito con contraseña temporal, aparece en la lista con estado «Invitación pendiente» y, tras su primer ingreso (cambio de contraseña y MFA), con estado «Activa».
2. **Dado** una cuenta de Conductor, **cuando** la desactivo, **entonces** su siguiente llamada a la API responde 403 aunque su token no haya vencido, su sesión de Cognito queda revocada y no puede volver a entrar hasta que la reactive.
3. **Dado** que soy la única Gerencia activa, **cuando** intento desactivarme o cambiar mi rol, **entonces** la operación se rechaza con un motivo claro.
4. **Dado** un correo que ya tiene cuenta, **cuando** intento crearla otra vez, **entonces** se rechaza sin crear nada nuevo.
5. **Dado** una invitación vencida (más de 1 día), **cuando** pulso «Reenviar invitación», **entonces** la persona recibe una nueva contraseña temporal.
6. **Dado** cualquier alta o cambio, **cuando** consulto la auditoría en la base, **entonces** existe un `AuditEvent` con quién, qué, cuándo, antes y después.

## Requisitos

### Funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| FR-01 | La web y la PWA DEBEN autenticarse con Cognito mediante Authorization Code + PKCE S256 usando `angular-auth-oidc-client`, con la página administrada de Cognito como pantalla de credenciales. Nuestra `/login` muestra la marca y un botón que la abre; ninguna pantalla propia recibe contraseñas. | ADR-10, ADR-18 |
| FR-02 | `authority`, `clientId`, `scope` (`openid email profile istpetdev-api/access`) y las URL de retorno y cierre DEBEN venir de la configuración pública por entorno (`environment.*.ts`), con los valores del output `cognito` de Terraform. Ningún secreto en el frontend. | Identidad OIDC y sesiones |
| FR-03 | Los tokens DEBEN vivir solo en memoria: nunca en `localStorage`, IndexedDB, el outbox, URLs ni logs. Al recargar, la app reautentica contra Cognito. | Identidad OIDC y sesiones; NFR-03 de 004 |
| FR-04 | Un interceptor DEBE añadir `Authorization: Bearer` únicamente a las llamadas dirigidas a la API configurada. | ADR-18 |
| FR-05 | `GET /api/v1/me` DEBE devolver `accountId`, `displayName`, `role` y `platform` (`web` o `field`) de la cuenta activa del token; sin `Account` activa responde 403. | 001 FR-17 |
| FR-06 | Las apps DEBEN decidir acceso y navegación con la respuesta de `GET /me`, no con claims del token ni con datos guardados localmente. | 001 FR-17 |
| FR-07 | La web DEBE admitir solo Gerencia y Planificador; la PWA solo Bodega, Conductor y Receptor. Un rol en la plataforma equivocada ve un mensaje que indica su plataforma y no accede a rutas privadas. | ADR-24 |
| FR-08 | Toda ruta privada de la web y de la PWA DEBE tener guard; sin sesión redirige a `/login`. `/warehouse` se retira de la web (ADR-24). | ADR-24, RNF-01 |
| FR-09 | El logout DEBE cerrar la sesión de Cognito y la de memoria. En la PWA, si hay capturas pendientes, advierte y las conserva asociadas a la cuenta; otra cuenta no las ve ni las sincroniza. | Identidad OIDC y sesiones |
| FR-10 | DEBE eliminarse todo login simulado: `loginWithCredentials`, `useMockSession`, perfiles demo con contraseña, `jwt-mock-auth-token` y la sesión en `localStorage`. | RNF-14 |
| FR-11 | Un script de arranque DEBE crear usuarios en Cognito y sus `Account` (`Kind = human`, `Subject = sub`, rol, activa) a partir de una lista de correo, nombre y rol; es idempotente y no imprime contraseñas. | Identidad OIDC y sesiones (registro por administración) |
| FR-12 | `GET /api/v1/accounts` (Gerencia) DEBE listar cuentas humanas con nombre, rol y estado activo desde `Account`, y correo y estado de invitación desde Cognito. | — |
| FR-13 | `POST /api/v1/accounts` (Gerencia) DEBE crear el usuario en Cognito (que envía la invitación) y su `Account` con uno de los cinco roles humanos. Si la base falla después de crear el usuario, DEBE deshabilitarlo en Cognito. Reintentar con el mismo correo reconcilia sin duplicar. | ADR-21 (roles fijos) |
| FR-14 | `PATCH /api/v1/accounts/{id}` (Gerencia) DEBE permitir cambiar nombre y rol entre los cinco humanos. `Automatizacion` NO DEBE asignarse ni retirarse por esta vía. | ADR-21, 003 FR-05 |
| FR-15 | `POST /api/v1/accounts/{id}/deactivate` y `/reactivate` (Gerencia) DEBEN cambiar `Account.IsActive` primero y luego deshabilitar/habilitar el usuario en Cognito y revocar sus sesiones (`AdminUserGlobalSignOut`). Si Cognito falla, la cuenta queda igualmente bloqueada en la API y la lista muestra el desfase para reintentar. | RNF-01 (revocación comprobada) |
| FR-16 | `POST /api/v1/accounts/{id}/resend-invitation` (Gerencia) DEBE reenviar la invitación solo a usuarios que no completaron su primer ingreso. | — |
| FR-17 | Una Gerencia NO DEBE poder desactivarse ni cambiar su propio rol, y la última Gerencia activa no puede perder ese rol ni desactivarse. | — |
| FR-18 | Cada alta, cambio de rol o nombre, desactivación, reactivación y reenvío DEBE registrar un `AuditEvent` con actor, acción, recurso, resultado, antes y después. | Seguridad y evidencias |
| FR-19 | La web DEBE ofrecer a Gerencia una pantalla «Usuarios» con lista, alta, edición, desactivación, reactivación y reenvío, mostrando los errores de FR-13 a FR-17 con su motivo. | — |
| FR-20 | La página administrada de Cognito DEBE mostrar logo, colores y tipografía institucionales de ISTPET. | Sistema de diseno y colores |

### No funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| NFR-01 | Tras desactivar una cuenta, su siguiente petición a la API responde 403 aunque el access token siga vigente. | RNF-01 |
| NFR-02 | La API accede a Cognito con credenciales del entorno (perfil de instancia en producción, credenciales IAM personales en local); ninguna clave de AWS en configuración ni repositorio. El permiso se limita a `AdminCreateUser`, `AdminDisableUser`, `AdminEnableUser`, `AdminUserGlobalSignOut`, `AdminGetUser` y `ListUsers` sobre el ARN del pool. | Credenciales y acceso del equipo |
| NFR-03 | Este spec NO DEBE añadir migraciones: correo y estado de invitación se leen de Cognito; rol y estado activo, de `Account`. | Runbook §3 (congelamiento de migraciones) |
| NFR-04 | Una prueba de CI DEBE fallar si reaparecen `loginWithCredentials`, `useMockSession` o `jwt-mock` en `apps/`. | RNF-14 |
| NFR-05 | Si Cognito no está disponible, los endpoints de gestión responden 503 sin dejar cambios parciales en la base, y las sesiones ya válidas siguen operando. | Identidad OIDC y sesiones |

## Entidades y contratos

**Entidades:** sin cambios de esquema. Usa `Account` (`Kind`, `Issuer`, `Subject`, `DisplayName`, `Role`, `IsActive`) y `AuditEvent` del núcleo 001. Roles válidos para cuentas humanas: `Gerencia`, `Planificador`, `Bodega`, `Conductor`, `Receptor` (`src/Domain/Common/Roles.cs`).

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

**Consumidores:** 004 (PWA: login, guards, interceptor, logout con outbox) y 005 (panel: login, guards por rol). Ambos dejan de implementar su propio login.

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
- Rol «Auditor» (ver Contradicciones y PA-01).
- Borrar cuentas: solo se desactivan, para conservar la trazabilidad de custodia.
- Cuentas para el jurado: el jurado usa la traza pública por QR (004).
- Gestión de la cuenta técnica de n8n (`Automatizacion`): la crea #38/003.
- Cambiar la configuración de MFA del pool.
- Login con proveedores externos (Google, Microsoft).

## Contradicciones con la bóveda

> **⚠️ Contradicción detectada** — Hecho nuevo: login real en web y PWA para la demo y pantalla de gestión de cuentas. · Hecho establecido: D3 en [docs/specs/README.md](../README.md#fuera-de-alcance-hasta-el-17-oct-d3) corta «Login de Cognito visible en la demo» y lo reemplaza por «Sesiones ya iniciadas en los dispositivos de la demo» (12-oct). · Impacto: las sesiones ya iniciadas exigen un login real (H1–H3), así que esa parte no contradice D3; la pantalla de Gerencia (H4) no la ve el jurado en el recorrido principal. · Opciones: (a) H1–H3 obligatorias y H4 como P2 después de ellas, sin mostrar el login en el pitch; (b) H4 obligatoria; (c) solo H3. · Propuesta: (a). Registrar en D3 que el login real es prerequisito de las sesiones iniciadas y que H4 se muestra solo si está terminada antes del congelamiento.

> **⚠️ Contradicción detectada** — Hecho nuevo: la web admite Gerencia y Planificador; no existe rol Auditor. · Hecho establecido: [ADR-24](../../boveda/03%20Arquitectura/Decisiones%20de%20arquitectura.md) asigna la web a «Planificador, Gerencia y Auditor», y el frontend define un perfil `Auditor`; la API tiene seis roles fijos sin Auditor (`Roles.cs`, spec 001). · Impacto: agregar Auditor cambia los roles fijos de 001 y su autorización. · Opciones: (a) no agregarlo y retirar `Auditor` del frontend; (b) agregarlo como séptimo rol de solo lectura en 001. · Propuesta: (a) para la demo; decide el equipo (PA-01).

> **⚠️ Contradicción detectada** — Hecho nuevo: el login, los guards y el interceptor de web y PWA pertenecen a 013. · Hecho establecido: [spec 004](../004-campo-qr-custodia-offline/spec.md) FR-17 y su tarea T1b los asignan a 004, y [spec 005](../005-panel-simulador/spec.md) FR-05 reutiliza «el login PKCE y el interceptor del spec 004». · Impacto: dos specs implementarían el mismo login. · Opciones: (a) 013 es dueño; 004 FR-17/T1b y 005 FR-05 pasan a depender de 013; (b) dejarlo en 004. · Propuesta: (a), y actualizar las referencias en 004 y 005.

> **⚠️ Contradicción detectada** — Hecho nuevo: tokens solo en memoria. · Hecho establecido: el [informe de entrega de 004](../004-campo-qr-custodia-offline/informe-entrega-spec-004-y-decisiones.md) (decisión 2) admite «memoria volátil o `sessionStorage`»; la nota [Identidad OIDC y sesiones](../../boveda/03%20Arquitectura/Identidad%20OIDC%20y%20sesiones.md) exige memoria. · Impacto: `sessionStorage` es accesible a cualquier script de la página. · Propuesta: seguir la bóveda (memoria) y corregir el informe.

Notas revisadas sin otra contradicción: Hechos canonicos (identidad Cognito ✅), RBAC y configuracion del sistema (roles fijos dentro del diseño diferido), Seguridad y evidencias, Runbook de DevOps y base de datos (sin migraciones nuevas), Credenciales y acceso del equipo.

## Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Alexander no llega con H1–H3 a H3 (14-oct 20:00) por su carga en 004 y 005 | Alta | Alto | H1–H3 primero y H4 después; revisor fijo (PA-03); H3 como plan B |
| Las URL HTTPS de retorno no están en Cognito a tiempo | Media | Alto | Prioridad Alta en #14; probar en local con `localhost` mientras tanto |
| La contraseña temporal vence en 1 día y la invitación caduca antes del evento | Alta | Medio | Crear o reenviar las cuentas de demo el 15-oct; reenviar desde H4 o el script |
| El contenedor no obtiene credenciales del perfil de instancia (hop limit 1) | Media | Medio | `HttpPutResponseHopLimit = 2`; probar `GET /accounts` en producción antes de H4 |
| Cognito envía como máximo unos 50 correos al día con su configuración por defecto | Baja | Medio | Evitar reenvíos masivos; crear las cuentas de demo en un solo lote |
| El MFA obligatorio frena al conductor en la demo | Media | Medio | MFA configurado antes del evento en los teléfonos de la demo; sesiones ya iniciadas (D3) |
| Se presenta H4 como funcionando sin estarlo | Baja | Alto | Regla del 006: solo se muestra lo probado en `test-e2e.md` |

## Criterios de éxito

- CE-01: en la web y la PWA desplegadas, cada uno de los cinco roles humanos entra con su cuenta real y MFA, y `GET /me` devuelve su rol; ningún rol entra a la plataforma que no le corresponde.
- CE-02: `grep` en `apps/` no encuentra `loginWithCredentials`, `useMockSession` ni `jwt-mock`, y ninguna prueba encuentra tokens en `localStorage` ni IndexedDB.
- CE-03: el script de H3 crea la Gerencia inicial y las cuentas de demo, y una segunda ejecución no duplica nada.
- CE-04: un logout con capturas pendientes las conserva y otra cuenta no las ve.
- CE-05 (H4): una cuenta desactivada recibe 403 en su siguiente llamada, sin esperar a que venza su token.
- CE-06 (H4): Gerencia crea un Conductor desde la web y esa persona completa su primer ingreso y ve solo sus entregas en la PWA.

## Preguntas abiertas

- PA-01: ¿Se agrega el rol Auditor o se retira del frontend y de ADR-24? Decide: equipo (Bryan como Tech Lead).
- PA-02: ¿Se acepta la opción (a) de la contradicción con D3 (H4 como P2)? Decide: Andres y equipo.
- PA-03: ¿Quién revisa los PR de Alexander en este spec? Propuesta: Bryan (aceptó ADR-18). Decide: equipo.
- PA-04: ¿Quién será la Gerencia inicial y quién guarda el archivo de entrada del script (fuera del repositorio)? Decide: equipo.
