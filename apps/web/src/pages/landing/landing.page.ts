import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SessionStore } from '@entities/session';
import { CheckApiHealthComponent } from '@features/check-api-health';
import { StatCardComponent, BadgeComponent, ButtonComponent } from '@shared/ui';

@Component({
  selector: 'page-landing',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    CheckApiHealthComponent,
    StatCardComponent,
    BadgeComponent
  ],
  template: `
    <div class="space-y-12 pb-8">
      <!-- 1. Hero Section: Base de Diseño y CTA Principal -->
      <section class="relative overflow-hidden bg-gradient-to-b from-white to-slate-50 border border-slate-200 rounded-2xl p-6 sm:p-12 shadow-xs">
        <div class="max-w-3xl space-y-6">
          <div class="flex flex-wrap items-center gap-2">
            <ui-badge variant="transit" size="md" [withDot]="true" [pulseDot]="true" label="Expo Clean Ecuador 2026"></ui-badge>
            <span class="text-xs text-slate-500 font-mono">Reto 1: Abastecimiento Hospitalario Nacional</span>
          </div>

          <h1 class="text-3xl sm:text-5xl font-black text-slate-900 tracking-tight leading-tight">
            Optimización y trazabilidad logística para insumos críticos.
          </h1>

          <p class="text-base sm:text-lg text-slate-600 leading-relaxed font-normal">
            Plataforma integral de abastecimiento hospitalario de alta precisión. Orquestación geoespacial con
            <span class="font-semibold text-slate-900">PostGIS</span>, custodia digital inmutable con
            <span class="font-semibold text-slate-900">UUID v7</span> y cálculo de cobertura en tiempo real bajo Clean Architecture.
          </p>

          <!-- Acciones Principales del Hero: Botón "Ingresar al sistema" (Energy Orange oficial del flyer) -->
          <div class="flex flex-wrap items-center gap-4 pt-2">
            <a
              routerLink="/login"
              class="inline-flex items-center justify-center font-semibold rounded-xl text-base px-6 py-3.5 bg-accent-500 hover:bg-accent-600 text-white shadow-md hover:shadow-lg focus:outline-none focus:ring-2 focus:ring-accent-500 focus:ring-offset-2 transition-all active:scale-[0.98]">
              <span>Ingresar al sistema</span>
              <svg class="w-5 h-5 ml-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M14 5l7 7m0 0l-7 7m7-7H3" />
              </svg>
            </a>

            <a
              routerLink="/dashboard"
              class="inline-flex items-center justify-center font-medium rounded-xl text-sm px-5 py-3.5 bg-white hover:bg-slate-50 text-slate-700 border border-slate-300 shadow-xs transition-colors">
              <svg class="w-4 h-4 mr-2 text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
              </svg>
              <span>Ver Panel de Control Operativo</span>
            </a>

            <a
              routerLink="/public-trace"
              class="inline-flex items-center justify-center font-medium rounded-xl text-sm px-5 py-3.5 text-slate-600 hover:text-primary-600 hover:bg-primary-50/50 transition-colors">
              <svg class="w-4 h-4 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              <span>Consulta de Trazabilidad Pública</span>
            </a>
          </div>
        </div>
      </section>

      <!-- 2. KPIs de Alto Impacto en Vivo (Preservando métricas actuales) -->
      <section class="space-y-3">
        <div class="flex items-center justify-between">
          <div>
            <h2 class="text-sm font-bold uppercase tracking-wider text-slate-500">Métricas Operativas en Tiempo Real</h2>
            <p class="text-xs text-slate-400">Datos sincronizados con base PostgreSQL 16 y NetTopologySuite</p>
          </div>
          <span class="text-xs font-mono text-slate-500 bg-slate-100 px-2.5 py-1 rounded">
            Actualización Continua
          </span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <ui-stat-card
            title="Red Hospitalaria"
            value="5"
            unit="Centros"
            badgeText="Quito, GYE, CUE, AMB, SD"
            badgeVariant="transit"
            description="1 Hub Central + 4 Hospitales Zonales">
          </ui-stat-card>

          <ui-stat-card
            title="Ahorro en Kilómetros"
            value="24.8"
            unit="%"
            trend="up"
            trendValue="+24.8%"
            trendLabel="vs ruta tradicional"
            badgeText="Eficiencia"
            badgeVariant="success"
            description="Optimización algorítmica de ruteo">
          </ui-stat-card>

          <ui-stat-card
            title="Custodia Activa"
            value="12"
            unit="HUs"
            badgeText="Ledger OK"
            badgeVariant="success"
            description="Trazabilidad cronológica con UUID v7">
          </ui-stat-card>

          <ui-stat-card
            title="Riesgo de Quiebre"
            value="0"
            unit="Críticos"
            valueColor="success"
            badgeText="Monitoreado"
            badgeVariant="neutral"
            description="Inventario dentro de umbrales seguros">
          </ui-stat-card>
        </div>
      </section>

      <!-- 3. Estado de Infraestructura y Servicios (Componente actual conservado) -->
      <section class="space-y-3">
        <div class="flex items-center justify-between">
          <h2 class="text-sm font-bold uppercase tracking-wider text-slate-500">Conectividad e Integración API</h2>
          <span class="text-xs text-primary-600 font-medium">Health Checks RFC 7807</span>
        </div>
        
        <feature-check-api-health></feature-check-api-health>
      </section>

      <!-- 4. Pilares de Arquitectura de la Solución (ADR-04, ADR-05, ADR-10) -->
      <section class="space-y-4">
        <div>
          <h2 class="text-lg font-bold text-slate-900">Capacidades del Sistema IstpetDev</h2>
          <p class="text-xs text-slate-500">Construido para soportar la operación logística más exigente del país</p>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div class="bg-white p-6 rounded-xl border border-slate-200 shadow-xs space-y-3">
            <div class="h-10 w-10 rounded-lg bg-primary-100 text-primary-800 flex items-center justify-center font-bold">
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
              </svg>
            </div>
            <h3 class="text-base font-bold text-slate-900">Ruteo Dinámico & PostGIS</h3>
            <p class="text-xs text-slate-600 leading-relaxed">
              Cálculo de trayectorias óptimas con Leaflet y soporte espacial en base de datos.
              Capacidad de replanificación inmediata ante incidentes viales preservando paradas previas.
            </p>
            <div class="pt-2">
              <span class="text-[11px] font-mono text-primary-700 bg-primary-50 px-2 py-1 rounded">ADR-05 Leaflet OSM</span>
            </div>
          </div>

          <div class="bg-white p-6 rounded-xl border border-slate-200 shadow-xs space-y-3">
            <div class="h-10 w-10 rounded-lg bg-secondary-100 text-secondary-900 border border-secondary-300 flex items-center justify-center font-bold">
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
              </svg>
            </div>
            <h3 class="text-base font-bold text-slate-900">Custodia Digital Inmutable</h3>
            <p class="text-xs text-slate-600 leading-relaxed">
              Transacciones atómicas con libro mayor (ledger) y control de concurrencia optimista.
              Identificadores universales cronológicos UUID v7 y Outbox pattern para auditoría integral.
            </p>
            <div class="pt-2">
              <span class="text-[11px] font-mono text-secondary-800 bg-secondary-50 px-2 py-1 rounded">Clean Architecture .NET 8</span>
            </div>
          </div>

          <div class="bg-white p-6 rounded-xl border border-slate-200 shadow-xs space-y-3">
            <div class="h-10 w-10 rounded-lg bg-accent-100 text-accent-800 flex items-center justify-center font-bold">
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 18h.01M8 21h8a2 2 0 002-2V5a2 2 0 00-2-2H8a2 2 0 00-2 2v14a2 2 0 002 2z" />
              </svg>
            </div>
            <h3 class="text-base font-bold text-slate-900">Operación Offline-First PWA</h3>
            <p class="text-xs text-slate-600 leading-relaxed">
              Entregas y recepciones hospitalarias capaces de operar sin conectividad a internet en carretera o sótanos,
              encolando sincronizaciones idempotentes hacia el servidor central.
            </p>
            <div class="pt-2">
              <span class="text-[11px] font-mono text-accent-800 bg-accent-50 px-2 py-1 rounded">ADR-06 / ADR-17 PWA</span>
            </div>
          </div>
        </div>
      </section>

      <!-- 5. Banner de Llamado a la Acción Final -->
      <section class="bg-slate-900 text-white rounded-2xl p-8 sm:p-10 flex flex-col md:flex-row items-center justify-between gap-6 shadow-md">
        <div class="space-y-2 text-center md:text-left">
          <h3 class="text-2xl font-bold tracking-tight">
            Listo para supervisar la operación logística hospitalaria
          </h3>
          <p class="text-sm text-slate-300 max-w-xl">
            Acceda al panel interactivo con perfiles simulados de Operador de Despacho, Supervisor Médico o Auditor de Custodia.
          </p>
        </div>

        <div class="flex items-center gap-3 shrink-0">
          <a
            routerLink="/login"
            class="px-6 py-3.5 rounded-xl bg-accent-500 hover:bg-accent-600 text-white font-semibold text-sm shadow transition-all active:scale-[0.98] inline-flex items-center gap-2">
            <span>Ingresar al sistema</span>
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3" />
            </svg>
          </a>
        </div>
      </section>
    </div>
  `
})
export class LandingPage {
  readonly sessionStore = inject(SessionStore);
}
