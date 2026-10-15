/**
 * Parser de códigos QR y códigos de unidades de manipulación (HU) para la PWA de campo.
 * Formato canónico emitido por el backend:
 * https://istpetdev.doicela.dev/public-trace/{unitId}#t={token}
 */

export interface ParsedUnitQr {
  unitId: string;
  token: string;
}

const QR_URL_REGEX = /public-trace\/([0-9a-f-]{36})#t=([0-9a-z_-]+)/i;
const TRACE_PATH_REGEX = /public-trace\/([0-9a-f-]{36})/i;
const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * Decodifica una URL de QR de custodia con su token efímero de acceso.
 * ⚠️ Por seguridad (Spec 004 / Issue #122), el token retornado no debe
 * persistirse en outbox, logs ni localStorage.
 */
export function parseUnitQr(raw: string): ParsedUnitQr | null {
  if (!raw) return null;
  const m = raw.trim().match(QR_URL_REGEX);
  return m ? { unitId: m[1].toLowerCase(), token: m[2] } : null;
}

/**
 * Extrae el identificador de unidad (UUID) a partir de una URL completa de QR
 * o directamente desde un UUID ingresado manualmente.
 */
export function extractUnitId(raw: string): string | null {
  if (!raw) return null;
  const trimmed = raw.trim();
  const traceMatch = trimmed.match(TRACE_PATH_REGEX);
  if (traceMatch) {
    return traceMatch[1].toLowerCase();
  }
  if (UUID_REGEX.test(trimmed)) {
    return trimmed.toLowerCase();
  }
  return null;
}
