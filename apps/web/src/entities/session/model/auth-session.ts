import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom, timeout } from 'rxjs';
import { HumanRole, MeResponse, cognitoLogoutUrl, safeReturnUrl } from '@istpetdev/shared-core';
import { environment } from '@shared/config';
import { NAVIGATE_EXTERNAL } from '../lib/navigate-external';

export type SessionStatus = 'loading' | 'anonymous' | 'authenticated' | 'wrong-platform' | 'disabled' | 'error';

const RETURN_URL_KEY = 'istpetdev.returnUrl';
const REFRESH_TIMEOUT_MS = 10_000;

/** Sesión de web: identidad OIDC + cuenta de GET /api/v1/me. */
@Injectable({ providedIn: 'root' })
export class AuthSession {
  private readonly oidc = inject(OidcSecurityService);
  private readonly http = inject(HttpClient);
  private readonly navigateExternal = inject(NAVIGATE_EXTERNAL);
  private readonly state = signal<{ status: SessionStatus; me: MeResponse | null }>({ status: 'loading', me: null });

  readonly status = computed(() => this.state().status);
  readonly me = computed(() => this.state().me);
  readonly role = computed<HumanRole | null>(() => this.state().me?.role ?? null);
  readonly canWrite = computed(() => this.role() === 'Gerencia' || this.role() === 'Planificador');

  async bootstrap(): Promise<void> {
    if (!(await this.restoreSession())) {
      this.state.set({ status: 'anonymous', me: null });
      return;
    }

    try {
      const me = await firstValueFrom(this.http.get<MeResponse>(`${environment.apiUrl}/api/v1/me`));
      this.state.set({ status: me.platform === 'web' ? 'authenticated' : 'wrong-platform', me });
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
      if (target) localStorage.setItem(RETURN_URL_KEY, target);
      else localStorage.removeItem(RETURN_URL_KEY);
    } catch {
      // Sin localStorage se vuelve al panel.
    }
    this.oidc.authorize();
  }

  takeReturnUrl(): string {
    let stored: string | null = null;
    try {
      stored = localStorage.getItem(RETURN_URL_KEY);
      localStorage.removeItem(RETURN_URL_KEY);
    } catch {
      // Sin localStorage se vuelve al panel.
    }
    return safeReturnUrl(stored) ?? '/dashboard';
  }

  /** 401 con la sesión iniciada: se descarta la local y se vuelve a Cognito, que reingresa sin contraseña si su sesión sigue viva. */
  expire(returnUrl: string): void {
    if (this.state().status !== 'authenticated') return;
    this.state.set({ status: 'anonymous', me: null });
    this.oidc.logoffLocal();
    this.login(returnUrl);
  }

  logout(): void {
    this.state.set({ status: 'anonymous', me: null });
    this.oidc.logoffLocal();
    this.navigateExternal(cognitoLogoutUrl(
      environment.oidc.cognitoDomain,
      environment.oidc.clientId,
      environment.oidc.postLogoutRedirectUri,
    ));
  }
}
