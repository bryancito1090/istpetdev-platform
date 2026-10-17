import { Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import {
  IonContent,
  AlertController,
  ToastController
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import { logOutOutline, mapOutline, syncOutline } from 'ionicons/icons';
import { RECEIPT_REJECTION_REASONS, MobileAssignmentDelivery } from '@istpetdev/shared-core';
import { FIELD_ROLE_LABELS, MobileAuthService } from '../auth';
import { FieldOutboxService } from '../field/outbox/field-outbox.service';
import { FIELD_API, IFieldApi, ReceiverUnit } from '../field/field-api.interface';
import { parseUnitQr } from '../field/unit-qr';
import { MockFieldApiService } from '../field/mock/mock-field-api.service';
import { WarehouseComponent } from '../field/pages/warehouse/warehouse.component';
import { FieldPageHeaderComponent } from '../shared/field-page-header/field-page-header.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IonContent,
    WarehouseComponent,
    FieldPageHeaderComponent
  ],
  template: `
    <app-field-page-header
      [home]="true"
      [userName]="authService.me()?.displayName || 'Operaciones de campo'"
      [pendingCount]="pendingCount()"
      (help)="openHelpGuide()"
      (menu)="openMenuAlert()"
      (sync)="openSyncQueue()"
      (search)="toggleSearch()" />

    @if (helpGuideOpen()) {
      <div class="help-sheet-backdrop" (click)="closeHelpGuide()" (keydown.escape)="closeHelpGuide()">
        <section class="help-sheet" role="dialog" aria-modal="true" aria-labelledby="help-sheet-title" (click)="$event.stopPropagation()">
          <div class="help-sheet-handle" aria-hidden="true"></div>
          <header class="help-sheet-header">
            <div>
              <span class="help-sheet-eyebrow">ISTET DEV · GUÍA RÁPIDA</span>
              <h2 id="help-sheet-title">Tus funciones en el sistema</h2>
              <p>Guía para el rol {{ roleSubtitle() || 'Operador de Campo' }}</p>
            </div>
            <button type="button" class="help-sheet-close" (click)="closeHelpGuide()" aria-label="Cerrar guía">×</button>
          </header>

          @if (authService.currentRole() === 'Conductor') {
            <div class="help-sheet-content">
              <article class="help-guide-card">
                <span class="help-step">01</span><div><h3>Consulta tu ruta</h3><p>Revisa las paradas, destinos y entregas asignadas en Inicio. Usa la búsqueda para encontrar una parada o código.</p></div>
              </article>
              <article class="help-guide-card">
                <span class="help-step">02</span><div><h3>Registra la custodia</h3><p>En cada parada, escanea el QR del bulto para registrar la carga o entrega. Confirma los productos y las cantidades antes de continuar.</p></div>
              </article>
              <article class="help-guide-card">
                <span class="help-step">03</span><div><h3>Confirma y sincroniza</h3><p>Guarda la entrega y añade evidencia si se solicita. Sin conexión, los registros quedan en el teléfono y se envían al recuperar señal; puedes revisar su estado en Notificaciones.</p></div>
              </article>
            </div>
          } @else if (authService.currentRole() === 'Bodega') {
            <div class="help-sheet-content">
              <article class="help-guide-card">
                <span class="help-step">01</span><div><h3>Abre la preparación</h3><p>Desde Inicio, entra a la entrega o preparación asignada para consultar destino, productos, lotes y cantidades requeridas.</p></div>
              </article>
              <article class="help-guide-card">
                <span class="help-step">02</span><div><h3>Verifica físicamente</h3><p>Comprueba cada producto y lote contra la lista, marca las líneas verificadas y prepara los bultos. Registra cualquier diferencia antes de cerrar.</p></div>
              </article>
              <article class="help-guide-card">
                <span class="help-step">03</span><div><h3>Identifica y entrega</h3><p>Genera o escanea los códigos de custodia para que el conductor pueda validar los bultos. Consulta Notificaciones si hay operaciones pendientes.</p></div>
              </article>
            </div>
          } @else if (authService.currentRole() === 'Receptor') {
            <div class="help-sheet-content">
              <article class="help-guide-card">
                <span class="help-step">01</span><div><h3>Busca el envío</h3><p>Escanea el QR de custodia con la cámara o pega el enlace o código del bulto en el campo de búsqueda.</p></div>
              </article>
              <article class="help-guide-card">
                <span class="help-step">02</span><div><h3>Valida lo recibido</h3><p>Compara productos, lotes y cantidades con la entrega física. Marca cada línea conforme; si hay diferencias, indícalas y registra el motivo.</p></div>
              </article>
              <article class="help-guide-card">
                <span class="help-step">03</span><div><h3>Confirma la recepción</h3><p>Revisa el resumen y guarda la recepción para dejar constancia de la custodia. Si no hay señal, la captura se conserva y se sincroniza después.</p></div>
              </article>
            </div>
          } @else {
            <div class="help-sheet-content">
              <article class="help-guide-card"><span class="help-step">i</span><div><h3>Funciones según tu acceso</h3><p>El sistema muestra tareas y acciones según tu rol. En Inicio puedes consultar las actividades disponibles y abrir Notificaciones para revisar operaciones pendientes.</p></div></article>
            </div>
          }

          <footer class="help-sheet-footer">Los cambios y registros realizados quedan asociados a tu sesión y al flujo de custodia.</footer>
        </section>
      </div>
    }

    <!-- Barra de búsqueda desplegable compartida con la cabecera móvil -->
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

      <!-- Botón Simulador Demo (visible solo si FieldSimulation:Enabled está activo) -->
      @if (demoAvailable()) {
        <div class="mb-3 p-3 bg-gradient-to-r from-teal-50 to-cyan-50 border border-teal-200 rounded-xl flex items-center justify-between shadow-xs">
          <div class="flex items-center gap-2.5">
            <span class="w-2.5 h-2.5 rounded-full bg-[#00A7C4] animate-pulse"></span>
            <div>
              <span class="font-bold text-xs text-[#04303B] block">Simulador de Campo (Demo)</span>
              <span class="text-[10px] text-slate-600">Ejecutar rutas publicadas del día sin labor física</span>
            </div>
          </div>
          <button
            type="button"
            (click)="openDemoSimulation()"
            class="px-3 py-1.5 bg-[#00A7C4] hover:bg-[#008CA5] text-white text-xs font-bold rounded-lg shadow-xs transition">
            Abrir Demo →
          </button>
        </div>
      }

      <!-- ======================================================== -->
      <!-- CASO 1: RECEPTOR HOSPITALARIO (Solo Escanear QR y Novedades) -->
      <!-- ======================================================== -->
      @if (authService.currentRole() === 'Receptor') {
        <div class="driver-stop-card mb-4">
          <div class="flex items-center justify-between mb-1.5">
            <span class="stop-tag">Recepción de Insumos</span>
            <span class="status-badge">Punto de distribución</span>
          </div>
          <h2 class="stop-title">Recepción y Validación</h2>
          <p class="stop-address">Escanea el código QR del bulto o ingresa el enlace de custodia recibido.</p>

          <div class="space-y-3 pt-2">
            <button type="button" class="stop-action-btn" (click)="openReceiverScan()">
              Escanear QR con cámara
            </button>

            <div class="space-y-1.5 pt-2 border-t border-slate-100">
              <label class="block text-xs font-semibold text-slate-700">Enlace o código de bulto:
                <input
                  type="text"
                  [(ngModel)]="qrInputText"
                  placeholder="Pega el enlace del bulto o UUID"
                  class="w-full mt-1 px-3 py-2 text-sm border border-slate-200 rounded-lg focus:outline-none focus:border-[#00A7C4]" />
              </label>
              <button
                type="button"
                (click)="searchReceiverUnit()"
                [disabled]="receiverLoading() || !qrInputText.trim()"
                class="w-full py-2 px-3 text-xs font-bold text-[#0A2540] bg-slate-100 hover:bg-slate-200 rounded-lg transition disabled:opacity-50">
                {{ receiverLoading() ? 'Buscando...' : 'Buscar bulto' }}
              </button>
            </div>

            @if (receiverError()) {
              <div role="alert" class="p-3 bg-rose-50 border border-rose-200 rounded-lg text-xs text-rose-800 font-medium">
                {{ receiverError() }}
              </div>
            }
            @if (receiverCached()) {
              <div class="p-2.5 bg-amber-50 border border-amber-200 rounded-lg text-xs text-amber-800">
                Última consulta guardada. La recepción se validará al recuperar señal.
              </div>
            }
          </div>
        </div>

        @if (receiverActiveBox(); as box) {
          <div class="driver-stop-card mb-4">
            <div class="flex items-center justify-between mb-1.5">
              <span class="stop-tag">Bulto: {{ box.code }}</span>
              <span class="status-badge is-done">
                {{ box.boxes.length }} bulto(s) en entrega
              </span>
            </div>
            <h2 class="stop-title">{{ box.destinationName }}</h2>
            <p class="stop-address">
              Recepción por entrega completa ({{ box.boxes.map(b => b.code).join(', ') }}). Confirma las líneas recibidas.
            </p>

            <div class="stop-products-section">
              <div class="flex items-center justify-between mb-2">
                <span class="stop-products-title">Líneas a Recibir (Doble Validación)</span>
                <button type="button" class="text-xs font-bold text-[#00A7C4] hover:underline"
                        (click)="markAllReceiverLinesConform(true)">
                  Marcar todos conformes
                </button>
              </div>

              <div class="space-y-2">
                @for (line of receiverLines; track line.deliveryLineId) {
                  <div class="p-3 rounded-lg border transition select-none"
                       [class.bg-emerald-50]="line.isConform !== false"
                       [class.border-emerald-200]="line.isConform !== false"
                       [class.bg-slate-50]="line.isConform === false"
                       [class.border-slate-200]="line.isConform === false">
                    <div class="flex items-start justify-between gap-2">
                      <div class="flex items-start gap-2.5">
                        <input type="checkbox"
                               class="mt-0.5 w-4 h-4 text-emerald-600 rounded border-slate-300 focus:ring-emerald-500 cursor-pointer"
                               [checked]="line.isConform !== false"
                               (change)="line.isConform = !line.isConform; onConformToggle(line)" />
                        <div>
                          <span class="font-bold text-sm text-slate-900 block">{{ line.productName }}</span>
                          <span class="text-xs text-slate-600 font-mono">
                            Lote: <strong class="text-slate-800">{{ line.lotCode }}</strong>
                          </span>
                        </div>
                      </div>
                      <div class="text-right">
                        <span class="product-qty block mb-1">{{ line.quantity }} {{ line.unit }}</span>
                        <span class="text-[10px] font-bold px-1.5 py-0.5 rounded"
                              [class.bg-emerald-200]="line.isConform !== false"
                              [class.text-emerald-900]="line.isConform !== false"
                              [class.bg-amber-200]="line.isConform === false"
                              [class.text-amber-900]="line.isConform === false">
                          {{ line.isConform !== false ? 'Conforme' : 'Con Novedad' }}
                        </span>
                      </div>
                    </div>

                    @if (line.isConform === false) {
                      <div class="pt-2 mt-2 border-t border-slate-200 space-y-2 text-xs">
                        <label class="block font-semibold text-slate-700">Cantidad aceptada
                          <input class="w-full border border-slate-300 rounded p-1.5 text-xs mt-1" type="number" min="0" [max]="line.quantity" step="0.001"
                            [(ngModel)]="line.quantityAccepted" />
                        </label>
                        <p class="text-rose-700 font-semibold">Rechazadas: {{ line.quantity - line.quantityAccepted }} {{ line.unit }}</p>
                        @if (line.quantityAccepted < line.quantity) {
                          <label class="block font-semibold text-slate-700">Motivo del rechazo *
                            <select class="w-full border border-slate-300 rounded p-1.5 text-xs mt-1" [(ngModel)]="line.rejectionReason">
                              <option value="">Selecciona un motivo</option>
                              @for (reason of receiverReasons; track reason.code) {
                                <option [value]="reason.code">{{ reason.label }}</option>
                              }
                            </select>
                          </label>
                        }
                      </div>
                    }

                    <div class="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-2 mt-2 border-t border-slate-200/60 text-xs">
                      <label class="block text-slate-600">Stock en percha antes de recibir ({{ line.unit }})
                        <input class="w-full border border-slate-300 rounded p-1 text-xs mt-0.5" type="number" min="0" step="0.001" [(ngModel)]="line.remainingQuantity" />
                      </label>
                      <label class="block text-slate-600">Solicitado para reabastecimiento ({{ line.unit }})
                        <input class="w-full border border-slate-300 rounded p-1 text-xs mt-0.5" type="number" min="0" step="0.001" [(ngModel)]="line.nextRequestedQuantity" />
                      </label>
                    </div>
                  </div>
                }
              </div>
            </div>

            <div class="space-y-3 pt-2">
              <label class="block text-xs font-semibold text-slate-700">
                Foto de la novedad (obligatoria si rechazas unidades)
                <input class="w-full mt-1 text-xs" type="file" accept="image/jpeg,image/png,image/webp" capture="environment" (change)="onReceiverPhoto($event)" />
              </label>
              @if (receiverPhoto(); as photo) {
                <p class="text-xs text-slate-600 font-medium">Foto adjunta: {{ photo.name }}</p>
              }

              <button
                type="button"
                class="stop-action-btn"
                (click)="confirmReceiverAction()"
                [disabled]="receiverLoading() || receiverSaved() || box.status !== 'Delivered'">
                {{ receiverLoading() ? 'Guardando...' : 'Guardar recepción de la entrega completa' }}
              </button>
            </div>
          </div>
        }

        @if (receiverConfirmedMessage()) {
          <div class="driver-stop-card mb-4 bg-emerald-50 border-emerald-200 text-xs space-y-2" role="status">
            <div class="flex items-center gap-1.5 font-bold text-emerald-900 text-sm">
              <span>Recepción guardada</span>
            </div>
            <p class="text-emerald-800 leading-relaxed">{{ receiverConfirmedMessage() }}</p>
            <div class="flex flex-col sm:flex-row gap-2 pt-2">
              <button
                type="button"
                class="flex-1 py-2 px-3 text-xs font-bold text-white bg-[#00A7C4] hover:bg-[#008CA5] rounded-lg transition"
                (click)="resetReceiverScreen()">
                Registrar otro bulto / Limpiar
              </button>
              <button
                type="button"
                class="flex-1 py-2 px-3 text-xs font-bold text-[#0A2540] bg-white border border-slate-200 hover:bg-slate-50 rounded-lg transition"
                (click)="openSyncQueue()">
                Ver cola de sincronización
              </button>
            </div>
          </div>
        }
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
        @if (shouldShowDriverNotice(); as notice) {
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

        <!-- Acciones del Conductor: Únicos botones requeridos -->
        <div class="stripe-actions-grid mb-4">
          @if (!isRouteCompleted()) {
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
          } @else {
            <div class="stripe-action-tile opacity-75 bg-slate-50 border-slate-200">
              <div class="tile-icon text-emerald-600 font-bold">✓</div>
              <span class="tile-title text-slate-800">Ruta Cerrada</span>
              <span class="tile-subtitle">Entregas completadas</span>
            </div>
          }

          <button type="button" class="stripe-action-tile" [disabled]="loadingRoute()" (click)="downloadRoute()">
            <div class="tile-icon">
              <svg class="w-5 h-5 text-[#00A7C4]" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" />
              </svg>
            </div>
            <span class="tile-title">
              {{ loadingRoute() ? (hasDownloadedRoute() ? 'Actualizando...' : 'Cargando...') : (hasDownloadedRoute() ? 'Actualizar Ruta' : 'Descargar Ruta') }}
            </span>
            <span class="tile-subtitle">
              {{ hasDownloadedRoute() ? 'Sincronizar cambios' : 'Asignación local' }}
            </span>
          </button>
        </div>

        @if (outboxService.activeAssignment(); as assignment) {
          <!-- Lista de Puntos de Distribución Asignados -->
          <div class="mb-2.5 flex items-center justify-between px-1">
            <span class="text-xs font-bold uppercase tracking-wider text-slate-700">
              Puntos de distribución de la ruta activa
            </span>
            <span class="text-[11px] text-slate-500 font-bold bg-slate-100 px-2 py-0.5 rounded-full">
              {{ totalDeliveries() }} entregas
            </span>
          </div>

          <div class="space-y-3">
            @for (route of filteredRoutes(); track route.routePlanId) {
              <div class="space-y-3">
                <div class="flex items-center justify-between text-xs text-slate-500 font-mono px-1">
                  <span>Ruta: {{ route.routePlanId }} (Rev {{ route.revision }})</span>
                  <span>Vehículo: {{ $any(route).vehicleDescription || $any(route).vehiclePlate || route.vehicleCode }}</span>
                </div>

                @for (pickup of route.pickupStops ?? []; track pickup.stopId) {
                  <div class="driver-stop-card cursor-pointer" (click)="openScan('pickup')">
                    <div class="flex items-center justify-between mb-1.5">
                      <span class="stop-tag">Parada #{{ pickup.sequence }} · Recogida en Bodega</span>
                      <span class="text-xs font-bold text-[#00A7C4] underline">Escanear y cargar caja →</span>
                    </div>
                    <h2 class="stop-title">{{ pickup.pointName }}</h2>
                    @if (pickup.address) {
                      <p class="stop-address">{{ pickup.address }}</p>
                    }
                    <div class="text-xs text-slate-600 font-medium">
                      Toca aquí para escanear y certificar las cajas que subes al vehículo.
                    </div>
                  </div>
                }

                @for (del of route.deliveries; track del.deliveryId) {
                  @if (del.kind === 'Pickup') {
                    <div class="driver-stop-card cursor-pointer" (click)="openScan('pickup')">
                      <div class="flex items-center justify-between mb-1.5">
                        <span class="stop-tag">Parada #{{ del.sequence }} · Recogida en Bodega</span>
                        <span class="text-xs font-bold text-[#00A7C4] underline">Escanear y cargar caja →</span>
                      </div>
                      <h2 class="stop-title">{{ del.pointName }}</h2>
                      @if (del.address) {
                        <p class="stop-address">{{ del.address }}</p>
                      }
                      @if (getDeliveryProducts(del).length > 0) {
                        <div class="stop-products-section">
                          <span class="stop-products-title">Cajas y productos a cargar:</span>
                          <div class="space-y-1.5">
                            @for (prod of getDeliveryProducts(del); track prod.name) {
                              <div class="product-row">
                                <span class="product-name">{{ prod.name }}</span>
                                <span class="product-qty">{{ prod.quantity }} {{ prod.unit }}</span>
                              </div>
                            }
                          </div>
                        </div>
                      }
                      <div class="text-xs text-slate-600 font-medium">
                        Toca aquí para escanear y certificar las cajas que subes al vehículo.
                      </div>
                    </div>
                  } @else {
                    <div class="driver-stop-card">
                      <div class="flex items-center justify-between mb-2">
                        <span class="stop-tag">Parada #{{ del.sequence }} · Entrega en Punto de Distribución</span>
                        <span class="status-badge" [class.is-done]="driverStage(del) === 'Received' || driverStage(del) === 'Partial'">
                          {{ driverStageLabel(del) }}
                        </span>
                      </div>

                      <!-- Nombre del lugar (Prominente, limpio, sin iconos) -->
                      <h2 class="stop-title">{{ del.pointName }}</h2>
                      @if (del.address) {
                        <p class="stop-address">{{ del.address }}</p>
                      }

                      <!-- Productos a entregar con cantidad y nombre -->
                      <div class="stop-products-section">
                        <span class="stop-products-title">Productos a entregar:</span>
                        @if (getDeliveryProducts(del).length > 0) {
                          <div class="space-y-1.5">
                            @for (prod of getDeliveryProducts(del); track prod.name) {
                              <div class="product-row">
                                <span class="product-name">{{ prod.name }}</span>
                                <span class="product-qty">{{ prod.quantity }} {{ prod.unit }}</span>
                              </div>
                            }
                          </div>
                        } @else {
                          <p class="text-xs text-slate-500 italic">
                            {{ del.handlingUnits.length > 0 ? (del.handlingUnits.length + ' bulto(s) asignados') : 'Sin bultos asignados' }}
                          </p>
                        }
                      </div>

                      <!-- Botón de acción al pie de la tarjeta sin cajas anidadas -->
                      <div class="stop-card-footer">
                        <button
                          type="button"
                          class="stop-action-btn"
                          [disabled]="!driverActionable(del)"
                          (click)="goToDelivery(del.deliveryId)">
                          {{ driverActionLabel(del) }}
                        </button>
                      </div>
                    </div>
                  }
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

      <!-- Espacio inferior normal -->
      <div class="h-6"></div>
    </ion-content>
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

    .help-trigger {
      width: 40px;
      height: 40px;
      padding: 10px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      background: transparent;
      border: 0;
      border-radius: 0;
      box-shadow: none;
      color: #ffffff;
    }

    .help-trigger svg { width: 21px; height: 21px; }
    .help-trigger:hover { background: transparent; }

    .help-sheet-backdrop {
      position: fixed;
      inset: 0;
      z-index: 1000;
      display: flex;
      align-items: flex-end;
      background: rgba(3, 20, 32, 0.52);
      animation: help-backdrop-in 160ms ease-out;
    }

    .help-sheet {
      width: 100%;
      max-height: min(86vh, 760px);
      overflow: auto;
      padding: 10px 20px calc(20px + env(safe-area-inset-bottom));
      background: #fff;
      border-radius: 22px 22px 0 0;
      color: #0A2540;
      box-shadow: 0 -12px 40px rgba(3, 20, 32, 0.2);
      animation: help-sheet-in 220ms cubic-bezier(.2,.8,.2,1);
    }

    .help-sheet-handle { width: 38px; height: 4px; margin: 0 auto 18px; border-radius: 999px; background: #CBD5E1; }
    .help-sheet-header { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; padding-bottom: 18px; }
    .help-sheet-eyebrow { color: #008CA5; font-size: 10px; font-weight: 800; letter-spacing: .1em; }
    .help-sheet-header h2 { margin: 5px 0 3px; font-size: 21px; line-height: 1.2; font-weight: 800; letter-spacing: -.03em; }
    .help-sheet-header p { margin: 0; color: #64748B; font-size: 13px; }
    .help-sheet-close { flex: 0 0 34px; width: 34px; height: 34px; border: 0; border-radius: 50%; background: #F1F5F9; color: #334155; font-size: 25px; line-height: 1; cursor: pointer; }
    .help-sheet-content { display: grid; gap: 10px; }
    .help-guide-card { display: grid; grid-template-columns: 30px 1fr; align-items: start; gap: 12px; padding: 14px; border: 1px solid #E3E8EF; border-radius: 13px; background: #fff; }
    .help-step { display: grid; place-items: center; width: 28px; height: 28px; border-radius: 9px; background: #E6F8FB; color: #007F96; font-size: 12px; font-weight: 800; }
    .help-guide-card h3 { margin: 1px 0 4px; color: #0A2540; font-size: 14px; font-weight: 750; }
    .help-guide-card p { margin: 0; color: #526174; font-size: 12px; line-height: 1.55; }
    .help-sheet-footer { margin-top: 14px; padding: 12px 2px 0; color: #64748B; font-size: 11px; line-height: 1.5; }

    @keyframes help-sheet-in { from { transform: translateY(30px); opacity: .7; } to { transform: translateY(0); opacity: 1; } }
    @keyframes help-backdrop-in { from { opacity: 0; } to { opacity: 1; } }

    @media (prefers-reduced-motion: reduce) {
      .help-sheet-backdrop, .help-sheet { animation: none; }
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
    /* --- Tarjetas de Parada y Entrega para el Conductor (Sin cajas dentro de cajas) --- */
    .driver-stop-card {
      background: #ffffff;
      border: 1px solid #E3E8EF;
      border-radius: 14px;
      padding: 16px;
      box-shadow: 0 1px 3px rgba(10, 37, 64, 0.05);
      transition: all 0.15s ease-in-out;
    }

    .driver-stop-card.is-pickup {
      border-left: 4px solid #EA601F;
      background: #FFFDFB;
    }

    .stop-tag {
      font-size: 11px;
      font-weight: 750;
      color: #64748B;
      text-transform: uppercase;
      letter-spacing: 0.03em;
    }

    .stop-tag.is-pickup {
      color: #C2410C;
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

    .stop-card-footer {
      display: flex;
      align-items: center;
      justify-content: flex-end;
    }

    .stop-action-btn {
      width: 100%;
      padding: 10px 16px;
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
export class HomePage {
  readonly authService = inject(MobileAuthService);
  readonly outboxService = inject(FieldOutboxService);
  private readonly fieldApi = inject<IFieldApi>(FIELD_API);
  readonly isMockApi = computed(() => this.fieldApi instanceof MockFieldApiService);
  private readonly router = inject(Router);
  private readonly alerts = inject(AlertController);
  private readonly toastController = inject(ToastController);

  readonly loadingRoute = signal<boolean>(false);
  readonly showAssignedRoute = signal(false);
  readonly demoAvailable = signal<boolean>(false);

  // Estados visuales
  readonly searchOpen = signal<boolean>(false);
  searchQuery = '';

  readonly hasDownloadedRoute = computed(() => {
    const a = this.outboxService.activeAssignment();
    return !!(a && a.routes && a.routes.length > 0);
  });

  readonly isRouteCompleted = computed(() => {
    const a = this.outboxService.activeAssignment();
    if (!a || !a.routes || a.routes.length === 0) return false;
    const allDeliveries = a.routes.flatMap((r) => r.deliveries || []);
    if (allDeliveries.length === 0) return false;
    return allDeliveries.every((d) => {
      const stage = this.driverStage(d);
      return stage === 'Received' || stage === 'Partial';
    });
  });

  readonly shouldShowDriverNotice = computed(() => {
    const notice = this.outboxService.activeDriverNotice();
    if (!notice) return null;

    const lastDownload = this.outboxService.lastDownloadTimestamp ? this.outboxService.lastDownloadTimestamp() : 0;
    if (!lastDownload) {
      return notice;
    }

    const now = new Date();
    const downloadDate = new Date(lastDownload);

    // Más de 12 horas sin actualizar (12 * 60 * 60 * 1000 = 43,200,000 ms)
    const isMoreThan12Hours = (now.getTime() - lastDownload) >= (12 * 60 * 60 * 1000);

    // Descargado el día de ayer o antes (fechas calendario distintas)
    const isPreviousDay = now.toDateString() !== downloadDate.toDateString();

    if (isMoreThan12Hours || isPreviousDay) {
      return notice;
    }

    return null;
  });

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
    isConform?: boolean;
  })[] = [];

  onConformToggle(line: (typeof this.receiverLines)[number]): void {
    if (line.isConform) {
      line.quantityAccepted = line.quantity;
      line.rejectionReason = '';
    } else {
      line.quantityAccepted = 0;
    }
  }

  markAllReceiverLinesConform(conform: boolean): void {
    for (const line of this.receiverLines) {
      line.isConform = conform;
      if (conform) {
        line.quantityAccepted = line.quantity;
        line.rejectionReason = '';
      }
    }
  }

  readonly pendingCount = this.outboxService.pendingCount;
  readonly helpGuideOpen = signal(false);

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
    addIcons({ logOutOutline, mapOutline, syncOutline });
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
    this.checkDemoAvailability();
    // Si es conductor y no hay ruta en caché, intentar descargarla
    if (this.authService.currentRole() === 'Conductor' && !this.outboxService.activeAssignment()) {
      await this.downloadRoute();
    }
  }

  ionViewWillEnter(): void {
    this.checkDemoAvailability();
    const qr = history.state?.receiverQr;
    if (this.authService.currentRole() === 'Receptor' && qr?.unitId && qr?.token) {
      void this.loadReceiverUnit(qr.unitId, qr.token);
    }
  }

  checkDemoAvailability(): void {
    if (typeof this.fieldApi?.getTodayRoutePlans !== 'function') {
      this.demoAvailable.set(false);
      return;
    }
    this.fieldApi.getTodayRoutePlans().subscribe({
      next: () => this.demoAvailable.set(true),
      error: () => this.demoAvailable.set(false)
    });
  }

  openDemoSimulation(): void {
    this.router.navigate(['/demo']);
  }

  // --- Métodos de Interacción del Nuevo Diseño ---
  toggleSearch(): void {
    this.searchOpen.set(!this.searchOpen());
  }

  openHelpGuide(): void {
    this.helpGuideOpen.set(true);
  }

  closeHelpGuide(): void {
    this.helpGuideOpen.set(false);
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
      message: 'Empresa: IstpetDev Operaciones Nacionales\nRUC: 1793148291001\nEstado: Validación en proceso (45%)\nProtocolo de custodia digital activo con claves M2M.',
      buttons: ['Aceptar']
    });
    await alert.present();
  }

  async openMenuAlert(): Promise<void> {
    const buttons: any[] = [
      { text: 'Escanear QR de Custodia', handler: () => this.openScan() },
      { text: 'Cola de Sincronización', handler: () => this.openSyncQueue() }
    ];

    if (this.demoAvailable()) {
      buttons.push({
        text: '⚡ Simulador de Campo (Demo)',
        handler: () => this.openDemoSimulation()
      });
    }

    buttons.push(
      { text: 'Cerrar Sesión', role: 'destructive', handler: () => this.logout() },
      { text: 'Cancelar', role: 'cancel' }
    );

    const alert = await this.alerts.create({
      header: this.authService.me()?.displayName || 'Alexander Martinez',
      subHeader: `Rol: ${this.roleSubtitle() || 'Operador de Campo'}`,
      buttons
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
        nextRequestedQuantity: null,
        isConform: true
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
    const status = typeof this.outboxService.effectiveDeliveryStatus === 'function'
      ? this.outboxService.effectiveDeliveryStatus(
          del.deliveryId,
          del.status,
          del.handlingUnits ? del.handlingUnits.map((hu) => hu.status) : [],
        )
      : del.status;
    if (status === 'Dispatched') {
      const unitsReady = (del.handlingUnits?.length || 0) > 0 && del.handlingUnits.every((hu) => hu.status === 'Dispatched');
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
    try {
      const assignment = await this.outboxService.downloadRouteAssignment();
      const total = assignment.routes ? assignment.routes.reduce((sum: number, r) => sum + r.deliveries.length, 0) : 0;
      this.showAssignedRoute.set(false);
      await this.showToast(`Ruta cargada con éxito: ${total} entrega(s) asignada(s) para el vehículo.`, 'success');
    } catch {
      await this.showToast('Aviso: Utilizando asignación almacenada localmente.', 'warning');
    } finally {
      this.loadingRoute.set(false);
    }
  }

  openScan(mode?: string): void {
    if (mode) {
      this.router.navigate(['/scan'], { queryParams: { mode } });
    } else {
      this.router.navigate(['/scan']);
    }
  }

  getDeliveryProducts(del: MobileAssignmentDelivery): Array<{ name: string; quantity: number; unit: string }> {
    if (!del || !del.handlingUnits) return [];
    const productMap = new Map<string, { name: string; quantity: number; unit: string }>();

    for (const hu of del.handlingUnits) {
      if (!hu.lines) continue;
      for (const line of hu.lines) {
        const rawName = (line as any).productName || line.sku || 'Producto';
        const displayName = this.formatProductName(rawName);
        const key = `${displayName}__${line.unit}`;
        const existing = productMap.get(key);
        if (existing) {
          existing.quantity += line.quantity;
        } else {
          productMap.set(key, {
            name: displayName,
            quantity: line.quantity,
            unit: line.unit
          });
        }
      }
    }
    return Array.from(productMap.values());
  }

  formatProductName(codeOrName: string): string {
    if (!codeOrName) return 'Producto';
    const upper = codeOrName.toUpperCase();
    if (upper.includes('PINOK') || upper.includes('DESINF')) return 'Pinoklin Desinfectante 5L';
    if (upper.includes('AMOX')) return 'Amoxicilina 500mg';
    if (upper.includes('ALCOHOL') || upper.includes('ALC')) return 'Alcohol 70% 1L';
    if (upper.includes('SUERO') || upper.includes('FISIO')) return 'Suero Fisiológico 500ml';
    return codeOrName;
  }

  openIncident(): void {
    this.router.navigate(['/scan']);
  }

  private async showToast(message: string, color: 'success' | 'warning'): Promise<void> {
    const toast = await this.toastController.create({ message, duration: 3000, position: 'top', color });
    await toast.present();
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
