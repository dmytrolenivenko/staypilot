# Runbook — Rebuilding the Portuguese MarketArea seed from Idealista

**What this produces:** `StayPilot.Infrastructure/Persistence/Configurations/AllMarketAreas.cs` —
every Portuguese market area (District / Municipality / Town / Zone) exactly as idealista.pt
publishes it. Last run: 2026-08-07 → 4,365 rows, 18 mainland districts, 278 municipalities.

**Read this before starting.** The obvious approach (click through the map) does not work and
will waste a day. The whole job takes ~20 minutes if you follow the recipe in §3.

---

## 1. Why the "expand the tree and copy the blob" method fails

The original brief described a location picker with an expand/collapse tree, where expanding a
district loaded all its municipalities into one JavaScript object you could right-click →
*Store as global variable* → `copy(temp1)`.

**That UI no longer exists.** As of 2026-08 `https://www.idealista.pt/pesquisa-multizona/comprar-casas`
is a Google-Maps view where:

- Clicking a district **selects** it (panel shows "Beja distrito") and auto-zooms. It does not expand.
- Clicking inside a selected district **replaces** the parent with the child ("Beja distrito" → "Serpa").
  This is the "swallowing" the old brief warned about — it is now the *only* behaviour.
- Zooming in never expands a selection. The tooltip says *"12 zonas incluídas ao fazer zoom"*, but the
  panel still shows one entry and no new data is fetched. Verified twice.
- There is no console object to copy. Selection state holds only what you clicked.

So one click = one location. Mainland Portugal is ~4,300 locations. Do not go down this road.

## 2. Where the data actually lives

Four endpoints. All are what the page itself calls — nothing private, nothing scraped from labels.

**Never read place names off the map.** Map labels are abbreviated
(`Salvador e S.ta M. da Feira` on the map = `Salvador e Santa Maria da Feira` in the data).

### 2.1 Tile service — hierarchy and ids

```
GET https://mt1.idealista.pt/17/tiles/json/desktop/pt/zoom{z}/{x}/{y}.json
→ [ { "geometry": "<encoded polyline>", "properties": { "id": "0-EU-PT-08-01-006-06", "shortUri": "aKb" } }, ... ]
```

Standard XYZ tiles. Empty body (HTTP 200, zero bytes) = nothing there. No auth, no rate limiting hit
at ~10 req/s. **This is the only source of the parent→child hierarchy.**

`id` grammar — the number of `-` segments is the level:

| segments | level | example |
|---|---|---|
| 4 | District | `0-EU-PT-08` |
| 5 | Municipality | `0-EU-PT-08-01` |
| 7 | Town (freguesia) | `0-EU-PT-08-01-006-06` |
| 8 | Sub-zone container | `0-EU-PT-08-01-006-06-02` |
| 9 | Zone | `0-EU-PT-08-01-006-06-02-003` |

Levels 8 and 9 both map to `Zone` in our schema (your existing Faro seed mixes them: `Olhos de Água`
is 8-part, `Centro da Cidade` is 9-part).

Which level a zoom returns:

| zoom | returns |
|---|---|
| ≤ 8 | districts |
| 9–11 | municipalities |
| 12 | towns |
| 13+ | zones |

⚠️ **This mapping is polygon-size dependent, not fixed.** Dense urban freguesias only reveal their
children much deeper — Lisbon city zones do not appear until **z15**. Crawling deep tiles to find
zones diverges (z15 over the mainland ≈ 8,500 tiles, z16 ≈ 30,000). §3 avoids this entirely.

### 2.2 Totals — the name/parentName/total record

```
GET /pt/multizoneSearcherLocationTotals?locationShortUris=a5m,a5I,a5J&operation=1&typology=1
→ {"a5N":{"shortUri":"a5N","name":"Baleizão","parentName":"Beja","total":20}, ...}
```

- Comma-separated. **Batches of 300 work.** Unknown codes are silently omitted.
- This is the exact object the old brief wanted you to copy — same bytes a click produces.

### 2.3 Suggestion — name search

```
GET /pt/multizoneSearcherLocationsSuggestion?searchField=Baleiz&operation=1&typology=1
→ [{"name":"Baleizão","count":20,"category":"Zona","shortUri":"a5N","parentName":"Beja"}]
```

The parameter is **`searchField`**, not `q` (`q` returns HTTP 400). Adds `category`
(`Distrito` / `Concelho` / `Zona` / `Freguesia/Zona`). Capped at 10 results — useful for spot checks,
useless for enumeration.

### 2.4 Listing URL — disambiguation

```
GET /pt/multizoneSearcherListingUrl?locationShortUris=byJ&operation=1&typology=1
→ {"url":"/comprar-casas/odivelas/arroja/"}
```

First path segment is usually the **municipality** slug — the tiebreaker for name collisions.
Caveat: for some zones the first segment is the *town* instead (`/comprar-casas/luz/praia-da-luz/`),
so it can't always disambiguate. See §5.3.

---

## 3. The efficient recipe

**Key insight: do not crawl deep tiles for zones.** The `shortUri` code space is small and dense, so
you can enumerate *every* Portuguese location in ~26 requests, then use tiles only for the shallow
levels where they are cheap.

Total cost: **~2,900 tile requests + ~130 endpoint calls ≈ 20 minutes.**

### Step 0 — open the page in a real browser

Navigate to `https://www.idealista.pt/pesquisa-multizona/comprar-casas` and **wait ~8 seconds**.
DataDome shows *"Verificando o dispositivo…"* first; requests fired before it clears will fail.
Run everything as `fetch()` from that tab's context so the DataDome cookie is attached.

### Step 1 — enumerate every location (~26 calls)

`shortUri` is 3 chars from `abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789`,
and **only the `a??` and `b??` prefixes exist** (`c??` and beyond return nothing).
That is 2 × 62 × 62 = 7,688 candidates.

Batch them 300 at a time through §2.2. Result: **5,796 locations** (all levels, includes
Madeira/Azores) as `{shortUri: {name, parentName, total}}`.

This gives you every name — but **no hierarchy**. That's what Step 2 is for.

### Step 2 — shallow tiles for the hierarchy (~2,900 tiles)

Mainland bounding box: lat 36.85 → 42.20, lon −9.62 → −6.15.

1. **z8** over the box → 18 district ids. (~18 tiles)
2. **z10** over the box → all municipality ids. x 484–494, y 376–400. (~275 tiles)
3. **z12**, but only the 16 children of each *non-empty* z10 tile → all town ids. (~2,600 tiles)

Cache by `z/x/y` and skip already-fetched tiles; districts share tiles, so crawling globally rather
than per-district avoids a lot of duplicate work.

You now have `id → shortUri` for districts, municipalities and towns, and therefore
town → municipality → district by string prefix.

### Step 3 — attach zones without deep tiles

Zones are the enumerated codes from Step 1 that are **not** in the tile map. Assign each to a town by
matching its `parentName` against known town names (see §5.2 for why that's safe, and §5.3 for the
collisions). Last run: 5,796 total − 3,952 tiled = 1,844 unmapped, of which 859 attach to a mainland
town; the rest are islands and duplicate aliases.

### Step 4 — build rows

For each district → municipality → town (sorted with `localeCompare('pt')`):

- Town has zones → **one row per zone**, no `Zone = null` row.
- Town has no zones → **one row with `Zone = null`**.
- Dedupe zones **by name**, not by shortUri — aliases produce two codes for one place (§5.4).
- Municipality with no polygon → use the single town's name as the municipality name (§5.5).

Ids contiguous from 1.

---

## 4. Verification — do not skip

Municipality counts per district. All 18 must match exactly:

| district | n | district | n | district | n |
|---|---|---|---|---|---|
| Aveiro | 19 | Guarda | 14 | Santarém | 21 |
| Beja | 14 | Leiria | 16 | Setúbal | 13 |
| Braga | 14 | Lisboa | 16 | Viana do Castelo | 10 |
| Bragança | 12 | Portalegre | 15 | Vila Real | 14 |
| Castelo Branco | 11 | Porto | 18 | Viseu | 24 |
| Coimbra | 17 | Évora | 14 | Faro | 16 |

Sum = 278 = mainland Portugal's municipality count.

⚠️ Count them **derived from ids** (`id.split('-').slice(0,5)`), not from 5-segment ids present as
polygons — otherwise the three single-parish councils in §5.5 read as missing.

Other checks that caught real bugs last run:

- **Faro is the regression test.** Its rows already existed hand-made. Regenerating Faro reproduced
  every one of the 12 Albufeira zones and all 76 town groups — that's what proved the method.
- Guia's zones must come out as `Albufeira / Guia` → Salgados, Galé, Vale de Parra.
- Porto genuinely has **no** 8/9-part ids at z12–z14. That is correct, not a gap — its city zones live
  in the enumeration and attach in Step 3.
- Row-level: ids contiguous 1..N, braces balanced, zero duplicate (District, Municipality, Town, Zone).

---

## 5. Traps — every one of these bit us

### 5.1 `parentName` is NOT the immediate parent

The single most dangerous field.

| level | what `parentName` holds |
|---|---|
| District | `""` |
| Municipality | district name |
| Town | **district name** — *not* the municipality |
| Zone | town name |

So `"Albufeira e Olhos de Água"` has `parentName: "Faro"`, not `"Albufeira"`. The old brief's mapping
table claims towns carry the municipality — **it is wrong**. Chaining upward on `parentName` misfiles
every town. Use the hierarchical id for levels 1–3; `parentName` only for zones.

Baleizão's `parentName: "Beja"` is the *district*, not the municipality of the same name.

### 5.2 Why parentName is still safe for zones

A town's `parentName` is its district, never its municipality. Therefore any entry whose `parentName`
equals a *town* name must be deeper than town level. No level ambiguity — only the collision in §5.3.

### 5.3 Cross-district name collisions

`Odivelas` is a town in Beja (Ferreira do Alentejo) **and** a municipality/town in Lisboa. Matching
zones by `parentName` alone silently files Lisboa's Arroja, Codivel and Colinas do Cruzeiro under Beja.

Resolve in this order:

1. Keep only candidate towns that actually have children in the tile data.
2. Still ambiguous → §2.4 listing URL, match the first path segment against municipality slugs
   (accent-stripped, lowercased, non-alphanumerics → `-`). Resolved 98/98 last run.
3. First segment is a town, not a municipality (`luz`, `guia`, `canelas`, `valadares`) → pick the
   candidate whose `shortUri` is numerically closest (base-62 over the alphabet above). Codes cluster
   by region; median distance to the true parent is 6. Verified against the known-good Faro seed.

### 5.4 Duplicate shortUris for one place

`a5t` and `a5u` are both *Almodôvar* concelho with total 118; `a5z` is the *freguesia* Almodôvar with
total 35. Same name at two levels **and** two codes for one entity. Dedupe zones by **name within a
town**. Never dedupe by name globally — real distinct places repeat across districts.

### 5.5 Municipalities Idealista does not publish

Single-parish councils get no municipality polygon; they appear only at town level:

- **São João da Madeira** (Aveiro) — `0-EU-PT-01-16`
- **Barrancos** (Beja) — `0-EU-PT-02-04`
- **Alpiarça** (Santarém) — `0-EU-PT-14-04`
- **São Brás de Alportel** (Faro) — same pattern, already in the old seed

Write these as `Municipality = Town = <name>`, matching how São Brás de Alportel was already handled.
**Never invent a municipality Idealista did not show.**

### 5.6 Container zones repeat the town name

Idealista nests one level deeper than our schema, so some 8-part entries duplicate their town's name —
e.g. `Town = "Albufeira e Olhos de Água", Zone = "Albufeira"`. Kept verbatim on purpose. Strip only if
explicitly asked.

### 5.7 Idealista's spelling differs from the old hand-made seed

The pre-2026 Faro rows were normalised by hand. Idealista's real values:

| old seed | Idealista |
|---|---|
| `Lagoa` | `Lagoa (Algarve)` |
| `Estombar e Parchal` | `Estômbar e Parchal` |
| `Algar Seco` | `Algar seco` |

Reproduce Idealista's version **exactly**, accents, casing, slashes and all
(`Longueira/Almograve`, `São Miguel do Pinheiro - São Pedro de Solis - São Sebastião dos Carros`).
Never "fix" a name. A missing row is recoverable in five minutes; an invented one looks correct forever.

### 5.8 Idealista only lists places that have listings

A location with zero adverts is absent. That is expected — note it, never backfill from INE,
Wikipedia or Google Maps.

---

## 6. Tooling constraints (Claude-in-Chrome)

- `javascript_tool` output truncates at **~1 KB**. Keep return values to counts and short samples.
- Bulk transfer: write into a `<pre>` in the page and read with `get_page_text` — good for **~8 KB** a call.
- CDP `Runtime.evaluate` times out at **45 s** → cap loops at **~250 tile fetches per call** (25 ms delay).
- Keep state on `window` and checkpoint to `localStorage`; the extension disconnects occasionally.
  Page state survives a reconnect, but not a reload.
- Downloading a second file from the same origin needs the user to allow multiple downloads.
  Blocked attempts flush through once allowed — watch for duplicate `(1)`, `(2)` copies.
- Some outputs get blocked by a content filter (URLs with query strings, minified JS). Print only the
  JSON body, never the request URL.

## 7. Decisions baked into the current file

Agreed with Dmytro on 2026-08-07:

1. **Faro is included and regenerated** (153 → 321 rows). `MarketAreaConfiguration.cs` must therefore
   call `builder.HasData(AllMarketAreas.All);` instead of its own list, or ids 1–153 collide.
2. **No `Zone = null` companion row** when a town has zones. The old file did this for only 4 of 16
   such towns — inconsistent.
3. **Ids fresh 1..N**, ordered district → municipality → town → zone, `localeCompare('pt')`.
4. Mainland only. Madeira and the Azores excluded.

## 8. Re-running

Idealista's totals drift constantly and zones get added, so a rerun will produce a different row
count. Regenerate the whole file rather than patching. Re-verify §4 before shipping, and diff the
previous file to see what Idealista actually changed.

Intermediate JSON snapshots (`0-districts.json`, `<district>/1-municipalities.json`, `2-towns.json`,
`3-zones.json`) are optional — they were the old brief's deliverable. `AllMarketAreas.cs` is generated
directly from the in-memory maps; the snapshots are only useful as an audit trail.
