import { describe, expect, it, vi, beforeEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { signal } from '@angular/core';
import { ScanPage } from './scan.page';
import { FieldOutboxService } from '../../outbox/field-outbox.service';
import { MobileAuthService } from '../../../auth/auth.service';
import { FieldRole } from '../../../auth/auth.models';

describe('ScanPage (Issue #122)', () => {
  let component: ScanPage;
  let fixture: ComponentFixture<ScanPage>;
  let routerNavigateSpy: any;

  const mockActiveAssignment = signal<any>(null);
  const mockCurrentRole = signal<FieldRole | null>('Conductor');

  const mockRouter = {
    navigate: vi.fn()
  };

  const mockRoute = {
    snapshot: {
      queryParamMap: {
        get: vi.fn().mockReturnValue(null)
      }
    }
  };

  const mockOutboxService = {
    activeAssignment: mockActiveAssignment
  };

  const mockAuthService = {
    currentRole: mockCurrentRole
  };

  beforeEach(() => {
    vi.clearAllMocks();
    mockActiveAssignment.set(null);
    mockCurrentRole.set('Conductor');
    routerNavigateSpy = mockRouter.navigate;

    TestBed.configureTestingModule({
      imports: [ScanPage],
      providers: [
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockRoute },
        { provide: FieldOutboxService, useValue: mockOutboxService },
        { provide: MobileAuthService, useValue: mockAuthService }
      ]
    });

    fixture = TestBed.createComponent(ScanPage);
    component = fixture.componentInstance;
  });

  describe('Rol Receptor', () => {
    it('navega a /home con state { receiverQr: { unitId, token } } al escanear URL completa', () => {
      mockCurrentRole.set('Receptor');
      const qrUrl = 'https://istpetdev.doicela.dev/public-trace/3fa85f64-5717-4562-b3fc-2c963f66afa6#t=a1b2c3d4';

      component.onQrScanned(qrUrl);

      expect(routerNavigateSpy).toHaveBeenCalledWith(['/home'], {
        state: {
          receiverQr: {
            unitId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
            token: 'a1b2c3d4'
          }
        }
      });
    });

    it('soporta activación por query parameter ?mode=receptor', () => {
      mockCurrentRole.set('Conductor');
      mockRoute.snapshot.queryParamMap.get.mockReturnValueOnce('receptor');

      component.onQrScanned('https://istpetdev.doicela.dev/public-trace/3fa85f64-5717-4562-b3fc-2c963f66afa6#t=xyz123');

      expect(routerNavigateSpy).toHaveBeenCalledWith(['/home'], {
        state: {
          receiverQr: {
            unitId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
            token: 'xyz123'
          }
        }
      });
    });
  });

  describe('Rol Bodega', () => {
    it('navega a /home con state { warehouseQr: { unitId } }', () => {
      mockCurrentRole.set('Bodega');
      const qrUrl = 'https://istpetdev.doicela.dev/public-trace/3fa85f64-5717-4562-b3fc-2c963f66afa6#t=tok';

      component.onQrScanned(qrUrl);

      expect(routerNavigateSpy).toHaveBeenCalledWith(['/home'], {
        state: {
          warehouseQr: {
            unitId: '3fa85f64-5717-4562-b3fc-2c963f66afa6'
          }
        }
      });
    });
  });

  describe('Rol Conductor', () => {
    it('muestra "Descarga tu ruta en Inicio" si no existe asignación activa descargada', () => {
      mockCurrentRole.set('Conductor');
      mockActiveAssignment.set(null);

      component.onQrScanned('https://istpetdev.doicela.dev/public-trace/3fa85f64-5717-4562-b3fc-2c963f66afa6#t=tok');

      expect(component.errorMessage()).toBe('Descarga tu ruta en Inicio');
      expect(routerNavigateSpy).not.toHaveBeenCalled();
    });

    it('navega a /receipt/{deliveryId} si la unidad pertenece a la ruta activa', () => {
      mockCurrentRole.set('Conductor');
      mockActiveAssignment.set({
        routes: [
          {
            deliveries: [
              {
                deliveryId: 'del-uuid-001',
                handlingUnits: [
                  {
                    handlingUnitId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
                    code: 'HU-101'
                  }
                ]
              }
            ]
          }
        ]
      });

      component.onQrScanned('https://istpetdev.doicela.dev/public-trace/3fa85f64-5717-4562-b3fc-2c963f66afa6#t=tok');

      expect(routerNavigateSpy).toHaveBeenCalledWith(['/receipt', 'del-uuid-001']);
      expect(component.errorMessage()).toBeNull();
    });

    it('muestra "Este bulto no pertenece a tu ruta" si la unidad no coincide con las paradas', () => {
      mockCurrentRole.set('Conductor');
      mockActiveAssignment.set({
        routes: [
          {
            deliveries: [
              {
                deliveryId: 'del-uuid-001',
                handlingUnits: [
                  {
                    handlingUnitId: 'otro-uuid',
                    code: 'HU-999'
                  }
                ]
              }
            ]
          }
        ]
      });

      component.onQrScanned('https://istpetdev.doicela.dev/public-trace/3fa85f64-5717-4562-b3fc-2c963f66afa6#t=tok');

      expect(component.errorMessage()).toBe('Este bulto no pertenece a tu ruta');
      expect(routerNavigateSpy).not.toHaveBeenCalled();
    });
  });
});
