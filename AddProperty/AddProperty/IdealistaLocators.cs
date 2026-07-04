using System.Text.RegularExpressions;

namespace AddProperty;

/// <summary>
/// Every CSS selector, regex pattern, and text marker the scraper depends on, in one place.
/// If Idealista changes its markup or wording, this is the only file that should need editing —
/// IdealScraper.cs itself should never need to change for a site-side tweak.
/// </summary>
public static class IdealistaLocators
{
    // ── Search URL construction ──────────────────────────────────────────
    public static class SearchUrl
    {
        public const string Base = "https://www.idealista.pt/comprar-casas/faro-distrito/com-apartamentos";
        public const string Suffix = "?ordem=atualizado-asc";

        public static string ForPage(int pageNum) =>
            pageNum == 1 ? $"{Base}/{Suffix}" : $"{Base}/pagina-{pageNum}{Suffix}";
    }

    // ── Search results page ───────────────────────────────────────────────
    public static class SearchResults
    {
        public const string ListingLinks = "article.item a.item-link";
    }

    // ── Individual listing page ──────────────────────────────────────────
    public static class Listing
    {
        public const string Price = ".info-data-price";
        public const string Title = "h1";
        public const string Location = ".main-info__title-minor";
        public const string Reference = ".txt-ref";
        public const string Features = ".details-property-feature-one li, .details-property_features li";
        public const string DescriptionPrimary = ".comment";
        public const string DescriptionFallback = ".adCommentsLanguage";
        public const string EnergyIcon = "[class*=\"icon-energy\"]";
        public static readonly Regex EnergyClassPattern = new(@"icon-energy-(\w)", RegexOptions.Compiled);
    }

    // ── Map page (listingUrl + "/mapa") ───────────────────────────────────
    public static class Map
    {
        // The browser context runs with Locale = pt-PT, so all page text is Portuguese.
        public const string ApproximateLocationMarker = "não indicou a localização exata";

        // Coordinates are read from the href of any Google Maps link on the page (they always
        // encode "@lat,lng"), never from the link's visible text — text is language/wording
        // dependent and breaks the moment Idealista rephrases it or renders a different locale.
        public static readonly Regex GoogleMapsHrefPattern =
            new(@"(google\.com/maps|maps\.google)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex CoordinatesInHrefPattern =
            new(@"@(-?\d{1,3}\.\d{3,8}),(-?\d{1,3}\.\d{3,8})", RegexOptions.Compiled);
    }

    // ── Anti-bot / block-page detection ───────────────────────────────────
    public static class BlockMarkers
    {
        public static readonly string[] Values =
        {
            "captcha",
            "datadome",
            "blocked",
            "verify you are human",
            "não sou um robô",
            "acesso bloqueado",
        };
    }

    // ── Feature-list parsing patterns ─────────────────────────────────────
    public static class FeaturePatterns
    {
        public static readonly Regex AreaUteis = new(@"(\d+)\s*m²\s*úteis", RegexOptions.Compiled);
        public static readonly Regex AreaBruta = new(@"(\d+)\s*m²\s*área\s*bruta", RegexOptions.Compiled);
        public static readonly Regex AreaPlain = new(@"(\d+)\s*m²", RegexOptions.Compiled);
        public static readonly Regex Typology = new(@"^T(\d+)$", RegexOptions.Compiled);
        public static readonly Regex Bathrooms = new(@"(\d+)\s*casa", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex Floor = new(@"(\d+)º\s*andar", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex ConstructionYear = new(@"Construído em (\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    // ── Boolean feature keywords (plain substring checks) ─────────────────
    public static class FeatureKeywords
    {
        public const string Elevator = "elevador";
        public const string Negation = "sem";
        public const string AirConditioning = "ar condicionado";
        public const string SwimmingPool = "piscina";
        public const string Garage = "garagem";
        public const string Parking = "estacionamento";
        public const string ParkingAlt = "parking";
        public const string Terrace = "terraço";
        public const string Balcony = "varanda";
        public const string Furnished = "mobilado";
        public const string Equipped = "equipado";
        public const string SeaView = "vista mar";
        public const string SeaViewAlt = "vista de mar";
        public const string Studio = "estúdio";
        public const string GroundFloor = "rés-do-chão";
        public const string GroundFloorAlt = "r/c";
        public const string TouristRentalException = "arrendamento turístico";
    }

    // ── Condition classification (from listing text) ──────────────────────
    public static class ConditionPatterns
    {
        public static readonly Regex NewBuild = new(@"nova construção|empreendimento.*terminado|obra nova", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex Renovated = new(@"remodelad|renovad|reabilitad", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex Good = new(@"bom estado", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex Used = new(@"segunda mão|usado", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex NeedsRenovation = new(@"para recuperar|para remodelar|a necessitar|ruína", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    // ── Exclusion filters (listings to skip entirely) ──────────────────────
    public static class ExclusionPatterns
    {
        public static readonly Regex NotApartment = new(@"\b(moradia|quinta|terreno|garagem|loja|escritório|armazém)\b", RegexOptions.Compiled);
        public static readonly Regex Usufruct = new(@"usufruto|nua-propriedade|nua propriedade|direito de superfície", RegexOptions.Compiled);
        public static readonly Regex Timeshare = new(@"multipropriedade|direito de habitação periódica|semanas? por ano", RegexOptions.Compiled);
        public static readonly Regex Auction = new(@"leilão|venda judicial|penhora|insolvência|hasta pública", RegexOptions.Compiled);
        public static readonly Regex RentalListing = new(@"\barrendamento\b|\bpara arrendar\b", RegexOptions.Compiled);
        public static readonly Regex Tenanted = new(@"\barrendado\b|com inquilino|com contrato em vigor|\bocupado\b", RegexOptions.Compiled);
    }

    public static class ExclusionKeywords
    {
        public const string Trespasse = "trespasse";
    }

    // ── Misc text patterns ──────────────────────────────────────────────────
    public static class MiscPatterns
    {
        public static readonly Regex Price = new(@"[\d.]+", RegexOptions.Compiled);
        public static readonly Regex BeachDistanceMeters = new(@"(\d+)\s*(?:m|metros?)\s*(?:da|do|das?)\s*praia", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex BeachDistanceMinutes = new(@"(\d+)\s*min.*praia", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex PhoneNumber = new(@"\+?\d[\d\s-]{7,}\d", RegexOptions.Compiled);
        public static readonly Regex Email = new(@"[\w.+-]+@[\w-]+\.[\w.-]+", RegexOptions.Compiled);
        public static readonly Regex PageNumberInFilename = new(@"page(\d+)", RegexOptions.Compiled);
    }
}
