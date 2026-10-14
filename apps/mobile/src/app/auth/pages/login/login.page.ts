import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardSubtitle,
  IonCardContent,
  IonButton,
  IonBadge,
  IonSpinner
} from '@ionic/angular/standalone';
import { MobileAuthService } from '../../auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardSubtitle,
    IonCardContent,
    IonButton,
    IonBadge,
    IonSpinner
  ],
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar color="primary">
        <ion-title>IstpetDev Móvil</ion-title>
        <div slot="end" class="mr-3">
          <span class="text-[10px] font-mono bg-white/20 text-white px-2 py-0.5 rounded font-bold">
            OIDC PKCE
          </span>
        </div>
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true" class="ion-padding bg-slate-50">
      <div class="max-w-md mx-auto space-y-4 py-2">
        <!-- Encabezado Institucional -->
        <div class="text-center pt-2 pb-1 space-y-1">
          <div class="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-gradient-to-br from-teal-600 to-cyan-700 text-white shadow-lg mb-2">
            <svg class="w-8 h-8 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
            </svg>
          </div>
          <h1 class="text-2xl font-black text-slate-900 tracking-tight">
            Acceso al Sistema Logístico
          </h1>
          <p class="text-xs text-slate-500 max-w-xs mx-auto leading-relaxed">
            Plataforma móvil de custodia de insumos hospitalarios y distribución en ruta
          </p>
          <div class="pt-1 flex items-center justify-center gap-1.5">
            <span class="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
            <span class="text-[11px] font-semibold text-emerald-700">Sistema en Línea • Operaciones de Campo</span>
          </div>
        </div>

        <!-- Tarjeta Principal de Inicio de Sesión -->
        <ion-card class="m-0 border border-slate-200/80 shadow-md rounded-2xl overflow-hidden bg-white">
          <ion-card-header class="pb-3 border-b border-slate-100 bg-slate-50/50">
            <div class="flex items-center justify-between">
              <ion-card-subtitle class="text-xs font-bold text-slate-500 uppercase tracking-wider">
                Credenciales Institucionales
              </ion-card-subtitle>
              <ion-badge color="primary">Ambiente Seguro</ion-badge>
            </div>
            <ion-card-title class="text-base text-slate-900 font-bold">
              Iniciar Sesión
            </ion-card-title>
          </ion-card-header>

          <ion-card-content class="pt-4 space-y-4">
            <!-- Error alert si las credenciales fallan -->
            @if (errorMessage()) {
              <div class="p-3 bg-red-50 border border-red-200 rounded-xl text-xs text-red-800 flex items-start gap-2.5">
                <svg class="w-5 h-5 text-red-600 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                </svg>
                <div class="flex-1">
                  <span class="font-bold block">Error de autenticación</span>
                  <span>{{ errorMessage() }}</span>
                </div>
              </div>
            }

            <!-- Success notification si se autentica -->
            @if (successMessage()) {
              <div class="p-3 bg-emerald-50 border border-emerald-200 rounded-xl text-xs text-emerald-800 flex items-start gap-2.5">
                <svg class="w-5 h-5 text-emerald-600 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
                </svg>
                <div class="flex-1">
                  <span class="font-bold block">Sesión Autorizada</span>
                  <span>{{ successMessage() }}</span>
                </div>
              </div>
            }

            <form (ngSubmit)="onSubmit($event)" class="space-y-3.5">
              <!-- Campo Correo -->
              <div class="space-y-1">
                <label class="text-xs font-bold text-slate-700 block">
                  Correo Electrónico Institucional
                </label>
                <div class="relative flex items-center bg-slate-50 rounded-xl border border-slate-300 focus-within:border-teal-500 focus-within:ring-2 focus-within:ring-teal-100 transition px-3 py-1">
                  <svg class="w-4 h-4 text-slate-400 mr-2 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                  </svg>
                  <input
                    type="email"
                    [(ngModel)]="email"
                    name="email"
                    required
                    [disabled]="loading()"
                    placeholder="ejemplo@istpetdev.ec"
                    class="w-full bg-transparent py-2 text-sm text-slate-800 placeholder-slate-400 focus:outline-none" />
                </div>
              </div>

              <!-- Campo Contraseña -->
              <div class="space-y-1">
                <div class="flex items-center justify-between">
                  <label class="text-xs font-bold text-slate-700">
                    Contraseña de Acceso
                  </label>
                  <button
                    type="button"
                    (click)="togglePasswordVisibility()"
                    class="text-[11px] font-semibold text-teal-700 hover:text-teal-900 transition">
                    {{ showPassword() ? 'Ocultar' : 'Ver contraseña' }}
                  </button>
                </div>
                <div class="relative flex items-center bg-slate-50 rounded-xl border border-slate-300 focus-within:border-teal-500 focus-within:ring-2 focus-within:ring-teal-100 transition px-3 py-1">
                  <svg class="w-4 h-4 text-slate-400 mr-2 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                  </svg>
                  <input
                    [type]="showPassword() ? 'text' : 'password'"
                    [(ngModel)]="password"
                    name="password"
                    required
                    [disabled]="loading()"
                    placeholder="••••••••••••"
                    class="w-full bg-transparent py-2 text-sm text-slate-800 placeholder-slate-400 focus:outline-none" />
                </div>
              </div>

              <!-- Opciones extras -->
              <div class="flex items-center justify-between pt-1 text-xs">
                <label class="flex items-center gap-2 cursor-pointer text-slate-600 select-none">
                  <input
                    type="checkbox"
                    [(ngModel)]="rememberMe"
                    name="rememberMe"
                    class="rounded border-slate-300 text-teal-600 focus:ring-teal-500 w-4 h-4" />
                  <span>Recordar este terminal</span>
                </label>
                <span class="text-[11px] font-mono text-slate-400">Volatile Token</span>
              </div>

              <!-- Botón de Envío -->
              <div class="pt-2">
                <ion-button
                  expand="block"
                  type="submit"
                  color="primary"
                  [disabled]="loading()"
                  class="font-bold shadow-md">
                  @if (loading()) {
                    <ion-spinner name="crescent" class="mr-2" style="width: 18px; height: 18px;"></ion-spinner>
                    Autenticando...
                  } @else {
                    Ingresar al Sistema Logístico
                  }
                </ion-button>
              </div>
            </form>

            <!-- Divisor -->
            <div class="relative py-2">
              <div class="absolute inset-0 flex items-center">
                <div class="w-full border-t border-slate-200"></div>
              </div>
              <div class="relative flex justify-center text-xs uppercase">
                <span class="bg-white px-3 font-semibold text-slate-500 text-[10px] tracking-wider">
                  Acceso Rápido Demo
                </span>
              </div>
            </div>

            <!-- Perfiles Rápidos -->
            <div class="space-y-2">
              <div class="flex items-center justify-between text-xs">
                <span class="font-bold text-slate-700">Seleccione un perfil para ingresar:</span>
                <span class="text-[10px] bg-teal-50 text-teal-700 px-2 py-0.5 rounded font-mono font-bold">1-Clic</span>
              </div>

              <!-- Conductor -->
              <button
                type="button"
                (click)="loginWithDemoRole('driver')"
                [disabled]="loading()"
                class="w-full text-left p-3 rounded-xl border border-slate-200 bg-white hover:bg-teal-50/60 hover:border-teal-400 active:scale-[0.99] transition flex items-center justify-between shadow-sm">
                <div class="flex items-center gap-3">
                  <div class="w-10 h-10 rounded-lg bg-teal-100 text-teal-800 flex items-center justify-center shrink-0">
                    <svg class="w-5 h-5 text-teal-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8a1 1 0 011-1h2.586a1 1 0 01.707.293l3.414 3.414a1 1 0 01.293.707V16a1 1 0 01-1 1h-1m-6-1a1 1 0 001 1h1M5 17a2 2 0 104 0m-4 0a2 2 0 114 0m6 0a2 2 0 104 0m-4 0a2 2 0 114 0" />
                    </svg>
                  </div>
                  <div>
                    <span class="text-xs font-black text-slate-900 block">Carlos Chofer</span>
                    <span class="text-[11px] text-slate-500 block">Conductor de Custodia • VH-EC-101</span>
                  </div>
                </div>
                <span class="text-xs text-teal-700 font-bold">Entrar</span>
              </button>

              <!-- Operador de Bodega -->
              <button
                type="button"
                (click)="loginWithDemoRole('warehouse')"
                [disabled]="loading()"
                class="w-full text-left p-3 rounded-xl border border-slate-200 bg-white hover:bg-sky-50/60 hover:border-sky-400 active:scale-[0.99] transition flex items-center justify-between shadow-sm">
                <div class="flex items-center gap-3">
                  <div class="w-10 h-10 rounded-lg bg-sky-100 text-sky-800 flex items-center justify-center shrink-0">
                    <svg class="w-5 h-5 text-sky-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
                    </svg>
                  </div>
                  <div>
                    <span class="text-xs font-black text-slate-900 block">Elena Bodega</span>
                    <span class="text-[11px] text-slate-500 block">Operadora de Bodega • WH-CENTRAL-01</span>
                  </div>
                </div>
                <span class="text-xs text-sky-700 font-bold">Entrar</span>
              </button>

              <!-- Receptor Hospitalario -->
              <button
                type="button"
                (click)="loginWithDemoRole('receiver')"
                [disabled]="loading()"
                class="w-full text-left p-3 rounded-xl border border-slate-200 bg-white hover:bg-emerald-50/60 hover:border-emerald-400 active:scale-[0.99] transition flex items-center justify-between shadow-sm">
                <div class="flex items-center gap-3">
                  <div class="w-10 h-10 rounded-lg bg-emerald-100 text-emerald-800 flex items-center justify-center shrink-0">
                    <svg class="w-5 h-5 text-emerald-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                  </div>
                  <div>
                    <span class="text-xs font-black text-slate-900 block">Lic. Marco Receptor</span>
                    <span class="text-[11px] text-slate-500 block">Receptor Hospitalario • Escaneo y Novedades</span>
                  </div>
                </div>
                <span class="text-xs text-emerald-700 font-bold">Entrar</span>
              </button>

              <!-- Supervisor -->
              <button
                type="button"
                (click)="loginWithDemoRole('manager')"
                [disabled]="loading()"
                class="w-full text-left p-3 rounded-xl border border-slate-200 bg-white hover:bg-indigo-50/60 hover:border-indigo-400 active:scale-[0.99] transition flex items-center justify-between shadow-sm">
                <div class="flex items-center gap-3">
                  <div class="w-10 h-10 rounded-lg bg-indigo-100 text-indigo-800 flex items-center justify-center shrink-0">
                    <svg class="w-5 h-5 text-indigo-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
                    </svg>
                  </div>
                  <div>
                    <span class="text-xs font-black text-slate-900 block">Santiago Supervisor</span>
                    <span class="text-[11px] text-slate-500 block">Supervisor Logístico • Monitoreo</span>
                  </div>
                </div>
                <span class="text-xs text-indigo-700 font-bold">Entrar</span>
              </button>
            </div>
          </ion-card-content>
        </ion-card>

        <!-- Información de Seguridad y Cumplimiento -->
        <div class="p-3 bg-slate-100/90 rounded-xl border border-slate-200 text-[11px] text-slate-500 leading-relaxed text-center space-y-1">
          <div class="flex items-center justify-center gap-1.5 font-bold text-slate-700">
            <svg class="w-3.5 h-3.5 text-slate-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
            </svg>
            <span>Cumplimiento ADR-10 & RNF-02</span>
          </div>
          <p>
            Autenticación segura mediante PKCE S256, tokens JWT en memoria volátil y túnel cifrado de datos.
          </p>
          <div class="text-[10px] text-slate-400 font-mono pt-1">
            IstpetDev Mobile v1.0.0 • Distribución Hospitalaria
          </div>
        </div>
      </div>
    </ion-content>
  `
})
export class LoginPage implements OnInit {
  readonly authService = inject(MobileAuthService);
  private readonly router = inject(Router);

  email = 'carlos.chofer@istpetdev.ec';
  password = 'password123';
  rememberMe = true;

  readonly showPassword = signal(false);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  ngOnInit(): void {
    // Si ya está autenticado, redirigir a /home
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/home']);
    }
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  loginWithDemoRole(role: 'driver' | 'warehouse' | 'manager' | 'receiver'): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const apiRoleMap: Record<string, string> = {
      driver: 'Conductor',
      warehouse: 'Bodega',
      manager: 'Planificador',
      receiver: 'Receptor'
    };

    this.authService.loginViaApi({ role: apiRoleMap[role] }).subscribe({
      next: (ok) => {
        if (!ok) {
          if (role === 'driver') this.authService.loginAsDriver();
          else if (role === 'warehouse') this.authService.loginAsWarehouse();
          else if (role === 'manager') this.authService.loginAsManager();
          else if (role === 'receiver') this.authService.loginAsReceiver();
        }
        this.loading.set(false);
        this.successMessage.set('Acceso concedido. Redirigiendo a operaciones de campo...');
        setTimeout(() => {
          this.router.navigate(['/home']);
        }, 300);
      },
      error: () => {
        if (role === 'driver') this.authService.loginAsDriver();
        else if (role === 'warehouse') this.authService.loginAsWarehouse();
        else if (role === 'manager') this.authService.loginAsManager();
        else if (role === 'receiver') this.authService.loginAsReceiver();
        this.loading.set(false);
        this.successMessage.set('Acceso concedido (Modo Local). Redirigiendo...');
        setTimeout(() => {
          this.router.navigate(['/home']);
        }, 300);
      }
    });
  }

  onSubmit(event: Event): void {
    event.preventDefault();
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (!this.email || !this.password) {
      this.errorMessage.set('Por favor ingrese el correo y la contraseña requeridos.');
      return;
    }

    this.loading.set(true);
    this.authService.loginViaApi({ email: this.email, password: this.password }).subscribe({
      next: (ok) => {
        if (ok) {
          this.loading.set(false);
          this.successMessage.set('Inicio de sesión exitoso. Redirigiendo...');
          setTimeout(() => {
            this.router.navigate(['/home']);
          }, 300);
        } else {
          const res = this.authService.loginWithCredentials(this.email, this.password);
          this.loading.set(false);
          if (res.success) {
            this.successMessage.set('Inicio de sesión exitoso (Modo Local). Redirigiendo...');
            setTimeout(() => {
              this.router.navigate(['/home']);
            }, 300);
          } else {
            this.errorMessage.set(res.error || 'Credenciales inválidas.');
          }
        }
      },
      error: () => {
        const res = this.authService.loginWithCredentials(this.email, this.password);
        this.loading.set(false);
        if (res.success) {
          this.successMessage.set('Inicio de sesión exitoso (Modo Local). Redirigiendo...');
          setTimeout(() => {
            this.router.navigate(['/home']);
          }, 300);
        } else {
          this.errorMessage.set('Error de conexión con el servidor.');
        }
      }
    });
  }
}
