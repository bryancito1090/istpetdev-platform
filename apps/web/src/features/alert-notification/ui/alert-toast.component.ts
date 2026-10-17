import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AlertNotificationService, AlertToast } from '../services/alert-notification.service';

@Component({
  selector: 'ui-alert-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (notificationService.toasts().length > 0) {
      <div 
        class="fixed bottom-6 right-6 z-[9999] flex flex-col gap-2.5 max-w-sm sm:max-w-md w-full pointer-events-none"
        role="region"
        aria-label="Notificaciones de alertas automáticas">
        
        @for (toast of notificationService.toasts(); track toast.id) {
          <div 
            class="pointer-events-auto bg-white border border-[#E3E8EE] rounded-lg shadow-lg p-4 flex flex-col gap-2.5 transition-all transform animate-in slide-in-from-bottom-3 duration-200"
            [ngClass]="toast.level === 'Critico' ? 'border-l-4 border-l-rose-500' : 'border-l-4 border-l-amber-500'"
            role="alert"
            aria-live="assertive">
            
            <!-- Encabezado con Nivel e Icono de Cierre -->
            <div class="flex items-center justify-between gap-2">
              <div class="flex items-center gap-2">
                <span 
                  class="text-[10px] font-semibold uppercase tracking-wider px-2 py-0.5 rounded border"
                  [ngClass]="toast.level === 'Critico'
                    ? 'bg-rose-50 text-rose-700 border-rose-200' 
                    : 'bg-amber-50 text-amber-700 border-amber-200'">
                  {{ toast.level === 'Critico' ? 'Quiebre Crítico' : 'Alerta Preventiva' }}
                </span>
                <span class="text-[11px] text-[#8792A2]">Alerta automática</span>
              </div>

              <button 
                type="button"
                (click)="notificationService.dismissToast(toast.id)"
                class="w-6 h-6 rounded flex items-center justify-center text-[#8792A2] hover:text-[#1A2C44] hover:bg-[#F8FAFC] transition-colors cursor-pointer"
                title="Cerrar notificación">
                <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <!-- Cuerpo del Mensaje -->
            <div class="space-y-1">
              <p class="text-[13px] font-semibold text-[#1A2C44]">
                {{ toast.servicePointCode }} &mdash; <span class="font-mono text-[12px] font-normal text-[#30313D]">{{ toast.sku }}</span>
              </p>
              <div class="text-[12px] text-[#4F5B66] flex flex-wrap gap-x-3 gap-y-0.5">
                <span>
                  Cobertura: 
                  <strong [ngClass]="toast.coverageDays != null && toast.coverageDays <= toast.leadTimeDays ? 'text-rose-600' : 'text-[#1A2C44]'">
                    {{ toast.coverageDays != null ? (toast.coverageDays.toFixed(1) + ' días') : 'Sin datos' }}
                  </strong>
                </span>
                <span>(Lead Time: {{ toast.leadTimeDays }}d)</span>
                @if (toast.suggestedQuantity > 0) {
                  <span class="font-medium text-[#635BFF]">+{{ toast.suggestedQuantity }} u requeridas</span>
                }
              </div>
            </div>

            <!-- Acción de Navegación Rápida -->
            <div class="flex items-center justify-between pt-1 border-t border-[#F1F3F5]">
              <span class="text-[10px] text-[#8792A2]">
                {{ toast.timestamp | date: 'HH:mm:ss' }}
              </span>
              <button
                type="button"
                (click)="notificationService.goToAlerts(toast)"
                class="inline-flex items-center gap-1 text-[12px] font-medium text-[#635BFF] hover:text-[#5248E0] hover:underline cursor-pointer">
                <span>Ver expediente de auditoría</span>
                <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M9 5l7 7-7 7" />
                </svg>
              </button>
            </div>
          </div>
        }
      </div>
    }
  `
})
export class AlertToastComponent {
  readonly notificationService = inject(AlertNotificationService);
}
