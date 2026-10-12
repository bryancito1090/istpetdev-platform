---
tipo: plan-skills
estado: vigente
actualizado: 2026-10-09
tags: [istpetdev, documentacion, ia]
---

# Catálogo y plan de skills

Esta nota documenta las skills del equipo y de dónde sale cada una; el contenido completo de cada skill está en su propia nota de esta carpeta (enlaces en el catálogo). **Los archivos `SKILL.md` no se suben a esta bóveda:** la bóveda es documentación y contexto. Cada integrante puede tenerlos en su equipo para su asistente de IA. Todas remiten a [[Hechos canonicos]] para todo dato no cerrado, de modo que ningún asistente complete con suposiciones.

## Dónde están y quién las lee

| Archivo (local, no versionado) | Para qué | Quién lo lee |
|---|---|---|
| `AGENTS.md` | Reglas comunes, orden de lectura e índice de notas | Antigravity, Codex, GitHub Copilot, Cursor y otros compatibles con AGENTS.md |
| `CLAUDE.md` / `GEMINI.md` | Importan `AGENTS.md`; sin reglas propias | Claude Code / Gemini CLI |
| `.agents/skills/<nombre>/SKILL.md` | Skills en formato Agent Skills | Antigravity, Codex, VS Code/Copilot, Cursor y otros compatibles |
| `.claude/skills/` | Copia de `.agents/skills/` | Claude Code |
| `.local-skills/<nombre>/SKILL.md` | Copia local generada cuando las rutas habituales están restringidas; AGENTS.md la enlaza | Asistente que lea las instrucciones locales del proyecto |

`.gitignore` excluye estos archivos. Lo que sí se versiona es `.github/scripts/check_vault.py`, el verificador de la bóveda que ejecuta el CI (`.github/workflows/check-vault.yml`).

**Pendiente (C-06):** comprobar en cada herramienta que use el equipo que descubre las skills desde esa ruta. Si alguna no lo hace, se agrega su ruta como copia generada por el mismo script, nunca como segunda fuente editable.

## Catálogo

| Skill | Se activa para | Origen |
|---|---|---|
| [[istpetdev-contexto]] | Cualquier tarea; datos faltantes, contradicciones, certeza de afirmaciones | Nueva + `business-analyst-reglas-negocio` + `continuidad-narrativa` + patrón anti-invención de Antigravity |
| [[istpetdev-docs]] | Editar la bóveda, ADR, mentorías, evidencias, hechos canónicos | `tech-writer` + `product-manager` + `continuidad-narrativa` |
| [[istpetdev-contrato]] | Endpoints, eventos, DTOs, errores, idempotencia, OpenAPI | `tech-writer` (OpenAPI) |
| [[istpetdev-backend]] | Código C#/.NET, inventario, custodia, RBAC, tiempo real | `backend-lead-cqrs` + `arquitecto-clean-architecture` |
| [[istpetdev-dbflow]] | Bases aisladas personales (dev_01 a dev_05), base de integración, conexión túnel SSM, roles DML vs DDL, migraciones EF Core y resolución de conflictos de esquema | Nueva para la colaboración del equipo en AWS RDS + `data-architect-dba` + `istpet-gitflow` |
| [[istpetdev-datos]] | Modelo de datos, migraciones, riesgo, rutas, simulador, KPIs | `data-architect-dba` + `qa-testing-engineer` |
| [[istpetdev-frontend]] | Angular web, PWA o app móvil, QR público, estados de UI, accesibilidad | `frontend-lead-personalidad` + `design-tokens-a11y` + `identidad-visual-brand` |
| [[istpetdev-revision]] | Commits, PR, checklist, pruebas, seguridad | `code-review-assistant` + `qa-testing-engineer` + `devsecops-github-guardian` + `gitlab-workflow-istpet` + `tech-writer` (+ `infra-devops-cloud` y `observability-performance` como referencias) |
| [[istpetdev-prelaunch]] | Antes de arranque/ensayo/despliegue: evidencia vigente y controles complementarios de RLS, campos/plantillas, datos clasificados, abuso, respuesta web y dependencias | Nueva por solicitud de Bryan, 2026-10-09; reutiliza las otras skills sin duplicar sus checklists |
| [[istpetdev-producto-pitch]] | Problema, usuarios, validación, negocio, privacidad, pitch | `product-manager` + `ux-researcher` + `privacidad-legal-compliance` |

Los nombres de la columna «Origen» son skills personales de un integrante (carpeta `~/.gemini/config/skills/` de su equipo), adaptadas para este proyecto. La carpeta personal no se modificó.

## Qué se tomó de las skills personales

| Skill personal | Elemento adaptado |
|---|---|
| `business-analyst-reglas-negocio` | «Nunca inventar reglas»; certeza ✅ confirmada / ⚠️ a validar; una pregunta a la vez; no borrar reglas, reemplazarlas |
| `continuidad-narrativa` | Protocolo «⚠️ Contradicción detectada»: hecho nuevo, hecho establecido, impacto, opciones; prohibido reparar en silencio |
| `product-manager` | ADR con Deciders y estado «reemplazado por»; propuesta (RFC) breve; MoSCoW |
| `data-architect-dba` | Integridad en la base; índices con `EXPLAIN`; migraciones reversibles y nunca editadas tras aplicarse; respaldo probado; RPO/RTO |
| `backend-lead-cqrs` | Command separado de Query; DTO siempre; errores tipados; trazar cada handler a un requisito; diffs antes que reescrituras |
| `arquitecto-clean-architecture` | Dependencias hacia el dominio; dominio sin frameworks |
| `code-review-assistant` | Conventional Commits; severidades BLOCKER/WARNING/SUGGESTION/INFO; checklist por categorías |
| `qa-testing-engineer` | Pirámide de pruebas; integración con base real; datos deterministas; prueba por cada defecto; pruebas intermitentes no se ignoran |
| `devsecops-github-guardian` | Secretos fuera del repositorio; `.env.example`; protección de ramas |
| `gitlab-workflow-istpet` | Asunto de commit de 10 a 72 caracteres; plantilla de MR adaptada a PR de GitHub |
| `tech-writer` | Plantilla de PR; OpenAPI documentado por endpoint; no duplicar definiciones |
| `infra-devops-cloud` | Docker multi-etapa, sin `root`, `.dockerignore`, health checks con dependencias, etiquetado de costos |
| `observability-performance` | Logs JSON con `correlationId`; p95/p99; no registrar datos sensibles |
| `frontend-lead-personalidad` | Cinco estados de UI; declarar arquetipo; iconos de un sistema real |
| `design-tokens-a11y` | No inventar valores de diseño; tokens en tres capas; WCAG 2.2 AA; leyes de UX |
| `identidad-visual-brand` | Evitar el aspecto genérico de IA; un elemento firma; textos desde el usuario |
| `ux-researcher` | Personas hipotéticas a validar; JTBD; prueba con cinco usuarios por tareas |
| `privacidad-legal-compliance` | LOPDP Ecuador: finalidad, consentimiento expreso, eliminación |

## Qué no se tomó y por qué

| Elemento | Motivo |
|---|---|
| Preguntar «¿qué stack usamos?» en cada skill | El stack está en [[Hechos canonicos]]; se pregunta solo lo PENDIENTE |
| Estructura Angular con NgModules, `src/components/ui/`, Tailwind v4 fijo | Contradice ADR-13 (FSD standalone) y ADR-04 |
| MediatR y repositorio genérico obligatorios | [[Backend y tiempo real]] los declara opcionales |
| Catálogo `RN-xxx` obligatorio | La bóveda ya usa RF/RNF/ADR; un segundo catálogo duplica |
| Un commit por issue con `amend` y `--force-with-lease` | [[Convenciones y estados]] prohíbe forzar el push; se usa squash al fusionar |
| Sintaxis de subagentes de Antigravity (`00-orquestador-equipo`) | Solo funciona en Antigravity |
| Ejemplos de otros proyectos institucionales | Contexto ajeno que confunde a los asistentes |
| Staging obligatorio, despliegue sin interrupción, Sentry | Excede la demo; queda como referencia para el piloto |
| Plantillas de Spring Boot, Next.js y React Native | No son el stack del equipo |
| Skills de docencia, narrativa, tiendas de apps y pagos (14) | No aplican al Reto 1 |
| Skills internas de Antigravity (`builtin`) | Nombran herramientas que solo existen en Antigravity; solo se tomó el patrón anti-invención |

## Reglas para mantener las skills

1. Un cambio en una skill se hace en su nota de `08 Skills/` (y en esta si cambia su alcance u origen); los archivos locales de cada integrante se actualizan después copiando ese contenido.
2. Frontmatter solo con `name` (igual a la carpeta) y `description`. Nada específico de una herramienta: ni comandos con barra, ni subagentes, ni servidores MCP.
3. Una skill no copia el contenido de la bóveda: indica qué notas leer, qué no se puede inventar, qué hacer si falta información y cómo verificar.
4. Rutas relativas a la raíz y sin enlaces de Obsidian (otros asistentes no los resuelven).
5. Al cerrar un ADR o cambiar un hecho canónico, revisar las skills que lo mencionan como PENDIENTE.
6. Si dos skills repiten instrucciones, mover la regla común a `istpetdev-contexto` o a una referencia.

El 9 de octubre se agrega [[istpetdev-prelaunch]] con su nota como fuente editable, paquete local generado y referencia de activación en AGENTS.md. Esa referencia se aplica antes de arranque/exposición/despliegue desde este contexto de trabajo. No instala por sí sola la skill globalmente ni modifica el repositorio de software; la copia local debe actualizarse desde la nota cuando cambie. La activación en las otras herramientas sigue requiriendo C-06.

## Validar una skill (C-06)

Con una tarea pequeña y real, en cada herramienta que use el equipo:

- [ ] La herramienta activa la skill correcta por su descripción.
- [ ] Lee las notas indicadas y [[Hechos canonicos]].
- [ ] Ante un dato faltante responde PENDIENTE y pregunta, en vez de inventar.
- [ ] Ante una contradicción la reporta con el formato acordado.
- [ ] Ejecuta `python .github/scripts/check_vault.py` y no deja errores.

Registrar el resultado con [[Plantilla evidencia]]. Puertas relacionadas: [[Decisiones de arquitectura]] y [[Plan de ejecucion]].

## Fuentes complementarias para implementación — 7 de octubre de 2026

Aporte de bryancito1090 (be0b127): las skills deben usar también [[Repositorio de software y versiones]], [[Entornos y operacion acordados]] e [[Identidad OIDC y sesiones]]. .NET 8 y Angular 22 fijados por Bryan; perfiles A/B, DAG/GHCR/SSH y FSD/Signals del compañero conservados. Aplicar la distinción host/contenedor, manifest de release/backup/rollback sin pull y n8n externo existente. Ninguna skill repite los ejemplos previos sin estos contratos posteriores. Workflows y aplicaciones aún no están implementados.
