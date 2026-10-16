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
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
      <!-- Encabezado Principal Ejecutivo Estilo Stripe -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-200/80 pb-6">
        <div>
          <div class="flex items-center gap-2.5">
            <h1 class="text-2xl sm:text-3xl font-bold text-slate-900 tracking-tight">
              @if (isAuditor()) {
                Consola de Fiscalización y Control de Operaciones
              } @else if (isGerencia()) {
                Tablero Directivo y Gestión Estratégica
              } @else {
                Flujo de Trabajo Operativo y Despacho Logístico
              }
            </h1>
            <span 
              class="inline-flex items-center gap-1.5 text-[11px] font-semibold px-2.5 py-0.5 rounded-full border shadow-2xs font-mono"
              [ngClass]="{
                'bg-indigo-50 text-indigo-700 border-indigo-200/80': isAuditor(),
                'bg-slate-100 text-slate-800 border-slate-200': isGerencia(),
                'bg-emerald-50 text-emerald-700 border-emerald-200/80': !isAuditor() && !isGerencia()
              }">
              <span class="w-1.5 h-1.5 rounded-full" [ngClass]="{
                'bg-indigo-500': isAuditor(),
                'bg-cyan-500': isGerencia(),
                'bg-emerald-500': !isAuditor() && !isGerencia()
              }"></span>
              {{ userRoleBadge() }}
            </span>
          </div>
          <p class="text-sm text-slate-500 mt-1.5 max-w-3xl leading-relaxed">
            @if (isAuditor()) {
              Fiscalización independiente de la red logística: control de alertas de quiebre, auditoría de rutas y verificación de custodia digital.
            } @else if (isGerencia()) {
              Supervisión ejecutiva de la red de distribución, simulación de políticas y administración de accesos del sistema.
            } @else {
              Ciclo operativo guiado: sincronización de catálogos maestros, análisis de rotura de inventario y optimización de rutas.
            }
          </p>
        </div>

        <div class="flex items-center gap-3 shrink-0">
          <button
            type="button"
            (click)="refreshPulse()"
            [disabled]="loadingPulse()"
            class="inline-flex items-center gap-2 text-xs font-semibold px-3.5 py-2 rounded-lg border border-slate-200 bg-white text-slate-700 hover:bg-slate-50 hover:border-slate-300 transition-all shadow-xs disabled:opacity-50 cursor-pointer">
            <svg 
              class="w-3.5 h-3.5 text-slate-500 transition-transform" 
              [ngClass]="{'animate-spin text-cyan-600': loadingPulse()}"
              xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            <span>{{ loadingPulse() ? 'Actualizando datos...' : 'Actualizar Estado' }}</span>
          </button>
        </div>
      </div>

      <!-- Layout en dos columnas: Principal (8 cols) y Panel de Recomendaciones & Sistema (4 cols) -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
        
        <!-- Columna Izquierda / Central: Métricas y Flujo Operativo -->
        <div class="lg:col-span-8 space-y-8">
          
          <!-- Cuadrícula de KPIs Ejecutivos Estilo Stripe -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <!-- 1. Red Hospitalaria -->
            <div class="p-5 rounded-xl bg-white border border-slate-200/90 shadow-xs hover:border-slate-300 hover:shadow-sm transition-all duration-200">
              <div class="flex items-center justify-between">
                <span class="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">Red Hospitalaria</span>
                <span class="text-[10px] font-mono font-semibold px-2 py-0.5 rounded-md bg-slate-100 text-slate-700 border border-slate-200/60">
                  Puntos Activos
                </span>
              </div>
              <div class="mt-3 flex items-baseline gap-2">
                <span class="text-3xl font-bold font-mono text-slate-900 tracking-tight">{{ servicePointsCount() }}</span>
                <span class="text-xs font-medium text-slate-500">centros de salud</span>
              </div>
              <div class="mt-2.5 pt-2.5 border-t border-slate-100 flex items-center justify-between text-[11px] text-slate-500">
                <span>{{ warehousesCount() }} bodegas base</span>
                <span class="font-medium text-cyan-600">Suministro activo</span>
              </div>
            </div>

            <!-- 2. Riesgo de Stock -->
            <div class="p-5 rounded-xl bg-white border border-slate-200/90 shadow-xs hover:border-slate-300 hover:shadow-sm transition-all duration-200">
              <div class="flex items-center justify-between">
                <span class="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">Riesgo de Stock</span>
                <span 
                  class="text-[10px] font-mono font-semibold px-2 py-0.5 rounded-md border"
                  [ngClass]="criticalRisksCount() > 0 ? 'bg-rose-50 text-rose-700 border-rose-200/80' : 'bg-emerald-50 text-emerald-700 border-emerald-200/80'">
                  {{ criticalRisksCount() > 0 ? criticalRisksCount() + ' Críticos' : 'Normal' }}
                </span>
              </div>
              <div class="mt-3 flex items-baseline gap-2">
                <span class="text-3xl font-bold font-mono tracking-tight" [ngClass]="criticalRisksCount() > 0 ? 'text-rose-600' : 'text-slate-900'">
                  {{ criticalRisksCount() }}
                </span>
                <span class="text-xs font-medium text-slate-500">quiebres inminentes</span>
              </div>
              <div class="mt-2.5 pt-2.5 border-t border-slate-100 flex items-center justify-between text-[11px] text-slate-500">
                <span>{{ warningRisksCount() }} adicionales en margen</span>
                <a routerLink="/alertas" class="font-medium text-rose-600 hover:underline">Ver detalle &rarr;</a>
              </div>
            </div>

            <!-- 3. Distribución y Rutas -->
            <div class="p-5 rounded-xl bg-white border border-slate-200/90 shadow-xs hover:border-slate-300 hover:shadow-sm transition-all duration-200">
              <div class="flex items-center justify-between">
                <span class="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">Distribución</span>
                <span class="text-[10px] font-mono font-semibold px-2 py-0.5 rounded-md bg-cyan-50 text-cyan-700 border border-cyan-200/80">
                  Rutas Activas
                </span>
              </div>
              <div class="mt-3 flex items-baseline gap-2">
                <span class="text-3xl font-bold font-mono text-cyan-600 tracking-tight">{{ routePlansCount() }}</span>
                <span class="text-xs font-medium text-slate-500">rutas generadas</span>
              </div>
              <div class="mt-2.5 pt-2.5 border-t border-slate-100 flex items-center justify-between text-[11px] text-slate-500">
                <span>{{ publishedRoutesCount() }} publicadas a conductores</span>
                <a routerLink="/rutas" class="font-medium text-cyan-600 hover:underline">Gestionar &rarr;</a>
              </div>
            </div>

            <!-- 4. Motor Logístico -->
            <div class="p-5 rounded-xl bg-white border border-slate-200/90 shadow-xs hover:border-slate-300 hover:shadow-sm transition-all duration-200">
              <div class="flex items-center justify-between">
                <span class="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">Motor Logístico</span>
                <span class="text-[10px] font-mono font-semibold px-2 py-0.5 rounded-md bg-slate-100 text-slate-700 border border-slate-200/60">
                  Política
                </span>
              </div>
              <div class="mt-3 flex items-baseline gap-2">
                <span class="text-xl font-bold font-mono text-slate-800 tracking-tight">
                  {{ autonomyMode() }}
                </span>
              </div>
              <div class="mt-2.5 pt-2.5 border-t border-slate-100 text-[11px] text-slate-500 truncate">
                Límite de desvío y sobrecosto controlado
              </div>
            </div>
          </div>

          <!-- SECCIÓN ESPECÍFICA PARA AUDITOR (SI APLICA) -->
          @if (isAuditor()) {
            <div class="space-y-4">
              <div class="flex items-center justify-between">
                <div>
                  <h2 class="text-base font-bold text-slate-900 tracking-tight">
                    Frentes de Fiscalización para Auditoría
                  </h2>
                  <p class="text-xs text-slate-500 mt-0.5">
                    Acceso a las vistas especializadas de inspección y trazabilidad conforme a la normativa.
                  </p>
                </div>
                <span class="text-xs font-semibold px-2.5 py-1 rounded-md bg-indigo-50 text-indigo-700 border border-indigo-200 font-mono">
                  Solo Lectura
                </span>
              </div>

              <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                <!-- 1. Alertas de Cobertura -->
                <div class="p-5 rounded-xl bg-white border border-slate-200/90 shadow-xs flex flex-col justify-between space-y-4">
                  <div>
                    <div class="flex items-center justify-between">
                      <span class="font-bold text-sm text-slate-900">Auditoría de Demanda</span>
                      <span class="text-[10px] font-mono px-2 py-0.5 rounded bg-rose-50 text-rose-700 border border-rose-200 font-semibold">
                        {{ criticalRisksCount() }} Alertas
                      </span>
                    </div>
                    <p class="text-xs text-slate-500 mt-2 leading-relaxed">
                      Fiscaliza el cálculo determinístico de días de cobertura contra el umbral normativo y audita el volumen sugerido.
                    </p>
                  </div>
                  <a
                    routerLink="/alertas"
                    class="inline-flex items-center justify-center text-xs font-semibold px-3.5 py-2 rounded-lg bg-[#0A2540] text-white hover:bg-slate-800 transition-colors shadow-2xs">
                    Auditar Alertas de Stock
                  </a>
                </div>

                <!-- 2. Fiscalización de Rutas -->
                <div class="p-5 rounded-xl bg-white border border-slate-200/90 shadow-xs flex flex-col justify-between space-y-4">
                  <div>
                    <div class="flex items-center justify-between">
                      <span class="font-bold text-sm text-slate-900">Fiscalización de Rutas</span>
                      <span class="text-[10px] font-mono px-2 py-0.5 rounded bg-cyan-50 text-cyan-700 border border-cyan-200 font-semibold">
                        {{ routePlansCount() }} Rutas
                      </span>
                    </div>
                    <p class="text-xs text-slate-500 mt-2 leading-relaxed">
                      Verifica tiempos de viaje, ventanas horarias de atención, kilometraje total y asignación de vehículos.
                    </p>
                  </div>
                  <a
                    routerLink="/rutas"
                    class="inline-flex items-center justify-center text-xs font-semibold px-3.5 py-2 rounded-lg bg-[#0A2540] text-white hover:bg-slate-800 transition-colors shadow-2xs">
                    Auditar Itinerario de Rutas
                  </a>
                </div>

                <!-- 3. Trazabilidad Pública QR -->
                <div class="p-5 rounded-xl bg-white border border-slate-200/90 shadow-xs flex flex-col justify-between space-y-4">
                  <div>
                    <div class="flex items-center justify-between">
                      <span class="font-bold text-sm text-slate-900">Custodia Digital</span>
                      <span class="text-[10px] font-mono px-2 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-200 font-semibold">
                        QR Seguro
                      </span>
                    </div>
                    <p class="text-xs text-slate-500 mt-2 leading-relaxed">
                      Comprueba la cadena de custodia de bultos y transferencias con firma criptográfica sin datos confidenciales.
                    </p>
                  </div>
                  <a
                    routerLink="/public-trace"
                    class="inline-flex items-center justify-center text-xs font-semibold px-3.5 py-2 rounded-lg bg-[#0A2540] text-white hover:bg-slate-800 transition-colors shadow-2xs">
                    Consultar Trazabilidad QR
                  </a>
                </div>
              </div>
            </div>
          }

          <!-- FLUJO OPERATIVO GUIADO (ESTILO STRIPE RESUMEN) -->
          <div class="space-y-4">
            <div class="flex items-center justify-between border-b border-slate-200/80 pb-3">
              <div>
                <h2 class="text-base font-bold text-slate-900 tracking-tight">
                  Ciclo del Proceso Logístico
                </h2>
                <p class="text-xs text-slate-500 mt-0.5">
                  Secuencia metodológica de operaciones: desde la ingesta de datos hasta la optimización y distribución.
                </p>
              </div>
              <span class="hidden sm:inline-flex text-[11px] font-mono text-slate-400">
                4 etapas configuradas
              </span>
            </div>

            <div class="space-y-3">
              @for (step of steps(); track step.id) {
                <div class="bg-white border border-slate-200/90 rounded-xl p-4 sm:p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-xs hover:border-slate-300 hover:shadow-sm transition-all duration-200 group">
                  <div class="flex items-start gap-4">
                    <div class="w-8 h-8 rounded-full flex items-center justify-center font-bold text-xs shrink-0 bg-slate-100 text-slate-700 border border-slate-200 group-hover:bg-cyan-50 group-hover:text-cyan-700 group-hover:border-cyan-200 transition-colors">
                      {{ step.id }}
                    </div>
                    <div>
                      <div class="flex items-center gap-2 flex-wrap">
                        <h3 class="text-sm font-bold text-slate-900 group-hover:text-cyan-700 transition-colors">{{ step.title }}</h3>
                        <span 
                          class="text-[10px] font-mono px-2 py-0.5 rounded-md font-semibold border"
                          [ngClass]="step.badgeClass">
                          {{ step.badge }}
                        </span>
                      </div>
                      <p class="text-xs text-slate-500 mt-1 leading-relaxed">{{ step.desc }}</p>
                    </div>
                  </div>

                  <!-- Enlace de acción respetando permisos RBAC -->
                  @if (!step.writeOnly || session.canWrite()) {
                    <div class="shrink-0 self-end sm:self-auto">
                      <a
                        [routerLink]="step.route"
                        class="inline-flex items-center gap-1.5 text-xs font-semibold px-4 py-2 rounded-lg bg-[#0A2540] hover:bg-[#008CA5] text-white transition-all shadow-xs group-hover:shadow cursor-pointer">
                        <span>{{ step.actionText }}</span>
                        <span class="text-white/70 group-hover:translate-x-0.5 transition-transform">&rarr;</span>
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
        </div>

        <!-- Columna Derecha: Panel Lateral Ejecutivo (Inspirado en Stripe Image 2) -->
        <div class="lg:col-span-4 space-y-6">
          
          <!-- Widget 1: Recomendaciones Operativas -->
          <div class="bg-white border border-slate-200/90 rounded-xl p-5 shadow-xs space-y-4">
            <div class="flex items-center justify-between border-b border-slate-100 pb-3">
              <div class="flex items-center gap-2">
                <span class="w-2 h-2 rounded-full bg-cyan-500"></span>
                <h3 class="text-sm font-bold text-slate-900">Recomendaciones</h3>
              </div>
              <span class="text-[10px] font-mono uppercase tracking-wide text-slate-400 font-semibold">En Vivo</span>
            </div>

            <div class="space-y-3.5 text-xs">
              <div class="space-y-1">
                <div class="font-semibold text-slate-800">Atención de Stock Crítico</div>
                <p class="text-slate-500 leading-relaxed">
                  {{ criticalRisksCount() > 0 ? 'Existen ' + criticalRisksCount() + ' centros con quiebre inminente en los próximos 5 días.' : 'No se registran quiebres inminentes en la red actual.' }}
                </p>
                <a routerLink="/alertas" class="inline-flex items-center gap-1 font-semibold text-cyan-600 hover:text-cyan-700 hover:underline pt-0.5">
                  Resolver alertas de stock &rarr;
                </a>
              </div>

              <div class="border-t border-slate-100 pt-3 space-y-1">
                <div class="font-semibold text-slate-800">Itinerario y Despacho</div>
                <p class="text-slate-500 leading-relaxed">
                  {{ routePlansCount() }} rutas calculadas con el motor determinista listas para despacho a conductores.
                </p>
                <a routerLink="/rutas" class="inline-flex items-center gap-1 font-semibold text-cyan-600 hover:text-cyan-700 hover:underline pt-0.5">
                  Revisar rutas y paradas &rarr;
                </a>
              </div>

              <div class="border-t border-slate-100 pt-3 space-y-1">
                <div class="font-semibold text-slate-800">Simulación Comparativa</div>
                <p class="text-slate-500 leading-relaxed">
                  Compara la política frente a baselines fijo y reactivo en el simulador a 90 días.
                </p>
                <a routerLink="/simulation" class="inline-flex items-center gap-1 font-semibold text-cyan-600 hover:text-cyan-700 hover:underline pt-0.5">
                  Abrir simulador 90d &rarr;
                </a>
              </div>
            </div>
          </div>

          <!-- Widget 2: Estado del Sistema & Entorno AWS (Inspirado en Claves de API / Detalles de Stripe) -->
          <div class="bg-white border border-slate-200/90 rounded-xl p-5 shadow-xs space-y-4">
            <div class="flex items-center justify-between border-b border-slate-100 pb-3">
              <h3 class="text-sm font-bold text-slate-900">Entorno del Sistema</h3>
              <span class="inline-flex items-center gap-1 text-[10px] font-mono font-semibold px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200/80">
                <span class="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse"></span>
                Operativo
              </span>
            </div>

            <div class="space-y-2.5 text-xs">
              <div class="flex items-center justify-between py-1 border-b border-slate-50">
                <span class="text-slate-500">Base de Datos</span>
                <span class="font-mono text-slate-800 font-semibold">AWS EC2 (Nube)</span>
              </div>
              <div class="flex items-center justify-between py-1 border-b border-slate-50">
                <span class="text-slate-500">Motor Logístico</span>
                <span class="font-mono text-slate-800 font-semibold">{{ autonomyMode() }}</span>
              </div>
              <div class="flex items-center justify-between py-1 border-b border-slate-50">
                <span class="text-slate-500">Zona Horaria</span>
                <span class="font-mono text-slate-800">America/Guayaquil</span>
              </div>
              <div class="flex items-center justify-between py-1 border-b border-slate-50">
                <span class="text-slate-500">Jornada Operativa</span>
                <span class="font-mono text-slate-800">07:00 — 17:00 (10h)</span>
              </div>
              <div class="flex items-center justify-between py-1">
                <span class="text-slate-500">Custodia Digital</span>
                <span class="font-mono text-emerald-600 font-semibold">QR Criptográfico</span>
              </div>
            </div>

            <div class="pt-2">
              <a
                routerLink="/public-trace"
                class="w-full inline-flex items-center justify-center gap-1.5 text-xs font-semibold py-2 px-3 rounded-lg border border-slate-200 bg-slate-50 hover:bg-slate-100 text-slate-700 transition-colors">
                <svg class="w-3.5 h-3.5 text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h4.01M16 20h4M4 12h4m12 0h.01M5 8h2a1 1 0 001-1V5a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1zm12 0h2a1 1 0 001-1V5a1 1 0 00-1-1h-2a1 1 0 00-1 1v2a1 1 0 001 1zM5 20h2a1 1 0 001-1v-2a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1z" />
                </svg>
                <span>Portal Trazabilidad QR</span>
              </a>
            </div>
          </div>

          <!-- Pie Institucional Compacto -->
          <div class="rounded-xl p-4 bg-slate-50/80 border border-slate-200/60 text-[11px] text-slate-500 space-y-1">
            <div class="font-semibold text-slate-700">Plataforma Logística ISTPET</div>
            <div>Expo Clean Ecuador 2026 &mdash; Reto 1</div>
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
