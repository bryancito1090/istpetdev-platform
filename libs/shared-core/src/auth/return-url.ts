/** Destino tras volver de Cognito: solo rutas internas, nunca otro sitio ni las rutas del propio login. */
export function safeReturnUrl(value: string | null | undefined): string | null {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) return null;
  if (value.startsWith('/login') || value.startsWith('/auth/')) return null;
  return value;
}
