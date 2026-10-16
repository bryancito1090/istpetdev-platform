import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
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
  IonButton,
  IonBadge
} from '@ionic/angular/standalone';
import { MobileAssignmentDelivery } from '@istpetdev/shared-core';
import { FieldOutboxService, OutboxItem } from '../../outbox/field-outbox.service';

type CustodyStage = 'WaitingWarehouse' | 'ToPickup' | 'ToDeliver' | 'Delivered' | 'Received';

@Component({
  selector: 'app-receipt',
  standalone: true,
  imports: [
    CommonModule,
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
    IonButton,
    IonBadge
  ],
  template: `
    <ion-header>
      <ion-toolbar color="primary">
        <ion-buttons slot="start">
          <ion-back-button defaultHref="/home"></ion-back-button>
        </ion-buttons>
        <ion-title>Custodia en Ruta</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      @if (delivery(); as del) {
        <ion-card class="mb-4">
          <ion-card-header>
            <div class="flex items-center justify-between mb-1">
              <span class="text-xs font-mono text-slate-500">{{ del.deliveryId }}</span>
              <div class="flex items-center gap-1">
                <ion-badge [color]="stageBadgeColor()">{{ stageLabel() }}</ion-badge>
                <ion-badge color="medium">#{{ del.sequence }}</ion-badge>
              </div>
            </div>
            <ion-card-title class="text-lg">{{ del.pointName }}</ion-card-title>
            <ion-card-subtitle>{{ del.address }}</ion-card-subtitle>
          </ion-card-header>
        </ion-card>

        <!-- Bultos de la entrega -->
        <ion-card class="mb-4">
          <ion-card-header>
            <ion-card-title class="text-base">Bultos de la parada</ion-card-title>
            <ion-card-subtitle>
              {{ del.handlingUnits.length }} bulto(s) · el receptor confirmará las cantidades con su cuenta
            </ion-card-subtitle>
          </ion-card-header>
          <ion-card-content class="space-y-2">
            @for (hu of del.handlingUnits; track hu.handlingUnitId) {
              <div class="p-3 bg-slate-50 border border-slate-200 rounded-lg flex items-center justify-between">
                <div>
                  <span class="font-bold text-xs text-slate-800 block">{{ hu.code }}</span>
                  <span class="text-[11px] text-slate-500">
                    {{ hu.lines.length }} línea(s) · {{ huStatusLabel(hu.status) }}
                  </span>
                </div>
                <ion-badge color="medium">{{ hu.status }}</ion-badge>
              </div>
            }
            @if (del.handlingUnits.length === 0) {
              <div class="p-3 bg-amber-50 border border-amber-200 rounded-lg text-xs text-amber-900">
                La entrega aún no tiene bultos empacados por Bodega.
              </div>
            }
          </ion-card-content>
        </ion-card>

        <!-- Acciones de custodia por turno -->
        <div class="space-y-2 pb-8">
          @if (stage() === 'WaitingWarehouse') {
            <div class="p-4 bg-amber-50 border border-amber-300 rounded-lg text-center space-y-1">
              <p class="font-bold text-amber-900">Esperando a Bodega</p>
              <p class="text-xs text-amber-800">
                Bodega debe empacar, sellar y despachar los bultos antes de que puedas recogerlos.
              </p>
              <ion-button size="small" fill="outline" color="warning" class="mt-2" [disabled]="isRefreshing()" (click)="refreshData()">
                {{ isRefreshing() ? 'Actualizando...' : '↻ Actualizar estado' }}
              </ion-button>
            </div>
          }

          @if (stage() === 'ToPickup') {
            <ion-button expand="block" size="large" color="warning" [disabled]="actionInFlight()" (click)="pickup()">
              {{ actionInFlight() ? 'Registrando...' : 'Marcar Recogido' }}
            </ion-button>
          }

          @if (stage() === 'ToDeliver') {
            <ion-button expand="block" size="large" color="success" [disabled]="actionInFlight()" (click)="deliver()">
              {{ actionInFlight() ? 'Registrando...' : 'Marcar Entregado en el Punto' }}
            </ion-button>
          }

          @if (stage() === 'Delivered') {
            <div class="p-4 bg-emerald-50 border border-emerald-300 rounded-lg text-center space-y-1">
              <p class="font-bold text-emerald-900">Entregado — esperando al Receptor</p>
              <p class="text-xs text-emerald-800">
                El Receptor del punto confirmará la recepción escaneando el QR del bulto.
              </p>
            </div>
          }

          @if (stage() === 'Received') {
            <div class="p-4 bg-emerald-50 border border-emerald-300 rounded-lg text-center space-y-1">
              <p class="font-bold text-emerald-900">Recibido en el punto</p>
              <p class="text-xs text-emerald-800">La recepción quedó registrada con la cuenta del Receptor.</p>
            </div>
          }

          <!-- Estado real del último comando guardado (nunca se presenta una captura local como confirmación) -->
          @if (activeItem(); as item) {
            <div class="p-3 rounded-lg border text-xs space-y-1"
                 [class.bg-blue-50]="item.status === 'SavedOnDevice'"
                 [class.border-blue-200]="item.status === 'SavedOnDevice'"
                 [class.bg-emerald-50]="item.status === 'Complete'"
                 [class.border-emerald-200]="item.status === 'Complete'"
                 [class.bg-amber-50]="item.status === 'RequiresReview' || item.status === 'SessionRequired'"
                 [class.border-amber-300]="item.status === 'RequiresReview' || item.status === 'SessionRequired'">
              <p class="font-bold">
                {{ commandTitle(item) }}: {{ outboxService.statusLabel(item.status) }}
              </p>
              @if (item.status === 'SavedOnDevice') {
                <p>Guardado en este teléfono; aún no confirmado por el servidor. Reintenta desde la cola.</p>
              }
              @if (item.status === 'Complete') {
                <p>Confirmado por el servidor a las {{ item.custodyResponse?.changedAtUtc | date:'shortTime' }}.</p>
              }
              @if (item.errorMessage) {
                <p class="text-amber-900">{{ item.errorMessage }}</p>
              }
            </div>
          }

          @if (validationError(); as error) {
            <div class="p-3 bg-rose-50 border border-rose-200 rounded-lg text-xs text-rose-700">{{ error }}</div>
          }

          <ion-button expand="block" fill="outline" (click)="openSyncQueue()">
            Ver Cola de Sincronización
          </ion-button>
        </div>
      } @else if (loadError(); as loadErrorMsg) {
        <div class="text-center py-12 space-y-4">
          <p class="text-sm text-slate-600">{{ loadErrorMsg }}</p>
          <ion-button expand="block" (click)="goHome()">Volver a Inicio</ion-button>
        </div>
      } @else {
        <div class="text-center py-12">
          <p class="text-sm text-slate-500">Cargando entrega seleccionada...</p>
        </div>
      }
    </ion-content>
  `
})
export class ReceiptPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly outboxService = inject(FieldOutboxService);

  readonly delivery = signal<MobileAssignmentDelivery | null>(null);
  readonly validationError = signal<string | null>(null);
  readonly loadError = signal<string | null>(null);
  readonly busy = signal<boolean>(false);

  readonly stage = computed<CustodyStage>(() => {
    const del = this.delivery();
    if (!del) return 'WaitingWarehouse';

    // Estado monotónico: snapshot + comandos confirmados + estado de los bultos.
    const status = this.outboxService.effectiveDeliveryStatus(
      del.deliveryId,
      del.status,
      del.handlingUnits.map((hu) => hu.status),
    );

    if (status === 'Delivered') return 'Delivered';
    if (status === 'Received' || status === 'Partial') return 'Received';
    if (status === 'InTransit') return 'ToDeliver';
    if (status === 'Dispatched') {
      const unitsOk = del.handlingUnits.length > 0 && del.handlingUnits.every((hu) => hu.status === 'Dispatched');
      return unitsOk ? 'ToPickup' : 'WaitingWarehouse';
    }
    return 'WaitingWarehouse';
  });

  readonly activeItem = computed<OutboxItem | null>(() => {
    const del = this.delivery();
    if (!del) return null;
    return this.outboxService.outboxItems()
      .filter((i) => i.deliveryId === del.deliveryId && i.commandKind)
      .sort((a, b) => new Date(b.capturedAtUtc).getTime() - new Date(a.capturedAtUtc).getTime())[0] ?? null;
  });

  readonly actionInFlight = computed(() => {
    if (this.busy()) return true;
    const item = this.activeItem();
    if (!item) return false;
    return item.status === 'Sending';
  });

  readonly isRefreshing = signal(false);

  ngOnInit(): void {
    const deliveryId = this.route.snapshot.paramMap.get('deliveryId');
    if (deliveryId) {
      this.loadDeliveryData(deliveryId);
      const del = this.delivery();
      if (
        del &&
        (del.status === 'Draft' || del.handlingUnits.length === 0) &&
        typeof navigator !== 'undefined' &&
        navigator.onLine &&
        !this.outboxService.isSimulatingOffline?.() &&
        typeof this.outboxService.downloadRouteAssignment === 'function'
      ) {
        void this.refreshData();
      }
    }
  }

  refreshData(): void {
    const deliveryId = this.route.snapshot.paramMap.get('deliveryId');
    if (!deliveryId) return;
    if (
      typeof navigator !== 'undefined' &&
      navigator.onLine &&
      !this.outboxService.isSimulatingOffline?.() &&
      typeof this.outboxService.downloadRouteAssignment === 'function'
    ) {
      this.isRefreshing.set(true);
      this.outboxService
        .downloadRouteAssignment()
        .then(() => {
          this.loadDeliveryData(deliveryId);
        })
        .catch(() => {
          this.loadDeliveryData(deliveryId);
        })
        .finally(() => {
          this.isRefreshing.set(false);
        });
    } else {
      this.loadDeliveryData(deliveryId);
    }
  }

  private loadDeliveryData(deliveryId: string): void {
    const assignment = this.outboxService.activeAssignment();
    let found: MobileAssignmentDelivery | null = null;

    if (assignment && assignment.routes) {
      for (const route of assignment.routes) {
        const d = route.deliveries.find((del: MobileAssignmentDelivery) => del.deliveryId === deliveryId);
        if (d) {
          found = d;
          break;
        }
      }
    }

    if (!found) {
      this.loadError.set('No encontramos esta entrega en tu ruta. Vuelve a Inicio y descarga la ruta.');
      return;
    }

    this.delivery.set(found);
  }

  stageLabel(): string {
    switch (this.stage()) {
      case 'WaitingWarehouse': return 'Esperando a Bodega';
      case 'ToPickup': return 'Listo para recoger';
      case 'ToDeliver': return 'En ruta';
      case 'Delivered': return 'Entregado';
      case 'Received': return 'Recibido';
    }
  }

  stageBadgeColor(): string {
    switch (this.stage()) {
      case 'WaitingWarehouse': return 'warning';
      case 'ToPickup': return 'warning';
      case 'ToDeliver': return 'primary';
      case 'Delivered': return 'success';
      case 'Received': return 'success';
    }
  }

  huStatusLabel(status: string): string {
    switch (status) {
      case 'Sealed':
      case 'Prepared': return 'Empacado por Bodega';
      case 'Dispatched': return 'Despachado al vehículo';
      case 'InTransit': return 'En custodia del conductor';
      case 'Delivered': return 'Entregado en el punto';
      case 'Received': return 'Recibido';
      case 'Partial': return 'Recibido parcialmente';
      default: return status;
    }
  }

  commandTitle(item: OutboxItem): string {
    return item.commandKind === 'Pickup' ? 'Recogida' : 'Entrega';
  }

  goHome(): void {
    this.router.navigate(['/home']);
  }

  openSyncQueue(): void {
    this.router.navigate(['/sync']);
  }

  async pickup(): Promise<void> {
    await this.runCommand('Pickup', 'Recogida de bultos (Conductor)');
  }

  async deliver(): Promise<void> {
    await this.runCommand('Deliver', 'Entrega en punto (Conductor)');
  }

  private async runCommand(command: 'Pickup' | 'Deliver', label: string): Promise<void> {
    const del = this.delivery();
    if (!del || this.busy()) return;

    // Guarda también antes de guardar el comando, no solo en la plantilla.
    const expectedStage: CustodyStage = command === 'Pickup' ? 'ToPickup' : 'ToDeliver';
    if (this.stage() !== expectedStage) {
      this.validationError.set('La entrega no está en el estado que permite esa acción. Descarga la ruta de nuevo.');
      return;
    }

    this.validationError.set(null);
    this.busy.set(true);
    try {
      await this.outboxService.saveCustodyCommand({ deliveryId: del.deliveryId, command, label });
    } catch (error) {
      this.validationError.set(error instanceof Error ? error.message : 'No se pudo guardar el comando de custodia.');
    } finally {
      this.busy.set(false);
    }
  }
}
