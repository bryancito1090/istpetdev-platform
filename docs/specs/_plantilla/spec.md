---
id: NNN
titulo: Nombre del spec
estado: borrador            # borrador | aprobado | planificado | en curso | cerrado
creado: AAAA-MM-DD
actualizado: AAAA-MM-DD
aprobado_por: PENDIENTE     # nombres y fecha; «el usuario» no vale (Convenciones y estados)
depende_de: []              # IDs de specs
boveda: []                  # notas de docs/boveda que este spec implementa o modifica
---

# NNN — Nombre del spec

> Qué y por qué. Sin detalles de implementación: eso va en `plan.md`. Borrar las notas entre `>` al completar.

## Contexto

> Dos o tres párrafos: problema que resuelve, estado actual del código y por qué ahora. Enlazar notas de la bóveda en vez de repetirlas.

## Objetivo y aporte a la nota

> Una frase de objetivo. Después, qué criterio de la matriz oficial (Evaluacion y entregables) mejora y con qué evidencia visible para un jurado no técnico.

| Criterio | Peso | Cómo lo sostiene este spec |
|---|---:|---|
| | | |

## Historias de usuario

> Ordenadas por prioridad (P1 = imprescindible para la demo). Cada historia se puede probar sola.

### H1 — Título (P1)

**Como** rol, **quiero** acción, **para** beneficio.

**Por qué esta prioridad:** …

**Escenarios de aceptación:**

1. **Dado** …, **cuando** …, **entonces** ….
2. **Dado** …, **cuando** …, **entonces** ….

## Requisitos

### Funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| FR-01 | El sistema DEBE … | RF-xx |

### No funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| NFR-01 | … | RNF-xx |

## Entidades y contratos

> Entidades nuevas o modificadas (nombres de Hechos canonicos, sin sinónimos) y endpoints o eventos que otros specs consumirán.

## Guardarraíles aplicados

> Cuáles de G1–G8 (docs/specs/README.md) aplica y cómo se prueban aquí.

## Fuera de alcance

> Lista explícita de lo que NO se hace, para que nadie lo implemente «de paso».

## Contradicciones con la bóveda

> Usar el formato de Convenciones y estados. Si no hay, escribir «Ninguna detectada» y qué notas se revisaron.

> **⚠️ Contradicción detectada** — Hecho nuevo: … · Hecho establecido: … (nota y sección) · Impacto: … · Opciones: … · Propuesta: …

## Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|

## Criterios de éxito

> Medibles y verificables en `test-e2e.md`. Sin «rápido» ni «robusto» sin número.

- CE-01: …

## Preguntas abiertas

> Deben quedar vacías antes de pasar a `aprobado`. Cada una con quién decide.
