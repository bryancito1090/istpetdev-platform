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
      <ion-toolbar color="primary">
        <ion-buttons slot="start">
          <ion-back-button defaultHref="/home"></ion-back-button>
        </ion-buttons>
        <ion-title>Recepción en Campo</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      @if (delivery(); as del) {
        <!-- Tarjeta de Información de Entrega -->
        <ion-card class="mb-4">
          <ion-card-header>
            <div class="flex items-center justify-between mb-1">
              <span class="text-xs font-mono text-slate-500">{{ del.deliveryId }}</span>
              <ion-badge color="primary">Secuencia {{ del.sequence }}</ion-badge>
            </div>
            <ion-card-title class="text-lg">{{ del.pointName }}</ion-card-title>
            <ion-card-subtitle>{{ del.address }}</ion-card-subtitle>
          </ion-card-header>
        </ion-card>

        <!-- Formulario de Líneas y Cantidades -->
        <ion-card class="mb-4">
          <ion-card-header>
            <ion-card-title class="text-base">Cantidades por Lote e Insumo</ion-card-title>
            <ion-card-subtitle>Ajuste aceptados o rechazados según verificación física</ion-card-subtitle>
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

                @if (line.quantityAccepted + line.quantityRejected !== line.expectedQuantity) {
                  <div class="text-[11px] text-amber-700 bg-amber-50 p-1.5 rounded">
                    ⚠ La suma ({{ line.quantityAccepted + line.quantityRejected }}) difiere de lo esperado ({{ line.expectedQuantity }}).
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

              <div class="flex gap-2">
                <ion-button expand="block" fill="outline" class="flex-1" (click)="fileInput.click()">
                  📷 {{ photoBlob() ? 'Cambiar Foto' : 'Tomar / Seleccionar Foto' }}
                </ion-button>

                <ion-button expand="block" fill="outline" (click)="generateSimulatedPhoto()">
                  ✨ Foto Demo
                </ion-button>
              </div>

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
            {{ isSaving() ? 'Guardando en Teléfono...' : '✓ Confirmar y Guardar en Dispositivo' }}
          </ion-button>

          <p class="text-[11px] text-center text-slate-500">
            Los datos se guardan atómicamente en IndexedDB (S-05) y se sincronizan al recuperar red.
          </p>
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

    // Si no está cargada aún la asignación en memoria, usar fallback de demo
    if (!found) {
      found = {
        deliveryId,
        sequence: 1,
        pointCode: 'HOSP-001',
        pointName: 'Farmacia Central Quito (Demo)',
        address: 'Av. 10 de Agosto y Colón',
        dueAtUtc: new Date().toISOString(),
        status: 'Dispatched',
        handlingUnits: [
          {
            handlingUnitId: 'hu-001',
            code: 'HU-001',
            deliveryId,
            status: 'Dispatched',
            labelUrl: 'https://api.istpetdev.local/labels/hu-001.pdf',
            createdAtUtc: new Date().toISOString(),
            lines: [
              {
                deliveryLineId: `dl-${deliveryId}-01`,
                lotId: 'LOT-INS-2026-A1',
                sku: 'INS-NPH-100UI',
                quantity: 50,
                unit: 'Viales'
              }
            ]
          }
        ]
      };
    }

    const target = found;
    this.delivery.set(target);
    this.deliveryVersion.set(version);
    this.recipientName = target.pointName;

    // Extraer líneas desde handlingUnits
    const allLines: HandlingUnitLineDto[] = target.handlingUnits.flatMap((hu: any) => hu.lines || []);
    if (allLines.length === 0) {
      allLines.push({
        deliveryLineId: `dl-${deliveryId}-01`,
        lotId: 'LOT-INS-2026-A1',
        sku: 'INS-NPH-100UI',
        quantity: 50,
        unit: 'Viales'
      });
    }

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

  generateSimulatedPhoto(): void {
    // Generar un Blob sintético simulado usando un canvas pequeño
    const canvas = document.createElement('canvas');
    canvas.width = 300;
    canvas.height = 200;
    const ctx = canvas.getContext('2d');
    if (ctx) {
      ctx.fillStyle = '#0284c7';
      ctx.fillRect(0, 0, 300, 200);
      ctx.fillStyle = '#ffffff';
      ctx.font = '14px sans-serif';
      ctx.fillText('Evidencia Remito Demo', 20, 50);
      ctx.fillText(new Date().toLocaleString(), 20, 80);
      ctx.fillText(`Entrega: ${this.delivery()?.deliveryId}`, 20, 110);
    }
    canvas.toBlob((blob) => {
      if (blob) {
        this.photoBlob.set(blob);
        this.photoPreviewUrl.set(URL.createObjectURL(blob));
      }
    }, 'image/jpeg');
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

    // Verificar si se ha firmado
    let signatureBlob: Blob | undefined;
    if (this.signaturePad && !this.signaturePad.isEmpty()) {
      const dataUrl = this.signaturePad.toDataURL('image/png');
      const res = await fetch(dataUrl);
      signatureBlob = await res.blob();
    } else {
      // Generar una firma de prueba rápida si no se dibujó en pantalla
      const canvas = document.createElement('canvas');
      canvas.width = 200;
      canvas.height = 80;
      const ctx = canvas.getContext('2d');
      if (ctx) {
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, 200, 80);
        ctx.fillStyle = '#1e293b';
        ctx.font = 'italic 16px sans-serif';
        ctx.fillText(this.recipientName || 'Firma Conforme', 10, 45);
      }
      signatureBlob = await new Promise<Blob>((resolve) =>
        canvas.toBlob((b) => resolve(b || new Blob()), 'image/png')
      );
    }

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
          unit: l.unit
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
