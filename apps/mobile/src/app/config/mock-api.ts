export function shouldMockFieldApi(isDev: boolean, search: string): boolean {
  return isDev && search.includes('mock=true');
}
