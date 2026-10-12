import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { SessionStore } from '@entities/session';
import { CheckApiHealthComponent } from '@features/check-api-health/check-api-health.component';
import { RouteMapWidget } from '@widgets/route-map';
import { StatCardComponent, BadgeComponent, ButtonComponent, CardComponent } from '@shared/ui';
import { LucidePackageSearch, LucideAlertTriangle, LucideCheckCircle2, LucideLayers, LucideRefreshCw } from '@lucide/angular';

interface ServiceStockAlert {
  facility: string;
  code: string;
  location: string;
  contractType: string;
  autonomyDays: number;
  stockLevelPercent: number;
  status: 'critical' | 'warning' | 'optimal';
  nextDelivery: string;
  criticalItem: string;
}

@Component({
  selector: 'page-operations-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    CheckApiHealthComponent, 
    RouteMapWidget, 
    StatCardComponent,
    BadgeComponent,
    ButtonComponent,
    CardComponent,
    LucidePackageSearch,
    LucideAlertTriangle,
    LucideCheckCircle2,
    LucideLayers,
    LucideRefreshCw
  ],
  template: `
    <div class="space-y-6">
      <!-- Encabezado Enterprise B2B con telemetría de contrato -->
      <div class="bg-white border border-slate-200/80 rounded-2xl p-5 sm:p-6 shadow-sm">
        <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
          <div class="space-y-1.5">
            <div class="flex flex-wrap items-center gap-2">
              <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-[11px] font-mono font-semibold bg-primary-50 text-primary-700 border border-primary-200/60">
                <span class="h-1.5 w-1.5 rounded-full bg-primary-500 animate-pulse"></span>
                RETO 1 • EXPO CLEAN ECUADOR 2026
              </span>
              <span class="text-xs text-slate-400 font-mono">/</span>
              <span class="text-xs font-semibold text-slate-600">DISTRIBUCIÓN INSTITUCIONAL B2B</span>
              @if (sessionStore.isAuthenticated()) {
                <ui-badge 
                  variant="success" 
                  size="sm" 
                  [withDot]="true" 
                  [label]="sessionStore.currentUser()?.roleLabel ?? 'Operador'">
                </ui-badge>
              }
            </div>
            
            <h1 class="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">
              Torre de Control de Suministros y Logística
            </h1>
            <p class="text-sm text-slate-500 max-w-3xl leading-relaxed">
              Monitoreo predictivo de inventario de químicos de limpieza, consumibles y equipamiento. Prevención de quiebres de stock 5 días antes con optimización de rutas PostGIS.
            </p>
          </div>

          <div class="flex flex-wrap items-center gap-2.5 self-start lg:self-auto shrink-0">
            <ui-button
              variant="secondary"
              size="sm"
              (buttonClick)="refreshData()"
              ariaLabel="Actualizar telemetría operativa">
              <svg lucideRefreshCw [size]="14" class="mr-1.5 text-slate-500 stroke-[2.2]"></svg>
              <span>Sincronizar</span>
            </ui-button>

            <ui-button
              variant="primary"
              size="sm"
              (buttonClick)="navigateToTrace()"
              ariaLabel="Escanear y consultar trazabilidad de unidad logística con QR">
              <svg lucidePackageSearch [size]="14" class="mr-1.5 text-white stroke-[2.2]"></svg>
              <span>Consultar Unidad QR</span>
            </ui-button>
          </div>
        </div>

        <!-- Barra inferior de telemetría de nodos -->
        <div class="mt-4 pt-4 border-t border-slate-100 flex flex-wrap items-center justify-between gap-3 text-xs text-slate-500">
          <div class="flex items-center gap-4">
            <span class="flex items-center gap-1.5">
              <span class="h-2 w-2 rounded-full bg-emerald-500"></span>
              <strong class="text-slate-700">Hub Central:</strong> Quito Calderón (B-01)
            </span>
            <span class="hidden sm:inline text-slate-300">•</span>
            <span class="hidden sm:inline">
              <strong class="text-slate-700">Cobertura:</strong> Pichincha, Guayas, Tungurahua, Azuay, Sto. Domingo
            </span>
          </div>
          <div class="font-mono text-[11px] bg-slate-50 px-2 py-0.5 rounded border border-slate-200">
            RDS PostGIS 16 • Algoritmo VRP Activo
          </div>
        </div>
      </div>

      <!-- Estado de la API .NET Core -->
      <feature-check-api-health></feature-check-api-health>

      <!-- Cuadrícula de KPIs Enterprise (Stripe Style) -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-5">
        <ui-stat-card
          title="Ahorro en Combustible"
          value="24.8"
          unit="%"
          badgeText="Optimizado"
          badgeVariant="success"
          trend="up"
          trendValue="+127 km"
          trendLabel="evitados vs ruta fija"
          description="Algoritmo VRP con geocercas PostGIS">
        </ui-stat-card>

        <ui-stat-card
          title="Tasa Cobertura Químicos"
          value="98.6"
          unit="%"
          badgeText="Sin Quiebres"
          badgeVariant="success"
          trend="up"
          trendValue="0 fallas"
          trendLabel="en 30 días"
          description="Detergentes, desinfectantes y neutros">
        </ui-stat-card>

        <ui-stat-card
          title="Custodia Digital QR"
          value="12"
          unit="HUs"
          badgeText="100% Inmutable"
          badgeVariant="transit"
          trend="neutral"
          trendValue="UUID v7"
          trendLabel="firmado en offline"
          description="Handling Units con sello criptográfico">
        </ui-stat-card>

        <ui-stat-card
          title="Puntos de Servicio Activos"
          value="5"
          unit="Sedes"
          badgeText="5 Provincias"
          badgeVariant="neutral"
          trend="up"
          trendValue="100%"
          trendLabel="operatividad"
          description="Hospitales y centros corporativos">
        </ui-stat-card>
      </div>

      <!-- Alertas Predictivas de Stock: Reabastecimiento Preventivo 5 Días Antes -->
      <ui-card [padding]="'none'">
        <div class="p-4 sm:p-5 border-b border-slate-100 flex flex-wrap items-center justify-between gap-3 bg-slate-50/50">
          <div>
            <div class="flex items-center gap-2">
              <svg lucideLayers [size]="16" class="text-primary-600 stroke-[2.2]"></svg>
              <h2 class="text-sm sm:text-base font-bold text-slate-900">
                Matriz de Autonomía de Químicos y Consumibles por Punto de Servicio
              </h2>
            </div>
            <p class="text-xs text-slate-500 mt-0.5">
              Predicción automática de demanda para emisión de pedidos de reposición con 5 días de anticipación.
            </p>
          </div>

          <span class="text-xs font-mono text-slate-500 bg-white border border-slate-200 px-2.5 py-1 rounded-md">
            Ventana de Seguridad: 5 Días Críticos
          </span>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full text-left text-xs text-slate-600">
            <thead class="bg-slate-50/80 text-[11px] font-semibold text-slate-500 uppercase tracking-wider border-b border-slate-200">
              <tr>
                <th class="py-3 px-4">Punto de Servicio / Contrato</th>
                <th class="py-3 px-4">Insumo Crítico</th>
                <th class="py-3 px-4">Nivel Actual</th>
                <th class="py-3 px-4">Autonomía</th>
                <th class="py-3 px-4">Estado Logístico</th>
                <th class="py-3 px-4 text-right">Próximo Despacho</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 font-sans">
              @for (item of stockMatrix; track item.code) {
                <tr class="hover:bg-slate-50/70 transition-colors">
                  <td class="py-3.5 px-4">
                    <div class="font-bold text-slate-900">{{ item.facility }}</div>
                    <div class="text-[11px] text-slate-400 font-mono">{{ item.code }} • {{ item.location }}</div>
                  </td>
                  <td class="py-3.5 px-4 font-medium text-slate-800">
                    {{ item.criticalItem }}
                  </td>
                  <td class="py-3.5 px-4">
                    <div class="w-28 space-y-1">
                      <div class="flex justify-between text-[11px] font-mono">
                        <span>{{ item.stockLevelPercent }}%</span>
                      </div>
                      <div class="w-full bg-slate-100 rounded-full h-1.5 overflow-hidden">
                        <div 
                          class="h-1.5 rounded-full transition-all duration-500"
                          [ngClass]="{
                            'bg-amber-500': item.status === 'warning',
                            'bg-rose-500': item.status === 'critical',
                            'bg-emerald-500': item.status === 'optimal'
                          }"
                          [style.width.%]="item.stockLevelPercent">
                        </div>
                      </div>
                    </div>
                  </td>
                  <td class="py-3.5 px-4 font-mono font-bold" [ngClass]="{
                    'text-amber-600': item.status === 'warning',
                    'text-rose-600': item.status === 'critical',
                    'text-emerald-600': item.status === 'optimal'
                  }">
                    {{ item.autonomyDays }} días
                  </td>
                  <td class="py-3.5 px-4">
                    @if (item.status === 'warning') {
                      <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-medium bg-amber-50 text-amber-700 border border-amber-200">
                        <svg lucideAlertTriangle [size]="12" class="stroke-[2.2]"></svg>
                        Reabastecimiento Sugerido
                      </span>
                    } @else if (item.status === 'critical') {
                      <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-medium bg-rose-50 text-rose-700 border border-rose-200">
                        <svg lucideAlertTriangle [size]="12" class="stroke-[2.2]"></svg>
                        Alerta Crítica (&lt;3 días)
                      </span>
                    } @else {
                      <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
                        <svg lucideCheckCircle2 [size]="12" class="stroke-[2.2]"></svg>
                        Stock Óptimo
                      </span>
                    }
                  </td>
                  <td class="py-3.5 px-4 text-right font-mono text-slate-700 font-semibold">
                    {{ item.nextDelivery }}
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </ui-card>

      <!-- Widget de Mapa y Optimización de Rutas con Split-Screen B2B -->
      <widget-route-map></widget-route-map>
    </div>
  `
})
export class OperationsDashboardPage {
  readonly sessionStore = inject(SessionStore);
  private readonly router = inject(Router);

  readonly stockMatrix: ServiceStockAlert[] = [
    {
      facility: 'Hospital General Docente Ambato',
      code: 'HOSP-AMB',
      location: 'Tungurahua',
      contractType: 'Hospitalario Limpieza Grado Quirúrgico',
      autonomyDays: 4,
      stockLevelPercent: 32,
      status: 'warning',
      nextDelivery: 'Hoy 16:45 (Flota C-01)',
      criticalItem: 'Desinfectante Amonio Cuaternario 5G'
    },
    {
      facility: 'Hospital Teodoro Maldonado Carbo',
      code: 'HOSP-GYE',
      location: 'Guayaquil',
      contractType: 'Hospitalario General & Áreas Críticas',
      autonomyDays: 8,
      stockLevelPercent: 68,
      status: 'optimal',
      nextDelivery: 'Mañana 08:30 (Flota C-01)',
      criticalItem: 'Detergente Neutro Enzimático 20L'
    },
    {
      facility: 'Hospital Vicente Corral Moscoso',
      code: 'HOSP-CUE',
      location: 'Cuenca',
      contractType: 'Hospitalario Especializado',
      autonomyDays: 6,
      stockLevelPercent: 54,
      status: 'optimal',
      nextDelivery: 'Mañana 15:00 (Flota C-01)',
      criticalItem: 'Paños Microfibra 40x40 Cod. Color'
    },
    {
      facility: 'Hospital Gustavo Domínguez',
      code: 'HOSP-SD',
      location: 'Santo Domingo',
      contractType: 'Integral Pabellones y Triaje',
      autonomyDays: 5,
      stockLevelPercent: 41,
      status: 'warning',
      nextDelivery: 'Hoy 11:20 (Flota C-01)',
      criticalItem: 'Bolsas Biopeligrosas Rojas 50L'
    },
    {
      facility: 'Centro Logístico Principal Quito (B-01)',
      code: 'HUB-UIO',
      location: 'Quito Norte',
      contractType: 'Bodega Central Distribuidora',
      autonomyDays: 28,
      stockLevelPercent: 88,
      status: 'optimal',
      nextDelivery: 'Origen Despachos',
      criticalItem: 'Reserva Estratégica 10,000L'
    }
  ];

  navigateToTrace(): void {
    this.router.navigate(['/public-trace']);
  }

  refreshData(): void {
    // Sincronización de telemetría sin parches
  }
}

