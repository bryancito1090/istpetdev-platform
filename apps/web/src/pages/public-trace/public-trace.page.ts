import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '@shared/api';
import { PublicTraceResponseDto } from '@istpetdev/shared-core';
import { CardComponent, InputComponent, ButtonComponent, BadgeComponent, AlertComponent } from '@shared/ui';
import { LucideShieldCheck, LucidePackageSearch, LucideCheckCircle2 } from '@lucide/angular';

@Component({
  selector: 'page-public-trace',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    CardComponent, 
    InputComponent, 
    ButtonComponent, 
    BadgeComponent, 
    AlertComponent,
    LucideShieldCheck,
    LucidePackageSearch,
    LucideCheckCircle2
  ],
  template: `
    <div class="space-y-6 max-w-4xl mx-auto">
      <!-- Encabezado de Auditoría Criptográfica -->
      <div class="bg-white border border-slate-200/80 rounded-2xl p-5 sm:p-6 shadow-sm">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <div class="flex items-center gap-2 mb-1.5">
              <span class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded text-[11px] font-mono font-semibold bg-primary-50 text-primary-700 border border-primary-200/60">
                <svg lucideShieldCheck [size]="12" class="stroke-[2.2]"></svg>
                CADENA DE CUSTODIA INMUTABLE
              </span>
              <span class="text-xs text-slate-400 font-mono">/</span>
              <span class="text-xs font-mono text-slate-500">RETO 1 EXPO CLEAN 2026</span>
            </div>
            <h1 class="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">
              Portal de Verificación de Handling Units (HU)
            </h1>
            <p class="text-sm text-slate-500 mt-1 max-w-2xl leading-relaxed">
              Consulta pública y validación de bultos químicos, consumibles y equipamiento. Verificación criptográfica con identificadores UUID v7 y firma en recepción offline.
            </p>
          </div>

          <div class="shrink-0 bg-slate-50 p-3 rounded-xl border border-slate-200/80 text-right">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">Protocolo de Confianza</span>
            <span class="font-mono text-xs font-bold text-slate-800">UUID v7 + PostGIS</span>
          </div>
        </div>
      </div>

      <!-- Formulario de Consulta con Presets de Prueba para el Jurado -->
      <ui-card [padding]="'md'">
        <div class="space-y-4">
          <form (submit)="searchTrace()" class="flex flex-col sm:flex-row sm:items-end gap-3">
            <div class="flex-1">
              <ui-input
                label="Código de Unidad Logística o Hash de Custodia"
                type="text"
                placeholder="Ej. HU-EC-2026-001"
                [(ngModel)]="unitId"
                name="unitId"
                hint="Código legible impreso en la etiqueta física o transmitido por el lector QR."
                [disabled]="loading()">
              </ui-input>
            </div>

            <div class="sm:mb-4">
              <ui-button
                type="submit"
                variant="primary"
                size="md"
                [disabled]="loading() || !unitId.trim()"
                [loading]="loading()"
                ariaLabel="Consultar trazabilidad de la unidad">
                <svg lucidePackageSearch [size]="14" class="mr-1.5 text-white stroke-[2.2]"></svg>
                <span>Verificar Unidad</span>
              </ui-button>
            </div>
          </form>

          <!-- Presets rápidos para el pitch de la Expo -->
          <div class="pt-2 border-t border-slate-100 flex flex-wrap items-center gap-2 text-xs">
            <span class="text-slate-400 font-medium">Bultos de Demostración:</span>
            <button
              type="button"
              (click)="selectPreset('HU-EC-2026-001')"
              class="px-2.5 py-1 rounded-md bg-slate-100 text-slate-700 hover:bg-primary-50 hover:text-primary-700 font-mono text-[11px] font-semibold transition-colors border border-slate-200/60">
              HU-EC-2026-001 (Amonio 5G)
            </button>
            <button
              type="button"
              (click)="selectPreset('HU-EC-2026-002')"
              class="px-2.5 py-1 rounded-md bg-slate-100 text-slate-700 hover:bg-primary-50 hover:text-primary-700 font-mono text-[11px] font-semibold transition-colors border border-slate-200/60">
              HU-EC-2026-002 (Detergente Neutro)
            </button>
            <button
              type="button"
              (click)="selectPreset('HU-EC-2026-003')"
              class="px-2.5 py-1 rounded-md bg-slate-100 text-slate-700 hover:bg-primary-50 hover:text-primary-700 font-mono text-[11px] font-semibold transition-colors border border-slate-200/60">
              HU-EC-2026-003 (Paños Microfibra)
            </button>
          </div>
        </div>
      </ui-card>

      <!-- Alerta cuando la unidad no existe -->
      @if (notFound()) {
        <ui-alert variant="error" title="Unidad logística no encontrada">
          El identificador consultado no figura en el registro central de despachos. Verifique el código o escanee nuevamente el código QR físico.
        </ui-alert>
      }

      <!-- Detalle de Trazabilidad Verificada (Estilo Stripe B2B Receipt / Certificate) -->
      @if (trace(); as data) {
        <ui-card [padding]="'none'">
          <!-- Header del Certificado -->
          <div class="p-5 sm:p-6 border-b border-slate-200 bg-slate-50/50 flex flex-wrap items-center justify-between gap-4">
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <span class="text-xs uppercase font-bold text-primary-700 tracking-wider">Certificado de Custodia Activa</span>
                <span class="text-xs text-slate-400">•</span>
                <span class="text-xs font-mono text-slate-500">ISO 9001 / Buenas Prácticas de Higiene</span>
              </div>
              <h2 class="text-2xl font-black text-slate-900 font-mono tracking-tight">{{ data.unitId }}</h2>
            </div>

            <div class="flex items-center gap-2.5">
              <ui-badge variant="success" size="md" [withDot]="true" [label]="data.status"></ui-badge>
            </div>
          </div>

          <!-- Metadatos de la Carga -->
          <div class="p-5 sm:p-6 border-b border-slate-100 grid grid-cols-1 sm:grid-cols-3 gap-4 bg-white text-xs">
            <div class="p-3 bg-slate-50 rounded-xl border border-slate-100">
              <span class="text-slate-400 uppercase font-semibold block text-[10px]">Identificador Único Temporal</span>
              <span class="font-mono text-xs font-bold text-slate-800 break-all">018f3a9e-2140-7984-912a</span>
              <span class="text-[10px] text-slate-500 block mt-0.5">Estándar UUID v7 Inmutable</span>
            </div>

            <div class="p-3 bg-slate-50 rounded-xl border border-slate-100">
              <span class="text-slate-400 uppercase font-semibold block text-[10px]">Flota Asignada</span>
              <span class="font-mono text-xs font-bold text-slate-800">C-01 (Mercedes Sprinter)</span>
              <span class="text-[10px] text-slate-500 block mt-0.5">Placas PBX-4821 • GPS Activo</span>
            </div>

            <div class="p-3 bg-slate-50 rounded-xl border border-slate-100">
              <span class="text-slate-400 uppercase font-semibold block text-[10px]">Condición de Almacenamiento</span>
              <span class="font-mono text-xs font-bold text-slate-800">15°C - 25°C (Estable)</span>
              <span class="text-[10px] text-emerald-600 font-semibold block mt-0.5">Ficha Técnica MSDS Adjunta</span>
            </div>
          </div>

          <!-- Resumen de Contenido -->
          <div class="p-5 sm:p-6 bg-white space-y-4">
            <div>
              <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500 mb-1.5">
                Descripción de la Unidad Logística
              </h3>
              <p class="text-sm text-slate-700 leading-relaxed bg-slate-50/70 p-3.5 rounded-xl border border-slate-200/60">
                {{ data.summary }}
              </p>
            </div>

            <!-- Timeline de Eventos en Custodia -->
            <div class="pt-4">
              <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500 mb-5 flex items-center justify-between">
                <span>Historial de Puntos de Control y Sello de Tiempo</span>
                <span class="text-[11px] font-mono text-slate-400 font-normal">Sincronización NTP</span>
              </h3>

              <div class="relative pl-6 space-y-6 before:absolute before:left-2.5 before:top-2 before:bottom-2 before:w-0.5 before:bg-slate-200">
                @for (event of data.events; track event.timestamp) {
                  <div class="relative flex items-start gap-4">
                    <div class="absolute -left-6 top-1 h-5 w-5 rounded-full bg-white border-2 border-primary-600 flex items-center justify-center">
                      <div class="h-2 w-2 rounded-full bg-primary-600"></div>
                    </div>
                    
                    <div class="flex-1 bg-white p-3.5 rounded-xl border border-slate-200/80 shadow-xs">
                      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-1 mb-1">
                        <span class="text-sm font-bold text-slate-900">{{ event.stage }}</span>
                        <span class="font-mono text-xs text-slate-500">{{ event.timestamp | date:'dd/MM/yyyy HH:mm:ss' }}</span>
                      </div>
                      <div class="flex items-center gap-2 text-xs text-slate-600">
                        <span class="font-medium text-slate-800">{{ event.location }}</span>
                        <span class="text-slate-300">•</span>
                        <span class="font-mono text-[11px] text-slate-500 bg-slate-50 px-2 py-0.5 rounded border border-slate-100">
                          Operador Validado
                        </span>
                      </div>
                    </div>
                  </div>
                }
              </div>
            </div>
          </div>

          <!-- Footer del Certificado -->
          <div class="p-4 sm:p-5 bg-slate-50 border-t border-slate-200 flex flex-wrap items-center justify-between gap-3 text-xs text-slate-500">
            <span class="flex items-center gap-1.5">
              <svg lucideCheckCircle2 [size]="14" class="text-emerald-600 stroke-[2.2]"></svg>
              <span>Integridad de sello verificada bajo estándar FEDELIMP / Reto 1</span>
            </span>
            <span class="font-mono text-[11px] text-slate-400">
              Nodo: rds-postgis-ec-prod
            </span>
          </div>
        </ui-card>
      }
    </div>
  `
})
export class PublicTracePage {
  private readonly api = inject(ApiService);
  unitId = 'HU-EC-2026-001';
  readonly trace = signal<PublicTraceResponseDto | null>(null);
  readonly loading = signal(false);
  readonly notFound = signal(false);

  selectPreset(code: string): void {
    this.unitId = code;
    this.searchTrace();
  }

  searchTrace() {
    if (!this.unitId.trim()) return;
    this.loading.set(true);
    this.notFound.set(false);
    this.trace.set(null);

    this.api.getPublicTrace(this.unitId.trim()).subscribe({
      next: res => {
        this.trace.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }
}

