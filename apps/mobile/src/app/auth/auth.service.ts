import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom, timeout } from 'rxjs';
import { MeResponse, cognitoLogoutUrl, safeReturnUrl } from '@istpetdev/shared-core';
import { environment } from '../config/environment';
import { FieldRole, FieldSessionStatus } from './auth.models';
import { NAVIGATE_EXTERNAL } from './navigate-external';
import { FieldOutboxService } from '../field/outbox/field-outbox.service';

const RETURN_URL_KEY = 'istpetdev.returnUrl';
const REFRESH_TIMEOUT_MS = 10_000;
const FIELD_ROLES: readonly string[] = ['Bodega', 'Conductor', 'Receptor'];
/** 015: última cuenta de campo (sin tokens) para abrir la app sin señal. Vive junto a la sesión OIDC (D-013-S1). */
const LAST_ACCOUNT_KEY = 'istpetdev.lastFieldAccount';

/** ok: hay access token vigente · none: no hay sesión o Cognito la rechazó · unreachable: no se pudo preguntar a Cognito. */
type SessionCheck = 'ok' | 'none' | 'unreachable';

/** Sesión de la PWA: identidad de Cognito + cuenta de GET /api/v1/me (FR-06). Sesión en localStorage (D-013-S1). */
@Injectable({ providedIn: 'root' })
export class MobileAuthService {
  private readonly oidc = inject(OidcSecurityService);
  private readonly http = inject(HttpClient);
  private readonly navigateExternal = inject(NAVIGATE_EXTERNAL);
  private readonly outbox = inject(FieldOutboxService);
  private readonly state = signal<{ status: FieldSessionStatus; me: MeResponse | null }>({ status: 'loading', me: null });
  private readonly offlineSession = signal(false);
  private removeOnlineListener: (() => void) | null = null;

  /** 015: autenticada con la última cuenta guardada porque al abrir no había señal; la cola espera a renovar la sesión. */
  readonly offline = this.offlineSession.asReadonly();

  readonly status = computed(() => this.state().status);
  readonly me = computed(() => this.state().me);
  readonly currentRole = computed<FieldRole | null>(() => {
    const role = this.state().status === 'authenticated' ? this.state().me?.role : undefined;
    return role && FIELD_ROLES.includes(role) ? (role as FieldRole) : null;
  });

  constructor() {
    inject(DestroyRef).onDestroy(() => this.removeOnlineListener?.());
  }

  async bootstrap(): Promise<void> {
    const session = await this.restoreSession();
    if (session === 'none') {
      this.state.set({ status: 'anonymous', me: null });
      return;
    }
    if (session === 'unreachable') {
      await this.enterOffline();
      return;
    }
    try {
      const me = await firstValueFrom(this.http.get<MeResponse>(`${environment.apiUrl}/api/v1/me`));
      this.state.set({ status: me.platform === 'field' ? 'authenticated' : 'wrong-platform', me });
      this.rememberAccount(me.platform === 'field' ? me : null);
      await this.outbox.setOwner(me.platform === 'field' ? me.accountId : null);
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 403) {
        this.state.set({ status: 'disabled', me: null });
      } else if (error instanceof HttpErrorResponse && (error.status === 0 || error.status >= 500)) {
        await this.enterOffline();
      } else {
        this.state.set({ status: 'error', me: null });
      }
    }
  }

  /** 015, FR-05: al volver la señal se renueva la sesión y se libera la cola; si Cognito la rechaza, se pide login sin borrar la cola. */
  async reconnect(): Promise<void> {
    if (!this.offlineSession()) return;
    const session = await this.restoreSession();
    if (session === 'unreachable') return;
    this.stopWaitingForNetwork();
    this.offlineSession.set(false);
    if (session === 'none') {
      this.state.set({ status: 'anonymous', me: null });
      return;
    }
    await this.bootstrap();
    if (this.offlineSession()) return;
    this.outbox.setSyncHeld(false);
    if (this.state().status === 'authenticated') await this.outbox.syncAllPending();
  }

  /** 015, FR-02: sin red no se exige token vigente para capturar; la identidad sale de la última cuenta guardada. */
  private async enterOffline(): Promise<void> {
    const me = this.lastAccount();
    if (!me) {
      this.state.set({ status: 'error', me: null });
      return;
    }
    this.outbox.setSyncHeld(true);
    this.offlineSession.set(true);
    this.state.set({ status: 'authenticated', me });
    await this.outbox.setOwner(me.accountId);
    this.waitForNetwork();
  }

  private waitForNetwork(): void {
    if (this.removeOnlineListener || typeof window === 'undefined') return;
    const onOnline = () => { void this.reconnect(); };
    window.addEventListener('online', onOnline);
    this.removeOnlineListener = () => window.removeEventListener('online', onOnline);
  }

  private stopWaitingForNetwork(): void {
    this.removeOnlineListener?.();
    this.removeOnlineListener = null;
  }

  private rememberAccount(me: MeResponse | null): void {
    try {
      if (me) {
        const { accountId, displayName, role, platform } = me;
        localStorage.setItem(LAST_ACCOUNT_KEY, JSON.stringify({ accountId, displayName, role, platform }));
      } else {
        localStorage.removeItem(LAST_ACCOUNT_KEY);
      }
    } catch {
      // Sin localStorage la app solo arranca con señal.
    }
  }

  private lastAccount(): MeResponse | null {
    try {
      const raw = localStorage.getItem(LAST_ACCOUNT_KEY);
      const me = raw ? (JSON.parse(raw) as MeResponse) : null;
      return me?.platform === 'field' && me.accountId && FIELD_ROLES.includes(me.role) ? me : null;
    } catch {
      return null;
    }
  }

  /** D-013-S1: con el access token vencido (15 min) se usa el refresh token antes de pedir login otra vez. */
  private async restoreSession(): Promise<SessionCheck> {
    const { isAuthenticated } = await firstValueFrom(this.oidc.checkAuth());
    if (isAuthenticated) return 'ok';
    if (!(await firstValueFrom(this.oidc.getRefreshToken()))) return 'none';
    // NFR-01: sin red conocida no se espera el timeout de Cognito.
    if (typeof navigator !== 'undefined' && !navigator.onLine) return 'unreachable';
    try {
      const restored = await firstValueFrom(this.oidc.checkAuthIncludingServer().pipe(timeout(REFRESH_TIMEOUT_MS)));
      return restored.isAuthenticated ? 'ok' : 'none';
    } catch {
      // Sin red o Cognito lento: no se sabe si la sesión sigue viva.
      return 'unreachable';
    }
  }

  login(returnUrl?: string | null): void {
    const target = safeReturnUrl(returnUrl);
    try {
      if (target) sessionStorage.setItem(RETURN_URL_KEY, target);
      else sessionStorage.removeItem(RETURN_URL_KEY);
    } catch {
      // Sin sessionStorage se vuelve al inicio.
    }
    this.oidc.authorize();
  }

  takeReturnUrl(): string {
    let stored: string | null = null;
    try {
      stored = sessionStorage.getItem(RETURN_URL_KEY);
      sessionStorage.removeItem(RETURN_URL_KEY);
    } catch {
      // Sin sessionStorage se vuelve al inicio.
    }
    return safeReturnUrl(stored) ?? '/home';
  }

  /** 401 con la sesión iniciada: se vuelve a Cognito sin soltar el outbox, que la misma cuenta retoma al reingresar. */
  expire(returnUrl: string): void {
    if (this.state().status !== 'authenticated') return;
    this.state.set({ status: 'anonymous', me: null });
    this.oidc.logoffLocal();
    this.login(returnUrl);
  }

  async logout(): Promise<void> {
    this.rememberAccount(null);
    this.stopWaitingForNetwork();
    this.offlineSession.set(false);
    this.outbox.setSyncHeld(false);
    await this.outbox.setOwner(null);
    this.state.set({ status: 'anonymous', me: null });
    this.oidc.logoffLocal();
    this.navigateExternal(cognitoLogoutUrl(environment.oidc.cognitoDomain, environment.oidc.clientId, environment.oidc.postLogoutRedirectUri));
  }
}
