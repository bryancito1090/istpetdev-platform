---
tipo: referencia
estado: vigente
actualizado: 2026-10-12
tags: [hackathon, equipo, ia]
---

# Hechos canónicos

Fuente única de los datos que el equipo y cualquier asistente de IA deben usar sin reinterpretar. Si un dato no está aquí o dice **PENDIENTE**, no se completa por suposición: se escribe `PENDIENTE` y se pregunta. Cada fila indica su fuente y su estado según [[Convenciones y estados]].

Certeza: ✅ confirmado con fuente · ⚠️ a validar (propuesta, supuesto o dato con contradicción abierta).

## Evento

| Dato | Valor | Fuente | Certeza |
|---|---|---|---|
| Evento | Hackathon Expo Clean Ecuador 2026 | Manual, p. 2 | ✅ |
| Reto | Reto 1 — Optimización y trazabilidad logística para el abastecimiento de insumos a nivel nacional | Manual, p. 19 | ✅ |
| Área | Gestión Inteligente de Procesos | Manual, p. 19 | ✅ |
| Capacitación | 8 de octubre de 2026, 09:00–13:00 | Manual, p. 27 | ✅ |
| Jornadas | 16 y 17 de octubre de 2026 | Manual, pp. 6 y 16 | ✅ |
| Zona horaria | America/Guayaquil (UTC−05:00) | Decisión del equipo en [[Cronograma oficial]] | ✅ |
| Sede | Centro de Exposiciones Quito (CEQ), según listado público de la feria; el manual solo dice «espacio definido por la organización» (p. 14) | [Listado público](https://www.cantonfair.net/event/67411-ecuador-cleaning-expo-2026) | ⚠️ confirmar en O-07 |
| Jurado | 7 personas: 3 del sector de limpieza (incluye JIMCORPSERVI/FEDELIMP), 2 expertos técnicos/tecnológicos, 1 académico, 1 de LACIF | Manual §11.3, p. 10 | ✅ |
| Pesos de evaluación | 15/15/15/15/20/10/10 (ver [[Evaluacion y entregables]]) | Manual, pp. 9 y 26 | ✅ |
| Trabajo previo permitido | PENDIENTE (consulta O-01) | [[Consultas para la organizacion]] | ⚠️ |
| Duración del pitch | PENDIENTE (consulta O-04); meta interna 3 min | [[Guion del pitch]] | ⚠️ |

## Equipo

| Dato | Valor | Fuente | Certeza |
|---|---|---|---|
| Nombre | IstpetDev | [[Informacion inicial del equipo]] | ✅ |
| Integrantes | 5, sin asignación fija de especialidades | [[Equipo y acuerdos]] | ✅ |
| Repositorio de la bóveda | `docs/boveda/` dentro de bryancito1090/istpetdev-platform (privado); repositorio anterior como referencia histórica | ADR-20; [[Repositorio de software y versiones]] | ✅ |
| Rama de trabajo | Ramas de trabajo mediante PR a `develop`; releases desde `develop` a `main` | ADR-20; README | ✅ |
| Repositorio de código | bryancito1090/istpetdev-platform (privado), con código y bóveda; carpeta local `~/Projects/ISTPETDEV` | ADR-20; [[Repositorio de software y versiones]] | ✅ |
| Skills de IA | Documentadas en [[Catalogo y plan de skills]]; los archivos no se versionan en la bóveda. Dónde viven para el código: PENDIENTE | C-01 | ⚠️ |

## Escenario de demostración

Estas cifras son propuesta del equipo, no datos de una empresa ni exigencias del manual.

| Dato | Valor | Fuente | Certeza |
|---|---|---|---|
| Puntos de servicio | 80 | [[Informacion inicial del equipo]] | ⚠️ propuesta |
| Horizonte simulado | 90 días | [[Simulador y metricas]] | ⚠️ propuesta |
| Caso hospital | 60 L disponibles, 12 L/día, lead time 2 días, margen 3 días → cobertura 5 días | [[Inventario y prediccion]] | ⚠️ ilustrativo |
| Fixture inicial | 6 puntos y 2 SKU | [[Dataset y escenarios]] | ⚠️ propuesta |
| Resultados medidos | Ninguno todavía | [[Plan de validacion]] | ✅ |
| Recorrido en vivo de la demo | ≈15 puntos sintéticos de Quito, 1 bodega, 2 vehículos (fixture `demo-region`) | D6 en [[Definiciones de producto y plan hasta el pitch]] | ⚠️ propuesta |
| Fixture de pruebas del importador | `demo-base`: 6 puntos, 2 SKU, 1 bodega, 1 vehículo | Spec 001 (`docs/specs/`) | ⚠️ propuesta |
| Congelamiento de funcionalidades | 15-oct-2026 14:00 | D8 en [[Definiciones de producto y plan hasta el pitch]] | ⚠️ propuesta |
| Simulador del spec 005 | API .NET asíncrona, base aislada, tres semillas base y una adversa; baselines reactivo y calendario fijo de 7 días con cantidad histórica fija | [[Simulador y metricas]]; [spec 005](../../specs/005-panel-simulador/spec.md) | ⚠️ propuesta; sin resultados medidos |

## Stack y versiones

Decisiones del 7 de octubre registradas por bryancito1090 (be0b127) en [[Repositorio de software y versiones]], [[Identidad OIDC y sesiones]] y [[Entornos y operacion acordados]]. Las filas con ⚠️ siguen siendo propuesta o tienen una contradicción abierta; no elegir una opción sin decisión registrada. Versiones exactas de paquetes: solo las de [[Repositorio de software y versiones]].

| Componente | Valor vigente | Decisión | Contradicción abierta | Certeza |
|---|---|---|---|---|
| Backend | C# con Clean Architecture y CQRS lógico | ADR-02 propuesta | — | ⚠️ |
| Versión .NET | .NET 8 (EF Core/Npgsql 8.x); soporte hasta 10-nov-2026, actualización posterior al evento | ADR-01 aceptada por Bryan | — | ✅ |
| Base de datos | PostgreSQL 16 + PostGIS 3.x compatible; UUID v7 generado en la aplicación | ADR-03 concretada | — | ✅ diseño, sin aprovisionar |
| Frontend web | Angular 22 standalone + FSD; TypeScript 6.0 | ADR-13 aceptada, ADR-04 (versión por Bryan) | — | ✅ |
| Móvil | PWA con Ionic 8 e IndexedDB primero; Capacitor 8 para el empaquetado nativo posterior | ADR-06 concretada | — | ✅ diseño |
| Tailwind | 3.4 | ADR-04 | — | ✅ |
| Mapas | Leaflet + OpenStreetMap en componente desacoplado `shared/ui/map-view`; sin costo ni cuotas | ADR-05 aceptada | — | ✅ |
| Arquetipo visual y UI | Arquetipo B — Técnico/Minimalista; Lucide Icons (`lucide-angular`), paleta oficial Expo Clean (Cyan `#00A7C4`, Lime `#9EC200`, Orange `#EA601F`) y tokens semánticos en `tailwind.config.js` ([[Sistema de diseno y colores]]) | ADR-04 ratificada | — | ✅ |
| Móvil offline y firma | PWA con IndexedDB (`idb`), `signature_pad` para firma táctil y UUID v7 | ADR-06, ADR-17 aceptadas | — | ✅ |
| Tiempo real | SignalR; Redis solo con varias réplicas | ADR-07 propuesta | — | ⚠️ |
| IA | Cálculo determinista; LLM (DeepSeek) solo explica; n8n en el servidor existente de Bryan. Propuesta del 12-oct: agente con herramientas y autonomía limitada, DeepSeek con prueba de aceptación (ADR-22) | ADR-08, ADR-22 | Papel del LLM (ver tabla de contradicciones) | ⚠️ propuesta |
| Identidad | Amazon Cognito (OIDC, Authorization Code + PKCE S256; AuthInterceptor en frontend) | ADR-10, ADR-18 definidas | — | ✅ |
| Autorización | RBAC administrable por organización: seis roles funcionales como seeds y ConfigurationAdmin administrativo propuesto; acciones implementadas en backend | ADR-16, solicitada por Bryan el 9-oct; [[RBAC y configuracion del sistema]] | Sustituye límite anterior de seis roles fijos; implementación pendiente | ✅ dirección aceptada; ⚠️ diseño físico propuesto |
| Base objetivo del sistema | 95 tablas de dominio aplicadas en `istpetdev_dev_01` mediante CompleteSystemSchema95, conservando InitialSchema; 95 ENABLE/FORCE RLS | [[Esquema completo de base de datos]]; evidencia del agente de software en [[Coordinacion migracion schema95]] | PR #1 como borrador; integridad RDS y aislamiento/PostGIS descartable aprobados, incluido cliente EF/Npgsql y pool secuencial; JWT/HTTP real, concurrencia, recorrido/aislamiento RDS, merge e integración pendientes | ✅ aplicación personal y pruebas según evidencia; ⚠️ validaciones funcionales pendientes |
| Plantillas y parámetros | Gestión en DB, versiones publicadas inmutables, formularios/roles/campos/ámbitos configurables | ADR-16; [[RBAC y configuracion del sistema]] | Condiciones y handlers permitidos; integridad/seguridad no desactivables por configuración | ✅ dirección aceptada; implementación pendiente |
| Hosting demo | Perfil A: EC2 + Docker Compose + Nginx, encendido solo para ensayos; región `us-east-1` | ADR-09 | — | ⚠️ ver Deciders |
| Desarrollo | Aplicaciones locales por integrante; RDS dev privado compartido (base por integrante + integración) | [[Entornos y operacion acordados]] | — | ✅ diseño, sin aprovisionar |
| Acceso AWS del equipo | IAM individual con MFA y `aws login` temporal, sin Organizations (escenario vigente) | [[AWS temporal para la hackathon y cierre]] | Sustituye a SSO de [[Credenciales y acceso del equipo]] durante la hackathon | ⚠️ propuesta |
| TLS | La configuración de ejemplo permite TLS 1.2 y 1.3 | [[Hardening y seguridad de servidores]] | Varias notas dicen «TLS 1.3»; solo 1.3 es PENDIENTE | ⚠️ |

## Estado de las decisiones (ADR)

«Registrado por» viene del historial de git; no prueba quién decidió. Los Deciders reales se confirman en reunión.

| Tema | ADR | Estado en la bóveda | Registrado por (commit) | Deciders |
|---|---|---|---|---|
| Versión backend | ADR-01 | Aceptada | bryancito1090 (765c532, be0b127) | Bryan |
| Topología | ADR-02 | Propuesta | bryancito1090 (765c532) | — |
| Motor e IDs | ADR-03 | Diseño concretado | bryancito1090 (765c532, be0b127) | Bryan |
| Stack web/móvil | ADR-04 | Aceptada; ratificada con Arquetipo B e iconos | JorgeDoicela (4c3a9ef), bryancito1090 (be0b127) | Jefe de Frontend y Bryan |
| Mapas | ADR-05 | Aceptada (Leaflet + OpenStreetMap) | bryancito1090 (765c532), 2026-10-11 | Jefe de Frontend y Bryan |
| Offline | ADR-06 | Diseño concretado | bryancito1090 (765c532, be0b127) | Bryan |
| Tiempo real | ADR-07 | Propuesta | bryancito1090 (765c532) | — |
| IA | ADR-08 | Propuesta | bryancito1090 (765c532) | — |
| Cloud y hosting | ADR-09 | Aceptada | JorgeDoicela (4c3a9ef) | PENDIENTE |
| Identidad | ADR-10 | Definida (delegada) | bryancito1090 (be0b127) | Delegada por Bryan |
| Auditoría | ADR-11 | Propuesta | bryancito1090 (765c532) | — |
| Repositorio | ADR-12 | Reemplazada por ADR-20; acuerdo del 7-oct conservado | bryancito1090 (765c532, be0b127) | Bryan |
| Repositorio unificado | ADR-20 | Aceptada el 12-oct-2026 | Instrucción directa de Bryan | Bryan |
| Frontend FSD | ADR-13 | Aceptada «por el equipo» | bryancito1090 (765c532) | PENDIENTE confirmar nombres |
| RBAC | ADR-14 | Aceptada | bryancito1090 (5905073) | PENDIENTE confirmar nombres |
| CI/CD y hardening | ADR-15 | Aceptada | JorgeDoicela (4c3a9ef) | PENDIENTE |
| Configuración y plantillas desde DB | ADR-16 | Aceptada la dirección, diseño propuesto | Instrucción directa, 2026-10-09 | Bryan |
| Móvil offline y firma | ADR-17 | Aceptada (IndexedDB + signature_pad) | 2026-10-11 | Jefe de Frontend y Bryan |
| Cliente OIDC Cognito | ADR-18 | Aceptada (PKCE S256 + AuthInterceptor) | 2026-10-11 | Jefe de Frontend y Bryan |
| Núcleo delgado para la demo | ADR-21 | Propuesta (difiere ADR-16 a P2) | Sesión de definición del 12-oct | PENDIENTE (incluir a Bryan) |
| Autonomía del agente | ADR-22 | Propuesta (complementa ADR-08) | Sesión de definición del 12-oct | PENDIENTE |
| Motor logístico y distancias | ADR-23 | Propuesta (complementa ADR-05) | Sesión de definición del 12-oct | PENDIENTE |

## Nombres en código

Tomados de [[Modelo de datos]] y [[Contratos API y eventos]]. Ningún asistente debe crear sinónimos.

| Término en la bóveda | Nombre en código | Fuente |
|---|---|---|
| Organización | `Organization` | Modelo de datos |
| Cuenta | `Account` | Modelo de datos |
| Acceso por organización | `OrganizationAccess` | Modelo de datos |
| Punto de servicio | `ServicePoint` | Modelo de datos |
| Ubicación | `Location` | Modelo de datos |
| Producto / SKU | `Product` | Modelo de datos |
| Lote | `Lot` | Modelo de datos |
| Unidad logística (caja con QR) | `HandlingUnit` | Modelo de datos |
| Movimiento | `InventoryMovement` | Modelo de datos |
| Saldo | `StockBalance` | Modelo de datos |
| Reserva | `Reservation` | Modelo de datos |
| Consumo | `Consumption` | Modelo de datos |
| Solicitud de reposición | `ReplenishmentRequest` | Modelo de datos |
| Vehículo | `Vehicle` | Modelo de datos |
| Plan de ruta / parada | `RoutePlan` / `RouteStop` | Modelo de datos |
| Entrega / línea | `Delivery` / `DeliveryLine` | Modelo de datos |
| Recepción / línea | `Receipt` / `ReceiptLine` | Modelo de datos |
| Evidencia | `Evidence` | Modelo de datos |
| Evento de custodia | `CustodyEvent` | Modelo de datos |
| Snapshot de riesgo | `RiskSnapshot` | Modelo de datos |
| Incidente | `Incident` | Modelo de datos |
| Conversación / mensaje | `Conversation` / `Message` | Modelo de datos |
| Registro de idempotencia | `IdempotencyRecord` | Modelo de datos |
| Evento outbox | `OutboxEvent` | Modelo de datos |
| Ejecución de simulación | `SimulationRun` | Modelo de datos |
| Roles, permisos y ámbitos administrables | `Role`, `Permission`, `RolePermission`, `AccessRole`, `AccessLocation`, `AccessServicePoint`, `DeliveryAssignment` | Esquema completo de base de datos |
| Definiciones y versiones de configuración | `ConfigurationDefinition`, `ConfigurationVersion`, `ConfigurationActivation`, `Catalog`, `CatalogOption` | Esquema completo de base de datos |
| Plantillas y capturas | `Template`, `TemplateVersion`, `TemplateField`, `TemplateFieldOption`, `TemplateFieldAccess`, `TemplateBinding`, `FormSubmission`, `FormAttachment` | Esquema completo de base de datos |
| Procesos configurables | `WorkflowDefinition`, `WorkflowVersion`, `WorkflowState`, `WorkflowTransition` | Esquema completo de base de datos |
| Resto de tablas del diseño ampliado | Nombres de las filas del catálogo físico; una fuente única, sin sinónimos nuevos | Esquema completo de base de datos |
| API base | `/api/v1` | Contratos API y eventos |
| Hub en tiempo real | `/hubs/operations` | Contratos API y eventos |
| Lote de importación | `ImportBatch` | ADR-21, spec 001 (⚠️ propuesta) |
| Decisión del agente logístico | `AgentDecision` | ADR-22, spec 001/002 (⚠️ propuesta) |
| Caché de tramos viales | `RoadMatrixEdge` (indexada por coordenadas) | ADR-23, spec 002 (⚠️ propuesta) |
| Ejecución del agente | `AgentRun` | ADR-22, spec 003 (⚠️ propuesta) |

## Rutas, puertos y comandos

| Dato | Valor | Certeza |
|---|---|---|
| Estructura del repositorio de código | `src/` (Api, Application, Domain, Infrastructure), `apps/web`, `apps/mobile`, `libs/shared-core`, `infra/`, `automation/n8n/workflows/`, `scripts/`, `tests/`; bóveda en `docs/boveda/`; árbol completo en [[Repositorio de software y versiones]]. Los filtros de CI usan `src/**`, no `backend/**` | ✅ estructura acordada; código en el repositorio unificado |
| Puertos internos (ejemplo, solo loopback) | API 5000, PostgreSQL 5432, Redis 6379, n8n 5678 | ⚠️ propuesta |
| Comando de verificación de la bóveda | `python docs/boveda/.github/scripts/check_vault.py` | ✅ |
| Comandos de build, test y despliegue del código | Consultar README.md y package.json de la plataforma; ejecución no comprobada durante el traslado documental | ⚠️ validación fuera del alcance del traslado |

## Contradicciones abiertas

Cada una sigue el protocolo de [[Convenciones y estados]]: no se corrige en silencio; se decide y se registra.

| Contradicción | Opciones | Decide |
|---|---|---|
| Mapas | Mapbox o Google Routes (ADR-05); «Frontend y componentes» lista Mapbox GL JS / Leaflet | ADR-05 |
| TLS | 1.2 + 1.3 o solo 1.3 | ADR-15 |
| Acceso AWS | SSO ([[Credenciales y acceso del equipo]]) o IAM + MFA temporal ([[AWS temporal para la hackathon y cierre]], marcado como vigente) | Bryan |
| Archivos de skills para el código | Versionarlos en istpetdev-platform o mantenerlos locales | C-01 |
| Autoría de ADR-04 (aceptación inicial), 09, 13, 14 y 15 | Confirmar Deciders | Reunión del equipo |
| Alcance de RBAC/configuración para la demo | RF-17/18/19 y RNF-13 en P0 (ADR-16) o diferidos a P2 con núcleo delgado (ADR-21) | Equipo, con Bryan como Decider de ADR-16 |
| Trabajo previo permitido | Un integrante informó el 12-oct que se puede llevar trabajo hecho; O-01 sigue sin respuesta registrada de la organización | Quien recibió la respuesta, registrándola en O-01 |
| Papel del LLM | Solo explica (ADR-08) o propone y orquesta con autonomía decidida por reglas deterministas (ADR-22) | Equipo |
| Scopes de automatización | Cuatro scopes finos ([[Identidad OIDC y sesiones]]) o un scope `istpetdev-api/automation` + rol `Automatizacion` (spec 003) | Equipo |
| Distancias en línea recta | Solo en un escenario separado ([[Rutas y sobrecostos]]) o mezcladas por tramo con etiqueta en la operación, nunca en el simulador (ADR-23) | Equipo |
| Cierre de parada y QR público | El marcado provisional de 002 completa sin recibo, y la página actual consulta entregas o un registro fijo sin token; el spec 004 propone recepción real y consulta de `HandlingUnit` con token revocable; para el pitch y los recorridos de 002 y 003, el endpoint de demo `advance` completa paradas con operaciones reales | Equipo; ver [[Definiciones de producto y plan hasta el pitch]] |
| Token del QR público | Query string `?token=` ([[Contratos API y eventos]]) o fragmento de URL `#t=` y cabecera `X-Trace-Token` (spec 004, revisión del 12-oct) | Equipo |
| Tamaño del esquema de demo | El criterio de 001 cuenta 27 tablas antes de 004 (desde el 12-oct su prueba exige ese núcleo sin Schema95, no un conteo cerrado); la asociación de caja/entrega y el token público de 004 requieren migración posterior, frente al esquema ampliado de [[Esquema completo de base de datos]] | Equipo |
| **⚠️ Contradicción detectada** — Baselines del 005 | Hecho nuevo: `reactive-v1` y `fixed7-v1` fijan aviso, frecuencia y cantidades antes de correr; hecho establecido: [[Simulador y metricas]]/D7 solo nombraban dos baselines y [[Rutas y sobrecostos]] pedía frecuencia razonable; impacto: el resultado depende de estos parámetros. Opciones: aceptar/versionar reglas propuestas; esperar proceso empresarial; recalibrar después (sesgaría comparación). Propuesta: aceptar/versionar y declarar sintético | Equipo (Deciders pendientes) |
| **⚠️ Contradicción detectada** — Cifras del panel | Hecho nuevo: spec 005 elimina valores, rutas y narración fijos del widget; hecho establecido: código actual muestra ahorro y telemetría sin medición, frente a [[Simulador y metricas]]; impacto: credibilidad del pitch. Opciones: maqueta etiquetada o datos reales de API. Propuesta: API con estado vacío; R-15 sigue abierto hasta probarlo | Equipo |
| **⚠️ Contradicción detectada** — Lecturas para panel | Hecho nuevo: `GET /api/v1/route-plans` en 002 y `GET /api/v1/deliveries` en 004; hecho establecido: solo había ruta individual y consulta móvil; impacto: sin listas, el panel requiere IDs incrustados. Opciones: lecturas en productores, agregador 005 o IDs fijos. Propuesta: lecturas en specs productores antes de H4 | Equipo de 002/004 |
| **⚠️ Contradicción detectada** — Reset y evidencia del 005 | Hecho nuevo: los resultados completados deben seguir exportables tras reiniciar `demo-region`; hecho establecido: la tarea T7 de 001 borraba `SimulationMetric`/`SimulationRun` en el reset; impacto: se pierde la evidencia del pitch. Opciones: conservar completados, exportar/borrar, prohibir reset. Propuesta: conservar resultados completados; FR-15 y T7 de 001 corregidos documentalmente, E2E del 005 lo verifica | Equipo de 001/005 |

Cerradas el 7 de octubre por be0b127: versión .NET (8), PostgreSQL (16), Angular (22) y Capacitor (8), móvil (PWA primero), identidad (Cognito), repositorio de código y rutas (`src/**`).

Para modificar esta nota: cambiar primero la nota fuente o el ADR y después esta tabla. Ver [[Mapa de la boveda]].
