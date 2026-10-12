---
tipo: guia
estado: vigente
actualizado: 2026-10-07
tags: [hackathon, equipo]
---

# Convenciones y estados

## Organización

Usamos Markdown y enlaces internos de Obsidian. La bóveda no necesita plugins adicionales. Los nombres de archivos son únicos y sin tildes para facilitar enlaces entre sistemas; el contenido sí usa español normal.

Cada nota tiene `tipo`, `estado`, `actualizado` y `tags`. Actualizar la fecha al cambiar el contenido.

| Estado | Significado |
|---|---|
| vigente | Guía o resumen utilizable de una fuente conocida |
| propuesta | Diseño que el equipo todavía debe validar |
| pendiente | Falta información o una decisión |
| aceptado | Se registró quién decidió y cuándo |
| validado | Hay evidencia enlazada del resultado |
| reemplazado | Existe una decisión posterior enlazada |
| plantilla | Formato para copiar y completar |

Una propuesta técnica **no certifica una implementación**. Una decisión aceptada tampoco equivale a una funcionalidad terminada.

Una decisión solo puede marcarse **aceptada** si registra sus **Deciders** (personas o roles que decidieron), la fecha y el origen (acta, mentoría o instrucción de una persona identificada). «El usuario» sin nombre no es un Decider válido: un asistente de IA lo interpreta como su propio usuario. Usar [[Plantilla ADR]].

## Tipos de afirmación

| Etiqueta | Cómo usarla |
|---|---|
| Oficial | Citar página física y sección del PDF |
| Información del equipo | Citar [[Informacion inicial del equipo]] |
| Propuesta | Explicar resultado esperado y aceptación |
| Supuesto | Indicar cómo se validará y qué cambiaría si falla |
| Medido | Enlazar ejecución, dataset, parámetros y resultado |
| Pendiente | Registrar información faltante y fecha de revisión |

Las páginas del manual se cuentan desde la portada, que es la página 1. Sus cuadros fueron revisados como imágenes; extraer solo el texto omite información importante. Las tablas copiadas del manual se marcan «transcrito de imagen, p. X».

## Reglas contra la invención (personas y asistentes de IA)

- **Hechos canónicos.** Versiones, cifras, rutas, puertos, nombres en código y estado de ADR salen de [[Hechos canonicos]]. Si un dato no está ahí o dice PENDIENTE, no se completa por suposición: se escribe `PENDIENTE` y se pregunta.
- **Certeza.** Al registrar un dato nuevo: ✅ confirmado (con fuente) o ⚠️ a validar (propuesta, supuesto o dato con contradicción). Lo ⚠️ no se usa en código ni en el pitch como si fuera confirmado.
- **Contradicciones.** Si un dato nuevo choca con uno establecido, no se arregla en silencio. Se reporta así y se registra en [[Hechos canonicos]]:
  > **⚠️ Contradicción detectada** — Hecho nuevo: … · Hecho establecido: … (nota y sección) · Impacto: … · Opciones: 2 o 3 alternativas.
- **Código en notas.** Todo bloque de código dentro de la bóveda es un **ejemplo no probado** salvo que enlace evidencia de ejecución. No se copia a un repositorio como si estuviera validado.
- **Lenguaje.** En notas de propuesta no usar «garantiza», «instantáneo», «cero», «imposible» o «absoluto» como hechos. Escribir el objetivo y si está medido.
- **Una sola fuente.** Antes de repetir una regla en otra nota, enlazar la nota que ya la contiene.
- **Verificación.** Antes de publicar cambios: `python .github/scripts/check_vault.py`.

## Trabajo compartido

- Coordinar cambios por entregable para evitar ediciones incompatibles.
- Actualizar la nota fuente antes de repetir información en otras notas.
- Contratos compartidos: [[Contratos API y eventos]].
- Fórmulas oficiales del equipo: [[Simulador y metricas]].
- Registrar cambios relevantes en [[Fuente y control documental]] y decisiones en [[Decisiones de arquitectura]].
- Las skills del equipo y las reglas para asistentes de IA se documentan en [[Catalogo y plan de skills]]; sus archivos (`AGENTS.md`, `.agents/`, `.claude/`) son locales y no se suben a esta bóveda.
- Compartimos la bóveda mediante Git y GitHub en bryancito1090/istpetdev-hackathon-2026. Hacer cambios pequeños y revisar conflictos antes de publicar.
- Trabajar los cambios en `develop` y actualizarla con `git pull --ff-only` antes de editar. Integrar en `main` cuando estén listos. Si hay divergencia, integrar conservando los cambios; no forzar el push para resolverla.
- `.obsidian/workspace*.json` guarda sesiones personales y `.obsidian/graph.json` guarda preferencias del grafo. Ambos están excluidos mediante `.gitignore`, junto con logs, cachés, papelera y temporales; la configuración común sí se versiona.

## Fuentes y evidencia

Conservar fuentes en `99 Fuentes`. Crear `99 Fuentes/Evidencias` cuando haya resultados. Mantener fuera de esta bóveda documentos de identidad, datos bancarios, credenciales, fotos de terceros y firmas reales.

Para pruebas usar [[Plantilla evidencia]]; para mentorías, [[Plantilla mentoria]]; para decisiones, [[Plantilla ADR]].
