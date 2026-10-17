---
tipo: demo
estado: propuesta
actualizado: 2026-10-17
tags: [demo, pitch, video, guion]
---

# Guion del video de la demo — 2:30 (pitch v7)

Video de respaldo del pitch [[Guion del pitch]] (versión v7): muestra la cadena detectar → decidir → entregar → probar con pantallas reales del sistema. Su PDF es `Entregable/9-Guion-del-video.pdf`; **esta nota es la fuente**. Sustituye en lo esencial al guion v4 del spec 006 (`guion-video-respaldo.md`), que queda como antecedente.

**Reglas de grabación**

- Todo con el escenario sintético de Quito; el rótulo «escenario sintético» va fijo en una esquina durante todo el video, así no hay que repetirlo en la voz.
- Si una toma falla, se corta: no se simula nada ni se sustituye por una captura de otra pantalla.
- Grabar a 1920×1080, sin barra de marcadores ni datos personales; cuenta de planificador (sin nombres reales visibles). Voz sin música que compita.
- Duración objetivo 150 s (2:30), máximo 3 min.

## Plan de planos

| Plano / tiempo | Pantalla (ruta web) | Acción | Texto en off | s |
|---|---|---|---|---:|
| 1 · 00:00–00:08 | Tarjeta de título | La pregunta en grande sobre fondo oscuro. | «¿Cómo se entera una empresa de limpieza de que a un hospital le falta desinfectante? Hoy, cuando el hospital llama.» | 8 |
| 2 · 00:08–00:33 | `/alertas` (Monitoreo de cobertura) | Entrar como planificador. Mostrar la tabla; pasar a la pestaña «Quiebres críticos» (9) y a «Margen preventivo» (203); abrir «Ver detalle» de un punto. | «Con IstpetDev el aviso llega antes. El monitoreo de cobertura revisa el inventario y muestra qué punto entra en riesgo: cuántos días de cobertura le quedan, su nivel de riesgo y cuánto conviene reponer.» | 25 |
| 3 · 00:33–01:03 | `/alertas` → «Planificar rutas de entrega» → paso 2 «Recogidas y entregas» | Clic en el botón morado. En el paso 2, dejar correr la vista previa Bodega Sur → Clínica Sur (recomendación del planificador, km y costo). Clic en «Comparar y aprobar». | «Desde la alerta se arma la ruta. El sistema propone de dónde recoger y a dónde entregar, y la dibuja en el mapa con sus kilómetros y su costo. El planificador la revisa antes de seguir.» | 30 |
| 4 · 01:03–01:28 | `/rutas` | Seleccionar VEH-02; mostrar «Aprobar y publicar ruta» y el seguimiento en vivo de paradas. Clic en «Pedir al Agente IA» y esperar la tarjeta del agente; acercar a la explicación. | «Aprobada la ruta, el conductor la recibe y el planificador sigue cada parada. Un agente de inteligencia artificial, orquestado con n8n, explica la propuesta en palabras simples. La IA no cambia las cifras del motor: solo las explica.» | 25 |
| 5 · 01:28–01:53 | Lámina 4 del pitch v7 (A/B/C) | Mostrar el panel con sus clics de animación. | «Para decidir, el motor compara flota propia y operador contratado con el costo completo de servir. La más barata, A, llega a tiempo solo al 83 % de los críticos. El motor recomienda C, con 90,8 puntos. Es un ensayo técnico con datos sintéticos.» | 25 |
| 6 · 01:53–02:13 | `/novedades` | Recorrer los cuatro indicadores y el recuadro «Líneas rechazadas por motivo» (daño, error de rotulado). | «Y lo que pasa en el camino queda registrado. Las novedades de campo muestran los rechazos con su motivo, el impacto estimado de reentrada y los ajustes de percha.» | 20 |
| 7 · 02:13–02:30 | Tarjeta de cierre | Tres columnas: Construido / Probado / Siguiente fase, con «Buscamos una empresa para el piloto». | «Esto está construido y probado con un escenario sintético de Quito. Lo siguiente es un piloto con una empresa real.» | 17 |

**Suma:** 8 + 25 + 30 + 25 + 25 + 20 + 17 = **150 s**.

## Si algo falla en la grabación

- **El agente de IA no responde (plano 4):** si la tarjeta muestra «Respuesta determinista», se deja y se narra «si la IA falla, el sistema responde con una explicación de respaldo»; es una capacidad registrada. Si no aparece nada, se corta el plano y se sigue con el 5.
- **El mapa no carga (planos 3 y 4):** esperar las teselas; sin conectividad, no recalcular ni inventar una ruta: cortar el plano y dejar una tarjeta de texto.
- **La vista previa de la ruta se ve vacía:** repetir la carga del paso 2; no usar una imagen fija en su lugar.

## Capturas de respaldo

Ya existen en `docs/specs/006-validacion-pitch-entrega/materiales/assets/capturas-v5/`: `alertas.png` (plano 2), `ruta-paso2-modal.png` y `ruta-paso2.gif` (plano 3), `ruta-vehiculo.png` y `ruta-seguimiento.png` (plano 4) y `novedades.png` (plano 6). Sirven para reemplazar un plano solo si se rotula como captura.

## Checklist previa

- [ ] Datos de demo cargados y backend respondiendo; notificaciones y datos personales cerrados.
- [ ] Cuenta de planificador sin nombres reales a la vista.
- [ ] Rótulo «escenario sintético» puesto en la esquina durante todo el video.
- [ ] Reproducir el MP4 completo: duración ≤ 3 min, audio sincronizado, ninguna cifra distinta de las del pitch.
- [ ] Revisar el «255 min» del paso 2 de la ruta antes de grabar: confirmar qué mide o evitar acercar la cámara a ese dato.
