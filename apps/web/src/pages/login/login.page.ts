import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LoginFormComponent } from '@features/login-form';
import { CardComponent, BadgeComponent } from '@shared/ui';

@Component({
  selector: 'page-login',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    LoginFormComponent,
    CardComponent,
    BadgeComponent
  ],
  template: `
    <div class="min-h-[75vh] flex flex-col items-center justify-center py-6 sm:py-12">
      <div class="w-full max-w-md space-y-6">
        <!-- Encabezado de la página de Login -->
        <div class="text-center space-y-2">
          <div class="inline-flex items-center justify-center h-12 w-12 rounded-xl bg-primary-600 text-white font-black text-2xl shadow-md mb-2">
            I
          </div>
          <h1 class="text-2xl font-extrabold text-slate-900 tracking-tight">
            Acceso al Sistema Logístico
          </h1>
          <p class="text-xs text-slate-500 max-w-sm mx-auto">
            Plataforma de abastecimiento hospitalario y custodia de insumos en tiempo real
          </p>
        </div>

        <!-- Tarjeta del Formulario de Login -->
        <ui-card [padding]="'lg'">
          <div class="flex items-center justify-between border-b border-slate-100 pb-3 mb-6">
            <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">
              Credenciales Autenticadas
            </span>
            <ui-badge variant="transit" size="sm" [withDot]="true" label="Amazon Cognito OIDC"></ui-badge>
          </div>

          <!-- Feature: Formulario con soporte reactivo -->
          <feature-login-form></feature-login-form>
        </ui-card>

        <!-- Información de Seguridad y Enlaces Auxiliares -->
        <div class="space-y-4 text-center">
          <div class="flex items-center justify-center gap-4 text-xs text-slate-500">
            <a routerLink="/" class="hover:text-primary-600 transition-colors">
              ← Volver al portal institucional
            </a>
            <span>•</span>
            <a routerLink="/public-trace" class="hover:text-primary-600 transition-colors">
              Consultar trazabilidad sin ingresar
            </a>
          </div>

          <div class="p-3 bg-slate-100/80 rounded-lg border border-slate-200/80 text-[11px] text-slate-500 max-w-sm mx-auto leading-normal">
            <span class="font-semibold text-slate-700 block mb-0.5">Entorno Seguro (ADR-10 & RNF-02)</span>
            Autenticación protegida mediante PKCE S256, tokens JWT Bearer y túnel cifrado SSM.
          </div>
        </div>
      </div>
    </div>
  `
})
export class LoginPage {}
