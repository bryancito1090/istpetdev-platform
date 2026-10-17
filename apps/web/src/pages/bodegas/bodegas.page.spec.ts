import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { ApiService, WarehouseAnalysisDto, TransferSuggestionDto } from '@shared/api';
import { BodegasPage } from './bodegas.page';

describe('BodegasPage (#332)', () => {
  const mockAnalysis: WarehouseAnalysisDto[] = [
    {
      locationId: 'loc-01',
      locationCode: 'BOD-SUR',
      productId: 'prod-01',
      sku: 'DES-01',
      productName: 'Desinfectante hospitalario',
      totalConsumedInWindow: 531.9,
      windowDays: 90,
      dailyConsumptionRate: 5.91,
      monthlyConsumptionRate: 177.3,
      quantityOnHand: 690,
      quantityReserved: 0,
      availableQuantity: 690,
      coverageDays: 116.7,
      confidenceLevel: 'High',
      minStock: 0,
      maxStock: 0,
      status: 'Normal'
    },
    {
      locationId: 'loc-01',
      locationCode: 'BOD-SUR',
      productId: 'prod-02',
      sku: 'GUA-01',
      productName: 'Guantes de nitrilo caja',
      totalConsumedInWindow: 70,
      windowDays: 90,
      dailyConsumptionRate: 0.78,
      monthlyConsumptionRate: 23.3,
      quantityOnHand: 280,
      quantityReserved: 0,
      availableQuantity: 280,
      coverageDays: 360,
      confidenceLevel: 'High',
      minStock: 0,
      maxStock: 0,
      status: 'Normal'
    },
    {
      locationId: 'loc-01',
      locationCode: 'BOD-SUR',
      productId: 'prod-03',
      sku: 'PAP-01',
      productName: 'Papel higiénico industrial',
      totalConsumedInWindow: 504,
      windowDays: 90,
      dailyConsumptionRate: 5.6,
      monthlyConsumptionRate: 168,
      quantityOnHand: 1290,
      quantityReserved: 0,
      availableQuantity: 1290,
      coverageDays: 230.4,
      confidenceLevel: 'High',
      minStock: 0,
      maxStock: 0,
      status: 'Normal'
    }
  ];

  function setup(options?: {
    suggestions?: TransferSuggestionDto[];
    generateReturns?: any[];
  }) {
    const api = {
      getWarehouseAnalysis: vi.fn(() => of(mockAnalysis)),
      getTransferSuggestions: vi.fn(() => of(options?.suggestions || [])),
      getWarehousePolicies: vi.fn(() => of([])),
      generateRebalancingSuggestions: vi.fn(() => of(options?.generateReturns || []))
    };

    TestBed.configureTestingModule({
      imports: [BodegasPage],
      providers: [
        provideRouter([]),
        { provide: ApiService, useValue: api }
      ]
    });

    const fixture = TestBed.createComponent(BodegasPage);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    return { fixture, component, api };
  }

  it('carga inicialmente el análisis de inventario con 2260 unidades y se mantiene en la pestaña analysis', () => {
    const { component, fixture } = setup();

    expect(component.totalStockUnits()).toBe(2260);
    expect(component.uniqueLocationsCount()).toBe(1);
    expect(component.activeTab()).toBe('analysis');
    expect(fixture.nativeElement.textContent).toContain('2260 unidades en red');
    expect(fixture.nativeElement.textContent).toContain('BOD-SUR');
    expect(fixture.nativeElement.textContent).toContain('Desinfectante hospitalario');
  });

  it('no cambia de pestaña ni oculta datos si el análisis no genera traslados en red monobodega (#332)', () => {
    const { component, fixture, api } = setup({ suggestions: [], generateReturns: [] });

    expect(component.activeTab()).toBe('analysis');

    component.generateSuggestions();
    fixture.detectChanges();

    expect(api.generateRebalancingSuggestions).toHaveBeenCalledTimes(1);
    // Conserva la pestaña 'analysis' para que no desaparezca la tabla de existencias
    expect(component.activeTab()).toBe('analysis');
    expect(component.feedback()?.type).toBe('success');
    expect(component.feedback()?.text).toContain('1 sola bodega activa (BOD-SUR)');
    expect(fixture.nativeElement.textContent).toContain('Desinfectante hospitalario');
    expect(fixture.nativeElement.textContent).toContain('690 u');
  });

  it('cambia a la pestaña rebalancing si se generan traslados preventivos (> 0)', () => {
    const mockSuggestions: TransferSuggestionDto[] = [
      {
        id: 'sug-1',
        sourceLocationId: 'loc-01',
        sourceLocationCode: 'BOD-SUR',
        sourceLocationName: 'Bodega Sur',
        targetLocationId: 'loc-02',
        targetLocationCode: 'BOD-NORTE',
        targetLocationName: 'Bodega Norte',
        productId: 'prod-01',
        sku: 'DES-01',
        productName: 'Desinfectante hospitalario',
        suggestedQuantity: 50,
        sourceCoverageDaysBefore: 120,
        sourceCoverageDaysAfter: 60,
        targetCoverageDaysBefore: 10,
        targetCoverageDaysAfter: 55,
        status: 'Suggested',
        rationale: 'Equilibrar stock',
        createdAtUtc: '2026-10-17T10:00:00Z'
      }
    ];

    const { component, fixture } = setup({
      suggestions: mockSuggestions,
      generateReturns: mockSuggestions
    });

    component.generateSuggestions();
    fixture.detectChanges();

    expect(component.activeTab()).toBe('rebalancing');
    expect(component.pendingSuggestionsCount()).toBe(1);
    expect(component.feedback()?.text).toContain('Se generó 1 sugerencia');
  });

  it('la pestaña rebalancing en red monobodega explica la causa y ofrece botón para volver al inventario', () => {
    const { component, fixture } = setup({ suggestions: [] });

    component.activeTab.set('rebalancing');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('No hay traslados pendientes de revisión');
    expect(text).toContain('La red opera actualmente con 1 sola bodega activa (BOD-SUR)');
    expect(text).toContain('Ver Inventario de Bodega');
  });
});
