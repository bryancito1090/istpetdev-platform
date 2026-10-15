import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonBackButton,
  IonContent,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardSubtitle,
  IonCardContent,
  IonItem,
  IonLabel,
  IonInput,
  IonButton
} from '@ionic/angular/standalone';
import { FieldOutboxService } from '../../outbox/field-outbox.service';
import { MobileAuthService } from '../../../auth/auth.service';
import { QrScannerComponent } from '../../../shared/qr-scanner/qr-scanner.component';
import { extractUnitId, parseUnitQr } from '../../unit-qr';
import { HandlingUnitResponse } from '@istpetdev/shared-core';

@Component({
  selector: 'app-scan',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonButtons,
    IonBackButton,
    IonContent,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardSubtitle,
    IonCardContent,
    IonItem,
    IonLabel,
    IonInput,
    IonButton,
    QrScannerComponent
  ],
  template: `
    <ion-header>
      <ion-toolbar color="primary">
        <ion-buttons slot="start">
          <ion-back-button defaultHref="/home"></ion-back-button>
        </ion-buttons>
        <ion-title>Escanear Unidad (QR)</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <!-- Visor de cámara en vivo con BarcodeDetector y fallback a jsQR -->
      <div class="mb-4">
        <app-qr-scanner (scanned)="onQrScanned($event)"></app-qr-scanner>
      </div>

      <ion-card class="m-0 shadow-sm border border-slate-200">
        <ion-card-header>
          <ion-card-title class="text-base font-semibold text-slate-800">
            Ingreso o Validación Manual
          </ion-card-title>
          <ion-card-subtitle class="text-xs text-slate-500">
            Pegue la URL completa del QR o el identificador del bulto
          </ion-card-subtitle>
        </ion-card-header>

        <ion-card-content>
          <!-- Mensaje de error / advertencia de pertenencia a ruta -->
          @if (errorMessage()) {
            <div class="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-xs text-red-700 font-medium">
              {{ errorMessage() }}
            </div>
          }

          <!-- Entrada manual -->
          <ion-item class="mb-3 rounded-lg border border-slate-200">
            <ion-label position="stacked">Código o URL de Unidad</ion-label>
            <ion-input
              placeholder="https://.../public-trace/... o UUID"
              [(ngModel)]="scannedCode"
              name="scannedCode"
              (keyup.enter)="validateAndProceed()">
            </ion-input>
          </ion-item>

          <ion-button
            expand="block"
            color="primary"
            (click)="validateAndProceed()"
            [disabled]="!scannedCode.trim()">
            Validar y Proceder
          </ion-button>
        </ion-card-content>
      </ion-card>
    </ion-content>
  `
})
export class ScanPage {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly outboxService = inject(FieldOutboxService);
  private readonly authService = inject(MobileAuthService);

  scannedCode = '';
  readonly errorMessage = signal<string | null>(null);

  onQrScanned(rawText: string): void {
    this.scannedCode = rawText;
    this.processCode(rawText);
  }

  validateAndProceed(): void {
    this.processCode(this.scannedCode);
  }

  private processCode(rawText: string): void {
    this.errorMessage.set(null);
    const text = rawText.trim();
    if (!text) return;

    // Determinar rol activo: query param ?mode= o authService.currentRole()
    const modeParam = this.route.snapshot.queryParamMap.get('mode')?.toLowerCase();
    const activeRole = this.authService.currentRole();

    const isReceptor = modeParam === 'receptor' || activeRole === 'Receptor';
    const isBodega = modeParam === 'bodega' || activeRole === 'Bodega';

    // 1. Rol Receptor: requiere { unitId, token } para validar custodia
    if (isReceptor) {
      const parsed = parseUnitQr(text);
      if (parsed) {
        this.router.navigate(['/home'], {
          state: { receiverQr: { unitId: parsed.unitId, token: parsed.token } }
        });
        return;
      }

      // Si ingresó manualmente solo el UUID o código de bulto
      const unitId = extractUnitId(text) || text;
      this.router.navigate(['/home'], {
        state: { receiverQr: { unitId, token: '' } }
      });
      return;
    }

    // 2. Rol Bodega: retorna con { unitId } para despacho o inventario
    if (isBodega) {
      const unitId = extractUnitId(text) || text;
      this.router.navigate(['/home'], {
        state: { warehouseQr: { unitId } }
      });
      return;
    }

    // 3. Rol Conductor: validar contra la asignación activa local (offline outbox)
    this.handleDriverScan(text);
  }

  private handleDriverScan(text: string): void {
    const assignment = this.outboxService.activeAssignment();
    if (!assignment || !assignment.routes || assignment.routes.length === 0) {
      this.errorMessage.set('Descarga tu ruta en Inicio');
      return;
    }

    const unitId = extractUnitId(text) || text.toLowerCase();
    const code = text.toLowerCase();
    let targetDeliveryId: string | null = null;

    for (const route of assignment.routes) {
      for (const del of route.deliveries) {
        if (del.deliveryId.toLowerCase() === code || del.deliveryId.toLowerCase() === unitId) {
          targetDeliveryId = del.deliveryId;
          break;
        }

        if (
          del.handlingUnits &&
          del.handlingUnits.some((hu: HandlingUnitResponse) =>
            hu.handlingUnitId.toLowerCase() === unitId ||
            hu.handlingUnitId.toLowerCase() === code ||
            hu.code.toLowerCase() === code
          )
        ) {
          targetDeliveryId = del.deliveryId;
          break;
        }
      }
      if (targetDeliveryId) break;
    }

    if (targetDeliveryId) {
      this.router.navigate(['/receipt', targetDeliveryId]);
    } else {
      this.errorMessage.set('Este bulto no pertenece a tu ruta');
    }
  }
}
