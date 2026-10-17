import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { IonContent } from '@ionic/angular/standalone';
import { FIELD_API } from '../../field-api.interface';
import { generateUuidV7 } from '../../uuid';
import { DemoAdvanceRequest, TodayRoutePlanDto } from '@istpetdev/shared-core';
import { FieldPageHeaderComponent } from '../../../shared/field-page-header/field-page-header.component';

@Component({
  selector: 'app-demo-simulation',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IonContent,
    FieldPageHeaderComponent
  ],
  template: `
    <app-field-page-header
      title="Simulador de Rutas (Demo)"
      [syncAction]="false"
      (back)="goBack()" />

    <ion-content class="ion-padding stripe-main-content">
      <!-- Banner explicativo -->
      <div class="demo-banner mb-4">
        <div class="flex items-start gap-2.5">
          <div class="demo-badge">DEMO</div>
          <div class="flex-1">
            <h2 class="text-xs font-bold text-[#04303B] uppercase tracking-wider">
              Ejecución Acelerada de Rutas de Campo
            </h2>
            <p class="text-[11px] text-slate-600 mt-0.5 leading-relaxed">
              Ejecuta al instante las rutas publicadas en el planificador sin requerir captura manual de campo. Los cambios impactan el planificador, inventarios y custodia de forma consistente.
            </p>
          </div>
        </div>
      </div>

      <!-- Feedback de última acción -->
      @if (lastResult(); as result) {
        <div
          class="mb-4 p-3.5 rounded-xl border flex items-start justify-between gap-3 shadow-xs animate-fadeIn"
          [ngClass]="{
            'bg-emerald-50 border-emerald-300 text-emerald-950': result.type === 'success',
            'bg-rose-50 border-rose-300 text-rose-950': result.type === 'error'
          }"
          role="alert">
          <div class="flex-1 space-y-1">
            <div class="flex items-center gap-2">
              <span class="font-bold text-xs uppercase tracking-wide">
                {{ result.type === 'success' ? '✓ Operación Completada' : '✕ Error en Simulación' }}
              </span>
            </div>
            <p class="text-xs leading-relaxed">{{ result.message }}</p>
          </div>
          <button
            type="button"
            (click)="lastResult.set(null)"
            class="text-xs font-bold text-slate-500 hover:text-slate-800 p-1"
            aria-label="Cerrar aviso">
            ✕
          </button>
        </div>
      }

      <!-- Barra de acciones superiores -->
      <div class="flex items-center justify-between mb-3 px-1">
        <span class="text-xs font-bold text-slate-700 uppercase tracking-wider">
          Rutas Publicadas de Hoy ({{ routes().length }})
        </span>
        <button
          type="button"
          (click)="loadRoutes()"
          [disabled]="loading()"
          class="text-xs font-bold text-[#00A7C4] hover:text-[#008CA5] flex items-center gap-1">
          <svg class="w-3.5 h-3.5" [class.animate-spin]="loading()" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.038 8.038 0 01-15.357-2m15.357 2H15" />
          </svg>
          {{ loading() ? 'Actualizando...' : 'Refrescar' }}
        </button>
      </div>

      <!-- Estado: Cargando -->
      @if (loading() && routes().length === 0) {
        <div class="stripe-card text-center py-10">
          <div class="inline-block w-6 h-6 border-2 border-[#00A7C4] border-t-transparent rounded-full animate-spin mb-2"></div>
          <p class="text-xs text-slate-500">Consultando rutas del día...</p>
        </div>
      }

      <!-- Estado: Error de carga -->
      @else if (error()) {
        <div class="stripe-card text-center py-8 border-rose-200 bg-rose-50/50">
          <p class="text-xs font-medium text-rose-800">{{ error() }}</p>
          <button
            type="button"
            (click)="loadRoutes()"
            class="mt-3 px-4 py-1.5 bg-[#00A7C4] text-white text-xs font-bold rounded-lg shadow-xs">
            Reintentar
          </button>
        </div>
      }

      <!-- Estado: Lista Vacía -->
      @else if (routes().length === 0) {
        <div class="stripe-card text-center py-8">
          <div class="w-10 h-10 mx-auto mb-2 text-slate-300">
            <svg fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
            </svg>
          </div>
          <p class="text-xs font-bold text-slate-700">No hay rutas publicadas hoy</p>
          <p class="text-[11px] text-slate-500 mt-1 max-w-xs mx-auto">
            El planificador debe publicar una ruta para que aparezca aquí y pueda ser simulada.
          </p>
        </div>
      }

      <!-- Lista de Rutas -->
      <div class="space-y-4">
        @for (route of routes(); track route.routePlanId) {
          <div class="stripe-card p-4">
            <!-- Encabezado de la tarjeta -->
            <div class="flex items-start justify-between gap-2 mb-2">
              <div>
                <span class="text-[10px] font-bold uppercase tracking-wider text-slate-500 font-mono">
                  {{ route.code }}
                </span>
                <h3 class="text-sm font-black text-[#0A2540] tracking-tight">
                  {{ route.vehicleCode || 'Sin Vehículo' }} · {{ route.driverName || 'Sin Conductor Asignado' }}
                </h3>
              </div>
              <span
                class="status-pill text-[10px] font-bold px-2 py-0.5 rounded-full"
                [ngClass]="{
                  'bg-emerald-100 text-emerald-800': isCompleted(route),
                  'bg-cyan-100 text-[#006678]': isInProgress(route),
                  'bg-slate-100 text-slate-700': isPending(route)
                }">
                {{ isCompleted(route) ? 'Completada' : isInProgress(route) ? 'En Curso' : 'Publicada' }}
              </span>
            </div>

            <!-- Barra de Progreso de Paradas -->
            <div class="mb-3">
              <div class="flex items-center justify-between text-[11px] font-medium text-slate-600 mb-1">
                <span>Avance de paradas</span>
                <span class="font-bold text-slate-800">
                  {{ route.completedStops }} / {{ route.totalStops }}
                </span>
              </div>
              <div class="w-full bg-slate-100 rounded-full h-2 overflow-hidden">
                <div
                  class="h-full rounded-full transition-all duration-300"
                  [ngClass]="isCompleted(route) ? 'bg-emerald-500' : 'bg-[#00A7C4]'"
                  [style.width.%]="getProgressPercentage(route)">
                </div>
              </div>
            </div>

            <!-- Indicador del Próximo Checkpoint -->
            <div class="next-checkpoint-box mb-3 p-2.5 rounded-lg bg-slate-50 border border-slate-200">
              <span class="text-[10px] uppercase font-bold text-slate-500 block">
                Próximo Checkpoint:
              </span>
              <span class="text-xs font-bold text-slate-800">
                {{ route.nextCheckpoint || (isCompleted(route) ? 'Ruta finalizada' : 'Preparación en bodega') }}
              </span>
            </div>

            <!-- Opciones y Botones de Ejecución -->
            @if (!isCompleted(route)) {
              <div class="pt-2 border-t border-slate-100 space-y-3">
                <!-- Checkbox de Novedades Aleatorias -->
                <label class="flex items-start gap-2 text-xs text-slate-700 cursor-pointer select-none">
                  <input
                    type="checkbox"
                    [(ngModel)]="withIncidents"
                    class="mt-0.5 rounded border-slate-300 text-[#00A7C4] focus:ring-[#00A7C4]" />
                  <span>
                    <strong>Generar novedades aleatorias</strong> (rechazo parcial, ajuste de percha, pedido de reposición)
                    <span class="text-[10px] text-slate-400 block">Aplica a modos Completa y Media ruta (semilla reproducible).</span>
                  </span>
                </label>

                <!-- 3 Botones de Acción -->
                <div class="grid grid-cols-3 gap-2 pt-1">
                  <!-- Botón Completa -->
                  <button
                    type="button"
                    (click)="runMode(route, 'complete')"
                    [disabled]="actionInProgress() !== null"
                    class="sim-action-btn bg-[#00A7C4] hover:bg-[#008CA5] text-white">
                    @if (actionInProgress() === route.routePlanId + '-complete') {
                      <span class="inline-block w-3 h-3 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                    } @else {
                      <span class="btn-title">Completa</span>
                      <span class="btn-sub">Todas las paradas</span>
                    }
                  </button>

                  <!-- Botón Media ruta -->
                  <button
                    type="button"
                    (click)="runMode(route, 'half')"
                    [disabled]="actionInProgress() !== null"
                    class="sim-action-btn bg-slate-700 hover:bg-slate-800 text-white">
                    @if (actionInProgress() === route.routePlanId + '-half') {
                      <span class="inline-block w-3 h-3 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                    } @else {
                      <span class="btn-title">Media ruta</span>
                      <span class="btn-sub">La mitad (⌈N/2⌉)</span>
                    }
                  </button>

                  <!-- Botón Paso a paso -->
                  <button
                    type="button"
                    (click)="runMode(route, 'next')"
                    [disabled]="actionInProgress() !== null"
                    class="sim-action-btn bg-amber-500 hover:bg-amber-600 text-white">
                    @if (actionInProgress() === route.routePlanId + '-next') {
                      <span class="inline-block w-3 h-3 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                    } @else {
                      <span class="btn-title">Paso a paso</span>
                      <span class="btn-sub">1 checkpoint</span>
                    }
                  </button>
                </div>
              </div>
            } @else {
              <div class="pt-2 border-t border-slate-100 flex items-center justify-between text-xs text-emerald-800 font-medium">
                <span class="flex items-center gap-1">
                  <svg class="w-4 h-4 text-emerald-600" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
                  </svg>
                  Todas las paradas completadas
                </span>
                <span class="text-[10px] text-slate-400">Ruta lista en planificador</span>
              </div>
            }
          </div>
        }
      </div>

      <div class="h-6"></div>
    </ion-content>
  `,
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      background-color: #04303B;
    }

    .stripe-main-content {
      --background: #F8FAFC;
      flex: 1;
    }

    .demo-banner {
      background: #E6F8FB;
      border: 1px solid #B4E8F0;
      border-radius: 12px;
      padding: 12px 14px;
    }

    .demo-badge {
      background: #00A7C4;
      color: #fff;
      font-size: 9px;
      font-weight: 900;
      letter-spacing: 0.08em;
      padding: 3px 6px;
      border-radius: 6px;
    }

    .stripe-card {
      background: #ffffff;
      border: 1px solid #E3E8EF;
      border-radius: 14px;
      box-shadow: 0 1px 3px rgba(10, 37, 64, 0.04);
    }

    .sim-action-btn {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 8px 6px;
      border-radius: 10px;
      border: none;
      cursor: pointer;
      font-family: inherit;
      transition: background-color 0.15s, transform 0.1s;
    }

    .sim-action-btn:active:not(:disabled) {
      transform: scale(0.97);
    }

    .sim-action-btn:disabled {
      opacity: 0.55;
      cursor: not-allowed;
    }

    .btn-title {
      font-size: 11px;
      font-weight: 800;
      letter-spacing: -0.01em;
      line-height: 1.2;
    }

    .btn-sub {
      font-size: 9px;
      opacity: 0.85;
      margin-top: 2px;
      white-space: nowrap;
    }

    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(-4px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .animate-fadeIn {
      animation: fadeIn 200ms ease-out;
    }
  `]
})
export class DemoSimulationComponent implements OnInit {
  private readonly fieldApi = inject(FIELD_API);
  private readonly router = inject(Router);

  readonly routes = signal<TodayRoutePlanDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly withIncidents = signal(true);
  readonly actionInProgress = signal<string | null>(null);
  readonly lastResult = signal<{ message: string; type: 'success' | 'error' } | null>(null);

  ngOnInit(): void {
    this.loadRoutes();
  }

  goBack(): void {
    this.router.navigate(['/home']);
  }

  loadRoutes(): void {
    this.loading.set(true);
    this.error.set(null);
    this.fieldApi.getTodayRoutePlans().subscribe({
      next: (data) => {
        this.routes.set(data || []);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.detail || err?.message || 'No se pudieron consultar las rutas del día.');
      }
    });
  }

  isCompleted(route: TodayRoutePlanDto): boolean {
    return route.totalStops > 0 && route.completedStops >= route.totalStops;
  }

  isInProgress(route: TodayRoutePlanDto): boolean {
    return route.completedStops > 0 && route.completedStops < route.totalStops;
  }

  isPending(route: TodayRoutePlanDto): boolean {
    return route.completedStops === 0;
  }

  getProgressPercentage(route: TodayRoutePlanDto): number {
    if (!route.totalStops || route.totalStops === 0) return 0;
    return Math.min(100, Math.round((route.completedStops / route.totalStops) * 100));
  }

  runMode(route: TodayRoutePlanDto, mode: 'complete' | 'half' | 'next'): void {
    const actionKey = `${route.routePlanId}-${mode}`;
    this.actionInProgress.set(actionKey);
    this.lastResult.set(null);

    const operationId = typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function'
      ? crypto.randomUUID()
      : generateUuidV7();
    const req: DemoAdvanceRequest = {
      operationId,
      mode,
      withIncidents: mode !== 'next' ? this.withIncidents() : false,
      noveltyRate: 0.25
    };

    this.fieldApi.advanceDemo(route.routePlanId, req).subscribe({
      next: (resp) => {
        this.actionInProgress.set(null);
        let msg = '';
        if (mode === 'complete') {
          msg = `Ruta completada: ${resp.completedStopsCount} paradas registradas a las horas planificadas.`;
        } else if (mode === 'half') {
          msg = `Media ruta ejecutada: ${resp.completedStopsCount} paradas hechas. ${resp.nextCheckpoint ? 'Próximo: ' + resp.nextCheckpoint : ''}`;
        } else {
          msg = `${resp.checkpointCompleted || 'Checkpoint completado'}. Próximo: ${resp.nextCheckpoint || 'Siguiente'}`;
        }
        this.lastResult.set({ message: msg, type: 'success' });
        this.loadRoutes();
      },
      error: (err) => {
        this.actionInProgress.set(null);
        const apiErrors = err?.error?.errors
          ? Object.values(err.error.errors).flat().join(' ')
          : null;
        const errMsg = err?.error?.detail || apiErrors || err?.error?.message || err?.message || 'Error al ejecutar la simulación de ruta.';
        this.lastResult.set({ message: errMsg, type: 'error' });
      }
    });
  }
}
