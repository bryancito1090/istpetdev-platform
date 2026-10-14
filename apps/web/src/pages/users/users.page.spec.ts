import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { AccountItem } from '@istpetdev/shared-core';
import { AccountsApi } from '@entities/account';
import { AuthSession } from '@entities/session';
import { UsersPage } from './users.page';

const rows: AccountItem[] = [
  { id: 'g', displayName: 'Gerencia', role: 'Gerencia', isActive: true, email: 'g@ejemplo.test', invitationStatus: 'active', directoryEnabled: true, outOfSync: false },
  { id: 'c', displayName: 'Conductor', role: 'Conductor', isActive: true, email: 'c@ejemplo.test', invitationStatus: 'pending', directoryEnabled: true, outOfSync: false },
  { id: 'b', displayName: 'Bodega', role: 'Bodega', isActive: false, email: 'b@ejemplo.test', invitationStatus: 'active', directoryEnabled: false, outOfSync: true },
  { id: 'u', displayName: 'Desconocido', role: 'Receptor', isActive: true, email: null, invitationStatus: 'unknown', directoryEnabled: null, outOfSync: false },
];

async function render(apiOverrides: Partial<Record<keyof AccountsApi, unknown>> = {}) {
  TestBed.configureTestingModule({
    imports: [UsersPage],
    providers: [
      {
        provide: AccountsApi,
        useValue: {
          list: vi.fn(() => of(rows)),
          create: vi.fn(() => of(rows[0])),
          update: vi.fn(() => of(rows[0])),
          deactivate: vi.fn(() => of(rows[0])),
          reactivate: vi.fn(() => of(rows[0])),
          resendInvitation: vi.fn(() => of(undefined)),
          ...apiOverrides
        }
      },
      {
        provide: AuthSession,
        useValue: {
          me: signal({ accountId: 'g', displayName: 'Gerencia', role: 'Gerencia', platform: 'web' })
        }
      },
    ],
  });
  const fixture = TestBed.createComponent(UsersPage);
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
  return fixture;
}

describe('UsersPage (FR-19)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('muestra estado, desfase y no ofrece acciones sobre la propia cuenta', async () => {
    const fixture = await render();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Invitación pendiente');
    expect(text).toContain('Activa');
    expect(text).toContain('Desactivada');
    expect(text).toContain('Desfase con Cognito');
    expect(text).toContain('Revisar en Cognito');

    const ownRow = fixture.nativeElement.querySelector('[data-account="g"]') as HTMLElement;
    expect(ownRow.textContent).not.toContain('Desactivar');
    expect(ownRow.textContent).not.toContain('Reactivar');
    expect(ownRow.querySelector('select')).toBeNull();
  });

  it('muestra el estado del directorio de forma visible', async () => {
    const fixture = await render();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Habilitado');
    expect(text).toContain('Deshabilitado');
    expect(text).toContain('Desconocido');
  });

  it('solo muestra botón "Reenviar invitación" si isActive && pending', async () => {
    const fixture = await render();

    const pendingRow = fixture.nativeElement.querySelector('[data-account="c"]') as HTMLElement;
    expect(pendingRow.textContent).toContain('Reenviar invitación');

    const activeRow = fixture.nativeElement.querySelector('[data-account="g"]') as HTMLElement;
    expect(activeRow.textContent).not.toContain('Reenviar invitación');

    const inactiveRow = fixture.nativeElement.querySelector('[data-account="b"]') as HTMLElement;
    expect(inactiveRow.textContent).not.toContain('Reenviar invitación');
  });

  it('pide confirmación antes de desactivar una cuenta', async () => {
    const deactivateMock = vi.fn(() => of(rows[1]));
    const fixture = await render({ deactivate: deactivateMock });

    // Caso 1: Usuario cancela confirmación
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);
    const conductorAccount = rows[1]; // Conductor, active
    await fixture.componentInstance.toggle(conductorAccount);
    expect(confirmSpy).toHaveBeenCalled();
    expect(deactivateMock).not.toHaveBeenCalled();

    // Caso 2: Usuario confirma
    confirmSpy.mockReturnValue(true);
    await fixture.componentInstance.toggle(conductorAccount);
    expect(deactivateMock).toHaveBeenCalledWith('c');
  });

  it('reactiva una cuenta inactiva sin pedir confirmación de desactivación', async () => {
    const reactivateMock = vi.fn(() => of(rows[2]));
    const confirmSpy = vi.spyOn(window, 'confirm');
    const fixture = await render({ reactivate: reactivateMock });

    const bodegaAccount = rows[2]; // Bodega, isActive = false
    await fixture.componentInstance.toggle(bodegaAccount);
    expect(confirmSpy).not.toHaveBeenCalled();
    expect(reactivateMock).toHaveBeenCalledWith('b');
  });

  it('muestra el motivo cuando la API rechaza el alta con role="alert" y aria-live="polite"', async () => {
    const create = vi.fn(() => throwError(() => new HttpErrorResponse({ status: 409, error: { code: 'account_exists' } })));
    const fixture = await render({ create });

    await fixture.componentInstance.create();
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement;
    expect(alert).not.toBeNull();
    expect(alert.getAttribute('aria-live')).toBe('polite');
    expect(alert.textContent).toContain('Ese correo ya tiene una cuenta.');
  });

  it('incluye labels accesibles con for/id en el formulario', async () => {
    const fixture = await render();

    const emailLabel = fixture.nativeElement.querySelector('label[for="user-email"]');
    const emailInput = fixture.nativeElement.querySelector('#user-email');
    expect(emailLabel).toBeTruthy();
    expect(emailInput).toBeTruthy();

    const nameLabel = fixture.nativeElement.querySelector('label[for="user-display-name"]');
    const nameInput = fixture.nativeElement.querySelector('#user-display-name');
    expect(nameLabel).toBeTruthy();
    expect(nameInput).toBeTruthy();

    const roleLabel = fixture.nativeElement.querySelector('label[for="user-role"]');
    const roleSelect = fixture.nativeElement.querySelector('#user-role');
    expect(roleLabel).toBeTruthy();
    expect(roleSelect).toBeTruthy();
  });
});
