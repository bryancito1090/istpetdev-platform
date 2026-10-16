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
  readonly title: string;
  readonly subtitle: string;
  readonly metricLabel: string;
  readonly metricValue: string;
  readonly cardSide: 'left' | 'right';
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
      aria-label="Recorrido interactivo de trazabilidad logística"
      class="relative w-full h-[460vh] bg-[#F8FAFC] border-y border-slate-200/70 select-none">

      <!-- Viewport Fijo Determinista (Inline style position evita colisión de orden CSS con Tailwind .relative) -->
      <div
        class="w-full h-[calc(100vh-4rem)] overflow-hidden z-20 bg-gradient-to-br from-white via-[#F4F9FF] to-[#ECFEF8]"
        [style.position]="pinMode() === 'pinned' ? 'fixed' : 'absolute'"
        [style.top]="pinMode() === 'pinned' ? '64px' : (pinMode() === 'before' ? '0px' : 'auto')"
        [style.bottom]="pinMode() === 'after' ? '0px' : 'auto'"
        [style.left]="'0px'"
        [style.right]="'0px'">

        <!-- ========================================================================= -->
        <!-- LIENZO CONTINUO LIMPIO (CERO CAJAS DE UI EN EL MAPA, SOLO ESCENA FÍSICA)  -->
        <!-- ========================================================================= -->
        <div class="absolute inset-0 overflow-hidden pointer-events-none z-0">
          <div
            class="absolute left-1/2 top-1/2 w-0 h-0 will-change-transform"
            [style.transform]="worldCameraTransform()">

            <svg
              class="absolute -left-[900px] -top-[500px] w-[3400px] h-[2800px] overflow-visible"
              viewBox="-900 -500 3400 2800"
              fill="none"
              xmlns="http://www.w3.org/2000/svg">
              <defs>
                <!-- Gradiente Oficial de Ruta (#0066FF -> #00C2FF -> #00E5A3) -->
                <linearGradient id="highwayFlowGrad" x1="0%" y1="0%" x2="100%" y2="100%">
                  <stop offset="0%" stop-color="#0066FF" />
                  <stop offset="50%" stop-color="#00C2FF" />
                  <stop offset="100%" stop-color="#00E5A3" />
                </linearGradient>

                <!-- Líquido Químico Azul/Cyan/Menta -->
                <linearGradient id="liquidGrad" x1="0%" y1="0%" x2="0%" y2="100%">
                  <stop offset="0%" stop-color="#00C2FF" />
                  <stop offset="100%" stop-color="#0066FF" />
                </linearGradient>

                <linearGradient id="mintLiquidGrad" x1="0%" y1="0%" x2="0%" y2="100%">
                  <stop offset="0%" stop-color="#00E5A3" />
                  <stop offset="100%" stop-color="#00A7C4" />
                </linearGradient>

                <!-- Haz de luz de faros LED delanteros del camión -->
                <linearGradient id="headlightBeamGrad" x1="0%" y1="50%" x2="100%" y2="50%">
                  <stop offset="0%" stop-color="#E0F2FE" stop-opacity="0.85" />
                  <stop offset="50%" stop-color="#BAE6FD" stop-opacity="0.35" />
                  <stop offset="100%" stop-color="#FFFFFF" stop-opacity="0" />
                </linearGradient>

                <!-- Auras Atmosféricas Suaves de cada Estación -->
                <radialGradient id="stationGlowBlue" cx="50%" cy="50%" r="50%">
                  <stop offset="0%" stop-color="#00C2FF" stop-opacity="0.24" />
                  <stop offset="55%" stop-color="#0066FF" stop-opacity="0.08" />
                  <stop offset="100%" stop-color="#FFFFFF" stop-opacity="0" />
                </radialGradient>

                <radialGradient id="stationGlowMint" cx="50%" cy="50%" r="50%">
                  <stop offset="0%" stop-color="#00E5A3" stop-opacity="0.26" />
                  <stop offset="55%" stop-color="#00C2FF" stop-opacity="0.08" />
                  <stop offset="100%" stop-color="#FFFFFF" stop-opacity="0" />
                </radialGradient>
              </defs>

              <!-- 1. Curvas Topográficas Orgánicas Sutiles (Sin Cuadros Grises) -->
              <path
                d="M -850 -120 C -200 -340, 600 120, 1600 -80"
                stroke="#0066FF"
                stroke-opacity="0.06"
                stroke-width="2" />
              <path
                d="M -850 380 C -100 180, 500 780, 1700 420"
                stroke="#00C2FF"
                stroke-opacity="0.08"
                stroke-width="2" />
              <path
                d="M -850 920 C -200 740, 480 1280, 1650 980"
                stroke="#00E5A3"
                stroke-opacity="0.09"
                stroke-width="2" />
              <path
                d="M -850 1480 C -100 1320, 520 1820, 1700 1520"
                stroke="#0066FF"
                stroke-opacity="0.06"
                stroke-width="2" />

              <!-- Auras de Luz en las 4 Estaciones -->
              <circle cx="140" cy="0" r="360" fill="url(#stationGlowBlue)" />
              <circle cx="560" cy="520" r="360" fill="url(#stationGlowMint)" />
              <circle cx="120" cy="1060" r="360" fill="url(#stationGlowBlue)" />
              <circle cx="520" cy="1600" r="380" fill="url(#stationGlowMint)" />

              <!-- =================================================================== -->
              <!-- AUTOPISTA REALISTA CON ASFALTO, SEÑALÉTICA VIAL Y BORDES DE HORMIGÓN -->
              <!-- =================================================================== -->
              <!-- AUTOPISTA ELEVADA REALISTA CON ASFALTO Y SEÑALÉTICA VIAL            -->
              <!-- =================================================================== -->
              <!-- 1. Sombra suave del viaducto sobre el terreno -->
              <path
                d="M 0 0 C 380 0, 340 520, 720 520 C 1100 520, 340 1060, -40 1060 C -420 1060, 300 1600, 680 1600"
                stroke="#0A2540"
                stroke-opacity="0.10"
                stroke-width="54"
                stroke-linecap="round"
                transform="translate(4, 10)" />

              <!-- 2. Base estructural de hormigón del viaducto -->
              <path
                d="M 0 0 C 380 0, 340 520, 720 520 C 1100 520, 340 1060, -40 1060 C -420 1060, 300 1600, 680 1600"
                stroke="#E2E8F0"
                stroke-width="46"
                stroke-linecap="round" />

              <!-- 3. Arcén / Berma de seguridad lateral -->
              <path
                d="M 0 0 C 380 0, 340 520, 720 520 C 1100 520, 340 1060, -40 1060 C -420 1060, 300 1600, 680 1600"
                stroke="#475569"
                stroke-width="38"
                stroke-linecap="round" />

              <!-- 4. Líneas blancas continuas laterales de calzada -->
              <path
                d="M 0 0 C 380 0, 340 520, 720 520 C 1100 520, 340 1060, -40 1060 C -420 1060, 300 1600, 680 1600"
                stroke="#FFFFFF"
                stroke-opacity="0.95"
                stroke-width="34"
                stroke-linecap="round" />

              <!-- 5. Calzada principal de asfalto pizarra técnico -->
              <path
                d="M 0 0 C 380 0, 340 520, 720 520 C 1100 520, 340 1060, -40 1060 C -420 1060, 300 1600, 680 1600"
                stroke="#1E293B"
                stroke-width="30"
                stroke-linecap="round" />

              <!-- 6. Línea divisoria central discontinua de carretera real (Blanco reflectante) -->
              <path
                d="M 0 0 C 380 0, 340 520, 720 520 C 1100 520, 340 1060, -40 1060 C -420 1060, 300 1600, 680 1600"
                stroke="#FFFFFF"
                stroke-opacity="0.9"
                stroke-width="2.5"
                stroke-dasharray="16 14"
                [attr.stroke-dashoffset]="-progress() * 900"
                stroke-linecap="round" />

              <!-- 7. Marcadores reflectantes LED en el asfalto (Cian) -->
              <path
                d="M 0 0 C 380 0, 340 520, 720 520 C 1100 520, 340 1060, -40 1060 C -420 1060, 300 1600, 680 1600"
                stroke="#00C2FF"
                stroke-width="3.5"
                stroke-dasharray="2 34"
                [attr.stroke-dashoffset]="-progress() * 900"
                stroke-linecap="round" />

              <!-- =================================================================== -->
              <!-- ESCENA FÍSICA 01: BIDÓN INDUSTRIAL 3D REALISTA + ESCANEO LÁSER      -->
              <!-- (Situada junto a la autopista como muelle de carga de bodega)       -->
              <!-- =================================================================== -->
              <g transform="translate(190, -170)">
                <!-- Plataforma base física de carga -->
                <ellipse cx="0" cy="115" rx="140" ry="46" fill="#FFFFFF" stroke="#00C2FF" stroke-width="2" stroke-opacity="0.4" />
                <ellipse cx="0" cy="115" rx="110" ry="34" fill="#F0F9FF" />

                <!-- Render 3D Fotorrealista: Bidón 60L HDPE con QR y Líquido Neon -->
                <image
                  [attr.href]="assets.drum"
                  x="-160"
                  y="-140"
                  width="320"
                  height="320"
                  style="mix-blend-mode: multiply;"
                  preserveAspectRatio="xMidYMid meet" />

                <!-- Haz Láser Óptico Menta (#00E5A3) que barre el bidón con el scroll -->
                <g [attr.transform]="'translate(-95, ' + (-30 + getStationLocalProgress(0) * 150) + ')'">
                  <rect x="0" y="-8" width="190" height="16" fill="#00E5A3" opacity="0.32" rx="4" />
                  <line x1="0" y1="0" x2="190" y2="0" stroke="#00E5A3" stroke-width="3" stroke-linecap="round" />
                  <circle cx="95" cy="0" r="4" fill="#00E5A3" />
                </g>
              </g>

              <!-- =================================================================== -->
              <!-- ESCENA FÍSICA 02: TELEMETRÍA POSTGIS Y DESVÍO EN CARRETERA          -->
              <!-- =================================================================== -->
              <!-- Ramal secundario bloqueado con asfalto y alerta vial -->
              <path
                d="M 320 220 C 480 180, 680 180, 760 360"
                stroke="#CBD5E1"
                stroke-width="30"
                stroke-linecap="round" />
              <path
                d="M 320 220 C 480 180, 680 180, 760 360"
                stroke="#64748B"
                stroke-width="22"
                stroke-linecap="round" />
              <path
                d="M 320 220 C 480 180, 680 180, 760 360"
                stroke="#EF4444"
                stroke-width="3"
                stroke-dasharray="8 8"
                stroke-linecap="round" />

              <!-- Barricada vial de cierre en el ramal bloqueado -->
              <g transform="translate(540, 200)">
                <rect x="-22" y="-7" width="44" height="14" rx="3" fill="#FEF2F2" stroke="#EF4444" stroke-width="2" />
                <line x1="-14" y1="-7" x2="-6" y2="7" stroke="#EF4444" stroke-width="2.5" />
                <line x1="-2" y1="-7" x2="6" y2="7" stroke="#EF4444" stroke-width="2.5" />
                <circle cx="0" cy="-12" r="4" fill="#EF4444" />
              </g>

              <!-- Estación de Radar y Telemetría Geoespacial PostGIS junto a la curva -->
              <g transform="translate(740, 400)">
                <ellipse cx="0" cy="70" rx="125" ry="40" fill="#FFFFFF" stroke="#00C2FF" stroke-width="2" stroke-opacity="0.4" />
                <ellipse cx="0" cy="70" rx="95" ry="28" fill="#F0F9FF" />

                <!-- Anillos concéntricos de Radar PostGIS que pulsan con el scroll -->
                <circle
                  cx="0"
                  cy="20"
                  [attr.r]="35 + getStationLocalProgress(1) * 65"
                  fill="none"
                  stroke="#00C2FF"
                  stroke-width="2"
                  [attr.stroke-opacity]="0.7 - getStationLocalProgress(1) * 0.5" />
                <circle
                  cx="0"
                  cy="20"
                  [attr.r]="20 + getStationLocalProgress(1) * 40"
                  fill="none"
                  stroke="#00E5A3"
                  stroke-width="1.8"
                  [attr.stroke-opacity]="0.8 - getStationLocalProgress(1) * 0.5" />

                <!-- Torre/Antena baliza central PostGIS -->
                <line x1="0" y1="20" x2="0" y2="-40" stroke="#0A2540" stroke-width="4" stroke-linecap="round" />
                <circle cx="0" cy="-40" r="8" fill="#0066FF" stroke="#FFFFFF" stroke-width="2" />
                <circle cx="0" cy="-40" r="3.5" fill="#00E5A3" />
                <!-- Ondas de transmisión satelital -->
                <path d="M-16 -50 A16 16 0 0 1 16 -50" fill="none" stroke="#00C2FF" stroke-width="2" stroke-linecap="round" />
                <path d="M-26 -58 A26 26 0 0 1 26 -58" fill="none" stroke="#0066FF" stroke-width="1.5" stroke-opacity="0.6" stroke-linecap="round" />

                <!-- Badge flotante de recalculo de ruta -->
                <g transform="translate(-60, -88)">
                  <rect x="0" y="0" width="120" height="24" rx="5" fill="#0A2540" fill-opacity="0.88" />
                  <text x="60" y="16" text-anchor="middle" fill="#00E5A3" font-family="monospace" font-size="8.5" font-weight="bold">
                    POSTGIS DESVÍO ACTIVO
                  </text>
                </g>
              </g>

              <!-- =================================================================== -->
              <!-- ESCENA FÍSICA 03: SILOS INDUSTRIALES 3D Y CONTROL DE RESERVA 90D   -->
              <!-- =================================================================== -->
              <g transform="translate(140, 1040)">
                <ellipse cx="0" cy="120" rx="150" ry="48" fill="#FFFFFF" stroke="#0066FF" stroke-width="2" stroke-opacity="0.35" />
                <ellipse cx="0" cy="120" rx="120" ry="35" fill="#F0F9FF" />

                <!-- Render 3D Fotorrealista: Silos Cilíndricos y Gráfico de Cobertura -->
                <image
                  [attr.href]="assets.silos"
                  x="-190"
                  y="-170"
                  width="380"
                  height="380"
                  style="mix-blend-mode: multiply;"
                  preserveAspectRatio="xMidYMid meet" />

                <!-- Indicador de alerta de nivel que pulsa con el scroll -->
                <g [attr.transform]="'translate(70, -40)'">
                  <circle cx="0" cy="0" r="14" fill="#00E5A3" fill-opacity="0.25" />
                  <circle cx="0" cy="0" r="7" fill="#00E5A3" />
                  <text x="14" y="4" fill="#0A2540" font-family="monospace" font-size="10" font-weight="bold">+90d OK</text>
                </g>
              </g>

              <!-- =================================================================== -->
              <!-- ESCENA FÍSICA 04: TERMINAL MÓVIL 3D Y SELLO OFFLINE SHA-256        -->
              <!-- =================================================================== -->
              <g transform="translate(500, 1580)">
                <ellipse cx="0" cy="120" rx="150" ry="48" fill="#FFFFFF" stroke="#00E5A3" stroke-width="2.5" stroke-opacity="0.45" />
                <ellipse cx="0" cy="120" rx="120" ry="35" fill="#ECFDF5" />

                <!-- Render 3D Fotorrealista: Terminal Rugerizada con Firma y Escudo QR -->
                <image
                  [attr.href]="assets.mobile"
                  x="-190"
                  y="-170"
                  width="380"
                  height="380"
                  style="mix-blend-mode: multiply;"
                  preserveAspectRatio="xMidYMid meet" />

                <!-- Indicador de verificación SHA-256 con el scroll -->
                <g [attr.transform]="'translate(-75, 80)'">
                  <rect x="0" y="0" width="150" height="26" rx="6" fill="#0A2540" fill-opacity="0.88" />
                  <text x="75" y="17" text-anchor="middle" fill="#00E5A3" font-family="monospace" font-size="9" font-weight="bold">
                    PASAPORTE SELLADO
                  </text>
                </g>
              </g>

              <!-- =================================================================== -->
              <!-- CAMIÓN LOGÍSTICO 3D REALISTA QUE RECORRE LA CARRETERA CON EL SCROLL -->
              <!-- =================================================================== -->
              <g [attr.transform]="'translate(' + cameraWorldPos().x + ', ' + cameraWorldPos().y + ')'">
                <!-- Halo suave de telemetría bajo el vehículo -->
                <circle cx="0" cy="0" r="46" fill="#00C2FF" fill-opacity="0.14" />
                <circle cx="0" cy="0" r="28" fill="#00E5A3" fill-opacity="0.22" />

                <!-- Vehículo rotado según la tangente exacta de la autopista -->
                <g [attr.transform]="'rotate(' + truckAngleDeg() + ')'">
                  <!-- Haces de faros LED delanteros proyectando luz sobre el asfalto -->
                  <polygon
                    points="34,-8 120,-24 120,24 34,8"
                    fill="url(#headlightBeamGrad)" />

                  <!-- Sombra de contacto ambiental suave bajo el chasis -->
                  <ellipse cx="0" cy="2" rx="42" ry="16" fill="#0A2540" fill-opacity="0.45" />

                  <!-- Luces traseras de posición/frenado rojas -->
                  <circle cx="-38" cy="-8" r="3.5" fill="#EF4444" fill-opacity="0.9" />
                  <circle cx="-38" cy="8" r="3.5" fill="#EF4444" fill-opacity="0.9" />
                  <circle cx="-38" cy="-8" r="6" fill="#EF4444" fill-opacity="0.35" />
                  <circle cx="-38" cy="8" r="6" fill="#EF4444" fill-opacity="0.35" />

                  <!-- Render 3D Fotorrealista del Camión Logístico (PNG con fondo transparente, 100% opaco) -->
                  <image
                    [attr.href]="assets.truckVehicle"
                    x="-42"
                    y="-23.6"
                    width="84"
                    height="47.25"
                    preserveAspectRatio="xMidYMid meet" />

                  <!-- Anillo sutil de telemetría GPS PostGIS -->
                  <circle cx="0" cy="0" r="32" fill="none" stroke="#00C2FF" stroke-width="1.5" stroke-dasharray="6 4" stroke-opacity="0.65" />
                </g>
              </g>
            </svg>

          </div>
        </div>

        <!-- ========================================================================= -->
        <!-- ÚNICA TARJETA NARRATIVA FLOTANTE POR ESTACIÓN (ESTILO SCROLL-WORLD PURO)  -->
        <!-- ========================================================================= -->
        <div class="relative z-20 max-w-7xl w-full h-full mx-auto px-6 sm:px-10 pointer-events-none flex items-center">
          <div class="w-full">
            @for (st of stations; track st.id; let idx = $index) {
              <div
                class="transition-all duration-150 ease-out max-w-[400px]"
                [class.ml-auto]="st.cardSide === 'right'"
                [class.pointer-events-auto]="getCardOpacity(idx) > 0.5"
                [style.opacity]="getCardOpacity(idx)"
                [style.transform]="getCardTransform(idx)"
                [style.display]="getCardOpacity(idx) <= 0.02 ? 'none' : 'block'">

                <article class="rounded-2xl bg-white/90 border border-slate-200/80 shadow-[0_20px_50px_-15px_rgba(10,37,64,0.14)] backdrop-blur-xl p-6 sm:p-7 space-y-3.5 text-[#0A2540]">
                  <span class="inline-flex items-center gap-1.5 text-[11px] font-mono font-bold uppercase tracking-wider text-[#0066FF]">
                    <span class="h-2 w-2 rounded-full bg-[#00E5A3]"></span>
                    {{ st.indexLabel }} · {{ st.badge }}
                  </span>

                  <h3 class="text-2xl font-bold tracking-[-0.025em] text-[#0A2540] leading-snug">
                    {{ st.title }}
                  </h3>

                  <p class="text-sm text-[#425466] leading-relaxed">
                    {{ st.subtitle }}
                  </p>

                  <div class="pt-3 border-t border-slate-200/70 flex items-center justify-between gap-4">
                    <div>
                      <span class="text-[10px] font-mono uppercase tracking-wider text-[#425466] block">
                        {{ st.metricLabel }}
                      </span>
                      <strong class="text-sm font-mono font-bold text-transparent bg-clip-text bg-gradient-to-r from-[#0066FF] to-[#00C2FF]">
                        {{ st.metricValue }}
                      </strong>
                    </div>

                    @if (idx === 3) {
                      <a
                        routerLink="/public-trace/HU-2026-EC-089"
                        class="inline-flex items-center gap-1.5 rounded-[8px] bg-[#0066FF] hover:bg-[#0052D9] px-4 py-2 text-xs font-medium text-white transition-colors shadow-sm">
                        <span>Verificar QR</span>
                        <span>&gt;</span>
                      </a>
                    }
                  </div>
                </article>
              </div>
            }
          </div>
        </div>

        <!-- Línea de progreso minimalista de 3px al borde inferior (Idéntica a scrub-engine.js) -->
        <div class="absolute bottom-0 left-0 right-0 h-1 bg-slate-200/60 z-30">
          <div
            class="h-full bg-gradient-to-r from-[#0066FF] via-[#00C2FF] to-[#00E5A3]"
            [style.width.%]="progressPercent()">
          </div>
        </div>

      </div>
    </section>
  `
})
export class ScrollWorldComponent implements AfterViewInit, OnDestroy {
  @ViewChild('scrollTrack', { static: true }) scrollTrackRef!: ElementRef<HTMLElement>;

  readonly assets = SCROLL_WORLD_ASSETS;

  readonly stations: readonly ScrollStation[] = [
    {
      id: 'bodega-central',
      indexLabel: '01',
      badge: 'Bodega Central',
      title: 'Escaneo láser y sellado QR en origen',
      subtitle:
        'Cada bidón de 60L de amonio cuaternario, cloro o detergente enzimático recibe su identidad criptográfica antes de abordar la unidad.',
      metricLabel: 'Lote Activo',
      metricValue: '60 L · Amonio Cuaternario 5G',
      cardSide: 'left',
      range: [0.0, 0.22]
    },
    {
      id: 'ruteo-postgis',
      indexLabel: '02',
      badge: 'Ruteo PostGIS',
      title: 'Desvío dinámico ante cierres viales',
      subtitle:
        'El motor recalcula la ruta en tiempo real cuando detecta un bloqueo en carretera, preservando las entregas previas sin retrasos.',
      metricLabel: 'Corredor Activo',
      metricValue: 'Unidad R-01 · 08 Hospitales',
      cardSide: 'left',
      range: [0.28, 0.48]
    },
    {
      id: 'cobertura-90d',
      indexLabel: '03',
      badge: 'Predicción 90 Días',
      title: 'Control de nivel y alerta 5 días antes',
      subtitle:
        'Monitorea la reserva física de cada tanque en los hospitales y dispara la orden de reposición antes de comprometer el stock.',
      metricLabel: 'Umbral Preventivo',
      metricValue: 'Cobertura <= LeadTime + 3d',
      cardSide: 'right',
      range: [0.54, 0.74]
    },
    {
      id: 'pasaporte-qr',
      indexLabel: '04',
      badge: 'Custodia Offline',
      title: 'Recepción con firma táctil y sello QR',
      subtitle:
        'En destino, la receptora escanea el envase y firma en pantalla táctil incluso en modo avión, sellando el pasaporte SHA-256.',
      metricLabel: 'Pasaporte Público',
      metricValue: 'HU-2026-EC-089 · Verificado',
      cardSide: 'left',
      range: [0.80, 1.0]
    }
  ];

  readonly pinMode = signal<'before' | 'pinned' | 'after'>('before');
  readonly progress = signal<number>(0);
  readonly progressPercent = computed(() => Math.round(this.progress() * 100));

  readonly cameraWorldPos = computed(() => this.evaluateHighwaySpline(this.progress()));

  readonly truckAngleDeg = computed(() => {
    const p = this.progress();
    const delta = 0.008;
    const p1 = this.evaluateHighwaySpline(Math.max(0, p - delta));
    const p2 = this.evaluateHighwaySpline(Math.min(1, p + delta));
    return Math.round((Math.atan2(p2.y - p1.y, p2.x - p1.x) * 180) / Math.PI);
  });

  readonly worldCameraTransform = computed(() => {
    const pos = this.cameraWorldPos();
    const p = this.progress();
    const stationPhase = Math.cos(p * Math.PI * 3);
    const zoom = 0.92 + Math.abs(stationPhase) * 0.14;
    return `scale(${zoom.toFixed(3)}) translate3d(${(-pos.x).toFixed(1)}px, ${(-pos.y).toFixed(1)}px, 0)`;
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

  getStationLocalProgress(index: number): number {
    const p = this.progress();
    const [start, end] = this.stations[index].range;
    return Math.max(0, Math.min(1, (p - start) / Math.max(0.01, end - start)));
  }

  getCardOpacity(index: number): number {
    const p = this.progress();
    const [start, end] = this.stations[index].range;
    const fadeSpan = 0.04;

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
    const offsetY = (1 - opacity) * 20;
    return `translate3d(0, ${offsetY.toFixed(1)}px, 0)`;
  }

  private evaluateHighwaySpline(p: number): { x: number; y: number } {
    const clamped = Math.max(0, Math.min(1, p));
    const segments = [
      {
        p0: { x: 0, y: 0 },
        c1: { x: 380, y: 0 },
        c2: { x: 340, y: 520 },
        p1: { x: 720, y: 520 }
      },
      {
        p0: { x: 720, y: 520 },
        c1: { x: 1100, y: 520 },
        c2: { x: 340, y: 1060 },
        p1: { x: -40, y: 1060 }
      },
      {
        p0: { x: -40, y: 1060 },
        c1: { x: -420, y: 1060 },
        c2: { x: 300, y: 1600 },
        p1: { x: 680, y: 1600 }
      }
    ];

    const scaled = clamped * segments.length;
    const idx = Math.min(segments.length - 1, Math.floor(scaled));
    const t = scaled - idx;
    const seg = segments[idx];
    const inv = 1 - t;

    const x =
      inv * inv * inv * seg.p0.x +
      3 * inv * inv * t * seg.c1.x +
      3 * inv * t * t * seg.c2.x +
      t * t * t * seg.p1.x;

    const y =
      inv * inv * inv * seg.p0.y +
      3 * inv * inv * t * seg.c1.y +
      3 * inv * t * t * seg.c2.y +
      t * t * t * seg.p1.y;

    return { x, y };
  }

  private updateScrollTarget(): void {
    if (typeof window === 'undefined') return;
    const track = this.scrollTrackRef?.nativeElement;
    if (!track) return;

    const headerHeight = 64;
    const stageHeight = Math.max(320, window.innerHeight - headerHeight);
    const rect = track.getBoundingClientRect();
    const scrollable = Math.max(1, rect.height - stageHeight);
    const scrolled = headerHeight - rect.top;

    if (rect.top > headerHeight) {
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
        this.currentProgress += diff * 0.12;
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
