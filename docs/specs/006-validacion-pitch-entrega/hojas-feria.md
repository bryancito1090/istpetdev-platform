# 006 — Hojas de feria y corrida de 005 (T3, fase 3 del issue #46)

**Preparado:** 14-oct-2026 por Claude Code (Opus 5.5) a pedido de Joseph Baño. **Las hojas están vacías:** las entrevistas son el 16-oct y el retorno con prototipo el 17-oct. No hay participantes, citas ni validación sectorial registrados. [Tareas](task.md) · [E2E-02](test-e2e.md#e2e-02--feria-sin-fabricar-validación) · [Guion de feria y hoja de campo](../../boveda/06%20Equipo/Registro%20de%20mentorias.md) · [Plantilla evidencia](../../boveda/09%20Plantillas/Plantilla%20evidencia.md).

## Reglas

- Se usan las cinco preguntas, la apertura y el cierre del [guion de feria](../../boveda/06%20Equipo/Registro%20de%20mentorias.md). No se añaden preguntas que sugieran la respuesta.
- Cada conversación recibe un código: `F16-01`, `F16-02`… el 16; `F17-R01`… en el retorno del 17. Si el 17 se vuelve con la misma persona, se anota su código del 16 en «Vuelve de».
- **Tres permisos separados**, cada uno con su propia respuesta: (1) notas anónimas, (2) cita textual sin nombre, (3) mención de rol u organización junto a la cita. Ninguno se deduce de otro. Sin permiso 1 solo se anota «intento sin consentimiento».
- No se anota nombre personal, teléfono, correo, firma, foto ni audio. «Sector/rol» solo si la persona lo ofrece.
- No se aborda JIMCORPSERVI/FEDELIMP ni LACIF (cautela del equipo, ER-03). Si se descubre a mitad de conversación, se cierra con amabilidad y se anota como intento.
- Una fila se escribe **después** de la conversación, por quien la hizo. Esta IA no completa filas.
- Un rechazo cuenta como intento, no como conversación (E2E-02).

## Registro de intentos (16 y 17-oct)

| Código | Fecha/hora | Pausa | Entrevistadores (papel o nombre del equipo) | Resultado: conversación / rechazo / cautela | Permiso 1 notas | Permiso 2 cita | Permiso 3 rol u org. | Hoja |
|---|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — | — | — |

## Hoja de conversación del 16-oct (copiar una por código)

```text
Código: F16-__            Fecha/hora: 16-oct __:__   Pausa: 11:00–11:30 / 13:00–14:00
Entrevistadores: __________   Contexto (stand, tipo de expositor ofrecido): __________
Cautela de jurado comprobada: ☐ sí
Permiso 1 — notas anónimas:              ☐ sí ☐ no   (si «no»: parar aquí)
Permiso 2 — cita textual sin nombre:     ☐ sí ☐ no ☐ no se preguntó   hora: __:__
Permiso 3 — rol/organización con la cita: ☐ sí ☐ no ☐ no se preguntó   qué se autorizó: ____
Vuelve el 17 con prototipo:              ☐ sí ☐ no ☐ no se preguntó

P1 ¿Cómo saben hoy que un punto se va a quedar sin un insumo?
   [literal entre comillas | «paráfrasis»]
P2 ¿Les ha pasado tener que cambiar una entrega o una ruta ya planificada? ...
P3 ¿Cómo comprueban lo enviado y lo recibido, incluso si falla la conexión?
P4 Si un punto se queda sin un insumo, ¿qué pasa después? ¿Queda registrado?
P5 Si probáramos una alerta, ruta y recepción con QR, ¿qué le impediría usarlo?

Objeción principal: __________
Supuesto que contradice (ID de matriz o hipótesis del Modelo de negocio): ______
Cambio propuesto o pendiente (enlace a issue/nota): __________
Fuente complementaria ofrecida (sin datos personales): __________
```

## Hoja de retorno con prototipo del 17-oct, 11:00–12:30

Se muestra primero la pantalla **sin explicar**. Se etiqueta lo mostrado (vivo / grabado / captura / diseño) igual que en el pitch.

```text
Código: F17-R__   Vuelve de: F16-__ / nueva   Fecha/hora: 17-oct __:__
Entrevistadores: __________
Permisos 1/2/3: confirmar de nuevo o pedir por primera vez (mismas casillas que el 16)
Qué se mostró y con qué etiqueta: __________   commit/versión: ______
Pregunta: «¿Qué haría ahora?» → respuesta antes de cualquier explicación: ______
Interpretó la alerta: ☐ sí ☐ parcial ☐ no   la ruta: ☐ ☐ ☐   el estado de la caja: ☐ ☐ ☐
Duda o confusión observada: __________
Corrección necesaria (guion, pantalla o matriz): __________   enlace: ______
```

## Hojas completadas

| Código | Resumen anonimizado | Objeción → decisión o pendiente | Evidencia | Cita autorizada para el pitch |
|---|---|---|---|---|
| — | — | — | — | — |

CE-02 se cumple solo con ≥3 conversaciones con permiso 1 antes del 17-oct 12:30, ≥1 hoja de retorno con reacción al prototipo y ≥1 objeción vinculada a una decisión o pendiente. Si no se alcanza, el guion dice «es una hipótesis que estamos validando con expositores de la feria» (A-29) y no «validado por el sector».

## Mentorías

Las mentorías del 16 y 17 se registran con la [Plantilla mentoria](../../boveda/09%20Plantillas/Plantilla%20mentoria.md) en el [Registro de mentorias](../../boveda/06%20Equipo/Registro%20de%20mentorias.md), indicando si el mentor podría integrar el jurado. Una recomendación de mentor no es validación sectorial ni regla oficial. Sin registros al 14-oct.

## Corrida oficial de 005

Las cifras de 005 entran en el pitch **solo** si se completa esta lista con una corrida real. Estado al 14-oct-2026: **no existe corrida oficial.** El [registro de ejecuciones de 005](../005-panel-simulador/test-e2e.md#registro-de-ejecuciones) está vacío y la [evidencia de 005](../../superpowers/evidence/005-e2e.md) no tiene `comparisonId`, ZIP, hashes, host ni cifras (matriz, EJ-05). A-19 sigue como propuesta y A-21 retirada.

| Comprobación | Valor | Verificado por / fecha |
|---|---|---|
| ZIP exportado (nombre y SHA-256) | — | — |
| `comparisonId` | — | — |
| `manifest.json`: SHA-256 de cada archivo de datos coincide; `ETag` = SHA-256 del manifiesto | — | — |
| Host / máquina dedicada | — | — |
| Commit del código | — | — |
| Seeds base (tres) y escenario adverso aparte | — | — |
| Dataset, matriz y `policyVersion` | — | — |
| Importado con `POST /api/v1/simulations/comparisons/import` y cifras iguales a la pantalla | — | — |
| Fila en el registro de ejecuciones de 005 | — | — |

Con la lista completa, pasar el rango a A-21 de la matriz con su enlace y pedir la revisión de E2E-05. Si falta cualquier casilla, C-05 sigue vigente.
