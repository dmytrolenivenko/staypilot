import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { catchError, from, switchMap, throwError } from 'rxjs';

// Attaches a bearer token to the API calls that need one (see needsToken below).
// Two sources, in priority order:
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

export const API_SCOPES = ['api://c447c11c-f8a9-4bf5-a9b1-6d176064370c/access_as_user'];

// The only GETs the API guards with [Authorize]. Every other GET is a public read, and
// every write (POST/PUT/DELETE) always gets the token. Add a path here when a new GET
// action gets [Authorize] - and if one is missed, the 401 retry below still covers it.
const PROTECTED_GET_PATHS = [
  '/api/OwnedProperty/',
  '/api/InvestmentAnalysis/AnalyzeOwnedProperty',
  // Public, but only a signed-in caller gets the AI narrative - so send the token when there is one.
  '/api/InvestmentAnalysis/Analyze/',
  '/api/MarketArea/GetRecalculationStatus'
];

// A public read goes out bare. With no Authorization header a GET is a "simple" request, so
// the browser skips the CORS preflight, and we skip the MSAL token lookup - two of the three
// round trips every dropdown, chart and table used to pay before its data even left.
function needsToken(req: HttpRequest<unknown>): boolean {
  return req.method !== 'GET' || PROTECTED_GET_PATHS.some(path => req.url.includes(path));
}

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

  const withToken = () =>
    from(msal.instance.acquireTokenSilent({ scopes: API_SCOPES, account })).pipe(
      switchMap(result => next(req.clone({ setHeaders: { Authorization: `Bearer ${result.accessToken}` } }))),
      // Silent renewal can fail (session expired, etc.) - fall back to no token
      // rather than breaking the request. The API's 401 is the real signal that
      // an interactive re-login is needed.
      catchError(() => next(req))
    );

  if (needsToken(req)) {
    return withToken();
  }

  // Safety net: a GET that turned out to be guarded is asked once more, with the token.
  return next(req).pipe(
    catchError(error =>
      error instanceof HttpErrorResponse && error.status === 401 ? withToken() : throwError(() => error)
    )
  );
};
