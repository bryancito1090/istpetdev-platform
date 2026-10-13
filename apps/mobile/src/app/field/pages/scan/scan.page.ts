import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
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
  IonButton,
  IonBadge
} from '@ionic/angular/standalone';
import { FieldOutboxService } from '../../outbox/field-outbox.service';
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
    IonButton
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
      <!-- Indicador de datos simulados -->
      <div class="mb-4 flex items-center justify-between p-2.5 bg-amber-50 border border-amber-200 rounded-lg text-xs text-amber-800">
        <span class="font-medium">Modo Simulado Activo</span>
        <span class="text-[10px] bg-amber-200 text-amber-900 px-2 py-0.5 rounded font-mono">Fase 4 Demo</span>
      </div>

      <ion-card>
        <ion-card-header>
          <ion-card-title class="text-lg">Lectura de Custodia QR</ion-card-title>
          <ion-card-subtitle>Escanee o digite el código de la unidad de manipulación</ion-card-subtitle>
        </ion-card-header>
        <ion-card-content>
          <!-- Mensaje de error si la unidad no coincide -->
          @if (errorMessage()) {
            <div class="mb-4 p-3 bg-red-50 border border-red-200 rounded text-xs text-red-700">
              {{ errorMessage() }}
            </div>
          }

          <!-- Simulación de visor QR con soporte de escaneo rápido -->
          <div class="p-6 bg-slate-900 rounded-xl text-center text-white mb-4 relative overflow-hidden flex flex-col items-center justify-center min-h-[160px]">
            <div class="w-24 h-24 border-2 border-dashed border-emerald-400 rounded-lg flex items-center justify-center mb-2 animate-pulse">
              <span class="text-3xl">📷</span>
            </div>
            <p class="text-xs text-slate-300">Apunte la cámara a la etiqueta del paquete</p>
            <p class="text-[11px] text-slate-400 mt-1">O seleccione un código demo abajo</p>
          </div>

          <!-- Entrada manual -->
          <ion-item class="mb-4">
            <ion-label position="stacked">Código de Unidad (HU / Delivery)</ion-label>
            <ion-input
              placeholder="Ej: hu-001 o del-001"
              [(ngModel)]="scannedCode"
              name="scannedCode">
            </ion-input>
          </ion-item>

          <ion-button expand="block" (click)="validateAndProceed()" [disabled]="!scannedCode.trim()">
            Validar y Proceder a Recepción
          </ion-button>

          <!-- Atajos rápidos para demostración en navegador -->
          <div class="mt-6 pt-4 border-t border-slate-200">
            <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider block mb-2">
              Unidades Demo en Asignación Activa:
            </span>
            <div class="flex flex-col gap-2">
              <button
                type="button"
                (click)="selectQuickDemo('hu-001')"
                class="p-2.5 bg-slate-100 hover:bg-slate-200 rounded text-left flex items-center justify-between text-xs transition">
                <div>
                  <span class="font-bold text-slate-800 block">hu-001 (Parada 1)</span>
                  <span class="text-[11px] text-slate-500">Farmacia Central Quito — Insulina NPH</span>
                </div>
                <span class="text-primary-600 font-semibold text-xs">Seleccionar →</span>
              </button>

              <button
                type="button"
                (click)="selectQuickDemo('hu-002')"
                class="p-2.5 bg-slate-100 hover:bg-slate-200 rounded text-left flex items-center justify-between text-xs transition">
                <div>
                  <span class="font-bold text-slate-800 block">hu-002 (Parada 2)</span>
                  <span class="text-[11px] text-slate-500">Hospital San Rafael — Amoxicilina</span>
                </div>
                <span class="text-primary-600 font-semibold text-xs">Seleccionar →</span>
              </button>
            </div>
          </div>
        </ion-card-content>
      </ion-card>
    </ion-content>
  `
})
export class ScanPage {
  private readonly router = inject(Router);
  private readonly outboxService = inject(FieldOutboxService);

  scannedCode = '';
  readonly errorMessage = signal<string | null>(null);

  selectQuickDemo(code: string): void {
    this.scannedCode = code;
    this.validateAndProceed();
  }

  validateAndProceed(): void {
    this.errorMessage.set(null);
    const code = this.scannedCode.trim().toLowerCase();

    const assignment = this.outboxService.activeAssignment();
    if (!assignment || !assignment.routes) {
      // Si aún no se ha descargado asignación, resolver por mapeo de demo
      if (code === 'hu-001' || code === 'del-001') {
        this.router.navigate(['/receipt', 'del-001']);
        return;
      }
      if (code === 'hu-002' || code === 'del-002') {
        this.router.navigate(['/receipt', 'del-002']);
        return;
      }
      this.errorMessage.set('No hay ruta activa cargada. Por favor descargue la ruta en Inicio.');
      return;
    }

    // Buscar en las rutas si alguna entrega contiene la unidad o coincide con el deliveryId
    let targetDeliveryId: string | null = null;

    for (const route of assignment.routes) {
      for (const del of route.deliveries) {
        if (del.deliveryId.toLowerCase() === code) {
          targetDeliveryId = del.deliveryId;
          break;
        }
        if (del.handlingUnits && del.handlingUnits.some((hu: HandlingUnitResponse) => 
          hu.handlingUnitId.toLowerCase() === code || hu.code.toLowerCase() === code
        )) {
          targetDeliveryId = del.deliveryId;
          break;
        }
      }
      if (targetDeliveryId) break;
    }

    if (targetDeliveryId) {
      this.router.navigate(['/receipt', targetDeliveryId]);
    } else {
      this.errorMessage.set(`La unidad "${code}" no pertenece a la asignación activa de este vehículo.`);
    }
  }
}
