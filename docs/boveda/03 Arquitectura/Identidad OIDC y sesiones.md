---
tipo: arquitectura
estado: aceptado
actualizado: 2026-10-09
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
| MFA | Obligatorio para administración de accesos/AWS; TOTP en cuentas humanas habilitadas para el piloto | MFA humano obligatorio en producción; probar experiencia del conductor |
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

- Web/PWA: tokens en memoria, no en URLs, logs, localStorage ni en la outbox. Tras cerrar/reabrir, usar reautenticación OIDC; la cola de capturas se conserva.
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
