# 000 — Gestión verificable de dependencias y vulnerabilidades del monorepo Node: tareas

> Pasos ejecutables de [spec.md](spec.md) y [plan.md](plan.md). **Todos los pasos están pendientes.**
> Cada tarea entrega un resultado comprobable de forma independiente y termina en un commit convencional en español.
> No se permiten marcadores vacíos ni supuestos arreglos sin verificación de red y compilación limpia.
> Comandos ejecutados desde la raíz del repositorio (`/home/joeman/Documents/istpet-dev/istpetdev-platform`).

## Resumen y trazabilidad

| ID | Tarea / Entrega comprobable | Depende de | Paralelizable | Estado |
|---|---|---|---|---|
| T0 | Fijar Node/npm, validar entorno y red | — | — | completada |
| T1 | Instalar lockfile y capturar inventario inicial versionado | T0 | — | completada |
| T2 | Limpiar dependencias directas de Web y validar builds | T1 | — | completada |
| T3 | Remediación de vulnerabilidades, evaluación de overrides y excepciones temporales | T1, T2 | — | pendiente |
| T4 | Automatización del control de seguridad en CI/CD y política de Node | T3 | — | pendiente |
| T5 | Conciliación documental, actualización de ADR-04 y checklist prelaunch | T2, T4 | [P] con T6 | pendiente |
| T6 | Evaluación experimental de pnpm (P2 condicional e independiente) | T4 | — | pendiente |

T0 → T1 → T2 → T3 → T4. T5 puede avanzar con T4 cuando sus cambios documentales no contradigan la política ya resuelta. T6 es opcional: se ejecuta en un `git worktree` aparte desde el commit de P1, sin tocar el checkout ni el lockfile de P1.

### Cobertura de requisitos del spec

| Requisito / Criterio | Tareas que lo implementan | Escenario E2E de aceptación |
|---|---|---|
| FR-01 (Retirar `lucide-angular` y `@angular/animations`) | T2, T5 | E2E-02, E2E-03 |
| FR-02 (Conservar Tailwind `3.4.19` en Web y Mobile) | T2, T3 | E2E-02, E2E-03 |
| FR-03 (Inventario estructurado de avisos de seguridad) | T0, T1 | E2E-04 |
| FR-04 (Resolución de `postcss-selector-parser`) | T3 | E2E-03, E2E-04 |
| FR-05 (Tratamiento de `braces` sin falso parche 3.0.3) | T3 | E2E-04 |
| FR-06 (Política unificada de Node 22 LTS y npm) | T0, T4 | E2E-01, E2E-05, E2E-06 |
| FR-07 (Control de seguridad automatizado en CI) | T4 | E2E-04, E2E-05 |
| FR-08 (Conciliar documentación del sistema de iconos) | T5 | E2E-03 |
| NFR-01 (Instalación reproducible con `npm ci`) | T0, T2, T3, T4 | E2E-01, E2E-05, E2E-06 |
| NFR-02 (Prevención de regresiones funcionales y visuales) | T2, T3 | E2E-02, E2E-03 |
| NFR-03 (Trazabilidad estricta de cambios de seguridad) | T1, T3, T4, T5 | E2E-04, E2E-05 |

---

## T0 — Preparación, validación de red y línea base conectada

**Archivos:**
- Crear: `.nvmrc`, `.npm-version`, `.npmrc`, `security/spec000/network-status.md`, `security/spec000/baseline-env.json`
- Modificar: `package.json` raíz (`engines`)
- Inspeccionar: `package-lock.json`, `.github/workflows/ci.yml`, `.github/workflows/deploy.yml`, `infra/docker/web/Dockerfile`

**Interfaces y artefactos:**
- Produce: Registro de versiones activas de Node/npm, hash del commit base y confirmación de conectividad al registro oficial de npm (`registry.npmjs.org`).

- [ ] **Step 1: Fijar versiones compatibles y registrar el commit base**

Crear `.nvmrc` con `22.23.3`, `.npm-version` con `10.9.9` y `.npmrc` con `engine-strict=true`. La release oficial de Node 22.23.3 trae npm 10.9.9; ambos están dentro de los requisitos del lockfile. En `package.json` raíz declarar `engines.node="^22.23.3"` y `engines.npm="10.9.9"`.

```bash
nvm install 22.23.3
nvm use
node --version
npm --version
test "$(node --version)" = "v$(cat .nvmrc)"
test "$(npm --version)" = "$(cat .npm-version)"
node -e 'const p=require("./package.json"); if(p.engines?.node!=="^22.23.3" || p.engines?.npm!=="10.9.9") process.exit(1)'
git rev-parse HEAD
```

Expected: Node `v22.23.3`, npm `10.9.9`; si el gestor de versiones no es nvm, activar esas mismas versiones equivalentes antes de continuar.

- [ ] **Step 2: Instalar desde el lockfile acordado antes de medir el árbol**

```bash
npm ci
npm ls --workspaces --include-workspace-root --depth=0
```

Expected: instalación limpia sin cambio al lockfile y los tres workspaces presentes. Una falla de resolución o instalación detiene T0/T1; no se transforma en inventario vacío.

- [ ] **Step 3: Probar conectividad y versionar su resultado**

```bash
mkdir -p security/spec000
date -u +"%Y-%m-%dT%H:%M:%SZ"
npm ping --registry=https://registry.npmjs.org
```

Registrar fecha, commit, versiones exactas y salida en `security/spec000/network-status.md`. Si falla DNS/conectividad, dejar la tarea bloqueada para auditorías; nunca continuar con un JSON vacío ni declarar ausencia de avisos.

- [ ] **Step 4: Guardar línea base del entorno versionada**

Crear `security/spec000/baseline-env.json` con timestamp UTC, commit registrado, Node `22.23.3`, npm `10.9.9` y el resultado de conectividad. Los archivos `security/spec000/` son política/evidencia revisable y deben poder incluirse en Git; los logs efímeros de ejecución permanecen en `artifacts/` y se suben como artefactos de CI.

---

## T1 — Inventario exhaustivo y clasificación de exposición de avisos

**Archivos:**
- Crear/versionar: `security/spec000/audit-before.json`, `security/spec000/audit-runtime-before.json`, `security/spec000/inventory.md`, `security/spec000/explains.txt`
- Inspeccionar: `package-lock.json` y `security/spec000/baseline-env.json`

**Interfaces y artefactos:**
- Consumes: Conectividad validada a `registry.npmjs.org`.
- Produces: Inventario clasificado por ID de aviso, severidad, paquete afectado, consumidores intermedios, ruta completa, ámbito (build vs runtime) y estado de parche upstream.

- [ ] **Step 1: Capturar auditoría completa del monorepo (raíz y todos los workspaces)**

```bash
if npm audit --json --workspaces --include-workspace-root > security/spec000/audit-before.json; then status=0; else status=$?; fi
if [ "$status" -gt 1 ]; then exit "$status"; fi
node -e 'const [f,s]=process.argv.slice(1); const r=JSON.parse(require("node:fs").readFileSync(f,"utf8")); const t=r.metadata?.vulnerabilities?.total; if(r.error || r.auditReportVersion!==2 || !r.vulnerabilities || !Number.isInteger(t) || (s==="0")!==(t===0)) process.exit(1)' security/spec000/audit-before.json "$status"
```

Expected: JSON v2 íntegro, sin `error`, con metadata y mapa de vulnerabilidades. El código 0 requiere total cero; código 1 requiere total mayor que cero. Cualquier otra combinación detiene T1. `--omit=dev` es una señal auxiliar de exposición; no demuestra alcance en el bundle.

- [ ] **Step 2: Capturar auditoría omitiendo dependencias de desarrollo (`--omit=dev`)**

```bash
if npm audit --json --omit=dev --workspaces --include-workspace-root > security/spec000/audit-runtime-before.json; then status=0; else status=$?; fi
if [ "$status" -gt 1 ]; then exit "$status"; fi
node -e 'const [f,s]=process.argv.slice(1); const r=JSON.parse(require("node:fs").readFileSync(f,"utf8")); const t=r.metadata?.vulnerabilities?.total; if(r.error || r.auditReportVersion!==2 || !r.vulnerabilities || !Number.isInteger(t) || (s==="0")!==(t===0)) process.exit(1)' security/spec000/audit-runtime-before.json "$status"
```

Expected: JSON v2 íntegro y error/status comprobados como en Step 1. `--omit=dev` ayuda a clasificar dependencias; no afirma por sí solo que los paquetes remanentes formen parte del bundle.

- [ ] **Step 3: Trazar rutas de dependencia exactas para los paquetes vulnerables conocidos**

```bash
{
  echo "=== EXPLAIN BRACES ==="
  npm explain braces
  echo ""
  echo "=== EXPLAIN POSTCSS-SELECTOR-PARSER ==="
  npm explain postcss-selector-parser
  echo ""
  echo "=== EXPLAIN MICROMATCH ==="
  npm explain micromatch
  echo ""
  echo "=== EXPLAIN POSTCSS-NESTED ==="
  npm explain postcss-nested
} > security/spec000/explains.txt
```

Expected: `security/spec000/explains.txt` muestra qué dependencias directas o de desarrollo introducen `braces` (e.g. `chokidar`, `micromatch`) y `postcss-selector-parser` (e.g. `tailwindcss`, `postcss-nested`).

- [ ] **Step 4: Construir la tabla de inventario en `security/spec000/inventory.md`**

Generar el inventario tabular contrastando con los 7 hallazgos históricos (`docs/schema95-jwt-http-checks.md`):

| ID Aviso | Severidad | Paquete Afectado | Versión Resuelta | Ruta / Consumidor | Ámbito | Exposición Real | Acción Técnica Propuesta |
|---|---|---|---|---|---|---|---|
| `GHSA-vfj7-8cjw-p6xm` | Alta | `braces` | `3.0.3` bloqueada | Confirmar con `npm explain` y el `nodePath` del audit | Según alcance medido | ReDoS/agotamiento de pila mediante patrones anidados; evaluar quién controla la entrada de build. | `3.0.3` sigue afectada; buscar actualización del consumidor o excepción aprobada por ruta y versión. |
| `GHSA-rj75-hqrm-r3gf` | Alta | `postcss-selector-parser` | `6.1.4` afectada | Confirmar consumidores y rutas instaladas | Según alcance medido | Agotamiento de CPU por complejidad de selectores; no afirmar entrada pública sin trazarla. | Probar `>=7.1.6` con cada consumidor; si no es compatible, proponer excepción exacta aprobada. |
| Demás avisos | ... | ... | ... | ... | ... | ... | Investigar según JSON actual. |

- [ ] **Step 5: Revisar integridad y versionar la línea base**

Verificar que ambos archivos sean JSON npm audit v2 con metadata, que cualquier código 1 corresponda a un reporte con hallazgos y que no contengan `error`. Comparar `metadata.vulnerabilities.total` con el total de entradas afectadas, conservar rutas de `npm explain`, fecha y versiones.

```bash
git diff --check -- security/spec000/
git add security/spec000/
git commit -m "docs(security): registrar inventario inicial de vulnerabilidades y línea base conectada"
```

---

## T2 — Saneamiento de dependencias directas en `apps/web` y preservación de Tailwind 3

**Archivos:**
- Modify: `apps/web/package.json`
- Modify: `package-lock.json` (generado y validado con npm `10.9.9`)
- Inspect: `apps/web/src/**`, `apps/mobile/package.json`

**Interfaces y artefactos:**
- Consumes: Confirmación de que `lucide-angular` y `@angular/animations` no tienen imports en `apps/web/src`.
- Produces: `apps/web/package.json` limpio de dependencias superfluas; lockfile sincronizado y consistente; builds verificados de Shared Core, Web y Mobile.

- [ ] **Step 1: Comprobar exhaustivamente la ausencia de imports en código fuente**

```bash
# Verificar que NO existen imports de lucide-angular (solo @lucide/angular)
grep -rn "from 'lucide-angular'" apps/web/src apps/mobile/src libs/shared-core/src || true
grep -rn 'from "lucide-angular"' apps/web/src apps/mobile/src libs/shared-core/src || true

# Verificar que NO existen imports de @angular/animations ni provideAnimations
grep -rn "@angular/animations" apps/web/src apps/mobile/src libs/shared-core/src || true
grep -rn "provideAnimations" apps/web/src apps/mobile/src libs/shared-core/src || true
```

Expected: Ningún resultado para `lucide-angular` (todos los componentes usan `@lucide/angular`). Ningún resultado para `@angular/animations` o `provideAnimations`.

- [ ] **Step 2: Retirar `lucide-angular` y `@angular/animations` de `apps/web/package.json`**

Editar `apps/web/package.json`:
- Eliminar `"@angular/animations": "22.2.1",` de `"dependencies"`
- Eliminar `"lucide-angular": "^1.0.0",` de `"dependencies"`
- Mantener estrictamente `"@lucide/angular": "^1.55.0"`
- Mantener estrictamente `"tailwindcss": "3.4.19"` en devDependencies de Web y Mobile

- [ ] **Step 3: Actualizar el lockfile de forma limpia con npm**

```bash
npm install --package-lock-only
```

Expected: `package-lock.json` se actualiza reflejando la eliminación de `lucide-angular` y `@angular/animations` sin introducir alteraciones arbitrarias en otros paquetes.

- [ ] **Step 4: Ejecutar verificación de instalación limpia y compilación completa del monorepo**

```bash
npm ci
npm run build:shared
npm run lint:web
npm run build:web
npm run build:mobile
```

Expected:
- `npm ci`: finaliza con código 0 sin modificar `package-lock.json`.
- `npm run build:shared`: genera `libs/shared-core/dist` con código 0.
- `npm run lint:web`: 0 errores de ESLint.
- `npm run build:web`: salida de Angular CLI con código 0.
- `npm run build:mobile`: salida de Angular/Ionic CLI con código 0.

- [ ] **Step 5: Inspección visual de componentes con Tailwind y Lucide**

Verificar que:
1. `apps/web/src/shared/ui/badge/badge.component.ts` renderiza iconos Lucide sin error.
2. `apps/web/src/shared/ui/button/button.component.ts` mantiene `LucideLoader`.
3. `apps/web/src/pages/operations-dashboard/operations-dashboard.page.ts` compila y renderiza estilos sin `@angular/animations`.
4. Las animaciones basadas en CSS de Tailwind (`animate-pulse`, `animate-spin`) operan normalmente vía `styles.css`.

- [ ] **Step 6: Commit de la limpieza de dependencias**

```bash
git add apps/web/package.json package-lock.json
git commit -m "chore(web): retirar dependencias sin uso lucide-angular y angular-animations"
```

---

## T3 — Remediación de vulnerabilidades, evaluación de overrides y excepciones temporales

**Archivos:**
- Modify: `package.json` (raíz: sección `overrides` si procede)
- Modify: `package-lock.json`
- Create/versionar: `security/spec000/exceptions.json`, `security/spec000/audit-after.json`
- Evidencia efímera de ejecución: `artifacts/spec000/` (ignorada por Git)

**Interfaces y artefactos:**
- Consumes: `security/spec000/inventory.md`, `security/spec000/explains.txt`.
- Produces: Lockfile con dependencias saneadas; registro de excepciones temporales formales con ID, justificación, responsable y fecha de vencimiento; `audit-after.json` con reducción comprobada.

- [ ] **Step 1: Análisis técnico de `GHSA-vfj7-8cjw-p6xm` (`braces <=3.0.3`)**

Verificar el aviso upstream y el árbol de T1. El rango `<=3.0.3` no tiene versión parcheada en el stream 3.x a la fecha de revisión; **no** declarar `braces@3.0.3` resuelto. Investigar consumidores instalados y versiones mantenidas. Si no hay salida compatible, proponer una excepción en `security/spec000/exceptions.json` con estado `pending-approval`; CI la rechazará hasta recibir aprobación identificable. No asignar responsables ni aprobación ficticios.

- [ ] **Step 2: Análisis y prueba empírica de `GHSA-rj75-hqrm-r3gf` (`postcss-selector-parser <7.1.6`)**

- Rango afectado: `< 7.1.6`. Versión segura: `7.1.6`.
- Consumidores actuales: `tailwindcss@3.4.19` declara `^6.1.2`; `postcss-nested@6.2.0` declara `^6.1.1`.
- Probar experimentalmente si un override a `7.1.6` en `package.json` raíz es compatible:

```json
{
  "overrides": {
    "postcss-selector-parser": "7.1.6"
  }
}
```

Ejecutar:
```bash
npm install --package-lock-only
npm ci
npm run build:web
npm run build:mobile
```

Criterio de decisión:
- **Si compila y las clases CSS complejas (pseudo-selectores, `@apply`, anidamientos) se procesan sin errores:** Conservar el override a `7.1.6`, registrar evidencia de compatibilidad y repetir builds.
- **Si `tailwindcss` o `postcss-nested` fallan al compilar o arrojan incompatibilidad de API:** Revertir el override de `postcss-selector-parser` y proponer una excepción en `security/spec000/exceptions.json`. La propuesta pendiente no habilita CI; conservar el bloqueo hasta aprobación o remediación.

- [ ] **Step 3: Registrar excepciones propuestas y aprobadas con alcance exacto**

La plantilla pendiente no autoriza el aviso. No cambiar `status` a `approved` sin decisión escrita de una persona responsable; si nadie aprueba, la compuerta permanece roja y el hallazgo sigue abierto. Una excepción aprobada identifica cada ruta/version instalada afectada; una entrada nueva en la misma ruta no queda cubierta si cambia la versión.

```json
{
  "schemaVersion": 1,
  "exceptions": [
    {
      "status": "pending-approval",
      "advisoryId": "GHSA-vfj7-8cjw-p6xm",
      "package": "braces",
      "vulnerableRange": "<=3.0.3",
      "nodePath": "node_modules/braces",
      "installedVersion": "3.0.3",
      "scope": "development-build",
      "rationale": "Explicar con evidencia por qué no es posible retirar o actualizar el consumidor y qué entradas pueden alcanzar el paquete.",
      "mitigations": ["Indicar controles que se verifican en la cadena de build."],
      "approvedBy": null,
      "approvalReference": null,
      "approvedAt": null,
      "expiresAt": null
    }
  ]
}
```

Para la aprobación, `status` debe ser `approved`, y `approvedBy`, `approvalReference`, `approvedAt` y `expiresAt` deben tener valores reales y válidos. Mantener el registro en Git para revisión de PR. El validador compara aviso, paquete, rango, ruta npm exacta y versión contra auditoría/lockfile; no acepta una lista global de IDs.

- [ ] **Step 4: Generar auditoría final `security/spec000/audit-after.json` y comparar diferencial**

```bash
if npm audit --json --workspaces --include-workspace-root > security/spec000/audit-after.json; then status=0; else status=$?; fi
if [ "$status" -gt 1 ]; then exit "$status"; fi
node -e 'const [f,s]=process.argv.slice(1); const r=JSON.parse(require("node:fs").readFileSync(f,"utf8")); const t=r.metadata?.vulnerabilities?.total; if(r.error || r.auditReportVersion!==2 || !r.vulnerabilities || !Number.isInteger(t) || (s==="0")!==(t===0)) process.exit(1)' security/spec000/audit-after.json "$status"
```

Comparar avisos, rangos, paquetes, rutas, severidades y totales antes/después; confirmar que cada residual tiene excepción aprobada y acotada. Estado de red, salida npm y JSON son evidencia conjunta: un fallo de conexión nunca cuenta como cero avisos.

- [ ] **Step 5: Commit de la remediación y registro de excepciones**

```bash
git add package.json package-lock.json security/spec000/
git commit -m "fix(security): aplicar remediaciones y registrar excepciones auditables"
```

---

## T4 — Automatización del control de seguridad en CI/CD y política de Node

**Archivos:**
- Modify: `.nvmrc`, `.npm-version`, `.npmrc`, `package.json` (raíz: `engines`, `scripts`), `.github/workflows/ci.yml`, `.github/workflows/deploy.yml`, `infra/docker/web/Dockerfile`
- Create: `.github/scripts/audit-check.mjs`, `.github/scripts/audit-check.test.mjs`
- Create/versionar política: `security/spec000/exceptions.json`

**Interfaces y artefactos:**
- Consumes: `security/spec000/exceptions.json`, `.nvmrc`, `.npm-version`, política única de Node/npm.
- Produces: Compuerta de auditoría cerrada ante reportes inválidos, excepciones no aprobadas y avisos fuera del alcance aprobado; versiones comprobadas de Node/npm en CI, Docker y deploy, con builds Web y Mobile.

- [ ] **Step 1: Reutilizar la política fijada en T0 y comprobarla al inicio de cada workflow**

`.nvmrc` queda fijado en `22.23.3`; `.npm-version`, en `10.9.9`; `.npmrc` activa `engine-strict=true`. El `package.json` raíz declara `engines.node="^22.23.3"` y `engines.npm="10.9.9"`. En ambos workflows, configurar Node con `node-version-file: .nvmrc`, imprimir versiones y detener si `npm --version` no coincide con `.npm-version`. Fijar la etapa Node del Dockerfile de Web a `node:22.23.3-alpine`; no dejarla en el tag mayor flotante.

- [ ] **Step 2: Escribir pruebas de la compuerta antes de implementarla**

Crear `.github/scripts/audit-check.test.mjs` con `node:test` y `node:assert/strict`; ejecutar `node --test .github/scripts/audit-check.test.mjs`. Probar reporte v2 limpio/status 0, reporte válido con hallazgo no aprobado/status 1, error de registro `{error:{code:"EAI_AGAIN"}}`, JSON vacío o truncado, versión de reporte desconocida, hallazgo con una ruta o versión nueva, excepción sin aprobación, excepción expirada y excepción aprobada exacta. Los errores, formas incompletas, hallazgos no cubiertos y excepciones sobrantes deben dar fallo. Confirmar rojo por falta de export antes de implementar.

- [ ] **Step 3: Implementar `.github/scripts/audit-check.mjs` como verificador cerrado ante error**

Exportar una función pura `evaluateAudit({status,stdout,exceptions,lockfile,now})` para tests y usarla desde el comando. Requisitos obligatorios:

1. Ejecutar `npm audit --json --workspaces --include-workspace-root` con `spawnSync`; conservar `status`, `stdout` y `stderr`. Aceptar únicamente status 0 con total 0 o status 1 con reporte válido y total mayor que 0. Cualquier otro status falla.
2. Exigir JSON parseable de `auditReportVersion === 2`, sin `error`, con `metadata.vulnerabilities` y `vulnerabilities` en forma válida. Comprobar que totales/severidades no estén vacíos, mal tipados o inconsistentes. Nunca usar `{}` por defecto ante ausencia de datos.
3. Cargar `security/spec000/exceptions.json` y `package-lock.json`. Exigir `schemaVersion === 1`; cada excepción debe tener `status === "approved"`, aviso, paquete, rango vulnerable, `nodePath`, versión instalada, alcance, justificación, al menos una mitigación, aprobador, referencia de aprobación y fechas RFC 3339 válidas con caducidad futura. Rechazar placeholders, duplicados y registros sin correspondencia actual.
4. Para cada aviso directo (`via` con URL GHSA), autorizar **cada** `info.nodes` solo si existe una excepción con igualdad exacta de ID, `package`, `via.range`, `nodePath` e `installedVersion`; validar esta última con `lockfile.packages[nodePath].version`. Las entradas string en `via` solo son relaciones transitivas: comprobar que su paquete exista en el mapa, y validar sus avisos hoja con el mismo control. Si falta detalle, ubicación o aviso hoja, fallar.
5. Cualquier hallazgo nuevo, severidad, rango, paquete, ruta/version no autorizados, error del registro, respuesta inesperada o excepción sobrante produce status 1. Imprimir exactamente qué regla falló sin declarar éxito ni ocultar errores de npm.
6. Persistir el JSON crudo de auditoría en `artifacts/spec000/audit-ci.json` antes de evaluar el resultado. `artifacts/` es evidencia de ejecución excluida de Git; la política y auditorías base/residual están en `security/spec000/` versionado.

Agregar los campos `engines` de raíz a `package.json` sin alterar workspaces. El comando no incorpora dependencias externas y se ejecuta con Node nativo.

- [ ] **Step 4: Agregar script al `package.json` raíz**

```json
{
  "scripts": {
    "audit:check": "node .github/scripts/audit-check.mjs"
  }
}
```

- [ ] **Step 5: Integrar la compuerta, builds y conservación de evidencia en CI**

En `frontend-qa` de CI y deploy, seleccionar Node mediante `.nvmrc`, validar npm mediante `.npm-version` y después de `npm ci` añadir el audit. Agregar también `npm run build:mobile` al QA de ambos workflows. Tras todos los pasos, subir el JSON con `actions/upload-artifact@v4` y `if: always()` para conservarlo incluso cuando la compuerta falle; mismo patrón para deploy.

En ambos `frontend-qa`, usar este setup y conservar además los pasos actuales de lint/Web:

```yaml
      - uses: actions/setup-node@v4
        with:
          node-version-file: .nvmrc
          cache: npm

      - name: "Validar versiones fijadas de Node y npm"
        run: |
          test "$(node --version)" = "v$(cat .nvmrc)"
          test "$(npm --version)" = "$(cat .npm-version)"

      - name: "Instalar dependencias npm"
        run: npm ci

      - name: "Verificar auditoría de dependencias y vulnerabilidades"
        run: npm run audit:check

      - name: "Compilar Mobile"
        run: npm run build:mobile

      - name: "Conservar el reporte de auditoría"
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: npm-audit-${{ github.run_id }}-${{ github.job }}
          path: artifacts/spec000/audit-ci.json
          if-no-files-found: warn
```

Aplicar el paso de upload también a deploy. El validador guarda el JSON antes de devolver fallo; `if: always()` conserva el motivo cuando el job falla. No versionar reportes generados en `artifacts/`.

- [ ] **Step 6: Alinear deploy, Docker y el build Mobile con la política fijada**

Verificar en CI/deploy `node-version-file: .nvmrc`, npm exacto de `.npm-version`, auditoría, lint, builds Web y Mobile, y upload-artifact. Fijar la etapa de construcción Docker en `node:22.23.3-alpine`; comprobar que npm corresponde a `.npm-version`. La etapa runtime sigue Nginx y no requiere Node.

- [ ] **Step 7: Correr pruebas positivas/negativas y luego el audit real**

```bash
node --test .github/scripts/audit-check.test.mjs
npm run audit:check
```

Expected: Todas las pruebas nativas pasan. El audit real sale 0 solo si su reporte está íntegro y todos los hallazgos residuales coinciden con excepciones exactas aprobadas; si la red falla o la excepción sigue pendiente, falla con diagnóstico explícito.

- [ ] **Step 8: Commit de la automatización en CI/CD**

```bash
git add .nvmrc .npm-version .npmrc package.json .github/scripts/audit-check.mjs .github/scripts/audit-check.test.mjs .github/workflows/ci.yml .github/workflows/deploy.yml infra/docker/web/Dockerfile security/spec000/exceptions.json
git commit -m "ci(security): integrar compuerta automatizada de auditoría npm y fijar Node 22 LTS"
```

---

## T5 — Conciliación documental, actualización de ADR-04 y checklist prelaunch

**Archivos:**
- Modify: `docs/boveda/03 Arquitectura/Decisiones de arquitectura.md:19, 51-60`
- Modify: `docs/boveda/03 Arquitectura/Verificacion de seguridad antes del lanzamiento.md:30-33`
- Modify: `CONTRIBUTING.md`

**Interfaces y artefactos:**
- Consumes: Especificación FR-08 y remediaciones ejecutadas.
- Produces: Documentación alineada con el paquete oficial `@lucide/angular`, política de versiones de Node/npm en `CONTRIBUTING.md` y checklist prelaunch actualizado.

- [ ] **Step 1: Conciliar ADR-04 en `docs/boveda/03 Arquitectura/Decisiones de arquitectura.md`**

Actualizar las menciones a iconos en ADR-04:
- Aclarar que la librería oficial utilizada en código es `@lucide/angular`.
- Conservar la nota histórica de que el paquete original registrado fue `lucide-angular`.
- Confirmar versión de Node.js fijada en Node 22 LTS.

- [ ] **Step 2: Actualizar el checklist de seguridad en `docs/boveda/03 Arquitectura/Verificacion de seguridad antes del lanzamiento.md`**

En la fila de «Dependencias vulnerables»:
- Especificar el comando formal: `npm run audit:check` (que consulta workspaces y verifica excepciones aprobadas).
- Documentar la política versionada y aprobada de excepciones en `security/spec000/exceptions.json`. Los logs de ejecución viven en `artifacts/` y CI los conserva mediante upload-artifact.

- [ ] **Step 3: Actualizar guía de desarrollo en `CONTRIBUTING.md`**

- Especificar Node `22.23.3` y npm `10.9.9`, activar `.nvmrc` y validar `.npm-version`.
- Indicar que antes de abrir PR, `npm run check` y `npm run audit:check` deben terminar con éxito.

- [ ] **Step 4: Commit de la conciliación documental**

```bash
git add "docs/boveda/03 Arquitectura/Decisiones de arquitectura.md" "docs/boveda/03 Arquitectura/Verificacion de seguridad antes del lanzamiento.md" CONTRIBUTING.md
git commit -m "docs(architecture): conciliar ADR-04 con @lucide/angular y documentar control de auditoría en prelaunch"
```

---

## T6 — Evaluación experimental de pnpm (P2 condicional e independiente)

**Archivos:**
- Worktree aislada: `/tmp/istpetdev-spec000-pnpm`, rama `experiment/pnpm-evaluation`, creada desde el commit final de P1. El checkout principal no cambia de rama.
- Create (solo en la worktree): `pnpm-workspace.yaml`, `pnpm-lock.yaml`, `security/spec000/pnpm-evaluation-report.md`
- Modify (solo en la worktree): `package.json`, y `apps/web/package.json` / `apps/mobile/package.json` si dependen de workspaces locales

**Interfaces y artefactos:**
- Consumes: commit de P1 con T4 en verde; decisión P2 diferida al equipo técnico (Bryan).
- Produces: informe con tiempos de instalación fría comparables, tamaño de `node_modules` y de cache/store por separado, compatibilidad con Angular 22/Ionic 8/Tailwind 3 y estado de auditoría.

- [ ] **Step 1: Crear la worktree aislada sin tocar el checkout de P1**

```bash
git status --porcelain            # Debe estar vacío; si no, detenerse.
test ! -e /tmp/istpetdev-spec000-pnpm
! git show-ref --verify --quiet refs/heads/experiment/pnpm-evaluation
git worktree add -b experiment/pnpm-evaluation /tmp/istpetdev-spec000-pnpm HEAD
cd /tmp/istpetdev-spec000-pnpm
node --version && npm --version   # v22.23.3 y 10.9.9, igual que P1
```

Expected: worktree nueva en el commit de P1. Si la ruta o la rama ya existen, no borrarlas ni sobrescribirlas: preguntar antes.

- [ ] **Step 2: Medir la línea base npm con cache fría aislada**

```bash
rm -rf /tmp/spec000-npm-cache
time -p npm ci --cache /tmp/spec000-npm-cache
du -sb node_modules /tmp/spec000-npm-cache
rm -rf node_modules apps/*/node_modules libs/*/node_modules
```

Expected: tiempo `real` y tamaños de `node_modules` y cache registrados por separado.

- [ ] **Step 3: Declarar el workspace pnpm y generar su lockfile**

En `package.json` raíz agregar `"packageManager": "pnpm@9.12.0"`. Crear `pnpm-workspace.yaml`:

```yaml
packages:
  - 'libs/shared-core'
  - 'apps/web'
  - 'apps/mobile'
```

Cambiar a `workspace:*` solo las dependencias que apunten a paquetes locales del monorepo.

```bash
corepack pnpm@9.12.0 install --lockfile-only
```

Expected: `pnpm-lock.yaml` generado sin instalar `node_modules`; esta ejecución no se cronometra.

- [ ] **Step 4: Medir una única instalación fría de pnpm con store aislado**

```bash
rm -rf /tmp/spec000-pnpm-store node_modules apps/*/node_modules libs/*/node_modules
time -p corepack pnpm@9.12.0 install --frozen-lockfile --store-dir /tmp/spec000-pnpm-store
du -sb node_modules /tmp/spec000-pnpm-store
```

Expected: medición comparable con el Step 2 (misma máquina, commit, Node, cache/store vacío). No repetir `pnpm install` sobre una instalación existente para tomar la medida.

- [ ] **Step 5: Probar compilación y auditoría bajo pnpm**

```bash
corepack pnpm@9.12.0 --filter @istpetdev/shared-core build
corepack pnpm@9.12.0 --filter @istpetdev/web lint
corepack pnpm@9.12.0 --filter @istpetdev/web build
corepack pnpm@9.12.0 --filter @istpetdev/mobile build
corepack pnpm@9.12.0 audit --json > /tmp/spec000-pnpm-audit.json || true
```

Expected: registrar código de salida de cada comando, errores de dependencias pares y totales de la auditoría. Un fallo de red de `pnpm audit` se informa como «sin medir», nunca como cero avisos.

- [ ] **Step 6: Documentar el informe comparativo**

Crear `security/spec000/pnpm-evaluation-report.md` en la worktree con:
- Versiones de SO, Node, npm y pnpm; commit base.
- Tiempo de instalación fría npm vs pnpm (Steps 2 y 4).
- Tamaño de `node_modules` local y de cache/store por separado; no comparar solo los symlinks de pnpm contra los paquetes copiados de npm.
- Compatibilidad con Angular CLI, Ionic y directivas de Tailwind.
- Conclusión y recomendación para el equipo técnico (Bryan).

```bash
git add package.json pnpm-workspace.yaml pnpm-lock.yaml apps/*/package.json security/spec000/pnpm-evaluation-report.md
git commit -m "docs(security): evaluar pnpm como gestor del monorepo (experimento P2)"
```

- [ ] **Step 7: Volver al checkout principal sin alterar P1**

```bash
cd /home/joeman/Documents/istpet-dev/istpetdev-platform
git status --porcelain            # Sigue igual que antes del Step 1
git worktree list
```

Expected: el checkout de P1 no cambió de rama ni de lockfile. La worktree se conserva hasta que el equipo revise el informe; se elimina con `git worktree remove /tmp/istpetdev-spec000-pnpm` solo con autorización.
