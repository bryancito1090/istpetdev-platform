import { HttpErrorResponse } from '@angular/common/http';
import { AccountItem } from '@istpetdev/shared-core';

const ERROR_MESSAGES: Record<string, string> = {
  account_exists: 'Ese correo ya tiene una cuenta.',
  last_manager: 'Debe quedar al menos una Gerencia activa.',
  self_change_forbidden: 'No puedes desactivarte ni cambiar tu propio rol.',
  invalid_role: 'Elige uno de los seis roles humanos.',
  invalid_email: 'Escribe un correo válido.',
  invalid_display_name: 'Escribe el nombre de la persona.',
  technical_account: 'La cuenta técnica de automatización no se gestiona aquí.',
  invitation_not_pending: 'La persona ya completó su primer ingreso.',
  not_found: 'La cuenta ya no existe. Recarga la lista.',
  identity_unavailable: 'Cognito no responde. Intenta de nuevo en unos minutos.',
};

export function accountErrorMessage(error: unknown): string {
  const code = error instanceof HttpErrorResponse ? (error.error as { code?: string } | null)?.code : undefined;
  return (code && ERROR_MESSAGES[code]) || 'No se pudo completar la operación. Intenta de nuevo.';
}

export function accountStatusLabel(item: AccountItem): string {
  if (!item.isActive) return 'Desactivada';
  switch (item.invitationStatus) {
    case 'pending': return 'Invitación pendiente';
    case 'active': return 'Activa';
    case 'missing': return 'Sin usuario en Cognito';
    default: return 'Estado de Cognito desconocido';
  }
}
