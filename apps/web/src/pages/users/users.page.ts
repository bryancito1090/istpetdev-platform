import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AccountItem, HUMAN_ROLES, HumanRole } from '@istpetdev/shared-core';
import { AccountsApi, accountErrorMessage, accountStatusLabel } from '@entities/account';
import { AuthSession } from '@entities/session';
import { ButtonComponent, BadgeComponent } from '@shared/ui';

/** FR-19: Gerencia da de alta, cambia de rol y retira el acceso. Nunca borra cuentas. */
@Component({
  selector: 'page-users',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, BadgeComponent],
  template: `
    <section class="max-w-7xl mx-auto px-6 lg:px-8 py-8 space-y-8 bg-white font-sans text-slate-800">
      <!-- Encabezado Principal Estilo Stripe: Usuarios -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-slate-100">
        <div>
          <div class="flex items-center gap-3">
            <h1 class="text-2xl sm:text-3xl font-bold text-[#1A2C44] tracking-tight font-sans">
              Usuarios
            </h1>
            <span class="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-slate-100 text-slate-600 border border-slate-200/70">
              {{ accounts().length }} cuentas
            </span>
          </div>
          <p class="text-xs sm:text-sm text-[#3C4F69] mt-1">
            Gestión de identidades institucionales, roles operativos RBAC y sincronización con directorio.
          </p>
        </div>

        <div class="flex items-center gap-3">
          <button
            type="button"
            (click)="reload()"
            [disabled]="busy()"
            class="inline-flex items-center gap-1.5 text-xs font-medium px-3.5 py-1.5 rounded-md border border-[#E3E8EE] bg-white text-[#30313D] hover:bg-slate-50 hover:border-slate-300 transition-all shadow-2xs disabled:opacity-50 cursor-pointer">
            <svg
              class="w-3.5 h-3.5 text-slate-400"
              [ngClass]="{'animate-spin text-[#635BFF]': busy()}"
              xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            <span>{{ busy() ? 'Sincronizando...' : 'Actualizar Estado' }}</span>
          </button>
        </div>
      </div>

      @if (error()) {
        <div role="alert" aria-live="polite" class="p-3.5 rounded-lg border border-rose-200 bg-rose-50/90 text-sm text-rose-800 flex items-center justify-between shadow-2xs">
          <div class="flex items-center gap-2.5">
            <svg class="w-4 h-4 text-rose-600 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <span>{{ error() }}</span>
          </div>
          <button type="button" (click)="error.set(null)" class="text-rose-500 hover:text-rose-700 text-xs font-semibold cursor-pointer" aria-label="Cerrar alerta">
            ✕
          </button>
        </div>
      }

      <!-- Tarjeta Formulario: Dar de alta / invitar usuario (Estándar Stripe) -->
      <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 sm:p-6 shadow-2xs">
        <div class="flex items-center justify-between pb-3.5 mb-5 border-b border-[#F1F3F5]">
          <div class="flex items-center gap-2">
            <h2 class="text-sm font-semibold text-[#1A2C44] tracking-tight">Invitar o dar de alta usuario</h2>
            <span class="text-[12px] text-[#8792A2] font-normal" title="Información">&#9432;</span>
          </div>
          <span class="text-[11px] px-2 py-0.5 rounded-full font-medium bg-slate-50 text-slate-600 border border-slate-200">
            Control RBAC
          </span>
        </div>

        <form class="grid gap-4 sm:grid-cols-12 items-end" (submit)="$event.preventDefault(); create()">
          <div class="sm:col-span-4">
            <label for="user-email" class="block text-[13px] font-medium text-[#30313D] mb-1.5">Correo electrónico</label>
            <input
              id="user-email"
              class="w-full h-9 px-3 text-[13px] text-[#1A2C44] bg-white border border-[#E3E8EE] rounded-md shadow-2xs placeholder:text-[#8792A2] focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] focus:outline-none transition-all"
              type="email"
              name="email"
              placeholder="ejemplo@istpet.edu.ec"
              [(ngModel)]="draft.email"
              required />
          </div>
          <div class="sm:col-span-4">
            <label for="user-display-name" class="block text-[13px] font-medium text-[#30313D] mb-1.5">Nombre y apellido</label>
            <input
              id="user-display-name"
              class="w-full h-9 px-3 text-[13px] text-[#1A2C44] bg-white border border-[#E3E8EE] rounded-md shadow-2xs placeholder:text-[#8792A2] focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] focus:outline-none transition-all"
              name="displayName"
              placeholder="Nombre de la persona"
              [(ngModel)]="draft.displayName"
              required />
          </div>
          <div class="sm:col-span-2">
            <label for="user-role" class="block text-[13px] font-medium text-[#30313D] mb-1.5">Rol operativo</label>
            <select
              id="user-role"
              class="w-full h-9 px-3 text-[13px] text-[#1A2C44] bg-white border border-[#E3E8EE] rounded-md shadow-2xs focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] focus:outline-none transition-all cursor-pointer"
              name="role"
              [(ngModel)]="draft.role">
              @for (role of roles; track role) {
                <option [value]="role">{{ role }}</option>
              }
            </select>
          </div>
          <div class="sm:col-span-2">
            <ui-button type="submit" variant="primary" [loading]="busy()" [fullWidth]="true">
              Crear e invitar
            </ui-button>
          </div>
        </form>
      </div>

      <!-- Tabla de Usuarios Estilo Stripe Dashboard (Patrón 3) -->
      <div class="bg-white border border-[#E3E8EE] rounded-lg overflow-hidden shadow-2xs">
        <div class="px-5 py-3.5 border-b border-[#E3E8EE] bg-white flex items-center justify-between">
          <div class="flex items-center gap-3">
            <h3 class="text-sm font-semibold text-[#1A2C44]">Cuentas en el sistema</h3>
            <span class="text-xs text-[#3C4F69]">&bull; {{ activeAccountsCount() }} activas</span>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse text-[13px]">
            <thead class="bg-[#F8FAFC] border-b border-[#E3E8EE] text-[12px] font-medium uppercase tracking-wider text-[#697386]">
              <tr>
                <th scope="col" class="py-3 px-4 font-medium text-[#697386]">Nombre</th>
                <th scope="col" class="py-3 px-4 font-medium text-[#697386]">Correo</th>
                <th scope="col" class="py-3 px-4 font-medium text-[#697386]">Rol</th>
                <th scope="col" class="py-3 px-4 font-medium text-[#697386]">Estado</th>
                <th scope="col" class="py-3 px-4 font-medium text-[#697386]">Directorio</th>
                <th scope="col" class="py-3 px-4 text-right font-medium text-[#697386]">Acciones</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-[#F1F3F5]">
              @for (account of accounts(); track account.id) {
                <tr class="hover:bg-[#F8FAFC]/80 transition-colors" [attr.data-account]="account.id">
                  <td class="py-3.5 px-4 font-semibold text-[#1A2C44]">
                    <div class="flex items-center gap-2.5">
                      <div class="w-7 h-7 rounded-full bg-slate-100 text-[#1A2C44] font-semibold text-xs flex items-center justify-center shrink-0 border border-slate-200/60 font-sans">
                        {{ account.displayName ? account.displayName.charAt(0).toUpperCase() : 'U' }}
                      </div>
                      <div class="flex flex-col">
                        <span class="text-[#1A2C44] font-semibold leading-tight">{{ account.displayName }}</span>
                        @if (isSelf(account)) {
                          <span class="text-[11px] text-[#635BFF] font-medium leading-tight">Tu cuenta</span>
                        }
                      </div>
                    </div>
                  </td>
                  <td class="py-3.5 px-4 text-[#3C4F69]">{{ account.email ?? '—' }}</td>
                  <td class="py-3.5 px-4">
                    @if (isSelf(account)) {
                      <span class="inline-flex items-center px-2 py-0.5 rounded text-[12px] font-medium bg-slate-100 text-slate-800 border border-slate-200">{{ account.role }}</span>
                    } @else {
                      <select
                        class="h-8 px-2 text-[12px] rounded-md border border-[#E3E8EE] bg-white text-[#30313D] focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF] focus:outline-none transition-all cursor-pointer shadow-2xs"
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
                  <td class="py-3.5 px-4">
                    <div class="flex flex-col gap-1">
                      <div>
                        <span
                          class="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium border"
                          [ngClass]="{
                            'bg-emerald-50 text-emerald-700 border-emerald-200/60': account.isActive && account.invitationStatus === 'active',
                            'bg-amber-50 text-amber-700 border-amber-200/60': account.isActive && account.invitationStatus === 'pending',
                            'bg-slate-100 text-slate-600 border-slate-200/70': !account.isActive,
                            'bg-rose-50 text-rose-700 border-rose-200/60': account.isActive && account.invitationStatus === 'missing'
                          }">
                          {{ statusLabel(account) }}
                        </span>
                      </div>
                      @if (account.outOfSync) {
                        <div class="flex flex-col gap-0.5 mt-0.5">
                          <ui-badge variant="warning" label="Revisar en Cognito"></ui-badge>
                          <span class="text-[11px] text-amber-700 font-medium">Desfase con Cognito: repite la última acción</span>
                        </div>
                      }
                    </div>
                  </td>
                  <td class="py-3.5 px-4">
                    <span
                      class="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium border"
                      [ngClass]="{
                        'bg-emerald-50 text-emerald-700 border-emerald-200/60': account.directoryEnabled === true,
                        'bg-slate-100 text-slate-700 border-slate-200/70': account.directoryEnabled === false,
                        'bg-amber-50 text-amber-700 border-amber-200/60': account.directoryEnabled === null
                      }">
                      {{ directoryStatusLabel(account.directoryEnabled) }}
                    </span>
                  </td>
                  <td class="py-3.5 px-4 text-right space-x-2 whitespace-nowrap">
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
              } @empty {
                <tr>
                  <td colspan="6" class="py-12 text-center text-[#8792A2]">
                    <div class="flex flex-col items-center justify-center space-y-2">
                      <svg class="w-8 h-8 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                      </svg>
                      <p class="text-sm font-medium text-[#30313D]">No hay usuarios registrados</p>
                      <p class="text-xs text-[#3C4F69]">Utiliza el formulario superior para crear la primera cuenta.</p>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
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
  readonly activeAccountsCount = computed(() => this.accounts().filter((a) => a.isActive).length);
  draft: { email: string; displayName: string; role: HumanRole } = { email: '', displayName: '', role: 'Conductor' };

  ngOnInit(): void {
    void this.run(async () => undefined);
  }

  reload(): void {
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
