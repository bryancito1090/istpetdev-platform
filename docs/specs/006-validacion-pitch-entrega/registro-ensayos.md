# 006 — Registro de ensayos y respuestas (T5, fase 3 del issue #46)

**Preparado:** 14-oct-2026 por Claude Code (Opus 5.5) a pedido de Joseph Baño. **Sin pasadas registradas:** Joseph informó el 14-oct que todavía no hubo ensayos. Los segundos del [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md) son reparto planificado y **no** se copian aquí como tiempos medidos. [Tareas](task.md) · [E2E-03/04/06](test-e2e.md) · [Escena y contingencias](escena-y-contingencias.md).

## Reglas

- Una fila por pasada real, escrita por quien cronometra, incluso si falla. Esta IA no completa filas.
- **Duración medida:** de la primera palabra al cierre, con cronómetro. Meta: tiempo oficial −20 s; mientras O-04 siga pendiente, 160/280/400 s son metas internas y la pasada cuenta como preparación.
- **Pasada completa sin fallo:** siete bloques en orden, cinco papeles hablan, ninguna frase fuera de la columna «Frase vigente» de la [escena](escena-y-contingencias.md#2-escena-del-guion-maestro-de-5-min-acción-visible-relevo-y-sustitución), duración ≤ meta, y ninguna contingencia que exceda su bloque.
- **Racha:** suma 1 por pasada completa sin fallo; **cualquier fallo la deja en 0**. Cambiar de versión (guion, diapositivas o commit) en una escena obliga a repetir al menos una pasada de esa versión.
- CE-04 se cierra solo con **diez pasadas consecutivas** del corte oficial y las **dos últimas sin la wifi del recinto** (conexión de respaldo elegida para la escena).
- Papeles: anotar quién ocupó cada uno en esa pasada (N/W/M/T/G = narración, web, móvil, técnica, negocio). Si un suplente reemplazó al titular, anotarlo.

## Pasadas del pitch

| # | Fecha/hora | Corte | Duración medida (s) | Meta (s) | N / W / M / T / G | Versión guion/diapositivas | Commit | Conexión | Fallos / contingencias usadas | Enlace (video interno o nota) | Racha |
|---:|---|---|---:|---:|---|---|---|---|---|---|---:|
| — | — | — | — | — | — | — | — | — | — | — | 0 |

**Racha actual: 0 / 10.** Pasadas sin wifi del recinto al final de la racha: 0 / 2.

## Ensayos funcionales de la demo

Pasadas técnicas del protocolo de la [sección 4 de escena](escena-y-contingencias.md#4-ensayo-funcional-y-video-de-respaldo-del-15-oct). Una pasada funcional exitosa habilita la columna «Si E2E-04 pasa conectado» solo para las partes que recorrió y en ese commit.

| Fecha/hora | Commit | Entorno (nube/local) | Dispositivos | Partes recorridas conectadas (3a–3e) | Contingencias provocadas (C-01–C-05) y resultado | Resultado | Enlace | Quién |
|---|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — | — | — |

## Ronda de veinte respuestas

Se cronometra **aparte** del pitch. Una persona lee la pregunta del [banco](../../boveda/07%20Demo/Guion%20del%20pitch.md); quien responde no la conoce de antemano. Límite: 30 s por respuesta; sin precio ni cifra improvisada; si la respuesta exige evidencia que no existe, se marca.

| Ronda | Fecha/hora | Versión del banco (commit) |
|---|---|---|
| — | — | — |

| # | Pregunta (banco) | Quién respondió | Tiempo medido (s) | ≤30 s | Afirmó algo sin evidencia (ID de matriz) | Ajuste al borrador |
|---:|---|---|---:|---|---|---|
| 1 | ¿La IA predice el consumo? | — | — | — | — | — |
| 2 | ¿La IA aprueba rutas sola? | — | — | — | — | — |
| 3 | ¿Qué ocurre si DeepSeek cae? | — | — | — | — | — |
| 4 | ¿Es la ruta óptima? | — | — | — | — | — |
| 5 | ¿Qué datos requiere? | — | — | — | — | — |
| 6 | ¿Qué pasa si nos dan otro Excel? | — | — | — | — | — |
| 7 | ¿Cómo conoce el stock? | — | — | — | — | — |
| 8 | ¿Qué pasa si no hay señal? | — | — | — | — | — |
| 9 | ¿Se duplica una recepción? | — | — | — | — | — |
| 10 | ¿Puede cualquiera escanear la caja? | — | — | — | — | — |
| 11 | ¿Funciona con 80 puntos o a escala nacional? | — | — | — | — | — |
| 12 | ¿Qué prueba la simulación? | — | — | — | — | — |
| 13 | ¿Por qué esos baselines? | — | — | — | — | — |
| 14 | ¿Qué significan quiebres evitados y nuevos? | — | — | — | — | — |
| 15 | ¿Por qué tres seeds y una adversa? | — | — | — | — | — |
| 16 | ¿Cuánto ahorra una empresa real? | — | — | — | — | — |
| 17 | ¿Cuánto cuesta y cuál es el precio? | — | — | — | — | — |
| 18 | ¿Evita multas hospitalarias? | — | — | — | — | — |
| 19 | ¿Qué validaron con el sector? | — | — | — | — | — |
| 20 | ¿Qué es trabajo previo y qué se hizo aquí? | — | — | — | — | — |

CE-05 se cumple con una ronda en la que las veinte respuestas quedan ≤30 s sin afirmaciones fuera de la [matriz](matriz-afirmaciones.md). Las respuestas 3, 8, 9 y 10 dependen de evidencia que hoy falta (A-12, A-15, A-14, A-16): se responden con su sustitución.
