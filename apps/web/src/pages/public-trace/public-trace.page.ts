import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
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
              <span class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded text-[11px] font-mono font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200/60">
                <svg lucideShieldCheck [size]="12" class="stroke-[2.2]"></svg>
                TRAZABILIDAD PÚBLICA SEGURA (QR)
              </span>
              <span class="text-xs text-slate-400 font-mono">/</span>
              <span class="text-xs font-mono text-slate-500">SPEC 004 & ADR-10</span>
            </div>
            <h1 class="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">
              Consulta Pública de Custodia de Unidades (HU)
            </h1>
            <p class="text-sm text-slate-500 mt-1 max-w-2xl leading-relaxed">
              Verificación de origen, sellado y recepción de insumos hospitalarios. El acceso seguro utiliza tokens efímeros transmitidos fuera de la URL del servidor en la cabecera X-Trace-Token.
            </p>
          </div>

          <div class="shrink-0 bg-slate-50 p-3 rounded-xl border border-slate-200/80 text-right">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">Protocolo de Confianza</span>
            <span class="font-mono text-xs font-bold text-slate-800">Token Hash SHA-256 + X-Trace-Token</span>
          </div>
        </div>
      </div>

      <!-- Formulario de Consulta -->
      <ui-card [padding]="'md'">
        <div class="space-y-4">
          <form (submit)="submitSearch()" class="flex flex-col sm:flex-row sm:items-end gap-3">
            <div class="flex-1">
              <ui-input
                label="Código o ID de Unidad de Manipulación (HU)"
                type="text"
                placeholder="Ej. CAJA-001 o UUID"
                [(ngModel)]="unitId"
                name="unitId"
                hint="Código legible impreso en la etiqueta física o transmitido por el lector QR."
                [disabled]="loading()">
              </ui-input>
            </div>

            <div class="flex-1">
              <ui-input
                label="Token Secreto de Traza (de la URL QR #t=...)"
                type="text"
                placeholder="Token de traza público"
                [(ngModel)]="traceToken"
                name="traceToken"
                hint="Transmitido en el fragmento #t=... del código QR y enviado en la cabecera X-Trace-Token."
                [disabled]="loading()">
              </ui-input>
            </div>

            <div class="sm:mb-4">
              <ui-button
                type="submit"
                variant="primary"
                size="md"
                [disabled]="loading() || !unitId.trim() || !traceToken.trim()"
                [loading]="loading()"
                ariaLabel="Consultar trazabilidad de la unidad">
                <svg lucidePackageSearch [size]="14" class="mr-1.5 text-white stroke-[2.2]"></svg>
                <span>Verificar</span>
              </ui-button>
            </div>
          </form>
        </div>
      </ui-card>

      <!-- Mensaje de Error / Token Inválido o 404 -->
      @if (errorMessage()) {
        <ui-alert [variant]="errorCode() === 429 ? 'warning' : 'error'" [title]="errorTitle()">
          {{ errorMessage() }}
        </ui-alert>
      }

      <!-- Detalle de Trazabilidad Verificada -->
      @if (trace(); as data) {
        <ui-card [padding]="'none'">
          <!-- Header del Certificado -->
          <div class="p-5 sm:p-6 border-b border-slate-200 bg-slate-50/50 flex flex-wrap items-center justify-between gap-4">
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <span class="text-xs uppercase font-bold text-primary-700 tracking-wider">
                  Verificación de Custodia Oficial
                </span>
                <span class="text-xs text-slate-400">•</span>
                <span class="text-xs font-mono text-slate-500">Trazabilidad Hospitalaria Segura</span>
              </div>
              <h2 class="text-2xl font-black text-slate-900 font-mono tracking-tight">{{ data.unitId }}</h2>
            </div>

            <div class="flex items-center gap-2.5">
              <ui-badge variant="success" size="md" [withDot]="true" [label]="data.status"></ui-badge>
            </div>
          </div>

          <!-- Metadatos de la Unidad y Estado -->
          <div class="p-5 sm:p-6 border-b border-slate-100 grid grid-cols-1 sm:grid-cols-2 gap-4 bg-white text-xs">
            <div class="p-3 bg-slate-50 rounded-xl border border-slate-100">
              <span class="text-slate-400 uppercase font-semibold block text-[10px]">Identificador Verificado</span>
              <span class="font-bold text-sm text-slate-900 block font-mono">{{ data.unitId }}</span>
              <span class="text-[10px] text-slate-500 block mt-0.5">Etiqueta QR Verificada</span>
            </div>

            <div class="p-3 bg-slate-50 rounded-xl border border-slate-100">
              <span class="text-slate-400 uppercase font-semibold block text-[10px]">Estado de Custodia</span>
              <span class="font-mono text-xs font-bold text-slate-800">{{ data.status }}</span>
              <span class="text-[10px] text-emerald-600 font-semibold block mt-0.5">Token de Traza Activo</span>
            </div>
          </div>

          <!-- Historial de Eventos de Custodia (Timeline) -->
          <div class="p-5 sm:p-6 bg-white space-y-4">
            <div>
              <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500 mb-3">
                Eventos Registrados de Custodia (Servidor)
              </h3>

              @if (data.events && data.events.length > 0) {
                <div class="space-y-2">
                  @for (ev of data.events; track ev.timestamp) {
                    <div class="p-3 bg-slate-50/70 border border-slate-200/80 rounded-xl flex items-center justify-between text-xs">
                      <div class="flex items-center gap-2">
                        <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
                        <span class="font-bold text-slate-800">{{ ev.stage }}</span>
                      </div>
                      <span class="font-mono text-slate-500 text-[11px]">{{ ev.timestamp | date:'short' }}</span>
                    </div>
                  }
                </div>
              } @else {
                <p class="text-xs text-slate-400 italic">No hay eventos de custodia registrados para esta unidad.</p>
              }
            </div>

            <!-- Insumos en la Unidad -->
            @if (data.productUnits && data.productUnits.length > 0) {
              <div class="pt-2">
                <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500 mb-2">
                  Insumos Verificados en el Bulto ({{ data.productUnits.length }} item(s))
                </h3>

                <div class="divide-y divide-slate-100 border border-slate-200 rounded-xl overflow-hidden text-xs">
                  @for (prod of data.productUnits; track prod.sku) {
                    <div class="p-3 bg-slate-50/50 flex items-center justify-between">
                      <span class="font-bold text-slate-800">{{ prod.sku }}</span>
                      <span class="text-slate-500 font-mono">{{ prod.unit }}</span>
                    </div>
                  }
                </div>
              </div>
            }
          </div>

          <!-- Footer del Certificado -->
          <div class="p-4 sm:p-5 bg-slate-50 border-t border-slate-200 flex flex-wrap items-center justify-between gap-3 text-xs text-slate-500">
            <span class="flex items-center gap-1.5">
              <svg lucideCheckCircle2 [size]="14" class="text-emerald-600 stroke-[2.2]"></svg>
              <span>Privacidad protegida: Sin datos de pacientes ni recetas médicas (ADR-10 / HIPAA).</span>
            </span>
            <span class="font-mono text-[11px] text-slate-400">
              X-Trace-Token Validado
            </span>
          </div>
        </ui-card>
      }
    </div>
  `
})
export class PublicTracePage implements OnInit {
  private readonly apiService = inject(ApiService);
  private readonly route = inject(ActivatedRoute);

  unitId = '';
  traceToken = '';

  readonly trace = signal<PublicTraceResponseDto | null>(null);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly errorTitle = signal<string>('Unidad no encontrada');
  readonly errorCode = signal<number>(404);

  ngOnInit(): void {
    // 1. Leer parámetros ruta (/public-trace/:unitId) o query (?u=...)
    const routeUnit = this.route.snapshot.paramMap.get('unitId');
    const queryUnit = this.route.snapshot.queryParamMap.get('u');
    if (routeUnit || queryUnit) {
      this.unitId = routeUnit || queryUnit || '';
    }

    // 2. Extraer fragmento hash (#t=...) de forma segura en cliente (S-07, FR-14)
    if (typeof window !== 'undefined' && window.location.hash) {
      const hash = window.location.hash;
      const match = hash.match(/[#&]t=([^&]+)/);
      if (match && match[1]) {
        this.traceToken = decodeURIComponent(match[1]);

        // Retirar inmediatamente el token de la barra de direcciones del navegador
        try {
          const cleanUrl = window.location.pathname + window.location.search;
          window.history.replaceState(null, '', cleanUrl);
        } catch {
          // Ignorar fallo de history API
        }
      }
    }

    // Si viene la unidad y el token en el enlace QR, consultar automáticamente
    if (this.unitId.trim() && this.traceToken.trim()) {
      this.searchTrace(this.unitId.trim(), this.traceToken.trim());
    }
  }

  submitSearch(): void {
    if (!this.unitId.trim() || !this.traceToken.trim()) return;
    this.searchTrace(this.unitId.trim(), this.traceToken.trim());
  }

  searchTrace(unitId: string, token: string): void {
    if (!unitId || !token) {
      this.errorTitle.set('Token requerido');
      this.errorMessage.set('La consulta pública exige el token secreto de traza (X-Trace-Token).');
      this.errorCode.set(404);
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);
    this.trace.set(null);

    this.apiService.getPublicTrace(unitId, token).subscribe({
      next: (res) => {
        this.trace.set(res);
        this.loading.set(false);
      },
      error: (err: any) => {
        this.loading.set(false);
        const status = err?.status || 404;
        this.errorCode.set(status);

        if (status === 429) {
          this.errorTitle.set('Límite de Consultas Excedido');
          this.errorMessage.set(
            'Se ha superado el número de consultas públicas permitidas para este token. Por favor espere unos segundos e intente de nuevo.'
          );
        } else {
          this.errorTitle.set('Unidad no encontrada');
          this.errorMessage.set(
            'El token de traza no es válido, ha sido revocado, ha vencido o la unidad no existe en el sistema.'
          );
        }
      }
    });
  }
}
