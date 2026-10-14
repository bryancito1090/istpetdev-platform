import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, catchError, map, of } from 'rxjs';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import {
  FieldUserRole,
  MobileSessionState,
  MobileUserProfile,
  DEMO_DRIVER_PROFILE,
  DEMO_WAREHOUSE_PROFILE,
  DEMO_RECEIVER_PROFILE,
  DEMO_MANAGER_PROFILE
} from './auth.models';

const VOLATILE_SESSION_KEY = 'istpetdev_volatile_mobile_session';

@Injectable({
  providedIn: 'root'
})
export class MobileAuthService {
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
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

    // Por defecto inicia en estado anónimo para solicitar autenticación en /login
    return this.defaultAnonymousState();
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
   * Conmuta la sesión activa al perfil de Receptor de Hospital (HospitalReceiver).
   */
  loginAsReceiver(): void {
    const state = this.createMockSessionState(DEMO_RECEIVER_PROFILE);
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
   * Intenta autenticar contra el backend API emitiendo un JWT real.
   * Si el backend responde, guarda el token real en memoria volátil; si falla, aplica fallback.
   */
  loginViaApi(credentials: { email?: string; password?: string; role?: string }): Observable<boolean> {
    const baseUrl = typeof window !== 'undefined' && window.location.hostname === 'localhost'
      ? 'http://localhost:5000'
      : '';
    return this.http.post<any>(`${baseUrl}/api/v1/auth/login`, credentials).pipe(
      map(res => {
        if (res && res.accessToken) {
          const mappedRole = this.mapBackendToFieldRole(res.user?.role);
          const profile: MobileUserProfile = {
            id: res.user?.id || 'usr-mobile',
            name: res.user?.name || 'Personal Institucional',
            email: res.user?.email || 'personal@istpetdev.ec',
            role: mappedRole,
            roleLabel: res.user?.roleLabel || 'Personal de Campo',
            vehicleId: mappedRole === 'CarrierDriver' ? 'VH-EC-101' : undefined,
            warehouseId: mappedRole === 'WarehouseOperator' ? 'WH-CENTRAL-01' : undefined,
            avatarInitials: (res.user?.name || 'PI').substring(0, 2).toUpperCase()
          };

          this.persistVolatileSession({
            isAuthenticated: true,
            user: profile,
            accessToken: res.accessToken,
            expiresAtUtc: new Date(Date.now() + (res.expiresIn || 28800) * 1000).toISOString()
          });
          return true;
        }
        return false;
      }),
      catchError(() => of(false))
    );
  }

  private mapBackendToFieldRole(role?: string): FieldUserRole {
    if (role === 'Bodega') return 'WarehouseOperator';
    if (role === 'Conductor') return 'CarrierDriver';
    if (role === 'Receptor') return 'HospitalReceiver';
    if (role === 'Planificador' || role === 'Gerencia') return 'LogisticsManager';
    return 'CarrierDriver';
  }

  /**
   * Autenticación mediante credenciales institucionales (correo y contraseña).
   */
  loginWithCredentials(email: string, password: string): { success: boolean; error?: string } {
    const trimmed = (email || '').trim().toLowerCase();
    if (!trimmed || !password) {
      return { success: false, error: 'Por favor ingrese su correo y contraseña.' };
    }

    if (password.length < 4) {
      return { success: false, error: 'La contraseña debe contener al menos 4 caracteres.' };
    }

    let profile: MobileUserProfile;
    if (trimmed.includes('bodega') || trimmed.includes('warehouse') || trimmed.includes('elena')) {
      profile = DEMO_WAREHOUSE_PROFILE;
    } else if (trimmed.includes('supervisor') || trimmed.includes('manager') || trimmed.includes('santiago')) {
      profile = DEMO_MANAGER_PROFILE;
    } else if (trimmed.includes('receptor') || trimmed.includes('marco')) {
      profile = DEMO_RECEIVER_PROFILE;
    } else {
      profile = {
        ...DEMO_DRIVER_PROFILE,
        email: trimmed,
        name: trimmed.startsWith('carlos') ? DEMO_DRIVER_PROFILE.name : trimmed.split('@')[0]
      };
    }

    const state = this.createMockSessionState(profile);
    this.persistVolatileSession(state);
    return { success: true };
  }

  /**
   * Cierra la sesión activa y limpia tokens en memoria.
   */
  logout(): void {
    this.persistVolatileSession(this.defaultAnonymousState());
    if (this.oidcSecurityService && !this.useMockSession()) {
      this.oidcSecurityService.logoff().subscribe();
    }
    this.router.navigate(['/login']);
  }
}
