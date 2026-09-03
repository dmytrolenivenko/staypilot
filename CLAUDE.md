# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

StayPilot Comps — a personal real-estate comparable-listings tool for Portugal. It began as an
Algarve-only tool and the collection has since widened: as of 2026-08-24 the database holds 36,464
listings across 18 districts, led by Lisboa, Faro and Setúbal but reaching down to Portalegre and
Bragança with about a hundred each. That range is the main thing to hold in mind when changing
anything in the valuation path — a rule calibrated on Algarve prices (an absolute EUR/m² floor,
say) is wrong in both directions once Bragança is in the same table, which is why admission and
confidence are both judged against a property's own município rather than against a constant.
It stores scraped Idealista apartment listings with price history, and will eventually support
market analysis and valuation of owned properties against comparables. This is an early-stage,
single-developer project (see commit history) — a lot of the design is intentionally minimal
(no AutoMapper, no repository abstraction over EF Core) and should stay that way unless a
milestone specifically calls for more. As of 2026-09-01 this is a genuine multi-tenant app, not a
single-user prototype with auth bolted on: real people sign up through Entra External ID and only
ever see their own `OwnedProperty` rows. See *Authentication & multi-tenancy* below before
touching anything under `OwnedPropertyController`, `ICurrentUser`, or the Angular auth wiring.

This repo holds the Web API (`StayPilot/`, `StayPilot.slnx`) and, as of 2026-07-14, a minimal
Angular front end at `StayPilot/StayPilot.Web/` — see its own `README.md` for how to run it. It's
plain CSS/standalone-components, no UI framework, and only wires up screens the API actually
supports. Most of the product pitch is now real (Market Overview, Leaderboard, What Money Buys,
Neighbour Gaps, Renovation Upside, Listing Browser, Listing Lookup,
My Properties, Valuation, Feature Impact, Build Cost); Beach Proximity is the last placeholder
screen stating which backend endpoint it's waiting on — check `app.routes.ts` there before
assuming a module is real. The plain Market Areas list screen was folded into Market Overview
(the zone table is a filter, not something to read as a list), so `/market-areas` no longer
routes anywhere. **Add Listing and Price Snapshots were removed from the front end** (2026-08-18):
they are data-entry screens for the scraper's job, not something a customer of this tool should
see. The `ListingSnapshot` and `PropertyListing` write endpoints on the API are untouched — the
scraper still uses them; only the UI for hand-entry is gone, so do not re-add those screens.
The scraper that feeds the API (`AddProperty`) used to
live here as a second solution but now has its own repo:
https://github.com/dmytrolenivenko/AddProperty — clone it separately if you need to work on
scraping/ingestion.

The living architecture doc, milestone plan, and design rationale for this project are tracked
outside this repo, in the Obsidian vault at `c:\repos\EngineeringVault\vault\StayPilot\Project\StayPilot.md`
(plan) and `vault\StayPilot\Development\Session-NN_*.md` (dated session logs of what was actually
built/decided). Check the latest session log there for the most recent state and any open
decisions before making architectural changes — the vault, not this file, is where day-to-day
design decisions get recorded.

## Commands

All commands below assume the `StayPilot/` folder as working directory unless noted.

```bash
# Build / run the API
dotnet build StayPilot.slnx
dotnet run --project StayPilot.Api           # Swagger UI at the launch URL (see StayPilot.Api/Properties/launchSettings.json)

# Tests (xUnit)
dotnet test StayPilot.slnx
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"   # run a single test

# EF Core migrations (run from StayPilot/ folder)
dotnet ef migrations add <Name> --project StayPilot.Infrastructure --startup-project StayPilot.Api
dotnet ef database update --project StayPilot.Infrastructure --startup-project StayPilot.Api
```

Local DB is SQL Server **LocalDB** (`(localdb)\MSSQLLocalDB`), configured in
`StayPilot.Api/appsettings.Development.json`.

**CI/CD exists** (`.github/workflows/`), trunk-based across three branches, each mapped to its own
Azure environment — `master`→dev, `stable`→qa, `release`→prod (see `resolve` job in any workflow
for the exact mapping):

- `ci.yml` — PR gate into any of the three branches: builds + tests the API, builds the Angular app. No lint tooling beyond the C# compiler/analyzers.
- `infra.yml` — on push to one of the three branches (if `infra/**` changed) or manual dispatch: `az deployment group create` against `infra/main.bicep`, idempotent (add/update only, never deletes).
- `deploy-api.yml` — on push (if the API/Application/Domain/Infrastructure projects changed): opens a temporary SQL firewall hole for the runner's IP, runs `dotnet ef database update` against that environment's real Azure SQL DB, closes the hole, then `dotnet publish` + `azure/webapps-deploy` to `api-staypilot-{env}`.
- `deploy-web.yml` — equivalent for the Angular app → the `web-staypilot-{env}` Static Web App.

All three environments deploy to **real Azure SQL** (`infra/main.bicep`'s `sqlServer`/`sqlDatabase`
resources, `srv-staypilot-{env}`/`db-staypilot-{env}`) — LocalDB is dev-machine-only. Auth to Azure
is OIDC (no stored Azure credentials in GitHub); the SQL admin password and Azure identity live in
GitHub Environment secrets/vars, one set per env (`dev`/`qa`/`prod`), with prod additionally gated
by GitHub's environment-approval mechanism.

The API's `appsettings.json` (including `AzureAd`) is **not** overridden per environment anywhere
in this pipeline — `deploy-api.yml` just publishes and ships it as committed. All three deployed
environments therefore share the exact same Entra config — one CIAM tenant for both human sign-in
and the scraper's machine role, see *Authentication & multi-tenancy* below. CORS *is*
environment-correct without any extra step: `main.bicep` wires each environment's API CORS policy
straight from that same environment's own Static Web App hostname.

## Architecture

Layered solution, dependency direction `Api → Application → Infrastructure`, `Infrastructure → Application + Domain`, `Domain` has zero dependencies:

```
StayPilot.Api             Controllers (thin — no business logic), Program.cs composition root, Swagger
StayPilot.Application     Request/Response DTOs (Contracts/) and service interfaces (Interfaces/) only — no implementations
StayPilot.Domain          Entities and enums, plain C# classes, no NuGet deps
StayPilot.Infrastructure  EF Core DbContext + migrations + entity configurations + repositories
StayPilot.UnitTests       xUnit
```

Service implementations (`PropertyListingService`, `MarketAreaService`, `MarketOverviewService`,
etc.) live in `StayPilot.Application/Services/`, alongside the DTOs in `Contracts/` and the
interfaces in `Interfaces/`. The pure calculation is pulled out one level further into
`StayPilot.Application/Helpers/Calculators/` — a service loads, delegates the maths to a static
calculator, and maps; that split is what makes the calculators unit-testable without a DbContext.
`StayPilot.Infrastructure` holds the EF Core side and the repositories.

> **Note on history:** these services used to live in `StayPilot.Infrastructure/Services/` and this
> file used to describe that as deliberate. They have since moved to `Application`; if you find a
> doc or comment still pointing at `Infrastructure/Services`, it is stale.

Controller → Service (interface, injected) → Repository (interface, injected) → `StayPilotDbContext`.
Repositories live in `StayPilot.Infrastructure/Repositories/` and own all LINQ/EF query logic
(`.Where()`, `.Include()`, `.ToListAsync()`, etc.) against entities — they return entities, never
DTOs. Services hold business rules and do entity↔DTO mapping (see `MapToResponse`/`MapToEntity` in
`PropertyListingService`) — there is no AutoMapper. Register each repository in `Program.cs` DI
alongside its service.

> **Note on history:** earlier sessions deliberately skipped a repository layer as a simplification
> for a single-developer project. That was revisited in Session 10 — the user wants the repository
> pattern for its own sake (practice), and is retrofitting existing services to use it. Being
> retrofitted incrementally, service by service — check current session logs in the vault for which
> services have been converted before assuming this pattern is uniformly in place everywhere yet.

### API conventions

- **Routing:** `[Route("api/[controller]/[action]")]` on every controller — the action name is
  part of the URL, and ASP.NET's default action-name resolution drops an `Async` suffix (so
  `AddOwnedPropertyAsync` routes as `POST /api/OwnedProperty/AddOwnedProperty`). No route ever
  carries a version segment or a verb attribute beyond `[HttpGet]`/`[HttpPost]`.
- **Response shape:** every response class extends `ResponseBase`
  (`StayPilot.Application/Contracts/Response/Base/ResponseBase.cs`) — a nullable `Errors` list plus
  a computed `Succeeded`. Services call `response.AddError(ErrorCode.X, ...params)` instead of
  throwing; controllers call `this.ToActionResult(response)`
  (`StayPilot.Api/Extensions/ControllerExtensions.cs`), which maps `Succeeded` → 200,
  an error whose `ErrorCode` is `[NotFound]`-tagged → 404, anything else → 400. There is no
  try/catch in controllers or services for expected failures — only `ErrorCode`.
- **Error codes:** one `enum` (`ErrorCode` in the same file as `ResponseBase`), grouped in bands
  per domain (`-1..-99` general, `-100..-199` market areas, `-200..-299` property listings,
  `-300..-399` snapshots, `-400..-499` owned properties, `-500..-599` valuation/premium features).
  Each value carries a `[Display(Description = "...")]` template with `{0}`, `{1}`... placeholders,
  and optionally `[NotFound]` to mark it as a 404. **Append new codes at the end of their band;
  never renumber or reuse one** — callers (including the separate scraper repo) key on the number.
- **Authorization:** `OwnedPropertyController` carries a class-level `[Authorize]` — every one of
  its actions needs a signed-in account, human or machine. No other controller has a class-level
  `[Authorize]`; individual actions that write bulk data carry `[Authorize(Roles = "Api.Write")]`
  instead — that role only ever belongs to the scraper's machine identity (see *Authentication &
  multi-tenancy* below), never a signed-up tenant:
  `PropertyListingController.BulkAddPropertyListing`, `ListingSnapshotController.CreateListingSnapshotAsync`,
  `ListingSnapshotController.ReconcileActiveListingsAsync`. `MarketAreaController.RecalculateMarketAreaStats`
  and `PremiumFeatureController.ReCalculatePremiumFeaturesValue` used to be `Api.Write`-only too;
  as of 2026-09-01 they're plain `[Authorize]` — any signed-in tenant can trigger them (a deliberate
  prototype-stage call: the recompute is cheap enough, and there's realistically one user besides
  the developer). All other reads stay anonymous.
- **Controller inventory** (all under `StayPilot.Api/Controllers/`):

  | Controller | Actions |
  |---|---|
  | `PropertyListingController` | `BulkAddPropertyListing` (POST, `Api.Write`), `GetById`, `FilterPropertyAsync` (POST — browse/filter/page) |
  | `ListingSnapshotController` | `CreateListingSnapshotAsync` (POST, `Api.Write`), `GetListingSnapshotByPropertyIdAsync`, `ReconcileActiveListingsAsync` (POST, `Api.Write` — marks listings missing from a caller's URL list as sold) |
  | `MarketAreaController` | `GetAll`, `GetOptions` (place picker), `GetLeaderboard`, `GetBudgetRanking`, `GetNeighbourGaps`, `RecalculateMarketAreaStats` (POST, `[Authorize]` — any signed-in user) |
  | `MarketOverviewController` | `GetMarketOverview` — live-computed slice by place + property type + typology, no recalculation step |
  | `OwnedPropertyController` | **class-level `[Authorize]`.** `GetOwnedPropertyAsync`, `GetAllOwnedPropertyAsync`, `AddOwnedPropertyAsync` (POST), `DeleteOwnedPropertyAsync`, `UpdateOwnedPropertyAsync`, `EstimateEvaluationsOwnedpropertyAsync` (single-property valuation), `ListValuationsOwnedpropertyAsync` (portfolio valuation), `RevalueOwnedPropertiesAsync`/`RevalueOwnedPropertyAsync` (POST, plain `[Authorize]` — recalculates only the caller's own rows, see below) |
  | `PremiumFeatureController` | `GetAllPremiumFeatures`, `ReCalculatePremiumFeaturesValue` (POST, `[Authorize]` — any signed-in user) |
  | `BuildCostController` | `GetBasis` — build-cost rates, see *Build cost* below |

### Domain model

- `MarketAreaStats` — one precomputed row per place per level, rebuilt wholesale by
  `RecalculateMarketAreaStats`. **Anything added to this entity is null/zero on existing rows until
  a recalculation runs**, and that endpoint needs a signed-in account (`[Authorize]`, any tenant —
  see *Authentication & multi-tenancy*) — so a new stats column cannot be backfilled from a dev
  machine without a token. Design the read side to degrade honestly in the meantime (the renovation
  confidence treats a missing spread as fully overlapping,
  i.e. "not measurable", rather than as a clean separation). Each row owns a collection of
  `MarketAreaTypologyStats` — the same median/€/m²/area numbers broken down per Typology (T0, T1,
  T2...), which is what makes "what does €300k buy me here" answerable instead of one blended
  median across studios and villas.
- `MarketArea` — a geographic zone (District/Municipality/Town/Zone). Seeded via EF migrations
  from real-world Portuguese data (all districts, not only the Algarve); matched by exact
  normalized-string lookup (`GetMarketId` in
  `PropertyListingService`), not by ID, when a scraped listing doesn't supply a `MarketAreaId`.
- `PropertyListing` — one row per property, uniquely identified by `SourceUrl` (unique index;
  `AddPropertyListingAsync` is idempotent on `SourceUrl` — re-posting the same URL returns the
  existing entity rather than duplicating it). On create, the service also resolves the nearest
  `BeachMarker` via a plain in-memory Haversine distance calculation over all beach markers (no
  spatial SQL types) and stamps `NearestBeachName`/`DistanceToBeachMeters` onto the listing.
- `ListingSnapshot` — price history, one row per observation date, linked to a `PropertyListing`.
- `BeachMarker` — reference points used purely for nearest-beach distance calculation.
- `OwnedProperty` — one tenant's own apartments, priced against comparables by the
  Valuation screen. That screen is a **portfolio** view: one call to
  `OwnedProperty/ListValuationsOwnedproperty` prices every property in one pass (the model is
  fitted once over the whole listing table — never loop the single-property estimate
  endpoint), and each row expands into comps, feature contributions, an area demand score
  and a growth projection. Carries `OwnerUserId` (FK → `User`, `DeleteBehavior.Cascade`) — every
  read/write in `OwnedPropertyService` resolves the caller's id via `ICurrentUser` and filters or
  stamps by it; see *Authentication & multi-tenancy* below. This is the IDOR fix: before it, id was
  a bare sequential int and any signed-in caller could read/edit/delete any other tenant's rows.
- `User` — a person who has signed in at least once. Created **just-in-time** by `ICurrentUser`
  (`StayPilot.Api/Services/CurrentUser.cs`) on first authenticated request, keyed by the Entra `oid`
  claim (`ExternalId`, unique index) — there is no registration endpoint and no admin-created-user
  flow. `UserEmail` is also uniquely indexed; it's read off the token's `preferred_username` claim
  first, falling back to the `emails` claim array (Entra External ID's local email+password accounts
  don't reliably populate `preferred_username` the way workforce accounts do).
- `HousePriceGrowth` — **seeded reference data**, one row per Portuguese district plus a
  national fallback (`District = ""`). The percentages are *planning assumptions*, not a
  measured index: nobody scraped INE into this table, and every screen that quotes them also
  prints their `Source` string, which says so. Correct them by editing
  `Persistence/Configurations/AllHousePriceGrowth.cs` and adding a migration — nothing
  computes off them at write time. They exist because this database holds a few months of
  adverts in one region, and a ten year projection built on that alone would call one season
  of the Algarve "Portugal". `GrowthForecastCalculator` blends the seeded rate with the trend
  measured from local snapshots, capping the local half at 50% of the blend however long the
  series gets, and returns both halves separately so a forecast can always be taken apart.
- Demand (`DemandCalculator`) scores a place out of 100 on exactly two inputs — median days
  on market and whether new supply is arriving faster than it was. Both guards matter: days
  on market switches itself off while the median approaches the collection window (early on,
  every listing looks young because collection is young), and supply needs two full 90 day
  windows of history. An unmeasurable place reports `IsMeasurable = false`, never the middle
  of the scale — "not measured" and "average" must not render the same.
- `PremiumFeature` — the average price premium of one feature (sea view, garage, …), fitted once
  across the whole listing table by `FeaturePremiumCalculator` and reused until the next
  `ReCalculatePremiumFeaturesValue` call. Carries a 95% confidence range (`LowerBoundPercent`/
  `UpperBoundPercent` — if these straddle zero, the headline `PremiumPercent` is not a finding) and
  `ListingsWithFeature`, which is *not* the same as `SampleSize`: the latter is the same for every
  feature, so on its own it made a rarely-measured feature look as solid as a common one. A couple
  of features (sea view, lift) also carry a conditional `MaximumPercent` + `MaximumBasis` — the
  best case under a stated condition — because their flat average hides a huge spread (a sea view
  bought inland vs. beachfront) that one number would otherwise misrepresent.
- Build cost (`BuildCostService`, `IIneRepository`) has **no entity or migration** — it is priced
  live from one external number (Portugal's INE construction cost index) rather than a stored
  price list. A 2021 €/m² anchor plus a set of dimensionless ratios (a concrete pool is 0.82× the
  house rate per m² of water, a garage bay 0.55×, …) are escalated by the latest published index;
  a few machine-like extras (lift, KNX bus, borehole) are anchored in 2021 euros and escalated by
  the materials-only half of the index instead of the blended one, because labour and materials
  drift apart over time. If INE is unreachable, everything degrades to plain 2021 prices with an
  empty `IndexPeriod` rather than a 500 — see `BuildCostService`'s class doc for the full rationale
  and the quotes it was cross-checked against. `IneRepository` exists purely because INE's site
  sends no CORS headers, so the browser cannot call it directly — this API is a proxy.

Known scaling caveats (documented, not yet fixed — see the vault's Session-08 log for full
reasoning before touching these): `GetMarketId` and the nearest-beach lookup both do a full
in-memory table scan with C#-side string normalization / Haversine math instead of pushing the
filter into SQL. Deliberately deferred until data volume or an observed slow response justifies
the work — don't "fix" this speculatively.

### Scraper (now a separate repo)

The `AddProperty` scraper that feeds this API lives in its own repo now:
https://github.com/dmytrolenivenko/AddProperty. It POSTs to this API's `PropertyListing` and
`ListingSnapshot` endpoints — including `ReconcileActiveListingsAsync`, called once after
`dotnet run upload` finishes with an empty `new\` folder, using the newest `checkdeleted-reports\`
file's `SeenUrls` as `ActiveUrls` — and keeps its **own copies** of `PropertyListingRequest`/`ListingSnapshotRequest` and
the enums (`PropertyType`, `Typology`, `PropertyCondition`, `ListingStatus`) — it does not
reference `StayPilot.Application` or `StayPilot.Domain`. If you change a contract or enum values
in this API, update the matching copy in that repo too, and double check enum **numeric values**
match exactly (JSON serializes/deserializes enums by the API's `JsonStringEnumConverter`, but a
numeric mismatch between the two independent enum definitions would silently produce the wrong
value). Its `TokenProvider.cs` gets an `Api.Write`-role bearer token via client-credentials against
the `StayPilot Scraper` app registration — see *Authentication & multi-tenancy* below for which
tenant and which credentials.

## Authentication & multi-tenancy

As of 2026-09-01 this app has real multi-user tenancy end to end: signup, sign-in, per-user data
isolation, and a working Angular UI — not just a backend contract. This section is the map; the
comments inside the files themselves carry the "why" for each specific decision.

### The two Entra identities in play

Everything — human sign-in *and* the scraper's machine token — now goes through **one** Entra
tenant: **Entra External ID (CIAM)**, domain `staypilot.ciamlogin.com`, tenant id
`8d7cb648-496f-43f3-900d-336292e7cb9b`. It replaced an earlier single-tenant **workforce** Entra ID
(`e5c42229-2ed7-4a03-a901-d84421fe1c08`) that `StayPilot.Api`'s `AzureAd` config used to point at —
that old tenant is no longer referenced anywhere in this codebase; don't resurrect it. Workforce
and External ID/CIAM tenants are **structurally different Azure resources** — a workforce tenant
cannot be converted into a CIAM one, which is why this was a from-scratch tenant + app registration
build, not a config flip. Reasoning for choosing CIAM over "multi-tenant + personal Microsoft
accounts" (the cheaper alternative): CIAM gives self-service signup with local email+password
accounts and no requirement that a user already have a Microsoft account, which the alternative
does not.

Two app registrations live inside that one CIAM tenant:

- **`StayPilot API`** (`c447c11c-f8a9-4bf5-a9b1-6d176064370c`) — the API itself. `StayPilot.Api`'s
  `appsettings.json` `AzureAd` section (`Instance`/`TenantId`/`ClientId`) points at this tenant/app;
  `Program.cs` wires it in generically via `AddMicrosoftIdentityWebApiAuthentication` — no
  environment-specific override exists anywhere in the deploy pipeline, so dev/qa/prod all trust
  this exact tenant/app. It exposes one delegated scope, `access_as_user`
  (`api://c447c11c-f8a9-4bf5-a9b1-6d176064370c/access_as_user`) — what Angular/MSAL requests on
  login — and one app role, `Api.Write` (member type **Applications**, not Users — no human account
  can ever hold it), granted to `StayPilot Scraper` alone via an application-permission + admin
  consent. It also has a **User flow** (`SignUpSignIn`: email+password, collects Email Address +
  Display Name) attached — that's the actual hosted signup/signin page a browser lands on; without
  a user flow attached, there is no sign-in experience at all, CIAM apps don't get one by default.
  Redirect URIs registered: `http://localhost:4201` (dev) plus each environment's real Static Web
  App hostname (Single-page application platform, Authorization Code + PKCE, no secret).
- **`StayPilot Scraper`** (`31968c43-9bd3-412f-b02e-c2cec1a68914`) — a machine-only identity with a
  client secret, used for **client-credentials** (app-only) token requests. Holds the `Api.Write`
  application permission on `StayPilot API`. Two callers use it today: the separate `AddProperty`
  repo's `TokenProvider.cs`, and manual Postman-driven recalculation triggers (client-credentials
  grant, scope `api://c447c11c-f8a9-4bf5-a9b1-6d176064370c/.default`, token URL
  `https://staypilot.ciamlogin.com/8d7cb648-496f-43f3-900d-336292e7cb9b/oauth2/v2.0/token`).

### A real quirk: two valid issuer hosts for the same tenant

Tokens from this one CIAM tenant come back with **different `iss` claims** depending on which
client library requested them — raw HTTP against the `ciamlogin.com` token endpoint (Postman,
`TokenProvider.cs`) yields `https://staypilot.ciamlogin.com/{tenantId}/v2.0`; MSAL.js (the Angular
app) yields the canonical `https://login.microsoftonline.com/{tenantId}/v2.0` instead. Same tenant,
same audience, genuinely two issuer hosts. `Program.cs` handles this with a `PostConfigure<JwtBearerOptions>`
block (search `ValidIssuers`) that accepts both — it has to be `PostConfigure`, not `Configure`,
specifically so it runs *after* `Microsoft.Identity.Web`'s own setup, which would otherwise
overwrite a plain `Configure` call back down to a single issuer. If a future Entra/MSAL update
changes this behavior, that block is the first place to look before assuming a validation bug.

### Per-user data isolation (the IDOR fix)

`User` (see *Domain model* above) is JIT-provisioned by `ICurrentUser`
(`StayPilot.Api/Services/CurrentUser.cs` — Api-layer only; the interface lives in
`StayPilot.Application/Interfaces/Services/ICurrentUser.cs`, framework-agnostic by design since
`IHttpContextAccessor`/`ClaimsPrincipal` are ASP.NET Core-specific and don't belong in Application).
It reads the Entra `oid` claim (`ClaimsPrincipal.GetObjectId()`), looks up a matching `User` by
`ExternalId` via `IUserRepository`, and inserts one on first sight. Known, deliberately unaddressed
edge case: two near-simultaneous first requests from the same brand-new user can both miss the
lookup and both try to insert — the unique index on `ExternalId` makes the loser's
`SaveChangesAsync` throw. Not worth solving speculatively; worth fixing if it's ever actually hit.

`OwnedPropertyService` and `OwnedPropertyRepository` resolve `ICurrentUser.GetCurrentUserIdAsync()`
on every call and filter (`GetOwnedPropertyAsync`, `GetAllOwnedPropertyAsync`, `DeleteOwnedPropertyAsync`)
or stamp (`AddOwnedPropertyAsync`) by it — this, plus `OwnedPropertyController`'s class-level
`[Authorize]`, is what actually closes the IDOR (before this, `id` was a bare sequential int and any
signed-in caller could read/edit/delete any other tenant's rows by guessing it).
`InvestmentAnalysisService.AnalyzeOwnedPropertyAsync` had the identical hole — discovered mid-fix
when its call site broke against the changed repository signature — and got the same treatment.

### Angular / MSAL (`StayPilot.Web/src/app/`)

- `core/config/msal.config.ts` — the single `PublicClientApplication` instance (`clientId`,
  `authority: https://staypilot.ciamlogin.com/{tenantId}`, `cacheLocation: 'localStorage'`). Carries
  `knownAuthorities: ['staypilot.ciamlogin.com']` — MSAL only trusts a short built-in list of
  Microsoft authority hosts by default; a custom CIAM domain has to be explicitly allow-listed or
  MSAL refuses to talk to it.
- `main.ts` — calls `msalInstance.initialize().then(() => bootstrapApplication(...))`. MSAL v3
  requires this async init to complete before anything else touches the instance; the whole app
  waits on it.
- `app.config.ts` — provides `MSAL_INSTANCE` (pointing at the config above), `MSAL_GUARD_CONFIG`
  (`InteractionType.Redirect` — a blocked route full-page-redirects to the hosted login rather than
  popping up), `MsalService`, `MsalGuard`, `MsalBroadcastService`.
- `app.routes.ts` — every route sits under one pathless parent with `canActivateChild: [MsalGuard]`,
  forcing sign-in for the entire app, not just My Properties. **Check this before relying on it** —
  it has been toggled off before for a local visual QA pass (look for a commented-out
  `canActivateChild` line with a TEMP note); confirm it's actually active rather than assuming from
  this doc.
- `core/interceptors/auth.interceptor.ts` — attaches a bearer token from **two possible sources**,
  in priority order: (1) a manual `localStorage` override (`localStorage.setItem('staypilot_token', ...)`,
  fetched the same client-credentials way `TokenProvider.cs`/Postman do) — needed to exercise
  `Api.Write`-only endpoints from a browser, since no signed-up tenant's token ever carries that
  role; (2) MSAL's `acquireTokenSilent` for whoever is actually signed in, covering everything
  gated by a plain `[Authorize]`. Silent renewal failure (an expired/dead session) falls back to no
  token rather than throwing — the API's resulting 401 is treated as the real signal that an
  interactive re-login is needed, not something to paper over client-side.
- `app.component.ts`/`.html` — sidebar Sign in/Sign out control; shows both the account's display
  name and email once signed in. Email extraction has a fallback: tries `account.username` first,
  falls back to the ID token's `emails` claim array — CIAM's local email+password accounts don't
  reliably populate `username` as an email the way workforce accounts do.

### Owner-only vs. any-signed-in-user, and why it's inconsistent on purpose

`RevalueOwnedPropertiesAsync`/`RevalueOwnedPropertyAsync` only ever touch the caller's own rows
(see *Per-user data isolation* above), so any signed-in tenant recalculating their own portfolio is
safe by construction — plain `[Authorize]`, no role check, button visible on the Valuation screen.
`RecalculateMarketAreaStats` and `ReCalculatePremiumFeaturesValue` are different in kind — they
recompute **shared** data that every tenant's screens read from, not just the caller's own. They
were `Api.Write`-only originally; **as of 2026-09-01 they're plain `[Authorize]` too**, opened up to
any signed-in user as a deliberate call for a prototype with realistically one user besides the
developer — not because the underlying "this affects everyone" reasoning changed. Revisit this the
moment there's a second real, unrelated user: a careless or hostile tenant hammering either button
recomputes data every other tenant sees, with nothing today rate-limiting it (M4/rate-limiting was
explicitly deferred, not built).

### Not built yet

- **Scheduled/automatic recalculation.** Discussed and deliberately not built: the API's App
  Service Plan is **F1 (Free tier)** (`infra/main.bicep`), which doesn't support "Always On" and
  idle-sleeps the app after ~20 minutes of no traffic — ruling out both an in-process
  `BackgroundService` timer and an App Service WebJob as reliable options. The concluded right
  answer, if/when this gets built, is an **Azure Function on a Consumption plan with a Timer
  Trigger**, calling the three endpoints above via the same `StayPilot Scraper` client-credentials
  flow — Functions' timer trigger wakes itself regardless of anything else's state, unlike Free-tier
  App Service. In the meantime, recalculation is manual (Postman, or the UI buttons).
- **Rate limiting / abuse surface (M4).** Nothing throttles repeated calls to any endpoint,
  including the shared-data recalculation ones above.
- **Roles beyond `Api.Write` (M2).** No "admin vs. regular tenant" distinction exists — every
  signed-in account is equivalent from the API's point of view, except for the one machine role.
