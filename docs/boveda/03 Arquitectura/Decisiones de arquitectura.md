---
tipo: arquitectura
estado: propuesta
actualizado: 2026-10-09
tags: [arquitectura, contratos]
---

# Decisiones de arquitectura

**Estado:** el 6 de octubre de 2026 se registraron como aceptadas ADR-04, ADR-09, ADR-13, ADR-14 y ADR-15; el 7 de octubre Bryan aceptó ADR-01 y concretó ADR-03, ADR-04 (versiones), ADR-06, ADR-08, ADR-10 y ADR-12 (commit be0b127, sección «Complemento de decisiones»). Las aceptaciones del 6 de octubre siguen sin **Deciders** (quién decidió), como exige [[Convenciones y estados]]: el historial de git solo muestra quién las registró. Resumen y contradicciones abiertas en [[Hechos canonicos]]. Una decisión aceptada no es evidencia de implementación.

## Registro inicial

| ID | Tema | Base solicitada | Recomendación / decisión pendiente |
|---|---|---|---|
| ADR-01 | Versión .NET | .NET 8 | **Aceptada por Bryan, 2026-10-07** (be0b127): .NET 8; actualización planificada antes del fin de soporte. Ver complemento |
| ADR-02 | Topología | Clean Architecture + CQRS | API modular, una base; separación lógica de lectura/escritura |
| ADR-03 | Motor/IDs | PostgreSQL/PostGIS + UUID v7 | **Diseño concretado, 2026-10-07** (be0b127): PostgreSQL 16 + PostGIS compatible; UUID v7 generado en aplicación. Ver complemento |
| ADR-04 | Stack Web/Móvil | Angular 22, Ionic 8, Signals y Shared Core | **Aceptada por Jefe de Frontend y Bryan, 2026-10-11**: Angular 22 Standalone, Ionic 8, Signals para UI, RxJS para flujos asíncronos, monorepo `@istpetdev/shared-core`, Arquetipo B Técnico/Minimalista, Lucide Icons (`lucide-angular`) y tokens oficiales de Expo Clean Ecuador en `tailwind.config.js` ([[Sistema de diseno y colores]]). |
| ADR-05 | Mapas | Leaflet + OpenStreetMap | **Aceptada por Jefe de Frontend y Bryan, 2026-10-11**: Leaflet (`leaflet` + `@types/leaflet`) con OpenStreetMap en componente desacoplado `shared/ui/map-view` y orquestación en `widgets/route-map`. Código abierto, sin costo ni cuotas de API keys que arriesguen la demo. |
| ADR-06 | Offline | SQLite/IndexedDB | **Diseño concretado, 2026-10-07** (be0b127): PWA con IndexedDB primero; Capacitor 8 después. Ver complemento |
| ADR-07 | Tiempo real | SignalR + autoscaling | Redis backplane al permitir varias réplicas; afinidad probada |
| ADR-08 | IA | n8n + DeepSeek | Riesgo numérico determinista; LLM solo explicación y recomendaciones acotadas. Hosting: n8n existente de Bryan (2026-10-07) |
| ADR-09 | Cloud y hosting | Dual: EC2 Hardened (Demo) / ECS Fargate (P2) | **Aceptada, 2026-10-06** (registrada por JorgeDoicela, commit 4c3a9ef; Deciders PENDIENTE): perfil ágil económico para hackathon y elástico para escala nacional; [[AWS y Terraform]]. Ampliada el 2026-10-07: ver complemento |
| ADR-10 | Identidad | No definida | **Definida, 2026-10-07** (decisión delegada por Bryan, be0b127): Amazon Cognito; [[Identidad OIDC y sesiones]] |
| ADR-11 | Auditoría | Trazabilidad «inmutable» | Ledger append-only en aplicación; describir garantías reales |
| ADR-12 | Repo y colaboración | Bóveda en GitHub personal | Bóveda en bryancito1090/istpetdev-hackathon-2026; software en bryancito1090/istpetdev-platform, creado vacío el 2026-10-07 (be0b127) |
| ADR-13 | Arquitectura frontend | Feature-Sliced Design (FSD) | **Aceptada por el equipo, 2026-10-06** (registrada por bryancito1090, commit 765c532; Deciders por nombrar): capas y límites en [[Frontend con Feature-Sliced Design]] |
| ADR-14 | Autorización | Control de acceso basado en roles (RBAC) | **Aceptada, 2026-10-06** (registrada por bryancito1090, commit 5905073, como «instrucción del usuario»; Deciders por nombrar): permisos por rol y ámbito en servidor; [[Seguridad y evidencias]] |
| ADR-15 | CI/CD y Hardening Host | Pipeline DAG + GHCR + Rollback SHA + Defensa 6 Capas | **Aceptada, 2026-10-06** (registrada por JorgeDoicela, commit 4c3a9ef; Deciders PENDIENTE): compilación en runners, rotación 3 imágenes, rollback con objetivo <30 s y Lynis >80/100; [[CI-CD y automatizacion de despliegue]] y [[Hardening y seguridad de servidores]] |
| ADR-16 | Configuración desde DB | Roles y concesiones administrables, plantillas/procesos/parámetros versionados y aislamiento RLS | **Aceptada por Bryan, 2026-10-09**, instrucción directa en conversación: amplía ADR-14 y sustituye su límite de roles fijos; diseño en [[RBAC y configuracion del sistema]] y [[Esquema completo de base de datos]], implementación pendiente |
| ADR-17 | Móvil offline y firma digital | PWA con IndexedDB y `signature_pad` | **Aceptada por Jefe de Frontend y Bryan, 2026-10-11**: Captura de firma táctil en canvas exportada a PNG, persistencia en IndexedDB (`idb`) con UUID v7, y sincronización encolada e idempotente hacia backend/S3. |
| ADR-18 | Cliente OIDC y Cognito PKCE | Flujo Authorization Code con PKCE S256 en Angular | **Aceptada por Jefe de Frontend y Bryan, 2026-10-11**: Flujo OIDC seguro PKCE S256 contra User Pool dev, sin client secret en cliente web/móvil, e inyección de token JWT mediante `AuthInterceptor` en `shared/api`. |

## ADR-01 — versión de backend

.NET 8 conserva compatibilidad con la propuesta y experiencia del equipo. Para un producto que continúe después de noviembre, .NET 10 LTS evita una actualización inmediata por fin de soporte. **No se cambia el stack automáticamente:** decidir según dependencias disponibles y tiempo. [Soporte oficial de .NET](https://dotnet.microsoft.com/en-us/platform/support/policy).

## ADR-03 — UUID v7

PostgreSQL 18 ofrece generación nativa uuidv7(); versiones previas pueden almacenar UUID v7 aunque no tengan esa función. Seleccionar versión disponible en RDS y compatible con PostGIS; si se conserva .NET 8, verificar una implementación de generación compatible, sin asumir que tiene las APIs de runtimes posteriores. [Funciones UUID de PostgreSQL 18](https://www.postgresql.org/docs/18/functions-uuid.html), [UUID de PostgreSQL 17](https://www.postgresql.org/docs/17/functions-uuid.html).

No tomar orden UUID como orden de recepción offline; conservar fechas y secuencia de aceptación separadas. Mejor localidad temporal no equivale a rendimiento máximo garantizado.

## ADR-04 — Stack Web/Móvil, Reactividad Híbrida y Monorepo Shared Core

**Aceptada, 2026-10-06.** Deciders: PENDIENTE (registrada por JorgeDoicela, commit 4c3a9ef). Contexto: se requiere coherencia estricta entre la web de gestión y la app móvil, evitando reescribir contratos o lidiar con suscripciones descontroladas de RxJS en componentes.

**Elección:**
1. **Versiones fijadas y diseño:** Node.js 22, Angular 22 (Standalone Components), Ionic 8, Capacitor 6, Tailwind CSS 3.4+, Arquetipo B (Técnico/Minimalista) y paleta oficial de Expo Clean Ecuador ([[Sistema de diseno y colores]]).
2. **Modelo reactivo:** Angular Signals (`signal()`, `computed()`, `input()`, `output()`) para todo el estado síncrono de UI y componentes. RxJS exclusivamente para flujos asíncronos complejos, debounce de inputs y canales WebSockets de SignalR, integrados con `toSignal()`.
3. **Monorepo Shared Core (`libs/shared-core`):** Librería compartida para tipos TypeScript de dominio, DTOs de API, validadores de esquemas y utilidades de conversión de unidades logísticas (litros, kilos, cajas), consumida por `apps/web` y `apps/mobile`.
4. **Desacoplamiento de mapas:** Componente `shared/ui/map-view` puramente visual y agnóstico a lógica de negocio, orquestado desde `widgets/route-map`.
5. **Carga ultraligera de QR:** Ruta `/trace/:id` aislada en lazy loading (< 150 KB gzip) para una carga rápida en el teléfono del jurado (objetivo sin medir).

Detalles en [[Frontend y componentes]], [[Sistema de diseno y colores]] y [[Frontend con Feature-Sliced Design]].

## ADR-05 — mapas y cartografía interactiva

**Fecha:** 2026-10-11. **Deciders:** Jefe de Frontend y Bryan. **Estado:** Aceptada.
**Contexto:** Se requiere visualizar geográficamente en el panel administrativo web los puntos de servicio (hospitales y bodegas con coordenadas PostGIS) y trazar la comparativa de ruta tradicional vs ruta optimizada durante el pitch y la operación.

**Opciones consideradas:**
- **Google Maps / Routes API:** Costosa, requiere facturación y tarjeta de crédito en GCP, riesgo de bloqueo por cuotas durante el evento.
- **Mapbox GL JS:** Requiere token privado de cuenta y cuotas de facturación tras superar el límite gratuito.
- **Leaflet + OpenStreetMap:** 100% de código abierto, ultraligera (<40 KB gzip), sin costo, sin necesidad de tokens ni tarjetas, madura y compatible con Angular 22 Standalone.

**Decisión:** Se adopta **Leaflet (`leaflet` + `@types/leaflet`)** con tiles base de OpenStreetMap. Siguiendo Feature-Sliced Design (FSD):
- El componente `apps/web/src/shared/ui/map-view/` actúa como lienzo visual puro agnóstico a la lógica de negocio (recibe marcadores y polilíneas GeoJSON).
- El widget `apps/web/src/widgets/route-map/` orquesta la carga de puntos desde el backend (`/api/v1/service-points`) y compone la ruta sobre el mapa.

## ADR-17 — móvil offline, firma digital y sincronización en campo

**Fecha:** 2026-10-11. **Deciders:** Jefe de Frontend y Bryan. **Estado:** Aceptada.
**Contexto:** Conforme a RF-06 y RNF-04, el conductor y recepcionista deben poder operar en modo avión o sin cobertura celular, registrando la recepción de la entrega, capturando cantidades y obteniendo la firma digital del receptor sin perder datos al reiniciar la app.

**Decisión:**
1. **Firma digital táctil:** Se utiliza la librería **`signature_pad`** en un canvas HTML5 responsivo dentro de la app móvil Ionic, exportando la firma como imagen PNG codificada para evidencia en S3.
2. **Cola de persistencia offline:** Se utiliza **IndexedDB** mediante el wrapper ligero **`idb`** para encolar eventos de custodia y recepciones locales con identificadores UUID v7.
3. **Protocolo de sincronización:** Al recuperar conexión a red, el cliente despacha las operaciones pendientes con claves de idempotencia UUID v7 hacia el backend. El estado de la interfaz respeta los 5 estados offline (`Guardado en el teléfono`, `Enviando`, `Aceptado por el servidor`, `Conflicto`, `Evidencia pendiente`).

## ADR-18 — cliente OIDC y gestión de sesiones Cognito en frontend

**Fecha:** 2026-10-11. **Deciders:** Jefe de Frontend y Bryan. **Estado:** Aceptada.
**Contexto:** Complementando ADR-10, se requiere integrar la autenticación en Angular 22 y la app móvil contra el User Pool de Amazon Cognito dev (`us-east-1_4Y2j5DINp`) mediante clientes públicos sin client secret.

**Decisión:**
1. **Flujo de autorización:** Flujo **Authorization Code con PKCE (Proof Key for Code Exchange) S256** nativo o mediante librería estándar OIDC, almacenando el `code_verifier` temporalmente en `sessionStorage`.
2. **Inyección de credenciales:** Creación de un `AuthInterceptor` en `apps/web/src/shared/api/` que intercepta peticiones HTTP salientes y adjunta la cabecera `Authorization: Bearer <accessToken>`.
3. **Manejo de tokens:** Tokens de acceso con expiración de 15 minutos; refresco silencioso con rotación de refresh tokens. Si la sesión caduca offline, la cola local no se borra y solicita reautenticación al recuperar red sin perder las entregas capturadas.

## Aceptar una decisión

Copiar [[Plantilla ADR]] y registrar contexto, opciones, elección, fecha y evidencia. Actualizar las notas afectadas, el contrato y el catálogo de skills. No cerrar una decisión solo por ser «más escalable» si no mejora un requisito concreto.

## Puerta de entrada para skills

Las skills del equipo están documentadas en [[Catalogo y plan de skills]] (7 de octubre de 2026) y remiten a [[Hechos canonicos]] para todo dato no cerrado. Al cerrar ADR-01/03/04/05/06/09/10 o validar una entrega vertical, actualizar primero el ADR, luego [[Hechos canonicos]] y por último las skills afectadas; ver [[Catalogo y plan de skills]].

## ADR-13 — FSD en Angular web

**Aceptada, 2026-10-06, por instrucción de IstpetDev.** Deciders: por nombrar (registrada por bryancito1090, commit 765c532). La estructura web usa app/pages/widgets/features/entities/shared, API pública por slice y dependencias hacia capas inferiores. Standalone Components y Tailwind se conservan. Las versiones siguen pendientes; FSD no decide el modelo de estado ni cambia el backend. Referencia: [[Frontend con Feature-Sliced Design]].

## ADR-14 — RBAC en API, web, móvil y automatización

**Ampliada por ADR-16 el 9 de octubre:** se conserva autorización por acción/recurso y se sustituye el límite de catálogo de roles no administrable. La matriz de seis roles es el seed funcional inicial; roles/concesiones pasan a ser datos configurables por organización.

**Aceptada, 2026-10-06.** Deciders: por nombrar (registrada por bryancito1090, commit 5905073, como «instrucción del usuario»). Contexto: ya se describían roles funcionales y autorización por recurso, pero faltaba declarar el modelo que cumple RNF-01.

**Opciones iniciales:** reglas aisladas por endpoint o un catálogo compartido de roles/acciones con restricciones por recurso. Se eligió **RBAC con validación de organización, almacén, punto y asignación**, para aplicar la misma matriz en API y SignalR. La ampliación ADR-16 permite administrar los roles; las acciones siguen vinculadas a handlers implementados. Definición en [[Seguridad y evidencias]].

**Consecuencias:** vincular identidades a roles por organización, centralizar políticas, reflejar permisos en web/móvil y limitar n8n. Para P0, administrar accesos por configuración/seed controlado y auditado. La sincronización offline vuelve a comprobar permisos vigentes. La elección del proveedor OIDC sigue pendiente en ADR-10.

**Verificación pendiente:** B-29 en [[Backlog]]; V-13/V-24 para permisos, aislamiento y revocación; V-14 al incorporar SignalR, según [[Plan de validacion]]. La decisión documenta lo que se implementará, sin afirmar que ya funciona.

## ADR-09 — Topología dual de infraestructura cloud (Demo vs Escala Nacional)

**Aceptada, 2026-10-06.** Deciders: PENDIENTE (registrada por JorgeDoicela, commit 4c3a9ef). Contexto: la propuesta inicial de ECS Fargate + RDS Multi-AZ + ALB implica costos elevados ($160-$280 USD/mes) derivados de NAT Gateways y balanceadores, además de complejidad innecesaria para un hackathon de 48 horas.

**Elección:** Se adopta formalmente una **estrategia dual**:
1. **Perfil A (Hackathon / Demo / Piloto):** Instancia AWS EC2 con Elastic IP, Docker Compose, Nginx Reverse Proxy con TLS 1.2/1.3 y estándar de blindaje Linux de 6 capas. Costo registrado originalmente como $18–28 USD/mes sin fuente; verificado el 7-oct-2026: t3.medium ≈ USD 37/mes, t3.small ≈ USD 22/mes. Control operativo directo del servidor.
2. **Perfil B (Operación Nacional Elástica P2):** ECS Fargate + RDS PostgreSQL Multi-AZ + ALB administrado con Terraform como entregable de viabilidad para producción extendida.

Detalles en [[AWS y Terraform]].

## ADR-15 — Pipeline CI/CD Multi-Etapa DAG, rollback rápido y Hardening Linux

**Aceptada, 2026-10-06.** Deciders: PENDIENTE (registrada por JorgeDoicela, commit 4c3a9ef). Contexto: el despliegue no puede depender de operaciones manuales, builds en el servidor que saturen memoria o falta de recuperación ante imprevistos en vivo.

**Elección:**
1. **Pipeline CI/CD DAG en GitHub Actions:** Filtrado inteligente de rutas, pruebas unitarias aisladas en runner, compilación de contenedores con Docker Buildx, etiquetas inmutables por SHA en GHCR, despliegue atómico por SSH y reporte ejecutivo en GitHub Summary.
2. **Rollback con objetivo < 30 s (sin medir):** Retención preventiva de las últimas 3 versiones de imágenes en el host; workflow disparado manualmente con `target_sha` sin tiempos de rebuild.
3. **Defensa en Profundidad de 6 Capas:** Mitigación obligatoria de la trampa de Docker vinculando todos los contenedores a loopback `127.0.0.1`, UFW con Default Deny, Fail2ban con jail recidive, OpenSSH con Ed25519 sin passwords ni root, Nginx con TLS 1.2/1.3 (solo 1.3 PENDIENTE), rate limit y cierre TCP HTTP 444 ante evasión de CDN, particiones `noexec/nosuid` y meta de auditoría Lynis > 80/100.

Referencias: [[CI-CD y automatizacion de despliegue]], [[Hardening y seguridad de servidores]], [[AWS y Terraform]].


## Complemento de decisiones — 7 de octubre de 2026

Se conserva el registro y las decisiones del compañero del 6 de octubre. Bryan confirmó decisiones posteriores y delegó concretar las recomendaciones. Este complemento actualiza el estado para implementación; no reescribe el contenido anterior ni declara software desplegado.

| Actualiza | Estado actualizado | Definición vigente |
|---|---|---|
| → ADR-01 | Aceptada por Bryan, 2026-10-07 | .NET 8; mantener su rama y planificar actualización antes de fin de soporte, sin sustituirlo ahora |
| → ADR-03 | Diseño concretado, implementación pendiente | PostgreSQL 16/PostGIS compatible; RDS privado dev, DB aislada por integrante/integración y UUID v7 generado en aplicación con interoperabilidad probada |
| → ADR-04 | Versiones concretadas por instrucción de Bryan | Angular 22, Node 22, Ionic 8 y Tailwind 3.4; Signals/RxJS/FSD/shared-core conservados. Parches y Capacitor recomendado en [[Repositorio de software y versiones]] |
| → ADR-06 | Diseño concretado | PWA con IndexedDB primero; nativo posterior con Capacitor 8 y persistencia probada |
| → ADR-08 | Hosting concretado | n8n existente de Bryan, workflows exportados sin credenciales; LLM explicativo y fallback conservados |
| → ADR-09 | Aceptada y ampliada | Perfiles A/B conservados; cada integrante ejecuta localmente, RDS dev compartido privado, stack completo AWS encendido solo para ensayos y producción posterior |
| → ADR-10 | Decisión delegada y definida, 2026-10-07 | Amazon Cognito User Pools; Authorization Code + PKCE, cliente M2M n8n y RBAC de ADR-14; [[Identidad OIDC y sesiones]] |
| → ADR-12 | Repositorio creado vacío, 2026-10-07 | GitHub privado `bryancito1090/istpetdev-platform`; esqueleto exclusivamente local en `~/Projects/ISTPETDEV` |
| → ADR-15 | Conservada; implementación pendiente del compañero | DAG/GHCR/SSH/SCP/retención/rollback/hardening, con condiciones operativas en [[Entornos y operacion acordados]] |

### ADR-10 — identidad OIDC y sesiones

Se selecciona Cognito administrado para evitar operar otro servidor de identidad. Web/PWA usan clientes públicos sin secret, PKCE S256, tokens de acceso de 15 minutos y reautenticación que conserva cola offline. La API valida issuer/firma/token_use/client_id/scopes y aplica roles y organización/asignación desde PostgreSQL. n8n usa cliente confidencial separado con scopes de Automatización. Configuración completa, alternativas operativas y pruebas pendientes en [[Identidad OIDC y sesiones]].

## ADR-16 — configuración, plantillas y RBAC administrable

**Fecha:** 2026-10-09. **Decider:** Bryan, solicitud directa en esta conversación. **Estado:** aceptada la dirección de producto; esquema/procedimientos propuestos, implementación y validación pendientes.

**Contexto:** la bóveda contiene entidades lógicas y una infraestructura documentada, pero no el detalle de todas las tablas. El backend local inspeccionado tiene cinco entidades/migración inicial y autenticación Cognito; no el esquema completo ni RBAC administrable. Bryan solicita definir primero esa base y permitir gestión de roles y plantillas desde DB, con revisión de seguridad previa a lanzamiento.

**Decisión:** PostgreSQL conserva roles/concesiones/ámbitos, parámetros versionados, catálogos, plantillas y workflows declarativos. Los seis roles funcionales son seeds; se propone un rol humano ConfigurationAdmin para administración acotada. Permission/FieldDefinition describen operaciones/campos implementados y límites de delegación; configurar no permite ejecutar código/SQL arbitrario ni apagar garantías de seguridad/inventario. Cognito conserva identidad y credenciales; RLS/FORCE y FKs por organización complementan la autorización de backend.

**Consecuencias:** se reemplaza la prohibición de editor dinámico de ADR-14/P0, se agregan RF-17/18/19 y RNF-13/14, con entregas B-37 a B-40 y casos V-34 a V-39. Publicación congela versiones y registra actor/motivo; capturas offline guardan su versión y se reautorizan al sincronizar. La nueva skill complementa las existentes y no se presenta como middleware/CI implementado.

**Definición:** [[Esquema completo de base de datos]], [[RBAC y configuracion del sistema]], [[Verificacion de seguridad antes del lanzamiento]] y [[istpetdev-prelaunch]]. Migraciones EF preservan esquema/datos previos y la separación migrador/API; [[Conexion PostgreSQL en DBeaver]] prepara las dos conexiones sin crear credenciales duplicadas.

Dominio institucional y CI/CD/Compose los profundizarán los compañeros indicados por Bryan. Mapas continúa en ADR-05 pendiente de prueba/costo/licencia. Cuenta AWS y presupuesto están disponibles; se cargan valores privados sin volver a tratar su disponibilidad como bloqueo. Ver [[Entornos y operacion acordados]].
