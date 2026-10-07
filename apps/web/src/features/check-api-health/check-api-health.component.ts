import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '@shared/api/api.service';
import { PingResponseDto } from '@istpetdev/shared-core';

@Component({
  selector: 'feature-check-api-health',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="bg-white rounded-xl shadow-sm border border-slate-200 p-6">
      <div class="flex items-center justify-between mb-4">
        <div>
          <h3 class="text-lg font-semibold text-slate-900">Estado de la API .NET</h3>
          <p class="text-sm text-slate-500">Conexión con el backend en http://localhost:5000</p>
        </div>
        <button
          (click)="checkNow()"
          [disabled]="loading()"
          class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium text-sm rounded-lg transition-colors disabled:opacity-50">
          {{ loading() ? 'Comprobando...' : 'Comprobar ahora' }}
        </button>
      </div>

      <div class="flex items-center gap-3 p-4 rounded-lg" [ngClass]="{
        'bg-green-50 text-green-700 border border-green-200': status() === 'online',
        'bg-red-50 text-red-700 border border-red-200': status() === 'offline',
        'bg-amber-50 text-amber-700 border border-amber-200': status() === 'checking'
      }">
        <span class="h-3 w-3 rounded-full" [ngClass]="{
          'bg-green-500 animate-pulse': status() === 'online',
          'bg-red-500': status() === 'offline',
          'bg-amber-500 animate-pulse': status() === 'checking'
        }"></span>
        <span class="font-medium text-sm capitalize">
          {{ status() === 'online' ? 'API Online y Respondiendo' : status() === 'offline' ? 'API Offline (No conectada)' : 'Verificando servicio...' }}
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
    </div>
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
