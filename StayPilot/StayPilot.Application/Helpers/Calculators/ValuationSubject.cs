using StayPilot.Application.Contracts.Response;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// A property in the exact terms the model prices on, so a scraped comp and one of the
    /// user's own flats are the same kind of thing to it.
    /// </summary>
    internal class ValuationSubject
    {
        /// <summary>Which market area the property sits in. Drives the location baseline.</summary>
        public int MarketAreaId { get; set; }

        /// <summary>
        /// The district and municipality the market area sits in, so a zone with too few
        /// listings to price on its own can still be priced as part of somewhere larger
        /// instead of falling back to the national average. Empty when unknown - the model
        /// then looks the geography up from the market area id.
        /// </summary>
        public string District { get; set; } = string.Empty;

        /// <inheritdoc cref="District"/>
        public string Municipality { get; set; } = string.Empty;

        /// <summary>
        /// The town and the zone within it. Carried so a fit over these subjects can spot the
        /// areas that are not really places (a catch-all zone named after the whole town). Empty
        /// on an owned property, which knows only its area id; a fit looks those up by id instead.
        /// </summary>
        public string Town { get; set; } = string.Empty;

        /// <inheritdoc cref="Town"/>
        public string Zone { get; set; } = string.Empty;

        /// <summary>Number of rooms, Portuguese T-style (T0, T1, T2...).</summary>
        public Typology Typology { get; set; }

        /// <summary>
        /// Apartment, villa, house or land. Every listing collected so far is an apartment, so
        /// this currently does nothing - it is here so the model does not silently price a villa
        /// as an apartment the day the scraper starts collecting them.
        /// </summary>
        public PropertyType PropertyType { get; set; }

        /// <summary>State of the property. Unknown is treated as the ordinary "Good" case.</summary>
        public PropertyCondition Condition { get; set; } = PropertyCondition.Good;

        /// <summary>Floor area in square meters. Must be greater than zero to be priceable.</summary>
        public int AreaM2 { get; set; }

        public int Bathrooms { get; set; }

        public int BalconyCount { get; set; }

        /// <summary>Which floor it is on, or null when the source did not say.</summary>
        public int? Floor { get; set; }

        /// <summary>Year built, or null when the source did not say.</summary>
        public int? ConstructionYear { get; set; }

        /// <summary>
        /// The energy certificate as a number so it can be priced per step: G is 0 through to
        /// A+ at 8. Null when the certificate is missing or not a grade we recognise, which the
        /// model flags rather than guessing at.
        /// </summary>
        public int? EnergyGradeScore { get; set; }

        /// <summary>Walking-line distance to the nearest beach, or null when unknown.</summary>
        public int? DistanceToBeachMeters { get; set; }

        /// <summary>Coordinates, used for the neighbourhood correction.</summary>
        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }

        public bool HasElevator { get; set; }

        public bool HasTerrace { get; set; }

        public bool HasGarage { get; set; }

        public bool HasSwimmingPool { get; set; }

        public bool IsFurnished { get; set; }

        public bool HasParking { get; set; }

        public bool HasSeaView { get; set; }

        public bool HasCityView { get; set; }

        /// <summary>
        /// Close enough to walk to the beach. One number, used everywhere the question comes up:
        /// the "Close to Beach" premium, and the sea view's beachfront figure. It lives here
        /// rather than on either calculator so the two can never disagree about what "close"
        /// means - which is exactly what happened when each kept its own copy.
        /// </summary>
        public const int CloseToBeachMeters = 500;

        /// <summary>
        /// Where being near the beach stops being worth anything at all. Between this and
        /// <see cref="CloseToBeachMeters"/> a property earns a shrinking share of the premium,
        /// so 501m is worth almost what 500m is instead of falling off a cliff.
        ///
        /// Two kilometres is a judgement, not a measurement - what was measured is "within 500m
        /// is worth X". It is the distance past which nobody calls a flat a beach property.
        /// </summary>
        public const int BeachCreditEndsAtMeters = 2000;

        /// <summary>
        /// The floor from which a lift stops being a convenience. Below it the premium does not
        /// clear zero at all; at and above it, it is one of the steadiest numbers we have.
        /// </summary>
        public const int LiftMattersFromFloor = 3;

        /// <summary>
        /// Nowhere in Portugal is 50km from the sea, so anything past this is a broken
        /// coordinate rather than an inland property.
        /// </summary>
        private const int ImplausibleBeachMeters = 50_000;

        /// <summary>
        /// Did this property come with a believable distance to the beach? Missing, zero and
        /// impossibly large all read the same way: we do not know. Callers must ask this before
        /// trusting <see cref="DistanceToBeachMeters"/>.
        /// </summary>
        public static bool KnowsBeachDistance(ValuationSubject subject)
        {
            return subject.DistanceToBeachMeters is > 0 and <= ImplausibleBeachMeters;
        }

        /// <summary>
        /// Is this property within <see cref="CloseToBeachMeters"/> of the sea? False when we do
        /// not know the distance - "we never measured it" is not evidence of being close.
        /// </summary>
        public static bool IsCloseToBeach(ValuationSubject subject)
        {
            return KnowsBeachDistance(subject) && subject.DistanceToBeachMeters <= CloseToBeachMeters;
        }

        /// <summary>
        /// High enough up that a lift is worth real money. False when the floor was never
        /// stated, for the same reason as above: a gap is not a ground floor.
        /// </summary>
        public static bool IsHighUp(ValuationSubject subject)
        {
            return subject.Floor >= LiftMattersFromFloor;
        }

        /// <summary>
        /// Certificate letter to a position on the scale, so the model prices one step rather
        /// than nine letters. Unrecognised comes back null, not "G" - a typo is missing data,
        /// not the worst possible rating.
        /// </summary>
        public static int? ScoreEnergyGrade(string? certificate)
        {
            if (string.IsNullOrWhiteSpace(certificate))
                return null;

            return certificate.Trim().ToUpperInvariant() switch
            {
                "A+" => 8,
                "A" => 7,
                "B" => 6,
                "B-" => 5,
                "C" => 4,
                "D" => 3,
                "E" => 2,
                "F" => 1,
                "G" => 0,
                _ => null,
            };
        }

        /// <summary>
        /// The letter behind a score - the inverse of <see cref="ScoreEnergyGrade"/>, kept
        /// beside it so the two can't drift. Off-scale values clamp to the ends.
        /// </summary>
        public static string GradeLetter(int score)
        {
            return Math.Clamp(score, 0, 8) switch
            {
                8 => "A+",
                7 => "A",
                6 => "B",
                5 => "B-",
                4 => "C",
                3 => "D",
                2 => "E",
                1 => "F",
                _ => "G",
            };
        }

        /// <summary>
        /// Reads a scraped comp. Note HasAirConditioning is deliberately not carried over:
        /// the column has thousands of trues and zero explicit falses, so "false" really means
        /// "the advert did not mention it" - modelling it would price the copywriting.
        /// </summary>
        public static ValuationSubject FromListing(PropertyListing listing)
        {
            return new ValuationSubject
            {
                MarketAreaId = listing.MarketAreaId,

                // Null whenever the caller did not Include the market area. Empty strings just
                // mean "unknown", which the location fallback already handles.
                District = listing.MarketArea?.District ?? string.Empty,
                Municipality = listing.MarketArea?.Municipality ?? string.Empty,
                Town = listing.MarketArea?.Town ?? string.Empty,
                Zone = listing.MarketArea?.Zone ?? string.Empty,
                Typology = listing.Typology,
                PropertyType = listing.PropertyType,
                Condition = listing.Condition,
                AreaM2 = listing.AreaM2,
                Bathrooms = listing.Bathrooms,
                BalconyCount = listing.BalconyCount,
                Floor = listing.Floor,
                ConstructionYear = listing.ConstructionYear,
                EnergyGradeScore = ScoreEnergyGrade(listing.EnergyCertificate),
                DistanceToBeachMeters = listing.DistanceToBeachMeters,
                Latitude = listing.Latitude,
                Longitude = listing.Longitude,
                HasElevator = listing.HasElevator ?? false,
                HasTerrace = listing.HasTerrace,
                HasGarage = listing.HasGarage,
                HasSwimmingPool = listing.HasSwimmingPool,
                IsFurnished = listing.IsFurnished,
                HasParking = listing.HasParking,
                HasSeaView = listing.HasSeaView,
                HasCityView = listing.HasCityView,
            };
        }

        /// <summary>
        /// Reads one of the user's own properties. Everything here is nullable on the response,
        /// and a missing flag means "does not have it" - the same reading the comps get, so an
        /// owned property is never accidentally credited for a feature nobody recorded.
        /// </summary>
        public static ValuationSubject FromOwnedProperty(OwnedPropertyResponse property)
        {
            return new ValuationSubject
            {
                MarketAreaId = property.MarketAreaId,
                Typology = property.Typology,
                PropertyType = property.PropertyType,
                Condition = property.Condition ?? PropertyCondition.Good,
                AreaM2 = property.AreaM2,
                Bathrooms = property.Bathrooms,
                BalconyCount = property.BalconyCount ?? 0,
                Floor = property.Floor,
                ConstructionYear = property.ConstructionYear,
                EnergyGradeScore = ScoreEnergyGrade(property.EnergyCertificate),
                DistanceToBeachMeters = property.DistanceToBeachMeters,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                HasElevator = property.HasElevator ?? false,
                HasTerrace = property.HasTerrace ?? false,
                HasGarage = property.HasGarage ?? false,
                HasSwimmingPool = property.HasSwimmingPool ?? false,
                IsFurnished = property.IsFurnished ?? false,
                HasParking = property.HasParking ?? false,
                HasSeaView = property.HasSeaView ?? false,
                HasCityView = property.HasCityView ?? false,
            };
        }
    }

    /// <summary>
    /// Which listings are fit to learn from. The scraper occasionally reads the wrong number as
    /// the floor area - a T3 that comes back as 2 m² with its €349,000 price intact - and the
    /// resulting €174,500/m² is not an expensive flat, it is a broken row.
    ///
    /// These are not cosmetic. Both fits minimise SQUARED error on ln(price/m²), so one listing
    /// off by a factor of 40 carries the weight of roughly two hundred ordinary ones and drags
    /// every coefficient with it. The old rule admitted anything between €100 and €100,000/m².
    ///
    /// One place, because the price model, the premium measurements and the backtest must agree
    /// on what counts as real data: score a valuation against a row the model refuses to learn
    /// from and you are measuring the scraper, not the valuation.
    /// </summary>
    internal static class ListingQuality
    {
        /// <summary>
        /// Circulation, kitchen and bathroom, before any bedroom is added. A home cannot be
        /// smaller than this no matter how it is described.
        /// </summary>
        private const int BaseAreaM2 = 20;

        /// <summary>Floor space per bedroom. Deliberately below any real room.</summary>
        private const int AreaPerBedroomM2 = 10;

        /// <summary>
        /// Nothing residential in Portugal is priced below this per m². The genuinely cheap
        /// interior runs to about €600, so this only catches parking spaces sold as flats,
        /// "price on application" placeholders, and prices that lost three digits.
        /// </summary>
        private const decimal MinimumPricePerM2 = 400m;

        /// <summary>
        /// Prime Lisbon and the Quinta do Lago strip reach roughly €12,000/m². Past double that
        /// is a broken area, not a trophy home - and the ones we checked all had areas in the
        /// single digits.
        /// </summary>
        private const decimal MaximumPricePerM2 = 25_000m;

        /// <summary>An apartment larger than this is a mis-parse or a whole building.</summary>
        private const int MaximumAreaM2 = 1_000;

        /// <summary>
        /// How far price per m2 may sit from price over area before the row is treated as broken.
        /// Five percent absorbs rounding and the gross-versus-net area the adverts are loose
        /// about; the rows this actually catches are out by a factor, not a few percent.
        /// </summary>
        private const decimal PricePerM2Tolerance = 0.05m;

        /// <summary>
        /// The smallest floor area a property of this typology could really have. The enum
        /// counts from T0 = 1, so bedrooms is one less than the stored value.
        /// </summary>
        public static int MinimumPlausibleAreaM2(Typology typology)
        {
            var bedrooms = Math.Max(0, (int)typology - 1);

            return BaseAreaM2 + (bedrooms * AreaPerBedroomM2);
        }

        /// <summary>
        /// Is this listing, at this snapshot, real enough to learn from? Everything rejected here
        /// is a data fault rather than an unusual property - the bounds sit well outside the real
        /// market on both sides, below the 1st percentile of every typology.
        /// </summary>
        public static bool IsUsable(PropertyListing listing, ListingSnapshot? snapshot)
        {
            if (snapshot is null)
                return false;

            if (listing.AreaM2 < MinimumPlausibleAreaM2(listing.Typology) || listing.AreaM2 > MaximumAreaM2)
                return false;

            if (!AgreesWithItself(listing, snapshot))
                return false;

            return snapshot.PricePerM2 >= MinimumPricePerM2 && snapshot.PricePerM2 <= MaximumPricePerM2;
        }

        /// <summary>
        /// Does the row agree with itself? Price per m2 should be the price over the floor area,
        /// and when it is not, one of the three numbers is wrong and there is no way to tell
        /// which. A Portimão listing asking EUR 123,123 over 91 m2 carried EUR 123/m2; a Loulé
        /// one asking EUR 229,000 over 45 m2 carried EUR 233. Both passed every bound above,
        /// because each field is plausible on its own - it is only the three together that are
        /// impossible.
        ///
        /// Both fields get used downstream: the fit learns from price per m2, the comps table
        /// prints the price. A row where they disagree teaches one thing and displays another.
        /// </summary>
        private static bool AgreesWithItself(PropertyListing listing, ListingSnapshot snapshot)
        {
            if (listing.AreaM2 <= 0 || snapshot.Price <= 0)
                return false;

            var impliedPricePerM2 = snapshot.Price / listing.AreaM2;

            // Room for honest rounding and for an advert quoting gross area against net, no more.
            return Math.Abs(snapshot.PricePerM2 - impliedPricePerM2) <= impliedPricePerM2 * PricePerM2Tolerance;
        }

        /// <summary>The newest snapshot, which is the one every price decision is made on.</summary>
        public static ListingSnapshot? NewestSnapshot(PropertyListing listing)
        {
            return listing.ListingSnapshots.OrderByDescending(x => x.SnapshotDateUtc).FirstOrDefault();
        }

        /// <summary>
        /// Every listing worth learning from, already paired with its price on the log scale -
        /// the exact input both fits start from. Having one method do this is what keeps the
        /// price model and the premium measurements looking at the same market.
        /// </summary>
        public static List<(ValuationSubject Subject, double LogPricePerM2)> UsableSubjects(
            IEnumerable<PropertyListing> listings)
        {
            return UsableSubjects(listings, out _);
        }

        /// <inheritdoc cref="UsableSubjects(IEnumerable{PropertyListing})"/>
        /// <param name="duplicatesCollapsed">
        /// How many admitted rows turned out to be re-advertisements of a property already in the
        /// list. Worth watching: a jump means the scraper started collecting the same stock twice.
        /// </param>
        public static List<(ValuationSubject Subject, double LogPricePerM2)> UsableSubjects(
            IEnumerable<PropertyListing> listings, out int duplicatesCollapsed)
        {
            var usable = new List<PropertyListing>();

            foreach (var listing in listings)
            {
                if (IsUsable(listing, NewestSnapshot(listing)))
                    usable.Add(listing);
            }

            // A row can be structurally sound and still be describing something other than a
            // home. That only shows up against the local market, so it needs the admitted set
            // in hand - which is why it sits here rather than inside IsUsable.
            usable = RejectLocallyImplausible(usable);

            // Admission first, then duplicates: done the other way round, a broken row could be
            // the copy we keep and the sound one the copy we throw away.
            var distinct = DistinctProperties(usable);

            duplicatesCollapsed = usable.Count - distinct.Count;

            return distinct
                .Select(x => (ValuationSubject.FromListing(x), Math.Log((double)NewestSnapshot(x)!.PricePerM2)))
                .ToList();
        }

        /// <summary>
        /// One row per physical property. Agencies re-advertise each other's stock, and the same
        /// flat under two URLs is counted twice by everything downstream: both fits minimise
        /// SQUARED error, so copies pull coefficients toward whatever they are, and the "ten
        /// nearest neighbours" can be the same advert ten times - which is how a valuation
        /// reports High confidence off one flat.
        ///
        /// Two passes, because there are two kinds of copy and one key cannot catch both:
        ///
        /// 1. <see cref="DuplicateKeyOf"/> - byte-identical re-posts. Cheap, exact, safe.
        /// 2. <see cref="CollapseBySourceReference"/> - the same agency reference re-advertised
        ///    with the coordinates or the price moved. Measured on this database, pass 1 alone
        ///    caught 11 of 293 such groups: it demands identical coordinates, and a re-listed
        ///    flat is usually re-geocoded a few hundred metres away. 380 of the 417 pairs it
        ///    misses sit within 500m of each other, which is the same flat, not a neighbour.
        ///
        /// Deliberately NOT collapsed: a development advertising many units at one price. Those
        /// rows share a price and a floor area but sit at different coordinates, and they are real
        /// separate flats - measured on this data, 1,625 of 1,823 same-price groups are that
        /// rather than re-advertisements. They are still only one price DECISION, so they
        /// over-weight the fit; that is a weighting problem, not a duplicate one, and deleting
        /// them would be throwing away real market evidence to fix it.
        /// </summary>
        public static List<PropertyListing> DistinctProperties(IEnumerable<PropertyListing> listings)
        {
            var exact = listings
                .GroupBy(DuplicateKeyOf)
                .Select(x => x.OrderBy(listing => listing.Id).First())
                .ToList();

            return CollapseBySourceReference(exact);
        }

        /// <summary>
        /// How far apart two adverts carrying the same agency reference can sit and still be one
        /// flat. Generous on purpose: the reference already did the identifying, and distance is
        /// only here to throw out reference COLLISIONS - two agencies that both call something
        /// "002". Measured on this database, pairs are either under 2km (407 of 417) or tens of
        /// kilometres apart with nothing in between, so the threshold sits in an empty gap and
        /// its exact value changes nothing.
        /// </summary>
        private const double DuplicateReferenceMeters = 2_000;

        /// <summary>
        /// Collapses adverts that carry the same agency reference AND describe the same physical
        /// thing - same floor area, same layout, same kind of property - and sit close enough
        /// together to be one flat rather than a reference collision.
        ///
        /// All four conditions are needed. The reference on its own is NOT unique: it is the
        /// agency's own filing number, not a portal id, so short ones repeat across agencies.
        /// Measured on this database, 447 same-reference groups hold properties of different
        /// floor areas in different municípios - "002" is a Faro flat and a Setúbal one. Keeping
        /// area, typology and property type in the key throws those out without losing a single
        /// real duplicate.
        ///
        /// Why this does not eat the developments <see cref="DistinctProperties"/> protects: a
        /// reference identifies a UNIT, which is what a reference is for, so a development's
        /// flats carry distinct ones. The largest groups in this database bear that out - five
        /// adverts under "123891235-27" all share the -27 unit suffix, so they are one flat
        /// posted five times, not units 27 through 31. Different portal ids, one reference.
        ///
        /// The exposure if that reading is ever wrong is bounded and small: groups of four or
        /// more - the only ones a development could plausibly be - account for 31 rows in 20,684.
        /// </summary>
        private static List<PropertyListing> CollapseBySourceReference(List<PropertyListing> listings)
        {
            var kept = new List<PropertyListing>(listings.Count);

            foreach (var group in listings.GroupBy(SourceReferenceKeyOf))
            {
                // No usable reference, or nothing to compare it against: keep every row as it is.
                if (group.Key is null || !group.Skip(1).Any())
                {
                    kept.AddRange(group);
                    continue;
                }

                // Oldest first, so the row that survives a collapse is the one already known to
                // everything downstream rather than whichever copy the scraper saw most recently.
                var candidates = group.OrderBy(x => x.Id).ToList();
                var survivors = new List<PropertyListing>();

                foreach (var listing in candidates)
                {
                    var isCopyOfSurvivor = survivors.Any(survivor => IsWithinDuplicateReferenceRange(survivor, listing));

                    if (!isCopyOfSurvivor)
                        survivors.Add(listing);
                }

                kept.AddRange(survivors);
            }

            // The grouping above scrambles the caller's order; everything downstream that cares
            // about order sorts for itself, but a stable return keeps results reproducible.
            return kept.OrderBy(x => x.Id).ToList();
        }

        /// <summary>
        /// Whether two adverts with the same reference are close enough to be one flat.
        /// </summary>
        private static bool IsWithinDuplicateReferenceRange(PropertyListing left, PropertyListing right)
        {
            var metres = Calculator.CalculateDistanceMeters(
                (double)left.Latitude, (double)left.Longitude,
                (double)right.Latitude, (double)right.Longitude);

            return metres <= DuplicateReferenceMeters;
        }

        /// <summary>

        /// <summary>
        /// The share of its own município's asking rate below which a listing stops being a cheap
        /// example of the local market and starts being a different kind of asset altogether.
        ///
        /// A fifth. Deliberately far below anything a bargain hunter would call a bargain - a
        /// genuine underpriced flat is 10% or 30% under, not 80%. What sits down there is
        /// timeshare weeks, garages filed as studios, land, and adverts whose price lost a digit:
        /// a 27 m2 studio in Albufeira asking EUR 13,500, where the município asks EUR 4,781/m2.
        /// </summary>
        private const decimal LocallyImplausibleFraction = 0.20m;

        /// <summary>
        /// How many listings a município needs before its median is steady enough to reject
        /// anything on. Below this the floor is not applied at all - a handful of adverts cannot
        /// establish what a place charges, and guessing would delete the thin markets first.
        /// </summary>
        private const int MinimumForLocalFloor = 30;

        /// <summary>
        /// Drops listings priced so far below their own município that they cannot be describing
        /// the same kind of property as everything around them.
        ///
        /// The absolute floor above cannot do this job on a national dataset, and raising it
        /// would be worse than leaving it. EUR 400/m2 is a data fault in Albufeira and an
        /// ordinary asking price in Bragança; one number cannot be right in both, so the
        /// comparison has to be local. Município rather than district on purpose - a district
        /// median blends the coast with the interior, and a genuine ruin in inland Faro would
        /// look like a fault measured against Vilamoura.
        ///
        /// This is a judgement, not a measurement, and it is set where it costs almost nothing:
        /// it removes on the order of 0.1% of rows, all of them at the bottom of the distribution
        /// where the model's worst misses live.
        /// </summary>
        private static List<PropertyListing> RejectLocallyImplausible(List<PropertyListing> listings)
        {
            var floorByMunicipality = listings
                .Where(x => !string.IsNullOrWhiteSpace(x.MarketArea?.Municipality))
                .GroupBy(x => Calculator.NormalizeText(x.MarketArea!.Municipality))
                .Where(x => x.Count() >= MinimumForLocalFloor)
                .ToDictionary(
                    x => x.Key,
                    x => MedianPricePerM2(x) * LocallyImplausibleFraction);

            return listings.Where(x => IsLocallyPlausible(x, floorByMunicipality)).ToList();
        }

        /// <summary>
        /// Kept unless its own município has a floor and this listing sits under it. A listing
        /// whose município we do not know, or which we have too few listings to judge, is always
        /// kept: no evidence is a reason to leave a row alone, not to delete it.
        /// </summary>
        private static bool IsLocallyPlausible(PropertyListing listing, Dictionary<string, decimal> floorByMunicipality)
        {
            var municipality = listing.MarketArea?.Municipality;

            if (string.IsNullOrWhiteSpace(municipality))
                return true;

            if (!floorByMunicipality.TryGetValue(Calculator.NormalizeText(municipality), out var floor))
                return true;

            return NewestSnapshot(listing) is not { } snapshot || snapshot.PricePerM2 >= floor;
        }

        /// <summary>The middle asking rate of a group, used to set its floor.</summary>
        private static decimal MedianPricePerM2(IEnumerable<PropertyListing> listings)
        {
            var sorted = listings
                .Select(x => NewestSnapshot(x)?.PricePerM2 ?? 0m)
                .OrderBy(x => x)
                .ToList();

            if (sorted.Count == 0)
                return 0m;

            return sorted.Count % 2 != 0
                ? sorted[sorted.Count / 2]
                : (sorted[sorted.Count / 2] + sorted[(sorted.Count / 2) - 1]) / 2m;
        }
        /// The agency reference plus what the advert says the property physically is. Null when
        /// there is no reference to key on, which keeps the row out of the second pass entirely.
        /// </summary>
        private static string? SourceReferenceKeyOf(PropertyListing listing)
        {
            var reference = SourceReferenceOf(listing);

            return reference is null
                ? null
                : $"{reference}|{(int)listing.Typology}|{listing.AreaM2}|{(int)listing.PropertyType}";
        }

        /// <summary>
        /// The agency's own reference for a listing. The scraper writes it as the first field of
        /// <see cref="PropertyListing.Notes"/> - <c>"Ref: 26960236 | Area basis: bruta | ..."</c> -
        /// so it is read back out rather than stored in a column of its own.
        ///
        /// Read through this one method: when the reference does get promoted to a real column,
        /// this is the only body that has to change.
        /// </summary>
        internal static string? SourceReferenceOf(PropertyListing listing)
        {
            const string prefix = "Ref:";

            var notes = listing.Notes;

            if (string.IsNullOrWhiteSpace(notes) || !notes.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            var afterPrefix = notes.AsSpan(prefix.Length);
            var end = afterPrefix.IndexOf('|');

            var reference = (end < 0 ? afterPrefix : afterPrefix[..end]).Trim();

            return reference.IsEmpty ? null : reference.ToString();
        }

        /// <summary>
        /// What makes two adverts the same property: same place, same layout, same floor area,
        /// same asking price, and the same point on the map. The coordinates are what separate a
        /// re-advertisement from a neighbour who happens to be asking a round number - without
        /// them this key merges genuinely different flats, because asking prices cluster hard on
        /// figures like EUR 350,000.
        ///
        /// A row with no price keeps a key of its own and is never merged: we cannot tell whether
        /// it is a copy, and guessing loses real listings.
        /// </summary>
        private static string DuplicateKeyOf(PropertyListing listing)
        {
            var price = NewestSnapshot(listing)?.Price;

            if (price is null)
                return $"listing:{listing.Id}";

            return $"{listing.MarketAreaId}|{(int)listing.Typology}|{listing.AreaM2}|{price.Value}|" +
                   $"{listing.Latitude}|{listing.Longitude}";
        }
    }
}
