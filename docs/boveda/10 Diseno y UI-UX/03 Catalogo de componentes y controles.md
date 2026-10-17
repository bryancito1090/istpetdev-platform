---
tipo: diseno
estado: vigente
actualizado: 2026-10-16
tags: [diseno, componentes, ui-ux, angular, tailwind, stripe-dashboard]
---

# Catálogo de componentes y controles UI

Especificación técnica de la biblioteca de componentes y controles para la plataforma web (`apps/web`) y PWA de campo (`apps/mobile`), estructurada bajo la estética ejecutiva de Stripe Dashboard y los principios de [[00 Indice y principios de diseno]].

```mermaid
flowchart TD
    subgraph CatalogoComponentes ["Catálogo de Componentes IstpetDev"]
        Botones["1. Botones y Acciones<br>(Primario, Secundario, Chips)"]
        Inputs["2. Controles de Formulario<br>(Inputs 36px, Selects, Dropzones)"]
        Badges["3. Badges de Estado<br>(Píldoras sin corchetes ni emojis)"]
        Tabs["4. Pestañas Horizontales<br>(-mb-px subrayadas)"]
        Drawers["5. Drawers Laterales<br>(fixed inset-0 !m-0)"]
        Tablas["6. Tablas y Tarjetas<br>(Bordes #E3E8EE, Divisores #F1F3F5)"]
        Buscador["7. Barra de Búsqueda y Filtros<br>(Input ancho completo + Botón embudo)"]
        Empty["8. Estados de Vacío<br>(Círculo #F1F3F5 y Sin resultados)"]
    end
```

> [!NOTE] Enfoque Puro de Presentación
> Los componentes y controles de este catálogo no contienen lógica de negocio aislada ni alteran contratos de API. Su función es 100% estructural: maquetar con precisión visual y ergonomía los datos reales provenientes de los servicios y signals del sistema, sin incrustar datos quemados ni simular acciones.

---

## 1. Botones y Acciones Interactivas

Todos los botones estándar tienen una altura fija de 36px (`h-9`), esquinas suavemente redondeadas de 6px (`rounded-md`), tipografía en 12px a 13px (`text-xs` o `text-[13px]`) y peso `font-medium`.

| Variante | Clase Tailwind Base | Rol en la Interfaz | Comportamiento Hover / Focus |
|---|---|---|---|
| **Primario (Acción Principal)** | `h-9 px-4 bg-[#635BFF] hover:bg-[#5248E0] text-white text-xs font-medium rounded-md shadow-2xs transition-colors inline-flex items-center justify-center gap-2 disabled:opacity-50` | Guardar, confirmar ruta, nuevo registro | Foco: `focus:ring-2 focus:ring-[#635BFF] focus:ring-offset-1` |
| **Secundario / Neutro** | `h-9 px-3.5 bg-white hover:bg-slate-50 text-[#30313D] border border-[#E3E8EE] text-xs font-medium rounded-md shadow-2xs transition-colors inline-flex items-center justify-center gap-2` | Cancelar, actualizar, exportar | Borde tenue, fondo sutil al pasar cursor |
| **Peligro / Destructivo** | `h-9 px-4 bg-[#DF1B41] hover:bg-[#C01033] text-white text-xs font-medium rounded-md shadow-2xs transition-colors inline-flex items-center justify-center gap-2` | Cancelar ruta publicada, revocar custodia | Foco con anillo rojo `#DF1B41` |
| **Botón Tipo Chip (Acción Rápida)** | `h-8 px-3 rounded-md border border-[#E3E8EE] bg-white text-xs font-medium text-[#30313D] hover:bg-slate-50 transition-colors inline-flex items-center gap-1.5 shadow-2xs` | Filtros horizontales, cambiar vista | Micro-elevación imperceptible |

### Código de Referencia (HTML / Angular)

```html
<!-- Botón Primario -->
<button
  type="button"
  (click)="onConfirm()"
  [disabled]="isLoading()"
  class="h-9 px-4 bg-[#635BFF] hover:bg-[#5248E0] text-white text-xs font-medium rounded-md shadow-2xs transition-colors inline-flex items-center justify-center gap-2 disabled:opacity-50 focus:outline-none focus:ring-2 focus:ring-[#635BFF] focus:ring-offset-1">
  @if (isLoading()) {
    <svg class="animate-spin h-3.5 w-3.5 text-white" viewBox="0 0 24 24" fill="none">
      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path>
    </svg>
  }
  <span>Publicar ruta planificada</span>
</button>
```

---

## 2. Controles de Formulario e Inputs

* **Altura Estandarizada:** 36px (`h-9`).
* **Borde y Superficie:** `border border-[#E3E8EE] bg-white text-[#30313D] rounded-md px-3 text-xs`.
* **Estado Focus Obligatorio:** `focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] focus:outline-none`.
* **Placeholder:** `placeholder:text-[#8792A2]`.
* **Estado de Error:** Borde rojo `#DF1B41` con microtexto inferior en `#DF1B41` de 11px.

```html
<div class="space-y-1.5">
  <label class="block text-xs font-medium text-[#30313D]">Capacidad máxima de carga (kg)</label>
  <input
    type="number"
    [(ngModel)]="maxLoadKg"
    placeholder="Ej. 3500"
    class="w-full h-9 px-3 text-xs bg-white border border-[#E3E8EE] rounded-md text-[#30313D] placeholder-[#8792A2] focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] focus:outline-none transition-colors" />
</div>
```

---

## 3. Badges e Indicadores de Estado

Queda estrictamente prohibido incluir corchetes como texto plano (ej. `[Éxito]` o `[Alerta]`) y emojis (ej. `⚠️`, `✅`, `❌`). Los estados se comunican mediante pastillas redondeadas (`rounded-full`), texto de 11px (`text-[11px]`) con seminegrita (`font-medium`) y borde sutil.

| Estado Semántico | Clases Tailwind del Badge | Ejemplo de Texto |
|---|---|---|
| **Conforme / Completado** | `bg-emerald-50 text-emerald-700 border border-emerald-200/80 px-2 py-0.5 rounded-full text-[11px] font-medium inline-flex items-center gap-1` | Entregado conforme |
| **En Tránsito / En Curso** | `bg-sky-50 text-sky-700 border border-sky-200/80 px-2 py-0.5 rounded-full text-[11px] font-medium inline-flex items-center gap-1` | En tránsito |
| **Alerta / En Observación** | `bg-amber-50 text-amber-700 border border-amber-200/80 px-2 py-0.5 rounded-full text-[11px] font-medium inline-flex items-center gap-1` | Stock crítico |
| **Rechazado / Daño** | `bg-rose-50 text-rose-700 border border-rose-200/80 px-2 py-0.5 rounded-full text-[11px] font-medium inline-flex items-center gap-1` | Recepción rechazada |
| **Planificado / Borrador** | `bg-slate-100 text-slate-600 border border-slate-200 px-2 py-0.5 rounded-full text-[11px] font-medium inline-flex items-center gap-1` | Plan borrador |

---

## 4. Pestañas Horizontales Subrayadas (Tabs)

Las pestañas no utilizan fondos oscuros ni cajas rectangulares toscas. Siguen el estándar en línea de Stripe Dashboard con borde inferior activo de 2px que se solapa a la línea divisoria de la página mediante margen negativo (`-mb-px`):

```html
<div class="flex items-center gap-6 sm:gap-8 border-b border-[#E3E8EE] -mb-px">
  <button
    type="button"
    (click)="activeTab.set('rutas')"
    class="pb-3 text-sm font-semibold transition-colors flex items-center gap-2 border-b-2"
    [ngClass]="activeTab() === 'rutas' 
      ? 'border-[#1A2C44] text-[#1A2C44]' 
      : 'border-transparent text-[#697386] hover:text-[#1A2C44]'">
    <span>Rutas Planificadas</span>
    <span class="text-xs px-2 py-0.2 rounded-full bg-[#F1F3F5] text-[#30313D] font-mono">
      {{ routePlans().length }}
    </span>
  </button>

  <button
    type="button"
    (click)="activeTab.set('novedades')"
    class="pb-3 text-sm font-semibold transition-colors flex items-center gap-2 border-b-2"
    [ngClass]="activeTab() === 'novedades' 
      ? 'border-[#1A2C44] text-[#1A2C44]' 
      : 'border-transparent text-[#697386] hover:text-[#1A2C44]'">
    <span>Novedades y Rechazos</span>
    @if (rejectionsCount() > 0) {
      <span class="text-xs px-2 py-0.2 rounded-full bg-rose-50 text-rose-700 border border-rose-200 font-mono">
        {{ rejectionsCount() }}
      </span>
    }
  </button>
</div>
```

---

## 5. Arquitectura de Drawers Laterales (Solución Definitiva a Margen Vertical)

> [!CAUTION] Regla Arquitectónica de Layout para Drawers
> Cuando un panel lateral `fixed` se ubica condicionalmente (`@if`) dentro de un contenedor padre con utilidades de espaciado vertical (`space-y-*`), el selector CSS inyecta `margin-top` al diálogo, desplazándolo verticalmente y rompiendo el alineamiento de pantalla completa.
> **Solución obligatoria:** Todo drawer o modal debe colocarse en un contenedor raíz desacoplado con `fixed inset-0 z-[100] overflow-hidden !m-0`.

```html
<!-- Estructura canónica del Drawer Lateral -->
@if (isDrawerOpen()) {
  <div class="fixed inset-0 z-[100] overflow-hidden !m-0" aria-labelledby="slide-over-title" role="dialog" aria-modal="true">
    <!-- Telón de fondo translúcido -->
    <div 
      class="absolute inset-0 bg-[#0A2540]/30 backdrop-blur-xs transition-opacity"
      (click)="closeDrawer()"></div>

    <div class="fixed inset-y-0 right-0 max-w-full flex pl-10">
      <div class="w-screen max-w-md sm:max-w-xl bg-white border-l border-[#E3E8EE] shadow-2xl flex flex-col">
        <!-- Cabecera del Drawer -->
        <div class="p-6 border-b border-[#E3E8EE] flex items-center justify-between bg-[#F8FAFC]">
          <div>
            <h3 class="text-base font-bold text-[#1A2C44]">{{ drawerTitle() }}</h3>
            <p class="text-xs text-[#697386] mt-0.5">{{ drawerSubtitle() }}</p>
          </div>
          <button 
            type="button" 
            (click)="closeDrawer()" 
            class="h-8 w-8 rounded-md border border-[#E3E8EE] bg-white text-[#697386] hover:text-[#1A2C44] flex items-center justify-center transition-colors">
            ✕
          </button>
        </div>

        <!-- Cuerpo Desplazable -->
        <div class="flex-1 overflow-y-auto p-6 space-y-6">
          <ng-content />
        </div>

        <!-- Pie de Acción -->
        <div class="p-4 border-t border-[#E3E8EE] bg-[#F8FAFC] flex items-center justify-between">
          <kbd class="text-[11px] text-[#8792A2] px-2 py-0.5 bg-white border border-[#E3E8EE] rounded">ESC para salir</kbd>
          <div class="flex items-center gap-2">
            <button (click)="closeDrawer()" class="h-9 px-3.5 border border-[#E3E8EE] rounded-md text-xs font-medium text-[#30313D] bg-white hover:bg-slate-50">Cerrar</button>
            <button (click)="saveChanges()" class="h-9 px-4 bg-[#635BFF] hover:bg-[#5248E0] text-white text-xs font-medium rounded-md">Guardar</button>
          </div>
        </div>
      </div>
    </div>
  </div>
}
```

---

## 6. Tarjetas y Tablas de Datos Maestras

* **Borde Exterior:** `border border-[#E3E8EE] rounded-xl overflow-hidden bg-white shadow-2xs`.
* **Cabecera de Tabla (`thead`):** `bg-[#F8FAFC] border-b border-[#E3E8EE] text-[11px] font-semibold text-[#4F566B] uppercase tracking-wider`.
* **Separador de Filas:** `divide-y divide-[#F1F3F5]`.
* **Fila Interactiva:** `hover:bg-slate-50/60 transition-colors text-xs text-[#30313D]`.

---

## 7. Barra de Búsqueda de Ancho Completo con Filtros y Columnas

Patrón extraído directamente de las capturas de la consola ejecutiva (Imágenes 4 y 5):

* **Estructura:** Fila horizontal con selector segmentado de píldoras arriba (`Todo`, `Activo`, `En Observación`), seguido de un buscador de ancho completo con botón de filtros rápidos y botón de configuración de columnas.
* **Clases Tailwind:**
  * Contenedor de búsqueda: `relative flex-1 flex items-center`.
  * Input de ancho completo: `w-full h-9 pl-9 pr-4 text-xs bg-white border border-[#E3E8EE] rounded-md text-[#30313D] placeholder-[#8792A2] focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] focus:outline-none transition-colors`.
  * Icono de lupa: `absolute left-3 text-[#8792A2] pointer-events-none`.
  * Botón Filtrar con embudo: `h-9 px-3 rounded-md border border-[#E3E8EE] bg-white text-xs font-medium text-[#30313D] hover:bg-slate-50 transition-colors inline-flex items-center gap-1.5 shadow-2xs`.
  * Botón de Columnas: `h-9 w-9 rounded-md border border-[#E3E8EE] bg-white text-[#4F566B] hover:text-[#1A2C44] hover:bg-slate-50 transition-colors flex items-center justify-center shadow-2xs`.

```html
<!-- Fila de Filtros en Píldora -->
<div class="flex items-center gap-2 mb-3">
  <button 
    type="button"
    class="px-3 py-1 rounded-full text-xs font-medium border transition-colors"
    [ngClass]="activeFilter() === 'all' 
      ? 'border-[#1A2C44] bg-white text-[#1A2C44] font-semibold' 
      : 'border-transparent text-[#697386] hover:text-[#1A2C44]'">
    Todo
  </button>
  <button 
    type="button"
    class="px-3 py-1 rounded-full text-xs font-medium border transition-colors"
    [ngClass]="activeFilter() === 'critical' 
      ? 'border-[#1A2C44] bg-white text-[#1A2C44] font-semibold' 
      : 'border-transparent text-[#697386] hover:text-[#1A2C44]'">
    Críticos
  </button>
</div>

<!-- Barra de Búsqueda y Botones de Utilidad -->
<div class="flex items-center gap-2">
  <div class="relative flex-1 flex items-center">
    <svg class="absolute left-3 w-4 h-4 text-[#8792A2]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <circle cx="11" cy="11" r="8"></circle>
      <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
    </svg>
    <input 
      type="text" 
      placeholder="Busca por nombre, código o destino..." 
      class="w-full h-9 pl-9 pr-4 text-xs bg-white border border-[#E3E8EE] rounded-md text-[#30313D] placeholder-[#8792A2] focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] focus:outline-none transition-colors" />
  </div>

  <button 
    type="button" 
    class="h-9 px-3 rounded-md border border-[#E3E8EE] bg-white text-xs font-medium text-[#30313D] hover:bg-slate-50 transition-colors inline-flex items-center gap-1.5 shadow-2xs">
    <svg class="w-3.5 h-3.5 text-[#697386]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"></polygon>
    </svg>
    <span>Filtrar</span>
  </button>

  <button 
    type="button" 
    title="Configurar columnas"
    class="h-9 w-9 rounded-md border border-[#E3E8EE] bg-white text-[#4F566B] hover:text-[#1A2C44] hover:bg-slate-50 transition-colors flex items-center justify-center shadow-2xs">
    <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <line x1="4" y1="21" x2="4" y2="14"></line>
      <line x1="4" y1="10" x2="4" y2="3"></line>
      <line x1="12" y1="21" x2="12" y2="12"></line>
      <line x1="12" y1="8" x2="12" y2="3"></line>
      <line x1="20" y1="21" x2="20" y2="16"></line>
      <line x1="20" y1="12" x2="20" y2="3"></line>
    </svg>
  </button>
</div>
```

---

## 8. Estados de Vacío y Sin Resultados (Empty States)

Patrones canónicos de Stripe Dashboard para listas sin datos (Imagen 4) y búsquedas sin coincidencias (Imagen 5):

### 8.1 Estado de Vacío Canónico (Primer Uso o Sin Registros)
Contenedor sobrio centrado verticalmente:

```html
<div class="p-12 text-center flex flex-col items-center justify-center bg-white border border-[#E3E8EE] rounded-xl">
  <!-- Círculo de icono neutro -->
  <div class="w-12 h-12 rounded-full bg-[#F1F3F5] text-[#4F566B] flex items-center justify-center mb-4">
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
      <circle cx="12" cy="7" r="4"></circle>
    </svg>
  </div>
  <h4 class="text-sm font-bold text-[#1A2C44]">No hay rutas planificadas para hoy</h4>
  <p class="text-xs text-[#697386] max-w-sm mt-1 mb-4 leading-relaxed">
    Genere una nueva propuesta de distribución a partir de los pedidos hospitalarios pendientes.
  </p>
  <button 
    type="button" 
    class="h-9 px-4 bg-white border border-[#E3E8EE] hover:bg-slate-50 text-[#30313D] text-xs font-medium rounded-md shadow-2xs transition-colors inline-flex items-center gap-1.5">
    + Iniciar planificación
  </button>
</div>
```

### 8.2 Estado Sin Coincidencias de Búsqueda (Filtro Activo)
Barra horizontal limpia con botón de restablecimiento:

```html
<div class="p-4 rounded-xl border border-[#E3E8EE] bg-white flex items-center justify-between">
  <div>
    <h5 class="text-xs font-bold text-[#1A2C44]">Sin resultados</h5>
    <p class="text-xs text-[#697386] mt-0.5">Ningún registro coincide con los criterios de búsqueda seleccionados.</p>
  </div>
  <button 
    type="button"
    (click)="resetFilters()"
    class="h-8 px-3 rounded-md border border-[#E3E8EE] bg-white text-xs font-medium text-[#30313D] hover:bg-slate-50 transition-colors shadow-2xs">
    Borrar todo
  </button>
</div>
```
