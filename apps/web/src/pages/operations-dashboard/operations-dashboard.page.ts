import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CheckApiHealthComponent } from '@features/check-api-health/check-api-health.component';

@Component({
  selector: 'page-operations-dashboard',
  standalone: true,
  imports: [CommonModule, CheckApiHealthComponent],
  template: `
    <div class="space-y-6">
      <div>
        <h1 class="text-2xl font-bold text-slate-900">Panel de Control Operativo</h1>
        <p class="text-sm text-slate-500 mt-1">Plataforma Logística Hospitalaria y Manejo de Insumos - IstpetDev</p>
      </div>

      <feature-check-api-health></feature-check-api-health>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div class="bg-white p-6 rounded-xl border border-slate-200 shadow-sm">
          <div class="flex items-center justify-between mb-2">
            <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">Puntos de Servicio</span>
            <span class="text-blue-600 bg-blue-50 px-2 py-0.5 rounded text-xs font-bold">Activos</span>
          </div>
          <div class="text-3xl font-extrabold text-slate-900">5</div>
          <p class="text-xs text-slate-500 mt-2">Quito, Guayaquil, Cuenca, Ambato, Santo Domingo</p>
        </div>

        <div class="bg-white p-6 rounded-xl border border-slate-200 shadow-sm">
          <div class="flex items-center justify-between mb-2">
            <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">Unidades en Tránsito</span>
            <span class="text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded text-xs font-bold">Logística</span>
          </div>
          <div class="text-3xl font-extrabold text-slate-900">12</div>
          <p class="text-xs text-slate-500 mt-2">Custodia digital verificada con QR</p>
        </div>

        <div class="bg-white p-6 rounded-xl border border-slate-200 shadow-sm">
          <div class="flex items-center justify-between mb-2">
            <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">Nivel de Riesgo Global</span>
            <span class="text-amber-600 bg-amber-50 px-2 py-0.5 rounded text-xs font-bold">Supervisión</span>
          </div>
          <div class="text-3xl font-extrabold text-amber-600">Bajo</div>
          <p class="text-xs text-slate-500 mt-2">Sin desabastecimiento crítico detectado</p>
        </div>
      </div>
    </div>
  `
})
export class OperationsDashboardPage {}
