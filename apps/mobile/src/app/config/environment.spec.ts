import { describe, expect, it } from 'vitest';
import { environment } from './environment';
import { environment as production } from './environment.production';

const ISSUER = /^https:\/\/cognito-idp\.[a-z0-9-]+\.amazonaws\.com\/[a-z0-9-]+_[A-Za-z0-9]+$/;

describe('configuración OIDC pública de la PWA (FR-02)', () => {
  it.each([
    ['desarrollo', environment, 'http://localhost:8100/auth/callback', 'http://localhost:8100'],
    ['producción', production, 'https://istpetdev.doicela.dev/campo/auth/callback', 'https://istpetdev.doicela.dev/campo/'],
  ])('%s usa los valores de Terraform y las URL exactas', (_name, env, redirect, logout) => {
    expect(env.oidc.authority).toMatch(ISSUER);
    expect(env.oidc.cognitoDomain).toMatch(/^https:\/\/[a-z0-9-]+\.auth\.[a-z0-9-]+\.amazoncognito\.com$/);
    expect(env.oidc.clientId).toMatch(/^[a-z0-9]{20,32}$/);
    expect(env.oidc.redirectUrl).toBe(redirect);
    expect(env.oidc.postLogoutRedirectUri).toBe(logout);
    expect(JSON.stringify(env)).not.toMatch(/secret|password|istpetdev\.local|«/i);
  });
});
