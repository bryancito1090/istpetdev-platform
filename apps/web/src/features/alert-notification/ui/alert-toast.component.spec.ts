import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { AlertNotificationService, AlertToast } from '../services/alert-notification.service';
import { AlertToastComponent } from './alert-toast.component';

describe('AlertToastComponent', () => {
  let toastsSignal: ReturnType<typeof signal<AlertToast[]>>;
  let notificationServiceMock: {
    toasts: ReturnType<typeof signal<AlertToast[]>>;
    dismissToast: ReturnType<typeof vi.fn>;
    goToAlerts: ReturnType<typeof vi.fn>;
  };

  const sampleToast: AlertToast = {
    id: 't-123',
    level: 'Critico',
    servicePointCode: 'HOS-01',
    sku: 'MED-01',
    coverageDays: 1.2,
    leadTimeDays: 2.0,
    suggestedQuantity: 80,
    timestamp: new Date()
  };

  beforeEach(() => {
    toastsSignal = signal<AlertToast[]>([]);
    notificationServiceMock = {
      toasts: toastsSignal,
      dismissToast: vi.fn(),
      goToAlerts: vi.fn()
    };

    TestBed.configureTestingModule({
      imports: [AlertToastComponent],
      providers: [
        { provide: AlertNotificationService, useValue: notificationServiceMock }
      ]
    });
  });

  it('no renderiza nada cuando la lista de toasts está vacía', () => {
    const fixture = TestBed.createComponent(AlertToastComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
  });

  it('renderiza la tarjeta con formato Arquetipo B y datos cuando hay una alerta activa', () => {
    toastsSignal.set([sampleToast]);
    const fixture = TestBed.createComponent(AlertToastComponent);
    fixture.detectChanges();

    const alertEl = fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement;
    expect(alertEl).not.toBeNull();
    expect(alertEl.textContent).toContain('HOS-01');
    expect(alertEl.textContent).toContain('MED-01');
    expect(alertEl.textContent).toContain('Quiebre Crítico');
    expect(alertEl.textContent).toContain('+80 u requeridas');
  });

  it('invoca dismissToast al pulsar el botón de cerrar', () => {
    toastsSignal.set([sampleToast]);
    const fixture = TestBed.createComponent(AlertToastComponent);
    fixture.detectChanges();

    const closeBtn = fixture.nativeElement.querySelector('button[title="Cerrar notificación"]') as HTMLButtonElement;
    closeBtn.click();

    expect(notificationServiceMock.dismissToast).toHaveBeenCalledWith('t-123');
  });

  it('invoca goToAlerts al hacer clic en ver expediente', () => {
    toastsSignal.set([sampleToast]);
    const fixture = TestBed.createComponent(AlertToastComponent);
    fixture.detectChanges();

    const viewBtn = fixture.nativeElement.querySelector('button:has(svg)') as HTMLButtonElement;
    const actionBtns = fixture.nativeElement.querySelectorAll('button');
    const verExpedienteBtn = Array.from(actionBtns).find((b: any) => b.textContent.includes('Ver expediente de auditoría')) as HTMLButtonElement;
    expect(verExpedienteBtn).not.toBeNull();
    verExpedienteBtn.click();

    expect(notificationServiceMock.goToAlerts).toHaveBeenCalledWith(sampleToast);
  });
});
