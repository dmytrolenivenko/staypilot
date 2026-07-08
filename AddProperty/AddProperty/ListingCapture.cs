using System.Globalization;
using System.Text.Json;

namespace AddProperty;

/// <summary>
/// Turns the raw text the browser extension captures into validated <see cref="PropertyListingRequest"/>
/// objects and saves them to today's JSON file. This is the one class with actual business logic —
/// everything else in the project is plumbing (the HTTP listener, the upload step) or plain data shapes.
///
/// Reliability choice: every captured ad is written to disk within moments of being received,
/// not held in memory until you finish browsing. If AddProperty crashes or the terminal closes
/// mid-session, you lose at most the one capture in flight — never the whole session.
/// </summary>
public class ListingCapture
{
    private readonly string _jsonFolder;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _lock = new();

    // SourceUrls already saved (any day), so re-capturing the same listing is a no-op instead
    // of a duplicate row. Loaded once at startup, kept in memory for the rest of the session.
    private readonly HashSet<string> _alreadyCapturedUrls;

    public ListingCapture(string jsonFolder, JsonSerializerOptions jsonOptions)
    {
        _jsonFolder = jsonFolder;
        _jsonOptions = jsonOptions;
        Directory.CreateDirectory(_jsonFolder);
        _alreadyCapturedUrls = LoadAllCapturedUrls();
    }

    // ── Entry points — called by CaptureServer as extension messages arrive ──────────────

    /// <summary>Handles one captured ad page. The browser extension only ever sends this once
    /// it already has coordinates in hand — content-ad.js asks background.js for the /mapa
    /// coordinates first and waits for them (or a genuine give-up) before extracting or sending
    /// anything — so building, validating, and saving all happen in one shot here.</summary>
    public string HandleCapturedAd(string dataJson)
    {
        PastedAdFields? fields;
        try
        {
            fields = JsonSerializer.Deserialize<PastedAdFields>(dataJson, _jsonOptions);
        }
        catch (JsonException ex)
        {
            return $"Could not parse captured ad JSON: {ex.Message}";
        }

        if (fields == null || string.IsNullOrWhiteSpace(fields.SourceUrl))
            return "Ignored: captured ad JSON has no sourceUrl.";

        var url = NormalizeUrl(fields.SourceUrl);

        lock (_lock)
        {
            if (_alreadyCapturedUrls.Contains(url))
                return $"SKIPPED (already captured): {url}";

            var (listing, exclusionReason) = BuildListingFromFields(url, fields);
            if (exclusionReason != null)
                return $"EXCLUDED: {url} — {exclusionReason}";

            if (!ApplyCoordinates(listing!, fields.MapHref, fields.Approximate))
                return $"SKIPPED (no usable coordinates on ad message): {url} (mapHref: {fields.MapHref ?? "null"})";

            var (listings, filePath) = LoadTodayFile();
            listings.Add(listing!);
            SaveTodayFile(listings, filePath);
            _alreadyCapturedUrls.Add(url);

            return $"KEPT: {url} — {listing!.Typology} {listing.AreaM2}m² {listing.ListingSnapshot.Price}€";
        }
    }

    // ── Turning raw fields into a listing ────────────────────────────────

    private (PropertyListingRequest? listing, string? exclusionReason) BuildListingFromFields(string url, PastedAdFields fields)
    {
        var title = fields.Title ?? "";
        var location = fields.Location ?? "";
        var desc = fields.Desc ?? "";
        var priceText = fields.Price ?? "";
        var refCode = fields.Ref ?? "";
        var features = fields.Features ?? new List<string>();

        var exclusion = CheckExclusion(title, desc, features);
        if (exclusion != null) return (null, exclusion);

        var listing = new PropertyListingRequest
        {
            SourceName = "Idealista",
            SourceUrl = url,
            Country = "Portugal",
            District = "Faro",
            PropertyType = PropertyType.Apartment,
            ListingSnapshot = new ListingSnapshotRequest
            {
                Status = ListingStatus.Active,
                SnapshotDateUtc = DateTime.UtcNow
            }
        };

        // Price, e.g. "250.000 €" -> 250000
        var priceMatch = IdealistaLocators.MiscPatterns.Price.Match(priceText);
        if (priceMatch.Success)
            listing.ListingSnapshot.Price = decimal.Parse(priceMatch.Value.Replace(".", ""), CultureInfo.InvariantCulture);

        // Location, e.g. "Torralta, Lagos Cidade, Lagos" -> Zone / Town / Municipality
        var locParts = location.Split(',').Select(s => s.Trim()).ToList();
        listing.Municipality = locParts.LastOrDefault() ?? "";
        if (locParts.Count >= 3) { listing.Zone = locParts[0]; listing.Town = locParts[1]; }
        else if (locParts.Count == 2) { listing.Zone = null; listing.Town = locParts[0]; }
        else if (locParts.Count == 1) { listing.Town = locParts[0]; }

        ParseFeatures(listing, features);

        // Area is required — if we couldn't find one, there's nothing useful to save.
        if (listing.AreaM2 == 0)
            return (null, "No usable area stated");

        if (listing.ListingSnapshot.Price > 0 && listing.AreaM2 > 0)
            listing.ListingSnapshot.PricePerM2 = Math.Round(listing.ListingSnapshot.Price / listing.AreaM2, 2);

        var energyMatch = IdealistaLocators.EnergyClassPattern.Match(fields.EnergyClassName ?? "");
        if (energyMatch.Success)
            listing.EnergyCertificate = energyMatch.Groups[1].Value.ToUpperInvariant();

        listing.Condition = ParseCondition(features, desc);

        if (!listing.IsFurnished)
            listing.IsFurnished = desc.Contains(IdealistaLocators.FeatureKeywords.Furnished, StringComparison.OrdinalIgnoreCase)
                               || desc.Contains(IdealistaLocators.FeatureKeywords.Equipped, StringComparison.OrdinalIgnoreCase);

        listing.Notes = BuildNotes(listing, refCode, features, desc);

        return (listing, null);
    }

    /// <summary>Fills in Latitude/Longitude from a captured map href and appends a note about it.
    /// Returns whether it actually found usable coordinates. In practice this should always
    /// succeed — content-ad.js only ever sends an "ad" message once background.js has already
    /// confirmed a usable map link exists — but stay defensive rather than assume it.</summary>
    private bool ApplyCoordinates(PropertyListingRequest listing, string? mapHref, bool approximate)
    {
        if (mapHref == null || !IdealistaLocators.Map.CoordinatesInHrefPattern.IsMatch(mapHref))
        {
            var missingNote = "coords: no location shown on page";
            listing.Notes = string.IsNullOrEmpty(listing.Notes) ? missingNote : $"{listing.Notes} | {missingNote}";
            return false;
        }

        var match = IdealistaLocators.Map.CoordinatesInHrefPattern.Match(mapHref);
        listing.Latitude = Math.Round(decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), 6);
        listing.Longitude = Math.Round(decimal.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), 6);

        var coordNote = approximate ? "coords approximate" : "coords exact";
        listing.Notes = string.IsNullOrEmpty(listing.Notes) ? coordNote : $"{listing.Notes} | {coordNote}";
        return true;
    }

    /// <summary>Builds the free-text Notes field: a short structured summary, then the raw description.</summary>
    private string BuildNotes(PropertyListingRequest listing, string refCode, List<string> features, string desc)
    {
        var notes = new List<string>
        {
            $"Ref: {refCode}",
            $"Area basis: {(features.Any(f => f.Contains("úteis")) ? "úteis" : "bruta")}",
            $"Condition: {listing.Condition} (text-based, no photo analysis)"
        };
        if (listing.Bathrooms == 0) notes.Add("bathrooms not stated, defaulted to 0");

        var beachMatch = IdealistaLocators.MiscPatterns.BeachDistanceMeters.Match(desc);
        if (beachMatch.Success) notes.Add($"Beach distance mentioned: {beachMatch.Value}");
        var beachMinMatch = IdealistaLocators.MiscPatterns.BeachDistanceMinutes.Match(desc);
        if (beachMinMatch.Success) notes.Add($"Beach distance mentioned: {beachMinMatch.Value}");

        var notesLine = string.Join(" | ", notes);

        var cleanDesc = StripContactInfo(desc);
        if (!string.IsNullOrWhiteSpace(cleanDesc))
        {
            var truncated = cleanDesc.Length > 1500 ? cleanDesc[..1500] + "..." : cleanDesc;
            notesLine += $"\n---RAW---\n{truncated}";
        }

        return notesLine;
    }

    private void ParseFeatures(PropertyListingRequest listing, List<string> features)
    {
        foreach (var feat in features)
        {
            var f = feat.Trim();

            // Area: prefer úteis (usable) over bruta (gross) over an unlabeled number.
            var areaUteis = IdealistaLocators.FeaturePatterns.AreaUteis.Match(f);
            var areaBruta = IdealistaLocators.FeaturePatterns.AreaBruta.Match(f);
            var areaPlain = IdealistaLocators.FeaturePatterns.AreaPlain.Match(f);
            if (areaUteis.Success)
                listing.AreaM2 = int.Parse(areaUteis.Groups[1].Value);
            else if (listing.AreaM2 == 0 && areaBruta.Success)
                listing.AreaM2 = int.Parse(areaBruta.Groups[1].Value);
            else if (listing.AreaM2 == 0 && areaPlain.Success && !f.Contains("€"))
                listing.AreaM2 = int.Parse(areaPlain.Groups[1].Value);

            var typoMatch = IdealistaLocators.FeaturePatterns.Typology.Match(f);
            if (typoMatch.Success)
            {
                var tNum = int.Parse(typoMatch.Groups[1].Value);
                if (Enum.IsDefined(typeof(Typology), tNum))
                    listing.Typology = (Typology)tNum;
            }
            if (f.Contains(IdealistaLocators.FeatureKeywords.Studio, StringComparison.OrdinalIgnoreCase))
                listing.Typology = Typology.T0;

            var bathMatch = IdealistaLocators.FeaturePatterns.Bathrooms.Match(f);
            if (bathMatch.Success)
                listing.Bathrooms = int.Parse(bathMatch.Groups[1].Value);

            var floorMatch = IdealistaLocators.FeaturePatterns.Floor.Match(f);
            if (floorMatch.Success)
                listing.Floor = int.Parse(floorMatch.Groups[1].Value);
            if (f.Contains(IdealistaLocators.FeatureKeywords.GroundFloor, StringComparison.OrdinalIgnoreCase)
                || f.Contains(IdealistaLocators.FeatureKeywords.GroundFloorAlt, StringComparison.OrdinalIgnoreCase))
                listing.Floor = 0;

            if (f.Contains(IdealistaLocators.FeatureKeywords.Elevator, StringComparison.OrdinalIgnoreCase))
                listing.HasElevator = !f.Contains(IdealistaLocators.FeatureKeywords.Negation, StringComparison.OrdinalIgnoreCase);
            if (f.Contains(IdealistaLocators.FeatureKeywords.AirConditioning, StringComparison.OrdinalIgnoreCase))
                listing.HasAirConditioning = !f.Contains(IdealistaLocators.FeatureKeywords.Negation, StringComparison.OrdinalIgnoreCase);
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

            var yearMatch = IdealistaLocators.FeaturePatterns.ConstructionYear.Match(f);
            if (yearMatch.Success)
                listing.ConstructionYear = int.Parse(yearMatch.Groups[1].Value);
        }
    }

    private PropertyCondition ParseCondition(List<string> features, string description)
    {
        var all = string.Join(" ", features) + " " + description;

        if (IdealistaLocators.ConditionPatterns.NewBuild.IsMatch(all)) return PropertyCondition.NewBuild;
        if (IdealistaLocators.ConditionPatterns.Renovated.IsMatch(all)) return PropertyCondition.Renovated;
        if (IdealistaLocators.ConditionPatterns.Good.IsMatch(all)) return PropertyCondition.Good;
        if (IdealistaLocators.ConditionPatterns.Used.IsMatch(all)) return PropertyCondition.Used;
        if (IdealistaLocators.ConditionPatterns.NeedsRenovation.IsMatch(all)) return PropertyCondition.NeedsRenovation;

        return PropertyCondition.Unknown;
    }

    private string? CheckExclusion(string title, string description, List<string> features)
    {
        // Property TYPE and transaction-type words (apartment vs. villa/garage/rental-only)
        // are checked against the TITLE ONLY. Idealista states these plainly in the title
        // ("Apartamento T2 em...", "Moradia T4 em...", "... para arrendar"), whereas checking
        // the full text — including the amenities feature list — false-positives constantly:
        // an apartment that simply HAS a garage as an amenity contains the word "garagem" just
        // as much as a listing that IS a garage, and a for-sale unit's description mentioning
        // rental income as an investment angle contains "arrendamento" just as much as an
        // actual rental listing. The title doesn't have that noise.
        var titleText = title.ToLowerInvariant();

        if (IdealistaLocators.ExclusionPatterns.NotApartment.IsMatch(titleText))
            return "Not an apartment";
        if (IdealistaLocators.ExclusionPatterns.RentalListing.IsMatch(titleText)
            && !titleText.Contains(IdealistaLocators.FeatureKeywords.TouristRentalException))
            return "Rental listing, not a sale";

        // Legal/ownership disclosures genuinely only show up in the description, not the
        // title, so these still need the full text — they're not common amenity/investment
        // vocabulary, so they're far less prone to the same false-positive problem.
        var all = (title + " " + description + " " + string.Join(" ", features)).ToLowerInvariant();

        if (all.Contains(IdealistaLocators.ExclusionKeywords.Trespasse))
            return "Trespasse (business transfer)";
        if (IdealistaLocators.ExclusionPatterns.Usufruct.IsMatch(all))
            return "Usufruct or bare ownership";
        if (IdealistaLocators.ExclusionPatterns.Timeshare.IsMatch(all))
            return "Timeshare or fractional ownership";
        if (IdealistaLocators.ExclusionPatterns.Auction.IsMatch(all))
            return "Auction or judicial sale";
        if (IdealistaLocators.ExclusionPatterns.Tenanted.IsMatch(all))
            return "Tenanted or occupied";

        return null;
    }

    private string StripContactInfo(string text)
    {
        var result = IdealistaLocators.MiscPatterns.PhoneNumber.Replace(text, "[REDACTED]");
        result = IdealistaLocators.MiscPatterns.Email.Replace(result, "[REDACTED]");
        return result;
    }

    // ── File I/O ──────────────────────────────────────────────────────────
    // Everything captured today lands in one "{yyyy-MM-dd}.json" file, read-modified-and
    // rewritten on every save. Fine at this scale (dozens of listings, not thousands).

    private (List<PropertyListingRequest> listings, string filePath) LoadTodayFile()
    {
        var filePath = Path.Combine(_jsonFolder, $"{DateTime.UtcNow:yyyy-MM-dd}.json");
        if (!File.Exists(filePath))
            return (new List<PropertyListingRequest>(), filePath);

        try
        {
            var listings = JsonSerializer.Deserialize<List<PropertyListingRequest>>(File.ReadAllText(filePath), _jsonOptions);
            return (listings ?? new List<PropertyListingRequest>(), filePath);
        }
        catch (JsonException)
        {
            return (new List<PropertyListingRequest>(), filePath);
        }
    }

    private void SaveTodayFile(List<PropertyListingRequest> listings, string filePath)
    {
        var json = JsonSerializer.Serialize(listings, new JsonSerializerOptions(_jsonOptions) { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    /// <summary>Scans every JSON file already in the folder (any day) for SourceUrls, for dedup.</summary>
    private HashSet<string> LoadAllCapturedUrls()
    {
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(_jsonFolder, "*.json"))
        {
            try
            {
                var listings = JsonSerializer.Deserialize<List<PropertyListingRequest>>(File.ReadAllText(file), _jsonOptions);
                if (listings == null) continue;
                foreach (var listing in listings)
                    if (!string.IsNullOrEmpty(listing.SourceUrl))
                        urls.Add(NormalizeUrl(listing.SourceUrl));
            }
            catch (JsonException) { }
        }
        return urls;
    }

    private static string NormalizeUrl(string url) => url.TrimEnd('/') + "/";
}
