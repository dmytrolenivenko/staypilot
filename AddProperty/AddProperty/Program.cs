
using AddProperty;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

public class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static readonly string JsonFolder =
        @"C:\repos\EngineeringVault\vault\StayPilot\Project\Adds\Idealista";

    public static async Task Main(string[] args)
    {
        // Usage:
        //   dotnet run                      → upload existing JSON files
        //   dotnet run upload               → upload existing JSON files
        //   dotnet run scrape               → scrape 1 page, save, then upload
        //   dotnet run scrape --pages 3     → scrape 3 pages, save, then upload
        //   dotnet run scrape-only          → scrape without uploading
        //   dotnet run scrape-only --pages 3

        var command = args.Length > 0 ? args[0].ToLower() : "upload";

        if (command is "scrape" or "scrape-only")
        {
            int pageCount = 1;
            var pagesIdx = Array.IndexOf(args, "--pages");
            if (pagesIdx >= 0 && pagesIdx + 1 < args.Length)
                int.TryParse(args[pagesIdx + 1], out pageCount);

            var scraper = new IdealScraper(JsonFolder, JsonOptions);

            Logger.LogInformation($"Next page to scrape: {scraper.GetNextPageNumber()}");

            var savedFiles = await scraper.ScrapeAsync(pageCount);

            if (command == "scrape" && savedFiles.Count > 0)
            {
                Logger.LogInformation("\n=== Uploading to API ===");
                CallApi(JsonOptions, savedFiles);
            }
        }
        else
        {
            // Default: upload all existing JSON files
            CallApi(JsonOptions);
        }
    }

    /// <summary>Upload all JSON files in the folder to the StayPilot API.</summary>
    public static void CallApi(JsonSerializerOptions jsonOptions, List<string>? specificFiles = null)
    {
        var client = new HttpClient();
        var files = specificFiles ?? Directory.GetFiles(JsonFolder, "*.json").ToList();

        foreach (var jsonFile in files)
        {
            Logger.LogInformation($"Uploading: {Path.GetFileName(jsonFile)}");

            var jsonData = File.ReadAllText(jsonFile);
            var properties = JsonSerializer.Deserialize<List<PropertyListingRequest>>(jsonData, jsonOptions);

            if (properties == null)
            {
                Logger.LogError($"Failed to deserialize {jsonFile}");
                continue;
            }

            var logFilePath = Path.Combine(
                @"C:\repos\EngineeringVault\vault\StayPilot\Project\Logs\Idealista",
                $"{Path.GetFileNameWithoutExtension(jsonFile)}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt");

            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);

            foreach (var property in properties)
            {
                var response = client.PostAsync(
                    "https://localhost:7056/api/PropertyListing",
                    new StringContent(
                        JsonSerializer.Serialize(property, jsonOptions),
                        System.Text.Encoding.UTF8,
                        "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().Result;
                    Logger.LogInformation($"  OK: {property.SourceUrl}");
                    Logger.ToFile(data, logFilePath);
                }
                else
                {
                    var errorBody = response.Content.ReadAsStringAsync().Result;
                    Logger.LogError($"  FAILED: {response.StatusCode} — {property.SourceUrl}");
                    Logger.ToFile($"Error: {response.StatusCode} - {errorBody}", logFilePath);
                }
            }
        }
    }
}

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

        public ListingSnapshotRequest ListingSnapshot { get; set; } = new ListingSnapshotRequest();
    }

    public class ListingSnapshotRequest
    {
        public int PropertyListingId { get; set; }

        public decimal Price { get; set; }

        public decimal PricePerM2 { get; set; }

        public ListingStatus Status { get; set; }

        public DateTime SnapshotDateUtc { get; set; } = DateTime.UtcNow;
    }

    public enum ListingStatus
    {
        Sold = 0,
        Active = 1,
        PriceChanged = 2
    }

    public enum PropertyCondition
    {
        Unknown = 0,
        NeedsRenovation = 1,
        Used = 2,
        Good = 3,
        Renovated = 4,
        NewBuild = 5
    }

    public enum PropertyType
    {
        Apartment = 0,
        Villa = 1,
        House = 2,
        Land = 3
    }

    public enum Typology
    {
        T0 = 0,
        T1 = 1,
        T2 = 2,
        T3 = 3,
        T4 = 4,
        T5 = 5,
        T6 = 6,
        T7 = 7,
        T8 = 8,
        T9 = 9,
        T10 = 10,
    }
