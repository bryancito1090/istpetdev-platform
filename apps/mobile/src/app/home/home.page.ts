import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import {
  IonContent,
  IonButton,
  IonInput,
  AlertController
} from '@ionic/angular/standalone';
import { RECEIPT_REJECTION_REASONS, MobileAssignmentDelivery } from '@istpetdev/shared-core';
import { FIELD_ROLE_LABELS, MobileAuthService } from '../auth';
import { FieldOutboxService } from '../field/outbox/field-outbox.service';
import { FIELD_API, IFieldApi, ReceiverUnit } from '../field/field-api.interface';
import { parseUnitQr } from '../field/unit-qr';
import { MockFieldApiService } from '../field/mock/mock-field-api.service';
import { WarehouseComponent } from '../field/pages/warehouse/warehouse.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IonContent,
    IonButton,
    IonInput,
    WarehouseComponent
  ],
  template: `
    <!-- Barra Superior: Entorno de Prueba (Stripe Inspired Dark Bar) -->
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
      <!-- Fila de Navegación: Hamburguesa, Título de Usuario, Campana, Búsqueda -->
      <div class="stripe-nav-header">
        <button type="button" class="stripe-nav-btn" aria-label="Menú principal" (click)="openMenuAlert()">
          <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.3">
            <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5M3.75 17.25h16.5" />
          </svg>
        </button>

        <h1 class="stripe-user-title">
          {{ authService.me()?.displayName || 'Alexander Martinez' }}
        </h1>

        <div class="stripe-header-actions">
          <button type="button" class="stripe-nav-btn relative" aria-label="Notificaciones" (click)="openSyncQueue()">
            <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8">
              <path stroke-linecap="round" stroke-linejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
            </svg>
            @if (pendingCount() > 0) {
              <span class="stripe-bell-badge">{{ pendingCount() }}</span>
            }
          </button>

          <button type="button" class="stripe-nav-btn" aria-label="Buscar" (click)="toggleSearch()">
            <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Barra de Búsqueda Desplegable -->
      @if (searchOpen()) {
        <div class="stripe-search-bar px-4 pb-2">
          <div class="relative flex items-center">
            <input
              type="text"
              [(ngModel)]="searchQuery"
              placeholder="Buscar parada, destino o código..."
              class="w-full bg-slate-100 text-slate-900 text-xs rounded-lg px-3 py-2 pr-8 focus:outline-none focus:ring-2 focus:ring-[#00A7C4]" />
            <button type="button" class="absolute right-2 text-slate-400 hover:text-slate-600 text-xs" (click)="searchQuery = ''; searchOpen.set(false)">
              ✕
            </button>
          </div>
        </div>
      }

      <!-- Barra de Filtros en Píldoras -->
      <div class="stripe-pills-bar">
        <div class="stripe-pills-scroll">
          <button type="button" class="stripe-pill-item" (click)="cycleDateRange()">
            <span>{{ selectedDateRange() }}</span>
            <svg class="w-3.5 h-3.5 text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
              <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
            </svg>
          </button>

          <button type="button" class="stripe-pill-item" (click)="cycleFrequency()">
            <span>{{ selectedFrequency() }}</span>
            <svg class="w-3.5 h-3.5 text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
              <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
            </svg>
          </button>

          <button type="button" class="stripe-pill-item" [class.is-active]="comparePrevious()" (click)="toggleComparePrevious()">
            <svg class="w-3.5 h-3.5 text-slate-500" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z" clip-rule="evenodd" />
            </svg>
            <span>Período anterior</span>
            <svg class="w-3.5 h-3.5 text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
              <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
            </svg>
          </button>
        </div>

        <button type="button" class="stripe-plus-btn" aria-label="Agregar acción" (click)="openQuickAction()">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Contenido Scrollable con Tarjetas Estilo Stripe -->
    <ion-content class="ion-padding stripe-main-content">
      <!-- Insignia fija: Datos Simulados (F7 / Spec 004) solo si usa mock -->
      @if (isMockApi()) {
        <div class="mb-3 flex items-center justify-between p-2.5 bg-cyan-50 border border-cyan-200 rounded-lg text-xs text-[#0C4956] shadow-xs">
          <div class="flex items-center gap-1.5">
            <span class="w-2 h-2 rounded-full bg-[#00A7C4] animate-ping"></span>
            <span class="font-bold">Datos simulados (Fase Frontend-First)</span>
          </div>
          <span class="text-[10px] bg-cyan-200 text-[#04303B] px-2 py-0.5 rounded font-mono font-bold">
            useMockApi
          </span>
        </div>
      }

      <!-- 015: la app se abrió sin señal; la cola se envía sola al renovar la sesión -->
      @if (authService.offline()) {
        <div role="status" class="mb-3 p-2.5 bg-amber-50 border border-amber-300 rounded-lg text-xs text-amber-900 shadow-xs">
          <span class="font-bold">Sin conexión: tus capturas se guardan en el teléfono.</span>
          Se enviarán solas cuando vuelva la señal.
        </div>
      }

      <!-- Resumen de pendientes sincronización -->
      @if (pendingCount() > 0) {
        <button type="button" class="stripe-pending-banner mb-3" (click)="openSyncQueue()">
          <span class="summary-icon" aria-hidden="true">↗</span>
          <span><strong>{{ pendingCount() }} operaciones pendientes</strong><small>Consulta el estado de tus registros</small></span>
          <span class="summary-arrow" aria-hidden="true">→</span>
        </button>
      }

      <!-- TARJETA 1: VOLUMEN BRUTO (Idéntica al screenshot de Stripe con colores oficiales) -->
      <div class="stripe-card mb-4">
        <div class="stripe-card-header">
          <div class="stripe-card-title-group">
            <span class="card-title-text">Volumen bruto</span>
            <span class="info-icon-badge" title="Volumen logístico acumulado en el período">ⓘ</span>
          </div>

          <button type="button" class="stripe-card-action-btn" aria-label="Inspeccionar volumen" (click)="inspectVolumeDetails()">
            <svg class="w-4 h-4 text-slate-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
              <path d="M10 10h4" />
              <path d="M19 7V4a1 1 0 0 0-1-1h-2a1 1 0 0 0-1 1v3" />
              <path d="M8 7V4a1 1 0 0 0-1-1H5a1 1 0 0 0-1 1v3" />
              <circle cx="7" cy="15" r="5" />
              <circle cx="17" cy="15" r="5" />
            </svg>
          </button>
        </div>

        <div class="stripe-card-main-val">
          0,00 US$
        </div>

        <div class="stripe-card-sub-val">
          0,00 US$ período anterior
        </div>
      </div>

      <!-- ======================================================== -->
      <!-- CASO 1: RECEPTOR HOSPITALARIO (Solo Escanear QR y Novedades) -->
      <!-- ======================================================== -->
      @if (authService.currentRole() === 'Receptor') {
        <div class="stripe-card mb-4">
          <div class="stripe-card-header pb-2">
            <span class="card-title-text">Recibir insumos</span>
          </div>
          <div class="space-y-4 pt-1">
            <ion-button expand="block" (click)="openReceiverScan()" class="stripe-action-btn">
              Escanear QR con cámara
            </ion-button>
            <label class="block text-xs font-semibold text-slate-700">Enlace completo del QR
              <ion-input [(ngModel)]="qrInputText" placeholder="Pega el enlace del bulto"></ion-input>
            </label>
            <ion-button (click)="searchReceiverUnit()" [disabled]="receiverLoading()" class="stripe-action-btn-secondary">
              Buscar bulto
            </ion-button>
            @if (receiverError()) { <p role="alert" class="text-xs text-rose-700 font-semibold">{{ receiverError() }}</p> }
            @if (receiverCached()) {
              <p class="text-xs text-amber-800">Última consulta guardada. La recepción se validará al recuperar señal.</p>
            }
            @if (receiverActiveBox(); as box) {
              <div class="p-3 bg-slate-50 border border-slate-200 rounded-xl space-y-2">
                <h2 class="font-bold text-sm text-slate-900">{{ box.code }} · {{ box.destinationName }}</h2>
                <p class="text-xs text-slate-600">
                  Recepción por <strong>entrega completa</strong>: {{ box.boxes.length }} bulto(s)
                  ({{ box.boxes.map(b => b.code).join(', ') }}). Confirma todas las líneas enviadas.
                </p>
                @for (line of receiverLines; track line.deliveryLineId) {
                  <div class="space-y-2.5 rounded-lg border border-slate-200 bg-white p-3">
                    <h3 class="font-bold text-xs text-slate-900">{{ line.productName }}</h3>
                    <p class="text-[11px] text-slate-500">Lote {{ line.lotCode }} · Enviadas {{ line.quantity }} {{ line.unit }}</p>
                    <label class="block text-xs font-medium text-slate-700">Cantidad aceptada
                      <input class="w-full border border-slate-200 rounded p-2 text-xs" type="number" min="0" [max]="line.quantity" step="0.001"
                        [(ngModel)]="line.quantityAccepted" />
                    </label>
                    <p class="text-[11px] text-slate-500">Rechazadas: {{ line.quantity - line.quantityAccepted }} {{ line.unit }}</p>
                    @if (line.quantityAccepted < line.quantity) {
                      <label class="block text-xs font-medium text-slate-700">Motivo del rechazo *
                        <select class="w-full border border-slate-200 rounded p-2 text-xs" [(ngModel)]="line.rejectionReason">
                          <option value="">Selecciona un motivo</option>
                          @for (reason of receiverReasons; track reason.code) {
                            <option [value]="reason.code">{{ reason.label }}</option>
                          }
                        </select>
                      </label>
                    }
                    <label class="block text-xs font-medium text-slate-700">Stock en percha antes de recibir ({{ line.unit }})
                      <input class="w-full border border-slate-200 rounded p-2 text-xs" type="number" min="0" step="0.001" [(ngModel)]="line.remainingQuantity" />
                    </label>
                    <label class="block text-xs font-medium text-slate-700">Cantidad solicitada para reabastecimiento ({{ line.unit }})
                      <input class="w-full border border-slate-200 rounded p-2 text-xs" type="number" min="0" step="0.001" [(ngModel)]="line.nextRequestedQuantity" />
                    </label>
                  </div>
                }
                <label class="block text-xs font-medium text-slate-700">Foto de la novedad (obligatoria si rechazas unidades)
                  <input type="file" accept="image/jpeg,image/png,image/webp" capture="environment" (change)="onReceiverPhoto($event)" class="mt-1" />
                </label>
                @if (receiverPhoto(); as photo) { <p class="text-xs text-emerald-700 font-medium">Foto adjunta: {{ photo.name }}</p> }
                <ion-button expand="block" color="success" (click)="confirmReceiverAction()"
                  [disabled]="receiverLoading() || receiverSaved() || box.status !== 'Delivered'">
                  Guardar recepción de la entrega completa
                </ion-button>
              </div>
            }
            @if (receiverConfirmedMessage()) {
              <div class="mt-3 p-3 bg-emerald-50 border border-emerald-300 rounded-lg text-emerald-950 text-xs space-y-2" role="status">
                <div class="flex items-center gap-1.5 font-bold">
                  <span class="text-emerald-700 font-bold text-sm">✓</span>
                  <span>Recepción guardada</span>
                </div>
                <p class="leading-relaxed">{{ receiverConfirmedMessage() }}</p>
                <div class="flex flex-col sm:flex-row gap-2 pt-1">
                  <ion-button size="small" color="primary" class="flex-1 font-bold" (click)="resetReceiverScreen()">
                    Registrar otro bulto / Limpiar
                  </ion-button>
                  <ion-button size="small" fill="outline" color="primary" class="flex-1 font-bold" (click)="openSyncQueue()">
                    Ver cola de sincronización
                  </ion-button>
                </div>
              </div>
            }
          </div>
        </div>
      }

      <!-- ======================================================== -->
      <!-- CASO 2: OPERADOR DE BODEGA (Móvil / Real)                -->
      <!-- ======================================================== -->
      @else if (authService.currentRole() === 'Bodega') {
        <app-warehouse />
      }

      <!-- ======================================================== -->
      <!-- CASO 3: CONDUCTOR DE CUSTODIA (Móvil)                     -->
      <!-- ======================================================== -->
      @else {
        <!-- Banner de Aviso de Revisión de Ruta (PA-02 / FR-12 / F5) -->
        @if (outboxService.activeDriverNotice(); as notice) {
          <div class="mb-4 p-3.5 bg-amber-50 border-2 border-amber-400 text-amber-950 rounded-xl shadow-xs flex items-start gap-3">
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
                  class="text-xs font-bold px-3 py-1.5 bg-amber-900 text-white rounded-lg active:bg-amber-950 transition shadow-xs">
                  Entendido / Reconocer Aviso
                </button>
              </div>
            </div>
          </div>
        }

        <!-- Acciones del Conductor con estilo Stripe Tile -->
        <div class="stripe-actions-grid mb-4">
          <button type="button" class="stripe-action-tile" (click)="openScan()">
            <div class="tile-icon">
              <svg class="w-5 h-5 text-[#00A7C4]" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 4.875c0-.621.504-1.125 1.125-1.125h4.5c.621 0 1.125.504 1.125 1.125v4.5c0 .621-.504 1.125-1.125 1.125h-4.5A1.125 1.125 0 013.75 9.375v-4.5zM3.75 14.625c0-.621.504-1.125 1.125-1.125h4.5c.621 0 1.125.504 1.125 1.125v4.5c0 .621-.504 1.125-1.125 1.125h-4.5a1.125 1.125 0 01-1.125-1.125v-4.5zM13.5 4.875c0-.621.504-1.125 1.125-1.125h4.5c.621 0 1.125.504 1.125 1.125v4.5c0 .621-.504 1.125-1.125 1.125h-4.5A1.125 1.125 0 0113.5 9.375v-4.5z" />
                <path stroke-linecap="round" stroke-linejoin="round" d="M15 15h.008v.008H15V15zm0 3h.008v.008H15V18zm3-3h.008v.008H18V15zm0 3h.008v.008H18V18zm-3-6h.008v.008H15V12z" />
              </svg>
            </div>
            <span class="tile-title">Escanear QR</span>
            <span class="tile-subtitle">Custodia y entrega</span>
          </button>

          <button type="button" class="stripe-action-tile" [disabled]="loadingRoute()" (click)="downloadRoute()">
            <div class="tile-icon">
              <svg class="w-5 h-5 text-[#00A7C4]" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" />
              </svg>
            </div>
            <span class="tile-title">{{ loadingRoute() ? 'Cargando...' : 'Descargar Ruta' }}</span>
            <span class="tile-subtitle">Asignación local</span>
          </button>
        </div>

        <!-- Mensaje si se descarga la ruta -->
        @if (downloadMessage()) {
          <div class="mb-4 p-3 bg-emerald-50 border border-emerald-200 rounded-lg text-xs text-emerald-800">
            {{ downloadMessage() }}
          </div>
        }

        <!-- Lista de Paradas Asignadas -->
        <div class="mb-2.5 flex items-center justify-between px-1">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-700">
            Paradas de la Ruta Activa
          </span>
          <span class="text-[11px] text-slate-500 font-bold bg-slate-100 px-2 py-0.5 rounded-full">
            {{ totalDeliveries() }} entregas
          </span>
        </div>

        @if (outboxService.activeAssignment(); as assignment) {
          <div class="space-y-3">
            @for (route of filteredRoutes(); track route.routePlanId) {
              <div class="space-y-3">
                <div class="flex items-center justify-between text-xs text-slate-500 font-mono px-1">
                  <span>Ruta: {{ route.routePlanId }} (Rev {{ route.revision }})</span>
                  <span>Vehículo: {{ route.vehicleCode }}</span>
                </div>
                @if (route.originWarehouseName) {
                  <div class="text-xs text-slate-600 px-1" data-origin-warehouse>
                    Sale de: <strong>{{ route.originWarehouseName }}</strong>
                  </div>
                }

                @for (del of route.deliveries; track del.deliveryId) {
                  <div class="stripe-card" [class.border-amber-300]="del.kind === 'Pickup'">
                    <div class="stripe-card-header pb-2">
                      <div class="flex items-center gap-1.5">
                        <span class="stripe-tag" [class.is-warning]="del.kind === 'Pickup'">
                          #{{ del.sequence }} {{ del.kind === 'Pickup' ? '📦 Recogida' : '🏥 Entrega' }}
                        </span>
                        @if (del.kind === 'Pickup') {
                          <span class="text-[10px] bg-amber-100 text-amber-900 font-bold px-1.5 py-0.5 rounded">Bodega</span>
                        }
                      </div>
                      <span class="text-xs font-semibold text-slate-500">ID: {{ del.pointCode }}</span>
                    </div>

                    <h3 class="font-bold text-sm text-slate-900 px-4 mb-1">{{ del.pointName }}</h3>
                    <p class="text-xs text-slate-500 px-4 mb-3">{{ del.address }}</p>

                    <div class="px-4 pb-3">
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
                            Bultos HU: {{ del.handlingUnits.length }} • {{ driverStageLabel(del) }}
                          </span>
                        </div>

                        <ion-button
                          size="small"
                          [color]="driverActionColor(del)"
                          [disabled]="!driverActionable(del)"
                          (click)="goToDelivery(del.deliveryId)">
                          {{ driverActionLabel(del) }}
                        </ion-button>
                      </div>
                    </div>
                  </div>
                }
              </div>
            }
          </div>
        } @else {
          <div class="stripe-card text-center py-8">
            <p class="text-sm text-slate-500">No hay ruta descargada en el dispositivo.</p>
            <button type="button" class="mt-3 px-4 py-2 bg-[#00A7C4] hover:bg-[#008CA5] text-white text-xs font-bold rounded-full shadow-xs transition" (click)="downloadRoute()">
              Descargar Ruta de Asignación
            </button>
          </div>
        }
      }

      <!-- Espaciador para no quedar oculto por la Guía de Configuración -->
      <div class="h-28"></div>
    </ion-content>

    <!-- GUÍA DE CONFIGURACIÓN (Dock Flotante Inferior Estilo Stripe) -->
    <div class="stripe-setup-dock" [class.is-expanded]="setupExpanded()">
      <div class="setup-dock-header" (click)="toggleSetupGuide()">
        <span class="setup-dock-title">Guía de configuración</span>
        <button type="button" class="setup-dock-chevron" aria-label="Desplegar guía de configuración">
          <svg class="w-4 h-4 transform transition-transform" [class.rotate-180]="setupExpanded()" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
            <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 15.75l7.5-7.5 7.5 7.5" />
          </svg>
        </button>
      </div>

      <!-- Barra de progreso con color oficial Clean Cyan (#00A7C4) -->
      <div class="setup-dock-progress-bar">
        <div class="setup-dock-progress-fill" [style.width.%]="setupProgress()"></div>
      </div>

      <div class="setup-dock-subtitle">
        Verifica tu empresa...
      </div>

      <!-- Pasos desplegables de la guía -->
      @if (setupExpanded()) {
        <div class="setup-dock-steps mt-3 pt-3 border-t border-slate-100 space-y-2.5">
          <div class="setup-step-item done">
            <span class="step-icon done">✓</span>
            <div class="step-text">
              <strong>1. Registro en la plataforma</strong>
              <small>Credenciales y rol vinculados exitosamente</small>
            </div>
          </div>

          <div class="setup-step-item current cursor-pointer" (click)="openVerifyModal()">
            <span class="step-icon current">◉</span>
            <div class="step-text">
              <strong>2. Verifica tu empresa</strong>
              <small>Razón social y certificación de custodia digital</small>
            </div>
          </div>

          <div class="setup-step-item pending cursor-pointer" (click)="downloadRoute()">
            <span class="step-icon pending">○</span>
            <div class="step-text">
              <strong>3. Descarga tu ruta de campo</strong>
              <small>Sincroniza paradas y bultos logísticos en memoria</small>
            </div>
          </div>

          <div class="setup-step-item pending cursor-pointer" (click)="openScan()">
            <span class="step-icon pending">○</span>
            <div class="step-text">
              <strong>4. Escaneo de custodia QR offline</strong>
              <small>Verifica transferencias de bulto con firma digital</small>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      background-color: #04303B; /* Clean Cyan Dark oficial / Fondo superior */
    }

    /* --- Barra Superior: Entorno de prueba --- */
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
      background: #00A7C4; /* Clean Cyan Oficial */
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

    /* --- Contenedor Principal Blanco con Esquinas Curvadas --- */
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
      background: #EA601F; /* Energy Orange CTA oficial */
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

    /* --- Filtros en Píldoras --- */
    .stripe-pills-bar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 4px 16px 12px;
      gap: 8px;
    }

    .stripe-pills-scroll {
      display: flex;
      align-items: center;
      gap: 8px;
      overflow-x: auto;
      scrollbar-width: none;
      -ms-overflow-style: none;
    }

    .stripe-pills-scroll::-webkit-scrollbar {
      display: none;
    }

    .stripe-pill-item {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      padding: 5px 12px;
      background: #ffffff;
      border: 1px solid #E3E8EF;
      border-radius: 9999px;
      font-size: 12px;
      font-weight: 650;
      color: #425466;
      white-space: nowrap;
      cursor: pointer;
      box-shadow: 0 1px 2px rgba(10, 37, 64, 0.04);
      transition: border-color 0.15s, background-color 0.15s;
    }

    .stripe-pill-item:hover {
      background: #F8FAFC;
      border-color: #CBD5E1;
    }

    .stripe-pill-item.is-active {
      border-color: #00A7C4;
      color: #04303B;
    }

    .stripe-plus-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      flex-shrink: 0;
      background: #ffffff;
      border: 1px solid #E3E8EF;
      border-radius: 8px;
      color: #425466;
      cursor: pointer;
      box-shadow: 0 1px 2px rgba(10, 37, 64, 0.04);
      transition: all 0.15s;
    }

    .stripe-plus-btn:hover {
      background: #F8FAFC;
      border-color: #CBD5E1;
    }

    /* --- Contenido Principal --- */
    .stripe-main-content {
      --background: #F8FAFC;
      flex: 1;
    }

    /* --- Tarjeta Estilo Stripe --- */
    .stripe-card {
      background: #ffffff;
      border: 1px solid #E3E8EF;
      border-radius: 14px;
      padding: 16px;
      box-shadow: 0 1px 3px rgba(10, 37, 64, 0.04);
      transition: box-shadow 0.2s;
    }

    .stripe-card-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .stripe-card-title-group {
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .card-title-text {
      font-size: 14px;
      font-weight: 700;
      color: #0A2540;
      letter-spacing: -0.01em;
    }

    .info-icon-badge {
      font-size: 13px;
      color: #94A3B8;
      cursor: help;
    }

    .stripe-card-action-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      background: #ffffff;
      border: 1px solid #E3E8EF;
      border-radius: 8px;
      cursor: pointer;
      transition: background 0.15s;
    }

    .stripe-card-action-btn:hover {
      background: #F8FAFC;
    }

    .stripe-card-main-val {
      font-size: 26px;
      font-weight: 800;
      color: #0A2540;
      margin: 10px 0 4px;
      letter-spacing: -0.03em;
      font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
    }

    .stripe-card-sub-val {
      font-size: 13px;
      font-weight: 500;
      color: #64748B;
    }

    .stripe-tag {
      font-size: 11px;
      font-weight: 700;
      padding: 3px 8px;
      border-radius: 9999px;
      background: #E6F7FA;
      color: #008CA5;
    }

    .stripe-tag.is-warning {
      background: #FEF3C7;
      color: #92400E;
    }

    /* --- Botones de Acción de Conductor Estilo Stripe --- */
    .stripe-actions-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 10px;
    }

    .stripe-action-tile {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      padding: 14px;
      background: #ffffff;
      border: 1px solid #E3E8EF;
      border-radius: 12px;
      box-shadow: 0 1px 2px rgba(10, 37, 64, 0.04);
      cursor: pointer;
      text-align: left;
      transition: all 0.15s;
    }

    .stripe-action-tile:hover {
      background: #F8FAFC;
      border-color: #CBD5E1;
    }

    .stripe-action-tile:active {
      transform: scale(0.98);
    }

    .stripe-action-tile .tile-icon {
      margin-bottom: 8px;
    }

    .stripe-action-tile .tile-title {
      font-size: 13px;
      font-weight: 700;
      color: #0A2540;
    }

    .stripe-action-tile .tile-subtitle {
      font-size: 11px;
      color: #64748B;
      margin-top: 2px;
    }

    .stripe-pending-banner {
      display: flex;
      width: 100%;
      align-items: center;
      gap: 12px;
      padding: 12px 14px;
      text-align: left;
      background: #04303B;
      color: white;
      border-radius: 12px;
      box-shadow: 0 4px 12px -4px rgba(4, 48, 59, 0.3);
      border: none;
      cursor: pointer;
    }

    .summary-icon {
      display: grid;
      place-items: center;
      width: 32px;
      height: 32px;
      border: 1px solid rgba(255, 255, 255, 0.2);
      border-radius: 8px;
      color: #00A7C4;
      font-size: 18px;
      flex-shrink: 0;
    }

    .summary-arrow {
      margin-left: auto;
      color: #00A7C4;
      font-size: 16px;
    }

    /* --- Guía de Configuración Inferior Flotante (Stripe Setup Guide) --- */
    .stripe-setup-dock {
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      z-index: 50;
      background: #ffffff;
      border-top: 1px solid #E3E8EF;
      border-top-left-radius: 18px;
      border-top-right-radius: 18px;
      box-shadow: 0 -4px 18px rgba(10, 37, 64, 0.08);
      padding: 14px 18px max(14px, env(safe-area-inset-bottom));
      transition: all 0.25s ease-in-out;
    }

    .setup-dock-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      cursor: pointer;
    }

    .setup-dock-title {
      font-size: 14px;
      font-weight: 750;
      color: #0A2540;
      letter-spacing: -0.01em;
    }

    .setup-dock-chevron {
      background: transparent;
      border: none;
      color: #64748B;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 4px;
    }

    .setup-dock-progress-bar {
      width: 100%;
      height: 4px;
      background: #E3E8EF;
      border-radius: 9999px;
      overflow: hidden;
      margin: 10px 0 8px;
    }

    .setup-dock-progress-fill {
      height: 100%;
      background: #00A7C4; /* Clean Cyan Oficial */
      border-radius: 9999px;
      transition: width 0.3s ease;
    }

    .setup-dock-subtitle {
      font-size: 12px;
      color: #64748B;
      font-weight: 500;
    }

    .setup-step-item {
      display: flex;
      align-items: flex-start;
      gap: 10px;
      padding: 6px 0;
    }

    .setup-step-item .step-icon {
      font-size: 13px;
      font-weight: 700;
      width: 20px;
      display: flex;
      justify-content: center;
    }

    .setup-step-item .step-icon.done {
      color: #00A7C4;
    }

    .setup-step-item .step-icon.current {
      color: #EA601F;
    }

    .setup-step-item .step-icon.pending {
      color: #CBD5E1;
    }

    .setup-step-item .step-text strong {
      display: block;
      font-size: 12px;
      color: #0A2540;
    }

    .setup-step-item .step-text small {
      display: block;
      font-size: 11px;
      color: #64748B;
    }
  `]
})
export class HomePage implements OnInit {
  readonly authService = inject(MobileAuthService);
  readonly outboxService = inject(FieldOutboxService);
  private readonly fieldApi = inject<IFieldApi>(FIELD_API);
  readonly isMockApi = computed(() => this.fieldApi instanceof MockFieldApiService);
  private readonly router = inject(Router);
  private readonly alerts = inject(AlertController);

  readonly loadingRoute = signal<boolean>(false);
  readonly downloadMessage = signal<string | null>(null);

  // Estados visuales del nuevo diseño Stripe-style
  readonly searchOpen = signal<boolean>(false);
  searchQuery = '';
  readonly selectedDateRange = signal<string>('Últimos 7 días');
  readonly selectedFrequency = signal<string>('Diario');
  readonly comparePrevious = signal<boolean>(true);
  readonly setupExpanded = signal<boolean>(false);
  readonly setupProgress = signal<number>(45);

  // --- Estado para el Receptor Hospitalario ---
  qrInputText = '';
  readonly receiverActiveBox = signal<ReceiverUnit | null>(null);
  readonly receiverQr = signal<{ unitId: string; token: string } | null>(null);
  readonly receiverLoading = signal<boolean>(false);
  readonly receiverError = signal<string | null>(null);
  readonly receiverCached = signal(false);
  readonly receiverSaved = signal(false);
  readonly receiverPhoto = signal<File | null>(null);
  readonly receiverConfirmedMessage = signal<string | null>(null);
  readonly receiverReasons = RECEIPT_REJECTION_REASONS;
  receiverLines: (ReceiverUnit['lines'][number] & {
    quantityAccepted: number;
    rejectionReason: string;
    remainingQuantity: number | null;
    nextRequestedQuantity: number | null;
  })[] = [];

  readonly pendingCount = this.outboxService.pendingCount;

  readonly totalDeliveries = computed(() => {
    const a = this.outboxService.activeAssignment();
    if (!a || !a.routes) return 0;
    return a.routes.reduce((sum: number, r) => sum + r.deliveries.length, 0);
  });

  readonly filteredRoutes = computed(() => {
    const a = this.outboxService.activeAssignment();
    if (!a || !a.routes) return [];
    const query = this.searchQuery.trim().toLowerCase();
    if (!query) return a.routes;
    return a.routes.map(r => ({
      ...r,
      deliveries: r.deliveries.filter(d =>
        d.pointName.toLowerCase().includes(query) ||
        d.pointCode.toLowerCase().includes(query) ||
        d.address.toLowerCase().includes(query) ||
        d.deliveryId.toLowerCase().includes(query)
      )
    }));
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

  constructor() {
    let accountId = this.authService.me?.()?.accountId ?? null;
    effect(() => {
      const nextAccountId = this.authService.me?.()?.accountId ?? null;
      if (nextAccountId === accountId) return;
      accountId = nextAccountId;
      this.qrInputText = '';
      this.receiverQr.set(null);
      this.receiverActiveBox.set(null);
      this.receiverPhoto.set(null);
      this.receiverLines = [];
      this.receiverSaved.set(false);
      this.receiverCached.set(false);
      this.receiverError.set(null);
      this.receiverConfirmedMessage.set(null);
    });
  }

  async ngOnInit(): Promise<void> {
    // Si es conductor y no hay ruta en caché, intentar descargarla
    if (this.authService.currentRole() === 'Conductor' && !this.outboxService.activeAssignment()) {
      await this.downloadRoute();
    }
  }

  ionViewWillEnter(): void {
    const qr = history.state?.receiverQr;
    if (this.authService.currentRole() === 'Receptor' && qr?.unitId && qr?.token) {
      void this.loadReceiverUnit(qr.unitId, qr.token);
    }
  }

  // --- Métodos de Interacción del Nuevo Diseño ---
  toggleSearch(): void {
    this.searchOpen.set(!this.searchOpen());
  }

  cycleDateRange(): void {
    const ranges = ['Últimos 7 días', 'Últimos 30 días', 'Hoy', 'Este mes'];
    const idx = ranges.indexOf(this.selectedDateRange());
    this.selectedDateRange.set(ranges[(idx + 1) % ranges.length]);
  }

  cycleFrequency(): void {
    const freqs = ['Diario', 'Semanal', 'Mensual'];
    const idx = freqs.indexOf(this.selectedFrequency());
    this.selectedFrequency.set(freqs[(idx + 1) % freqs.length]);
  }

  toggleComparePrevious(): void {
    this.comparePrevious.set(!this.comparePrevious());
  }

  toggleSetupGuide(): void {
    this.setupExpanded.set(!this.setupExpanded());
  }

  async openTestEnvInfo(): Promise<void> {
    const alert = await this.alerts.create({
      header: 'Entorno de prueba activo',
      subHeader: 'Expo Clean Ecuador 2026',
      message: 'Estás operando en el modo seguro de validación de campo. Las operaciones offline se concilian mediante firma y código QR trazable.',
      buttons: ['Entendido']
    });
    await alert.present();
  }

  async openVerifyModal(): Promise<void> {
    const alert = await this.alerts.create({
      header: 'Verificación de Empresa',
      subHeader: 'Certificación de Custodia Digital',
      message: 'Empresa: IstpetDev Operaciones Nacionales\\nRUC: 1793148291001\\nEstado: Validación en proceso (45%)\\nProtocolo de custodia digital activo con claves M2M.',
      buttons: ['Aceptar']
    });
    await alert.present();
  }

  async openMenuAlert(): Promise<void> {
    const alert = await this.alerts.create({
      header: this.authService.me()?.displayName || 'Alexander Martinez',
      subHeader: `Rol: ${this.roleSubtitle() || 'Operador de Campo'}`,
      buttons: [
        { text: 'Escanear QR de Custodia', handler: () => this.openScan() },
        { text: 'Cola de Sincronización', handler: () => this.openSyncQueue() },
        { text: 'Cerrar Sesión', role: 'destructive', handler: () => this.logout() },
        { text: 'Cancelar', role: 'cancel' }
      ]
    });
    await alert.present();
  }

  async openQuickAction(): Promise<void> {
    const alert = await this.alerts.create({
      header: 'Acción Rápida',
      buttons: [
        { text: 'Escanear QR', handler: () => this.openScan() },
        { text: 'Descargar / Actualizar Ruta', handler: () => this.downloadRoute() },
        { text: 'Ver Cola Pendientes', handler: () => this.openSyncQueue() },
        { text: 'Cancelar', role: 'cancel' }
      ]
    });
    await alert.present();
  }

  async inspectVolumeDetails(): Promise<void> {
    const alert = await this.alerts.create({
      header: 'Detalle de Volumen Bruto',
      subHeader: this.selectedDateRange(),
      message: `Volumen actual: 0,00 US$\\nPeríodo anterior: 0,00 US$\\nEntregas activas: ${this.totalDeliveries()}\\nEstado del inventario: En regla`,
      buttons: ['Cerrar']
    });
    await alert.present();
  }

  // --- Métodos de Receptor Hospitalario ---
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
    this.receiverActiveBox.set(null);
    this.receiverPhoto.set(null);
    this.receiverSaved.set(false);
    this.receiverCached.set(false);
    this.receiverLines = [];

    try {
      const { unit, cached } = await this.outboxService.loadReceiverUnit(unitId, token);
      this.receiverCached.set(cached);
      this.receiverActiveBox.set(unit);
      this.receiverLines = unit.lines.map(line => ({
        ...line,
        quantityAccepted: line.quantity,
        rejectionReason: '',
        remainingQuantity: null,
        nextRequestedQuantity: null
      }));
      const stateMessage = this.receiverStateMessage(unit.status);
      if (stateMessage) {
        this.receiverError.set(stateMessage);
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
      } else {
        this.receiverError.set(err instanceof Error ? err.message : 'QR no válido o vencido');
      }
    } finally {
      this.receiverLoading.set(false);
    }
  }

  onReceiverPhoto(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.receiverPhoto.set(null);
    if (file && (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 10 * 1024 * 1024 || file.size === 0)) {
      this.receiverError.set('Selecciona una imagen JPEG, PNG o WebP de hasta 10 MB.');
      return;
    }
    this.receiverError.set(null);
    this.receiverPhoto.set(file);
  }

  async confirmReceiverAction(): Promise<void> {
    const box = this.receiverActiveBox();
    const qr = this.receiverQr();
    if (!box || !qr || box.status !== 'Delivered' || this.receiverLoading() || this.receiverSaved()) return;
    this.receiverError.set(null);
    if (!this.receiverLines.length || this.receiverLines.some(l =>
      !Number.isFinite(l.quantityAccepted) || l.quantityAccepted < 0 || l.quantityAccepted > l.quantity ||
      [l.remainingQuantity, l.nextRequestedQuantity].some(q => q !== null && (!Number.isFinite(q) || q < 0)))) {
      this.receiverError.set('Revisa las cantidades: no pueden ser negativas ni superar lo enviado.');
      return;
    }
    if (this.receiverLines.some(l => l.quantityAccepted < l.quantity && !this.receiverReasons.some(r => r.code === l.rejectionReason))) {
      this.receiverError.set('Selecciona un motivo de catálogo por cada línea rechazada.');
      return;
    }
    if (this.receiverLines.some(l => l.quantityAccepted < l.quantity) && !this.receiverPhoto()) {
      this.receiverError.set('Adjunta una foto cuando hay rechazo.');
      return;
    }
    this.receiverLoading.set(true);
    try {
      await this.outboxService.saveReceiptAtomic({
        deliveryId: box.deliveryId,
        expectedDeliveryVersion: box.deliveryVersion,
        recipientName: this.authService.me()?.displayName ?? 'Receptor',
        receiverContext: { unitId: qr.unitId, traceToken: qr.token },
        lines: this.receiverLines.map(l => ({
          deliveryLineId: l.deliveryLineId,
          lotId: l.lotId,
          unit: l.unit,
          quantityAccepted: l.quantityAccepted,
          quantityRejected: Number((l.quantity - l.quantityAccepted).toFixed(3)),
          rejectionReason: l.quantityAccepted < l.quantity ? l.rejectionReason : undefined,
          remainingQuantity: l.remainingQuantity ?? undefined,
          nextRequestedQuantity: l.nextRequestedQuantity ?? undefined
        })),
        photoBlob: this.receiverPhoto() ?? undefined
      });
      this.receiverSaved.set(true);
      this.receiverConfirmedMessage.set('Guardado en este teléfono para tu cuenta. Consulta la cola para ver la confirmación del servidor y el envío de la foto.');
    } catch (error) {
      this.receiverError.set(error instanceof Error ? error.message : 'No se pudo guardar la recepción.');
    } finally {
      this.receiverLoading.set(false);
    }
  }

  resetReceiverScreen(): void {
    this.qrInputText = '';
    this.receiverActiveBox.set(null);
    this.receiverQr.set(null);
    this.receiverLines = [];
    this.receiverPhoto.set(null);
    this.receiverError.set(null);
    this.receiverSaved.set(false);
    this.receiverConfirmedMessage.set(null);
    this.receiverCached.set(false);
  }

  private receiverStateMessage(status: string): string | null {
    switch (status) {
      case 'Delivered':
        return null;
      case 'Received':
      case 'Partial':
        return 'Este bulto ya fue recibido.';
      case 'Sealed':
      case 'Prepared':
        return 'El bulto aún está en bodega. El Conductor aún no entrega este bulto.';
      default:
        return 'El Conductor aún no entrega este bulto.';
    }
  }

  // --- Métodos de Conductor ---
  private driverStage(del: MobileAssignmentDelivery): string {
    const status = this.outboxService.effectiveDeliveryStatus(
      del.deliveryId,
      del.status,
      del.handlingUnits.map((hu) => hu.status),
    );
    if (status === 'Dispatched') {
      const unitsReady = del.handlingUnits.length > 0 && del.handlingUnits.every((hu) => hu.status === 'Dispatched');
      return unitsReady ? 'Dispatched' : 'WaitingWarehouse';
    }
    return status;
  }

  driverStageLabel(del: MobileAssignmentDelivery): string {
    switch (this.driverStage(del)) {
      case 'WaitingWarehouse': return 'Esperando a Bodega';
      case 'Draft': return 'En preparación';
      case 'Dispatched': return 'Listo para recoger';
      case 'InTransit': return 'En ruta';
      case 'Delivered': return 'Entregado';
      case 'Received': return 'Recibido';
      case 'Partial': return 'Recibido parcialmente';
      default: return del.status;
    }
  }

  driverActionable(del: MobileAssignmentDelivery): boolean {
    const stage = this.driverStage(del);
    return stage === 'Dispatched' || stage === 'InTransit';
  }

  driverActionLabel(del: MobileAssignmentDelivery): string {
    switch (this.driverStage(del)) {
      case 'WaitingWarehouse': return 'Esperando a Bodega';
      case 'Dispatched': return 'Recoger →';
      case 'InTransit': return 'Entregar →';
      case 'Delivered': return 'Esperando receptor';
      case 'Received':
      case 'Partial': return 'Recibido';
      default: return 'Abrir →';
    }
  }

  driverActionColor(del: MobileAssignmentDelivery): string {
    switch (this.driverStage(del)) {
      case 'Dispatched': return 'warning';
      case 'InTransit': return 'primary';
      case 'Delivered':
      case 'Received':
      case 'Partial': return 'success';
      default: return 'medium';
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
