import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '@shared/api';
import { PublicTraceResponseDto } from '@istpetdev/shared-core';
import { CardComponent, InputComponent, ButtonComponent, BadgeComponent, AlertComponent } from '@shared/ui';

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
    AlertComponent
  ],
  template: `
    <div class="space-y-6 max-w-3xl mx-auto">
      <div>
        <div class="flex items-center gap-2 mb-1">
          <ui-badge variant="transit" size="sm" label="Cadena de Custodia"></ui-badge>
          <span class="text-xs text-slate-500 font-mono">Reto 1: Expo Clean Ecuador 2026</span>
        </div>
        <h1 class="text-2xl font-bold text-slate-900 tracking-tight">Consulta de Trazabilidad Pública</h1>
        <p class="text-sm text-slate-500 mt-1">Verificación de cadena de custodia mediante identificador de unidad logística o QR</p>
      </div>

      <!-- Formulario de Consulta con Componentes del Design System -->
      <ui-card [padding]="'md'">
        <form (submit)="searchTrace()" class="flex flex-col sm:flex-row sm:items-end gap-3">
          <div class="flex-1">
            <ui-input
              label="Código de Unidad Logística (Handling Unit)"
              type="text"
              placeholder="Ej. HU-EC-2026-001"
              [(ngModel)]="unitId"
              name="unitId"
              hint="Ingrese el identificador impreso en el rotulado físico o código de despacho."
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
              <span>Consultar Unidad</span>
            </ui-button>
          </div>
        </form>
      </ui-card>

      <!-- Alerta Estandarizada cuando la unidad no existe -->
      @if (notFound()) {
        <ui-alert variant="error" title="Unidad logística no encontrada">
          El identificador consultado no figura en los registros de despacho de la plataforma. Verifique el código o escanee el código QR físico.
        </ui-alert>
      }

      <!-- Detalle de Trazabilidad Verificada con Tarjeta y Badges Estandarizados -->
      @if (trace(); as data) {
        <ui-card [padding]="'lg'">
          <div class="flex items-center justify-between border-b border-slate-100 pb-4 mb-4">
            <div>
              <span class="text-xs uppercase font-bold text-primary-700 tracking-wider">Unidad Logística Verificada</span>
              <h2 class="text-xl font-bold text-slate-900 font-mono mt-0.5">{{ data.unitId }}</h2>
            </div>
            <ui-badge variant="success" size="md" [withDot]="true" [label]="data.status"></ui-badge>
          </div>

          <p class="text-sm text-slate-600 mb-6 leading-relaxed">{{ data.summary }}</p>

          <div>
            <h3 class="text-xs uppercase font-bold tracking-wider text-slate-500 mb-4">
              Eventos Registrados en Custodia
            </h3>
            <div class="space-y-4">
              @for (event of data.events; track event.timestamp) {
                <div class="flex items-start gap-4">
                  <div class="h-2.5 w-2.5 rounded-full bg-primary-600 mt-1.5 shrink-0 ring-4 ring-primary-100"></div>
                  <div class="flex-1">
                    <p class="text-sm font-semibold text-slate-800">{{ event.stage }}</p>
                    <p class="text-xs text-slate-500 mt-0.5">
                      {{ event.location }} • <span class="font-mono">{{ event.timestamp | date:'medium' }}</span>
                    </p>
                  </div>
                </div>
              }
            </div>
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
