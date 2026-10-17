import { AfterViewInit, Component, computed, DestroyRef, ElementRef, inject, input, OnDestroy, OnInit, output, signal, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LucideArrowLeft, LucideArrowUpRight, LucideCheck, LucideClock, LucideCoins, LucideLayers, LucideRefreshCw, LucideRoute, LucideTruck, LucideX } from '@lucide/angular';
import { AuthSession } from '@entities/session';
import { ApiService, DriverOptionDto, PlanningAlternative, PlanningDecision } from '@shared/api';
import { MapMarker } from '@shared/ui';
import { RouteJourneyPlayer } from './route-journey-player.component';
import { JourneyRoute } from './route-journey.model';
import { alternativeLabels, assignedVehicleCount, FLEET_COLORS, planningDecisionStorageKey } from './planning-decision.model';

@Component({
  selector: 'route-decision-workbench',
  standalone: true,
  imports: [CommonModule, FormsModule, RouteJourneyPlayer, LucideArrowLeft, LucideArrowUpRight, LucideCheck, LucideClock, LucideCoins, LucideLayers, LucideRefreshCw, LucideRoute, LucideTruck, LucideX],
  styleUrl: './route-decision-workbench.component.css',
  template: `
    <dialog #dialog class="decision-dialog" aria-labelledby="decision-title" (cancel)="$event.preventDefault(); dismiss()">
      <header class="decision-header">
        <div><p class="eyebrow">ISTPET DEV <span>/</span> PLANIFICACIÓN</p>
          <h2 id="decision-title">{{ decision()?.status === 'Accepted' ? 'Propuesta aprobada' : !decision() && !busy() ? 'Selecciona los pedidos a planificar' : reviewing() ? 'Revisa y aprueba tu propuesta' : 'Compara las propuestas de ruta' }}</h2>
          <p class="subheading">{{ decision()?.status === 'Accepted' ? 'Todas las rutas de la propuesta elegida están guardadas. Ahora puedes asignar conductores y publicar.' : 'Este comparador te permite elegir y aprobar una propuesta según costo, distancia, tiempo y cobertura.' }}</p>
        </div>
        <button type="button" class="icon-button" aria-label="Cerrar planificación" [disabled]="busy()" (click)="dismiss()"><svg lucideX [size]="20"></svg></button>
      </header>
      <div class="decision-scroll">
        @if (error()) { <div class="notice error" role="alert">{{ error() }} @if (recoveryId()) { <button type="button" [disabled]="busy()" (click)="recover()">Reintentar consulta</button> } @else if (orderIds().length) { <button type="button" [disabled]="busy()" (click)="prepare()">Reintentar cálculo</button> }</div> }
        @if (message()) { <div class="notice" role="status">{{ message() }}</div> }
        @if (busy() && !decision()) {
          <div class="loading-state" aria-live="polite"><svg lucideRoute [size]="30"></svg><h3>Preparando tus alternativas</h3><p>Validando abastecimiento, vehículos y recorridos.</p><div class="skeleton-grid"><div></div><div></div><div></div></div></div>
        }
        @if (!busy() && !decision()) {
          <div class="loading-state flex flex-col items-start justify-center gap-4" data-testid="decision-empty-state"><svg lucideRoute [size]="32"></svg><h3>No hay propuestas para comparar todavía.</h3><p>Selecciona los pedidos que necesitas entregar. Calcularemos sus propuestas y podrás aprobar la que más te convenga.</p><button class="primary-button" type="button" (click)="requestOrders.emit()">Seleccionar pedidos<svg lucideArrowUpRight [size]="18"></svg></button></div>
        }
        @if (decision(); as run) {
          <div class="step-line"><span [class.current]="!reviewing() && run.status !== 'Accepted'">03 <span>Comparar</span></span><i></i><span [class.current]="reviewing() || run.status === 'Accepted'">04 <span>Revisar y aceptar</span></span><span class="operational-date">{{ run.plannedStartUtc | date:'dd MMM yyyy':'-0500' }}</span></div>
          @if (run.status !== 'Accepted') {
            <div class="alternatives" role="radiogroup" aria-label="Alternativas de planificación">
              @for (option of run.options; track option.optionId) {
                <label class="alternative-card" [class.selected]="selected()?.optionId === option.optionId" [class.blocked]="!!option.blockedReason">
                  <div class="card-top flex justify-between items-center mb-4"><span class="option-code">OPCIÓN {{ option.optionId }}</span><input type="radio" name="alternative" [value]="option.optionId" [checked]="selected()?.optionId === option.optionId" [disabled]="busy()" (change)="selectOption(option.optionId)"></div>
                  <div class="flex flex-wrap gap-2 mb-3" aria-label="Por qué considerar esta propuesta">@for (label of labels(option, run.options); track label) { <span class="tag !bg-sky-50 !border-sky-200 !text-sky-900 !text-xs !font-semibold">{{ label }}</span> }</div>
                  <h3>{{ option.title }}</h3>
                  <p class="option-description">{{ option.description }}</p>
                  <div class="price">@if (option.totalCost != null && (!option.blockedReason || option.routes.length || option.carrierType === 'Contratada')) { <span>$</span>{{ option.totalCost | number:'1.2-2' }} } @else { Sin dato }<small>costo total estimado · USD</small></div>
                  <div class="card-metrics flex flex-wrap gap-5 text-[13px] border-t border-[#dce4ed] pt-4"><span><svg lucideRoute [size]="16"></svg>{{ option.totalDistanceKm == null || (option.blockedReason && !option.routes.length && option.carrierType !== 'Contratada') ? 'Sin dato' : (option.totalDistanceKm | number:'1.0-1') + ' km' }}</span><span><svg lucideTruck [size]="16"></svg>{{ option.routes.length ? vehicleCount(option) + (vehicleCount(option) === 1 ? ' vehículo' : ' vehículos') : 'Por confirmar' }}</span></div>
                  <div class="card-tags flex flex-wrap gap-2 mt-4">@if (option.isRecommended) { <span class="tag recommended"><svg lucideCheck [size]="12"></svg> Recomendada</span> }<span class="tag">{{ option.carrierType }}</span>@if (option.pendingOrdersCount) { <span class="tag warning">Cobertura parcial · {{ option.pendingOrdersCount }} pedidos con remanentes</span> } @else if (!option.blockedReason) { <span class="tag">Cobertura completa</span> }</div>
                  @if (option.blockedReason) { <p class="blocked-reason">{{ option.blockedReason }}</p> }
                </label>
              }
            </div>
          }
          @if (selected(); as option) {
            <section class="operation-section" aria-label="Recorridos de la alternativa seleccionada">
              <div class="section-heading flex justify-between items-center gap-5 mb-5"><div><p class="eyebrow">{{ run.status === 'Accepted' ? 'ALTERNATIVA ACEPTADA' : 'VISTA PREVIA' }} · OPCIÓN {{ option.optionId }}</p><h3>{{ option.routes.length > 1 ? 'Cada vehículo, su recorrido.' : 'Todo el recorrido, a la vista.' }}</h3></div><button class="quiet-button" type="button" (click)="focusedRouteId.set(null)" [attr.aria-pressed]="!focusedRouteId()"><svg lucideLayers [size]="16"></svg> Ver todos</button></div>
              <div class="fleet-layout">
                <div class="fleet-list" aria-label="Vehículos de la propuesta">
                  <div class="flex flex-wrap gap-2" role="tablist" aria-label="Seleccionar vehículo">@for (route of option.routes; track route.id; let index = $index) {
                    <button class="vehicle-card" type="button" role="tab" (click)="selectVehicle(route.id, $event)" [class.focused]="focusedRouteId() === route.id" [attr.aria-selected]="focusedRouteId() === route.id" [style.--vehicle-color]="color(index)"><span class="vehicle-name"><svg lucideTruck [size]="18"></svg><strong>{{ route.vehicleCode }}</strong></span><span class="vehicle-values">{{ route.evaluation.totalDistanceKm | number:'1.0-1' }} km · {{ route.stops.length }} paradas · $ {{ route.evaluation.totalCost | number:'1.2-2' }}</span></button>
                  }</div>
                  @if (focusedRoute(); as route) { <label class="text-xs text-slate-600">Conductor de {{ route.vehicleCode }}<select [attr.aria-label]="'Conductor de ' + route.vehicleCode" [ngModel]="assignments()[route.vehicleCode] || ''" (ngModelChange)="assignDriver(route.vehicleCode, $event)" [disabled]="busy() || published()"><option value="">Seleccionar conductor</option>@for (driver of drivers(); track driver.accountId) { <option [value]="driver.accountId">{{ driver.displayName }}</option> }</select></label> }
                  @if (!option.routes.length) { <p class="text-xs text-amber-800">{{ option.blockedReason || 'No hay recorridos disponibles.' }}</p> }
                </div>
                <div class="operation-map"><route-journey-player [routes]="journeyRoutes()" [focusedRouteId]="focusedRouteId()" [autoplay]="animateSelection()" [replayKey]="journeyReplayKey()" (focusRoute)="focusVehicle($event)"></route-journey-player></div>
              </div>
              @if (approximate()) { <p class="map-note">Los trazos punteados son aproximados; no representan un recorrido confirmado por calles.</p> }
              <div class="operation-summary flex flex-wrap gap-5 py-5 border-b border-[#dce4ed] text-xs"><span><svg lucideTruck [size]="18"></svg><strong>{{ vehicleCount(option) }}</strong> {{ vehicleCount(option) === 1 ? 'vehículo' : 'vehículos' }}</span><span><svg lucideRoute [size]="18"></svg><strong>{{ option.totalDistanceKm | number:'1.0-1' }}</strong> km totales</span><span><svg lucideClock [size]="18"></svg><strong>{{ duration() | number:'1.0-0' }}</strong> min para completar</span><span><svg lucideCoins [size]="18"></svg><strong>$ {{ option.totalCost | number:'1.2-2' }}</strong> estimados</span></div>
            </section>
            @if (reviewing() && run.status === 'Open' && !option.blockedReason) {
              <section class="review-section"><div><p class="eyebrow">ANTES DE CONFIRMAR</p><h3>Se guardarán {{ option.routes.length }} rutas de la opción {{ option.optionId }}.</h3><p>Las otras alternativas de este cálculo quedarán descartadas. Las rutas ajenas a este cálculo se conservan.</p>@if (option.pendingOrdersCount) { <p class="warning">Esta alternativa deja remanentes en {{ option.pendingOrdersCount }} pedidos.</p> }</div></section>
            }
            @if (run.status === 'Accepted') {
              <section class="review-section"><div><p class="eyebrow">SIGUIENTE PASO</p><h3>Asigna conductores sobre el mapa.</h3><p>Selecciona cada vehículo sobre el mapa y elige su conductor. Publicar envía todas las rutas de la propuesta a sus conductores.</p></div></section>
              <details class="discarded"><summary>{{ discardedCount() }} alternativas descartadas · consultar historial</summary>@for (discarded of run.options; track discarded.optionId) { @if (discarded.status === 'Discarded') { <div><span>Opción {{ discarded.optionId }} · {{ discarded.title }}</span><span>Descartada al aceptar {{ run.acceptedOptionId }}</span></div> } }</details>
            }
          }
          @if (!run.options.length) { <div class="no-route"><h3>No hay alternativas disponibles</h3><p>Revisa el stock y los pedidos seleccionados antes de recalcular.</p></div> }
        }
      </div>
      @if (decision()) { <footer class="decision-footer"><div><span class="footer-indicator"></span><p>{{ busy() ? 'Procesando con el servidor…' : decision()?.status === 'Accepted' ? 'Elección guardada en el servidor' : 'La selección no descarta alternativas' }}</p></div><div class="footer-actions flex items-center gap-3">
        @if (decision()?.status === 'Accepted') { <button type="button" class="quiet-button" (click)="dismiss()" [disabled]="busy()">Ver rutas</button><button class="primary-button" type="button" (click)="publish()" [disabled]="busy() || published() || !allDriversAssigned()">{{ published() ? 'Conjunto publicado' : 'Publicar conjunto' }}<svg lucideArrowUpRight [size]="17"></svg></button> }
        @else { <button type="button" class="quiet-button" (click)="reviewing() ? reviewing.set(false) : dismiss()" [disabled]="busy()"><svg lucideArrowLeft [size]="16"></svg> Volver</button><button type="button" class="quiet-button" (click)="prepare()" [disabled]="busy()"><svg lucideRefreshCw [size]="16"></svg> Recalcular</button><button type="button" class="primary-button" [disabled]="busy() || !selected() || !!selected()?.blockedReason" (click)="reviewing() ? accept() : reviewing.set(true)">{{ busy() ? 'Guardando aprobación…' : reviewing() ? 'Aprobar opción ' + selected()?.optionId : 'Revisar y aprobar opción ' + selected()?.optionId }}<svg lucideArrowUpRight [size]="17"></svg></button> }
      </div></footer> }
    </dialog>
  `
})
export class RouteDecisionWorkbench implements OnInit, AfterViewInit, OnDestroy {
  readonly orderIds = input<string[]>([]);
  readonly decisionId = input<string | null>(null);
  readonly initialOptionId = input('');
  readonly initialAssignments = input<Record<string, string>>({});
  readonly closed = output<void>();
  readonly requestOrders = output<void>();
  readonly assignmentsChanged = output<Record<string, string>>();
  readonly saved = output<string[]>();
  private readonly api = inject(ApiService);
  private readonly session = inject(AuthSession);
  private readonly destroyRef = inject(DestroyRef);
  @ViewChild('dialog') dialog?: ElementRef<HTMLDialogElement>;
  private readonly previousFocus = document.activeElement as HTMLElement | null;
  readonly decision = signal<PlanningDecision | null>(null);
  readonly selectedOptionId = signal('');
  readonly focusedRouteId = signal<string | null>(null);
  readonly animateSelection = signal(true);
  readonly journeyReplayKey = signal(0);
  readonly busy = signal(false);
  readonly reviewing = signal(false);
  readonly error = signal('');
  readonly recoveryId = signal<string | null>(null);
  readonly message = signal('');
  readonly drivers = signal<DriverOptionDto[]>([]);
  readonly assignments = signal<Record<string, string>>({});
  readonly published = signal(false);
  private acceptOperationId = crypto.randomUUID();
  private publishOperationId = crypto.randomUUID();
  private pendingPrepareId: string = crypto.randomUUID();
  private storageKey(): string { return planningDecisionStorageKey(this.session.me()?.accountId); }
  readonly labels = alternativeLabels;
  readonly vehicleCount = assignedVehicleCount;
  readonly color = (index: number) => FLEET_COLORS[index % FLEET_COLORS.length];
  readonly selected = computed(() => this.decision()?.options.find(o => o.optionId === (this.decision()?.acceptedOptionId || this.selectedOptionId())));
  readonly focusedRoute = computed(() => this.selected()?.routes.find(r => r.id === this.focusedRouteId()));
  readonly discardedCount = computed(() => this.decision()?.options.filter(o => o.status === 'Discarded').length || 0);
  readonly duration = computed(() => Math.max(0, ...(this.selected()?.routes.map(r => r.evaluation.totalDurationMinutes) || [])));
  readonly allDriversAssigned = computed(() => !!this.selected()?.routes.length && this.selected()!.routes.every(r => !!this.assignments()[r.vehicleCode]));
  readonly approximate = computed(() => this.selected()?.routes.some(r => r.evaluation.coordinatesSource !== 'osrm'));
  readonly journeyRoutes = computed<JourneyRoute[]>(() => (this.selected()?.routes || []).map((route, index) => ({
    id: route.id, vehicleCode: route.vehicleCode, color: this.color(index), approximate: route.evaluation.coordinatesSource !== 'osrm',
    coordinates: route.evaluation.coordinates, stops: route.stops.map((stop, stopIndex) => ({ ...stop,
      legDistanceKm: route.evaluation.stops[stopIndex]?.legDistanceKm,
      legDurationMinutes: route.evaluation.stops[stopIndex]?.legDurationMinutes }))
  })));

  selectVehicle(id: string, _event: MouseEvent): void { this.focusVehicle(id); }
  focusVehicle(id: string): void {
    this.animateSelection.set(true);
    this.focusedRouteId.set(id);
    this.journeyReplayKey.update(key => key + 1);
  }

  ngOnInit(): void {
    this.selectedOptionId.set(this.initialOptionId());
    this.assignments.set({ ...this.initialAssignments() });
    this.api.getDrivers().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(list => this.drivers.set(list));
    let previous: string | null = null;
    try { previous = localStorage.getItem(this.storageKey()); } catch { /* Storage opcional; el servidor conserva la decisión. */ }
    if (this.decisionId()) this.recover(this.decisionId()!);
    else if (!this.orderIds().length && previous) this.recover(previous);
    else if (this.orderIds().length) this.prepare();
  }
  ngAfterViewInit(): void { this.dialog?.nativeElement.showModal?.(); }
  ngOnDestroy(): void { this.dialog?.nativeElement.close?.(); this.previousFocus?.focus(); }
  dismiss(): void { if (!this.busy()) this.closed.emit(); }
  selectOption(id: string): void {
    if (this.busy()) return;
    this.selectedOptionId.set(id);
    const first = this.selected()?.routes[0];
    if (first) this.focusVehicle(first.id); else this.focusedRouteId.set(null);
    this.reviewing.set(false);
    this.acceptOperationId = crypto.randomUUID(); this.error.set('');
  }
  focusMarker(marker: MapMarker): void { this.focusedRouteId.set(marker.id.split(':')[0]); }
  assignDriver(code: string, id: string): void { this.assignments.update(current => ({ ...current, [code]: id })); this.assignmentsChanged.emit(this.assignments()); this.publishOperationId = crypto.randomUUID(); }
  prepare(): void {
    if (this.busy()) return;
    const ids = this.orderIds().length ? this.orderIds() : this.decision()?.orderIds || [];
    if (!ids.length) return;
    this.busy.set(true); this.error.set(''); this.message.set('');
    this.api.preparePlanningDecision(ids, this.pendingPrepareId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: value => { this.apply(value); this.pendingPrepareId = crypto.randomUUID(); this.reviewing.set(false); this.published.set(false); },
      error: err => { this.busy.set(false); this.error.set(err?.error?.detail || err?.error?.message || 'No se pudo preparar la propuesta. Reintenta con los mismos pedidos.'); }
    });
  }
  recover(id = this.recoveryId()): void {
    if (!id || this.busy()) return;
    this.recoveryId.set(id);
    this.busy.set(true);
    this.api.getPlanningDecision(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: value => { this.apply(value); if (value.status === 'Accepted') { this.error.set(''); this.saved.emit(value.routeIds); } },
      error: err => {
        this.busy.set(false);
        if (err?.status === 404) {
          this.recoveryId.set(null);
          try { localStorage.removeItem(this.storageKey()); } catch { /* Storage opcional. */ }
          this.error.set('Este cálculo ya no está disponible. Selecciona pedidos para crear nuevas propuestas.');
        } else this.error.set('No se pudo consultar el cálculo. Reintenta la consulta o selecciona pedidos para crear nuevas propuestas.');
      }
    });
  }
  accept(): void {
    const run = this.decision(), selected = this.selected();
    if (!run || !selected || selected.blockedReason || this.busy()) return;
    this.busy.set(true); this.error.set('');
    this.api.acceptPlanningDecision(run.id, selected.optionId, run.version, this.acceptOperationId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: value => { this.apply(value); this.saved.emit(value.routeIds); this.message.set('Alternativa aceptada. Sus rutas están guardadas; las otras opciones de este cálculo quedaron descartadas.'); },
      error: err => { this.busy.set(false); this.error.set(err?.error?.detail || err?.error?.message || 'No se confirmó la respuesta. Consulta el estado antes de volver a aceptar.'); this.recover(); }
    });
  }
  publish(): void {
    const run = this.decision();
    if (!run || this.busy() || !this.allDriversAssigned()) return;
    this.busy.set(true); this.error.set('');
    this.api.publishPlanningDecision(run.id, this.assignments(), this.publishOperationId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: value => { this.apply(value); this.published.set(true); this.saved.emit(value.routeIds); this.message.set('Todas las rutas del conjunto están publicadas para sus conductores.'); },
      error: err => { this.busy.set(false); this.error.set(err?.error?.detail || err?.error?.message || 'No se pudo confirmar la publicación. Reintenta con la misma selección de conductores.'); }
    });
  }
  private apply(value: PlanningDecision): void {
    this.decision.set(value); this.recoveryId.set(value.id); this.published.set(value.published); this.busy.set(false);
    if (!value.options.some(o => o.optionId === this.selectedOptionId()))
      this.selectedOptionId.set(value.acceptedOptionId || value.options.find(o => o.isRecommended && !o.blockedReason)?.optionId || value.options.find(o => !o.blockedReason)?.optionId || value.options[0]?.optionId || '');
    if (!this.selected()?.routes.some(route => route.id === this.focusedRouteId())) {
      const first = this.selected()?.routes[0];
      if (first) this.focusVehicle(first.id); else this.focusedRouteId.set(null);
    }
    try { localStorage.setItem(this.storageKey(), value.id); } catch { /* La decisión sigue persistida en el servidor. */ }
  }
}
