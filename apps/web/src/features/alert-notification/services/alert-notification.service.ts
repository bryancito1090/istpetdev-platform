import { Injectable, inject, signal, computed, effect, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService, RiskItemDto } from '@shared/api';
import { AuthSession } from '@entities/session';

export interface AlertToast {
  id: string;
  level: 'Critico' | 'Alto' | 'Vigilar';
  servicePointCode: string;
  sku: string;
  coverageDays: number | null;
  leadTimeDays: number;
  suggestedQuantity: number;
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class AlertNotificationService implements OnDestroy {
  private readonly api = inject(ApiService);
  private readonly session = inject(AuthSession);
  private readonly router = inject(Router);

  /** Todas las alertas de stock activas detectadas en el último sondeo */
  readonly activeRisks = signal<RiskItemDto[]>([]);

  /** Lista de notificaciones flotantes activas en pantalla */
  readonly toasts = signal<AlertToast[]>([]);

  /** Ítems críticos o con riesgo alto */
  readonly criticalRisks = computed(() => {
    return this.activeRisks().filter(r => r.level === 'Critico' || r.level === 'Critical');
  });

  readonly warningRisks = computed(() => {
    return this.activeRisks().filter(r => r.level === 'Alto' || r.level === 'High' || r.level === 'Vigilar' || r.level === 'Medium');
  });

  /** Conteo total de alertas para badges y campana */
  readonly alertCount = computed(() => {
    const crit = this.criticalRisks().length;
    return crit > 0 ? crit : this.activeRisks().length;
  });

  readonly hasAlerts = computed(() => this.alertCount() > 0);

  /** Indica si se detectó una alerta nueva en el ciclo más reciente (dispara animación en Navbar) */
  readonly hasNewAlert = signal<boolean>(false);

  private pollTimer: ReturnType<typeof setInterval> | null = null;
  private readonly notifiedRiskKeys = new Set<string>();
  private isFirstCheck = true;

  constructor() {
    // Si la sesión cambia a autenticada, iniciar monitoreo automático
    effect(() => {
      const hasStatusFn = typeof this.session?.status === 'function';
      const status = hasStatusFn ? this.session.status() : 'authenticated';
      if (status === 'authenticated') {
        this.startPolling();
      } else {
        this.stopPolling();
      }
    });
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  /**
   * Inicia el sondeo periódico reactivo (Smart Polling).
   * Intervalo por defecto: 30 segundos.
   */
  startPolling(intervalMs = 30000): void {
    if (this.pollTimer) return;

    // Ejecución inicial inmediata
    this.checkRisks();

    this.pollTimer = setInterval(() => {
      // Pausar si la pestaña está oculta para ahorrar recursos del cliente
      if (typeof document !== 'undefined' && document.visibilityState === 'hidden') {
        return;
      }
      this.checkRisks();
    }, intervalMs);
  }

  stopPolling(): void {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }

  /** Fuerza una verificación manual de riesgos bajo demanda */
  checkRisks(): void {
    const hasStatusFn = typeof this.session?.status === 'function';
    if (hasStatusFn && this.session.status() !== 'authenticated') return;

    this.api.getRisks(undefined, true).subscribe({
      next: res => {
        if (!res || !res.items) return;

        const currentItems = res.items;
        this.activeRisks.set(currentItems);

        // Detectar nuevos quiebres de stock crítico
        const criticalItems = currentItems.filter(i => i.level === 'Critico' || i.level === 'Critical' || i.level === 'Alto' || i.level === 'High');

        if (this.isFirstCheck) {
          // En la primera carga, registrar las existentes sin spamear toasts
          for (const item of criticalItems) {
            this.notifiedRiskKeys.add(this.itemKey(item));
          }
          this.isFirstCheck = false;
          return;
        }

        let newAlertFound = false;
        for (const item of criticalItems) {
          const key = this.itemKey(item);
          if (!this.notifiedRiskKeys.has(key)) {
            this.notifiedRiskKeys.add(key);
            this.enqueueToast(item);
            newAlertFound = true;
          }
        }

        if (newAlertFound) {
          this.hasNewAlert.set(true);
          // Restablecer el flag de pulso tras 4 segundos
          setTimeout(() => this.hasNewAlert.set(false), 4000);
        }
      },
      error: () => {
        // En caso de fallo transitorio de red, no interrumpir la experiencia de usuario
      }
    });
  }

  dismissToast(id: string): void {
    this.toasts.update(list => list.filter(t => t.id !== id));
  }

  goToAlerts(toast?: AlertToast): void {
    if (toast) {
      this.dismissToast(toast.id);
    }
    this.router.navigate(['/alertas']);
  }

  private itemKey(item: RiskItemDto): string {
    return `${item.servicePointCode}:${item.sku}:${item.level}`;
  }

  private enqueueToast(item: RiskItemDto): void {
    const toast: AlertToast = {
      id: crypto.randomUUID ? crypto.randomUUID() : `toast-${Date.now()}-${Math.random()}`,
      level: (item.level === 'Critico' || item.level === 'Critical') ? 'Critico' : 'Alto',
      servicePointCode: item.servicePointCode,
      sku: item.sku,
      coverageDays: item.coverageDays,
      leadTimeDays: item.leadTimeDays,
      suggestedQuantity: item.suggestedQuantity,
      timestamp: new Date()
    };

    this.toasts.update(list => [toast, ...list.slice(0, 4)]); // Máximo 5 toasts en pantalla

    // Auto-cierre tras 8 segundos
    setTimeout(() => {
      this.dismissToast(toast.id);
    }, 8000);
  }
}
