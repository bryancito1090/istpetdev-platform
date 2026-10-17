# Guion de video de respaldo — demo de planificación

## Versión v4 (17-oct): la cadena completa en 2:30

Acompaña la [propuesta v4 del guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md). Muestra la historia «detectar → decidir → entregar → probar» con pantallas reales del sistema. Todo con el escenario sintético `demo-quito-planificacion`; el rótulo «escenario sintético» va fijo en una esquina durante todo el video, así no hay que repetirlo en la voz. Si una toma falla, se corta: no se simula.

| Plano / tiempo | Pantalla (ruta web) | Acción | Texto en off | s |
|---|---|---|---|---:|
| 1 · 00:00–00:10 | Tarjeta de título | Pregunta en grande sobre fondo oscuro. | «¿Cómo se entera una empresa de limpieza de que a un hospital le falta desinfectante? Hoy, cuando el hospital llama.» | 10 |
| 2 · 00:10–00:30 | `/inicio` → `/alertas` | Entrar como planificador. En `/inicio`, esperar a que aparezca la notificación de alerta (#318) y la campana con conteo. Clic en la notificación → `/alertas`. | «Con IstpetDev el aviso llega antes: el sistema revisa el inventario y avisa qué punto entra en riesgo, con días de cobertura y cantidad sugerida.» | 20 |
| 3 · 00:30–00:45 | `/alertas` (detalle) | Abrir el detalle de un punto crítico: cobertura, demanda diaria, tiempo de reposición. | «Cada alerta explica sus datos: cuánto se consume, cuánto queda y cuánto tarda en reponerse.» | 15 |
| 4 · 00:45–01:20 | `/rutas` | Mostrar la ruta propuesta en el mapa con paradas numeradas. Clic en **«Pedir al Agente IA»**; esperar la tarjeta «Propuesta del agente logístico» (modelo, latencia, estado). Hacer zoom a la explicación. | «Luego el sistema decide cómo atenderlo. El motor arma la ruta respetando capacidad, horarios y jornada. Y un agente de IA, orquestado con n8n, explica en palabras simples la propuesta. La IA no cambia las cifras: solo explica las del motor.» | 35 |
| 5 · 01:20–01:45 | Lámina A/B/C (`render-v3`, lámina 5) | Mostrar el panel con los clics de animación. | «Comparamos flota propia y un operador contratado con el costo completo de servir. La más barata llega tarde a parte de los críticos; el motor recomienda la opción C, con 90,8 puntos.» | 25 |
| 6 · 01:45–02:10 | `/public-trace/:unitId` (y, si está listo, la app móvil del conductor) | Abrir la traza pública de un bulto: hitos de bodega, despacho y recepción. | «Y cada entrega deja prueba: el bulto viaja con un QR y cualquiera puede consultar sus hitos, sin ver datos internos.» | 25 |
| 7 · 02:10–02:30 | Tarjeta de cierre | Tres columnas: Construido / Probado / Falta. Logo y «Buscamos una empresa para el piloto». | «Esto está construido y probado en un escenario sintético de Quito. Lo siguiente es un piloto con una empresa real.» | 20 |

**Si falla DeepSeek en el plano 4:** la tarjeta mostrará «⚠ Respuesta determinista». Se puede dejar y narrar «si la IA falla, el sistema responde con una explicación de respaldo»: es una capacidad probada (A-12).

### Capturas que necesitamos (para láminas y como respaldo del video)

Todas a 1920×1080, navegador sin barras de marcadores ni datos personales, con `demo-quito-planificacion` cargado.

| # | Pantalla | Qué debe verse | Uso |
|---|---|---|---|
| C1 | `/inicio` con la notificación de alerta abierta | Toast de alerta (nivel, punto, SKU, cobertura) y la campana con el conteo | Lámina 5 v4 y plano 2 |
| C2 | `/alertas` con el detalle de un punto crítico | Cobertura en días, demanda diaria, tiempo de reposición, cantidad sugerida | Lámina 5 v4 (alterna) y plano 3 |
| C3 | `/rutas` con la tarjeta «Propuesta del agente logístico» | Estado **Completado**, modelo y la explicación en texto | Lámina 4 v4 y plano 4 |
| C4 | `/rutas`, mapa con la ruta | Paradas numeradas y ventanas horarias | Plano 4 |
| C5 | `/public-trace/<bulto>` | Hitos de custodia de un bulto entregado | Lámina 3 v4 («Probar») y plano 6 |
| C6 | Editor de n8n con el workflow del agente | Nodos del flujo (disparo → DeepSeek → respuesta / respaldo), sin credenciales visibles | Lámina 4 v4 |
| C7 (opcional) | App móvil del conductor | Lista de paradas o escaneo de un bulto | Lámina 3 v4 |

`<bulto>` es un `unitId` existente en el entorno de demo; quien graba lo anota aquí antes de grabar.

**Duración objetivo:** 2 min 50 s (170 s). **Estado:** guion; grabación pendiente. El video enseña el alcance real descrito en la [matriz de afirmaciones](matriz-afirmaciones.md), con `demo-quito-planificacion`. El panel A/B/C muestra solo la columna A con el costo estimado del motor (ensayo sintético de #240, `demo-base`, commit `6adcf53`); B, C y la recomendación quedan «en integración» hasta que #241 entre en `develop`. Mientras tanto se conserva el marcador de captura y no se reemplaza por una captura ficticia.

| Plano / tiempo | Pantalla y rol | Acción | Texto en off | Duración |
|---|---|---|---|---:|
| 1 · 00:00–00:15 | Título / Narración | Abrir con nombre del módulo y rótulo «escenario sintético». | «Este video recorre el módulo de planificación con un escenario sintético de Quito. No representa clientes, tráfico en vivo ni operaciones medidas.» | 15 s |
| 2 · 00:15–00:40 | Web | Abrir `demo-quito-planificacion`; mostrar mapa y puntos, sin zoom a cifras operativas no verificadas. | «El escenario contiene puntos de servicio y referencias geográficas públicas; ventanas, demanda, costos e incidentes son supuestos del ejercicio. Esta vista es una demostración del escenario, no una operación real.» | 25 s |
| 3 · 00:40–01:05 | Lámina 4 / Web | Mostrar la lámina 4 (`render-v2/diapositiva-4.png`): columnas A/B/C con «flota propia», «propia, críticos primero» y «operador contratado». B y C dicen «en integración»; mantener visible el marcador `[CAPTURA PENDIENTE: pantalla A/B/C]` mientras no exista la toma conectada. | «Este es el panel de comparación. A es flota propia; B, flota propia con los críticos primero; C, un operador contratado. La comparación con el operador contratado y la recomendación calculada se integran en #241, por eso B y C todavía no tienen cifras ni hay una opción recomendada.» | 25 s |
| 4 · 01:05–01:30 | Lámina 5 / Técnica | Señalar la tabla A/B/C y su rótulo «Ensayo técnico del motor · escenario sintético de Quito». Leer totales, porcentajes, puntaje y recomendación; no leer desglose por componente. | «En un ensayo técnico con nuestro escenario sintético de Quito, el motor compara tres opciones. A, flota propia: la más barata, pero llega a tiempo al 83 % de los críticos. B llega al 100 %, con un 20,7 % más de costo. C, operador contratado, también llega al 100 % y cuesta un 4,3 % menos que B. El motor recomienda C, con 90,8 puntos. La más barata no siempre es la mejor. Son costos estimados, no mediciones de una empresa.» | 25 s |
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
