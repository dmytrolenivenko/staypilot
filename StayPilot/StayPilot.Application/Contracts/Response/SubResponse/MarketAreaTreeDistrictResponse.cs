namespace StayPilot.Application.Contracts.Response.SubResponse
{
    /// <summary>One district in the place tree, with its municípios.</summary>
    public class MarketAreaTreeDistrictResponse
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Sorted by name.</summary>
        public List<MarketAreaTreeMunicipalityResponse> Municipalities { get; set; } = new();
    }

    /// <summary>One município in the place tree, with its freguesias.</summary>
    public class MarketAreaTreeMunicipalityResponse
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Sorted by name.</summary>
        public List<MarketAreaTreeTownResponse> Towns { get; set; } = new();
    }

    /// <summary>One freguesia in the place tree, with its zones (often none).</summary>
    public class MarketAreaTreeTownResponse
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Sorted by name. Empty when the freguesia is not split into zones.</summary>
        public List<string> Zones { get; set; } = new();
    }
}
