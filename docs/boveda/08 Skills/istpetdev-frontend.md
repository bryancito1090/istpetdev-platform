---
tipo: skill
estado: vigente
actualizado: 2026-10-09
tags: [istpetdev, ia, skills]
---

# IstpetDev — frontend web y móvil

**Cuándo usarla:** Interfaz de IstpetDev. Usar al crear o revisar pantallas y componentes Angular del panel web, la app o PWA del conductor y receptor, la consulta pública por QR, mapas, estados de carga y error, captura offline, accesibilidad, tokens de diseño o textos de la interfaz.

> Documentación de la skill `istpetdev-frontend`. El archivo `SKILL.md` no se versiona en la bóveda: quien quiera usarla con su asistente de IA copia este contenido a la carpeta de skills de su herramienta (frontmatter con `name: istpetdev-frontend` y la descripción de arriba). Catálogo y origen en [[Catalogo y plan de skills]].

Rutas relativas a la raíz del repositorio. Leer antes [[istpetdev-contexto]] y [[istpetdev-contrato]].

## Leer primero

- `03 Arquitectura/Frontend con Feature-Sliced Design.md`: capas, slices, API pública e imports (ADR-13, aceptada).
- `03 Arquitectura/Frontend y componentes.md`: Signals y RxJS, `libs/shared-core`, mapas, consulta QR, RBAC en cliente.
- `03 Arquitectura/Movil offline y sincronizacion.md`: persistencia, protocolo de sincronización y conflictos.
- `02 Producto/Usuarios y flujos.md`: roles, flujo central, excepciones y estados.
- `03 Arquitectura/RBAC y configuracion del sistema.md`: roles/campos y formularios versionados; la UI administra configuración declarativa bajo contrato, sin convertir nombres de roles en autoridad.

## Versiones y decisiones

Versiones exactas solo de `03 Arquitectura/Repositorio de software y versiones.md`; no subir ni bajar ninguna por cuenta propia:

- Angular 22 con TypeScript 6.0, Node 22, RxJS 7.8 y Tailwind 3.4 (ADR-04).
- Móvil: PWA con Ionic 8 e IndexedDB primero; Capacitor 8 solo para el empaquetado nativo posterior (ADR-06).
- Sesión: Cognito con Authorization Code + PKCE (`03 Arquitectura/Identidad OIDC y sesiones.md`); los tokens no entran en la cola offline.
- Librería de mapas: PENDIENTE (ADR-05).

## Reglas FSD

- Capas de arriba abajo: app → pages → widgets → features → entities → shared. Una capa solo importa capas inferiores.
- Cada slice expone su API pública (`index.ts`); sin imports profundos ni entre slices de la misma capa.
- Empezar en la página o entidad y extraer a `features` o `widgets` solo cuando haya reutilización real.
- Contratos y DTOs desde `libs/shared-core`; no redefinir tipos del backend.
- `shared/ui/map-view` es visual y no conoce logística; `widgets/route-map` lo orquesta.

## Reglas de interfaz

- **Cinco estados en cada pantalla o componente con datos:** normal, cargando (skeleton, no el texto «Cargando…»), vacío (mensaje y acción sugerida), error (mensaje y reintento) y sin permiso (qué ve un rol sin acceso).
- **Offline:** distinguir siempre «guardado en el teléfono», «enviando», «aceptado por el servidor», «conflicto» y «evidencia pendiente». Nunca mostrar aceptado lo que solo está en el dispositivo.
- **RBAC en cliente** solo orienta (ocultar botones, guards); la decisión real es del servidor.
- **Accesibilidad WCAG 2.2 AA:** contraste 4,5:1 en texto normal y 3:1 en texto grande; estado que no dependa solo del color; foco visible; objetivos táctiles de al menos 24×24 px (en móvil, 44–48 px); navegación completa con teclado; respetar `prefers-reduced-motion`.
- **Tokens:** ningún color, espaciado, radio o tipografía escrito directamente en un componente; todo sale de tokens semánticos.
- **Textos:** el botón nombra la acción y el resultado usa la misma palabra («Publicar ruta» → «Ruta publicada»); los errores dicen qué pasó y qué hacer.
- **Iconos** de un sistema real (por ejemplo Lucide o el de la librería de componentes elegida); nunca emojis como iconos.
- Consulta pública QR: ruta aislada y liviana, sin datos privados (firmas, teléfonos, ubicación del conductor).

Detalle de identidad visual, tokens y leyes de UX: sección «Referencia: diseno-y-accesibilidad» de esta nota.

## Prohibido inventar

- Versiones, librerías o rutas de carpetas no registradas en `00 Inicio/Hechos canonicos.md`.
- Campos o endpoints que no estén en el contrato.
- Un arquetipo visual como decisión del equipo: hasta que se registre, es PENDIENTE (recomendación en las referencias).

## Verificar

- Revisar los cinco estados y el recorrido offline (V-08, V-11 y V-12 de `07 Demo/Plan de validacion.md`).
- Límites FSD (V-23) y build/lint del repositorio cuando existan (comandos PENDIENTES en `00 Inicio/Hechos canonicos.md`).
- Probar en un teléfono real por HTTPS antes de dar por terminado un flujo móvil.
- Antes de levantar una versión o preparar un ensayo/despliegue, usar [[istpetdev-prelaunch]] y aportar la evidencia de esta skill; verificar respuesta web/runtime afectado.

## Origen

Adaptada de las skills personales `frontend-lead-personalidad` (cinco estados, declarar arquetipo, autocrítica, iconos reales), `design-tokens-a11y` (no inventar valores, tokens en tres capas, WCAG 2.2 AA, leyes de UX) e `identidad-visual-brand` (evitar el aspecto genérico de IA, un elemento firma, textos desde el usuario). Se excluyeron, por contradecir la bóveda: preguntar el stack, la estructura Angular con NgModules, `src/components/ui/` y Tailwind v4 como valor fijo.

## Referencia: diseno-y-accesibilidad

*Diseño visual, tokens y accesibilidad*

Referencia de la skill `istpetdev-frontend`. Nada de esto reemplaza una decisión del equipo: lo marcado PENDIENTE se confirma y se registra en `00 Inicio/Hechos canonicos.md`.

### Arquetipo visual e identidad cromática

Estado: **Aceptado y ratificado (Arquetipo B — Técnico/Minimalista con Identidad Expo Clean Ecuador).**

- **Estilo:** Función antes que decoración, espacio en blanco generoso, tipografía sans neutral y tipografía monoespaciada para cifras, color disciplinado enfocado en acción y estado logístico.
- **Paleta oficial:** Extraída del material oficial de Expo Clean Ecuador ([[Sistema de diseno y colores]]):
  - Primario (`primary`): Clean Cyan `#00A7C4` (higiene, agua, desinfección, navegación).
  - Secundario (`secondary`): Expo Lime `#9EC200` (sostenibilidad, insumos verdes, stock en regla).
  - Acento (`accent` / `cta`): Energy Orange `#EA601F` (botones de acción crítica, escaneo QR, alerta preventiva).
  - Neutro oscuro (`neutral-dark`): Charcoal Slate `#2D3748` (legibilidad y contraste AAA).
- **Iconografía oficial:** Lucide Icons (`lucide-angular`).
- **Prohibido:** Arquetipos expresivos (neumorfismo, aurora, retro) en pantallas densas en datos o críticas en accesibilidad.

### Evitar el aspecto genérico de IA

Salvo que el equipo lo pida expresamente, no usar:

- Fondo crema con acento terracota.
- Fondo negro puro con un único acento neón.
- Diseño tipo periódico con líneas finas y cero radio de borde.
- Tailwind sin tokens propios.

Gastar la audacia en **un solo elemento firma** (por ejemplo, la línea de tiempo de custodia de la caja) y mantener el resto disciplinado. Animar solo cuando informe algo (carga, cambio de estado).

### Tokens en tres capas

1. **Primitivos:** valores puros (`blue-500`).
2. **Semánticos:** propósito (`--color-action-primary`, `--color-status-risk`).
3. **Componente:** uso concreto (`--button-primary-bg`).

Ningún componente usa primitivos ni valores directos. Antes de crear un token o componente nuevo, buscar si ya existe uno equivalente.

### Estados de riesgo y entrega

El estado nunca depende solo del color: acompañarlo de texto e icono.

| Estado | Texto mínimo |
|---|---|
| Riesgo crítico | «Crítico: X días de cobertura» |
| En tránsito | «En tránsito» con hora del último evento |
| Guardado en el teléfono | «Guardado en el teléfono; pendiente de envío» |
| Aceptado | «Aceptado por el servidor» con hora del servidor |
| Conflicto | «Conflicto: requiere revisión» con el motivo |

### Leyes de UX aplicadas

- **Miller:** máximo 5–7 elementos en la navegación principal y 5–7 campos por paso de formulario.
- **Hick:** pocas opciones visibles a la vez; la acción principal explícita («Publicar ruta», no «Aceptar»).
- **Fitts:** la acción principal es la más grande y cercana al pulgar en móvil; las destructivas, lejos.
- **Tesler:** el sistema absorbe la complejidad (valores por defecto, validación en tiempo real).
- **Jakob:** no reinventar patrones estándar de navegación.

### Accesibilidad (WCAG 2.2 AA)

- Contraste 4,5:1 en texto normal y 3:1 en texto grande o en negrita.
- Foco de teclado visible y no tapado por barras fijas.
- Objetivos táctiles de al menos 24×24 px en web; en móvil seguir 44 pt (iOS) o 48 dp (Android).
- Todo arrastre tiene alternativa por clic.
- Permitir pegar contraseñas y usar gestores; no exigir pruebas cognitivas.
- Respetar `prefers-reduced-motion`.

Origen: adaptado de las skills personales `frontend-lead-personalidad`, `design-tokens-a11y` e `identidad-visual-brand`.
