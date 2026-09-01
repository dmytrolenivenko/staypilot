import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { catchError, from, switchMap } from 'rxjs';

// Attaches a bearer token to every API call. Two sources, in priority order:
//
// 1. A manual override in localStorage, set by hand from devtools:
//      localStorage.setItem('staypilot_token', '<token>')
//    Get one the same way AddProperty does — client credentials against Entra,
//    see AddProperty/TokenProvider.cs. Needed for the endpoints that require
//    the Api.Write role (CreateListingSnapshot, ReCalculatePremiumFeaturesValue,
//    RevalueOwnedProperty*) — no signed-in tenant account ever carries that
//    role, by design. This override exists purely to test those by hand.
//
// 2. MSAL's silent token acquisition, for whoever is actually signed in
//    through Entra External ID. Covers everything gated by plain [Authorize]
//    (OwnedPropertyController's CRUD/read actions).
//
// Deliberately NOT in environment.ts: that file is committed, and a token in
// git history is a live credential in git history.
export const TOKEN_KEY = 'staypilot_token';

const SCOPES = ['api://c447c11c-f8a9-4bf5-a9b1-6d176064370c/access_as_user'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const overrideToken = localStorage.getItem(TOKEN_KEY);

  if (overrideToken) {
    return next(req.clone({ setHeaders: { Authorization: `Bearer ${overrideToken}` } }));
  }

  const msal = inject(MsalService);
  const account = msal.instance.getActiveAccount() ?? msal.instance.getAllAccounts()[0];

  // No signed-in account is normal — most of the app is anonymous browsing.
  if (!account) {
    return next(req);
  }

  return from(msal.instance.acquireTokenSilent({ scopes: SCOPES, account })).pipe(
    switchMap(result => next(req.clone({ setHeaders: { Authorization: `Bearer ${result.accessToken}` } }))),
    // Silent renewal can fail (session expired, etc.) - fall back to no token
    // rather than breaking the request. The API's 401 is the real signal that
    // an interactive re-login is needed.
    catchError(() => next(req))
  );
};
