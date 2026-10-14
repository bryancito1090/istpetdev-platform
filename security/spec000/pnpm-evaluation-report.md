# Informe de Evaluación Experimental de pnpm (Spec 000 - Tarea T6 / P2)

- **Fecha:** 2026-10-14
- **Entorno evaluado:** Windows 10/11, Node.js v24.21.0 local (Objetivo de CI: Node v22.23.3, npm 10.9.9), Corepack 0.36.0, pnpm 9.12.0.
- **Rama experimental:** `experiment/pnpm-evaluation`
- **Commit base P1:** `48e5bbe` (PR #61)

---

## 1. Hallazgos y Fricciones Detectadas

### A. Fallo de permisos en Corepack (`EPERM`)
Al intentar habilitar pnpm mediante `corepack enable pnpm` en la máquina local:
```text
Internal Error: EPERM: operation not permitted, open 'C:\Program Files\nodejs\pnpm'
```
* **Impacto en el equipo:** Los desarrolladores y pipelines que no cuenten con permisos de administrador en Windows o que usen runners restringidos no pueden ejecutar `pnpm` de forma nativa sin configurar shims manuales de Corepack.

### B. Incompatibilidad de Sintaxis de Workspace con npm
En `apps/web/package.json` y `apps/mobile/package.json`, las dependencias cruzadas del monorepo están declaradas como:
```json
"@istpetdev/shared-core": "*"
```
* **Comportamiento en npm:** `npm` reconoce el workspace local y lo enlaza correctamente por symlink sin salir a internet.
* **Comportamiento en pnpm:** `pnpm` intenta resolver `@istpetdev/shared-core` consultando el registro público `https://registry.npmjs.org/@istpetdev%2Fshared-core`, fallando con error **HTTP 404 (Not Found)**. Para que funcione en pnpm, se requiere migrar la sintaxis a `"workspace:*"`, lo cual causaría incompatibilidad si otro miembro del equipo continúa usando `npm`.

### C. Cumplimiento Estricto de Motores (`engines.node`)
Al definir `"engines": { "node": "^22.23.3" }`, pnpm bloquea la instalación (`ERR_PNPM_UNSUPPORTED_ENGINE`) de manera inmediata si la máquina anfitriona tiene cualquier versión superior (ej: Node 24), a menos que se fuerce explícitamente con banderas de omisión.

---

## 2. Comparativa de Rendimiento y Operación

| Criterio | npm 10.9.9 (Línea Base P1) | pnpm 9.12.0 (Experimento P2) | Conclusión |
|---|---|---|---|
| **Resolución de Workspaces** | Nativa (`libs/`, `apps/`) sin cambios de manifiestos | Exige `pnpm-workspace.yaml` y cambiar dependencias a `workspace:*` | npm es más tolerante y estándar en monorepos pequeños. |
| **Integración CI/CD y Deploy** | Soportado nativamente en GitHub Actions (`setup-node` con `cache: npm`) y Docker | Requiere `setup-pnpm` action y ajustar Dockerfiles para copiar store | Mayor sobrecarga de configuración en pipelines. |
| **Auditoría de Seguridad** | `npm audit` integrado con script `.github/scripts/audit-check.mjs` | `pnpm audit` tiene esquema y códigos de salida distintos | Requiere reescribir la compuerta de auditoría. |
| **Tiempo de instalación en CI** | ~5-7 s con `npm ci` y cache caliente | ~3-5 s estimado | Ganancia marginal de 2 segundos que no compensa el riesgo de rotura. |

---

## 3. Conclusión y Recomendación para el Equipo

1. **Recomendación inmediata:** **Permanecer en npm (Opción A / P1).**
   * El flujo actual con npm 10.9.9, overrides de seguridad y compuerta automatizada `npm run audit:check` funciona al 100% en local y en CI sin riesgo de errores 404 ni problemas de permisos en Windows.
2. **Postergación de pnpm a P2 (Post-Hackathon):**
   * Si en el futuro se decide migrar a `pnpm`, se deberá realizar una migración sincronizada en todo el equipo, cambiando los manifiestos a `workspace:*` y agregando el action `pnpm/action-setup` en los flujos de GitHub Actions.
