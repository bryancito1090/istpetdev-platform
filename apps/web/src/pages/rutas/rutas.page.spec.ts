import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { ApiService } from '@shared/api';
import { RutasPage } from './rutas.page';

describe('RutasPage (FR-21)', () => {
  it('oculta al Auditor las acciones para generar y publicar rutas', () => {
    const plan = {
      id: 'route-1',
      code: 'R-1',
      revision: 1,
      status: 'Proposed',
      vehicleId: 'vehicle-1',
      plannedStartUtc: '2026-10-14T12:00:00Z',
      totalDistanceKm: 0,
      totalDurationMinutes: 0,
      stopsCount: 0,
    };
    TestBed.configureTestingModule({
      imports: [RutasPage],
      providers: [
        provideRouter([]),
        { provide: AuthSession, useValue: { canWrite: () => false } },
        {
          provide: ApiService,
          useValue: {
            getRoutePlans: vi.fn(() => of([plan])),
            getRoutePlanById: vi.fn(() => of({ ...plan, stops: [] })),
            getRoutePlanGeometry: vi.fn(() => of(null)),
            getDrivers: vi.fn(() => of([{ accountId: 'drv-1', displayName: 'Conductor 1' }])),
            getServicePoints: vi.fn(() => of([])),
            getDeliveryOrders: vi.fn(() => of([])),
            getWarehouses: vi.fn(() => of([])),
            getEntityData: vi.fn(() => of([])),
            getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
          },
        },
      ],
    });
    const fixture = TestBed.createComponent(RutasPage);
    fixture.detectChanges();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    expect(buttons.some(button => /Generar|Publicar/.test(button.textContent ?? ''))).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Pendiente de publicación');
  });

  it('permite al Planificador consultar la bandeja de capturas rechazadas por punto (Issue #114)', () => {
    const rejectedCapture = {
      receiptId: 'rcpt-1',
      deliveryId: 'del-1',
      deliveryCode: 'DEL-001',
      servicePointId: 'sp-1',
      servicePointCode: 'CS-NORTE',
      servicePointName: 'Centro de Salud Norte',
      driverId: 'drv-1',
      driverName: 'Carlos Chofer',
      // #162: el campo legado sigue viajando, pero la bandeja atribuye la recepción al Receptor.
      registeredByName: 'Laura Receptor',
      receivedAtUtc: '2026-10-15T10:00:00Z',
      reason: 'Versión desactualizada o colisión de negocio (ESC-23)',
      status: 'RequiresReview',
      lines: [
        {
          productId: 'prod-1',
          productName: 'Paracetamol 500mg',
          sku: 'MED-001',
          quantityAccepted: 8,
          quantityRejected: 2,
          rejectionReason: 'Cajas deterioradas'
        }
      ]
    };
    const getRejectedCapturesMock = vi.fn(() => of({ items: [rejectedCapture], count: 1 }));

    TestBed.configureTestingModule({
      imports: [RutasPage],
      providers: [
        provideRouter([]),
        { provide: AuthSession, useValue: { canWrite: () => true } },
        {
          provide: ApiService,
          useValue: {
            getRoutePlans: vi.fn(() => of([])),
            getRoutePlanById: vi.fn(() => of(null)),
            getRoutePlanGeometry: vi.fn(() => of(null)),
            getDrivers: vi.fn(() => of([{ accountId: 'drv-1', displayName: 'Conductor 1' }])),
            getServicePoints: vi.fn(() => of([{ id: 'sp-1', code: 'CS-NORTE', name: 'Centro de Salud Norte' }])),
            getDeliveryOrders: vi.fn(() => of([])),
            getWarehouses: vi.fn(() => of([])),
            getEntityData: vi.fn(() => of([])),
            getRejectedCaptures: getRejectedCapturesMock,
          },
        },
      ],
    });
    const fixture = TestBed.createComponent(RutasPage);
    fixture.detectChanges();

    fixture.componentInstance.activeTab.set('rechazos');
    fixture.detectChanges();

    expect(getRejectedCapturesMock).toHaveBeenCalled();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Bandeja de Capturas Rechazadas');
    expect(text).toContain('Centro de Salud Norte');
    expect(text).toContain('Paracetamol 500mg');
    expect(text).toContain('Requiere Revisión');
    expect(text).toContain('DEL-001');
    // #162: la recepción la registra el Receptor; el campo legado no se presenta como «Conductor».
    expect(text).toContain('Registrado por:');
    expect(text).toContain('Laura Receptor');
    expect(text).not.toContain('Conductor:');
  });

  it('usa el nombre legado con la etiqueta Registrado por si el servidor no envía registeredByName', () => {
    const legacyCapture = {
      receiptId: 'rcpt-legacy',
      deliveryId: 'del-legacy',
      deliveryCode: 'DEL-LEG',
      servicePointId: 'sp-1',
      servicePointCode: 'CS-NORTE',
      servicePointName: 'Centro de Salud Norte',
      driverId: 'drv-legacy',
      driverName: 'Receptor Legado',
      receivedAtUtc: '2026-10-15T10:00:00Z',
      reason: 'Daño',
      status: 'Partial',
      lines: [],
    };

    TestBed.configureTestingModule({
      imports: [RutasPage],
      providers: [
        provideRouter([]),
        { provide: AuthSession, useValue: { canWrite: () => true } },
        {
          provide: ApiService,
          useValue: {
            getRoutePlans: vi.fn(() => of([])),
            getRoutePlanById: vi.fn(() => of(null)),
            getRoutePlanGeometry: vi.fn(() => of(null)),
            getDrivers: vi.fn(() => of([])),
            getServicePoints: vi.fn(() => of([{ id: 'sp-1', code: 'CS-NORTE', name: 'Centro de Salud Norte' }])),
            getDeliveryOrders: vi.fn(() => of([])),
            getWarehouses: vi.fn(() => of([])),
            getEntityData: vi.fn(() => of([])),
            getRejectedCaptures: vi.fn(() => of({ items: [legacyCapture], count: 1 })),
          },
        },
      ],
    });
    const fixture = TestBed.createComponent(RutasPage);
    fixture.detectChanges();
    fixture.componentInstance.activeTab.set('rechazos');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Registrado por:');
    expect(text).toContain('Receptor Legado');
    expect(text).not.toContain('Conductor:');
  });

  describe('Comparativa A/B/C en el Asistente de Rutas (#242)', () => {
    it('renderiza la tabla comparativa A/B/C con precio de combustible, badge ★ en la recomendada y justificación', () => {
      TestBed.configureTestingModule({
        imports: [RutasPage],
        providers: [
          provideRouter([]),
          { provide: AuthSession, useValue: { canWrite: () => true, account: () => ({ id: 'usr-1', email: 'plan@test.com' }) } },
          {
            provide: ApiService,
            useValue: {
              getRoutePlans: vi.fn(() => of([])),
              getRoutePlanById: vi.fn(() => of(null)),
              getRoutePlanGeometry: vi.fn(() => of(null)),
              getDrivers: vi.fn(() => of([])),
              getServicePoints: vi.fn(() => of([])),
              getDeliveryOrders: vi.fn(() => of([])),
              getWarehouses: vi.fn(() => of([])),
              getEntityData: vi.fn(() => of([])),
              getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
              suggestRoutes: vi.fn(() => of({ options: [] })),
            },
          },
        ],
      });
      const fixture = TestBed.createComponent(RutasPage);
      const component = fixture.componentInstance;
      fixture.detectChanges();

      // Abrir asistente y avanzar al paso 3
      component.isWizardOpen.set(true);
      component.wizardStep.set(3);
      fixture.detectChanges();

      const el = fixture.nativeElement;
      const text = el.textContent;

      // Verifica encabezado y panel de pitch
      expect(text).toContain('Comparativa de Alternativas A/B/C');
      expect(text).toContain('Panel Pitch #200');

      // Verifica línea de precio de combustible con valor, fecha y fuente
      expect(text).toContain('Combustible de referencia:');
      expect(text).toContain('USD 2.40/galón');
      expect(text).toContain('16-oct-2026');
      expect(text).toContain('Petroecuador');

      // Verifica presencia de columnas A, B, C
      expect(text).toContain('Opción A');
      expect(text).toContain('Opción B');
      expect(text).toContain('Opción C');

      // Verifica insignia ★ RECOMENDADA
      expect(text).toContain('★ RECOMENDADA');

      // Verifica justificación de recomendación
      expect(text).toContain('Recomendación: Opción B');
      expect(text).toContain('cumple los 3 hospitales críticos');

      // Verifica los 3 botones de acción requeridos
      const buttons = Array.from(el.querySelectorAll('button')) as HTMLButtonElement[];
      const btnPublicar = buttons.find(b => b.textContent?.includes('Publicar'));
      const btnElegirOtra = buttons.find(b => b.textContent?.includes('Elegir otra'));
      const btnVerMapa = buttons.find(b => b.textContent?.includes('Ver en mapa'));

      expect(btnPublicar).toBeDefined();
      expect(btnElegirOtra).toBeDefined();
      expect(btnVerMapa).toBeDefined();

      // Verifica filas de la tabla
      expect(text).toContain('Kilómetros');
      expect(text).toContain('Combustible');
      expect(text).toContain('Desgaste vehículo');
      expect(text).toContain('Conductor');
      expect(text).toContain('Ayudantes (n.º y costo)');
      expect(text).toContain('Merma esperada');
      expect(text).toContain('Costo total');
      expect(text).toContain('Críticos a tiempo');
      expect(text).toContain('Pedidos pendientes');
      expect(text).toContain('Riesgos');

      // Celdas de Opción C reflejan inclusión en tarifa
      expect(text).toContain('incluido en la tarifa');
      expect(text).toContain('del operador');
    });

    it('al hacer clic en «Publicar recomendada» selecciona la opción B y avanza al paso 4', () => {
      TestBed.configureTestingModule({
        imports: [RutasPage],
        providers: [
          provideRouter([]),
          { provide: AuthSession, useValue: { canWrite: () => true } },
          {
            provide: ApiService,
            useValue: {
              getRoutePlans: vi.fn(() => of([])),
              getRoutePlanById: vi.fn(() => of(null)),
              getRoutePlanGeometry: vi.fn(() => of(null)),
              getDrivers: vi.fn(() => of([])),
              getServicePoints: vi.fn(() => of([])),
              getDeliveryOrders: vi.fn(() => of([])),
              getWarehouses: vi.fn(() => of([])),
              getEntityData: vi.fn(() => of([])),
              getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
            },
          },
        ],
      });
      const fixture = TestBed.createComponent(RutasPage);
      const component = fixture.componentInstance;
      fixture.detectChanges();

      component.isWizardOpen.set(true);
      component.wizardStep.set(3);
      component.selectedOptionId.set('A');
      fixture.detectChanges();

      component.publishRecommended();
      fixture.detectChanges();

      expect(component.selectedOptionId()).toBe('B');
      expect(component.wizardStep()).toBe(4);
      expect(component.feedbackMessage()?.text).toContain('Opción B');
    });

    it('si la API no trae la opción C, la tabla muestra solo A y B sin romperse (#242)', () => {
      TestBed.configureTestingModule({
        imports: [RutasPage],
        providers: [
          provideRouter([]),
          { provide: AuthSession, useValue: { canWrite: () => true } },
          {
            provide: ApiService,
            useValue: {
              getRoutePlans: vi.fn(() => of([])),
              getRoutePlanById: vi.fn(() => of(null)),
              getRoutePlanGeometry: vi.fn(() => of(null)),
              getDrivers: vi.fn(() => of([])),
              getServicePoints: vi.fn(() => of([])),
              getDeliveryOrders: vi.fn(() => of([])),
              getWarehouses: vi.fn(() => of([])),
              getEntityData: vi.fn(() => of([])),
              getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
            },
          },
        ],
      });
      const fixture = TestBed.createComponent(RutasPage);
      const component = fixture.componentInstance;
      fixture.detectChanges();

      // Configurar modo API en vivo con solo 2 opciones (A y B)
      component.useMockABC.set(false);
      component.routeOptions.set([
        {
          optionId: 'A',
          title: 'Opción A Consolidada',
          description: 'Ruta directa',
          isRecommended: false,
          evaluation: {
            isValid: true,
            totalDistanceKm: 42,
            totalDurationMinutes: 120,
            totalCost: 55.40,
            maxWeightOnRoute: 600,
            maxVolumeOnRoute: 2.5,
            stops: [],
            validationErrors: [],
            warnings: [],
            coordinates: []
          },
          stops: [],
          sourcingLines: [],
          vehicleMatches: []
        },
        {
          optionId: 'B',
          title: 'Opción B Prioritaria',
          description: 'Ruta con prioridad a hospitales',
          isRecommended: true,
          evaluation: {
            isValid: true,
            totalDistanceKm: 49,
            totalDurationMinutes: 135,
            totalCost: 62.10,
            maxWeightOnRoute: 600,
            maxVolumeOnRoute: 2.5,
            stops: [],
            validationErrors: [],
            warnings: [],
            coordinates: []
          },
          stops: [],
          sourcingLines: [],
          vehicleMatches: []
        }
      ]);

      component.isWizardOpen.set(true);
      component.wizardStep.set(3);
      fixture.detectChanges();

      const text = fixture.nativeElement.textContent;
      expect(text).toContain('Opción A');
      expect(text).toContain('Opción B');
      expect(text).not.toContain('Opción C');
      expect(text).toContain('42 km');
      expect(text).toContain('49 km');
      expect(text).toContain('$ 55.40');
      expect(text).toContain('$ 62.10');
      expect(component.comparisonOptions().length).toBe(2);
    });
  });
});
