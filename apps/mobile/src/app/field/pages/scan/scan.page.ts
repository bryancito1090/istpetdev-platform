import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  IonContent,
  AlertController
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
    IonContent,
    QrScannerComponent
  ],
  template: `
    <!-- Barra Superior: Entorno de Prueba (Dark Cyan Bar) -->
    <div class="stripe-top-banner">
      <div class="stripe-banner-left">
        <span class="banner-title">Entorno de prueba</span>
        <button type="button" class="info-bubble-btn" (click)="openTestEnvInfo()" aria-label="Información de entorno">
          <svg class="w-4 h-4" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </button>
      </div>

      <button type="button" class="stripe-verify-pill" (click)="openVerifyModal()">
        Verificar tu empresa
      </button>
    </div>

    <!-- Contenedor Principal con Bordes Redondeados Superiores -->
    <div class="stripe-header-card">
      <div class="stripe-nav-header">
        <button type="button" class="stripe-nav-btn" aria-label="Volver" (click)="goHome()">
          <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.3">
            <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
          </svg>
        </button>

        <h1 class="stripe-user-title">
          {{ isPickupMode() ? 'Escanear Cajas' : 'Escanear Código QR' }}
        </h1>

        <div class="stripe-header-actions">
          <button type="button" class="stripe-nav-btn relative" aria-label="Cola de sincronización" (click)="openSyncQueue()">
            <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8">
              <path stroke-linecap="round" stroke-linejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
            </svg>
            @if (pendingCount() > 0) {
              <span class="stripe-bell-badge">{{ pendingCount() }}</span>
            }
          </button>
        </div>
      </div>
    </div>

    <!-- Contenido Scrollable con Tarjetas Estilo Stripe -->
    <ion-content class="ion-padding stripe-main-content">
      <!-- Visor de cámara en vivo -->
      <div class="mb-4 overflow-hidden rounded-2xl border border-slate-200 shadow-sm bg-black">
        <app-qr-scanner (scanned)="onQrScanned($event)"></app-qr-scanner>
      </div>

      <!-- Tarjeta de Bulto Escaneado -->
      @if (scannedBoxInfo(); as info) {
        <div class="driver-stop-card mb-4 animate-fadeIn">
          <div class="flex items-center justify-between mb-2">
            <span class="stop-tag">
              Bulto: {{ info.boxCode }} · Parada #{{ info.sequence }}
            </span>
            <span class="status-badge is-done">
              En Ruta
            </span>
          </div>

          <h2 class="stop-title">
            {{ info.pointName }}
          </h2>
          @if (info.address) {
            <p class="stop-address">{{ info.address }}</p>
          }

          <!-- Productos a entregar -->
          <div class="stop-products-section">
            <span class="stop-products-title">
              Productos a entregar en este punto:
            </span>
            @if (info.products.length > 0) {
              <div class="space-y-1.5">
                @for (prod of info.products; track prod.name) {
                  <div class="product-row">
                    <span class="product-name">{{ prod.name }}</span>
                    <span class="product-qty">{{ prod.quantity }} {{ prod.unit }}</span>
                  </div>
                }
              </div>
            } @else {
              <p class="text-xs text-slate-500 italic">Bulto verificado para entrega</p>
            }
          </div>

          <div class="flex items-center gap-2 mt-4 pt-1">
            <button
              type="button"
              class="flex-1 py-2.5 px-3 text-xs font-bold text-slate-700 bg-slate-100 hover:bg-slate-200 rounded-xl transition"
              (click)="clearScannedBox()">
              Escanear otra
            </button>
            <button
              type="button"
              class="flex-1 stop-action-btn"
              (click)="goToDelivery(info.deliveryId)">
              Ver entrega →
            </button>
          </div>
        </div>
      }

      <!-- Validación Manual -->
      <div class="driver-stop-card mb-4">
        <div class="flex items-center justify-between mb-1.5">
          <span class="stop-tag">Validación Manual</span>
        </div>
        <h2 class="stop-title">Ingreso por Código o URL</h2>
        <p class="stop-address">Pegue la URL completa del QR o el código identificador del bulto.</p>

        @if (errorMessage()) {
          <div class="mb-3 p-3 bg-rose-50 border border-rose-200 rounded-xl text-xs text-rose-700 font-medium">
            {{ errorMessage() }}
          </div>
        }

        <div class="space-y-3">
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">Código o URL de Unidad</label>
            <input
              type="text"
              placeholder="https://.../public-trace/... o UUID"
              [(ngModel)]="scannedCode"
              name="scannedCode"
              (keyup.enter)="validateAndProceed()"
              class="w-full px-3 py-2.5 text-xs bg-slate-50 border border-slate-200 rounded-xl text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-[#00A7C4] focus:bg-white transition" />
          </div>

          <button
            type="button"
            class="stop-action-btn"
            (click)="validateAndProceed()"
            [disabled]="!scannedCode.trim()">
            Validar y Proceder
          </button>
        </div>
      </div>
    </ion-content>
  `,
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      background-color: #04303B;
    }

    .stripe-top-banner {
      background: #04303B;
      color: #ffffff;
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 12px 16px 14px;
      min-height: 48px;
    }

    .stripe-banner-left {
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .banner-title {
      font-size: 15px;
      font-weight: 700;
      letter-spacing: -0.01em;
      color: #ffffff;
    }

    .info-bubble-btn {
      background: transparent;
      border: none;
      color: rgba(255, 255, 255, 0.85);
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2px;
    }

    .stripe-verify-pill {
      background: #00A7C4;
      color: #ffffff;
      border: none;
      border-radius: 9999px;
      padding: 6px 16px;
      font-size: 12.5px;
      font-weight: 700;
      letter-spacing: -0.01em;
      cursor: pointer;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.2);
      transition: background-color 0.2s, transform 0.1s;
    }

    .stripe-verify-pill:hover {
      background: #008CA5;
    }

    .stripe-verify-pill:active {
      transform: scale(0.97);
    }

    .stripe-header-card {
      background: #ffffff;
      border-top-left-radius: 20px;
      border-top-right-radius: 20px;
      padding-top: 14px;
      box-shadow: 0 -2px 10px rgba(0, 0, 0, 0.05);
      z-index: 20;
    }

    .stripe-nav-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 14px 10px;
    }

    .stripe-user-title {
      margin: 0;
      font-size: 17px;
      font-weight: 750;
      color: #0A2540;
      letter-spacing: -0.02em;
      flex: 1;
      margin-left: 8px;
    }

    .stripe-nav-btn {
      background: transparent;
      border: none;
      color: #0A2540;
      cursor: pointer;
      padding: 6px;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: background 0.15s;
    }

    .stripe-nav-btn:hover {
      background: #f1f5f9;
    }

    .stripe-header-actions {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .stripe-bell-badge {
      position: absolute;
      top: 2px;
      right: 2px;
      background: #EA601F;
      color: white;
      font-size: 9px;
      font-weight: 750;
      border-radius: 9999px;
      min-width: 16px;
      height: 16px;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 0 4px;
    }

    .stripe-main-content {
      --background: #F8FAFC;
      flex: 1;
    }

    .driver-stop-card {
      background: #ffffff;
      border: 1px solid #E3E8EF;
      border-radius: 14px;
      padding: 16px;
      box-shadow: 0 1px 3px rgba(10, 37, 64, 0.05);
      transition: all 0.15s ease-in-out;
    }

    .stop-tag {
      font-size: 11px;
      font-weight: 750;
      color: #64748B;
      text-transform: uppercase;
      letter-spacing: 0.03em;
    }

    .status-badge {
      font-size: 11px;
      font-weight: 700;
      padding: 3px 8px;
      border-radius: 9999px;
      background: #F1F5F9;
      color: #475569;
    }

    .status-badge.is-done {
      background: #DCFCE7;
      color: #166534;
    }

    .stop-title {
      font-size: 16px;
      font-weight: 750;
      color: #0A2540;
      margin: 0 0 4px;
      line-height: 1.25;
    }

    .stop-address {
      font-size: 12px;
      color: #64748B;
      margin: 0 0 12px;
      line-height: 1.35;
    }

    .stop-products-section {
      padding: 10px 0;
      border-top: 1px solid #F1F5F9;
      border-bottom: 1px solid #F1F5F9;
      margin-bottom: 12px;
    }

    .stop-products-title {
      display: block;
      font-size: 11px;
      font-weight: 750;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: #94A3B8;
      margin-bottom: 6px;
    }

    .product-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      font-size: 13px;
      padding: 3px 0;
    }

    .product-name {
      color: #1E293B;
      font-weight: 600;
      flex: 1;
    }

    .product-qty {
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
      font-weight: 750;
      color: #0A2540;
      background: #F8FAFC;
      border: 1px solid #E2E8F0;
      padding: 2px 8px;
      border-radius: 6px;
      font-size: 12px;
      white-space: nowrap;
    }

    .stop-action-btn {
      width: 100%;
      padding: 11px 16px;
      background: #00A7C4;
      color: #ffffff;
      border: none;
      border-radius: 10px;
      font-size: 13px;
      font-weight: 750;
      cursor: pointer;
      transition: background-color 0.15s;
    }

    .stop-action-btn:hover {
      background: #008CA5;
    }

    .stop-action-btn:disabled {
      background: #E2E8F0;
      color: #94A3B8;
      cursor: not-allowed;
    }
  `]
})
export class ScanPage implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly outboxService = inject(FieldOutboxService);
  private readonly authService = inject(MobileAuthService);
  private readonly alerts = inject(AlertController, { optional: true });

  scannedCode = '';
  readonly errorMessage = signal<string | null>(null);
  readonly scannedBoxInfo = signal<ScannedBoxInfo | null>(null);
  readonly isPickupMode = signal(false);

  readonly pendingCount = computed(() => {
    return typeof this.outboxService.pendingCount === 'function' ? this.outboxService.pendingCount() : 0;
  });

  goHome(): void {
    this.router.navigate(['/home']);
  }

  openSyncQueue(): void {
    this.router.navigate(['/sync']);
  }

  async openTestEnvInfo(): Promise<void> {
    if (!this.alerts) return;
    const alert = await this.alerts.create({
      header: 'Entorno de prueba activo',
      subHeader: 'Expo Clean Ecuador 2026',
      message: 'Estás operando en el modo seguro de validación de campo. Las operaciones offline se concilian mediante firma y código QR trazable.',
      buttons: ['Entendido']
    });
    await alert.present();
  }

  async openVerifyModal(): Promise<void> {
    if (!this.alerts) return;
    const alert = await this.alerts.create({
      header: 'Verificación de Empresa',
      subHeader: 'Certificación de Custodia Digital',
      message: 'Empresa: IstpetDev Operaciones Nacionales\nRUC: 1793148291001\nEstado: Validación en proceso (45%)\nProtocolo de custodia digital activo con claves M2M.',
      buttons: ['Aceptar']
    });
    await alert.present();
  }

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
