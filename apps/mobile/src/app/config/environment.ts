// Fuente: docs/deployment-status.md (8-oct); confirmar con terraform output -json cognito
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',
  oidc: {
    authority: 'https://cognito-idp.us-east-1.amazonaws.com/us-east-1_4Y2j5DINp',
    cognitoDomain: 'https://istpetdev-dev-747456040427.auth.us-east-1.amazoncognito.com',
    clientId: '3k59rv5cov1sinllgd7igmpe1a',
    redirectUrl: 'http://localhost:8100/auth/callback',
    postLogoutRedirectUri: 'http://localhost:8100',
  },
};
