import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import {
  FieldUserRole,
  MobileSessionState,
  MobileUserProfile,
  DEMO_DRIVER_PROFILE,
  DEMO_WAREHOUSE_PROFILE,
  DEMO_MANAGER_PROFILE
} from './auth.models';

const VOLATILE_SESSION_KEY = 'istpetdev_volatile_mobile_session';

@Injectable({
  providedIn: 'root'
})
export class MobileAuthService {
  private readonly router = inject(Router);
  private readonly oidcSecurityService = inject(OidcSecurityService, { optional: true });

  /**
   * Bandera para habilitar sesión simulada durante desarrollo y pruebas frontend-first.
   */
  readonly useMockSession = signal<boolean>(true);

  private readonly sessionState = signal<MobileSessionState>(this.initVolatileSession());

  // Señales públicas reactivas
  readonly isAuthenticated = computed(() => this.sessionState().isAuthenticated);
  readonly currentUser = computed(() => this.sessionState().user);
  readonly currentRole = computed<FieldUserRole | null>(() => this.sessionState().user?.role ?? null);
  readonly currentVehicleId = computed<string | null>(() => this.sessionState().user?.vehicleId ?? null);
  readonly accessToken = computed(() => this.sessionState().accessToken);

  /**
   * Carga sesión transitoria desde sessionStorage (memoria volátil por pestaña).
   * NFR-03: No almacenar tokens OIDC en IndexedDB durable.
   */
  private initVolatileSession(): MobileSessionState {
    if (typeof window === 'undefined') {
      return this.defaultAnonymousState();
    }

    try {
      const raw = sessionStorage.getItem(VOLATILE_SESSION_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as MobileSessionState;
        if (parsed && parsed.isAuthenticated && parsed.user) {
          return parsed;
        }
      }
    } catch {
      // Ignorar fallo de sessionStorage
    }

    // Por defecto en la PWA móvil iniciamos con el perfil de conductor activo para facilitar pruebas
    return this.createMockSessionState(DEMO_DRIVER_PROFILE);
  }

  private defaultAnonymousState(): MobileSessionState {
    return {
      isAuthenticated: false,
      user: null,
      accessToken: null,
      expiresAtUtc: null
    };
  }

  private createMockSessionState(profile: MobileUserProfile): MobileSessionState {
    const fakeToken = `mock-bearer-token-${profile.role.toLowerCase()}-${profile.id}`;
    const expiresAt = new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString();
    return {
      isAuthenticated: true,
      user: profile,
      accessToken: fakeToken,
      expiresAtUtc: expiresAt
    };
  }

  private persistVolatileSession(state: MobileSessionState): void {
    this.sessionState.set(state);
    if (typeof window !== 'undefined') {
      try {
        if (state.isAuthenticated) {
          sessionStorage.setItem(VOLATILE_SESSION_KEY, JSON.stringify(state));
        } else {
          sessionStorage.removeItem(VOLATILE_SESSION_KEY);
        }
      } catch {
        // Ignorar fallos de cuota o modo privado
      }
    }
  }

  /**
   * Conmuta la sesión activa al perfil de Conductor de Ruta (CarrierDriver).
   */
  loginAsDriver(): void {
    const state = this.createMockSessionState(DEMO_DRIVER_PROFILE);
    this.persistVolatileSession(state);
  }

  /**
   * Conmuta la sesión activa al perfil de Operador de Bodega (WarehouseOperator).
   */
  loginAsWarehouse(): void {
    const state = this.createMockSessionState(DEMO_WAREHOUSE_PROFILE);
    this.persistVolatileSession(state);
  }

  /**
   * Conmuta la sesión activa al perfil de Supervisor / Gerente Logístico (LogisticsManager).
   */
  loginAsManager(): void {
    const state = this.createMockSessionState(DEMO_MANAGER_PROFILE);
    this.persistVolatileSession(state);
  }

  /**
   * Verifica si el usuario actual posee alguno de los roles solicitados.
   */
  hasAnyRole(roles: FieldUserRole[]): boolean {
    const userRole = this.currentRole();
    if (!userRole) return false;
    return roles.includes(userRole);
  }

  /**
   * Inicia el flujo OIDC PKCE real con Identity Provider cuando useMockSession=false.
   */
  loginOidc(): void {
    if (this.oidcSecurityService) {
      this.oidcSecurityService.authorize();
    } else {
      this.loginAsDriver();
    }
  }

  /**
   * Cierra la sesión activa y limpia tokens en memoria.
   */
  logout(): void {
    this.persistVolatileSession(this.defaultAnonymousState());
    if (this.oidcSecurityService && !this.useMockSession()) {
      this.oidcSecurityService.logoff().subscribe();
    }
    this.router.navigate(['/home']);
  }
}
