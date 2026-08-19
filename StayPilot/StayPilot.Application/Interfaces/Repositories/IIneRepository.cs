namespace StayPilot.Application.Interfaces.Repositories
{
    /// <summary>
    /// The construction cost index, as INE publishes it: base 2021 = 100, split into the two
    /// halves that move at different speeds.
    ///
    /// A read model for an external source, not an entity - nothing stores it.
    /// </summary>
    /// <param name="Total">The blended index. 134.33 in June 2026.</param>
    /// <param name="Labour">Mão-de-obra. 144.70 in June 2026 - running well ahead of materials.</param>
    /// <param name="Materials">Materiais. 126.24 in June 2026.</param>
    /// <param name="Period">The month INE published, written the way INE writes it: "Junho de 2026".</param>
    public readonly record struct ConstructionIndex(decimal Total, decimal Labour, decimal Materials, string Period);

    /// <summary>
    /// Reads INE's construction cost index off their public JSON endpoint.
    ///
    /// No key and no registration, but also no CORS headers, which is why this sits on the server
    /// rather than in the browser. INE throttles a caller that asks repeatedly, and the series
    /// only changes once a month, so an implementation must cache it and must survive a refusal.
    /// </summary>
    public interface IIneRepository
    {
        /// <summary>
        /// The latest published month of the index (INE indicator 0011748).
        /// Null when INE could not be reached, which the caller is expected to say out loud
        /// rather than paper over.
        /// </summary>
        Task<ConstructionIndex?> GetConstructionIndexAsync(CancellationToken cancellationToken = default);
    }
}
