import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthSession } from '@entities/session';
import { ApiService, RiskItemDto } from '@shared/api';
import { AlertNotificationService } from '@features/alert-notification';

type FilterType = 'ALL' | 'CRITICAL' | 'WARNING' | 'NORMAL';

@Component({
  selector: 'page-alertas',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="max-w-7xl mx-auto px-4 py-8 space-y-6 font-sans text-[#30313D]">
      <!-- Encabezado Principal Estilo Stripe -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[#E3E8EE] pb-5">
        <div>
          <div class="flex items-center gap-2.5">
            <h1 class="text-[22px] font-semibold text-[#1A2C44] tracking-tight">
              Monitoreo y Alertas de Inventario
            </h1>
            <span class="text-[11px] font-medium px-2 py-0.5 rounded-md bg-[#F8FAFC] text-[#697386] border border-[#E3E8EE]">
              Auditoría y Supervisión
            </span>
          </div>
          <p class="text-[13px] text-[#3C4F69] mt-0.5">
            Control de inventario en tiempo real, alerta temprana de desabastecimiento hospitalario y sugerencias automáticas de reposición.
          </p>
        </div>

        <!-- Acciones de Cabecera -->
        <div class="flex items-center gap-2.5">
          <button
            type="button"
            (click)="refreshData()"
            [disabled]="loading()"
            class="inline-flex items-center gap-1.5 h-9 px-3.5 text-[13px] font-medium rounded-md bg-white border border-[#E3E8EE] text-[#30313D] hover:bg-[#F8FAFC] transition-colors shadow-xs disabled:opacity-50 cursor-pointer">
            @if (loading()) {
              <span class="inline-block animate-spin h-3.5 w-3.5 border-2 border-[#635BFF] border-t-transparent rounded-full"></span>
            }
            <span>{{ loading() ? 'Actualizando...' : 'Actualizar Análisis' }}</span>
          </button>

          <a
            routerLink="/rutas"
            class="inline-flex items-center h-9 px-4 text-[13px] font-medium rounded-md bg-[#635BFF] hover:bg-[#5248E0] text-white transition-colors shadow-xs cursor-pointer">
            <span>Planificar Rutas de Entrega</span>
          </a>
        </div>
      </div>

      <!-- Tarjetas de Métricas Resumen (KPIs) -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3.5">
        <!-- Puntos Críticos -->
        <div class="p-4 rounded-lg bg-white border border-[#E3E8EE] shadow-xs space-y-1.5">
          <div class="flex items-center justify-between">
            <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Stock Crítico</span>
            <span class="text-[10px] font-medium px-2 py-0.5 rounded bg-rose-50 text-rose-700 border border-rose-200">
              Atención inmediata
            </span>
          </div>
          <div class="mt-2 flex items-baseline gap-2">
            <span class="text-[24px] font-bold text-rose-600 tracking-tight">{{ criticalCount() }}</span>
            <span class="text-[12px] text-[#8792A2]">ítems en riesgo</span>
          </div>
          <p class="text-[12px] text-[#4F5B66]">
            El inventario disponible no alcanza para cubrir los días de reposición.
          </p>
        </div>

        <!-- Puntos en Observación -->
        <div class="p-4 rounded-lg bg-white border border-[#E3E8EE] shadow-xs space-y-1.5">
          <div class="flex items-center justify-between">
            <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">En Observación</span>
            <span class="text-[10px] font-medium px-2 py-0.5 rounded bg-amber-50 text-amber-700 border border-amber-200">
              Margen de reserva
            </span>
          </div>
          <div class="mt-2 flex items-baseline gap-2">
            <span class="text-[24px] font-bold text-amber-600 tracking-tight">{{ warningCount() }}</span>
            <span class="text-[12px] text-[#8792A2]">ítems preventivos</span>
          </div>
          <p class="text-[12px] text-[#4F5B66]">
            Cobertura ajustada dentro de la ventana de seguridad.
          </p>
        </div>

        <!-- Cobertura Promedio -->
        <div class="p-4 rounded-lg bg-white border border-[#E3E8EE] shadow-xs space-y-1.5">
          <div class="flex items-center justify-between">
            <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Cobertura Promedio</span>
            <span class="text-[10px] font-medium px-2 py-0.5 rounded bg-[#F8FAFC] text-[#4F5B66] border border-[#E3E8EE]">
              Red general
            </span>
          </div>
          <div class="mt-2 flex items-baseline gap-2">
            <span class="text-[24px] font-bold text-[#1A2C44] tracking-tight">{{ averageCoverage().toFixed(1) }}</span>
            <span class="text-[12px] text-[#8792A2]">días promedio</span>
          </div>
          <p class="text-[12px] text-[#4F5B66]">
            Calculado sobre los centros de salud con consumo activo registrado.
          </p>
        </div>

        <!-- Volumen Sugerido -->
        <div class="p-4 rounded-lg bg-white border border-[#E3E8EE] shadow-xs space-y-1.5">
          <div class="flex items-center justify-between">
            <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Reposición Sugerida</span>
            <span class="text-[10px] font-medium px-2 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-200">
              Cálculo sugerido
            </span>
          </div>
          <div class="mt-2 flex items-baseline gap-2">
            <span class="text-[24px] font-bold text-[#635BFF] tracking-tight">{{ totalSuggested() | number }}</span>
            <span class="text-[12px] text-[#8792A2]">unidades requeridas</span>
          </div>
          <p class="text-[12px] text-[#4F5B66]">
            Cantidad estimada para restablecer el stock de seguridad en toda la red.
          </p>
        </div>
      </div>

      <!-- Barra de Segmentación por Pestañas Estilo Stripe (Patrón 3) -->
      <div class="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 border-b border-[#E3E8EE]">
        <div class="flex items-center gap-6 overflow-x-auto no-scrollbar">
          <button 
            type="button"
            (click)="setFilter('ALL')"
            [ngClass]="currentFilter() === 'ALL'
              ? 'border-[#635BFF] text-[#635BFF] border-b-2 font-semibold -mb-px'
              : 'border-transparent text-[#697386] hover:text-[#1A2C44] border-b-2 font-medium'"
            class="flex items-center gap-2 py-3 text-[13px] transition-colors cursor-pointer whitespace-nowrap">
            <span>Todas las Alertas</span>
            <span 
              class="text-[11px] px-2 py-0.5 rounded-full font-semibold transition-colors"
              [ngClass]="currentFilter() === 'ALL' ? 'bg-[#635BFF]/10 text-[#635BFF]' : 'bg-[#F1F3F5] text-[#697386]'">
              {{ allItems().length }}
            </span>
          </button>

          <button 
            type="button"
            (click)="setFilter('CRITICAL')"
            [ngClass]="currentFilter() === 'CRITICAL'
              ? 'border-[#635BFF] text-[#635BFF] border-b-2 font-semibold -mb-px'
              : 'border-transparent text-[#697386] hover:text-[#1A2C44] border-b-2 font-medium'"
            class="flex items-center gap-2 py-3 text-[13px] transition-colors cursor-pointer whitespace-nowrap">
            <span>Quiebres Críticos</span>
            <span class="text-[11px] px-2 py-0.5 rounded-full bg-rose-50 text-rose-700 border border-rose-200 font-semibold">
              {{ criticalCount() }}
            </span>
          </button>

          <button 
            type="button"
            (click)="setFilter('WARNING')"
            [ngClass]="currentFilter() === 'WARNING'
              ? 'border-[#635BFF] text-[#635BFF] border-b-2 font-semibold -mb-px'
              : 'border-transparent text-[#697386] hover:text-[#1A2C44] border-b-2 font-medium'"
            class="flex items-center gap-2 py-3 text-[13px] transition-colors cursor-pointer whitespace-nowrap">
            <span>Margen Preventivo</span>
            <span class="text-[11px] px-2 py-0.5 rounded-full bg-amber-50 text-amber-700 border border-amber-200 font-semibold">
              {{ warningCount() }}
            </span>
          </button>

          <button 
            type="button"
            (click)="setFilter('NORMAL')"
            [ngClass]="currentFilter() === 'NORMAL'
              ? 'border-[#635BFF] text-[#635BFF] border-b-2 font-semibold -mb-px'
              : 'border-transparent text-[#697386] hover:text-[#1A2C44] border-b-2 font-medium'"
            class="flex items-center gap-2 py-3 text-[13px] transition-colors cursor-pointer whitespace-nowrap">
            <span>Conformes</span>
            <span class="text-[11px] px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200 font-semibold">
              {{ normalCount() }}
            </span>
          </button>
        </div>

        <div class="w-full sm:w-64 pb-2 sm:pb-0">
          <div class="relative">
            <span class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-[#8792A2]">
              <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </span>
            <input
              type="text"
              [(ngModel)]="searchQuery"
              placeholder="Buscar por hospital o SKU..."
              class="w-full h-9 pl-9 pr-3 text-[13px] rounded-md border border-[#E3E8EE] focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] text-[#1A2C44] placeholder-[#8792A2] bg-white transition-all shadow-2xs" />
          </div>
        </div>
      </div>

      <!-- Estado cargando -->
      @if (loading()) {
        <div class="p-12 text-center text-[#8792A2] text-[13px] bg-white border border-[#E3E8EE] rounded-lg shadow-xs space-y-2">
          <div class="font-semibold text-[#1A2C44]">Calculando balance de inventario y demanda...</div>
          <div class="text-[12px]">Analizando existencias en almacenes, consumos históricos y órdenes en camino.</div>
        </div>
      }

      <!-- Estado vacío -->
      @if (!loading() && allItems().length === 0) {
        <div class="p-10 text-center bg-white border border-[#E3E8EE] rounded-lg shadow-xs space-y-3">
          <div class="w-10 h-10 mx-auto rounded-full bg-[#F8FAFC] border border-[#E3E8EE] text-[#8792A2] flex items-center justify-center font-bold text-sm">
            i
          </div>
          <p class="text-[#1A2C44] text-[14px] font-semibold">
            No se han registrado consumos o inventario aún
          </p>
          <p class="text-[13px] text-[#8792A2] max-w-md mx-auto">
            Para evaluar alertas y niveles de abastecimiento, carga centros de salud con consumos e inventario inicial en la sección de datos.
          </p>
          @if (session.canWrite()) {
            <div class="pt-2">
              <a
                routerLink="/datos"
                class="inline-flex items-center h-9 px-4 text-[13px] font-medium rounded-md bg-[#635BFF] hover:bg-[#5248E0] text-white transition-colors shadow-xs">
                Cargar Datos de Inventario
              </a>
            </div>
          }
        </div>
      }

      <!-- Tabla Principal de Auditoría -->
      @if (!loading() && filteredItems().length > 0) {
        <div class="bg-white border border-[#E3E8EE] rounded-lg overflow-hidden shadow-xs">
          <div class="overflow-x-auto">
            <table class="w-full text-left text-[13px] text-[#30313D] min-w-[750px]">
              <thead class="bg-[#F8FAFC] text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider border-b border-[#E3E8EE]">
                <tr>
                  <th class="py-3 px-4">Centro de Salud</th>
                  <th class="py-3 px-4">Medicamento / SKU</th>
                  <th class="py-3 px-4 text-right">Disponible</th>
                  <th class="py-3 px-4 text-right">En Camino</th>
                  <th class="py-3 px-4 text-right">Consumo Diario</th>
                  <th class="py-3 px-4 text-right">Días Cobertura</th>
                  <th class="py-3 px-4 text-center">Nivel de Riesgo</th>
                  <th class="py-3 px-4 text-right">Sugerido</th>
                  <th class="py-3 px-4 text-center">Detalle</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-[#F1F3F5]">
                @for (item of filteredItems(); track item.servicePointCode + item.sku) {
                  <tr class="hover:bg-[#F8FAFC] transition-colors" [class.bg-rose-50/20]="item.level === 'Critico' || item.level === 'Critical'">
                    <!-- Punto / Hospital -->
                    <td class="py-3 px-4">
                      <span class="font-medium text-[#1A2C44] block">{{ item.servicePointCode }}</span>
                      @if (item.criticality) {
                        <span class="text-[11px] text-[#8792A2]">Prioridad: {{ item.criticality }}</span>
                      }
                    </td>

                    <!-- SKU -->
                    <td class="py-3 px-4 font-mono text-[12px] text-[#30313D]">
                      <div class="font-medium">{{ item.sku }}</div>
                      @if (item.baseUnit) {
                        <span class="text-[11px] text-[#8792A2] font-sans">{{ item.baseUnit }}</span>
                      }
                    </td>

                    <!-- Disponible -->
                    <td class="py-3 px-4 text-right font-medium text-[#1A2C44]">
                      {{ item.availableQuantity | number }}
                    </td>

                    <!-- En Tránsito -->
                    <td class="py-3 px-4 text-right text-[#4F5B66]">
                      {{ (item.inTransitQuantity || 0) | number }}
                    </td>

                    <!-- Demanda Diaria -->
                    <td class="py-3 px-4 text-right text-[#4F5B66]">
                      {{ item.dailyDemand ? (item.dailyDemand.toFixed(1) + ' /día') : 'Sin registro' }}
                    </td>

                    <!-- Cobertura -->
                    <td class="py-3 px-4 text-right font-semibold" [ngClass]="{
                      'text-rose-600': item.level === 'Critico' || item.level === 'Critical' || item.level === 'Alto' || item.level === 'High',
                      'text-amber-600': item.level === 'Vigilar' || item.level === 'Medium',
                      'text-emerald-700': item.level === 'Normal' || item.level === 'Low',
                      'text-[#8792A2]': !item.coverageDays
                    }">
                      {{ item.coverageDays != null ? (item.coverageDays.toFixed(1) + ' días') : 'N/D' }}
                    </td>

                    <!-- Nivel de Riesgo (Badge Stripe Limpio) -->
                    <td class="py-3 px-4 text-center">
                      <span 
                        class="inline-flex px-2 py-0.5 rounded text-[11px] font-medium"
                        [ngClass]="getRiskBadgeClass(item.level)">
                        {{ item.level }}
                      </span>
                    </td>

                    <!-- Reabastecimiento Sugerido -->
                    <td class="py-3 px-4 text-right font-medium" [ngClass]="item.suggestedQuantity > 0 ? 'text-[#635BFF]' : 'text-[#8792A2]'">
                      {{ item.suggestedQuantity > 0 ? ('+' + (item.suggestedQuantity | number)) : '0' }}
                    </td>

                    <!-- Botón de Inspección / Trazabilidad -->
                    <td class="py-3 px-4 text-center">
                      <button
                        type="button"
                        (click)="inspectItem(item)"
                        class="text-[12px] font-medium text-[#635BFF] hover:underline cursor-pointer">
                        Ver detalle
                      </button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      <!-- Modal de Detalle / Trazabilidad de Auditoría (Estilo Stripe Modal) -->
      @if (selectedAuditItem()) {
        <div 
          class="fixed inset-0 z-[100] !m-0 flex items-center justify-center p-3 sm:p-4 bg-slate-900/50 backdrop-blur-xs transition-opacity duration-200"
          role="dialog"
          aria-modal="true"
          (click)="selectedAuditItem.set(null)">
          <div 
            class="bg-white rounded-lg shadow-xl border border-[#E3E8EE] max-w-lg w-full overflow-hidden"
            (click)="$event.stopPropagation()">
            <!-- Header Modal -->
            <div class="bg-white px-5 py-4 flex items-center justify-between border-b border-[#E3E8EE]">
              <div>
                <span class="text-[11px] font-semibold uppercase tracking-wider text-[#635BFF]">Expediente de Auditoría</span>
                <h3 class="text-[15px] font-bold text-[#1A2C44] mt-0.5">
                  {{ selectedAuditItem()!.servicePointCode }} &mdash; {{ selectedAuditItem()!.sku }}
                </h3>
              </div>
              <button
                type="button"
                (click)="selectedAuditItem.set(null)"
                class="w-7 h-7 rounded-md hover:bg-[#F8FAFC] flex items-center justify-center text-[#8792A2] hover:text-[#1A2C44] transition-colors cursor-pointer"
                title="Cerrar">
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <div class="p-5 space-y-4 text-[13px]">
              <!-- Desglose de Parámetros -->
              <div class="grid grid-cols-2 gap-2.5 bg-[#F8FAFC] p-3.5 rounded-lg border border-[#E3E8EE]">
                <div>
                  <span class="text-[11px] text-[#8792A2] block">Stock Disponible en Sitio</span>
                  <span class="font-bold text-[#1A2C44] text-[15px]">{{ selectedAuditItem()!.availableQuantity }} u</span>
                </div>
                <div>
                  <span class="text-[11px] text-[#8792A2] block">En Camino (Despachos)</span>
                  <span class="font-bold text-[#1A2C44] text-[15px]">{{ selectedAuditItem()!.inTransitQuantity || 0 }} u</span>
                </div>
                <div>
                  <span class="text-[11px] text-[#8792A2] block">Consumo Diario Estimado</span>
                  <span class="font-bold text-[#1A2C44] text-[15px]">{{ selectedAuditItem()!.dailyDemand != null ? (selectedAuditItem()!.dailyDemand.toFixed(1) + ' /día') : '0.0 /día' }}</span>
                </div>
                <div>
                  <span class="text-[11px] text-[#8792A2] block">Días de Cobertura Reales</span>
                  <span class="font-bold text-[15px]" [ngClass]="selectedAuditItem()!.coverageDays <= selectedAuditItem()!.leadTimeDays ? 'text-rose-600' : 'text-[#1A2C44]'">
                    {{ selectedAuditItem()!.coverageDays != null ? (selectedAuditItem()!.coverageDays.toFixed(1) + ' días') : '0.0 días' }}
                  </span>
                </div>
              </div>

              <!-- Umbrales de Suministro del Centro de Salud -->
              <div class="border border-[#E3E8EE] rounded-lg p-3.5 space-y-2">
                <span class="font-semibold text-[#1A2C44] block text-[11px] uppercase tracking-wider">
                  Parámetros de Reposición y Tiempos de Entrega
                </span>
                <div class="space-y-1.5 text-[#4F5B66] text-[12px]">
                  <div class="flex justify-between">
                    <span>Tiempo de entrega del proveedor (Lead Time):</span>
                    <span class="font-medium text-[#1A2C44]">{{ selectedAuditItem()!.leadTimeDays }} días</span>
                  </div>
                  <div class="flex justify-between">
                    <span>Margen de seguridad preventivo (Buffer):</span>
                    <span class="font-medium text-[#1A2C44]">{{ selectedAuditItem()!.safetyMarginDays }} días</span>
                  </div>
                  <div class="flex justify-between border-t border-[#E3E8EE] pt-1.5 font-medium">
                    <span class="text-rose-700">Límite Crítico (Ruptura de inventario):</span>
                    <span class="text-rose-600 font-bold">&le; {{ selectedAuditItem()!.leadTimeDays }} días</span>
                  </div>
                  <div class="flex justify-between font-medium">
                    <span class="text-amber-700">Límite Preventivo (Observación):</span>
                    <span class="text-amber-600 font-bold">&le; {{ selectedAuditItem()!.leadTimeDays + selectedAuditItem()!.safetyMarginDays }} días</span>
                  </div>
                </div>
              </div>

              <!-- Justificación Técnica / Conclusión -->
              <div class="bg-[#F8FAFC] border border-[#E3E8EE] rounded-lg p-3.5 space-y-1">
                <span class="font-semibold text-[#1A2C44] block text-[11px] uppercase tracking-wider">
                  Evaluación del Sistema
                </span>
                <p class="text-[12px] leading-relaxed text-[#4F5B66]">
                  @if (selectedAuditItem()!.level === 'Critico' || selectedAuditItem()!.level === 'Critical') {
                    El inventario actual no alcanza a cubrir el tiempo de entrega del proveedor. Se requiere un despacho prioritario urgente de {{ selectedAuditItem()!.suggestedQuantity }} unidades.
                  } @else if (selectedAuditItem()!.level === 'Alto' || selectedAuditItem()!.level === 'High' || selectedAuditItem()!.level === 'Vigilar') {
                    El inventario cubre el tiempo de entrega pero ha ingresado en el margen de seguridad de reserva. Se sugiere programar el reabastecimiento en la siguiente ruta planificada.
                  } @else {
                    El nivel de inventario cumple satisfactoriamente con los días de cobertura objetivo. No se requiere intervención emergente.
                  }
                </p>
              </div>
            </div>

            <!-- Footer Modal -->
            <div class="bg-[#F8FAFC] px-5 py-3 border-t border-[#E3E8EE] flex justify-end">
              <button
                type="button"
                (click)="selectedAuditItem.set(null)"
                class="h-9 px-4 text-[13px] font-medium rounded-md bg-[#635BFF] text-white hover:bg-[#5248E0] transition-colors shadow-xs cursor-pointer">
                Entendido
              </button>
            </div>
          </div>
        </div>
      }
    </div>
  `
})
export class AlertasPage implements OnInit {
  private readonly apiService = inject(ApiService);
  readonly session = inject(AuthSession);
  private readonly alertNotification = inject(AlertNotificationService, { optional: true });

  readonly allItems = signal<RiskItemDto[]>([]);
  readonly loading = signal(true);
  readonly currentFilter = signal<FilterType>('ALL');
  searchQuery = '';
  readonly selectedAuditItem = signal<RiskItemDto | null>(null);

  readonly criticalCount = computed(() => {
    return this.allItems().filter(i => i.level === 'Critico' || i.level === 'Critical').length;
  });

  readonly warningCount = computed(() => {
    return this.allItems().filter(i => i.level === 'Alto' || i.level === 'High' || i.level === 'Vigilar' || i.level === 'Medium').length;
  });

  readonly normalCount = computed(() => {
    return this.allItems().filter(i => i.level === 'Normal' || i.level === 'Low').length;
  });

  readonly averageCoverage = computed(() => {
    const valid = this.allItems().filter(i => i.coverageDays != null && i.coverageDays > 0);
    if (valid.length === 0) return 0;
    const sum = valid.reduce((acc, cur) => acc + (cur.coverageDays || 0), 0);
    return sum / valid.length;
  });

  readonly totalSuggested = computed(() => {
    return this.allItems().reduce((acc, cur) => acc + (cur.suggestedQuantity || 0), 0);
  });

  readonly filteredItems = computed(() => {
    let items = this.allItems();

    // Filtro por estado
    if (this.currentFilter() === 'CRITICAL') {
      items = items.filter(i => i.level === 'Critico' || i.level === 'Critical');
    } else if (this.currentFilter() === 'WARNING') {
      items = items.filter(i => i.level === 'Alto' || i.level === 'High' || i.level === 'Vigilar' || i.level === 'Medium');
    } else if (this.currentFilter() === 'NORMAL') {
      items = items.filter(i => i.level === 'Normal' || i.level === 'Low');
    }

    // Filtro por texto de búsqueda
    const q = this.searchQuery.trim().toLowerCase();
    if (q) {
      items = items.filter(i =>
        i.servicePointCode.toLowerCase().includes(q) ||
        i.sku.toLowerCase().includes(q)
      );
    }

    return items;
  });

  ngOnInit(): void {
    this.fetchRisks(false);
  }

  setFilter(filter: FilterType): void {
    this.currentFilter.set(filter);
  }

  refreshData(): void {
    this.alertNotification?.checkRisks();
    this.fetchRisks(true);
  }

  inspectItem(item: RiskItemDto): void {
    this.selectedAuditItem.set(item);
  }

  getRiskBadgeClass(level: string): string {
    switch (level) {
      case 'Critico':
      case 'Critical':
        return 'bg-rose-50 text-rose-700 border border-rose-200';
      case 'Alto':
      case 'High':
        return 'bg-orange-50 text-orange-700 border border-orange-200';
      case 'Vigilar':
      case 'Medium':
        return 'bg-amber-50 text-amber-700 border border-amber-200';
      case 'Normal':
      case 'Low':
        return 'bg-emerald-50 text-emerald-700 border border-emerald-200';
      default:
        return 'bg-slate-50 text-slate-600 border border-slate-200';
    }
  }

  private fetchRisks(refresh: boolean): void {
    this.loading.set(true);
    this.apiService.getRisks(undefined, refresh).subscribe(res => {
      this.loading.set(false);
      if (res && res.items) {
        this.allItems.set(res.items);
      } else {
        this.allItems.set([]);
      }
    });
  }
}
