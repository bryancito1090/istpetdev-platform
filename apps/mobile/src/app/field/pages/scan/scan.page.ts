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

export interface ScannedBoxProduct {
  name: string;
  quantity: number;
  unit: string;
}

export interface ScannedBoxInfo {
  boxCode: string;
  pointName: string;
  address: string;
  deliveryId: string;
  sequence: number;
  products: ScannedBoxProduct[];
}

function formatProductName(codeOrName: string): string {
  if (!codeOrName) return 'Producto';
  const upper = codeOrName.toUpperCase();
  if (upper.includes('PINOK') || upper.includes('DESINF')) return 'Pinoklin Desinfectante 5L';
  if (upper.includes('AMOX')) return 'Amoxicilina 500mg';
  if (upper.includes('ALCOHOL') || upper.includes('ALC')) return 'Alcohol 70% 1L';
  if (upper.includes('SUERO') || upper.includes('FISIO')) return 'Suero Fisiológico 500ml';
  return codeOrName;
}

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
        <ion-title>{{ isPickupMode() ? 'Recogida: Escanear Cajas' : 'Escanear Unidad (QR)' }}</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <!-- Visor de cámara en vivo con BarcodeDetector y fallback a jsQR -->
      <div class="mb-4">
        <app-qr-scanner (scanned)="onQrScanned($event)"></app-qr-scanner>
      </div>

      <!-- Tarjeta limpia sin cajas anidadas con nombre del lugar y productos identificados -->
      @if (scannedBoxInfo(); as info) {
        <div class="mb-4 bg-white border border-slate-200 rounded-xl p-4 shadow-sm">
          <div class="flex items-center justify-between mb-2">
            <span class="text-xs font-bold text-amber-800 uppercase tracking-wide">
              Bulto: {{ info.boxCode }} · Parada #{{ info.sequence }}
            </span>
            <span class="text-[11px] font-semibold px-2 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-200">
              En Ruta
            </span>
          </div>

          <!-- Nombre prominente del lugar de entrega -->
          <h2 class="text-lg font-bold text-slate-900 leading-tight mb-1">
            {{ info.pointName }}
          </h2>
          @if (info.address) {
            <p class="text-xs text-slate-500 mb-3">{{ info.address }}</p>
          }

          <!-- Productos a dejar -->
          <div class="border-t border-slate-100 pt-2.5 mt-2">
            <span class="text-xs font-bold text-slate-700 block mb-2 uppercase tracking-wider">
              Productos a dejar en este punto:
            </span>
            @if (info.products.length > 0) {
              <div class="space-y-1.5">
                @for (prod of info.products; track prod.name) {
                  <div class="flex items-center justify-between text-xs py-1 border-b border-slate-50">
                    <span class="font-medium text-slate-800">{{ prod.name }}</span>
                    <span class="font-bold text-slate-900 bg-slate-100 px-2 py-0.5 rounded">{{ prod.quantity }} {{ prod.unit }}</span>
                  </div>
                }
              </div>
            } @else {
              <p class="text-xs text-slate-500 italic">Bulto verificado para entrega</p>
            }
          </div>

          <div class="flex items-center gap-2 mt-4 pt-3 border-t border-slate-100">
            <ion-button expand="block" fill="outline" color="medium" class="flex-1" (click)="clearScannedBox()">
              Escanear otra
            </ion-button>
            <ion-button expand="block" color="primary" class="flex-1" (click)="goToDelivery(info.deliveryId)">
              Ver entrega
            </ion-button>
          </div>
        </div>
      }

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
export class ScanPage implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly outboxService = inject(FieldOutboxService);
  private readonly authService = inject(MobileAuthService);

  scannedCode = '';
  readonly errorMessage = signal<string | null>(null);
  readonly scannedBoxInfo = signal<ScannedBoxInfo | null>(null);
  readonly isPickupMode = signal(false);

  ngOnInit(): void {
    const mode = this.route.snapshot.queryParamMap.get('mode')?.toLowerCase();
    this.isPickupMode.set(mode === 'pickup');
  }

  clearScannedBox(): void {
    this.scannedBoxInfo.set(null);
    this.scannedCode = '';
  }

  goToDelivery(deliveryId: string): void {
    this.router.navigate(['/receipt', deliveryId]);
  }

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
    const isPickup = modeParam === 'pickup';

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
    this.handleDriverScan(text, isPickup);
  }

  private handleDriverScan(text: string, isPickup = false): void {
    const assignment = this.outboxService.activeAssignment();
    if (!assignment || !assignment.routes || assignment.routes.length === 0) {
      this.errorMessage.set('Descarga tu ruta en Inicio');
      return;
    }

    const traceMatch = text.match(/public-trace\/([^#/?]+)/i);
    const traceUnitId = traceMatch ? traceMatch[1].toLowerCase() : null;
    const unitId = extractUnitId(text) || traceUnitId || text.toLowerCase();
    const code = text.toLowerCase();
    let targetDelivery: any = null;
    let matchedHu: HandlingUnitResponse | null = null;

    for (const route of assignment.routes) {
      for (const del of route.deliveries) {
        if (del.deliveryId.toLowerCase() === code || del.deliveryId.toLowerCase() === unitId || (traceUnitId && del.deliveryId.toLowerCase() === traceUnitId)) {
          targetDelivery = del;
          if (del.handlingUnits && del.handlingUnits.length > 0) {
            matchedHu = del.handlingUnits[0];
          }
          break;
        }

        if (del.handlingUnits) {
          const matched = del.handlingUnits.find((hu: HandlingUnitResponse) =>
            hu.handlingUnitId.toLowerCase() === unitId ||
            hu.handlingUnitId.toLowerCase() === code ||
            hu.code.toLowerCase() === code ||
            (traceUnitId && (hu.handlingUnitId.toLowerCase() === traceUnitId || hu.code.toLowerCase() === traceUnitId))
          );
          if (matched) {
            targetDelivery = del;
            matchedHu = matched;
            break;
          }
        }
      }
      if (targetDelivery) break;
    }

    if (targetDelivery) {
      if (matchedHu && typeof sessionStorage !== 'undefined') {
        sessionStorage.setItem('lastScannedUnitId', matchedHu.handlingUnitId);
      }

      if (isPickup) {
        const huLines = matchedHu?.lines || [];
        const products: ScannedBoxProduct[] = huLines.map((l: any) => ({
          name: formatProductName(l.productName || l.sku),
          quantity: l.quantity,
          unit: l.unit
        }));

        if (products.length === 0 && targetDelivery.handlingUnits) {
          const allLines = targetDelivery.handlingUnits.flatMap((h: any) => h.lines || []);
          for (const l of allLines) {
            products.push({
              name: formatProductName(l.productName || l.sku),
              quantity: l.quantity,
              unit: l.unit
            });
          }
        }

        this.scannedBoxInfo.set({
          boxCode: matchedHu?.code || code.toUpperCase(),
          pointName: targetDelivery.pointName,
          address: targetDelivery.address,
          deliveryId: targetDelivery.deliveryId,
          sequence: targetDelivery.sequence,
          products
        });
      } else {
        this.router.navigate(['/receipt', targetDelivery.deliveryId]);
      }
    } else {
      this.errorMessage.set('Este bulto no pertenece a tu ruta');
    }
  }
}
