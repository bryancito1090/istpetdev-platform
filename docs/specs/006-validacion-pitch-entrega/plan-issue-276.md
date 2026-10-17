# Plan de trabajo — issue #276

## Objetivo y acuerdo de revisión

La comparación A/B/C mostrará datos calculados sobre `demo-quito-planificacion`: API en vivo por defecto y corrida guardada activada exclusivamente a mano. No habrá números de maqueta ni sustituciones silenciosas.

Rama creada: `fix/276-comparacion-abc-datos-reales`.
Base local inspeccionada: `5fa4e9b2fb8238d6660e9c9d89e8a0cf6f5360bb` (`develop`).

Esta entrega contiene únicamente preparación y planificación. Cada tarea funcional se entregará por separado con explicación, archivos modificados, pruebas ejecutadas y pasos manuales exactos. Después esperaré la comprobación del usuario antes de avanzar. Si falla, se corrige la misma tarea. Ninguna prueba manual se marcará aprobada sin su resultado.

## Comentarios de Joseph incorporados

- Según la actualización suministrada, #241 ya entró en develop mediante PR #278. Conectar C y la recomendación forma parte del trabajo inmediato; no desarrollar de nuevo ese issue.
- #275 está a cargo de Bryan. No asumir su configuración ni modificarla como parte de este issue.
- Se puede escribir y probar el generador antes de #275 usando configuración local sin commitear, basada en el README del dataset. Esa corrida es provisional.
- La corrida final se genera después de integrar #275 para usar la configuración acordada para producción.
- Proponer bodega y puntos del pitch en #276; Joseph aprueba la selección. Se preparará el texto para revisión del usuario antes de publicarlo, salvo autorización explícita para publicar.
- Mantener coordinación de #253 respecto a cambios en `rutas.page.ts` y #247. El historial local ya contiene un merge relacionado con #247: verificar el estado real antes de aplicar el orden descrito originalmente.

## Hallazgos de la copia local

1. `rutas.page.ts` iniciaba en modo de maqueta y recuperaba esos valores cuando la respuesta estaba vacía.
2. El mapeo en vivo también rellena datos ausentes: costos por kilómetro/hora, ayudantes, merma, críticos, pendientes, riesgos y tiempo del operador. Quitar el interruptor no elimina por sí solo las cifras inventadas.
3. Se presupone B recomendada y se toma la primera opción cuando no hay recomendación. Esto debe ajustarse al contrato real.
4. El modelo de comparación contiene una referencia fija de combustible. Algunos formateadores confunden cero con ausencia.
5. `suggestRoutes` envía `orderIds` y `plannedDate`; la selección de bodega debe respetar cómo funciona realmente el abastecimiento, sin inventar parámetros.
6. El backend local define un desglose con `Fuel`, `Wear`, `Driver`, `Helpers`, `Shrink`, `Total`, `HelperCount` y `FuelPrice`; el mapeo web consulta otros nombres mediante `any`. Hay que verificar la respuesta serializada de `/suggest`.
7. El contrato de `SourcingService.cs` inspeccionado aún describe A/B y no permite confirmar PR #278. Primero se debe sincronizar/verificar la base; no tratar como ausente en remoto algo que Joseph ya confirmó integrado.

## T0 — Sincronizar base y documentar contrato

**Estado:** rama creada y diagnóstico inicial realizado; sincronización pendiente.

**Cómo funciona:** la web debe consumir el contrato que realmente entrega el motor actualizado.

**Qué haré:** consultar el estado remoto, incorporar develop sin descartar cambios locales y verificar PR #278. Leer `SourcingService.cs`, el controlador `/suggest`, sus DTO y pruebas. Documentar campo de origen de cada celda, C, recomendación, justificación y `costBreakdown.fuelPrice`. Comprobar comandos de arranque y acceso local con rol planificador.

**Qué documentaré:** commit actualizado, forma de la petición/respuesta y correspondencia campo → celda. Registrar discrepancias y dependencias reales.

**Revisión manual:** comprobar la rama con `git branch --show-current`; abrir Rutas y recorrer el asistente. En Network localizar `/api/v1/route-plans/suggest`, guardar una respuesta sin credenciales y verificar juntos campos de C y recomendación. Entregaré el recorrido exacto según la pantalla disponible.

**Terminado cuando:** contrato confirmado y entorno local identificado. No implica aprobación funcional de la comparación.

## T1 — En vivo por defecto; vacío y error honestos

**Estado:** implementada; revisión manual pendiente. **Depende de:** T0; no requiere #275.

**Cómo funciona:** una respuesta vacía representa ausencia de opciones, no permiso para mostrar ejemplos.

**Qué hice:** dejar API en vivo al abrir, quitar el respaldo silencioso y distinguir vacío/error. Mostrar «Sin opciones calculadas para esta selección» y el motivo del servidor; si no existe, indicar que la API no proporcionó motivo. Limpiar selección, cifras y recomendación anteriores al recalcular y bloquear avance sin opciones. Se retiró el modo de diseño numérico.

**Pruebas:** inicio en vivo, `{ options: [] }`, error HTTP y secuencia respuesta válida → vacía/error.

**Revisión manual:** abrir el asistente, calcular y comprobar el modo inicial. Repetir con respuesta vacía mediante un caso local o sustitución controlada de respuesta. Bloquear la petición de sugerencias en Network, cambiar selección y recalcular: debe aparecer error, sin cifras de la consulta anterior. Desbloquear la petición.

**Documentación y salida:** capturas de vacío/error, resultados técnicos y tu comprobación. T1 no acredita todavía los rellenos internos: se eliminan en T2.

## T2 — Datos fieles, combustible, C y recomendación real

**Estado:** pendiente. **Depende de:** T1 y contrato #241 confirmado en T0.

**Cómo funciona:** la pantalla presenta lo que devuelve el motor; una ausencia se muestra como «sin dato» y una recomendación solo existe si llega en el contrato.

**Qué haré:** tipar el contrato y extraer un mapeo compartido para API y futura corrida. Eliminar costos, cantidades y justificaciones inventadas, etiquetas de ventaja sin respaldo y selección automática de ganador por letra/posición. Conectar C y su tarifa/inclusiones según el contrato de #241. Leer valor, fecha, fuente y unidad de combustible desde el desglose. Conservar notas de configuración incompleta y no confundir costo histórico/parcial con costo total de servir. Distinguir cero válido de dato ausente. No reemplazar el precio fijo por otro precio fijo.

**Pruebas:** datos completos/parciales, cero válido, combustible sin fuente/fecha, C real, opción recomendada explícita y ninguna recomendación. Si el motor no entrega C, mostrar «en integración (#241)» sin importes, sin asumir un problema de integración solo porque una selección no genera esa alternativa.

**Revisión manual:** comparar Network con todas las filas, total, combustible, fuente, fecha y justificación. Cambiar selección y comprobar que no se fuerza B como ganadora. Revisar datos incompletos, proyección y elección explícita del planificador. Mientras falte #275 puede ser correcto mostrar costos no disponibles: se registra esa limitación.

**Documentación y salida:** tabla campo → celda y evidencia comparada con la respuesta. C queda conectada ahora; no se pospone innecesariamente hasta #275.

## T3 — Propuesta de selección para el pitch

**Estado:** pendiente. **Puede prepararse:** desde T0.

**Cómo funciona:** para repetir una demostración debemos conocer exactamente qué pedidos y fecha se calculan y qué bodegas intervienen.

**Qué haré:** revisar `docs/boveda/07 Demo/Guion del pitch.md`, `data/fixtures/demo-quito-planificacion/README.md` y datos del fixture. Preparar una propuesta con códigos y nombres reales de bodega/puntos, pedidos, fecha de planificación y motivo de la selección. Indicar cómo resuelve la API los almacenes y qué resultados se esperan comprobar, sin prometer importes ni ganador.

**Revisión manual:** revisar juntos los puntos en la UI y el texto propuesto para #276. Obtener la aprobación de Joseph mediante el usuario o el comentario del issue; no darla por recibida ni contactar por otro canal sin autorización.

**Documentación y salida:** selección propuesta y estado de aprobación. El script puede probarse con una selección provisional identificada; la evidencia final usa la aprobada.

## T4 — Escribir y probar el generador local

**Estado:** pendiente. **Depende de:** contrato confirmado; no necesita esperar #275 para desarrollar/probar.

**Cómo funciona:** el respaldo conserva una respuesta auténtica del motor, junto con las entradas que permiten reproducirla.

**Qué haré:** crear `scripts/demo/generar-corrida-abc.mjs`. Contra API y base local aislada: resetear mediante `POST /api/v1/demo/reset?fixture=demo-quito-planificacion`, resolver IDs del fixture y llamar a `/suggest`. Guardar respuesta sin alterar dentro de un contenedor con commit verificable del motor, fecha/hora UTC, fixture, hash SHA-256 de configuración relevante y selección/petición exactas. Destino previsto: `apps/web/src/pages/rutas/corrida-abc.demo-quito.json`.

Para probar, usar configuración local no versionada basada en el README, verificar que Git la ignora y documentar su carácter provisional. No inventar valores faltantes ni comprometer cambios de #275. Incluir entradas efectivas de configuración, fecha de planificación y proveedor de distancias al evaluar reproducibilidad. No incluir secretos ni tokens. Verificar versión del backend: el SHA del frontend no acredita por sí solo qué motor respondió.

**Pruebas:** reset fallido, autenticación/error HTTP, selección inexistente, JSON inválido y respuesta correcta. No sobrescribir una corrida válida si falla la generación. Validar destino local antes de resetear; no resetear una base compartida.

**Revisión manual:** ejecutar el comando concreto que entregaré con entorno y selección documentados; cotejar respuesta con JSON y metadatos. Repetir con mismo código/configuración/entradas y comparar resultados calculados. La fecha de captura cambia; documentar/controlar IDs, reloj y fuentes externas si afectan la igualdad. No afirmar que el commit por sí solo garantiza reproducción.

**Documentación y salida:** script probado, instrucciones y corrida provisional claramente identificada. La corrida final sigue pendiente de #275 y aprobación de selección.

## T5 — Corrida guardada manual y eliminación definitiva de maqueta

**Estado:** implementada; revisión manual pendiente. **Depende de:** T2 y T4 para validación real.

**Cómo funciona:** el usuario activa una captura histórica explícitamente; un error de red nunca cambia el origen de datos.

**Qué hice:** selector En vivo / Corrida guardada, inicio siempre en vivo y mapeo común de T2. La tabla muestra commit y fecha. Se informa si falta el archivo o es inválido. Se eliminaron las constantes de opciones y combustible de maqueta y las operaciones quedan bloqueadas al mostrar la corrida guardada.

**Pruebas:** activación manual, metadatos visibles, falta de archivo, error de red sin cambio de modo y vuelta a vivo sin arrastrar datos. Verificar mapeo idéntico para la misma respuesta.

**Revisión manual:** bloquear `/suggest`, confirmar error sin respaldo automático, activar corrida a mano y comparar con JSON. Revisar rótulo en proyección. Volver a En vivo y recalcular. Comprobar que el respaldo no permite operaciones con pedidos históricos.

**Documentación y salida:** capturas rotuladas y búsqueda global sin las constantes retiradas de opciones y combustible de maqueta.

## T6 — Corrida final con #275 y evidencia del pitch

**Estado:** preparada parcialmente; corrida y evidencia pendientes. #275 ya integrado en `33387ca` (PR #283). Depende todavía de API/base local disponibles y selección aprobada.

**Cómo funciona:** la evidencia final usa la configuración integrada y el mismo motor que se pretende presentar.

**Qué hice:** sincronizar `develop` a `33387ca`, que incluye #275 (PR #283), revisar y reaplicar los cambios pendientes de esta rama sin conflictos, verificar la configuración versionada y el generador en modo offline. La configuración logística tiene SHA-256 `69cc3e59b1d4b223e52f209e83ae524e6792ae4606af5e1f525fee46e9c82b79`.

**Qué falta:** arrancar API y base de datos local aislada, ejecutar el generador con la configuración integrada, inspeccionar una respuesta válida, cargar la tabla y capturar lámina 4. En esta comprobación Docker Desktop no está activo y los puertos localhost 5000/5001 no responden. La selección del pitch aún figura como propuesta pendiente de aprobación de Joseph; no se generará evidencia final con una selección sin aprobar. No se llamó al reset ni se modificó la base.

**Verificación tras sincronizar:** suite web completa y build de producción repetidas; build API sin errores; seis pruebas unitarias de `ContractedCarrierAndRecommendationTests` pasaron. El script validó offline la configuración integrada y la selección, sin red ni escritura. La búsqueda global sigue sin constantes retiradas.

**Pruebas de cierre pendientes:** repetir tras corrida real; ejecutar integración del reset cuando Docker/base estén disponibles. Registrar por separado limitaciones o fallos anteriores. Verificar búsqueda global sin las constantes retiradas.

**Revisión manual:** repetir casos de API válida, vacía, parcial y error; comprobar C y recomendación; activar respaldo y revisar commit/fecha; volver a vivo; revisar navegación y proyección. Comparar evidencia con JSON final y la selección aprobada.

**Documentación y salida:** tu aprobación, resultados reales de pruebas y evidencia para #200. Actualizar guion/matriz únicamente con capacidades comprobadas. Frase habilitada tras validar: «Lo que ven es lo que calcula el motor con el escenario sintético de Quito». Si falta la integración/configuración/evidencia, registrar el bloqueo y no dar por cerrado el issue ni presentar resultados como verificados.

## Registro obligatorio en cada entrega

| Campo | Qué se registra |
|---|---|
| Tarea y estado | Pendiente / implementada / revisión manual pendiente / validada |
| Cambio y explicación | Qué hice, cómo funciona y archivos afectados |
| Verificación técnica | Comando, resultado y limitaciones reales |
| Guía manual | URL, recorrido, selección, acción y resultado esperado |
| Resultado del usuario | Pendiente hasta recibir su comprobación |
| Evidencia | Captura, respuesta o log sin credenciales |
| Siguiente acción | Corrección de esta etapa o siguiente tarea |

## Cobertura de aceptación

| Requisito | Tarea |
|---|---|
| En vivo inicial, vacío con motivo, sin respaldo silencioso | T1 |
| Sin rellenos inventados, combustible con fuente | T2 |
| C y recomendación del contrato real | T0, T2 |
| Selección del pitch aprobada | T3 |
| Script y reproducción de respuesta con metadatos | T4 |
| Respaldo manual rotulado, misma transformación, sin maqueta | T5 |
| Configuración final y captura para lámina 4 | T6 |

**Avance T0/T1:** rama sincronizada a `ef31353`, contrato de #241 inspeccionado y T1 implementada. Suite web (157 pruebas), lint y build aprobados. [Entrega, hallazgo contractual y guía de revisión manual](revision-issue-276-t1.md). Pendiente tu comprobación de T1 antes de implementar T2. T0 no acredita todavía una sesión manual con API local.

**Avance T2:** el usuario solicitó continuar. Se añadió el contrato comparativo a `/suggest`, se retiraron rellenos del mapeo y se conectaron combustible, tarifa contratada y recomendación. [Entrega T2, correspondencia de campos, límites y revisión manual](revision-issue-276-t2.md). Pendiente revisión manual; no se registra T1 como validada visualmente sin resultados del usuario.

**Avance T3:** preparada la [selección propuesta y comentario para Joseph](revision-issue-276-t3.md), con códigos verificados y demanda derivada del consumo. Pendiente aprobación; no se publicaron mensajes ni se crearon pedidos. El generador de T4 puede desarrollarse con esta selección marcada como provisional.

**Avance T4:** [generador implementado y validado sin red](revision-issue-276-t4.md). Se añadió resolución de IDs para puntos/productos y limpieza de pedidos en el reset para permitir repetir corridas. La build de API y chequeo offline pasan; el test de integración requiere Docker, que no está disponible en este entorno. No se llamó a reset ni se creó corrida; la prueba API local está pendiente de Docker, configuración y token.

**Avance T5:** selector manual de corrida guardada y mapeo común implementados; la corrida no existe aún y por eso no se generó ni se publicó un archivo de ejemplo. En el modo guardado la vista es de solo lectura. Pruebas técnicas y guía manual están pendientes en [revisión T5](revision-issue-276-t5.md); la validación visual del usuario sigue pendiente.

**Avance T6:** #275 ya está en la rama desde `develop` (`33387ca`, PR #283). Build web/API, suite web y pruebas unitarias de sourcing pasan; el generador valida offline el archivo de configuración. La corrida final y captura no se hicieron: no hay Docker/API local activos y la selección todavía espera aprobación de Joseph. [Detalle y pasos de cierre](revision-issue-276-t6.md).
