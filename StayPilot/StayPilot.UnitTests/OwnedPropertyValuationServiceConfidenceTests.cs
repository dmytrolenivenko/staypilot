using StayPilot.Application.Services;
using StayPilot.Domain.Enums;

namespace StayPilot.UnitTests
{
    /// <summary>
    /// Confidence has to be believable from the comp table a user is actually looking at: plenty
    /// of nearby comps reads High, a handful further out reads Medium, and nothing nearby reads
    /// Low.
    /// </summary>
    public class OwnedPropertyValuationServiceConfidenceTests
    {
        [Fact]
        public void ConfidenceFromComps_TenNearbyWithinOneKilometre_IsHigh()
        {
            var confidence = OwnedPropertyValuationService.ConfidenceFromComps(comparablesUsed: 10, nearestComparableMeters: 900);

            Assert.Equal(ValuationConfidence.High, confidence);
        }

        [Fact]
        public void ConfidenceFromComps_TenComparablesButNearestIsFar_DropsToMedium()
        {
            // Plenty of comps, but the closest one is past the High-confidence distance.
            var confidence = OwnedPropertyValuationService.ConfidenceFromComps(comparablesUsed: 10, nearestComparableMeters: 4_000);

            Assert.Equal(ValuationConfidence.Medium, confidence);
        }

        [Fact]
        public void ConfidenceFromComps_FewComparablesWithinFiveKilometres_IsMedium()
        {
            var confidence = OwnedPropertyValuationService.ConfidenceFromComps(comparablesUsed: 2, nearestComparableMeters: 4_500);

            Assert.Equal(ValuationConfidence.Medium, confidence);
        }

        [Fact]
        public void ConfidenceFromComps_NoComparablesNearby_IsLow()
        {
            var confidence = OwnedPropertyValuationService.ConfidenceFromComps(comparablesUsed: 3, nearestComparableMeters: 8_000);

            Assert.Equal(ValuationConfidence.Low, confidence);
        }

        [Fact]
        public void ConfidenceFromComps_NoComparablesAtAll_IsLow()
        {
            var confidence = OwnedPropertyValuationService.ConfidenceFromComps(comparablesUsed: 0, nearestComparableMeters: double.MaxValue);

            Assert.Equal(ValuationConfidence.Low, confidence);
        }
    }
}
