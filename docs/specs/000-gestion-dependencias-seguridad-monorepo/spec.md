---
id: 000
titulo: Gestión de dependencias, remediación de vulnerabilidades y evaluación pnpm
estado: borrador
creado: 2026-10-13
actualizado: 2026-10-13
aprobado_por: PENDIENTE
depende_de: []
boveda: ["03 Arquitectura/Decisiones de arquitectura", "03 Arquitectura/Verificacion de seguridad antes del lanzamiento", "03 Arquitectura/Frontend y componentes"]
---

# 000 — Gestión de dependencias, remediación de vulnerabilidades y evaluación pnpm

> Especificación técnica para sanear las dependencias deprecadas y mitigar las vulnerabilidades de seguridad detectadas en el espacio de trabajo web (`apps/web`) y en el monorepo raíz, previniendo rupturas mayores (*breaking changes*) y definiendo la hoja de ruta hacia `pnpm`.

## Contexto

Durante la ejecución de rutinas de instalación y auditoría de paquetes en `apps/web` (`npm install` y `npm audit`), el gestor de paquetes de Node.js reportó advertencias de deprecación críticas y 7 vulnerabilidades de seguridad (2 de severidad moderada y 5 de severidad alta):

1. **Deprecación de `@angular/animations@22.2.1`:** Angular v22 desaconseja el uso de `@angular/animations` en favor del soporte nativo de transiciones y directivas de control (`animate.enter` / `animate.leave`). La inspección del código fuente revela que `apps/web` no importa este módulo ni utiliza `provideAnimations()` en su configuración (`app.config.ts`), por lo que su presencia en `dependencies` es un residuo innecesario.
2. **Deprecación y duplicidad de `lucide-angular@1.0.0`:** El proyecto incluye tanto `lucide-angular` (paquete heredado y deprecado por la comunidad de Lucide) como `@lucide/angular` (`^1.55.0`). La totalidad de la base de código (`operations-dashboard.page.ts`, `public-trace.page.ts`, `badge.component.ts`, `route-map.widget.ts`, etc.) consume exclusivamente `@lucide/angular`. `lucide-angular` es redundante.
3. **Vulnerabilidades de dependencias transitivas (Tailwind CSS v3):**
   - **`braces` (Alta - GHSA-vfj7-8cjw-p6xm):** Agotamiento de pila / Denegación de Servicio (ReDoS) ante patrones anidados extremos. Proviene de la cadena `tailwindcss` $\rightarrow$ `chokidar` / `fast-glob` / `micromatch` $\rightarrow$ `braces`.
   - **`postcss-selector-parser` (Moderada - GHSA-rj75-hqrm-r3gf):** Complejidad computacional cuadrática en parseo de selectores que causa consumo excesivo de CPU. Proviene de `tailwindcss` $\rightarrow$ `postcss-nested` $\rightarrow$ `postcss-selector-parser`.
   - **Peligro de `npm audit fix --force`:** El comando automático fuerza la actualización a `tailwindcss@4.x`. Tailwind CSS v4 elimina la arquitectura clásica de `tailwind.config.js`, descarta directivas `@tailwind` y modifica el comportamiento de `@apply`, lo que rompería completamente los estilos y tokens visuales acordados para la demo y el evento Expo Clean Ecuador 2026.
4. **Ámbito de exposición:** Todas las vulnerabilidades reportadas residen en herramientas de compilación y empaquetado (*build-time* / `devDependencies`). No se empaquetan en el bundle cliente enviado al navegador del usuario final, pero representan un riesgo de estabilidad y denegación de servicio en los pipelines de integración continua (CI/CD) y vulneran el control de seguridad previo al lanzamiento (estipulado en la nota de bóveda [[Verificacion de seguridad antes del lanzamiento]]).

## Objetivo y aporte a la nota

Garantizar una cadena de suministro de dependencias limpia, reproducible y libre de vulnerabilidades de severidad moderada o alta en `apps/web` y en el monorepo raíz, protegiendo la estabilidad del stack visual (Tailwind v3 + Angular 22) y estableciendo una política clara de gestión de paquetes (parche con `overrides` en npm o transición a `pnpm`).

| Criterio | Peso | Cómo lo sostiene este spec |
|---|---:|---|
| **Viabilidad técnica y arquitectura** | 20 % | Asegura que la aplicación web compile limpiamente, sin advertencias de librerías deprecadas y sin riesgo de ruptura por actualizaciones involuntarias mayores de frameworks CSS. |
| **Seguridad y buenas prácticas (Prelaunch)** | 15 % | Cumple con el control de "Dependencias vulnerables" documentado en [[Verificacion de seguridad antes del lanzamiento]] sin confiar ciegamente en actualizaciones automáticas de versión mayor. |
| **Calidad de entrega y estabilidad** | 10 % | Elimina bloatware y dependencias fantasmas, garantizando builds deterministas en entornos locales y en runners de GitHub Actions. |

## Historias de usuario

### H1 — Limpieza de paquetes huérfanos y deprecados en Web (P1)

**Como** desarrollador de frontend, **quiero** eliminar las dependencias redundantes (`lucide-angular`) y deprecadas sin uso (`@angular/animations`), **para** mantener un árbol de dependencias limpio y evitar advertencias molestas durante la instalación.

**Por qué esta prioridad:** No introduce ningún riesgo de ruptura y alinea las dependencias declaradas con las importaciones reales en TypeScript.

**Escenarios de aceptación:**
1. **Dado** el archivo `apps/web/package.json`, **cuando** se retiran `lucide-angular` y `@angular/animations`, **entonces** `npm run build:web` y `npm run lint:web` ejecutan y finalizan con código de salida 0 sin ningún error de importación faltante.
2. **Dado** un entorno limpio, **cuando** se ejecuta `npm install` en el proyecto, **entonces** la salida no emite `npm warn deprecated` referidos a Lucide o Angular Animations.

### H2 — Mitigación de vulnerabilidades de Tailwind v3 mediante Overrides (P1)

**Como** ingeniero de DevSecOps, **quiero** forzar la resolución segura de subdependencias transitivas (`braces` y `postcss-selector-parser`) en el nivel raíz del monorepo, **para** neutralizar las 7 vulnerabilidades de `apps/web` sin forzar la subida destructiva a Tailwind CSS v4.

**Por qué esta prioridad:** Permite alcanzar 0 vulnerabilidades en el espacio web de forma inmediata y segura, protegiendo la fecha límite de congelamiento de funcionalidades (D8).

**Escenarios de aceptación:**
1. **Dado** el archivo `package.json` raíz, **cuando** se configura la sección `overrides` con `braces >= 3.0.3` y `postcss-selector-parser >= 7.1.6`, **entonces** la reinstalación resuelve las versiones parcheadas para los árboles de `chokidar`, `fast-glob` y `postcss-nested`.
2. **Dado** el directorio `apps/web`, **cuando** se corre `npm audit`, **entonces** el reporte no muestra vulnerabilidades activas atribuibles a `braces` o `postcss-selector-parser`.
3. **Dado** el build de producción `npm run build:web`, **cuando** compila el CSS con Tailwind 3.4.19, **entonces** los estilos, utilidades y variables de tokens se generan idénticamente.

### H3 — Evaluación y plan de migración al gestor `pnpm` (P2)

**Como** equipo de desarrollo del monorepo `istpetdev-platform`, **quiero** contar con una especificación y protocolo formal de migración a `pnpm`, **para** obtener builds más veloces, aislamiento estricto de dependencias en workspaces y un ahorro significativo de almacenamiento mediante *hard links*.

**Por qué esta prioridad:** Si bien npm con workspaces funciona actualmente, `pnpm` previene que un paquete en `apps/web` consuma accidentalmente librerías instaladas en la raíz o en otra app (prevención de dependencias fantasma / *phantom dependencies*).

**Escenarios de aceptación:**
1. **Dado** Node.js 24 con Corepack activo (`corepack enable pnpm`), **cuando** se define `pnpm-workspace.yaml`, **entonces** la orden `pnpm install` enlaza `libs/shared-core`, `apps/web` y `apps/mobile` sin duplicar paquetes en disco.
2. **Dado** el uso de `pnpm.overrides` en `package.json`, **cuando** se auditan dependencias con `pnpm audit`, **entonces** se obtiene el mismo control estricto de parches que en npm sin scripts de workaround.

---

## Requisitos

### Funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| FR-01 | El archivo `apps/web/package.json` DEBE eliminar `lucide-angular` de `dependencies`, conservando exclusivamente `@lucide/angular`. | ADR-04 ([[Decisiones de arquitectura]]), [[Frontend y componentes]] |
| FR-02 | El archivo `apps/web/package.json` DEBE eliminar `@angular/animations` de `dependencies` mientras la aplicación no haga uso de APIs de animación legadas. | [[Frontend y componentes]] |
| FR-03 | El monorepo DEBE mantener anclada la versión de `tailwindcss` en la línea 3.4.x para `apps/web` y `apps/mobile` hasta que se planifique un sprint explícito de migración a Tailwind v4. | [[Sistema de diseno y colores]], ADR-04 |
| FR-04 | El gestor de paquetes DEBE forzar la resolución segura de `braces` (mínimo `3.0.3`) y `postcss-selector-parser` (mínimo `7.1.6`) mediante directivas de anulación (`overrides`). | [[Verificacion de seguridad antes del lanzamiento]] |

### No funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| NFR-01 | **Reproducibilidad:** El proceso de instalación y build debe ser 100 % determinista en cualquier máquina de desarrollo Windows/Linux y en runners de CI/CD. | [[CI-CD y automatizacion de despliegue]] |
| NFR-02 | **Cero regresiones visuales:** Los cambios de dependencias no deben alterar ninguna regla CSS compilada ni clases de Tailwind en los componentes web. | [[Sistema de diseno y colores]] |
| NFR-03 | **Compatibilidad con Node 24:** Las herramientas y scripts de workspace deben operar sin warnings de motor en Node.js v24.21.0. | [[Puesta en marcha del workspace y AWS dev]] |

---

## Entidades y contratos (Archivos afectados)

1. `apps/web/package.json`: Remoción de dependencias superfluas y deprecadas.
2. `package.json` (Raíz): Inclusión de directivas `overrides` (o `pnpm.overrides`).
3. `pnpm-workspace.yaml` (Condicional a H3): Definición de los paquetes del monorepo (`libs/*`, `apps/*`).
4. `package-lock.json` / `pnpm-lock.yaml`: Actualización de la traza de dependencias y hashes de integridad.

---

## Guardarraíles aplicados

| Guardarraíl | Aplicación en este spec |
|---|---|
| **G8 (Trabajo previo trazable)** | Todos los cambios en archivos de configuración y lockfiles deben registrarse mediante commits claros y trazables bajo Conventional Commits (`fix(deps): ...`, `refactor(web): ...`). |
| **Principio de Mínima Intervención** | No actualizar librerías principales (como Angular, Tailwind o RxJS) de forma impulsiva; solucionar las vulnerabilidades en las hojas del árbol de dependencias (*transitive dependencies*). |

---

## Fuera de alcance

- Migración de las hojas de estilo y clases a la sintaxis de Tailwind CSS v4.
- Reescritura de los componentes web a nuevos paradigmas de animación mientras no sea un requerimiento de negocio.
- Migración de proyectos backend (.NET 8) a gestores alternativos (este spec cubre exclusivamente el ecosistema Node.js / TypeScript).

---

## Contradicciones con la bóveda

> **Nota de conciliación de ADR-04:**
> En [[Decisiones de arquitectura]] (ADR-04), se registró textualmente `Lucide Icons (lucide-angular)`. La inspección del código fuente evidencia que todos los archivos consumen la nueva librería `@lucide/angular`. Este spec aclara que la intención real del equipo es utilizar el paquete oficial y mantenido `@lucide/angular`, dejando `lucide-angular` catalogado como error de transcripción histórico sin impacto funcional.

---

## Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Ejecución accidental de `npm audit fix --force` por un integrante del equipo | Media | Alta | Documentar explícitamente la prohibición de `--force` en el `CONTRIBUTING.md` y en este spec. |
| Incompatibilidad de *hoisting* al migrar a `pnpm` con Angular CLI | Baja | Media | Si se migra a `pnpm`, configurar en `.npmrc` la directiva `public-hoist-pattern[]=*eslint*` y verificar la compilación de `apps/web`. |
| Desajuste de scripts del monorepo al cambiar de gestor | Media | Media | Mantener scripts unificados en la raíz y documentar equivalencias (`npm run dev:web` vs `pnpm --filter @istpetdev/web start`). |

---

## Criterios de éxito

- **CE-01:** `npm audit` ejecutado en el ámbito de `apps/web` reporta 0 vulnerabilidades de severidad alta o crítica.
- **CE-02:** `npm install` en `apps/web` no arroja advertencias de deprecación sobre `@angular/animations` ni `lucide-angular`.
- **CE-03:** `npm run check:web` (que encadena `lint:web` y `build:web`) finaliza exitosamente con código 0.
- **CE-04:** El checklist de pre-lanzamiento de seguridad valida el cumplimiento de dependencias limpias para el módulo web.

---

## Preguntas abiertas

| Pregunta | Decidido por | Estado |
|---|---|---|
| ¿Se adopta `pnpm` antes del evento del 16-octubre o se posterga para la fase post-hackathon (P2), manteniendo `npm` con `overrides` durante la demo? | Bryan / Equipo técnico | Abierta |
