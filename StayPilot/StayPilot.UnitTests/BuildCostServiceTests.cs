using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Services;

namespace StayPilot.UnitTests
{
    // The build cost rates.
    //
    // Two things worth defending. First, INE being unreachable must leave the screen at 2021
    // prices - not at zero, and not at a guess. Second, the ratios must keep reproducing the
    // published 2026 quotes they were fitted to: if someone edits one without checking it against
    // a source, that is what these catch.
    public class BuildCostServiceTests
    {
        // INE indicator 0011748, June 2026. Base 2021 = 100.
        private static readonly ConstructionIndex June2026 = new(134.33m, 144.70m, 126.24m, "Junho de 2026");

        private sealed class StubIne(ConstructionIndex? index) : IIneRepository
        {
            public Task<ConstructionIndex?> GetConstructionIndexAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(index);
            }
        }

        private static Task<BuildCostBasisResponse> Basis()
        {
            return new BuildCostService(new StubIne(June2026)).GetBuildCostBasisAsync();
        }

        /// <summary>INE unreachable, throttled, or answering with something unusable.</summary>
        private static Task<BuildCostBasisResponse> BasisWithoutIndex()
        {
            return new BuildCostService(new StubIne(null)).GetBuildCostBasisAsync();
        }

        private static decimal Rate(List<BuildCostOption> options, string key)
        {
            return options.Single(option => option.Key == key).RatePerM2!.Value;
        }

        private static decimal Cost(List<BuildCostOption> options, string key)
        {
            return options.Single(option => option.Key == key).Cost!.Value;
        }

        [Fact]
        public async Task StandardTier_LandsInThePublishedRange()
        {
            var basis = await Basis();

            // Published 2026 figures put a normal build at EUR 950-1500/m2. The anchor is only
            // honest if escalating it lands inside that - this is the test that says the numbers
            // were fitted to reality rather than to a round number.
            Assert.InRange(Rate(basis.Tiers, "standard"), 950m, 1500m);
            Assert.Equal(1343m, Rate(basis.Tiers, "standard"));
            Assert.Equal("Junho de 2026", basis.IndexPeriod);
            Assert.Equal(34.3m, basis.SinceBasePercent);
        }

        [Fact]
        public async Task WithoutTheIndex_EverythingFallsBackTo2021Prices()
        {
            var basis = await BasisWithoutIndex();

            // Not zero and not a guess. The empty period is how the screen knows to say so.
            Assert.Equal(1000m, Rate(basis.Tiers, "standard"));
            Assert.Equal(string.Empty, basis.IndexPeriod);
            Assert.Equal(0m, basis.SinceBasePercent);
        }

        [Fact]
        public async Task EquipmentEscalatesOnMaterials_NotOnTheBlendedIndex()
        {
            var basis = await Basis();

            // The whole reason the two halves are read separately: labour is 18 points ahead of
            // materials, and a lift is a bought machine rather than a job.
            // One-off fees round to the nearest EUR 10, so this compares on that grid; the point
            // of the test is which index was applied, and the blended one lands elsewhere.
            Assert.Equal(Math.Round(12675m * 1.2624m / 10m) * 10m, Cost(basis.Elevators, "two"));
        }

        [Theory]
        [InlineData("modular", 550, 680)]
        [InlineData("concrete", 1000, 1200)]
        [InlineData("infinity", 1750, 2050)]
        public async Task PoolRates_ReproduceTheObserved2026QuoteBands(string key, decimal low, decimal high)
        {
            var basis = await Basis();

            // Fitted against 2026 quotes: modular ~600, concrete ~1100, infinity ~1900 EUR/m2.
            Assert.InRange(Rate(basis.Pools, key), low, high);
        }

        [Fact]
        public async Task PoolFloor_IsTheRateOverItsMinimumSurface()
        {
            var basis = await Basis();
            var concrete = basis.Pools.Single(pool => pool.Key == "concrete");

            // A small pool still needs excavation, a plant room and filtration, and those cost
            // what they cost. A 6x3 concrete pool is quoted at EUR 20-35k.
            Assert.InRange(concrete.MinCost!.Value, 18000m, 25000m);
            Assert.Equal(Math.Round(concrete.RatePerM2!.Value * 18m), concrete.MinCost.Value);
        }

        [Fact]
        public async Task GarageBays_ReproduceTheObservedPrices()
        {
            var basis = await Basis();

            // A 30 m2 garage attached to a house is quoted at EUR 15-30k in 2026.
            Assert.InRange(Cost(basis.Garages, "two"), 15000m, 30000m);
        }

        [Fact]
        public async Task FullKnx_ReproducesTheCypePricedSystem()
        {
            var basis = await Basis();

            // CYPE's gerador de precos puts a complete KNX system at EUR 12,499 - the anchor was
            // fitted by dividing that across a 150 m2 house.
            Assert.InRange(Rate(basis.Automation, "knx") * 150m, 11500m, 13500m);
        }

        [Fact]
        public async Task Garden_ReproducesTheObservedRate()
        {
            var basis = await Basis();

            // A finished garden runs about EUR 55/m2: turf, irrigation, planting and paving.
            Assert.InRange(basis.GardenRatePerM2, 48m, 62m);
            Assert.Equal(Math.Round(basis.GardenRatePerM2 * 300m), Cost(basis.Gardens, "medium"));
        }

        [Fact]
        public async Task Solar_DoesNotEscalate()
        {
            var escalated = await Basis();
            var flat = await BasisWithoutIndex();

            // Panels got cheaper since 2021 while building got dearer. Running them through a
            // construction index would invent a rise that never happened.
            Assert.Equal(Cost(flat.Solar, "battery"), Cost(escalated.Solar, "battery"));
            Assert.Equal(3400m, escalated.Solar.Single(option => option.Key == "battery").Grant);
        }
    }
}
