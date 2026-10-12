import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '@shared/api';
import { ButtonComponent, BadgeComponent, CardComponent } from '@shared/ui';
import { PingResponseDto } from '@istpetdev/shared-core';

@Component({
  selector: 'feature-check-api-health',
  standalone: true,
  imports: [CommonModule, ButtonComponent, BadgeComponent, CardComponent],
  template: `
    <ui-card [padding]="'md'">
      <div class="flex items-center justify-between mb-4">
        <div>
          <h3 class="text-lg font-semibold text-slate-900">Estado de la API .NET</h3>
          <p class="text-sm text-slate-500">Conexión con el backend en http://localhost:5000</p>
        </div>
        <ui-button
          variant="primary"
          size="sm"
          [loading]="loading()"
          (buttonClick)="checkNow()"
          ariaLabel="Comprobar conectividad con la API .NET">
          Comprobar ahora
        </ui-button>
      </div>

      <div class="flex items-center gap-3 p-3.5 rounded-lg border bg-slate-50 border-slate-200">
        <ui-badge
          [variant]="status() === 'online' ? 'success' : status() === 'offline' ? 'critical' : 'warning'"
          size="md"
          [withDot]="true"
          [pulseDot]="status() === 'online' || status() === 'checking'"
          [label]="status() === 'online' ? 'API Online y Respondiendo' : status() === 'offline' ? 'API Offline (No conectada)' : 'Verificando servicio...'">
        </ui-badge>
        <span class="text-xs text-slate-500 font-mono">
          Endpoint: /health/live
        </span>
      </div>

      @if (pingData(); as data) {
        <div class="mt-4 grid grid-cols-2 gap-4 text-xs bg-slate-50 p-4 rounded-lg border border-slate-200">
          <div>
            <span class="text-slate-500">Servicio:</span>
            <span class="font-semibold text-slate-800 ml-1">{{ data.service }}</span>
          </div>
          <div>
            <span class="text-slate-500">Versión:</span>
            <span class="font-semibold text-slate-800 ml-1">v{{ data.version }}</span>
          </div>
          <div>
            <span class="text-slate-500">Entorno:</span>
            <span class="font-semibold text-slate-800 ml-1">{{ data.environment }}</span>
          </div>
          <div>
            <span class="text-slate-500">Hora Servidor:</span>
            <span class="font-semibold text-slate-800 ml-1">{{ data.timestamp | date:'mediumTime' }}</span>
          </div>
        </div>
      }
    </ui-card>
  `
})
export class CheckApiHealthComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly status = this.api.apiLiveStatus;
  readonly pingData = this.api.lastPing;
  readonly loading = signal(false);

  ngOnInit() {
    this.checkNow();
  }

  checkNow() {
    this.loading.set(true);
    this.api.getPing().subscribe({
      next: () => this.loading.set(false),
      error: () => this.loading.set(false)
    });
  }
}
