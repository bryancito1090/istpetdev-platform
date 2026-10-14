# Gestión verificable de dependencias — plan de implementación

> Para agentes de implementación: ejecutar las tareas en orden y marcar cada paso (`- [ ]`) solo con evidencia guardada. Leer primero [spec.md](spec.md).

**Goal:** Reducir dependencias sin uso y tratar cada vulnerabilidad Node con remediación verificada o excepción temporal trazable.

**Architecture:** P1 conserva npm y su lockfile raíz. El informe de auditoría determina las acciones; ninguna versión de override se da por segura antes de comprobar aviso, árbol y build. P2 evalúa pnpm por separado.

**Tech Stack:** npm workspaces `10.9.9`, Node `22.23.3`, Angular 22, Tailwind 3.4.19 y GitHub Actions. `.nvmrc`, `.npm-version`, `engines` y `.npmrc` mantienen la política común en desarrollo, CI, Docker y deploy.

---

## Archivos y responsabilidad

| Archivo | Responsabilidad |
|---|---|
| `apps/web/package.json` y `package-lock.json` | Retirar dependencias sin uso y registrar el árbol resuelto. |
| `package.json`, `.nvmrc`, `.npm-version`, `.npmrc` | Fijar compatibilidad y herramientas Node/npm; guardar overrides solo si son compatibles y demostrados. |
| `.github/workflows/ci.yml` | Ejecutar instalación limpia, builds y política de auditoría. |
| `.github/workflows/deploy.yml`, `infra/docker/web/Dockerfile` | Usar el mismo gestor, lockfile y versiones de Node/npm que CI. |
| `CONTRIBUTING.md` y notas de bóveda | Documentar política de gestor, auditoría y nombre del paquete de iconos. |
| `pnpm-workspace.yaml` y `pnpm-lock.yaml` | Solo P2, desde una worktree temporal; no cambian P1 durante la evaluación. |

## Tarea 1: capturar inventario actual, con red disponible

- [ ] Registrar `node --version` y `npm --version` junto con fecha y commit.
- [ ] Ejecutar desde la raíz `npm audit --json --workspaces --include-workspace-root > audit-before.json` y conservar **código de salida e informe**. La salida no cero por hallazgos es esperada; la salida de error de red no equivale a cero hallazgos.
- [ ] Ejecutar `npm audit --json --omit=dev --workspaces --include-workspace-root > audit-runtime-before.json` para clasificar exposición. Este informe no reemplaza el anterior.
- [ ] Ejecutar `npm explain braces` y `npm explain postcss-selector-parser` tras `npm ci`; registrar rutas, consumidores, versiones y sus rangos declarados.
- [ ] Crear inventario de avisos con ID, severidad, ruta, alcance, versión corregida disponible, opción de tratamiento y responsable. Confirmar si los siete hallazgos históricos siguen vigentes.

## Tarea 2: limpieza directa sin migrar Tailwind

- [ ] Confirmar con búsqueda en `apps/web/src` ausencia de imports de `lucide-angular`, `@angular/animations` y `provideAnimations`.
- [ ] Retirar los dos paquetes de `apps/web/package.json` y actualizar **solo** `package-lock.json` con la versión de npm elegida para el proyecto.
- [ ] Ejecutar `npm ci` desde raíz, `npm run build:shared`, `npm run lint:web`, `npm run build:web` y `npm run build:mobile`. Registrar códigos de salida.
- [ ] Revisar iconos y estilos principales de login, dashboard y mapa. Dejar la versión de Tailwind `3.4.19` en ambos workspaces.

## Tarea 3: resolver avisos uno por uno

- [ ] Para `braces`, comprobar el aviso [GHSA-vfj7-8cjw-p6xm](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm): `3.0.3` está afectada y no hay parche publicado al corte del spec. **No añadir `"braces": "^3.0.3"` como supuesto arreglo.** Investigar actualización o reemplazo compatible de los consumidores; volver a auditar el árbol. Si permanece, redactar excepción con mitigación, responsable y caducidad.
- [ ] Para `postcss-selector-parser`, comprobar [GHSA-rj75-hqrm-r3gf](https://github.com/advisories/GHSA-rj75-hqrm-r3gf) y probar una ruta que resuelva `7.1.6` o superior. Tailwind 3.4.19 y `postcss-nested@6.2.0` declaran rangos 6.x; una anulación a 7 requiere compilación Web/Mobile y revisión visual antes de aceptarse.
- [ ] Repetir para los demás avisos que arroje la auditoría actual; no inferir que los dos paquetes anteriores explican todos los hallazgos.
- [ ] Registrar para cada override propuesto versión exacta, consumidor, motivo, prueba de build, resultado del audit y condición para retirarlo. Repetir `npm ci` y los cuatro comandos de verificación de la tarea 2 tras cambiar el lockfile.
- [ ] Guardar `audit-after.json`, comparar IDs, severidades y rutas con el inventario inicial; documentar hallazgos nuevos o excepciones pendientes.

## Tarea 4: política automatizada y documentación

- [ ] Fijar Node `22.23.3` y npm `10.9.9` según la release soportada por las dependencias del lockfile. Verificar `.nvmrc`, `.npm-version`, `engines`, `.npmrc`, ambos workflows, Docker y una instalación limpia.
- [ ] Añadir auditoría de raíz y workspaces a CI/deploy, subir informe aunque falle y probar respuestas npm inválidas. Toda excepción versionada cubre un solo aviso, paquete, rango, ruta y versión; exige aprobación, responsable, justificación y vencimiento. La compuerta falla ante reporte inválido o hallazgo sin autorización.
- [ ] Actualizar `CONTRIBUTING.md`, checklist de seguridad y ADR-04: el código usa `@lucide/angular`; conservar nota histórica del nombre anterior.
- [ ] Revisar diff del lockfile, salida de builds, comparación visual e informes de auditoría antes de marcar P1 como implementado. No declarar cero vulnerabilidades si una excepción permanece.

## Tarea 5: experimento pnpm (P2 independiente)

- [ ] Fijar una versión de pnpm en `packageManager` y crear `pnpm-workspace.yaml` para `libs/shared-core`, `apps/web` y `apps/mobile` en una rama de prueba.
- [ ] Cambiar las dependencias locales a `workspace:*`; generar `pnpm-lock.yaml` y ejecutar instalación limpia, builds y auditoría.
- [ ] Medir tiempo y espacio frente a npm bajo condiciones comparables. Probar imports, Angular CLI y pares antes de considerar hoisting o relajación de reglas.
- [ ] Si se adopta, migrar CI, deploy, documentación, cachés y scripts en el mismo cambio; eliminar `package-lock.json` para dejar un solo gestor y lockfile activo.

- Política versionada y auditorías inicial/residual: `security/spec000/`. Logs, TRX y capturas efímeros: `artifacts/spec000/`, con retención de CI.

## Reversión

Si una remediación rompe build o estilos, revertir **el cambio específico** de manifiesto y lockfile y registrar el fallo. Mantener los informes para investigar otra ruta; no ejecutar `npm audit fix --force` ni aceptar una excepción implícita.
