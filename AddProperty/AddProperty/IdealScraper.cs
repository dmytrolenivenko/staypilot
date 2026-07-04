using Microsoft.Playwright;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AddProperty;

/// <summary>
/// Scrapes Idealista Faro/Algarve apartment listings using Playwright in headed mode.
/// Designed for personal-scale use: one browser, human-paced, persistent login.
/// </summary>
public class IdealScraper
{
    private const string SearchUrlBase = "https://www.idealista.pt/comprar-casas/faro-distrito/com-apartamentos";
    private const string SearchUrlSuffix = "?ordem=atualizado-asc";

    private readonly string _jsonFolder;
    private readonly string _profilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Random _rng = new();

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
        // Build URL: page 1 has no /pagina-N/ segment
        var url = pageNum == 1
            ? $"{SearchUrlBase}/{SearchUrlSuffix}"
            : $"{SearchUrlBase}/pagina-{pageNum}{SearchUrlSuffix}";

        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        await Task.Delay(3000);

        if (await IsBlockedAsync(page))
        {
            Logger.LogError("Blocked on search page. Solve CAPTCHA manually, then press Enter.");
            Console.ReadLine();
        }

        // Pause on results page like a human scanning listings
        await RandomDelay(5_000, 12_000);

        // Collect listing URLs
        var listingUrls = await page.EvaluateAsync<string[]>(
            "Array.from(document.querySelectorAll('article.item a.item-link')).map(a => a.href)");

        if (listingUrls.Length == 0)
        {
            Logger.LogError("No listings found on page. Might be blocked or end of results.");
            return null;
        }

        Logger.LogInformation($"Found {listingUrls.Length} listings on page {pageNum}.");

        var kept = new List<PropertyListingRequest>();
        var excluded = new List<(string url, string reason)>();

        foreach (var listingUrl in listingUrls)
        {
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
        Logger.LogInformation($"  Total: {listingUrls.Length}, Kept: {kept.Count}, Excluded: {excluded.Count}");
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
        await Task.Delay(2000);

        if (await IsBlockedAsync(page))
        {
            Logger.LogError("  Blocked on listing. Solve CAPTCHA, then press Enter.");
            Console.ReadLine();
        }

        // Extract all data in one JS call
        var raw = await page.EvaluateAsync<JsonElement>(@"(() => {
            const price = document.querySelector('.info-data-price')?.innerText || '';
            const title = document.querySelector('h1')?.innerText || '';
            const location = document.querySelector('.main-info__title-minor')?.innerText || '';
            const ref = document.querySelector('.txt-ref')?.innerText || '';
            const features = Array.from(
                document.querySelectorAll('.details-property-feature-one li, .details-property_features li')
            ).map(li => li.innerText);
            const desc = document.querySelector('.comment')?.innerText
                      || document.querySelector('.adCommentsLanguage')?.innerText || '';
            const energy = document.querySelector('[class*=""icon-energy""]');
            const energyClass = energy
                ? (energy.className.match(/icon-energy-(\w)/)?.[1] || '').toUpperCase()
                : '';
            return { price, title, location, ref, features, desc, energyClass };
        })()");

        var title = raw.GetProperty("title").GetString() ?? "";
        var location = raw.GetProperty("location").GetString() ?? "";
        var desc = raw.GetProperty("desc").GetString() ?? "";
        var priceText = raw.GetProperty("price").GetString() ?? "";
        var refCode = raw.GetProperty("ref").GetString() ?? "";
        var energyClass = raw.GetProperty("energyClass").GetString();
        var features = raw.GetProperty("features").EnumerateArray().Select(f => f.GetString() ?? "").ToList();

        // ── Exclusion checks ──
        var exclusion = CheckExclusion(title, desc, features);
        if (exclusion != null) return (null, exclusion);

        // ── Parse fields ──
        var listing = new PropertyListingRequest
        {
            SourceName = "Idealista",
            SourceUrl = url.TrimEnd('/') + "/",
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
        var priceMatch = Regex.Match(priceText, @"[\d.]+");
        if (priceMatch.Success)
        {
            var priceVal = decimal.Parse(priceMatch.Value.Replace(".", ""));
            listing.ListingSnapshot.Price = priceVal;
        }

        // Location (e.g. "Torralta, Lagos Cidade, Lagos")
        var locParts = location.Split(',').Select(s => s.Trim()).ToList();
        if (locParts.Count >= 1) listing.Town = locParts[0];
        if (locParts.Count >= 2) listing.Zone = locParts.Count > 2 ? locParts[0] : null;
        listing.Municipality = locParts.LastOrDefault() ?? "";
        if (locParts.Count >= 2) listing.Town = locParts.Count > 2 ? locParts[1] : locParts[0];

        // Features parsing
        ParseFeatures(listing, features);

        // Area validation (required — skip if missing)
        if (listing.AreaM2 == 0)
            return (null, "No usable area stated");

        // Price per m2
        if (listing.ListingSnapshot.Price > 0 && listing.AreaM2 > 0)
            listing.ListingSnapshot.PricePerM2 = Math.Round(listing.ListingSnapshot.Price / listing.AreaM2, 2);

        // Energy certificate
        if (!string.IsNullOrEmpty(energyClass) && energyClass.Length == 1)
            listing.EnergyCertificate = energyClass;

        // Condition from text (photos can't be analyzed programmatically)
        listing.Condition = ParseCondition(features, desc);

        // Furnished detection
        if (!listing.IsFurnished)
            listing.IsFurnished = desc.Contains("mobilado", StringComparison.OrdinalIgnoreCase)
                               || desc.Contains("equipado", StringComparison.OrdinalIgnoreCase);

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
        var beachMatch = Regex.Match(desc, @"(\d+)\s*(?:m|metros?)\s*(?:da|do|das?)\s*praia", RegexOptions.IgnoreCase);
        if (beachMatch.Success) notes.Add($"Beach distance mentioned: {beachMatch.Value}");
        var beachMinMatch = Regex.Match(desc, @"(\d+)\s*min.*praia", RegexOptions.IgnoreCase);
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

        // Check if approximate location
        var isApproximate = await page.EvaluateAsync<bool>(
            "document.body.innerText.includes('não indicou a localização exata')");

        // Extract coordinates from "Report a map error" link
        var coords = await page.EvaluateAsync<JsonElement>(@"(() => {
            const a = Array.from(document.querySelectorAll('a'))
                .find(x => /report a map error/i.test(x.textContent || ''));
            if (a) {
                const m = a.href.match(/@(-?\d{1,3}\.\d{3,8}),(-?\d{1,3}\.\d{3,8})/);
                if (m) return { lat: m[1], lng: m[2] };
            }
            return { lat: null, lng: null };
        })()");

        var latStr = coords.GetProperty("lat").GetString();
        var lngStr = coords.GetProperty("lng").GetString();

        if (latStr != null && lngStr != null)
        {
            var lat = decimal.Parse(latStr, System.Globalization.CultureInfo.InvariantCulture);
            var lng = decimal.Parse(lngStr, System.Globalization.CultureInfo.InvariantCulture);
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
            var areaUteis = Regex.Match(f, @"(\d+)\s*m²\s*úteis");
            var areaBruta = Regex.Match(f, @"(\d+)\s*m²\s*área\s*bruta");
            var areaPlain = Regex.Match(f, @"(\d+)\s*m²");
            if (areaUteis.Success)
                listing.AreaM2 = int.Parse(areaUteis.Groups[1].Value);
            else if (listing.AreaM2 == 0 && areaBruta.Success)
                listing.AreaM2 = int.Parse(areaBruta.Groups[1].Value);
            else if (listing.AreaM2 == 0 && areaPlain.Success && !f.Contains("€"))
                listing.AreaM2 = int.Parse(areaPlain.Groups[1].Value);

            // Typology
            var typoMatch = Regex.Match(f, @"^T(\d+)$");
            if (typoMatch.Success)
            {
                var tNum = int.Parse(typoMatch.Groups[1].Value);
                if (Enum.IsDefined(typeof(Typology), tNum))
                    listing.Typology = (Typology)tNum;
            }
            if (f.Contains("Estúdio", StringComparison.OrdinalIgnoreCase))
                listing.Typology = Typology.T0;

            // Bathrooms
            var bathMatch = Regex.Match(f, @"(\d+)\s*casa", RegexOptions.IgnoreCase);
            if (bathMatch.Success)
                listing.Bathrooms = int.Parse(bathMatch.Groups[1].Value);

            // Floor
            var floorMatch = Regex.Match(f, @"(\d+)º\s*andar", RegexOptions.IgnoreCase);
            if (floorMatch.Success)
                listing.Floor = int.Parse(floorMatch.Groups[1].Value);
            if (f.Contains("Rés-do-chão", StringComparison.OrdinalIgnoreCase) || f.Contains("R/C", StringComparison.OrdinalIgnoreCase))
                listing.Floor = 0;

            // Boolean features
            if (f.Contains("elevador", StringComparison.OrdinalIgnoreCase))
                listing.HasElevator = !f.Contains("sem", StringComparison.OrdinalIgnoreCase);
            if (f.Contains("Ar condicionado", StringComparison.OrdinalIgnoreCase))
                listing.HasAirConditioning = true;
            if (f.Contains("Piscina", StringComparison.OrdinalIgnoreCase))
                listing.HasSwimmingPool = true;
            if (f.Contains("garagem", StringComparison.OrdinalIgnoreCase))
                listing.HasGarage = true;
            if (f.Contains("estacionamento", StringComparison.OrdinalIgnoreCase) || f.Contains("parking", StringComparison.OrdinalIgnoreCase))
                listing.HasParking = true;
            if (f.Contains("Terraço", StringComparison.OrdinalIgnoreCase) || f.Contains("Terraço", StringComparison.OrdinalIgnoreCase))
                listing.HasTerrace = true;
            if (f.Contains("Varanda", StringComparison.OrdinalIgnoreCase))
                listing.BalconyCount = Math.Max(listing.BalconyCount, 1);
            if (f.Contains("mobilado", StringComparison.OrdinalIgnoreCase))
                listing.IsFurnished = true;
            if (f.Contains("vista mar", StringComparison.OrdinalIgnoreCase) || f.Contains("vista de mar", StringComparison.OrdinalIgnoreCase))
                listing.HasSeaView = true;

            // Construction year
            var yearMatch = Regex.Match(f, @"Construído em (\d{4})", RegexOptions.IgnoreCase);
            if (yearMatch.Success)
                listing.ConstructionYear = int.Parse(yearMatch.Groups[1].Value);
        }
    }

    private PropertyCondition ParseCondition(List<string> features, string description)
    {
        var all = string.Join(" ", features) + " " + description;

        if (Regex.IsMatch(all, @"nova construção|empreendimento.*terminado|obra nova", RegexOptions.IgnoreCase))
            return PropertyCondition.NewBuild;
        if (Regex.IsMatch(all, @"remodelad|renovad|reabilitad", RegexOptions.IgnoreCase))
            return PropertyCondition.Renovated;
        if (Regex.IsMatch(all, @"bom estado", RegexOptions.IgnoreCase))
            return PropertyCondition.Good;
        if (Regex.IsMatch(all, @"segunda mão|usado", RegexOptions.IgnoreCase))
            return PropertyCondition.Used;
        if (Regex.IsMatch(all, @"para recuperar|para remodelar|a necessitar|ruína", RegexOptions.IgnoreCase))
            return PropertyCondition.NeedsRenovation;

        return PropertyCondition.Unknown;
    }

    // ── Exclusion filter ────────────────────────────────────────────────

    private string? CheckExclusion(string title, string description, List<string> features)
    {
        var all = (title + " " + description + " " + string.Join(" ", features)).ToLowerInvariant();

        // Not an apartment
        if (Regex.IsMatch(all, @"\b(moradia|quinta|terreno|garagem|loja|escritório|armazém)\b"))
            return "Not an apartment";

        // Trespass / business transfer
        if (all.Contains("trespasse"))
            return "Trespasse (business transfer)";

        // Usufruct / bare ownership
        if (Regex.IsMatch(all, @"usufruto|nua-propriedade|nua propriedade|direito de superfície"))
            return "Usufruct or bare ownership";

        // Timeshare / fractional
        if (Regex.IsMatch(all, @"multipropriedade|direito de habitação periódica|semanas? por ano"))
            return "Timeshare or fractional ownership";

        // Auction / judicial
        if (Regex.IsMatch(all, @"leilão|venda judicial|penhora|insolvência|hasta pública"))
            return "Auction or judicial sale";

        // Rental mislisted
        if (Regex.IsMatch(all, @"\barrendamento\b|\bpara arrendar\b") && !all.Contains("arrendamento turístico"))
            return "Rental listing, not a sale";

        // Tenanted / occupied
        if (Regex.IsMatch(all, @"\barrendado\b|com inquilino|com contrato em vigor|\bocupado\b"))
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
            var match = Regex.Match(Path.GetFileName(file), @"page(\d+)");
            if (match.Success)
            {
                var pageNum = int.Parse(match.Groups[1].Value);
                if (pageNum > maxPage) maxPage = pageNum;
            }
        }

        return maxPage + 1;
    }

    private async Task RandomDelay(int minMs, int maxMs)
    {
        var delay = _rng.Next(minMs, maxMs);
        Logger.LogInformation($"  Waiting {delay / 1000}s...");
        await Task.Delay(delay);
    }

    private async Task<bool> IsBlockedAsync(IPage page)
    {
        var content = await page.ContentAsync();
        return content.Contains("captcha", StringComparison.OrdinalIgnoreCase)
            || content.Contains("datadome", StringComparison.OrdinalIgnoreCase)
            || content.Contains("blocked", StringComparison.OrdinalIgnoreCase)
            || content.Contains("verify you are human", StringComparison.OrdinalIgnoreCase);
    }

    private string StripContactInfo(string text)
    {
        // Remove phone numbers
        var result = Regex.Replace(text, @"\+?\d[\d\s-]{7,}\d", "[REDACTED]");
        // Remove email addresses
        result = Regex.Replace(result, @"[\w.+-]+@[\w-]+\.[\w.-]+", "[REDACTED]");
        return result;
    }
}
