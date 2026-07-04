using Microsoft.Playwright;
using System.Globalization;
using System.Text.Json;

namespace AddProperty;

/// <summary>
/// Scrapes Idealista Faro/Algarve apartment listings using Playwright in headed mode.
/// Designed for personal-scale use: one browser, human-paced, persistent login.
/// All CSS selectors, regexes, and text markers live in <see cref="IdealistaLocators"/> —
/// update that file when Idealista's markup or wording changes, not this one.
/// </summary>
public class IdealScraper
{
    private readonly string _jsonFolder;
    private readonly string _profilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Random _rng = new();
    private HashSet<string> _alreadyScrapedUrls = new(StringComparer.OrdinalIgnoreCase);

    public IdealScraper(string jsonFolder, JsonSerializerOptions jsonOptions)
    {
        _jsonFolder = jsonFolder;
        _profilePath = Path.Combine(AppContext.BaseDirectory, ".playwright-profile");
        _jsonOptions = jsonOptions;

        Directory.CreateDirectory(_jsonFolder);
        Directory.CreateDirectory(_profilePath);
    }

    // ── Public entry point ──────────────────────────────────────────────

    /// <summary>Scrapes the given number of pages starting from the next unscraped page.</summary>
    /// <returns>List of saved JSON file paths.</returns>
    public async Task<List<string>> ScrapeAsync(int pageCount)
    {
        int startPage = GetNextPageNumber();
        var savedFiles = new List<string>();

        _alreadyScrapedUrls = LoadAlreadyScrapedSourceUrls();
        Logger.LogInformation($"Loaded {_alreadyScrapedUrls.Count} previously-scraped listing URL(s) for dedup.");

        Logger.LogInformation($"Starting scrape from page {startPage}, {pageCount} page(s).");
        Logger.LogInformation($"Browser profile: {_profilePath}");
        Logger.LogInformation("A browser window will open. If not logged in, log in manually then press Enter here.");

        using var pw = await Playwright.CreateAsync();
        await using var context = await pw.Chromium.LaunchPersistentContextAsync(
            _profilePath,
            new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                SlowMo = 50,
                Locale = "pt-PT",
                ViewportSize = new ViewportSize { Width = 1400, Height = 800 },
                Args = ["--disable-blink-features=AutomationControlled"]
            });

        var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

        // Navigate to Idealista homepage first so user can log in if needed
        await page.GotoAsync("https://www.idealista.pt", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Task.Delay(2000);

        if (await IsBlockedAsync(page))
        {
            Logger.LogError("Blocked on homepage. Solve CAPTCHA manually, then press Enter.");
            Console.ReadLine();
        }

        // Check if login prompt or already logged in — give user a chance
        Logger.LogInformation("Press Enter when ready to start scraping (browser should show idealista.pt)...");
        Console.ReadLine();

        for (int p = 0; p < pageCount; p++)
        {
            int pageNum = startPage + p;
            Logger.LogInformation($"\n=== Scraping page {pageNum} ===");

            var file = await ScrapeSearchPageAsync(page, pageNum);
            if (file != null) savedFiles.Add(file);

            if (p < pageCount - 1)
                await RandomDelay(30_000, 60_000);
        }

        Logger.LogInformation($"\nDone. Saved {savedFiles.Count} file(s).");
        return savedFiles;
    }

    // ── Search results page ─────────────────────────────────────────────

    private async Task<string?> ScrapeSearchPageAsync(IPage page, int pageNum)
    {
        var url = IdealistaLocators.SearchUrl.ForPage(pageNum);

        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        try
        {
            await page.WaitForSelectorAsync(IdealistaLocators.SearchResults.ListingLinks,
                new PageWaitForSelectorOptions { Timeout = 15_000 });
        }
        catch (TimeoutException)
        {
            // Might be blocked, or genuinely no listings on this page — both checked below.
        }

        if (await IsBlockedAsync(page))
        {
            Logger.LogError("Blocked on search page. Solve CAPTCHA manually, then press Enter.");
            Console.ReadLine();
        }

        // Pause on results page like a human scanning listings
        await RandomDelay(5_000, 12_000);

        // Collect listing URLs
        var listingUrls = await page.EvaluateAsync<string[]>(
            $"Array.from(document.querySelectorAll('{IdealistaLocators.SearchResults.ListingLinks}')).map(a => a.href)");

        if (listingUrls.Length == 0)
        {
            Logger.LogError("No listings found on page. Might be blocked or end of results.");
            return null;
        }

        Logger.LogInformation($"Found {listingUrls.Length} listings on page {pageNum}.");

        var kept = new List<PropertyListingRequest>();
        var excluded = new List<(string url, string reason)>();
        var skipped = 0;

        foreach (var listingUrl in listingUrls)
        {
            var normalizedUrl = NormalizeUrl(listingUrl);
            if (_alreadyScrapedUrls.Contains(normalizedUrl))
            {
                skipped++;
                Logger.LogInformation($"  SKIPPED (already scraped): {listingUrl}");
                continue;
            }

            await RandomDelay(15_000, 40_000); // Human-paced between listings

            try
            {
                var (listing, exclusionReason) = await ScrapeListingAsync(page, listingUrl);

                if (exclusionReason != null)
                {
                    excluded.Add((listingUrl, exclusionReason));
                    Logger.LogInformation($"  EXCLUDED: {listingUrl} — {exclusionReason}");
                }
                else if (listing != null)
                {
                    kept.Add(listing);
                    _alreadyScrapedUrls.Add(normalizedUrl);
                    Logger.LogInformation($"  KEPT: {listingUrl} — {listing.Typology} {listing.AreaM2}m² {listing.ListingSnapshot.Price}€");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"  ERROR on {listingUrl}: {ex.Message}");
                excluded.Add((listingUrl, $"Error: {ex.Message}"));
            }
        }

        // Save JSON
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var fileName = $"{date}-page{pageNum:D2}.json";
        var filePath = Path.Combine(_jsonFolder, fileName);

        var json = JsonSerializer.Serialize(kept, new JsonSerializerOptions(_jsonOptions) { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);

        // Report
        Logger.LogInformation($"\nPage {pageNum} summary:");
        Logger.LogInformation($"  Total: {listingUrls.Length}, Kept: {kept.Count}, Excluded: {excluded.Count}, Skipped (dedup): {skipped}");
        Logger.LogInformation($"  Saved: {filePath}");
        foreach (var (eUrl, reason) in excluded)
            Logger.LogInformation($"  - {eUrl}: {reason}");

        return filePath;
    }

    // ── Individual listing ──────────────────────────────────────────────

    private async Task<(PropertyListingRequest? listing, string? exclusionReason)> ScrapeListingAsync(IPage page, string url)
    {
        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        try
        {
            await page.WaitForSelectorAsync(IdealistaLocators.Listing.Title,
                new PageWaitForSelectorOptions { Timeout = 10_000 });
        }
        catch (TimeoutException)
        {
            // Might be blocked — checked below.
        }

        if (await IsBlockedAsync(page))
        {
            Logger.LogError("  Blocked on listing. Solve CAPTCHA, then press Enter.");
            Console.ReadLine();
        }

        // Extract all data in one JS call
        var raw = await page.EvaluateAsync<JsonElement>($@"(() => {{
            const price = document.querySelector('{IdealistaLocators.Listing.Price}')?.innerText || '';
            const title = document.querySelector('{IdealistaLocators.Listing.Title}')?.innerText || '';
            const location = document.querySelector('{IdealistaLocators.Listing.Location}')?.innerText || '';
            const ref = document.querySelector('{IdealistaLocators.Listing.Reference}')?.innerText || '';
            const features = Array.from(
                document.querySelectorAll('{IdealistaLocators.Listing.Features}')
            ).map(li => li.innerText);
            const desc = document.querySelector('{IdealistaLocators.Listing.DescriptionPrimary}')?.innerText
                      || document.querySelector('{IdealistaLocators.Listing.DescriptionFallback}')?.innerText || '';
            const energy = document.querySelector('{IdealistaLocators.Listing.EnergyIcon}');
            const energyClassName = energy ? energy.className : '';
            return {{ price, title, location, ref, features, desc, energyClassName }};
        }})()");

        var title = raw.GetProperty("title").GetString() ?? "";
        var location = raw.GetProperty("location").GetString() ?? "";
        var desc = raw.GetProperty("desc").GetString() ?? "";
        var priceText = raw.GetProperty("price").GetString() ?? "";
        var refCode = raw.GetProperty("ref").GetString() ?? "";
        var energyClassName = raw.GetProperty("energyClassName").GetString() ?? "";
        var features = raw.GetProperty("features").EnumerateArray().Select(f => f.GetString() ?? "").ToList();

        var energyMatch = IdealistaLocators.Listing.EnergyClassPattern.Match(energyClassName);
        var energyClass = energyMatch.Success ? energyMatch.Groups[1].Value.ToUpperInvariant() : null;

        // ── Exclusion checks ──
        var exclusion = CheckExclusion(title, desc, features);
        if (exclusion != null) return (null, exclusion);

        // ── Parse fields ──
        var listing = new PropertyListingRequest
        {
            SourceName = "Idealista",
            SourceUrl = NormalizeUrl(url),
            Country = "Portugal",
            District = "Faro",
            PropertyType = PropertyType.Apartment,
            ListingSnapshot = new ListingSnapshotRequest
            {
                Status = ListingStatus.Active,
                SnapshotDateUtc = DateTime.UtcNow
            }
        };

        // Price
        var priceMatch = IdealistaLocators.MiscPatterns.Price.Match(priceText);
        if (priceMatch.Success)
        {
            var priceVal = decimal.Parse(priceMatch.Value.Replace(".", ""), CultureInfo.InvariantCulture);
            listing.ListingSnapshot.Price = priceVal;
        }

        // Location (e.g. "Torralta, Lagos Cidade, Lagos")
        var locParts = location.Split(',').Select(s => s.Trim()).ToList();
        listing.Municipality = locParts.LastOrDefault() ?? "";
        if (locParts.Count >= 3)
        {
            listing.Zone = locParts[0];
            listing.Town = locParts[1];
        }
        else if (locParts.Count == 2)
        {
            listing.Zone = null;
            listing.Town = locParts[0];
        }
        else if (locParts.Count == 1)
        {
            listing.Town = locParts[0];
        }

        // Features parsing
        ParseFeatures(listing, features);

        // Area validation (required — skip if missing)
        if (listing.AreaM2 == 0)
            return (null, "No usable area stated");

        // Price per m2
        if (listing.ListingSnapshot.Price > 0 && listing.AreaM2 > 0)
            listing.ListingSnapshot.PricePerM2 = Math.Round(listing.ListingSnapshot.Price / listing.AreaM2, 2);

        // Energy certificate
        if (!string.IsNullOrEmpty(energyClass))
            listing.EnergyCertificate = energyClass;

        // Condition from text (photos can't be analyzed programmatically)
        listing.Condition = ParseCondition(features, desc);

        // Furnished detection
        if (!listing.IsFurnished)
            listing.IsFurnished = desc.Contains(IdealistaLocators.FeatureKeywords.Furnished, StringComparison.OrdinalIgnoreCase)
                               || desc.Contains(IdealistaLocators.FeatureKeywords.Equipped, StringComparison.OrdinalIgnoreCase);

        // Strip contact info from description for rawText
        var cleanDesc = StripContactInfo(desc);

        // ── Coordinates ──
        string coordNote = "no map";
        try
        {
            var (lat, lng, precision) = await GetCoordinatesAsync(page, url);
            listing.Latitude = lat;
            listing.Longitude = lng;
            coordNote = $"coords {precision}";
        }
        catch (Exception ex)
        {
            coordNote = $"coord error: {ex.Message}";
        }

        // Build notes
        var notes = new List<string>();
        notes.Add($"Ref: {refCode}");
        notes.Add($"Area basis: {(features.Any(f => f.Contains("úteis")) ? "úteis" : "bruta")}");
        notes.Add(coordNote);
        notes.Add($"Condition: {listing.Condition} (text-based, no photo analysis)");
        if (listing.Bathrooms == 0) notes.Add("bathrooms not stated, defaulted to 0");

        // Beach distance mention
        var beachMatch = IdealistaLocators.MiscPatterns.BeachDistanceMeters.Match(desc);
        if (beachMatch.Success) notes.Add($"Beach distance mentioned: {beachMatch.Value}");
        var beachMinMatch = IdealistaLocators.MiscPatterns.BeachDistanceMinutes.Match(desc);
        if (beachMinMatch.Success) notes.Add($"Beach distance mentioned: {beachMinMatch.Value}");

        listing.Notes = string.Join(" | ", notes);

        // Append rawText to notes (truncated if very long) so it's preserved
        if (!string.IsNullOrWhiteSpace(cleanDesc))
        {
            var truncated = cleanDesc.Length > 1500 ? cleanDesc[..1500] + "..." : cleanDesc;
            listing.Notes += $"\n---RAW---\n{truncated}";
        }

        return (listing, null);
    }

    // ── Coordinate extraction ───────────────────────────────────────────

    private async Task<(decimal? lat, decimal? lng, string precision)> GetCoordinatesAsync(IPage page, string listingUrl)
    {
        var mapUrl = listingUrl.TrimEnd('/') + "/mapa";
        await page.GotoAsync(mapUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 20_000
        });

        // Wait for map tiles and footer links to render
        await Task.Delay(10_000);

        // Check if the advertiser opted out of showing an exact pin
        var isApproximate = await page.EvaluateAsync<bool>(
            $"document.body.innerText.includes('{IdealistaLocators.Map.ApproximateLocationMarker}')");

        // Pull coordinates from any Google Maps link's href (always encodes "@lat,lng"),
        // independent of that link's visible text or the page's rendering language.
        var hrefs = await page.EvaluateAsync<string[]>(
            "Array.from(document.querySelectorAll('a')).map(a => a.href)");

        var coordinateHref = hrefs.FirstOrDefault(href =>
            IdealistaLocators.Map.GoogleMapsHrefPattern.IsMatch(href) &&
            IdealistaLocators.Map.CoordinatesInHrefPattern.IsMatch(href));

        if (coordinateHref != null)
        {
            var match = IdealistaLocators.Map.CoordinatesInHrefPattern.Match(coordinateHref);
            var lat = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var lng = decimal.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var precision = isApproximate ? "approximate" : "exact";
            return (Math.Round(lat, 6), Math.Round(lng, 6), precision);
        }

        return (null, null, "none");
    }

    // ── Feature parsing ─────────────────────────────────────────────────

    private void ParseFeatures(PropertyListingRequest listing, List<string> features)
    {
        foreach (var feat in features)
        {
            var f = feat.Trim();

            // Area: prefer úteis over bruta
            var areaUteis = IdealistaLocators.FeaturePatterns.AreaUteis.Match(f);
            var areaBruta = IdealistaLocators.FeaturePatterns.AreaBruta.Match(f);
            var areaPlain = IdealistaLocators.FeaturePatterns.AreaPlain.Match(f);
            if (areaUteis.Success)
                listing.AreaM2 = int.Parse(areaUteis.Groups[1].Value);
            else if (listing.AreaM2 == 0 && areaBruta.Success)
                listing.AreaM2 = int.Parse(areaBruta.Groups[1].Value);
            else if (listing.AreaM2 == 0 && areaPlain.Success && !f.Contains("€"))
                listing.AreaM2 = int.Parse(areaPlain.Groups[1].Value);

            // Typology
            var typoMatch = IdealistaLocators.FeaturePatterns.Typology.Match(f);
            if (typoMatch.Success)
            {
                var tNum = int.Parse(typoMatch.Groups[1].Value);
                if (Enum.IsDefined(typeof(Typology), tNum))
                    listing.Typology = (Typology)tNum;
            }
            if (f.Contains(IdealistaLocators.FeatureKeywords.Studio, StringComparison.OrdinalIgnoreCase))
                listing.Typology = Typology.T0;

            // Bathrooms
            var bathMatch = IdealistaLocators.FeaturePatterns.Bathrooms.Match(f);
            if (bathMatch.Success)
                listing.Bathrooms = int.Parse(bathMatch.Groups[1].Value);

            // Floor
            var floorMatch = IdealistaLocators.FeaturePatterns.Floor.Match(f);
            if (floorMatch.Success)
                listing.Floor = int.Parse(floorMatch.Groups[1].Value);
            if (f.Contains(IdealistaLocators.FeatureKeywords.GroundFloor, StringComparison.OrdinalIgnoreCase)
                || f.Contains(IdealistaLocators.FeatureKeywords.GroundFloorAlt, StringComparison.OrdinalIgnoreCase))
                listing.Floor = 0;

            // Boolean features
            if (f.Contains(IdealistaLocators.FeatureKeywords.Elevator, StringComparison.OrdinalIgnoreCase))
                listing.HasElevator = !f.Contains(IdealistaLocators.FeatureKeywords.Negation, StringComparison.OrdinalIgnoreCase);
            if (f.Contains(IdealistaLocators.FeatureKeywords.AirConditioning, StringComparison.OrdinalIgnoreCase))
                listing.HasAirConditioning = true;
            if (f.Contains(IdealistaLocators.FeatureKeywords.SwimmingPool, StringComparison.OrdinalIgnoreCase))
                listing.HasSwimmingPool = true;
            if (f.Contains(IdealistaLocators.FeatureKeywords.Garage, StringComparison.OrdinalIgnoreCase))
                listing.HasGarage = true;
            if (f.Contains(IdealistaLocators.FeatureKeywords.Parking, StringComparison.OrdinalIgnoreCase)
                || f.Contains(IdealistaLocators.FeatureKeywords.ParkingAlt, StringComparison.OrdinalIgnoreCase))
                listing.HasParking = true;
            if (f.Contains(IdealistaLocators.FeatureKeywords.Terrace, StringComparison.OrdinalIgnoreCase))
                listing.HasTerrace = true;
            if (f.Contains(IdealistaLocators.FeatureKeywords.Balcony, StringComparison.OrdinalIgnoreCase))
                listing.BalconyCount = Math.Max(listing.BalconyCount, 1);
            if (f.Contains(IdealistaLocators.FeatureKeywords.Furnished, StringComparison.OrdinalIgnoreCase))
                listing.IsFurnished = true;
            if (f.Contains(IdealistaLocators.FeatureKeywords.SeaView, StringComparison.OrdinalIgnoreCase)
                || f.Contains(IdealistaLocators.FeatureKeywords.SeaViewAlt, StringComparison.OrdinalIgnoreCase))
                listing.HasSeaView = true;

            // Construction year
            var yearMatch = IdealistaLocators.FeaturePatterns.ConstructionYear.Match(f);
            if (yearMatch.Success)
                listing.ConstructionYear = int.Parse(yearMatch.Groups[1].Value);
        }
    }

    private PropertyCondition ParseCondition(List<string> features, string description)
    {
        var all = string.Join(" ", features) + " " + description;

        if (IdealistaLocators.ConditionPatterns.NewBuild.IsMatch(all))
            return PropertyCondition.NewBuild;
        if (IdealistaLocators.ConditionPatterns.Renovated.IsMatch(all))
            return PropertyCondition.Renovated;
        if (IdealistaLocators.ConditionPatterns.Good.IsMatch(all))
            return PropertyCondition.Good;
        if (IdealistaLocators.ConditionPatterns.Used.IsMatch(all))
            return PropertyCondition.Used;
        if (IdealistaLocators.ConditionPatterns.NeedsRenovation.IsMatch(all))
            return PropertyCondition.NeedsRenovation;

        return PropertyCondition.Unknown;
    }

    // ── Exclusion filter ────────────────────────────────────────────────

    private string? CheckExclusion(string title, string description, List<string> features)
    {
        var all = (title + " " + description + " " + string.Join(" ", features)).ToLowerInvariant();

        // Not an apartment
        if (IdealistaLocators.ExclusionPatterns.NotApartment.IsMatch(all))
            return "Not an apartment";

        // Trespass / business transfer
        if (all.Contains(IdealistaLocators.ExclusionKeywords.Trespasse))
            return "Trespasse (business transfer)";

        // Usufruct / bare ownership
        if (IdealistaLocators.ExclusionPatterns.Usufruct.IsMatch(all))
            return "Usufruct or bare ownership";

        // Timeshare / fractional
        if (IdealistaLocators.ExclusionPatterns.Timeshare.IsMatch(all))
            return "Timeshare or fractional ownership";

        // Auction / judicial
        if (IdealistaLocators.ExclusionPatterns.Auction.IsMatch(all))
            return "Auction or judicial sale";

        // Rental mislisted
        if (IdealistaLocators.ExclusionPatterns.RentalListing.IsMatch(all)
            && !all.Contains(IdealistaLocators.FeatureKeywords.TouristRentalException))
            return "Rental listing, not a sale";

        // Tenanted / occupied
        if (IdealistaLocators.ExclusionPatterns.Tenanted.IsMatch(all))
            return "Tenanted or occupied";

        return null;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    public int GetNextPageNumber()
    {
        if (!Directory.Exists(_jsonFolder))
            return 1;

        var files = Directory.GetFiles(_jsonFolder, "*.json");
        int maxPage = 0;

        foreach (var file in files)
        {
            var match = IdealistaLocators.MiscPatterns.PageNumberInFilename.Match(Path.GetFileName(file));
            if (match.Success)
            {
                var pageNum = int.Parse(match.Groups[1].Value);
                if (pageNum > maxPage) maxPage = pageNum;
            }
        }

        return maxPage + 1;
    }

    /// <summary>
    /// Reads every JSON file already saved in the output folder and collects their SourceUrls,
    /// so a listing already captured in a prior run is skipped instead of re-visited and
    /// re-written — pagination on Idealista shifts daily ("atualizado-asc"), so the same
    /// listing can reappear on a different page number across runs.
    /// </summary>
    private HashSet<string> LoadAlreadyScrapedSourceUrls()
    {
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(_jsonFolder))
            return urls;

        foreach (var file in Directory.GetFiles(_jsonFolder, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var listings = JsonSerializer.Deserialize<List<PropertyListingRequest>>(json, _jsonOptions);
                if (listings == null) continue;

                foreach (var listing in listings)
                    if (!string.IsNullOrEmpty(listing.SourceUrl))
                        urls.Add(NormalizeUrl(listing.SourceUrl));
            }
            catch (JsonException ex)
            {
                Logger.LogError($"Could not parse existing file {Path.GetFileName(file)} for dedup: {ex.Message}");
            }
        }

        return urls;
    }

    private static string NormalizeUrl(string url) => url.TrimEnd('/') + "/";

    private async Task RandomDelay(int minMs, int maxMs)
    {
        var delay = _rng.Next(minMs, maxMs);
        Logger.LogInformation($"  Waiting {delay / 1000}s...");
        await Task.Delay(delay);
    }

    private async Task<bool> IsBlockedAsync(IPage page)
    {
        var content = await page.ContentAsync();
        return IdealistaLocators.BlockMarkers.Values.Any(marker =>
            content.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private string StripContactInfo(string text)
    {
        var result = IdealistaLocators.MiscPatterns.PhoneNumber.Replace(text, "[REDACTED]");
        result = IdealistaLocators.MiscPatterns.Email.Replace(result, "[REDACTED]");
        return result;
    }
}
