import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthSession } from '@entities/session';
import { ApiService, ImportReportDto } from '@shared/api';

interface EntityGuide {
  key: string;
  name: string;
  desc: string;
  fields: string;
}

@Component({
  selector: 'page-datos',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="max-w-4xl mx-auto px-4 py-8 space-y-8">
      <!-- Encabezado -->
      <div>
        <h1 class="text-2xl font-bold text-slate-900 tracking-tight">
          Cargar Datos del Sistema
        </h1>
        <p class="text-sm text-slate-500 mt-1">
          Sube tus archivos CSV o Excel con la información logística. Se procesan en orden automático de dependencias.
        </p>
      </div>

      <!-- Guía de orden de carga -->
      <div class="bg-white border border-slate-200 rounded-xl p-5 space-y-3">
        <h2 class="text-sm font-bold text-slate-800 uppercase tracking-wider">
          Orden requerido de datos (de arriba a abajo)
        </h2>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
          @for (item of entityList; track item.key) {
            <div class="p-3 bg-slate-50 border border-slate-200/60 rounded-lg">
              <div class="font-bold text-slate-900">{{ item.name }}</div>
              <div class="text-slate-500 text-[11px] mt-0.5">{{ item.desc }}</div>
              <div class="text-[10px] text-slate-400 font-mono mt-1">Campos: {{ item.fields }}</div>
            </div>
          }
        </div>
      </div>

      @if (session.canWrite()) {
        <!-- Área de carga -->
        <div class="bg-white border border-slate-200 rounded-xl p-6 space-y-6">
        <div>
          <label class="block text-sm font-semibold text-slate-800 mb-2">
            Seleccionar archivo(s) CSV o XLSX
          </label>
          <input 
            type="file" 
            multiple
            (change)="onFilesSelected($event)"
            accept=".csv,.xlsx"
            class="block w-full text-xs text-slate-500 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-xs file:font-semibold file:bg-primary-50 file:text-primary-700 hover:file:bg-primary-100 cursor-pointer border border-slate-200 rounded-lg p-2" />
          <p class="text-[11px] text-slate-400 mt-1">
            Puedes seleccionar uno o varios archivos a la vez. Los nombres o columnas se detectan automáticamente.
          </p>
        </div>

        @if (selectedFiles().length > 0) {
          <div class="space-y-2">
            <h3 class="text-xs font-semibold text-slate-700">Archivos listos para procesar:</h3>
            <ul class="text-xs space-y-1 divide-y divide-slate-100">
              @for (file of selectedFiles(); track file.name) {
                <li class="py-1.5 flex justify-between text-slate-600">
                  <span>📄 {{ file.name }}</span>
                  <span class="font-mono text-[11px] text-slate-400">{{ (file.size / 1024).toFixed(1) }} KB</span>
                </li>
              }
            </ul>
          </div>

          <div class="flex items-center gap-3 pt-2">
            <button 
              type="button"
              (click)="processImport(true)"
              [disabled]="loading()"
              class="px-4 py-2 rounded-lg bg-slate-100 text-slate-800 hover:bg-slate-200 text-xs font-semibold transition-colors disabled:opacity-50">
              {{ loading() ? 'Validando...' : '1. Validar sin guardar (Simulación)' }}
            </button>

            <button 
              type="button"
              (click)="processImport(false)"
              [disabled]="loading()"
              class="px-4 py-2 rounded-lg bg-primary-600 text-white hover:bg-primary-700 text-xs font-semibold transition-colors disabled:opacity-50">
              {{ loading() ? 'Procesando...' : '2. Guardar en Base de Datos' }}
            </button>
          </div>
        }
        </div>
      } @else {
        <p role="alert" class="rounded-xl border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
          No tienes permiso para importar datos.
        </p>
      }

      <!-- Resultado de la importación -->
      @if (report()) {
        <div 
          class="border rounded-xl p-5 space-y-4"
          [ngClass]="report()!.errorCount > 0 ? 'bg-amber-50/50 border-amber-200' : 'bg-emerald-50/50 border-emerald-200'">
          <div class="flex items-center justify-between">
            <h3 class="text-sm font-bold" [ngClass]="report()!.errorCount > 0 ? 'text-amber-900' : 'text-emerald-900'">
              Resultado: {{ report()!.dryRun ? 'Validación Completada (No guardado)' : (report()!.applied ? 'Guardado Exitosamente' : 'Errores encontrados') }}
            </h3>
            <span class="text-xs font-mono px-2 py-0.5 rounded bg-white border border-slate-200">
              Errores: {{ report()!.errorCount }} | Avisos: {{ report()!.warningCount }}
            </span>
          </div>

          <!-- Entidades procesadas -->
          @if (report()!.entities.length > 0) {
            <div class="bg-white rounded-lg border border-slate-200/80 overflow-hidden">
              <table class="w-full text-left text-xs">
                <thead class="bg-slate-50 text-slate-500 text-[11px] uppercase border-b border-slate-200">
                  <tr>
                    <th class="p-2.5">Entidad</th>
                    <th class="p-2.5">Filas</th>
                    <th class="p-2.5">Creados</th>
                    <th class="p-2.5">Actualizados</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  @for (ent of report()!.entities; track ent.entity) {
                    <tr>
                      <td class="p-2.5 font-bold text-slate-800">{{ ent.entity }}</td>
                      <td class="p-2.5 font-mono">{{ ent.rowsRead }}</td>
                      <td class="p-2.5 font-mono text-emerald-600 font-semibold">+{{ ent.created }}</td>
                      <td class="p-2.5 font-mono text-blue-600">{{ ent.updated }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }

          <!-- Lista de incidencias si las hay -->
          @if (report()!.issues.length > 0) {
            <div class="space-y-1 text-xs">
              <h4 class="font-bold text-slate-800">Detalles e incidencias:</h4>
              <ul class="space-y-1 max-h-48 overflow-y-auto">
                @for (issue of report()!.issues; track $index) {
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
  `
})
export class DatosPage {
  private readonly apiService = inject(ApiService);
  readonly session = inject(AuthSession);

  readonly selectedFiles = signal<File[]>([]);
  readonly loading = signal(false);
  readonly report = signal<ImportReportDto | null>(null);

  readonly entityList: EntityGuide[] = [
    { key: 'warehouses', name: '1. Bodegas (warehouses.csv)', desc: 'Puntos de partida y origen de los despachos.', fields: 'code, name, lat, lon' },
    { key: 'service_points', name: '2. Hospitales (service_points.csv)', desc: 'Puntos de servicio de destino.', fields: 'code, name, lat, lon' },
    { key: 'products', name: '3. Productos (products.csv)', desc: 'Catálogo de insumos médicos y de limpieza.', fields: 'sku, name, unit' },
    { key: 'vehicles', name: '4. Vehículos (vehicles.csv)', desc: 'Flota de transporte y sus capacidades.', fields: 'code, capacity_kg, capacity_m3' },
    { key: 'lots', name: '5. Lotes (lots.csv)', desc: 'Trazabilidad de fabricación y caducidad.', fields: 'sku, lot_code, expires_at' },
    { key: 'stock', name: '6. Inventario (stock.csv)', desc: 'Cantidad disponible por bodega o punto.', fields: 'location_code, sku, lot_code, quantity' },
    { key: 'consumptions', name: '7. Consumos (consumptions.csv)', desc: 'Historial de uso para proyectar quiebres.', fields: 'point_code, sku, date, quantity' }
  ];

  onFilesSelected(event: Event): void {
    if (!this.session.canWrite()) return;
    const input = event.target as HTMLInputElement;
    if (input.files) {
      this.selectedFiles.set(Array.from(input.files));
      this.report.set(null);
    }
  }

  processImport(dryRun: boolean): void {
    if (!this.session.canWrite()) return;
    const files = this.selectedFiles();
    if (files.length === 0) return;

    const formData = new FormData();
    for (const file of files) {
      // Usamos el nombre del archivo para que el backend detecte la entidad
      formData.append(file.name, file);
    }

    this.loading.set(true);
    this.apiService.importData(formData, dryRun).subscribe(res => {
      this.loading.set(false);
      this.report.set(res);
    });
  }
}
