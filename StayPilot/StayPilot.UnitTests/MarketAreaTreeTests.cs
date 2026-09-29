using StayPilot.Application.Services;
using StayPilot.Domain.Entities;

namespace StayPilot.UnitTests
{
    /// <summary>
    /// Tests for <see cref="MarketAreaService.BuildTree"/> - the one-shot place tree the place
    /// picker fills its dropdowns from. It replaced a call per level, so it has to give back
    /// exactly what those calls did: the same grouping, the same order, the same zones.
    /// </summary>
    public class MarketAreaTreeTests
    {
        private static MarketArea Area(string district, string municipality, string town, string? zone = null)
        {
            return new MarketArea { District = district, Municipality = municipality, Town = town, Zone = zone };
        }

        [Fact]
        public void BuildTree_GroupsEachLevelUnderItsParent()
        {
            var tree = MarketAreaService.BuildTree(new List<MarketArea>
            {
                Area("Faro", "Loulé", "Quarteira"),
                Area("Faro", "Loulé", "Almancil"),
                Area("Faro", "Albufeira", "Guia"),
                Area("Lisboa", "Cascais", "Estoril")
            });

            Assert.Equal(new[] { "Faro", "Lisboa" }, tree.Select(x => x.Name));

            var faro = tree[0];
            Assert.Equal(new[] { "Albufeira", "Loulé" }, faro.Municipalities.Select(x => x.Name));
            Assert.Equal(new[] { "Almancil", "Quarteira" }, faro.Municipalities[1].Towns.Select(x => x.Name));
        }

        [Fact]
        public void BuildTree_SortsAccentedNamesWithTheirLetter()
        {
            // Ordinal order would put "Évora" after "Viseu"; the old SQL picker put it with the E's.
            var tree = MarketAreaService.BuildTree(new List<MarketArea>
            {
                Area("Viseu", "Viseu", "Viseu"),
                Area("Évora", "Évora", "Évora"),
                Area("Beja", "Beja", "Beja")
            });

            Assert.Equal(new[] { "Beja", "Évora", "Viseu" }, tree.Select(x => x.Name));
        }

        [Fact]
        public void BuildTree_TownRowWithoutZone_IsNotListedAsAZone()
        {
            var tree = MarketAreaService.BuildTree(new List<MarketArea>
            {
                Area("Faro", "Loulé", "Quarteira"),
                Area("Faro", "Loulé", "Quarteira", "Marina"),
                Area("Faro", "Loulé", "Quarteira", "Centro"),
                Area("Faro", "Loulé", "Quarteira", "Centro")
            });

            var zones = tree[0].Municipalities[0].Towns[0].Zones;

            Assert.Equal(new[] { "Centro", "Marina" }, zones);
        }

        [Fact]
        public void BuildTree_SameNameInDifferentCase_IsOnePlace()
        {
            // The old picker's SQL DISTINCT was case-insensitive; the tree must not split them.
            var tree = MarketAreaService.BuildTree(new List<MarketArea>
            {
                Area("Faro", "Loulé", "Quarteira"),
                Area("FARO", "Loulé", "Almancil")
            });

            Assert.Single(tree);
            Assert.Equal(2, tree[0].Municipalities[0].Towns.Count);
        }
    }
}
