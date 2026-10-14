export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',
  appName: 'IstpetDev Web',
  version: '1.0.0',
  // Fuente: docs/deployment-status.md (8-oct); confirmar con terraform output -json cognito
  oidc: {
    authority: 'https://cognito-idp.us-east-1.amazonaws.com/us-east-1_4Y2j5DINp',
    cognitoDomain: 'https://istpetdev-dev-747456040427.auth.us-east-1.amazoncognito.com',
    clientId: '6ejhdau47gad74id7id1l7ugln',
    redirectUrl: 'http://localhost:4200/auth/callback',
    postLogoutRedirectUri: 'http://localhost:4200',
  },
};
