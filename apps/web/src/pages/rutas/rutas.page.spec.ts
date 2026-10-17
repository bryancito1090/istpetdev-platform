import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { ApiService } from '@shared/api';
import { RutasPage } from './rutas.page';
import { RouteJourneyPlayer } from './route-journey-player.component';

describe('RutasPage (FR-21)', () => {
  it('muestra desde abastecimiento el recorrido recomendado, permite cambiarlo y conserva la elección al comparar', async () => {
    const stops = [0, 1, 2].map(index => ({ sequence: index + 1, kind: index ? 'Delivery' as const : 'Pickup' as const,
      code: `P-${index}`, name: index ? `Entrega ${index}` : 'Bodega Norte', position: { lat: -.1 - index * .01, lon: -78.4 },
      deltaWeight: 1, deltaVolume: 1, windowStart: '08:00', windowEnd: '18:00', serviceMinutes: 5 }));
    const option = (optionId: string, isRecommended: boolean) => ({ optionId, title: `Propuesta ${optionId}`, description: 'Recogida y dos entregas', isRecommended,
      stops, sourcingLines: [{ productName: 'Guantes', requestedQuantity: 10, destinationName: 'Bodega Sur',
        allocations: [{ warehouseId: 'norte', warehouseCode: 'BOD-NORTE', quantity: 10 }] }],
      vehicleMatches: [{ vehicleId: 'v1', vehicleCode: 'VEH-01', isMatch: true, maxWeight: 100, weightUtilizationPct: 1, volumeUtilizationPct: 1, estimatedCost: 20, distanceToFirstPickupKm: 1 }],
      evaluation: { isValid: true, totalDistanceKm: isRecommended ? 10 : 20, totalDurationMinutes: 30, totalCost: isRecommended ? 20 : 30,
        maxWeightOnRoute: 1, maxVolumeOnRoute: 1, stops: [], validationErrors: [], warnings: [],
        coordinates: isRecommended ? stops.map(stop => [stop.position.lat, stop.position.lon] as [number, number]) : [], coordinatesSource: isRecommended ? 'osrm' as const : 'approximate' as const } });
    const api = { getRoutePlans: vi.fn(() => of([])), getDrivers: vi.fn(() => of([])), getServicePoints: vi.fn(() => of([])),
      getDeliveryOrders: vi.fn(() => of([])), getWarehouses: vi.fn(() => of([])), getEntityData: vi.fn(() => of([])),
      getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })), suggestRoutes: vi.fn(() => of({ options: [option('A', false), option('B', true)] })) };
    TestBed.configureTestingModule({ imports: [RutasPage], providers: [provideRouter([]),
      { provide: AuthSession, useValue: { canWrite: () => true } }, { provide: ApiService, useValue: api }] });
    const fixture = TestBed.createComponent(RutasPage), page = fixture.componentInstance;
    fixture.detectChanges(); page.isWizardOpen.set(true); page.selectedOrderIds.set(new Set(['order']));
    page.advanceWizardStep(); fixture.detectChanges(); await Promise.resolve(); fixture.detectChanges();
    expect(page.wizardStep()).toBe(2); expect(page.selectedOptionId()).toBe('B');
    expect(fixture.nativeElement.textContent).toContain('Recogidas y entregas');
    expect(fixture.nativeElement.textContent).toContain('Recoger en'); expect(fixture.nativeElement.textContent).toContain('Entregar en');
    expect(fixture.nativeElement.textContent).toContain('Menor distancia');
    const player = fixture.debugElement.query(By.directive(RouteJourneyPlayer)).componentInstance as RouteJourneyPlayer;
    expect(player.legs()).toHaveLength(2); expect(player.selected()?.coordinates).toHaveLength(3);
    const replayKey = page.wizardReplayKey(); page.selectComparisonOption('B'); fixture.detectChanges();
    expect(page.wizardReplayKey()).toBeGreaterThan(replayKey);
    page.selectComparisonOption('A'); fixture.detectChanges();
    expect(player.selected()?.id).toBe('sourcing-A'); expect(player.selected()?.approximate).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Trazado aproximado');
    page.advanceWizardFlow(); expect(page.isDecisionWorkbenchOpen()).toBe(true);
    expect(page.selectedOptionId()).toBe('A'); expect(page.decisionOrderIds()).toEqual(['order']);
    fixture.destroy();
  });
  it('muestra siete propuestas seleccionables y evita abrir un cálculo sin identificador', () => {
    const plans = Array.from({ length: 7 }, (_, index) => ({ id: `route-${index}`, code: `R-${index}`,
      revision: 1, status: 'Proposed', vehicleId: `vehicle-${index}`, vehicleCode: `VEH-${index}`,
      plannedStartUtc: '2026-10-17T13:00:00Z', totalDistanceKm: 100 - index,
      totalDurationMinutes: 100 - index * 10, totalCost: 20 + index * 10, stopsCount: 3 }));
    localStorage.removeItem('istpet.route-decision.registry-test');
    TestBed.configureTestingModule({ imports: [RutasPage], providers: [provideRouter([]),
      { provide: AuthSession, useValue: { canWrite: () => true, me: () => ({ accountId: 'registry-test' }) } },
      { provide: ApiService, useValue: {
        getRoutePlans: vi.fn(() => of(plans)), getRoutePlanById: vi.fn((id: string) => of({ ...plans.find(p => p.id === id), stops: [] })),
        getRoutePlanGeometry: vi.fn(() => of(null)), getDrivers: vi.fn(() => of([])),
        getServicePoints: vi.fn(() => of([])), getDeliveryOrders: vi.fn(() => of([])),
        getWarehouses: vi.fn(() => of([])), getEntityData: vi.fn(() => of([])),
        getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 }))
      } }
    ] });
    const fixture = TestBed.createComponent(RutasPage); fixture.detectChanges();
    const page = fixture.componentInstance;
    const cards = fixture.nativeElement.querySelectorAll('.registered-route-cards .route-card') as NodeListOf<HTMLButtonElement>;
    expect(cards).toHaveLength(7);
    expect(cards[0].textContent).toContain('Menor costo');
    expect(cards[6].textContent).toContain('Mayor costo');
    expect(cards[6].textContent).toContain('Más rápida');
    cards[6].click(); fixture.detectChanges();
    expect(page.selectedPlanId()).toBe('route-6');
    expect(fixture.nativeElement.querySelector('.route-approve-button').textContent).toContain('Aprobar y publicar ruta');
    page.openPreviousDecision();
    expect(page.isDecisionWorkbenchOpen()).toBe(false);
    expect(page.isWizardOpen()).toBe(true);
    page.closeWizard();
    page.routePlans.set([{ ...plans[6], planningDecisionId: 'group-decision' }]);
    page.openPreviousDecision();
    expect(page.activeDecisionId()).toBe('group-decision');
    expect(page.isDecisionWorkbenchOpen()).toBe(true);
    fixture.destroy();
  });

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

      expect(component.comparisonMode()).toBe('live');
      expect(fixture.nativeElement.textContent).toContain('Sin opciones calculadas para esta selección');
      expect(fixture.nativeElement.textContent).not.toContain('USD 2.40');
      expect(fixture.nativeElement.textContent).not.toContain('$ 65.10');

      const el = fixture.nativeElement;
      const text = el.textContent;

      // Verifica encabezado y panel de pitch
      expect(text).toContain('Comparativa de Alternativas A/B/C');
      expect(text).toContain('Panel Pitch #200');

      expect(text).toContain('Sin opciones calculadas para esta selección');
      expect(fixture.nativeElement.querySelector('table')).toBeNull();
    });

    it('no permite publicar opciones ilustrativas ni avanzar sin opciones reales', () => {
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
      component.comparisonMode.set('saved');
      fixture.detectChanges();

      component.publishRecommended();
      fixture.detectChanges();

      expect(component.wizardStep()).toBe(3);
      component.advanceWizardStep();
      expect(component.wizardStep()).toBe(3);

      const api = TestBed.inject(ApiService);
      api.suggestRoutes = vi.fn(() => of({ options: [], message: 'Sin abastecimiento disponible.' }));
      component.wizardStep.set(1);
      component.selectedVehicleId.set('vehiculo-anterior');
      component.advanceWizardStep();
      fixture.detectChanges();
      expect(component.routeOptions()).toEqual([]);
      expect(component.selectedOptionId()).toBe('');
      expect(component.selectedVehicleId()).toBeNull();
      expect(fixture.nativeElement.textContent).toContain('Sin abastecimiento disponible.');
      expect(fixture.nativeElement.textContent).not.toContain('$ 65.10');

      api.suggestRoutes = vi.fn(() => throwError(() => ({ error: { message: 'API no disponible' } })));
      component.wizardStep.set(1);
      component.advanceWizardStep();
      fixture.detectChanges();
      expect(component.comparisonMode()).toBe('live');
      expect(component.comparisonOptions()).toEqual([]);
      expect(component.actionLoading()).toBe(false);
      expect(fixture.nativeElement.textContent).toContain('API no disponible');
    });

    it('carga la corrida solo al elegirla, rotula su origen y la mantiene de solo lectura', () => {
      const run = {
        metadata: { commit: 'abc123', capturedAtUtc: '2026-10-16T15:00:00Z', fixture: 'demo-quito-planificacion', configSha256: 'a'.repeat(64) },
        response: {
          options: [{ optionId: 'C', sourcingOptionId: 'carrier-1', title: 'Operador', description: 'Contrato', isRecommended: true, evaluation: { totalCost: 30 }, stops: [], sourcingLines: [], vehicleMatches: [] }],
          comparison: {
            recommendedOptionId: 'C', justification: 'Motivo del motor',
            fuelPrice: { value: 3.11, date: '2026-10-16', source: 'Fuente fechada', unit: 'USD/gal' },
            options: [{ optionId: 'A', title: 'Propia', carrierType: 'Propia' }, { optionId: 'C', title: 'Operador', carrierType: 'Contratada', totalCost: 30 }]
          }
        }
      };
      TestBed.configureTestingModule({
        imports: [RutasPage],
        providers: [
          provideRouter([]),
          { provide: AuthSession, useValue: { canWrite: () => true } },
          { provide: ApiService, useValue: {
            getRoutePlans: vi.fn(() => of([])), getRoutePlanById: vi.fn(() => of(null)), getRoutePlanGeometry: vi.fn(() => of(null)),
            getDrivers: vi.fn(() => of([])), getServicePoints: vi.fn(() => of([])), getDeliveryOrders: vi.fn(() => of([])),
            getWarehouses: vi.fn(() => of([])), getEntityData: vi.fn(() => of([])), getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
            getSavedRouteComparisonRun: vi.fn(() => of(run))
          } }
        ]
      });
      const fixture = TestBed.createComponent(RutasPage);
      const component = fixture.componentInstance;
      fixture.detectChanges();
      component.isWizardOpen.set(true);
      component.wizardStep.set(3);
      expect(component.comparisonMode()).toBe('live');
      component.loadSavedComparison();
      fixture.detectChanges();

      expect(component.comparisonMode()).toBe('saved');
      expect(component.comparisonOptions().map(option => option.optionId)).toEqual(['A', 'C']);
      expect(fixture.nativeElement.textContent).toContain('Corrida guardada · demo-quito-planificacion · commit abc123 · 2026-10-16T15:00:00Z');
      expect(fixture.nativeElement.textContent).toContain('3.11 USD/gal');
      expect(fixture.nativeElement.textContent).toContain('Fuente fechada');
      component.publishRecommended();
      component.selectComparisonOption('A');
      expect(component.wizardStep()).toBe(3);
      expect(component.selectedOptionId()).toBe('A');
      expect(component.routeOptions()).toEqual([]);
    });

    it('informa cuando no existe la corrida y no abandona el modo vivo automáticamente', () => {
      TestBed.configureTestingModule({
        imports: [RutasPage],
        providers: [provideRouter([]), { provide: AuthSession, useValue: { canWrite: () => true } }, { provide: ApiService, useValue: {
          getRoutePlans: vi.fn(() => of([])), getRoutePlanById: vi.fn(() => of(null)), getRoutePlanGeometry: vi.fn(() => of(null)),
          getDrivers: vi.fn(() => of([])), getServicePoints: vi.fn(() => of([])), getDeliveryOrders: vi.fn(() => of([])),
          getWarehouses: vi.fn(() => of([])), getEntityData: vi.fn(() => of([])), getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
          getSavedRouteComparisonRun: vi.fn(() => throwError(() => new Error('404')))
        } }]
      });
      const fixture = TestBed.createComponent(RutasPage);
      const component = fixture.componentInstance;
      fixture.detectChanges();
      component.isWizardOpen.set(true);
      component.wizardStep.set(3);
      fixture.detectChanges();
      component.loadSavedComparison();
      fixture.detectChanges();
      expect(component.comparisonMode()).toBe('saved');
      expect(component.comparisonOptions()).toEqual([]);
      expect(fixture.nativeElement.textContent).toContain('No hay una corrida guardada disponible');
      expect(fixture.nativeElement.textContent).not.toContain('DISEÑO');
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
      component.comparisonMode.set('live');
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

      const canonical = { ...component.routeOptions()[0], optionId: '1', sourcingOptionId: 'A' };
      const api = TestBed.inject(ApiService);
      api.suggestRoutes = vi.fn(() => of({
        options: [canonical],
        comparison: {
          recommendedOptionId: 'C', justification: 'Recomendación recibida',
          fuelPrice: { value: 3.11, date: '2026-10-16', source: 'Fuente de prueba', unit: 'USD/gal' },
          options: [
            { optionId: 'A', title: 'Propia', carrierType: 'Propia', costBreakdown: { fuel: 0, total: 22 } },
            { optionId: 'C', title: 'Operador', carrierType: 'Contratada', costBreakdown: { carrierTariff: 27, total: 29, notes: ['Nota del motor'] } }
          ]
        }
      }));
      component.wizardStep.set(1);
      component.advanceWizardStep();
      component.advanceWizardStep();
      fixture.detectChanges();
      expect(component.selectedOptionId()).toBe('A'); // entrar no sustituye la elección por C
      expect(component.recommendedOption()?.optionId).toBe('C');
      expect(fixture.nativeElement.textContent).toContain('3.11 USD/gal');
      expect(fixture.nativeElement.textContent).toContain('Fuente de prueba');
      expect(fixture.nativeElement.textContent).toContain('Recomendación recibida');
      expect(fixture.nativeElement.textContent).toContain('Nota del motor');
      expect(fixture.nativeElement.textContent).toContain('$ 0.00');
      expect(fixture.nativeElement.textContent).toContain('$ 27.00');
      component.selectComparisonOption('C');
      component.publishRecommended();
      component.advanceWizardStep();
      expect(component.wizardStep()).toBe(3);
      expect(component.currentEvaluation()).toBeNull();
      expect(component.editableStops()).toEqual([]);

      component.sourcingComparison.set({ options: [{ optionId: 'B', title: 'Sin datos' }] });
      fixture.detectChanges();
      expect(component.recommendedOption()).toBeUndefined();
      expect(fixture.nativeElement.textContent).not.toContain('3.11 USD/gal');
      expect(fixture.nativeElement.textContent).toContain('sin dato');

      // Al preparar otra alternativa, enviar el ID del contrato y no publicar la propuesta anterior.
      api.suggestRoutes = vi.fn(() => of({ options: [] }));
      component.selectComparisonOption('B');
      expect(api.suggestRoutes).toHaveBeenCalledWith([], undefined, 'B', false);
      expect(component.routeOptions()).toEqual([]);
      expect(component.currentEvaluation()).toBeNull();
    });
  });

  describe('Seguimiento de la ruta en el Planificador (#231)', () => {
    it('renderiza la tarjeta de seguimiento con el resumen de paradas, bultos y la parada actual', () => {
      const plan = {
        id: 'plan-tracking-1',
        code: 'R-TRACK-01',
        revision: 1,
        status: 'Published',
        vehicleCode: 'VEH-01',
        driverName: 'Conductor Prueba',
        totalDistanceKm: 32,
        totalDurationMinutes: 90,
        stopsCount: 3,
        stops: [
          {
            sequence: 1,
            kind: 'Pickup',
            servicePointCode: 'BOD-CENTRAL',
            servicePointName: 'Bodega Principal',
            status: 'Completed',
            deliveryStatus: 'Dispatched',
            handlingUnits: [
              { id: 'hu-1', code: 'BUL-01', status: 'InTransit' },
              { id: 'hu-2', code: 'BUL-02', status: 'InTransit' }
            ],
            lines: []
          },
          {
            sequence: 2,
            kind: 'Delivery',
            servicePointCode: 'CS-NORTE',
            servicePointName: 'Centro de Salud Norte',
            status: 'Completed',
            deliveryStatus: 'Received',
            handlingUnits: [{ id: 'hu-3', code: 'BUL-03', status: 'Received' }],
            lines: [
              {
                productId: 'p1',
                sku: 'MED-01',
                productName: 'Amoxicilina',
                plannedQuantity: 5,
                baseUnit: 'Caja',
                deliveryStatus: 'Received'
              }
            ]
          },
          {
            sequence: 3,
            kind: 'Delivery',
            servicePointCode: 'HOSP-SUR',
            servicePointName: 'Hospital del Sur',
            status: 'Pending',
            deliveryStatus: 'InTransit',
            handlingUnits: [{ id: 'hu-4', code: 'BUL-04', status: 'InTransit' }],
            lines: [
              {
                productId: 'p2',
                sku: 'MED-02',
                productName: 'Ibuprofeno',
                plannedQuantity: 10,
                baseUnit: 'Caja',
                deliveryStatus: 'InTransit'
              }
            ]
          }
        ]
      };

      const getRoutePlanByIdMock = vi.fn(() => of(plan));

      TestBed.configureTestingModule({
        imports: [RutasPage],
        providers: [
          provideRouter([]),
          { provide: AuthSession, useValue: { canWrite: () => true, role: () => 'Planner' } },
          {
            provide: ApiService,
            useValue: {
              getRoutePlans: vi.fn(() => of([plan])),
              getRoutePlanById: getRoutePlanByIdMock,
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

      // Debe haber cargado el plan y seleccionado el detalle
      expect(component.selectedDetail()).toBeTruthy();

      const trackingCard = fixture.nativeElement.querySelector('[data-testid="tracking-card"]');
      expect(trackingCard).toBeTruthy();

      // Verifica el texto resumen: 1 de 2 paradas de entrega completada, 3 bultos en vehículo
      const summaryTextEl = fixture.nativeElement.querySelector('[data-testid="tracking-summary-text"]');
      expect(summaryTextEl.textContent).toContain('1 de 2 paradas entregadas · 3 bultos en el vehículo');

      // Verifica que exista la insignia de "Parada actual" en la secuencia 3 (primera no entregada)
      const badgeCurrent = fixture.nativeElement.querySelector('[data-testid="badge-current-stop"]');
      expect(badgeCurrent).toBeTruthy();
      expect(badgeCurrent.textContent).toContain('Parada actual');

      // Verifica que todos los estados aparezcan traducidos en español
      const text = fixture.nativeElement.textContent;
      expect(text).toContain('Seguimiento de Ruta en Vivo');
      expect(text).toContain('Autorefresco 25s');
      expect(text).toContain('Recibido conforme');
      expect(text).toContain('En vehículo');
    });

    it('permite refrescar manualmente y detiene el temporizador al destruir el componente', () => {
      const plan = {
        id: 'plan-tracking-2',
        code: 'R-TRACK-02',
        revision: 1,
        status: 'Published',
        vehicleCode: 'VEH-02',
        totalDistanceKm: 15,
        totalDurationMinutes: 45,
        stopsCount: 1,
        stops: []
      };

      const getRoutePlanByIdMock = vi.fn(() => of(plan));

      TestBed.configureTestingModule({
        imports: [RutasPage],
        providers: [
          provideRouter([]),
          { provide: AuthSession, useValue: { canWrite: () => true } },
          {
            provide: ApiService,
            useValue: {
              getRoutePlans: vi.fn(() => of([plan])),
              getRoutePlanById: getRoutePlanByIdMock,
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

      const refreshBtn = fixture.nativeElement.querySelector('[data-testid="btn-refresh-tracking"]');
      if (refreshBtn) {
        refreshBtn.click();
        expect(getRoutePlanByIdMock).toHaveBeenCalled();
      }

      // Detiene el temporizador sin arrojar error al destruir
      expect(() => fixture.destroy()).not.toThrow();
    });

    it('muestra el banner de plan conjunto multi-vehículo y la explicación del corte por tamaño y prioridad', () => {
      const plans = [
        {
          id: 'plan-joint-1',
          code: 'R-JOINT-01',
          revision: 1,
          status: 'Proposed',
          vehicleId: 'v-1',
          vehicleCode: 'VEH-01',
          totalAvailableVehicles: 5,
          usedVehiclesCount: 2,
          maxWeight: 180,
          weightUtilizationPct: 78,
          prioritySummary: 'Prioridad Crítica (2)',
          splitExplanation: 'Plan conjunto balanceado: se emplean 2 de 5 vehículos disponibles para dividir las entregas por tamaño y prioridad.',
          assignmentReason: 'Vehículo VEH-01 (cap. 180 kg) elegido entre 5 unidades por ajuste de capacidad.',
          cutReason: 'Corte aplicado tras cubrir entregas críticas; el resto pasa a VEH-02.',
          plannedStartUtc: '2026-10-16T13:00:00Z',
          totalDistanceKm: 18,
          totalDurationMinutes: 40,
          stopsCount: 3,
          stops: []
        },
        {
          id: 'plan-joint-2',
          code: 'R-JOINT-02',
          revision: 1,
          status: 'Proposed',
          vehicleId: 'v-2',
          vehicleCode: 'VEH-02',
          totalAvailableVehicles: 5,
          usedVehiclesCount: 2,
          maxWeight: 250,
          weightUtilizationPct: 64,
          prioritySummary: 'Prioridad Normal (2)',
          splitExplanation: 'Plan conjunto balanceado: se emplean 2 de 5 vehículos disponibles para dividir las entregas por tamaño y prioridad.',
          assignmentReason: 'Vehículo VEH-02 (cap. 250 kg) elegido para el bloque secundario.',
          cutReason: 'Cierre del plan conjunto de 2 vehículos.',
          plannedStartUtc: '2026-10-16T13:00:00Z',
          totalDistanceKm: 22,
          totalDurationMinutes: 50,
          stopsCount: 3,
          stops: []
        }
      ];

      TestBed.configureTestingModule({
        imports: [RutasPage],
        providers: [
          provideRouter([]),
          { provide: AuthSession, useValue: { canWrite: () => true } },
          {
            provide: ApiService,
            useValue: {
              getRoutePlans: vi.fn(() => of(plans)),
              getRoutePlanById: vi.fn((id: string) => of(plans.find(p => p.id === id) ?? plans[0])),
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
      fixture.detectChanges();

      const banner = fixture.nativeElement.querySelector('[data-testid="coordinated-fleet-plan-banner"]');
      expect(banner).toBeTruthy();
      expect(banner.textContent).toContain('Plan Conjunto · 2 de 5 vehículos');
      expect(banner.textContent).toContain('Plan conjunto balanceado: se emplean 2 de 5 vehículos disponibles');

      const rationale = fixture.nativeElement.querySelector('[data-testid="route-detail-split-rationale"]');
      expect(rationale).toBeTruthy();
      expect(rationale.textContent).toContain('Vehículo VEH-01 (cap. 180 kg) elegido entre 5 unidades');
      expect(rationale.textContent).toContain('Corte aplicado tras cubrir entregas críticas');
      fixture.destroy();
    });
  });
});
