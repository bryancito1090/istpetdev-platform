/** Roles que operan en la PWA (ADR-24). La identidad y el rol vienen de GET /api/v1/me. */
export type FieldRole = 'Bodega' | 'Conductor' | 'Receptor';

export const FIELD_ROLE_LABELS: Record<FieldRole, string> = {
  Bodega: 'Bodega',
  Conductor: 'Conductor',
  Receptor: 'Receptor',
};

export type FieldSessionStatus = 'loading' | 'anonymous' | 'authenticated' | 'wrong-platform' | 'disabled' | 'error';
