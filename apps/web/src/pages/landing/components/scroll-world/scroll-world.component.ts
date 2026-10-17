import {
  AfterViewInit,
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  ViewChild,
  signal,
  computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SCROLL_WORLD_ASSETS } from './scroll-world-assets';

export interface ScrollStation {
  readonly id: string;
  readonly indexLabel: string;
  readonly badge: string;
  readonly location: string;
  readonly title: string;
  readonly subtitle: string;
  readonly metricLabel: string;
  readonly metricValue: string;
  readonly tags: readonly string[];
  readonly image: string;
  readonly range: readonly [number, number];
}

@Component({
  selector: 'landing-scroll-world',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section
      #scrollTrack
      id="recorrido-scroll"
      aria-label="Recorrido cinemático en primera persona por instalaciones logísticas ISTPET"
      class="relative w-full h-[460vh] bg-[#0A2540] select-none">

      <!-- Viewport Fijo Determinista (Fondo cinemático que llena el viewport) -->
      <div
        class="w-full h-screen overflow-hidden z-20 bg-[#0A2540]"
        [style.position]="pinMode() === 'pinned' ? 'fixed' : 'absolute'"
        [style.top]="pinMode() === 'pinned' ? '0px' : (pinMode() === 'before' ? '0px' : 'auto')"
        [style.bottom]="pinMode() === 'after' ? '0px' : 'auto'"
        [style.left]="'0px'"
        [style.right]="'0px'">

        <!-- ========================================================================= -->
        <!-- 1. ESCENAS FOTOGRÁFICAS CINEMÁTICAS EN PRIMERA PERSONA (CROSS-DISSOLVE)    -->
        <!-- ========================================================================= -->
        <div class="absolute inset-0 overflow-hidden pointer-events-none z-0">
          @for (station of stations; track station.id; let i = $index) {
            <div
              class="absolute inset-0 overflow-hidden will-change-transform transition-opacity duration-300 ease-out"
              [style.opacity]="getSceneOpacity(i)"
              [style.z-index]="getSceneZIndex(i)">
              
              <img
                [src]="station.image"
                [alt]="station.title"
                class="absolute inset-0 w-full h-full object-cover object-center will-change-transform"
                [style.transform]="getSceneTransform(i)" />
            </div>
          }

          <!-- Scrim Atmosférico Lateral y Viñeta de Contraste para Tipografía -->
          <div
            class="absolute inset-0 z-10 pointer-events-none"
            style="background: linear-gradient(90deg, rgba(10,37,64,0.92) 0%, rgba(10,37,64,0.75) 36%, rgba(10,37,64,0.32) 68%, rgba(10,37,64,0.18) 100%), radial-gradient(circle at 80% 20%, rgba(0,194,255,0.08) 0%, transparent 60%);">
          </div>
        </div>

        <!-- ========================================================================= -->
        <!-- 2. TOPBAR INFORMATIVO: ESTADO DE RECORRIDO Y TELEMETRÍA GLOBAL            -->
        <!-- ========================================================================= -->
        <div class="absolute top-0 left-0 right-0 z-30 pt-20 px-6 sm:px-12 flex items-center justify-between pointer-events-none">
          <div class="flex items-center gap-3">
            <span class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/10 backdrop-blur-md border border-white/20 text-xs font-mono font-medium text-white shadow-sm">
              <span class="w-2 h-2 rounded-full bg-[#00E5A3] animate-pulse"></span>
              RECORRIDO EN PRIMERA PERSONA
            </span>
            <span class="hidden md:inline-block text-xs font-mono text-slate-300/80">
              Corredor Logístico Hospitalario • Ecuador
            </span>
          </div>

          <div class="flex items-center gap-3">
            <span class="text-xs font-mono text-slate-300/90 bg-[#0A2540]/60 backdrop-blur-md px-3 py-1 rounded-lg border border-white/10">
              Estación <strong class="text-[#00C2FF]">{{ activeStationIndex() + 1 }}</strong> de 4
            </span>
            <span class="text-xs font-mono text-white/80 hidden sm:inline-block bg-[#0A2540]/60 backdrop-blur-md px-3 py-1 rounded-lg border border-white/10">
              Progreso: <strong class="text-[#00E5A3]">{{ progressPercent() }}%</strong>
            </span>
          </div>
        </div>

        <!-- ========================================================================= -->
        <!-- 3. TARJETAS DE HISTORIA FLOTANTES (.sw-copylayer)                         -->
        <!-- ========================================================================= -->
        <div class="absolute inset-0 z-20 pointer-events-none flex items-center px-6 sm:px-12 lg:px-20">
          <div class="w-full max-w-xl pointer-events-auto">
            @for (station of stations; track station.id; let i = $index) {
              <article
                class="transition-all duration-500 ease-out absolute top-1/2 -translate-y-1/2 w-full max-w-xl"
                [style.opacity]="getCardOpacity(i)"
                [style.transform]="getCardTransform(i)"
                [style.pointer-events]="getCardOpacity(i) > 0.4 ? 'auto' : 'none'"
                [style.z-index]="activeStationIndex() === i ? 25 : 10">

                <div class="rounded-2xl bg-[#0A2540]/85 backdrop-blur-xl border border-white/15 p-6 sm:p-8 text-white shadow-2xl relative overflow-hidden">
                  
                  <!-- Resplandor de acento superior de la tarjeta -->
                  <div class="absolute -top-10 -right-10 w-40 h-40 rounded-full bg-[#0066FF]/20 blur-2xl pointer-events-none"></div>

                  <!-- Header: Número y Ubicación -->
                  <div class="flex items-center justify-between gap-4 mb-4">
                    <span class="font-mono text-xs text-[#00C2FF] font-semibold tracking-wider uppercase flex items-center gap-2">
                      <span class="w-1.5 h-1.5 rounded-full bg-[#00C2FF]"></span>
                      {{ station.location }}
                    </span>
                    <span class="font-mono text-xs text-slate-400 bg-white/5 px-2.5 py-0.5 rounded-md border border-white/10">
                      {{ station.indexLabel }} / 04
                    </span>
                  </div>

                  <!-- Título con text-wrap: balance -->
                  <h3 class="text-2xl sm:text-3xl lg:text-4xl font-extrabold text-white tracking-tight leading-tight mb-3 [text-wrap:balance]">
                    {{ station.title }}
                  </h3>

                  <!-- Subtítulo con text-wrap: pretty -->
                  <p class="text-sm sm:text-base text-slate-200 leading-relaxed mb-6 font-normal [text-wrap:pretty]">
                    {{ station.subtitle }}
                  </p>

                  <!-- Pills técnicas -->
                  <div class="flex flex-wrap gap-2 mb-6">
                    @for (tag of station.tags; track tag) {
                      <span class="text-xs font-mono font-medium px-2.5 py-1 rounded-md bg-white/10 text-white/90 border border-white/10 shadow-2xs">
                        {{ tag }}
                      </span>
                    }
                  </div>

                  <!-- Barra de Telemetría Operativa en Vivo -->
                  <div class="p-3.5 rounded-xl bg-white/5 border border-white/10 flex items-center justify-between gap-4">
                    <div class="flex flex-col">
                      <span class="text-[10px] font-mono uppercase tracking-wider text-slate-400">
                        {{ station.metricLabel }}
                      </span>
                      <strong class="text-sm font-mono font-bold text-[#00E5A3]">
                        {{ station.metricValue }}
                      </strong>
                    </div>

                    <a
                      routerLink="/auth/login"
                      class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[#0066FF] hover:bg-[#0052D9] text-white text-xs font-semibold tracking-wide transition-all shadow-md active:scale-95">
                      <span>Ver Registro</span>
                      <svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                        <path d="M5 12h14M12 5l7 7-7 7" stroke-linecap="round" stroke-linejoin="round"/>
                      </svg>
                    </a>
                  </div>

                </div>
              </article>
            }
          </div>
        </div>

        <!-- ========================================================================= -->
        <!-- 4. NAVEGACIÓN VERTICAL LATERAL DERECHA (.sw-route)                         -->
        <!-- ========================================================================= -->
        <nav
          aria-label="Selector de estaciones de trazabilidad"
          class="absolute right-6 sm:right-10 top-1/2 -translate-y-1/2 z-30 flex flex-col gap-3 pointer-events-auto">
          @for (station of stations; track station.id; let i = $index) {
            <button
              type="button"
              (click)="jumpTo(i)"
              [attr.aria-current]="activeStationIndex() === i ? 'step' : null"
              class="group flex items-center justify-end gap-3 text-right focus:outline-none focus-visible:ring-2 focus-visible:ring-[#00C2FF] rounded-lg p-1.5 transition-all">
              
              <span
                class="hidden md:inline-block font-mono text-xs tracking-wider transition-colors duration-200"
                [class.text-white]="activeStationIndex() === i"
                [class.font-bold]="activeStationIndex() === i"
                [class.text-slate-400]="activeStationIndex() !== i"
                [class.group-hover:text-slate-200]="activeStationIndex() !== i">
                {{ station.badge }}
              </span>

              <span
                class="w-3 h-3 rounded-full border-2 transition-all duration-300 flex items-center justify-center shrink-0"
                [class.bg-[#00C2FF]]="activeStationIndex() === i"
                [class.border-[#00C2FF]]="activeStationIndex() === i"
                [class.shadow-[0_0_12px_#00C2FF]]="activeStationIndex() === i"
                [class.scale-125]="activeStationIndex() === i"
                [class.border-white/40]="activeStationIndex() !== i"
                [class.bg-white/10]="activeStationIndex() !== i"
                [class.group-hover:border-white]="activeStationIndex() !== i">
              </span>
            </button>
          }
        </nav>

        <!-- ========================================================================= -->
        <!-- 5. BARRA DE PROGRESO INFERIOR CONTINUA (.sw-scrollbar)                    -->
        <!-- ========================================================================= -->
        <div class="absolute bottom-0 left-0 right-0 h-1 bg-white/10 z-40">
          <div
            class="h-full bg-gradient-to-r from-[#0066FF] via-[#00C2FF] to-[#00E5A3] transition-all duration-75 ease-out origin-left"
            [style.transform]="'scaleX(' + progress() + ')'">
          </div>
        </div>

        <!-- Indicador de desplazamiento inferior -->
        <div
          class="absolute bottom-5 left-1/2 -translate-x-1/2 z-30 flex items-center gap-2 text-[11px] font-mono text-slate-400 pointer-events-none transition-opacity duration-300"
          [style.opacity]="progress() < 0.95 ? 0.8 : 0">
          <span>Desplaza para continuar recorrido</span>
          <svg class="w-3.5 h-3.5 animate-bounce" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M12 5v14M5 12l7 7 7-7" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </div>

      </div>
    </section>
  `
})
export class ScrollWorldComponent implements AfterViewInit, OnDestroy {
  @ViewChild('scrollTrack') scrollTrackRef?: ElementRef<HTMLElement>;

  readonly stations: readonly ScrollStation[] = [
    {
      id: 'bodega',
      indexLabel: '01',
      badge: '01 BODEGA MATRIZ',
      location: 'BODEGA CENTRAL • PALLATANGA',
      title: 'Envasado Certificado y Registro Óptico de Lotes',
      subtitle:
        'Inspección automatizada de bidones HDPE de 60L. Verificación láser de sellos de seguridad inviolables antes de su transferencia a la flota de transporte pesado.',
      metricLabel: 'Lotes Auditados Hoy',
      metricValue: '1,280 Unidades 100% OK',
      tags: ['Bidones HDPE 60L', 'Escáner Láser Óptico', 'Sello Inviolable', 'Control de Derrames'],
      image: SCROLL_WORLD_ASSETS.walkthrough.scene1Warehouse,
      range: [0.0, 0.25]
    },
    {
      id: 'telemetria',
      indexLabel: '02',
      badge: '02 TELEMETRÍA POSTGIS',
      location: 'CENTRO DE CONTROL • 24/7',
      title: 'Supervisión Satelital PostGIS y Geocercas Dinámicas',
      subtitle:
        'Monitoreo telemático con telemetría en tiempo real cada 500 ms. Geocercas dinámicas automáticas y alertas tempranas ante desvíos en el corredor interprovincial.',
      metricLabel: 'Latencia de Transmisión',
      metricValue: '< 120 ms PostGIS',
      tags: ['Geocercas PostGIS', 'Sensor IoT de Puertas', 'Doble Conductor', 'Corte Remoto'],
      image: SCROLL_WORLD_ASSETS.walkthrough.scene2Telemetry,
      range: [0.25, 0.50]
    },
    {
      id: 'reserva',
      indexLabel: '03',
      badge: '03 PLANTA DE RESERVA',
      location: 'PLANTA INDUSTRIAL • BUFFER CRÍTICO',
      title: 'Silos de Presurización y Almacenamiento Regulado',
      subtitle:
        'Control continuo de presión manométrica y temperatura de sustancias desinfectantes. Respaldo de reserva estratégica para garantizar 0 quiebres de stock hospitalario.',
      metricLabel: 'Capacidad de Reserva',
      metricValue: '99.98% Operativa',
      tags: ['Presión Manométrica', 'Aislamiento Térmico', 'Válvulas Reguladas', 'Respaldo UPS'],
      image: SCROLL_WORLD_ASSETS.walkthrough.scene3Silos,
      range: [0.50, 0.75]
    },
    {
      id: 'custodia',
      indexLabel: '04',
      badge: '04 CUSTODIA HOSPITALARIA',
      location: 'MUELLE ESTÉRIL • QUITO NORTE',
      title: 'Recepción en Esclusa y Custodia Digital SHA-256',
      subtitle:
        'Entrega en muelle hospitalario estéril con escaneo QR de un solo uso y firma electrónica criptográfica validada en base de datos central sin margen de adulteración.',
      metricLabel: 'Validación Criptográfica',
      metricValue: 'SHA-256 Verificado',
      tags: ['Esclusa Presurizada', 'QR Desechable', 'Firma Criptográfica', 'Acta Digital'],
      image: SCROLL_WORLD_ASSETS.walkthrough.scene4Hospital,
      range: [0.75, 1.0]
    }
  ];

  readonly pinMode = signal<'before' | 'pinned' | 'after'>('before');
  readonly progress = signal<number>(0);
  readonly progressPercent = computed(() => Math.round(this.progress() * 100));

  readonly activeStationIndex = computed(() => {
    const p = this.progress();
    if (p < 0.25) return 0;
    if (p < 0.50) return 1;
    if (p < 0.75) return 2;
    return 3;
  });

  private targetProgress = 0;
  private currentProgress = 0;
  private rafId: number | null = null;

  ngAfterViewInit(): void {
    if (typeof window === 'undefined') {
      return;
    }
    this.updateScrollTarget();
    this.startLoop();
  }

  ngOnDestroy(): void {
    if (this.rafId !== null && typeof cancelAnimationFrame === 'function') {
      cancelAnimationFrame(this.rafId);
      this.rafId = null;
    }
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.updateScrollTarget();
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.updateScrollTarget();
  }

  getSceneOpacity(index: number): number {
    const p = this.progress();
    const count = this.stations.length;
    // Station centers are at: 0.125, 0.375, 0.625, 0.875
    // Transitions occur smoothly across 0.20 width windows
    if (index === 0) {
      if (p <= 0.20) return 1;
      if (p >= 0.32) return 0;
      return 1 - (p - 0.20) / 0.12;
    }
    if (index === count - 1) {
      if (p >= 0.78) return 1;
      if (p <= 0.66) return 0;
      return (p - 0.66) / 0.12;
    }

    // Intermediate stations
    const center = (index + 0.5) / count;
    const halfWidth = 0.5 / count; // 0.125
    const dist = Math.abs(p - center);
    if (dist <= halfWidth * 0.5) return 1;
    if (dist >= halfWidth * 1.35) return 0;
    return 1 - (dist - halfWidth * 0.5) / (halfWidth * 0.85);
  }

  getSceneZIndex(index: number): number {
    return this.activeStationIndex() === index ? 10 : 5;
  }

  getSceneTransform(index: number): string {
    const p = this.progress();
    const count = this.stations.length;
    const start = index / count;
    const local = Math.max(0, Math.min(1, (p - start) * count));
    // Subtle cinematic zoom in and gentle forward pan
    const scale = 1.02 + local * 0.10;
    const panY = -(local * 2.0);
    return `scale(${scale.toFixed(3)}) translate3d(0, ${panY.toFixed(1)}%, 0)`;
  }

  getCardOpacity(index: number): number {
    const p = this.progress();
    const [start, end] = this.stations[index].range;
    const fadeSpan = 0.05;

    if (p < start - fadeSpan || p > end + fadeSpan) {
      return 0;
    }
    if (p < start + fadeSpan) {
      return Math.max(0, Math.min(1, (p - (start - fadeSpan)) / (fadeSpan * 2)));
    }
    if (p > end - fadeSpan) {
      return Math.max(0, Math.min(1, (end + fadeSpan - p) / (fadeSpan * 2)));
    }
    return 1;
  }

  getCardTransform(index: number): string {
    const opacity = this.getCardOpacity(index);
    const offsetY = (1 - opacity) * 24;
    return `translate3d(0, calc(-50% + ${offsetY.toFixed(1)}px), 0)`;
  }

  jumpTo(index: number): void {
    if (typeof window === 'undefined') return;
    const track = this.scrollTrackRef?.nativeElement;
    if (!track) return;

    const stageHeight = window.innerHeight;
    const rect = track.getBoundingClientRect();
    const scrollable = Math.max(1, rect.height - stageHeight);
    const trackTop = window.scrollY + rect.top;
    const targetProgress = (index + 0.5) / this.stations.length;
    const targetScrollY = trackTop + targetProgress * scrollable;

    window.scrollTo({ top: targetScrollY, behavior: 'smooth' });
  }

  private updateScrollTarget(): void {
    if (typeof window === 'undefined') return;
    const track = this.scrollTrackRef?.nativeElement;
    if (!track) return;

    const stageHeight = window.innerHeight;
    const rect = track.getBoundingClientRect();
    const scrollable = Math.max(1, rect.height - stageHeight);
    const scrolled = -rect.top;

    if (rect.top > 0) {
      if (this.pinMode() !== 'before') {
        this.pinMode.set('before');
      }
    } else if (rect.bottom < window.innerHeight) {
      if (this.pinMode() !== 'after') {
        this.pinMode.set('after');
      }
    } else {
      if (this.pinMode() !== 'pinned') {
        this.pinMode.set('pinned');
      }
    }

    this.targetProgress = Math.max(0, Math.min(1, scrolled / scrollable));
  }

  private startLoop(): void {
    const tick = () => {
      const diff = this.targetProgress - this.currentProgress;
      if (Math.abs(diff) > 0.0004) {
        this.currentProgress += diff * 0.14;
        this.progress.set(this.currentProgress);
      } else if (this.currentProgress !== this.targetProgress) {
        this.currentProgress = this.targetProgress;
        this.progress.set(this.currentProgress);
      }

      this.rafId = requestAnimationFrame(tick);
    };

    this.rafId = requestAnimationFrame(tick);
  }
}
