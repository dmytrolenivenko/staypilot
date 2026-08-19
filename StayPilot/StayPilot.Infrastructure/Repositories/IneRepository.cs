using System.Globalization;
using System.Text.Json;
using StayPilot.Application.Interfaces.Repositories;

namespace StayPilot.Infrastructure.Repositories
{
    /// <summary>
    /// Reads INE's construction cost index off their public JSON endpoint.
    ///
    /// The response is an array of one object whose "Dados" maps a period name to that period's
    /// rows, and only the latest period comes back, so the parsing takes the single entry.
    ///
    /// The first successful read is kept for the life of the process. Not an optimisation - INE
    /// stops answering a caller that asks repeatedly, and the series only changes once a month,
    /// so holding on to it is what keeps this working. A restart re-reads.
    /// </summary>
    public class IneRepository : IIneRepository
    {
        /// <summary>Índice de custo de construção de habitação nova, base 2021 = 100.</summary>
        private const string IndicatorCode = "0011748";

        /// <summary>The values INE puts in "dim_3" for this series.</summary>
        private const string TotalDimension = "T";
        private const string MaterialsDimension = "1";
        private const string LabourDimension = "2";

        private static ConstructionIndex? _lastRead;

        private readonly HttpClient _http;

        public IneRepository(HttpClient http)
        {
            _http = http;
        }

        /// <inheritdoc/>
        public async Task<ConstructionIndex?> GetConstructionIndexAsync(CancellationToken cancellationToken = default)
        {
            return _lastRead ??= await ReadIndexAsync(cancellationToken);
        }

        private async Task<ConstructionIndex?> ReadIndexAsync(CancellationToken cancellationToken)
        {
            var url = $"ine/json_indicador/pindica.jsp?op=2&varcd={IndicatorCode}&lang=PT";

            try
            {
                using var response = await _http.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
                {
                    return null;
                }

                if (!document.RootElement[0].TryGetProperty("Dados", out var data) || data.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                // The period is the property name - there is no separate field carrying it.
                var period = data.EnumerateObject().FirstOrDefault();

                if (period.Value.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                decimal? total = null;
                decimal? labour = null;
                decimal? materials = null;

                foreach (var row in period.Value.EnumerateArray())
                {
                    switch (ReadString(row, "dim_3"))
                    {
                        case TotalDimension:
                            total = ReadDecimal(row, "valor");
                            break;

                        case LabourDimension:
                            labour = ReadDecimal(row, "valor");
                            break;

                        case MaterialsDimension:
                            materials = ReadDecimal(row, "valor");
                            break;
                    }
                }

                if (total is null || labour is null || materials is null)
                {
                    return null;
                }

                return new ConstructionIndex(total.Value, labour.Value, materials.Value, period.Name);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
            {
                // Unreachable, throttled, or answering with something unexpected. All the same to
                // a caller that already knows how to price without it.
                return null;
            }
        }

        private static string ReadString(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }

        /// <summary>INE sends numbers as strings, and "…" for a cell it withholds.</summary>
        private static decimal? ReadDecimal(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }
    }
}
