import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthSession } from '@entities/session';
import {
  LucideQrCode,
  LucideArrowRight,
  LucideChevronRight,
  LucideMaximize2,
  LucidePlus,
  LucideZap,
  LucideBarChart3,
  LucideLock
} from '@lucide/angular';
import { ScrollWorldComponent } from './components/scroll-world/scroll-world.component';

@Component({
  selector: 'page-landing',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    ScrollWorldComponent,
    LucideQrCode,
    LucideArrowRight,
    LucideChevronRight,
    LucideMaximize2,
    LucidePlus,
    LucideZap,
    LucideBarChart3,
    LucideLock
  ],
  template: `
    <div class="relative overflow-x-clip bg-white text-slate-800 min-h-screen">
      
      <!-- Arte Fluido Curvado 3D Estilo Stripe con Colores Originales (Cinta de Seda Tridimensional en Azul Expo, Cyan y Menta) -->
      <div aria-hidden="true" class="pointer-events-none absolute -top-12 -right-16 w-[620px] md:w-[780px] lg:w-[940px] xl:w-[1080px] h-[780px] md:h-[900px] overflow-hidden select-none z-0">
        <svg class="w-full h-full" viewBox="0 0 1000 900" fill="none" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <!-- Gradiente Cinta 1: Cyan Eléctrico a Menta Brillante -->
            <linearGradient id="stripeFlowCyanMint" x1="15%" y1="0%" x2="85%" y2="100%">
              <stop offset="0%" stop-color="#00C2FF" stop-opacity="0.95" />
              <stop offset="45%" stop-color="#00DDA2" stop-opacity="0.92" />
              <stop offset="80%" stop-color="#00E5A3" stop-opacity="0.90" />
              <stop offset="100%" stop-color="#2DD4BF" stop-opacity="0.85" />
            </linearGradient>

            <!-- Gradiente Cinta 2: Azul Eléctrico a Cyan Profundo -->
            <linearGradient id="stripeFlowBlueCyan" x1="10%" y1="10%" x2="90%" y2="90%">
              <stop offset="0%" stop-color="#0052D9" stop-opacity="0.92" />
              <stop offset="40%" stop-color="#0066FF" stop-opacity="0.88" />
              <stop offset="75%" stop-color="#0099FF" stop-opacity="0.85" />
              <stop offset="100%" stop-color="#00C2FF" stop-opacity="0.82" />
            </linearGradient>

            <!-- Gradiente Cinta Base 3: Azul Zafiro Profundo -->
            <linearGradient id="stripeFlowDeepBlue" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stop-color="#0040B0" stop-opacity="0.95" />
              <stop offset="50%" stop-color="#0066FF" stop-opacity="0.90" />
              <stop offset="100%" stop-color="#0084FF" stop-opacity="0.85" />
            </linearGradient>

            <!-- Resplandor Atmosférico Difuso Ultra Suave -->
            <radialGradient id="stripeAmbientGlow" cx="62%" cy="38%" r="55%">
              <stop offset="0%" stop-color="#00C2FF" stop-opacity="0.25" />
              <stop offset="40%" stop-color="#0066FF" stop-opacity="0.15" />
              <stop offset="75%" stop-color="#00E5A3" stop-opacity="0.08" />
              <stop offset="100%" stop-color="#ffffff" stop-opacity="0" />
            </radialGradient>

            <!-- Filtro desenfoque sedoso Stripe -->
            <filter id="stripeSilkBlur" x="-25%" y="-25%" width="150%" height="150%">
              <feGaussianBlur stdDeviation="24" />
            </filter>

            <!-- Filtro resplandor amplio -->
            <filter id="stripeGlowBlur" x="-40%" y="-40%" width="180%" height="180%">
              <feGaussianBlur stdDeviation="60" />
            </filter>
          </defs>

          <!-- 0. Resplandor difuso de fondo -->
          <circle cx="660" cy="360" r="420" fill="url(#stripeAmbientGlow)" filter="url(#stripeGlowBlur)" />

          <!-- 1. Cinta Inferior: Gran sábana Azul Zafiro -->
          <path
            d="M380 -80 C480 60, 620 220, 680 440 C740 660, 780 780, 840 980 L1080 980 L1080 -80 Z"
            fill="url(#stripeFlowDeepBlue)"
            filter="url(#stripeSilkBlur)"
            opacity="0.78"
          />

          <!-- 2. Cinta Central: Pliegue Azul a Cyan con torsión 3D -->
          <path
            d="M440 -60 C540 80, 660 250, 715 450 C765 630, 815 740, 885 920 C825 890, 755 780, 705 620 C645 430, 565 220, 485 -60 Z"
            fill="url(#stripeFlowBlueCyan)"
            filter="url(#stripeSilkBlur)"
            opacity="0.84"
          />

          <!-- 3. Cinta Superior: Pliegue Cyan a Menta diagonal curvado -->
          <path
            d="M515 -40 C615 90, 725 270, 775 460 C815 610, 865 710, 935 860 C895 830, 835 720, 785 570 C725 380, 635 180, 565 -40 Z"
            fill="url(#stripeFlowCyanMint)"
            filter="url(#stripeSilkBlur)"
            opacity="0.90"
          />

          <!-- 4. Reflejo de Seda Satinada Translúcida (Brillo de Luz 3D) -->
          <path
            d="M545 -30 C635 90, 735 250, 765 420 C795 560, 835 650, 885 780 C865 750, 815 660, 775 530 C725 370, 645 200, 575 -30 Z"
            fill="white"
            opacity="0.32"
            filter="url(#stripeSilkBlur)"
          />
        </svg>
      </div>

      <!-- ===================================================================== -->
      <!-- 1. HERO SECTION ESTILO STRIPE 1:1 (Ajustado al Primer Viewport)       -->
      <!-- ===================================================================== -->
      <section class="relative max-w-7xl mx-auto px-6 sm:px-8 lg:px-10 pt-8 sm:pt-12 pb-6 min-h-[calc(100vh-4rem)] flex flex-col justify-between border-x border-slate-100/90">
        
        <!-- Bloque Principal de Titular y Acciones -->
        <div class="pt-2 sm:pt-4">
          <!-- Micro-eyebrow tipo Stripe -->
          <p class="text-[13px] sm:text-[14px] text-[#425466] font-normal tracking-tight mb-6 sm:mb-8">
            Red logística que opera con IstpetDev: <span class="font-mono text-[#0A2540] font-medium">80 Puntos de Servicio · 100% Cobertura Activa</span>
          </p>

          <!-- Titular Estilo Stripe 1:1 (Jerarquía Editorial Separada con Colores del Proyecto) -->
          <div class="max-w-[720px]">
            <!-- H1: Titular Principal Corto, Compacto y Contundente -->
            <h1 class="text-[42px] sm:text-[52px] lg:text-[60px] xl:text-[64px] font-bold tracking-[-0.03em] text-[#0A2540] leading-[1.06]">
              Infraestructura logística
              <span class="text-transparent bg-clip-text bg-gradient-to-r from-expo-blue via-[#0084FF] to-expo-cyan">
                para predecir y asegurar
              </span>
              tus insumos.
            </h1>

            <!-- Párrafo Secundario (Body Copy) con Escala Editorial Stripe y Acento Original -->
            <p class="text-[18px] sm:text-[20px] text-[#425466] font-normal leading-[1.45] mt-6 sm:mt-7 max-w-[650px]">
              Detecta riesgos 5 días antes, optimiza rutas en PostGIS e implementa
              <span class="text-transparent bg-clip-text bg-gradient-to-r from-expo-cyan to-expo-mint font-medium">
                custodia digital
              </span>
              en el abastecimiento nacional de insumos de limpieza y desinfección.
            </p>

            <!-- Acciones Duales Estilo Stripe 1:1 con Botones Originales -->
            <div class="flex flex-wrap items-center gap-4 pt-7 sm:pt-8">
              <button
                type="button"
                (click)="session.login('/inicio')"
                class="inline-flex items-center gap-2 rounded-[8px] bg-expo-blue hover:bg-expo-blueDark px-5 py-3 font-medium text-white text-[15px] shadow-sm transition-all hover:shadow-md hover:-translate-y-0.5 active:translate-y-0 cursor-pointer">
                <span>Primeros pasos</span>
                <span class="text-white/80 text-xs font-bold">&gt;</span>
              </button>

              <a
                routerLink="/public-trace"
                class="inline-flex items-center gap-2.5 rounded-[8px] border border-[#E3E8EE] bg-white hover:bg-slate-50 hover:border-slate-300 px-5 py-3 font-medium text-[#0A2540] text-[15px] shadow-2xs transition-all hover:-translate-y-0.5 active:translate-y-0 cursor-pointer">
                <svg lucideQrCode [size]="16" class="text-expo-blue stroke-[2]"></svg>
                <span>Consultar Pasaporte QR</span>
              </a>
            </div>
          </div>
        </div>

        <!-- =================================================================== -->
        <!-- CINTA DE MARCAS Y SOCIOS LOGÍSTICOS ESTILO STRIPE 1:1                -->
        <!-- =================================================================== -->
        <div class="w-full border-t border-slate-200/80 py-6 my-2 overflow-hidden relative tape-fade-mask select-none mt-auto">
          <div class="animate-marquee-tape items-center gap-14 text-slate-500 hover:text-slate-700 text-xs sm:text-[13px] font-semibold tracking-wider">
            
            <!-- Grupo 1: Entidades Oficiales con Emblemas Vectoriales Corporativos -->
            <div class="flex items-center gap-14 shrink-0">
              <!-- Hospital Laura -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-expo-blue" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
                  <rect x="3" y="3" width="18" height="18" rx="4" fill="currentColor" fill-opacity="0.1" />
                  <path d="M12 8v8M8 12h8" stroke-linecap="round" />
                </svg>
                <span class="font-bold tracking-tight text-slate-800 text-[13px]">HOSPITAL LAURA</span>
              </div>

              <!-- Clinica San Rafael -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-slate-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="9" />
                  <path d="M12 7v10M7 12h10" stroke-linecap="round" />
                </svg>
                <div class="flex flex-col leading-none">
                  <span class="font-bold text-[12px] tracking-tight text-slate-800">CLÍNICA</span>
                  <span class="text-[10px] text-slate-500 font-semibold tracking-wider">SAN RAFAEL</span>
                </div>
              </div>

              <!-- Hospital del Sur -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-expo-cyan" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5" />
                </svg>
                <span class="font-bold tracking-tight text-slate-800 text-[13px]">HOSPITAL DEL SUR</span>
              </div>

              <!-- Laboratorios EC -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-expo-blue" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M10 2v6l-4 8a3 3 0 003 4h6a3 3 0 003-4l-4-8V2" stroke-linecap="round" stroke-linejoin="round" />
                  <path d="M8 14h8" />
                </svg>
                <div class="flex items-center gap-1">
                  <span class="font-bold text-[12px] tracking-tight text-slate-800">LABORATORIOS</span>
                  <span class="font-mono font-bold text-[11px] text-expo-blue bg-blue-50 px-1 rounded">EC</span>
                </div>
              </div>

              <!-- Sercop Hospitalario -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-amber-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M12 3l8 4.5v9L12 21l-8-4.5v-9L12 3z" />
                  <path d="M12 12l8-4.5M12 12v9M12 12L4 7.5" />
                </svg>
                <div class="flex flex-col leading-none">
                  <span class="font-mono font-bold text-[12px] text-slate-800">SERCOP</span>
                  <span class="text-[9px] text-slate-500 font-semibold tracking-wide">HOSPITALARIO</span>
                </div>
              </div>

              <!-- Distribuidora Guayas -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-slate-700" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="2" y="7" width="20" height="14" rx="2" />
                  <path d="M16 21V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v16" />
                </svg>
                <span class="font-bold tracking-tight text-slate-800 text-[13px]">DISTRIBUIDORA GUAYAS</span>
              </div>

              <!-- Fedelimp Ecuador -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-slate-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
                </svg>
                <div class="flex flex-col leading-none">
                  <span class="font-bold text-[12px] text-slate-800">FEDELIMP</span>
                  <span class="text-[9px] text-slate-500 font-semibold">ECUADOR</span>
                </div>
              </div>

              <!-- Lacif Logistics -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-expo-blue" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="10" />
                  <path d="M8 12l3 3 5-6" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
                <span class="font-mono font-bold text-slate-800 text-[12px]">LACIF LOGISTICS</span>
              </div>
            </div>

            <!-- Grupo 2: Duplicado para Marquee Infinito Fluido -->
            <div class="flex items-center gap-14 shrink-0" aria-hidden="true">
              <!-- Hospital Laura -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-expo-blue" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
                  <rect x="3" y="3" width="18" height="18" rx="4" fill="currentColor" fill-opacity="0.1" />
                  <path d="M12 8v8M8 12h8" stroke-linecap="round" />
                </svg>
                <span class="font-bold tracking-tight text-slate-800 text-[13px]">HOSPITAL LAURA</span>
              </div>

              <!-- Clinica San Rafael -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-slate-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="9" />
                  <path d="M12 7v10M7 12h10" stroke-linecap="round" />
                </svg>
                <div class="flex flex-col leading-none">
                  <span class="font-bold text-[12px] tracking-tight text-slate-800">CLÍNICA</span>
                  <span class="text-[10px] text-slate-500 font-semibold tracking-wider">SAN RAFAEL</span>
                </div>
              </div>

              <!-- Hospital del Sur -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-expo-cyan" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5" />
                </svg>
                <span class="font-bold tracking-tight text-slate-800 text-[13px]">HOSPITAL DEL SUR</span>
              </div>

              <!-- Laboratorios EC -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-expo-blue" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M10 2v6l-4 8a3 3 0 003 4h6a3 3 0 003-4l-4-8V2" stroke-linecap="round" stroke-linejoin="round" />
                  <path d="M8 14h8" />
                </svg>
                <div class="flex items-center gap-1">
                  <span class="font-bold text-[12px] tracking-tight text-slate-800">LABORATORIOS</span>
                  <span class="font-mono font-bold text-[11px] text-expo-blue bg-blue-50 px-1 rounded">EC</span>
                </div>
              </div>

              <!-- Sercop Hospitalario -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-amber-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M12 3l8 4.5v9L12 21l-8-4.5v-9L12 3z" />
                  <path d="M12 12l8-4.5M12 12v9M12 12L4 7.5" />
                </svg>
                <div class="flex flex-col leading-none">
                  <span class="font-mono font-bold text-[12px] text-slate-800">SERCOP</span>
                  <span class="text-[9px] text-slate-500 font-semibold tracking-wide">HOSPITALARIO</span>
                </div>
              </div>

              <!-- Distribuidora Guayas -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-slate-700" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="2" y="7" width="20" height="14" rx="2" />
                  <path d="M16 21V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v16" />
                </svg>
                <span class="font-bold tracking-tight text-slate-800 text-[13px]">DISTRIBUIDORA GUAYAS</span>
              </div>

              <!-- Fedelimp Ecuador -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-slate-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
                </svg>
                <div class="flex flex-col leading-none">
                  <span class="font-bold text-[12px] text-slate-800">FEDELIMP</span>
                  <span class="text-[9px] text-slate-500 font-semibold">ECUADOR</span>
                </div>
              </div>

              <!-- Lacif Logistics -->
              <div class="flex items-center gap-2 text-slate-700/80 hover:text-slate-900 transition-colors">
                <svg class="w-5 h-5 text-expo-blue" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="10" />
                  <path d="M8 12l3 3 5-6" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
                <span class="font-mono font-bold text-slate-800 text-[12px]">LACIF LOGISTICS</span>
              </div>
            </div>
          </div>
        </div>

      </section>

      <!-- ===================================================================== -->
      <!-- 1.5 SCROLL WORLD: RECORRIDO INTERACTIVO 3D SINCRONIZADO CON SCROLL    -->
      <!-- ===================================================================== -->
      <landing-scroll-world></landing-scroll-world>

      <!-- ===================================================================== -->
      <!-- 2. BENTO GRID MODULAR (Estilo Stripe Showcase de Productos)           -->
      <!-- ===================================================================== -->
      <section id="soluciones" class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 sm:py-24 space-y-10">
        
        <!-- Encabezado de Sección Estilo Stripe -->
        <div class="max-w-3xl space-y-3">
          <h2 class="text-3xl sm:text-4xl font-extrabold tracking-[-0.03em] text-slate-900 leading-tight">
            Soluciones integrales para cada modelo operativo.
            <span class="text-slate-500 font-normal">
              Gestiona tu abastecimiento con herramientas diseñadas para actuar de forma proactiva, en oficina o en campo.
            </span>
          </h2>
        </div>

        <!-- Fila de Tarjetas Bento Grid -->
        <div class="grid lg:grid-cols-12 gap-8 items-stretch">
          
          <!-- Tarjeta Izquierda (Ancha): Terminal Móvil + Despacho SaaS -->
          <div class="lg:col-span-7 bg-white rounded-2xl border border-slate-200 shadow-sm p-6 sm:p-8 flex flex-col justify-between space-y-6 hover:border-slate-300 transition-all">
            <div class="space-y-3">
              <div class="flex items-center justify-between">
                <h3 class="text-xl sm:text-2xl font-bold tracking-tight text-slate-900">
                  Acepta y optimiza entregas en todo el país, en ruta y en destino
                </h3>
                <span class="h-8 w-8 rounded-lg bg-slate-50 border border-slate-200 flex items-center justify-center text-slate-500 shrink-0">
                  <svg lucideMaximize2 [size]="14" class="stroke-[2]"></svg>
                </span>
              </div>
              <p class="text-sm text-slate-600 leading-relaxed">
                El chofer navega su hoja de ruta y el receptor hospitalario dictamina la recepción en destino con firma táctil digital, incluso en zonas sin señal de telefonía.
              </p>
            </div>

            <!-- Mockup Visual Compuesto: Dispositivo Móvil + Modal Web -->
            <div class="bg-gradient-to-tr from-slate-50 to-blue-50/40 rounded-xl p-4 sm:p-6 border border-slate-100 flex flex-col sm:flex-row items-center gap-5 justify-center">
              
              <!-- Terminal Móvil PWA Simulada -->
              <div class="w-52 bg-white rounded-2xl p-4 shadow-xl border border-slate-200/90 space-y-3 shrink-0">
                <div class="h-1.5 w-10 bg-slate-200 rounded-full mx-auto"></div>
                <div class="text-center space-y-1">
                  <span class="text-[10px] text-slate-400 font-mono uppercase block">Entrega Asignada</span>
                  <strong class="text-base font-extrabold text-slate-900 block font-mono">60 L Químico</strong>
                  <span class="text-[10px] font-semibold text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded-full inline-block">
                    Modo Avión Activo
                  </span>
                </div>
                <div class="border-t border-slate-100 pt-2 text-[11px] space-y-1 text-slate-600 font-mono">
                  <div class="flex justify-between">
                    <span>Parada:</span>
                    <strong>04 / 08</strong>
                  </div>
                  <div class="flex justify-between">
                    <span>Ventana:</span>
                    <strong>08:00 - 10:00</strong>
                  </div>
                </div>
                <button type="button" class="w-full py-2 bg-expo-blue text-white rounded-lg text-xs font-bold shadow-sm">
                  Confirmar Recepción
                </button>
              </div>

              <!-- Vista SaaS de Escritorio Simulada -->
              <div class="flex-1 w-full bg-white rounded-xl p-4 shadow-sm border border-slate-200 space-y-3">
                <div class="flex items-center justify-between border-b border-slate-100 pb-2">
                  <span class="text-xs font-bold text-slate-800 font-mono">Despacho Central CEQ</span>
                  <span class="text-[10px] font-mono text-slate-400">Ruta R-01</span>
                </div>
                <div class="space-y-2 text-xs">
                  <div class="flex items-center justify-between p-2 rounded bg-slate-50">
                    <span class="font-medium text-slate-700">Hospital Laura</span>
                    <span class="text-emerald-600 font-semibold font-mono">Entregado</span>
                  </div>
                  <div class="flex items-center justify-between p-2 rounded bg-blue-50/50">
                    <span class="font-medium text-slate-700">Clínica San Rafael</span>
                    <span class="text-expo-blue font-semibold font-mono">En Tránsito</span>
                  </div>
                </div>
              </div>

            </div>
          </div>

          <!-- Tarjeta Derecha (Facturación / Demanda con Gráfica): Medidor de Consumo y Barras -->
          <div class="lg:col-span-5 bg-white rounded-2xl border border-slate-200 shadow-sm p-6 sm:p-8 flex flex-col justify-between space-y-6 hover:border-slate-300 transition-all">
            <div class="space-y-3">
              <div class="flex items-center justify-between">
                <h3 class="text-xl sm:text-2xl font-bold tracking-tight text-slate-900">
                  Predicción y control de demanda crítica
                </h3>
                <span class="h-8 w-8 rounded-lg bg-slate-50 border border-slate-200 flex items-center justify-center text-slate-500 shrink-0">
                  <svg lucideMaximize2 [size]="14" class="stroke-[2]"></svg>
                </span>
              </div>
              <p class="text-sm text-slate-600 leading-relaxed">
                Supervisa el nivel de cobertura determinista en cada punto hospitalario con advertencias automáticas antes del quiebre.
              </p>
            </div>

            <!-- Gráfica de Barras y Barra de Uso Estilo Stripe -->
            <div class="bg-gradient-to-b from-slate-50 to-white rounded-xl p-5 border border-slate-100 space-y-4">
              
              <!-- Medidor de Consumo con Gradiente -->
              <div class="bg-white p-3.5 rounded-xl border border-slate-200/70 shadow-sm space-y-2">
                <div class="flex items-center justify-between text-xs">
                  <span class="font-bold text-slate-900 font-mono">Hospital Laura · Cobertura</span>
                  <span class="font-mono text-expo-blue font-bold">5 Días Restantes</span>
                </div>
                <!-- Barra con Gradiente Azul -> Cyan -> Menta -->
                <div class="h-2.5 w-full bg-slate-100 rounded-full overflow-hidden">
                  <div class="h-full w-2/5 bg-gradient-to-r from-expo-blue via-expo-cyan to-expo-mint rounded-full"></div>
                </div>
                <div class="flex justify-between text-[10px] text-slate-400 font-mono">
                  <span>Stock: 60L</span>
                  <span>Lead Time: 2d + Margen 3d</span>
                </div>
              </div>

              <!-- Gráfica de Barras Diarias Simuladas -->
              <div class="bg-white p-3.5 rounded-xl border border-slate-200/70 shadow-sm space-y-2">
                <span class="text-[11px] font-bold text-slate-700 block font-mono">Demanda Diaria Observada (L/día)</span>
                
                <div class="h-28 flex items-end justify-between gap-1 pt-4 px-1">
                  <div class="w-full bg-blue-100 rounded-t h-[40%]"></div>
                  <div class="w-full bg-blue-200 rounded-t h-[65%]"></div>
                  <div class="w-full bg-blue-300 rounded-t h-[50%]"></div>
                  <div class="w-full bg-blue-400 rounded-t h-[80%]"></div>
                  <div class="w-full bg-expo-blue rounded-t h-[95%]"></div>
                  <div class="w-full bg-blue-500 rounded-t h-[60%]"></div>
                  <div class="w-full bg-blue-400 rounded-t h-[75%]"></div>
                  <div class="w-full bg-blue-300 rounded-t h-[85%]"></div>
                  <div class="w-full bg-expo-cyanDeep rounded-t h-[100%]"></div>
                  <div class="w-full bg-blue-200 rounded-t h-[45%]"></div>
                  <div class="w-full bg-blue-100 rounded-t h-[30%]"></div>
                </div>
                
                <div class="flex justify-between text-[9px] text-slate-400 font-mono">
                  <span>Día 01</span>
                  <span>Disparo de Alerta (Día 15)</span>
                  <span>Día 30</span>
                </div>
              </div>

            </div>
          </div>

        </div>
      </section>

      <!-- ===================================================================== -->
      <!-- 3. LA COLUMNA VERTEBRAL: MÉTRICAS Y BURST RADIAL (Estilo Stripe)      -->
      <!-- ===================================================================== -->
      <section id="metricas" class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 sm:py-28 text-center space-y-12">
        
        <div class="max-w-3xl mx-auto space-y-3">
          <h2 class="text-3xl sm:text-5xl font-extrabold tracking-[-0.035em] text-slate-900 leading-tight">
            La columna vertebral del abastecimiento institucional
          </h2>
        </div>

        <!-- Matriz de 4 Cifras Clave Estilo Stripe -->
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-8 text-left border-y border-slate-200 py-10">
          <div class="space-y-2">
            <strong class="text-3xl sm:text-4xl font-extrabold text-slate-900 tracking-tight font-mono block">
              80 o más
            </strong>
            <p class="text-xs text-slate-600 leading-relaxed font-sans">
              puntos de servicio y hospitales monitoreados a nivel nacional en la red de higiene.
            </p>
          </div>

          <div class="space-y-2">
            <strong class="text-3xl sm:text-4xl font-extrabold text-expo-blue tracking-tight font-mono block">
              5 Días Antes
            </strong>
            <p class="text-xs text-slate-600 leading-relaxed font-sans">
              anticipación predictiva de riesgo antes de que ocurra la rotura de stock.
            </p>
          </div>

          <div class="space-y-2">
            <strong class="text-3xl sm:text-4xl font-extrabold text-slate-900 tracking-tight font-mono block">
              100% Offline
            </strong>
            <p class="text-xs text-slate-600 leading-relaxed font-sans">
              continuidad operativa en carretera y zonas hospitalarias sin señal celular.
            </p>
          </div>

          <div class="space-y-2">
            <strong class="text-3xl sm:text-4xl font-extrabold text-emerald-600 tracking-tight font-mono block">
              90 Días Run
            </strong>
            <p class="text-xs text-slate-600 leading-relaxed font-sans">
              simulación verificable con doble baseline y manifiesto exportable.
            </p>
          </div>
        </div>

        <!-- Gráfico Burst Radial de Rutas (Simulando el Burst de Stripe) -->
        <div class="relative max-w-2xl mx-auto h-48 flex items-center justify-center overflow-hidden">
          <svg class="w-full h-full text-slate-300" viewBox="0 0 600 200" fill="none">
            <circle cx="300" cy="200" r="40" stroke="#0066FF" stroke-width="2" stroke-dasharray="4 4" />
            <circle cx="300" cy="200" r="90" stroke="#00C2FF" stroke-width="1.5" stroke-dasharray="3 3" opacity="0.6" />
            <circle cx="300" cy="200" r="150" stroke="#00E5A3" stroke-width="1" stroke-dasharray="2 2" opacity="0.4" />
            
            <!-- Rayos / Rutas Vectoriales que irradian desde el centro -->
            <line x1="300" y1="200" x2="100" y2="40" stroke="#0066FF" stroke-width="1.5" opacity="0.4" />
            <line x1="300" y1="200" x2="160" y2="20" stroke="#00C2FF" stroke-width="1.5" opacity="0.6" />
            <line x1="300" y1="200" x2="240" y2="10" stroke="#00E5A3" stroke-width="2" />
            <line x1="300" y1="200" x2="300" y2="5" stroke="#0066FF" stroke-width="2" />
            <line x1="300" y1="200" x2="360" y2="10" stroke="#00C2FF" stroke-width="2" />
            <line x1="300" y1="200" x2="440" y2="20" stroke="#00E5A3" stroke-width="1.5" opacity="0.6" />
            <line x1="300" y1="200" x2="500" y2="40" stroke="#0066FF" stroke-width="1.5" opacity="0.4" />

            <!-- Nodos Hospitalarios Destacados -->
            <circle cx="240" cy="10" r="4" fill="#00E5A3" />
            <circle cx="300" cy="5" r="5" fill="#0066FF" />
            <circle cx="360" cy="10" r="4" fill="#00C2FF" />
            <circle cx="160" cy="20" r="3" fill="#0066FF" />
            <circle cx="440" cy="20" r="3" fill="#00E5A3" />
          </svg>
        </div>

      </section>

      <!-- ===================================================================== -->
      <!-- 4. CASO DE ESTUDIO Y PROTOCOLOS (Estilo Hertz / Aerial View Stripe)   -->
      <!-- ===================================================================== -->
      <section id="flujo" class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 space-y-10">
        
        <!-- Banner Superior con Logo de Cliente -->
        <div class="flex items-center justify-between border-b border-slate-200 pb-4">
          <div class="flex items-center gap-3">
            <span class="h-8 w-8 rounded-lg bg-blue-600 text-white font-black text-sm flex items-center justify-center font-mono">
              HL
            </span>
            <span class="text-sm font-bold text-slate-900">
              Hospital Laura optimiza su abastecimiento de limpieza y desinfección con IstpetDev.
            </span>
          </div>
          <a routerLink="/public-trace/HU-2026-EC-089" class="text-xs font-bold text-expo-blue hover:underline hidden sm:inline-block">
            Ver pasaporte de muestra &gt;
          </a>
        </div>

        <!-- Tarjeta Visual Panorámica con Vista de Carretera / Corredor Logístico -->
        <div class="rounded-2xl border border-slate-200 overflow-hidden shadow-sm bg-slate-900 text-white relative">
          
          <div class="h-72 sm:h-96 w-full relative flex items-center justify-center p-8 bg-gradient-to-r from-slate-950 via-slate-900 to-blue-950">
            <!-- Representación gráfica de la red logística en carretera -->
            <div class="max-w-xl text-center space-y-4 relative z-10">
              <span class="text-xs font-mono font-bold uppercase tracking-wider text-expo-cyanLight bg-white/10 px-3 py-1 rounded-full border border-white/10">
                Corredor Logístico Quito - Pichincha
              </span>
              <h3 class="text-2xl sm:text-4xl font-extrabold tracking-tight">
                Control ininterrumpido en carretera y bodegas
              </h3>
              <p class="text-sm text-slate-300">
                Resiliencia ante bloqueos viales: el sistema replanifica la ruta conservando las paradas ejecutadas y recalculando los tiempos para las entregas pendientes.
              </p>
            </div>
          </div>

          <!-- Métricas del Caso al Pie de la Imagen -->
          <div class="bg-white text-slate-800 p-6 sm:p-8 grid sm:grid-cols-3 gap-6 border-t border-slate-200 text-xs font-mono">
            <div>
              <span class="text-slate-400 block uppercase text-[10px]">Puntos Conectados</span>
              <strong class="text-sm text-slate-900 font-bold block mt-0.5">80 Hospitales & Sedes</strong>
            </div>
            <div>
              <span class="text-slate-400 block uppercase text-[10px]">Rutas Optimizadas</span>
              <strong class="text-sm text-slate-900 font-bold block mt-0.5">+1,200 km / ciclo</strong>
            </div>
            <div>
              <span class="text-slate-400 block uppercase text-[10px]">Protocolos Integrados</span>
              <strong class="text-sm text-expo-blue font-bold block mt-0.5">VRP, QR, Offline PWA, 90d</strong>
            </div>
          </div>

        </div>

        <!-- Lista Acordeón de Protocolos Operativos Estilo Stripe -->
        <div class="divide-y divide-slate-200 border-y border-slate-200">
          
          <div class="py-5">
            <button
              type="button"
              (click)="toggleAccordion('p1')"
              class="w-full flex items-center justify-between text-left group">
              <div class="flex items-center gap-3">
                <span class="h-6 w-6 rounded bg-blue-50 text-expo-blue flex items-center justify-center font-mono text-xs font-bold">1</span>
                <span class="text-base font-bold text-slate-900 group-hover:text-expo-blue transition-colors">
                  Contingencia vial y replanificación en ruta ante bloqueos
                </span>
              </div>
              <svg lucidePlus [size]="18" class="text-slate-400 group-hover:text-slate-900 transition-transform" [class.rotate-45]="accordions['p1']"></svg>
            </button>
            @if (accordions['p1']) {
              <p class="text-sm text-slate-600 pt-3 pl-9 max-w-3xl leading-relaxed">
                Cuando una vía es cerrada por mantenimiento o emergencia, el motor de rutas recalcula de forma determinista la secuencia de paradas pendientes, actualiza la hora estimada de llegada y avisa al chofer sin alterar las entregas ya asentadas.
              </p>
            }
          </div>

          <div class="py-5">
            <button
              type="button"
              (click)="toggleAccordion('p2')"
              class="w-full flex items-center justify-between text-left group">
              <div class="flex items-center gap-3">
                <span class="h-6 w-6 rounded bg-cyan-50 text-expo-cyanDeep flex items-center justify-center font-mono text-xs font-bold">2</span>
                <span class="text-base font-bold text-slate-900 group-hover:text-expo-cyanDeep transition-colors">
                  Custodia inmutable con tokens criptográficos SHA-256
                </span>
              </div>
              <svg lucidePlus [size]="18" class="text-slate-400 group-hover:text-slate-900 transition-transform" [class.rotate-45]="accordions['p2']"></svg>
            </button>
            @if (accordions['p2']) {
              <p class="text-sm text-slate-600 pt-3 pl-9 max-w-3xl leading-relaxed">
                Cada bulto físico generado en bodega lleva una etiqueta QR respaldada por un hash criptográfico. La consulta pública es segura y permite a cualquier auditor o cliente verificar su origen sin exponer datos internos.
              </p>
            }
          </div>

          <div class="py-5">
            <button
              type="button"
              (click)="toggleAccordion('p3')"
              class="w-full flex items-center justify-between text-left group">
              <div class="flex items-center gap-3">
                <span class="h-6 w-6 rounded bg-emerald-50 text-emerald-600 flex items-center justify-center font-mono text-xs font-bold">3</span>
                <span class="text-base font-bold text-slate-900 group-hover:text-emerald-600 transition-colors">
                  Recepción hospitalaria con firma digital en modo avión
                </span>
              </div>
              <svg lucidePlus [size]="18" class="text-slate-400 group-hover:text-slate-900 transition-transform" [class.rotate-45]="accordions['p3']"></svg>
            </button>
            @if (accordions['p3']) {
              <p class="text-sm text-slate-600 pt-3 pl-9 max-w-3xl leading-relaxed">
                En subsuelos hospitalarios sin red móvil, el receptor escanea el bulto y asienta su conformidad con firma digital táctil. Los datos se resguardan en IndexedDB y se sincronizan de forma idempotente con la API al recuperar la conexión.
              </p>
            }
          </div>

        </div>

      </section>

      <!-- ===================================================================== -->
      <!-- 5. TRES PILARES CON ICONOS Y TESTIMONIO (Estilo Stripe Bottom)        -->
      <!-- ===================================================================== -->
      <section class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 sm:py-24 space-y-16">
        
        <!-- 3 Columnas con Iconos de Línea en Cajas Cuadradas -->
        <div class="grid md:grid-cols-3 gap-8">
          
          <div class="space-y-4">
            <div class="h-10 w-10 rounded-lg border border-blue-200 bg-blue-50/50 flex items-center justify-center text-expo-blue">
              <svg lucideZap [size]="18" class="stroke-[2.2]"></svg>
            </div>
            <h3 class="text-base font-bold text-slate-900">
              Detección sin falsos positivos
            </h3>
            <p class="text-sm text-slate-600 leading-relaxed">
              Cruza existencias físicas, consumo diario y lead time de transporte con fórmulas deterministas verificables.
            </p>
            <button type="button" (click)="session.login('/inicio')" class="inline-flex items-center gap-1 text-xs font-bold text-expo-blue hover:underline cursor-pointer">
              <span>Ver panel operativo</span>
              <svg lucideChevronRight [size]="14"></svg>
            </button>
          </div>

          <div class="space-y-4">
            <div class="h-10 w-10 rounded-lg border border-cyan-200 bg-cyan-50/50 flex items-center justify-center text-expo-cyanDeep">
              <svg lucideBarChart3 [size]="18" class="stroke-[2.2]"></svg>
            </div>
            <h3 class="text-base font-bold text-slate-900">
              Rutas factibles con PostGIS
            </h3>
            <p class="text-sm text-slate-600 leading-relaxed">
              Calcula secuencias óptimas considerando ventanas de recepción y capacidades vehiculares mediante OSRM.
            </p>
            <button type="button" (click)="session.login('/rutas')" class="inline-flex items-center gap-1 text-xs font-bold text-expo-blue hover:underline cursor-pointer">
              <span>Explorar mapa</span>
              <svg lucideChevronRight [size]="14"></svg>
            </button>
          </div>

          <div class="space-y-4">
            <div class="h-10 w-10 rounded-lg border border-emerald-200 bg-emerald-50/50 flex items-center justify-center text-emerald-600">
              <svg lucideLock [size]="18" class="stroke-[2.2]"></svg>
            </div>
            <h3 class="text-base font-bold text-slate-900">
              Custodia física inviolable
            </h3>
            <p class="text-sm text-slate-600 leading-relaxed">
              El receptor en destino dictamina la entrega conforme y sella el pasaporte con firma táctil digital.
            </p>
            <a routerLink="/public-trace" class="inline-flex items-center gap-1 text-xs font-bold text-expo-blue hover:underline">
              <span>Consultar pasaporte</span>
              <svg lucideChevronRight [size]="14"></svg>
            </a>
          </div>

        </div>

        <!-- Cita Testimonial Estilo Stripe -->
        <div class="max-w-4xl mx-auto text-center space-y-6 pt-10">
          <div class="h-14 w-14 rounded-full bg-gradient-to-tr from-expo-blue to-expo-cyan text-white font-bold text-lg flex items-center justify-center mx-auto shadow-md">
            AM
          </div>

          <blockquote class="text-xl sm:text-2xl font-medium text-slate-800 leading-relaxed max-w-3xl mx-auto">
            « Con IstpetDev dejamos de apagar incendios. Ya no esperamos la llamada desesperada de un hospital sin desinfectante: sabemos con 5 días de anticipación qué abastecer y conservamos la evidencia física de cada entrega sin depender de la señal móvil. »
          </blockquote>

          <div class="space-y-1">
            <p class="text-sm font-bold text-slate-900">Dra. Andrea Morales</p>
            <p class="text-xs text-slate-500 font-mono">Dirección de Operaciones y Abastecimiento de Higiene Hospitalaria</p>
          </div>

          <div>
            <button type="button" (click)="session.login('/inicio')" class="text-xs font-bold text-expo-blue hover:underline inline-flex items-center gap-1 cursor-pointer">
              <span>Ver caso de estudio completo</span>
              <svg lucideChevronRight [size]="14"></svg>
            </button>
          </div>
        </div>

      </section>

      <!-- ===================================================================== -->
      <!-- 6. CIERRE INFERIOR Y LLAMADO A LA ACCIÓN (Estilo Stripe Dark Banner)  -->
      <!-- ===================================================================== -->
      <section class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-16">
        <div class="rounded-3xl bg-expo-dark text-white p-8 sm:p-14 relative overflow-hidden shadow-2xl">
          <div aria-hidden="true" class="absolute -right-20 -bottom-20 w-96 h-96 rounded-full bg-gradient-to-br from-expo-blue via-expo-cyan to-expo-mint blur-3xl opacity-25 pointer-events-none"></div>

          <div class="relative z-10 max-w-2xl space-y-6">
            <span class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-mono font-semibold bg-white/10 text-expo-cyanLight border border-white/10">
              Expo Clean Ecuador 2026 · Reto 1
            </span>
            <h2 class="text-3xl sm:text-5xl font-extrabold tracking-tight leading-tight">
              La plataforma lista para la evaluación en vivo.
            </h2>
            <p class="text-slate-300 text-base sm:text-lg leading-relaxed">
              Inicia sesión con tu rol asignado en la Torre de Control o escanea el código QR de un bulto con tu propio teléfono móvil para auditar la custodia.
            </p>
            <div class="flex flex-wrap items-center gap-4 pt-2">
              <button
                type="button"
                (click)="session.login('/inicio')"
                class="inline-flex items-center gap-2 rounded-full bg-expo-blue hover:bg-expo-blueDark px-7 py-3.5 font-bold text-white shadow-lg shadow-blue-900/40 transition-colors text-sm cursor-pointer">
                <span>Ingresar a Torre de Control</span>
                <svg lucideArrowRight [size]="16" class="stroke-[2.5]"></svg>
              </button>
              <a
                routerLink="/public-trace"
                class="inline-flex items-center gap-2 rounded-full border border-slate-700 bg-white/5 hover:bg-white/10 px-6 py-3.5 font-semibold text-white transition-colors text-sm">
                <svg lucideQrCode [size]="16" class="stroke-[2] text-expo-cyan"></svg>
                <span>Consultar Pasaporte QR</span>
              </a>
            </div>
          </div>
        </div>
      </section>

    </div>
  `
})
export class LandingPage implements OnInit {
  readonly session = inject(AuthSession);
  private readonly router = inject(Router);

  accordions: Record<string, boolean> = {
    p1: true,
    p2: false,
    p3: false
  };

  ngOnInit(): void {
    if (this.session.status() === 'authenticated') {
      void this.router.navigateByUrl('/inicio');
    }
  }

  toggleAccordion(id: string): void {
    this.accordions[id] = !this.accordions[id];
  }
}
