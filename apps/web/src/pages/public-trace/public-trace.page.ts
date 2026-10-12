import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '@shared/api';
import { PublicTraceResponseDto } from '@istpetdev/shared-core';

@Component({
  selector: 'page-public-trace',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-6 max-w-3xl mx-auto">
      <div>
        <h1 class="text-2xl font-bold text-slate-900">Consulta de Trazabilidad Pública</h1>
        <p class="text-sm text-slate-500 mt-1">Verificación de cadena de custodia mediante identificador de unidad logística o QR</p>
      </div>

      <div class="bg-white p-6 rounded-xl border border-slate-200 shadow-sm">
        <form (submit)="searchTrace()" class="flex gap-3">
          <input
            type="text"
            [(ngModel)]="unitId"
            name="unitId"
            placeholder="Ingrese código de unidad (ej. HU-EC-001)"
            class="flex-1 px-4 py-2 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 text-sm">
          <button
            type="submit"
            [disabled]="loading() || !unitId.trim()"
            class="px-6 py-2 bg-primary-600 hover:bg-primary-700 text-white font-medium text-sm rounded-lg transition-colors disabled:opacity-50">
            {{ loading() ? 'Buscando...' : 'Consultar' }}
          </button>
        </form>
      </div>

      @if (notFound()) {
        <div class="bg-red-50 border border-red-200 text-red-800 p-4 rounded-xl text-sm flex items-start gap-3">
          <svg class="w-5 h-5 text-red-600 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
          <div>
            <span class="font-semibold block">Unidad logística no encontrada</span>
            El identificador consultado no figura en los registros de despacho de la plataforma. Verifique el código o escanee el código QR físico.
          </div>
        </div>
      }

      @if (trace(); as data) {
        <div class="bg-white p-6 rounded-xl border border-slate-200 shadow-sm space-y-6">
          <div class="flex items-center justify-between border-b border-slate-100 pb-4">
            <div>
              <span class="text-xs uppercase font-bold text-primary-700 tracking-wider">Unidad Logística</span>
              <h2 class="text-xl font-bold text-slate-900">{{ data.unitId }}</h2>
            </div>
            <span class="bg-secondary-100 text-secondary-900 border border-secondary-300 text-xs font-semibold px-3 py-1 rounded-full">
              {{ data.status }}
            </span>
          </div>

          <p class="text-sm text-slate-600">{{ data.summary }}</p>

          <div>
            <h3 class="text-sm font-semibold text-slate-900 mb-4">Eventos Registrados</h3>
            <div class="space-y-4">
              @for (event of data.events; track event.timestamp) {
                <div class="flex items-start gap-4">
                  <div class="h-2 w-2 rounded-full bg-primary-600 mt-2"></div>
                  <div class="flex-1">
                    <p class="text-sm font-semibold text-slate-800">{{ event.stage }}</p>
                    <p class="text-xs text-slate-500">{{ event.location }} • {{ event.timestamp | date:'medium' }}</p>
                  </div>
                </div>
              }
            </div>
          </div>
        </div>
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
