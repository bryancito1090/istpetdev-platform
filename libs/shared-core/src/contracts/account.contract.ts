/** Roles humanos fijos (ADR-14, ADR-26). `Automatizacion` es técnico y no aparece en las apps. */
export type HumanRole = 'Gerencia' | 'Planificador' | 'Auditor' | 'Bodega' | 'Conductor' | 'Receptor';
export type Platform = 'web' | 'field';

export const WEB_ROLES: readonly HumanRole[] = ['Gerencia', 'Planificador', 'Auditor'];
export const FIELD_ROLES: readonly HumanRole[] = ['Bodega', 'Conductor', 'Receptor'];
export const HUMAN_ROLES: readonly HumanRole[] = [...WEB_ROLES, ...FIELD_ROLES];

/** Respuesta de GET /api/v1/me (FR-05). */
export interface MeResponse {
  accountId: string;
  displayName: string;
  role: HumanRole;
  platform: Platform;
}

export type InvitationStatus = 'pending' | 'active' | 'missing' | 'unknown' | 'other';

/** Fila de GET /api/v1/accounts (FR-12). */
export interface AccountItem {
  id: string;
  displayName: string;
  role: HumanRole;
  isActive: boolean;
  email: string | null;
  invitationStatus: InvitationStatus;
  directoryEnabled: boolean | null;
  outOfSync: boolean;
}

export interface CreateAccountRequest {
  email: string;
  displayName: string;
  role: HumanRole;
}

export interface UpdateAccountRequest {
  displayName?: string;
  role?: HumanRole;
}
