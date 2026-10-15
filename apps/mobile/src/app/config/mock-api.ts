export function shouldMockFieldApi(isDev: boolean, search: string): boolean {
  return isDev && new URLSearchParams(search).get('mock') === 'true';
}
