// Fuente: docs/deployment-status.md (8-oct); confirmar con terraform output -json cognito
// La API se sirve en el mismo origen detrás de Nginx y la PWA Móvil se entrega bajo /campo/.
export const environment = {
  production: true,
  apiUrl: '',
  mobileAppUrl: '/campo/',
  appName: 'IstpetDev Web',
  version: '1.0.0',
  oidc: {
    authority: 'https://cognito-idp.us-east-1.amazonaws.com/us-east-1_4Y2j5DINp',
    cognitoDomain: 'https://istpetdev-dev-747456040427.auth.us-east-1.amazoncognito.com',
    clientId: '6ejhdau47gad74id7id1l7ugln',
    redirectUrl: 'https://istpetdev.doicela.dev/auth/callback',
    postLogoutRedirectUri: 'https://istpetdev.doicela.dev',
  },
};
