import { HttpInterceptorFn } from '@angular/common/http';

// Attaches the Entra ID bearer token to every API call. The write endpoints
// (CreateListingSnapshot, ReCalculatePremiumFeaturesValue) carry
// [Authorize(Roles = "Api.Write")] and return 401 without it.
//
// The token is read from localStorage, put there by hand from devtools:
//   localStorage.setItem('staypilot_token', '<token>')
// Get one the same way AddProperty does — client credentials against Entra,
// see AddProperty/TokenProvider.cs. They last about an hour.
//
// Deliberately NOT in environment.ts: that file is committed, and a token in
// git history is a live credential in git history.
//
// When this moves to a real sign-in, only the getItem line changes — MSAL's
// acquireTokenSilent goes here and the rest stays as it is.
export const TOKEN_KEY = 'staypilot_token';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem(TOKEN_KEY);

  // No token is normal — every GET endpoint is anonymous, so unauthenticated
  // browsing works. Only the writes need it.
  if (!token) {
    return next(req);
  }

  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
