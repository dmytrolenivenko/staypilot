using AddProperty;
using System.Text.Json;

// AddProperty has exactly two jobs, run as two separate, deliberate commands:
//
//   dotnet run           -> capture listings from the browser extension, save to JSON
//   dotnet run upload    -> upload the saved JSON files to the StayPilot API (writes to the DB)
//
// These are two steps on purpose, not one continuous flow — it gives you a chance to look at
// what got captured before anything touches the database.

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
};

var jsonFolder = @"C:\repos\EngineeringVault\vault\StayPilot\Project\Adds\Idealista";
var logFolder = @"C:\repos\EngineeringVault\vault\StayPilot\Project\Logs\Idealista";

if (args.Length > 0 && args[0].Equals("upload", StringComparison.OrdinalIgnoreCase))
{
    ApiUploader.UploadAll(jsonFolder, logFolder, jsonOptions);
}
else
{
    RunCaptureSession(jsonFolder, jsonOptions);
}

// ── The capture session ──────────────────────────────────────────────────
// Starts the local listener, waits while you browse Idealista normally in Chrome, and stops
// when you type "done". Every listing is already saved to disk as it's captured (see
// ListingCapture.cs) — typing "done" only stops the listener, it isn't a save point.
static void RunCaptureSession(string jsonFolder, JsonSerializerOptions jsonOptions)
{
    var capture = new ListingCapture(jsonFolder, jsonOptions);
    var server = new CaptureServer(capture);
    server.Start();

    Console.WriteLine("Browse Idealista normally in Chrome now — open ad tabs, the extension handles the rest.");
    Console.WriteLine("Type \"done\" and press Enter when you're finished capturing for this session.");

    while (true)
    {
        var input = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (input == "done") break;
    }

    server.Stop();
    Console.WriteLine("Stopped listening. Everything captured is already saved.");
    Console.WriteLine("Run \"dotnet run upload\" when you're ready to write it to the database.");
}
