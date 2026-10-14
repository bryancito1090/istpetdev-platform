/** El descubrimiento de Cognito no publica end_session_endpoint: el cierre se arma a mano. */
export function cognitoLogoutUrl(cognitoDomain: string, clientId: string, logoutUri: string): string {
  const url = new URL('/logout', cognitoDomain);
  url.searchParams.set('client_id', clientId);
  url.searchParams.set('logout_uri', logoutUri);
  return url.toString();
}
