---
id: 000
titulo: Gestión verificable de dependencias y vulnerabilidades del monorepo Node
estado: borrador
creado: 2026-10-13
actualizado: 2026-10-13
aprobado_por: PENDIENTE
depende_de: []
boveda: ["03 Arquitectura/Decisiones de arquitectura", "03 Arquitectura/Verificacion de seguridad antes del lanzamiento", "03 Arquitectura/Frontend y componentes"]
---

# 000 — Gestión verificable de dependencias y vulnerabilidades del monorepo Node

## Propósito

Reducir dependencias sin uso y vulnerabilidades del árbol Node de `apps/web`, `apps/mobile` y la raíz, con cambios compatibles con Angular 22 y Tailwind 3.4.19. Cada hallazgo tendrá una resolución comprobada o una excepción temporal documentada. La evaluación de `pnpm` es una decisión posterior e independiente de la remediación inmediata.

## Línea base y evidencia (13 de octubre de 2026)

| Hecho | Evidencia | Consecuencia |
|---|---|---|
| `apps/web/package.json` declara `lucide-angular@^1.0.0` y `@lucide/angular@^1.55.0`; los imports revisados de `apps/web/src` usan `@lucide/angular`. | Manifiesto e imports del repositorio. | Retirar el paquete redundante tras compilar. |
| `@angular/animations@22.2.1` está declarado; no hay imports de él ni `provideAnimations()` en `apps/web/src`. | Manifiesto y búsqueda de código. | Retirar tras comprobar build y dependencias pares. Las clases `animate-*` de Tailwind no dependen de este paquete. |
| Web y Mobile fijan `tailwindcss` en `3.4.19`, usan `tailwind.config.js` y directivas `@tailwind` en `styles.css`. | Manifiestos y hojas de estilo. | Una migración a Tailwind 4 requiere proyecto y pruebas visuales propios. |
| El `package-lock.json` resuelve `braces@3.0.3`, `postcss-selector-parser@6.1.4`, `postcss-nested@6.2.0` y `micromatch@4.0.8`. Angular 22 del lockfile requiere al menos Node 22.22.3 dentro de la rama 22. Tailwind 3 depende directamente de `micromatch` y `postcss-selector-parser`; `postcss-nested` también depende del parser 6.x. | Lockfile raíz. | Evaluar el árbol completo y compatibilidad de cada sustitución. |
| `GHSA-vfj7-8cjw-p6xm` afecta `braces <=3.0.3` y no registra versión corregida. | [Aviso de GitHub](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm). | **`"braces": "^3.0.3"` no corrige la vulnerabilidad.** |
| `GHSA-rj75-hqrm-r3gf` afecta `postcss-selector-parser <7.1.6`; la corrección está en 7.1.6. | [Aviso de GitHub](https://github.com/advisories/GHSA-rj75-hqrm-r3gf). | El salto 6→7 requiere pruebas de compatibilidad con Tailwind y `postcss-nested`. |
| CI y deploy usan Node 22 y `npm ci`; no hay paso de `npm audit`. | `.github/workflows/ci.yml` y `deploy.yml`. | Alinear política de Node y control de auditoría en ambos workflows. |
| Documentos previos registraron 5 hallazgos altos y 2 moderados. | `docs/schema95-jwt-http-checks.md` y `docs/schema95-migration.md`. | Es un reporte histórico, no una auditoría vigente ni prueba de que dos overrides resuelvan los siete. |

**Límite de esta verificación:** el entorno local tenía Node `v26.10.0` y npm `12.2.0`; `npm audit` no pudo consultar `registry.npmjs.org` (`EAI_AGAIN`). No hay cifra actual validada para la raíz o los workspaces. El conteo, rutas de dependencia y propuestas de `fixAvailable` se deben capturar de nuevo con red disponible antes de implementar. `npm ls` tampoco pudo mostrar un árbol instalado en este entorno; las versiones citadas provienen del lockfile.

## Decisiones de alcance

1. **P1, saneamiento npm:** mantener npm, retirar dependencias directas sin uso, investigar cada aviso y actualizar el lockfile raíz. No ejecutar `npm audit fix --force` como mecanismo de decisión.
2. **P1, seguridad:** preferir una actualización compatible y mantenida de la dependencia que introduce el paquete vulnerable. Usar `overrides` solo para una versión publicada, corregida y probada; limitar su alcance cuando sea posible. Si no existe tal versión, registrar una excepción con mitigaciones y vencimiento. Un `audit` en cero es una meta condicionada a una ruta técnica real, no una promesa.
3. **Entorno reproducible:** fijar Node `22.23.3` y npm `10.9.9`, compatibles con los requisitos Node del lockfile al corte del spec. Una actualización de parche exige cambiar los archivos de versión/engine juntos y repetir instalación, builds y auditoría en CI/deploy/Docker.
4. **P2, pnpm:** realizar una evaluación separada con resultados medidos en worktree temporal. No introducir dos gestores ni dos lockfiles activos durante P1.

## Requisitos y criterios de aceptación

| ID | Requisito | Evidencia de aceptación |
|---|---|---|
| FR-01 | Retirar `lucide-angular` y `@angular/animations` de `apps/web/package.json` cuando la búsqueda de imports y el build confirmen que no se usan. | Lockfile sin esas dependencias directas; `npm run lint:web` y `npm run build:web` con salida 0. Si alguna permanece por dependencia transitiva, documentar la ruta. |
| FR-02 | Conservar Tailwind `3.4.19` en Web y Mobile durante P1. | Versiones resueltas comprobadas en el lockfile; ambos builds y revisión de estilos críticos. |
| FR-03 | Crear un inventario por aviso con ID, severidad, versión afectada, ruta de dependencia, ámbito (runtime/build), exposición, resolución y responsable. | Registro derivado de `npm audit --json --workspaces --include-workspace-root`, `npm explain <paquete>` y lockfile, con fecha y versiones de Node/npm. No sumar rutas transitivas como si fueran avisos distintos. |
| FR-04 | Resolver `postcss-selector-parser` solo con una versión fuera del rango afectado y compatible con sus consumidores, o registrar excepción justificada. | Para remediación: auditoría sin ese ID, `npm ls`/lockfile con versión efectiva y builds Web/Mobile. Para excepción: ficha aprobada y fechada. Un override a 7.1.6 requiere prueba explícita porque los consumidores actuales declaran 6.x. |
| FR-05 | No declarar `braces@3.0.3` corregido. Investigar actualización o sustitución de los consumidores; si no hay ruta compatible, documentar excepción temporal. | Auditoría y árbol después del cambio, o ficha de excepción con alcance, mitigaciones, responsable, revisión y fecha de caducidad. |
| FR-06 | Fijar una política única de Node/npm para desarrollo, CI, imagen Docker y despliegue. | Node `22.23.3`, npm `10.9.9` al corte de este spec; CI, deploy y Docker verifican las versiones con instalación limpia. |
| FR-07 | Añadir control de seguridad automatizado una vez fijada la política de excepciones. | CI audita raíz/workspaces, conserva el reporte y falla ante errores de red/JSON/esquema, avisos sin excepción exacta y excepciones pendientes, vencidas, globales o sin aprobación. |
| FR-08 | Conciliar la documentación que llama `lucide-angular` al sistema oficial de iconos. | ADR-04 y referencias mantenidas indican el paquete realmente utilizado, `@lucide/angular`, sin borrar el historial de la decisión. |
| NFR-01 | Mantener instalación reproducible. | `npm ci` desde raíz, con `package-lock.json` coherente y sin modificación del lockfile; registrar Node/npm usados. `npm install` sirve para actualizar el lockfile, no para probar reproducibilidad. |
| NFR-02 | Evitar regresiones funcionales y visuales. | `npm run build:shared`, `npm run lint:web`, `npm run build:web`, `npm run build:mobile`; comparación antes/después de las vistas y tokens críticos (colores primario, secundario y acento; iconos; mapa; login). Una igualdad byte a byte del CSS no es requisito realista si cambia una dependencia del compilador. |
| NFR-03 | Mantener trazabilidad de cambios de seguridad. | PR con informe antes/después, justificación de cada override, lockfile revisado y referencia a avisos; fecha para retirar overrides o excepciones. |

## Flujo de implementación P1

1. **Capturar línea base conectada:** desde la raíz, guardar versiones de Node/npm, `npm audit --json --workspaces --include-workspace-root`, `npm audit --json --omit=dev --workspaces --include-workspace-root`, `npm explain braces` y `npm explain postcss-selector-parser`. El segundo informe sirve para clasificar exposición; no sustituye el control del árbol completo. Si falla la red, registrar la falla y no interpretar un JSON vacío como cero vulnerabilidades.
2. **Retirar paquetes directos sin uso:** modificar `apps/web/package.json`, actualizar `package-lock.json` con la misma versión de npm elegida para CI y repetir búsquedas de imports. Verificar si hay dependencias pares u otro consumidor que impida la remoción.
3. **Remediar cada aviso:** primero intentar una versión compatible del consumidor. Para `postcss-selector-parser`, probar el parche 7.1.6 con consumidores actuales antes de aceptar un override que cruza versión mayor. Para `braces`, investigar si se puede eliminar del árbol mediante consumidores compatibles. Un override a 3.0.3 queda descartado por el aviso vigente.
4. **Validar:** hacer instalación limpia, builds y comparación visual de Web/Mobile; repetir auditoría y comparar IDs y rutas. Registrar cualquier hallazgo nuevo o que permanezca.
5. **Aplicar control en CI:** incluir auditoría y evidencia en el workflow de PR; alinear también el workflow de despliegue con el gestor, lockfile y versión de Node escogidos. Actualizar el checklist de seguridad previo al lanzamiento.

## Tratamiento de excepciones

Una excepción solo procede si la remediación compatible no está publicada o rompe un flujo verificado. Debe estar versionada y aprobada; identificar advisory, paquete, rango, ruta de instalación y versión exacta; incluir alcance, justificación, mitigación, aprobador, referencia, aprobación y caducidad. La compuerta falla ante datos npm ausentes/invalidos, hallazgo nuevo, ruta o versión cambiada, excepción pendiente/vencida/sin correspondencia. No se permite desactivar toda la auditoría ni usar `--omit=dev` para ocultar riesgo de tooling. Revalidar excepciones ante upstream nuevo o cambio de exposición.

## Evaluación P2 de pnpm

La migración requiere una decisión basada en prueba: instalar desde cero, compilar Shared Core/Web/Mobile, medir tiempo y espacio en condiciones comparables, comprobar resolución de imports y dependencias pares, y ejecutar auditoría equivalente. Definir una versión fija de pnpm en `packageManager`, crear `pnpm-workspace.yaml`, declarar `@istpetdev/shared-core` con `workspace:*` en sus consumidores, actualizar scripts, cachés y pasos de **CI y deploy**, y reemplazar el lockfile npm por `pnpm-lock.yaml` en un único cambio. No habilitar hoisting amplio ni relajar dependencias pares por anticipado; hacerlo solo ante un fallo reproducible y documentado. [Workspaces de pnpm](https://pnpm.io/workspaces/) describen el enlace explícito con `workspace:`.

**Decisión pendiente:** Bryan/equipo técnico elige fecha de P2 después de revisar el experimento; P1 no depende de esa elección.

## Archivos previstos

| Fase | Archivos |
|---|---|
| P1 | `apps/web/package.json`, `package.json` raíz si existe un override probado, `package-lock.json`, `.github/workflows/ci.yml`, `.github/workflows/deploy.yml`, `infra/docker/web/Dockerfile`, `.nvmrc`, `.npm-version`, `.npmrc`, `security/spec000/exceptions.json`, `CONTRIBUTING.md`, ADR-04 y checklist de seguridad. |
| P2 condicional | `pnpm-workspace.yaml`, `package.json` raíz y de workspaces, `pnpm-lock.yaml`, eliminación de `package-lock.json`, workflows y documentación de arranque. |

## Riesgos y límites

- El aviso de `braces` no tiene parche publicado al corte citado; puede impedir una meta estricta de cero hallazgos altos sin cambiar el árbol o aceptar una excepción evaluada.
- Forzar `postcss-selector-parser` 7 sobre consumidores que declaran 6.x puede alterar el procesamiento de CSS. Build y revisión visual son condiciones de aceptación, no garantías previas.
- Los avisos citados describen denegación de servicio bajo entradas especialmente construidas. La exposición concreta depende de si el pipeline o alguna ruta de servidor acepta patrones o selectores no confiables; debe documentarse, no presumirse.
- `npm audit` consulta una base cambiante; guardar fecha e informe permite comparar resultados sin tratar el número histórico de siete como verdad permanente.
- Esta especificación no cubre vulnerabilidades NuGet ni escaneo de imágenes de contenedor.

## Fuentes de reglas del gestor

- [npm `overrides`](https://docs.npmjs.com/cli/v11/configuring-npm/package-json/#overrides): solo se aplican los del `package.json` raíz.
- [npm `ci`](https://docs.npmjs.com/cli/v11/commands/npm-ci/): instalación desde lockfile para automatización.
- [npm `audit`](https://docs.npmjs.com/cli/v11/commands/npm-audit/): opciones de workspaces y umbral de fallo.
