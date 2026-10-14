import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '@shared/config';
import { AccountItem } from '@istpetdev/shared-core';
import { accountErrorMessage, accountStatusLabel } from '../model/account-messages';
import { AccountsApi } from './accounts.api';

const BASE = `${environment.apiUrl}/api/v1/accounts`;

describe('AccountsApi', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }));

  it('usa los métodos y rutas del contrato de 013 con environment.apiUrl', () => {
    const api = TestBed.inject(AccountsApi);
    const backend = TestBed.inject(HttpTestingController);

    api.list().subscribe();
    api.create({ email: 'a@ejemplo.test', displayName: 'A', role: 'Conductor' }).subscribe();
    api.update('id-1', { role: 'Bodega' }).subscribe();
    api.deactivate('id-1').subscribe();
    api.reactivate('id-1').subscribe();
    api.resendInvitation('id-1').subscribe();

    expect(backend.expectOne({ method: 'GET', url: BASE })).toBeTruthy();
    expect(backend.expectOne({ method: 'POST', url: BASE }).request.body).toEqual({ email: 'a@ejemplo.test', displayName: 'A', role: 'Conductor' });
    expect(backend.expectOne({ method: 'PATCH', url: `${BASE}/id-1` }).request.body).toEqual({ role: 'Bodega' });
    expect(backend.expectOne({ method: 'POST', url: `${BASE}/id-1/deactivate` })).toBeTruthy();
    expect(backend.expectOne({ method: 'POST', url: `${BASE}/id-1/reactivate` })).toBeTruthy();
    expect(backend.expectOne({ method: 'POST', url: `${BASE}/id-1/resend-invitation` })).toBeTruthy();
  });

  it('traduce los códigos de Problem Details de acuerdo a la especificación', () => {
    const codes: Record<string, string> = {
      account_exists: 'Ese correo ya tiene una cuenta.',
      last_manager: 'Debe quedar al menos una Gerencia activa.',
      self_change_forbidden: 'No puedes desactivarte ni cambiar tu propio rol.',
      invalid_role: 'Elige uno de los seis roles humanos.',
      invalid_email: 'Escribe un correo válido.',
      invalid_display_name: 'Escribe el nombre de la persona.',
      technical_account: 'La cuenta técnica de automatización no se gestiona aquí.',
      invitation_not_pending: 'La persona ya completó su primer ingreso.',
      not_found: 'La cuenta ya no existe. Recarga la lista.',
      identity_unavailable: 'Cognito no responde. Intenta de nuevo en unos minutos.'
    };

    for (const [code, message] of Object.entries(codes)) {
      expect(accountErrorMessage(new HttpErrorResponse({ status: 400, error: { code } }))).toBe(message);
    }

    expect(accountErrorMessage(new HttpErrorResponse({ status: 500, error: null }))).toBe('No se pudo completar la operación. Intenta de nuevo.');
    expect(accountErrorMessage(new HttpErrorResponse({ status: 400, error: { code: 'unknown_code' } }))).toBe('No se pudo completar la operación. Intenta de nuevo.');
    expect(accountErrorMessage(new Error('Network error'))).toBe('No se pudo completar la operación. Intenta de nuevo.');
  });

  it('traduce el estado de la cuenta según accountStatusLabel', () => {
    const base: AccountItem = {
      id: 'x',
      displayName: 'X',
      role: 'Conductor',
      isActive: true,
      email: 'x@ejemplo.test',
      invitationStatus: 'active',
      directoryEnabled: true,
      outOfSync: false
    };

    expect(accountStatusLabel({ ...base, isActive: true, invitationStatus: 'pending' })).toBe('Invitación pendiente');
    expect(accountStatusLabel({ ...base, isActive: true, invitationStatus: 'active' })).toBe('Activa');
    expect(accountStatusLabel({ ...base, isActive: true, invitationStatus: 'missing' })).toBe('Sin usuario en Cognito');
    expect(accountStatusLabel({ ...base, isActive: true, invitationStatus: 'unknown' })).toBe('Estado de Cognito desconocido');
    expect(accountStatusLabel({ ...base, isActive: false, invitationStatus: 'active' })).toBe('Desactivada');
    expect(accountStatusLabel({ ...base, isActive: false, invitationStatus: 'pending' })).toBe('Desactivada');
  });
});
