/** FR-04: el token solo viaja a la API configurada y nunca a la traza pública. */
export function isPrivateApiRequest(url: string, apiUrl: string): boolean {
  const base = apiUrl.replace(/\/+$/, '');
  if (!url.startsWith(`${base}/api/`)) return false;
  return !url.startsWith(`${base}/api/v1/public/`);
}
