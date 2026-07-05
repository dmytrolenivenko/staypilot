using System.Text.RegularExpressions;

namespace AddProperty;

// ─────────────────────────────────────────────────────────────────────────
// IF IDEALISTA'S LISTINGS CHANGE FORMAT AND PARSING BREAKS, START HERE.
//
// This app no longer visits Idealista's pages itself (that used to be Playwright, and got
// blocked). Instead, the browser extension in BrowserExtension/ reads pages you open normally
// and sends the raw text here. That split means there are TWO places selectors/patterns live:
//
//   - CSS selectors (which HTML element has the price, the title, etc.) live in
//     BrowserExtension/content-ad.js and content-map.js, as a SELECTORS object at the
//     top of each file. If Idealista changes its page markup, edit those.
//
//   - Everything in THIS file is about making sense of the TEXT once it's already been
//     read off the page — regexes and keyword lists for area/typology/price/condition/etc.
//     If a listing gets parsed wrong (wrong area, missed a feature, wrongly excluded), the
//     fix is almost always here.
// ─────────────────────────────────────────────────────────────────────────
public static class IdealistaLocators
{
    // ── Energy certificate ────────────────────────────────────────────────
    // The extension sends the raw CSS class name of the energy icon (e.g. "icon-energy-c");
    // this pulls the single letter grade out of it.
    public static readonly Regex EnergyClassPattern = new(@"icon-energy-(\w)", RegexOptions.Compiled);

    // ── Coordinates ───────────────────────────────────────────────────────
    // The extension sends the href of whatever Google Maps link it found on the /mapa page.
    // Coordinates always appear in that URL as "@lat,lng" regardless of page wording/language.
    public static class Map
    {
        public static readonly Regex GoogleMapsHrefPattern =
            new(@"(google\.com/maps|maps\.google)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex CoordinatesInHrefPattern =
            new(@"@(-?\d{1,3}\.\d{3,8}),(-?\d{1,3}\.\d{3,8})", RegexOptions.Compiled);
    }

    // ── Feature-list parsing ──────────────────────────────────────────────
    // Idealista lists a bullet-point "feature" per line (e.g. "63 m² úteis", "T2", "Elevador").
    // These pull structured values out of that plain text.
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

    // Boolean amenities are just "does this feature line contain this word" checks.
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

    // ── Condition classification (from the description text) ─────────────
    public static class ConditionPatterns
    {
        public static readonly Regex NewBuild = new(@"nova construção|empreendimento.*terminado|obra nova", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex Renovated = new(@"remodelad|renovad|reabilitad", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex Good = new(@"bom estado", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex Used = new(@"segunda mão|usado", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex NeedsRenovation = new(@"para recuperar|para remodelar|a necessitar|ruína", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    // ── Exclusion filters (listings we never want to keep) ────────────────
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

    // ── Everything else ────────────────────────────────────────────────────
    public static class MiscPatterns
    {
        public static readonly Regex Price = new(@"[\d.]+", RegexOptions.Compiled);
        public static readonly Regex BeachDistanceMeters = new(@"(\d+)\s*(?:m|metros?)\s*(?:da|do|das?)\s*praia", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex BeachDistanceMinutes = new(@"(\d+)\s*min.*praia", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex PhoneNumber = new(@"\+?\d[\d\s-]{7,}\d", RegexOptions.Compiled);
        public static readonly Regex Email = new(@"[\w.+-]+@[\w-]+\.[\w.-]+", RegexOptions.Compiled);
    }
}
