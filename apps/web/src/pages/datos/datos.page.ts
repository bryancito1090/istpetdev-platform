import { Component, HostListener, ViewChild, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthSession } from '@entities/session';
import { FormsModule } from '@angular/forms';
import { ApiService, ImportReportDto } from '@shared/api';
import { MapViewComponent, MapMarker } from '@shared/ui';

export interface EntityColumnDef {
  name: string;
  type: string;
  required: boolean;
  desc: string;
  example: string;
}

export interface EntityDetailConfig {
  key: string;
  orderNumber: string;
  name: string;
  fileName: string;
  desc: string;
  fields: string;
  hasLocationMap: boolean;
  columns: EntityColumnDef[];
  csvTemplate: string;
  tableCols: { key: string; label: string; badge?: boolean }[];
}

/** Un perfil de mapeo es un objeto con `files` y solo claves de perfil (ver MappingProfile en el backend). */
export function isMappingProfile(text: string): boolean {
  try {
    const value: unknown = JSON.parse(text);
    if (!value || typeof value !== 'object' || Array.isArray(value)) return false;
    const allowed = new Set(['name', 'delimiter', 'decimalSeparator', 'dateFormats', 'files']);
    const keys = Object.keys(value);
    const files = (value as Record<string, unknown>)['files'];
    return keys.every(k => allowed.has(k)) && !!files && typeof files === 'object' && !Array.isArray(files);
  } catch {
    return false;
  }
}

@Component({
  selector: 'page-datos',
  standalone: true,
  imports: [CommonModule, FormsModule, MapViewComponent],
  template: `
    <div class="max-w-6xl mx-auto px-6 py-8 space-y-6">
      <!-- Encabezado Principal Estilo Stripe -->
      <div class="pb-1 border-b border-[#F1F3F5]">
        <h1 class="text-2xl sm:text-3xl font-bold text-[#1A2C44] tracking-tight font-sans">
          Catálogos y Datos Maestros
        </h1>
        <p class="text-xs sm:text-[13px] text-[#3C4F69] mt-1 max-w-2xl leading-relaxed">
          Gestiona los centros de salud, almacenes de origen, productos de higiene, vehículos de transporte y lotes del sistema.
        </p>
      </div>

      <!-- Banner Informativo Descartable Estilo Stripe Captura 1 -->
      @if (showTipBanner()) {
        <div class="bg-[#F8FAFC] border border-[#E3E8EE] rounded-lg px-4 py-2.5 flex items-center justify-between text-xs text-[#4F566B] shadow-2xs">
          <div class="flex items-center gap-2.5 min-w-0">
            <svg class="w-4 h-4 text-[#8792A2] shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 18v-5.25m0 0a6.002 6.002 0 00-4-5.659V5.25a2 2 0 014 0v1.841a6.002 6.002 0 004 5.659V18m-4 0h.01M9 21h6" />
            </svg>
            <span class="truncate">
              Carga tus archivos CSV o XLSX directamente en cada catálogo para actualizar inventarios hospitalarios y capacidades de transporte.
            </span>
          </div>
          <button 
            type="button" 
            (click)="showTipBanner.set(false)"
            class="text-[#8792A2] hover:text-[#1A2C44] p-1 rounded transition-colors ml-2 cursor-pointer shrink-0"
            title="Cerrar aviso">
            <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/></svg>
          </button>
        </div>
      }

      <!-- Cuadrícula de Entidades (Tarjetas Estilo Stripe) -->
      <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)] space-y-4">
        <div class="flex items-center justify-between">
          <div>
            <h2 class="text-[14px] leading-[20px] font-semibold text-[#1A2C44]">
              Catálogos Base del Sistema
            </h2>
            <p class="text-[13px] text-[#3C4F69] mt-0.5">
              Haz clic en cualquier categoría para consultar registros, agregar mediante formulario o subir archivos.
            </p>
          </div>
          <span class="text-[11px] font-medium text-[#3C4F69] bg-[#F8FAFC] px-2.5 py-1 rounded-full border border-[#E3E8EE]">
            5 Catálogos Principales
          </span>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 pt-1">
          @for (item of visibleEntityCards(); track item.key) {
            <div 
              (click)="openDrawer(item)"
              role="button"
              tabindex="0"
              (keydown.enter)="openDrawer(item)"
              (keydown.space)="openDrawer(item)"
              class="group bg-white border border-[#E3E8EE] rounded-lg p-4 hover:border-slate-300 hover:shadow-[0_1px_3px_rgba(0,0,0,0.05)] transition-all cursor-pointer flex flex-col justify-between text-left">
              
              <div>
                <div class="flex items-start justify-between gap-2 mb-2">
                  <div class="flex items-center gap-2.5">
                    <span class="w-7 h-7 rounded-md bg-[#F8FAFC] border border-[#E3E8EE] text-[#1A2C44] font-semibold text-xs flex items-center justify-center shrink-0 group-hover:bg-[#635BFF] group-hover:text-white transition-colors">
                      {{ item.orderNumber }}
                    </span>
                    <div>
                      <h3 class="text-[14px] leading-[20px] font-semibold text-[#1A2C44] group-hover:text-[#635BFF] transition-colors">
                        {{ item.name }}
                      </h3>
                      <span class="font-mono text-[11px] text-[#8792A2]">
                        {{ item.fileName }}
                      </span>
                    </div>
                  </div>
                  
                  <span class="text-[#635BFF] opacity-0 group-hover:opacity-100 transition-opacity">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
                    </svg>
                  </span>
                </div>

                <p class="text-[13px] leading-[18px] text-[#3C4F69] line-clamp-2 mt-1">
                  {{ item.desc }}
                </p>
              </div>

              <div class="mt-3 pt-2.5 border-t border-[#F1F3F5] flex items-center justify-between text-[11px]">
                <span class="text-[#8792A2] font-mono truncate max-w-[190px]" title="Campos: {{ item.fields }}">
                  {{ item.fields }}
                </span>
                <span class="text-[13px] font-medium text-[#635BFF] group-hover:underline flex items-center gap-1">
                  <span>Gestionar</span>
                  <span>&rarr;</span>
                </span>
              </div>
            </div>
          }
        </div>
      </div>

      @if (session.canWrite()) {
        <!-- Sección de Carga Masiva (Multi-archivo simultáneo) -->
        <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)] space-y-4">
          <div class="border-b border-[#F1F3F5] pb-3">
            <h2 class="text-[14px] leading-[20px] font-semibold text-[#1A2C44]">
              Carga Masiva de Múltiples Archivos
            </h2>
            <p class="text-[13px] text-[#3C4F69] mt-0.5">
              Si dispones de varios archivos CSV o XLSX preparados, puedes cargarlos todos juntos. Las dependencias entre catálogos se resolverán automáticamente.
            </p>
          </div>
          <div>
            <label class="block text-xs font-semibold text-[#30313D] mb-1.5">
              Seleccionar archivo(s) CSV o XLSX
            </label>
            <input 
              type="file" 
              multiple
              (change)="onFilesSelected($event)"
              accept=".csv,.xlsx,.json"
              class="block w-full text-xs text-[#3C4F69] file:mr-4 file:py-2 file:px-3.5 file:rounded-md file:border file:border-[#E3E8EE] file:text-xs file:font-medium file:bg-white file:text-[#30313D] hover:file:bg-slate-50 cursor-pointer border border-[#E3E8EE] rounded-md p-2 bg-[#F8FAFC]" />
            <p class="text-[11px] text-[#8792A2] mt-1.5">
              Formatos admitidos: .csv, .xlsx. El sistema detecta automáticamente la entidad por encabezados. Si incluyes un perfil de mapeo (.json), se aplicará de inmediato.
            </p>
          </div>

          @if (selectedFiles().length > 0) {
            <div class="space-y-3 bg-[#F8FAFC] p-4 rounded-md border border-[#E3E8EE]">
              <h3 class="text-xs font-semibold text-[#30313D] flex items-center gap-2">
                <svg class="w-4 h-4 text-[#8792A2]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                </svg>
                <span>Archivos listos para procesar ({{ selectedFiles().length }}):</span>
              </h3>
              <ul class="text-xs space-y-1.5 divide-y divide-[#E3E8EE]/60">
                @for (file of selectedFiles(); track file.name) {
                  <li class="pt-1.5 flex justify-between items-center text-[#30313D]">
                    <span class="font-medium truncate max-w-sm">{{ file.name }}</span>
                    <span class="font-mono text-[11px] text-[#8792A2] bg-white px-2 py-0.5 rounded border border-[#E3E8EE]">
                      {{ (file.size / 1024).toFixed(1) }} KB
                    </span>
                  </li>
                }
              </ul>

              <div class="flex flex-wrap items-center gap-3 pt-2">
                <button 
                  type="button"
                  (click)="processImport(true)"
                  [disabled]="loading()"
                  class="px-3.5 py-1.5 rounded-md border border-[#E3E8EE] bg-white text-[#30313D] hover:bg-slate-50 text-[13px] font-medium transition-colors disabled:opacity-50 shadow-2xs cursor-pointer">
                  {{ loading() ? 'Validando...' : '1. Validar sin guardar (Simulación)' }}
                </button>

                <button 
                  type="button"
                  (click)="processImport(false)"
                  [disabled]="loading()"
                  class="px-3.5 py-1.5 rounded-md bg-[#635BFF] hover:bg-[#5248E0] text-white text-[13px] font-medium transition-colors disabled:opacity-50 shadow-xs cursor-pointer">
                  {{ loading() ? 'Procesando...' : '2. Guardar en Base de Datos' }}
                </button>
              </div>
            </div>
          }

          @if (importNotice()) {
            <div class="border border-[#E3E8EE] bg-[#F8FAFC] text-[#30313D] rounded-md p-3 text-xs" role="status">{{ importNotice() }}</div>
          }
          @if (importError()) {
            <div class="border border-rose-200 bg-rose-50 text-rose-800 rounded-md p-3.5 text-xs" role="alert">
              <p class="font-semibold">No se pudo procesar la carga</p>
              <p class="mt-1">{{ importError() }}</p>
            </div>
          }
          <!-- Resultado de la importación masiva -->
          @if (report()) {
            <div 
              class="border rounded-md p-5 space-y-4 transition-all"
              [ngClass]="report()!.errorCount > 0 ? 'bg-amber-50/50 border-amber-200' : 'bg-emerald-50/40 border-emerald-200'">
              <div class="flex items-center justify-between">
                <h3 class="text-sm font-bold flex items-center gap-2" [ngClass]="report()!.errorCount > 0 ? 'text-amber-900' : 'text-emerald-900'">
                  @if (report()!.errorCount > 0) {
                    <svg class="w-4 h-4 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/>
                    </svg>
                  } @else {
                    <svg class="w-4 h-4 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                  }
                  <span>Resultado: {{ report()!.dryRun ? 'Validación Completada (Simulación, no guardado)' : (report()!.applied ? 'Guardado Exitosamente en BD' : 'Errores encontrados') }}</span>
                </h3>
                <span class="text-xs px-2.5 py-1 rounded bg-white border border-[#E3E8EE] shadow-2xs font-medium">
                  Errores: {{ report()!.errorCount }} | Avisos: {{ report()!.warningCount }}
                </span>
              </div>

              @if (report()!.entities.length > 0) {
                <div class="bg-white rounded-md border border-[#E3E8EE] overflow-hidden shadow-2xs">
                  <table class="w-full text-left text-xs">
                    <thead class="bg-[#F8FAFC] text-[#697386] text-[11px] uppercase border-b border-[#E3E8EE] font-semibold">
                      <tr>
                        <th class="p-2.5">Entidad</th>
                        <th class="p-2.5">Filas Leídas</th>
                        <th class="p-2.5">Creados</th>
                        <th class="p-2.5">Actualizados</th>
                        <th class="p-2.5">Omitidos</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-[#F1F3F5]">
                      @for (ent of report()!.entities; track ent.entity) {
                        <tr class="hover:bg-slate-50/60 transition-colors">
                          <td class="p-2.5 font-semibold text-[#1A2C44]">{{ ent.entity }}</td>
                          <td class="p-2.5 text-[#30313D]">{{ ent.rowsRead }}</td>
                          <td class="p-2.5 text-emerald-600 font-semibold">+{{ ent.created }}</td>
                          <td class="p-2.5 text-blue-600">{{ ent.updated }}</td>
                          <td class="p-2.5 text-[#8792A2]">{{ ent.skipped }}</td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              }

              @if (report()!.issues.length > 0) {
                <div class="space-y-1.5 text-xs">
                  <h4 class="font-semibold text-[#30313D]">Detalles e incidencias reportadas:</h4>
                  <ul class="space-y-1.5 max-h-48 overflow-y-auto">
                    @for (issue of report()!.issues; track $index) {
                      <li class="p-2.5 rounded bg-white border border-[#E3E8EE] text-[#30313D] shadow-2xs">
                        <span class="font-semibold text-[#1A2C44]">[{{ issue.entity }} Fila {{ issue.row }}]:</span>
                        {{ issue.message }}
                      </li>
                    }
                  </ul>
                </div>
              }
            </div>
          }
        </div>
      } @else {
        <p role="alert" class="rounded-md border border-[#E3E8EE] bg-[#F8FAFC] p-4 text-xs text-[#3C4F69]">
          No tienes permiso para importar datos. Solo lectura disponible.
        </p>
      }

      <!-- ============================================================= -->
      <!-- DRAWER LATERAL DE ENTIDAD                                     -->
      <!-- ============================================================= -->
      @if (selectedEntity()) {
        <!-- Contenedor Raíz de Nivel Ventana (Aislado de Stacking Context y Márgenes) -->
        <div class="fixed inset-0 z-[100] overflow-hidden !m-0" role="dialog" aria-modal="true">
          <!-- Fondo oscuro semitransparente -->
          <div 
            class="fixed inset-0 bg-slate-900/40 backdrop-blur-xs transition-opacity duration-300"
            (click)="closeDrawer()">
          </div>

          <!-- Posicionador del Panel Lateral a la Derecha -->
          <div class="fixed inset-y-0 right-0 max-w-full flex pl-6 sm:pl-10 pointer-events-none">
            <!-- Panel Lateral Deslizable -->
            <aside 
              class="pointer-events-auto w-screen max-w-4xl lg:max-w-5xl xl:max-w-6xl bg-white shadow-2xl border-l border-[#E3E8EE] flex flex-col h-full transition-all duration-300 ease-out"
              (click)="$event.stopPropagation()">
          
          <!-- Encabezado del Drawer -->
          <div class="px-6 py-4 border-b border-[#E3E8EE] bg-white flex items-center justify-between shrink-0">
            <div class="flex items-center gap-3">
              <span class="w-8 h-8 rounded-md bg-[#635BFF]/10 text-[#635BFF] font-semibold text-xs flex items-center justify-center border border-[#635BFF]/20">
                {{ selectedEntity()!.orderNumber }}
              </span>
              <div>
                <div class="flex items-center gap-2">
                  <h2 class="text-[17px] font-semibold tracking-tight text-[#1A2C44]">
                    {{ selectedEntity()!.name }}
                  </h2>
                  <span class="text-[11px] font-mono bg-[#F8FAFC] text-[#697386] px-2 py-0.5 rounded border border-[#E3E8EE]">
                    {{ selectedEntity()!.fileName }}
                  </span>
                </div>
                <p class="text-[13px] text-[#697386] mt-0.5 line-clamp-1 leading-normal">
                  {{ selectedEntity()!.desc }}
                </p>
              </div>
            </div>

            <button 
              type="button"
              (click)="closeDrawer()"
              class="w-8 h-8 rounded-md text-[#697386] hover:text-[#1A2C44] hover:bg-[#F8FAFC] border border-transparent hover:border-[#E3E8EE] flex items-center justify-center transition-colors cursor-pointer"
              title="Cerrar panel (Esc)">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <!-- Navegación por Pestañas del Drawer Estilo Stripe -->
          <div class="flex border-b border-[#E3E8EE] bg-white px-6 gap-6 text-[13px] font-medium overflow-x-auto no-scrollbar shrink-0">
            <button
              type="button"
              (click)="switchTab('data')"
              [ngClass]="activeTab() === 'data' 
                ? 'border-[#635BFF] text-[#635BFF] border-b-2 font-semibold pb-3 -mb-px' 
                : 'text-[#697386] hover:text-[#1A2C44] border-b-2 border-transparent pb-3'"
              class="pt-3.5 transition-colors flex items-center gap-2 whitespace-nowrap cursor-pointer">
              <span>Datos Actuales</span>
              <span 
                class="px-2 py-0.5 rounded-full text-[11px] font-semibold transition-colors"
                [ngClass]="activeTab() === 'data' 
                  ? 'bg-[#635BFF]/10 text-[#635BFF]' 
                  : 'bg-[#F1F3F5] text-[#697386]'">
                {{ entityData().length }}
              </span>
            </button>

            <button
              type="button"
              (click)="switchTab('form')"
              [ngClass]="activeTab() === 'form' 
                ? 'border-[#635BFF] text-[#635BFF] border-b-2 font-semibold pb-3 -mb-px' 
                : 'text-[#697386] hover:text-[#1A2C44] border-b-2 border-transparent pb-3'"
              class="pt-3.5 transition-colors flex items-center gap-1.5 whitespace-nowrap cursor-pointer">
              <span>+ Nuevo Registro</span>
            </button>

            <button
              type="button"
              (click)="switchTab('upload')"
              [ngClass]="activeTab() === 'upload' 
                ? 'border-[#635BFF] text-[#635BFF] border-b-2 font-semibold pb-3 -mb-px' 
                : 'text-[#697386] hover:text-[#1A2C44] border-b-2 border-transparent pb-3'"
              class="pt-3.5 transition-colors flex items-center gap-1.5 whitespace-nowrap cursor-pointer">
              <span>Subir Archivo CSV</span>
            </button>

            <button
              type="button"
              (click)="switchTab('format')"
              [ngClass]="activeTab() === 'format' 
                ? 'border-[#635BFF] text-[#635BFF] border-b-2 font-semibold pb-3 -mb-px' 
                : 'text-[#697386] hover:text-[#1A2C44] border-b-2 border-transparent pb-3'"
              class="pt-3.5 transition-colors flex items-center gap-1.5 whitespace-nowrap cursor-pointer">
              <span>Formato y Plantilla</span>
            </button>
          </div>

          <!-- Contenido Scrollable del Drawer -->
          <div class="p-6 overflow-y-auto flex-1 space-y-6">

            <!-- ============================================== -->
            <!-- PESTAÑA 1: DATOS ACTUALES REGISTRADOS          -->
            <!-- ============================================== -->
            @if (activeTab() === 'data') {
              <div class="space-y-4">
                <!-- Banner Contextual si se seleccionó una bodega específica para inventario -->
                @if (selectedEntity()!.key === 'stock' && selectedWarehouseContext()) {
                  <div class="p-3.5 bg-[#F8FAFC] border border-[#E3E8EE] rounded-lg flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-2xs">
                    <div class="flex items-center gap-3">
                      <div class="w-9 h-9 rounded-md bg-[#635BFF]/10 text-[#635BFF] flex items-center justify-center shrink-0 border border-[#635BFF]/20">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
                        </svg>
                      </div>
                      <div>
                        <div class="flex items-center gap-2">
                          <span class="text-xs font-semibold text-[#1A2C44]">Bodega Seleccionada: {{ selectedWarehouseContext()!.name }}</span>
                          <span class="font-mono text-[11px] font-semibold bg-white text-[#1A2C44] px-2 py-0.5 rounded border border-[#E3E8EE]">
                            {{ selectedWarehouseContext()!.code }}
                          </span>
                        </div>
                        <p class="text-[12px] text-[#697386] mt-0.5">
                          Mostrando el inventario de esta bodega. Puedes cargar nuevo stock manual o importar CSV directo.
                        </p>
                      </div>
                    </div>

                    <div class="flex items-center gap-2">
                      <button 
                        type="button" 
                        (click)="switchTab('form')"
                        class="h-8 px-3 text-xs font-medium rounded-md bg-[#635BFF] hover:bg-[#5248E0] text-white shadow-2xs flex items-center gap-1.5 transition-colors cursor-pointer">
                        <span class="font-bold">+</span>
                        <span>Ingresar Stock</span>
                      </button>

                      <button 
                        type="button" 
                        (click)="switchTab('upload')"
                        class="h-8 px-3 text-xs font-medium rounded-md bg-white hover:bg-[#F8FAFC] border border-[#E3E8EE] text-[#30313D] shadow-2xs flex items-center gap-1.5 transition-colors cursor-pointer">
                        <svg class="w-3.5 h-3.5 text-[#697386]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/>
                        </svg>
                        <span>Importar CSV</span>
                      </button>

                      <button 
                        type="button" 
                        (click)="clearWarehouseContext()"
                        class="h-8 px-2.5 text-xs font-medium text-[#697386] hover:text-[#1A2C44] hover:bg-slate-100 rounded-md transition-colors cursor-pointer"
                        title="Ver todas las ubicaciones">
                        ✕ Quitar filtro
                      </button>
                    </div>
                  </div>
                }

                <!-- Banner Contextual si se seleccionó un punto de entrega específico para consumos -->
                @if (selectedEntity()!.key === 'consumptions' && selectedServicePointContext()) {
                  <div class="p-3.5 bg-[#F8FAFC] border border-[#E3E8EE] rounded-lg flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-2xs">
                    <div class="flex items-center gap-3">
                      <div class="w-9 h-9 rounded-md bg-[#635BFF]/10 text-[#635BFF] flex items-center justify-center shrink-0 border border-[#635BFF]/20">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
                        </svg>
                      </div>
                      <div>
                        <div class="flex items-center gap-2">
                          <span class="text-xs font-semibold text-[#1A2C44]">Punto de Entrega: {{ selectedServicePointContext()!.name }}</span>
                          <span class="font-mono text-[11px] font-semibold bg-white text-[#1A2C44] px-2 py-0.5 rounded border border-[#E3E8EE]">
                            {{ selectedServicePointContext()!.code }}
                          </span>
                        </div>
                        <p class="text-[12px] text-[#697386] mt-0.5">
                          Mostrando consumos históricos registrados para este establecimiento de salud.
                        </p>
                      </div>
                    </div>

                    <div class="flex items-center gap-2">
                      <button 
                        type="button" 
                        (click)="switchTab('form')"
                        class="h-8 px-3 text-xs font-medium rounded-md bg-[#635BFF] hover:bg-[#5248E0] text-white shadow-2xs flex items-center gap-1.5 transition-colors cursor-pointer">
                        <span class="font-bold">+</span>
                        <span>Registrar Consumo</span>
                      </button>

                      <button 
                        type="button" 
                        (click)="switchTab('upload')"
                        class="h-8 px-3 text-xs font-medium rounded-md bg-white hover:bg-[#F8FAFC] border border-[#E3E8EE] text-[#30313D] shadow-2xs flex items-center gap-1.5 transition-colors cursor-pointer">
                        <svg class="w-3.5 h-3.5 text-[#697386]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/>
                        </svg>
                        <span>Importar CSV Consumo</span>
                      </button>

                      <button 
                        type="button" 
                        (click)="clearServicePointContext()"
                        class="h-8 px-2.5 text-xs font-medium text-[#697386] hover:text-[#1A2C44] hover:bg-slate-100 rounded-md transition-colors cursor-pointer"
                        title="Ver todos los puntos">
                        ✕ Quitar filtro
                      </button>
                    </div>
                  </div>
                }

                <!-- Barra de herramientas: Búsqueda y Acciones Estilo Stripe -->
                <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                  <div class="relative flex-1">
                    <span class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-[#8792A2]">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
                      </svg>
                    </span>
                    <input 
                      type="text"
                      [ngModel]="dataFilter()"
                      (ngModelChange)="dataFilter.set($event)"
                      placeholder="Buscar por código, nombre o cualquier campo..."
                      class="w-full h-9 pl-9 pr-3 text-[13px] border border-[#E3E8EE] rounded-md focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] bg-white text-[#1A2C44] placeholder-[#8792A2] transition-colors" />
                  </div>

                  <div class="flex items-center gap-2">
                    <button 
                      type="button"
                      (click)="loadEntityData(selectedEntity()!.key)"
                      [disabled]="loadingData()"
                      class="h-9 px-3.5 text-[13px] font-medium rounded-md border border-[#E3E8EE] bg-white text-[#30313D] hover:bg-[#F8FAFC] flex items-center gap-2 transition-colors disabled:opacity-50 shadow-2xs cursor-pointer">
                      <svg class="w-3.5 h-3.5 text-[#697386]" [class.animate-spin]="loadingData()" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
                      </svg>
                      <span>{{ loadingData() ? 'Cargando...' : 'Refrescar' }}</span>
                    </button>

                    <button 
                      type="button"
                      (click)="switchTab('form')"
                      class="h-9 px-4 text-[13px] font-medium rounded-md bg-[#635BFF] text-white hover:bg-[#5248E0] flex items-center gap-2 transition-colors shadow-2xs cursor-pointer">
                      <span class="font-bold text-sm leading-none">+</span>
                      <span>Agregar Manual</span>
                    </button>
                  </div>
                </div>

                <!-- Estado de carga -->
                @if (loadingData()) {
                  <div class="py-16 text-center space-y-3">
                    <div class="inline-block w-6 h-6 border-2 border-[#635BFF] border-t-transparent rounded-full animate-spin"></div>
                    <p class="text-xs text-[#697386]">Consultando registros en base de datos...</p>
                  </div>
                } @else if (filteredEntityData().length === 0) {
                  <!-- Estado vacío refinado Estilo Stripe -->
                  <div class="py-14 px-6 text-center border border-[#E3E8EE] rounded-lg bg-white space-y-4 shadow-2xs">
                    <div class="w-12 h-12 rounded-full bg-[#F1F3F5] text-[#697386] flex items-center justify-center mx-auto">
                      <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.75" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4"/>
                      </svg>
                    </div>
                    <div class="space-y-1">
                      <h3 class="text-[15px] font-semibold text-[#1A2C44] tracking-tight">
                        {{ dataFilter() ? 'No se encontraron coincidencias' : 'No hay datos cargados para este catálogo' }}
                      </h3>
                      <p class="text-[13px] text-[#697386] max-w-sm mx-auto leading-relaxed">
                        {{ dataFilter() ? 'Prueba con otros términos de búsqueda para encontrar los registros deseados.' : 'Puedes agregar registros directamente con el formulario o importar un archivo CSV.' }}
                      </p>
                    </div>
                    @if (!dataFilter()) {
                      <div class="flex items-center justify-center gap-3 pt-2">
                        <button 
                          type="button"
                          (click)="switchTab('form')"
                          class="h-9 px-4 text-[13px] font-medium rounded-md bg-[#635BFF] text-white hover:bg-[#5248E0] transition-colors shadow-2xs flex items-center gap-2 cursor-pointer">
                          <span class="font-bold text-sm leading-none">+</span>
                          <span>Agregar mediante Formulario</span>
                        </button>
                        <button 
                          type="button"
                          (click)="switchTab('upload')"
                          class="h-9 px-4 text-[13px] font-medium rounded-md border border-[#E3E8EE] bg-white text-[#30313D] hover:bg-[#F8FAFC] transition-colors shadow-2xs flex items-center gap-2 cursor-pointer">
                          <svg class="w-4 h-4 text-[#697386]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.75" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/>
                          </svg>
                          <span>Subir archivo CSV</span>
                        </button>
                      </div>
                    }
                  </div>
                } @else {
                  <!-- Tabla de datos registrados Estilo Stripe -->
                  <div class="border border-[#E3E8EE] rounded-lg overflow-hidden shadow-2xs bg-white">
                    <div class="overflow-x-auto max-h-[460px]">
                      <table class="w-full text-left text-xs">
                        <thead class="bg-[#F8FAFC] text-[#697386] text-[11px] uppercase border-b border-[#E3E8EE] sticky top-0 z-10 font-semibold tracking-wider">
                          <tr>
                            <th class="p-3 w-12 text-center text-[#8792A2]">#</th>
                            @for (col of selectedEntity()!.tableCols; track col.key) {
                              <th class="p-3 whitespace-nowrap">{{ col.label }}</th>
                            }
                            @if (selectedEntity()!.key === 'warehouses') {
                              <th class="p-3 whitespace-nowrap text-right">Inventario</th>
                            }
                            @if (selectedEntity()!.key === 'service_points') {
                              <th class="p-3 whitespace-nowrap text-right">Consumo Histórico</th>
                            }
                          </tr>
                        </thead>
                        <tbody class="divide-y divide-[#F1F3F5]">
                          @for (row of filteredEntityData(); track $index) {
                            <tr 
                              (click)="onTableRowClick(row)"
                              [class.cursor-pointer]="selectedEntity()!.key === 'warehouses' || selectedEntity()!.key === 'service_points'"
                              class="hover:bg-slate-50/70 transition-colors group">
                              <td class="p-3 text-center text-[#8792A2] font-mono text-[11px]">
                                {{ $index + 1 }}
                              </td>
                              @for (col of selectedEntity()!.tableCols; track col.key) {
                                <td class="p-3 whitespace-nowrap">
                                  @if (col.badge) {
                                    <span 
                                      class="px-2 py-0.5 rounded text-[11px] font-medium border"
                                      [ngClass]="getBadgeClass(col.key, row[col.key])">
                                      {{ formatValue(row[col.key]) }}
                                    </span>
                                  } @else if (col.key === 'code' || col.key === 'sku' || col.key === 'lot_code' || col.key === 'location_code' || col.key === 'point_code') {
                                    <span class="font-mono font-medium text-[#1A2C44] bg-[#F8FAFC] px-1.5 py-0.5 rounded border border-[#E3E8EE]">
                                      {{ formatValue(row[col.key]) }}
                                    </span>
                                  } @else {
                                    <span class="text-[#30313D]">
                                      {{ formatValue(row[col.key]) }}
                                    </span>
                                  }
                                </td>
                              }
                               @if (selectedEntity()!.key === 'warehouses') {
                                <td class="p-3 whitespace-nowrap text-right">
                                  <button
                                    type="button"
                                    (click)="manageWarehouseStock(row); $event.stopPropagation()"
                                    class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-[#635BFF] hover:bg-[#5248E0] text-white font-medium text-[12px] transition-colors shadow-2xs">
                                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"/>
                                    </svg>
                                    <span>Ver / Cargar Stock</span>
                                  </button>
                                </td>
                              }
                              @if (selectedEntity()!.key === 'service_points') {
                                <td class="p-3 whitespace-nowrap text-right">
                                  <button
                                    type="button"
                                    (click)="manageServicePointConsumption(row); $event.stopPropagation()"
                                    class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-[#635BFF] hover:bg-[#5248E0] text-white font-medium text-[12px] transition-colors shadow-2xs">
                                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/>
                                    </svg>
                                    <span>Ver Consumo</span>
                                  </button>
                                </td>
                              }
                            </tr>
                          }
                        </tbody>
                      </table>
                    </div>

                    <div class="px-4 py-2.5 bg-[#F8FAFC] border-t border-[#E3E8EE] flex items-center justify-between text-[11px] text-[#697386]">
                      <span>Mostrando {{ filteredEntityData().length }} de {{ entityData().length }} registro(s)</span>
                      <span class="font-mono text-[#8792A2]">{{ selectedEntity()!.key }}</span>
                    </div>
                  </div>
                }
              </div>
            }

            <!-- ============================================== -->
            <!-- PESTAÑA 2: FORMULARIO COMÚN + MAPA             -->
            <!-- ============================================== -->
            @if (activeTab() === 'form') {
              <form (ngSubmit)="submitForm()" class="space-y-6">
                <div>
                  <h3 class="text-[15px] font-semibold text-[#1A2C44]">
                    Registrar nuevo registro en {{ selectedEntity()!.name }}
                  </h3>
                  <p class="text-[13px] text-[#3C4F69] mt-0.5">
                    Completa los campos del formulario. Los cambios se guardarán directamente en la base de datos.
                  </p>
                </div>

                <!-- Mensaje de éxito o error -->
                @if (formSuccess()) {
                  <div class="p-3.5 rounded-lg bg-emerald-50 border border-emerald-200 text-[13px] text-emerald-800 flex items-center justify-between">
                    <div class="flex items-center gap-2">
                      <svg class="w-4 h-4 text-emerald-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                      </svg>
                      <span>{{ formSuccess() }}</span>
                    </div>
                    <button 
                      type="button" 
                      (click)="switchTab('data')"
                      class="text-[12px] font-medium text-emerald-800 underline hover:text-emerald-950">
                      Ver en tabla →
                    </button>
                  </div>
                }

                @if (formError()) {
                  <div class="p-3.5 rounded-lg bg-rose-50 border border-rose-200 text-[13px] text-rose-800 flex items-center gap-2">
                    <svg class="w-4 h-4 text-rose-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
                    </svg>
                    <span>{{ formError() }}</span>
                  </div>
                }

                <!-- Campos de formulario dinámicos -->
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  @for (col of selectedEntity()!.columns; track col.name) {
                    <!-- Omitir lat y lon de los campos normales si hay mapa integrado -->
                    @if (!selectedEntity()!.hasLocationMap || (col.name !== 'lat' && col.name !== 'lon')) {
                      <div class="space-y-1" [class.sm:col-span-2]="col.name === 'name' || col.name === 'sku' || col.name === 'lot_code'">
                        <label class="block text-[12px] font-medium text-[#30313D]">
                          {{ getFieldLabel(col.name) }}
                          @if (col.required) {
                            <span class="text-rose-500 font-semibold">*</span>
                          }
                        </label>

                        <!-- UBICACIÓN PARA STOCK: Si hay bodega fija seleccionada -->
                        @if (selectedEntity()!.key === 'stock' && col.name === 'location_code' && selectedWarehouseContext()) {
                          <div class="p-2.5 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE] flex items-center justify-between">
                            <div class="flex items-center gap-2">
                              <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
                              <span class="text-[13px] font-semibold font-mono text-[#1A2C44]">{{ selectedWarehouseContext()!.code }}</span>
                              <span class="text-[13px] text-[#3C4F69]">({{ selectedWarehouseContext()!.name }})</span>
                            </div>
                            <span class="text-[11px] font-medium text-[#30313D] bg-white px-2 py-0.5 rounded border border-[#E3E8EE]">
                              Almacén activo
                            </span>
                          </div>
                        } @else if (selectedEntity()!.key === 'consumptions' && col.name === 'point_code' && selectedServicePointContext()) {
                          <!-- PUNTO DE ENTREGA PARA CONSUMO: Si hay punto fijo seleccionado -->
                          <div class="p-2.5 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE] flex items-center justify-between">
                            <div class="flex items-center gap-2">
                              <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
                              <span class="text-[13px] font-semibold font-mono text-[#1A2C44]">{{ selectedServicePointContext()!.code }}</span>
                              <span class="text-[13px] text-[#3C4F69]">({{ selectedServicePointContext()!.name }})</span>
                            </div>
                            <span class="text-[11px] font-medium text-[#30313D] bg-white px-2 py-0.5 rounded border border-[#E3E8EE]">
                              Destino activo
                            </span>
                          </div>
                        } @else if ((selectedEntity()!.key === 'stock' || selectedEntity()!.key === 'consumptions' || selectedEntity()!.key === 'lots') && col.name === 'sku' && productsCatalog().length > 0) {
                          <!-- PRODUCTO PARA STOCK, CONSUMO O LOTES: Selector enriquecido con SKU y Nombre -->
                          <div class="space-y-2">
                            <select 
                              [ngModel]="formValues()[col.name]"
                              (ngModelChange)="onSkuSelected($event)"
                              [name]="col.name"
                              class="w-full h-9 px-3 text-[13px] border border-[#E3E8EE] rounded-md focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] bg-white text-[#1A2C44]">
                              <option value="">-- Selecciona el Producto o Insumo --</option>
                              @for (p of productsCatalog(); track p.sku) {
                                <option [value]="p.sku">{{ p.sku }} — {{ p.name }} ({{ p.base_unit }})</option>
                              }
                            </select>

                            @if (selectedEntity()!.key === 'lots' && formValues()['product_name']) {
                              <div class="p-2.5 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE] flex items-center justify-between text-[12px]">
                                <div class="flex items-center gap-2">
                                  <span class="w-2 h-2 rounded-full bg-[#635BFF]"></span>
                                  <span class="font-medium text-[#1A2C44]">{{ formValues()['product_name'] }}</span>
                                </div>
                                <span class="font-mono text-[11px] font-medium text-[#3C4F69] bg-white px-2 py-0.5 rounded border border-[#E3E8EE]">
                                  {{ formValues()['base_unit'] || 'UNIDAD' }}
                                </span>
                              </div>
                            }
                          </div>
                        } @else if (selectedEntity()!.key === 'stock' && col.name === 'lot_code') {
                          <!-- LOTE PARA STOCK: Selección de existentes o creación de nuevo -->
                          <div class="space-y-2">
                            @if (availableLotsForSelectedSku().length > 0) {
                              <div class="flex items-center gap-2">
                                <select 
                                  [ngModel]="lotSelectionMode()"
                                  (ngModelChange)="lotSelectionMode.set($event)"
                                  class="text-[12px] px-2.5 py-1 rounded-md border border-[#E3E8EE] bg-[#F8FAFC] text-[#30313D]">
                                  <option value="existing">Elegir Lote Existente ({{ availableLotsForSelectedSku().length }})</option>
                                  <option value="new">+ Escribir Lote Nuevo</option>
                                </select>
                              </div>

                              @if (lotSelectionMode() === 'existing') {
                                <select 
                                  [ngModel]="formValues()['lot_code']"
                                  (ngModelChange)="updateFormField('lot_code', $event)"
                                  name="lot_code"
                                  class="w-full h-9 px-3 text-[13px] border border-[#E3E8EE] rounded-md focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] bg-white font-mono text-[#1A2C44]">
                                  <option value="">-- Seleccionar Lote Existente --</option>
                                  @for (lot of availableLotsForSelectedSku(); track lot.lot_code) {
                                    <option [value]="lot.lot_code">
                                      {{ lot.lot_code }} (Vence: {{ lot.expires_on || 'Sin caducidad' }}) {{ lot.origin ? '• ' + lot.origin : '' }}
                                    </option>
                                  }
                                </select>
                              }
                            }

                            @if (lotSelectionMode() === 'new' || availableLotsForSelectedSku().length === 0) {
                              <input 
                                type="text"
                                [ngModel]="formValues()['lot_code']"
                                (ngModelChange)="updateFormField('lot_code', $event)"
                                name="lot_code"
                                placeholder="Ej: L-DES-2610"
                                class="w-full h-9 px-3 text-[13px] border border-[#E3E8EE] rounded-md focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] bg-white font-mono text-[#1A2C44]" />
                              <p class="text-[11px] text-[#697386]">
                                Si el lote es nuevo, se registrará automáticamente en la base de datos con este código.
                              </p>
                            }
                          </div>
                        } @else if (col.name === 'criticality') {
                          <select 
                            [ngModel]="formValues()[col.name]"
                            (ngModelChange)="updateFormField(col.name, $event)"
                            [name]="col.name"
                            class="w-full h-9 px-3 text-[13px] border border-[#E3E8EE] rounded-md focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] bg-white text-[#1A2C44]">
                            <option value="alta">Alta</option>
                            <option value="media">Media</option>
                            <option value="normal">Normal</option>
                          </select>
                        } @else if (col.name === 'category') {
                          <select 
                            [ngModel]="formValues()[col.name]"
                            (ngModelChange)="updateFormField(col.name, $event)"
                            [name]="col.name"
                            class="w-full h-9 px-3 text-[13px] border border-[#E3E8EE] rounded-md focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] bg-white text-[#1A2C44]">
                            <option value="hospital">Hospital</option>
                            <option value="clinica">Clínica</option>
                            <option value="oficina">Oficina Corporativa</option>
                            <option value="educacion">Unidad Educativa</option>
                            <option value="comercial">Centro Comercial</option>
                          </select>
                        } @else if (col.name === 'location_kind') {
                          <select 
                            [ngModel]="formValues()[col.name]"
                            (ngModelChange)="updateFormField(col.name, $event)"
                            [name]="col.name"
                            class="w-full h-9 px-3 text-[13px] border border-[#E3E8EE] rounded-md focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] bg-white text-[#1A2C44]">
                            <option value="warehouse">Bodega (warehouse)</option>
                            <option value="service_point">Punto de Entrega (service_point)</option>
                          </select>
                        } @else if (col.name === 'period_days') {
                          <select 
                            [ngModel]="formValues()[col.name]"
                            (ngModelChange)="updateFormField(col.name, $event)"
                            [name]="col.name"
                            class="w-full h-9 px-3 text-[13px] border border-[#E3E8EE] rounded-md focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] bg-white text-[#1A2C44]">
                            <option [value]="1">Diario (1 día)</option>
                            <option [value]="7">Semanal (7 días)</option>
                            <option [value]="15">Quincenal (15 días)</option>
                            <option [value]="30">Mensual (30 días)</option>
                          </select>
                        } @else if (col.type.includes('Fecha')) {
                          <input 
                            type="date"
                            [ngModel]="formValues()[col.name]"
                            (ngModelChange)="updateFormField(col.name, $event)"
                            [name]="col.name"
                            class="w-full h-9 px-3 text-[13px] border border-[#E3E8EE] rounded-md focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] bg-white text-[#1A2C44]" />
                        } @else if (col.type.includes('Hora')) {
                          <input 
                            type="time"
                            [ngModel]="formValues()[col.name]"
                            (ngModelChange)="updateFormField(col.name, $event)"
                            [name]="col.name"
                            class="w-full h-9 px-3 text-[13px] border border-[#E3E8EE] rounded-md focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] bg-white text-[#1A2C44]" />
                        } @else {
                          <input 
                            [type]="col.type === 'Decimal' || col.type === 'Entero' ? 'number' : 'text'"
                            [step]="col.type === 'Decimal' ? '0.0001' : '1'"
                            [ngModel]="formValues()[col.name]"
                            (ngModelChange)="updateFormField(col.name, $event)"
                            [name]="col.name"
                            [placeholder]="'Ej: ' + col.example"
                            class="w-full h-9 px-3 text-[13px] border border-[#E3E8EE] rounded-md focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] bg-white text-[#1A2C44] placeholder-[#8792A2]" />
                          @if (selectedEntity()!.key === 'stock' && col.name === 'quantity') {
                            <p class="text-[11px] text-[#3C4F69] bg-[#F8FAFC] p-2 rounded-md border border-[#E3E8EE] mt-1">
                              Saldo disponible físico. El sistema guardará esta cantidad y generará la auditoría en el Kardex.
                            </p>
                          } @else if (selectedEntity()!.key === 'consumptions' && col.name === 'quantity') {
                            <p class="text-[11px] text-[#3C4F69] bg-[#F8FAFC] p-2 rounded-md border border-[#E3E8EE] mt-1">
                              Total de unidades consumidas por el centro de salud en este período.
                            </p>
                          }
                        }

                        <p class="text-[11px] text-[#697386]">
                          {{ col.desc }}
                        </p>
                      </div>
                    }
                  }
                </div>

                <!-- SECCIÓN ESPECIAL: GEORREFERENCIACIÓN CON MAPA INTERACTIVO -->
                @if (selectedEntity()!.hasLocationMap) {
                  <div class="border border-[#E3E8EE] rounded-lg p-4 bg-[#F8FAFC] space-y-3">
                    <div class="flex items-center justify-between">
                      <div>
                        <h4 class="text-[13px] font-semibold text-[#1A2C44] uppercase tracking-wider flex items-center gap-1.5">
                          <svg class="w-3.5 h-3.5 text-[#635BFF]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/>
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/>
                          </svg>
                          <span>Ubicación en el Mapa</span>
                        </h4>
                        <p class="text-[12px] text-[#3C4F69] mt-0.5">
                          Haz clic en cualquier punto del mapa para colocar el marcador y asignar automáticamente las coordenadas.
                        </p>
                      </div>

                      <div class="flex items-center gap-1 text-[11px]">
                        <button 
                          type="button" 
                          (click)="centerMap([-0.1807, -78.4678], 12)" 
                          class="px-2 py-1 bg-white hover:bg-[#F8FAFC] border border-[#E3E8EE] rounded-md text-[#30313D] font-medium transition-colors">
                          Quito
                        </button>
                        <button 
                          type="button" 
                          (click)="centerMap([-2.1894, -79.8891], 12)" 
                          class="px-2 py-1 bg-white hover:bg-[#F8FAFC] border border-[#E3E8EE] rounded-md text-[#30313D] font-medium transition-colors">
                          Guayaquil
                        </button>
                        <button 
                          type="button" 
                          (click)="centerMap([-1.8312, -78.1834], 7)" 
                          class="px-2 py-1 bg-white hover:bg-[#F8FAFC] border border-[#E3E8EE] rounded-md text-[#30313D] font-medium transition-colors">
                          Ecuador
                        </button>
                      </div>
                    </div>

                    <!-- Inputs de Coordenadas Manuales o Autocompletadas -->
                    <div class="grid grid-cols-2 gap-3 pt-1">
                      <div>
                        <label class="block text-[11px] font-medium text-[#30313D] mb-1">
                          Latitud (WGS84) <span class="text-rose-500 font-bold">*</span>
                        </label>
                        <input 
                          type="number"
                          step="0.000001"
                          [ngModel]="formValues()['lat']"
                          (ngModelChange)="updateCoordinates('lat', $event)"
                          name="lat"
                          placeholder="Ej: -0.2295"
                          class="w-full h-9 px-3 text-[13px] border border-[#E3E8EE] rounded-md focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] bg-white font-mono text-[#1A2C44]" />
                      </div>

                      <div>
                        <label class="block text-[11px] font-medium text-[#30313D] mb-1">
                          Longitud (WGS84) <span class="text-rose-500 font-bold">*</span>
                        </label>
                        <input 
                          type="number"
                          step="0.000001"
                          [ngModel]="formValues()['lon']"
                          (ngModelChange)="updateCoordinates('lon', $event)"
                          name="lon"
                          placeholder="Ej: -78.5243"
                          class="w-full h-9 px-3 text-[13px] border border-[#E3E8EE] rounded-md focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] bg-white font-mono text-[#1A2C44]" />
                      </div>
                    </div>

                    <!-- Componente MapView interactivo -->
                    <div class="h-64 rounded-lg overflow-hidden border border-[#E3E8EE] shadow-2xs relative">
                      <ui-map-view
                        #formMapView
                        [center]="mapCenter()"
                        [zoom]="mapZoom()"
                        [markers]="mapMarkers()"
                        [showLegend]="false"
                        (mapClick)="onMapClick($event)">
                      </ui-map-view>
                      
                      @if (mapMarkers().length > 0) {
                        <div class="absolute top-2 right-2 z-[400] bg-white/95 backdrop-blur px-2.5 py-1 rounded-md border border-[#E3E8EE] shadow-xs text-[11px] font-mono text-[#30313D]">
                          Pin: {{ formValues()['lat'] }}, {{ formValues()['lon'] }}
                        </div>
                      }
                    </div>
                  </div>
                }

                <div class="flex items-center justify-end gap-3 pt-3 border-t border-[#E3E8EE]">
                  <button 
                    type="button"
                    (click)="resetForm()"
                    class="h-9 px-4 text-[13px] font-medium rounded-md border border-[#E3E8EE] bg-white text-[#30313D] hover:bg-[#F8FAFC] transition-colors shadow-2xs cursor-pointer">
                    Limpiar formulario
                  </button>

                  <button 
                    type="submit"
                    [disabled]="formLoading()"
                    class="h-9 px-4 text-[13px] font-medium rounded-md bg-[#635BFF] text-white hover:bg-[#5248E0] transition-colors shadow-2xs flex items-center gap-2 disabled:opacity-50 cursor-pointer">
                    @if (formLoading()) {
                      <svg class="w-3.5 h-3.5 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
                      </svg>
                      <span>Guardando en BD...</span>
                    } @else {
                      <span>Guardar Registro</span>
                    }
                  </button>
                </div>
              </form>
            }

            <!-- ============================================== -->
            <!-- PESTAÑA 3: OPCIÓN PARA SUBIR EL CSV            -->
            <!-- ============================================== -->
            @if (activeTab() === 'upload') {
              <div class="space-y-6">
                <!-- Banner para importación directa a bodega activa -->
                @if (selectedEntity()!.key === 'stock' && selectedWarehouseContext()) {
                  <div class="p-4 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE] text-[13px] text-[#1A2C44] space-y-2">
                    <div class="flex items-center justify-between">
                      <div class="flex items-center gap-2">
                        <span class="w-2.5 h-2.5 rounded-full bg-emerald-500"></span>
                        <span class="font-semibold">Importación directa a: {{ selectedWarehouseContext()!.name }}</span>
                        <span class="font-mono text-[11px] font-medium bg-white text-[#30313D] border border-[#E3E8EE] px-1.5 py-0.5 rounded">{{ selectedWarehouseContext()!.code }}</span>
                      </div>
                      <button 
                        type="button"
                        (click)="downloadTemplateForWarehouse(selectedWarehouseContext()!)"
                        class="text-[12px] font-medium text-[#30313D] bg-white hover:bg-[#F8FAFC] px-2.5 py-1 rounded-md border border-[#E3E8EE] transition-colors shadow-2xs">
                        Descargar Plantilla {{ selectedWarehouseContext()!.code }}
                      </button>
                    </div>
                    <p class="text-[12px] text-[#3C4F69]">
                      Puedes subir un CSV estándar o simplificado (con columnas <code class="font-mono bg-white px-1 py-0.5 rounded border border-[#E3E8EE]">sku,lot_code,quantity</code>). Los registros se asociarán automáticamente a este almacén.
                    </p>
                  </div>
                }

                <!-- Banner para importación directa a punto de entrega activo -->
                @if (selectedEntity()!.key === 'consumptions' && selectedServicePointContext()) {
                  <div class="p-4 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE] text-[13px] text-[#1A2C44] space-y-2">
                    <div class="flex items-center justify-between">
                      <div class="flex items-center gap-2">
                        <span class="w-2.5 h-2.5 rounded-full bg-emerald-500"></span>
                        <span class="font-semibold">Importación directa a: {{ selectedServicePointContext()!.name }}</span>
                        <span class="font-mono text-[11px] font-medium bg-white text-[#30313D] border border-[#E3E8EE] px-1.5 py-0.5 rounded">{{ selectedServicePointContext()!.code }}</span>
                      </div>
                      <button 
                        type="button"
                        (click)="downloadTemplateForServicePoint(selectedServicePointContext()!)"
                        class="text-[12px] font-medium text-[#30313D] bg-white hover:bg-[#F8FAFC] px-2.5 py-1 rounded-md border border-[#E3E8EE] transition-colors shadow-2xs">
                        Descargar Plantilla {{ selectedServicePointContext()!.code }}
                      </button>
                    </div>
                    <p class="text-[12px] text-[#3C4F69]">
                      Puedes subir un CSV estándar o simplificado (con columnas <code class="font-mono bg-white px-1 py-0.5 rounded border border-[#E3E8EE]">sku,date,quantity,period_days</code>). Los registros se asignarán automáticamente a este centro de salud.
                    </p>
                  </div>
                }

                <!-- Área de selección de archivo -->
                <div class="border-2 border-dashed border-[#E3E8EE] hover:border-[#635BFF] rounded-lg p-8 text-center bg-[#F8FAFC]/50 hover:bg-white transition-all space-y-4">
                  <div class="w-12 h-12 rounded-full bg-[#F1F3F5] text-[#697386] flex items-center justify-center mx-auto">
                    <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.75" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"/>
                    </svg>
                  </div>

                  <div class="space-y-1">
                    <h4 class="text-[14px] font-semibold text-[#1A2C44]">
                      Cargar archivo para {{ selectedEntity()!.name }}
                    </h4>
                    <p class="text-[12px] text-[#697386]">
                      Formatos soportados: <span class="font-mono font-medium text-[#1A2C44]">.csv</span> o <span class="font-mono font-medium text-[#1A2C44]">.xlsx</span>
                    </p>
                  </div>

                  <div class="pt-2">
                    <input 
                      type="file" 
                      id="drawerFileInput"
                      (change)="onDrawerFileSelected($event)"
                      accept=".csv,.xlsx"
                      class="hidden" />
                    <label 
                      for="drawerFileInput"
                      class="inline-flex items-center gap-2 px-4 h-9 rounded-md bg-[#635BFF] hover:bg-[#5248E0] text-white text-[13px] font-medium cursor-pointer transition-colors shadow-2xs">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/>
                      </svg>
                      <span>Seleccionar archivo local</span>
                    </label>
                  </div>

                  <p class="text-[11px] text-[#697386]">
                    Archivo sugerido: <code class="font-mono bg-white px-1.5 py-0.5 rounded border border-[#E3E8EE] text-[#30313D]">{{ selectedEntity()!.fileName }}</code>
                  </p>
                </div>

                <!-- Archivo seleccionado -->
                @if (drawerSelectedFile()) {
                  <div class="p-4 bg-white border border-[#E3E8EE] rounded-lg space-y-3 shadow-2xs">
                    <div class="flex items-center justify-between">
                      <div class="flex items-center gap-3">
                        <div class="w-9 h-9 rounded-md bg-[#F1F3F5] text-[#697386] flex items-center justify-center">
                          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                          </svg>
                        </div>
                        <div>
                          <div class="text-[13px] font-semibold text-[#1A2C44]">{{ drawerSelectedFile()!.name }}</div>
                          <div class="text-[11px] font-mono text-[#8792A2]">
                            {{ (drawerSelectedFile()!.size / 1024).toFixed(1) }} KB | {{ drawerSelectedFile()!.type || 'CSV' }}
                          </div>
                        </div>
                      </div>

                      <button 
                        type="button"
                        (click)="drawerSelectedFile.set(null); drawerReport.set(null)"
                        class="text-[12px] text-rose-600 hover:text-rose-800 font-medium cursor-pointer">
                        Quitar
                      </button>
                    </div>

                    <div class="flex items-center gap-3 pt-2 border-t border-[#E3E8EE]">
                      <button 
                        type="button"
                        (click)="processDrawerImport(true)"
                        [disabled]="drawerLoading()"
                        class="h-9 px-4 rounded-md bg-white border border-[#E3E8EE] hover:bg-[#F8FAFC] text-[#30313D] text-[13px] font-medium transition-colors disabled:opacity-50 shadow-2xs cursor-pointer">
                        {{ drawerLoading() ? 'Validando...' : '1. Simulación (Validar sin guardar)' }}
                      </button>

                      <button 
                        type="button"
                        (click)="processDrawerImport(false)"
                        [disabled]="drawerLoading()"
                        class="h-9 px-4 rounded-md bg-[#635BFF] hover:bg-[#5248E0] text-white text-[13px] font-medium transition-colors disabled:opacity-50 shadow-2xs cursor-pointer">
                        {{ drawerLoading() ? 'Procesando...' : '2. Guardar en Base de Datos' }}
                      </button>
                    </div>
                  </div>
                }

                <!-- Reporte de la importación individual -->
                @if (drawerReport()) {
                  <div 
                    class="border rounded-lg p-5 space-y-4"
                    [ngClass]="drawerReport()!.errorCount > 0 ? 'bg-amber-50/60 border-amber-200' : 'bg-emerald-50/60 border-emerald-200'">
                    
                    <div class="flex items-center justify-between">
                      <h4 class="text-[13px] font-semibold flex items-center gap-2" [ngClass]="drawerReport()!.errorCount > 0 ? 'text-amber-900' : 'text-emerald-900'">
                        @if (drawerReport()!.errorCount > 0) {
                          <svg class="w-4 h-4 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/>
                          </svg>
                        } @else {
                          <svg class="w-4 h-4 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                          </svg>
                        }
                        <span>Resultado: {{ drawerReport()!.dryRun ? 'Validación Completada (Simulación)' : (drawerReport()!.applied ? 'Guardado Exitosamente' : 'Errores en datos') }}</span>
                      </h4>
                      
                      <div class="flex items-center gap-2">
                        <span class="text-[11px] font-mono px-2 py-0.5 rounded bg-white border border-[#E3E8EE] shadow-2xs text-[#30313D]">
                          Errores: {{ drawerReport()!.errorCount }} | Avisos: {{ drawerReport()!.warningCount }}
                        </span>
                        @if (drawerReport()!.applied) {
                          <button 
                            type="button"
                            (click)="switchTab('data')"
                            class="text-[12px] font-medium text-emerald-800 underline hover:text-emerald-950">
                            Ver datos actualizados →
                          </button>
                        }
                      </div>
                    </div>

                    @if (drawerReport()!.entities.length > 0) {
                      <div class="bg-white rounded-lg border border-[#E3E8EE] overflow-hidden shadow-2xs">
                        <table class="w-full text-left text-xs">
                          <thead class="bg-[#F8FAFC] text-[#697386] text-[11px] uppercase border-b border-[#E3E8EE] font-semibold">
                            <tr>
                              <th class="p-2.5">Entidad</th>
                              <th class="p-2.5">Filas</th>
                              <th class="p-2.5">Creados</th>
                              <th class="p-2.5">Actualizados</th>
                              <th class="p-2.5">Omitidos</th>
                            </tr>
                          </thead>
                          <tbody class="divide-y divide-[#F1F3F5]">
                            @for (ent of drawerReport()!.entities; track ent.entity) {
                              <tr class="hover:bg-[#F8FAFC]/50">
                                <td class="p-2.5 font-medium text-[#1A2C44]">{{ ent.entity }}</td>
                                <td class="p-2.5 font-mono text-[#30313D]">{{ ent.rowsRead }}</td>
                                <td class="p-2.5 font-mono text-emerald-600 font-semibold">+{{ ent.created }}</td>
                                <td class="p-2.5 font-mono text-[#635BFF]">{{ ent.updated }}</td>
                                <td class="p-2.5 font-mono text-[#8792A2]">{{ ent.skipped }}</td>
                              </tr>
                            }
                          </tbody>
                        </table>
                      </div>
                    }

                    @if (drawerReport()!.issues.length > 0) {
                      <div class="space-y-1.5 text-xs">
                        <h5 class="font-semibold text-[#1A2C44]">Detalles de incidencias:</h5>
                        <ul class="space-y-1 max-h-36 overflow-y-auto">
                          @for (issue of drawerReport()!.issues; track $index) {
                            <li class="p-2 rounded-md bg-white border border-[#E3E8EE] text-[#30313D]">
                              <span class="font-semibold text-[#1A2C44]">[{{ issue.entity }} Fila {{ issue.row }}]:</span>
                              {{ issue.message }}
                            </li>
                          }
                        </ul>
                      </div>
                    }
                  </div>
                }
              </div>
            }

            <!-- ============================================== -->
            <!-- PESTAÑA 4: FORMATO CSV Y PLANTILLA             -->
            <!-- ============================================== -->
            @if (activeTab() === 'format') {
              <div class="space-y-6">
                <!-- Botones de Acción de Plantilla -->
                <div class="flex flex-wrap items-center justify-between gap-3 p-4 bg-[#F8FAFC] border border-[#E3E8EE] rounded-lg">
                  <div>
                    <h4 class="text-[13px] font-semibold text-[#1A2C44]">
                      Plantilla Oficial: {{ selectedEntity()!.fileName }}
                    </h4>
                    <p class="text-[12px] text-[#3C4F69] mt-0.5">
                      Descarga el archivo o copia los datos de ejemplo para editar en Excel o cualquier hoja de cálculo.
                    </p>
                  </div>

                  <div class="flex items-center gap-2">
                    <button 
                      type="button"
                      (click)="copyTemplate(selectedEntity()!)"
                      class="h-9 px-3.5 text-[13px] font-medium rounded-md bg-white border border-[#E3E8EE] text-[#30313D] hover:bg-[#F8FAFC] transition-colors shadow-2xs flex items-center gap-2 cursor-pointer">
                      @if (copySuccess()) {
                        <svg class="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                        </svg>
                        <span class="text-emerald-700">Copiado</span>
                      } @else {
                        <svg class="w-3.5 h-3.5 text-[#8792A2]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"/>
                        </svg>
                        <span>Copiar Ejemplo</span>
                      }
                    </button>

                    <button 
                      type="button"
                      (click)="downloadTemplate(selectedEntity()!)"
                      class="h-9 px-4 text-[13px] font-medium rounded-md bg-[#635BFF] text-white hover:bg-[#5248E0] transition-colors shadow-2xs flex items-center gap-2 cursor-pointer">
                      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
                      </svg>
                      <span>Descargar Plantilla CSV</span>
                    </button>
                  </div>
                </div>

                <!-- Tabla de Especificación de Columnas -->
                <div class="space-y-2">
                  <h4 class="text-[13px] font-semibold text-[#1A2C44] uppercase tracking-wider">
                    Estructura y Columnas Requeridas
                  </h4>
                  
                  <div class="border border-[#E3E8EE] rounded-lg overflow-hidden bg-white shadow-2xs">
                    <table class="w-full text-left text-xs">
                      <thead class="bg-[#F8FAFC] text-[#697386] text-[11px] uppercase border-b border-[#E3E8EE] font-semibold">
                        <tr>
                          <th class="p-3">Columna</th>
                          <th class="p-3">Requerido</th>
                          <th class="p-3">Tipo</th>
                          <th class="p-3">Descripción</th>
                          <th class="p-3">Ejemplo</th>
                        </tr>
                      </thead>
                      <tbody class="divide-y divide-[#F1F3F5]">
                        @for (col of selectedEntity()!.columns; track col.name) {
                          <tr class="hover:bg-[#F8FAFC]/50">
                            <td class="p-3 font-mono font-medium text-[#1A2C44]">
                              {{ col.name }}
                            </td>
                            <td class="p-3">
                              @if (col.required) {
                                <span class="px-2 py-0.5 rounded text-[10px] font-medium bg-rose-50 text-rose-700 border border-rose-200">
                                  Obligatorio
                                </span>
                              } @else {
                                <span class="px-2 py-0.5 rounded text-[10px] font-medium bg-[#F8FAFC] text-[#697386] border border-[#E3E8EE]">
                                  Opcional
                                </span>
                              }
                            </td>
                            <td class="p-3 text-[#697386] font-mono text-[11px]">
                              {{ col.type }}
                            </td>
                            <td class="p-3 text-[#30313D]">
                              {{ col.desc }}
                            </td>
                            <td class="p-3 font-mono text-[11px] text-[#8792A2]">
                              {{ col.example }}
                            </td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>
                </div>

                <!-- Previsualización del CSV -->
                <div class="space-y-2">
                  <div class="flex items-center justify-between">
                    <h4 class="text-[13px] font-semibold text-[#1A2C44] uppercase tracking-wider">
                      Vista Previa del Archivo CSV
                    </h4>
                    <span class="text-[11px] text-[#8792A2]">Delimitador: coma (,)</span>
                  </div>

                  <pre class="p-4 bg-[#1A2C44] text-[#E3E8EE] text-[12px] font-mono rounded-lg overflow-x-auto leading-relaxed border border-[#E3E8EE]/20">{{ selectedEntity()!.csvTemplate }}</pre>
                </div>
              </div>
            }
          </div>

          <!-- Pie del Drawer -->
          <div class="px-6 py-3.5 border-t border-[#E3E8EE] bg-white flex items-center justify-between shrink-0">
            <span class="text-[12px] text-[#697386] flex items-center gap-2">
              <span>Presiona</span>
              <kbd class="px-1.5 py-0.5 bg-[#F8FAFC] border border-[#E3E8EE] rounded text-[11px] text-[#4F566B] font-mono shadow-2xs">Esc</kbd>
              <span>para cerrar</span>
            </span>

            <button 
              type="button"
              (click)="closeDrawer()"
              class="h-9 px-4 text-[13px] font-medium rounded-md border border-[#E3E8EE] bg-white text-[#30313D] hover:bg-[#F8FAFC] transition-colors shadow-2xs cursor-pointer">
              Cerrar
            </button>
          </div>
        </aside>
      </div>
    </div>
  }
</div>
  `
})
export class DatosPage {
  @ViewChild('formMapView') formMapView?: MapViewComponent;

  private readonly apiService = inject(ApiService);
  readonly session = inject(AuthSession);

  readonly showTipBanner = signal(true);
  readonly selectedFiles = signal<File[]>([]);
  readonly loading = signal(false);
  readonly report = signal<ImportReportDto | null>(null);
  readonly importError = signal<string | null>(null);
  readonly importNotice = signal<string | null>(null);

  // Estado del Drawer Lateral
  readonly selectedEntity = signal<EntityDetailConfig | null>(null);
  readonly activeTab = signal<'data' | 'form' | 'upload' | 'format'>('data');
  readonly entityData = signal<any[]>([]);
  readonly loadingData = signal(false);
  readonly dataFilter = signal('');
  readonly drawerSelectedFile = signal<File | null>(null);
  readonly drawerLoading = signal(false);
  readonly drawerReport = signal<ImportReportDto | null>(null);
  readonly copySuccess = signal(false);

  // Estado del Formulario
  readonly formValues = signal<Record<string, any>>({});
  readonly formLoading = signal(false);
  readonly formSuccess = signal<string | null>(null);
  readonly formError = signal<string | null>(null);

  // Estado del Mapa en el Formulario
  readonly mapCenter = signal<[number, number]>([-0.1807, -78.4678]);
  readonly mapZoom = signal<number>(12);
  readonly mapMarkers = signal<MapMarker[]>([]);

  // Contexto de Bodega Específica para Gestión de Inventario
  readonly selectedWarehouseContext = signal<{ code: string; name: string } | null>(null);
  // Contexto de Punto de Entrega para Gestión de Consumos Históricos
  readonly selectedServicePointContext = signal<{ code: string; name: string } | null>(null);
  readonly productsCatalog = signal<any[]>([]);
  readonly lotsCatalog = signal<any[]>([]);
  readonly lotSelectionMode = signal<'existing' | 'new'>('existing');

  readonly availableLotsForSelectedSku = computed(() => {
    const currentSku = this.formValues()['sku'];
    if (!currentSku) return [];
    return this.lotsCatalog().filter(l => l.sku === currentSku);
  });

  readonly filteredEntityData = computed(() => {
    const data = this.entityData();
    const filter = this.dataFilter().trim().toLowerCase();
    if (!filter) return data;
    return data.filter(item => {
      return Object.values(item).some(val => 
        val !== null && val !== undefined && String(val).toLowerCase().includes(filter)
      );
    });
  });

  readonly entityList: EntityDetailConfig[] = [
    {
      key: 'warehouses',
      orderNumber: '01',
      name: 'Bodegas',
      fileName: 'warehouses.csv',
      desc: 'Puntos de partida y origen de los despachos logísticos. Haz clic en una bodega para ver y gestionar su inventario directo.',
      fields: 'code, name, lat, lon',
      hasLocationMap: true,
      columns: [
        { name: 'code', type: 'Texto', required: true, desc: 'Identificador único de la bodega o centro logístico', example: 'BOD-UIO' },
        { name: 'name', type: 'Texto', required: true, desc: 'Nombre descriptivo de la instalación', example: 'Bodega Central Quito' },
        { name: 'lat', type: 'Decimal', required: true, desc: 'Latitud geográfica en grados decimales (WGS84)', example: '-0.2295' },
        { name: 'lon', type: 'Decimal', required: true, desc: 'Longitud geográfica en grados decimales (WGS84)', example: '-78.5243' }
      ],
      csvTemplate: `code,name,lat,lon\n`,
      tableCols: [
        { key: 'code', label: 'Código' },
        { key: 'name', label: 'Nombre de la Bodega' },
        { key: 'lat', label: 'Latitud' },
        { key: 'lon', label: 'Longitud' },
        { key: 'isActive', label: 'Estado', badge: true }
      ]
    },
    {
      key: 'service_points',
      orderNumber: '02',
      name: 'Puntos de entrega',
      fileName: 'service_points.csv',
      desc: 'Puntos de entrega de destino: hospitales, clínicas y dependencias. Haz clic en un establecimiento para ver o registrar sus consumos históricos.',
      fields: 'code, name, lat, lon, region, category, criticality, window_start, window_end, service_minutes, lead_time_days',
      hasLocationMap: true,
      columns: [
        { name: 'code', type: 'Texto', required: true, desc: 'Código único del establecimiento receptor', example: 'HOS-001' },
        { name: 'name', type: 'Texto', required: true, desc: 'Razón social o nombre público', example: 'Hospital Norte' },
        { name: 'category', type: 'Texto', required: false, desc: 'Tipo (hospital, clinica, oficina, etc.)', example: 'hospital' },
        { name: 'region', type: 'Texto', required: false, desc: 'Provincia o zona operativa', example: 'Pichincha' },
        { name: 'criticality', type: 'Texto', required: false, desc: 'Nivel de prioridad (alta, media, normal)', example: 'alta' },
        { name: 'lat', type: 'Decimal', required: false, desc: 'Latitud geográfica para ruteo vehicular', example: '-0.1700' },
        { name: 'lon', type: 'Decimal', required: false, desc: 'Longitud geográfica para ruteo vehicular', example: '-78.4800' },
        { name: 'window_start', type: 'Hora (HH:mm)', required: false, desc: 'Hora apertura recepción de carga', example: '07:00' },
        { name: 'window_end', type: 'Hora (HH:mm)', required: false, desc: 'Hora límite recepción de carga', example: '15:00' },
        { name: 'service_minutes', type: 'Entero', required: false, desc: 'Minutos estimados de atención / descarga', example: '20' },
        { name: 'lead_time_days', type: 'Decimal', required: false, desc: 'Días de anticipación requeridos', example: '2' }
      ],
      csvTemplate: `code,name,lat,lon,region,category,criticality,window_start,window_end,service_minutes,lead_time_days\n`,
      tableCols: [
        { key: 'code', label: 'Código' },
        { key: 'name', label: 'Establecimiento' },
        { key: 'category', label: 'Categoría', badge: true },
        { key: 'region', label: 'Región' },
        { key: 'criticality', label: 'Criticidad', badge: true },
        { key: 'lat', label: 'Latitud' },
        { key: 'lon', label: 'Longitud' },
        { key: 'window_start', label: 'Apertura' },
        { key: 'window_end', label: 'Cierre' }
      ]
    },
    {
      key: 'products',
      orderNumber: '03',
      name: 'Productos e Insumos',
      fileName: 'products.csv',
      desc: 'Catálogo de insumos médicos, fármacos, productos de limpieza y suministros hospitalarios.',
      fields: 'sku, name, base_unit, weight_per_unit, volume_per_unit',
      hasLocationMap: false,
      columns: [
        { name: 'sku', type: 'Texto', required: true, desc: 'Código SKU o identificador del producto', example: 'DES-01' },
        { name: 'name', type: 'Texto', required: true, desc: 'Descripción del producto o insumo', example: 'Desinfectante hospitalario' },
        { name: 'base_unit', type: 'Texto', required: true, desc: 'Unidad de medida base (L, UN, caja, kg)', example: 'L' },
        { name: 'weight_per_unit', type: 'Decimal', required: false, desc: 'Peso en kilogramos por unidad de medida', example: '1.05' },
        { name: 'volume_per_unit', type: 'Decimal', required: false, desc: 'Volumen en m³ por unidad de medida', example: '0.001' }
      ],
      csvTemplate: `sku,name,base_unit,weight_per_unit,volume_per_unit\n`,
      tableCols: [
        { key: 'sku', label: 'SKU' },
        { key: 'name', label: 'Descripción del Producto' },
        { key: 'base_unit', label: 'Unidad Base' },
        { key: 'weight_per_unit', label: 'Peso Unit. (kg)' },
        { key: 'volume_per_unit', label: 'Volumen Unit. (m³)' },
        { key: 'isActive', label: 'Activo', badge: true }
      ]
    },
    {
      key: 'vehicles',
      orderNumber: '04',
      name: 'Vehículos y Flota',
      fileName: 'vehicles.csv',
      desc: 'Flota de transporte logístico, bodegas base y límites de carga vehicular (peso y volumen).',
      fields: 'code, depot_code, max_weight, max_volume, cost_per_km',
      hasLocationMap: false,
      columns: [
        { name: 'code', type: 'Texto', required: true, desc: 'Código identificador o placa de la unidad', example: 'VEH-01' },
        { name: 'depot_code', type: 'Texto', required: true, desc: 'Código de la bodega base donde pernocta', example: 'BOD-UIO' },
        { name: 'max_weight', type: 'Decimal', required: true, desc: 'Capacidad máxima de carga útil en kg', example: '1200' },
        { name: 'max_volume', type: 'Decimal', required: false, desc: 'Capacidad volumétrica máxima en m³', example: '8' },
        { name: 'cost_per_km', type: 'Decimal', required: false, desc: 'Costo operacional por km en USD', example: '0.45' }
      ],
      csvTemplate: `code,depot_code,max_weight,max_volume,cost_per_km\n`,
      tableCols: [
        { key: 'code', label: 'Código Unidad' },
        { key: 'depot_code', label: 'Bodega Base' },
        { key: 'max_weight', label: 'Cap. Peso (kg)' },
        { key: 'max_volume', label: 'Cap. Vol. (m³)' },
        { key: 'cost_per_km', label: 'Costo / Km ($)' },
        { key: 'isActive', label: 'Operativo', badge: true }
      ]
    },
    {
      key: 'lots',
      orderNumber: '05',
      name: 'Lotes y Caducidades',
      fileName: 'lots.csv',
      desc: 'Trazabilidad de fabricación, fechas de caducidad, proveedor y datos del producto conforme.',
      fields: 'sku, lot_code, product_name, base_unit, expires_on, origin',
      hasLocationMap: false,
      columns: [
        { name: 'sku', type: 'Texto', required: true, desc: 'Código SKU del producto al que pertenece el lote', example: 'DES-01' },
        { name: 'product_name', type: 'Texto', required: false, desc: 'Nombre descriptivo del producto conforme', example: 'Desinfectante hospitalario' },
        { name: 'base_unit', type: 'Texto', required: false, desc: 'Unidad de medida base del producto (L, UN, caja, kg)', example: 'L' },
        { name: 'lot_code', type: 'Texto', required: true, desc: 'Código de lote impreso en el empaque', example: 'L-DES-2610' },
        { name: 'expires_on', type: 'Fecha (YYYY-MM-DD)', required: false, desc: 'Fecha de caducidad o vencimiento', example: '2027-04-30' },
        { name: 'origin', type: 'Texto', required: false, desc: 'Proveedor, laboratorio o fabricante', example: 'Proveedor sintético A' }
      ],
      csvTemplate: `sku,lot_code,product_name,base_unit,expires_on,origin\n`,
      tableCols: [
        { key: 'lot_code', label: 'Código de Lote' },
        { key: 'sku', label: 'SKU' },
        { key: 'product_name', label: 'Producto Conforme' },
        { key: 'base_unit', label: 'Unidad' },
        { key: 'expires_on', label: 'Fecha Vencimiento' },
        { key: 'origin', label: 'Origen / Proveedor' },
        { key: 'isBlocked', label: 'Bloqueado', badge: true }
      ]
    },
    {
      key: 'stock',
      orderNumber: '06',
      name: 'Inventario y Existencias',
      fileName: 'stock.csv',
      desc: 'Cantidad física disponible por ubicación (bodega o hospital) y lote.',
      fields: 'location_code, sku, lot_code, quantity',
      hasLocationMap: false,
      columns: [
        { name: 'location_code', type: 'Texto', required: true, desc: 'Código de bodega o punto donde reside el stock', example: 'BOD-UIO' },
        { name: 'location_kind', type: 'Texto', required: false, desc: 'Tipo (warehouse o service_point). Se auto-detecta', example: 'warehouse' },
        { name: 'sku', type: 'Texto', required: true, desc: 'Código SKU del producto en stock', example: 'DES-01' },
        { name: 'lot_code', type: 'Texto', required: false, desc: 'Código de lote específico (vacío para genérico)', example: 'L-DES-2610' },
        { name: 'quantity', type: 'Decimal', required: true, desc: 'Saldo o existencias disponibles físicamente', example: '800' }
      ],
      csvTemplate: `location_kind,location_code,sku,lot_code,quantity\n`,
      tableCols: [
        { key: 'location_code', label: 'Ubicación' },
        { key: 'location_kind', label: 'Tipo Ubicación', badge: true },
        { key: 'sku', label: 'SKU' },
        { key: 'lot_code', label: 'Lote' },
        { key: 'quantity', label: 'Disponible' },
        { key: 'reserved', label: 'Reservado' }
      ]
    },
    {
      key: 'consumptions',
      orderNumber: '07',
      name: 'Consumos Históricos',
      fileName: 'consumptions.csv',
      desc: 'Historial de uso y demanda hospitalaria para proyectar riesgos de quiebre.',
      fields: 'point_code, sku, date, quantity, period_days',
      hasLocationMap: false,
      columns: [
        { name: 'point_code', type: 'Texto', required: true, desc: 'Código del punto de servicio que consumió el insumo', example: 'HOS-001' },
        { name: 'sku', type: 'Texto', required: true, desc: 'Código SKU del insumo consumido', example: 'DES-01' },
        { name: 'date', type: 'Fecha (YYYY-MM-DD)', required: true, desc: 'Fecha en la que se registró el consumo', example: '2026-10-05' },
        { name: 'quantity', type: 'Decimal', required: true, desc: 'Cantidad de unidades consumidas en el período', example: '84' },
        { name: 'period_days', type: 'Entero', required: false, desc: 'Días acumulados (1=diario, 7=semanal, 30=mensual)', example: '7' }
      ],
      csvTemplate: `point_code,sku,date,quantity,period_days\n`,
      tableCols: [
        { key: 'point_code', label: 'Punto Hospitalario' },
        { key: 'sku', label: 'SKU' },
        { key: 'date', label: 'Fecha' },
        { key: 'quantity', label: 'Cantidad' },
        { key: 'period_days', label: 'Días Período' }
      ]
    }
  ];

  readonly visibleEntityCards = computed(() => 
    this.entityList.filter(e => e.key !== 'stock' && e.key !== 'consumptions')
  );

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.selectedEntity()) {
      this.closeDrawer();
    }
  }

  openDrawer(item: EntityDetailConfig): void {
    if (item.key !== 'stock') {
      this.selectedWarehouseContext.set(null);
    }
    if (item.key !== 'consumptions') {
      this.selectedServicePointContext.set(null);
    }
    if (item.key === 'stock' || item.key === 'consumptions' || item.key === 'lots') {
      this.loadCatalogsForStock();
    }
    this.selectedEntity.set(item);
    this.activeTab.set('data');
    const filter = this.selectedWarehouseContext()?.code || this.selectedServicePointContext()?.code || '';
    this.dataFilter.set(filter);
    this.drawerSelectedFile.set(null);
    this.drawerReport.set(null);
    this.resetForm();
    this.loadEntityData(item.key);
  }

  closeDrawer(): void {
    this.selectedEntity.set(null);
    this.selectedWarehouseContext.set(null);
    this.selectedServicePointContext.set(null);
    this.drawerSelectedFile.set(null);
    this.drawerReport.set(null);
    this.entityData.set([]);
    this.resetForm();
  }

  onTableRowClick(row: any): void {
    if (this.selectedEntity()?.key === 'warehouses') {
      this.manageWarehouseStock(row);
    } else if (this.selectedEntity()?.key === 'service_points') {
      this.manageServicePointConsumption(row);
    }
  }

  manageWarehouseStock(warehouse: { code: string; name?: string }): void {
    const stockConfig = this.entityList.find(e => e.key === 'stock');
    if (!stockConfig) return;

    this.selectedWarehouseContext.set({
      code: warehouse.code,
      name: warehouse.name || warehouse.code
    });
    this.loadCatalogsForStock();
    this.openDrawer(stockConfig);
    this.dataFilter.set(warehouse.code);

    this.formValues.update(vals => ({
      ...vals,
      location_code: warehouse.code,
      location_kind: 'warehouse'
    }));
  }

  clearWarehouseContext(): void {
    this.selectedWarehouseContext.set(null);
    this.dataFilter.set('');
  }

  manageServicePointConsumption(point: { code: string; name?: string }): void {
    const consumptionsConfig = this.entityList.find(e => e.key === 'consumptions');
    if (!consumptionsConfig) return;

    this.selectedServicePointContext.set({
      code: point.code,
      name: point.name || point.code
    });
    this.loadCatalogsForStock();
    this.openDrawer(consumptionsConfig);
    this.dataFilter.set(point.code);

    this.formValues.update(vals => ({
      ...vals,
      point_code: point.code,
      date: new Date().toISOString().substring(0, 10),
      period_days: 1
    }));
  }

  clearServicePointContext(): void {
    this.selectedServicePointContext.set(null);
    this.dataFilter.set('');
  }

  loadCatalogsForStock(): void {
    if (this.productsCatalog().length === 0) {
      this.apiService.getEntityData('products').subscribe(prods => {
        this.productsCatalog.set(prods || []);
      });
    }
    if (this.lotsCatalog().length === 0) {
      this.apiService.getEntityData('lots').subscribe(lots => {
        this.lotsCatalog.set(lots || []);
      });
    }
  }

  onSkuSelected(sku: string): void {
    this.updateFormField('sku', sku);
    const prod = this.productsCatalog().find(p => p.sku === sku);
    if (prod) {
      this.updateFormField('product_name', prod.name || '');
      this.updateFormField('base_unit', prod.base_unit || '');
    }
    const lots = this.lotsCatalog().filter(l => l.sku === sku);
    if (lots.length > 0) {
      this.lotSelectionMode.set('existing');
      if (this.selectedEntity()?.key === 'stock') {
        this.updateFormField('lot_code', lots[0].lot_code);
      }
    } else {
      this.lotSelectionMode.set('new');
      if (this.selectedEntity()?.key === 'stock') {
        this.updateFormField('lot_code', '');
      }
    }
  }

  downloadTemplateForWarehouse(warehouse: { code: string; name: string }): void {
    const csv = `location_kind,location_code,sku,lot_code,quantity\n`;
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', `stock_${warehouse.code.toLowerCase()}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  downloadTemplateForServicePoint(point: { code: string; name: string }): void {
    const csv = `point_code,sku,date,quantity,period_days\n`;
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', `consumptions_${point.code.toLowerCase()}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  switchTab(tab: 'data' | 'form' | 'upload' | 'format'): void {
    this.activeTab.set(tab);
    if (tab === 'form') {
      setTimeout(() => {
        this.formMapView?.invalidateSize();
      }, 150);
    }
  }

  loadEntityData(key: string): void {
    this.loadingData.set(true);
    this.apiService.getEntityData(key).subscribe({
      next: (data) => {
        this.entityData.set(data || []);
        this.loadingData.set(false);
      },
      error: () => {
        this.entityData.set([]);
        this.loadingData.set(false);
      }
    });
  }

  // ==========================================
  // LÓGICA DEL FORMULARIO Y MAPA INTERACTIVO
  // ==========================================

  resetForm(): void {
    const entity = this.selectedEntity();
    const initial: Record<string, any> = {};
    if (entity) {
      for (const col of entity.columns) {
        if (col.name === 'criticality') initial[col.name] = 'normal';
        else if (col.name === 'category') initial[col.name] = 'hospital';
        else if (col.name === 'location_kind') initial[col.name] = 'warehouse';
        else if (col.name === 'date') initial[col.name] = new Date().toISOString().substring(0, 10);
        else if (col.name === 'period_days') initial[col.name] = 1;
        else initial[col.name] = '';
      }
    }
    // Preservar bodega activa si estamos en stock
    if (this.selectedEntity()?.key === 'stock' && this.selectedWarehouseContext()) {
      initial['location_code'] = this.selectedWarehouseContext()!.code;
      initial['location_kind'] = 'warehouse';
    }
    // Preservar punto activo si estamos en consumos
    if (this.selectedEntity()?.key === 'consumptions' && this.selectedServicePointContext()) {
      initial['point_code'] = this.selectedServicePointContext()!.code;
    }
    this.formValues.set(initial);
    this.formError.set(null);
    this.formSuccess.set(null);
    this.mapMarkers.set([]);
    this.mapCenter.set([-0.1807, -78.4678]);
    this.mapZoom.set(12);
  }

  updateFormField(key: string, value: any): void {
    this.formValues.set({
      ...this.formValues(),
      [key]: value
    });
  }

  updateCoordinates(key: 'lat' | 'lon', val: any): void {
    this.updateFormField(key, val);
    const lat = parseFloat(this.formValues()['lat']);
    const lon = parseFloat(this.formValues()['lon']);
    if (!isNaN(lat) && !isNaN(lon) && lat >= -90 && lat <= 90 && lon >= -180 && lon <= 180) {
      this.setMarkerAt(lat, lon);
      this.mapCenter.set([lat, lon]);
    }
  }

  onMapClick(coords: [number, number]): void {
    const [lat, lon] = coords;
    const current = this.formValues();
    this.formValues.set({
      ...current,
      lat: Number(lat.toFixed(6)),
      lon: Number(lon.toFixed(6))
    });
    this.setMarkerAt(lat, lon);
  }

  centerMap(coords: [number, number], zoom = 12): void {
    this.mapCenter.set(coords);
    this.mapZoom.set(zoom);
    setTimeout(() => {
      this.formMapView?.invalidateSize();
    }, 100);
  }

  private setMarkerAt(lat: number, lon: number): void {
    const isWarehouse = this.selectedEntity()?.key === 'warehouses';
    const name = this.formValues()['name'] || this.formValues()['code'] || 'Ubicación seleccionada';
    this.mapMarkers.set([{
      id: 'selected-location-pin',
      latitude: lat,
      longitude: lon,
      title: name,
      badge: isWarehouse ? 'Bodega' : 'Punto de Servicio',
      badgeColor: isWarehouse ? 'amber' : 'emerald',
      description: `Coordenadas: ${lat.toFixed(6)}, ${lon.toFixed(6)}`
    }]);
  }

  submitForm(): void {
    const entity = this.selectedEntity();
    if (!entity) return;

    this.formError.set(null);
    this.formSuccess.set(null);

    const values = this.formValues();

    // 1. Validar campos requeridos
    for (const col of entity.columns) {
      if (col.required) {
        const v = values[col.name];
        if (v === null || v === undefined || String(v).trim() === '') {
          this.formError.set(`El campo '${this.getFieldLabel(col.name)}' es requerido.`);
          return;
        }
      }
    }

    // 2. Generar CSV de 1 fila para importación canónica
    const headers = entity.columns.map(c => c.name);
    const row = headers.map(h => {
      const val = values[h];
      if (val === null || val === undefined) return '';
      const str = String(val).trim();
      if (str.includes(',') || str.includes('"') || str.includes('\n')) {
        return `"${str.replace(/"/g, '""')}"`;
      }
      return str;
    });

    const csvText = `${headers.join(',')}\n${row.join(',')}`;
    const blob = new Blob([csvText], { type: 'text/csv;charset=utf-8;' });
    const file = new File([blob], entity.fileName, { type: 'text/csv' });

    const formData = new FormData();
    formData.append(entity.key, file, entity.fileName);

    this.formLoading.set(true);
    this.apiService.importData(formData, false).subscribe({
      next: (res) => {
        this.formLoading.set(false);
        if (res && res.applied) {
          this.formSuccess.set(`Registro guardado exitosamente en base de datos.`);
          this.loadEntityData(entity.key);
          if (entity.key === 'stock' || entity.key === 'lots') {
            this.apiService.getEntityData('lots').subscribe(lots => this.lotsCatalog.set(lots || []));
            this.apiService.getEntityData('products').subscribe(prods => this.productsCatalog.set(prods || []));
          }
          this.resetForm();
        } else if (res && res.issues && res.issues.length > 0) {
          this.formError.set(`Incidencia detectada: ${res.issues[0].message}`);
        } else {
          this.formError.set(`No se pudo guardar el registro. Verifica los valores ingresados.`);
        }
      },
      error: () => {
        this.formLoading.set(false);
        this.formError.set(`Error de conexión con el servidor API al guardar.`);
      }
    });
  }

  getFieldLabel(colName: string): string {
    const labels: Record<string, string> = {
      code: 'Código',
      name: 'Nombre descriptivo',
      lat: 'Latitud',
      lon: 'Longitud',
      sku: 'Código SKU',
      product_name: 'Nombre del producto conforme',
      base_unit: 'Unidad de medida base',
      weight_per_unit: 'Peso por unidad (kg)',
      volume_per_unit: 'Volumen por unidad (m³)',
      depot_code: 'Código de Bodega Base',
      max_weight: 'Capacidad máx. peso (kg)',
      max_volume: 'Capacidad máx. volumen (m³)',
      cost_per_km: 'Costo operacional / km ($)',
      lot_code: 'Código de Lote',
      expires_on: 'Fecha de caducidad',
      origin: 'Proveedor / Laboratorio de origen',
      location_code: 'Código de Ubicación',
      location_kind: 'Tipo de Ubicación',
      quantity: 'Cantidad',
      point_code: 'Código Punto Hospitalario',
      date: 'Fecha de consumo',
      period_days: 'Días del período',
      region: 'Región / Provincia',
      category: 'Categoría',
      criticality: 'Nivel de Criticidad',
      window_start: 'Hora de Apertura',
      window_end: 'Hora de Cierre',
      service_minutes: 'Minutos de atención en descarga',
      lead_time_days: 'Días de anticipación (Lead Time)'
    };
    return labels[colName] || colName;
  }

  // ==========================================
  // DESCARGA Y SUBIDA CSV
  // ==========================================

  downloadTemplate(entity: EntityDetailConfig): void {
    const blob = new Blob([entity.csvTemplate], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', entity.fileName);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  copyTemplate(entity: EntityDetailConfig): void {
    navigator.clipboard.writeText(entity.csvTemplate).then(() => {
      this.copySuccess.set(true);
      setTimeout(() => this.copySuccess.set(false), 2200);
    });
  }

  onDrawerFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.drawerSelectedFile.set(input.files[0]);
      this.drawerReport.set(null);
    }
  }

  processDrawerImport(dryRun: boolean): void {
    const file = this.drawerSelectedFile();
    const entity = this.selectedEntity();
    if (!file || !entity) return;

    if (entity.key === 'stock' && this.selectedWarehouseContext()) {
      const warehouse = this.selectedWarehouseContext()!;
      file.text().then(text => {
        const enriched = this.enrichStockCsvWithWarehouse(file, text, warehouse.code);
        const formData = new FormData();
        formData.append(entity.key, enriched, enriched.name);
        this.executeDrawerImport(entity.key, formData, dryRun);
      }).catch(() => {
        const formData = new FormData();
        formData.append(entity.key, file, file.name);
        this.executeDrawerImport(entity.key, formData, dryRun);
      });
      return;
    }

    if (entity.key === 'consumptions' && this.selectedServicePointContext()) {
      const point = this.selectedServicePointContext()!;
      file.text().then(text => {
        const enriched = this.enrichConsumptionCsvWithPoint(file, text, point.code);
        const formData = new FormData();
        formData.append(entity.key, enriched, enriched.name);
        this.executeDrawerImport(entity.key, formData, dryRun);
      }).catch(() => {
        const formData = new FormData();
        formData.append(entity.key, file, file.name);
        this.executeDrawerImport(entity.key, formData, dryRun);
      });
      return;
    }

    const formData = new FormData();
    formData.append(entity.key, file, file.name);
    this.executeDrawerImport(entity.key, formData, dryRun);
  }

  private executeDrawerImport(key: string, formData: FormData, dryRun: boolean): void {
    this.drawerLoading.set(true);
    this.apiService.importData(formData, dryRun).subscribe(res => {
      this.drawerLoading.set(false);
      this.drawerReport.set(res);
      if (!dryRun && res?.applied) {
        this.loadEntityData(key);
        if (key === 'stock') {
          this.apiService.getEntityData('lots').subscribe(lots => this.lotsCatalog.set(lots || []));
        }
      }
    });
  }

  private enrichStockCsvWithWarehouse(file: File, text: string, warehouseCode: string): File {
    const lines = text.split(/\r?\n/).filter(l => l.trim().length > 0);
    if (lines.length === 0) return file;
    const header = lines[0].toLowerCase();
    if (header.includes('location_code')) {
      return file;
    }
    const newHeader = `location_kind,location_code,${lines[0]}`;
    const newLines = [newHeader];
    for (let i = 1; i < lines.length; i++) {
      newLines.push(`warehouse,${warehouseCode},${lines[i]}`);
    }
    const blob = new Blob([newLines.join('\n')], { type: 'text/csv;charset=utf-8;' });
    return new File([blob], file.name, { type: 'text/csv' });
  }

  private enrichConsumptionCsvWithPoint(file: File, text: string, pointCode: string): File {
    const lines = text.split(/\r?\n/).filter(l => l.trim().length > 0);
    if (lines.length === 0) return file;
    const header = lines[0].toLowerCase();
    if (header.includes('point_code')) {
      return file;
    }
    const newHeader = `point_code,${lines[0]}`;
    const newLines = [newHeader];
    for (let i = 1; i < lines.length; i++) {
      newLines.push(`${pointCode},${lines[i]}`);
    }
    const blob = new Blob([newLines.join('\n')], { type: 'text/csv;charset=utf-8;' });
    return new File([blob], file.name, { type: 'text/csv' });
  }

  onFilesSelected(event: Event): void {
    if (!this.session.canWrite()) return;
    const input = event.target as HTMLInputElement;
    if (input.files) {
      this.selectedFiles.set(Array.from(input.files));
      this.report.set(null);
      this.importError.set(null);
      this.importNotice.set(null);
    }
  }

  async processImport(dryRun: boolean): Promise<void> {
    if (!this.session.canWrite()) return;
    const files = this.selectedFiles();
    if (files.length === 0) return;
    this.importError.set(null);

    this.importNotice.set(null);

    // Un dataset suele traer varios .json (mapping, manifest, calendario...). Solo uno es el perfil
    // de mapeo; se reconoce por su contenido y el resto se omite avisando, sin bloquear la carga.
    const jsonFiles = files.filter(f => f.name.toLowerCase().endsWith('.json'));
    const profiles: Array<{ name: string; text: string }> = [];
    const skipped: string[] = [];
    for (const file of jsonFiles) {
      const text = await file.text();
      if (isMappingProfile(text)) profiles.push({ name: file.name, text });
      else skipped.push(file.name);
    }
    if (profiles.length > 1) {
      this.importError.set(`Hay más de un perfil de mapeo (${profiles.map(p => p.name).join(', ')}); deje solo uno.`);
      return;
    }

    const formData = new FormData();
    for (const file of files) {
      if (jsonFiles.includes(file)) continue;
      // El servidor reconoce la entidad por el nombre del archivo o por sus columnas.
      formData.append(file.name, file);
    }
    if (profiles.length === 1) formData.append('profile', profiles[0].text);
    if (skipped.length > 0) {
      this.importNotice.set(`Se omitieron ${skipped.join(', ')}: no son perfiles de mapeo ni tablas importables.`);
    }
    if (![...formData.keys()].some(k => k !== 'profile')) {
      this.importError.set('Seleccione al menos un archivo .csv o .xlsx con datos.');
      return;
    }

    this.loading.set(true);
    this.apiService.importDataOrExplain(formData, dryRun).subscribe({
      next: res => {
        this.loading.set(false);
        this.report.set(res);
      },
      error: (err: Error) => {
        this.loading.set(false);
        this.report.set(null);
        this.importError.set(err.message);
      },
    });
  }

  formatValue(val: any): string {
    if (val === null || val === undefined || val === '') return '—';
    if (typeof val === 'boolean') return val ? 'Activo' : 'Inactivo';
    if (typeof val === 'number') {
      return Number.isInteger(val) ? val.toString() : val.toFixed(4).replace(/\.?0+$/, '');
    }
    return String(val);
  }

  getBadgeClass(colKey: string, val: any): string {
    if (colKey === 'isActive') {
      return val ? 'bg-emerald-50 text-emerald-700 border border-emerald-200' : 'bg-red-50 text-red-700 border border-red-200';
    }
    if (colKey === 'isBlocked') {
      return val ? 'bg-red-50 text-red-700 border border-red-200' : 'bg-emerald-50 text-emerald-700 border border-emerald-200';
    }
    if (colKey === 'criticality') {
      const lower = String(val).toLowerCase();
      if (lower === 'alta' || lower === 'critica') return 'bg-red-50 text-red-700 border border-red-200';
      if (lower === 'media') return 'bg-amber-50 text-amber-700 border border-amber-200';
      return 'bg-blue-50 text-blue-700 border border-blue-200';
    }
    if (colKey === 'category' || colKey === 'location_kind') {
      return 'bg-slate-100 text-slate-700 border border-slate-200';
    }
    return 'bg-slate-100 text-slate-600';
  }
}
