export const REJECTION_REASONS_VERSION = '2026-10-15';

export interface RejectionReasonItem {
  code: string;
  label: string;
}

export interface RejectionReasonsResponse {
  version: string;
  items: RejectionReasonItem[];
}

export const REJECTION_REASONS: readonly RejectionReasonItem[] = [
  { code: 'faltante', label: 'Faltante' },
  { code: 'sobrante', label: 'Sobrante no solicitado' },
  { code: 'dano', label: 'Producto dañado' },
  { code: 'precinto_violado', label: 'Precinto violado o alterado' },
  { code: 'lote_no_coincide', label: 'Lote o vencimiento no coincide' },
  { code: 'cadena_frio', label: 'Cadena de frío rota' }
] as const;

export type RejectionReasonCode = (typeof REJECTION_REASONS)[number]['code'];

export function isRejectionReasonCode(code: string | null | undefined): code is RejectionReasonCode {
  if (!code) return false;
  return REJECTION_REASONS.some((r) => r.code === code);
}
