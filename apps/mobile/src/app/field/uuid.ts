/**
 * Generador simple de UUID v7 estándar (RFC 9562) en cliente sin librerías externas.
 * Monótono dentro del mismo milisegundo (RFC 9562 §6.2, método 1): los 12 bits rand_a llevan un contador,
 * así dos comandos capturados en el mismo milisegundo conservan su orden en la cola sin conexión.
 */
let lastMs = 0;
let sequence = 0;

export function generateUuidV7(): string {
  let now = Date.now();
  if (now > lastMs) {
    lastMs = now;
    sequence = Math.floor(Math.random() * 0x400); // arranque aleatorio con margen para incrementar
  } else {
    // Mismo milisegundo o reloj que retrocede: se sigue sobre el último instante usado.
    now = lastMs;
    sequence++;
    if (sequence > 0xfff) {
      lastMs = ++now;
      sequence = 0;
    }
  }

  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);

  // Timestamp de 48 bits
  bytes[0] = (now / 0x10000000000) & 0xff;
  bytes[1] = (now / 0x100000000) & 0xff;
  bytes[2] = (now / 0x1000000) & 0xff;
  bytes[3] = (now / 0x10000) & 0xff;
  bytes[4] = (now / 0x100) & 0xff;
  bytes[5] = now & 0xff;

  // Versión 7 (0b0111) en bits 48..51 y contador de 12 bits en rand_a
  bytes[6] = 0x70 | ((sequence >> 8) & 0x0f);
  bytes[7] = sequence & 0xff;
  // Variante RFC (0b10) en bits 64..65
  bytes[8] = (bytes[8] & 0x3f) | 0x80;

  const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}
