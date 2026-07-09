using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AddProperty;

/// <summary>
/// Mirrors StayPilot.Infrastructure/Persistence/Configurations/MarketAreaConfiguration.cs — that
/// EF seed data is the ONLY thing the API's exact-match lookup (GetMarketId in
/// PropertyListingService) will accept for Municipality/Town/Zone. Idealista's page only exposes
/// a single, un-leveled location string (just the parish/town name — see the "location" selector
/// note in BrowserExtension/content-ad.js), never a "Zone, Town, Municipality" breadcrumb, so
/// AddProperty has to re-derive the real Municipality itself instead of trusting the page text.
/// If MarketAreaConfiguration changes, mirror the edit here too (same duplication pattern already
/// used for the request/enum copies in Program.cs).
/// </summary>
public static class MarketAreaLookup
{
    private sealed record Row(string Municipality, string Town, string? Zone);

    private static readonly Row[] Rows =
    {
        // Albufeira
        new("Albufeira", "Albufeira e Olhos de Água", "Centro da Cidade"),
        new("Albufeira", "Albufeira e Olhos de Água", "Olhos de Água"),
        new("Albufeira", "Albufeira e Olhos de Água", "Montechoro"),
        new("Albufeira", "Albufeira e Olhos de Água", "Marina de Albufeira - Cerro da Piedade"),
        new("Albufeira", "Albufeira e Olhos de Água", "Balaia"),
        new("Albufeira", "Albufeira e Olhos de Água", "Praia da Falésia"),
        new("Albufeira", "Albufeira e Olhos de Água", "Clube Albufeira"),
        new("Albufeira", "Albufeira e Olhos de Água", "Praia da Oura - Areias de S. João"),
        new("Albufeira", "Albufeira e Olhos de Água", "São Rafael"),
        new("Albufeira", "Albufeira e Olhos de Água", "Cerro de Águia - Patroves"),
        new("Albufeira", "Albufeira e Olhos de Água", "Sesmarias"),
        new("Albufeira", "Albufeira e Olhos de Água", "Forte São João"),
        new("Albufeira", "Ferreiras", null),
        new("Albufeira", "Guia", "Salgados"),
        new("Albufeira", "Guia", "Galé"),
        new("Albufeira", "Guia", "Vale de Parra"),
        new("Albufeira", "Paderne", null),
        // Alcoutim
        new("Alcoutim", "Alcoutim e Pereiro", null),
        new("Alcoutim", "Giões", null),
        new("Alcoutim", "Martim Longo", null),
        new("Alcoutim", "Vaqueiros", null),
        // Aljezur
        new("Aljezur", "Aljezur", null),
        new("Aljezur", "Bordeira", null),
        new("Aljezur", "Odeceixe", null),
        new("Aljezur", "Rogil", null),
        // Castro Marim
        new("Castro Marim", "Altura", null),
        new("Castro Marim", "Azinhal", null),
        new("Castro Marim", "Castro Marim", "Centro"),
        new("Castro Marim", "Castro Marim", "Praia Verde"),
        new("Castro Marim", "Castro Marim", "Quinta do Sobral - São Bartolomeu"),
        new("Castro Marim", "Castro Marim", "Monte Francisco"),
        new("Castro Marim", "Castro Marim", "Junqueira - Beliche"),
        new("Castro Marim", "Castro Marim", "Golf Resort"),
        new("Castro Marim", "Odeleite", null),
        // Faro
        new("Faro", "Conceição", null),
        new("Faro", "Estoi", null),
        new("Faro", "Faro", "Centro"),
        new("Faro", "Faro", "Arneiro - Braciais - Patacão"),
        new("Faro", "Faro", "Areal Gordo - Rio Seco - Ilha da Culatra"),
        new("Faro", "Faro", "Horta das Figuras - Lejana - Senhora da Saúde"),
        new("Faro", "Faro", "Penha - Vale da Amoreira"),
        new("Faro", "Faro", "São Luís"),
        new("Faro", "Faro", "Alto de Santo António - Bom João - João de Deus"),
        new("Faro", "Faro", "Alto Rodes"),
        new("Faro", "Montenegro", null),
        new("Faro", "Montenegro", "Quinta do Eucalipto - Ilha de Faro"),
        new("Faro", "Santa Bárbara de Nexe", null),
        // Lagoa
        new("Lagoa", "Estombar e Parchal", null),
        new("Lagoa", "Ferragudo", null),
        new("Lagoa", "Lagoa e Carvoeiro", "Cidade de Lagoa"),
        new("Lagoa", "Lagoa e Carvoeiro", "Centro de Carvoeiro"),
        new("Lagoa", "Lagoa e Carvoeiro", "Vale Milho - Vale Centeanes - Algar Seco"),
        new("Lagoa", "Lagoa e Carvoeiro", "Salicos - Sesmarias - Boavista"),
        new("Lagoa", "Lagoa e Carvoeiro", "Caramujeira - Vale d'El Rei - Benagil"),
        new("Lagoa", "Lagoa e Carvoeiro", "Mato Serrão - Vale da Lapa - Vale Currais"),
        new("Lagoa", "Porches", null),
        // Lagos
        new("Lagos", "Barão de São João", null),
        new("Lagos", "Bensafrim", null),
        new("Lagos", "Lagos", null),
        new("Lagos", "Lagos", "Lagos Cidade"),
        new("Lagos", "Lagos", "Meia Praia"),
        new("Lagos", "Lagos", "Quinta da Boavista"),
        new("Lagos", "Lagos", "Falfeira - Monte Funchal"),
        new("Lagos", "Lagos", "Chinicato - Sargaçal"),
        new("Lagos", "Luz", null),
        new("Lagos", "Odiaxere", null),
        // Loulé
        new("Loulé", "Almancil", "Centro"),
        new("Loulé", "Almancil", "Vale do Lobo"),
        new("Loulé", "Almancil", "Quinta do Lago - Pinheiros Altos"),
        new("Loulé", "Almancil", "Vale do Garrão - Varandas do Lago - Quinta das Salinas"),
        new("Loulé", "Almancil", "The Village - Fonte Algarve - Quinta Verde"),
        new("Loulé", "Almancil", "Vale Formoso - Vale d'Éguas"),
        new("Loulé", "Almancil", "São Lourenço - São João da Venda"),
        new("Loulé", "Alte", null),
        new("Loulé", "Ameixial", null),
        new("Loulé", "Benafim", null),
        new("Loulé", "Boliqueime", null),
        new("Loulé", "Quarteira", "Praia de Quarteira"),
        new("Loulé", "Quarteira", "Centro - Quarteira Velha"),
        new("Loulé", "Quarteira", "Fonte Santa"),
        new("Loulé", "Quarteira", "Aldeia do Golf - Alto do Golf"),
        new("Loulé", "Quarteira", "Vilamoura"),
        new("Loulé", "Quarteira", "Marina de Vilamoura"),
        new("Loulé", "Quarteira", "Pinhal Velho - Terraços do Pinhal - Encosta das Oliveiras"),
        new("Loulé", "Quarteira", "Vila Sol - Morgadinho"),
        new("Loulé", "Querença", null),
        new("Loulé", "Salir", null),
        new("Loulé", "São Clemente", "Centro Histórico Este de Loulé"),
        new("Loulé", "São Clemente", "Centro Este da Cidade de Loulé"),
        new("Loulé", "São Sebastião", "Centro Oeste da Cidade de Loulé"),
        new("Loulé", "São Sebastião", "Cerro de Cabeça de Câmara - Estação de Loulé"),
        new("Loulé", "Tor", null),
        // Monchique
        new("Monchique", "Alferce", null),
        new("Monchique", "Marmelete", null),
        new("Monchique", "Monchique", null),
        // Olhão
        new("Olhão", "Fuseta", null),
        new("Olhão", "Moncarapacho", null),
        new("Olhão", "Olhão", "Baixa"),
        new("Olhão", "Olhão", "Marina"),
        new("Olhão", "Olhão", "Cavalinha - Bombeiros"),
        new("Olhão", "Olhão", "Estádio"),
        new("Olhão", "Pechão", null),
        new("Olhão", "Quelfes", null),
        // Portimão
        new("Portimão", "Alvor", null),
        new("Portimão", "Mexilhoeira Grande", null),
        new("Portimão", "Portimão", null),
        new("Portimão", "Portimão", "Portimão Cidade"),
        new("Portimão", "Portimão", "Praia da Rocha"),
        new("Portimão", "Portimão", "Aldeia do Carrasco - Vale da Arrancada"),
        new("Portimão", "Portimão", "Amparo - Alto do Quintão"),
        new("Portimão", "Portimão", "Bemposta - Quatro Estradas"),
        new("Portimão", "Portimão", "Quinta da Malata"),
        new("Portimão", "Portimão", "Vale de Lagar - Quinta das Oliveiras - Pedra Mourinha"),
        // São Brás de Alportel
        new("São Brás de Alportel", "São Brás de Alportel", "Centro"),
        new("São Brás de Alportel", "São Brás de Alportel", "Campina - Mesquita"),
        new("São Brás de Alportel", "São Brás de Alportel", "São Romão - Fonte do Touro"),
        new("São Brás de Alportel", "São Brás de Alportel", "Funchais - Corotelo"),
        new("São Brás de Alportel", "São Brás de Alportel", "Barrabés - Peral"),
        // Silves
        new("Silves", "Alcantarilha", null),
        new("Silves", "Algoz", null),
        new("Silves", "Armação de Pêra", null),
        new("Silves", "Pêra", null),
        new("Silves", "Silves", "Centro da Cidade"),
        new("Silves", "Silves", "Zona Histórica"),
        new("Silves", "Silves", "Vila Fria"),
        new("Silves", "Silves", "Vale da Vila - Poço Barreto"),
        new("Silves", "Silves", "Enxerim - Barrada"),
        new("Silves", "Silves", "Estação de Silves - Cerro de São Miguel"),
        new("Silves", "Silves", "Serra - Barragem do Arade"),
        new("Silves", "São Bartolomeu de Messines", null),
        new("Silves", "São Marcos da Serra", null),
        new("Silves", "Tunes", null),
        // Tavira
        new("Tavira", "Cabanas de Tavira", null),
        new("Tavira", "Cachopo", null),
        new("Tavira", "Conceição", null),
        new("Tavira", "Luz de Tavira", null),
        new("Tavira", "Santa Catarina - Fonte do Bispo", null),
        new("Tavira", "Santa Luzia", null),
        new("Tavira", "Santo Estêvão", null),
        new("Tavira", "Tavira", null),
        new("Tavira", "Tavira", "Centro Histórico"),
        new("Tavira", "Tavira", "Porta Nova - Colinas da Boavista"),
        new("Tavira", "Tavira", "Pegada - Mato Santo Espírito - Vale Carangueijo"),
        new("Tavira", "Tavira", "Quinta da Foz - Escolas"),
        new("Tavira", "Tavira", "Colina de Asseca - Quinta de Perogil - São Pedro"),
        new("Tavira", "Tavira", "Serra"),
        // Vila do Bispo
        new("Vila do Bispo", "Barão de São Miguel", null),
        new("Vila do Bispo", "Budens", null),
        new("Vila do Bispo", "Sagres", null),
        new("Vila do Bispo", "Vila do Bispo e Raposeira", null),
        // Vila Real de Santo António
        new("Vila Real de Santo António", "Monte Gordo", null),
        new("Vila Real de Santo António", "Vila Nova de Cacela", null),
        new("Vila Real de Santo António", "Vila Real de Santo António", null),
    };

    /// <summary>UnmatchedParts carries any comma-separated segments from the raw location text
    /// that couldn't be tied to a seed Zone — e.g. a sub-neighborhood finer than our data goes
    /// (like "Fojo" inside Portimão), or a redundant city-name segment. Nothing authoritative
    /// hinges on it; it exists purely so the caller can surface it in Notes instead of silently
    /// dropping real information off the page.</summary>
    public sealed record Resolution(string Municipality, string Town, string? Zone, bool ZoneAmbiguous, IReadOnlyList<string> UnmatchedParts);

    // Idealista appends a disambiguator in parentheses to town/zone names that clash with a
    // same-named place elsewhere in the country (e.g. "Lagoa (Algarve)", because there's also
    // a Lagoa in the Azores) — that suffix has no equivalent in the seed data and must be
    // stripped before matching.
    private static readonly Regex Disambiguator = new(@"\s*\([^)]*\)\s*$", RegexOptions.Compiled);

    /// <summary>Resolves the raw location string Idealista gives us against the seed data.
    /// The subtitle lists most-specific → least-specific, left to right (e.g. "Fojo, Portimão
    /// Cidade, Portimão" = sub-neighborhood, zone, parish/town) — it is NOT always a single
    /// value, and the position of "the Zone" isn't fixed either (see UnmatchedParts doc).
    /// The last part is always the Parish/Town (sometimes identical to the Municipality name).
    /// Returns null if even that last part doesn't match anything known, so the caller can fall
    /// back to the old best-effort behavior and flag it for manual review instead of silently
    /// saving a wrong Municipality.</summary>
    public static Resolution? Resolve(string? rawLocationText)
    {
        var parts = (rawLocationText ?? "")
            .Split(',')
            .Select(p => Disambiguator.Replace(p, "").Trim())
            .Where(p => p.Length > 0)
            .ToList();

        if (parts.Count == 0)
            return null;

        var townCandidate = parts[^1];
        var normalizedTown = Normalize(townCandidate);

        var byTown = Rows.Where(r => Normalize(r.Town) == normalizedTown).ToList();
        var municipalitiesForTown = byTown.Select(r => r.Municipality).Distinct().ToList();

        string municipality;
        string town;
        List<Row> candidateRows;

        if (municipalitiesForTown.Count == 1)
        {
            municipality = municipalitiesForTown[0];
            town = byTown[0].Town;
            candidateRows = byTown;
        }
        else
        {
            // Last part isn't a known (unambiguous) Town — e.g. "Conceição" exists under both
            // Faro and Tavira. Try it as a Municipality name instead. Town/Zone stay
            // unresolved since a municipality alone doesn't tell us the parish.
            var byMunicipality = Rows.Where(r => Normalize(r.Municipality) == normalizedTown).ToList();
            if (byMunicipality.Count == 0)
                return null;
            municipality = byMunicipality[0].Municipality;
            town = townCandidate;
            candidateRows = byMunicipality;
        }

        // Everything left of the Town is a Zone candidate. Idealista doesn't put the Zone at a
        // fixed position — sometimes it's the part right before the Town ("Vilamoura, Quarteira"),
        // sometimes there's an extra finer-grained or redundant part first ("Fojo, Portimão
        // Cidade, Portimão", "Centro da Cidade, Albufeira, Albufeira e Olhos de Água") — so try
        // every remaining part against this Town's real seed Zones rather than assuming a slot.
        var remaining = parts.Take(parts.Count - 1).ToList();
        string? zone = null;
        var matchedIndex = -1;
        for (var i = 0; i < remaining.Count; i++)
        {
            var normalizedZone = Normalize(remaining[i]);
            var zoneRow = candidateRows.FirstOrDefault(r => Normalize(r.Zone ?? "") == normalizedZone);
            if (zoneRow == null) continue;
            zone = zoneRow.Zone;
            matchedIndex = i;
            break;
        }

        var zoneAmbiguous = false;
        if (zone == null)
        {
            var distinctZones = candidateRows.Select(r => r.Zone).Distinct().ToList();
            if (distinctZones.Count == 1)
                zone = distinctZones[0];
            else
                zoneAmbiguous = true;
        }

        var unmatched = matchedIndex >= 0
            ? remaining.Where((_, i) => i != matchedIndex).ToList()
            : remaining;

        return new Resolution(municipality, town, zone, zoneAmbiguous, unmatched);
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
