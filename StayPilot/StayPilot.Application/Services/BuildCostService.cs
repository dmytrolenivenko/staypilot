using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Application.Services
{
    /// <summary>
    /// Prices the Build Cost screen from one live number.
    ///
    /// Nobody in Portugal publishes an API for "what does a pool cost" - not the retailers, not
    /// the quote marketplaces. What INE does publish, monthly and free, is the construction cost
    /// index. So rather than storing a price list that starts rotting the day it is written, this
    /// holds one anchor and a set of ratios:
    ///
    ///   - A standard build was about €1,000/m² in 2021, the index base year.
    ///   - Everything else is a dimensionless ratio of that. A concrete pool is 0.82 of the house
    ///     rate per m² of water, a garage bay 0.55, a finished garden 0.041. Ratios do not rot -
    ///     what a pool costs relative to a house does not move with inflation, only the price of
    ///     both does.
    ///   - A few things are machines rather than areas (a lift, a KNX bus, a borehole), so they
    ///     are anchored in 2021 euros instead and escalated by the materials half of the index.
    ///
    /// The anchors were fitted to real published 2026 quotes and then left to escalate themselves.
    /// Cross-check: €1,000 × the June 2026 index of 134.33 is €1,343/m², against €950-1,500/m² in
    /// published quotes; the pool ratios come out at €604, €1,101 and €1,894 per m² against
    /// observed bands of roughly €600, €1,100 and €1,900.
    ///
    /// INE is allowed to fail. When it does, everything comes back at 2021 prices with an empty
    /// period and no error - a projection that admits it is behind is useful; a 500 is not.
    /// </summary>
    public class BuildCostService : IBuildCostService
    {
        /// <summary>
        /// A standard mid-range build, € per m², in 2021 - the year the index is based on.
        /// The single number the whole screen hangs off.
        /// </summary>
        private const decimal StandardRatePerM2In2021 = 1000m;

        /// <summary>The index reads 100 in its base year, so a rate at base is a rate ÷ 100.</summary>
        private const decimal IndexBase = 100m;

        /// <summary>Standard rate on new construction in mainland Portugal.</summary>
        private const decimal VatPercent = 23m;

        /// <summary>Quality tiers, as multiples of the standard rate.</summary>
        private static readonly (string Key, string Label, decimal Ratio)[] Tiers =
        [
            ("economy", "Economy (basic finishes)", 0.75m),
            ("standard", "Standard (mid-range)", 1.00m),
            ("premium", "Premium (high-end finishes)", 1.33m),
            ("luxury", "Luxury (bespoke)", 1.85m)
        ];

        /// <summary>
        /// Pools, per m² of water surface, plus the surface below which the price stops falling.
        /// The floor is in m² rather than euros so it escalates with everything else, and it
        /// exists because excavation, plant room and filtration cost roughly the same whatever
        /// size the water is - about 40% of a pool budget is that infrastructure.
        /// </summary>
        private static readonly (string Key, string Label, decimal Ratio, decimal FloorM2)[] Pools =
        [
            ("modular", "Modular / prefab shell", 0.45m, 12m),
            ("concrete", "Reinforced concrete", 0.82m, 18m),
            ("infinity", "Overflow / infinity edge", 1.41m, 30m)
        ];

        /// <summary>Pool equipment, in 2021 euros. Products, so escalated by materials.</summary>
        private static readonly (string Key, string Label, decimal CostIn2021)[] PoolAddons =
        [
            ("heatpump", "Heat pump (heated pool)", 3565m),
            ("cover", "Telescopic cover", 11090m),
            ("salt", "Salt electrolysis", 1585m)
        ];

        /// <summary>
        /// A garage is cheaper per m² than living space - no kitchen, no bathroom, simpler
        /// finishes - but it is the same trade on the same site, so it tracks the same rate.
        /// A car needs 14-18 m², two need 25-36, three about 45.
        /// </summary>
        private const decimal GarageRatio = 0.55m;

        private static readonly (string Key, string Label, decimal AreaM2, string Note)[] Garages =
        [
            ("one", "1 car (~16 m²)", 16m, "incl. sectional gate"),
            ("two", "2 cars (~30 m²)", 30m, "incl. automatic gate"),
            ("three", "3 cars (~45 m²)", 45m, "incl. automatic gate")
        ];

        /// <summary>A two stop lift installed, and each stop after it. 2021 euros, materials.</summary>
        private const decimal ElevatorTwoStopIn2021 = 12675m;
        private const decimal ElevatorExtraStopIn2021 = 3565m;

        /// <summary>
        /// A complete KNX bus, € per m² of built area, in 2021. Automation scales with the house
        /// rather than with a shopping list: more rooms means more circuits, sensors and cable.
        /// Fitted to CYPE's priced KNX system of €12,499 across a 150 m² house.
        /// </summary>
        private const decimal KnxRatePerM2In2021 = 67m;

        private static readonly (string Key, string Label, decimal Ratio)[] Automation =
        [
            ("devices", "Smart devices (Wi-Fi lights, plugs, thermostat)", 0.12m),
            ("wired", "Partial wired (lighting, blinds, HVAC)", 0.47m),
            ("knx", "Full KNX bus (whole house)", 1.00m)
        ];

        /// <summary>A finished garden per m²: turf, irrigation, planting and paving.</summary>
        private const decimal GardenRatio = 0.041m;

        /// <summary>Fixed sizes, because nobody knows their garden in m².</summary>
        private static readonly (string Key, string Label, decimal AreaM2)[] Gardens =
        [
            ("small", "Small (~100 m²)", 100m),
            ("medium", "Medium (~300 m²)", 300m),
            ("large", "Large (~800 m²)", 800m)
        ];

        /// <summary>Extras priced per m² of built area, as ratios of the house rate.</summary>
        private static readonly (string Key, string Label, decimal Ratio)[] RatedExtras =
        [
            ("ac", "Ducted air conditioning", 0.034m),
            ("underfloor", "Underfloor heating", 0.045m)
        ];

        /// <summary>
        /// Flat priced extras, in 2021 euros. <c>IsEquipment</c> picks the materials half of the
        /// index instead of the blended one - a charger is a product, a borehole is a job.
        /// </summary>
        private static readonly (string Key, string Label, decimal CostIn2021, bool IsEquipment, string Note)[] FlatExtras =
        [
            ("thermal", "Solar thermal water heating", 1980m, true, ""),
            ("borehole", "Borehole / water well (furo)", 4467m, false, "common in the Algarve"),
            ("walls", "Boundary walls & gates", 8933m, false, ""),
            ("outdoor", "Outdoor kitchen / BBQ", 4467m, false, ""),
            ("evcharger", "EV charger (7.4 kW)", 950m, true, "")
        ];

        /// <summary>
        /// Solar, in 2026 euros, deliberately NOT escalated: photovoltaics got cheaper since 2021
        /// while construction got dearer, so a construction index would invent a rise that did
        /// not happen. The grant is the 2026 state comparticipação ceiling.
        /// </summary>
        private static readonly (string Key, string Label, decimal Cost, decimal Grant)[] Solar =
        [
            ("small", "3 kWp (~8 panels)", 4500m, 1700m),
            ("medium", "6 kWp (~13 panels)", 7500m, 1700m),
            ("battery", "6 kWp + 10 kWh battery", 14000m, 3400m)
        ];

        private readonly IIneRepository _ine;

        public BuildCostService(IIneRepository ine)
        {
            _ine = ine;
        }

        /// <inheritdoc/>
        public async Task<BuildCostBasisResponse> GetBuildCostBasisAsync(CancellationToken cancellationToken = default)
        {
            var index = await _ine.GetConstructionIndexAsync(cancellationToken);

            // Labour and materials have drifted a long way apart - 144.7 against 126.2 in June
            // 2026 - so a concrete pool and an imported lift must not move by the same number.
            // No index at all leaves both at 1, which is 2021 prices, which the screen says.
            var blended = Escalation(index?.Total);
            var materials = Escalation(index?.Materials);

            // Rounded here, once, and everything derives from the rounded figure. The screen
            // shows the rate and shows the working, so "€55/m² × 300 m²" has to actually come to
            // the garden price beside it - deriving from the unrounded rate is how a receipt ends
            // up not adding up.
            var standardRate = Round(StandardRatePerM2In2021 * blended);
            var gardenRate = Round(standardRate * GardenRatio);
            var knxRate = Round(KnxRatePerM2In2021 * materials);

            return new BuildCostBasisResponse
            {
                IndexPeriod = index?.Period ?? string.Empty,
                SinceBasePercent = index is null ? 0m : Round(index.Value.Total - IndexBase, 1),
                GardenRatePerM2 = Round(gardenRate),
                VatPercent = VatPercent,

                Tiers = Tiers
                    .Select(tier => new BuildCostOption
                    {
                        Key = tier.Key,
                        Label = tier.Label,
                        RatePerM2 = Round(standardRate * tier.Ratio)
                    })
                    .ToList(),

                Pools = Pools
                    .Select(pool => new BuildCostOption
                    {
                        Key = pool.Key,
                        Label = pool.Label,
                        RatePerM2 = Round(standardRate * pool.Ratio),
                        MinCost = Round(standardRate * pool.Ratio) * pool.FloorM2,
                        Note = $"floor is {pool.FloorM2:0} m² of water"
                    })
                    .ToList(),

                PoolAddons = PoolAddons
                    .Select(addon => new BuildCostOption
                    {
                        Key = addon.Key,
                        Label = addon.Label,
                        Cost = RoundToTen(addon.CostIn2021 * materials)
                    })
                    .ToList(),

                Garages = Garages
                    .Select(bay => new BuildCostOption
                    {
                        Key = bay.Key,
                        Label = bay.Label,
                        Cost = Round(standardRate * GarageRatio * bay.AreaM2),
                        AreaM2 = bay.AreaM2,
                        Note = bay.Note
                    })
                    .ToList(),

                Elevators = BuildElevators(materials),

                Automation = Automation
                    .Select(level => new BuildCostOption
                    {
                        Key = level.Key,
                        Label = level.Label,
                        RatePerM2 = Round(knxRate * level.Ratio)
                    })
                    .ToList(),

                Gardens = Gardens
                    .Select(size => new BuildCostOption
                    {
                        Key = size.Key,
                        Label = size.Label,
                        Cost = Round(gardenRate * size.AreaM2),
                        AreaM2 = size.AreaM2
                    })
                    .ToList(),

                Solar = Solar
                    .Select(option => new BuildCostOption
                    {
                        Key = option.Key,
                        Label = option.Label,
                        Cost = option.Cost,
                        Grant = option.Grant,
                        Note = "2026 price, not escalated"
                    })
                    .ToList(),

                Extras = BuildExtras(standardRate, blended, materials)
            };
        }

        /// <summary>The multiplier that carries a 2021 price to the index's month. 1 when there is none.</summary>
        private static decimal Escalation(decimal? indexLevel)
        {
            return indexLevel is > 0m ? indexLevel.Value / IndexBase : 1m;
        }

        private static List<BuildCostOption> BuildElevators(decimal materials)
        {
            var elevators = new List<BuildCostOption>();

            // Two stops is the base machine; everything after is the same lift with more doors.
            for (var stops = 2; stops <= 4; stops++)
            {
                elevators.Add(new BuildCostOption
                {
                    Key = stops switch { 2 => "two", 3 => "three", _ => "four" },
                    Label = stops == 2 ? "2 stops (ground + 1)" : $"{stops} stops",
                    Cost = RoundToTen((ElevatorTwoStopIn2021 + ElevatorExtraStopIn2021 * (stops - 2)) * materials)
                });
            }

            return elevators;
        }

        private static List<BuildCostOption> BuildExtras(decimal standardRate, decimal blended, decimal materials)
        {
            var extras = RatedExtras
                .Select(extra => new BuildCostOption
                {
                    Key = extra.Key,
                    Label = extra.Label,
                    RatePerM2 = Round(standardRate * extra.Ratio)
                })
                .ToList();

            extras.AddRange(FlatExtras.Select(extra => new BuildCostOption
            {
                Key = extra.Key,
                Label = extra.Label,
                Cost = RoundToTen(extra.CostIn2021 * (extra.IsEquipment ? materials : blended)),
                Note = extra.Note
            }));

            return extras;
        }


        /// <summary>
        /// Rounds a one-off fee to the nearest EUR 10. A fee is a price, not a measurement, and
        /// escalation lands them on EUR 6,001 and EUR 1,199 - which reads as precision nobody has.
        /// Rates per m2 keep their euro because they get multiplied by an area afterwards; these
        /// are terminal, and the working shown on screen derives from this rounded figure, so the
        /// receipt still adds up.
        /// </summary>
        private static decimal RoundToTen(decimal value)
        {
            return Math.Round(value / 10m, 0, MidpointRounding.AwayFromZero) * 10m;
        }
        private static decimal Round(decimal value, int decimals = 0)
        {
            return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        }
    }
}
