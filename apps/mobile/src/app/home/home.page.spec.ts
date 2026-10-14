import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Router } from '@angular/router';
import { describe, expect, it, beforeEach } from 'vitest';
import { AlertController } from '@ionic/angular/standalone';
import { MobileAuthService } from '../auth';
import { FieldOutboxService } from '../field/outbox/field-outbox.service';
import { FIELD_API } from '../field/field-api.interface';
import { HomePage } from './home.page';

describe('HomePage logout y capturas pendientes (H2, FR-09)', () => {
  let authServiceMock: { logout: ReturnType<typeof vi.fn>; currentRole: ReturnType<typeof signal> };
  let outboxServiceMock: {
    pendingCount: ReturnType<typeof signal<number>>;
    outboxItems: ReturnType<typeof signal<any[]>>;
    activeAssignment: ReturnType<typeof signal<any>>;
    isSimulatingOffline: ReturnType<typeof signal<boolean>>;
    isSyncing: ReturnType<typeof signal<boolean>>;
    activeDriverNotice: ReturnType<typeof signal<any>>;
    downloadRouteAssignment: ReturnType<typeof vi.fn>;
  };
  let alertControllerMock: { create: ReturnType<typeof vi.fn> };
  let alertElementMock: { present: ReturnType<typeof vi.fn>; onDidDismiss: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    authServiceMock = {
      logout: vi.fn(async () => undefined),
      currentRole: signal('Conductor')
    };

    outboxServiceMock = {
      pendingCount: signal(0),
      outboxItems: signal([]),
      activeAssignment: signal(null),
      isSimulatingOffline: signal(false),
      isSyncing: signal(false),
      activeDriverNotice: signal(null),
      downloadRouteAssignment: vi.fn(async () => ({} as any))
    };

    alertElementMock = {
      present: vi.fn(async () => undefined),
      onDidDismiss: vi.fn(async () => ({ role: 'confirm' }))
    };

    alertControllerMock = {
      create: vi.fn(async () => alertElementMock)
    };

    TestBed.configureTestingModule({
      imports: [HomePage],
      providers: [
        { provide: MobileAuthService, useValue: authServiceMock },
        { provide: FieldOutboxService, useValue: outboxServiceMock },
        { provide: AlertController, useValue: alertControllerMock },
        { provide: Router, useValue: { navigate: vi.fn() } },
        { provide: FIELD_API, useValue: {} }
      ]
    });
  });

  it('si pendingCount es 0, cierra sesión directamente sin mostrar alerta', async () => {
    outboxServiceMock.pendingCount.set(0);
    const fixture = TestBed.createComponent(HomePage);
    const component = fixture.componentInstance;

    await component.logout();

    expect(alertControllerMock.create).not.toHaveBeenCalled();
    expect(authServiceMock.logout).toHaveBeenCalled();
  });

  it('si pendingCount > 0 y confirma, muestra alerta y cierra sesión', async () => {
    outboxServiceMock.pendingCount.set(2);
    alertElementMock.onDidDismiss.mockResolvedValueOnce({ role: 'confirm' });

    const fixture = TestBed.createComponent(HomePage);
    const component = fixture.componentInstance;

    await component.logout();

    expect(alertControllerMock.create).toHaveBeenCalledWith({
      header: 'Capturas sin sincronizar',
      message: 'Tienes 2 capturas pendientes. Se guardarán en este teléfono para tu cuenta y se enviarán cuando vuelvas a entrar.',
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        { text: 'Cerrar sesión', role: 'confirm' }
      ]
    });
    expect(alertElementMock.present).toHaveBeenCalled();
    expect(authServiceMock.logout).toHaveBeenCalled();
  });

  it('si pendingCount es 1, muestra alerta con texto singular y si cancela no cierra sesión', async () => {
    outboxServiceMock.pendingCount.set(1);
    alertElementMock.onDidDismiss.mockResolvedValueOnce({ role: 'cancel' });

    const fixture = TestBed.createComponent(HomePage);
    const component = fixture.componentInstance;

    await component.logout();

    expect(alertControllerMock.create).toHaveBeenCalledWith(
      expect.objectContaining({
        header: 'Capturas sin sincronizar',
        message: 'Tienes 1 captura pendiente. Se guardarán en este teléfono para tu cuenta y se enviarán cuando vuelvas a entrar.'
      })
    );
    expect(authServiceMock.logout).not.toHaveBeenCalled();
  });
});
