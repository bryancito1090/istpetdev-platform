import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Router } from '@angular/router';
import { describe, expect, it, beforeEach } from 'vitest';
import { AlertController, ToastController } from '@ionic/angular/standalone';
import { MobileAuthService } from '../auth';
import { FieldOutboxService } from '../field/outbox/field-outbox.service';
import { FIELD_API } from '../field/field-api.interface';
import { HomePage } from './home.page';

describe('HomePage logout y capturas pendientes (H2, FR-09)', () => {
  let authServiceMock: {
    logout: ReturnType<typeof vi.fn>;
    currentRole: ReturnType<typeof signal>;
    me: ReturnType<typeof signal>;
    offline: ReturnType<typeof signal<boolean>>;
  };
  let outboxServiceMock: {
    pendingCount: ReturnType<typeof signal<number>>;
    outboxItems: ReturnType<typeof signal<any[]>>;
    activeAssignment: ReturnType<typeof signal<any>>;
    isSimulatingOffline: ReturnType<typeof signal<boolean>>;
    isSyncing: ReturnType<typeof signal<boolean>>;
    activeDriverNotice: ReturnType<typeof signal<any>>;
    lastDownloadTimestamp: ReturnType<typeof signal<number>>;
    downloadRouteAssignment: ReturnType<typeof vi.fn>;
  };
  let alertControllerMock: { create: ReturnType<typeof vi.fn> };
  let alertElementMock: { present: ReturnType<typeof vi.fn>; onDidDismiss: ReturnType<typeof vi.fn> };
  let toastControllerMock: { create: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    authServiceMock = {
      logout: vi.fn(async () => undefined),
      currentRole: signal('Conductor'),
      me: signal({ accountId: 'c-1', displayName: 'Conductor', role: 'Conductor', platform: 'field' }),
      offline: signal(false)
    };

    outboxServiceMock = {
      pendingCount: signal(0),
      outboxItems: signal([]),
      activeAssignment: signal(null),
      isSimulatingOffline: signal(false),
      isSyncing: signal(false),
      activeDriverNotice: signal(null),
      lastDownloadTimestamp: signal(0),
      downloadRouteAssignment: vi.fn(async () => ({} as any))
    };

    alertElementMock = {
      present: vi.fn(async () => undefined),
      onDidDismiss: vi.fn(async () => ({ role: 'confirm' }))
    };

    alertControllerMock = {
      create: vi.fn(async () => alertElementMock)
    };

    toastControllerMock = {
      create: vi.fn(async () => ({ present: vi.fn(async () => undefined) }))
    };

    TestBed.configureTestingModule({
      imports: [HomePage],
      providers: [
        { provide: MobileAuthService, useValue: authServiceMock },
        { provide: FieldOutboxService, useValue: outboxServiceMock },
        { provide: AlertController, useValue: alertControllerMock },
        { provide: ToastController, useValue: toastControllerMock },
        { provide: Router, useValue: { navigate: vi.fn() } },
        { provide: FIELD_API, useValue: {} }
      ]
    });
  });

  it('015: en modo sin conexión avisa que las capturas se guardan en el teléfono', async () => {
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Sin conexión');

    authServiceMock.offline.set(true);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Sin conexión: tus capturas se guardan en el teléfono');
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


describe('Receptor: recepción parcial y evidencia #115', () => {
  let page: HomePage;
  let outbox: { saveReceiptAtomic: ReturnType<typeof vi.fn>; loadReceiverUnit: ReturnType<typeof vi.fn>; pendingCount: ReturnType<typeof signal<number>> };
  const unit = { handlingUnitId: 'hu', code: 'BULTO', status: 'Delivered', deliveryId: 'delivery', deliveryVersion: 3,
    destinationName: 'Hospital', boxes: [{ handlingUnitId: 'hu', code: 'BULTO', status: 'Delivered' }, { handlingUnitId: 'hu-2', code: 'BULTO-2', status: 'Delivered' }],
    lines: [{ deliveryLineId: 'line', lotId: 'lot', lotCode: 'L-01', sku: 'SKU', productName: 'Insumo', quantity: 10, unit: 'L' }] };
  beforeEach(async () => {
    outbox = { pendingCount: signal(0), saveReceiptAtomic: vi.fn(async () => ({})),
      loadReceiverUnit: vi.fn(async () => ({ unit, cached: true })) };
    TestBed.configureTestingModule({ imports: [HomePage], providers: [
      { provide: MobileAuthService, useValue: { currentRole: signal('Receptor'), me: signal({ displayName: 'Receptor' }) } },
      { provide: FieldOutboxService, useValue: outbox },
      { provide: FIELD_API, useValue: {} },
      { provide: Router, useValue: { navigate: vi.fn() } },
      { provide: AlertController, useValue: {} },
      { provide: ToastController, useValue: { create: vi.fn().mockResolvedValue({ present: vi.fn() }) } }
    ] });
    page = TestBed.createComponent(HomePage).componentInstance;
    await page.loadReceiverUnit('hu', 'qr');
  });

  it('valida motivo y foto antes de guardar 6 aceptadas y 4 rechazadas', async () => {
    // #162: el alcance es la entrega completa con todos sus bultos.
    expect(page.receiverActiveBox()?.boxes.length).toBe(2);
    page.receiverLines[0].quantityAccepted = 6;
    await page.confirmReceiverAction();
    expect(page.receiverError()).toContain('motivo');
    page.receiverLines[0].rejectionReason = 'Daño';
    await page.confirmReceiverAction();
    expect(page.receiverError()).toContain('foto');
    expect(outbox.saveReceiptAtomic).not.toHaveBeenCalled();
    page.receiverPhoto.set(new File(['image'], 'photo.jpg', { type: 'image/jpeg' }));
    page.receiverLines[0].remainingQuantity = 2;
    page.receiverLines[0].nextRequestedQuantity = 15;
    await page.confirmReceiverAction();
    expect(outbox.saveReceiptAtomic).toHaveBeenCalledWith(expect.objectContaining({
      expectedDeliveryVersion: 3, receiverContext: { unitId: 'hu', traceToken: 'qr' },
      lines: [expect.objectContaining({ quantityAccepted: 6, quantityRejected: 4,
        rejectionReason: 'Daño', remainingQuantity: 2, nextRequestedQuantity: 15 })]
    }));
    expect(page.receiverConfirmedMessage()).toContain('teléfono');
    await page.confirmReceiverAction();
    expect(outbox.saveReceiptAtomic).toHaveBeenCalledTimes(1);
  });

  it('conforme no exige foto y cantidades negativas, vacías o superiores se rechazan', async () => {
    for (const quantity of [-1, 11, NaN, null]) {
      page.receiverLines[0].quantityAccepted = quantity as number;
      await page.confirmReceiverAction();
      expect(page.receiverError()).toContain('cantidades');
    }
    expect(outbox.saveReceiptAtomic).not.toHaveBeenCalled();
    page.receiverLines[0].quantityAccepted = 10;
    await page.confirmReceiverAction();
    expect(outbox.saveReceiptAtomic).toHaveBeenCalledWith(expect.objectContaining({
      lines: [expect.objectContaining({ quantityAccepted: 10, quantityRejected: 0, rejectionReason: undefined })]
    }));
  });

  it('seleccionar un archivo inválido elimina la foto anterior', () => {
    page.receiverPhoto.set(new File(['old'], 'old.jpg', { type: 'image/jpeg' }));
    page.onReceiverPhoto({ target: { files: [new File(['pdf'], 'evidence.pdf', { type: 'application/pdf' })] } } as unknown as Event);
    expect(page.receiverPhoto()).toBeNull();
    expect(page.receiverError()).toContain('imagen');
  });

  // #162: el receptor no recibe un bulto que el conductor aún no entregó.
  it('bulto despachado pero no entregado muestra espera y no guarda', async () => {
    outbox.loadReceiverUnit = vi.fn(async () => ({ unit: { ...unit, status: 'Dispatched' }, cached: true }));
    await page.loadReceiverUnit('hu', 'qr');
    expect(page.receiverError()).toContain('El Conductor aún no entrega este bulto');
    await page.confirmReceiverAction();
    expect(outbox.saveReceiptAtomic).not.toHaveBeenCalled();
  });

  it('bulto ya recibido informa y no permite un segundo recibo', async () => {
    outbox.loadReceiverUnit = vi.fn(async () => ({ unit: { ...unit, status: 'Received' }, cached: true }));
    await page.loadReceiverUnit('hu', 'qr');
    expect(page.receiverError()).toContain('ya fue recibido');
    await page.confirmReceiverAction();
    expect(outbox.saveReceiptAtomic).not.toHaveBeenCalled();
  });

  // #163: acción de limpiar pantalla y resetear formulario del Receptor tras registro
  it('#163: resetReceiverScreen limpia el estado, señales y formulario del receptor', async () => {
    page.qrInputText = 'QR-123';
    page.receiverSaved.set(true);
    page.receiverConfirmedMessage.set('Guardado');
    page.receiverError.set('Error previo');
    page.receiverLines = [{ deliveryLineId: 'dl-1' } as any];

    page.resetReceiverScreen();

    expect(page.qrInputText).toBe('');
    expect(page.receiverSaved()).toBe(false);
    expect(page.receiverConfirmedMessage()).toBeNull();
    expect(page.receiverError()).toBeNull();
    expect(page.receiverActiveBox()).toBeNull();
    expect(page.receiverQr()).toBeNull();
    expect(page.receiverLines).toEqual([]);
    expect(page.receiverPhoto()).toBeNull();
  });
});

describe('Conductor: Botones simplificados y regla de aviso de revisión 12h / día anterior', () => {
  let page: HomePage;
  let outbox: {
    pendingCount: ReturnType<typeof signal<number>>;
    activeAssignment: ReturnType<typeof signal<any>>;
    activeDriverNotice: ReturnType<typeof signal<any>>;
    lastDownloadTimestamp: ReturnType<typeof signal<number>>;
    downloadRouteAssignment: ReturnType<typeof vi.fn>;
    effectiveDeliveryStatus?: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    outbox = {
      pendingCount: signal(0),
      activeAssignment: signal(null),
      activeDriverNotice: signal(null),
      lastDownloadTimestamp: signal(0),
      downloadRouteAssignment: vi.fn(async () => ({} as any)),
      effectiveDeliveryStatus: vi.fn((_id: string, status: string) => status)
    };

    TestBed.configureTestingModule({
      imports: [HomePage],
      providers: [
        { provide: MobileAuthService, useValue: { currentRole: signal('Conductor'), me: signal({ displayName: 'Conductor' }) } },
        { provide: FieldOutboxService, useValue: outbox },
        { provide: FIELD_API, useValue: {} },
        { provide: Router, useValue: { navigate: vi.fn() } },
        { provide: AlertController, useValue: {} },
        { provide: ToastController, useValue: { create: vi.fn().mockResolvedValue({ present: vi.fn() }) } }
      ]
    });

    page = TestBed.createComponent(HomePage).componentInstance;
  });

  it('no muestra aviso si no hay activeDriverNotice', () => {
    outbox.activeDriverNotice.set(null);
    expect(page.shouldShowDriverNotice()).toBeNull();
  });

  it('no muestra aviso si descargó hoy hace menos de 12 horas', () => {
    outbox.activeDriverNotice.set({ revision: 2, message: 'Ruta editada', routePlanId: 'RP-1' });
    // Descargó hace 10 minutos hoy
    outbox.lastDownloadTimestamp.set(Date.now() - 10 * 60 * 1000);
    expect(page.shouldShowDriverNotice()).toBeNull();
  });

  it('muestra aviso si han pasado más de 12 horas sin actualizar', () => {
    outbox.activeDriverNotice.set({ revision: 2, message: 'Ruta editada', routePlanId: 'RP-1' });
    // Descargó hace 13 horas
    outbox.lastDownloadTimestamp.set(Date.now() - 13 * 60 * 60 * 1000);
    expect(page.shouldShowDriverNotice()).toEqual(expect.objectContaining({ revision: 2 }));
  });

  it('muestra aviso si se descargó el día de ayer', () => {
    outbox.activeDriverNotice.set({ revision: 2, message: 'Ruta editada', routePlanId: 'RP-1' });
    const yesterday = new Date();
    yesterday.setDate(yesterday.getDate() - 1);
    yesterday.setHours(20, 0, 0, 0);
    outbox.lastDownloadTimestamp.set(yesterday.getTime());
    expect(page.shouldShowDriverNotice()).toEqual(expect.objectContaining({ revision: 2 }));
  });

  it('hasDownloadedRoute cambia a true cuando hay rutas asignadas', () => {
    expect(page.hasDownloadedRoute()).toBe(false);

    outbox.activeAssignment.set({
      routes: [{ routePlanId: 'RP-1', deliveries: [] }]
    });
    expect(page.hasDownloadedRoute()).toBe(true);
  });

  it('isRouteCompleted es true solo cuando todas las entregas están cerradas (Received/Partial)', () => {
    outbox.activeAssignment.set({
      routes: [{
        routePlanId: 'RP-1',
        deliveries: [
          { deliveryId: 'd-1', status: 'Delivered', kind: 'Delivery', pointName: 'P1', pointCode: 'C1', address: 'A1', handlingUnits: [] }
        ]
      }]
    });
    // Entregado no es Received aún
    expect(page.isRouteCompleted()).toBe(false);

    // Cambiar a Received
    outbox.activeAssignment.set({
      routes: [{
        routePlanId: 'RP-1',
        deliveries: [
          { deliveryId: 'd-1', status: 'Received', kind: 'Delivery', pointName: 'P1', pointCode: 'C1', address: 'A1', handlingUnits: [] }
        ]
      }]
    });
    expect(page.isRouteCompleted()).toBe(true);
  });
});


