const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-7][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export function isValidUuid(id: string): boolean {
  if (!id) return false;
  return UUID_REGEX.test(id.trim());
}

export function validateIdempotencyKey(key: string): { valid: boolean; reason?: string } {
  if (!key || key.trim().length === 0) {
    return { valid: false, reason: 'La clave de idempotencia no puede estar vacía.' };
  }
  if (!isValidUuid(key)) {
    return { valid: false, reason: 'La clave de idempotencia debe ser un UUID v4 o v7 válido.' };
  }
  return { valid: true };
}
