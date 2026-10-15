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

@Component({
  selector: 'page-datos',
  standalone: true,
  imports: [CommonModule, FormsModule, MapViewComponent],
  template: `
    <div class="max-w-5xl mx-auto px-4 py-8">
      <!-- Encabezado Principal -->
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 ">
        <div>
          <div class="flex items-center gap-2">
            <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-primary-100 text-primary-800">
              Gestión de Datos Logísticos
            </span>
            <span class="text-xs text-slate-400">Expo Clean Ecuador 2026</span>
          </div>
          <h1 class="text-2xl font-bold text-slate-900 tracking-tight mt-1">
            Carga y Consulta de Datos del Sistema
          </h1>
          <p class="text-sm text-slate-500 mt-1 max-w-2xl">
            Gestiona la información logística mediante carga CSV masiva o formularios individuales con georreferenciación en mapa interactivo.
          </p>
        </div>
      </div>

      <!-- Cuadrícula de Entidades (Tarjetas Interactivas) -->
      <div class="bg-white border border-slate-200 rounded-2xl p-6 shadow-sm space-y-4">
        <div class="flex items-center justify-between">
          <div>
            <h2 class="text-sm font-bold text-slate-800 uppercase tracking-wider">
              Entidades del Sistema (Orden de Carga Requerido)
            </h2>
            <p class="text-xs text-slate-400 mt-0.5">
              Haz clic en cualquier tarjeta para ver registros, agregar mediante formulario con mapa o subir CSV
            </p>
          </div>
          <span class="text-xs font-mono text-slate-500 bg-slate-100 px-2.5 py-1 rounded-md border border-slate-200">
            5 Entidades Principales
          </span>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3.5 pt-1">
          @for (item of visibleEntityCards(); track item.key) {
            <div 
              (click)="openDrawer(item)"
              role="button"
              tabindex="0"
              (keydown.enter)="openDrawer(item)"
              (keydown.space)="openDrawer(item)"
              class="group relative p-4 bg-slate-50/70 hover:bg-white border border-slate-200/80 hover:border-primary-500 rounded-xl cursor-pointer transition-all duration-200 shadow-sm hover:shadow-md flex flex-col justify-between text-left">
              
              <div>
                <div class="flex items-start justify-between gap-2 mb-2">
                  <div class="flex items-center gap-2.5">
                    <span class="w-8 h-8 rounded-lg bg-primary-50 border border-primary-200/60 text-primary-700 font-mono text-xs font-bold flex items-center justify-center shrink-0 group-hover:bg-primary-600 group-hover:text-white transition-colors">
                      {{ item.orderNumber }}
                    </span>
                    <div>
                      <h3 class="font-bold text-slate-900 text-sm group-hover:text-primary-700 transition-colors">
                        {{ item.name }}
                      </h3>
                      <span class="font-mono text-[11px] text-slate-400">
                        {{ item.fileName }}
                      </span>
                    </div>
                  </div>
                  
                  <span class="text-primary-600 opacity-0 group-hover:opacity-100 transition-opacity">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
                    </svg>
                  </span>
                </div>

                <p class="text-slate-600 text-xs leading-relaxed line-clamp-2">
                  {{ item.desc }}
                </p>
              </div>

              <div class="mt-3 pt-2.5 border-t border-slate-200/60 flex items-center justify-between text-[11px]">
                <span class="text-slate-400 font-mono truncate max-w-[190px]" title="Campos: {{ item.fields }}">
                  {{ item.fields }}
                </span>
                <span class="text-primary-600 font-medium group-hover:underline flex items-center gap-1">
                  Gestionar
                </span>
              </div>
            </div>
          }
        </div>
      </div>

      @if (session.canWrite()) {
        <!-- Sección de Carga Masiva (Multi-archivo simultáneo) -->
        <div class="bg-white border border-slate-200 rounded-2xl p-6 shadow-sm space-y-5">
          <div class="border-b border-slate-100 pb-3">
            <h2 class="text-base font-bold text-slate-800">
              Carga Masiva de Múltiples Archivos
            </h2>
            <p class="text-xs text-slate-500 mt-0.5">
              Si dispones de varios archivos CSV o XLSX preparados, puedes cargarlos todos juntos. Las dependencias se resolverán automáticamente.
            </p>
          </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-2">
            Seleccionar archivo(s) CSV o XLSX
          </label>
          <input 
            type="file" 
            multiple
            (change)="onFilesSelected($event)"
            accept=".csv,.xlsx,.json"
            class="block w-full text-xs text-slate-500 file:mr-4 file:py-2.5 file:px-4 file:rounded-lg file:border-0 file:text-xs file:font-semibold file:bg-primary-50 file:text-primary-700 hover:file:bg-primary-100 cursor-pointer border border-slate-200 rounded-lg p-2 bg-slate-50/50" />
          <p class="text-[11px] text-slate-400 mt-1.5">
            Formatos admitidos: .csv, .xlsx. El nombre del archivo o sus encabezados detectarán la entidad correspondiente. Si el dataset trae un perfil de mapeo (.json), inclúyalo en la selección.
          </p>
        </div>

        @if (selectedFiles().length > 0) {
          <div class="space-y-3 bg-slate-50/70 p-4 rounded-xl border border-slate-200/70">
            <h3 class="text-xs font-semibold text-slate-700 flex items-center gap-2">
              <svg class="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
              </svg>
              <span>Archivos listos para procesar ({{ selectedFiles().length }}):</span>
            </h3>
            <ul class="text-xs space-y-1.5 divide-y divide-slate-200/50">
              @for (file of selectedFiles(); track file.name) {
                <li class="pt-1.5 flex justify-between items-center text-slate-700">
                  <span class="font-medium truncate max-w-sm">{{ file.name }}</span>
                  <span class="font-mono text-[11px] text-slate-400 bg-white px-2 py-0.5 rounded border border-slate-200">
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
                class="px-4 py-2 rounded-lg bg-white border border-slate-300 text-slate-700 hover:bg-slate-100 text-xs font-semibold transition-colors disabled:opacity-50 shadow-2xs">
                {{ loading() ? 'Validando...' : '1. Validar sin guardar (Simulación)' }}
              </button>

              <button 
                type="button"
                (click)="processImport(false)"
                [disabled]="loading()"
                class="px-4 py-2 rounded-lg bg-primary-600 text-white hover:bg-primary-700 text-xs font-semibold transition-colors disabled:opacity-50 shadow-2xs">
                {{ loading() ? 'Procesando...' : '2. Guardar en Base de Datos' }}
              </button>
            </div>
          </div>
        }

        @if (importError()) {
          <div class="border border-rose-200 bg-rose-50 text-rose-800 rounded-xl p-4 text-xs" role="alert">
            <p class="font-semibold">No se pudo procesar la carga</p>
            <p class="mt-1">{{ importError() }}</p>
          </div>
        }
        <!-- Resultado de la importación masiva -->
        @if (report()) {
          <div 
            class="border rounded-xl p-5 space-y-4 transition-all"
            [ngClass]="report()!.errorCount > 0 ? 'bg-amber-50/60 border-amber-200' : 'bg-emerald-50/60 border-emerald-200'">
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
              <span class="text-xs font-mono px-2.5 py-1 rounded bg-white border border-slate-200 shadow-2xs">
                Errores: {{ report()!.errorCount }} | Avisos: {{ report()!.warningCount }}
              </span>
            </div>

            @if (report()!.entities.length > 0) {
              <div class="bg-white rounded-lg border border-slate-200/80 overflow-hidden shadow-2xs">
                <table class="w-full text-left text-xs">
                  <thead class="bg-slate-50 text-slate-600 text-[11px] uppercase border-b border-slate-200 font-bold">
                    <tr>
                      <th class="p-2.5">Entidad</th>
                      <th class="p-2.5">Filas Leídas</th>
                      <th class="p-2.5">Creados</th>
                      <th class="p-2.5">Actualizados</th>
                      <th class="p-2.5">Omitidos</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    @for (ent of report()!.entities; track ent.entity) {
                      <tr class="hover:bg-slate-50/50">
                        <td class="p-2.5 font-bold text-slate-800">{{ ent.entity }}</td>
                        <td class="p-2.5 font-mono text-slate-600">{{ ent.rowsRead }}</td>
                        <td class="p-2.5 font-mono text-emerald-600 font-semibold">+{{ ent.created }}</td>
                        <td class="p-2.5 font-mono text-blue-600">{{ ent.updated }}</td>
                        <td class="p-2.5 font-mono text-slate-400">{{ ent.skipped }}</td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }

            @if (report()!.issues.length > 0) {
              <div class="space-y-1.5 text-xs">
                <h4 class="font-bold text-slate-800">Detalles e incidencias reportadas:</h4>
                <ul class="space-y-1.5 max-h-48 overflow-y-auto">
                  @for (issue of report()!.issues; track $index) {
                    <li class="p-2.5 rounded bg-white border border-slate-200 text-slate-700 shadow-2xs">
                      <span class="font-bold text-slate-900">[{{ issue.entity }} Fila {{ issue.row }}]:</span>
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
        <p role="alert" class="rounded-xl border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
          No tienes permiso para importar datos.
        </p>
      }

      <!-- ============================================================= -->
      <!-- DRAWER LATERAL DE ENTIDAD                                     -->
      <!-- ============================================================= -->
      @if (selectedEntity()) {
        <!-- Fondo oscuro semitransparente -->
        <div 
          class="fixed inset-0 z-40 bg-slate-900/40 backdrop-blur-xs transition-opacity duration-300"
          (click)="closeDrawer()">
        </div>

        <!-- Panel Lateral Deslizable -->
        <aside 
          class="fixed inset-y-0 right-0 z-50 w-full max-w-4xl lg:max-w-5xl xl:max-w-6xl bg-white shadow-2xl border-l border-slate-200 flex flex-col h-full transform transition-transform duration-300 ease-out animate-in slide-in-from-right"
          role="dialog"
          aria-modal="true"
          (click)="$event.stopPropagation()">
          
          <!-- Encabezado del Drawer -->
          <div class="px-6 py-4.5 border-b border-slate-200 bg-slate-50/90 flex items-center justify-between shrink-0 mt-6">
            <div class="flex items-center gap-3">
              <span class="w-9 h-9 rounded-lg bg-primary-600 text-white font-mono text-sm font-bold flex items-center justify-center shadow-xs">
                {{ selectedEntity()!.orderNumber }}
              </span>
              <div>
                <div class="flex items-center gap-2">
                  <h2 class="text-base font-bold text-slate-900">
                    {{ selectedEntity()!.name }}
                  </h2>
                  <span class="text-xs font-mono bg-slate-100 text-slate-600 px-2 py-0.5 rounded border border-slate-200">
                    {{ selectedEntity()!.fileName }}
                  </span>
                </div>
                <p class="text-xs text-slate-500 mt-0.5 line-clamp-1">
                  {{ selectedEntity()!.desc }}
                </p>
              </div>
            </div>

            <button 
              type="button"
              (click)="closeDrawer()"
              class="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-200/70 rounded-lg transition-colors"
              title="Cerrar panel (Esc)">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <!-- Navegación por Pestañas del Drawer -->
          <div class="flex border-b border-slate-200 bg-slate-50/50 px-4 sm:px-6 gap-1.5 sm:gap-2 text-xs font-semibold overflow-x-auto no-scrollbar shrink-0 mt-4">
            <button
              type="button"
              (click)="switchTab('data')"
              [ngClass]="activeTab() === 'data' 
                ? 'border-primary-600 text-primary-700 bg-white border-b-2 shadow-2xs' 
                : 'text-slate-500 hover:text-slate-800 border-b-2 border-transparent'"
              class="py-3 px-3 transition-colors flex items-center gap-1.5 whitespace-nowrap">
              <span>Datos Actuales</span>
              <span class="ml-1 px-1.5 py-0.5 rounded-full text-[10px] bg-slate-100 text-slate-600 border border-slate-200">
                {{ entityData().length }}
              </span>
            </button>

            <button
              type="button"
              (click)="switchTab('form')"
              [ngClass]="activeTab() === 'form' 
                ? 'border-primary-600 text-primary-700 bg-white border-b-2 shadow-2xs' 
                : 'text-slate-500 hover:text-slate-800 border-b-2 border-transparent'"
              class="py-3 px-3 transition-colors flex items-center gap-1.5 whitespace-nowrap">
              <span class="font-bold">+</span>
              <span>Nuevo Registro</span>
            </button>

            <button
              type="button"
              (click)="switchTab('upload')"
              [ngClass]="activeTab() === 'upload' 
                ? 'border-primary-600 text-primary-700 bg-white border-b-2 shadow-2xs' 
                : 'text-slate-500 hover:text-slate-800 border-b-2 border-transparent'"
              class="py-3 px-3 transition-colors flex items-center gap-1.5 whitespace-nowrap">
              <span>Subir Archivo CSV</span>
            </button>

            <button
              type="button"
              (click)="switchTab('format')"
              [ngClass]="activeTab() === 'format' 
                ? 'border-primary-600 text-primary-700 bg-white border-b-2 shadow-2xs' 
                : 'text-slate-500 hover:text-slate-800 border-b-2 border-transparent'"
              class="py-3 px-3 transition-colors flex items-center gap-1.5 whitespace-nowrap">
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
                  <div class="p-3.5 bg-gradient-to-r from-primary-50 via-sky-50 to-indigo-50 border border-primary-200 rounded-xl flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-2xs">
                    <div class="flex items-center gap-3">
                      <div class="w-9 h-9 rounded-lg bg-primary-600 text-white flex items-center justify-center shrink-0 shadow-xs">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
                        </svg>
                      </div>
                      <div>
                        <div class="flex items-center gap-2">
                          <span class="text-xs font-bold text-slate-900">Bodega Seleccionada: {{ selectedWarehouseContext()!.name }}</span>
                          <span class="font-mono text-[11px] font-bold bg-primary-100 text-primary-800 px-2 py-0.5 rounded border border-primary-200">
                            {{ selectedWarehouseContext()!.code }}
                          </span>
                        </div>
                        <p class="text-[11px] text-slate-500 mt-0.5">
                          Mostrando el inventario de esta bodega. Puedes cargar nuevo stock manual o importar CSV directo.
                        </p>
                      </div>
                    </div>

                    <div class="flex items-center gap-2">
                      <button 
                        type="button" 
                        (click)="switchTab('form')"
                        class="px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary-600 hover:bg-primary-700 text-white shadow-2xs flex items-center gap-1.5 transition-colors">
                        <span class="font-bold">+</span>
                        <span>Ingresar Stock</span>
                      </button>

                      <button 
                        type="button" 
                        (click)="switchTab('upload')"
                        class="px-3 py-1.5 text-xs font-semibold rounded-lg bg-white hover:bg-slate-50 border border-slate-300 text-slate-700 shadow-2xs flex items-center gap-1.5 transition-colors">
                        <svg class="w-3.5 h-3.5 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/>
                        </svg>
                        <span>Importar CSV</span>
                      </button>

                      <button 
                        type="button" 
                        (click)="clearWarehouseContext()"
                        class="px-2.5 py-1.5 text-xs font-semibold text-slate-500 hover:text-slate-800 hover:bg-slate-200/60 rounded-lg transition-colors"
                        title="Ver todas las ubicaciones">
                        ✕ Quitar filtro
                      </button>
                    </div>
                  </div>
                }

                <!-- Banner Contextual si se seleccionó un punto de entrega específico para consumos -->
                @if (selectedEntity()!.key === 'consumptions' && selectedServicePointContext()) {
                  <div class="p-3.5 bg-gradient-to-r from-emerald-50 via-teal-50 to-sky-50 border border-emerald-200 rounded-xl flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-2xs">
                    <div class="flex items-center gap-3">
                      <div class="w-9 h-9 rounded-lg bg-emerald-600 text-white flex items-center justify-center shrink-0 shadow-xs">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
                        </svg>
                      </div>
                      <div>
                        <div class="flex items-center gap-2">
                          <span class="text-xs font-bold text-slate-900">Punto de Entrega: {{ selectedServicePointContext()!.name }}</span>
                          <span class="font-mono text-[11px] font-bold bg-emerald-100 text-emerald-800 px-2 py-0.5 rounded border border-emerald-200">
                            {{ selectedServicePointContext()!.code }}
                          </span>
                        </div>
                        <p class="text-[11px] text-slate-500 mt-0.5">
                          Mostrando consumos históricos registrados para este establecimiento de salud.
                        </p>
                      </div>
                    </div>

                    <div class="flex items-center gap-2">
                      <button 
                        type="button" 
                        (click)="switchTab('form')"
                        class="px-3 py-1.5 text-xs font-semibold rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white shadow-2xs flex items-center gap-1.5 transition-colors">
                        <span class="font-bold">+</span>
                        <span>Registrar Consumo</span>
                      </button>

                      <button 
                        type="button" 
                        (click)="switchTab('upload')"
                        class="px-3 py-1.5 text-xs font-semibold rounded-lg bg-white hover:bg-slate-50 border border-slate-300 text-slate-700 shadow-2xs flex items-center gap-1.5 transition-colors">
                        <svg class="w-3.5 h-3.5 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/>
                        </svg>
                        <span>Importar CSV Consumo</span>
                      </button>

                      <button 
                        type="button" 
                        (click)="clearServicePointContext()"
                        class="px-2.5 py-1.5 text-xs font-semibold text-slate-500 hover:text-slate-800 hover:bg-slate-200/60 rounded-lg transition-colors"
                        title="Ver todos los puntos">
                        ✕ Quitar filtro
                      </button>
                    </div>
                  </div>
                }

                <!-- Barra de herramientas: Búsqueda y Acciones -->
                <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                  <div class="relative flex-1">
                    <span class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <svg class="w-3.5 h-3.5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
                      </svg>
                    </span>
                    <input 
                      type="text"
                      [ngModel]="dataFilter()"
                      (ngModelChange)="dataFilter.set($event)"
                      placeholder="Buscar por código, nombre o cualquier campo..."
                      class="w-full pl-9 pr-3 py-2 text-xs border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent bg-slate-50/50" />
                  </div>

                  <div class="flex items-center gap-2">
                    <button 
                      type="button"
                      (click)="loadEntityData(selectedEntity()!.key)"
                      [disabled]="loadingData()"
                      class="px-3 py-2 text-xs font-semibold rounded-lg border border-slate-200 bg-white text-slate-700 hover:bg-slate-50 flex items-center gap-1.5 transition-colors disabled:opacity-50">
                      <svg class="w-3.5 h-3.5" [class.animate-spin]="loadingData()" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
                      </svg>
                      <span>{{ loadingData() ? 'Cargando...' : 'Refrescar' }}</span>
                    </button>

                    <button 
                      type="button"
                      (click)="switchTab('form')"
                      class="px-3 py-2 text-xs font-semibold rounded-lg bg-primary-600 text-white hover:bg-primary-700 flex items-center gap-1.5 transition-colors shadow-2xs">
                      <span class="font-bold">+</span>
                      <span>Agregar Manual</span>
                    </button>
                  </div>
                </div>

                <!-- Estado de carga -->
                @if (loadingData()) {
                  <div class="py-16 text-center space-y-3">
                    <div class="inline-block w-7 h-7 border-3 border-primary-600 border-t-transparent rounded-full animate-spin"></div>
                    <p class="text-xs text-slate-500">Consultando registros en base de datos...</p>
                  </div>
                } @else if (filteredEntityData().length === 0) {
                  <!-- Estado vacío -->
                  <div class="py-12 px-4 text-center border-2 border-dashed border-slate-200 rounded-xl space-y-3 bg-slate-50/40">
                    <svg class="w-10 h-10 text-slate-300 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4"/>
                    </svg>
                    <h3 class="text-sm font-bold text-slate-800">
                      {{ dataFilter() ? 'No se encontraron coincidencias' : 'No hay datos cargados para esta entidad' }}
                    </h3>
                    <p class="text-xs text-slate-500 max-w-sm mx-auto">
                      {{ dataFilter() ? 'Prueba con otros términos de búsqueda.' : 'Puedes agregar registros directamente con el formulario o importar un archivo CSV.' }}
                    </p>
                    @if (!dataFilter()) {
                      <div class="flex items-center justify-center gap-2 pt-1">
                        <button 
                          type="button"
                          (click)="switchTab('form')"
                          class="px-4 py-2 text-xs font-semibold rounded-lg bg-primary-600 text-white hover:bg-primary-700 shadow-2xs">
                          + Agregar mediante Formulario
                        </button>
                        <button 
                          type="button"
                          (click)="switchTab('upload')"
                          class="px-4 py-2 text-xs font-semibold rounded-lg border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 shadow-2xs">
                          Subir archivo CSV
                        </button>
                      </div>
                    }
                  </div>
                } @else {
                  <!-- Tabla de datos registrados -->
                  <div class="border border-slate-200 rounded-xl overflow-hidden shadow-2xs bg-white">
                    <div class="overflow-x-auto max-h-[460px]">
                      <table class="w-full text-left text-xs">
                        <thead class="bg-slate-100 text-slate-700 text-[11px] uppercase border-b border-slate-200 sticky top-0 z-10 font-bold">
                          <tr>
                            <th class="p-3 w-12 text-center text-slate-400">#</th>
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
                        <tbody class="divide-y divide-slate-100">
                          @for (row of filteredEntityData(); track $index) {
                            <tr 
                              (click)="onTableRowClick(row)"
                              [class.cursor-pointer]="selectedEntity()!.key === 'warehouses' || selectedEntity()!.key === 'service_points'"
                              class="hover:bg-primary-50/40 transition-colors group">
                              <td class="p-3 text-center text-slate-400 font-mono text-[11px]">
                                {{ $index + 1 }}
                              </td>
                              @for (col of selectedEntity()!.tableCols; track col.key) {
                                <td class="p-3 whitespace-nowrap">
                                  @if (col.badge) {
                                    <span 
                                      class="px-2 py-0.5 rounded-full text-[10px] font-semibold"
                                      [ngClass]="getBadgeClass(col.key, row[col.key])">
                                      {{ formatValue(row[col.key]) }}
                                    </span>
                                  } @else if (col.key === 'code' || col.key === 'sku' || col.key === 'lot_code' || col.key === 'location_code' || col.key === 'point_code') {
                                    <span class="font-mono font-bold text-slate-900 bg-slate-100 px-1.5 py-0.5 rounded">
                                      {{ formatValue(row[col.key]) }}
                                    </span>
                                  } @else {
                                    <span class="text-slate-700">
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
                                    class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-primary-600 hover:bg-primary-700 text-white font-semibold text-xs transition-colors shadow-2xs group-hover:shadow-md">
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
                                    class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-xs transition-colors shadow-2xs group-hover:shadow-md">
                                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/>
                                    </svg>
                                    <span>Ver / Cargar Consumo</span>
                                  </button>
                                </td>
                              }
                            </tr>
                          }
                        </tbody>
                      </table>
                    </div>

                    <div class="px-4 py-2.5 bg-slate-50 border-t border-slate-200 flex items-center justify-between text-[11px] text-slate-500">
                      <span>Mostrando {{ filteredEntityData().length }} de {{ entityData().length }} registro(s)</span>
                      <span class="font-mono text-slate-400">{{ selectedEntity()!.key }}</span>
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
                  <h3 class="text-sm font-bold text-slate-900">
                    Registrar nuevo registro en {{ selectedEntity()!.name }}
                  </h3>
                  <p class="text-xs text-slate-500 mt-0.5">
                    Completa los campos del formulario. Los cambios se guardarán directamente en la base de datos.
                  </p>
                </div>

                <!-- Mensaje de éxito o error -->
                @if (formSuccess()) {
                  <div class="p-3.5 rounded-xl bg-emerald-50 border border-emerald-200 text-xs text-emerald-800 flex items-center justify-between">
                    <div class="flex items-center gap-2">
                      <svg class="w-4 h-4 text-emerald-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                      </svg>
                      <span>{{ formSuccess() }}</span>
                    </div>
                    <button 
                      type="button" 
                      (click)="switchTab('data')"
                      class="text-xs font-bold underline hover:text-emerald-950">
                      Ver en tabla →
                    </button>
                  </div>
                }

                @if (formError()) {
                  <div class="p-3.5 rounded-xl bg-red-50 border border-red-200 text-xs text-red-800 flex items-center gap-2">
                    <svg class="w-4 h-4 text-red-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
                        <label class="block text-xs font-semibold text-slate-700">
                          {{ getFieldLabel(col.name) }}
                          @if (col.required) {
                            <span class="text-red-500 font-bold">*</span>
                          }
                        </label>

                        <!-- UBICACIÓN PARA STOCK: Si hay bodega fija seleccionada -->
                        @if (selectedEntity()!.key === 'stock' && col.name === 'location_code' && selectedWarehouseContext()) {
                          <div class="p-2.5 rounded-lg bg-primary-50/70 border border-primary-200 flex items-center justify-between">
                            <div class="flex items-center gap-2">
                              <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
                              <span class="text-xs font-bold font-mono text-slate-900">{{ selectedWarehouseContext()!.code }}</span>
                              <span class="text-xs text-slate-600">({{ selectedWarehouseContext()!.name }})</span>
                            </div>
                            <span class="text-[10px] font-semibold text-primary-700 bg-white px-2 py-0.5 rounded border border-primary-200">
                              Bodega activa
                            </span>
                          </div>
                        } @else if (selectedEntity()!.key === 'consumptions' && col.name === 'point_code' && selectedServicePointContext()) {
                          <!-- PUNTO DE ENTREGA PARA CONSUMO: Si hay punto fijo seleccionado -->
                          <div class="p-2.5 rounded-lg bg-emerald-50/70 border border-emerald-200 flex items-center justify-between">
                            <div class="flex items-center gap-2">
                              <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
                              <span class="text-xs font-bold font-mono text-slate-900">{{ selectedServicePointContext()!.code }}</span>
                              <span class="text-xs text-slate-600">({{ selectedServicePointContext()!.name }})</span>
                            </div>
                            <span class="text-[10px] font-semibold text-emerald-700 bg-white px-2 py-0.5 rounded border border-emerald-200">
                              Punto de entrega activo
                            </span>
                          </div>
                        } @else if ((selectedEntity()!.key === 'stock' || selectedEntity()!.key === 'consumptions' || selectedEntity()!.key === 'lots') && col.name === 'sku' && productsCatalog().length > 0) {
                          <!-- PRODUCTO PARA STOCK, CONSUMO O LOTES: Selector enriquecido con SKU y Nombre -->
                          <div class="space-y-2">
                            <select 
                              [ngModel]="formValues()[col.name]"
                              (ngModelChange)="onSkuSelected($event)"
                              [name]="col.name"
                              class="w-full px-3 py-2 text-xs border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 bg-white">
                              <option value="">-- Selecciona el Producto / Insumo --</option>
                              @for (p of productsCatalog(); track p.sku) {
                                <option [value]="p.sku">{{ p.sku }} — {{ p.name }} ({{ p.base_unit }})</option>
                              }
                            </select>

                            @if (selectedEntity()!.key === 'lots' && formValues()['product_name']) {
                              <div class="p-2.5 rounded-lg bg-indigo-50/80 border border-indigo-200 flex items-center justify-between text-xs">
                                <div class="flex items-center gap-2">
                                  <span class="w-2 h-2 rounded-full bg-indigo-600"></span>
                                  <span class="font-bold text-slate-800">{{ formValues()['product_name'] }}</span>
                                </div>
                                <span class="font-mono text-[11px] font-bold text-indigo-700 bg-white px-2 py-0.5 rounded border border-indigo-200">
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
                                  class="text-[11px] px-2.5 py-1.5 rounded-lg border border-slate-200 bg-slate-50 text-slate-700">
                                  <option value="existing">Elegir Lote Existente ({{ availableLotsForSelectedSku().length }})</option>
                                  <option value="new">+ Escribir Lote Nuevo</option>
                                </select>
                              </div>

                              @if (lotSelectionMode() === 'existing') {
                                <select 
                                  [ngModel]="formValues()['lot_code']"
                                  (ngModelChange)="updateFormField('lot_code', $event)"
                                  name="lot_code"
                                  class="w-full px-3 py-2 text-xs border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 bg-white font-mono">
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
                                class="w-full px-3 py-2 text-xs border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 bg-white font-mono" />
                              <p class="text-[10px] text-slate-500">
                                Si el lote es nuevo, se registrará automáticamente en la base de datos con este código.
                              </p>
                            }
                          </div>
                        } @else if (col.name === 'criticality') {
                          <select 
                            [ngModel]="formValues()[col.name]"
                            (ngModelChange)="updateFormField(col.name, $event)"
                            [name]="col.name"
                            class="w-full px-3 py-2 text-xs border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 bg-white">
                            <option value="alta">Alta</option>
                            <option value="media">Media</option>
                            <option value="normal">Normal</option>
                          </select>
                        } @else if (col.name === 'category') {
                          <select 
                            [ngModel]="formValues()[col.name]"
                            (ngModelChange)="updateFormField(col.name, $event)"
                            [name]="col.name"
                            class="w-full px-3 py-2 text-xs border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 bg-white">
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
                            class="w-full px-3 py-2 text-xs border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 bg-white">
                            <option value="warehouse">Bodega (warehouse)</option>
                            <option value="service_point">Punto de Servicio (service_point)</option>
                          </select>
                        } @else if (col.name === 'period_days') {
                          <select 
                            [ngModel]="formValues()[col.name]"
                            (ngModelChange)="updateFormField(col.name, $event)"
                            [name]="col.name"
                            class="w-full px-3 py-2 text-xs border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 bg-white">
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
                            class="w-full px-3 py-2 text-xs border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 bg-white" />
                        } @else if (col.type.includes('Hora')) {
                          <input 
                            type="time"
                            [ngModel]="formValues()[col.name]"
                            (ngModelChange)="updateFormField(col.name, $event)"
                            [name]="col.name"
                            class="w-full px-3 py-2 text-xs border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 bg-white" />
                        } @else {
                          <input 
                            [type]="col.type === 'Decimal' || col.type === 'Entero' ? 'number' : 'text'"
                            [step]="col.type === 'Decimal' ? '0.0001' : '1'"
                            [ngModel]="formValues()[col.name]"
                            (ngModelChange)="updateFormField(col.name, $event)"
                            [name]="col.name"
                            [placeholder]="'Ej: ' + col.example"
                            class="w-full px-3 py-2 text-xs border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 bg-white" />
                          @if (selectedEntity()!.key === 'stock' && col.name === 'quantity') {
                            <p class="text-[10px] text-primary-700 bg-primary-50 p-1.5 rounded border border-primary-200 mt-1">
                              💡 Saldo disponible físico. El sistema guardará esta cantidad y generará la auditoría en el Kardex.
                            </p>
                          } @else if (selectedEntity()!.key === 'consumptions' && col.name === 'quantity') {
                            <p class="text-[10px] text-emerald-700 bg-emerald-50 p-1.5 rounded border border-emerald-200 mt-1">
                              💡 Total de unidades consumidas por el centro de salud en este período.
                            </p>
                          }
                        }

                        <p class="text-[10px] text-slate-400">
                          {{ col.desc }}
                        </p>
                      </div>
                    }
                  }
                </div>

                <!-- SECCIÓN ESPECIAL: GEORREFERENCIACIÓN CON MAPA INTERACTIVO -->
                @if (selectedEntity()!.hasLocationMap) {
                  <div class="border border-slate-200 rounded-xl p-4 bg-slate-50/60 space-y-3">
                    <div class="flex items-center justify-between">
                      <div>
                        <h4 class="text-xs font-bold text-slate-800 uppercase tracking-wider flex items-center gap-1.5">
                          <svg class="w-3.5 h-3.5 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/>
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/>
                          </svg>
                          <span>Georreferenciación en el Mapa</span>
                        </h4>
                        <p class="text-[11px] text-slate-500 mt-0.5">
                          Haz clic en cualquier punto del mapa para colocar el marcador y asignar automáticamente las coordenadas.
                        </p>
                      </div>

                      <div class="flex items-center gap-1 text-[11px]">
                        <button 
                          type="button" 
                          (click)="centerMap([-0.1807, -78.4678], 12)" 
                          class="px-2 py-1 bg-white hover:bg-slate-100 border border-slate-200 rounded text-slate-600 font-medium">
                          Quito
                        </button>
                        <button 
                          type="button" 
                          (click)="centerMap([-2.1894, -79.8891], 12)" 
                          class="px-2 py-1 bg-white hover:bg-slate-100 border border-slate-200 rounded text-slate-600 font-medium">
                          Guayaquil
                        </button>
                        <button 
                          type="button" 
                          (click)="centerMap([-1.8312, -78.1834], 7)" 
                          class="px-2 py-1 bg-white hover:bg-slate-100 border border-slate-200 rounded text-slate-600 font-medium">
                          Ecuador
                        </button>
                      </div>
                    </div>

                    <!-- Inputs de Coordenadas Manuales o Autocompletadas -->
                    <div class="grid grid-cols-2 gap-3 pt-1">
                      <div>
                        <label class="block text-[11px] font-semibold text-slate-700 mb-1">
                          Latitud (WGS84) <span class="text-red-500 font-bold">*</span>
                        </label>
                        <input 
                          type="number"
                          step="0.000001"
                          [ngModel]="formValues()['lat']"
                          (ngModelChange)="updateCoordinates('lat', $event)"
                          name="lat"
                          placeholder="Ej: -0.2295"
                          class="w-full px-3 py-2 text-xs border border-slate-200 rounded-lg focus:ring-2 focus:ring-primary-500 bg-white font-mono" />
                      </div>

                      <div>
                        <label class="block text-[11px] font-semibold text-slate-700 mb-1">
                          Longitud (WGS84) <span class="text-red-500 font-bold">*</span>
                        </label>
                        <input 
                          type="number"
                          step="0.000001"
                          [ngModel]="formValues()['lon']"
                          (ngModelChange)="updateCoordinates('lon', $event)"
                          name="lon"
                          placeholder="Ej: -78.5243"
                          class="w-full px-3 py-2 text-xs border border-slate-200 rounded-lg focus:ring-2 focus:ring-primary-500 bg-white font-mono" />
                      </div>
                    </div>

                    <!-- Componente MapView interactivo -->
                    <div class="h-64 rounded-xl overflow-hidden border border-slate-200 shadow-2xs relative">
                      <ui-map-view
                        #formMapView
                        [center]="mapCenter()"
                        [zoom]="mapZoom()"
                        [markers]="mapMarkers()"
                        [showLegend]="false"
                        (mapClick)="onMapClick($event)">
                      </ui-map-view>
                      
                      @if (mapMarkers().length > 0) {
                        <div class="absolute top-2 right-2 z-[400] bg-white/95 backdrop-blur px-2.5 py-1 rounded-md border border-slate-200 shadow-xs text-[11px] font-mono text-slate-700">
                          Pin: {{ formValues()['lat'] }}, {{ formValues()['lon'] }}
                        </div>
                      }
                    </div>
                  </div>
                }

                <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100">
                  <button 
                    type="button"
                    (click)="resetForm()"
                    class="px-4 py-2 text-xs font-semibold rounded-lg border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 transition-colors">
                    Limpiar formulario
                  </button>

                  <button 
                    type="submit"
                    [disabled]="formLoading()"
                    class="px-5 py-2 text-xs font-semibold rounded-lg bg-primary-600 text-white hover:bg-primary-700 transition-colors shadow-2xs flex items-center gap-1.5 disabled:opacity-50">
                    @if (formLoading()) {
                      <svg class="w-3.5 h-3.5 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
                      </svg>
                      <span>Guardando en BD...</span>
                    } @else {
                      <span>Guardar Registro en BD</span>
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
                  <div class="p-4 rounded-xl bg-primary-50/70 border border-primary-200 text-xs text-primary-950 space-y-2">
                    <div class="flex items-center justify-between">
                      <div class="flex items-center gap-2">
                        <span class="w-2.5 h-2.5 rounded-full bg-emerald-500"></span>
                        <span class="font-bold">Importación directa a: {{ selectedWarehouseContext()!.name }}</span>
                        <span class="font-mono text-[10px] font-bold bg-primary-200 text-primary-900 px-1.5 py-0.5 rounded">{{ selectedWarehouseContext()!.code }}</span>
                      </div>
                      <button 
                        type="button"
                        (click)="downloadTemplateForWarehouse(selectedWarehouseContext()!)"
                        class="text-xs font-semibold text-primary-700 bg-white hover:bg-primary-50 px-2.5 py-1 rounded-lg border border-primary-300 transition-colors shadow-2xs">
                        📥 Descargar Plantilla {{ selectedWarehouseContext()!.code }}
                      </button>
                    </div>
                    <p class="text-[11px] text-slate-600">
                      Puedes subir un CSV estándar o simplificado (con columnas <code>sku,lot_code,quantity</code>). Los registros se asociarán automáticamente a esta bodega.
                    </p>
                  </div>
                }

                <!-- Banner para importación directa a punto de entrega activo -->
                @if (selectedEntity()!.key === 'consumptions' && selectedServicePointContext()) {
                  <div class="p-4 rounded-xl bg-emerald-50/70 border border-emerald-200 text-xs text-emerald-950 space-y-2">
                    <div class="flex items-center justify-between">
                      <div class="flex items-center gap-2">
                        <span class="w-2.5 h-2.5 rounded-full bg-emerald-500"></span>
                        <span class="font-bold">Importación directa a: {{ selectedServicePointContext()!.name }}</span>
                        <span class="font-mono text-[10px] font-bold bg-emerald-200 text-emerald-900 px-1.5 py-0.5 rounded">{{ selectedServicePointContext()!.code }}</span>
                      </div>
                      <button 
                        type="button"
                        (click)="downloadTemplateForServicePoint(selectedServicePointContext()!)"
                        class="text-xs font-semibold text-emerald-700 bg-white hover:bg-emerald-50 px-2.5 py-1 rounded-lg border border-emerald-300 transition-colors shadow-2xs">
                        📥 Descargar Plantilla {{ selectedServicePointContext()!.code }}
                      </button>
                    </div>
                    <p class="text-[11px] text-slate-600">
                      Puedes subir un CSV estándar o simplificado (con columnas <code>sku,date,quantity,period_days</code>). Los registros se asignarán automáticamente a este punto de entrega.
                    </p>
                  </div>
                }

                <!-- Área de selección de archivo -->
                <div class="border-2 border-dashed border-slate-200 hover:border-primary-400 rounded-xl p-6 text-center bg-slate-50/40 hover:bg-white transition-all space-y-3">
                  <svg class="w-10 h-10 text-slate-400 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"/>
                  </svg>

                  <div>
                    <h4 class="text-sm font-bold text-slate-800">
                      Cargar archivo para {{ selectedEntity()!.name }}
                    </h4>
                    <p class="text-xs text-slate-500 mt-1">
                      Formatos soportados: <span class="font-mono font-semibold text-slate-700">.csv</span> o <span class="font-mono font-semibold text-slate-700">.xlsx</span>
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
                      class="inline-flex items-center gap-2 px-4 py-2.5 rounded-lg bg-primary-600 hover:bg-primary-700 text-white text-xs font-semibold cursor-pointer transition-colors shadow-2xs">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/>
                      </svg>
                      <span>Seleccionar archivo local</span>
                    </label>
                  </div>

                  <p class="text-[11px] text-slate-400">
                    Archivo sugerido: <code class="font-mono bg-slate-100 px-1.5 py-0.5 rounded text-slate-600">{{ selectedEntity()!.fileName }}</code>
                  </p>
                </div>

                <!-- Archivo seleccionado -->
                @if (drawerSelectedFile()) {
                  <div class="p-4 bg-white border border-slate-200 rounded-xl space-y-3 shadow-2xs">
                    <div class="flex items-center justify-between">
                      <div class="flex items-center gap-3">
                        <div class="p-2 rounded-lg bg-slate-100 border border-slate-200 text-slate-600">
                          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                          </svg>
                        </div>
                        <div>
                          <div class="text-xs font-bold text-slate-900">{{ drawerSelectedFile()!.name }}</div>
                          <div class="text-[11px] font-mono text-slate-400">
                            {{ (drawerSelectedFile()!.size / 1024).toFixed(1) }} KB | {{ drawerSelectedFile()!.type || 'CSV' }}
                          </div>
                        </div>
                      </div>

                      <button 
                        type="button"
                        (click)="drawerSelectedFile.set(null); drawerReport.set(null)"
                        class="text-xs text-red-600 hover:text-red-800 font-semibold">
                        Quitar
                      </button>
                    </div>

                    <div class="flex items-center gap-3 pt-2 border-t border-slate-100">
                      <button 
                        type="button"
                        (click)="processDrawerImport(true)"
                        [disabled]="drawerLoading()"
                        class="px-4 py-2 rounded-lg bg-slate-100 hover:bg-slate-200 text-slate-800 text-xs font-semibold transition-colors disabled:opacity-50">
                        {{ drawerLoading() ? 'Validando...' : '1. Simulación (Validar sin guardar)' }}
                      </button>

                      <button 
                        type="button"
                        (click)="processDrawerImport(false)"
                        [disabled]="drawerLoading()"
                        class="px-4 py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-white text-xs font-semibold transition-colors disabled:opacity-50 shadow-2xs">
                        {{ drawerLoading() ? 'Procesando...' : '2. Guardar en Base de Datos' }}
                      </button>
                    </div>
                  </div>
                }

                <!-- Reporte de la importación individual -->
                @if (drawerReport()) {
                  <div 
                    class="border rounded-xl p-5 space-y-4"
                    [ngClass]="drawerReport()!.errorCount > 0 ? 'bg-amber-50/60 border-amber-200' : 'bg-emerald-50/60 border-emerald-200'">
                    
                    <div class="flex items-center justify-between">
                      <h4 class="text-xs font-bold flex items-center gap-2" [ngClass]="drawerReport()!.errorCount > 0 ? 'text-amber-900' : 'text-emerald-900'">
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
                        <span class="text-xs font-mono px-2 py-0.5 rounded bg-white border border-slate-200 shadow-2xs">
                          Errores: {{ drawerReport()!.errorCount }} | Avisos: {{ drawerReport()!.warningCount }}
                        </span>
                        @if (drawerReport()!.applied) {
                          <button 
                            type="button"
                            (click)="switchTab('data')"
                            class="text-xs font-bold text-emerald-800 underline hover:text-emerald-950">
                            Ver datos actualizados →
                          </button>
                        }
                      </div>
                    </div>

                    @if (drawerReport()!.entities.length > 0) {
                      <div class="bg-white rounded-lg border border-slate-200/80 overflow-hidden">
                        <table class="w-full text-left text-xs">
                          <thead class="bg-slate-50 text-slate-600 text-[11px] uppercase border-b border-slate-200 font-bold">
                            <tr>
                              <th class="p-2.5">Entidad</th>
                              <th class="p-2.5">Filas</th>
                              <th class="p-2.5">Creados</th>
                              <th class="p-2.5">Actualizados</th>
                              <th class="p-2.5">Omitidos</th>
                            </tr>
                          </thead>
                          <tbody class="divide-y divide-slate-100">
                            @for (ent of drawerReport()!.entities; track ent.entity) {
                              <tr>
                                <td class="p-2.5 font-bold text-slate-800">{{ ent.entity }}</td>
                                <td class="p-2.5 font-mono">{{ ent.rowsRead }}</td>
                                <td class="p-2.5 font-mono text-emerald-600 font-semibold">+{{ ent.created }}</td>
                                <td class="p-2.5 font-mono text-blue-600">{{ ent.updated }}</td>
                                <td class="p-2.5 font-mono text-slate-400">{{ ent.skipped }}</td>
                              </tr>
                            }
                          </tbody>
                        </table>
                      </div>
                    }

                    @if (drawerReport()!.issues.length > 0) {
                      <div class="space-y-1.5 text-xs">
                        <h5 class="font-bold text-slate-800">Detalles de incidencias:</h5>
                        <ul class="space-y-1 max-h-36 overflow-y-auto">
                          @for (issue of drawerReport()!.issues; track $index) {
                            <li class="p-2 rounded bg-white border border-slate-200 text-slate-700">
                              <span class="font-bold text-slate-900">[{{ issue.entity }} Fila {{ issue.row }}]:</span>
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
                <div class="flex flex-wrap items-center justify-between gap-3 p-4 bg-primary-50/60 border border-primary-200/60 rounded-xl">
                  <div>
                    <h4 class="text-xs font-bold text-primary-900">
                      Plantilla Oficial: {{ selectedEntity()!.fileName }}
                    </h4>
                    <p class="text-[11px] text-primary-700 mt-0.5">
                      Descarga el archivo o copia los datos de ejemplo para editar en Excel o cualquier hoja de cálculo.
                    </p>
                  </div>

                  <div class="flex items-center gap-2">
                    <button 
                      type="button"
                      (click)="copyTemplate(selectedEntity()!)"
                      class="px-3 py-1.5 text-xs font-semibold rounded-lg bg-white border border-slate-200 text-slate-700 hover:bg-slate-50 transition-colors shadow-2xs flex items-center gap-1.5">
                      @if (copySuccess()) {
                        <svg class="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                        </svg>
                        <span class="text-emerald-700">Copiado</span>
                      } @else {
                        <svg class="w-3.5 h-3.5 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"/>
                        </svg>
                        <span>Copiar Ejemplo</span>
                      }
                    </button>

                    <button 
                      type="button"
                      (click)="downloadTemplate(selectedEntity()!)"
                      class="px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary-600 text-white hover:bg-primary-700 transition-colors shadow-2xs flex items-center gap-1.5">
                      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
                      </svg>
                      <span>Descargar Plantilla CSV</span>
                    </button>
                  </div>
                </div>

                <!-- Tabla de Especificación de Columnas -->
                <div class="space-y-2">
                  <h4 class="text-xs font-bold text-slate-800 uppercase tracking-wider">
                    Estructura y Columnas Requeridas
                  </h4>
                  
                  <div class="border border-slate-200 rounded-xl overflow-hidden bg-white shadow-2xs">
                    <table class="w-full text-left text-xs">
                      <thead class="bg-slate-50 text-slate-600 text-[11px] uppercase border-b border-slate-200 font-bold">
                        <tr>
                          <th class="p-3">Columna</th>
                          <th class="p-3">Requerido</th>
                          <th class="p-3">Tipo</th>
                          <th class="p-3">Descripción</th>
                          <th class="p-3">Ejemplo</th>
                        </tr>
                      </thead>
                      <tbody class="divide-y divide-slate-100">
                        @for (col of selectedEntity()!.columns; track col.name) {
                          <tr class="hover:bg-slate-50/50">
                            <td class="p-3 font-mono font-bold text-primary-700">
                              {{ col.name }}
                            </td>
                            <td class="p-3">
                              @if (col.required) {
                                <span class="px-2 py-0.5 rounded text-[10px] font-semibold bg-red-50 text-red-700 border border-red-200">
                                  Obligatorio
                                </span>
                              } @else {
                                <span class="px-2 py-0.5 rounded text-[10px] font-semibold bg-slate-100 text-slate-500">
                                  Opcional
                                </span>
                              }
                            </td>
                            <td class="p-3 text-slate-600 font-mono text-[11px]">
                              {{ col.type }}
                            </td>
                            <td class="p-3 text-slate-700">
                              {{ col.desc }}
                            </td>
                            <td class="p-3 font-mono text-[11px] text-slate-500">
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
                    <h4 class="text-xs font-bold text-slate-800 uppercase tracking-wider">
                      Vista Previa del Archivo CSV
                    </h4>
                    <span class="text-[11px] text-slate-400">Delimitador: coma (,)</span>
                  </div>

                  <pre class="p-4 bg-slate-900 text-slate-100 text-xs font-mono rounded-xl overflow-x-auto leading-relaxed border border-slate-800">{{ selectedEntity()!.csvTemplate }}</pre>
                </div>
              </div>
            }
          </div>

          <!-- Pie del Drawer -->
          <div class="px-6 py-3 border-t border-slate-200 bg-slate-50 flex items-center justify-between shrink-0">
            <span class="text-[11px] text-slate-400">
              Presiona <kbd class="px-1.5 py-0.5 bg-white border border-slate-300 rounded text-[10px] text-slate-600 font-mono">Esc</kbd> para cerrar
            </span>

            <button 
              type="button"
              (click)="closeDrawer()"
              class="px-4 py-2 text-xs font-semibold rounded-lg border border-slate-300 bg-white text-slate-700 hover:bg-slate-100 transition-colors shadow-2xs">
              Cerrar
            </button>
          </div>
        </aside>
      }
    </div>
  `
})
export class DatosPage {
  @ViewChild('formMapView') formMapView?: MapViewComponent;

  private readonly apiService = inject(ApiService);
  readonly session = inject(AuthSession);

  readonly selectedFiles = signal<File[]>([]);
  readonly loading = signal(false);
  readonly report = signal<ImportReportDto | null>(null);
  readonly importError = signal<string | null>(null);

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
      csvTemplate: `code,name,lat,lon
BOD-UIO,Bodega central Quito (sintética),-0.2295,-78.5243
BOD-SUR,Bodega Sur Distribución,-0.2850,-78.5400`,
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
      csvTemplate: `code,name,lat,lon,region,category,criticality,window_start,window_end,service_minutes,lead_time_days
HOS-001,Hospital Norte (sintético),-0.1700,-78.4800,Pichincha,hospital,alta,07:00,15:00,20,2
CLI-002,Clínica Centro (sintética),-0.2100,-78.5000,Pichincha,clinica,alta,08:00,16:00,15,1
OFI-003,Oficina Corporativa (sintética),-0.1900,-78.4850,Pichincha,oficina,normal,08:00,17:00,10,1`,
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
      csvTemplate: `sku,name,base_unit,weight_per_unit,volume_per_unit
DES-01,Desinfectante hospitalario,L,1.05,0.001
PAP-01,Papel higiénico industrial,UN,0.80,0.004
ALC-70,Alcohol antiséptico 70% 1L,L,0.95,0.0012`,
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
      csvTemplate: `code,depot_code,max_weight,max_volume,cost_per_km
VEH-01,BOD-UIO,1200,8,0.45
VEH-02,BOD-UIO,2500,16,0.70
VEH-03,BOD-SUR,800,5,0.35`,
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
      csvTemplate: `sku,lot_code,product_name,base_unit,expires_on,origin
DES-01,L-DES-2610,Desinfectante hospitalario,L,2027-04-30,Proveedor sintético A
PAP-01,L-PAP-2610,Papel higiénico industrial,UN,2028-12-31,Proveedor sintético B`,
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
      csvTemplate: `location_kind,location_code,sku,lot_code,quantity
warehouse,BOD-UIO,DES-01,L-DES-2610,800
warehouse,BOD-UIO,PAP-01,L-PAP-2610,1500
service_point,HOS-001,DES-01,L-DES-2610,60
service_point,CLI-002,DES-01,L-DES-2610,90`,
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
      csvTemplate: `point_code,sku,date,quantity,period_days
HOS-001,DES-01,2026-10-05,84,7
HOS-001,PAP-01,2026-10-05,70,7
CLI-002,DES-01,2026-10-05,42,7
OFI-003,PAP-01,2026-10-05,35,7`,
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
    const csv = `location_kind,location_code,sku,lot_code,quantity\nwarehouse,${warehouse.code},DES-01,L-DES-2610,500\nwarehouse,${warehouse.code},PAP-01,L-PAP-2610,1200`;
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
    const today = new Date().toISOString().substring(0, 10);
    const csv = `point_code,sku,date,quantity,period_days\n${point.code},DES-01,${today},84,7\n${point.code},PAP-01,${today},70,7`;
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
    }
  }

  async processImport(dryRun: boolean): Promise<void> {
    if (!this.session.canWrite()) return;
    const files = this.selectedFiles();
    if (files.length === 0) return;
    this.importError.set(null);

    // Un .json entre los archivos es el perfil de mapeo del dataset (por ejemplo mapping.json).
    const profiles = files.filter(f => f.name.toLowerCase().endsWith('.json'));
    if (profiles.length > 1) {
      this.importError.set('Seleccione un solo archivo .json de perfil de mapeo.');
      return;
    }

    const formData = new FormData();
    for (const file of files) {
      if (profiles.includes(file)) continue;
      // El servidor reconoce la entidad por el nombre del archivo o por sus columnas.
      formData.append(file.name, file);
    }
    if (profiles.length === 1) formData.append('profile', await profiles[0].text());

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
