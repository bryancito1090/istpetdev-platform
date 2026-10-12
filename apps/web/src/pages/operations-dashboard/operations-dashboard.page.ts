import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SessionStore } from '@entities/session';
import { CheckApiHealthComponent } from '@features/check-api-health/check-api-health.component';
import { RouteMapWidget } from '@widgets/route-map';
import { StatCardComponent, BadgeComponent } from '@shared/ui';

@Component({
  selector: 'page-operations-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    RouterLink,
    CheckApiHealthComponent, 
    RouteMapWidget, 
    StatCardComponent,
    BadgeComponent
  ],
  template: `
    <div class="space-y-6">
      <!-- Encabezado con estado del operador -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div class="flex items-center gap-2 mb-1">
            <span class="text-xs font-mono text-primary-700 bg-primary-50 px-2 py-0.5 rounded font-semibold">
              RETO 1: EXPO CLEAN ECUADOR 2026
            </span>
            @if (sessionStore.isAuthenticated()) {
              <ui-badge 
                variant="success" 
                size="sm" 
                [withDot]="true" 
                [label]="'Sesión: ' + sessionStore.currentUser()?.roleLabel">
              </ui-badge>
            }
          </div>
          <h1 class="text-2xl font-bold text-slate-900 tracking-tight">Panel de Control Operativo</h1>
          <p class="text-sm text-slate-500">
            Monitoreo en tiempo real de insumos hospitalarios, ruteo PostGIS y trazabilidad con UUID v7
          </p>
        </div>

        <div class="flex items-center gap-2">
          <a
            routerLink="/public-trace"
            class="px-3 py-1.5 text-xs font-medium bg-white hover:bg-slate-50 text-slate-700 border border-slate-300 rounded-lg shadow-xs transition-colors flex items-center gap-1.5">
            <svg class="w-3.5 h-3.5 text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            <span>Ver Trazabilidad HU</span>
          </a>
        </div>
      </div>

      <!-- Estado de la API .NET (Existente) -->
      <feature-check-api-health></feature-check-api-health>

      <!-- Métricas y Tarjetas de Estado (Existente) -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
        <ui-stat-card
          title="Puntos de Servicio"
          value="5"
          unit="Puntos"
          badgeText="Activos"
          badgeVariant="transit"
          description="Quito, Guayaquil, Cuenca, Ambato, Santo Domingo">
        </ui-stat-card>

        <ui-stat-card
          title="Unidades en Tránsito"
          value="12"
          unit="HUs"
          badgeText="Custodia OK"
          badgeVariant="success"
          trend="up"
          trendValue="+24.8%"
          trendLabel="ahorro km"
          description="Custodia digital verificada con UUID v7">
        </ui-stat-card>

        <ui-stat-card
          title="Nivel de Riesgo Global"
          value="Bajo"
          valueColor="warning"
          badgeText="Supervisión"
          badgeVariant="warning"
          description="Sin desabastecimiento crítico inmediato detectado">
        </ui-stat-card>
      </div>

      <!-- Widget de Mapa y Optimización de Rutas (ADR-05) (Existente) -->
      <widget-route-map></widget-route-map>
    </div>
  `
})
export class OperationsDashboardPage {
  readonly sessionStore = inject(SessionStore);
}
