import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { ReportesPage } from './reportes.page';

describe('ReportesPage', () => {
  const mockSession = {
    me: () => ({ displayName: 'Bryan Baño' }),
    role: () => 'Gerencia Operativa',
    status: () => 'authenticated'
  };

  function setup() {
    TestBed.configureTestingModule({
      imports: [ReportesPage],
      providers: [
        provideRouter([]),
        { provide: AuthSession, useValue: mockSession }
      ]
    });
    const fixture = TestBed.createComponent(ReportesPage);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance };
  }

  it('se inicializa correctamente con métricas y títulos ejecutivos', () => {
    const { fixture, component } = setup();
    expect(component).toBeTruthy();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Reportes & Analítica Logística');
    expect(text).toContain('Cumplimiento SLA (OTIF)');
    expect(text).toContain('Red Abastecida');
    expect(text).toContain('Costo Transporte / Km');
    expect(text).toContain('Cadena de Frío');
  });

  it('filtra los despachos al seleccionar una zona geográfica', () => {
    const { component } = setup();
    expect(component.filteredDispatches().length).toBe(8);

    component.selectedZone.set('NORTE');
    expect(component.filteredDispatches().every(d => d.zone === 'NORTE')).toBe(true);
    expect(component.filteredDispatches().length).toBe(2);
  });

  it('filtra despachos por búsqueda de texto reactiva', () => {
    const { component } = setup();
    component.searchQuery.set('Calderón');
    expect(component.filteredDispatches().length).toBe(1);
    expect(component.filteredDispatches()[0].destination).toContain('Calderón');
  });

  it('permite cambiar entre pestañas del tablero', () => {
    const { fixture, component } = setup();
    expect(component.activeTab()).toBe('consolidated');

    component.activeTab.set('fleet');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Rendimiento Operativo de Vehículos');

    component.activeTab.set('inventory');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Auditoría de Cobertura y Riesgo Hospitalario');

    component.activeTab.set('returns');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Control de Logística Inversa y Devoluciones');
  });

  it('ejecuta exportCsv y exportJson sin excepciones', () => {
    const { component } = setup();
    const createObjectURLMock = vi.fn().mockReturnValue('blob:mock-url');
    const revokeObjectURLMock = vi.fn();
    window.URL.createObjectURL = createObjectURLMock;
    window.URL.revokeObjectURL = revokeObjectURLMock;

    expect(() => component.exportCsv()).not.toThrow();
    expect(() => component.exportJson()).not.toThrow();
    expect(createObjectURLMock).toHaveBeenCalledTimes(2);
  });
});
