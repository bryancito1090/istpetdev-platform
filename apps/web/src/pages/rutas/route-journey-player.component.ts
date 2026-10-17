import { Component, computed, effect, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideArrowLeft, LucideArrowRight, LucidePause, LucidePlay, LucideRotateCcw, LucideRoute } from '@lucide/angular';
import { MapMarker, MapPolyline, MapViewComponent } from '@shared/ui';
import { journeyBackground, journeyLegs, journeyMarkers, JourneyRoute } from './route-journey.model';

@Component({
  selector: 'route-journey-player', standalone: true,
  imports: [CommonModule, MapViewComponent, LucideArrowLeft, LucideArrowRight, LucidePause, LucidePlay, LucideRotateCcw, LucideRoute],
  styles: [`
    :host{display:flex;flex-direction:column;height:100%;min-height:380px;color:#152a42}
    .journey-map{flex:1;min-height:320px;isolation:isolate;border-radius:14px;overflow:hidden}
    .journey-bar{display:flex;align-items:center;gap:14px;padding:14px 16px;background:#fff;border:1px solid #dce4ed;border-radius:12px;margin-bottom:10px}
    .journey-bar strong{display:block;font-size:14px}.journey-bar p{font-size:11px;color:#53647b;margin:3px 0 0}.journey-number{color:#007fa8;font-size:10px;letter-spacing:.1em}
    .journey-controls{display:flex;gap:6px;margin-left:auto}.journey-controls button{min-width:38px;min-height:38px;display:grid;place-items:center;background:#f6f8fb;border:1px solid #dce4ed;border-radius:50%;color:#152a42;cursor:pointer}
    .journey-controls button:disabled{opacity:.4;cursor:default}button:focus-visible{outline:2px solid #0284c7;outline-offset:3px}.journey-controls button.play{background:#0066ff;color:#fff;border-color:#0066ff}
    .journey-note{font-size:10px;color:#53647b;margin:7px 0 0}.leg-navigation{display:flex;gap:6px;flex-wrap:wrap;margin-top:10px}.leg-navigation button{background:#fff;border:1px solid #dce4ed;padding:7px 11px;border-radius:999px;font-size:11px;color:#53647b;cursor:pointer}.leg-navigation button.active{background:#eaf5ff;border-color:#0284c7;color:#075985}
    @media(max-width:680px){.journey-bar{flex-wrap:wrap;padding:12px}.journey-controls{margin-left:0}.journey-map{min-height:330px}.journey-controls button{min-height:44px;min-width:44px}.leg-navigation button{min-height:40px}}
  `],
  template: `
    @if (selected(); as route) {
      <div class="journey-bar"><svg lucideRoute [size]="24" class="text-sky-600"></svg><div>
        <span class="journey-number">{{ route.vehicleCode }} · TRAMO {{ activeLegIndex() + 1 }} DE {{ legs().length }}</span>
        @if (activeLeg(); as leg) { <strong>{{ leg.from.name }} → {{ leg.to.name }}</strong><p>@if (leg.to.legDistanceKm != null) { {{ leg.to.legDistanceKm | number:'1.0-1' }} km · }@if (leg.to.legDurationMinutes != null) { {{ leg.to.legDurationMinutes | number:'1.0-0' }} min de viaje · }{{ playing() ? 'Mostrando recorrido' : finished() ? 'Recorrido completo' : 'Recorrido calculado' }}</p> }
      </div><div class="journey-controls">
        <button type="button" (click)="goToLeg(activeLegIndex() - 1)" [disabled]="activeLegIndex() === 0" aria-label="Tramo anterior"><svg lucideArrowLeft [size]="16"></svg></button>
        <button type="button" class="play" (click)="playing() ? pause() : play()" [disabled]="!legs().length" [attr.aria-label]="playing() ? 'Pausar recorrido' : 'Reproducir recorrido'">@if (playing()) { <svg lucidePause [size]="17" aria-hidden="true"></svg> } @else { <svg lucidePlay [size]="17" aria-hidden="true"></svg> }</button>
        <button type="button" (click)="goToLeg(activeLegIndex() + 1)" [disabled]="activeLegIndex() >= legs().length - 1" aria-label="Tramo siguiente"><svg lucideArrowRight [size]="16"></svg></button>
        <button type="button" (click)="replay()" aria-label="Volver a reproducir desde la recogida"><svg lucideRotateCcw [size]="16"></svg></button>
      </div></div>
    }
    <div class="journey-map"><ui-map-view [markers]="markers()" [polylines]="polylines()" [showLegend]="false" [animatedPolylineId]="playbackStarted() ? activePolylineId() : null" [animationPaused]="!playing()" animationMode="journey" (animationComplete)="completeLeg($event)" (markerClick)="selectMarker($event)"></ui-map-view></div>
    @if (selected()) { <nav class="leg-navigation" aria-label="Tramos de la ruta">@for (leg of legs(); track $index) { <button type="button" [class.active]="activeLegIndex() === $index" [attr.aria-current]="activeLegIndex() === $index ? 'step' : null" (click)="goToLeg($index)">{{ $index + 1 }} · {{ leg.to.name }}</button> }</nav> }
    <p class="journey-note">{{ approximate() ? 'Trazado aproximado entre paradas. Las líneas punteadas no son un recorrido confirmado por calles.' : 'Visualización del recorrido calculado por calles. El indicador muestra el avance de la explicación, no la posición GPS del vehículo.' }}</p>
  `
})
export class RouteJourneyPlayer {
  readonly routes = input<JourneyRoute[]>([]);
  readonly focusedRouteId = input<string | null>(null);
  readonly autoplay = input(true);
  readonly replayKey = input(0);
  readonly focusRoute = output<string>();
  readonly legChange = output<number>();
  readonly activeLegIndex = signal(0);
  readonly playing = signal(false);
  readonly playbackStarted = signal(false);
  readonly finished = signal(false);
  private readonly playbackId = signal(0);
  private lastSelection: string | null = null;
  readonly selected = computed(() => this.routes().find(r => r.id === this.focusedRouteId()));
  readonly legs = computed(() => this.selected() ? journeyLegs(this.selected()!) : []);
  readonly activeLeg = computed(() => this.legs()[this.activeLegIndex()]);
  readonly approximate = computed(() => this.selected()?.approximate ?? this.routes().some(r => r.approximate));
  readonly activePolylineId = computed(() => `${this.focusedRouteId()}:leg:${this.activeLegIndex()}:${this.playbackId()}`);
  readonly markers = computed(() => (this.selected() ? [this.selected()!] : this.routes()).flatMap(journeyMarkers));
  readonly polylines = computed<MapPolyline[]>(() => {
    const result = this.routes().map(r => journeyBackground(r, r.id === this.focusedRouteId()));
    if (!this.selected()) return this.routes().map(r => ({ ...journeyBackground(r, false), color: r.color, opacity: .9, weight: 4 }));
    this.legs().forEach((leg, index) => result.push({ id: index === this.activeLegIndex() ? this.activePolylineId() : `${this.focusedRouteId()}:leg:${index}`,
      coordinates: leg.coordinates, color: index < this.activeLegIndex() || this.finished() ? '#0b9870' : this.selected()!.color,
      opacity: index <= this.activeLegIndex() || this.finished() ? 1 : .55, weight: index === this.activeLegIndex() ? 6 : 4,
      dashArray: this.selected()!.approximate ? '6 6' : undefined, tooltip: `${leg.from.name} → ${leg.to.name}` }));
    return result;
  });
  constructor() {
    effect(() => {
      const id = this.focusedRouteId();
      const selection = `${id}:${this.replayKey()}`;
      const route = this.selected();
      if (!id) { this.lastSelection = null; this.playing.set(false); this.playbackStarted.set(false); return; }
      if (!route || route.coordinates.length < 2 || route.stops.length < 2 || selection === this.lastSelection) return;
      this.lastSelection = selection; this.activeLegIndex.set(0); this.finished.set(false); this.playbackId.update(n => n + 1);
      const play = !!id && this.autoplay() && !this.reducedMotion();
      this.playing.set(play); this.playbackStarted.set(play);
    });
  }
  private reducedMotion(): boolean { return typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches; }
  play(): void {
    if (this.reducedMotion()) return;
    if (this.finished()) this.activeLegIndex.set(0);
    if (!this.playbackStarted()) this.playbackId.update(n => n + 1);
    this.finished.set(false); this.playbackStarted.set(true); this.playing.set(true);
  }
  pause(): void { this.playing.set(false); }
  replay(): void { this.activeLegIndex.set(0); this.playbackStarted.set(false); this.legChange.emit(0); this.play(); }
  goToLeg(index: number): void {
    this.playing.set(false); this.playbackStarted.set(false); this.finished.set(false);
    this.activeLegIndex.set(Math.max(0, Math.min(index, this.legs().length - 1))); this.legChange.emit(this.activeLegIndex());
  }
  completeLeg(id: string): void {
    if (!this.playing() || id !== this.activePolylineId()) return;
    if (this.activeLegIndex() + 1 < this.legs().length) { this.activeLegIndex.update(n => n + 1); this.legChange.emit(this.activeLegIndex()); }
    else { this.playing.set(false); this.playbackStarted.set(false); this.finished.set(true); }
  }
  selectMarker(marker: MapMarker): void {
    const [routeId, sequence] = marker.id.split(':');
    if (routeId !== this.focusedRouteId()) this.focusRoute.emit(routeId);
    else this.goToLeg((this.selected()?.stops.findIndex(stop => stop.sequence === Number(sequence)) ?? 0) - 1);
  }
}
