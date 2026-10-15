import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
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
      <section class="relative max-w-7xl mx-auto px-6 py-20 sm:py-28 space-y-8">
        <div aria-hidden="true" class="absolute right-0 top-12 w-72 h-72 rounded-full bg-gradient-to-br from-primary-100 via-secondary-100 to-orange-50 blur-3xl"></div>
        <p class="relative text-sm font-semibold text-primary-700">IstpetDev · Abastecimiento institucional</p>
        <h1 class="relative max-w-4xl text-4xl sm:text-6xl tracking-tight leading-tight">
          Planifica tus rutas y sigue la custodia de tus insumos.
        </h1>
        <p class="relative max-w-2xl text-lg text-slate-600">
          Consulta inventario, prepara despachos y registra recepciones con las cuentas autorizadas de tu organización.
        </p>
        <div class="relative flex flex-wrap gap-4">
          <a routerLink="/login" class="rounded-full bg-primary-600 px-6 py-3 font-semibold text-white hover:bg-primary-700">Iniciar sesión</a>
          <a href="/campo/" class="rounded-full border border-slate-300 px-6 py-3 font-semibold hover:bg-slate-50">Abrir aplicación de campo</a>
        </div>
      </section>
      <section class="max-w-7xl mx-auto px-6 pb-20 grid md:grid-cols-3 gap-6" aria-label="Funciones de la plataforma">
        <article class="rounded-3xl border border-slate-200 bg-slate-50 p-8 space-y-3">
          <h2 class="text-xl font-semibold">Inventario y planificación</h2>
          <p class="text-slate-600">Consulta existencias y alertas de cobertura para preparar pedidos y propuestas de rutas.</p>
        </article>
        <article class="rounded-3xl border border-slate-200 bg-primary-50 p-8 space-y-3">
          <h2 class="text-xl font-semibold">Despacho y custodia</h2>
          <p class="text-slate-600">Prepara bultos en bodega y consulta sus entregas y movimientos mediante la etiqueta QR.</p>
        </article>
        <article class="rounded-3xl border border-slate-200 bg-secondary-50 p-8 space-y-3">
          <h2 class="text-xl font-semibold">Recepción en destino</h2>
          <p class="text-slate-600">El receptor consulta el bulto y confirma su recepción con conexión, registrando las novedades encontradas.</p>
        </article>
      </section>
    </div>
  `
})
export class LandingPage implements OnInit {
  readonly session = inject(AuthSession);
  private readonly router = inject(Router);

  ngOnInit(): void {
    if (this.session.status() === 'authenticated') {
      void this.router.navigateByUrl('/inicio');
    }
  }
}

