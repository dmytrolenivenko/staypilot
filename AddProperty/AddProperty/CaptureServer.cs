using System.Net;
using System.Text;
using System.Text.Json;

namespace AddProperty;

/// <summary>
/// Opens a local-only HTTP listener that the browser extension's background script posts
/// captured listing data to. Bound to "localhost" specifically — nothing outside this
/// machine can reach it, and Idealista's servers never see this traffic at all.
/// </summary>
public class CaptureServer
{
    private readonly ListingCapture _capture;
    private readonly int _port;
    private HttpListener? _listener;

    public CaptureServer(ListingCapture capture, int port = 5099)
    {
        _capture = capture;
        _port = port;
    }

    public void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _listener.Start();
        Logger.LogInformation($"Listening on http://localhost:{_port}/capture — open Idealista tabs now.");
        _ = Task.Run(ListenLoopAsync);
    }

    public void Stop()
    {
        try
        {
            _listener?.Stop();
            _listener?.Close();
        }
        catch (ObjectDisposedException) { }
    }

    private async Task ListenLoopAsync()
    {
        while (_listener != null && _listener.IsListening)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync();
            }
            catch (HttpListenerException)
            {
                break; // Stop() was called
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => HandleRequest(ctx));
        }
    }

    private void HandleRequest(HttpListenerContext ctx)
    {
        try
        {
            if (ctx.Request.HttpMethod != "POST" || ctx.Request.Url?.AbsolutePath != "/capture")
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.Close();
                return;
            }

            string body;
            using (var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                body = reader.ReadToEnd();

            var message = JsonSerializer.Deserialize<CaptureMessage>(
                body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var result = message?.Type switch
            {
                "ad" => _capture.HandleCapturedAd(message.Data.GetRawText()),
                "coords" => _capture.HandleCapturedCoords(message.Data.GetRawText()),
                _ => "Ignored: unknown or missing message type."
            };

            Logger.LogInformation($"  {result}");

            ctx.Response.StatusCode = 200;
            ctx.Response.Close();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error handling capture request: {ex.Message}");
            try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { }
        }
    }

    /// <summary>The {type, data} envelope background.js wraps every message in.</summary>
    private class CaptureMessage
    {
        public string? Type { get; set; }
        public JsonElement Data { get; set; }
    }
}
