using System.Text;
using System.Text.Json;

namespace AddProperty;

/// <summary>
/// Reads every saved listing JSON file and POSTs each listing to the StayPilot API.
/// This is the only part of the app that writes to the database — capturing listings
/// (see ListingCapture.cs) never touches it.
/// </summary>
public static class ApiUploader
{
    private const string ApiUrl = "https://localhost:7056/api/PropertyListing";

    public static void UploadAll(string jsonFolder, string logFolder, JsonSerializerOptions jsonOptions)
    {
        var client = new HttpClient();
        var files = Directory.GetFiles(jsonFolder, "*.json");

        if (files.Length == 0)
        {
            Logger.LogInformation("Nothing to upload — no JSON files found.");
            return;
        }

        foreach (var jsonFile in files)
        {
            Logger.LogInformation($"Uploading: {Path.GetFileName(jsonFile)}");

            var jsonData = File.ReadAllText(jsonFile);
            var listings = JsonSerializer.Deserialize<List<PropertyListingRequest>>(jsonData, jsonOptions);

            if (listings == null)
            {
                Logger.LogError($"  Could not read {jsonFile} — skipped.");
                continue;
            }

            var logFilePath = Path.Combine(
                logFolder, $"{Path.GetFileNameWithoutExtension(jsonFile)}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt");
            Directory.CreateDirectory(logFolder);

            foreach (var listing in listings)
                UploadOne(client, listing, jsonOptions, logFilePath);
        }
    }

    private static void UploadOne(HttpClient client, PropertyListingRequest listing, JsonSerializerOptions jsonOptions, string logFilePath)
    {
        var response = client.PostAsync(
            ApiUrl,
            new StringContent(JsonSerializer.Serialize(listing, jsonOptions), Encoding.UTF8, "application/json")
        ).Result;

        if (response.IsSuccessStatusCode)
        {
            var body = response.Content.ReadAsStringAsync().Result;
            Logger.LogInformation($"  OK: {listing.SourceUrl}");
            Logger.ToFile(body, logFilePath);
        }
        else
        {
            var errorBody = response.Content.ReadAsStringAsync().Result;
            Logger.LogError($"  FAILED: {response.StatusCode} — {listing.SourceUrl}");
            Logger.ToFile($"Error: {response.StatusCode} - {errorBody}", logFilePath);
        }
    }
}
