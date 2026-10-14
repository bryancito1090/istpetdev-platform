// Fuente: docs/deployment-status.md (8-oct); confirmar con terraform output -json cognito
// La PWA se publica en /campo/ del dominio de la demo; la API en el mismo origen detrás de Nginx (/api/).
export const environment = {
  production: true,
  apiUrl: '',
  oidc: {
    authority: 'https://cognito-idp.us-east-1.amazonaws.com/us-east-1_4Y2j5DINp',
    cognitoDomain: 'https://istpetdev-dev-747456040427.auth.us-east-1.amazoncognito.com',
    clientId: '3k59rv5cov1sinllgd7igmpe1a',
    redirectUrl: 'https://istpetdev.doicela.dev/campo/auth/callback',
    postLogoutRedirectUri: 'https://istpetdev.doicela.dev/campo/',
  },
};
