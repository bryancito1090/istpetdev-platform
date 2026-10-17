import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '@shared/api';
import { PublicTraceResponseDto } from '@istpetdev/shared-core';
import { LucideShieldCheck, LucidePackageSearch, LucideCheckCircle2 } from '@lucide/angular';

@Component({
  selector: 'page-public-trace',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideShieldCheck,
    LucidePackageSearch,
    LucideCheckCircle2
  ],
  template: `
    <div class="max-w-4xl mx-auto px-4 py-8 space-y-6 font-sans text-[#30313D]">
      <!-- Encabezado de Auditoría Criptográfica -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[#E3E8EE] pb-5">
        <div>
          <div class="flex items-center gap-2 mb-1">
            <span class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-md text-[11px] font-mono font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
              <svg lucideShieldCheck [size]="12" class="stroke-[2.2]"></svg>
              TRAZABILIDAD PÚBLICA SEGURA (QR)
            </span>
            <span class="text-xs text-[#8792A2] font-mono">/</span>
            <span class="text-xs font-mono text-[#697386]">SPEC 004 &amp; ADR-10</span>
          </div>
          <h1 class="text-[22px] font-semibold text-[#1A2C44] tracking-tight">
            Consulta Pública de Custodia de Unidades (HU)
          </h1>
          <p class="text-[13px] text-[#3C4F69] mt-0.5 max-w-2xl leading-relaxed">
            Verificación de origen, sellado y recepción de insumos hospitalarios. El acceso seguro utiliza tokens efímeros transmitidos fuera de la URL del servidor en la cabecera X-Trace-Token.
          </p>
        </div>

        <div class="shrink-0 bg-[#F8FAFC] p-3 rounded-lg border border-[#E3E8EE] text-right shadow-2xs">
          <span class="text-[10px] text-[#8792A2] uppercase font-semibold block">Protocolo de Confianza</span>
          <span class="font-mono text-[11px] font-bold text-[#1A2C44]">Token Hash SHA-256 + X-Trace-Token</span>
        </div>
      </div>

      <!-- Formulario de Consulta -->
      <div class="bg-white p-5 rounded-lg border border-[#E3E8EE] shadow-xs space-y-4">
        <form (submit)="submitSearch()" class="space-y-4">
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-[12px] font-medium text-[#4F5B66] mb-1">
                Código o ID de Unidad de Manipulación (HU)
              </label>
              <input
                type="text"
                placeholder="Ej. CAJA-001 o UUID"
                [(ngModel)]="unitId"
                name="unitId"
                [disabled]="loading()"
                class="w-full h-9 px-3 rounded-md border border-[#E3E8EE] text-[13px] text-[#1A2C44] placeholder-[#8792A2] focus:outline-none focus:border-[#635BFF] transition-all disabled:opacity-50" />
              <p class="text-[11px] text-[#8792A2] mt-1">
                Código legible impreso en la etiqueta física o transmitido por el lector QR.
              </p>
            </div>

            <div>
              <label class="block text-[12px] font-medium text-[#4F5B66] mb-1">
                Token Secreto de Traza (de la URL QR #t=...)
              </label>
              <input
                type="text"
                placeholder="Token de traza público"
                [(ngModel)]="traceToken"
                name="traceToken"
                [disabled]="loading()"
                class="w-full h-9 px-3 rounded-md border border-[#E3E8EE] text-[13px] text-[#1A2C44] placeholder-[#8792A2] focus:outline-none focus:border-[#635BFF] transition-all disabled:opacity-50" />
              <p class="text-[11px] text-[#8792A2] mt-1">
                Transmitido en el fragmento #t=... del código QR y enviado en la cabecera X-Trace-Token.
              </p>
            </div>
          </div>

          <div class="flex justify-end pt-1">
            <button
              type="submit"
              [disabled]="loading() || !unitId.trim() || !traceToken.trim()"
              class="inline-flex items-center gap-1.5 h-9 px-4 text-[13px] font-medium rounded-md bg-[#635BFF] hover:bg-[#5248E0] text-white transition-colors shadow-xs disabled:opacity-50 cursor-pointer">
              @if (loading()) {
                <span class="inline-block animate-spin h-3.5 w-3.5 border-2 border-white border-t-transparent rounded-full"></span>
                <span>Verificando...</span>
              } @else {
                <svg lucidePackageSearch [size]="14" class="text-white stroke-[2.2]"></svg>
                <span>Verificar Custodia</span>
              }
            </button>
          </div>
        </form>
      </div>

      <!-- Mensaje de Error / Token Inválido o 404 -->
      @if (errorMessage()) {
        <div
          class="p-4 rounded-lg text-[13px] border shadow-2xs space-y-1"
          [ngClass]="errorCode() === 429 ? 'bg-amber-50 border-amber-200 text-amber-800' : 'bg-rose-50 border-rose-200 text-rose-800'">
          <div class="font-semibold">{{ errorTitle() }}</div>
          <p class="text-[12px]">{{ errorMessage() }}</p>
        </div>
      }

      <!-- Detalle de Trazabilidad Verificada (Certificado de Custodia) -->
      @if (trace(); as data) {
        <div class="bg-white rounded-lg border border-[#E3E8EE] shadow-xs overflow-hidden">
          <!-- Header del Certificado -->
          <div class="p-5 border-b border-[#E3E8EE] bg-[#F8FAFC] flex flex-wrap items-center justify-between gap-4">
            <div class="space-y-0.5">
              <div class="flex items-center gap-2">
                <span class="text-[11px] uppercase font-semibold text-[#635BFF] tracking-wider">
                  Verificación de Custodia Oficial
                </span>
                <span class="text-[11px] text-[#8792A2]">•</span>
                <span class="text-[11px] font-mono text-[#697386]">Trazabilidad Hospitalaria Segura</span>
              </div>
              <h2 class="text-[22px] font-bold text-[#1A2C44] font-mono tracking-tight">{{ data.unitId }}</h2>
            </div>

            <div class="flex items-center gap-2">
              <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-[11px] font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
                <span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                <span>{{ data.status }}</span>
              </span>
            </div>
          </div>

          <!-- Metadatos de la Unidad y Estado -->
          <div class="p-5 border-b border-[#E3E8EE] grid grid-cols-1 sm:grid-cols-2 gap-3.5 bg-white text-[12px]">
            <div class="p-3.5 bg-[#F8FAFC] rounded-lg border border-[#E3E8EE]">
              <span class="text-[#8792A2] uppercase font-semibold block text-[10px]">Identificador Verificado</span>
              <span class="font-bold text-[14px] text-[#1A2C44] block font-mono mt-0.5">{{ data.unitId }}</span>
              <span class="text-[11px] text-[#697386] block mt-0.5">Etiqueta QR Verificada</span>
            </div>

            <div class="p-3.5 bg-[#F8FAFC] rounded-lg border border-[#E3E8EE]">
              <span class="text-[#8792A2] uppercase font-semibold block text-[10px]">Estado de Custodia</span>
              <span class="font-mono text-[14px] font-bold text-[#1A2C44] mt-0.5 block">{{ data.status }}</span>
              <span class="text-[11px] text-emerald-600 font-medium block mt-0.5">Token de Traza Activo</span>
            </div>
          </div>

          <!-- Historial de Eventos de Custodia (Timeline) -->
          <div class="p-5 bg-white space-y-4">
            <div>
              <h3 class="text-[11px] font-semibold uppercase tracking-wider text-[#8792A2] mb-3">
                Eventos Registrados de Custodia (Servidor)
              </h3>

              @if (data.events && data.events.length > 0) {
                <div class="space-y-2">
                  @for (ev of data.events; track ev.timestamp) {
                    <div class="p-3 bg-[#F8FAFC] border border-[#E3E8EE] rounded-lg flex items-center justify-between text-[12px]">
                      <div class="flex items-center gap-2.5">
                        <span class="w-2 h-2 rounded-full bg-emerald-500 shrink-0"></span>
                        <span class="font-medium text-[#1A2C44]">{{ ev.stage }}</span>
                      </div>
                      <span class="font-mono text-[#8792A2] text-[11px]">{{ ev.timestamp | date:'short' }}</span>
                    </div>
                  }
                </div>
              } @else {
                <p class="text-[12px] text-[#8792A2] italic">No hay eventos de custodia registrados para esta unidad.</p>
              }
            </div>

            <!-- Insumos en la Unidad -->
            @if (data.productUnits && data.productUnits.length > 0) {
              <div class="pt-2">
                <h3 class="text-[11px] font-semibold uppercase tracking-wider text-[#8792A2] mb-2">
                  Insumos Verificados en el Bulto ({{ data.productUnits.length }} item(s))
                </h3>

                <div class="divide-y divide-[#F1F3F5] border border-[#E3E8EE] rounded-lg overflow-hidden text-[12px]">
                  @for (prod of data.productUnits; track prod.sku) {
                    <div class="p-3 bg-white hover:bg-[#F8FAFC] flex items-center justify-between transition-colors">
                      <span class="font-mono font-medium text-[#1A2C44]">{{ prod.sku }}</span>
                      <span class="text-[#697386] font-mono">{{ prod.unit }}</span>
                    </div>
                  }
                </div>
              </div>
            }
          </div>

          <!-- Footer del Certificado -->
          <div class="p-4 bg-[#F8FAFC] border-t border-[#E3E8EE] flex flex-wrap items-center justify-between gap-3 text-[12px] text-[#697386]">
            <span class="flex items-center gap-1.5">
              <svg lucideCheckCircle2 [size]="14" class="text-emerald-600 stroke-[2.2]"></svg>
              <span>Privacidad protegida: Sin datos de pacientes ni recetas médicas (ADR-10 / HIPAA).</span>
            </span>
            <span class="font-mono text-[11px] text-[#8792A2]">
              X-Trace-Token Validado
            </span>
          </div>
        </div>
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
