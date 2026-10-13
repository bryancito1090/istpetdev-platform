---
tipo: ejecucion
estado: propuesta
actualizado: 2026-10-12
tags: [hackathon, istpetdev, alcance, demo, agente]
---

# Definiciones de producto y plan hasta el pitch

Registro de la sesión de definición del **12 de octubre de 2026**, en la que un integrante del equipo, con asistencia de Claude Code, reestructuró el alcance para maximizar la nota del jurado a cuatro días del evento. **Decididas por:** PENDIENTE (registrar el nombre del integrante y la confirmación del equipo, según [[Convenciones y estados]]). Mientras no se confirmen, son propuestas y no cambian decisiones aceptadas: las contradicciones con decisiones vigentes están en [[Hechos canonicos]] y como ADR-21 a ADR-23 en [[Decisiones de arquitectura]].

La implementación detallada vive en los specs del repositorio (`docs/specs/`): [índice y guardarraíles](../../specs/README.md), [001 — Núcleo delgado e importador](../../specs/001-nucleo-delgado-importador/spec.md) y [002 — Motor logístico](../../specs/002-motor-logistico/spec.md). Esta nota conserva el **porqué** de cada decisión para no perder el hilo.

## Punto de partida

Diagnóstico del 12-oct sobre el repositorio unificado:

- Todo el P0 de [[Backlog]] (B-01 a B-10) seguía pendiente; el código tenía lectura de puntos, trazabilidad simulada y login de demostración.
- La infraestructura (95 tablas con RLS, CI/CD, Terraform) iba muy por delante del producto visible.
- Cinco de los siete jurados no son técnicos ([[Evaluacion y entregables]]): la administración interna (RBAC configurable, plantillas, workflows) no suma nota.
- El prototipo pesa 20% y es el primer desempate, pero además sostiene Viabilidad, Impacto e Innovación; Comprensión del problema no tenía evidencia del sector.
- El widget de rutas de la web muestra cifras escritas a mano («418 km», «Ahorro: 24.8%»), contrario a la regla de honestidad de [[Dataset y escenarios]]; el spec 005 debe reemplazarlas por datos del motor.

## Objetivo norte

> En 3 minutos, el jurado ve cómo IstpetDev evita que un hospital se quede sin desinfectante: el agente lo detecta 5 días antes, propone la ruta y una persona la aprueba; el conductor entrega sin señal; el jurado escanea la caja con su propio teléfono; y el tablero muestra qué habría pasado en 90 días.

**Filtro para construir algo antes del 17-oct** (si una respuesta es «no», no se construye):

1. ¿El jurado lo ve en el pitch o en sus preguntas?
2. ¿Funciona sin la wifi del recinto?
3. ¿Sus cifras salen de una ejecución reproducible?
4. ¿Lo entiende un gerente de limpieza sin explicación técnica?
5. ¿Sobrevive a un dataset distinto (guardarraíles G1–G8)?

### Métricas de «listo para ganar»

| Métrica | Umbral |
|---|---|
| Ensayos completos sin fallo | 10 consecutivos; los 2 últimos sin la wifi del recinto |
| Duración del pitch | Tiempo oficial −20 s; versiones de 3, 5 y 7 min hasta que responda O-04 ([[Consultas para la organizacion]]) |
| Banco de preguntas del jurado | 20 preguntas difíciles con respuesta ensayada en ≤ 30 s |
| Evidencia del sector | ≥ 3 conversaciones documentadas antes del 17-oct 12:30, ≥ 1 con reacción al prototipo |
| Impacto | ≥ 3 semillas + 1 escenario adverso, presentado como rango frente a los dos baselines |
| Reinicio de la demo | ≤ 10 s con un clic |
| Propuesta del agente | ≤ 10 s (p95); todas sus cifras coinciden con las de las herramientas |

## Definiciones D1–D8

| # | Definición | Por qué |
|---|---|---|
| D1 | Objetivo norte y métricas de esta nota | Un criterio común para decidir qué entra |
| D2 | **Evidencia del sector solo durante el evento.** Antes del 16 los cinco construyen. Día 1: dos personas entrevistan expositores de la feria en las pausas (11:00–11:30 y 13:00–14:00) con un guion de 5 minutos. Día 2: se vuelve con el prototipo en el bloque de validación (11:00–12:30). Como cautela propia, el equipo evita entrevistar organizaciones con representante en el jurado; el manual §11.5 exige registrar vínculos de cada jurado con el proyecto y abstención individual ante conflicto, incluida mentoría. Antes del 16, un **kit previo sin horas del equipo**, preparado con IA: guion y hoja de entrevista, esqueleto del pitch con huecos, modelo de negocio, banco de preguntas e investigación de pliegos de compras públicas (SERCOP) para citar el costo de un quiebre **solo si aparece una fuente** | El equipo no tiene acceso al sector; la hackathon ocurre dentro de la Expo Clean, donde están las empresas |
| D3 | **Cortes de alcance** (tabla siguiente) | Liberar horas para el recorrido que ve el jurado |
| D4 | **Agente con niveles.** A — vigía: detecta riesgos y propone reposición y ruta. B — gestor de incidentes: evalúa un cierre de vía, replanifica lo pendiente y avisa al conductor. Ambos obligatorios. C — «qué pasaría si» con el simulador, solo si A y B están terminados. **El LLM orquesta y explica; nunca calcula** rutas, cantidades ni fechas. **Modelo: DeepSeek** (decisión del 12-oct, sin comparación), validado con una **prueba de aceptación** de 3 escenarios (riesgo, incidente, dato con instrucciones inyectadas) × 10 ejecuciones y umbrales fijos: llamadas válidas ≥ 95%, latencia p95 ≤ 10 s, explicaciones con cifras coincidentes ≥ 90% e inyección 10/10. Si no los cumple en H1, el agente opera en fallback determinista y el equipo decide proveedor con la misma prueba | Innovación defendible ante el jurado técnico; un agente que opera, no un chatbot |
| D5 | **Autonomía con límites.** Un motor de reglas determinista en el backend clasifica cada propuesta en «se ejecuta» o «se escala»; el LLM no puede saltarlo. Se ejecuta sola si no se retira ninguna parada obligatoria, se cumplen todas las ventanas, el costo sube ≤ 15% y ningún retraso supera 60 min. Se escala lo demás, con dos opciones calculadas («atender tarde» y «priorizar críticos»). Interruptor «solo proponer». Deshacer crea otra revisión y compensa únicamente reservas activas de entregas aún no despachadas; el stock movido físicamente requiere conciliación | Respuesta a «¿y si la IA se equivoca?»: la IA hace lo rutinario y deja a la persona lo que importa |
| D6 | **Recorrido en vivo regional:** unos 15 puntos sintéticos de Quito, 1 bodega y 2 vehículos (fixture `demo-region`); el incidente afecta a un vehículo y no al otro. La escala nacional (80 puntos) se muestra en el simulador | Con 6 puntos el mapa parece un juguete; con 80 no se entiende en 3 minutos |
| D7 | **Doble baseline:** el simulador compara contra reposición reactiva (cuando el punto avisa) y contra calendario fijo (cada N días, cantidad fija). Ambas políticas se escriben antes de la primera simulación y no se ajustan después de ver resultados | Sin datos del sector no sabemos cuál usan; ganar a ambas hace el resultado robusto (ver [[Simulador y metricas]]) |
| D8 | **Congelamiento de funcionalidades: 15-oct a las 14:00**, con hitos de control y plan B por componente | Asegura ensayos, simulacro de dataset sorpresa y video de respaldo |

### Cortes de alcance hasta el 17-oct (D3)

| Se corta | Lo reemplaza |
|---|---|
| RBAC administrable, plantillas, workflows y configuración desde la base | Roles fijos y configuración en archivos (spec 001) |
| Chat libre gerencia–conductor | Avisos del agente al conductor (nivel B) |
| SignalR con varias réplicas, backplane Redis, ECS/Fargate, autoscaling | Una instancia; actualización periódica o SignalR simple |
| Varias bodegas y más de 2 vehículos en el recorrido en vivo | 1 bodega y 2 vehículos (D6) |
| Pronóstico con ML entrenado | Media de la ventana declarada ([[Inventario y prediccion]]) |
| App nativa (build de Capacitor) | PWA instalada |
| Matriz vial de pago | Fuente abierta o aproximación declarada |
| Login de Cognito visible en la demo | Sesiones ya iniciadas en los dispositivos de la demo |
| Más de un caso en el pitch | La historia del hospital; los demás casos, para las preguntas |
| Blockchain, sensores IoT simulados, varios agentes | — |

## Guardarraíles contra el dataset del evento

La ficha final del reto se presenta el 16-oct a las 09:30 ([[Cronograma oficial]]). Para que un dataset distinto no invalide el trabajo previo:

| ID | Regla |
|---|---|
| G1 | Nada del escenario en el código: cantidades, nombres, regiones o SKUs vienen de datos o parámetros |
| G2 | Una sola puerta de entrada de datos: el importador canónico (CSV/XLSX + perfil de mapeo), incluido nuestro dataset de demo |
| G3 | Lo que falta se declara como supuesto; nunca se inventa precisión |
| G4 | Unidades explícitas; las conversiones se declaran en el perfil |
| G5 | Algoritmos parametrizados, con valores por fila desde los datos |
| G6 | Simulacro de «dataset sorpresa» antes del 16: integrar un formato ajeno en ≤ 60 minutos sin tocar código |
| G7 | Modelo de dominio genérico: el hospital es un caso dentro de los datos, no una entidad |
| G8 | Trabajo previo trazable: tag `pre-evento` y registro en [[Registro de trabajo previo y del evento]] |

**Límite:** cubren cambios de datos y de formato dentro del Reto 1. Un cambio de problema (por ejemplo, cotización del Reto 2) obliga a replanificar el alcance el mismo día.

## Plan hasta el pitch (D8)

### Hitos de control

| Hito | Fecha y hora | Condición | Si se retrasa más de 4 h |
|---|---|---|---|
| H1 | 13-oct 13:00 | Specs 002–005 aprobados; prueba de aceptación de DeepSeek ejecutada | Se implementa con el spec en borrador; si DeepSeek no cumple, el agente queda en fallback determinista y el equipo decide proveedor |
| H2 | 13-oct 20:00 | Spec 001 integrado en `develop` | Prioridad del equipo entero: bloquea 002–005 |
| H3 | 14-oct 20:00 | Recorrido sin agente de punta a punta: riesgo → ruta de 2 vehículos → despacho con QR → recepción offline → saldo actualizado, **desde los teléfonos con sesión PKCE y la PWA publicada por HTTPS** (FR-17 y FR-18 del spec 004) | El nivel B del agente pasa a su plan B |
| H4 | 15-oct 11:00 | Agente A y B, panel con decisiones y simulador con resultados frente a los dos baselines | Se aplican los planes B |
| H5 | 15-oct 14:00 | Congelamiento de funcionalidades | — |

### Plan B por componente

| Componente | Si no llega al congelamiento |
|---|---|
| Agente nivel B | Botón de incidente con replanificación determinista y tarjeta escalada con explicación por plantilla |
| Agente nivel A | Riesgos calculados con tarjeta de acción manual y explicación por plantilla |
| PWA offline | Recepción en línea; el modo avión se muestra en el video de respaldo |
| Mapa de ruta | Lista ordenada de paradas con horas y costos |
| Despliegue en la nube | Stack local en un portátil con hotspot propio; el QR se escanea con un teléfono del equipo conectado al hotspot |
| Simulador de 90 días | Sin plan B honesto: es prioridad por encima de los niveles del agente |

### Días del evento

Se superponen al [[Cronograma oficial]] y se **participa de verdad en la dinámica oficial**: «Proceso y equipo» observa la participación.

| Momento | Qué hace el equipo |
|---|---|
| 16-oct 09:30–10:30 | Una persona transcribe la ficha final; cada cambio se clasifica en sin impacto, se resuelve con perfil o parámetros, o cambia el alcance. Decisión «seguir o adaptar» |
| 16-oct pausas | Dos personas entrevistan en la feria; tres integran |
| 16-oct 14:00–16:00 | Tres adaptan el código; dos trabajan negocio y rellenan el esqueleto del pitch |
| 16-oct 16:00–16:30 | Mentoría con 3 preguntas preparadas; registrar en [[Registro de mentorias]] y comprobar si el mentor es jurado |
| 16-oct 16:30–19:00 | Integrar el consejo del mentor; ensayo 1 con los datos del evento; checkpoint |
| 17-oct 10:30 | Congelamiento del código; después solo se corrige lo que bloquee |
| 17-oct 11:00–12:30 | Dos validan con expositores usando el prototipo; tres ejecutan la simulación final y 3 ensayos |
| 17-oct 14:00–15:45 | Completar huecos del pitch (citas, cifras, precio); 2 ensayos cronometrados; 15:45 congelamiento de materiales |

## Decisiones de diseño del motor logístico (spec 002)

Aprobadas en el brainstorming del 12-oct; detalle en el [spec 002](../../specs/002-motor-logistico/spec.md) y su [plan](../../specs/002-motor-logistico/plan.md).

| Tema | Decisión | Alternativas descartadas |
|---|---|---|
| Dónde vive el motor | Servicios deterministas en .NET (`Application`) | OR-Tools en un microservicio Python; lógica en n8n |
| Distancias | Cadena: tramos guardados con el fixture → OSRM con URL configurable → línea recta × 1,3 a 40 km/h etiquetada como aproximación | Solo aproximación; solo OSRM público en vivo |
| Puntos de una ruta | Obligatorios (críticos y altos) + consolidación de los «en vigilancia» con desvío ≤ 20 min | Solo los en riesgo; todos los puntos del día |
| Reloj operativo | Por defecto, el día siguiente al último consumo importado | Hora real (con datasets históricos no detectaría riesgos) |
| Incidentes | Cierre de vía, demora y avería de vehículo | — |
| Escalado | Dos opciones calculadas por el planificador: «atender tarde» y «priorizar críticos» | Alternativas propuestas por el LLM |
| Explicaciones | Plantillas construidas solo con `facts` estructurados; el agente se valida contra los mismos `facts` | Texto libre del LLM sin validación |

## Decisiones de diseño del agente (spec 003)

Aprobadas en el brainstorming del 12-oct; detalle en el [spec 003](../../specs/003-agente-logistico/spec.md) y su [plan](../../specs/003-agente-logistico/plan.md).

| Tema | Decisión | Alternativas descartadas |
|---|---|---|
| Dónde corre el agente | Workflow de n8n con nodo AI Agent; herramientas = 5 endpoints del motor (002) | Bucle en el backend .NET; varios agentes |
| Dónde corre n8n | Servidor existente de Bryan contra la API desplegada; copia local en Docker como plan B | Solo el servidor de Bryan; n8n dentro del EC2 de la demo (memoria insuficiente) |
| Avisos al conductor | Dentro de la PWA, al detectar una revisión nueva de su ruta | Telegram; ambos |
| Modelo | DeepSeek con prueba de aceptación y umbrales fijos | Comparación entre Claude (Bedrock) y DeepSeek |
| Envío del resultado | Paso fijo del workflow tras el nodo AI Agent, con salida JSON validada | Herramienta `submit_result` que debe llamar el modelo |
| Traza | Medida por la API con la cabecera `X-Agent-Run-Id` | Pasos intermedios informados por n8n |
| Si el agente falla o tarda | La API responde a los 10 s con el camino determinista de 002 y plantillas, sin repetir replanificaciones | Esperar indefinidamente |
| Cifras del LLM | Cada número se valida contra los `facts`; si no coincide, se usa la plantilla y se registra el motivo | Confiar en el texto |
| Seguridad | Sin herramientas para publicar ni aprobar; rol `Automatizacion` con 403 en esos endpoints; datos tratados como datos en el prompt | Solo instrucciones en el prompt |
| Identidad | Cliente Cognito `client_credentials` con un scope `istpetdev-api/automation` y rol en la API | Cuatro scopes finos de [[Identidad OIDC y sesiones]] |

## Decisiones de diseño del campo (spec 004)

**Propuesta del 12-oct; Deciders PENDIENTE.** El [spec 004](../../specs/004-campo-qr-custodia-offline/spec.md) documenta el recorrido de la caja física y sus criterios de aceptación; su [plan](../../specs/004-campo-qr-custodia-offline/plan.md) detalla los contratos de implementación. Estas decisiones no afirman implementación.

| Tema | Propuesta | Motivo |
|---|---|---|
| Preparación QR | Bodega crea y etiqueta la `HandlingUnit` desde una entrega publicada, antes de despachar | La caja física existe al asociar el QR; los lotes ya están elegidos por 002 |
| Roles del recorrido | Bodega escanea el despacho; conductor asignado captura recepción y el receptor firma voluntariamente en su dispositivo | Dos sesiones iniciadas bastan para el pitch |
| Recepción y evidencia | La API acepta cantidades/custodia de forma transaccional; foto y firma se suben después a S3 privado con reintento por archivo | Una falla de carga no obliga a repetir el movimiento de inventario |
| Revisión sin señal | Una revisión nueva no invalida por sí sola la recepción; se validan entrega, asignación y cantidades vigentes | Conserva entregas legítimas y muestra los conflictos reales de S-11 |
| Parcial o rechazo | Lo no aceptado sigue en el vehículo bajo custodia del conductor hasta devolución física registrada en bodega | Evita incrementar saldos sin transferencia |
| QR del jurado | URL HTTPS con token aleatorio revocable; resumen de producto/unidad, estado e hitos generales, sin cantidades ni datos personales | Da una comprobación visible y limitada según RF-13 |
| Aviso al conductor | Consulta de revisiones en la PWA y mensaje de `AgentDecision.DriverMessage` | Cumple el nivel B del agente sin chat ni push |
| Login de web y PWA | El spec 004 asume ADR-18: Cognito con PKCE mediante `angular-auth-oidc-client` 22.0.1 en la web de bodega y en la PWA | Ningún spec tenía dueño del login; sin él ninguna pantalla llama a la API (revisión del 12-oct) |
| PWA por HTTPS | La PWA se publica en `/campo/` del dominio de demo y Cognito suma las URL de retorno HTTPS | La PWA no se desplegaba en ningún sitio; sin HTTPS no se instala ni funciona sin conexión |
| Paradas completadas para el pitch | Endpoint de demo `advance`: completa N paradas con preparación, despacho y recepción reales | Sustituye al marcado provisional de 002 en el pitch y en los recorridos de 002 y 003 sin descuadrar el inventario |
| Token del QR | En el fragmento de la URL (`#t=`) y enviado por cabecera `X-Trace-Token` | Con query string quedaba en logs de acceso e historial |

El marcado provisional de parada completa de 002 queda sustituido por recepción aceptada al integrar 004. La página pública actual y su registro fijo no representan custodia real; el spec pide reemplazarlos. El esquema de 001 cierra con 27 tablas; desde la revisión del 12-oct su prueba exige ese núcleo y la ausencia de Schema95, no un conteo cerrado, para que 002–004 añadan tablas sin romper la CI.

**Decisiones abiertas del campo (revisión del 12-oct):** dónde vive la pantalla de Bodega que prepara y despacha la caja (web o PWA); si la evidencia se sube con URL prefirmadas (requiere CORS e IAM del equipo de infraestructura) o a través de la API; y si el límite de consultas del QR se aplica por IP (los jurados comparten IP en la wifi del recinto) o por token. Registradas como PA-03 a PA-05 del spec 004.

## Panel y el simulador (spec 005)

**Propuesta del 12-oct-2026; Deciders PENDIENTE.** El [spec 005](../../specs/005-panel-simulador/spec.md) y su [plan](../../specs/005-panel-simulador/plan.md) fijan la comparación antes de generar resultados. La web muestra el recorrido `demo-region` mediante riesgos explicados, mapa con tramos viales o aproximados etiquetados, entregas y decisiones en dos columnas («ejecutadas solas» / «esperan tu aprobación»), con aprobar, rechazar, deshacer y modo «solo proponer». El login PKCE es del 004; `facts`, planificador y política son del 002; la traza es del 003. La maqueta actual contiene cifras y geografía fijas (R-15), que el 005 debe sustituir por API.

El simulador corre como trabajo asíncrono dentro de la API, en base de simulación separada y usando el importador canónico de 001. Compara 90 días de IstpetDev con reposición reactiva y calendario fijo cada siete días, escalonado por día laborable, de cantidad histórica fija; las tres políticas secuencian sus paradas con el mismo planificador de 002; comparte calendario exógeno, recursos, matriz y costos. Muestra tres seeds base y un escenario adverso, con resultados como rango y cuatro tarjetas para jurados no técnicos: quiebres evitados/nuevos, demanda no atendida, costo de transporte y puntualidad. La red de unos 80 puntos solo aparece en `/simulation`; la demo en vivo conserva unos 15 puntos y dos vehículos. Los parámetros, supuestos y hashes quedan en la exportación. No se presentan porcentajes fijos.

**Bloqueos y plazo.** El 005 añade dos contratos de lectura a sus specs productores: `GET /api/v1/route-plans` en 002 y `GET /api/v1/deliveries` en 004, ambos antes de H4. La matriz completa 005↔001–004 está en el spec. El login y `advance` del 004 deben llegar antes de H3. Las 1080 jornadas de política se planifican por región; la corrida completa tiene objetivo medido ≤60 min antes de H4 (15-oct 11:00), y el límite por propuesta de 002 no basta. El simulador conserva prioridad sobre niveles adicionales del agente y no tiene plan B honesto. Esto es diseño propuesto, no software implementado ni resultados medidos.

**Revisión del 12-oct (antes de la primera corrida):** se corrigieron el ruteo común y el calendario escalonado de los baselines, y la corrida oficial se ejecuta en una máquina dedicada cuyo ZIP se importa en la demo con verificación de hashes (FR-21 del 005), porque la instancia de la demo es `t3.micro` según la plantilla de Terraform. Las lecturas de 002 y 004 quedaron también en los `task.md` de sus dueños.

La redacción del E2E detectó otro bloqueo: la tarea T7 de 001 borraba runs/métricas de simulación al ejecutar el reset. El contrato de reset de 001 se corrige en este PR para conservar evidencia completada; el 005 conserva eventos/ZIP y E2E-04/E2E-07 comprueban que la escena puede reiniciarse sin perder el resultado del pitch. Sigue siendo una corrección documental pendiente de implementación.

## Pendientes de esta sesión

- Confirmar quién decide D1–D8 y ADR-21 a ADR-23.
- Aprobación por Deciders de 004 y 005; spec 006. `task.md` y `test-e2e.md` del 005 se redactaron por petición posterior del usuario; ejecución y evidencia siguen pendientes.
- Bryan: credenciales de DeepSeek, OAuth2 de Cognito y secreto HMAC en su n8n; versión exacta de n8n para la copia local.
- Kit previo de 006 e investigación de pliegos de compras públicas.
- Confirmación escrita de la organización sobre trabajo previo (O-01).
- Hallazgo de seguridad reportado al equipo de infraestructura: el grupo de seguridad de Terraform abre SSH a `0.0.0.0/0` por defecto (`infra/terraform/modules/security_groups/variables.tf`); la recomendación fue dejarlo vacío por defecto o desplegar por SSM.
