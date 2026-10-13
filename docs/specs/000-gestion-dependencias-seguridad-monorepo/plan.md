# 000 — Gestión de dependencias, remediación de vulnerabilidades y evaluación pnpm: plan de implementación

**Goal:** Eliminar las dependencias deprecadas de `apps/web`, remediar las 7 vulnerabilidades de Tailwind CSS v3 mediante directivas de control de versiones y definir la hoja de ruta para la migración a `pnpm`.

**Architecture:** Mantenimiento de la capa de tooling y workspaces del monorepo Node.js / TypeScript. Modificaciones acotadas a manifiestos de paquetes (`package.json`) y configuración de paquetes anidados sin alterar código fuente ni dependencias de runtime.

**Tech Stack:** Node.js v24.21.0, npm 11.1.0 / pnpm (Corepack 0.36.0), Angular 22.2.1, Tailwind CSS 3.4.19, Lucide Angular 1.55.0.

**Spec:** [`spec.md`](spec.md)

## Global Constraints

- Prohibido el uso de `npm audit fix --force` para evitar la actualización descontrolada a Tailwind CSS v4.
- No alterar código de componentes en `apps/web/src/` ni `libs/shared-core/`.
- Respetar la compatibilidad con Node.js 24.
- Conservar los comandos de build y lint existentes (`npm run build:web`, `npm run lint:web`).

## Review Focus

1. Ruptura de clases CSS o configuración al tocar Tailwind $\rightarrow$ Comprobar `npm run build:web` y verificar que el bundle CSS contenga las utilidades de color corporativo.
2. Iconos faltantes tras eliminar `lucide-angular` $\rightarrow$ Verificar que todas las importaciones apunten a `@lucide/angular` y compilen sin error.
3. Regresiones en la resolución de módulos de monorepo $\rightarrow$ Probar la resolución cruzada con `@istpetdev/shared-core`.

---

## Estructura de archivos

| Acción | Ruta | Responsabilidad |
|---|---|---|
| Modificar | `apps/web/package.json` | Eliminar `lucide-angular` y `@angular/animations`. |
| Modificar | `package.json` | Agregar directivas de `overrides` para `braces` y `postcss-selector-parser`. |
| Crear (Opcional pnpm) | `pnpm-workspace.yaml` | Declarar las rutas de los workspaces para pnpm. |
| Crear (Opcional pnpm) | `.npmrc` | Definir políticas de hoisting (`public-hoist-pattern`). |

---

## Planes de Ejecución

### Opción A — Solución Inmediata en npm (Recomendada para la demo / Pre-evento)

Conserva `npm` sin alterar el flujo de trabajo de los integrantes del equipo:

1. **Limpieza en `apps/web/package.json`:**
   Remover de `dependencies`:
   ```json
   - "@angular/animations": "22.2.1",
   - "lucide-angular": "^1.0.0",
   ```
2. **Overrides en `package.json` (Raíz):**
   Agregar al objeto raíz:
   ```json
   "overrides": {
     "braces": "^3.0.3",
     "postcss-selector-parser": "^7.1.6"
   }
   ```
3. **Reinstalación y verificación:**
   ```powershell
   npm install
   npm run check:web
   cd apps/web; npm audit
   ```

---

### Opción B — Migración Integral a pnpm (Arquitectura Monorepo Definitiva)

1. **Habilitación de pnpm:**
   ```powershell
   corepack enable pnpm
   ```
2. **Creación de `pnpm-workspace.yaml` en la raíz:**
   ```yaml
   packages:
     - 'libs/*'
     - 'apps/*'
   ```
3. **Configuración de `.npmrc` en la raíz:**
   ```ini
   auto-install-peers=true
   strict-peer-dependencies=false
   ```
4. **Overrides en `package.json` raíz:**
   ```json
   "pnpm": {
     "overrides": {
       "braces": "^3.0.3",
       "postcss-selector-parser": "^7.1.6"
     }
   }
   ```
5. **Generación de lockfile y verificación:**
   ```powershell
   pnpm install
   pnpm --filter @istpetdev/web build
   pnpm audit
   ```

---

## Reversión

En caso de requerir reversión en Git:
```powershell
git checkout HEAD -- package.json apps/web/package.json package-lock.json
npm install
```
