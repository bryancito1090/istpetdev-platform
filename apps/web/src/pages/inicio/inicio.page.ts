import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { AuthSession } from '@entities/session';
import { ApiService, ServicePoint, RiskItemDto, RoutePlanSummaryDto, AutonomyModeDto } from '@shared/api';

interface ProcessStep {
  id: number;
  title: string;
  desc: string;
  route: string;
  actionText: string;
  badge: string;
  badgeClass: string;
  writeOnly: boolean;
}

@Component({
  selector: 'page-inicio',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="max-w-7xl mx-auto px-6 lg:px-8 py-8 space-y-10 bg-white">
      <!-- Encabezado Principal Estilo Stripe: "Hoy" -->
      <div class="flex items-center justify-between pb-1 border-b border-slate-100">
        <div class="flex items-baseline gap-3">
          <h1 class="text-2xl sm:text-3xl font-bold text-slate-900 tracking-tight font-sans">
            Hoy
          </h1>
          <span class="text-xs font-semibold px-2 py-0.5 rounded-full bg-slate-100 text-slate-600 border border-slate-200/70">
            {{ userRoleBadge() }}
          </span>
        </div>

        <div class="flex items-center gap-3">
          <button
            type="button"
            (click)="refreshPulse()"
            [disabled]="loadingPulse()"
            class="inline-flex items-center gap-1.5 text-xs font-medium px-3.5 py-1.5 rounded-md border border-slate-200 bg-white text-slate-700 hover:bg-slate-50 hover:border-slate-300 transition-all shadow-2xs disabled:opacity-50 cursor-pointer">
            <svg 
              class="w-3.5 h-3.5 text-slate-400" 
              [ngClass]="{'animate-spin text-[#635BFF]': loadingPulse()}"
              xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            <span>{{ loadingPulse() ? 'Actualizando...' : 'Actualizar Estado' }}</span>
          </button>
        </div>
      </div>

      <!-- Fila Superior Asimétrica: Gráfico a la Izquierda y KPIs a la Derecha (Estilo Exacto Stripe Dashboard) -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-12 items-start">
        
        <!-- Columna Izquierda (8 cols): Métrica Principal + Gráfico de Línea Temporal -->
        <div class="lg:col-span-8 space-y-5">
          <!-- Selectores y Métrica Principal limpia -->
          <div>
            <div class="flex items-center gap-4 text-xs font-medium text-slate-500">
              <button type="button" class="inline-flex items-center gap-1 hover:text-slate-800 transition-colors cursor-pointer">
                <span>Red Hospitalaria</span>
                <svg class="w-3 h-3 text-slate-400" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd"/></svg>
              </button>
              <button type="button" class="inline-flex items-center gap-1 hover:text-slate-800 transition-colors cursor-pointer">
                <span>Jornada Operativa</span>
                <svg class="w-3 h-3 text-slate-400" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd"/></svg>
              </button>
            </div>

            <!-- Número Principal en Sans-serif (sin ceros punteados retro) -->
            <div class="mt-2 flex items-baseline gap-2">
              <span class="text-3xl sm:text-4xl font-semibold text-slate-900 tracking-tight font-sans">
                {{ servicePointsCount() }}
              </span>
              <span class="text-sm font-medium text-slate-500">
                centros de salud activos
              </span>
            </div>

            <div class="mt-1 flex items-center gap-2 text-xs text-slate-500">
              <span>{{ warehousesCount() }} bodega base</span>
              <span class="text-slate-300">&bull;</span>
              <span class="text-emerald-600 font-medium">100% Red asignada</span>
              <span class="text-slate-300">&bull;</span>
              <span>17:00 corte de despacho</span>
            </div>
          </div>

          <!-- Gráfico de Línea Temporal Estilo Stripe (con línea continua y tramo punteado) -->
          <div class="pt-4">
            <div class="relative w-full h-12 flex flex-col justify-end">
              <!-- SVG Curva/Línea estilo Stripe con proyección punteada -->
              <div class="w-full h-2 relative flex items-center">
                <div class="w-3/4 h-[2px] bg-[#635BFF]"></div>
                <div class="w-1/4 h-[2px] border-b-2 border-dashed border-[#635BFF]/40"></div>
              </div>
              <!-- Eje de horas -->
              <div class="flex justify-between items-center text-[11px] text-slate-400 font-sans mt-2">
                <span>07:00</span>
                <span class="text-slate-400 font-normal">12:00</span>
                <span>17:00</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Columna Derecha (4 cols): KPIs en Lista Vertical Limpia (Estilo Saldos y Transferencias de Stripe) -->
        <div class="lg:col-span-4 space-y-5 lg:border-l lg:border-[#E3E8EE] lg:pl-8">
          
          <!-- KPI 1: Riesgo de Stock (equivalente a Saldos en Stripe) -->
          <div class="space-y-0.5">
            <div class="flex items-center justify-between text-[14px] leading-[20px]">
              <span class="text-[#30313D] font-medium">Riesgo de Stock</span>
              <a routerLink="/alertas" class="text-[#635BFF] hover:underline font-medium text-[13px]">Ver más</a>
            </div>
            <div class="flex items-center gap-1.5 pt-0.5">
              <span 
                class="text-[20px] leading-[28px] font-semibold tracking-tight"
                [ngClass]="criticalRisksCount() > 0 ? 'text-rose-600' : 'text-[#1A2C44]'">
                {{ criticalRisksCount() }} crítico
              </span>
              <span class="text-[13px] text-[#8792A2] cursor-help" title="Insumos con cobertura crítica menor a 5 días">&#9432;</span>
            </div>
            <p class="text-[13px] leading-[18px] text-[#3C4F69]">
              {{ warningRisksCount() }} en margen de reposición
            </p>
          </div>

          <!-- KPI 2: Distribución y Rutas (equivalente a Transferencias en Stripe) -->
          <div class="space-y-0.5 pt-4 border-t border-[#F1F3F5]">
            <div class="flex items-center justify-between text-[14px] leading-[20px]">
              <span class="text-[#30313D] font-medium">Distribución</span>
              <a routerLink="/rutas" class="text-[#635BFF] hover:underline font-medium text-[13px]">Ver datos</a>
            </div>
            <div class="flex items-center gap-1.5 pt-0.5">
              <span class="text-[20px] leading-[28px] font-semibold text-[#1A2C44] tracking-tight">
                {{ publishedRoutesCount() }} publicadas
              </span>
              <span class="text-[13px] text-[#8792A2] cursor-help" title="Rutas activas publicadas para despacho">&#9432;</span>
            </div>
            <p class="text-[13px] leading-[18px] text-[#3C4F69]">
              {{ routePlansCount() }} rutas planificadas hoy
            </p>
          </div>

          <!-- KPI 3: Motor Logístico -->
          <div class="space-y-0.5 pt-4 border-t border-[#F1F3F5]">
            <div class="flex items-center justify-between text-[14px] leading-[20px]">
              <span class="text-[#30313D] font-medium">Planificación de Rutas</span>
              <span class="text-[12px] text-[#8792A2]">Automática</span>
            </div>
            <div class="flex items-center gap-1.5 pt-0.5">
              <span class="text-[18px] leading-[26px] font-semibold text-[#1A2C44] tracking-tight">
                {{ autonomyMode() === 'AutoWithLimits' ? 'Automático Supervisado' : autonomyMode() }}
              </span>
              <span class="text-[13px] text-[#8792A2] cursor-help" title="El sistema calcula automáticamente las mejores rutas de entrega">&#9432;</span>
            </div>
            <p class="text-[13px] leading-[18px] text-[#3C4F69]">
              Rutas eficientes &bull; Cobertura hospitalaria total
            </p>
          </div>

          <!-- KPI 4: Conectividad y Base de Datos -->
          <div class="space-y-0.5 pt-4 border-t border-[#F1F3F5]">
            <div class="flex items-center justify-between text-[14px] leading-[20px]">
              <span class="text-[#30313D] font-medium">Infraestructura</span>
              <a routerLink="/public-trace" class="text-[#635BFF] hover:underline font-medium text-[13px]">Traza QR</a>
            </div>
            <div class="flex items-center gap-2 pt-0.5">
              <span class="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
              <span class="text-[14px] font-semibold text-[#1A2C44]">
                AWS EC2
              </span>
              <span class="text-[11px] text-emerald-700 bg-emerald-50 px-1.5 py-0.5 rounded font-medium border border-emerald-200/50">
                Conectado
              </span>
            </div>
            <p class="text-[12px] text-[#8792A2] font-mono">
              34.234.100.56:5432
            </p>
          </div>

        </div>
      </div>

      <!-- SECCIÓN: Tu resumen (Idéntica a Stripe Imagen 3 con Tarjetas de Métricas) -->
      <div class="pt-6 space-y-5 border-t border-slate-100">
        <!-- Encabezado de Tu Resumen -->
        <div class="flex items-center justify-between">
          <div class="space-y-0.5">
            <h2 class="text-xl font-bold text-slate-900 tracking-tight font-sans">
              Tu resumen
            </h2>
            <p class="text-xs text-[#697386]">
              Estado del flujo logístico de abastecimiento hospitalario en tiempo real.
            </p>
          </div>
        </div>

        <!-- Tarjetas de Resumen en Cuadrícula Estilo Stripe (Imagen 3) -->
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          @for (step of steps(); track step.id) {
            <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 hover:border-slate-300 hover:shadow-[0_1px_3px_rgba(0,0,0,0.05)] transition-all flex flex-col justify-between group">
              <div>
                <!-- Encabezado de la Tarjeta con ⓘ (Exacto a DevTools Stripe: h3, 14px, #30313D) -->
                <div class="flex items-center justify-between">
                  <h3 class="text-[14px] leading-[20px] font-medium text-[#30313D] truncate">
                    {{ step.title }}
                  </h3>
                  <span class="text-[#8792A2] group-hover:text-[#3C4F69] transition-colors cursor-help text-[13px]" title="{{ step.desc }}">&#9432;</span>
                </div>

                <!-- Métrica Principal de la Tarjeta (24px, #1A2C44) -->
                <div class="mt-2.5">
                  <div class="text-[24px] leading-[32px] font-semibold text-[#1A2C44] tracking-tight">
                    @if (step.id === 1) {
                      {{ servicePointsCount() }} <span class="text-[14px] font-normal text-[#3C4F69]">puntos</span>
                    } @else if (step.id === 2) {
                      <span [ngClass]="criticalRisksCount() > 0 ? 'text-rose-600' : 'text-[#1A2C44]'">
                        {{ criticalRisksCount() }}
                      </span>
                      <span class="text-[14px] font-normal text-[#3C4F69]"> quiebres</span>
                    } @else if (step.id === 3) {
                      {{ publishedRoutesCount() }} <span class="text-[14px] font-normal text-[#3C4F69]">/ {{ routePlansCount() }} rutas</span>
                    } @else {
                      {{ warehousesCount() }} <span class="text-[14px] font-normal text-[#3C4F69]">bodega</span>
                    }
                  </div>

                  <!-- Subtexto comparativo (Exacto a DevTools Stripe: 14px, #3C4F69, mb-0.5) -->
                  <div class="text-[13px] leading-[18px] text-[#3C4F69] mt-0.5 mb-[2px] line-clamp-2">
                    {{ step.desc }}
                  </div>
                </div>
              </div>

              <!-- Acción Inferior estilo Stripe Link -->
              <div class="mt-4 pt-3 border-t border-[#F1F3F5] flex items-center justify-between">
                <span 
                  class="text-[11px] px-2 py-0.5 rounded-full font-medium border"
                  [ngClass]="step.badgeClass">
                  {{ step.badge }}
                </span>

                @if (!step.writeOnly || session.canWrite()) {
                  <a
                    [routerLink]="step.route"
                    class="text-[13px] font-medium text-[#635BFF] hover:underline inline-flex items-center gap-1 group-hover:translate-x-0.5 transition-transform cursor-pointer">
                    <span>{{ step.actionText }}</span>
                    <span>&rarr;</span>
                  </a>
                } @else {
                  <span class="text-[12px] text-[#8792A2] italic">Solo lectura</span>
                }
              </div>
            </div>
          }
        </div>
      </div>

      <!-- Sección Secundaria: Recomendaciones Operativas del Sistema -->
      <div class="pt-4 border-t border-[#F1F3F5]">
        <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)] space-y-4">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <h3 class="text-[14px] leading-[20px] font-semibold text-[#1A2C44]">
                Recomendaciones Operativas del Sistema
              </h3>
              <span class="text-[11px] px-2 py-0.5 rounded-full font-medium bg-amber-50 text-amber-700 border border-amber-200/60">
                Atención Prioritaria
              </span>
            </div>
            <span class="text-[12px] text-[#8792A2]">Motor Determinista Activo</span>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-3 gap-6 pt-1 text-xs">
            <div class="space-y-1.5 p-3 rounded-md bg-[#F8FAFC] border border-[#E3E8EE]/70">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-rose-600">Alerta de Stock</span>
              <p class="text-[13px] leading-[18px] text-[#30313D]">
                {{ criticalRisksCount() > 0 ? 'Detectado ' + criticalRisksCount() + ' quiebre de stock crítico inminente en centros de salud.' : 'Todos los puntos de salud operan en niveles normales de cobertura.' }}
              </p>
              <a routerLink="/alertas" class="inline-flex items-center gap-1 text-[13px] text-[#635BFF] hover:underline font-medium cursor-pointer pt-1">
                <span>Resolver alertas de stock</span>
                <span>&rarr;</span>
              </a>
            </div>

            <div class="space-y-1.5 p-3 rounded-md bg-[#F8FAFC] border border-[#E3E8EE]/70">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-blue-600">Flota y Despacho</span>
              <p class="text-[13px] leading-[18px] text-[#30313D]">
                {{ publishedRoutesCount() }} rutas despachadas. Revisa el estado de entrega y custodia con firma digital.
              </p>
              <a routerLink="/rutas" class="inline-flex items-center gap-1 text-[13px] text-[#635BFF] hover:underline font-medium cursor-pointer pt-1">
                <span>Inspeccionar rutas</span>
                <span>&rarr;</span>
              </a>
            </div>

            <div class="space-y-1.5 p-3 rounded-md bg-[#F8FAFC] border border-[#E3E8EE]/70">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-emerald-600">Simulación y Proyección</span>
              <p class="text-[13px] leading-[18px] text-[#30313D]">
                Prueba el modelo de optimización logística frente a baselines a 90 días.
              </p>
              <a routerLink="/simulation" class="inline-flex items-center gap-1 text-[13px] text-[#635BFF] hover:underline font-medium cursor-pointer pt-1">
                <span>Abrir simulador 90d</span>
                <span>&rarr;</span>
              </a>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class InicioPage implements OnInit {
  private readonly apiService = inject(ApiService);
  readonly session = inject(AuthSession);

  readonly loadingPulse = signal(true);
  readonly servicePointsCount = signal(0);
  readonly warehousesCount = signal(0);
  readonly criticalRisksCount = signal(0);
  readonly warningRisksCount = signal(0);
  readonly routePlansCount = signal(0);
  readonly publishedRoutesCount = signal(0);
  readonly autonomyMode = signal('AutoWithLimits');

  readonly isAuditor = computed(() => {
    const r = this.session.role ? this.session.role() : null;
    if (r) return r === 'Auditor';
    return !this.session.canWrite();
  });

  readonly isGerencia = computed(() => {
    const r = this.session.role ? this.session.role() : null;
    return r === 'Gerencia';
  });

  readonly userRoleBadge = computed(() => {
    if (this.isAuditor()) return 'AUDITORÍA';
    if (this.isGerencia()) return 'GERENCIA';
    return 'PLANIFICACIÓN';
  });

  readonly steps = signal<ProcessStep[]>([
    {
      id: 1,
      title: '1. Cargar Datos Maestros',
      desc: 'Subir archivos CSV o Excel con bodegas, hospitales, productos, vehículos y consumos.',
      route: '/datos',
      actionText: 'Ir a Datos',
      badge: 'Catálogo Base',
      badgeClass: 'bg-slate-50 text-slate-700 border-slate-200',
      writeOnly: true
    },
    {
      id: 2,
      title: '2. Monitorear Alertas de Stock',
      desc: 'Verificar qué hospitales tienen insumos con menos de 5 días de cobertura o quiebre de stock.',
      route: '/alertas',
      actionText: 'Ver Alertas',
      badge: 'Riesgo y Demanda',
      badgeClass: 'bg-amber-50 text-amber-700 border-amber-200',
      writeOnly: false
    },
    {
      id: 3,
      title: '3. Planificar y Publicar Rutas',
      desc: 'Generar la ruta de despacho óptima, validar ventanas de atención y despachar a conductores.',
      route: '/rutas',
      actionText: 'Ir a Rutas',
      badge: 'Ruteo y Despacho',
      badgeClass: 'bg-blue-50 text-blue-700 border-blue-200',
      writeOnly: false
    },
    {
      id: 4,
      title: '4. Bodegas & Rebalanceo de Stock',
      desc: 'Supervisar niveles de existencias centrales y sugerencias de rebalanceo entre almacenes.',
      route: '/bodegas',
      actionText: 'Ir a Bodegas',
      badge: 'Existencias',
      badgeClass: 'bg-emerald-50 text-emerald-700 border-emerald-200',
      writeOnly: true
    }
  ]);

  ngOnInit(): void {
    this.refreshPulse();
  }

  refreshPulse(): void {
    this.loadingPulse.set(true);

    const points$ = this.apiService.getServicePoints ? this.apiService.getServicePoints() : of([] as ServicePoint[]);
    const warehouses$ = this.apiService.getWarehouses ? this.apiService.getWarehouses() : of([] as ServicePoint[]);
    const risks$ = this.apiService.getRisks ? this.apiService.getRisks() : of(null);
    const plans$ = this.apiService.getRoutePlans ? this.apiService.getRoutePlans() : of([] as RoutePlanSummaryDto[]);
    const autonomy$ = this.apiService.getAutonomyMode ? this.apiService.getAutonomyMode() : of(null);

    forkJoin({
      points: points$,
      warehouses: warehouses$,
      risks: risks$,
      plans: plans$,
      autonomy: autonomy$
    }).subscribe({
      next: ({ points, warehouses, risks, plans, autonomy }) => {
        this.loadingPulse.set(false);

        if (points) {
          this.servicePointsCount.set(points.length);
        }
        if (warehouses) {
          this.warehousesCount.set(warehouses.length);
        }
        if (risks && risks.items) {
          const crit = risks.items.filter(i => i.level === 'Critico' || i.level === 'Critical').length;
          const warn = risks.items.filter(i => i.level === 'Alto' || i.level === 'High' || i.level === 'Vigilar' || i.level === 'Medium').length;
          this.criticalRisksCount.set(crit);
          this.warningRisksCount.set(warn);
        }
        if (plans) {
          this.routePlansCount.set(plans.length);
          const published = plans.filter(p => p.status === 'Published').length;
          this.publishedRoutesCount.set(published);
        }
        if (autonomy) {
          this.autonomyMode.set(autonomy.mode);
        }

        // Actualizar badges de pasos
        this.steps.update(steps => steps.map(s => {
          if (s.id === 1) {
            return {
              ...s,
              badge: `${this.servicePointsCount()} Puntos Cargados`,
              badgeClass: this.servicePointsCount() > 0 ? 'bg-emerald-50 text-emerald-700 border-emerald-200' : 'bg-amber-50 text-amber-700 border-amber-200'
            };
          }
          if (s.id === 2) {
            return {
              ...s,
              badge: this.criticalRisksCount() > 0 ? `${this.criticalRisksCount()} Críticos` : 'Sin Quiebres',
              badgeClass: this.criticalRisksCount() > 0 ? 'bg-rose-50 text-rose-700 border-rose-200' : 'bg-emerald-50 text-emerald-700 border-emerald-200'
            };
          }
          if (s.id === 3) {
            return {
              ...s,
              badge: `${this.routePlansCount()} Rutas`,
              badgeClass: this.routePlansCount() > 0 ? 'bg-primary-50 text-primary-700 border-primary-200' : 'bg-slate-50 text-slate-600 border-slate-200'
            };
          }
          return s;
        }));
      },
      error: () => {
        this.loadingPulse.set(false);
      }
    });
  }
}
