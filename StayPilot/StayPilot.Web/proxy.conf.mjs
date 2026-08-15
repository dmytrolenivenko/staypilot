// Dev-server proxy: /api -> the local StayPilot API.
//
// It also attaches the Entra ID bearer token, so the browser never handles a
// token and nothing has to be pasted into localStorage. Credentials come from
// the same .NET user-secrets store AddProperty uses (AddProperty/TokenProvider.cs),
// so there is nothing extra to configure and nothing that can be committed.
//
// The token refreshes itself before it expires — a long session keeps working,
// no restarting `npm start` after an hour.

import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

// Must match <UserSecretsId> in AddProperty.csproj — that is what points this
// file and `dotnet user-secrets` at the same folder.
const USER_SECRETS_ID = 'a1594112-1619-422a-938d-f9cbd910513b';

// Renew early. A token that passes the check here and then expires in flight
// would fail the request for no good reason.
const RENEW_BEFORE_MS = 5 * 60 * 1000;

let token = null;
let expiresAt = 0;
let refreshing = null;

// Reads the user-secrets store — a flat JSON file kept in the user profile,
// deliberately outside the repository. A missing file is not an error; the
// environment may supply everything instead.
function readSecrets() {
  const path = join(process.env.APPDATA ?? '', 'Microsoft', 'UserSecrets', USER_SECRETS_ID, 'secrets.json');

  if (!existsSync(path)) {
    return {};
  }

  try {
    // `dotnet user-secrets` writes this file with a UTF-8 BOM. System.Text.Json
    // skips it silently, JSON.parse does not — strip it or every read fails.
    return JSON.parse(readFileSync(path, 'utf8').replace(/^﻿/, ''));
  } catch (error) {
    // Loud on purpose: a malformed store looks exactly like a missing one,
    // and that cost an afternoon once.
    console.warn(`[proxy] Could not read ${path}: ${error.message}`);

    return {};
  }
}

// Environment wins over the secret store, matching TokenProvider.cs.
function setting(secrets, name) {
  return process.env[name] || secrets[name];
}

async function fetchToken() {
  const secrets = readSecrets();
  const tenantId = setting(secrets, 'STAYPILOT_TENANT_ID');
  const clientId = setting(secrets, 'STAYPILOT_CLIENT_ID');
  const clientSecret = setting(secrets, 'STAYPILOT_CLIENT_SECRET');
  const scope = setting(secrets, 'STAYPILOT_SCOPE');

  if (!tenantId || !clientId || !clientSecret || !scope) {
    throw new Error(
      `Missing STAYPILOT_* settings. Set them with:  ` +
      `dotnet user-secrets set STAYPILOT_CLIENT_SECRET "<value>" --project AddProperty`
    );
  }

  // Client credentials: no user, no browser, no prompt — the app proves who it
  // is with its own id and secret, and Entra puts the Api.Write role in the token.
  const response = await fetch(`https://login.microsoftonline.com/${tenantId}/oauth2/v2.0/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'client_credentials',
      client_id: clientId,
      client_secret: clientSecret,
      scope,
    }),
  });

  const body = await response.json();

  if (!response.ok) {
    // Entra's error names the actual problem (wrong secret, unknown scope, no
    // consent) and never echoes the secret back, so it is safe to surface.
    throw new Error(`${response.status} ${body.error_description ?? JSON.stringify(body)}`);
  }

  token = body.access_token;
  expiresAt = Date.now() + body.expires_in * 1000;

  console.log(`[proxy] API token acquired, valid until ${new Date(expiresAt).toLocaleTimeString()}.`);
}

// Kicks off a refresh when the token is missing or close to expiry. Deliberately
// not awaited by the request path: proxyReq is synchronous, and refreshing five
// minutes early means the current token is always still good for this request.
function ensureFreshToken() {
  if (refreshing || (token && Date.now() < expiresAt - RENEW_BEFORE_MS)) {
    return;
  }

  refreshing = fetchToken()
    .catch((error) => {
      console.warn(`[proxy] Could not get an API token: ${error.message}`);
      console.warn('[proxy] Write endpoints will return 401. Reads are anonymous and still work.');
    })
    .finally(() => {
      refreshing = null;
    });
}

// Fetch once up front so the first write of the session does not race the token.
// A failure here is logged, not thrown — a broken token should not stop the app
// from serving, since everything except the three write endpoints is anonymous.
await fetchToken().catch((error) => {
  console.warn(`[proxy] Could not get an API token at startup: ${error.message}`);
  console.warn('[proxy] Write endpoints will return 401. Reads are anonymous and still work.');
});

export default {
  '/api': {
    target: 'https://localhost:7056/',
    secure: false, // the local API serves a dev certificate
    changeOrigin: true,
    configure: (proxy) => {
      proxy.on('proxyReq', (proxyReq) => {
        ensureFreshToken();

        if (token) {
          proxyReq.setHeader('Authorization', `Bearer ${token}`);
        }
      });
    },
  },
};
