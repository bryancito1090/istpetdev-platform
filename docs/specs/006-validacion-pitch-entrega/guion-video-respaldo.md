# Guion de video de respaldo — demo de planificación

**Duración objetivo:** 2 min 50 s (170 s). **Estado:** guion; grabación pendiente. El video enseña el alcance real descrito en la [matriz de afirmaciones](matriz-afirmaciones.md), con `demo-quito-planificacion`. La pantalla A/B/C se muestra solo como diseño, sin cifras ni recomendación atribuida al motor. Si el contrato real de #241 no está integrado, se conserva el marcador y no se reemplaza por una captura ficticia.

| Plano / tiempo | Pantalla y rol | Acción | Texto en off | Duración |
|---|---|---|---|---:|
| 1 · 00:00–00:15 | Título / Narración | Abrir con nombre del módulo y rótulo «escenario sintético». | «Este video recorre el módulo de planificación con un escenario sintético de Quito. No representa clientes, tráfico en vivo ni operaciones medidas.» | 15 s |
| 2 · 00:15–00:40 | Web | Abrir `demo-quito-planificacion`; mostrar mapa y puntos, sin zoom a cifras operativas no verificadas. | «El escenario contiene puntos de servicio y referencias geográficas públicas; ventanas, demanda, costos e incidentes son supuestos del ejercicio. Esta vista es una demostración del escenario, no una operación real.» | 25 s |
| 3 · 00:40–01:05 | Web | Mostrar pantalla A/B/C con rótulos “flota propia”, “priorización” y “operador contratado”; si sigue pendiente, mostrar el marcador visible. | «Aquí está el diseño de comparación: A representa flota propia, B una priorización de entregas y C un operador contratado. La comparación contratada y la recomendación calculada siguen en integración. No mostramos resultados ni un ganador del motor.» | 25 s |
| 4 · 01:05–01:30 | Técnica | Mostrar desglose conceptual sin valores. Si se enseña reporte técnico, identificar aparte `demo-base` y “escenario sintético”; no mezclarlo con Quito. | «El costo total considera componentes como combustible, desgaste, conductor, descarga y merma estimada. Hay un ensayo sintético técnico de desglose con `demo-base`; no es resultado de esta pantalla ni de este dataset.» | 25 s |
| 5 · 01:30–01:50 | Web → Técnica | Mostrar la configuración de franjas horarias solo si se identifica como prueba configurada; no mostrar mapa de tráfico actual. | «El motor tiene pruebas con franjas horarias configuradas. Son supuestos de prueba, no tráfico en vivo ni validación de restricciones municipales para cada vehículo.» | 20 s |
| 6 · 01:50–02:15 | Narración → Negocio | Lámina de indicadores en lenguaje sencillo: entregas a tiempo, pendientes, distancia y costo estimado, cada uno rotulado “por medir en piloto”. | «En un piloto proponemos acordar una línea base y medir entregas a tiempo, pendientes, kilómetros y costo estimado con datos de la operación. No afirmamos ahorros observados.» | 25 s |
| 7 · 02:15–02:50 | Lámina / Narración | Cierre con riesgos/mitigación y hoja de ruta en texto; sin simular funcionalidades futuras. | «Riesgos: datos incompletos, rutas poco realistas, costos sin calibrar y adopción. Mitigaciones propuestas: revisar datos y supuestos, validar reglas con el planificador, conciliar costos con viajes y capacitar por rol. Hoja de ruta: integrar A/B/C, validar restricciones de Quito, añadir guía para lo no entregado y calibrar costos en un piloto. La IA explica; el planificador decide.» | 35 s |

## Checklist técnica de grabación

- [ ] Grabar a **1920×1080** (16:9), con cursor visible y tamaño de texto legible; exportar MP4 H.264.
- [ ] Capturar micrófono limpio; hacer prueba de nivel y escuchar el inicio y el cierre con audífonos. Evitar música que compita con la voz.
- [ ] Cargar previamente `demo-quito-planificacion`, comprobar que el navegador y el backend responden y cerrar notificaciones/datos personales.
- [ ] Para mapas que dependan de teselas, verificar conectividad antes de grabar. Tener hotspot disponible; confirmar datos móviles y batería.
- [ ] **Si falla OSRM:** no recalcular ni sustituir por una ruta inventada. Cambiar a la vista estática disponible del escenario y narrar que el cálculo de ruta no está disponible en esa toma; si no se puede mostrar sin inducir a error, cortar el plano y dejar una tarjeta textual.
- [ ] **Si falla DeepSeek:** continuar solo si la explicación determinista registrada aparece en la matriz para el caso mostrado; de lo contrario, omitir esa pantalla y usar la narración factual. No simular respuesta de IA.
- [ ] Si A/B/C no está integrada o ensayada, mostrar `[CAPTURA PENDIENTE: pantalla A/B/C]` como tarjeta y decir que la integración sigue pendiente. La toma de la pantalla conectada corresponde a Alexander (Web), una vez resuelto #241 por Bryan.
- [ ] Reproducir el MP4 completo, confirmar duración ≤3 min, sincronía de audio, rótulos “escenario sintético” y ausencia de cifras/resultados no respaldados.
