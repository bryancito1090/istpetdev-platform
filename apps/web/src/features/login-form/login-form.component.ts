import { Component, EventEmitter, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SessionStore, UserRole, DEMO_PROFILES } from '@entities/session';
import { ButtonComponent, BadgeComponent, InputComponent } from '@shared/ui';

@Component({
  selector: 'feature-login-form',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonComponent,
    BadgeComponent,
    InputComponent
  ],
  template: `
    <div class="space-y-6">
      <!-- Mensaje de error accesible si falla el login -->
      @if (errorMessage()) {
        <div 
          role="alert" 
          class="p-3.5 bg-red-50 border border-red-200 rounded-lg text-xs text-red-800 flex items-start gap-2.5 animate-fadeIn">
          <svg class="w-4 h-4 text-red-600 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
          <div class="flex-1">
            <span class="font-semibold block">Error de autenticación</span>
            <span>{{ errorMessage() }}</span>
          </div>
        </div>
      }

      <form (submit)="onSubmit($event)" class="space-y-4">
        <!-- Campo Correo -->
        <ui-input
          label="Correo institucional"
          type="email"
          placeholder="ejemplo@istpetdev.ec"
          [required]="true"
          [(ngModel)]="email"
          name="email"
          [disabled]="loading()">
        </ui-input>

        <!-- Campo Contraseña -->
        <div>
          <div class="relative">
            <ui-input
              label="Contraseña de acceso"
              [type]="showPassword() ? 'text' : 'password'"
              placeholder="••••••••••••"
              [required]="true"
              [(ngModel)]="password"
              name="password"
              [disabled]="loading()">
            </ui-input>

            <button
              type="button"
              (click)="togglePasswordVisibility()"
              class="absolute right-3 top-8 text-xs font-medium text-slate-500 hover:text-slate-800 focus:outline-none transition-colors"
              [attr.aria-label]="showPassword() ? 'Ocultar contraseña' : 'Ver contraseña'">
              {{ showPassword() ? 'Ocultar' : 'Ver' }}
            </button>
          </div>
        </div>

        <div class="flex items-center justify-between text-xs pt-1">
          <label class="flex items-center gap-2 cursor-pointer text-slate-600 select-none">
            <input 
              type="checkbox" 
              [(ngModel)]="rememberMe" 
              name="rememberMe"
              class="rounded border-slate-300 text-primary-600 focus:ring-primary-500 w-3.5 h-3.5">
            <span>Recordar dispositivo</span>
          </label>
          <span class="text-slate-400 font-mono text-[11px]">Cognito OIDC PKCE</span>
        </div>

        <div class="pt-2">
          <ui-button
            type="submit"
            variant="primary"
            size="lg"
            [fullWidth]="true"
            [loading]="loading()"
            ariaLabel="Ingresar al sistema logístico">
            <span>Iniciar Sesión en IstpetDev</span>
          </ui-button>
        </div>
      </form>

      <!-- Selector rápido de credenciales demo para evaluación del Reto 1 -->
      <div class="pt-4 border-t border-slate-200">
        <div class="flex items-center justify-between mb-3">
          <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">
            Perfiles Demo Rápidos
          </span>
          <ui-badge variant="neutral" size="sm" label="Acceso Rápido"></ui-badge>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-3 gap-2">
          @for (role of demoRoles; track role.key) {
            <button
              type="button"
              (click)="selectDemoRole(role.key)"
              [disabled]="loading()"
              class="p-2.5 text-left rounded-lg border border-slate-200 hover:border-primary-400 hover:bg-primary-50/50 transition-all focus:outline-none focus:ring-2 focus:ring-primary-500 bg-white">
              <span class="text-xs font-bold text-slate-900 block truncate">{{ role.name }}</span>
              <span class="text-[10px] text-slate-500 block truncate font-mono">{{ role.email }}</span>
            </button>
          }
        </div>
      </div>
    </div>
  `
})
export class LoginFormComponent {
  private readonly sessionStore = inject(SessionStore);
  private readonly router = inject(Router);

  @Output() loginSuccess = new EventEmitter<void>();

  email = 'operador@istpetdev.ec';
  password = 'password123';
  rememberMe = true;

  readonly showPassword = signal(false);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly demoRoles = [
    { key: 'WarehouseOperator' as UserRole, name: 'Operador Bodega (Campo)', email: DEMO_PROFILES.WarehouseOperator.email },
    { key: 'OPERADOR' as UserRole, name: 'Operador Logístico', email: DEMO_PROFILES.OPERADOR.email },
    { key: 'SUPERVISOR' as UserRole, name: 'Supervisor Médico', email: DEMO_PROFILES.SUPERVISOR.email },
    { key: 'AUDITOR' as UserRole, name: 'Auditor Custodia', email: DEMO_PROFILES.AUDITOR.email }
  ];

  togglePasswordVisibility(): void {
    this.showPassword.update(v => !v);
  }

  selectDemoRole(roleKey: UserRole): void {
    const profile = DEMO_PROFILES[roleKey];
    this.email = profile.email;
    this.password = 'password123';
    this.errorMessage.set(null);
    this.submitWithProfile(roleKey);
  }

  submitWithProfile(role: UserRole): void {
    this.loading.set(true);
    setTimeout(() => {
      this.sessionStore.loginAsDemo(role);
      this.loading.set(false);
      this.loginSuccess.emit();
      this.router.navigate(['/dashboard']);
    }, 400);
  }

  onSubmit(event: Event): void {
    event.preventDefault();
    this.errorMessage.set(null);

    if (!this.email || !this.password) {
      this.errorMessage.set('Por favor ingrese el correo y la contraseña requeridos.');
      return;
    }

    this.loading.set(true);
    setTimeout(() => {
      const res = this.sessionStore.loginWithCredentials(this.email, this.password);
      this.loading.set(false);

      if (res.success) {
        this.loginSuccess.emit();
        this.router.navigate(['/dashboard']);
      } else {
        this.errorMessage.set(res.error || 'Credenciales inválidas.');
      }
    }, 500);
  }
}
