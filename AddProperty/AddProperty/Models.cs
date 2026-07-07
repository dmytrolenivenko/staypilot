using System.ComponentModel.DataAnnotations;

namespace AddProperty;

// Everything in this file just describes SHAPES of data — no logic lives here.
// PropertyListingRequest/ListingSnapshotRequest/the enums must match the StayPilot API's
// request contract exactly (field names + enum member names). Enum NUMBERS don't need to
// match the API's — the app serializes enums as their string names (see JsonStringEnumConverter
// in Program.cs), so "Apartment" always means Apartment regardless of its underlying number.

/// <summary>What gets POSTed to StayPilot's POST /api/PropertyListing.</summary>
public class PropertyListingRequest
{
    public int? MarketAreaId { get; set; }
    public string? Country { get; set; } = "Portugal";
    public string? District { get; set; } = string.Empty;
    public string? Municipality { get; set; } = string.Empty;
    public string? Town { get; set; } = string.Empty;
    public string? Zone { get; set; }
    public PropertyType PropertyType { get; set; }
    public Typology Typology { get; set; }
    public string SourceName { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SourceUrl { get; set; } = string.Empty;

    public int AreaM2 { get; set; }
    public int Bathrooms { get; set; }
    public int? Floor { get; set; }
    public int? TotalFloors { get; set; }
    public bool? HasElevator { get; set; }
    public bool? HasAirConditioning { get; set; }
    public PropertyCondition Condition { get; set; }
    public int? ConstructionYear { get; set; }
    public int? RenovationYear { get; set; }
    public int BalconyCount { get; set; }
    public bool HasTerrace { get; set; }
    public bool HasGarage { get; set; }
    public bool HasParking { get; set; }
    public bool HasSwimmingPool { get; set; }
    public bool IsFurnished { get; set; }
    public bool HasSeaView { get; set; }
    public bool HasCityView { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? EnergyCertificate { get; set; }
    public string? Notes { get; set; }
    public ListingSnapshotRequest ListingSnapshot { get; set; } = new();
}

/// <summary>The price/status half of a listing — its own table on the API side so price history can be tracked.</summary>
public class ListingSnapshotRequest
{
    public int PropertyListingId { get; set; }
    public decimal Price { get; set; }
    public decimal PricePerM2 { get; set; }
    public ListingStatus Status { get; set; }
    public DateTime SnapshotDateUtc { get; set; } = DateTime.UtcNow;
}

public enum ListingStatus { Sold, Active, PriceChanged }

public enum PropertyCondition { Unknown, NeedsRenovation, Used, Good, Renovated, NewBuild }

public enum PropertyType { Apartment, Villa, House, Land }

public enum Typology { T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10 }

// ── What the browser extension sends ──────────────────────────────────────
// These two match the JSON shape of BrowserExtension/content-ad.js and content-map.js
// exactly. If you change what a content script sends, update the matching class here.

/// <summary>Raw fields read straight off an Idealista ad page by content-ad.js — nothing parsed yet.</summary>
public class PastedAdFields
{
    public string? SourceUrl { get; set; }
    public string? Price { get; set; }
    public string? Title { get; set; }
    public string? Location { get; set; }
    public string? Ref { get; set; }
    public List<string>? Features { get; set; }
    public string? Desc { get; set; }
    public string? EnergyClassName { get; set; }
}

/// <summary>Coordinates read off a listing's /mapa page by content-map.js.</summary>
public class PastedCoordFields
{
    public string? SourceUrl { get; set; }
    public string? MapHref { get; set; }
    public bool Approximate { get; set; }
    public bool TimedOut { get; set; }
}
