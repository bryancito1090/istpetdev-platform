/** Acepta la URL del QR (`…/public-trace/{unitId}#t={token}`) y devuelve unidad y token. */
export function parseUnitQr(raw: string | null | undefined): { unitId: string; token: string } | null {
  if (!raw) return null;
  const match = raw.trim().match(/public-trace\/([0-9a-f-]{36})#t=([0-9a-f]+)/i);
  return match ? { unitId: match[1], token: match[2] } : null;
}
