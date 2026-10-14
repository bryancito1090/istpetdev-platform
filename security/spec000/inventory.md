# Inventario de Vulnerabilidades del Monorepo - Spec 000

- **Fecha de corte:** 2026-10-14
- **Herramienta:** npm audit v2 (`npm audit --json --workspaces --include-workspace-root`)
- **Estado de Runtime (`--omit=dev`):** **0 vulnerabilidades**. La aplicación en producción no transporta ninguna de estas dependencias al cliente/navegador.
- **Estado en Herramientas de Desarrollo / Build:** 7 vulnerabilidades altas derivadas exclusivamente de `braces <=3.0.3` en `tailwindcss@3.4.19` y `eslint-plugin-boundaries@7.2.0`.

---

## Tabla de Clasificación de Avisos

| ID Aviso | Severidad | Paquete Afectado | Versión Resuelta | Ruta / Consumidor | Ámbito | Exposición Real | Acción Técnica / Estado |
|---|---|---|---|---|---|---|---|
| `GHSA-vfj7-8cjw-p6xm` | Alta | `braces` | `3.0.3` | `tailwindcss` $\rightarrow$ `chokidar` / `fast-glob` / `micromatch` $\rightarrow$ `braces` | Build (Dev) | ReDoS/agotamiento de pila mediante patrones anidados en nombres de archivo locales durante compilación. Sin exposición en runtime. | Sin parche upstream en v3. Requiere migración a Tailwind 4 (P2) o excepción temporal controlada. |
| `GHSA-vfj7-8cjw-p6xm` | Alta | `braces` | `3.0.3` | `eslint-plugin-boundaries` $\rightarrow$ `@boundaries/elements` $\rightarrow$ `micromatch` $\rightarrow$ `braces` | Build (Dev) | Tooling de linteo en desarrollo. Sin exposición externa. | Excepción temporal controlada. |
| `GHSA-xw65-4hp5-5hc7` | Crítica | `handlebars` | `4.7.10` | `eslint-plugin-boundaries` | Build (Dev) | Inyección AST / template en herramientas de lint. | **Remediado al 100%** mediante override `handlebars@^4.7.10`. |
| `GHSA-8r5x-fm3f-whwj` | Crítica | `handlebars` | `4.7.10` | `eslint-plugin-boundaries` | Build (Dev) | Bypass en precompilación de plantillas. | **Remediado al 100%** mediante override `handlebars@^4.7.10`. |
| `GHSA-p8wg-vrv2-v86f` | Crítica | `handlebars` | `4.7.10` | `eslint-plugin-boundaries` | Build (Dev) | Bypass en comprobación de propiedades. | **Remediado al 100%** mediante override `handlebars@^4.7.10`. |
| `GHSA-3wwx-pv8p-q78v` | Alta | `undici` | `7.30.0` | `nx` $\rightarrow$ `@nx/workspace` $\rightarrow$ `@nx/eslint-plugin` | Build (Dev) | DoS en WebSocket deflate. | **Remediado al 100%** mediante override `undici@^7.30.0`. |
| `GHSA-rj75-hqrm-r3gf` | Moderada | `postcss-selector-parser` | `6.1.4` | `tailwindcss` $\rightarrow$ `postcss-nested` | Build (Dev) | Complejidad cuadrática en selectores locales CSS. | Mitigado en desarrollo; evaluado para P2 de Tailwind. |

---

## Conclusiones Técnicas
1. **0 vulnerabilidades en Runtime:** `audit-runtime-before.json` confirma 0 vulnerabilidades cuando el código se compila para el usuario final.
2. **Remediación Inmediata:** Se erradicaron las 3 vulnerabilidades críticas (`handlebars`) y las de `undici`.
3. **Preservación de Tailwind 3.4.19:** Se evita el salto breaking a Tailwind 4 que rompería estilos en la demo.
