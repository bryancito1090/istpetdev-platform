import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardSubtitle,
  IonCardContent,
  IonButton,
  IonBadge,
  IonButtons,
  IonInput,
  AlertController
} from '@ionic/angular/standalone';
import { CreateReceiptRequest } from '@istpetdev/shared-core';
import { FIELD_ROLE_LABELS, MobileAuthService } from '../auth';
import { FieldOutboxService } from '../field/outbox/field-outbox.service';
import { FIELD_API, IFieldApi, ReceiverUnit } from '../field/field-api.interface';
import { parseUnitQr } from '../field/unit-qr';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardSubtitle,
    IonCardContent,
    IonButton,
    IonBadge,
    IonButtons,
    IonInput
  ],
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar color="primary">
        <ion-title>IstpetDev Móvil</ion-title>
        <ion-buttons slot="end">
          <ion-button (click)="openSyncQueue()">
            Cola
            @if (pendingCount() > 0) {
              <ion-badge color="danger" class="ml-1">{{ pendingCount() }}</ion-badge>
            }
          </ion-button>
          <ion-button (click)="logout()">
            Salir
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true" class="ion-padding bg-slate-50">
      <!-- Insignia fija: Datos Simulados (F7 / Spec 004) -->
      <div class="mb-3 flex items-center justify-between p-2.5 bg-blue-50 border border-blue-200 rounded-lg text-xs text-blue-900 shadow-sm">
        <div class="flex items-center gap-1.5">
          <span class="w-2 h-2 rounded-full bg-blue-600 animate-ping"></span>
          <span class="font-bold">Datos simulados (Fase Frontend-First)</span>
        </div>
        <span class="text-[10px] bg-blue-200 text-blue-800 px-2 py-0.5 rounded font-mono font-bold">
          useMockApi
        </span>
      </div>

      <!-- Tarjeta de Perfil Activo -->
      <ion-card class="m-0 mb-4 border border-slate-200 shadow-sm bg-white">
        <ion-card-header class="pb-2">
          <div class="flex items-center justify-between">
            <span class="text-xs font-bold text-slate-500 uppercase tracking-wider">
              {{ roleSubtitle() }}
            </span>
            <ion-badge [color]="roleBadgeColor()">
              {{ roleEntityBadge() }}
            </ion-badge>
          </div>
          <ion-card-title class="text-lg text-slate-900 font-bold">
            {{ authService.me()?.displayName ?? '' }}
          </ion-card-title>
        </ion-card-header>
      </ion-card>

      <!-- ======================================================== -->
      <!-- CASO 1: RECEPTOR HOSPITALARIO (Solo Escanear QR y Novedades) -->
      <!-- ======================================================== -->
      @if (authService.currentRole() === 'Receptor') {
        <div class="space-y-4">
          <ion-card class="m-0 border border-emerald-200 bg-white shadow-sm overflow-hidden">
            <div class="p-3 bg-emerald-50 border-b border-emerald-100 flex items-center justify-between">
              <div>
                <h3 class="text-xs font-bold text-emerald-950 uppercase tracking-wide">
                  Recepción de Custodia en Hospital
                </h3>
                <p class="text-[11px] text-emerald-700">
                  Escanea el QR del bulto y reporta si llegó bien o si hubo novedades
                </p>
              </div>
            </div>

            <ion-card-content class="p-4 space-y-4">
              <!-- Botones de Escaneo y Entrada manual -->
              <ion-button expand="block" size="default" color="primary" class="font-bold" (click)="openReceiverScan()">
                Escanear QR con Cámara
              </ion-button>

              <div class="p-3 bg-slate-50 border border-slate-200 rounded-xl space-y-2">
                <label class="text-xs font-bold text-slate-700 block">
                  Pega o escanea el código del QR
                </label>
                <div class="flex gap-2">
                  <ion-input
                    [(ngModel)]="qrInputText"
                    placeholder="https://.../public-trace/...#t=..."
                    class="bg-white border border-slate-300 rounded-lg text-xs px-2 flex-1">
                  </ion-input>
                  <ion-button size="small" color="secondary" class="font-bold" (click)="searchReceiverUnit()" [disabled]="receiverLoading()">
                    Buscar bulto
                  </ion-button>
                </div>
              </div>

              @if (receiverLoading()) {
                <div class="p-3 text-center text-xs text-slate-500 font-medium">
                  Cargando información del bulto...
                </div>
              }

              @if (receiverError()) {
                <div class="p-3 bg-rose-50 border border-rose-300 rounded-xl text-xs text-rose-900 font-medium">
                  {{ receiverError() }}
                </div>
              }

              <!-- Ficha del Bulto Escaneado -->
              @if (receiverActiveBox(); as box) {
                <div class="p-3.5 bg-slate-50 border border-slate-200 rounded-xl space-y-2">
                  <div class="flex items-center justify-between border-b border-slate-200 pb-2">
                    <span class="text-[11px] font-mono font-bold text-teal-800 bg-teal-100 px-2 py-0.5 rounded">
                      {{ box.code }}
                    </span>
                    <span class="text-[11px] text-slate-500 font-medium">Destino: {{ box.destinationName }}</span>
                  </div>

                  <div class="text-xs space-y-2">
                    @for (line of box.lines; track line.deliveryLineId) {
                      <div class="p-2 bg-white border border-slate-200 rounded-lg space-y-1">
                        <span class="font-black text-slate-900 block text-xs">{{ line.productName }}</span>
                        <div class="grid grid-cols-2 gap-1 text-[11px] text-slate-600">
                          <div>Lote: <strong class="text-slate-800 font-mono">{{ line.lotCode }}</strong></div>
                          <div>Cantidad: <strong class="text-slate-800">{{ line.quantity }} {{ line.unit }}</strong></div>
                        </div>
                      </div>
                    }
                  </div>
                </div>

                <!-- Pregunta Clave: ¿Pasó algo malo al recibir? -->
                <div class="space-y-2">
                  <label class="text-xs font-black text-slate-800 block">
                    ¿En qué estado recibes este bulto?
                  </label>

                  <div class="grid grid-cols-2 gap-2">
                    <!-- Opción Todo Conforme -->
                    <button
                      type="button"
                      (click)="setReceiverStatus('conforme')"
                      [class.bg-emerald-600]="receiverStatus() === 'conforme'"
                      [class.text-white]="receiverStatus() === 'conforme'"
                      [class.border-emerald-600]="receiverStatus() === 'conforme'"
                      [class.bg-white]="receiverStatus() !== 'conforme'"
                      [class.text-emerald-900]="receiverStatus() !== 'conforme'"
                      [class.border-emerald-300]="receiverStatus() !== 'conforme'"
                      class="p-3 rounded-xl border text-left transition flex flex-col justify-between shadow-sm">
                      <svg class="w-5 h-5 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
                      </svg>
                      <div>
                        <div class="text-xs font-bold">Todo Conforme</div>
                        <div class="text-[10px] opacity-80">Sin daños ni faltantes</div>
                      </div>
                    </button>

                    <!-- Opción Pasó algo malo -->
                    <button
                      type="button"
                      (click)="setReceiverStatus('problema')"
                      [class.bg-rose-600]="receiverStatus() === 'problema'"
                      [class.text-white]="receiverStatus() === 'problema'"
                      [class.border-rose-600]="receiverStatus() === 'problema'"
                      [class.bg-white]="receiverStatus() !== 'problema'"
                      [class.text-rose-900]="receiverStatus() !== 'problema'"
                      [class.border-rose-300]="receiverStatus() !== 'problema'"
                      class="p-3 rounded-xl border text-left transition flex flex-col justify-between shadow-sm">
                      <svg class="w-5 h-5 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                      </svg>
                      <div>
                        <div class="text-xs font-bold">Pasó algo malo</div>
                        <div class="text-[10px] opacity-80">Reportar daño o faltante</div>
                      </div>
                    </button>
                  </div>
                </div>

                <!-- Desglose si pasó algo malo -->
                @if (receiverStatus() === 'problema') {
                  <div class="p-3.5 bg-rose-50 border border-rose-200 rounded-xl space-y-3">
                    <span class="text-xs font-black text-rose-900 block uppercase tracking-wide">
                      Selecciona la novedad encontrada:
                    </span>

                    <div class="flex flex-wrap gap-1.5">
                      @for (chip of receiverIssueChips; track chip.id) {
                        <button
                          type="button"
                          (click)="toggleReceiverIssue(chip.label)"
                          [class.bg-rose-700]="receiverSelectedIssues().includes(chip.label)"
                          [class.text-white]="receiverSelectedIssues().includes(chip.label)"
                          [class.bg-white]="!receiverSelectedIssues().includes(chip.label)"
                          [class.text-rose-900]="!receiverSelectedIssues().includes(chip.label)"
                          class="text-xs font-semibold px-2.5 py-1 rounded-lg border border-rose-300 transition shadow-sm">
                          {{ chip.label }}
                        </button>
                      }
                    </div>

                    <div class="space-y-1">
                      <label class="text-[11px] font-bold text-rose-900 block">
                        Detalle / Observaciones adicionales:
                      </label>
                      <textarea
                        [(ngModel)]="receiverIssueNotes"
                        rows="2"
                        placeholder="Ej: Empaque perforado por impacto, faltaron 3 galones según remisión..."
                        class="w-full text-xs p-2 rounded-lg border border-rose-300 bg-white text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-rose-500"></textarea>
                    </div>
                  </div>
                }

                <!-- Botón de Confirmación -->
                <div class="pt-1">
                  <ion-button
                    expand="block"
                    color="success"
                    class="font-black shadow-md"
                    [disabled]="receiverLoading() || box.status !== 'Dispatched'"
                    (click)="confirmReceiverAction()">
                    Confirmar Recepción de Insumo
                  </ion-button>
                </div>
              }

              <!-- Mensaje de Confirmación Exitosa -->
              @if (receiverConfirmedMessage()) {
                <div class="p-3 bg-emerald-50 border border-emerald-300 rounded-xl text-xs text-emerald-900 space-y-1">
                  <div class="font-bold flex items-center gap-1.5">
                    <svg class="w-4 h-4 text-emerald-700 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
                    </svg>
                    <span>Recepción Registrada Exitosamente</span>
                  </div>
                  <p class="text-[11px] leading-relaxed">{{ receiverConfirmedMessage() }}</p>
                </div>
              }
            </ion-card-content>
          </ion-card>
        </div>
      }

      <!-- ======================================================== -->
      <!-- CASO 2: OPERADOR DE BODEGA (Móvil)                       -->
      <!-- ======================================================== -->
      @else if (authService.currentRole() === 'Bodega') {
        <div class="space-y-4">
          <ion-card class="m-0 border border-sky-200 bg-white shadow-sm">
            <ion-card-header class="pb-2 bg-sky-50 border-b border-sky-100">
              <ion-card-title class="text-base text-sky-950 font-bold">
                Despacho y Preparación de Bultos
              </ion-card-title>
              <ion-card-subtitle class="text-xs text-sky-700">
                Punto Central: WH-CENTRAL-01
              </ion-card-subtitle>
            </ion-card-header>

            <ion-card-content class="pt-4 space-y-3">
              <p class="text-xs text-slate-600 leading-relaxed">
                Desde esta terminal móvil se realiza el escaneo de salida de bultos (HU) y la asignación a las unidades de transporte con custodia digital.
              </p>

              <div class="grid grid-cols-2 gap-2 pt-1">
                <ion-button expand="block" size="small" color="primary" class="font-bold" (click)="openScan()">
                  Escanear Salida QR
                </ion-button>
                <ion-button expand="block" size="small" fill="outline" color="primary" class="font-bold" (click)="openSyncQueue()">
                  Cola de Despacho
                </ion-button>
              </div>
            </ion-card-content>
          </ion-card>

          <div class="mb-2 flex items-center justify-between px-1">
            <span class="text-xs font-bold uppercase tracking-wider text-slate-600">
              Cargas Listas para Despachar
            </span>
            <span class="text-[11px] text-slate-500 font-bold">2 Lotes Pendientes</span>
          </div>

          <div class="space-y-2">
            <ion-card class="m-0 border shadow-sm">
              <ion-card-header class="pb-2">
                <div class="flex items-center justify-between">
                  <ion-badge color="primary">Vehículo VH-EC-101</ion-badge>
                  <span class="text-[11px] text-slate-500 font-mono">HU-2026-MED-042</span>
                </div>
                <ion-card-title class="text-sm font-bold">Ruta Hospitalaria Tungurahua</ion-card-title>
                <ion-card-subtitle class="text-xs">4 Bultos preparados • 1,200 Litros</ion-card-subtitle>
              </ion-card-header>
              <ion-card-content class="pt-0">
                <ion-button expand="block" size="small" fill="outline" (click)="openScan()">
                  Verificar y Cargar al Vehículo →
                </ion-button>
              </ion-card-content>
            </ion-card>
          </div>
        </div>
      }

      <!-- ======================================================== -->
      <!-- CASO 3: CONDUCTOR DE CUSTODIA (Móvil)                     -->
      <!-- ======================================================== -->
      @else {
        <!-- Banner de Aviso de Revisión de Ruta (PA-02 / FR-12 / F5) -->
        @if (outboxService.activeDriverNotice(); as notice) {
          <div class="mb-4 p-3.5 bg-amber-50 border-2 border-amber-400 text-amber-950 rounded-xl shadow-sm flex items-start gap-3">
            <svg class="w-6 h-6 text-amber-600 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <div class="flex-1">
              <div class="flex items-center justify-between mb-1">
                <span class="font-black text-xs uppercase tracking-wider text-amber-900">
                  Aviso de Revisión de Ruta (Rev {{ notice.revision }})
                </span>
                <span class="text-[10px] font-mono bg-amber-200/80 text-amber-900 px-1.5 py-0.5 rounded font-bold">
                  {{ notice.routePlanId }}
                </span>
              </div>
              <p class="text-xs font-medium text-amber-900 leading-relaxed">{{ notice.message }}</p>
              <div class="mt-2.5">
                <button
                  type="button"
                  (click)="outboxService.acknowledgeNotice(notice.revision)"
                  class="text-xs font-bold px-3 py-1.5 bg-amber-900 text-white rounded-lg active:bg-amber-950 transition shadow-sm">
                  Entendido / Reconocer Aviso
                </button>
              </div>
            </div>
          </div>
        }

        <!-- Acciones del Conductor -->
        <ion-card class="m-0 mb-4 border border-slate-200 shadow-sm">
          <ion-card-content class="p-3">
            <div class="flex gap-2">
              <ion-button expand="block" size="small" class="flex-1 font-bold" (click)="openScan()">
                Escanear QR
              </ion-button>
              <ion-button
                expand="block"
                size="small"
                fill="outline"
                class="flex-1 font-bold"
                [disabled]="loadingRoute()"
                (click)="downloadRoute()">
                {{ loadingRoute() ? 'Cargando...' : 'Descargar Ruta' }}
              </ion-button>
            </div>
          </ion-card-content>
        </ion-card>

        <!-- Mensaje si se descarga la ruta -->
        @if (downloadMessage()) {
          <div class="mb-4 p-3 bg-emerald-50 border border-emerald-200 rounded-lg text-xs text-emerald-800">
            {{ downloadMessage() }}
          </div>
        }

        <!-- Lista de Paradas Asignadas -->
        <div class="mb-2 flex items-center justify-between px-1">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-600">
            Paradas de la Ruta Activa
          </span>
          <span class="text-[11px] text-slate-500 font-bold">
            {{ totalDeliveries() }} entregas
          </span>
        </div>

        @if (outboxService.activeAssignment(); as assignment) {
          <div class="space-y-3">
            @for (route of assignment.routes; track route.routePlanId) {
              <div class="space-y-3">
                <div class="flex items-center justify-between text-xs text-slate-500 font-mono px-1">
                  <span>Ruta: {{ route.routePlanId }} (Rev {{ route.revision }})</span>
                  <span>Vehículo: {{ route.vehicleCode }}</span>
                </div>

                @for (del of route.deliveries; track del.deliveryId) {
                  <ion-card class="m-0 border shadow-sm" [class.border-amber-300]="del.kind === 'Pickup'">
                    <ion-card-header class="pb-2">
                      <div class="flex items-center justify-between">
                        <div class="flex items-center gap-1.5">
                          <ion-badge [color]="del.kind === 'Pickup' ? 'warning' : 'primary'">
                            #{{ del.sequence }} {{ del.kind === 'Pickup' ? '📦 Recogida' : '🏥 Entrega' }}
                          </ion-badge>
                          @if (del.kind === 'Pickup') {
                            <span class="text-[10px] bg-amber-100 text-amber-900 font-bold px-1.5 py-0.5 rounded">Bodega</span>
                          }
                        </div>
                        <span class="text-xs font-semibold text-slate-500">ID: {{ del.pointCode }}</span>
                      </div>
                      <ion-card-title class="text-base">{{ del.pointName }}</ion-card-title>
                      <ion-card-subtitle>{{ del.address }}</ion-card-subtitle>
                    </ion-card-header>

                    <ion-card-content class="pt-0 space-y-2">
                      <div class="p-2.5 rounded-lg border flex items-center justify-between"
                           [class.bg-amber-50]="del.kind === 'Pickup'"
                           [class.border-amber-200]="del.kind === 'Pickup'"
                           [class.bg-slate-50]="del.kind !== 'Pickup'"
                           [class.border-slate-200]="del.kind !== 'Pickup'">
                        <div>
                          <span class="text-xs font-bold block" [class.text-amber-950]="del.kind === 'Pickup'" [class.text-slate-800]="del.kind !== 'Pickup'">
                            {{ del.kind === 'Pickup' ? 'Carga / Retiro' : 'Entrega' }} {{ del.deliveryId }}
                          </span>
                          <span class="text-[11px] text-slate-500">
                            Bultos HU: {{ del.handlingUnits.length }} • Estado: {{ del.status }}
                          </span>
                        </div>

                        <ion-button
                          size="small"
                          [color]="del.kind === 'Pickup' ? 'warning' : 'primary'"
                          (click)="goToDelivery(del.deliveryId)">
                          {{ del.kind === 'Pickup' ? 'Cargar →' : 'Recibir →' }}
                        </ion-button>
                      </div>
                    </ion-card-content>
                  </ion-card>
                }
              </div>
            }
          </div>
        } @else {
          <ion-card class="m-0">
            <ion-card-content class="text-center py-8">
              <p class="text-sm text-slate-500">No hay ruta descargada en el dispositivo.</p>
              <ion-button class="mt-3" size="small" (click)="downloadRoute()">
                Descargar Ruta de Asignación
              </ion-button>
            </ion-card-content>
          </ion-card>
        }
      }
    </ion-content>
  `
})
export class HomePage implements OnInit {
  readonly authService = inject(MobileAuthService);
  readonly outboxService = inject(FieldOutboxService);
  private readonly router = inject(Router);
  private readonly alerts = inject(AlertController);

  readonly loadingRoute = signal<boolean>(false);
  readonly downloadMessage = signal<string | null>(null);

  private readonly fieldApi = inject<IFieldApi>(FIELD_API);

  // --- Estado para el Receptor Hospitalario ---
  qrInputText = '';
  readonly receiverActiveBox = signal<ReceiverUnit | null>(null);
  readonly receiverQr = signal<{ unitId: string; token: string } | null>(null);
  readonly receiverLoading = signal<boolean>(false);
  readonly receiverError = signal<string | null>(null);
  readonly receiverStatus = signal<'conforme' | 'problema'>('conforme');
  readonly receiverSelectedIssues = signal<string[]>([]);
  receiverIssueNotes = '';
  readonly receiverConfirmedMessage = signal<string | null>(null);

  readonly receiverIssueChips = [
    { id: 'damaged', label: 'Empaque roto o golpeado' },
    { id: 'seal_broken', label: 'Sello de seguridad roto / alterado' },
    { id: 'missing', label: 'Faltan galones / unidades' },
    { id: 'lot_mismatch', label: 'Lote o fecha no coincide' }
  ];

  readonly pendingCount = this.outboxService.pendingCount;

  readonly totalDeliveries = computed(() => {
    const a = this.outboxService.activeAssignment();
    if (!a || !a.routes) return 0;
    return a.routes.reduce((sum: number, r) => sum + r.deliveries.length, 0);
  });

  readonly roleSubtitle = computed(() => {
    const role = this.authService.currentRole();
    return role ? FIELD_ROLE_LABELS[role] : '';
  });

  readonly roleEntityBadge = computed(() => this.roleSubtitle());

  readonly roleBadgeColor = computed(() => {
    const role = this.authService.currentRole();
    if (role === 'Receptor') return 'success';
    if (role === 'Bodega') return 'tertiary';
    return 'primary';
  });

  async ngOnInit(): Promise<void> {
    // Si es conductor y no hay ruta en caché, intentar descargarla
    if (this.authService.currentRole() === 'Conductor' && !this.outboxService.activeAssignment()) {
      await this.downloadRoute();
    }

    const receiverQr = history.state?.receiverQr;
    if (receiverQr?.unitId && receiverQr?.token) {
      await this.loadReceiverUnit(receiverQr.unitId, receiverQr.token);
    }
  }

  openReceiverScan(): void {
    this.router.navigate(['/scan'], { queryParams: { mode: 'receiver' } });
  }

  async searchReceiverUnit(): Promise<void> {
    this.receiverError.set(null);
    this.receiverConfirmedMessage.set(null);
    const parsed = parseUnitQr(this.qrInputText);
    if (!parsed) {
      this.receiverError.set('QR no válido o vencido');
      this.receiverActiveBox.set(null);
      this.receiverQr.set(null);
      return;
    }
    await this.loadReceiverUnit(parsed.unitId, parsed.token);
  }

  async loadReceiverUnit(unitId: string, token: string): Promise<void> {
    this.receiverLoading.set(true);
    this.receiverError.set(null);
    this.receiverConfirmedMessage.set(null);
    this.receiverQr.set({ unitId, token });
    this.receiverStatus.set('conforme');
    this.receiverSelectedIssues.set([]);
    this.receiverIssueNotes = '';

    try {
      const unit = await firstValueFrom(this.fieldApi.getReceiverUnit(unitId, token));
      this.receiverActiveBox.set(unit);
      if (unit.status !== 'Dispatched') {
        this.receiverError.set('Este bulto ya fue recibido');
      }
    } catch (err: unknown) {
      this.receiverActiveBox.set(null);
      if (err instanceof HttpErrorResponse) {
        if (err.status === 404) {
          this.receiverError.set('QR no válido o vencido');
        } else if (err.status === 409) {
          this.receiverError.set('Este bulto ya fue recibido');
        } else if (err.status === 0 || (typeof navigator !== 'undefined' && !navigator.onLine)) {
          this.receiverError.set('Necesitas señal para confirmar');
        } else {
          this.receiverError.set(err.error?.detail || err.message || 'Error al cargar bulto');
        }
      } else if (typeof navigator !== 'undefined' && !navigator.onLine) {
        this.receiverError.set('Necesitas señal para confirmar');
      } else {
        this.receiverError.set('QR no válido o vencido');
      }
    } finally {
      this.receiverLoading.set(false);
    }
  }

  setReceiverStatus(status: 'conforme' | 'problema'): void {
    this.receiverStatus.set(status);
    if (status === 'conforme') {
      this.receiverSelectedIssues.set([]);
      this.receiverIssueNotes = '';
    }
  }

  toggleReceiverIssue(issueLabel: string): void {
    const current = this.receiverSelectedIssues();
    if (current.includes(issueLabel)) {
      this.receiverSelectedIssues.set(current.filter((i) => i !== issueLabel));
    } else {
      this.receiverSelectedIssues.set([...current, issueLabel]);
    }
  }

  async confirmReceiverAction(): Promise<void> {
    const box = this.receiverActiveBox();
    const qr = this.receiverQr();
    if (!box || !qr || box.status !== 'Dispatched') return;

    this.receiverLoading.set(true);
    this.receiverError.set(null);
    this.receiverConfirmedMessage.set(null);

    const ok = this.receiverStatus() === 'conforme';
    const req: CreateReceiptRequest = {
      operationId: crypto.randomUUID(),
      expectedDeliveryVersion: box.deliveryVersion,
      occurredAtDevice: new Date().toISOString(),
      lines: box.lines.map((l) => ({
        deliveryLineId: l.deliveryLineId,
        lotId: l.lotId,
        unit: l.unit,
        quantityAccepted: ok ? l.quantity : 0,
        quantityRejected: ok ? 0 : l.quantity,
        rejectionReason: ok ? undefined : this.receiverSelectedIssues().join(', ') || 'Novedad'
      }))
    };

    try {
      const receipt = await firstValueFrom(
        this.fieldApi.receiveAsReceiver(qr.unitId, qr.token, req, req.operationId)
      );
      this.receiverConfirmedMessage.set(
        `Recepción registrada: entrega ${receipt.deliveryStatus === 'Delivered' ? 'completa' : 'con novedades'}.`
      );
      this.receiverActiveBox.set({
        ...box,
        status: receipt.deliveryStatus === 'Delivered' ? 'Received' : 'Partial'
      });
    } catch (err: unknown) {
      if (err instanceof HttpErrorResponse) {
        if (err.status === 404) {
          this.receiverError.set('QR no válido o vencido');
        } else if (err.status === 409) {
          this.receiverError.set('Este bulto ya fue recibido');
        } else if (err.status === 0 || (typeof navigator !== 'undefined' && !navigator.onLine)) {
          this.receiverError.set('Necesitas señal para confirmar');
        } else {
          this.receiverError.set(err.error?.detail || err.message || 'Error al registrar recepción');
        }
      } else if (typeof navigator !== 'undefined' && !navigator.onLine) {
        this.receiverError.set('Necesitas señal para confirmar');
      } else {
        this.receiverError.set('Error al registrar recepción');
      }
    } finally {
      this.receiverLoading.set(false);
    }
  }

  async downloadRoute(): Promise<void> {
    this.loadingRoute.set(true);
    this.downloadMessage.set(null);
    try {
      const assignment = await this.outboxService.downloadRouteAssignment();
      const total = assignment.routes ? assignment.routes.reduce((sum: number, r) => sum + r.deliveries.length, 0) : 0;
      this.downloadMessage.set(
        `Ruta cargada con éxito: ${total} entrega(s) asignada(s) para el vehículo.`
      );
    } catch {
      this.downloadMessage.set('Aviso: Utilizando asignación almacenada localmente.');
    } finally {
      this.loadingRoute.set(false);
    }
  }

  openScan(): void {
    this.router.navigate(['/scan']);
  }

  openSyncQueue(): void {
    this.router.navigate(['/sync']);
  }

  goToDelivery(deliveryId: string): void {
    this.router.navigate(['/receipt', deliveryId]);
  }

  async logout(): Promise<void> {
    const pending = this.outboxService.pendingCount();
    if (pending > 0) {
      const alert = await this.alerts.create({
        header: 'Capturas sin sincronizar',
        message: `Tienes ${pending} ${pending === 1 ? 'captura pendiente' : 'capturas pendientes'}. Se guardarán en este teléfono para tu cuenta y se enviarán cuando vuelvas a entrar.`,
        buttons: [{ text: 'Cancelar', role: 'cancel' }, { text: 'Cerrar sesión', role: 'confirm' }],
      });
      await alert.present();
      if ((await alert.onDidDismiss()).role !== 'confirm') return;
    }
    await this.authService.logout();
  }
}
