import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '@shared/api';
import { PublicTraceResponseDto } from '@istpetdev/shared-core';
import { PublicTracePage } from './public-trace.page';

describe('PublicTracePage (Spec 004 & ADR-10)', () => {
  const sampleTrace: PublicTraceResponseDto = {
    unitId: 'CAJA-001',
    status: 'Delivered',
    events: [
      { stage: 'Sealed', timestamp: '2026-10-14T10:00:00Z' },
      { stage: 'Dispatched', timestamp: '2026-10-14T11:00:00Z' },
      { stage: 'Delivered', timestamp: '2026-10-14T14:30:00Z' }
    ],
    productUnits: [
      { sku: 'QUIM-L-01', unit: '20 L' },
      { sku: 'DESINF-KG-02', unit: '10 kg' }
    ]
  };

  it('muestra el formulario de consulta pública con campos requeridos', () => {
    TestBed.configureTestingModule({
      imports: [PublicTracePage],
      providers: [
        {
          provide: ApiService,
          useValue: {
            getPublicTrace: vi.fn(() => of(sampleTrace))
          }
        },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: { get: () => null },
              queryParamMap: { get: () => null }
            }
          }
        }
      ]
    });

    const fixture = TestBed.createComponent(PublicTracePage);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Consulta Pública de Custodia de Unidades (HU)');
    expect(fixture.nativeElement.textContent).toContain('Código o ID de Unidad de Manipulación (HU)');
    expect(fixture.nativeElement.textContent).toContain('Token Secreto de Traza');
  });

  it('renderiza el detalle de trazabilidad cuando se obtiene respuesta exitosa', () => {
    const apiMock = {
      getPublicTrace: vi.fn(() => of(sampleTrace))
    };

    TestBed.configureTestingModule({
      imports: [PublicTracePage],
      providers: [
        { provide: ApiService, useValue: apiMock },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: { get: () => 'CAJA-001' },
              queryParamMap: { get: () => null }
            }
          }
        }
      ]
    });

    const fixture = TestBed.createComponent(PublicTracePage);
    const component = fixture.componentInstance;
    component.unitId = 'CAJA-001';
    component.traceToken = 'secret-hash-token';
    component.submitSearch();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('CAJA-001');
    expect(fixture.nativeElement.textContent).toContain('Verificación de Custodia Oficial');
    expect(fixture.nativeElement.textContent).toContain('Delivered');
    expect(fixture.nativeElement.textContent).toContain('QUIM-L-01');
  });

  it('muestra mensaje de error cuando la API retorna 404 o token inválido', () => {
    const apiMock = {
      getPublicTrace: vi.fn(() => throwError(() => ({ status: 404 })))
    };

    TestBed.configureTestingModule({
      imports: [PublicTracePage],
      providers: [
        { provide: ApiService, useValue: apiMock },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: { get: () => null },
              queryParamMap: { get: () => null }
            }
          }
        }
      ]
    });

    const fixture = TestBed.createComponent(PublicTracePage);
    const component = fixture.componentInstance;
    component.unitId = 'INVALIDA';
    component.traceToken = 'token-falso';
    component.submitSearch();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Unidad no encontrada');
    expect(fixture.nativeElement.textContent).toContain('El token de traza no es válido');
  });
});
