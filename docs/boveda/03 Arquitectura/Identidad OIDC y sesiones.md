---
tipo: arquitectura
estado: aceptado
actualizado: 2026-10-14
tags: [arquitectura, identidad, oidc, rbac, seguridad]
---

# Identidad OIDC y sesiones

## ADR-10 — decisión delegada por Bryan, 7 de octubre de 2026

Elegir **Amazon Cognito User Pools** como proveedor OIDC administrado. Evita agregar un servidor de identidad al host EC2 y se integra con AWS. Se conserva íntegramente el RBAC por acción/recurso de ADR-14: Cognito prueba identidad; PostgreSQL mantiene organización, roles, asignaciones y revocaciones operativas.

Esta es la configuración a implementar; no hay todavía pools, clientes ni sesiones provisionados. El dominio institucional continúa pendiente del compañero responsable; Cognito puede usar su dominio administrado para desarrollo.

## Configuración por entorno

| Parámetro | Desarrollo | Demo / producción |
|---|---|---|
| User pool | Un pool `istpetdev-dev` para el equipo | Pool `istpetdev-demo`; pool `istpetdev-prod` separado cuando exista producción |
| Registro | Cuentas creadas por administración; sin autosignup | Mismo criterio; roles administrables por organización según ADR-16, con la matriz funcional inicial como seed |
| Clientes | Web y PWA separados, públicos, **sin client secret** | Web y móvil separados; cliente confidencial distinto para automatización |
| Flujo humano | Authorization Code + PKCE **S256**, `state` y `nonce` | Mismo flujo; nunca implicit ni contraseña enviada por nuestra API |
| Access token | **15 minutos** | 15 minutos |
| Refresh token | **7 días**, rotación y revocación habilitadas | 7 días; gracia de rotación 10 segundos |
| Scopes | `openid email profile` y scope API propio | Scope API propio; scopes mínimos por cliente |
| MFA | Obligatorio para administración de accesos/AWS; desactivado en el User Pool de Cognito durante la hackathon (15-oct-2026) para agilizar las cuentas multi-rol | MFA humano obligatorio en producción; probar experiencia del conductor |
| Callbacks locales | `http://localhost:4200/auth/callback`, `http://localhost:8100/auth/callback` | HTTPS exacto bajo dominio asignado; no wildcards |
| Logout local | `http://localhost:4200`, `http://localhost:8100` | URLs exactas aprobadas por entorno |

Fijar scopes/issuer/client IDs en configuración pública sin secrets. Usar una librería OIDC mantenida compatible con Angular 22 al scaffold y validar sus peers antes de elegir versión; no escribir una implementación OAuth propia. Si se empaqueta nativo, usar navegador del sistema y almacenamiento seguro del sistema operativo, no login embebido en un WebView.

## Validación en API .NET 8

1. Validar firma RS256 y claves JWKS del issuer configurado, expiración, issuer exacto y `token_use=access`. Un ID token no autoriza la API.
2. Validar `client_id` contra la lista permitida del entorno y scopes requeridos por endpoint. Separar scope de API humana de scopes M2M; no exigir el scope humano a n8n. Cognito no garantiza `aud` en todos los access tokens: no configurar una validación de audience que acepte cualquier valor o rechace todos los tokens. Si se activa resource binding, validar además su `aud` explícito.
3. Resolver identidad humana con `(issuer, sub)`, no email mutable, y comprobar cuenta activa, organización, permisos y recurso en PostgreSQL. Resolver el actor técnico de n8n con `(issuer, client_id)` registrado por administración, no buscarlo como un usuario humano.
4. Reusar políticas en HTTP, evidencias y SignalR. Un claim de grupo no elimina controles de organización/entrega.
5. Cachear JWKS con rotación; disponer de claves previamente cargadas para cortes breves. Un issuer desconocido o una clave no validable se rechazan.

Limitar confianza de `X-Forwarded-*` al proxy conocido. El servicio confía en Nginx/ALB configurado, no en cualquier cliente. CORS solo permite los orígenes de web/móvil enumerados por entorno; no usar wildcard con credenciales.

## Sesión web, móvil y offline

- Web/PWA (D-013-S1, 15-oct): sesión OIDC en `sessionStorage` en la web y en `localStorage` en la PWA; nunca en URLs, logs, IndexedDB ni la outbox. Al reabrir con el access token vencido se renueva con el refresh token; si eso falla, reautenticación OIDC. La cola de capturas se conserva.
- Una persona = un correo = un rol (`Account` tiene índice único `Issuer`+`Subject`). Para dos roles se usan dos correos.
- PWA offline: almacenar asignaciones/capturas/archivos; no exigir token vigente para conservar una captura ya autorizada localmente. Mostrar su estado pendiente.
- Sincronización: obtener sesión válida, revalidar cuenta/rol/asignación y enviar con operationId original. Un 401 pide login sin borrar nada; un 403 conserva el conflicto y sus evidencias.
- Logout: cerrar conexiones y memoria de sesión. Si hay capturas pendientes, advertir y conservarlas aisladas por identidad; no permitir que otro usuario las consulte o sincronice.
- Revocación operativa: cambiar permisos en BD y cerrar canales SignalR correspondientes. No depender únicamente de que expire el JWT para retirar acceso.
- Cognito indisponible: una sesión aún válida puede operar si firma/permiso se verifican; una sesión nueva o expirada espera recuperación. P0 conserva captura offline; no se omite autenticación.

## Automatización desde n8n

El servidor existente es `https://n8n.bryan-bano.com/`. Configurar un cliente Cognito confidencial exclusivo, grant `client_credentials`, sin scopes `openid` humanos, con scopes `automation/risk.run`, `automation/risk.read`, `automation/explanation.write` y `automation/demo.incident` únicamente en demo. `risk.run` autoriza solicitar el cálculo por POST; `risk.read` solo su snapshot permitido.

La API vincula ese cliente al rol Automatización y organización autorizada. Nunca le permite despachar, recibir o administrar accesos. n8n obtiene tokens cortos y renueva ante expiración; client secret reside en su almacén de credenciales, no en exportaciones ni en la PWA. El cliente productivo no recibe scope de incidentes sintéticos. Revisar costo M2M antes de configurar frecuencia de tokens; reutilizar cada token válido hasta su renovación.

Una cuenta/clave API administrativa de n8n sirve para administrar/importar workflows; no sustituye la identidad con que los workflows llaman a nuestra API.

## Evidencia de cierre

B-29 y V-13/V-14/V-24: login web/PWA, token expirado, ID token rechazado, issuer/client ajeno rechazado, acciones permitidas/denegadas para cada rol, revocación durante offline, logout con cola y reconexión SignalR. Guardar resultados anonimizados; la elección del proveedor no marca estas pruebas como hechas.

## Referencias

- [Authorization Code, PKCE y callbacks Cognito](https://docs.aws.amazon.com/cognito/latest/developerguide/authorization-endpoint.html).
- [Verificación de JWT y client_id](https://docs.aws.amazon.com/cognito/latest/developerguide/amazon-cognito-user-pools-using-tokens-verifying-a-jwt.html).
- [Refresh token y rotación](https://docs.aws.amazon.com/cognito/latest/developerguide/amazon-cognito-user-pools-using-the-refresh-token.html).
- [Scopes y M2M Cognito](https://docs.aws.amazon.com/cognito/latest/developerguide/cognito-user-pools-define-resource-servers.html).

Ver [[Seguridad y evidencias]], [[Movil offline y sincronizacion]] y [[Entornos y operacion acordados]].

## Cliente de automatización — propuesta del 12 de octubre de 2026

El spec 003 (`docs/specs/003-agente-logistico/`) propone crear en Terraform el cliente confidencial de n8n con un único scope `istpetdev-api/automation`, en lugar de los cuatro scopes finos de la sección «Automatización desde n8n». El límite real lo aplica la API: la identidad con ese scope solo recibe el rol `Automatizacion`, que no puede publicar, aprobar, rechazar, deshacer ni cambiar el modo de autonomía. Contradicción registrada en [[Hechos canonicos]].

## Clientes web y PWA — implementación efectiva y hotfix del 14 de octubre de 2026

La especificación 013 implementó el flujo OIDC PKCE real con Cognito User Pool (`us-east-1_4Y2j5DINp`) retirando completamente los mocks de desarrollo:

1. **Clientes de Cognito y Callbacks de Producción**:
   - Cliente Web (`6ejhdau47gad74id7id1l7ugln`):
     - `CallbackURLs`: `http://localhost:4200/auth/callback` y `https://istpetdev.doicela.dev/auth/callback`
     - `LogoutURLs`: `http://localhost:4200` y `https://istpetdev.doicela.dev`
   - Cliente Móvil (`3k59rv5cov1sinllgd7igmpe1a`):
     - `CallbackURLs`: `http://localhost:8100/auth/callback` y `https://istpetdev.doicela.dev/campo/auth/callback`
     - `LogoutURLs`: `http://localhost:8100` y `https://istpetdev.doicela.dev/campo/`
   - Sincronizados en AWS Cognito y formalizados en `infra/terraform/environments/dev/identity.tf`.

2. **Persistencia de la sesión OIDC (`OidcSessionStorage`, antes `TokenFreeStorage`)**:
   - Inicialmente, la especificación 013 restringía los tokens a memoria JavaScript estricta, persistiendo únicamente los verificadores PKCE (`AUTH_FLOW_KEYS`) en `sessionStorage`.
   - **Diagnóstico de Causa Raíz**: Los navegadores modernos bloquean el uso de cookies de terceros dentro de iframes de orígenes cruzados (`amazoncognito.com`), lo que impide la renovación silenciosa (*silent renew*) al recargar la página. Como consecuencia, cualquier actualización de página (`F5`) o navegación directa por la barra de direcciones (por ejemplo, a `/inicio`) perdía los tokens en memoria y expulsaba al usuario al login.
   - **Resolución bajo IETF OAuth 2.0 BCP**: Conforme a la recomendación IETF OAuth 2.0 Security Best Current Practice para Browser-Based Apps, `TokenFreeStorage` persiste la sesión OIDC en `sessionStorage`. Esta capa provee aislamiento estricto por pestaña, eliminación automática al cerrar la pestaña y protección contra ataques CSRF (al no depender de cookies ambientales), permitiendo la rehidratación inmediata de la sesión en recargas y navegación interna directa. Los tokens continúan excluidos de `localStorage` e `IndexedDB`.
   - **Ajuste del 15-oct (D-013-S1)**: la sesión moría al volver después de 15 minutos porque al arrancar solo se miraba el access token y no se usaba el refresh token. Ahora el arranque llama a `checkAuthIncludingServer` cuando hay refresh token, un 401 de la API devuelve a Cognito y `clear()` borra solo las claves de la librería. La PWA pasa a `localStorage` (con `useRefreshTokenLock`) para que el conductor no pierda la sesión cuando el sistema cierra la app. Detalle y riesgo aceptado en la sección de contradicciones del spec 013.
   - En `auth.config.ts` (Web y Móvil) se configuró `disableRefreshTokenOfflineAccessScopeWarning: true` para suprimir la advertencia sobre el scope `offline_access`, no requerido por AWS Cognito.

3. **Validación de Identidad y Claims en API**:
   - Despliegue en EC2 parametriza `Authentication__Authority`, `Authentication__AllowedClientIds__0` y `Authentication__AllowedClientIds__1` desde variables de repositorio en GitHub Actions (`deploy.yml`).
   - Endpoint `GET /api/v1/me` resuelve la identidad autenticada contra la tabla `Account` en PostgreSQL (`istpetdev_integration`), validando rol institucional y plataforma autorizada (`web` o `field`).

4. **Aprovisionamiento y Sincronización del Equipo (Resolución Definitiva Issue #55)**:
   - Se formalizó la matriz canónica de 6 cuentas por rol para pruebas E2E y demostración oficial (Spec 013), sincronizadas en AWS Cognito User Pool (`us-east-1_4Y2j5DINp`) y en las bases de datos de Desarrollo Central (`istpetdev_dev_01`) y Producción Amazon RDS (`istpetdev_integration`):
     - `bryan.bano@istpet.edu.ec`: Rol `Gerencia` (`sub: 64e8d438-80e1-7076-2c94-293a80eb6845`, plataforma `web`)
     - `anthony.pilatasig@istpet.edu.ec`: Rol `Planificador` (`sub: f4385408-9011-7031-dbef-8b1c77a0459e`, plataforma `web`)
     - `joseph.bano@istpet.edu.ec`: Rol `Auditor` (`sub: c4e844f8-d061-70ef-58fd-641218081778`, plataforma `web`)
     - `andres.jbo2003@gmail.com`: Rol `Receptor` (`sub: 04b8e4b8-7011-7002-946e-dab293f2a188`, plataforma `field`)
     - `alkut202@gmail.com`: Rol `Conductor` (`sub: 94a85418-50b1-7067-3252-76ff8425cc4c`, plataforma `field`)
     - `jorge.doicela.m@gmail.com`: Rol `Bodega` (`sub: 14987428-f011-700e-75d2-5340b3fba178`, plataforma `field`)
   - **Contraseñas Definitivas:** Fijadas mediante `admin-set-user-password --permanent` (clave corporativa distribuida por canal privado seguro, cumpliendo los 14 caracteres y requisitos de `identity.tf`), eliminando enlaces temporales de invitación y requerimientos de cambio en primer inicio.
   - **Desactivación Segura:** Cuentas provisionales de pruebas previas (`ismael02doicela@gmail.com`, `bryanbano1090@gmail.com`, `banobryan1090@gmail.com`) desactivadas en base de datos (`IsActive = false`) y deshabilitadas en Cognito (`admin-disable-user`), preservando la integridad referencial para evitar fallos en tablas de auditoría.
   - Navegación superior (`HeaderWidget`) adaptada por rol, habilitando `/users` para Gerencia y manteniendo los enlaces accesibles durante toda la sesión autenticada.


