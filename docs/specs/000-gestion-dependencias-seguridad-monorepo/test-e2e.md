# 000 — Gestión verificable de dependencias y vulnerabilidades del monorepo Node: pruebas de punta a punta

> **Estado: sin ejecutar.** Este documento define los recorridos completos de aceptación para el spec [spec.md](spec.md) y [plan.md](plan.md).
> Un spec se cierra únicamente cuando todos los escenarios obligatorios pasan con evidencia verificable guardada.
> Las cifras de un resultado se marcan «Medido» solo si enlazan una ejecución real con commit y hash.
> Convención de nombre del archivo: `test-e2e.md`.

---

## Preparación del entorno

Los siguientes comandos preparan el entorno local desde la raíz del monorepo (`/home/joeman/Documents/istpet-dev/istpetdev-platform`) para reproducir fielmente las condiciones de un runner de integración continua y un desarrollador desde cero.

```bash
# 1. Comprobar herramientas base del sistema
git rev-parse HEAD
node --version       # Debe coincidir con .nvmrc: v22.23.3
npm --version        # Debe coincidir con .npm-version: 10.9.9

# 2. Verificar conectividad de red con el registro oficial de npm
npm ping --registry=https://registry.npmjs.org

# 3. Limpiar cachés y node_modules para garantizar ejecución en frío
rm -rf node_modules apps/web/node_modules apps/mobile/node_modules libs/shared-core/node_modules
rm -rf dist apps/web/.angular apps/mobile/.angular
npm cache verify

# 4. Crear directorio de evidencias
mkdir -p artifacts/spec000/e2e
```

---

## Matriz de escenarios

| ID | Escenario | Cubre | Tipo / Medio | Obligatorio |
|---|---|---|---|---|
| E2E-01 | Instalación reproducible desde cero con lockfile intacto | FR-06, NFR-01 | Automatizado (CLI) | Sí |
| E2E-02 | Compilación limpia e integridad de empaquetado del monorepo | FR-01, FR-02, NFR-02 | Automatizado (CLI) | Sí |
| E2E-03 | Verificación visual y de renderizado de iconos y estilos Tailwind | FR-01, FR-02, FR-08, NFR-02 | Manual / Navegador | Sí |
| E2E-04 | Auditoría conectada, inventario y validación diferencial de avisos | FR-03, FR-04, FR-05, NFR-03 | Automatizado (CLI) | Sí |
| E2E-05 | Control de seguridad y compuerta automatizada de auditoría en CI | FR-06, FR-07, NFR-01 | Automatizado (Workflow) | Sí |
| E2E-06 | Paridad estricta y reproducibilidad en el pipeline de despliegue CD | FR-06, FR-07 | Automatizado / Cotejo | Sí |
| E2E-07 | Evaluación controlada y comparativa de rendimiento pnpm (P2) | Decisión P2 | Experimental (CLI) | Condicional |

---

## Detalle de escenarios de prueba

### E2E-01 — Instalación reproducible desde cero con lockfile intacto

**Objetivo:**
Demostrar que una instalación limpia (`npm ci`) en un entorno nuevo utilizando la versión acordada de Node.js (Node 22 LTS) restaura todas las dependencias del monorepo de forma determinista y sin alterar ni un byte de `package-lock.json`.

**Precondiciones:**
- Node `22.23.3` y npm `10.9.9` activos y comprobados contra `.nvmrc`/`.npm-version`.
- `node_modules` eliminados en raíz y workspaces.
- `package-lock.json` sincronizado tras los cambios de manifiesto.

**Pasos:**
1. Ejecutar instalación limpia desde la raíz:
   ```bash
   npm ci
   ```
2. Inspeccionar el estado de control de versiones de git:
   ```bash
   git status --porcelain package-lock.json
   ```
3. Verificar la consistencia de los paquetes instalados en el árbol:
   ```bash
   npm ls --workspaces --include-workspace-root --depth=0
   ```

**Resultado esperado:**
- El comando `npm ci` finaliza con código de salida `0`.
- `git status --porcelain package-lock.json` devuelve una cadena vacía (el lockfile no sufre modificaciones).
- El listado de workspaces reporta `@istpetdev/shared-core`, `@istpetdev/web` y `@istpetdev/mobile` correctamente resueltos.

**Evidencia a guardar:**
- Salida del terminal guardada en `artifacts/spec000/e2e/e2e-01-npm-ci.log`.

---

### E2E-02 — Compilación limpia e integridad de empaquetado del monorepo

**Objetivo:**
Comprobar que tras la eliminación de `lucide-angular` y `@angular/animations` de `apps/web/package.json`, todas las librerías y aplicaciones del monorepo compilan sin errores en modo producción y pasan el análisis estático de código sin advertencias ni dependencias faltantes.

**Precondiciones:**
- `npm ci` completado con éxito (E2E-01).
- `tailwindcss` conservado en `3.4.19` en Web y Mobile.

**Pasos:**
1. Compilar librería compartida:
   ```bash
   npm run build:shared
   ```
2. Ejecutar linteo estricto de la aplicación web:
   ```bash
   npm run lint:web
   ```
3. Compilar aplicación web en modo producción:
   ```bash
   npm run build:web
   ```
4. Compilar aplicación móvil en modo producción:
   ```bash
   npm run build:mobile
   ```

**Resultado esperado:**
- `npm run build:shared` genera los artefactos en `libs/shared-core/dist` con código `0`.
- `npm run lint:web` completa sin errores ni advertencias (código `0`).
- `npm run build:web` genera el bundle Angular 22 en `apps/web/dist/web/browser` con código `0`, sin errores de símbolos faltantes de Lucide o de animación.
- `npm run build:mobile` genera el bundle Ionic 8/Angular 22 en `apps/mobile/dist/mobile/browser` con código `0`.

**Evidencia a guardar:**
- Logs de compilación y tamaños de bundle guardados en `artifacts/spec000/e2e/e2e-02-builds.log`.

---

### E2E-03 — Verificación visual y de renderizado de iconos y estilos Tailwind

**Objetivo:**
Verificar en navegador real que no existen regresiones visuales en las pantallas críticas del sistema administrativo y de trazabilidad tras la remoción de `@angular/animations` y `lucide-angular`, validando que todos los iconos renderizan con `@lucide/angular` y los estilos Tailwind 3.4.19 (colores primario, secundario, acento y animaciones CSS) se visualizan con total nitidez.

**Precondiciones:**
- Monorepo compilado exitosamente (E2E-02).
- Servidor de desarrollo levantado con `npm run dev:web` (el script `predev:web` ejecuta el lint).

**Pasos:**
1. Iniciar el servidor local de desarrollo web:
   ```bash
   npm run dev:web
   ```
2. Abrir en navegador Chromium las siguientes rutas clave:
   - `/login`: Comprobar formulario, tokens de color primario (`bg-primary-600`) y botón con indicador de carga `LucideLoader`.
   - `/dashboard`: Comprobar tarjetas estadísticas con iconos `LucideTrendingUp`, `LucideTrendingDown`, `LucideMinus`, y badges de estado (`LucideTriangleAlert`, `LucideCheck`, `LucideTruck`).
   - `/warehouse`: Comprobar iconos `LucidePackageCheck`, `LucidePrinter`, `LucideQrCode`, `LucideTruck`.
   - `/public-trace`: Comprobar el formulario público y los iconos `LucideShieldCheck`, `LucidePackageSearch`, `LucideCheckCircle2`. Si se prueba el resultado de consulta, usar un `unitId` y token sintéticos válidos mediante `?u=<unitId>#t=<token>`; no usar una ruta inventada `/trace/...`.
3. Inspeccionar la consola del navegador (`F12`): verificar ausencia de errores `NullInjectorError`, `UnknownElementError` o advertencias de módulos de animación no provistos.
4. Inspeccionar elementos interactivos con animaciones: verificar que las transiciones CSS de Tailwind (`animate-pulse`, `animate-spin`, `hover:scale-*`) funcionan con fluidez sin requerir `@angular/animations`.

**Resultado esperado:**
- Todos los iconos SVG se renderizan completos, con dimensiones y trazos definidos (`stroke-[2.2]`).
- Los tokens corporativos de Expo Clean Ecuador (`primary`, `secondary`, `accent`) se aplican idénticos a la línea base previa.
- Consola de desarrollo con 0 errores JavaScript.

**Evidencia a guardar:**
- Capturas de pantalla de las vistas críticas guardadas en `artifacts/spec000/e2e/screenshots/` (`login.png`, `dashboard.png`, `warehouse.png`, `public-trace.png`).

---

### E2E-04 — Auditoría conectada, inventario y validación diferencial de avisos

**Objetivo:**
Demostrar mediante escaneo conectado a `registry.npmjs.org` que todas las vulnerabilidades del monorepo han sido remediadas o cuentan con una excepción temporal justificada con controles compensatorios y fecha de vencimiento, validando que:
1. `braces <=3.0.3` (`GHSA-vfj7-8cjw-p6xm`) no fue marcado falsamente como corregido con `^3.0.3` y cuenta con su ficha de excepción documentada.
2. `postcss-selector-parser` (`GHSA-rj75-hqrm-r3gf`) fue elevado a `>=7.1.6` mediante override probado o tiene excepción formal si causó incompatibilidad en Tailwind 3.
3. No existen hallazgos de severidad Alta o Crítica no clasificados.

**Precondiciones:**
- Conectividad activa con npm advisory database (`npm ping`).
- Archivos versionados `security/spec000/audit-before.json`, `security/spec000/audit-after.json`, `security/spec000/inventory.md` y `security/spec000/exceptions.json` presentes.

**Pasos:**
1. Ejecutar auditoría conectada completa:
   ```bash
   if npm audit --json --workspaces --include-workspace-root > artifacts/spec000/e2e/e2e-04-audit.json; then status=0; else status=$?; fi
   echo "npm audit status: $status"
   ```
2. Ejecutar el validador automatizado de excepciones:
   ```bash
   npm run audit:check
   ```
3. Trazar el estado de los paquetes críticos:
   ```bash
   npm explain braces
   npm explain postcss-selector-parser
   ```
4. Comparar el recuento diferencial entre `audit-before.json` y `audit-after.json`.

**Resultado esperado:**
- El comando `npm audit` produce reporte JSON v2 con metadata y sin `error`. Salida 0 solo es válida con total cero; salida 1 solo con hallazgos presentes.
- `npm run audit:check` finaliza con código `0` únicamente si el reporte está íntegro y cada aviso residual está cubierto por una excepción aprobada que coincida en ID, paquete, rango, `nodePath` y versión real del lockfile.
- Un fallo simulado de DNS/red, stdout vacío/truncado, esquema desconocido o metadata inconsistente termina con código distinto de cero y nunca imprime éxito.
- Cambiar paquete, rango, versión o ubicación del paquete vulnerable rompe la excepción y detiene CI. Ninguna excepción de ID global habilita nuevas rutas.

**Evidencia a guardar:**
- Inventario y reportes antes/después versionados en `security/spec000/`.
- Log de `audit-check.mjs` y JSON sin secretos en `artifacts/spec000/e2e/`, retenidos por el upload-artifact del workflow.

---

### E2E-05 — Control de seguridad y compuerta automatizada de auditoría en CI

**Objetivo:**
Demostrar que GitHub Actions verifica Node/npm fijados, audita sin falsos pases, compila Web y Mobile, y conserva el reporte incluso al fallar. Una respuesta npm inválida, un hallazgo nuevo o una excepción pendiente/vencida/diferente detiene el job.

**Precondiciones:**
- Workflows CI/deploy y Docker alineados a `.nvmrc`/`.npm-version`, auditados y con build Mobile.
- Pruebas nativas del evaluador: `node --test .github/scripts/audit-check.test.mjs`.

**Pasos:**
1. Comprobar la versión Node/npm y que ambos workflows y Docker declaran la misma política:
   ```bash
   grep -n "audit:check" .github/workflows/ci.yml
   grep -n 'node-version-file: .nvmrc' .github/workflows/ci.yml .github/workflows/deploy.yml
   grep -n 'node:22.23.3-alpine' infra/docker/web/Dockerfile
   ```
2. Ejecutar la secuencia exacta de pasos de CI de forma secuencial en local:
   ```bash
   npm ci
   npm run audit:check
   npm run build:shared
   npm run lint:web
   npm run build:web
   npm run build:mobile
   ```
3. Ejecutar pruebas con fixtures JSON aislados, sin alterar la política aprobada ni la base real:
   - `EAI_AGAIN` con stdout tipo `{ "error": { "code": "EAI_AGAIN" } }` → fallo y sin `[AUDIT-SUCCESS]`.
   - excepción vencida con hallazgo activo → fallo.
   - excepción válida para `braces` en la ruta aprobada; agregar otro `nodePath`, cambiar versión o paquete → el nuevo hallazgo no queda autorizado.
   - excepción pendiente/sin aprobador o huérfana tras resolver el aviso → fallo.
   - reporte sin hallazgos y metadata completa → éxito.

**Resultado esperado:**
- CI y deploy validan Node `22.23.3`/npm `10.9.9`, ejecutan audit y builds Shared/Web/Mobile.
- Ambos workflows suben `artifacts/spec000/audit-ci.json` con `actions/upload-artifact@v4` incluso si la compuerta falla.
- Las pruebas negativas distinguen hallazgo cubierto del nuevo, detienen ante red/JSON/schema inválidos y nunca producen un falso cero.

**Evidencia a guardar:**
- Salida de ejecución del workflow simulado en `artifacts/spec000/e2e/e2e-05-ci-simulation.log`.

---

### E2E-06 — Paridad estricta y reproducibilidad en el pipeline de despliegue CD

**Objetivo:**
Comprobar la paridad absoluta entre el workflow de integración continua (`ci.yml`) y el workflow de despliegue a producción (`deploy.yml`) respecto a la versión de Node.js, comandos de instalación, gestor de dependencias y lockfile utilizado.

**Precondiciones:**
- `.github/workflows/deploy.yml` y `.github/workflows/ci.yml` actualizados.

**Pasos:**
1. Comparar configuraciones de Node en ambos flujos:
   ```bash
   grep -H "node-version" .github/workflows/*.yml
   grep -H "npm ci" .github/workflows/*.yml
   ```
2. Verificar Dockerfile de frontend (`infra/docker/web/Dockerfile`):
   ```bash
   grep -n "FROM node" infra/docker/web/Dockerfile || true
   grep -n "npm ci" infra/docker/web/Dockerfile || true
   ```

**Resultado esperado:**
- CI, deploy y la etapa build de Docker usan Node `22.23.3`; npm es `10.9.9`, comprobado por lockfile y `.npm-version`.
- CI y deploy hacen build de Mobile además de Shared/Web.
- Ningún flujo conserva una referencia flotante `node-version: "22"` o `node:22-alpine` que eluda el pin, o una instalación con versión de npm distinta.
- Las etapas de QA previas al despliegue garantizan que el código validado en PR es idéntico al empaquetado para EC2/GHCR.

**Evidencia a guardar:**
- Matriz de cotejo de workflows guardada en `artifacts/spec000/e2e/e2e-06-parity-matrix.txt`.

---

### E2E-07 — Evaluación controlada y comparativa de rendimiento pnpm (P2)

**Objetivo:**
Evaluar en una worktree separada (`/tmp/istpetdev-spec000-pnpm`) el impacto de pnpm (P2), comparando instalación fría/cache separada, disco local y almacén global por separado, y compatibilidad con Angular 22 CLI. P1 permanece intacto.

**Precondiciones:**
- Commit de P1 identificado; ruta de worktree y rama `experiment/pnpm-evaluation` no existen ya.
- `corepack` disponible para ejecutar `pnpm@9.12.0`.

**Pasos:**
1. Crear la worktree desde el commit de P1 sin cambiar el checkout activo (T6 Step 1):
   ```bash
   git worktree add -b experiment/pnpm-evaluation /tmp/istpetdev-spec000-pnpm HEAD
   cd /tmp/istpetdev-spec000-pnpm
   ```
2. Medir npm en frío con cache aislada y vacía:
   ```bash
   rm -rf /tmp/spec000-npm-cache
   time -p npm ci --cache /tmp/spec000-npm-cache
   du -sb node_modules /tmp/spec000-npm-cache
   rm -rf node_modules apps/*/node_modules libs/*/node_modules
   ```
3. Preparar el workspace pnpm (T6 Step 3) y generar solo el lockfile, sin cronometrar:
   ```bash
   corepack pnpm@9.12.0 install --lockfile-only
   ```
4. Medir pnpm en frío con store aislado y vacío, una sola vez:
   ```bash
   rm -rf /tmp/spec000-pnpm-store node_modules apps/*/node_modules libs/*/node_modules
   time -p corepack pnpm@9.12.0 install --frozen-lockfile --store-dir /tmp/spec000-pnpm-store
   du -sb node_modules /tmp/spec000-pnpm-store
   ```
5. Probar compilación y auditoría bajo pnpm:
   ```bash
   corepack pnpm@9.12.0 --filter @istpetdev/shared-core build
   corepack pnpm@9.12.0 --filter @istpetdev/web build
   corepack pnpm@9.12.0 --filter @istpetdev/mobile build
   corepack pnpm@9.12.0 audit --json
   ```

Las dos mediciones usan la misma máquina, commit y versión de Node, con cache/store vacíos. Los tamaños de `node_modules` y de cache/store se reportan aparte; el almacén global no se atribuye a `node_modules`.

**Resultado esperado:**
- Generación de informe cuantitativo con métricas de tiempo y espacio.
- Identificación de cualquier conflicto con dependencias pares de Angular 22 o Ionic 8.
- Recomendación técnica fundada para que el equipo (Bryan) decida el momento de adopción de P2.

**Evidencia a guardar:**
- Informe comparativo `security/spec000/pnpm-evaluation-report.md` commiteado en la rama `experiment/pnpm-evaluation`. Puede revisarse sin traer su lockfile a P1.

---

## Registro de ejecuciones

| Fecha | Commit | Escenario | Resultado | Evidencia | Quién |
|---|---|---|---|---|---|
| PENDIENTE | — | E2E-01 | pendiente | `artifacts/spec000/e2e/e2e-01-npm-ci.log` | — |
| PENDIENTE | — | E2E-02 | pendiente | `artifacts/spec000/e2e/e2e-02-builds.log` | — |
| PENDIENTE | — | E2E-03 | pendiente | `artifacts/spec000/e2e/screenshots/` | — |
| PENDIENTE | — | E2E-04 | pendiente | `artifacts/spec000/e2e/e2e-04-audit.json` y log de `audit:check` | — |
| PENDIENTE | — | E2E-05 | pendiente | `artifacts/spec000/e2e/e2e-05-ci-simulation.log` | — |
| PENDIENTE | — | E2E-06 | pendiente | `artifacts/spec000/e2e/e2e-06-parity-matrix.txt` | — |
| PENDIENTE | — | E2E-07 | pendiente | `security/spec000/pnpm-evaluation-report.md` (rama experimental) | — |
