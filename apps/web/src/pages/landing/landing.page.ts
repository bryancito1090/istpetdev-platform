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

@Component({
  selector: 'page-landing',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
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
    <div class="relative overflow-hidden bg-white text-slate-800 min-h-screen">
      
      <!-- Arte Fluido Curvado 3D Estilo Stripe (Ondas de Seda Suaves en Azul Eléctrico, Cyan y Menta) -->
      <div aria-hidden="true" class="pointer-events-none absolute -top-20 right-0 w-[550px] md:w-[700px] lg:w-[850px] h-[800px] overflow-hidden select-none z-0">
        <svg class="w-full h-full" viewBox="0 0 800 800" fill="none" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id="stripeFlow1" x1="100%" y1="0%" x2="0%" y2="100%">
              <stop offset="0%" stop-color="#0066FF" stop-opacity="0.85" />
              <stop offset="45%" stop-color="#00C2FF" stop-opacity="0.8" />
              <stop offset="100%" stop-color="#00E5A3" stop-opacity="0.6" />
            </linearGradient>

            <linearGradient id="stripeFlow2" x1="85%" y1="15%" x2="15%" y2="85%">
              <stop offset="0%" stop-color="#00C2FF" stop-opacity="0.9" />
              <stop offset="55%" stop-color="#0084FF" stop-opacity="0.75" />
              <stop offset="100%" stop-color="#00E5A3" stop-opacity="0.3" />
            </linearGradient>

            <radialGradient id="stripeAtmosphere" cx="65%" cy="35%" r="60%">
              <stop offset="0%" stop-color="#00C2FF" stop-opacity="0.35" />
              <stop offset="50%" stop-color="#0066FF" stop-opacity="0.12" />
              <stop offset="100%" stop-color="#ffffff" stop-opacity="0" />
            </radialGradient>

            <filter id="softAtmosphere" x="-30%" y="-30%" width="160%" height="160%">
              <feGaussianBlur stdDeviation="45" />
            </filter>
            
            <filter id="silkBlur" x="-20%" y="-20%" width="140%" height="140%">
              <feGaussianBlur stdDeviation="22" />
            </filter>
          </defs>

          <!-- Resplandor atmosférico envolvente sin bordes duros -->
          <circle cx="580" cy="320" r="360" fill="url(#stripeAtmosphere)" filter="url(#softAtmosphere)" />

          <!-- Capa 1: Sábana fluida suave diagonal -->
          <path
            d="M520 -40 C660 90, 760 270, 680 470 C600 650, 460 730, 390 820 L840 820 L840 -40 Z"
            fill="url(#stripeFlow1)"
            filter="url(#silkBlur)"
            opacity="0.7"
          />

          <!-- Capa 2: Cinta interior curvada con reflejo satinado tipo seda -->
          <path
            d="M580 -60 C690 80, 740 250, 640 430 C550 590, 490 680, 430 790 C480 760, 560 670, 640 520 C740 350, 760 160, 670 -60 Z"
            fill="url(#stripeFlow2)"
            filter="url(#silkBlur)"
            opacity="0.8"
          />

          <!-- Capa 3: Trazo brillante blanco translúcido para dar volumen y profundidad -->
          <path
            d="M610 -20 C710 110, 740 270, 630 440 C560 560, 510 650, 470 740 C500 720, 560 620, 640 480 C730 320, 750 140, 650 -20 Z"
            fill="white"
            opacity="0.32"
            filter="url(#silkBlur)"
          />
        </svg>
      </div>

      <!-- ===================================================================== -->
      <!-- 1. HERO SECTION ESTILO STRIPE 1:1 (Limpio, Sin Bloques Rígidos)      -->
      <!-- ===================================================================== -->
      <!-- ===================================================================== -->
      <!-- 1. HERO SECTION ESTILO STRIPE 1:1 (Ajustado al Primer Viewport)       -->
      <!-- ===================================================================== -->
      <section class="relative max-w-7xl mx-auto px-6 sm:px-8 lg:px-10 pt-6 sm:pt-8 pb-4 min-h-[calc(100vh-4rem)] flex flex-col justify-between border-x border-slate-100/90">
        
        <!-- Bloque Principal de Titular y Acciones -->
        <div>
          <!-- Micro-eyebrow tipo Stripe -->
          <p class="text-[13px] sm:text-[14px] text-[#425466] font-normal tracking-tight mb-5 sm:mb-6">
            Red logística que opera con IstpetDev: <span class="text-[#0A2540] font-normal">80 Puntos de Servicio · 100% Cobertura Activa</span>
          </p>

          <!-- Titular Estilo Stripe 1:1 (Flujo Natural, Sin Breaks Forzados) -->
          <div class="max-w-[700px] space-y-6 sm:space-y-7">
            <h1 class="text-[36px] sm:text-[44px] lg:text-[52px] xl:text-[56px] font-normal tracking-[-0.025em] text-[#0A2540] leading-[1.1]">Infraestructura logística <span class="text-transparent bg-clip-text bg-gradient-to-r from-expo-blue via-[#0084FF] to-expo-cyan font-normal">para predecir y asegurar tus </span><span class="text-[#0A2540]">insumos.</span><span class="text-[#425466] font-normal"> Detecta riesgos 5 días antes, optimiza rutas en PostGIS e implementa </span><span class="text-transparent bg-clip-text bg-gradient-to-r from-expo-cyan to-expo-mint font-normal">custodia digital </span><span class="text-[#425466] font-normal">desde bodega hasta la recepción hospitalaria.</span>
            </h1>

            <!-- Acciones Duales Estilo Stripe 1:1 (Rectangulares Suaves rounded-[6px]) -->
            <div class="flex flex-wrap items-center gap-3.5 pt-1 sm:pt-2">
              <button
                type="button"
                (click)="session.login('/inicio')"
                class="inline-flex items-center gap-2 rounded-[6px] bg-expo-blue hover:bg-expo-blueDark px-5 py-2.5 font-medium text-white text-[14px] shadow-xs transition-all hover:shadow hover:-translate-y-0.5 active:translate-y-0 cursor-pointer">
                <span>Primeros pasos</span>
                <span class="text-white/80 text-xs font-semibold">&gt;</span>
              </button>

              <a
                routerLink="/public-trace"
                class="inline-flex items-center gap-2 rounded-[6px] border border-slate-200/90 bg-white hover:bg-slate-50 px-4.5 py-2.5 font-medium text-[#0A2540] text-[14px] shadow-2xs transition-all hover:border-slate-300">
                <svg lucideQrCode [size]="15" class="text-expo-blue stroke-[2]"></svg>
                <span>Consultar Pasaporte QR</span>
              </a>
            </div>
          </div>
        </div>

        <!-- =================================================================== -->
        <!-- CINTA MÓVIL CONTINUA INFINITA CON ORGANIZACIONES DE LA BÓVEDA      -->
        <!-- =================================================================== -->
        <div class="w-full border-t border-b border-slate-200/70 py-4 my-2 overflow-hidden relative tape-fade-mask select-none mt-auto">
          <div class="animate-marquee-tape items-center gap-12 text-[#0A2540]/65 text-xs sm:text-[13px] font-medium tracking-wide">
            
            <!-- Grupo 1: Entidades Oficiales de la Bóveda -->
            <div class="flex items-center gap-12 shrink-0">
              <div class="flex items-center gap-2 hover:text-[#0A2540] transition-colors">
                <svg class="w-4 h-4 text-expo-blue stroke-[2.2]" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" />
                </svg>
                <span class="font-semibold tracking-tight">HOSPITAL LAURA</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-bold text-sm tracking-tighter">CLÍNICA</span>
                <span class="font-medium text-xs text-slate-500">SAN RAFAEL</span>
              </div>

              <div class="flex items-center gap-2 hover:text-[#0A2540] transition-colors">
                <div class="w-3.5 h-3.5 rounded-full border-2 border-expo-cyan"></div>
                <span class="font-semibold tracking-tight">HOSPITAL DEL SUR</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-mono font-bold tracking-tight text-slate-800">ALMACÉN CEQ</span>
                <span class="text-[9px] uppercase font-mono px-1.5 py-0.5 bg-slate-100 rounded text-slate-600 font-semibold">HUB QUITO</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-bold tracking-tight text-slate-900">JIMCORPSERVI</span>
                <span class="text-[9px] uppercase font-mono px-1 bg-blue-50 text-expo-blue rounded font-semibold">SECTOR</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-bold text-sm tracking-normal">FEDELIMP</span>
                <span class="text-[10px] text-slate-400 font-medium">ECUADOR</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-semibold tracking-tight">LACIF</span>
                <span class="font-mono text-[10px] text-expo-blue font-bold">LOGISTICS</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-bold tracking-tight text-slate-800">ACESS</span>
                <span class="text-[10px] text-emerald-600 font-medium">SALUD PÚBLICA</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-mono font-bold text-slate-700">SERCOP</span>
                <span class="text-[9px] uppercase font-mono px-1 bg-amber-50 text-amber-700 rounded font-semibold">HOSPITALARIO</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-bold text-sm tracking-normal">LABORATORIOS</span>
                <span class="font-semibold text-xs text-expo-blue uppercase tracking-widest">EC</span>
              </div>

              <div class="flex items-center gap-2 hover:text-[#0A2540] transition-colors">
                <span class="font-semibold tracking-tight">DISTRIBUIDORA GUAYAS</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-bold tracking-tight text-slate-800">RED SUR</span>
                <span class="text-[10px] text-slate-500 font-medium">PICHINCHA</span>
              </div>
            </div>

            <!-- Grupo 2: Duplicado para Scroll Infinito Fluido Continuo -->
            <div class="flex items-center gap-12 shrink-0" aria-hidden="true">
              <div class="flex items-center gap-2 hover:text-[#0A2540] transition-colors">
                <svg class="w-4 h-4 text-expo-blue stroke-[2.2]" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" />
                </svg>
                <span class="font-semibold tracking-tight">HOSPITAL LAURA</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-bold text-sm tracking-tighter">CLÍNICA</span>
                <span class="font-medium text-xs text-slate-500">SAN RAFAEL</span>
              </div>

              <div class="flex items-center gap-2 hover:text-[#0A2540] transition-colors">
                <div class="w-3.5 h-3.5 rounded-full border-2 border-expo-cyan"></div>
                <span class="font-semibold tracking-tight">HOSPITAL DEL SUR</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-mono font-bold tracking-tight text-slate-800">ALMACÉN CEQ</span>
                <span class="text-[9px] uppercase font-mono px-1.5 py-0.5 bg-slate-100 rounded text-slate-600 font-semibold">HUB QUITO</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-bold tracking-tight text-slate-900">JIMCORPSERVI</span>
                <span class="text-[9px] uppercase font-mono px-1 bg-blue-50 text-expo-blue rounded font-semibold">SECTOR</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-bold text-sm tracking-normal">FEDELIMP</span>
                <span class="text-[10px] text-slate-400 font-medium">ECUADOR</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-semibold tracking-tight">LACIF</span>
                <span class="font-mono text-[10px] text-expo-blue font-bold">LOGISTICS</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-bold tracking-tight text-slate-800">ACESS</span>
                <span class="text-[10px] text-emerald-600 font-medium">SALUD PÚBLICA</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-mono font-bold text-slate-700">SERCOP</span>
                <span class="text-[9px] uppercase font-mono px-1 bg-amber-50 text-amber-700 rounded font-semibold">HOSPITALARIO</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-bold text-sm tracking-normal">LABORATORIOS</span>
                <span class="font-semibold text-xs text-expo-blue uppercase tracking-widest">EC</span>
              </div>

              <div class="flex items-center gap-2 hover:text-[#0A2540] transition-colors">
                <span class="font-semibold tracking-tight">DISTRIBUIDORA GUAYAS</span>
              </div>

              <div class="flex items-center gap-1.5 hover:text-[#0A2540] transition-colors">
                <span class="font-bold tracking-tight text-slate-800">RED SUR</span>
                <span class="text-[10px] text-slate-500 font-medium">PICHINCHA</span>
              </div>
            </div>

          </div>
        </div>

      </section>

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
              Hospital Laura optimiza su abastecimiento quirúrgico con IstpetDev.
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
