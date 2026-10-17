import {
  AfterViewInit,
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  ViewChild,
  computed,
  effect,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthSession } from '@entities/session';
import {
  LucideQrCode,
  LucideArrowUpRight,
  LucidePlus,
  LucidePhone,
  LucideMail,
  LucideMapPin
} from '@lucide/angular';
import { SCROLL_WORLD_ASSETS } from './components/scroll-world/scroll-world-assets';

export interface LandingStation {
  readonly id: string;
  readonly code: string;
  readonly badge: string;
  readonly range: readonly [number, number];
}

interface InsumoDestacado {
  readonly numero: string;
  readonly nombre: string;
  readonly detalle: string;
  readonly categoria: string;
}

interface ExperienciaLogistica {
  readonly nombre: string;
  readonly descripcion: string;
  readonly precio: string;
  readonly periodo: string;
  readonly cobertura: string;
  readonly etiqueta?: string;
}

interface FaqItem {
  readonly id: string;
  readonly pregunta: string;
  readonly respuesta: string;
}

@Component({
  selector: 'page-landing',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    LucideQrCode,
    LucideArrowUpRight,
    LucidePlus,
    LucidePhone,
    LucideMail,
    LucideMapPin
  ],
  template: `
    <div id="landing-root" class="bg-[#060D1A] text-[#F0F4F8] selection:bg-[#00C2FF] selection:text-[#060D1A] overflow-x-clip min-h-screen">

      <!-- ========================================================================= -->
      <!-- 1. HEADER FLOTANTE TIPO PÍLDORA (CALCA MIDUDEV)                           -->
      <!-- ========================================================================= -->
      <a
        href="#recorrido"
        class="fixed top-4 left-4 z-[60] -translate-y-24 focus-visible:translate-y-0 px-4 py-2 rounded-full bg-[#0066FF] text-white font-zen text-xs font-medium transition-transform shadow-lg">
        Saltar al contenido
      </a>

      <header class="pointer-events-none fixed inset-x-0 top-0 z-50 flex justify-center px-4 pt-6 lg:justify-start lg:px-8 xl:px-20">
        <div class="pointer-events-auto flex w-max items-center gap-6 rounded-full bg-[#060D1A]/75 p-2 pl-6 ring-1 ring-[#F0F4F8]/15 backdrop-blur-xl lg:gap-5 shadow-2xl">
          <a routerLink="/" class="flex items-center text-[#F0F4F8] font-display text-base font-normal tracking-widest hover:text-[#00C2FF] transition-colors" aria-label="IstpetDev, volver al inicio">
            <span>ISTPET DEV</span>
          </a>

          <nav aria-label="Principal" class="relative hidden lg:block">
            <ul class="relative flex items-center text-sm font-zen text-[#F0F4F8]/70">
              <li>
                <a href="#recorrido" class="flex h-9 items-center rounded-full px-3 transition-colors duration-300 hover:text-[#F0F4F8]">
                  Recorrido
                </a>
              </li>
              <li>
                <a href="#filosofia" class="flex h-9 items-center rounded-full px-3 transition-colors duration-300 hover:text-[#F0F4F8]">
                  Filosofía
                </a>
              </li>
              <li>
                <a href="#insumos" class="flex h-9 items-center rounded-full px-3 transition-colors duration-300 hover:text-[#F0F4F8]">
                  Insumos
                </a>
              </li>
              <li>
                <a href="#prensa" class="flex h-9 items-center rounded-full px-3 transition-colors duration-300 hover:text-[#F0F4F8]">
                  Prensa
                </a>
              </li>
              <li>
                <a href="#modalidades" class="flex h-9 items-center rounded-full px-3 transition-colors duration-300 hover:text-[#F0F4F8]">
                  Modalidades
                </a>
              </li>
              <li>
                <a href="#preguntas" class="flex h-9 items-center rounded-full px-3 transition-colors duration-300 hover:text-[#F0F4F8]">
                  Preguntas
                </a>
              </li>
              <li>
                <a href="#auditoria" class="flex h-9 items-center rounded-full px-3 transition-colors duration-300 hover:text-[#F0F4F8]">
                  Auditoría
                </a>
              </li>
            </ul>
          </nav>

          <div class="flex items-center gap-2">
            <a
              routerLink="/public-trace"
              class="hidden sm:inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full border border-white/10 bg-white/5 text-xs font-zen text-[#F0F4F8]/80 hover:text-white transition-all">
              <svg lucideQrCode [size]="13" class="text-[#00C2FF]"></svg>
              <span>Rastrear QR</span>
            </a>

            <button
              type="button"
              (click)="session.login('/inicio')"
              class="inline-flex items-center gap-1.5 px-4 py-1.5 rounded-full bg-[#0066FF] hover:bg-[#0052D9] text-white text-xs font-zen font-medium tracking-wide shadow-md transition-all active:scale-95 cursor-pointer">
              <span>Torre de Control</span>
            </button>
          </div>
        </div>
      </header>


      <!-- ========================================================================= -->
      <!-- 2. SCROLL WORLD: EL PASEO POR LA CADENA LOGÍSTICA (CALCA MIDUDEV)         -->
      <!-- ========================================================================= -->
      <section
        #worldSection
        id="recorrido"
        aria-label="Paseo por las instalaciones logísticas"
        class="relative min-h-[500vh]">

        <!-- Escenario fijo (Stage): El video y las fotos ocupan toda la pantalla -->
        <div class="sticky top-0 h-screen overflow-hidden bg-[#060D1A]">
          
          <div aria-hidden="true" class="absolute inset-0">
            <!-- Escena 1 & Video Clip 2K MiniMax H3 -->
            <div
              class="frame absolute inset-0 transition-opacity duration-700 ease-out"
              [style.opacity]="getSceneOpacity(0)">
              <video
                #clip1Video
                muted
                playsinline
                preload="auto"
                class="absolute inset-0 size-full object-cover will-change-transform"
                [style.transform]="getSceneTransform(0)">
              </video>
              <img
                [src]="assets.walkthrough.scene1Warehouse"
                alt="Bodega Central Sur (Turubamba)"
                class="absolute inset-0 size-full object-cover -z-10"
                [style.transform]="getSceneTransform(0)" />
            </div>

            <!-- Escena 2: Sala de Telemetría Satelital PostGIS -->
            <div
              class="frame absolute inset-0 transition-opacity duration-700 ease-out"
              [style.opacity]="getSceneOpacity(1)">
              <img
                [src]="assets.walkthrough.scene2Telemetry"
                alt="Centro de Telemetría PostGIS"
                class="absolute inset-0 size-full object-cover will-change-transform"
                [style.transform]="getSceneTransform(1)" />
            </div>

            <!-- Escena 3: Planta Industrial y Silos de Reserva -->
            <div
              class="frame absolute inset-0 transition-opacity duration-700 ease-out"
              [style.opacity]="getSceneOpacity(2)">
              <img
                [src]="assets.walkthrough.scene3Silos"
                alt="Planta de Silos y Reserva"
                class="absolute inset-0 size-full object-cover will-change-transform"
                [style.transform]="getSceneTransform(2)" />
            </div>

            <!-- Escena 4: Esclusa Hospitalaria y Muelle Clínico -->
            <div
              class="frame absolute inset-0 transition-opacity duration-700 ease-out"
              [style.opacity]="getSceneOpacity(3)">
              <img
                [src]="assets.walkthrough.scene4Hospital"
                alt="Esclusa Hospitalaria y Entrega"
                class="absolute inset-0 size-full object-cover will-change-transform"
                [style.transform]="getSceneTransform(3)" />
            </div>
          </div>

          <!-- Velos exactos de Midudev: gradientes suaves para leer texto sin cuadros -->
          <div aria-hidden="true" class="absolute inset-x-0 top-0 h-40 bg-gradient-to-b from-[#060D1A]/80 to-transparent pointer-events-none"></div>
          <div aria-hidden="true" class="absolute inset-0 bg-gradient-to-t from-[#060D1A] from-15% via-[#060D1A]/50 via-50% to-transparent to-80% lg:bg-gradient-to-r lg:from-[#060D1A]/95 lg:via-[#060D1A]/50 lg:via-40% lg:to-transparent lg:to-70% pointer-events-none"></div>
          <div aria-hidden="true" class="absolute inset-x-0 bottom-0 hidden h-1/2 bg-gradient-to-t from-[#060D1A]/80 to-transparent lg:block pointer-events-none"></div>

          <!-- Raíl de paradas idéntico a Midudev -->
          <nav aria-label="Paradas del paseo logístico" class="absolute top-1/2 right-6 hidden -translate-y-1/2 lg:block xl:right-8 z-30 pointer-events-auto select-none">
            <ol class="relative flex flex-col items-end gap-4">
              <span aria-hidden="true" class="absolute top-3 right-1 bottom-3 w-px translate-x-1/2 bg-[#F0F4F8]/20">
                <span
                  class="absolute inset-0 origin-top bg-[#00C2FF] transition-transform duration-100 ease-out"
                  [style.transform]="'scaleY(' + (activeStationIndex() / (stations.length - 1)) + ')'">
                </span>
              </span>
              @for (st of stations; track st.id; let i = $index) {
                <li>
                  <button
                    type="button"
                    (click)="jumpTo(i)"
                    class="rail-link group flex h-6 items-center gap-3 cursor-pointer focus:outline-none"
                    [attr.aria-current]="activeStationIndex() === i ? 'true' : 'false'">
                    <span class="rail-label rounded-full bg-[#060D1A]/80 px-3 py-1 font-zen text-xs whitespace-nowrap text-[#F0F4F8] ring-1 ring-white/10 backdrop-blur-xl">
                      {{ st.badge }}
                    </span>
                    <span class="relative grid size-2 place-items-center">
                      <span class="rail-dot size-2 rounded-full bg-[#F0F4F8]/50"></span>
                    </span>
                  </button>
                </li>
              }
            </ol>
          </nav>

          <!-- Indicador animado de scroll (Hint) -->
          <p
            aria-hidden="true"
            class="absolute right-8 bottom-8 hidden items-center gap-3 font-zen text-xs tracking-widest text-[#F0F4F8]/70 uppercase lg:flex pointer-events-none transition-opacity duration-300"
            [style.opacity]="progress() < 0.15 ? '1' : '0'">
            Desliza para conocer el recorrido
            <span class="hint-line relative block h-8 w-px overflow-hidden bg-[#F0F4F8]/20"></span>
          </p>
        </div>

        <!-- Paradas del recorrido: artículos que hacen scroll y dejan su texto abajo a la izquierda SIN CUADROS -->
        <div class="relative z-10 -mt-[100vh]">

          <!-- Parada 0: Inicio / Presentación -->
          <article id="inicio" class="stop pointer-events-none flex min-h-[160vh] flex-col justify-end" [class.is-current]="activeStationIndex() === 0">
            <div class="stop-body pointer-events-auto sticky bottom-0 flex w-full max-w-2xl xl:max-w-3xl flex-col items-start px-4 pt-32 pb-12 md:px-12 lg:pb-16 xl:px-20">
              <p class="font-zen text-xs font-medium tracking-widest text-[#00C2FF] uppercase">
                IstpetDev · Reto 1 Expo Clean Ecuador 2026
              </p>
              <h1 class="mt-4 font-display text-4xl lg:text-6xl text-[#F0F4F8] text-balance font-normal leading-tight">
                Abastecimiento y trazabilidad<br />de insumos a nivel nacional
              </h1>
              <p class="mt-4 max-w-md font-zen text-base text-[#F0F4F8]/80 lg:text-lg text-pretty">
                Anticipamos el desabastecimiento, decidimos desde qué bodega y en qué vehículo atender primero lo crítico, y garantizamos la custodia de cada entrega incluso sin señal.
              </p>
              <div class="mt-6 flex flex-wrap items-center gap-3">
                <button
                  type="button"
                  (click)="session.login('/inicio')"
                  class="btn btn-primary px-6 py-3 rounded-full bg-[#0066FF] hover:bg-[#00C2FF] text-white font-zen text-sm font-medium shadow-lg transition-all cursor-pointer">
                  <span>Ingresar a Torre de Control</span>
                </button>
                <a
                  routerLink="/public-trace"
                  class="btn btn-ghost px-5 py-3 rounded-full ring-1 ring-[#F0F4F8]/20 hover:bg-[#F0F4F8] hover:text-[#060D1A] text-[#F0F4F8] font-zen text-sm font-medium transition-all">
                  <span>Rastrear paquete con QR</span>
                </a>
              </div>
              <ul class="mt-6 flex flex-wrap gap-2 text-xs text-[#F0F4F8]/80 font-zen" aria-label="Capacidades operativas">
                <li class="rounded-full bg-[#060D1A]/60 px-3 py-1 ring-1 ring-[#F0F4F8]/20 backdrop-blur-md">Cadena completa en una sola operación</li>
                <li class="rounded-full bg-[#060D1A]/60 px-3 py-1 ring-1 ring-[#F0F4F8]/20 backdrop-blur-md">Prioridad explicable con datos</li>
                <li class="rounded-full bg-[#060D1A]/60 px-3 py-1 ring-1 ring-[#F0F4F8]/20 backdrop-blur-md">Operación de campo 100% offline</li>
              </ul>
            </div>
          </article>

          <!-- Parada 1: Bodegas de Origen -->
          <article id="bodega" class="stop pointer-events-none flex min-h-[160vh] flex-col justify-end" [class.is-current]="activeStationIndex() === 1">
            <div class="stop-body pointer-events-auto sticky bottom-0 flex w-full max-w-2xl xl:max-w-3xl flex-col items-start px-4 pt-32 pb-12 md:px-12 lg:pb-16 xl:px-20">
              <p class="flex items-center gap-2 font-zen text-xs tracking-widest text-[#F0F4F8]/60 uppercase">
                <span class="tabular-nums font-mono text-[#00C2FF]">01 / 04</span>
                <span class="size-1 rounded-full bg-[#00E5A3]"></span>
                <span>Bodegas Central Sur, Norte y Valle</span>
              </p>
              <p class="mt-4 font-zen text-xs font-medium tracking-widest text-[#00C2FF] uppercase">Despacho y Precinto QR</p>
              <h2 class="mt-2 max-w-xl font-display text-4xl lg:text-5xl xl:text-6xl text-[#F0F4F8] text-balance font-normal leading-tight">
                Control de bultos y asignación de carga
              </h2>
              <p class="mt-4 max-w-md font-zen text-base text-[#F0F4F8]/80 lg:text-lg text-pretty">
                Cada caja o caneca se registra con un código QR único (unidad logística). Se valida peso y volumen antes de asignar el camión de reparto.
              </p>
              <ol class="mt-6 flex max-w-md flex-col gap-4 font-zen">
                <li class="flex gap-4 items-start">
                  <span class="font-display text-2xl text-[#00C2FF] tabular-nums">1</span>
                  <div>
                    <h3 class="text-base text-[#F0F4F8] font-medium">Selección de Insumos</h3>
                    <p class="text-sm text-[#F0F4F8]/70">Reserva inmediata de hipoclorito, amonio cuaternario y consumibles críticos.</p>
                  </div>
                </li>
                <li class="flex gap-4 items-start">
                  <span class="font-display text-2xl text-[#00C2FF] tabular-nums">2</span>
                  <div>
                    <h3 class="text-base text-[#F0F4F8] font-medium">Sellado Digital con QR</h3>
                    <p class="text-sm text-[#F0F4F8]/70">Registro digital inmutable por bulto para garantizar la custodia de inicio a fin.</p>
                  </div>
                </li>
                <li class="flex gap-4 items-start">
                  <span class="font-display text-2xl text-[#00C2FF] tabular-nums">3</span>
                  <div>
                    <h3 class="text-base text-[#F0F4F8] font-medium">Asignación a Flota</h3>
                    <p class="text-sm text-[#F0F4F8]/70">Elección de vehículo según capacidad (Hino Dutro, Chevrolet N400 o BYD T35 eléctrico).</p>
                  </div>
                </li>
              </ol>
              <a href="#insumos" class="link mt-6 font-zen text-sm text-[#00C2FF]">Ver insumos y presentaciones</a>
            </div>
          </article>

          <!-- Parada 2: Planificación de Rutas -->
          <article id="telemetria" class="stop pointer-events-none flex min-h-[160vh] flex-col justify-end" [class.is-current]="activeStationIndex() === 2">
            <div class="stop-body pointer-events-auto sticky bottom-0 flex w-full max-w-2xl xl:max-w-3xl flex-col items-start px-4 pt-32 pb-12 md:px-12 lg:pb-16 xl:px-20">
              <p class="flex items-center gap-2 font-zen text-xs tracking-widest text-[#F0F4F8]/60 uppercase">
                <span class="tabular-nums font-mono text-[#00C2FF]">02 / 04</span>
                <span class="size-1 rounded-full bg-[#00E5A3]"></span>
                <span>Planificación de Rutas y Tráfico en Quito</span>
              </p>
              <p class="mt-4 font-zen text-xs font-medium tracking-widest text-[#00C2FF] uppercase">Planificación Inteligente</p>
              <h2 class="mt-2 max-w-xl font-display text-4xl lg:text-5xl xl:text-6xl text-[#F0F4F8] text-balance font-normal leading-tight">
                Rutas priorizadas según ventanas y estado vial
              </h2>
              <p class="mt-4 max-w-md font-zen text-base text-[#F0F4F8]/80 lg:text-lg text-pretty">
                Organizamos paradas respetando horarios restringidos (ej. centros comerciales de 07:00 a 09:30) y restricciones de Quito, con recálculo automático ante incidentes o cierres viales.
              </p>
              <dl class="mt-6 grid w-full max-w-md grid-cols-3 gap-4">
                <div class="flex flex-col-reverse gap-1">
                  <dt class="font-zen text-xs text-[#F0F4F8]/60">Ventanas de entrega</dt>
                  <dd class="font-display text-2xl lg:text-3xl text-[#00C2FF]">100% integradas</dd>
                </div>
                <div class="flex flex-col-reverse gap-1">
                  <dt class="font-zen text-xs text-[#F0F4F8]/60">Recálculo vial</dt>
                  <dd class="font-display text-2xl lg:text-3xl text-[#00E5A3]">Automático</dd>
                </div>
                <div class="flex flex-col-reverse gap-1">
                  <dt class="font-zen text-xs text-[#F0F4F8]/60">Desvíos sin justificar</dt>
                  <dd class="font-display text-2xl lg:text-3xl text-[#F0F4F8]">0%</dd>
                </div>
              </dl>
              <a href="#modalidades" class="link mt-6 font-zen text-sm text-[#00C2FF]">Consultar opciones de flota</a>
            </div>
          </article>

          <!-- Parada 3: Reabastecimiento Predictivo -->
          <article id="reserva" class="stop pointer-events-none flex min-h-[160vh] flex-col justify-end" [class.is-current]="activeStationIndex() === 3">
            <div class="stop-body pointer-events-auto sticky bottom-0 flex w-full max-w-2xl xl:max-w-3xl flex-col items-start px-4 pt-32 pb-12 md:px-12 lg:pb-16 xl:px-20">
              <p class="flex items-center gap-2 font-zen text-xs tracking-widest text-[#F0F4F8]/60 uppercase">
                <span class="tabular-nums font-mono text-[#00C2FF]">03 / 04</span>
                <span class="size-1 rounded-full bg-[#00E5A3]"></span>
                <span>Reabastecimiento Predictivo</span>
              </p>
              <p class="mt-4 font-zen text-xs font-medium tracking-widest text-[#00C2FF] uppercase">Cálculo de Cobertura</p>
              <h2 class="mt-2 max-w-xl font-display text-4xl lg:text-5xl xl:text-6xl text-[#F0F4F8] text-balance font-normal leading-tight">
                Reposición preventiva con 5 días de anticipación
              </h2>
              <p class="mt-4 max-w-md font-zen text-base text-[#F0F4F8]/80 lg:text-lg text-pretty">
                El sistema evalúa el consumo diario frente al lead time de transporte más 3 días de margen de seguridad. Si el stock no cubre este umbral, se genera la orden de reposición antes del quiebre.
              </p>
              <div class="mt-6 flex items-end gap-4">
                <span class="font-display text-5xl text-[#00E5A3] lg:text-7xl">5 d</span>
                <span class="max-w-48 pb-1 font-zen text-sm text-[#F0F4F8]/70">Umbral preventivo: tiempo de viaje más 3 días de reserva de seguridad</span>
              </div>
              <a href="#modalidades" class="link mt-6 font-zen text-sm text-[#00C2FF]">Ver cobertura de servicio</a>
            </div>
          </article>

          <!-- Parada 4: Muelle Hospitalario y Recepción -->
          <article id="esclusa" class="stop pointer-events-none flex min-h-[160vh] flex-col justify-end" [class.is-current]="activeStationIndex() === 4">
            <div class="stop-body pointer-events-auto sticky bottom-0 flex w-full max-w-2xl xl:max-w-3xl flex-col items-start px-4 pt-32 pb-12 md:px-12 lg:pb-16 xl:px-20">
              <p class="flex items-center gap-2 font-zen text-xs tracking-widest text-[#F0F4F8]/60 uppercase">
                <span class="tabular-nums font-mono text-[#00C2FF]">04 / 04</span>
                <span class="size-1 rounded-full bg-[#00E5A3]"></span>
                <span>Muelle de Entrega y Recepción de Campo</span>
              </p>
              <p class="mt-4 font-zen text-xs font-medium tracking-widest text-[#00C2FF] uppercase">Custodia y Recepción Verificada</p>
              <h2 class="mt-2 max-w-xl font-display text-4xl lg:text-5xl xl:text-6xl text-[#F0F4F8] text-balance font-normal leading-tight">
                Recepción sin internet y firma digital en pantalla
              </h2>
              <p class="mt-4 max-w-md font-zen text-base text-[#F0F4F8]/80 lg:text-lg text-pretty">
                El conductor y el encargado del hospital escanean el QR, verifican cantidades y firman en el teléfono celular sin necesitar cobertura. Todo se sincroniza automáticamente sin duplicar registros.
              </p>
              <ul class="mt-6 grid w-full max-w-md grid-cols-2 gap-4 font-zen">
                <li class="flex flex-col gap-1 rounded-2xl bg-[#060D1A]/60 p-4 ring-1 ring-white/10 backdrop-blur-md">
                  <span class="font-display text-2xl lg:text-3xl text-[#00E5A3]">100 %</span>
                  <span class="text-xs text-[#F0F4F8]/70 lg:text-sm">Custodia verificada</span>
                </li>
                <li class="flex flex-col gap-1 rounded-2xl bg-[#060D1A]/60 p-4 ring-1 ring-white/10 backdrop-blur-md">
                  <span class="font-display text-2xl lg:text-3xl text-[#00C2FF]">Offline</span>
                  <span class="text-xs text-[#F0F4F8]/70 lg:text-sm">PWA con firma táctil en pantalla</span>
                </li>
              </ul>
              <button
                type="button"
                (click)="session.login('/inicio')"
                class="btn btn-primary mt-6 px-6 py-3 rounded-full bg-[#0066FF] hover:bg-[#00C2FF] text-white font-zen text-sm font-medium shadow-md transition-all cursor-pointer">
                <span>Acceder a la Plataforma</span>
              </button>
            </div>
          </article>
        </div>
      </section>


      <!-- ========================================================================= -->
      <!-- 3. TAGLINE / FILOSOFÍA CON REVELADO PALABRA POR PALABRA (CALCA MIDUDEV)   -->
      <!-- ========================================================================= -->
      <section #filosofiaSection id="filosofia" aria-label="Nuestra filosofía" class="relative overflow-hidden px-4 py-32 md:px-12 xl:px-24">
        <!-- Espiral geométrica concéntrica idéntica a Midudev -->
        <svg
          viewBox="0 0 100 100"
          fill="none"
          stroke="currentColor"
          class="pointer-events-none absolute top-1/2 left-1/2 w-[min(140vw,960px)] -translate-x-1/2 -translate-y-1/2 text-[#F0F4F8]/10 stroke-[0.3]">
          <circle cx="50" cy="50" r="10" />
          <circle cx="50" cy="50" r="18" />
          <circle cx="50" cy="50" r="26" />
          <circle cx="50" cy="50" r="34" />
          <circle cx="50" cy="50" r="42" />
          <circle cx="50" cy="50" r="50" />
        </svg>

        <div class="relative mx-auto flex min-h-[60vh] max-w-[760px] items-center justify-center text-center">
          <p class="font-display text-3xl sm:text-4xl lg:text-6xl [text-wrap:balance] leading-[1.3]">
            @for (line of taglineLines; track $index; let lIdx = $index) {
              <span class="block">
                @for (word of line; track $index; let wIdx = $index) {
                  <span
                    class="tagline-word inline-block mx-1.5 my-1 transition-colors duration-700 ease-[cubic-bezier(0.32,0.72,0,1)] text-[#F0F4F8]/30"
                    [class.text-[#F0F4F8]]="isWordLitIndex(lIdx, wIdx)">
                    {{ word }}
                  </span>
                }
              </span>
            }
          </p>
        </div>
      </section>


      <!-- ========================================================================= -->
      <!-- 4. CATÁLOGO DE INSUMOS DESTACADOS (CALCA EXACTA DISHES.ASTRO MIDUDEV)    -->
      <!-- ========================================================================= -->
      <section id="insumos" aria-labelledby="insumos-title" class="px-4 py-24 md:px-12 xl:px-24 border-t border-[#F0F4F8]/10">
        <div class="mx-auto max-w-6xl">
          <div class="flex flex-col gap-6 md:flex-row md:items-end md:justify-between">
            <div class="max-w-xl">
              <p class="text-sm font-zen text-[#00C2FF] uppercase tracking-widest">Insumos críticos de higiene</p>
              <h2 id="insumos-title" class="mt-4 font-display text-3xl lg:text-5xl text-[#F0F4F8] font-normal">
                Lo que llega a los centros de salud
              </h2>
            </div>
            <button
              type="button"
              (click)="session.login('/datos')"
              class="link inline-flex items-center gap-2 self-start font-zen text-sm text-[#00C2FF] hover:underline md:self-auto cursor-pointer">
              <span>Ver catálogo completo</span>
              <svg lucideArrowUpRight [size]="16"></svg>
            </button>
          </div>

          <ol class="mt-12 divide-y divide-[#F0F4F8]/10">
            @for (item of insumos; track item.numero) {
              <li class="grid grid-cols-[auto_1fr] items-baseline gap-x-6 gap-y-1 py-6 md:grid-cols-[auto_1fr_auto] md:items-center lg:py-8">
                <span class="font-display text-base text-[#00C2FF] tabular-nums font-normal">
                  {{ item.numero }}
                </span>
                <div>
                  <h3 class="font-display text-2xl lg:text-3xl text-[#F0F4F8] font-normal">
                    {{ item.nombre }}
                  </h3>
                  <p class="mt-2 font-zen text-base text-[#F0F4F8]/70">
                    {{ item.detalle }}
                  </p>
                </div>
                <div class="col-start-2 mt-4 flex items-center gap-2 md:col-start-3 md:mt-0">
                  <span class="rounded-full bg-[#060D1A]/80 px-3 py-1 font-zen text-xs text-[#00E5A3] ring-1 ring-[#00E5A3]/30">
                    {{ item.categoria }}
                  </span>
                </div>
              </li>
            }
          </ol>
        </div>
      </section>


      <!-- ========================================================================= -->
      <!-- 5. PROPUESTA DE VALOR OFICIAL (ENTREGABLE 2 · ANEXO 5.6)                   -->
      <!-- ========================================================================= -->
      <section id="prensa" aria-labelledby="prensa-title" class="px-4 py-24 md:px-12 xl:px-24 border-t border-[#F0F4F8]/10 bg-[#060D1A]">
        <figure class="mx-auto flex max-w-4xl flex-col items-center text-center">
          <p id="prensa-title" class="text-sm font-zen text-[#00C2FF] uppercase tracking-widest">
            Propuesta de Valor · Entregable Oficial 2
          </p>
          <blockquote class="mt-6">
            <p class="font-display text-2xl sm:text-3xl lg:text-4xl text-[#F0F4F8] font-normal leading-snug">
              “Anticipamos el faltante, decidimos desde qué bodega y en qué vehículo atender primero lo crítico, y demostramos cada entrega con código QR y firma digital, incluso sin señal.”
            </p>
            <p class="mx-auto mt-6 max-w-2xl font-zen text-base text-[#F0F4F8]/70 lg:text-lg">
              Frente a las hojas de cálculo, rutas fijas y listas de control en papel que hoy generan quiebres de stock y sobrecostos de transporte.
            </p>
          </blockquote>
          <figcaption class="mt-8 flex flex-col items-center gap-2 font-zen text-sm text-[#F0F4F8]/60">
            <span>IstpetDev · Reto 1 Expo Clean Ecuador 2026 · Gestión Inteligente de Procesos</span>
            <a routerLink="/public-trace" class="link inline-flex items-center gap-1.5 text-[#00C2FF] hover:underline">
              <span>Ver consulta pública de custodia QR</span>
              <svg lucideArrowUpRight [size]="14"></svg>
            </a>
          </figcaption>
        </figure>
      </section>


      <!-- ========================================================================= -->
      <!-- 6. PLANIFICACIÓN Y FLOTA (ENTREGABLE 2 · PASOS 4 Y 5)                      -->
      <!-- ========================================================================= -->
      <section id="modalidades" aria-labelledby="exp-title" class="px-4 py-24 md:px-12 xl:px-24 border-t border-[#F0F4F8]/10">
        <div class="mx-auto max-w-6xl">
          <div class="max-w-xl">
            <p class="text-sm font-zen text-[#00C2FF] uppercase tracking-widest">Planificación y Flota</p>
            <h2 id="exp-title" class="mt-4 font-display text-3xl lg:text-5xl text-[#F0F4F8] font-normal">
              Flota propia frente a contratada
            </h2>
            <p class="mt-6 font-zen text-base text-[#F0F4F8]/70 lg:text-lg">
              El sistema calcula el costo total de servir cada ruta (combustible, kilómetros, desgaste y personal de descarga) y compara con la tarifa de un operador contratado.
            </p>
          </div>

          <div class="mt-12 grid gap-6 lg:grid-cols-[1fr_1fr_0.9fr]">
            @for (exp of modalidades; track exp.nombre) {
              <article
                class="relative flex flex-col rounded-3xl p-6 ring-1 lg:p-8 bg-[#060D1A]"
                [class.ring-[#00C2FF]/60]="exp.etiqueta"
                [class.ring-[#F0F4F8]/15]="!exp.etiqueta">
                @if (exp.etiqueta) {
                  <p class="absolute -top-3 left-6 rounded-full bg-[#00C2FF] px-3 py-1 font-zen text-xs font-bold text-[#060D1A] lg:left-8">
                    {{ exp.etiqueta }}
                  </p>
                }
                <h3 class="font-display text-2xl text-[#F0F4F8] font-normal">{{ exp.nombre }}</h3>
                <p class="mt-2 font-zen text-base text-[#F0F4F8]/70">{{ exp.descripcion }}</p>
                <p class="mt-8 flex items-baseline gap-2">
                  <span class="font-display text-5xl text-white">{{ exp.precio }}</span>
                  <span class="font-zen text-sm text-[#F0F4F8]/50">{{ exp.periodo }}</span>
                </p>
                <p class="mt-1 font-zen text-sm text-[#F0F4F8]/50">{{ exp.cobertura }}</p>
                <div class="mt-8 flex flex-1 items-end">
                  <button
                    type="button"
                    (click)="session.login('/inicio')"
                    class="btn btn-primary w-full py-3 rounded-full bg-[#0066FF] hover:bg-[#00C2FF] text-white font-zen text-sm font-medium transition-all cursor-pointer">
                    <span>Ver en Torre de Control</span>
                  </button>
                </div>
              </article>
            }

            <!-- Tarjeta lateral con máscara oval: 3 Bodegas -->
            <article class="flex flex-col items-center gap-6 rounded-3xl p-6 text-center ring-1 ring-[#F0F4F8]/15 lg:p-8 bg-[#060D1A]">
              <div class="mirror-frame mask-oval w-40 lg:w-48">
                <img
                  [src]="assets.walkthrough.scene3Silos"
                  alt="Bodegas de Distribución"
                  class="aspect-[3/4] w-full mask-oval object-cover" />
              </div>
              <div>
                <h3 class="font-display text-2xl text-[#F0F4F8] font-normal">Red de 3 Bodegas</h3>
                <p class="mt-2 font-zen text-base text-[#F0F4F8]/70">
                  Suministro coordinado desde Bodega Central Sur (Turubamba), Bodega Norte (Carcelén) y Bodega Valle (Conocoto).
                </p>
              </div>
              <p class="font-display text-4xl text-[#00E5A3]">3 Bodegas</p>
            </article>
          </div>
        </div>
      </section>


      <!-- ========================================================================= -->
      <!-- 7. PREGUNTAS FRECUENTES (CALCA EXACTA FAQ.ASTRO MIDUDEV)                  -->
      <!-- ========================================================================= -->
      <section id="preguntas" aria-labelledby="faq-title" class="px-4 py-24 md:px-12 xl:px-24 border-t border-[#F0F4F8]/10">
        <div class="mx-auto grid max-w-6xl gap-12 lg:grid-cols-[1fr_1.6fr]">
          <div>
            <p class="font-zen text-sm text-[#00C2FF] uppercase tracking-widest">Preguntas frecuentes</p>
            <h2 id="faq-title" class="mt-4 font-display text-3xl lg:text-5xl text-[#F0F4F8] font-normal">
              Antes de operar
            </h2>
            <p class="mt-6 max-w-sm font-zen text-base text-[#F0F4F8]/70">
              ¿Tienes dudas sobre cómo funciona la plataforma o la entrega? Contáctanos directamente y te asesoramos.
            </p>
          </div>

          <div class="divide-y divide-[#F0F4F8]/10">
            @for (faq of faqs; track faq.id) {
              <details class="faq group">
                <summary class="flex cursor-pointer list-none items-center justify-between gap-6 rounded-xl py-6 text-left text-lg font-zen text-[#F0F4F8] hover:text-[#00C2FF] transition-colors">
                  <h3 class="text-lg font-normal">{{ faq.pregunta }}</h3>
                  <svg lucidePlus [size]="20" class="faq-icon text-[#00C2FF] shrink-0 transition-transform"></svg>
                </summary>
                <div class="pb-6">
                  <p class="max-w-2xl font-zen text-base text-[#F0F4F8]/70 leading-relaxed">
                    {{ faq.respuesta }}
                  </p>
                </div>
              </details>
            }
          </div>
        </div>
      </section>


      <!-- ========================================================================= -->
      <!-- 8. AUDITORÍA Y CERTIFICACIÓN (CALCA EXACTA GIFT.ASTRO MIDUDEV)            -->
      <!-- ========================================================================= -->
      <section id="auditoria" aria-labelledby="auditoria-title" class="px-4 py-24 md:px-12 xl:px-24 border-t border-[#F0F4F8]/10">
        <div class="mx-auto grid max-w-6xl items-center gap-12 md:grid-cols-2">
          <div class="mirror-frame mask-mirror mx-auto w-full max-w-sm">
            <img
              [src]="assets.walkthrough.scene1Warehouse"
              alt="Bodega Central Sur e inspección y despacho"
              class="aspect-[4/5] w-full mask-mirror object-cover" />
          </div>
          <div>
            <p class="font-zen text-sm text-[#00C2FF] uppercase tracking-widest">Transparencia y Control Público</p>
            <h2 id="auditoria-title" class="mt-4 font-display text-3xl lg:text-5xl text-[#F0F4F8] font-normal leading-tight">
              Transparencia total en cada paquete entregado
            </h2>
            <p class="mt-6 max-w-md font-zen text-base text-[#F0F4F8]/70 lg:text-lg leading-relaxed">
              Un comprobante digital único para cada entrega hospitalaria. Se consulta al instante desde cualquier teléfono celular con un código QR público, sin contraseñas ni trámites complicados.
            </p>
            <div class="mt-8 flex flex-wrap items-center gap-4">
              <a
                routerLink="/public-trace"
                class="btn btn-ghost inline-flex items-center gap-2 px-6 py-3 rounded-full ring-1 ring-[#F0F4F8]/20 hover:bg-[#F0F4F8] hover:text-[#060D1A] text-[#F0F4F8] font-zen text-sm font-medium transition-all">
                <svg lucideQrCode [size]="16" class="text-[#00C2FF]"></svg>
                <span>Verificar entrega con QR</span>
              </a>
              <button
                type="button"
                (click)="session.login('/datos')"
                class="link font-zen text-sm text-[#00C2FF] hover:underline cursor-pointer">
                <span>Ver inventario de insumos</span>
              </button>
            </div>
          </div>
        </div>
      </section>


      <!-- ========================================================================= -->
      <!-- 9. CIERRE FINAL / RESERVA (CALCA EXACTA FINALCTA.ASTRO MIDUDEV)           -->
      <!-- ========================================================================= -->
      <section id="final-cta" aria-labelledby="final-title" class="relative overflow-hidden px-4 py-32 md:px-12 xl:px-24 border-t border-[#F0F4F8]/10 bg-[#060D1A]">
        <svg
          viewBox="0 0 100 100"
          fill="none"
          stroke="currentColor"
          class="pointer-events-none absolute right-0 bottom-0 w-[640px] translate-x-1/3 translate-y-1/3 text-[#F0F4F8]/10 stroke-[0.3]">
          <circle cx="50" cy="50" r="15" />
          <circle cx="50" cy="50" r="30" />
          <circle cx="50" cy="50" r="45" />
        </svg>

        <div class="relative mx-auto flex max-w-[680px] flex-col items-center text-center">
          <!-- Marco en arco (mask-arch) idéntico a Midudev -->
          <div class="w-48 lg:w-56 mirror-frame mask-arch">
            <img
              [src]="assets.walkthrough.scene4Hospital"
              alt="Muelle Hospitalario"
              class="aspect-[3/4] w-full mask-arch object-cover" />
          </div>

          <h2 id="final-title" class="mt-12 font-display text-4xl lg:text-6xl text-[#F0F4F8] font-normal leading-tight">
            Te esperamos en la<br />Torre de Control
          </h2>
          <p class="mt-6 font-zen text-base text-[#F0F4F8]/70 lg:text-lg max-w-md">
            Monitoreo continuo de camiones e inventarios las 24 horas, los 7 días de la semana en todo el país.
          </p>
          <div class="mt-8 flex flex-col items-center gap-4">
            <button
              type="button"
              (click)="session.login('/inicio')"
              class="btn btn-primary px-8 py-3.5 rounded-full bg-[#0066FF] hover:bg-[#00C2FF] text-white font-zen text-sm font-medium shadow-xl transition-all cursor-pointer">
              <span>Ingresar a la Plataforma</span>
            </button>
            <p class="font-zen text-sm text-[#F0F4F8]/70">
              O consulta una entrega con código QR en <a routerLink="/public-trace" class="link text-[#00C2FF] hover:underline">Rastreo Público</a>
            </p>
          </div>
        </div>
      </section>


      <!-- ========================================================================= -->
      <!-- 9. FOOTER SEMÁNTICO (CALCA EXACTA FOOTER.ASTRO MIDUDEV)                   -->
      <!-- ========================================================================= -->
      <footer class="px-4 pt-24 pb-12 md:px-12 xl:px-24 border-t border-[#F0F4F8]/10 bg-[#060D1A]">
        <div class="grid gap-12 md:grid-cols-2 xl:grid-cols-4">
          
          <div class="flex flex-col gap-4">
            <div class="flex items-center text-[#F0F4F8] font-display text-lg tracking-widest">
              <span>ISTPET DEV</span>
            </div>
            <p class="max-w-xs font-zen text-sm text-[#F0F4F8]/70">
              Plataforma inteligente de optimización de rutas y control de entregas hospitalarias para el Reto 1 de Expo Clean Ecuador 2026.
            </p>
            <nav aria-label="Secciones del sitio">
              <ul class="flex flex-wrap gap-x-4 gap-y-2 font-zen text-sm text-[#F0F4F8]/70">
                <li><a href="#recorrido" class="hover:text-white transition-colors">Recorrido</a></li>
                <li><a href="#filosofia" class="hover:text-white transition-colors">Filosofía</a></li>
                <li><a href="#insumos" class="hover:text-white transition-colors">Insumos</a></li>
                <li><a href="#modalidades" class="hover:text-white transition-colors">Modalidades</a></li>
                <li><a href="#preguntas" class="hover:text-white transition-colors">Preguntas</a></li>
                <li><a href="#auditoria" class="hover:text-white transition-colors">Auditoría</a></li>
              </ul>
            </nav>
          </div>

          <div class="flex flex-col gap-2 font-zen text-sm">
            <h3 class="text-xs tracking-widest text-[#F0F4F8]/50 uppercase">Instalaciones</h3>
            <address class="not-italic text-[#F0F4F8]/80 leading-relaxed">
              Bodega Central Sur (Turubamba)<br />
              Bodega Norte (Carcelén) · Bodega Valle (Conocoto)<br />
              Quito, Ecuador
            </address>
            <a routerLink="/public-trace" class="inline-flex items-center gap-2 text-[#00C2FF] hover:underline pt-2">
              <svg lucideMapPin [size]="14"></svg>
              <span>Ver centros monitoreados</span>
            </a>
          </div>

          <div class="flex flex-col gap-2 font-zen text-sm">
            <h3 class="text-xs tracking-widest text-[#F0F4F8]/50 uppercase">Operación</h3>
            <p class="text-[#F0F4F8]/80">24 horas al día, 7 días a la semana</p>
            <p class="text-[#F0F4F8]/60 text-xs">Ubicación satelital en tiempo real</p>
          </div>

          <div class="flex flex-col gap-2 font-zen text-sm">
            <h3 class="text-xs tracking-widest text-[#F0F4F8]/50 uppercase">Contacto</h3>
            <a href="mailto:soporte@istpetdev.doicela.dev" class="inline-flex items-center gap-2 text-[#F0F4F8]/80 hover:text-white">
              <svg lucideMail [size]="14"></svg>
              <span>soporte&#64;istpetdev.doicela.dev</span>
            </a>
            <a href="tel:+593999999999" class="inline-flex items-center gap-2 text-[#F0F4F8]/80 hover:text-white">
              <svg lucidePhone [size]="14"></svg>
              <span>+593 (02) 299-9000</span>
            </a>
            <span class="inline-flex items-center gap-2 text-[#00E5A3] pt-1 text-xs">
              <span class="size-1.5 rounded-full bg-[#00E5A3] animate-pulse"></span>
              <span>Sistema de rutas activo</span>
            </span>
          </div>

        </div>

        <div class="mt-16 flex flex-col gap-4 pt-8 border-t border-[#F0F4F8]/10 font-zen text-xs text-[#F0F4F8]/50 md:flex-row md:items-center md:justify-between">
          <p>© 2026 IstpetDev Platform · Expo Clean Ecuador 2026</p>
          <ul class="flex flex-wrap gap-x-6 gap-y-2">
            <li><a routerLink="/public-trace" class="hover:text-white transition-colors">Trazabilidad Pública</a></li>
            <li><a routerLink="/simulation" class="hover:text-white transition-colors">Simulación 90d</a></li>
            <li><a routerLink="/inicio" class="hover:text-white transition-colors">Acceso Operativo</a></li>
          </ul>
        </div>
      </footer>

    </div>
  `
})
export class LandingPage implements AfterViewInit, OnDestroy {
  readonly session = inject(AuthSession);
  private readonly router = inject(Router);

  @ViewChild('worldSection') worldSectionRef?: ElementRef<HTMLElement>;
  @ViewChild('clip1Video') clip1VideoRef?: ElementRef<HTMLVideoElement>;
  @ViewChild('filosofiaSection') filosofiaSectionRef?: ElementRef<HTMLElement>;

  readonly assets = SCROLL_WORLD_ASSETS;

  readonly stations: readonly LandingStation[] = [
    { id: 'inicio', code: '00', badge: '00 INICIO', range: [0.0, 0.22] },
    { id: 'bodega', code: '01', badge: '01 BODEGA', range: [0.22, 0.46] },
    { id: 'telemetria', code: '02', badge: '02 RUTAS GPS', range: [0.46, 0.70] },
    { id: 'reserva', code: '03', badge: '03 RESERVA', range: [0.70, 0.88] },
    { id: 'esclusa', code: '04', badge: '04 ENTREGA', range: [0.88, 1.0] }
  ];

  readonly taglineLines = [
    ['Anticipar', 'el', 'faltante,'],
    ['atender', 'primero', 'lo', 'crítico,'],
    ['y', 'demostrar', 'cada', 'entrega'],
    ['incluso', 'sin', 'señal', 'de', 'internet.']
  ];

  readonly insumos: readonly InsumoDestacado[] = [
    {
      numero: '01',
      nombre: 'Hipoclorito de Sodio 5% (Caja de 4 Galones)',
      detalle: 'Desinfección general de pisos, superficies y áreas clínicas comunes (PRD-0001).',
      categoria: 'Desinfección General'
    },
    {
      numero: '02',
      nombre: 'Desinfectante Amonio Cuaternario (Caja de 4 Galones)',
      detalle: 'Limpieza profunda en quirófanos, áreas de aislamiento y neonatología (PRD-0002).',
      categoria: 'Desinfección Crítica'
    },
    {
      numero: '03',
      nombre: 'Detergente Industrial en Polvo (Saco de 30 kg)',
      detalle: 'Lavado intensivo de ropa hospitalaria, textiles e instrumental de limpieza (PRD-0003).',
      categoria: 'Lavado Pesado'
    },
    {
      numero: '04',
      nombre: 'Jabón Líquido Antibacterial (Caja de 4 Galones)',
      detalle: 'Higiene antiséptica de manos para personal sanitario y pacientes (PRD-0004).',
      categoria: 'Higiene de Manos'
    },
    {
      numero: '05',
      nombre: 'Alcohol Gel 70% (Caja de 12 Frascos de 1 L)',
      detalle: 'Dispensadores de desinfección rápida en pasillos y accesos hospitalarios (PRD-0005).',
      categoria: 'Bioseguridad'
    },
    {
      numero: '06',
      nombre: 'Guantes de Nitrilo (Caja de 100 Unidades)',
      detalle: 'Protección personal de operarios de limpieza y manejo de desechos (PRD-0010).',
      categoria: 'Protección Personal'
    }
  ];

  readonly modalidades: readonly ExperienciaLogistica[] = [
    {
      nombre: 'Flota Propia de Distribución',
      descripcion: 'Camiones Hino Dutro (3.500 a 6.500 kg), furgonetas Chevrolet N400 y camiones eléctricos BYD T35 para rutas urbanas y extraurbanas.',
      precio: '10 Vehículos',
      periodo: 'en red de Quito',
      cobertura: 'Cálculo de costo por km, combustible, desgaste y cuadrilla de descarga',
      etiqueta: 'Flota Dedicada'
    },
    {
      nombre: 'Transporte Externo Contratado',
      descripcion: 'Activación automática de operadores contratados cuando la demanda excede la capacidad de la flota propia o la distancia lo justifica.',
      precio: 'Tarifa Dinámica',
      periodo: 'según km y kg',
      cobertura: 'Comparación automática de costo total de servir antes de publicar la ruta'
    }
  ];

  readonly faqs: readonly FaqItem[] = [
    {
      id: 'faq1',
      pregunta: '¿Cómo funciona la firma y recepción sin internet en sótanos?',
      respuesta: 'La aplicación móvil PWA guarda el escaneo del QR, la firma táctil y la hora exacta en la memoria local (IndexedDB). Cuando el conductor recupera cobertura, los datos se sincronizan con la API central de forma idempotente, sin duplicar registros ni perder entregas.'
    },
    {
      id: 'faq2',
      pregunta: '¿Cómo calcula el sistema los 5 días de anticipación?',
      respuesta: 'El sistema calcula la media móvil de consumo diario de cada centro de salud y la compara contra el tiempo de viaje (lead time) más 3 días de margen de reserva obligatorio. Si las existencias no cubren este umbral, se genera de inmediato una orden de reposición.'
    },
    {
      id: 'faq3',
      pregunta: '¿Qué ocurre si hay un cierre de vía o bloqueo en la carretera?',
      respuesta: 'El sistema detecta el incidente vial, conserva todas las entregas que ya fueron completadas y recalcula automáticamente la secuencia de las paradas pendientes, priorizando los puntos con mayor criticidad hospitalaria.'
    },
    {
      id: 'faq4',
      pregunta: '¿Cómo puede cualquier persona verificar una entrega sin tener cuenta?',
      respuesta: 'Cualquier ciudadano, inspector o auditor puede escanear el código QR impreso en la caja o ingresar el código de la unidad en la página de rastreo público (/public-trace). Podrá consultar la fecha de salida de bodega, el vehículo asignado y la confirmación de recepción firmada.'
    }
  ];

  readonly progress = signal<number>(0);
  readonly litWordsCount = signal<number>(0);
  private videoBlobUrl: string | null = null;
  private targetProgress = 0;
  private currentProgress = 0;
  private rafId: number | null = null;

  readonly activeStationIndex = computed(() => {
    const p = this.progress();
    for (let i = 0; i < this.stations.length; i++) {
      const [start, end] = this.stations[i].range;
      if (p >= start && p < end) return i;
    }
    return this.stations.length - 1;
  });

  readonly activeStation = computed(() => this.stations[this.activeStationIndex()]);

  constructor() {
    effect(() => {
      if (this.session.status() === 'authenticated') {
        void this.router.navigateByUrl('/inicio');
      }
    });
  }

  ngAfterViewInit(): void {
    if (typeof window === 'undefined') return;
    void this.initVideoBlob(this.assets.walkthrough.clip1Video);
    this.updateScroll();
    this.updateTaglineReveal();
    this.startLoop();
  }

  ngOnDestroy(): void {
    if (this.rafId !== null && typeof cancelAnimationFrame === 'function') {
      cancelAnimationFrame(this.rafId);
      this.rafId = null;
    }
    if (this.videoBlobUrl && typeof URL !== 'undefined' && typeof URL.revokeObjectURL === 'function') {
      URL.revokeObjectURL(this.videoBlobUrl);
      this.videoBlobUrl = null;
    }
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.updateScroll();
    this.updateTaglineReveal();
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.updateScroll();
    this.updateTaglineReveal();
  }

  jumpTo(index: number): void {
    if (typeof window === 'undefined') return;
    const world = this.worldSectionRef?.nativeElement;
    if (!world) return;

    const [start, end] = this.stations[index].range;
    const targetP = (start + end) / 2;
    const rect = world.getBoundingClientRect();
    const scrollTop = window.scrollY || window.pageYOffset;
    const worldTop = rect.top + scrollTop;
    const worldHeight = world.offsetHeight - window.innerHeight;
    const targetY = worldTop + targetP * worldHeight;

    window.scrollTo({ top: targetY, behavior: 'smooth' });
  }

  getSceneOpacity(index: number): number {
    const p = this.progress();
    if (index === 0) {
      if (p <= 0.25) return 1;
      if (p >= 0.38) return 0;
      return 1 - (p - 0.25) / 0.13;
    }
    if (index === 1) {
      if (p < 0.22 || p > 0.65) return 0;
      if (p <= 0.35) return (p - 0.22) / 0.13;
      if (p >= 0.52) return 1 - (p - 0.52) / 0.13;
      return 1;
    }
    if (index === 2) {
      if (p < 0.48 || p > 0.85) return 0;
      if (p <= 0.62) return (p - 0.48) / 0.14;
      if (p >= 0.74) return 1 - (p - 0.74) / 0.11;
      return 1;
    }
    if (index === 3) {
      if (p <= 0.72) return 0;
      if (p >= 0.86) return 1;
      return (p - 0.72) / 0.14;
    }
    return 0;
  }

  getSceneTransform(index: number): string {
    const p = this.progress();
    const local = Math.max(0, Math.min(1, p * 4 - index));
    const scale = 1.0 + local * 0.06;
    return `scale(${scale.toFixed(3)})`;
  }

  isWordLitIndex(lIdx: number, wIdx: number): boolean {
    const offsets = [0, 4, 7, 10];
    const index = (offsets[lIdx] ?? 0) + wIdx;
    return index < this.litWordsCount();
  }

  private updateTaglineReveal(): void {
    if (typeof window === 'undefined') return;
    const el = this.filosofiaSectionRef?.nativeElement;
    if (!el) return;
    const rect = el.getBoundingClientRect();
    const windowH = window.innerHeight;
    const startY = windowH * 0.85;
    const endY = windowH * 0.35;
    const totalDist = rect.height + (startY - endY);
    if (totalDist <= 0) return;
    const currentDist = startY - rect.top;
    const p = Math.max(0, Math.min(1, currentDist / totalDist));
    this.litWordsCount.set(Math.round(p * 16));
  }

  private updateScroll(): void {
    if (typeof window === 'undefined') return;
    const world = this.worldSectionRef?.nativeElement;
    if (!world) return;

    const rect = world.getBoundingClientRect();
    const worldHeight = world.offsetHeight - window.innerHeight;
    if (worldHeight <= 0) return;

    const scrolled = -rect.top;
    this.targetProgress = Math.max(0, Math.min(1, scrolled / worldHeight));
  }

  private startLoop(): void {
    const tick = () => {
      const diff = this.targetProgress - this.currentProgress;
      if (Math.abs(diff) > 0.0004) {
        this.currentProgress += diff * 0.15;
        this.progress.set(this.currentProgress);
        this.syncVideoScrub(this.currentProgress);
      } else if (this.currentProgress !== this.targetProgress) {
        this.currentProgress = this.targetProgress;
        this.progress.set(this.currentProgress);
        this.syncVideoScrub(this.currentProgress);
      }

      this.rafId = requestAnimationFrame(tick);
    };

    this.rafId = requestAnimationFrame(tick);
  }

  private async initVideoBlob(src: string): Promise<void> {
    const video = this.clip1VideoRef?.nativeElement;
    if (!video) return;

    video.muted = true;
    video.playsInline = true;

    try {
      const res = await fetch(src);
      if (!res.ok) throw new Error(res.statusText);
      const blob = await res.blob();
      this.videoBlobUrl = URL.createObjectURL(blob);
      video.src = this.videoBlobUrl;
    } catch {
      video.src = src;
    }

    video.load();

    const loaded = await new Promise<boolean>((resolve) => {
      video.addEventListener('loadeddata', () => resolve(true), { once: true });
      video.addEventListener('error', () => resolve(false), { once: true });
    });

    if (loaded) {
      await video.play().catch(() => {});
      video.pause();
      try {
        video.currentTime = 0.01;
      } catch {
        // Ignored on locked decode
      }
    }
  }

  private syncVideoScrub(p: number): void {
    const video = this.clip1VideoRef?.nativeElement;
    if (!video || !video.duration || Number.isNaN(video.duration)) return;
    if (video.seeking) return;

    // Clip 1 covers scroll progress from 0.0 to 0.40
    const local = Math.max(0, Math.min(1, p / 0.40));
    const targetTime = local * Math.max(0, video.duration - 0.05);
    if (Math.abs(video.currentTime - targetTime) > 0.02) {
      try {
        video.currentTime = targetTime;
      } catch {
        // Ignored on locked decode
      }
    }
  }
}
