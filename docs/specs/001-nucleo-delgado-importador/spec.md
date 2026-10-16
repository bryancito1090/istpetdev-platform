---
id: "001"
titulo: Núcleo delgado e importador genérico
estado: en curso
creado: 2026-10-12
actualizado: 2026-10-13
responsable: Anthony Pilatasig y Joseph Baño   # asignación de Joseph Baño, 13-oct
aprobado_por: PENDIENTE     # requiere acuerdo del equipo; afecta trabajo de Schema95 (ver Contradicciones)
depende_de: []
boveda:
  - 00 Inicio/Hechos canonicos.md
  - 02 Producto/Alcance y prioridades.md
  - 02 Producto/Requisitos y aceptacion.md
  - 03 Arquitectura/RBAC y configuracion del sistema.md
  - 03 Arquitectura/Contratos API y eventos.md
  - 04 Datos y algoritmos/Modelo de datos.md
  - 04 Datos y algoritmos/Dataset y escenarios.md
  - 04 Datos y algoritmos/Esquema completo de base de datos.md
---

# 001 — Núcleo delgado e importador genérico

## Contexto

El backend tiene hoy un esquema de 95 tablas (Schema95) con RLS forzado, RBAC administrable, plantillas, workflows y configuración versionada desde la base de datos. Es un diseño sólido para un producto multiempresa, pero a cuatro días del evento ninguna capacidad P0 del flujo logístico está construida ([Backlog](../../boveda/06%20Equipo/Backlog.md): B-01 a B-10 pendientes). Cada pantalla nueva exige sembrar organización, cuenta, membresía, rol, permiso y ámbito, y abrir contexto RLS en cada petición. Además, el despliegue de demo conecta la API con el rol migrador, que tiene una política RLS `USING (true)`, así que el aislamiento no se aplica en producción.

El segundo problema es el dataset del evento. La ficha final del reto se presenta el 16 de octubre a las 09:30 ([Cronograma oficial](../../boveda/01%20Oficial/Cronograma%20oficial.md)) y puede traer datos con otras columnas, otro separador, otras unidades u otra granularidad. Si nuestros datos de demo están escritos a mano en el código o en un seed rígido, ese cambio invalida el trabajo previo.

Este spec reduce el esquema al núcleo que la demo necesita y crea una **única puerta de entrada de datos** (importador canónico con perfiles de mapeo) que usan por igual nuestro dataset de demo, el generador sintético futuro y cualquier dataset que entregue la organización.

## Objetivo y aporte a la nota

**Objetivo:** que cualquier dataset tabular de una red de abastecimiento (puntos, productos, inventario, consumo, flota) quede cargado y consultable en minutos, sin escribir código, sobre un esquema que los demás specs puedan usar sin fricción.

| Criterio | Peso | Cómo lo sostiene este spec |
|---|---:|---|
| Prototipo/demo | 20% | Habilita 002–005: sin núcleo de datos no hay demo. El reset de demo deja el escenario listo en un clic antes de cada ensayo |
| Viabilidad técnica | 15% | «Cargamos sus datos tal como vienen»: demostrable en vivo con el dataset del evento. Arquitectura simple y explicable |
| Comprensión del problema | 15% | Permite trabajar con los datos que entregue la organización o una empresa del sector en lugar de datos inventados |
| Proceso y equipo | 10% | Simulacro de dataset sorpresa registrado como evidencia de preparación (G6) |

## Historias de usuario

### H1 — Cargar un dataset desconocido (P1)

**Como** planificador, **quiero** subir los archivos que me entregan (CSV o Excel) y declarar cómo se llaman sus columnas, **para** trabajar con datos reales sin pedir cambios de código.

**Por qué esta prioridad:** es el guardarraíl contra el dataset sorpresa del evento (G2, G6).

**Escenarios de aceptación:**

1. **Dado** un CSV con columnas en español, separador `;` y decimales con coma, **cuando** lo subo con un perfil de mapeo, **entonces** los puntos, productos y consumos quedan cargados con las cantidades correctas.
2. **Dado** un archivo con encabezados reconocibles (`Latitud`, `Código`, `Cantidad`), **cuando** lo subo sin perfil, **entonces** el sistema asocia las columnas por sinónimos y me dice cuáles asoció y cuáles ignoró.
3. **Dado** un consumo expresado en galones por mes, **cuando** el perfil declara escala `3.78541` y `period_days = 30`, **entonces** el consumo queda en litros y la demanda diaria se puede derivar sin cálculos manuales.
4. **Dado** un Excel `.xlsx`, **cuando** lo subo, **entonces** se lee la primera hoja (u otra declarada) con fechas y números correctos.

### H2 — Saber qué se cargó y qué se supuso (P1)

**Como** planificador, **quiero** un reporte por entidad con filas leídas, creadas, actualizadas y omitidas, errores con número de fila y los supuestos aplicados, **para** confiar en los datos y explicarlos al jurado.

**Escenarios de aceptación:**

1. **Dado** un archivo con filas inválidas, **cuando** hago una prueba en seco (`dryRun`), **entonces** veo cada error con fila, campo y mensaje en español, y no cambia nada en la base.
2. **Dado** un archivo de puntos sin ventana horaria, **cuando** lo importo, **entonces** el reporte indica «window_start: N filas sin valor → se usará la ventana global por defecto».
3. **Dado** un archivo con errores, **cuando** lo importo sin `allowPartial`, **entonces** no se aplica nada y la respuesta es 422 con el reporte; con `allowPartial` se aplican solo las filas válidas.

### H3 — Reiniciar la demo en un paso (P1)

**Como** integrante del equipo, **quiero** restaurar el escenario de demostración con una sola llamada, **para** ensayar y presentar siempre desde el mismo estado.

**Escenarios de aceptación:**

1. **Dado** un entorno con datos modificados, **cuando** ejecuto el reset de demo, **entonces** quedan exactamente los datos **operativos** del fixture `demo-base` y el reporte de importación correspondiente; el historial de simulaciones completadas se conserva para auditoría del 005.
2. **Dado** un entorno con la demo desactivada (`Demo:Enabled=false`), **cuando** llamo al reset, **entonces** responde 404.

### H4 — Acceso por roles simples (P1)

**Como** equipo, **queremos** que cada cuenta tenga un rol fijo (Gerencia, Planificador, Bodega, Conductor, Receptor o Automatización), **para** proteger la API sin la complejidad del RBAC administrable.

**Escenarios de aceptación:**

1. **Dado** un token válido de una cuenta con rol Planificador, **cuando** consulto `/api/v1/service-points`, **entonces** recibo 200.
2. **Dado** un token válido de una identidad sin cuenta registrada, **cuando** consulto cualquier endpoint protegido, **entonces** recibo 403.
3. **Dado** un token de Conductor, **cuando** intento importar datos o reiniciar la demo, **entonces** recibo 403.

### H5 — Esquema que los demás specs usan sin fricción (P1)

**Como** desarrollador de 002–005, **quiero** un esquema de unas 27 tablas sin RLS ni configuración dinámica, con los nombres de la bóveda, **para** construir funcionalidades consultando las tablas directamente.

**Escenarios de aceptación:**

1. **Dado** una base vacía, **cuando** arranca la API con `ApplyMigrations=true`, **entonces** se crean las tablas del núcleo, la organización demo y la extensión PostGIS, y se puede insertar un punto con coordenadas sin reiniciar.
2. **Dado** el esquema, **cuando** reviso las tablas, **entonces** no existen tablas de configuración, plantillas, workflows, RBAC granular, chat ni auditoría genérica.

## Requisitos

### Funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| FR-01 | El sistema DEBE definir un **formato canónico** de siete entidades importables: `warehouses`, `service_points`, `products`, `vehicles`, `lots`, `stock`, `consumptions`, con campos obligatorios, opcionales, tipos y sinónimos de encabezado. | Dataset y escenarios (campos) |
| FR-02 | El sistema DEBE leer **CSV** (separador `,` `;` tab o `\|`, detectado o declarado; UTF-8 o Latin-1) y **XLSX** (primera hoja o la declarada). | — |
| FR-03 | El sistema DEBE aceptar un **perfil de mapeo JSON** opcional con: columnas origen por campo canónico, constantes para campos ausentes, factores de escala, separador decimal, separador de campos, formatos de fecha y hoja. Un perfil con entidades o campos desconocidos se rechaza con la lista de errores. | — |
| FR-04 | Sin perfil, el sistema DEBE asociar columnas por **nombre canónico o sinónimo**, ignorando mayúsculas, tildes, espacios y signos. | — |
| FR-05 | El sistema DEBE interpretar decimales con coma o punto (inferidos o declarados), fechas en los formatos comunes (`yyyy-MM-dd`, `dd/MM/yyyy`, `yyyy-MM`, serial de Excel) y horas `HH:mm`. | Modelo de datos (cantidades) |
| FR-06 | El sistema DEBE **insertar o actualizar** por clave natural: código de bodega, punto, vehículo y SKU; (SKU, lote); (ubicación, lote) para saldos; (punto, SKU, fecha) para consumos. Reimportar el mismo archivo no duplica registros. | RNF-02 (espíritu de idempotencia) |
| FR-07 | Las claves de código DEBEN normalizarse (sin espacios extremos, en mayúsculas) para que `hos-001` y `HOS-001` sean el mismo punto. | — |
| FR-08 | Un saldo importado DEBE registrar un `InventoryMovement` de tipo `ImportAdjustment` por la diferencia con el saldo anterior, para que el saldo siga siendo conciliable. | RF-02 |
| FR-09 | Filas duplicadas en un mismo archivo: en entidades maestras gana la última con advertencia; en `stock` y `consumptions` se **suman** con advertencia. | — |
| FR-10 | Cada importación DEBE devolver un **reporte**: por entidad, archivo, filas leídas/creadas/actualizadas/omitidas, columnas asociadas e ignoradas y supuestos aplicados; y la lista de incidencias (fila, campo, severidad, código, mensaje en español), acotada a 500 con contador de las omitidas. El reporte incluye `status` (`Applied`/`DryRun`/`Rejected`) y, calculada en el plan en memoria, la sección `feasibility` (#209): puntos sin coordenadas no ruteables, bodegas sin flota activa, productos sin peso/volumen y déficit de stock por SKU, con veredicto determinista. | — |
| FR-11 | `dryRun=true` (valor por defecto) DEBE validar todo, incluidas las referencias, sin modificar datos operativos. | — |
| FR-12 | Con errores y sin `allowPartial`, no se aplica nada (respuesta 422). Con `allowPartial=true` se aplican las filas válidas. | — |
| FR-13 | Cada ejecución DEBE quedar registrada como `ImportBatch` con perfil, hash SHA-256 de los archivos, estado (`Applied`, `DryRun`, `Rejected`) y reporte. | Simulador y metricas (trazabilidad del dataset) |
| FR-14 | `POST /api/v1/imports` (multipart) DEBE aceptar archivos asociados a una entidad por el nombre del campo del formulario (clave canónica o alias en español) o por el nombre de archivo declarado en el perfil. | Contratos API y eventos (convenciones) |
| FR-15 | `POST /api/v1/demo/reset` DEBE borrar los datos operativos (no cuentas, historial de importaciones ni `SimulationRun`/`SimulationMetric` completados) e importar el fixture configurado en una transacción; solo existe con `Demo:Enabled=true`. El 005 conserva también sus `SimulationEvent`/exportaciones completadas al integrarse. | Dataset y escenarios (reinicio de demo), spec 005 |
| FR-16 | El repositorio DEBE incluir el fixture `data/fixtures/demo-base` (6 puntos, 2 SKU, 1 bodega, 1 vehículo, incluido el caso 60 L / 12 L por día) y el fixture `data/fixtures/ejemplo-externo` (formato distinto con perfil de mapeo). | Hechos canonicos (fixture inicial 6 puntos/2 SKU; caso hospital) |
| FR-17 | La API DEBE autorizar por **rol fijo** almacenado en `Account.Role` y resuelto por `(issuer, subject)` del token; una identidad sin cuenta activa no tiene rol. | ADR-14 (roles funcionales), Identidad OIDC |
| FR-18 | Las cuentas de la demo DEBEN declararse en configuración (`Demo:Accounts`) y sembrarse al arrancar. | — |
| FR-19 | El esquema DEBE reducirse a las entidades listadas en «Entidades y contratos»; todas conservan `OrganizationId` con una organización demo fija. | Modelo de datos |
| FR-20 | `GET /api/v1/service-points` DEBE seguir respondiendo con el contrato actual de `@istpetdev/shared-core` (`id, code, name, type, latitude, longitude, isActive`), con `type` = categoría importada. | — |

### No funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| NFR-01 | Importar el fixture `demo-base` completo tarda menos de 5 s en un portátil del equipo; un archivo de 10 000 filas de consumo, menos de 30 s. | RNF-10 |
| NFR-02 | El código no depende de ICU: el contenedor corre con `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`. | — |
| NFR-03 | Mensajes de error al usuario en español, con la acción que lo corrige. | RNF-09 |
| NFR-04 | Cantidades `numeric(18,3)` con unidad base del SKU; saldos no negativos por restricción de base de datos. | Modelo de datos (cantidades e invariantes), RNF-03 |
| NFR-05 | Pruebas automatizadas: unitarias sin Docker; integración y E2E con PostgreSQL 16 + PostGIS 3.4 en Testcontainers; todas corren en `ci.yml`. | — |

## Entidades y contratos

**Esquema del núcleo (27 tablas).** Los nombres siguen [Hechos canonicos](../../boveda/00%20Inicio/Hechos%20canonicos.md); `ImportBatch` y `AgentDecision` son nombres nuevos que se registrarán allí al aprobar.

| Grupo | Entidades | Uso |
|---|---|---|
| Identidad | `Organization`, `Account` (con `Role`) | Organización demo fija; cuentas con rol fijo |
| Red | `Location` (bodega, punto, vehículo), `ServicePoint`, `Vehicle` | Importador, ruteo (002), mapa |
| Catálogo e inventario | `Product`, `Lot`, `StockBalance`, `InventoryMovement`, `Consumption` | Importador, riesgo (002), simulador (005) |
| Custodia | `HandlingUnit`, `HandlingUnitLine`, `CustodyEvent` | QR y custodia (004) |
| Planificación y entrega | `RoutePlan`, `RouteStop`, `Delivery`, `DeliveryLine`, `Receipt`, `ReceiptLine`, `Evidence` | 002, 004 |
| Riesgo e incidentes | `RiskSnapshot`, `Incident` | 002, 003 |
| Agente | `AgentDecision` | 003 |
| Operación | `IdempotencyRecord`, `ImportBatch` | 004, este spec |
| Simulación | `SimulationRun`, `SimulationMetric` | 005 |

**Contrato del importador:**

| Método y ruta | Rol | Entrada | Salida |
|---|---|---|---|
| `POST /api/v1/imports?dryRun=true&allowPartial=false` | Gerencia, Planificador | `multipart/form-data`: archivos + campo opcional `profile` (JSON) | 200 reporte; 422 reporte si no se aplicó; 400 perfil o archivos inválidos |
| `POST /api/v1/demo/reset` | Gerencia, Planificador | — | 200 reporte; 422 si el fixture falla; 404 si la demo está desactivada |
| `GET /api/v1/service-points` | Cualquier rol | — | Lista sin cambios de contrato |

**Formato canónico** (campo obligatorio en negrita; el detalle de sinónimos y valores por defecto está en `plan.md`):

| Entidad | Campos |
|---|---|
| `warehouses` | **code**, **name**, **lat**, **lon** |
| `service_points` | **code**, **name**, lat, lon, region, category, criticality, window_start, window_end, service_minutes, lead_time_days |
| `products` | **sku**, **name**, **base_unit**, weight_per_unit, volume_per_unit |
| `vehicles` | **code**, **depot_code**, **max_weight**, max_volume, cost_per_km |
| `lots` | **sku**, **lot_code**, expires_on, origin |
| `stock` | location_kind, **location_code**, **sku**, lot_code, **quantity** |
| `consumptions` | **point_code**, **sku**, **date**, **quantity**, period_days |

## Guardarraíles aplicados

| Guardarraíl | Aplicación en este spec |
|---|---|
| G1 | El fixture vive en `data/fixtures/`, no en código ni en migraciones (solo la organización demo es un seed). |
| G2 | El reset de demo usa el mismo importador que la API pública. |
| G3 | Valores por defecto declarados en el esquema canónico y listados como supuestos en el reporte. |
| G4 | `base_unit` obligatorio; escala por campo en el perfil. |
| G6 | Escenario E2E-03 (simulacro cronometrado con dataset preparado por otra persona). |
| G7 | Sin entidad «hospital»: `category` y `criticality` son datos. |

## Fuera de alcance

- Editor de perfiles de mapeo en la web (se escriben a mano en JSON; la web de 005 podrá subir archivos).
- Importar rutas, entregas, recepciones o incidentes (se generan en el sistema).
- Geocodificar direcciones sin coordenadas (el punto queda importado y marcado como no ruteable).
- Archivos `.xls` antiguos, PDF o JSON (se guardan como `.xlsx` o `.csv`).
- RLS, RBAC administrable, plantillas, workflows, configuración versionada en base de datos, chat y auditoría genérica.
- Corregir el uso del rol migrador en producción más allá de lo necesario: sin RLS, el riesgo de aislamiento desaparece; separar migrador y rol de ejecución queda como mejora posterior.

## Contradicciones con la bóveda

> **⚠️ Contradicción detectada** — Hecho nuevo: RBAC administrable, plantillas y parámetros/workflows desde DB pasan a P2 (fuera de la demo). · Hecho establecido: RF-17, RF-18 y RF-19 son P0 ([Requisitos y aceptacion](../../boveda/02%20Producto/Requisitos%20y%20aceptacion.md)); ADR-16 aceptada por Bryan el 9-oct ([RBAC y configuracion del sistema](../../boveda/03%20Arquitectura/RBAC%20y%20configuracion%20del%20sistema.md), [Alcance y prioridades](../../boveda/02%20Producto/Alcance%20y%20prioridades.md) «Ampliación solicitada»). · Impacto: libera la mayor fuente de fricción; el jurado no ve estas funciones. · Opciones: (a) diferir a P2 con roles fijos (propuesta); (b) mantener P0 y construir la demo encima; (c) mantener las 95 tablas sin usarlas. · Propuesta: (a), registrada como ADR-21 «Núcleo delgado para la demo», con Deciders a confirmar (debe incluir a Bryan por ser Decider de ADR-16).

> **⚠️ Contradicción detectada** — Hecho nuevo: sin RLS; aislamiento por organización solo como columna `OrganizationId` con una organización fija. · Hecho establecido: RNF-13 (P0) exige RLS/FORCE y rol API sin bypass; Hechos canonicos registra 95 tablas con RLS como «base objetivo». · Impacto: se pierde la garantía multiempresa, que la demo no necesita (una sola empresa). · Opciones: (a) diferir RNF-13 a P2 conservando el diseño en el tag `schema95-archivo` y la documentación; (b) mantener RLS. · Propuesta: (a). En el pitch se presenta como diseño listo para la versión multiempresa.

> **⚠️ Contradicción detectada** — Hecho nuevo: autorización por rol fijo por cuenta. · Hecho establecido: RNF-01 y [Contratos API y eventos](../../boveda/03%20Arquitectura/Contratos%20API%20y%20eventos.md) exigen permiso por acción y ámbito del recurso. · Impacto: los ámbitos por asignación (conductor solo ve sus entregas) se implementan en 004 con `Delivery.AssignedAccountId`, no con tablas de concesiones. · Propuesta: volver a los seis roles funcionales de ADR-14 sin editor; el rol se guarda en la base (no en un grupo de Cognito), lo que respeta la regla de [Identidad OIDC y sesiones](../../boveda/03%20Arquitectura/Identidad%20OIDC%20y%20sesiones.md) de no confiar en roles enviados por el cliente.

> **⚠️ Contradicción detectada** — Hecho nuevo: un integrante informó el 12-oct que se puede llevar trabajo hecho al evento. · Hecho establecido: «Trabajo previo permitido: PENDIENTE (consulta O-01)» en Hechos canonicos y [Consultas para la organizacion](../../boveda/01%20Oficial/Consultas%20para%20la%20organizacion.md). · Impacto: define cómo se presenta este trabajo. · Propuesta: registrar la respuesta con su fuente (quién de la organización lo confirmó y cuándo) antes de cerrar O-01; mantener G8 en cualquier caso.

Notas revisadas sin contradicción: Contratos API y eventos (prefijo `/api/v1`, camelCase, Problem Details), Modelo de datos (nombres, cantidades decimales, saldo no negativo), Dataset y escenarios (fixture 6 puntos/2 SKU, reinicio de demo), Inventario y prediccion (caso 60 L / 12 L por día), Frontend con Feature-Sliced Design (sin cambios de contrato web).

## Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| El equipo no acepta archivar Schema95 | Media | Alto: bloquea 002–005 | Decidir el 13-oct a primera hora; el tag conserva todo y la documentación sigue en la bóveda |
| Las bases dev/integración en RDS tienen Schema95 aplicado y la nueva migración choca | Alta | Medio | Recrear las bases con `scripts/base_datos` (datos sintéticos); confirmación explícita antes de borrar (Task 4) |
| El dataset del evento trae una entidad no canónica (p. ej., pedidos o tarifas) | Media | Medio | Las columnas extra se reportan como ignoradas; se decide en el evento si se añade una entidad. El formato canónico cubre lo que piden RF-01 a RF-04 |
| La organización entrega datos agregados (totales mensuales) | Media | Bajo | `period_days` y escala en el perfil |
| Testcontainers no funciona en alguna máquina | Baja | Bajo | Las pruebas unitarias no usan Docker; CI corre las de integración |

## Criterios de éxito

- CE-01: con una base vacía, `POST /api/v1/demo/reset` deja 1 bodega, 6 puntos, 2 productos, 1 vehículo, 2 lotes, 9 saldos y 7 consumos, y responde en menos de 5 s.
- CE-02: el fixture `ejemplo-externo` (separador `;`, decimales con coma, galones por mes, encabezados con tildes) se importa con su perfil y deja el consumo de `C-100` en 359,614 L con `period_days = 30`.
- CE-03: un dataset preparado por alguien ajeno al importador se carga en ≤ 60 minutos escribiendo solo un perfil de mapeo (E2E-03).
- CE-04: al cerrar 001, el esquema contiene las 27 tablas de «Entidades y contratos» (más `__EFMigrationsHistory` y las tablas de PostGIS) y ninguna tabla eliminada de Schema95. La prueba automática exige ese núcleo y la ausencia de Schema95, no un conteo cerrado: los specs 002–004 añaden tablas con sus propias migraciones sin romperla.
- CE-05: `dotnet test` pasa completo en `ci.yml` y el paso Schema95Check desaparece de `deploy.yml`.

## Preguntas abiertas

- PA-01: ¿Aprueba el equipo archivar Schema95 (opción «esquema delgado»)? Decide: equipo, con Bryan como Decider de ADR-16. **Bloquea el paso a `aprobado`.**
- PA-02: ¿Quién de la organización confirmó el trabajo previo y cuándo? Decide: quien recibió la respuesta; se registra en O-01.
