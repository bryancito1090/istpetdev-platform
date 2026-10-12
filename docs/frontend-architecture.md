# Arquitectura y Decisiones de Frontend — IstpetDev

Documento oficial de frontend para el Reto 1 de Expo Clean Ecuador 2026.
Liderado por: **Jefe de Frontend** y validado con **Líder Técnico (Bryan)**.

---

## 1. Decisiones Canónicas Registradas (ADR)

Toda decisión técnica proviene de la bóveda de decisiones (`istpetdev-hackathon-2026`):

| ID ADR | Componente / Decisión | Elección Adoptada | Justificación |
|---|---|---|---|
| **ADR-04** | Stack Web & Móvil, UI Kit y Reactividad | **Angular 22 Standalone + Ionic 8**, Signals + RxJS, `@istpetdev/shared-core`, **Arquetipo B (Técnico/Minimalista)**, **Lucide Icons** (`lucide-angular`) y tokens semánticos en `tailwind.config.js`. | Arquitectura FSD limpia, sin emojis como iconos, alta densidad de datos (estilo Linear/Vercel) y estado reactivo sin fugas de memoria. |
| **ADR-05** | Mapas y Cartografía | **Leaflet (`leaflet` + `@types/leaflet`) con OpenStreetMap**. Componente desacoplado `shared/ui/map-view` orquestado por `widgets/route-map`. | 100% libre, de código abierto, ultraligera (<40 KB gzip), sin costos de facturación ni riesgo de cuotas de API keys que arriesguen la demo. |
| **ADR-06 / ADR-17** | Modo Offline y Firma Digital Móvil | **PWA con IndexedDB (`idb`) y captura de firma táctil con `signature_pad`**, con identificadores UUID v7 y sincronización idempotente. | Conforme a RF-06 y RNF-04: operatividad en modo avión en hospitales/carretera, cola de eventos local y preservación íntegra de la custodia al reiniciar. |
| **ADR-10 / ADR-18** | Cliente OIDC y Sesiones Cognito | **Authorization Code con PKCE S256** contra el User Pool dev (`us-east-1_4Y2j5DINp`), sin client secret en navegador/móvil, con `AuthInterceptor` para Bearer JWT. | Seguridad estándar conforme a RFC 7636 y ADR-10, protegiendo rutas privadas y peticiones HTTP hacia la API. |
| **ADR-19** | Automatización FSD y Marcadores Arquitectónicos | **ESLint (`eslint-plugin-boundaries`) + Nx Project Tags (`nx.json`, `project.json`)**. Validación estricta de jerarquía unidireccional, aislamiento entre slices y acceso exclusivo vía Public API (`index.ts`). | Evita que cualquier desarrollador o IA rompa las capas FSD. Falla de forma determinista en `npm run check` y pipelines de CI/CD ante cualquier import ilegal o deep-import no autorizado. |

---

## 2. Reglas de Interfaz Obligatorias

1. **Los 5 Estados en Pantallas y Componentes con Datos:**
   - **Normal:** Datos presentados correctamente.
   - **Cargando:** **Skeleton Screens** (prohibido poner texto plano "Cargando...").
   - **Vacío:** Mensaje explicativo y acción sugerida (ej. "No hay rutas activas. Planificar ruta").
   - **Error:** Mensaje claro de error y botón de reintento.
   - **Sin Permiso:** Vista controlada si el rol autenticado no tiene acceso.

2. **Los 5 Estados Offline en Móvil:**
   - `Guardado en el teléfono; pendiente de envío`
   - `Enviando`
   - `Aceptado por el servidor` (con marca temporal del servidor)
   - `Conflicto: requiere revisión`
   - `Evidencia pendiente` (foto en S3 separada del registro transaccional)

3. **Tokens de Diseño en 3 Capas:**
   - **Primitivos:** `blue-500`, `slate-900`, `emerald-500`.
   - **Semánticos:** `--color-action-primary`, `--color-status-risk`, `--color-status-success`.
   - **Componente:** `--button-primary-bg`, `--card-border`.
   - *Prohibido hardcodear colores o clases aisladas en componentes sin tokens.*

4. **Accesibilidad (WCAG 2.2 AA):**
   - Contraste 4.5:1 en texto normal y 3:1 en texto grande/negrita.
   - Ningún estado depende solo del color: siempre se acompaña de texto e icono.
   - Objetivos táctiles mínimos: 24x24 px en web y 44x44 px en móvil.

---

## 3. Gobernanza y Automatización FSD (ESLint & Marcadores Nx)

Conforme a **ADR-19**, la arquitectura Feature-Sliced Design está protegida contra regresiones mediante validación estática automatizada.

### 3.1 Marcadores de Capas (Tags & Boundaries)
Cada archivo TypeScript en `apps/web/src` es categorizado automáticamente según su marcador de capa:

| Marcador / Elemento | Patrón de Directorio | Jerarquía | Puede Importar De |
|---|---|---|---|
| `app` | `apps/web/src/app/**/*` | Capa 6 (Raíz) | `app`, `pages`, `widgets`, `features`, `entities`, `shared`, `@istpetdev/shared-core` |
| `pages` | `apps/web/src/pages/*` | Capa 5 | `widgets`, `features`, `entities`, `shared`, `@istpetdev/shared-core` |
| `widgets` | `apps/web/src/widgets/*` | Capa 4 | `features`, `entities`, `shared`, `@istpetdev/shared-core` (y mismo widget) |
| `features` | `apps/web/src/features/*` | Capa 3 | `entities`, `shared`, `@istpetdev/shared-core` (y misma feature) |
| `entities` | `apps/web/src/entities/*` | Capa 2 | `shared`, `@istpetdev/shared-core` (y misma entity) |
| `shared` | `apps/web/src/shared/**/*` | Capa 1 (Base) | `shared`, `@istpetdev/shared-core` |
| `shared-core` | `libs/shared-core/src/**/*` | Biblioteca Compartida | Ninguna dependencia de UI/frontend |

### 3.2 Reglas Inviolables
1. **Flujo Unidireccional (Regla FSD 1):** Un módulo solo puede importar elementos de capas estrictamente inferiores. Si un componente de `features` importa de `widgets` o `pages`, ESLint emite un error bloqueante `boundaries/dependencies`.
2. **Aislamiento Horizontal entre Slices (Regla FSD 2):** Prohibido el acoplamiento cruzado entre slices del mismo nivel (ejemplo: `feature-A` no puede importar de `feature-B`).
3. **Public API Obligatoria (Regla FSD 3):** Todo slice debe exponerse mediante un archivo `index.ts`. Están estrictamente prohibidos los *deep imports* (ejemplo: importar `@features/check-api-health/check-api-health.component` generará un fallo; se debe importar `@features/check-api-health`).

### 3.3 Comandos de Verificación
- **Ejecutar Linting FSD:** `npm run lint` o `npm run lint:web`
- **Corrección automática:** `npm run lint:fix`
- **Validación global de proyecto (CI/CD):** `npm run check` (ejecuta linting + compilación .NET + build Web + build Mobile).

### 3.4 Barreras de Bloqueo Inevitables (Cero Evasión)
Para garantizar que ningún desarrollador pueda ejecutar o compilar omitiendo estas pruebas:
1. **Pre-compilación forzada (`prebuild` & `prebuild:web`):** Antes de que `ng build` genere el bundle, npm ejecuta automáticamente `npm run lint`. Si hay violaciones FSD, el build se cancela de inmediato con código de error 1.
2. **Pre-arranque forzado (`prestart` & `predev:web`):** Al levantar el servidor de desarrollo (`npm run dev:web`), se valida la arquitectura previamente.
3. **Bloqueo en Git (`.githooks/pre-commit`):** Ningún desarrollador puede realizar `git commit` si su código rompe los límites arquitectónicos. El hook se activa automáticamente al instalar dependencias (`npm install`).
