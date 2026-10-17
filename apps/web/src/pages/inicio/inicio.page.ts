import { Component, DestroyRef, inject, signal, OnInit, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, map, of, switchMap } from 'rxjs';
import { AuthSession } from '@entities/session';
import { ApiService, ServicePoint, RiskItemDto, RoutePlanSummaryDto, AutonomyModeDto } from '@shared/api';
import { FieldExceptionsApiService, SimulationsApiService } from '@shared/api';
import { operationIndicators } from './operation-indicators';

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
    <div class="max-w-7xl mx-auto px-6 lg:px-8 py-8 space-y-8 bg-white">
      <!-- Encabezado Principal Estilo Stripe: "Hoy" -->
      <div class="flex items-center justify-between pb-4 border-b border-[#E3E8EE]">
        <div class="flex items-baseline gap-3">
          <h1 class="text-2xl sm:text-3xl font-bold text-[#1A2C44] tracking-tight font-sans">
            Hoy
          </h1>
          <span class="text-xs font-semibold px-2.5 py-0.5 rounded-full border font-mono text-[11px]"
            [ngClass]="{
              'bg-[#F1F3F5] text-[#4F5B66] border-[#E3E8EE]': isAuditor(),
              'bg-[#635BFF]/10 text-[#635BFF] border-[#635BFF]/30': isGerencia(),
              'bg-[#00A7C4]/10 text-[#00A7C4] border-[#00A7C4]/30': isPlanificador()
            }">
            {{ userRoleBadge() }}
          </span>
        </div>

        <div class="flex items-center gap-3">
          <button
            type="button"
            (click)="refreshPulse()"
            [disabled]="loadingPulse()"
            class="h-9 inline-flex items-center gap-1.5 text-xs font-medium px-3.5 rounded-md border border-[#E3E8EE] bg-white text-[#2D3748] hover:bg-[#F8FAFC] transition-colors shadow-2xs disabled:opacity-50 cursor-pointer">
            <svg 
              class="w-3.5 h-3.5 text-[#697386]" 
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
            <div class="flex items-center gap-4 text-xs font-medium text-[#697386]">
              <button type="button" class="inline-flex items-center gap-1 hover:text-[#1A2C44] transition-colors cursor-pointer">
                <span>Red Hospitalaria</span>
                <svg class="w-3 h-3 text-[#697386]" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd"/></svg>
              </button>
              <button type="button" class="inline-flex items-center gap-1 hover:text-[#1A2C44] transition-colors cursor-pointer">
                <span>Jornada Operativa</span>
                <svg class="w-3 h-3 text-[#697386]" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd"/></svg>
              </button>
            </div>

            <!-- Número Principal en Sans-serif (sin ceros punteados retro) -->
            <div class="mt-2 flex items-baseline gap-2">
              <span class="text-3xl sm:text-4xl font-semibold text-[#1A2C44] tracking-tight font-sans">
                {{ servicePointsCount() }}
              </span>
              <span class="text-sm font-medium text-[#697386]">
                centros de salud activos
              </span>
            </div>

            <div class="mt-1 flex items-center gap-2 text-xs text-[#697386]">
              <span>{{ warehousesCount() }} bodega base</span>
              <span class="text-[#CBD5E1]">&bull;</span>
              <span class="text-[#00875A] font-medium">100% Red asignada</span>
              <span class="text-[#CBD5E1]">&bull;</span>
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
              <div class="flex justify-between items-center text-[11px] text-[#697386] font-sans mt-2">
                <span>07:00</span>
                <span class="text-[#697386] font-normal">12:00</span>
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
              <span class="text-[#1A2C44] font-medium">Riesgo de Stock</span>
              <a routerLink="/alertas" class="text-[#635BFF] hover:underline font-medium text-[13px]">Ver más</a>
            </div>
            <div class="flex items-center gap-1.5 pt-0.5">
              <span 
                class="text-[20px] leading-[28px] font-semibold tracking-tight font-mono"
                [ngClass]="criticalRisksCount() > 0 ? 'text-[#DF1B41]' : 'text-[#1A2C44]'">
                {{ criticalRisksCount() }} crítico
              </span>
              <span class="text-[13px] text-[#8792A2] cursor-help" title="Insumos con cobertura crítica menor a 5 días">&#9432;</span>
            </div>
            <p class="text-[13px] leading-[18px] text-[#697386]">
              {{ warningRisksCount() }} en margen de reposición
            </p>
          </div>

          <!-- KPI 2: Distribución y Rutas (equivalente a Transferencias en Stripe) -->
          <div class="space-y-0.5 pt-4 border-t border-[#F1F3F5]">
            <div class="flex items-center justify-between text-[14px] leading-[20px]">
              <span class="text-[#1A2C44] font-medium">Distribución</span>
              <a routerLink="/rutas" class="text-[#635BFF] hover:underline font-medium text-[13px]">Ver datos</a>
            </div>
            <div class="flex items-center gap-1.5 pt-0.5">
              <span class="text-[20px] leading-[28px] font-semibold text-[#1A2C44] tracking-tight font-mono">
                {{ publishedRoutesCount() }} publicadas
              </span>
              <span class="text-[13px] text-[#8792A2] cursor-help" title="Rutas activas publicadas para despacho">&#9432;</span>
            </div>
            <p class="text-[13px] leading-[18px] text-[#697386]">
              {{ routePlansCount() }} rutas planificadas hoy
            </p>
          </div>

          <!-- KPI 3: Motor Logístico -->
          <div class="space-y-0.5 pt-4 border-t border-[#F1F3F5]">
            <div class="flex items-center justify-between text-[14px] leading-[20px]">
              <span class="text-[#1A2C44] font-medium">Planificación de Rutas</span>
              <span class="text-[12px] text-[#697386] font-mono">Automática</span>
            </div>
            <div class="flex items-center gap-1.5 pt-0.5">
              <span class="text-[18px] leading-[26px] font-semibold text-[#1A2C44] tracking-tight">
                {{ autonomyMode() === 'AutoWithLimits' ? 'Automático Supervisado' : autonomyMode() }}
              </span>
              <span class="text-[13px] text-[#8792A2] cursor-help" title="El sistema calcula automáticamente las mejores rutas de entrega">&#9432;</span>
            </div>
            <p class="text-[13px] leading-[18px] text-[#697386]">
              Rutas eficientes &bull; Cobertura hospitalaria total
            </p>
          </div>

          <!-- KPI 4: Conectividad y Trazabilidad Segura -->
          <div class="space-y-0.5 pt-4 border-t border-[#F1F3F5]">
            <div class="flex items-center justify-between text-[14px] leading-[20px]">
              <span class="text-[#1A2C44] font-medium">Estado del Sistema</span>
              <a routerLink="/public-trace" class="text-[#635BFF] hover:underline font-medium text-[13px]">Traza QR</a>
            </div>
            <div class="flex items-center gap-2 pt-0.5">
              <span class="w-2 h-2 rounded-full bg-[#00D924] animate-pulse"></span>
              <span class="text-[14px] font-semibold text-[#1A2C44]">
                Servicios en Línea
              </span>
              <span class="text-[11px] text-[#00875A] bg-[#E8F7F0] px-2 py-0.5 rounded font-medium border border-[#B3E6CD]">
                Operativo
              </span>
            </div>
            <p class="text-[12px] text-[#697386]">
              Sincronización en la nube &bull; Alta disponibilidad
            </p>
          </div>

        </div>
      </div>

      <!-- ======================================================== -->
      <!-- PANEL GERENCIA: INDICADORES DE LA OPERACIÓN (Issue #235) -->
      <!-- ======================================================== -->
      @if (isGerencia()) {
        <section aria-labelledby="operation-indicators-title" class="bg-white border border-[#E3E8EE] rounded-xl p-5 shadow-2xs space-y-4">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div>
              <div class="flex items-center gap-2">
                <h2 id="operation-indicators-title" class="text-base font-bold text-[#1A2C44]">Indicadores de la operación</h2>
                <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-[#635BFF]/10 text-[#635BFF] border border-[#635BFF]/30 font-mono">
                  DIRECCIÓN EJECUTIVA
                </span>
              </div>
              <p class="text-xs text-[#697386] mt-0.5">Evidencia disponible y origen de cada indicador estratégico.</p>
            </div>
            <button type="button" (click)="refreshIndicators()" [disabled]="loadingIndicators()"
              class="h-9 inline-flex items-center gap-1.5 text-xs font-medium px-3.5 rounded-md border border-[#E3E8EE] bg-white text-[#2D3748] hover:bg-[#F8FAFC] transition-colors shadow-2xs disabled:opacity-50 cursor-pointer">
              <svg class="w-3.5 h-3.5 text-[#697386]" [ngClass]="{'animate-spin text-[#635BFF]': loadingIndicators()}" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
              <span>Actualizar indicadores</span>
            </button>
          </div>
          @if (indicatorError()) {
            <p role="status" class="text-xs text-[#DF1B41] font-medium bg-[#FEECEC] border border-[#F8B4C0] rounded-md p-2.5">{{ indicatorError() }} Usa «Actualizar indicadores» para reintentar.</p>
          }
          <dl class="divide-y divide-[#F1F3F5]" [attr.aria-busy]="loadingIndicators()">
            @for (indicator of indicators(); track indicator.name) {
              <div class="grid grid-cols-1 sm:grid-cols-3 gap-2 py-3" data-operation-indicator>
                <dt class="text-xs font-semibold text-[#1A2C44]">{{ indicator.name }}</dt>
                <dd class="text-sm font-semibold text-[#1A2C44] tabular-nums font-mono">
                  @if (loadingIndicators()) {
                    <span class="block h-5 w-24 rounded bg-[#F1F3F5] motion-safe:animate-pulse" aria-label="Consultando indicador"></span>
                  } @else if (indicator.value !== null) {
                    {{ indicator.value | number:'1.0-2' }} <span class="font-normal text-xs text-[#697386]">{{ indicator.unit }}</span>
                  } @else {
                    <span class="text-[#8792A2] font-normal italic">sin dato</span>
                  }
                </dd>
                <dd class="text-xs text-[#697386] space-y-1 min-w-0">
                  <span class="inline-block rounded border border-[#E3E8EE] bg-[#F8FAFC] px-2 py-0.5 text-[10px] font-mono font-medium text-[#4F5B66]">{{ indicator.source }}</span>
                  <p class="break-words text-[11px] leading-relaxed">{{ indicator.detail }}</p>
                </dd>
              </div>
            }
          </dl>
          <div class="pt-2 border-t border-[#F1F3F5] flex items-center justify-between text-xs">
            <a routerLink="/simulation" class="font-medium text-[#635BFF] hover:underline inline-flex items-center gap-1">
              <span>Consultar comparaciones de simulación 90d</span>
              <span>&rarr;</span>
            </a>
            <span class="text-[11px] text-[#697386] font-mono">Base determinista: istpetdev-v1</span>
          </div>
        </section>
      }

      <!-- ======================================================== -->
      <!-- PANEL PLANIFICADOR: MONITOR DE DESPACHO & FLOTA          -->
      <!-- ======================================================== -->
      @if (isPlanificador()) {
        <section aria-label="Monitor de Planificación Operativa" class="bg-white border border-[#E3E8EE] rounded-xl p-5 shadow-2xs space-y-4">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-[#F1F3F5] pb-3">
            <div>
              <div class="flex items-center gap-2">
                <h2 class="text-base font-bold text-[#1A2C44]">Monitor de Despacho & Flota</h2>
                <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-[#00A7C4]/10 text-[#00A7C4] border border-[#00A7C4]/30 font-mono">
                  CONTROL DE TRÁFICO
                </span>
              </div>
              <p class="text-xs text-[#697386] mt-0.5">Estado de abastecimiento matutino y asignación de vehículos.</p>
            </div>
            <div class="flex items-center gap-2">
              <a routerLink="/rutas" class="h-9 inline-flex items-center gap-1.5 text-xs font-medium px-3.5 rounded-md bg-[#635BFF] text-white hover:bg-[#5851EA] transition-colors shadow-2xs">
                <span>Gestionar Rutas</span>
                <span>&rarr;</span>
              </a>
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-1">
            <div class="p-3.5 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE] space-y-1">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-[#697386]">Puntos Críticos a Abastecer</span>
              <div class="flex items-baseline justify-between">
                <span class="text-xl font-bold font-mono" [ngClass]="criticalRisksCount() > 0 ? 'text-[#DF1B41]' : 'text-[#00875A]'">
                  {{ criticalRisksCount() }}
                </span>
                <span class="text-xs font-mono text-[#697386]">{{ warningRisksCount() }} en alerta</span>
              </div>
              <p class="text-[11px] text-[#697386]">Hospitales y centros que requieren reposición prioritaria hoy.</p>
            </div>

            <div class="p-3.5 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE] space-y-1">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-[#697386]">Rutas Despachadas vs Planificadas</span>
              <div class="flex items-baseline justify-between">
                <span class="text-xl font-bold font-mono text-[#1A2C44]">
                  {{ publishedRoutesCount() }} / {{ routePlansCount() }}
                </span>
                <span class="text-xs font-mono text-[#00875A] font-medium">
                  {{ routePlansCount() > 0 ? ((publishedRoutesCount() / routePlansCount()) * 100 | number:'1.0-0') : 100 }}%
                </span>
              </div>
              <p class="text-[11px] text-[#697386]">Rutas publicadas a conductores para entrega en campo.</p>
            </div>

            <div class="p-3.5 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE] space-y-1">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-[#697386]">Ventanas de Atención Matutinas</span>
              <div class="flex items-baseline justify-between">
                <span class="text-xl font-bold font-mono text-[#1A2C44]">07:00 – 11:00</span>
                <span class="text-xs font-semibold px-2 py-0.5 rounded bg-[#FFF4E5] text-[#B76E00] border border-[#FFE2B5] font-mono text-[10px]">Prioridad 1</span>
              </div>
              <p class="text-[11px] text-[#697386]">Descargas hospitalarias y centros comerciales con horario restringido.</p>
            </div>
          </div>
        </section>
      }

      <!-- ======================================================== -->
      <!-- PANEL AUDITOR: CONFORMIDAD Y CUSTODIA (ESC-05)           -->
      <!-- ======================================================== -->
      @if (isAuditor()) {
        <section aria-label="Monitor de Control Interno y Auditoría" class="bg-white border border-[#E3E8EE] rounded-xl p-5 shadow-2xs space-y-4">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-[#F1F3F5] pb-3">
            <div>
              <div class="flex items-center gap-2">
                <h2 class="text-base font-bold text-[#1A2C44]">Monitor de Conformidad & Cadena de Custodia</h2>
                <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-[#F1F3F5] text-[#4F5B66] border border-[#E3E8EE] font-mono">
                  CONTROL INTERNO · ESC-05
                </span>
              </div>
              <p class="text-xs text-[#697386] mt-0.5">Auditoría forense de entregas, actas de recepción y novedades en campo (Solo Lectura).</p>
            </div>
            <div class="flex items-center gap-2">
              <a routerLink="/public-trace" class="h-9 inline-flex items-center gap-1.5 text-xs font-medium px-3.5 rounded-md bg-[#635BFF] text-white hover:bg-[#5851EA] transition-colors shadow-2xs">
                <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h4.01M16 20h4M4 12h4m12 0h.01M5 8h2a1 1 0 001-1V5a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1zm12 0h2a1 1 0 001-1V5a1 1 0 00-1-1h-2a1 1 0 00-1 1v2a1 1 0 001 1zM5 20h2a1 1 0 001-1v-2a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1z" />
                </svg>
                <span>Verificar Custodia QR</span>
              </a>
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-1">
            <div class="p-3.5 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE] space-y-1">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-[#697386]">Integridad Criptográfica</span>
              <div class="flex items-baseline justify-between">
                <span class="text-base font-bold font-mono text-[#00875A]">SHA-256</span>
                <span class="text-[10px] font-mono px-2 py-0.5 rounded bg-[#E8F7F0] text-[#00875A] border border-[#B3E6CD]">Verificado</span>
              </div>
              <p class="text-[11px] text-[#697386]">Sellado digital de bultos y actas de entrega hospitalaria.</p>
            </div>

            <div class="p-3.5 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE] space-y-1">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-[#697386]">Bandeja de Incidencias</span>
              <div class="flex items-baseline justify-between">
                <span class="text-base font-bold text-[#1A2C44]">Recepciones</span>
                <a routerLink="/rutas" class="text-xs font-medium text-[#635BFF] hover:underline">Auditar &rarr;</a>
              </div>
              <p class="text-[11px] text-[#697386]">Inspección de firmas, evidencias fotográficas y rechazos de campo.</p>
            </div>

            <div class="p-3.5 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE] space-y-1">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-[#697386]">Protección de Datos</span>
              <div class="flex items-baseline justify-between">
                <span class="text-base font-bold text-[#1A2C44]">Solo Lectura</span>
                <span class="text-[10px] font-mono px-2 py-0.5 rounded bg-[#F1F3F5] text-[#4F5B66] border border-[#E3E8EE]">Inmutable</span>
              </div>
              <p class="text-[11px] text-[#697386]">Garantía de no alteración accidental de rutas ni órdenes maestras.</p>
            </div>
          </div>
        </section>
      }

      <!-- SECCIÓN: Tu resumen (Exacto a Captura 3 de Stripe con Píldoras de Filtro y Tarjetas) -->
      <div class="pt-4 space-y-4 border-t border-[#E3E8EE]">
        <!-- Encabezado de Tu Resumen y Barra de Filtros en Píldora -->
        <div class="space-y-3">
          <div class="flex items-center justify-between">
            <h2 class="text-xl sm:text-2xl font-bold text-[#1A2C44] tracking-tight font-sans">
              Tu resumen
            </h2>
            <!-- Acciones secundarias derecha estilo Stripe -->
            <div class="flex items-center gap-2">
              <button
                type="button"
                class="h-7 px-2.5 rounded-md border border-[#E3E8EE] bg-white text-xs font-medium text-[#2D3748] hover:bg-[#F8FAFC] transition-colors shadow-2xs inline-flex items-center gap-1 cursor-pointer">
                <svg class="w-3.5 h-3.5 text-[#697386]" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15"/></svg>
                <span>Añadir</span>
              </button>
              <button
                type="button"
                class="h-7 px-2.5 rounded-md border border-[#E3E8EE] bg-white text-xs font-medium text-[#2D3748] hover:bg-[#F8FAFC] transition-colors shadow-2xs inline-flex items-center gap-1 cursor-pointer">
                <svg class="w-3.5 h-3.5 text-[#697386]" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L6.832 19.82a4.5 4.5 0 01-1.897 1.13l-2.685.8.8-2.685a4.5 4.5 0 011.13-1.897L16.863 4.487zm0 0L19.5 7.125"/></svg>
                <span>Editar</span>
              </button>
            </div>
          </div>

          <!-- Barra de Filtros en Píldora Estilo Stripe (Captura 3) -->
          <div class="flex items-center gap-2 flex-wrap text-xs text-[#2D3748]">
            <button
              type="button"
              class="h-7 px-3 rounded-full border border-[#E3E8EE] bg-white hover:bg-[#F8FAFC] transition-colors inline-flex items-center gap-1.5 shadow-2xs cursor-pointer">
              <span class="text-[#697386]">Intervalo de fechas</span>
              <span class="font-medium text-[#635BFF]">Últimos 7 días</span>
              <svg class="w-3 h-3 text-[#697386]" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd"/></svg>
            </button>

            <button
              type="button"
              class="h-7 px-3 rounded-full border border-[#E3E8EE] bg-white hover:bg-[#F8FAFC] transition-colors inline-flex items-center gap-1.5 shadow-2xs cursor-pointer">
              <span class="font-medium text-[#1A2C44]">Diario</span>
              <svg class="w-3 h-3 text-[#697386]" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd"/></svg>
            </button>

            <button
              type="button"
              class="h-7 px-3 rounded-full border border-[#E3E8EE] bg-white hover:bg-[#F8FAFC] transition-colors inline-flex items-center gap-1.5 shadow-2xs cursor-pointer">
              <svg class="w-3.5 h-3.5 text-[#8792A2]" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/></svg>
              <span class="text-[#697386]">Compara</span>
              <span class="font-medium text-[#635BFF]">Período anterior</span>
              <svg class="w-3 h-3 text-[#697386]" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd"/></svg>
            </button>
          </div>
        </div>

        <!-- Tarjetas de Resumen en Cuadrícula Estilo Stripe (Captura 3) -->
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 pt-1">
          @for (step of steps(); track step.id) {
            @if (!step.writeOnly || session.canWrite()) {
              <a
                [routerLink]="step.route"
                class="bg-white border border-[#E3E8EE] rounded-xl p-5 hover:border-[#635BFF] hover:shadow-xs transition-all flex flex-col justify-between group shadow-2xs block cursor-pointer no-underline text-inherit">
                <div>
                  <!-- Encabezado de la Tarjeta con ⓘ y Micro-icono de cuadrícula -->
                  <div class="flex items-center justify-between">
                    <div class="flex items-center gap-1.5 min-w-0">
                      <h3 class="text-[14px] leading-[20px] font-semibold text-[#1A2C44] truncate group-hover:text-[#635BFF] transition-colors">
                        {{ step.title }}
                      </h3>
                      <span class="text-[#8792A2] group-hover:text-[#1A2C44] transition-colors cursor-help text-[13px]" title="{{ step.desc }}">&#9432;</span>
                    </div>
                    <svg class="w-3.5 h-3.5 text-[#8792A2] group-hover:text-[#635BFF] transition-colors" viewBox="0 0 20 20" fill="currentColor">
                      <path d="M5 3a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2V5a2 2 0 00-2-2H5zM5 11a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2v-2a2 2 0 00-2-2H5zM11 5a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V5zM11 13a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
                    </svg>
                  </div>

                  <!-- Métrica Principal de la Tarjeta (24px, #1A2C44) -->
                  <div class="mt-3">
                    <div class="text-[24px] leading-[32px] font-semibold text-[#1A2C44] tracking-tight font-mono">
                      @if (step.id === 1) {
                        {{ servicePointsCount() }} <span class="text-[13px] font-normal text-[#697386] font-sans">puntos activos</span>
                      } @else if (step.id === 2) {
                        <span [ngClass]="criticalRisksCount() > 0 ? 'text-[#DF1B41]' : 'text-[#1A2C44]'">
                          {{ criticalRisksCount() }}
                        </span>
                        <span class="text-[13px] font-normal text-[#697386] font-sans"> quiebres críticos</span>
                      } @else if (step.id === 3) {
                        {{ publishedRoutesCount() }} <span class="text-[13px] font-normal text-[#697386] font-sans">/ {{ routePlansCount() }} rutas</span>
                      } @else {
                        {{ warehousesCount() }} <span class="text-[13px] font-normal text-[#697386] font-sans">bodega base</span>
                      }
                    </div>

                    <!-- Subtexto comparativo exacto a período anterior de Stripe -->
                    <div class="text-[12px] leading-[18px] text-[#697386] mt-0.5">
                      0 en período anterior
                    </div>
                  </div>
                </div>

                <!-- Acción Inferior estilo Stripe Link -->
                <div class="mt-4 pt-3 border-t border-[#F1F3F5] flex items-center justify-between">
                  <span 
                    class="text-[11px] px-2 py-0.5 rounded font-medium border font-mono"
                    [ngClass]="step.badgeClass">
                    {{ step.badge }}
                  </span>

                  <span
                    class="text-[13px] font-medium text-[#635BFF] group-hover:underline inline-flex items-center gap-1 group-hover:translate-x-0.5 transition-transform">
                    <span>{{ step.actionText }}</span>
                    <span>&rarr;</span>
                  </span>
                </div>
              </a>
            } @else {
              <div class="bg-white border border-[#E3E8EE] rounded-xl p-5 flex flex-col justify-between group shadow-2xs opacity-85">
                <div>
                  <!-- Encabezado de la Tarjeta con ⓘ y Micro-icono de cuadrícula -->
                  <div class="flex items-center justify-between">
                    <div class="flex items-center gap-1.5 min-w-0">
                      <h3 class="text-[14px] leading-[20px] font-semibold text-[#1A2C44] truncate">
                        {{ step.title }}
                      </h3>
                      <span class="text-[#8792A2] cursor-help text-[13px]" title="{{ step.desc }}">&#9432;</span>
                    </div>
                    <svg class="w-3.5 h-3.5 text-[#8792A2]" viewBox="0 0 20 20" fill="currentColor">
                      <path d="M5 3a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2V5a2 2 0 00-2-2H5zM5 11a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2v-2a2 2 0 00-2-2H5zM11 5a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V5zM11 13a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
                    </svg>
                  </div>

                  <!-- Métrica Principal de la Tarjeta (24px, #1A2C44) -->
                  <div class="mt-3">
                    <div class="text-[24px] leading-[32px] font-semibold text-[#1A2C44] tracking-tight font-mono">
                      @if (step.id === 1) {
                        {{ servicePointsCount() }} <span class="text-[13px] font-normal text-[#697386] font-sans">puntos activos</span>
                      } @else if (step.id === 2) {
                        <span [ngClass]="criticalRisksCount() > 0 ? 'text-[#DF1B41]' : 'text-[#1A2C44]'">
                          {{ criticalRisksCount() }}
                        </span>
                        <span class="text-[13px] font-normal text-[#697386] font-sans"> quiebres críticos</span>
                      } @else if (step.id === 3) {
                        {{ publishedRoutesCount() }} <span class="text-[13px] font-normal text-[#697386] font-sans">/ {{ routePlansCount() }} rutas</span>
                      } @else {
                        {{ warehousesCount() }} <span class="text-[13px] font-normal text-[#697386] font-sans">bodega base</span>
                      }
                    </div>

                    <!-- Subtexto comparativo exacto a período anterior de Stripe -->
                    <div class="text-[12px] leading-[18px] text-[#697386] mt-0.5">
                      0 en período anterior
                    </div>
                  </div>
                </div>

                <!-- Acción Inferior estilo Stripe Link -->
                <div class="mt-4 pt-3 border-t border-[#F1F3F5] flex items-center justify-between">
                  <span 
                    class="text-[11px] px-2 py-0.5 rounded font-medium border font-mono"
                    [ngClass]="step.badgeClass">
                    {{ step.badge }}
                  </span>
                  <span class="text-[12px] text-[#8792A2] italic">Solo lectura</span>
                </div>
              </div>
            }
          }
        </div>
      </div>

      <!-- Sección Secundaria: Recomendaciones Operativas del Sistema -->
      <div class="pt-4 border-t border-[#F1F3F5]">
        <div class="bg-white border border-[#E3E8EE] rounded-xl p-5 shadow-2xs space-y-4">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <h3 class="text-[14px] leading-[20px] font-semibold text-[#1A2C44]">
                Recomendaciones Operativas del Sistema
              </h3>
              <span class="text-[11px] px-2 py-0.5 rounded-full font-medium bg-[#FFF4E5] text-[#B76E00] border border-[#FFE2B5] font-mono text-[10px]">
                Atención Prioritaria
              </span>
            </div>
            <span class="text-[11px] text-[#697386] font-mono">Motor Determinista Activo</span>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-3 gap-6 pt-1 text-xs">
            <a routerLink="/alertas" class="group block space-y-1.5 p-3.5 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE] hover:border-[#635BFF] hover:bg-[#F1F5F9] transition-all cursor-pointer no-underline text-inherit shadow-2xs">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-[#DF1B41]">Cobertura de Stock</span>
              <p class="text-[13px] leading-[18px] text-[#2D3748]">
                {{ criticalRisksCount() > 0 ? 'Detectado ' + criticalRisksCount() + ' quiebre de stock crítico inminente en centros de salud.' : 'Todos los puntos de salud operan en niveles normales de cobertura.' }}
              </p>
              <span class="inline-flex items-center gap-1 text-[13px] text-[#635BFF] group-hover:underline font-medium pt-1 group-hover:translate-x-0.5 transition-transform">
                <span>Monitorear cobertura de stock</span>
                <span>&rarr;</span>
              </span>
            </a>

            <a routerLink="/rutas" class="group block space-y-1.5 p-3.5 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE] hover:border-[#635BFF] hover:bg-[#F1F5F9] transition-all cursor-pointer no-underline text-inherit shadow-2xs">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-[#635BFF]">Flota y Despacho</span>
              <p class="text-[13px] leading-[18px] text-[#2D3748]">
                {{ publishedRoutesCount() }} rutas despachadas. Revisa el estado de entrega y custodia con firma digital.
              </p>
              <span class="inline-flex items-center gap-1 text-[13px] text-[#635BFF] group-hover:underline font-medium pt-1 group-hover:translate-x-0.5 transition-transform">
                <span>Inspeccionar rutas</span>
                <span>&rarr;</span>
              </span>
            </a>

            <a routerLink="/simulation" class="group block space-y-1.5 p-3.5 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE] hover:border-[#635BFF] hover:bg-[#F1F5F9] transition-all cursor-pointer no-underline text-inherit shadow-2xs">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-[#00875A]">Simulación y Proyección</span>
              <p class="text-[13px] leading-[18px] text-[#2D3748]">
                Prueba el modelo de optimización logística frente a baselines a 90 días.
              </p>
              <span class="inline-flex items-center gap-1 text-[13px] text-[#635BFF] group-hover:underline font-medium pt-1 group-hover:translate-x-0.5 transition-transform">
                <span>Abrir simulador 90d</span>
                <span>&rarr;</span>
              </span>
            </a>
          </div>
        </div>
      </div>
    </div>
  `
})
export class InicioPage implements OnInit {
  private readonly apiService = inject(ApiService);
  private readonly simulations = inject(SimulationsApiService);
  private readonly exceptions = inject(FieldExceptionsApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly session = inject(AuthSession);

  readonly loadingIndicators = signal(false);
  readonly indicatorError = signal('');
  readonly indicators = signal(operationIndicators(null, null, null));

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

  readonly isPlanificador = computed(() => {
    return !this.isAuditor() && !this.isGerencia();
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
      badgeClass: 'bg-[#F1F3F5] text-[#4F5B66] border-[#E3E8EE]',
      writeOnly: true
    },
    {
      id: 2,
      title: '2. Monitorear Cobertura de Stock',
      desc: 'Verificar qué hospitales tienen insumos con menos de 5 días de cobertura o quiebre de stock.',
      route: '/alertas',
      actionText: 'Ver Cobertura',
      badge: 'Riesgo y Demanda',
      badgeClass: 'bg-[#FFF4E5] text-[#B76E00] border-[#FFE2B5]',
      writeOnly: false
    },
    {
      id: 3,
      title: '3. Planificar y Publicar Rutas',
      desc: 'Generar la ruta de despacho óptima, validar ventanas de atención y despachar a conductores.',
      route: '/rutas',
      actionText: 'Ir a Rutas',
      badge: 'Ruteo y Despacho',
      badgeClass: 'bg-[#635BFF]/10 text-[#635BFF] border-[#635BFF]/30',
      writeOnly: false
    },
    {
      id: 4,
      title: '4. Bodegas & Rebalanceo de Stock',
      desc: 'Supervisar niveles de existencias centrales y sugerencias de rebalanceo entre almacenes.',
      route: '/bodegas',
      actionText: 'Ir a Bodegas',
      badge: 'Existencias',
      badgeClass: 'bg-[#E8F7F0] text-[#00875A] border-[#B3E6CD]',
      writeOnly: true
    }
  ]);

  ngOnInit(): void {
    this.refreshPulse();
    if (this.isGerencia()) this.refreshIndicators();
  }

  refreshIndicators(): void {
    if (!this.isGerencia() || this.loadingIndicators()) return;
    this.loadingIndicators.set(true);
    this.indicatorError.set('');
    this.indicators.set(operationIndicators(null, null, null));
    const unavailable = (source: string) => {
      this.indicatorError.update(message => `${message}${message ? ' ' : ''}No se pudo consultar ${source}.`);
      return of(null);
    };
    forkJoin({
      comparison: this.simulations.listComparisons().pipe(
        map(items => items.filter(item => item.status === 'Completed').sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))[0]),
        switchMap(item => item ? this.simulations.getComparison(item.comparisonId) : of(null)),
        catchError(() => unavailable('la simulación')),
      ),
      receipts: this.exceptions.listReceiptExceptions(undefined, 100).pipe(catchError(() => unavailable('las recepciones'))),
      plans: this.apiService.getRoutePlans(undefined, 'Published').pipe(
        map(plans => {
          if (this.apiService.loadErrors?.()['rutas']) throw new Error('No se pudo consultar las rutas.');
          return plans;
        }),
        catchError(() => unavailable('las rutas')),
      ),
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.loadingIndicators.set(false)),
    ).subscribe(({ comparison, receipts, plans }) => {
      this.indicators.set(operationIndicators(comparison, receipts, plans));
    });
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
              badgeClass: this.servicePointsCount() > 0 ? 'bg-[#E8F7F0] text-[#00875A] border-[#B3E6CD]' : 'bg-[#FFF4E5] text-[#B76E00] border-[#FFE2B5]'
            };
          }
          if (s.id === 2) {
            return {
              ...s,
              badge: this.criticalRisksCount() > 0 ? `${this.criticalRisksCount()} Críticos` : 'Sin Quiebres',
              badgeClass: this.criticalRisksCount() > 0 ? 'bg-[#FEECEC] text-[#DF1B41] border-[#F8B4C0]' : 'bg-[#E8F7F0] text-[#00875A] border-[#B3E6CD]'
            };
          }
          if (s.id === 3) {
            return {
              ...s,
              badge: `${this.routePlansCount()} Rutas`,
              badgeClass: this.routePlansCount() > 0 ? 'bg-[#635BFF]/10 text-[#635BFF] border-[#635BFF]/30' : 'bg-[#F1F3F5] text-[#4F5B66] border-[#E3E8EE]'
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
