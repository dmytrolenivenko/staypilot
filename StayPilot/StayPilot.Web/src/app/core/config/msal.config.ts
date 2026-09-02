import { PublicClientApplication, LogLevel } from '@azure/msal-browser';

export const msalInstance = new PublicClientApplication({
  auth: {
    clientId: 'c447c11c-f8a9-4bf5-a9b1-6d176064370c',
    authority: 'https://staypilot.ciamlogin.com/8d7cb648-496f-43f3-900d-336292e7cb9b',
    knownAuthorities: ['staypilot.ciamlogin.com'],
    redirectUri: '/'
  },
  cache: {
    cacheLocation: 'localStorage'
  }
});
