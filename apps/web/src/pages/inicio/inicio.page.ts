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
    <div class="max-w-6xl mx-auto px-4 py-8 space-y-8">
      <!-- Encabezado Principal según Rol -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-200 pb-5">
        <div>
          <div class="flex items-center gap-2">
            <h1 class="text-2xl font-bold text-slate-900 tracking-tight">
              @if (isAuditor()) {
                Consola de Fiscalización y Control de Operaciones
              } @else if (isGerencia()) {
                Tablero Directivo y Gestión Estratégica
              } @else {
                Flujo de Trabajo Operativo y Despacho Logístico
              }
            </h1>
            <span 
              class="text-xs font-semibold px-2.5 py-0.5 rounded-full border"
              [ngClass]="{
                'bg-indigo-50 text-indigo-700 border-indigo-200': isAuditor(),
                'bg-slate-100 text-slate-800 border-slate-300': isGerencia(),
                'bg-emerald-50 text-emerald-700 border-emerald-200': !isAuditor() && !isGerencia()
              }">
              [{{ userRoleBadge() }}]
            </span>
          </div>
          <p class="text-sm text-slate-500 mt-1">
            @if (isAuditor()) {
              Fiscalización independiente de la red logística: control de alertas de quiebre, auditoría de rutas y verificación de custodia digital.
            } @else if (isGerencia()) {
              Supervisión ejecutiva de la red de distribución, simulación de políticas y administración de accesos del sistema.
            } @else {
              Ciclo operativo guiado: sincronización de catálogos maestros, análisis de rotura de inventario y optimización de rutas.
            }
          </p>
        </div>

        <div class="flex items-center gap-3">
          <button
            type="button"
            (click)="refreshPulse()"
            [disabled]="loadingPulse()"
            class="inline-flex items-center text-xs font-semibold px-3 py-2 rounded-lg border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 transition-colors shadow-xs disabled:opacity-50">
            <span>{{ loadingPulse() ? 'Actualizando...' : 'Actualizar Estado' }}</span>
          </button>
        </div>
      </div>

      <!-- Tarjetas de Pulso de la Red en Tiempo Real (KPIs del Sistema) -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Puntos Hospitalarios -->
        <div class="p-4 rounded-xl bg-white border border-slate-200 shadow-xs">
          <div class="flex items-center justify-between">
            <span class="text-xs font-semibold text-slate-500 uppercase tracking-wide">Red Hospitalaria</span>
            <span class="text-[10px] font-mono font-bold px-2 py-0.5 rounded bg-slate-100 text-slate-700">
              Puntos Activos
            </span>
          </div>
          <div class="mt-2 flex items-baseline gap-2">
            <span class="text-2xl font-bold font-mono text-slate-900">{{ servicePointsCount() }}</span>
            <span class="text-xs text-slate-500">centros de salud</span>
          </div>
          <p class="text-[11px] text-slate-500 mt-2">
            {{ warehousesCount() }} bodegas base de suministro registradas.
          </p>
        </div>

        <!-- Alertas de Stock -->
        <div class="p-4 rounded-xl bg-white border border-slate-200 shadow-xs">
          <div class="flex items-center justify-between">
            <span class="text-xs font-semibold text-slate-500 uppercase tracking-wide">Riesgo de Stock</span>
            <span 
              class="text-[10px] font-mono font-bold px-2 py-0.5 rounded"
              [ngClass]="criticalRisksCount() > 0 ? 'bg-rose-50 text-rose-700 border border-rose-200' : 'bg-emerald-50 text-emerald-700 border border-emerald-200'">
              {{ criticalRisksCount() > 0 ? criticalRisksCount() + ' Críticos' : 'Normal' }}
            </span>
          </div>
          <div class="mt-2 flex items-baseline gap-2">
            <span class="text-2xl font-bold font-mono" [ngClass]="criticalRisksCount() > 0 ? 'text-rose-700' : 'text-slate-900'">
              {{ criticalRisksCount() }}
            </span>
            <span class="text-xs text-slate-500">quiebres inminentes</span>
          </div>
          <p class="text-[11px] text-slate-500 mt-2">
            {{ warningRisksCount() }} centros adicionales en margen preventivo.
          </p>
        </div>

        <!-- Rutas del Día -->
        <div class="p-4 rounded-xl bg-white border border-slate-200 shadow-xs">
          <div class="flex items-center justify-between">
            <span class="text-xs font-semibold text-slate-500 uppercase tracking-wide">Distribución</span>
            <span class="text-[10px] font-mono font-bold px-2 py-0.5 rounded bg-primary-50 text-primary-700 border border-primary-200">
              Rutas Activas
            </span>
          </div>
          <div class="mt-2 flex items-baseline gap-2">
            <span class="text-2xl font-bold font-mono text-primary-700">{{ routePlansCount() }}</span>
            <span class="text-xs text-slate-500">rutas generadas</span>
          </div>
          <p class="text-[11px] text-slate-500 mt-2">
            {{ publishedRoutesCount() }} publicadas a conductores para entrega.
          </p>
        </div>

        <!-- Modo de Autonomía -->
        <div class="p-4 rounded-xl bg-white border border-slate-200 shadow-xs">
          <div class="flex items-center justify-between">
            <span class="text-xs font-semibold text-slate-500 uppercase tracking-wide">Motor Logístico</span>
            <span class="text-[10px] font-mono font-bold px-2 py-0.5 rounded bg-slate-100 text-slate-700">
              Política
            </span>
          </div>
          <div class="mt-2 flex items-baseline gap-2">
            <span class="text-base font-bold font-mono text-slate-800">
              {{ autonomyMode() }}
            </span>
          </div>
          <p class="text-[11px] text-slate-500 mt-2">
            Algoritmo determinista con límite de desvío y sobrecosto.
          </p>
        </div>
      </div>

      <!-- SECCIÓN ESPECÍFICA PARA AUDITOR -->
      @if (isAuditor()) {
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <div>
              <h2 class="text-base font-bold text-slate-900">
                Frentes de Fiscalización para Auditoría
              </h2>
              <p class="text-xs text-slate-500 mt-0.5">
                Accede a las vistas especializadas de inspección y trazabilidad conforme a la normativa.
              </p>
            </div>
            <span class="text-xs font-semibold px-2 py-1 rounded bg-indigo-50 text-indigo-700 border border-indigo-200 font-mono">
              Solo Lectura
            </span>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
            <!-- 1. Alertas de Cobertura -->
            <div class="p-5 rounded-xl bg-white border border-slate-200 shadow-xs flex flex-col justify-between space-y-3">
              <div>
                <div class="flex items-center justify-between">
                  <span class="font-bold text-sm text-slate-900">Auditoría de Demanda</span>
                  <span class="text-[10px] font-mono px-2 py-0.5 rounded bg-rose-50 text-rose-700 border border-rose-200 font-semibold">
                    {{ criticalRisksCount() }} Alertas
                  </span>
                </div>
                <p class="text-xs text-slate-500 mt-2 leading-relaxed">
                  Fiscaliza el cálculo determinístico de días de cobertura contra el umbral normativo (Lead Time + Margen) y audita el volumen sugerido.
                </p>
              </div>
              <a
                routerLink="/alertas"
                class="inline-flex items-center justify-center text-xs font-semibold px-4 py-2 rounded-lg bg-slate-900 text-white hover:bg-slate-800 transition-colors">
                Auditar Alertas de Stock
              </a>
            </div>

            <!-- 2. Fiscalización de Rutas -->
            <div class="p-5 rounded-xl bg-white border border-slate-200 shadow-xs flex flex-col justify-between space-y-3">
              <div>
                <div class="flex items-center justify-between">
                  <span class="font-bold text-sm text-slate-900">Fiscalización de Rutas</span>
                  <span class="text-[10px] font-mono px-2 py-0.5 rounded bg-blue-50 text-blue-700 border border-blue-200 font-semibold">
                    {{ routePlansCount() }} Rutas
                  </span>
                </div>
                <p class="text-xs text-slate-500 mt-2 leading-relaxed">
                  Verifica tiempos de viaje, ventanas horarias de atención en centros de salud, kilometraje total y asignación de vehículos.
                </p>
              </div>
              <a
                routerLink="/rutas"
                class="inline-flex items-center justify-center text-xs font-semibold px-4 py-2 rounded-lg bg-slate-900 text-white hover:bg-slate-800 transition-colors">
                Auditar Itinerario de Rutas
              </a>
            </div>

            <!-- 3. Trazabilidad Pública QR -->
            <div class="p-5 rounded-xl bg-white border border-slate-200 shadow-xs flex flex-col justify-between space-y-3">
              <div>
                <div class="flex items-center justify-between">
                  <span class="font-bold text-sm text-slate-900">Custodia Digital</span>
                  <span class="text-[10px] font-mono px-2 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-200 font-semibold">
                    QR Seguro
                  </span>
                </div>
                <p class="text-xs text-slate-500 mt-2 leading-relaxed">
                  Comprueba la cadena de custodia de bultos y transferencias con firma criptográfica sin exposición de datos confidenciales.
                </p>
              </div>
              <a
                routerLink="/public-trace"
                class="inline-flex items-center justify-center text-xs font-semibold px-4 py-2 rounded-lg bg-slate-900 text-white hover:bg-slate-800 transition-colors">
                Consultar Trazabilidad QR
              </a>
            </div>
          </div>
        </div>
      }

      <!-- FLUJO OPERATIVO GUIADO (Para Planificador y Referencia Arquitectónica) -->
      <div class="space-y-4">
        <div>
          <h2 class="text-base font-bold text-slate-900">
            Ciclo del Proceso Logístico
          </h2>
          <p class="text-xs text-slate-500 mt-0.5">
            Secuencia metodológica de operaciones: desde la ingesta de datos hasta la optimización y distribución.
          </p>
        </div>

        <div class="space-y-3">
          @for (step of steps(); track step.id) {
            <div class="bg-white border border-slate-200 rounded-xl p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-xs">
              <div class="flex items-start gap-4">
                <div class="w-8 h-8 rounded-full flex items-center justify-center font-bold text-sm shrink-0 bg-slate-100 text-slate-700">
                  {{ step.id }}
                </div>
                <div>
                  <div class="flex items-center gap-2">
                    <h3 class="text-sm font-bold text-slate-900">{{ step.title }}</h3>
                    <span 
                      class="text-[10px] font-mono px-2 py-0.5 rounded-full font-semibold border"
                      [ngClass]="step.badgeClass">
                      {{ step.badge }}
                    </span>
                  </div>
                  <p class="text-xs text-slate-500 mt-1">{{ step.desc }}</p>
                </div>
              </div>

              <!-- Enlace de acción respetando permisos RBAC (FR-21: Auditor no escribe) -->
              @if (!step.writeOnly || session.canWrite()) {
                <div class="shrink-0 self-end sm:self-auto">
                  <a
                    [routerLink]="step.route"
                    class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-slate-900 text-white hover:bg-slate-800 transition-colors">
                    {{ step.actionText }}
                  </a>
                </div>
              } @else {
                <div class="shrink-0 self-end sm:self-auto">
                  <span class="text-[11px] font-medium text-slate-400 italic">
                    Acceso de solo lectura
                  </span>
                </div>
              }
            </div>
          }
        </div>
      </div>

      <!-- Resumen Institucional y de Entorno -->
      <div class="bg-slate-50 border border-slate-200 rounded-xl p-4 text-xs text-slate-600 flex flex-col sm:flex-row sm:items-center justify-between gap-2">
        <div>
          <span class="font-bold text-slate-800 block">Plataforma Logística ISTPET &mdash; Expo Clean Ecuador 2026</span>
          <span class="text-slate-500 text-[11px]">Conectado a la base centralizada en AWS EC2 (America/Guayaquil)</span>
        </div>
        <div class="font-mono text-[11px] text-slate-500">
          Modo Operativo: {{ isAuditor() ? 'Auditoría' : (isGerencia() ? 'Gerencia' : 'Planificación') }}
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
