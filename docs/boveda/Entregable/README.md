---
tipo: demo
estado: propuesta
actualizado: 2026-10-17
tags: [entregable, pdf, indice]
---

# Carpeta Entregable

Aquí van **solo PDF actualizados** de lo que se entrega al jurado, más el PPTX del pitch (con los GIF animados). No se edita nada en esta carpeta: cada PDF se genera desde su fuente y se vuelve a copiar aquí. Si cambia la fuente, se regenera el PDF.

| PDF | Qué es | Fuente (se edita allí) |
|---|---|---|
| `1-Problema.pdf` | Entregable 1: problema | `docs/specs/006-validacion-pitch-entrega/materiales/entregables/e1_problema.py` |
| `2-Solucion.pdf` | Entregable 2: solución y propuesta de valor | nota «Entregable Solucion y propuesta de valor» (07 Demo) y `materiales/entregable-2/` |
| `3-Prototipo-demo.pdf` | Entregable 3: prototipo y demo | `materiales/entregables/e3_prototipo.py` |
| `4-Validacion.pdf` | Entregable 4: validación | `materiales/entregables/e4_validacion.py` |
| `5-Viabilidad.pdf` | Entregable 5: viabilidad | `materiales/entregables/e5_viabilidad.py` |
| `6-Modelo-de-negocio.pdf` | Entregable 6: modelo de negocio | `materiales/entregables/e6_modelo_negocio.py` |
| `7-Pitch.pdf` | Diapositivas del pitch v8, 10 láminas, con imágenes fijas. **La lámina 10 muestra la clave de la cuenta del jurado (Auditor, solo lectura)** | `materiales/generar_v8.py` (capturas en `materiales/assets/capturas-v5/`) |
| `7-Pitch.pptx` | Las mismas 10 láminas con animaciones y el GIF de rutas (lámina 5); es el que se presenta. Incluye la clave del jurado | `materiales/generar_v8.py` (lee `~/.config/istpetdev/private/jurado-pitch-v8.json`) |
| `8-Guion-del-pitch.pdf` | Guion hablado v8, 10 láminas, meta 4:50; no contiene la clave | `guion-pitch-v8.md` (spec 006); se regenera con `materiales/generar_v8.py` |
| `9-Guion-del-video.pdf` | Guion del video de la demo, 2:30, plano por plano | nota «Guion del video» (07 Demo); se regenera con `materiales/generar_guion_video_pdf.py` |
| `04-flujo-concurrente-campo-3-roles.gif` | Demostración animada del flujo móvil concurrente en campo (3 roles: bodega, conductor y receptor) | `docs/specs/004-campo-qr-custodia-offline/evidencia/` |

## Pendiente

- Grabar el video con el guion 9 y revisar el «255 min» del paso 2 de rutas.
- Ensayar el recorrido sin señal en un teléfono real (modo avión, reinicio y sincronización) y grabarlo; hasta entonces el pitch lo dice como siguiente fase.
- Simulación de 90 días: la corrida del 17-oct no muestra ahorro frente al calendario fijo; el 9,8 % se retiró de los entregables 4 y 6.
- Al terminar la evaluación, desactivar la cuenta `jurado@expo.ec` desde Usuarios (Gerencia): su clave está en `7-Pitch.*` y en el historial de Git.
- Esta carpeta solo guarda PDF, el PPTX del pitch y este índice; el guion en texto vive en `07 Demo/Guion del pitch.md`.
