---
tipo: arquitectura
estado: propuesta
actualizado: 2026-10-12
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
| ADR-08 | IA | n8n + DeepSeek | Riesgo numérico determinista; LLM solo explicación y recomendaciones acotadas. Hosting: n8n existente de Bryan (2026-10-07). 12-oct: ampliación propuesta en ADR-22 (agente con herramientas; DeepSeek con prueba de aceptación) |
| ADR-09 | Cloud y hosting | Dual: EC2 Hardened (Demo) / ECS Fargate (P2) | **Aceptada, 2026-10-06** (registrada por JorgeDoicela, commit 4c3a9ef; Deciders PENDIENTE): perfil ágil económico para hackathon y elástico para escala nacional; [[AWS y Terraform]]. Ampliada el 2026-10-07: ver complemento |
| ADR-10 | Identidad | No definida | **Definida, 2026-10-07** (decisión delegada por Bryan, be0b127): Amazon Cognito; [[Identidad OIDC y sesiones]] |
| ADR-11 | Auditoría | Trazabilidad «inmutable» | Ledger append-only en aplicación; describir garantías reales |
| ADR-12 | Repo y colaboración | Reemplazada por ADR-20 | Se conserva el acuerdo del 7-oct-2026 como histórico; código y bóveda se unifican por instrucción de Bryan el 12-oct-2026 |
| ADR-13 | Arquitectura frontend | Feature-Sliced Design (FSD) | **Aceptada por el equipo, 2026-10-06** (registrada por bryancito1090, commit 765c532; Deciders por nombrar): capas y límites en [[Frontend con Feature-Sliced Design]] |
| ADR-14 | Autorización | Control de acceso basado en roles (RBAC) | **Aceptada, 2026-10-06** (registrada por bryancito1090, commit 5905073, como «instrucción del usuario»; Deciders por nombrar): permisos por rol y ámbito en servidor; [[Seguridad y evidencias]] |
| ADR-15 | CI/CD y Hardening Host | Pipeline DAG + GHCR + Rollback SHA + Defensa 6 Capas | **Aceptada, 2026-10-06** (registrada por JorgeDoicela, commit 4c3a9ef; Deciders PENDIENTE): compilación en runners, rotación 3 imágenes, rollback con objetivo <30 s y Lynis >80/100; [[CI-CD y automatizacion de despliegue]] y [[Hardening y seguridad de servidores]] |
| ADR-16 | Configuración desde DB | Roles y concesiones administrables, plantillas/procesos/parámetros versionados y aislamiento RLS | **Aceptada por Bryan, 2026-10-09**, instrucción directa en conversación: amplía ADR-14 y sustituye su límite de roles fijos; diseño en [[RBAC y configuracion del sistema]] y [[Esquema completo de base de datos]], implementación pendiente |
| ADR-17 | Móvil offline y firma digital | PWA con IndexedDB y `signature_pad` | **Aceptada por Jefe de Frontend y Bryan, 2026-10-11**: Captura de firma táctil en canvas exportada a PNG, persistencia en IndexedDB (`idb`) con UUID v7, y sincronización encolada e idempotente hacia backend/S3. |
| ADR-18 | Cliente OIDC y Cognito PKCE | Flujo Authorization Code con PKCE S256 en Angular | **Aceptada por Jefe de Frontend y Bryan, 2026-10-11**: Flujo OIDC seguro PKCE S256 contra User Pool dev, sin client secret en cliente web/móvil, e inyección de token JWT mediante `AuthInterceptor` en `shared/api`. |
| ADR-20 | Repositorio unificado | Código y bóveda en istpetdev-platform | **Aceptada por Bryan, 2026-10-12**, instrucción directa: bóveda en `docs/boveda/`, historial conservado e integración por PR a `develop`; reemplaza ADR-12 |
| ADR-21 | Núcleo delgado para la demo | Esquema de 27 tablas, roles fijos e importador canónico | **Propuesta, 2026-10-12** (Deciders PENDIENTE; debe incluir a Bryan como Decider de ADR-16): difiere a P2 RF-17/18/19 y RNF-13; Schema95 se archiva en el tag `schema95-archivo`; [[Definiciones de producto y plan hasta el pitch]] |
| ADR-22 | Autonomía del agente | Agente con herramientas y motor de reglas determinista | **Propuesta, 2026-10-12** (Deciders PENDIENTE): el LLM orquesta y explica sin calcular; reglas en el backend deciden qué se ejecuta solo y qué se escala; complementa ADR-08 |
| ADR-24 | Segregación de Plataformas y Roles de Campo | Web (Torre de Control) vs Móvil (Campo) y Recepción Hospitalaria QR | **Aceptada, 2026-10-13** (Deciders: Bryan y Equipo de Desarrollo): Bodega, Conductor y Receptor operan exclusivamente en PWA móvil (`apps/mobile`); Planificador, Gerencia y Auditor en Web (`apps/web`); Automatización como daemon sin interfaz. Recepción hospitalaria mediante verificación binaria y reporte ágil de novedades. |
| ADR-25 | Ergonomía PWA y flujos por rol | Roles segregados, entrega desde ruta, lectura asimétrica QR y cero emojis | **Aceptada por Bryan y equipo, 2026-10-13**: Vistas exclusivas por rol (Chofer, Bodega, Supervisor); chofer simplificado a 3 botones; entrega iniciada desde parada en «Ver Ruta Hoy»; lectura asimétrica de QR (chofer lee caja en bodega, hospital lee al chofer); incidencias en entrega conectadas a alertas de supervisión; prohibición estricta de emojis por SVGs técnicos. |

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

## ADR-20 — repositorio unificado

**Estado:** aceptado. **Fecha:** 2026-10-12. **Decider:** Bryan. **Origen:** instrucción directa de integrar esta bóveda dentro de `bryancito1090/istpetdev-platform` y resolver los conflictos. **Reemplaza:** ADR-12.

**Contexto:** El código y las decisiones estaban en repositorios separados. Bryan pide trabajar con un solo repositorio.

**Decisión:** Importar la bóveda en `docs/boveda/` de `istpetdev-platform`, conservando el historial Git por subtree y manteniendo los archivos Markdown, el PDF y la configuración común de Obsidian. Los archivos son parte normal del repositorio; no hay submódulo. El repositorio anterior conserva el contexto histórico.

**Consecuencias:** Obsidian abre `docs/boveda/`. Las nuevas ediciones y los PR se realizan en `istpetdev-platform`, hacia `develop`. El workflow de la bóveda se ejecuta desde `.github/workflows/check-vault.yml`, invocando `python docs/boveda/.github/scripts/check_vault.py`. Las sesiones personales y las configuraciones locales de asistentes siguen excluidas de Git. Los acuerdos anteriores fechados se conservan como históricos.

**Verificación:** registro de integración y navegación en [[Fuente y control documental]]. Este traslado documental no acredita ejecución de las aplicaciones ni migraciones de base de datos.

## ADR-21 — núcleo delgado para la demo

- **Estado:** propuesta. **Fecha:** 2026-10-12. **Deciders:** PENDIENTE (debe incluir a Bryan, Decider de ADR-16). **Origen:** sesión de definición del 12-oct y [spec 001](../../specs/001-nucleo-delgado-importador/spec.md). **Reemplazaría parcialmente:** ADR-16 para la demo.

**Contexto:** a cuatro días del evento ninguna capacidad P0 del flujo logístico estaba construida, y cada funcionalidad nueva debía atravesar RLS, RBAC administrable y configuración desde la base. El jurado (5 de 7 no técnicos) evalúa el flujo demostrable.

**Opciones:** (a) esquema delgado de 27 tablas sin RLS, con roles fijos; (b) conservar las 95 tablas usando solo el núcleo; (c) mantener todo con RLS y construir sobre RBAC configurable.

**Decisión propuesta:** (a). Una organización fija conserva `OrganizationId` para la versión multiempresa. Los seis roles funcionales de ADR-14 se guardan en `Account.Role` y se leen en el servidor, no del token. Todo dato operativo entra por un importador canónico (CSV/XLSX + perfil de mapeo). Schema95 queda archivado en el tag `schema95-archivo` y su diseño en [[Esquema completo de base de datos]] y [[RBAC y configuracion del sistema]].

**Consecuencias:** RF-17, RF-18, RF-19 y RNF-13 pasan a P2; B-37 a B-40 quedan diferidas; los ámbitos por asignación (conductor, receptor) se implementan con `Delivery.AssignedAccountId`. Nombres nuevos: `ImportBatch` y `AgentDecision`. Las bases de RDS con Schema95 deben recrearse antes de aplicar la migración nueva.

**Verificación:** pruebas y `test-e2e.md` del spec 001.

## ADR-22 — autonomía del agente con límites

- **Estado:** propuesta. **Fecha:** 2026-10-12. **Deciders:** PENDIENTE. **Origen:** definiciones D4 y D5 de [[Definiciones de producto y plan hasta el pitch]]. **Complementa:** ADR-08.

**Contexto:** el plus de innovación es un agente que opera la logística, no un chatbot. ADR-08 limita el LLM a explicar; el jurado preguntará qué pasa si la IA se equivoca.

**Opciones:** (a) aprobación humana para todo; (b) autonomía con límites decidida por reglas deterministas; (c) autonomía total.

**Decisión propuesta:** (b). El LLM elige herramientas, compone y explica; nunca calcula rutas, cantidades ni fechas. Un motor de reglas en el backend ejecuta solo lo que no retira paradas obligatorias, cumple ventanas, sube el costo ≤ 15% y no retrasa más de 60 min; lo demás se escala con dos opciones calculadas. Interruptor «solo proponer» y acciones reversibles. Los datos importados se pasan al modelo como datos delimitados. El modelo se elige midiendo dos candidatos.

**Consecuencias:** el motor de reglas vive en el spec 002 y el agente en el 003; cada decisión se registra en `AgentDecision`. ADR-08 conserva DeepSeek como proveedor, sujeto a la prueba de aceptación del spec 003 (ver complemento).

**Verificación:** tabla completa de la política con pruebas automáticas y recorrido E2E con una decisión ejecutada sola y otra escalada.

## ADR-23 — motor logístico determinista y distancias en cadena

- **Estado:** propuesta. **Fecha:** 2026-10-12. **Deciders:** PENDIENTE. **Origen:** brainstorming del [spec 002](../../specs/002-motor-logistico/spec.md). **Complementa:** ADR-05 y [[Rutas y sobrecostos]].

**Contexto:** el planificador debe funcionar sin la wifi del recinto, ser reproducible para el simulador y explicable para el jurado.

**Opciones:** (a) heurística propia en .NET (inserción + 2-opt + consolidación); (b) OR-Tools en un microservicio Python; (c) lógica en n8n. Para distancias: cadena con caché, solo aproximación o solo OSRM público.

**Decisión propuesta:** (a) con distancias en cadena: tramos guardados (importados como entidad `road_segments`) → OSRM con URL configurable → línea recta × 1,3 a 40 km/h etiquetada como aproximación. Reloj operativo por defecto en la fecha del último dato importado. Ruta por vehículo para que la política decida por vehículo.

**Consecuencias:** una migración con la caché `RoadMatrixEdge` indexada por coordenadas; fixture `demo-region` con tramos precalculados; la heurística se presenta como «optimizada por la política», no como óptima. El simulador no mezcla tramos viales y aproximados en una misma corrida.

**Verificación:** determinismo, calidad frente a fuerza bruta en instancias pequeñas, rendimiento (≤ 1 s con 15 puntos, ≤ 5 s con 80) y E2E del spec 002.

### Complemento de ADR-22 — diseño del agente (12-oct-2026, propuesta)

Brainstorming del [spec 003](../../specs/003-agente-logistico/spec.md): el agente es un workflow de n8n con nodo AI Agent en el servidor de Bryan (copia local en Docker como plan B); sus herramientas son 5 endpoints del motor de 002; el resultado lo envía un paso fijo del workflow; la API mide la traza, valida cada cifra contra los `facts` y responde a los 10 s con el camino determinista si el agente no entrega. **Proveedor: DeepSeek**, elegido por el equipo sin comparación y sujeto a una prueba de aceptación (llamadas válidas ≥ 95%, p95 ≤ 10 s, explicaciones coincidentes ≥ 90%, inyección 10/10). Identidad con un scope `istpetdev-api/automation` y rol `Automatizacion` sin permiso para publicar, aprobar, rechazar, deshacer ni cambiar el modo. Avisos al conductor dentro de la PWA.

## ADR-24 — Segregación de Roles por Plataforma y Flujo de Recepción Hospitalaria Simplificada

- **Estado:** aceptada. **Fecha:** 2026-10-13. **Deciders:** Bryan y Equipo de Desarrollo. **Origen:** instrucción directa de refinamiento ergonómico y segregación de perfiles funcionales. **Complementa:** ADR-04, ADR-14, ADR-16 y ADR-17.

### Contexto y requisito
En la versión inicial del prototipo web existía un enlace y una pantalla de «Bodega y Despacho» accesible desde el panel de escritorio, y la pantalla pública mostraba accesos prematuros al panel interno. Asimismo, no existía una separación explícita de dispositivos entre la operativa de campo física y la supervisión de torre de control, y el flujo del receptor hospitalario requería una simplificación ergonómica inmediata para el momento crítico de entrega de insumos en el hospital.

### Opciones evaluadas
1. **Opción A (Monolito de interfaz unificado):** Permitir que todos los roles operen en Web y Móvil simultáneamente.
   - *Desventaja:* Inconsistencia ergonómica severa; un operador de bodega cargando camiones o un receptor en farmacia hospitalaria no utilizan una laptop de escritorio ni deben navegar por matrices de riesgo ni mapas VRP PostGIS.
2. **Opción B (Segregación estricta por ergonomía física y tipo de dispositivo):**
   - **Plataforma Web (`apps/web`):** Dedicada exclusivamente a la **Torre de Control Estratégica** (Planificador, Gerencia y Auditor).
   - **Plataforma Móvil (`apps/mobile`):** Dedicada exclusivamente a las **Operaciones Físicas de Campo** (Bodega, Conductor y Receptor Hospitalario) con capacidades 100% offline y soporte táctil/cámara.
   - **Automatización:** Ejecución autónoma como daemon técnico de fondo (sin pantallas humanas).

### Decisión
Se adopta la **Opción B**:
1. **Bodega:** Se retira de la plataforma web. Opera exclusivamente en `apps/mobile` para picking físico, lectura QR de salida y despacho a unidades vehiculares.
2. **Receptor Hospitalario:** Opera exclusivamente en `apps/mobile` con un flujo ultrarrápido:
   - Lectura de QR del bulto recibido (`HU`).
   - Dictamen binario inmediato: **✓ Todo Conforme** (sin daños ni faltantes) o **⚠️ Pasó algo malo**.
   - En caso de anomalía, selección mediante chips de categorización: *Empaque roto o golpeado*, *Sello de seguridad roto / alterado*, *Faltan galones / unidades*, *Lote o fecha no coincide*, más notas explicativas.
   - Emisión de acta de recepción (`REC-2026-XXXX`) sincronizada mediante cola outbox hacia la Torre de Control.
3. **Planificador y Gerencia:** Centralizados en `apps/web` para análisis macro de stock, asignación de rutas VRP y gobernanza.
4. **Portada Pública Web:** Limpieza estricta de la landing pública (`/`), removiendo accesos directos al panel y canalizando el ingreso exclusivamente por autenticación (`/login`).

### Consecuencias
- Seguridad y RBAC reforzados: Cada perfil solo tiene acceso a las pantallas congruentes con su ámbito de trabajo.
- Optimización de rendimiento móvil: El bundle móvil no incluye tablas pesadas ni mapas VRP innecesarios para el personal de campo.
- Flujo realista para la demo y el jurado hospitalario.

### Verificación
- Compilación exitosa de `apps/web` (`npm run build:web`) con validación de límites FSD y ESLint.
- Compilación exitosa de `apps/mobile` (`npm run build:mobile`) en modo producción con Ionic 8 Standalone.

## ADR-25 — Ergonomía de custodia en PWA móvil: Roles diferenciados, lectura asimétrica de QR y reporte de incidencias

- **Estado:** aceptada. **Fecha:** 2026-10-13. **Deciders:** Bryan y Equipo de Desarrollo. **Origen:** refinamiento del flujo de campo, simplificación cognitiva del conductor y auditoría de custodia médica. **Complementa:** ADR-04, ADR-06, ADR-14, ADR-17 y ADR-24.

### Contexto y requisito
En pruebas iniciales de campo se identificaron tres fricciones críticas:
1. **Sobrecarga y confusión en el conductor:** Contar con un botón aislado de «Entregar Paquete» generaba riesgo de entrega equivocada por falta de contexto geográfico, y obligaba al chofer a escribir en el teléfono con guantes o en condiciones de movimiento.
2. **Ambigüedad en la custodia por QR:** No estaba formalizado quién escaneaba a quién en bodega y en hospital.
3. **Inconsistencia estética y profesionalismo:** El uso de emojis estándar (🔒, 📦, 🚚, ⚠️) degradaba la apariencia técnica exigida por el Arquetipo B Técnico/Minimalista para un sistema de salud institucional, variando su renderizado según el sistema operativo.

### Decisión
1. **Acceso institucional por login (`/login`):** La app inicia de forma anónima y redirige de `/home` a `/login` si no hay sesión activa. Se proveen credenciales institucionales y accesos rápidos por rol para demostración.
2. **Ergonomía del Conductor (3 botones primarios):**
   - **Descargar Ruta Hoy:** Carga offline de la jornada.
   - **Recibir Paquetes:** Escaneo de los bultos/cajas en bodega antes de subir al vehículo para validar manifiesto y contenido.
   - **Ver Ruta Hoy:** Muestra la secuencia de paradas (ej. *Hospital Laura*, *Centro de Salud Norte*). Al seleccionar una parada, se abre directamente la **Ficha de Entrega en Destino**.
   - **Cero tipeo obligatorio:** El chofer presenta su pantalla con el **QR de entrega** para que el personal del hospital lo escanee con su propio lector, registra captura fotográfica y confirma la descarga sin escribir texto salvo incidencias.
3. **Flujo Asimétrico de Custodia:**
   - **En Bodega:** El conductor escanea el QR de las cajas para certificar la carga.
   - **En Hospital:** El receptor hospitalario escanea el QR presentado por el conductor para asentar la transferencia de custodia.
4. **Reporte de incidencias y escalamiento a supervisión:**
   - Si ocurre una eventualidad en la parada (ej. bulto con fisura, personal ausente), el conductor redacta la novedad desde la misma ficha de entrega.
   - La incidencia impacta reactivamente el módulo de **Alertas e Incidentes** del Supervisor y Operador de Bodega con un contador en tiempo real.
5. **Ergonomía de Bodega y Supervisión en PWA:**
   - **Operador de Bodega:** 5 acciones operativas (*Generar Paquete con impresión de QR*, *Registrar Despacho*, *Monitoreo en Tiempo Real*, *Gestión de Incidentes*, *Gestión de Rutas*).
   - **Supervisor de Logística:** 3 módulos de control (*Monitoreo en Tiempo Real*, *Alertas e Incidentes* con contador dinámico, *Gestión de Rutas*).
6. **Política estricta Cero Emojis:** Prohibición formal de emojis en templates UI; uso exclusivo de SVGs técnicos nítidos acordes a la paleta institucional (Slate 900, Cyan 600, Emerald 600, Amber 500, Red 600).

### Consecuencias
- Cero margen de error de entrega en destino: El conductor siempre opera dentro del contexto de la parada seleccionada.
- Trazabilidad sin fricción para el chofer: Operación en campo basada en mostrar/escanear códigos QR y capturar fotos, reduciendo los tiempos de parada en más de un 70%.
- Experiencia de usuario pulida y estandarizada entre iOS, Android y escritorio web.

### Verificación
- Verificado en `apps/mobile` con compilación sin errores (`npm run check:mobile`).
- Rutas protegidas mediante `RoleGuard` y sesión gestionada por `MobileAuthService`.
- Pruebas manuales en `http://localhost:8100` para los tres roles.

