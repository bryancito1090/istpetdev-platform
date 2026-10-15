import { describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { QrScannerComponent } from './qr-scanner.component';

describe('QrScannerComponent', () => {
  let component: QrScannerComponent;
  let fixture: ComponentFixture<QrScannerComponent>;

  it('se inicializa en un entorno de navegador o prueba', () => {
    fixture = TestBed.createComponent(QrScannerComponent);
    component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  it('emite el evento scanned y detiene la cámara al leer un código', () => {
    fixture = TestBed.createComponent(QrScannerComponent);
    component = fixture.componentInstance;

    let scannedValue = '';
    component.scanned.subscribe((val) => {
      scannedValue = val;
    });

    const stopSpy = vi.spyOn(component, 'stopCamera');

    (component as any).emitSuccessfulScan('https://istpetdev.doicela.dev/public-trace/3fa85f64-5717-4562-b3fc-2c963f66afa6#t=token123');

    expect(scannedValue).toBe('https://istpetdev.doicela.dev/public-trace/3fa85f64-5717-4562-b3fc-2c963f66afa6#t=token123');
    expect(stopSpy).toHaveBeenCalled();
  });

  it('detiene la cámara en ngOnDestroy', () => {
    fixture = TestBed.createComponent(QrScannerComponent);
    component = fixture.componentInstance;

    const stopSpy = vi.spyOn(component, 'stopCamera');
    component.ngOnDestroy();

    expect(stopSpy).toHaveBeenCalled();
  });
});
