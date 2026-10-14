import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';
import { MeResponse, cognitoLogoutUrl, safeReturnUrl } from '@istpetdev/shared-core';
import { environment } from '../config/environment';
import { FieldRole, FieldSessionStatus } from './auth.models';
import { NAVIGATE_EXTERNAL } from './navigate-external';

const RETURN_URL_KEY = 'istpetdev.returnUrl';
const FIELD_ROLES: readonly string[] = ['Bodega', 'Conductor', 'Receptor'];

/** Sesión de la PWA: identidad de Cognito + cuenta de GET /api/v1/me (FR-06). Tokens solo en memoria (FR-03). */
@Injectable({ providedIn: 'root' })
export class MobileAuthService {
  private readonly oidc = inject(OidcSecurityService);
  private readonly http = inject(HttpClient);
  private readonly navigateExternal = inject(NAVIGATE_EXTERNAL);
  private readonly state = signal<{ status: FieldSessionStatus; me: MeResponse | null }>({ status: 'loading', me: null });

  readonly status = computed(() => this.state().status);
  readonly me = computed(() => this.state().me);
  readonly currentRole = computed<FieldRole | null>(() => {
    const role = this.state().status === 'authenticated' ? this.state().me?.role : undefined;
    return role && FIELD_ROLES.includes(role) ? (role as FieldRole) : null;
  });

  async bootstrap(): Promise<void> {
    const { isAuthenticated } = await firstValueFrom(this.oidc.checkAuth());
    if (!isAuthenticated) {
      this.state.set({ status: 'anonymous', me: null });
      return;
    }
    try {
      const me = await firstValueFrom(this.http.get<MeResponse>(`${environment.apiUrl}/api/v1/me`));
      this.state.set({ status: me.platform === 'field' ? 'authenticated' : 'wrong-platform', me });
    } catch (error) {
      const disabled = error instanceof HttpErrorResponse && error.status === 403;
      this.state.set({ status: disabled ? 'disabled' : 'error', me: null });
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

  async logout(): Promise<void> {
    this.state.set({ status: 'anonymous', me: null });
    this.oidc.logoffLocal();
    this.navigateExternal(cognitoLogoutUrl(environment.oidc.cognitoDomain, environment.oidc.clientId, environment.oidc.postLogoutRedirectUri));
  }
}
