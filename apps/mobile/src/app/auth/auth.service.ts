import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
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

/** Sesión de la PWA: identidad de Cognito + cuenta de GET /api/v1/me (FR-06). Sesión en localStorage (D-013-S1). */
@Injectable({ providedIn: 'root' })
export class MobileAuthService {
  private readonly oidc = inject(OidcSecurityService);
  private readonly http = inject(HttpClient);
  private readonly navigateExternal = inject(NAVIGATE_EXTERNAL);
  private readonly outbox = inject(FieldOutboxService);
  private readonly state = signal<{ status: FieldSessionStatus; me: MeResponse | null }>({ status: 'loading', me: null });

  readonly status = computed(() => this.state().status);
  readonly me = computed(() => this.state().me);
  readonly currentRole = computed<FieldRole | null>(() => {
    const role = this.state().status === 'authenticated' ? this.state().me?.role : undefined;
    return role && FIELD_ROLES.includes(role) ? (role as FieldRole) : null;
  });

  async bootstrap(): Promise<void> {
    if (!(await this.restoreSession())) {
      this.state.set({ status: 'anonymous', me: null });
      return;
    }
    try {
      const me = await firstValueFrom(this.http.get<MeResponse>(`${environment.apiUrl}/api/v1/me`));
      this.state.set({ status: me.platform === 'field' ? 'authenticated' : 'wrong-platform', me });
      await this.outbox.setOwner(me.platform === 'field' ? me.accountId : null);
    } catch (error) {
      const disabled = error instanceof HttpErrorResponse && error.status === 403;
      this.state.set({ status: disabled ? 'disabled' : 'error', me: null });
    }
  }

  /** D-013-S1: con el access token vencido (15 min) se usa el refresh token antes de pedir login otra vez. */
  private async restoreSession(): Promise<boolean> {
    const { isAuthenticated } = await firstValueFrom(this.oidc.checkAuth());
    if (isAuthenticated) return true;
    if (!(await firstValueFrom(this.oidc.getRefreshToken()))) return false;
    try {
      const restored = await firstValueFrom(this.oidc.checkAuthIncludingServer().pipe(timeout(REFRESH_TIMEOUT_MS)));
      return restored.isAuthenticated;
    } catch {
      // Sin red o Cognito lento: se pide login en vez de bloquear el arranque.
      return false;
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
    await this.outbox.setOwner(null);
    this.state.set({ status: 'anonymous', me: null });
    this.oidc.logoffLocal();
    this.navigateExternal(cognitoLogoutUrl(environment.oidc.cognitoDomain, environment.oidc.clientId, environment.oidc.postLogoutRedirectUri));
  }
}
