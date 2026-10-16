---
tipo: nota-evento
estado: pendiente
actualizado: 2026-10-16
tags: [hackathon, evento, notas, roles, ruteo, productos, dijkstra]
---

# Nota del evento — Flexibilidad operativa, control de productos y ruteo con Dijkstra y DeepSeek

- **Registró:** Anthony Pilatasig (`ap`)
- **Registrado:** 2026-10-16 11:15 (America/Guayaquil)
- **Ocurrió:** 2026-10-16 11:00 (America/Guayaquil)
- **Contexto:** Hackathon Expo Clean Ecuador 2026. Revisión de flexibilidad entre automatización por defecto e intervención humana en parámetros de productos, costos vehiculares, zonas logísticas y algoritmos de optimización de rutas.
- **Fuente del apunte:** dictado de un integrante / requerimiento operativo de equipo

---

## Apunte transcrito

«Revisar hasta qué punto tienen los roles accesos a los productos para manejo de promociones, valores, manejo de costos en envío y volúmenes de los productos. Agregar a las historias de usuario de notas del evento porque necesitamos que el sistema sea flexible para que sea automático pero puedan meter mano en productos, manejo de volúmenes, acceso a los costos de vehículos, zonas de repartos más usuales y uso de DeepSeek para que la ruta con Dijkstra maneje mejor los trayectos de los vehículos.»

---

## Resumen operativo

Se requiere un modelo híbrido de operación («Human-in-the-Loop»): la plataforma debe sugerir planes de abastecimiento y rutas de forma automatizada (mediante cálculo de riesgo determinista, orquestación de n8n y análisis de DeepSeek), pero otorgando a los roles operativos la capacidad explícita de intervenir, calibrar y sobrescribir («meter mano») en cuatro áreas críticas:
1. **Catálogo y cubicaje:** Modificación manual de volúmenes de empaque, peso y parámetros comerciales/valores/promociones por producto.
2. **Parámetros de flota:** Control y edición de costos por kilómetro, costo horario y capacidades físicas de cada vehículo.
3. **Zonificación logística:** Configuración y asociación de puntos a zonas de reparto habituales (clusters urbanos/regionales) para guiar la asignación de flota.
4. **Ruteo asistido:** Empleo de Dijkstra sobre la matriz vial dirigida para el cálculo de distancias mínimas y desvíos, complementado con DeepSeek para el razonamiento contextual de paradas y explicación de restricciones, manteniendo la aprobación final bajo el control del Planificador.

---

## Hallazgos y análisis del estado actual del sistema

### 1. Nivel de acceso y capacidades de los roles sobre productos y valores
- **Entidad `Product` actual:** Contiene `Sku`, `Name`, `BaseUnit`, `WeightPerBaseUnit`, `VolumePerBaseUnit` y `IsActive` (ver [[Esquema completo de base de datos]]).
- **Manejo de valores y promociones:** El modelo actual de la hackathon está orientado a logística institucional de abastecimiento crítico (SERCOP / salud), por lo que **no dispone de campos nativos de precio comercial, lista de tarifas de venta ni módulos de promociones o descuentos**. Toda adición de estos atributos debe incorporarse como extensión comercial o mediante campos configurables (ver [[RBAC y configuracion del sistema]]).
- **Costos de envío:** No se calcula una tarifa de flete facturable al cliente; el sistema calcula el **costo operativo interno de transporte** compuesto por `distancia_km * CostPerKm + duracion_horas * DriverHourlyCostUsd` asociado a la entidad `Vehicle` y a la configuración de logística (ver [[Rutas y sobrecostos]]).
- **Volumen de productos:** Aunque `VolumePerBaseUnit` existe en base de datos, actualmente no existen endpoints de mantenimiento interactivo (CRUD) en la API web: la carga depende de la importación masiva por lotes ([[Usuarios y flujos]]). Los roles de `Planificador` y `Bodega` carecen de una pantalla para ajustar tolerancias volumétricas o factor de estiba cuando un bulto físico difiere de la ficha teórica.
- **Separación de roles frente a productos:**
  - `Planificador` y `Gerencia`: Tienen capacidad de importar datasets (`POST /imports`). Requieren pantalla de edición rápida de parámetros logísticos.
  - `Bodega`: Solo consulta catálogo y empaca unidades (`HandlingUnit`); requiere permiso para registrar observaciones de cubicaje discrepante.
  - `Auditor`: Dispone de visibilidad de lectura para trazabilidad.
  - `Conductor` y `Receptor`: No modifican catálogo; operan en la entrega y recepción en campo.

---

## Historias de usuario para el evento

### Historia HU-EVT-01: Override manual y parametrización de productos, volúmenes y valores
- **Prioridad:** P1
- **Rol:** Planificador / Gerencia
- **Necesidad:** Como Planificador logístico, quiero consultar y ajustar manualmente el volumen unitario (`VolumePerBaseUnit`), peso y valores de referencia de un producto, para corregir discrepancias de cubicaje real en bodega o aplicar condiciones especiales de reabastecimiento sin tener que regenerar todo el dataset de importación.
- **Contexto:** La bodega detecta que un lote de insumos llegó en cajas más voluminosas que el estándar, lo que provocaría que un vehículo asignado sature su capacidad volumétrica antes de tiempo.
- **Criterios de Aceptación:**
  - **Dado** un producto registrado en el catálogo, **cuando** el Planificador ingresa a la vista de detalle del producto y actualiza el campo de volumen por unidad base o su valor/costo de referencia, **entonces** el sistema valida que el valor sea numérico positivo, registra la versión del cambio con auditoría (`ActorAccountId`, fecha y motivo) y actualiza el valor efectivo.
  - **Caso sin datos/error:** Si se introduce un volumen igual o menor a cero, la interfaz y la API rechazan la operación indicando error de validación.
  - **Autorización:** Solo permitido para cuentas con rol `Planificador` o `Gerencia`. Usuarios con rol `Bodega` pueden proponer sugerencia de cambio, pero no publicar directamente.
  - **Efecto en ruteo:** El nuevo volumen es considerado de inmediato en las siguientes evaluaciones de capacidad de flota (`VehicleMatcher`).

---

### Historia HU-EVT-02: Calibración operativa de costos y capacidades de vehículos
- **Prioridad:** P1
- **Rol:** Planificador / Gerencia
- **Necesidad:** Como Planificador de transporte, quiero calibrar los costos por kilómetro (`CostPerKm`), costos fijos por hora y capacidades máximas (`MaxVolume`, `MaxWeight`) de cada vehículo activo, para que los cálculos de costo estimado de ruta reflejen la realidad operativa de la flota y el consumo de combustible.
- **Contexto:** Cambios en el precio del combustible o mantenimiento de unidades alteran el costo variable por kilómetro.
- **Criterios de Aceptación:**
  - **Dado** un vehículo registrado en la organización, **cuando** el usuario autorizado edita el costo por kilómetro o la capacidad volumétrica útil, **entonces** la plataforma guarda los nuevos valores sin alterar retroactivamente las rutas ya cerradas o en tránsito.
  - **Autorización:** Restringido a `Planificador` y `Gerencia`. `Conductor` solo tiene acceso de lectura a las características del vehículo asignado.
  - **Verificación:** Al simular o calcular una nueva ruta, la métrica de costo proyectado utiliza los costos actualizados del vehículo.

---

### Historia HU-EVT-03: Zonificación logística habitual y agrupamiento de entregas
- **Prioridad:** P1
- **Rol:** Planificador
- **Necesidad:** Como Planificador, quiero definir y asociar puntos de servicio a zonas de reparto usuales (por ejemplo: Norte, Centro, Sur, Valles), para que el sistema agrupe paradas compatibles y evite dispersión geográfica ineficiente antes de optimizar los trayectos.
- **Contexto:** En ciudades como Quito, cruzar corredores viales en horas pico eleva los tiempos de servicio; agrupar por zona habitual estabiliza los despachos.
- **Criterios de Aceptación:**
  - **Dado** el listado de puntos de atención y centros de salud, **cuando** el Planificador filtra o agrupa por zona logística, **entonces** el motor de ruteo prioriza la asignación de un mismo vehículo a los puntos de dicha zona.
  - **Flexibilidad:** El usuario puede remover o añadir manualmente paradas fuera de la zona habitual antes de confirmar el plan.

---

### Historia HU-EVT-04: Ruteo optimizado con Dijkstra y razonamiento contextual de DeepSeek
- **Prioridad:** P0 / P1
- **Rol:** Planificador
- **Necesidad:** Como Planificador, quiero que el sistema calcule los caminos mínimos y tiempos reales usando el algoritmo de Dijkstra sobre el grafo vial, y que DeepSeek proporcione recomendaciones contextuales de secuencia y alternativas ante incidentes, permitiendo al operador aceptar, modificar o rechazar la propuesta.
- **Contexto:** Ante un cierre vial o alerta de congestión, la distancia euclidiana falla. Se requiere topología real (Dijkstra) y explicación analítica (DeepSeek) sin que la IA tome control autónomo de la base de datos.
- **Criterios de Aceptación:**
  - **Dado** un conjunto de paradas con pedidos confirmados y un grafo vial dirigido (`RoadMatrixEdge`), **cuando** se ejecuta la optimización de ruta, **entonces** el componente algorítmico ejecuta Dijkstra para encontrar el trayecto más corto/rápido respetando restricciones de sentido vial y tramos bloqueados.
  - **Intervención de DeepSeek:** El agente n8n envía el contexto de riesgos y paradas al modelo DeepSeek para generar un resumen explicativo en español (justificación de secuencia, riesgos de ventana horaria y recomendaciones de carga).
  - **Control humano («meter mano»):** El Planificador puede reordenar las paradas generadas en la interfaz de mapa, intercambiar el vehículo asignado o descartar paradas inviables antes de accionar la publicación oficial (`route.publish`).
  - **Fallback determinista:** Si el servicio de DeepSeek excede el tiempo límite (10 segundos) o no está disponible, el sistema completa la ruta calculada por Dijkstra con la explicación determinista de respaldo sin detener la operación.

---

## Acciones y preguntas abiertas

- [ ] Definir si para la demo del hackathon los valores unitarios de productos se manejan como costo de adquisición de insumos o si se expondrá margen de precio comercial.
- [ ] Implementar endpoint ligero `PATCH /catalog/products/{id}/dimensions` para permitir al Planificador alterar peso y volumen sin reimportación completa.
- [ ] Habilitar en la pantalla de edición de rutas la reordenación visual de paradas (drag-and-drop o botones de subida/bajada) para consolidar el override del Planificador.
- [ ] Confirmar la integración de los arcos del grafo vial para el algoritmo Dijkstra en las matrices locales de Quito y red nacional.

---

## Seguimiento

- Enlaces relacionados:
  - [[Notas del evento]]
  - [[RBAC y configuracion del sistema]]
  - [[Rutas y sobrecostos]]
  - [[n8n e IA]]
  - [[Usuarios y flujos]]
  - [[Decisiones de arquitectura]]
  - [[Hallazgos de la charla para el Reto 1]]

---

## Correcciones posteriores

- 2026-10-16: Registro inicial estructurado con base en la sesión de alineación del equipo para el Reto 1.
