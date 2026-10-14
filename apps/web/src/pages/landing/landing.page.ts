import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthSession } from '@entities/session';

@Component({
  selector: 'page-landing',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink
  ],
  template: `
    <div class="relative overflow-hidden bg-white text-charcoal-800">
      
      <!-- ========================================================================= -->
      <!-- 1. BLOQUE HERO PRINCIPAL (Paleta Oficial Expo Clean Ecuador 2026)          -->
      <!-- ========================================================================= -->
      <section class="relative min-h-[90vh] flex flex-col justify-between pt-12 sm:pt-20 pb-16 overflow-hidden">
        
        <!-- Cinta Escultural de Marca (Clean Cyan -> Expo Lime -> Energy Orange) -->
        <div class="absolute right-[-140px] sm:right-[-80px] lg:right-[-40px] top-[-100px] w-[650px] sm:w-[920px] lg:w-[1200px] h-[750px] sm:h-[1020px] pointer-events-none select-none z-0">
          <svg viewBox="0 0 1200 1200" fill="none" xmlns="http://www.w3.org/2000/svg" class="w-full h-full">
            <defs>
              <!-- Gradiente Principal de Marca: Clean Cyan hacia Expo Lime y Energy Orange -->
              <linearGradient id="brandRibbonBody" x1="15%" y1="10%" x2="85%" y2="85%">
                <stop offset="0%" stop-color="#E6F7FA" stop-opacity="0.8" />
                <stop offset="20%" stop-color="#26BFD5" stop-opacity="0.9" />
                <stop offset="45%" stop-color="#00A7C4" stop-opacity="0.95" />
                <stop offset="70%" stop-color="#9EC200" stop-opacity="0.95" />
                <stop offset="85%" stop-color="#CEEC2D" stop-opacity="0.9" />
                <stop offset="100%" stop-color="#EA601F" stop-opacity="0.85" />
              </linearGradient>

              <!-- Pliegue Frontal Iluminado en Torsión Vertical -->
              <linearGradient id="brandRibbonFoldTwist" x1="90%" y1="0%" x2="10%" y2="100%">
                <stop offset="0%" stop-color="#97E2EC" stop-opacity="0.9" />
                <stop offset="30%" stop-color="#00A7C4" stop-opacity="0.95" />
                <stop offset="65%" stop-color="#9EC200" stop-opacity="0.95" />
                <stop offset="100%" stop-color="#EA601F" stop-opacity="0.9" />
              </linearGradient>

              <!-- Desenfoque Gaussiano Orgánico Satinado -->
              <filter id="ribbonSatinBlur" x="-30%" y="-30%" width="160%" height="160%">
                <feGaussianBlur stdDeviation="40" result="blur" />
                <feComposite in="SourceGraphic" in2="blur" operator="over" />
              </filter>
            </defs>

            <!-- Capa Posterior Fluida -->
            <path
              d="M 640,40 C 820,130 1020,300 1100,520 C 1160,700 1080,890 960,1010 C 840,1110 680,1100 580,980 C 490,870 540,710 680,570 C 820,440 960,300 980,120 Z"
              fill="url(#brandRibbonBody)"
              filter="url(#ribbonSatinBlur)"
            />

            <!-- Pliegue Frontal Escultural con Líneas Definidas -->
            <path
              d="M 720,60 C 880,180 1050,340 1040,560 C 1030,750 900,920 790,980 C 700,1030 630,950 640,840 C 660,730 790,560 880,420 C 970,300 970,180 940,90 Z"
              fill="url(#brandRibbonFoldTwist)"
              opacity="0.88"
            />
          </svg>
        </div>

        <!-- Contenido Central del Hero -->
        <div class="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full space-y-7 sm:space-y-9">
          
          <!-- Micro-etiqueta Superior -->
          <div class="flex items-center gap-2 text-[13px] text-charcoal-500">
            <span>Volumen institucional distribuido vía ISTPET:</span>
            <span class="font-mono text-charcoal-900 font-semibold tracking-tight">1.903.542 L &nbsp;•&nbsp; 100% trazable</span>
          </div>

          <!-- Titular Principal con Identidad Cromática Canónica -->
          <div class="max-w-4xl space-y-3">
            <h1 class="hds-heading-hero text-4xl sm:text-5xl lg:text-[56px] tracking-[-0.035em] leading-[1.12] text-charcoal-800">
              La infraestructura logística para 
              <span class="text-primary-600 font-normal">
                hacer crecer tu red de abastecimiento.
              </span>
              Optimiza rutas, ofrece trazabilidad de 
              <span class="text-primary-600 font-normal">
                insumos críticos
              </span>
              e implementa modelos inteligentes de 
              <span class="text-accent-500 font-normal">
                reabastecimiento,
              </span>
              <span class="text-charcoal-500 font-light">
                desde tu primera remisión.
              </span>
            </h1>
          </div>

          <!-- Botonera de Acción Principal -->
          <div class="flex flex-wrap items-center gap-4 pt-2">
            @if (session.status() === 'authenticated') {
              <!-- CTA para sesión activa hacia Torre de Control -->
              <a
                routerLink="/inicio"
                class="inline-flex items-center justify-center font-medium text-[15px] px-6 py-3.5 bg-primary-600 hover:bg-primary-700 text-white rounded-full shadow-[0_4px_14px_rgba(0,167,196,0.38)] hover:shadow-[0_6px_22px_rgba(0,167,196,0.48)] transition-all hover:-translate-y-0.5 active:translate-y-0 group">
                <span>Ir a la Torre de Control</span>
                <span class="ml-2 font-normal text-white/90 group-hover:translate-x-1 transition-transform">›</span>
              </a>
            } @else {
              <!-- Botón 1: Clean Cyan con forma de píldora redondeada y chevron '›' -->
              <a
                routerLink="/login"
                class="inline-flex items-center justify-center font-medium text-[15px] px-6 py-3.5 bg-primary-600 hover:bg-primary-700 text-white rounded-full shadow-[0_4px_14px_rgba(0,167,196,0.38)] hover:shadow-[0_6px_22px_rgba(0,167,196,0.48)] transition-all hover:-translate-y-0.5 active:translate-y-0 group">
                <span>Iniciar sesión</span>
                <span class="ml-2 font-normal text-white/90 group-hover:translate-x-1 transition-transform">›</span>
              </a>

              <!-- Botón 2: Blanco con borde limpio tipo pastilla e icono oficial de Google -->
              <a
                routerLink="/login"
                class="inline-flex items-center justify-center font-medium text-[15px] px-6 py-3.5 bg-white hover:bg-slate-50 text-charcoal-800 rounded-full border border-slate-200/90 shadow-2xs hover:border-slate-300 transition-all hover:-translate-y-0.5 active:translate-y-0 group">
                <!-- Icono Oficial Multicolor de Google -->
                <svg class="w-4 h-4 mr-2.5" viewBox="0 0 24 24">
                  <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
                  <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
                  <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z" />
                  <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z" />
                </svg>
                <span>Accede con tu cuenta de Google</span>
              </a>
            }
          </div>
        </div>

        <!-- Barra Inferior de Partners e Instituciones -->
        <div class="relative z-10 w-full pt-16 sm:pt-24">
          <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 border-t border-slate-100 pt-8">
            <div class="flex flex-wrap items-center justify-between gap-6 sm:gap-8 opacity-75 hover:opacity-100 transition-opacity text-slate-700 text-sm font-semibold select-none">
              <!-- FEDELIMP (Energy Orange) -->
              <div class="flex items-center gap-1.5 font-bold tracking-tight text-accent-500">
                <span class="text-base font-black">●</span>
                <span class="font-sans text-[15px]">FEDELIMP</span>
              </div>

              <!-- JIMCORPSERVI (Charcoal) -->
              <div class="flex items-center gap-2 font-mono text-xs tracking-widest text-charcoal-800">
                <span class="h-3.5 w-1 bg-charcoal-800 inline-block"></span>
                <span>JIMCORPSERVI</span>
              </div>

              <!-- ARCSA (Charcoal) -->
              <div class="font-mono text-xs font-bold tracking-tight text-charcoal-700">
                ARCSA ECUADOR
              </div>

              <!-- Hospital Ambato (Charcoal) -->
              <div class="font-sans text-xs sm:text-sm font-semibold text-charcoal-700 tracking-tight">
                H. Ambato
              </div>

              <!-- PRONACA (Expo Lime) -->
              <div class="font-sans text-sm font-bold tracking-tight text-secondary-600">
                PRONACA
              </div>

              <!-- Expo Clean 2026 (Clean Cyan oficial) -->
              <div class="font-mono text-xs font-medium text-primary-600">
                Expo Clean 2026
              </div>

              <!-- PostGIS 16 (Clean Cyan oficial) -->
              <div class="flex items-center gap-1 font-mono text-xs text-charcoal-900 font-bold">
                <svg class="w-3.5 h-3.5 text-primary-500" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"/>
                </svg>
                <span>PostGIS 16</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- ========================================================================= -->
      <!-- 2. SECCIÓN: SOLUCIONES FLEXIBLES (Paleta Oficial de Diseño)                -->
      <!-- ========================================================================= -->
      <section class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 sm:py-24 space-y-14">
        
        <!-- Título Continuo de Sección -->
        <div class="max-w-4xl space-y-4">
          <h2 class="hds-heading-section text-3xl sm:text-4xl lg:text-[44px] leading-[1.16] text-charcoal-800">
            Soluciones flexibles para cualquier contrato de abastecimiento.
            <span class="text-charcoal-500 font-light">
              Haz crecer tu cobertura con un conjunto completo de herramientas para logística e higiene. Elige solo lo que necesitas y combina los productos para que trabajen de la mano en el crecimiento de tu operación.
            </span>
          </h2>
        </div>

        <!-- Cuadrícula de 2 Tarjetas Showcase Grandes -->
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-8 sm:gap-10">
          
          <!-- ===================================================================== -->
          <!-- TARJETA IZQUIERDA: Terminal Físico POS + Checkout Web                 -->
          <!-- ===================================================================== -->
          <div class="bg-gradient-to-br from-white via-slate-50/70 to-primary-50/40 rounded-[32px] p-8 sm:p-10 border border-slate-200/80 shadow-xs flex flex-col justify-between space-y-8 relative overflow-hidden group">
            
            <!-- Resplandor de Marca Cálido-Cian (Clean Cyan + Energy Orange) -->
            <div class="absolute -right-16 -bottom-16 w-80 h-80 bg-gradient-to-br from-primary-400/20 via-secondary-300/25 to-accent-400/20 rounded-full blur-3xl pointer-events-none"></div>

            <!-- Encabezado de la Tarjeta con Botón de Expandir -->
            <div class="flex items-start justify-between gap-4 relative z-10">
              <h3 class="text-xl sm:text-[22px] font-normal tracking-tight text-charcoal-800 max-w-sm leading-snug">
                Acepta y optimiza despachos en todo el país, tanto en ruta como en recepción hospitalaria
              </h3>
              <!-- Icono de Expandir en Esquina Superior Derecha -->
              <button
                type="button"
                aria-label="Expandir vista de despacho"
                class="w-7 h-7 shrink-0 rounded-lg bg-white/90 border border-slate-200/80 flex items-center justify-center text-primary-600 shadow-2xs hover:bg-slate-50 transition-colors">
                <svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4" />
                </svg>
              </button>
            </div>

            <!-- Contenedor del Mockup: Terminal POS + Ventana de Checkout Web Flotante -->
            <div class="relative pt-6 flex items-end justify-center min-h-[420px] select-none">
              
              <!-- VENTANA WEB DE CHECKOUT FLOTANTE -->
              <div class="w-[88%] sm:w-[84%] bg-white rounded-2xl border border-slate-200 shadow-[0_12px_32px_rgba(45,55,72,0.08)] p-5 space-y-4 transform translate-x-8 sm:translate-x-12 -translate-y-4 relative z-0">
                <!-- Barra Superior de Navegador con 3 Puntos y URL -->
                <div class="flex items-center justify-between border-b border-slate-100 pb-3">
                  <div class="flex items-center gap-1.5">
                    <span class="w-2.5 h-2.5 rounded-full bg-slate-300"></span>
                    <span class="w-2.5 h-2.5 rounded-full bg-slate-300"></span>
                    <span class="w-2.5 h-2.5 rounded-full bg-slate-300"></span>
                  </div>
                  <div class="bg-slate-50 border border-slate-200/80 rounded-full px-3 py-0.5 text-[10px] font-mono text-slate-500 flex items-center gap-1">
                    <svg class="w-2.5 h-2.5 text-primary-500" fill="currentColor" viewBox="0 0 20 20">
                      <path fill-rule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clip-rule="evenodd" />
                    </svg>
                    <span>clean.istpetdev.com/remisiones/rm-042</span>
                  </div>
                  <div class="w-8"></div>
                </div>

                <!-- Contenido del Checkout B2B -->
                <div class="space-y-3">
                  <!-- Logo Naranja B2B (Energy Orange) -->
                  <div class="text-xs font-black tracking-wider text-accent-500 font-mono">
                    ISTPET CLEAN
                  </div>

                  <!-- Formulario Simulado -->
                  <div class="space-y-2">
                    <div class="space-y-1">
                      <label class="text-[10px] text-slate-500 font-medium">E-Mail de Recepción Institucional</label>
                      <div class="w-full bg-slate-50 border border-slate-200 rounded-lg px-2.5 py-1.5 text-[11px] text-charcoal-700 font-mono truncate">
                        bodega.central&#64;hospitalambato.gob.ec
                      </div>
                    </div>

                    <!-- Botones Rápidos (Expo Lime y Charcoal) -->
                    <div class="grid grid-cols-2 gap-2 pt-1">
                      <div class="bg-secondary-500 text-charcoal-950 text-[10px] font-bold py-1.5 rounded-lg text-center flex items-center justify-center gap-1">
                        <span class="w-1.5 h-1.5 rounded-full bg-charcoal-900"></span>
                        <span>Stock OK</span>
                      </div>
                      <div class="bg-charcoal-900 text-white text-[10px] font-bold py-1.5 rounded-lg text-center flex items-center justify-center gap-1 font-mono">
                        <span>Custodia OK</span>
                      </div>
                    </div>

                    <!-- Métodos de Despacho -->
                    <div class="space-y-1 pt-1 text-[11px] text-slate-600">
                      <div class="flex items-center gap-2 p-1.5 bg-slate-50/80 rounded border border-slate-200/60">
                        <input type="radio" checked class="accent-primary-600 text-primary-600 w-3 h-3" />
                        <span class="font-medium text-charcoal-800">Ruta C-01 Hospitalaria (ARCSA)</span>
                      </div>
                    </div>

                    <!-- Detalle de Ítems -->
                    <div class="pt-2 border-t border-slate-100 flex items-center justify-between text-[11px]">
                      <span class="text-slate-500">Volumen Despachado:</span>
                      <span class="font-mono font-bold text-charcoal-900">45 Galones</span>
                    </div>

                    <div class="bg-primary-50 hover:bg-primary-100 text-primary-700 text-[11px] font-medium py-1.5 rounded-lg text-center transition-colors">
                      Emitir Guía Digital
                    </div>
                  </div>
                </div>
              </div>

              <!-- TERMINAL FÍSICO BLANCO POS -->
              <div class="absolute left-2 sm:left-4 bottom-0 w-60 sm:w-64 bg-slate-100/90 rounded-[28px] p-2.5 shadow-[0_24px_50px_rgba(45,55,72,0.22)] border border-slate-300/80 transform -rotate-1 z-10">
                <!-- Carcasa Blanca/Plata del Datáfono -->
                <div class="bg-white rounded-[22px] p-4 space-y-3.5 border border-slate-200/70 shadow-xs">
                  <!-- Altavoz / Lector Contactless Superior -->
                  <div class="flex flex-col items-center gap-1.5">
                    <div class="h-1 w-8 bg-slate-200 rounded-full"></div>
                    <!-- Icono Contactless -->
                    <svg class="w-5 h-5 text-primary-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M8.111 16.404a5.5 5.5 0 017.778 0M12 20h.01m-7.08-7.071c3.904-3.905 10.236-3.905 14.141 0M1.393 9.393c5.857-5.857 15.356-5.857 21.213 0" />
                    </svg>
                  </div>

                  <!-- Título de Cobro / Recepción -->
                  <div class="text-center space-y-0.5">
                    <p class="text-[11px] text-slate-400 font-sans">Recepción en Campo</p>
                    <div class="text-2xl sm:text-[26px] font-bold text-charcoal-900 font-sans tracking-tight">
                      45,00 Gal
                    </div>
                    <p class="text-[9px] text-slate-400 font-sans">
                      Aproxima credencial o escanea QR de lote
                    </p>
                  </div>

                  <!-- Desglose de Insumos -->
                  <div class="space-y-1.5 pt-1 text-[10px] border-t border-slate-100">
                    <div class="flex justify-between text-slate-500">
                      <span>Amonio Cuat. 5G</span>
                      <span class="font-mono text-charcoal-700 font-medium">38,00 Gal</span>
                    </div>
                    <div class="flex justify-between text-slate-500">
                      <span>Detergente Enzim.</span>
                      <span class="font-mono text-charcoal-700 font-medium">7,00 Gal</span>
                    </div>
                    <div class="flex justify-between text-charcoal-800 font-semibold border-t border-slate-100 pt-1">
                      <span>Total Entrega</span>
                      <span class="font-mono text-charcoal-900">45,00 Gal</span>
                    </div>
                  </div>

                  <!-- Botón Acción Primaria -->
                  <div class="w-full bg-primary-600 hover:bg-primary-700 text-white text-xs font-medium py-2 rounded-lg text-center transition-colors shadow-xs">
                    Confirmar Entrega
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- ===================================================================== -->
          <!-- TARJETA DERECHA: Modelo de Reabastecimiento y Medidor de Consumo       -->
          <!-- ===================================================================== -->
          <div class="bg-gradient-to-br from-white via-slate-50/70 to-secondary-50/40 rounded-[32px] p-8 sm:p-10 border border-slate-200/80 shadow-xs flex flex-col justify-between space-y-8 relative overflow-hidden group">
            
            <!-- Resplandor Orgánico de Marca (Expo Lime + Clean Cyan) -->
            <div class="absolute -right-16 -bottom-16 w-80 h-80 bg-gradient-to-br from-secondary-400/20 via-primary-300/20 to-accent-300/20 rounded-full blur-3xl pointer-events-none"></div>

            <!-- Encabezado de la Tarjeta con Botón de Expandir -->
            <div class="flex items-start justify-between gap-4 relative z-10">
              <h3 class="text-xl sm:text-[22px] font-normal tracking-tight text-charcoal-800 max-w-sm leading-snug">
                Diseña tu propio modelo de reabastecimiento continuo
              </h3>
              <!-- Icono de Expandir en Esquina Superior Derecha -->
              <button
                type="button"
                aria-label="Expandir modelo de facturación"
                class="w-7 h-7 shrink-0 rounded-lg bg-white/90 border border-slate-200/80 flex items-center justify-center text-primary-600 shadow-2xs hover:bg-slate-50 transition-colors">
                <svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4" />
                </svg>
              </button>
            </div>

            <!-- Contenedor de las Dos Tarjetas Flotantes -->
            <div class="space-y-5 pt-4 relative z-10 select-none">
              
              <!-- TARJETA SUPERIOR FLOTANTE: Medidor de Consumo -->
              <div class="bg-white rounded-2xl border border-slate-200 shadow-[0_8px_24px_rgba(45,55,72,0.06)] p-6 space-y-4">
                <div class="flex items-center gap-3">
                  <!-- Icono Cuadrado con Indicador Clean Cyan -->
                  <div class="w-8 h-8 rounded-lg bg-primary-50 text-primary-600 flex items-center justify-center">
                    <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M13 10V3L4 14h7v7l9-11h-7z" />
                    </svg>
                  </div>
                  <div>
                    <h4 class="text-sm font-semibold text-charcoal-900">Plan Hospitalario Integral</h4>
                    <p class="text-[11px] text-slate-400">Reabastecimiento preventivo mensual</p>
                  </div>
                </div>

                <div class="text-[11px] text-slate-500 font-mono">
                  <span>Químicos críticos:</span>
                  <span class="text-charcoal-800 font-bold ml-1">0,12 $ por litro distribuido</span>
                </div>

                <!-- Barra Degradada de Marca: Clean Cyan -> Expo Lime -> Energy Orange -->
                <div class="space-y-1.5 pt-1">
                  <div class="flex items-center justify-between text-[11px] text-slate-500">
                    <span class="flex items-center gap-1">
                      <svg class="w-3 h-3 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <circle cx="12" cy="12" r="10" stroke-width="2" />
                        <polyline points="12 6 12 12 16 14" stroke-width="2" />
                      </svg>
                      Medidor de consumo
                    </span>
                    <span class="font-mono font-bold text-primary-600">78.4%</span>
                  </div>
                  <div class="w-full bg-slate-100 rounded-full h-2 overflow-hidden p-0.5">
                    <div class="h-full rounded-full bg-gradient-to-r from-primary-500 via-secondary-400 to-accent-500" style="width: 78.4%"></div>
                  </div>
                </div>
              </div>

              <!-- TARJETA INFERIOR FLOTANTE: Gráfico de Barras Clean Cyan -->
              <div class="bg-white rounded-2xl border border-slate-200 shadow-[0_8px_24px_rgba(45,55,72,0.06)] p-6 space-y-3">
                <div class="text-[12px] text-slate-500">
                  Insumos distribuidos en los últimos 30 días
                </div>
                
                <!-- Cifra Grande y Nítida -->
                <div class="text-2xl sm:text-3xl font-normal font-sans tracking-tight text-charcoal-900">
                  1.903.542 L
                </div>

                <!-- Gráfico de Barras en Clean Cyan con Variación de Opacidad -->
                <div class="h-28 flex items-end justify-between gap-1 sm:gap-1.5 pt-3">
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-60 h-[22%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-65 h-[28%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-60 h-[25%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-70 h-[38%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-65 h-[34%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-75 h-[48%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-70 h-[44%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-80 h-[58%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-75 h-[52%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-85 h-[68%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-90 h-[74%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-95 h-[88%]"></div>
                  <!-- Barra Pico Central Alta -->
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-100 h-[100%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-95 h-[90%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-90 h-[82%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-85 h-[76%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-80 h-[68%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-85 h-[72%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-80 h-[64%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-75 h-[58%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-70 h-[50%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-65 h-[42%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-60 h-[36%]"></div>
                  <div class="w-full bg-primary-500 hover:bg-primary-400 transition-colors rounded-t-sm opacity-55 h-[30%]"></div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- ========================================================================= -->
      <!-- 3. SECCIÓN: MÉTRICAS (Fila de 4 KPIs con Línea Superior Fina)             -->
      <!-- ========================================================================= -->
      <section class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 sm:py-24 space-y-12">
        <div class="text-left max-w-2xl space-y-3">
          <h2 class="hds-heading-section text-3xl sm:text-4xl lg:text-[42px] leading-tight text-charcoal-800">
            La columna vertebral del abastecimiento institucional
          </h2>
          <p class="text-sm text-charcoal-500 font-light">
            Métricas de alto impacto en tiempo real evaluadas sobre la red hospitalaria de Ecuador.
          </p>
        </div>

        <!-- Fila de 4 Métricas con Línea Superior Fina -->
        <div class="pt-8 border-t border-slate-200/80 grid grid-cols-2 md:grid-cols-4 gap-8 text-left">
          <div class="space-y-1">
            <div class="text-3xl sm:text-4xl font-normal font-sans text-charcoal-900 tracking-tight">
              24.8 %
            </div>
            <p class="text-xs font-semibold text-charcoal-800">Ahorro en combustible</p>
            <p class="text-xs text-charcoal-500">127 km evitados por ciclo con algoritmo VRP en PostGIS.</p>
          </div>

          <div class="space-y-1">
            <div class="text-3xl sm:text-4xl font-normal font-sans text-charcoal-900 tracking-tight">
              0 quiebres
            </div>
            <p class="text-xs font-semibold text-charcoal-800">Continuidad operativa</p>
            <p class="text-xs text-charcoal-500">Abastecimiento preventivo garantizado 5 días antes.</p>
          </div>

          <div class="space-y-1">
            <div class="text-3xl sm:text-4xl font-normal font-sans text-charcoal-900 tracking-tight">
              100 %
            </div>
            <p class="text-xs font-semibold text-charcoal-800">Custodia trazada</p>
            <p class="text-xs text-charcoal-500">Handling Units auditadas mediante QR y UUID v7 cronológico.</p>
          </div>

          <div class="space-y-1">
            <div class="text-3xl sm:text-4xl font-normal font-sans text-charcoal-900 tracking-tight">
              5 sedes
            </div>
            <p class="text-xs font-semibold text-charcoal-800">Red interprovincial</p>
            <p class="text-xs text-charcoal-500">Quito, Guayaquil, Cuenca, Ambato y Santo Domingo conectados.</p>
          </div>
        </div>
      </section>
    </div>
  `
})
export class LandingPage {
  readonly session = inject(AuthSession);
}

