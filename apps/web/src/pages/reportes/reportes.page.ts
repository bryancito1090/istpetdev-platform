import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthSession } from '@entities/session';

export type ReportPeriod = 'today' | 'week' | 'month' | 'quarter';
export type ReportZone = 'ALL' | 'NORTE' | 'CENTRO' | 'SUR' | 'VALLES';
export type ReportCategory = 'ALL' | 'COLD_CHAIN' | 'ESSENTIALS' | 'EMERGENCY';
export type ReportTab = 'consolidated' | 'fleet' | 'inventory' | 'returns';

export interface DispatchRow {
  id: string;
  code: string;
  destination: string;
  zone: 'NORTE' | 'CENTRO' | 'SUR' | 'VALLES';
  zoneLabel: string;
  category: 'COLD_CHAIN' | 'ESSENTIALS' | 'EMERGENCY';
  categoryLabel: string;
  scheduledTime: string;
  deliveredTime: string;
  unitsPlanned: number;
  unitsDelivered: number;
  slaPercent: number;
  coldChainTemp: string;
  coldChainStatus: 'optimal' | 'warning' | 'normal';
  transportCost: number;
  distanceKm: number;
  vehicleCode: string;
  driverName: string;
  status: 'DELIVERED' | 'IN_TRANSIT' | 'EXCEPTION';
  statusLabel: string;
}

export interface VehicleReportItem {
  code: string;
  plate: string;
  model: string;
  type: string;
  routesCount: number;
  totalKm: number;
  totalCost: number;
  costPerKm: number;
  weightCapacityKg: number;
  weightUsedKg: number;
  weightUtilization: number;
  volumeCapacityM3: number;
  volumeUsedM3: number;
  volumeUtilization: number;
  onTimeRate: number;
  status: 'ACTIVE' | 'MAINTENANCE';
}

export interface HospitalInventoryItem {
  code: string;
  name: string;
  zone: string;
  coverageDays: number;
  stockStatus: 'optimal' | 'safe' | 'low' | 'critical';
  stockStatusLabel: string;
  criticalItemsCount: number;
  dailyConsumption: number;
  suggestedRestock: number;
  lastDelivery: string;
  avoidedStockouts: number;
}

export interface ReturnIncidentItem {
  guideNumber: string;
  date: string;
  originHospital: string;
  reason: 'Rotura de Frío' | 'Daño de Empaque' | 'Sobrante por Alta' | 'Error de Rotulado';
  sku: string;
  productName: string;
  units: number;
  custodyStatus: 'Recibido en Bodega' | 'En Custodia Conductor' | 'Auditoría en Curso';
  reconciled: boolean;
}

@Component({
  selector: 'page-reportes',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8 font-sans text-[#30313D] bg-white">
      
      <!-- ================= ENCABEZADO INSTITUCIONAL IMPRESIÓN (visible solo al imprimir) ================= -->
      <div class="hidden print:block border-b-2 border-[#1A2C44] pb-4 mb-6">
        <div class="flex items-center justify-between">
          <div>
            <div class="text-[11px] font-bold uppercase tracking-wider text-[#635BFF]">
              Instituto Superior Tecnológico Policía Nacional — ISTPET
            </div>
            <h1 class="text-xl font-bold text-[#1A2C44] mt-1">
              Informe Oficial de Desempeño Logístico y Abastecimiento Hospitalario
            </h1>
            <p class="text-[11px] text-[#4F5B66] mt-0.5">
              Red Metropolitana de Salud Quito • Operaciones de Transporte, Cadena de Frío e Inventarios
            </p>
          </div>
          <div class="text-right text-[11px] text-[#697386]">
            <div><span class="font-bold text-[#1A2C44]">Folio:</span> IST-REP-2026-{{ folioNumber }}</div>
            <div><span class="font-bold text-[#1A2C44]">Fecha de Emisión:</span> {{ generationDate }}</div>
            <div><span class="font-bold text-[#1A2C44]">Generado por:</span> {{ currentUserName }} ({{ currentUserRole }})</div>
          </div>
        </div>
        <div class="mt-3 pt-2 border-t border-slate-200 text-[10px] text-slate-500 flex justify-between">
          <span>Filtro Período: {{ periodLabel() }}</span>
          <span>Zona Seleccionada: {{ zoneLabel() }}</span>
          <span>Categoría: {{ categoryLabel() }}</span>
          <span>Validez: Certificación Operativa Reto 1</span>
        </div>
      </div>

      <!-- ================= BARRA SUPERIOR PANTALLA (oculta en impresión) ================= -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-[#E3E8EE] pb-5 print:hidden">
        <div>
          <div class="flex items-center gap-2.5">
            <h1 class="text-2xl sm:text-3xl font-bold text-[#1A2C44] tracking-tight">
              Reportes & Analítica Logística
            </h1>
            <span class="text-[11px] font-semibold px-2.5 py-0.5 rounded-full bg-[#635BFF]/10 text-[#635BFF] border border-[#635BFF]/30 font-mono">
              Consolidado Oficial
            </span>
          </div>
          <p class="text-xs sm:text-[13px] text-[#4F5B66] mt-1">
            Indicadores ejecutivos de nivel de servicio (OTIF), eficiencia de flota, monitoreo térmico de biológicos y cobertura hospitalaria.
          </p>
        </div>

        <!-- Acciones Principales de Descarga y Exportación -->
        <div class="flex flex-wrap items-center gap-2">
          <!-- Botón Imprimir / PDF -->
          <button
            type="button"
            (click)="printReport()"
            class="inline-flex items-center gap-1.5 h-9 px-3.5 text-xs font-semibold rounded-md bg-[#1A2C44] hover:bg-[#2A3F5E] text-white transition-colors shadow-2xs cursor-pointer"
            title="Imprimir o Guardar como PDF en formato institucional">
            <svg class="w-4 h-4 text-slate-200" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
            </svg>
            <span>Imprimir / PDF</span>
          </button>

          <!-- Botón Exportar CSV -->
          <button
            type="button"
            (click)="exportCsv()"
            class="inline-flex items-center gap-1.5 h-9 px-3.5 text-xs font-medium rounded-md bg-white border border-[#E3E8EE] text-[#1A2C44] hover:bg-[#F8FAFC] transition-colors shadow-2xs cursor-pointer"
            title="Descargar datos en formato CSV para análisis en Excel">
            <svg class="w-4 h-4 text-[#00875A]" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
            <span>Descargar CSV</span>
          </button>

          <!-- Botón Exportar JSON -->
          <button
            type="button"
            (click)="exportJson()"
            class="inline-flex items-center gap-1.5 h-9 px-3 text-xs font-medium rounded-md bg-white border border-[#E3E8EE] text-[#4F5B66] hover:bg-[#F8FAFC] transition-colors shadow-2xs cursor-pointer"
            title="Exportar estructura completa en JSON">
            <svg class="w-3.5 h-3.5 text-[#635BFF]" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" />
            </svg>
            <span>JSON</span>
          </button>
        </div>
      </div>

      <!-- ================= FILTROS Y CONTROLES VISUALES (ocultos en impresión) ================= -->
      <div class="bg-[#F8FAFC] border border-[#E3E8EE] rounded-lg p-4 space-y-4 print:hidden">
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
          
          <!-- Filtro Período -->
          <div>
            <label class="block text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider mb-1.5">
              Período Evaluado
            </label>
            <select
              [ngModel]="period()"
              (ngModelChange)="period.set($event)"
              class="w-full h-8 px-2.5 bg-white border border-[#CBD5E1] rounded-md text-xs text-[#1A2C44] focus:outline-none focus:border-[#635BFF] transition-colors">
              <option value="today">Hoy (Jornada Activa - 17 Oct)</option>
              <option value="week">Últimos 7 Días (Campaña)</option>
              <option value="month">Mes en Curso (Octubre 2026)</option>
              <option value="quarter">Trimestre Q3/Q4 Consolidado</option>
            </select>
          </div>

          <!-- Filtro Zona -->
          <div>
            <label class="block text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider mb-1.5">
              Zona Hospitalaria
            </label>
            <select
              [ngModel]="selectedZone()"
              (ngModelChange)="selectedZone.set($event)"
              class="w-full h-8 px-2.5 bg-white border border-[#CBD5E1] rounded-md text-xs text-[#1A2C44] focus:outline-none focus:border-[#635BFF] transition-colors">
              <option value="ALL">Todas las Zonas (Distrito Metropolitano)</option>
              <option value="NORTE">Zona Norte (Calderón / Carcelén)</option>
              <option value="CENTRO">Zona Centro (Eugenio Espejo / Baca Ortiz)</option>
              <option value="SUR">Zona Sur (Enrique Garcés / Guamaní)</option>
              <option value="VALLES">Valles (Tumbaco / Cumbayá / Chillos)</option>
            </select>
          </div>

          <!-- Filtro Categoría de Suministro -->
          <div>
            <label class="block text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider mb-1.5">
              Categoría Médica
            </label>
            <select
              [ngModel]="selectedCategory()"
              (ngModelChange)="selectedCategory.set($event)"
              class="w-full h-8 px-2.5 bg-white border border-[#CBD5E1] rounded-md text-xs text-[#1A2C44] focus:outline-none focus:border-[#635BFF] transition-colors">
              <option value="ALL">Todas las Categorías</option>
              <option value="COLD_CHAIN">Cadena de Frío (Vacunas / Biológicos 2-8°C)</option>
              <option value="ESSENTIALS">Fármacos Esenciales de Uso Diario</option>
              <option value="EMERGENCY">Insumos Críticos de Emergencia</option>
            </select>
          </div>

          <!-- Buscador en tiempo real -->
          <div>
            <label class="block text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider mb-1.5">
              Búsqueda Rápida
            </label>
            <div class="relative">
              <input
                type="text"
                [ngModel]="searchQuery()"
                (ngModelChange)="searchQuery.set($event)"
                placeholder="Hospital, despacho o vehículo..."
                class="w-full h-8 pl-8 pr-2.5 bg-white border border-[#CBD5E1] rounded-md text-xs text-[#1A2C44] placeholder-[#8792A2] focus:outline-none focus:border-[#635BFF] transition-colors" />
              <svg class="w-3.5 h-3.5 text-[#8792A2] absolute left-2.5 top-2.5 pointer-events-none" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </div>
          </div>
        </div>

        <!-- Pestañas de Vista de Reporte -->
        <div class="flex items-center gap-2 pt-2 border-t border-slate-200/80 overflow-x-auto text-xs">
          <button
            type="button"
            (click)="activeTab.set('consolidated')"
            [ngClass]="activeTab() === 'consolidated' 
              ? 'bg-[#635BFF] text-white font-medium shadow-2xs' 
              : 'text-[#4F5B66] hover:text-[#1A2C44] hover:bg-slate-200/60 font-normal'"
            class="px-3 py-1.5 rounded-md transition-colors whitespace-nowrap cursor-pointer">
            Tablero Consolidado
          </button>
          <button
            type="button"
            (click)="activeTab.set('fleet')"
            [ngClass]="activeTab() === 'fleet' 
              ? 'bg-[#635BFF] text-white font-medium shadow-2xs' 
              : 'text-[#4F5B66] hover:text-[#1A2C44] hover:bg-slate-200/60 font-normal'"
            class="px-3 py-1.5 rounded-md transition-colors whitespace-nowrap cursor-pointer">
            Flota y Rutas (Eficiencia)
          </button>
          <button
            type="button"
            (click)="activeTab.set('inventory')"
            [ngClass]="activeTab() === 'inventory' 
              ? 'bg-[#635BFF] text-white font-medium shadow-2xs' 
              : 'text-[#4F5B66] hover:text-[#1A2C44] hover:bg-slate-200/60 font-normal'"
            class="px-3 py-1.5 rounded-md transition-colors whitespace-nowrap cursor-pointer">
            Cobertura & Stock Hospitalario
          </button>
          <button
            type="button"
            (click)="activeTab.set('returns')"
            [ngClass]="activeTab() === 'returns' 
              ? 'bg-[#635BFF] text-white font-medium shadow-2xs' 
              : 'text-[#4F5B66] hover:text-[#1A2C44] hover:bg-slate-200/60 font-normal'"
            class="px-3 py-1.5 rounded-md transition-colors whitespace-nowrap cursor-pointer">
            Logística Inversa & Novedades
          </button>
        </div>
      </div>

      <!-- ================= TARJETAS KPI RESUMEN EJECUTIVO (Minimalistas) ================= -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        
        <!-- KPI 1: OTIF / SLA -->
        <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-2xs space-y-2">
          <div class="flex items-center justify-between">
            <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Cumplimiento SLA (OTIF)</span>
            <span class="inline-flex items-center text-[10px] font-bold px-2 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-200">
              +1.6% vs meta
            </span>
          </div>
          <div class="flex items-baseline gap-2">
            <span class="text-3xl font-bold text-[#1A2C44] tracking-tight">{{ kpiOtif() }}%</span>
            <span class="text-xs text-[#697386]">entregas perfectas</span>
          </div>
          <div class="w-full bg-slate-100 h-1.5 rounded-full overflow-hidden">
            <div class="bg-emerald-500 h-full rounded-full transition-all duration-500" [style.width.%]="kpiOtif()"></div>
          </div>
          <p class="text-[11px] text-[#697386]">
            {{ totalDeliveredOrders() }} de {{ totalPlannedOrders() }} órdenes a tiempo y completas.
          </p>
        </div>

        <!-- KPI 2: Cobertura Hospitalaria -->
        <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-2xs space-y-2">
          <div class="flex items-center justify-between">
            <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Red Abastecida</span>
            <span class="inline-flex items-center text-[10px] font-bold px-2 py-0.5 rounded bg-blue-50 text-blue-700 border border-blue-200">
              100% Cobertura
            </span>
          </div>
          <div class="flex items-baseline gap-2">
            <span class="text-3xl font-bold text-[#1A2C44] tracking-tight">34 / 34</span>
            <span class="text-xs text-[#697386]">hospitales activos</span>
          </div>
          <div class="w-full bg-slate-100 h-1.5 rounded-full overflow-hidden">
            <div class="bg-[#635BFF] h-full rounded-full w-full"></div>
          </div>
          <p class="text-[11px] text-[#697386]">
            0 quiebres de inventario registrados en el período.
          </p>
        </div>

        <!-- KPI 3: Costo de Transporte -->
        <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-2xs space-y-2">
          <div class="flex items-center justify-between">
            <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Costo Transporte / Km</span>
            <span class="inline-flex items-center text-[10px] font-bold px-2 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-200">
              Meta < $0.95
            </span>
          </div>
          <div class="flex items-baseline gap-2">
            <span class="text-3xl font-bold text-[#1A2C44] tracking-tight">\${{ kpiCostPerKm() }}</span>
            <span class="text-xs text-[#697386]">USD / km</span>
          </div>
          <div class="w-full bg-slate-100 h-1.5 rounded-full overflow-hidden">
            <div class="bg-emerald-600 h-full rounded-full" style="width: 84%"></div>
          </div>
          <p class="text-[11px] text-[#697386]">
            Total: \${{ totalTransportCost() | number:'1.2-2' }} en {{ totalDistanceKm() }} km recorridos.
          </p>
        </div>

        <!-- KPI 4: Cadena de Frío e Incidentes -->
        <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-2xs space-y-2">
          <div class="flex items-center justify-between">
            <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Cadena de Frío (2-8°C)</span>
            <span class="inline-flex items-center text-[10px] font-bold px-2 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-200">
              100% Calificada
            </span>
          </div>
          <div class="flex items-baseline gap-2">
            <span class="text-3xl font-bold text-[#1A2C44] tracking-tight">0</span>
            <span class="text-xs text-[#697386]">alertas térmicas</span>
          </div>
          <div class="w-full bg-slate-100 h-1.5 rounded-full overflow-hidden">
            <div class="bg-emerald-500 h-full rounded-full w-full"></div>
          </div>
          <p class="text-[11px] text-[#697386]">
            Tasa de mermas/devoluciones: {{ returnRate() }}% (conciliado).
          </p>
        </div>
      </div>

      <!-- ================= PESTAÑA 1: TABLERO CONSOLIDADO (Gráficos Visuales SVG) ================= -->
      @if (activeTab() === 'consolidated') {
        <div class="space-y-6">
          
          <!-- Bloque de Gráficos Principales (Línea de Tendencia + Donut de Categorías) -->
          <div class="grid grid-cols-1 lg:grid-cols-12 gap-6 items-stretch">
            
            <!-- Curva de Despacho y Cumplimiento Diario (8 columnas) -->
            <div class="lg:col-span-8 bg-white border border-[#E3E8EE] rounded-lg p-5 sm:p-6 shadow-2xs space-y-4">
              <div class="flex items-center justify-between">
                <div>
                  <h2 class="text-sm sm:text-base font-bold text-[#1A2C44]">
                    Evolución de Entregas y Cumplimiento Diario
                  </h2>
                  <p class="text-xs text-[#697386]">
                    Curva de entregas efectivas diarias vs. demanda programada en centros de salud.
                  </p>
                </div>
                <div class="flex items-center gap-3 text-xs text-[#697386]">
                  <span class="inline-flex items-center gap-1.5">
                    <span class="w-3 h-3 rounded-full bg-[#635BFF]"></span>
                    Entregado
                  </span>
                  <span class="inline-flex items-center gap-1.5">
                    <span class="w-3 h-0.5 bg-slate-300 border-dashed"></span>
                    Meta (95%)
                  </span>
                </div>
              </div>

              <!-- Gráfico SVG Área / Línea Dinámica y Minimalista -->
              <div class="pt-2">
                <div class="relative w-full h-56">
                  <svg class="w-full h-full overflow-visible" viewBox="0 0 600 180" preserveAspectRatio="none">
                    <defs>
                      <linearGradient id="areaGradient" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stop-color="#635BFF" stop-opacity="0.28" />
                        <stop offset="100%" stop-color="#635BFF" stop-opacity="0.01" />
                      </linearGradient>
                    </defs>

                    <!-- Líneas Guía Horizontales -->
                    <line x1="0" y1="30" x2="600" y2="30" stroke="#F1F3F5" stroke-width="1" />
                    <line x1="0" y1="80" x2="600" y2="80" stroke="#F1F3F5" stroke-width="1" />
                    <line x1="0" y1="130" x2="600" y2="130" stroke="#F1F3F5" stroke-width="1" />

                    <!-- Línea de Umbral Meta 95% -->
                    <line x1="0" y1="42" x2="600" y2="42" stroke="#CBD5E1" stroke-dasharray="4 4" stroke-width="1.5" />
                    <text x="590" y="38" font-size="9" text-anchor="end" fill="#8792A2" font-family="sans-serif">Meta SLA 95%</text>

                    <!-- Área con Relleno de Degradado -->
                    <polygon
                      points="0,150 0,70 100,55 200,60 300,38 400,45 500,28 600,32 600,150"
                      fill="url(#areaGradient)" />

                    <!-- Línea de Tendencia Principal -->
                    <polyline
                      points="0,70 100,55 200,60 300,38 400,45 500,28 600,32"
                      fill="none"
                      stroke="#635BFF"
                      stroke-width="2.5"
                      stroke-linecap="round"
                      stroke-linejoin="round" />

                    <!-- Puntos de Datos con Efecto Visual -->
                    @for (pt of trendPoints; track pt.day) {
                      <g class="cursor-pointer group">
                        <circle [attr.cx]="pt.x" [attr.cy]="pt.y" r="4.5" fill="#FFFFFF" stroke="#635BFF" stroke-width="2.5" />
                        <!-- Tooltip / Etiqueta -->
                        <text [attr.x]="pt.x" [attr.y]="pt.y - 10" font-size="10" font-weight="600" text-anchor="middle" fill="#1A2C44">
                          {{ pt.value }}%
                        </text>
                      </g>
                    }
                  </svg>
                </div>

                <!-- Eje X (Días) -->
                <div class="flex justify-between text-[11px] text-[#8792A2] pt-2 border-t border-[#F1F3F5] font-mono">
                  <span>Lunes 12</span>
                  <span>Martes 13</span>
                  <span>Miércoles 14</span>
                  <span>Jueves 15</span>
                  <span>Viernes 16</span>
                  <span>Sábado 17 (Hoy)</span>
                  <span>Proyección</span>
                </div>
              </div>

              <div class="pt-2 flex items-center justify-between text-xs text-[#4F5B66] bg-[#F8FAFC] p-3 rounded-md border border-slate-200/60">
                <div class="flex items-center gap-2">
                  <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
                  <span class="font-medium text-[#1A2C44]">Comportamiento Óptimo:</span>
                  <span>Todas las jornadas de la semana superaron el 96% de efectividad de entrega.</span>
                </div>
                <span class="font-bold font-mono text-[#635BFF]">Promedio 98.6%</span>
              </div>
            </div>

            <!-- Distribución de Carga & Donut Chart (4 columnas) -->
            <div class="lg:col-span-4 bg-white border border-[#E3E8EE] rounded-lg p-5 sm:p-6 shadow-2xs space-y-4 flex flex-col justify-between">
              <div>
                <h2 class="text-sm sm:text-base font-bold text-[#1A2C44]">
                  Composición de Carga
                </h2>
                <p class="text-xs text-[#697386]">
                  Distribución porcentual por categoría terapéutica y requerimiento térmico.
                </p>
              </div>

              <!-- Gráfico Donut SVG con Métricas Centrales -->
              <div class="flex items-center justify-center py-2">
                <div class="relative w-40 h-40">
                  <svg class="w-full h-full transform -rotate-90" viewBox="0 0 100 100">
                    <!-- Segmento 1: Fármacos Esenciales (45%) -> stroke-dasharray="45 55" -->
                    <circle cx="50" cy="50" r="38" fill="transparent" stroke="#635BFF" stroke-width="12"
                      stroke-dasharray="107.4 131.3" stroke-dashoffset="0" />
                    <!-- Segmento 2: Cadena de Frío (35%) -> stroke-dasharray="35 65" -->
                    <circle cx="50" cy="50" r="38" fill="transparent" stroke="#00A7C4" stroke-width="12"
                      stroke-dasharray="83.5 155.2" stroke-dashoffset="-107.4" />
                    <!-- Segmento 3: Emergencia (20%) -> stroke-dasharray="20 80" -->
                    <circle cx="50" cy="50" r="38" fill="transparent" stroke="#F5A623" stroke-width="12"
                      stroke-dasharray="47.7 191" stroke-dashoffset="-190.9" />
                  </svg>
                  <div class="absolute inset-0 flex flex-col items-center justify-center text-center">
                    <span class="text-xs text-[#8792A2] font-semibold uppercase tracking-wider">Total</span>
                    <span class="text-xl font-bold text-[#1A2C44] leading-tight">100%</span>
                    <span class="text-[10px] text-slate-500">12,450 uds</span>
                  </div>
                </div>
              </div>

              <!-- Leyenda Detallada del Donut -->
              <div class="space-y-2 text-xs pt-2 border-t border-[#F1F3F5]">
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <span class="w-2.5 h-2.5 rounded-sm bg-[#635BFF]"></span>
                    <span class="text-[#1A2C44] font-medium">Fármacos Esenciales</span>
                  </div>
                  <span class="font-mono font-semibold text-[#1A2C44]">45%</span>
                </div>
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <span class="w-2.5 h-2.5 rounded-sm bg-[#00A7C4]"></span>
                    <span class="text-[#1A2C44] font-medium">Cadena Frío (2-8°C)</span>
                  </div>
                  <span class="font-mono font-semibold text-[#1A2C44]">35%</span>
                </div>
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <span class="w-2.5 h-2.5 rounded-sm bg-[#F5A623]"></span>
                    <span class="text-[#1A2C44] font-medium">Insumos de Emergencia</span>
                  </div>
                  <span class="font-mono font-semibold text-[#1A2C44]">20%</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Cumplimiento por Zona / Distrito Metropolitano -->
          <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 sm:p-6 shadow-2xs space-y-4">
            <div class="flex items-center justify-between">
              <div>
                <h2 class="text-sm sm:text-base font-bold text-[#1A2C44]">
                  Efectividad de Distribución por Zona Geográfica (Quito)
                </h2>
                <p class="text-xs text-[#697386]">
                  Nivel de servicio hospitalario por distrito metropolitano frente al estándar mínimo institucional (95%).
                </p>
              </div>
              <span class="text-xs font-medium text-[#697386] font-mono">
                Umbral Objetivo: 95.0%
              </span>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 pt-1">
              @for (zone of zoneBreakdown; track zone.code) {
                <div class="p-3.5 rounded-lg border border-slate-200/70 bg-[#FBFBFC] space-y-2">
                  <div class="flex items-center justify-between">
                    <span class="text-xs font-bold text-[#1A2C44]">{{ zone.name }}</span>
                    <span class="text-xs font-mono font-bold" [ngClass]="zone.sla >= 98 ? 'text-emerald-600' : 'text-[#635BFF]'">
                      {{ zone.sla }}%
                    </span>
                  </div>
                  <div class="w-full bg-slate-200/80 h-2 rounded-full overflow-hidden">
                    <div class="h-full rounded-full transition-all duration-500"
                      [ngClass]="zone.sla >= 98 ? 'bg-emerald-500' : 'bg-[#635BFF]'"
                      [style.width.%]="zone.sla"></div>
                  </div>
                  <div class="flex items-center justify-between text-[11px] text-[#697386] pt-1">
                    <span>{{ zone.centersCount }} centros asignados</span>
                    <span>{{ zone.deliveredUnits | number }} uds</span>
                  </div>
                </div>
              }
            </div>
          </div>

        </div>
      }

      <!-- ================= PESTAÑA 2: EFICIENCIA DE FLOTA Y RUTAS ================= -->
      @if (activeTab() === 'fleet') {
        <div class="space-y-6">
          <div class="flex items-center justify-between">
            <div>
              <h2 class="text-base font-bold text-[#1A2C44]">Rendimiento Operativo de Vehículos</h2>
              <p class="text-xs text-[#697386]">Kilómetros recorridos, costo por kilómetro y utilización volumétrica de flota institucional y contratada.</p>
            </div>
            <span class="text-xs font-semibold px-2.5 py-1 rounded bg-slate-100 text-slate-700 font-mono">
              Flota Activa: 6 Móviles
            </span>
          </div>

          <!-- Tarjetas de Vehículos -->
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            @for (veh of vehicleReports; track veh.code) {
              <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-2xs space-y-4">
                <div class="flex items-start justify-between">
                  <div>
                    <div class="flex items-center gap-2">
                      <span class="font-mono font-bold text-sm text-[#1A2C44]">{{ veh.code }}</span>
                      <span class="text-[10px] font-mono px-2 py-0.5 rounded bg-slate-100 text-slate-700">
                        {{ veh.plate }}
                      </span>
                    </div>
                    <div class="text-xs text-[#697386] mt-0.5">{{ veh.model }} • {{ veh.type }}</div>
                  </div>
                  <span class="text-[10px] font-bold px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200">
                    Operativo
                  </span>
                </div>

                <!-- Métricas Clave del Móvil -->
                <div class="grid grid-cols-3 gap-2 py-2 px-3 rounded-md bg-[#F8FAFC] border border-slate-200/60 text-center">
                  <div>
                    <div class="text-[10px] text-[#8792A2] uppercase font-bold">Km Totales</div>
                    <div class="text-xs font-mono font-bold text-[#1A2C44] mt-0.5">{{ veh.totalKm }} km</div>
                  </div>
                  <div>
                    <div class="text-[10px] text-[#8792A2] uppercase font-bold">Costo / Km</div>
                    <div class="text-xs font-mono font-bold text-emerald-600 mt-0.5">\${{ veh.costPerKm }}</div>
                  </div>
                  <div>
                    <div class="text-[10px] text-[#8792A2] uppercase font-bold">Puntualidad</div>
                    <div class="text-xs font-mono font-bold text-[#635BFF] mt-0.5">{{ veh.onTimeRate }}%</div>
                  </div>
                </div>

                <!-- Barras de Ocupación -->
                <div class="space-y-2.5 text-xs">
                  <div>
                    <div class="flex justify-between text-[11px] mb-1">
                      <span class="text-[#4F5B66]">Ocupación de Peso (Carga)</span>
                      <span class="font-mono font-semibold text-[#1A2C44]">{{ veh.weightUsedKg }} / {{ veh.weightCapacityKg }} kg ({{ veh.weightUtilization }}%)</span>
                    </div>
                    <div class="w-full bg-slate-100 h-2 rounded-full overflow-hidden">
                      <div class="h-full rounded-full bg-[#635BFF]" [style.width.%]="veh.weightUtilization"></div>
                    </div>
                  </div>

                  <div>
                    <div class="flex justify-between text-[11px] mb-1">
                      <span class="text-[#4F5B66]">Ocupación Volumétrica (m³)</span>
                      <span class="font-mono font-semibold text-[#1A2C44]">{{ veh.volumeUsedM3 }} / {{ veh.volumeCapacityM3 }} m³ ({{ veh.volumeUtilization }}%)</span>
                    </div>
                    <div class="w-full bg-slate-100 h-2 rounded-full overflow-hidden">
                      <div class="h-full rounded-full bg-[#00A7C4]" [style.width.%]="veh.volumeUtilization"></div>
                    </div>
                  </div>
                </div>

                <div class="pt-2 border-t border-slate-100 flex items-center justify-between text-[11px] text-[#697386]">
                  <span>Rutas Ejecutadas: {{ veh.routesCount }}</span>
                  <span class="font-bold text-[#1A2C44]">Costo Total: \${{ veh.totalCost | number:'1.2-2' }}</span>
                </div>
              </div>
            }
          </div>
        </div>
      }

      <!-- ================= PESTAÑA 3: COBERTURA & STOCK HOSPITALARIO ================= -->
      @if (activeTab() === 'inventory') {
        <div class="space-y-6">
          <div class="flex items-center justify-between">
            <div>
              <h2 class="text-base font-bold text-[#1A2C44]">Auditoría de Cobertura y Riesgo Hospitalario</h2>
              <p class="text-xs text-[#697386]">Monitoreo de días de autonomía en centros de salud y prevención de quiebres por reposición anticipada.</p>
            </div>
            <div class="flex items-center gap-2">
              <span class="inline-flex items-center gap-1.5 text-xs text-emerald-700 bg-emerald-50 px-2.5 py-1 rounded-md border border-emerald-200">
                <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
                0 Hospitales en Desabastecimiento
              </span>
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            @for (hosp of hospitalInventory; track hosp.code) {
              <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-2xs space-y-3">
                <div class="flex items-start justify-between">
                  <div>
                    <h3 class="text-xs font-bold text-[#1A2C44] leading-snug">{{ hosp.name }}</h3>
                    <div class="text-[11px] text-[#697386] font-mono mt-0.5">{{ hosp.code }} • {{ hosp.zone }}</div>
                  </div>
                  <span class="text-[10px] font-bold px-2 py-0.5 rounded-full font-mono"
                    [ngClass]="{
                      'bg-emerald-50 text-emerald-700 border border-emerald-200': hosp.stockStatus === 'optimal',
                      'bg-blue-50 text-blue-700 border border-blue-200': hosp.stockStatus === 'safe',
                      'bg-amber-50 text-amber-700 border border-amber-200': hosp.stockStatus === 'low'
                    }">
                    {{ hosp.stockStatusLabel }}
                  </span>
                </div>

                <div class="flex items-baseline justify-between p-3 rounded-md bg-[#F8FAFC] border border-slate-200/60">
                  <div>
                    <div class="text-[10px] text-[#8792A2] uppercase font-bold">Autonomía</div>
                    <div class="text-base font-bold text-[#1A2C44] mt-0.5 font-mono">{{ hosp.coverageDays }} días</div>
                  </div>
                  <div>
                    <div class="text-[10px] text-[#8792A2] uppercase font-bold">Demanda Diaria</div>
                    <div class="text-xs font-bold text-[#4F5B66] mt-0.5 font-mono">{{ hosp.dailyConsumption }} uds/día</div>
                  </div>
                  <div>
                    <div class="text-[10px] text-[#8792A2] uppercase font-bold">Quiebres Evitados</div>
                    <div class="text-xs font-bold text-emerald-600 mt-0.5 font-mono">{{ hosp.avoidedStockouts }}</div>
                  </div>
                </div>

                <div class="flex items-center justify-between text-[11px] text-[#697386] pt-1">
                  <span>Última entrega: {{ hosp.lastDelivery }}</span>
                  <span class="font-medium text-[#635BFF]">Reposición: +{{ hosp.suggestedRestock }} uds</span>
                </div>
              </div>
            }
          </div>
        </div>
      }

      <!-- ================= PESTAÑA 4: LOGÍSTICA INVERSA & NOVEDADES ================= -->
      @if (activeTab() === 'returns') {
        <div class="space-y-6">
          <div class="flex items-center justify-between">
            <div>
              <h2 class="text-base font-bold text-[#1A2C44]">Control de Logística Inversa y Devoluciones</h2>
              <p class="text-xs text-[#697386]">Trazabilidad de guías de retorno (GRT), custodia de ítems no entregados y conciliación en bodega central.</p>
            </div>
            <a
              routerLink="/guias-retorno"
              class="inline-flex items-center gap-1.5 text-xs font-semibold text-[#635BFF] hover:underline">
              <span>Ver Módulo Guías de Retorno</span>
              <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3" />
              </svg>
            </a>
          </div>

          <div class="bg-white border border-[#E3E8EE] rounded-lg overflow-hidden shadow-2xs">
            <div class="overflow-x-auto">
              <table class="w-full text-left text-xs border-collapse">
                <thead>
                  <tr class="bg-[#F8FAFC] border-b border-[#E3E8EE] text-[11px] font-semibold text-[#4F5B66]">
                    <th class="py-3 px-4">Guía de Retorno</th>
                    <th class="py-3 px-4">Fecha</th>
                    <th class="py-3 px-4">Centro Hospitalario</th>
                    <th class="py-3 px-4">Motivo de Devolución</th>
                    <th class="py-3 px-4">Producto / SKU</th>
                    <th class="py-3 px-4 text-right">Cantidad</th>
                    <th class="py-3 px-4">Estado Custodia</th>
                    <th class="py-3 px-4 text-center">Conciliado</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-[#E3E8EE] text-[#30313D]">
                  @for (ret of returnIncidents; track ret.guideNumber) {
                    <tr class="hover:bg-slate-50/70 transition-colors">
                      <td class="py-3 px-4 font-mono font-bold text-[#635BFF]">
                        <a [routerLink]="['/guias-retorno', ret.guideNumber]" class="hover:underline">
                          {{ ret.guideNumber }}
                        </a>
                      </td>
                      <td class="py-3 px-4 font-mono text-[#697386]">{{ ret.date }}</td>
                      <td class="py-3 px-4 font-medium text-[#1A2C44]">{{ ret.originHospital }}</td>
                      <td class="py-3 px-4">
                        <span class="inline-flex items-center text-[10px] font-semibold px-2 py-0.5 rounded-full"
                          [ngClass]="{
                            'bg-rose-50 text-rose-700 border border-rose-200': ret.reason === 'Rotura de Frío',
                            'bg-amber-50 text-amber-700 border border-amber-200': ret.reason === 'Daño de Empaque',
                            'bg-slate-100 text-slate-700 border border-slate-200': ret.reason === 'Sobrante por Alta' || ret.reason === 'Error de Rotulado'
                          }">
                          {{ ret.reason }}
                        </span>
                      </td>
                      <td class="py-3 px-4">
                        <div class="font-medium text-[#1A2C44]">{{ ret.productName }}</div>
                        <div class="text-[10px] font-mono text-[#8792A2]">{{ ret.sku }}</div>
                      </td>
                      <td class="py-3 px-4 text-right font-mono font-bold">{{ ret.units }} uds</td>
                      <td class="py-3 px-4 text-[#4F5B66]">{{ ret.custodyStatus }}</td>
                      <td class="py-3 px-4 text-center">
                        @if (ret.reconciled) {
                          <span class="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-700">
                            <svg class="w-4 h-4 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                            </svg>
                            Sí
                          </span>
                        } @else {
                          <span class="text-[11px] font-medium text-amber-600">Pendiente</span>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        </div>
      }

      <!-- ================= TABLA DE REGISTROS DETALLADOS (Común a todas las vistas) ================= -->
      <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 sm:p-6 shadow-2xs space-y-4">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div>
            <h2 class="text-sm sm:text-base font-bold text-[#1A2C44]">
              Desglose Operativo de Despachos y Entregas
            </h2>
            <p class="text-xs text-[#697386]">
              Registro pormenorizado de viajes, niveles térmicos y puntualidad para trazabilidad y auditoría.
            </p>
          </div>
          <div class="text-xs text-[#697386] font-mono">
            Mostrando {{ filteredDispatches().length }} de {{ dispatches.length }} registros
          </div>
        </div>

        <!-- Tabla Responsive -->
        <div class="overflow-x-auto border border-[#E3E8EE] rounded-md">
          <table class="w-full text-left text-xs border-collapse">
            <thead>
              <tr class="bg-[#F8FAFC] border-b border-[#E3E8EE] text-[11px] font-semibold text-[#4F5B66]">
                <th class="py-3 px-3.5">Código Despacho</th>
                <th class="py-3 px-3.5">Destino / Centro Hospitalario</th>
                <th class="py-3 px-3.5">Zona</th>
                <th class="py-3 px-3.5">Categoría</th>
                <th class="py-3 px-3.5 text-center">Cadena Frío</th>
                <th class="py-3 px-3.5 text-right">Unidades</th>
                <th class="py-3 px-3.5 text-center">Cumplimiento SLA</th>
                <th class="py-3 px-3.5 text-right">Costo Estimado</th>
                <th class="py-3 px-3.5 text-center">Estado</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-[#E3E8EE] text-[#30313D]">
              @for (row of filteredDispatches(); track row.id) {
                <tr class="hover:bg-[#F8FAFC] transition-colors">
                  <td class="py-3 px-3.5 font-mono font-semibold text-[#1A2C44] whitespace-nowrap">
                    {{ row.code }}
                  </td>
                  <td class="py-3 px-3.5">
                    <div class="font-medium text-[#1A2C44]">{{ row.destination }}</div>
                    <div class="text-[10px] text-[#8792A2] font-mono">{{ row.vehicleCode }} • Conductor: {{ row.driverName }}</div>
                  </td>
                  <td class="py-3 px-3.5">
                    <span class="inline-block text-[11px] font-medium text-[#4F5B66]">
                      {{ row.zoneLabel }}
                    </span>
                  </td>
                  <td class="py-3 px-3.5">
                    <span class="inline-flex items-center text-[10px] font-medium px-2 py-0.5 rounded-full"
                      [ngClass]="{
                        'bg-blue-50 text-blue-700 border border-blue-200': row.category === 'COLD_CHAIN',
                        'bg-purple-50 text-purple-700 border border-purple-200': row.category === 'ESSENTIALS',
                        'bg-amber-50 text-amber-700 border border-amber-200': row.category === 'EMERGENCY'
                      }">
                      {{ row.categoryLabel }}
                    </span>
                  </td>
                  <td class="py-3 px-3.5 text-center">
                    <span class="inline-flex items-center gap-1 font-mono text-[11px] font-bold"
                      [ngClass]="row.coldChainStatus === 'optimal' ? 'text-emerald-700' : 'text-slate-600'">
                      @if (row.category === 'COLD_CHAIN') {
                        <span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                      }
                      {{ row.coldChainTemp }}
                    </span>
                  </td>
                  <td class="py-3 px-3.5 text-right font-mono font-semibold">
                    {{ row.unitsDelivered | number }} / {{ row.unitsPlanned | number }}
                  </td>
                  <td class="py-3 px-3.5 text-center">
                    <div class="flex items-center justify-center gap-1.5">
                      <span class="font-mono font-bold" [ngClass]="row.slaPercent >= 98 ? 'text-emerald-600' : 'text-[#635BFF]'">
                        {{ row.slaPercent }}%
                      </span>
                    </div>
                  </td>
                  <td class="py-3 px-3.5 text-right font-mono text-[#1A2C44]">
                    \${{ row.transportCost | number:'1.2-2' }}
                  </td>
                  <td class="py-3 px-3.5 text-center">
                    <span class="inline-flex items-center text-[10px] font-bold px-2 py-0.5 rounded-full"
                      [ngClass]="{
                        'bg-emerald-50 text-emerald-700 border border-emerald-200': row.status === 'DELIVERED',
                        'bg-blue-50 text-blue-700 border border-blue-200': row.status === 'IN_TRANSIT',
                        'bg-amber-50 text-amber-700 border border-amber-200': row.status === 'EXCEPTION'
                      }">
                      {{ row.statusLabel }}
                    </span>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="9" class="py-8 text-center text-xs text-[#8792A2]">
                    No se encontraron registros que coincidan con los filtros aplicados.
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>

      <!-- ================= SECCIÓN DE FIRMAS PARA FORMATO IMPRESO (visible solo en impresión) ================= -->
      <div class="hidden print:grid grid-cols-2 gap-12 pt-16 mt-8 border-t border-slate-300 text-xs text-[#1A2C44]">
        <div class="text-center space-y-2">
          <div class="h-14 border-b border-dashed border-slate-400 max-w-xs mx-auto"></div>
          <div class="font-bold uppercase">{{ currentUserName }}</div>
          <div class="text-[11px] text-[#697386]">Responsable de Planificación y Logística Hospitalaria</div>
          <div class="text-[10px] text-slate-400 font-mono">ISTPET — Operaciones DMQ</div>
        </div>
        <div class="text-center space-y-2">
          <div class="h-14 border-b border-dashed border-slate-400 max-w-xs mx-auto"></div>
          <div class="font-bold uppercase">Dirección de Auditoría y Control de Calidad</div>
          <div class="text-[11px] text-[#697386]">Certificación de Cadena de Frío y Entregas Institucionales</div>
          <div class="text-[10px] text-slate-400 font-mono">Registro Oficial MSP / Policía Nacional</div>
        </div>
      </div>

    </div>
  `,
  styles: [`
    @media print {
      body {
        background: #ffffff !important;
        color: #000000 !important;
      }
      .print\\:hidden {
        display: none !important;
      }
      .print\\:block {
        display: block !important;
      }
      .print\\:grid {
        display: grid !important;
      }
    }
  `]
})
export class ReportesPage {
  readonly session = inject(AuthSession);

  readonly folioNumber = '9842';
  readonly generationDate = new Date().toLocaleString('es-EC', { dateStyle: 'long', timeStyle: 'short' });

  // Estado reactivo de filtros
  readonly period = signal<ReportPeriod>('today');
  readonly selectedZone = signal<ReportZone>('ALL');
  readonly selectedCategory = signal<ReportCategory>('ALL');
  readonly activeTab = signal<ReportTab>('consolidated');
  readonly searchQuery = signal<string>('');

  get currentUserName(): string {
    return this.session.me()?.displayName || 'Bryan Baño';
  }

  get currentUserRole(): string {
    return this.session.role() || 'Gerencia Operativa';
  }

  readonly periodLabel = computed(() => {
    switch (this.period()) {
      case 'today': return 'Hoy (Jornada Activa - 17 Oct)';
      case 'week': return 'Últimos 7 Días';
      case 'month': return 'Mes en Curso (Octubre 2026)';
      case 'quarter': return 'Trimestre Q3/Q4';
    }
  });

  readonly zoneLabel = computed(() => {
    switch (this.selectedZone()) {
      case 'ALL': return 'Todas las Zonas (DMQ)';
      case 'NORTE': return 'Zona Norte';
      case 'CENTRO': return 'Zona Centro';
      case 'SUR': return 'Zona Sur';
      case 'VALLES': return 'Valles (Tumbaco / Chillos)';
    }
  });

  readonly categoryLabel = computed(() => {
    switch (this.selectedCategory()) {
      case 'ALL': return 'Todas las Categorías';
      case 'COLD_CHAIN': return 'Cadena de Frío (2-8°C)';
      case 'ESSENTIALS': return 'Fármacos Esenciales';
      case 'EMERGENCY': return 'Insumos de Emergencia';
    }
  });

  // Puntos del gráfico de tendencia
  readonly trendPoints = [
    { day: 'Lun 12', x: 0, y: 70, value: 96.2 },
    { day: 'Mar 13', x: 100, y: 55, value: 97.5 },
    { day: 'Mié 14', x: 200, y: 60, value: 97.1 },
    { day: 'Jue 15', x: 300, y: 38, value: 98.8 },
    { day: 'Vie 16', x: 400, y: 45, value: 98.2 },
    { day: 'Sáb 17', x: 500, y: 28, value: 99.4 },
    { day: 'Dom 18', x: 600, y: 32, value: 99.1 }
  ];

  // Desglose por zona
  readonly zoneBreakdown = [
    { code: 'NORTE', name: 'Distrito Norte', sla: 99.2, centersCount: 9, deliveredUnits: 3420 },
    { code: 'CENTRO', name: 'Distrito Centro', sla: 98.4, centersCount: 11, deliveredUnits: 4890 },
    { code: 'SUR', name: 'Distrito Sur', sla: 97.8, centersCount: 8, deliveredUnits: 2750 },
    { code: 'VALLES', name: 'Valles (Tumbaco / Chillos)', sla: 98.9, centersCount: 6, deliveredUnits: 1390 }
  ];

  // Datos de vehículos
  readonly vehicleReports: VehicleReportItem[] = [
    {
      code: 'VEH-01',
      plate: 'PBA-4120',
      model: 'Hino Dutro City',
      type: 'Camión Ligero Seco',
      routesCount: 4,
      totalKm: 285,
      totalCost: 239.40,
      costPerKm: 0.84,
      weightCapacityKg: 3500,
      weightUsedKg: 3080,
      weightUtilization: 88,
      volumeCapacityM3: 16,
      volumeUsedM3: 14.7,
      volumeUtilization: 92,
      onTimeRate: 99.0,
      status: 'ACTIVE'
    },
    {
      code: 'VEH-02',
      plate: 'PCZ-1892',
      model: 'Mercedes Sprinter Frío',
      type: 'Van Refrigerada (2-8°C)',
      routesCount: 5,
      totalKm: 310,
      totalCost: 260.40,
      costPerKm: 0.84,
      weightCapacityKg: 2000,
      weightUsedKg: 1520,
      weightUtilization: 76,
      volumeCapacityM3: 10,
      volumeUsedM3: 8.4,
      volumeUtilization: 84,
      onTimeRate: 100.0,
      status: 'ACTIVE'
    },
    {
      code: 'VEH-03',
      plate: 'PCH-9023',
      model: 'Isuzu NPR 4.5T',
      type: 'Camión Mediano',
      routesCount: 3,
      totalKm: 240,
      totalCost: 201.60,
      costPerKm: 0.84,
      weightCapacityKg: 4500,
      weightUsedKg: 4095,
      weightUtilization: 91,
      volumeCapacityM3: 20,
      volumeUsedM3: 17.8,
      volumeUtilization: 89,
      onTimeRate: 98.5,
      status: 'ACTIVE'
    },
    {
      code: 'VEH-04',
      plate: 'PDF-3341',
      model: 'Renault Master Frigorífico',
      type: 'Furgón Isotermo (2-8°C)',
      routesCount: 4,
      totalKm: 275,
      totalCost: 231.00,
      costPerKm: 0.84,
      weightCapacityKg: 1800,
      weightUsedKg: 1476,
      weightUtilization: 82,
      volumeCapacityM3: 9,
      volumeUsedM3: 7.9,
      volumeUtilization: 88,
      onTimeRate: 99.2,
      status: 'ACTIVE'
    },
    {
      code: 'VEH-05',
      plate: 'PBK-7711',
      model: 'Chevrolet NHR',
      type: 'Camión Urbano',
      routesCount: 3,
      totalKm: 190,
      totalCost: 159.60,
      costPerKm: 0.84,
      weightCapacityKg: 2500,
      weightUsedKg: 2050,
      weightUtilization: 82,
      volumeCapacityM3: 12,
      volumeUsedM3: 9.8,
      volumeUtilization: 82,
      onTimeRate: 97.5,
      status: 'ACTIVE'
    },
    {
      code: 'VEH-06',
      plate: 'PBM-2054',
      model: 'Hyundai H350',
      type: 'Van Exprés Emergencias',
      routesCount: 4,
      totalKm: 180,
      totalCost: 151.20,
      costPerKm: 0.84,
      weightCapacityKg: 1500,
      weightUsedKg: 1050,
      weightUtilization: 70,
      volumeCapacityM3: 8,
      volumeUsedM3: 6.0,
      volumeUtilization: 75,
      onTimeRate: 100.0,
      status: 'ACTIVE'
    }
  ];

  // Datos de inventario hospitalario
  readonly hospitalInventory: HospitalInventoryItem[] = [
    {
      code: 'HOSP-CAL',
      name: 'Hospital General Docente de Calderón',
      zone: 'NORTE',
      coverageDays: 14.2,
      stockStatus: 'optimal',
      stockStatusLabel: 'Óptimo',
      criticalItemsCount: 0,
      dailyConsumption: 420,
      suggestedRestock: 600,
      lastDelivery: 'Hoy, 09:15',
      avoidedStockouts: 3
    },
    {
      code: 'HOSP-EE',
      name: 'Hospital de Especialidades Eugenio Espejo',
      zone: 'CENTRO',
      coverageDays: 11.5,
      stockStatus: 'optimal',
      stockStatusLabel: 'Óptimo',
      criticalItemsCount: 0,
      dailyConsumption: 680,
      suggestedRestock: 950,
      lastDelivery: 'Hoy, 10:40',
      avoidedStockouts: 5
    },
    {
      code: 'HOSP-BO',
      name: 'Hospital Pediátrico Baca Ortiz',
      zone: 'CENTRO',
      coverageDays: 9.8,
      stockStatus: 'safe',
      stockStatusLabel: 'Seguro',
      criticalItemsCount: 0,
      dailyConsumption: 350,
      suggestedRestock: 500,
      lastDelivery: 'Hoy, 11:20',
      avoidedStockouts: 4
    },
    {
      code: 'HOSP-EG',
      name: 'Hospital General Enrique Garcés',
      zone: 'SUR',
      coverageDays: 8.4,
      stockStatus: 'safe',
      stockStatusLabel: 'Seguro',
      criticalItemsCount: 0,
      dailyConsumption: 510,
      suggestedRestock: 750,
      lastDelivery: 'Hoy, 12:05',
      avoidedStockouts: 2
    },
    {
      code: 'HOSP-GNA',
      name: 'Hospital Gineco Obstétrico Nueva Aurora',
      zone: 'SUR',
      coverageDays: 7.2,
      stockStatus: 'safe',
      stockStatusLabel: 'Seguro',
      criticalItemsCount: 0,
      dailyConsumption: 390,
      suggestedRestock: 480,
      lastDelivery: 'Hoy, 13:15',
      avoidedStockouts: 2
    },
    {
      code: 'CS-TUMB',
      name: 'Centro de Salud N°2 Tumbaco',
      zone: 'VALLES',
      coverageDays: 12.0,
      stockStatus: 'optimal',
      stockStatusLabel: 'Óptimo',
      criticalItemsCount: 0,
      dailyConsumption: 180,
      suggestedRestock: 250,
      lastDelivery: 'Hoy, 08:30',
      avoidedStockouts: 1
    }
  ];

  // Datos de logística inversa
  readonly returnIncidents: ReturnIncidentItem[] = [
    {
      guideNumber: 'GRT-20261017-001',
      date: '17/10/2026',
      originHospital: 'Hospital General Enrique Garcés',
      reason: 'Daño de Empaque',
      sku: 'MED-PAR-500',
      productName: 'Paracetamol 500mg Gotas Hospitalarias',
      units: 12,
      custodyStatus: 'Recibido en Bodega',
      reconciled: true
    },
    {
      guideNumber: 'GRT-20261016-004',
      date: '16/10/2026',
      originHospital: 'Hospital Pediátrico Baca Ortiz',
      reason: 'Sobrante por Alta',
      sku: 'SOL-SAL-09',
      productName: 'Solución Salina 0.9% 500ml',
      units: 24,
      custodyStatus: 'Recibido en Bodega',
      reconciled: true
    },
    {
      guideNumber: 'GRT-20261015-002',
      date: '15/10/2026',
      originHospital: 'Centro de Salud N°1 Centro Histórico',
      reason: 'Error de Rotulado',
      sku: 'INS-JER-3ML',
      productName: 'Jeringas Descartables 3ml',
      units: 50,
      custodyStatus: 'Recibido en Bodega',
      reconciled: true
    }
  ];

  // Base completa de despachos
  readonly dispatches: DispatchRow[] = [
    {
      id: 'DSP-01',
      code: 'DSP-2026-1041',
      destination: 'Hospital General Docente de Calderón',
      zone: 'NORTE',
      zoneLabel: 'Distrito Norte',
      category: 'COLD_CHAIN',
      categoryLabel: 'Cadena de Frío (2-8°C)',
      scheduledTime: '08:00',
      deliveredTime: '08:45',
      unitsPlanned: 850,
      unitsDelivered: 850,
      slaPercent: 100,
      coldChainTemp: '4.2°C',
      coldChainStatus: 'optimal',
      transportCost: 48.20,
      distanceKm: 57.3,
      vehicleCode: 'VEH-02',
      driverName: 'Carlos Mendoza',
      status: 'DELIVERED',
      statusLabel: 'Entregado a Tiempo'
    },
    {
      id: 'DSP-02',
      code: 'DSP-2026-1042',
      destination: 'Hospital de Especialidades Eugenio Espejo',
      zone: 'CENTRO',
      zoneLabel: 'Distrito Centro',
      category: 'ESSENTIALS',
      categoryLabel: 'Fármacos Esenciales',
      scheduledTime: '09:00',
      deliveredTime: '09:40',
      unitsPlanned: 1200,
      unitsDelivered: 1200,
      slaPercent: 100,
      coldChainTemp: 'Ambiente',
      coldChainStatus: 'normal',
      transportCost: 32.50,
      distanceKm: 38.6,
      vehicleCode: 'VEH-01',
      driverName: 'Marco Vinicio',
      status: 'DELIVERED',
      statusLabel: 'Entregado a Tiempo'
    },
    {
      id: 'DSP-03',
      code: 'DSP-2026-1043',
      destination: 'Hospital Pediátrico Baca Ortiz',
      zone: 'CENTRO',
      zoneLabel: 'Distrito Centro',
      category: 'COLD_CHAIN',
      categoryLabel: 'Cadena de Frío (2-8°C)',
      scheduledTime: '10:00',
      deliveredTime: '10:35',
      unitsPlanned: 620,
      unitsDelivered: 620,
      slaPercent: 100,
      coldChainTemp: '3.8°C',
      coldChainStatus: 'optimal',
      transportCost: 28.00,
      distanceKm: 33.3,
      vehicleCode: 'VEH-04',
      driverName: 'Galo Paredes',
      status: 'DELIVERED',
      statusLabel: 'Entregado a Tiempo'
    },
    {
      id: 'DSP-04',
      code: 'DSP-2026-1044',
      destination: 'Hospital General Enrique Garcés',
      zone: 'SUR',
      zoneLabel: 'Distrito Sur',
      category: 'ESSENTIALS',
      categoryLabel: 'Fármacos Esenciales',
      scheduledTime: '11:00',
      deliveredTime: '11:55',
      unitsPlanned: 940,
      unitsDelivered: 928,
      slaPercent: 98.7,
      coldChainTemp: 'Ambiente',
      coldChainStatus: 'normal',
      transportCost: 54.10,
      distanceKm: 64.4,
      vehicleCode: 'VEH-03',
      driverName: 'Jorge Alarcón',
      status: 'EXCEPTION',
      statusLabel: 'Novedad Menor (GRT)'
    },
    {
      id: 'DSP-05',
      code: 'DSP-2026-1045',
      destination: 'Hospital Gineco Obstétrico Nueva Aurora',
      zone: 'SUR',
      zoneLabel: 'Distrito Sur',
      category: 'EMERGENCY',
      categoryLabel: 'Insumos de Emergencia',
      scheduledTime: '12:30',
      deliveredTime: '13:10',
      unitsPlanned: 450,
      unitsDelivered: 450,
      slaPercent: 100,
      coldChainTemp: 'Ambiente',
      coldChainStatus: 'normal',
      transportCost: 46.00,
      distanceKm: 54.7,
      vehicleCode: 'VEH-06',
      driverName: 'Esteban Ortiz',
      status: 'DELIVERED',
      statusLabel: 'Entregado a Tiempo'
    },
    {
      id: 'DSP-06',
      code: 'DSP-2026-1046',
      destination: 'Centro de Salud N°1 Centro Histórico',
      zone: 'CENTRO',
      zoneLabel: 'Distrito Centro',
      category: 'COLD_CHAIN',
      categoryLabel: 'Cadena de Frío (2-8°C)',
      scheduledTime: '13:30',
      deliveredTime: '14:15',
      unitsPlanned: 380,
      unitsDelivered: 380,
      slaPercent: 100,
      coldChainTemp: '4.5°C',
      coldChainStatus: 'optimal',
      transportCost: 22.40,
      distanceKm: 26.6,
      vehicleCode: 'VEH-02',
      driverName: 'Carlos Mendoza',
      status: 'DELIVERED',
      statusLabel: 'Entregado a Tiempo'
    },
    {
      id: 'DSP-07',
      code: 'DSP-2026-1047',
      destination: 'Centro de Salud N°2 Tumbaco',
      zone: 'VALLES',
      zoneLabel: 'Valles (Tumbaco / Chillos)',
      category: 'ESSENTIALS',
      categoryLabel: 'Fármacos Esenciales',
      scheduledTime: '14:30',
      deliveredTime: '15:10',
      unitsPlanned: 520,
      unitsDelivered: 520,
      slaPercent: 100,
      coldChainTemp: 'Ambiente',
      coldChainStatus: 'normal',
      transportCost: 38.60,
      distanceKm: 45.9,
      vehicleCode: 'VEH-05',
      driverName: 'Luis Quishpe',
      status: 'DELIVERED',
      statusLabel: 'Entregado a Tiempo'
    },
    {
      id: 'DSP-08',
      code: 'DSP-2026-1048',
      destination: 'Centro de Salud Carcelén',
      zone: 'NORTE',
      zoneLabel: 'Distrito Norte',
      category: 'EMERGENCY',
      categoryLabel: 'Insumos de Emergencia',
      scheduledTime: '15:30',
      deliveredTime: '16:05',
      unitsPlanned: 310,
      unitsDelivered: 310,
      slaPercent: 100,
      coldChainTemp: 'Ambiente',
      coldChainStatus: 'normal',
      transportCost: 31.00,
      distanceKm: 36.9,
      vehicleCode: 'VEH-01',
      driverName: 'Marco Vinicio',
      status: 'DELIVERED',
      statusLabel: 'Entregado a Tiempo'
    }
  ];

  // Filtros aplicados de manera reactiva
  readonly filteredDispatches = computed(() => {
    const zone = this.selectedZone();
    const category = this.selectedCategory();
    const query = this.searchQuery().trim().toLowerCase();

    return this.dispatches.filter(d => {
      if (zone !== 'ALL' && d.zone !== zone) return false;
      if (category !== 'ALL' && d.category !== category) return false;
      if (query) {
        const matchesCode = d.code.toLowerCase().includes(query);
        const matchesDest = d.destination.toLowerCase().includes(query);
        const matchesVeh = d.vehicleCode.toLowerCase().includes(query);
        const matchesDriver = d.driverName.toLowerCase().includes(query);
        if (!matchesCode && !matchesDest && !matchesVeh && !matchesDriver) return false;
      }
      return true;
    });
  });

  // KPIs dinámicos
  readonly totalPlannedOrders = computed(() => this.filteredDispatches().length);
  readonly totalDeliveredOrders = computed(() =>
    this.filteredDispatches().filter(d => d.status === 'DELIVERED' || d.status === 'EXCEPTION').length
  );

  readonly kpiOtif = computed(() => {
    const list = this.filteredDispatches();
    if (list.length === 0) return 98.6;
    const sum = list.reduce((acc, curr) => acc + curr.slaPercent, 0);
    return Math.round((sum / list.length) * 10) / 10;
  });

  readonly totalTransportCost = computed(() => {
    const list = this.filteredDispatches();
    if (list.length === 0) return 1243.20;
    return list.reduce((acc, curr) => acc + curr.transportCost, 0);
  });

  readonly totalDistanceKm = computed(() => {
    const list = this.filteredDispatches();
    if (list.length === 0) return 1480;
    return Math.round(list.reduce((acc, curr) => acc + curr.distanceKm, 0));
  });

  readonly kpiCostPerKm = computed(() => {
    const km = this.totalDistanceKm();
    if (km === 0) return '0.84';
    return (this.totalTransportCost() / km).toFixed(2);
  });

  readonly returnRate = computed(() => 0.28);

  // Acción Imprimir / Exportar PDF
  printReport(): void {
    window.print();
  }

  // Acción Exportar CSV
  exportCsv(): void {
    const rows = this.filteredDispatches();
    const headers = [
      'Código Despacho',
      'Destino / Centro Hospitalario',
      'Zona',
      'Categoría Carga',
      'Cadena de Frío',
      'Hora Programada',
      'Hora Entrega',
      'Uds Planificadas',
      'Uds Entregadas',
      'Cumplimiento SLA (%)',
      'Costo Transporte (USD)',
      'Distancia (Km)',
      'Vehículo',
      'Conductor',
      'Estado'
    ];

    const csvContent = [
      headers.join(';'),
      ...rows.map(r => [
        `"${r.code}"`,
        `"${r.destination}"`,
        `"${r.zoneLabel}"`,
        `"${r.categoryLabel}"`,
        `"${r.coldChainTemp}"`,
        `"${r.scheduledTime}"`,
        `"${r.deliveredTime}"`,
        r.unitsPlanned,
        r.unitsDelivered,
        r.slaPercent,
        r.transportCost.toFixed(2),
        r.distanceKm.toFixed(1),
        `"${r.vehicleCode}"`,
        `"${r.driverName}"`,
        `"${r.statusLabel}"`
      ].join(';'))
    ].join('\r\n');

    // UTF-8 BOM para soporte correcto en Excel
    const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `reporte-logistico-istpet-${this.period()}-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  // Acción Exportar JSON
  exportJson(): void {
    const data = {
      institution: 'Instituto Superior Tecnológico Policía Nacional — ISTPET',
      system: 'Plataforma Logística y Distribución Hospitalaria',
      generatedAt: new Date().toISOString(),
      generatedBy: this.currentUserName,
      role: this.currentUserRole,
      period: this.periodLabel(),
      zone: this.zoneLabel(),
      category: this.categoryLabel(),
      kpis: {
        slaOtifPercent: this.kpiOtif(),
        costPerKmUsd: this.kpiCostPerKm(),
        totalCostUsd: this.totalTransportCost(),
        totalKm: this.totalDistanceKm(),
        activeHospitals: '34/34',
        coldChainConformancePercent: 100
      },
      dispatches: this.filteredDispatches(),
      fleet: this.vehicleReports,
      inventory: this.hospitalInventory,
      returns: this.returnIncidents
    };

    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `reporte-logistico-istpet-${this.period()}-${new Date().toISOString().slice(0, 10)}.json`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
