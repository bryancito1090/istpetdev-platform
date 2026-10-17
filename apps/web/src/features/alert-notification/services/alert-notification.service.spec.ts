import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { ApiService, RiskItemDto } from '@shared/api';
import { AuthSession } from '@entities/session';
import { AlertNotificationService } from './alert-notification.service';

describe('AlertNotificationService', () => {
  let service: AlertNotificationService;
  let apiServiceMock: { getRisks: ReturnType<typeof vi.fn> };
  let sessionMock: { status: ReturnType<typeof vi.fn> };
  let routerMock: { navigate: ReturnType<typeof vi.fn> };

  const mockRisk1: RiskItemDto = {
    servicePointId: 'sp-1',
    servicePointCode: 'HOS-01',
    criticality: 'Alta',
    productId: 'prod-1',
    sku: 'PAR-500',
    baseUnit: 'u',
    availableQuantity: 10,
    inTransitQuantity: 0,
    dailyDemand: 10,
    coverageDays: 1.0,
    leadTimeDays: 2.0,
    safetyMarginDays: 3.0,
    suggestedQuantity: 50,
    level: 'Critico'
  };

  const mockRisk2: RiskItemDto = {
    servicePointId: 'sp-2',
    servicePointCode: 'HOS-02',
    criticality: 'Media',
    productId: 'prod-2',
    sku: 'IBU-400',
    baseUnit: 'u',
    availableQuantity: 5,
    inTransitQuantity: 0,
    dailyDemand: 5,
    coverageDays: 1.0,
    leadTimeDays: 2.0,
    safetyMarginDays: 3.0,
    suggestedQuantity: 40,
    level: 'Critico'
  };

  beforeEach(() => {
    vi.useFakeTimers();
    apiServiceMock = {
      getRisks: vi.fn(() => of({ items: [mockRisk1] }))
    };
    sessionMock = {
      status: vi.fn(() => 'authenticated')
    };
    routerMock = {
      navigate: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        AlertNotificationService,
        { provide: ApiService, useValue: apiServiceMock },
        { provide: AuthSession, useValue: sessionMock },
        { provide: Router, useValue: routerMock }
      ]
    });

    service = TestBed.inject(AlertNotificationService);
  });

  afterEach(() => {
    service.stopPolling();
    vi.useRealTimers();
  });

  it('inicializa y calcula los contadores de riesgo a partir de los ítems', () => {
    service.checkRisks();
    expect(service.activeRisks().length).toBe(1);
    expect(service.criticalRisks().length).toBe(1);
    expect(service.alertCount()).toBe(1);
    expect(service.hasAlerts()).toBe(true);
    // En la primera carga no debe emitir toasts automáticos
    expect(service.toasts().length).toBe(0);
  });

  it('emite un toast y pulso cuando se detecta un nuevo riesgo crítico en un sondeo posterior', () => {
    // 1. Primera carga
    service.checkRisks();
    expect(service.toasts().length).toBe(0);

    // 2. Segunda carga con un nuevo hospital en riesgo crítico
    apiServiceMock.getRisks.mockReturnValue(of({ items: [mockRisk1, mockRisk2] }));
    service.checkRisks();

    expect(service.toasts().length).toBe(1);
    expect(service.toasts()[0].servicePointCode).toBe('HOS-02');
    expect(service.toasts()[0].sku).toBe('IBU-400');
    expect(service.hasNewAlert()).toBe(true);
  });

  it('permite descartar manualmente un toast por su ID', () => {
    service.checkRisks();
    apiServiceMock.getRisks.mockReturnValue(of({ items: [mockRisk1, mockRisk2] }));
    service.checkRisks();

    expect(service.toasts().length).toBe(1);
    const toastId = service.toasts()[0].id;

    service.dismissToast(toastId);
    expect(service.toasts().length).toBe(0);
  });

  it('redirige a /alertas y descarta el toast al invocar goToAlerts', () => {
    service.checkRisks();
    apiServiceMock.getRisks.mockReturnValue(of({ items: [mockRisk1, mockRisk2] }));
    service.checkRisks();

    const toast = service.toasts()[0];
    service.goToAlerts(toast);

    expect(routerMock.navigate).toHaveBeenCalledWith(['/alertas']);
    expect(service.toasts().length).toBe(0);
  });
});
