import { describe, expect, it } from 'vitest';
import { cognitoLogoutUrl, isPrivateApiRequest, safeReturnUrl } from './index';

describe('isPrivateApiRequest (FR-04)', () => {
  it.each([
    ['http://localhost:5000/api/v1/me', 'http://localhost:5000', true],
    ['http://localhost:5000/api/v1/route-plans?limit=5', 'http://localhost:5000/', true],
    ['/api/v1/me', '', true],
    ['http://localhost:5000/api/v1/public/trace/abc', 'http://localhost:5000', false],
    ['/api/v1/public/trace/abc', '', false],
    ['http://localhost:5000.evil.example/api/v1/me', 'http://localhost:5000', false],
    ['https://tiles.example.org/api/1/2.png', '', false],
    ['http://localhost:5000/health/ready', 'http://localhost:5000', false],
  ])('%s con apiUrl «%s» → %s', (url, apiUrl, expected) => {
    expect(isPrivateApiRequest(url, apiUrl)).toBe(expected);
  });
});

describe('safeReturnUrl', () => {
  it.each([
    ['/dashboard', '/dashboard'],
    ['/users?tab=2', '/users?tab=2'],
    ['//evil.example', null],
    ['/\\evil.example', null],
    ['https://evil.example/dashboard', null],
    ['javascript:alert(1)', null],
    ['/login?returnUrl=/dashboard', null],
    ['/auth/callback?code=x', null],
    ['', null],
    [null, null],
  ])('%s → %s', (value, expected) => {
    expect(safeReturnUrl(value)).toBe(expected);
  });
});

describe('cognitoLogoutUrl', () => {
  it('arma el cierre de la página administrada con el logout_uri exacto', () => {
    expect(cognitoLogoutUrl('https://istpetdev-dev-123.auth.us-east-1.amazoncognito.com', 'abc123', 'http://localhost:4200'))
      .toBe('https://istpetdev-dev-123.auth.us-east-1.amazoncognito.com/logout?client_id=abc123&logout_uri=http%3A%2F%2Flocalhost%3A4200');
  });
});
