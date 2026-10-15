import { Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
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
  IonButton,
  IonBadge
} from '@ionic/angular/standalone';
import SignaturePad from 'signature_pad';
import { MobileAssignmentDelivery, HandlingUnitLineDto } from '@istpetdev/shared-core';
import { FieldOutboxService } from '../../outbox/field-outbox.service';

interface ReceiptLineForm {
  deliveryLineId: string;
  lotId: string;
  sku: string;
  expectedQuantity: number;
  quantityAccepted: number;
  quantityRejected: number;
  unit: string;
  remainingQuantity?: number;
  nextRequestedQuantity?: number;
}

@Component({
  selector: 'app-receipt',
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
    IonBadge
  ],
  template: `
    <ion-header>
      <ion-toolbar [color]="delivery()?.kind === 'Pickup' ? 'warning' : 'primary'">
        <ion-buttons slot="start">
          <ion-back-button defaultHref="/home"></ion-back-button>
        </ion-buttons>
        <ion-title>{{ delivery()?.kind === 'Pickup' ? 'Carga / Retiro en Bodega' : 'Recepción en Campo' }}</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      @if (delivery(); as del) {
        <!-- Tarjeta de Información de Entrega / Recogida -->
        <ion-card class="mb-4" [class.border-amber-300]="del.kind === 'Pickup'">
          <ion-card-header>
            <div class="flex items-center justify-between mb-1">
              <span class="text-xs font-mono text-slate-500">{{ del.deliveryId }}</span>
              <div class="flex items-center gap-1">
                <ion-badge [color]="del.kind === 'Pickup' ? 'warning' : 'primary'">
                  {{ del.kind === 'Pickup' ? '📦 Retiro en Bodega' : '🏥 Entrega en Destino' }}
                </ion-badge>
                <ion-badge color="medium">#{{ del.sequence }}</ion-badge>
              </div>
            </div>
            <ion-card-title class="text-lg">{{ del.pointName }}</ion-card-title>
            <ion-card-subtitle>{{ del.address }}</ion-card-subtitle>
          </ion-card-header>
        </ion-card>

        <!-- Formulario de Líneas y Cantidades -->
        <ion-card class="mb-4">
          <ion-card-header>
            <ion-card-title class="text-base">
              {{ del.kind === 'Pickup' ? 'Bultos a Retirar e Ingresar al Vehículo' : 'Cantidades por Lote e Insumo' }}
            </ion-card-title>
            <ion-card-subtitle>
              {{ del.kind === 'Pickup' ? 'Verifique unidades cargadas desde la bodega emisora' : 'Ajuste aceptados o rechazados según verificación física' }}
            </ion-card-subtitle>
          </ion-card-header>
          <ion-card-content class="space-y-4">
            @for (line of lines(); track line.deliveryLineId) {
              <div class="p-3 bg-slate-50 border border-slate-200 rounded-lg space-y-2">
                <div class="flex justify-between items-start">
                  <div>
                    <span class="font-bold text-xs text-slate-800 block">{{ line.sku }}</span>
                    <span class="text-[11px] font-mono text-slate-500">Lote: {{ line.lotId }}</span>
                  </div>
                  <span class="text-xs font-bold text-slate-700">
                    Esperado: {{ line.expectedQuantity }} {{ line.unit }}
                  </span>
                </div>

                <!-- Controles táctiles accesibles (altura >= 44px) -->
                <div class="grid grid-cols-2 gap-2 pt-1">
                  <!-- Aceptados -->
                  <div class="bg-white p-2 rounded border border-emerald-200">
                    <span class="text-[11px] font-semibold text-emerald-800 block mb-1">Aceptados:</span>
                    <div class="flex items-center justify-between">
                      <button
                        type="button"
                        (click)="decrementAccepted(line)"
                        class="w-11 h-11 bg-slate-100 active:bg-slate-200 text-slate-700 font-bold rounded-lg text-lg flex items-center justify-center select-none"
                        aria-label="Disminuir aceptados">
                        -
                      </button>
                      <span class="font-bold text-base text-emerald-700 mx-2">{{ line.quantityAccepted }}</span>
                      <button
                        type="button"
                        (click)="incrementAccepted(line)"
                        class="w-11 h-11 bg-emerald-100 active:bg-emerald-200 text-emerald-800 font-bold rounded-lg text-lg flex items-center justify-center select-none"
                        aria-label="Aumentar aceptados">
                        +
                      </button>
                    </div>
                  </div>

                  <!-- Rechazados -->
                  <div class="bg-white p-2 rounded border border-rose-200">
                    <span class="text-[11px] font-semibold text-rose-800 block mb-1">Rechazados:</span>
                    <div class="flex items-center justify-between">
                      <button
                        type="button"
                        (click)="decrementRejected(line)"
                        class="w-11 h-11 bg-slate-100 active:bg-slate-200 text-slate-700 font-bold rounded-lg text-lg flex items-center justify-center select-none"
                        aria-label="Disminuir rechazados">
                        -
                      </button>
                      <span class="font-bold text-base text-rose-700 mx-2">{{ line.quantityRejected }}</span>
                      <button
                        type="button"
                        (click)="incrementRejected(line)"
                        class="w-11 h-11 bg-rose-100 active:bg-rose-200 text-rose-800 font-bold rounded-lg text-lg flex items-center justify-center select-none"
                        aria-label="Aumentar rechazados">
                        +
                      </button>
                    </div>
                  </div>
                </div>

                <!-- Reconciliación física y pedido futuro -->
                <div class="grid grid-cols-2 gap-2 pt-2 border-t border-slate-200">
                  <div class="bg-white p-2 rounded border border-slate-200">
                    <span class="text-[11px] font-semibold text-slate-700 block mb-1">Stock en percha:</span>
                    <input
                      type="number"
                      min="0"
                      [(ngModel)]="line.remainingQuantity"
                      placeholder="Ej: 10"
                      class="w-full px-2 py-1 border border-slate-300 rounded text-xs text-slate-800 bg-white" />
                  </div>
                  <div class="bg-white p-2 rounded border border-blue-200">
                    <span class="text-[11px] font-semibold text-blue-800 block mb-1">Pedir próx. visita:</span>
                    <input
                      type="number"
                      min="0"
                      [(ngModel)]="line.nextRequestedQuantity"
                      placeholder="Ej: 50"
                      class="w-full px-2 py-1 border border-blue-300 rounded text-xs text-blue-900 bg-white" />
                  </div>
                </div>

                @if (line.quantityAccepted + line.quantityRejected !== line.expectedQuantity) {
                  <div class="text-[11px] text-amber-700 bg-amber-50 p-1.5 rounded">
                    Aviso: La suma ({{ line.quantityAccepted + line.quantityRejected }}) difiere de lo esperado ({{ line.expectedQuantity }}).
                  </div>
                }
              </div>
            }
          </ion-card-content>
        </ion-card>

        <!-- Datos del Receptor -->
        <ion-card class="mb-4">
          <ion-card-header>
            <ion-card-title class="text-base">Datos de Quien Recibe</ion-card-title>
          </ion-card-header>
          <ion-card-content class="space-y-3">
            <ion-item>
              <ion-label position="stacked">Nombre del Receptor / Farmacéutico *</ion-label>
              <ion-input
                placeholder="Nombre completo"
                [(ngModel)]="recipientName"
                name="recipientName">
              </ion-input>
            </ion-item>

            <ion-item>
              <ion-label position="stacked">Notas u Observaciones</ion-label>
              <ion-input
                placeholder="Ej: Embalaje en buen estado"
                [(ngModel)]="notes"
                name="notes">
              </ion-input>
            </ion-item>
          </ion-card-content>
        </ion-card>

        <!-- Captura de Foto de Evidencia -->
        <ion-card class="mb-4">
          <ion-card-header>
            <ion-card-title class="text-base">Evidencia Fotográfica (Remito o Bulto)</ion-card-title>
          </ion-card-header>
          <ion-card-content>
            <div class="space-y-3">
              <input
                #fileInput
                type="file"
                accept="image/*"
                capture="environment"
                (change)="onPhotoSelected($event)"
                class="hidden" />

              <ion-button expand="block" fill="outline" (click)="fileInput.click()">
                {{ photoBlob() ? 'Cambiar Foto' : 'Tomar / Seleccionar Foto' }}
              </ion-button>

              @if (photoPreviewUrl()) {
                <div class="relative mt-2 border rounded overflow-hidden max-h-48 flex justify-center bg-black">
                  <img [src]="photoPreviewUrl()" alt="Vista previa de evidencia" class="object-contain max-h-48" />
                  <button
                    type="button"
                    (click)="clearPhoto()"
                    class="absolute top-2 right-2 bg-rose-600 text-white rounded-full p-1 text-xs px-2 shadow">
                    Eliminar
                  </button>
                </div>
              }
            </div>
          </ion-card-content>
        </ion-card>

        <!-- Firma Digital con SignaturePad -->
        <ion-card class="mb-4">
          <ion-card-header>
            <div class="flex items-center justify-between">
              <ion-card-title class="text-base">Firma Digital del Receptor *</ion-card-title>
              <button
                type="button"
                (click)="clearSignature()"
                class="text-xs text-rose-600 font-semibold px-2 py-1 bg-rose-50 rounded">
                Limpiar
              </button>
            </div>
            <ion-card-subtitle>Firme dentro del recuadro a continuación</ion-card-subtitle>
          </ion-card-header>
          <ion-card-content>
            <div class="border-2 border-dashed border-slate-300 rounded-lg bg-white overflow-hidden touch-none">
              <canvas
                #signatureCanvas
                width="340"
                height="140"
                class="w-full h-[140px] block cursor-crosshair">
              </canvas>
            </div>
          </ion-card-content>
        </ion-card>

        <!-- Botón de Confirmación y Guardado Atómico en Outbox -->
        <div class="space-y-2 pb-8">
          @if (validationError()) {
            <div class="p-3 bg-rose-50 border border-rose-200 rounded-lg text-xs text-rose-700">
              {{ validationError() }}
            </div>
          }

          <ion-button
            expand="block"
            size="large"
            color="success"
            [disabled]="isSaving()"
            (click)="submitReceipt()">
            {{ isSaving() ? 'Guardando en Teléfono...' : 'Confirmar y Guardar en Dispositivo' }}
          </ion-button>

          <p class="text-[11px] text-center text-slate-500">
            Los datos se guardan atómicamente en IndexedDB (S-05) y se sincronizan al recuperar red.
          </p>
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
  private readonly outboxService = inject(FieldOutboxService);

  @ViewChild('signatureCanvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  readonly delivery = signal<MobileAssignmentDelivery | null>(null);
  readonly deliveryVersion = signal<number>(1);
  readonly lines = signal<ReceiptLineForm[]>([]);
  readonly photoBlob = signal<Blob | null>(null);
  readonly photoPreviewUrl = signal<string | null>(null);
  readonly isSaving = signal<boolean>(false);
  readonly validationError = signal<string | null>(null);
  readonly loadError = signal<string | null>(null);

  recipientName = '';
  notes = '';
  private signaturePad: SignaturePad | null = null;

  ngOnInit(): void {
    const deliveryId = this.route.snapshot.paramMap.get('deliveryId');
    if (deliveryId) {
      this.loadDeliveryData(deliveryId);
    }
  }

  private loadDeliveryData(deliveryId: string): void {
    const assignment = this.outboxService.activeAssignment();
    let found: MobileAssignmentDelivery | null = null;
    let version = 1;

    if (assignment && assignment.routes) {
      for (const route of assignment.routes) {
        const d = route.deliveries.find((del: MobileAssignmentDelivery) => del.deliveryId === deliveryId);
        if (d) {
          found = d;
          version = route.revision;
          break;
        }
      }
    }

    if (!found) {
      this.loadError.set('No encontramos esta entrega en tu ruta. Vuelve a Inicio y descarga la ruta.');
      return;
    }

    // Extraer líneas desde handlingUnits; sin líneas no se registra recepción
    const allLines: HandlingUnitLineDto[] = found.handlingUnits.flatMap((hu: any) => hu.lines || []);
    if (allLines.length === 0) {
      this.loadError.set('Esta entrega no tiene líneas de recepción registradas. Vuelve a Inicio y descarga la ruta.');
      return;
    }

    this.delivery.set(found);
    this.deliveryVersion.set(version);
    this.recipientName = found.pointName;

    const initialLines: ReceiptLineForm[] = allLines.map((l: HandlingUnitLineDto) => ({
      deliveryLineId: l.deliveryLineId,
      lotId: l.lotId,
      sku: l.sku,
      expectedQuantity: l.quantity,
      quantityAccepted: l.quantity,
      quantityRejected: 0,
      unit: l.unit
    }));
    this.lines.set(initialLines);

    // Inicializar canvas de firma tras el ciclo de render
    setTimeout(() => {
      this.initSignaturePad();
    }, 150);
  }

  private initSignaturePad(): void {
    if (this.canvasRef && this.canvasRef.nativeElement) {
      const canvas = this.canvasRef.nativeElement;
      this.signaturePad = new SignaturePad(canvas, {
        backgroundColor: 'rgb(255, 255, 255)',
        penColor: 'rgb(15, 23, 42)'
      });
    }
  }

  incrementAccepted(line: ReceiptLineForm): void {
    line.quantityAccepted++;
    this.lines.set([...this.lines()]);
  }

  decrementAccepted(line: ReceiptLineForm): void {
    if (line.quantityAccepted > 0) {
      line.quantityAccepted--;
      this.lines.set([...this.lines()]);
    }
  }

  incrementRejected(line: ReceiptLineForm): void {
    line.quantityRejected++;
    this.lines.set([...this.lines()]);
  }

  decrementRejected(line: ReceiptLineForm): void {
    if (line.quantityRejected > 0) {
      line.quantityRejected--;
      this.lines.set([...this.lines()]);
    }
  }

  clearSignature(): void {
    if (this.signaturePad) {
      this.signaturePad.clear();
    }
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      this.photoBlob.set(file);
      this.photoPreviewUrl.set(URL.createObjectURL(file));
    }
  }

  goHome(): void {
    this.router.navigate(['/home']);
  }

  clearPhoto(): void {
    this.photoBlob.set(null);
    this.photoPreviewUrl.set(null);
  }

  async submitReceipt(): Promise<void> {
    this.validationError.set(null);
    const del = this.delivery();
    if (!del) return;

    if (!this.recipientName.trim()) {
      this.validationError.set('Por favor ingrese el nombre de la persona que recibe.');
      return;
    }

    if (!this.signaturePad || this.signaturePad.isEmpty()) {
      this.validationError.set('Dibuje la firma de quien recibe para confirmar la recepción.');
      return;
    }

    const dataUrl = this.signaturePad.toDataURL('image/png');
    const res = await fetch(dataUrl);
    const signatureBlob: Blob = await res.blob();

    this.isSaving.set(true);

    try {
      await this.outboxService.saveReceiptAtomic({
        deliveryId: del.deliveryId,
        expectedDeliveryVersion: this.deliveryVersion(),
        recipientName: this.recipientName,
        notes: this.notes,
        lines: this.lines().map((l: ReceiptLineForm) => ({
          deliveryLineId: l.deliveryLineId,
          lotId: l.lotId,
          quantityAccepted: l.quantityAccepted,
          quantityRejected: l.quantityRejected,
          unit: l.unit,
          remainingQuantity: l.remainingQuantity != null && l.remainingQuantity !== ('' as any) ? Number(l.remainingQuantity) : undefined,
          nextRequestedQuantity: l.nextRequestedQuantity != null && l.nextRequestedQuantity !== ('' as any) ? Number(l.nextRequestedQuantity) : undefined
        })),
        photoBlob: this.photoBlob() ?? undefined,
        signatureBlob
      });

      this.isSaving.set(false);
      // Redirigir a la cola de sincronización para ver el estado del ítem
      this.router.navigate(['/sync']);
    } catch (err: any) {
      this.isSaving.set(false);
      this.validationError.set(err?.message || 'Error guardando en el almacenamiento local.');
    }
  }
}
