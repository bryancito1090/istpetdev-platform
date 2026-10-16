---
tipo: guia
estado: vigente
actualizado: 2026-10-16
tags: [hackathon, equipo, evento, notas]
---

# Notas del evento

Esta es la nota índice de `NotasEvento/` dentro de la bóveda Obsidian. Cada intervención, observación o bloque de dictado se guarda en **un archivo nuevo** para que varias personas puedan registrar información a la vez. Usa [[Plantilla nota del evento]] para crear esos archivos. [[Plantilla notas del participante]] contiene el contexto del evento y una ficha opcional de preparación.

## Nombre de cada archivo

Usar `AAAA-MM-DD-HHMM-autor-tema.md`, con fecha y hora de **registro** en `America/Guayaquil` (UTC−05:00), reloj de 24 horas, letras minúsculas ASCII, números y guiones. `tema` es una descripción corta de 2 a 5 palabras, sin tildes ni espacios. Ejemplo: `2026-10-16-1432-jd-pregunta-del-mentor.md`.

| Código de autor | Integrante |
|---|---|
| `bb` | Bryan Baño |
| `jd` | Jorge Doicela |
| `jb` | Joseph Baño |
| `ap` | Anthony Pilatasig |
| `am` | Alexander Martínez |

El código identifica a **quien registra** la nota, aunque otras personas hayan hablado. Si registra alguien más, usar un identificador propio y estable de 2 a 12 caracteres ASCII y anotarlo en `Registró`. Si ya existe ese nombre, añadir `-02`, `-03`, etc. después del tema; **nunca reemplazar un archivo ajeno**. Si dos ramas generan el mismo nombre antes de fusionarse, quien integre la segunda renombra su archivo con el siguiente sufijo libre y revisa sus enlaces. La hora del hecho puede diferir de la del registro: anotarla dentro de la nota, sin renombrar el archivo. Si no se conoce, escribir `PENDIENTE`.

## Cómo registrar y compartir

1. Envía el apunte en lenguaje natural, incluso si está incompleto o desordenado. Incluye, si los sabes: quién lo observó, cuándo ocurrió, quién habló, qué dijo y qué queda por hacer. Una foto, audio o documento se conserva solo en un lugar autorizado; aquí se enlaza su referencia permitida.
2. Quien transcribe copia [[Plantilla nota del evento]] a un archivo nuevo con el nombre anterior. Conserva el texto recibido con correcciones mínimas de legibilidad y marca cualquier omisión necesaria como `[dato reservado]`. Después separa lo observado de interpretaciones y tareas, y deja `PENDIENTE` para datos desconocidos. Una cita solo va entre comillas si se dictó literalmente; una paráfrasis se identifica como tal.
3. Antes de compartir, revisa nombres de terceros, cédulas, teléfonos, correos, credenciales, firmas, fotos y audios. Esta bóveda compartida no debe contener esos datos. Para entrevistas, respeta los permisos separados descritos en [[Registro de mentorias]]; si no hubo permiso para registrar respuestas, anota únicamente el intento sin consentimiento.
4. Cada integrante trabaja en su rama de `develop` actualizado y envía un PR a `develop`. Si llegan notas simultáneas, crea archivos distintos. Busca los apuntes por carpeta, fecha, autor o texto en Obsidian; **no añadas una fila al índice por cada nota**, porque ese archivo sería un punto de conflicto. Antes de publicar, ejecuta `python docs/boveda/.github/scripts/check_vault.py` desde la raíz del repositorio y revisa el diff. No hagas push directo a `develop`.

Para pedirle a un asistente que lo haga: **«Registra esto en NotasEvento siguiendo [[Notas del evento]] y [[Plantilla nota del evento]]. Autor que registra: [código]. Hora del hecho: [si se sabe]. Apunte: [dictado]. Conserva dudas y citas exactas; no inventes datos ni edites notas previas.»** El asistente debe devolver el nombre del archivo creado y las dudas pendientes.

## Estado y seguimiento

El frontmatter `tipo`, `estado`, `actualizado` y `tags` es obligatorio. La copia de la plantilla empieza con `tipo: nota-evento` y `estado: pendiente`. Tras revisar que la transcripción representa fielmente el relato, puede pasar a `vigente`. `validado` exige evidencia enlazada. Al corregir una nota, actualizar la fecha y registrar la aclaración al final. Para una nueva intervención, crear otro archivo y enlazar la nota anterior; no mezclar dictados distintos en un archivo compartido.

Una nota de evento no convierte por sí sola una opinión en regla oficial ni una tarea en resultado verificado. Si aporta una decisión o evidencia, pasarla a la nota dueña según [[Convenciones y estados]]. El resumen de `check_vault.py` calcula los conteos actuales: no hay que editar [[Fuente y control documental]] por cada apunte; sus entradas son registros históricos de cambios documentales relevantes.
