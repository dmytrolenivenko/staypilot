# Misfiled listings — how to find them, what they break

**Found:** 2026-09-21, during a full audit of the API and the front end, on the local
`StayPilotCompsDb` (65,821 listings).

A listing's coordinates and its `MarketArea` disagree. The coordinates are right; the market area
is wrong. Because every place-level number in this app is grouped by `MarketArea` and never by
coordinates, a misfiled listing is counted in full against a district it is nowhere near.

This is an **ingestion** fault, not a reading one. Nothing in `StayPilot.Api` can produce it:
`Calculator.GetMarketId` matches district → município → freguesia and every one of its three
attempts requires the district to match, so it cannot cross a district boundary on its own. The
listings arrived already carrying the wrong district (or an explicit wrong `MarketAreaId`) from
the scraper, [AddProperty](https://github.com/dmytrolenivenko/AddProperty).

## What it breaks

`MarketAreaStats` is rebuilt from listings grouped by their market area, so one misfiled cluster
moves a whole district's row on the Leaderboard, the budget ranking, neighbour gaps and the home
page — all of which read that table.

The worst case in the current data:

| | Listings | Median €/m² | Leaderboard rank |
|---|---|---|---|
| Guarda, as shown | 1,128 | **3,984** | 4th of 18 — above Porto, Braga and Setúbal |
| Guarda, misfiled cluster removed | 93 | **1,643** | second cheapest district in the country |

Guarda is one of the cheapest districts in Portugal. It reads as the fourth most expensive
because 1,035 of its 1,128 listings are physically in **Porto** (average coordinates 41.15,
−8.62 — Porto city centre), filed under `Guarda / Seia / Alvoco da Serra`.

It also breaks the radius filter in a way that looks like a bug in the filter itself: a search
for `Lisboa` município `WithinKm = 15` returns listings labelled **Braga**, because 377 flats in
Lisbon are filed under `Braga / Braga / São Vicente`. The circle is correct; the labels are not.

## The clusters in the current data

Everything more than 100 km from its own district's capital:

| Filed as | Listings | Actually in |
|---|---|---|
| `Guarda / Seia / Alvoco da Serra` | 1,035 | Porto (41.15, −8.62) |
| `Braga / Braga / São Vicente` | 377 | Lisboa (38.72, −9.12) |
| `Aveiro / Vagos / Santo António` | 304 | Lisboa (38.72, −9.15) |
| `Leiria / Pombal / Carnide` | 31 | Lisboa |

1,747 listings, about 2.7% of the table.

Three of the four are **freguesia names that exist in more than one district** — São Vicente,
Santo António and Carnide are all Lisbon freguesias *and* names used elsewhere in the country.
That is the shape of the fault: a freguesia resolved by name without its district.

## How to find them

Run this against whichever database you are checking. It compares each listing's
coordinates against its district's capital, which is public geography rather than anything
derived from this table, so a polluted district cannot hide its own pollution. No district in
Portugal reaches 120 km from its capital, so 100 km is comfortably outside the noise.

```sql
WITH capital(District, Lat, Lon) AS (
  SELECT * FROM (VALUES
    ('Aveiro',40.6405,-8.6538),('Beja',38.0150,-7.8650),('Braga',41.5454,-8.4265),
    ('Bragança',41.8061,-6.7567),('Castelo Branco',39.8222,-7.4931),('Coimbra',40.2033,-8.4103),
    ('Évora',38.5714,-7.9135),('Faro',37.0194,-7.9304),('Guarda',40.5373,-7.2677),
    ('Leiria',39.7436,-8.8071),('Lisboa',38.7223,-9.1393),('Portalegre',39.2967,-7.4286),
    ('Porto',41.1579,-8.6291),('Santarém',39.2362,-8.6860),('Setúbal',38.5244,-8.8882),
    ('Viana do Castelo',41.6946,-8.8302),('Vila Real',41.3006,-7.7441),('Viseu',40.6566,-7.9125)
  ) v(District, Lat, Lon))
SELECT ma.District, ma.Municipality, ma.Town,
       COUNT(*) AS misfiledListings,
       CAST(MIN(SQRT(SQUARE(pl.Latitude-c.Lat)
            + SQUARE((pl.Longitude-c.Lon)*COS(c.Lat*PI()/180)))*111.32) AS DECIMAL(6,0)) AS minKmFromCapital
FROM PropertyListings pl
JOIN MarketAreas ma ON ma.Id = pl.MarketAreaId
JOIN capital c ON c.District = ma.District
WHERE pl.Latitude IS NOT NULL
  AND SQRT(SQUARE(pl.Latitude-c.Lat)
      + SQUARE((pl.Longitude-c.Lon)*COS(c.Lat*PI()/180)))*111.32 > 100
GROUP BY ma.District, ma.Municipality, ma.Town
ORDER BY misfiledListings DESC;
```

## Fixing it

In order, because the later steps are wasted without the first:

1. **Fix the resolution in `AddProperty`.** A freguesia has to be resolved inside its district,
   never by name alone. Until that is done, every new import re-creates the problem.
2. **Re-home the rows already stored.** The coordinates are the trustworthy half, so the repair
   is a reverse-geocode of each affected listing's latitude/longitude back to a `MarketArea`.
   This repo cannot do that offline — there is no coordinate on `MarketArea` and no geocoder in
   the solution. Re-scraping the affected `SourceUrl`s through a fixed scraper is the cheaper
   route, and `AddPropertyListingAsync` is idempotent on `SourceUrl`, so a re-import updates
   rather than duplicates.
3. **Run `MarketArea/RecalculateMarketAreaStats`.** Nothing on any screen changes until it does —
   `MarketAreaStats` is a stored table, not a live read.

Until step 2, treat the Guarda, Braga, Aveiro and Leiria rows on every place-level screen as
unreliable, and do not read anything into a radius search that returns a far-away district name.

## Not the same thing

A listing with **no** coordinates is a separate and much smaller matter — it is refused at
ingestion (`ErrorCode.ListingLocationRequired`), so it never reaches the table. A listing whose
market area cannot be matched at all is also refused (`ErrorCode.ListingMarketAreaNotFound`).
What gets through is the case where a *wrong* answer is nonetheless a valid one.
