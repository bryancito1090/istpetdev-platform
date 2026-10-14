import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AccountItem, HUMAN_ROLES, HumanRole } from '@istpetdev/shared-core';
import { AccountsApi, accountErrorMessage, accountStatusLabel } from '@entities/account';
import { AuthSession } from '@entities/session';
import { ButtonComponent, CardComponent, BadgeComponent } from '@shared/ui';

/** FR-19: Gerencia da de alta, cambia de rol y retira el acceso. Nunca borra cuentas. */
@Component({
  selector: 'page-users',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, CardComponent, BadgeComponent],
  template: `
    <section class="max-w-5xl mx-auto py-6 space-y-6">
      <h1 class="text-2xl font-extrabold text-slate-900">Usuarios</h1>

      @if (error()) {
        <p role="alert" aria-live="polite" class="p-3 rounded-lg border border-red-200 bg-red-50 text-sm text-red-800">
          {{ error() }}
        </p>
      }

      <ui-card [padding]="'lg'">
        <form class="grid gap-3 sm:grid-cols-4 items-end" (submit)="$event.preventDefault(); create()">
          <div>
            <label for="user-email" class="block text-xs font-semibold text-slate-700">Correo</label>
            <input
              id="user-email"
              class="mt-1 w-full rounded border border-slate-300 px-2 py-1.5 text-sm focus:border-primary-500 focus:outline-none"
              type="email"
              name="email"
              [(ngModel)]="draft.email"
              required />
          </div>
          <div>
            <label for="user-display-name" class="block text-xs font-semibold text-slate-700">Nombre</label>
            <input
              id="user-display-name"
              class="mt-1 w-full rounded border border-slate-300 px-2 py-1.5 text-sm focus:border-primary-500 focus:outline-none"
              name="displayName"
              [(ngModel)]="draft.displayName"
              required />
          </div>
          <div>
            <label for="user-role" class="block text-xs font-semibold text-slate-700">Rol</label>
            <select
              id="user-role"
              class="mt-1 w-full rounded border border-slate-300 px-2 py-1.5 text-sm focus:border-primary-500 focus:outline-none bg-white"
              name="role"
              [(ngModel)]="draft.role">
              @for (role of roles; track role) {
                <option [value]="role">{{ role }}</option>
              }
            </select>
          </div>
          <div>
            <ui-button type="submit" variant="primary" [loading]="busy()" [fullWidth]="true">
              Crear e invitar
            </ui-button>
          </div>
        </form>
      </ui-card>

      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="text-left text-xs uppercase text-slate-500 border-b border-slate-200">
            <tr>
              <th class="py-2.5 px-3">Nombre</th>
              <th class="py-2.5 px-3">Correo</th>
              <th class="py-2.5 px-3">Rol</th>
              <th class="py-2.5 px-3">Estado</th>
              <th class="py-2.5 px-3">Directorio</th>
              <th class="py-2.5 px-3 text-right">Acciones</th>
            </tr>
          </thead>
          <tbody>
            @for (account of accounts(); track account.id) {
              <tr class="border-b border-slate-100 hover:bg-slate-50/50" [attr.data-account]="account.id">
                <td class="py-3 px-3 font-semibold text-slate-900">{{ account.displayName }}</td>
                <td class="py-3 px-3 text-slate-600">{{ account.email ?? '—' }}</td>
                <td class="py-3 px-3">
                  @if (isSelf(account)) {
                    <span class="inline-block py-1 text-sm font-medium text-slate-800">{{ account.role }}</span>
                  } @else {
                    <select
                      class="rounded border border-slate-300 px-2 py-1 text-sm bg-white"
                      [ngModel]="account.role"
                      (ngModelChange)="changeRole(account, $event)"
                      [disabled]="busy()"
                      [attr.aria-label]="'Cambiar rol de ' + account.displayName">
                      @for (role of roles; track role) {
                        <option [value]="role">{{ role }}</option>
                      }
                    </select>
                  }
                </td>
                <td class="py-3 px-3">
                  <span class="text-slate-800">{{ statusLabel(account) }}</span>
                  @if (account.outOfSync) {
                    <div class="mt-1 flex flex-col gap-0.5">
                      <ui-badge variant="warning" label="Revisar en Cognito"></ui-badge>
                      <span class="text-xs text-amber-700">Desfase con Cognito: repite la última acción</span>
                    </div>
                  }
                </td>
                <td class="py-3 px-3">
                  <span
                    class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium"
                    [ngClass]="{
                      'bg-green-100 text-green-800': account.directoryEnabled === true,
                      'bg-slate-100 text-slate-800': account.directoryEnabled === false,
                      'bg-amber-100 text-amber-800': account.directoryEnabled === null
                    }">
                    {{ directoryStatusLabel(account.directoryEnabled) }}
                  </span>
                </td>
                <td class="py-3 px-3 text-right space-x-2 whitespace-nowrap">
                  @if (account.isActive && account.invitationStatus === 'pending') {
                    <ui-button size="sm" variant="ghost" (buttonClick)="resend(account)">
                      Reenviar invitación
                    </ui-button>
                  }
                  @if (!isSelf(account)) {
                    <ui-button
                      size="sm"
                      [variant]="account.isActive ? 'destructive' : 'outline'"
                      (buttonClick)="toggle(account)">
                      {{ account.isActive ? 'Desactivar' : 'Reactivar' }}
                    </ui-button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>
  `
})
export class UsersPage implements OnInit {
  private readonly api = inject(AccountsApi);
  private readonly me = inject(AuthSession).me;

  readonly roles = HUMAN_ROLES;
  readonly accounts = signal<AccountItem[]>([]);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  readonly statusLabel = accountStatusLabel;
  draft: { email: string; displayName: string; role: HumanRole } = { email: '', displayName: '', role: 'Conductor' };

  ngOnInit(): void {
    void this.run(async () => undefined);
  }

  isSelf(account: AccountItem): boolean {
    return account.id === this.me()?.accountId;
  }

  directoryStatusLabel(enabled: boolean | null): string {
    if (enabled === true) return 'Habilitado';
    if (enabled === false) return 'Deshabilitado';
    return 'Desconocido';
  }

  async create(): Promise<void> {
    await this.run(async () => {
      await firstValueFrom(this.api.create({ ...this.draft }));
      this.draft = { email: '', displayName: '', role: 'Conductor' };
    });
  }

  async changeRole(account: AccountItem, role: HumanRole): Promise<void> {
    await this.run(() => firstValueFrom(this.api.update(account.id, { role })));
  }

  async toggle(account: AccountItem): Promise<void> {
    if (account.isActive) {
      const confirmed = window.confirm(`¿Seguro que deseas desactivar la cuenta de ${account.displayName}?`);
      if (!confirmed) {
        return;
      }
    }
    await this.run(() => firstValueFrom(account.isActive ? this.api.deactivate(account.id) : this.api.reactivate(account.id)));
  }

  async resend(account: AccountItem): Promise<void> {
    await this.run(() => firstValueFrom(this.api.resendInvitation(account.id)));
  }

  // Toda acción recarga la lista para reflejar el estado real de la API y de Cognito.
  private async run(action: () => Promise<unknown>): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      await action();
      this.accounts.set(await firstValueFrom(this.api.list()));
    } catch (error) {
      this.error.set(accountErrorMessage(error));
    } finally {
      this.busy.set(false);
    }
  }
}
