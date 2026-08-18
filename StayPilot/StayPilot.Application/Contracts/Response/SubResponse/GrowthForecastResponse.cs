namespace StayPilot.Application.Contracts.Response.SubResponse
{
    /// <summary>
    /// What a property could be worth in the years ahead, with the two rates behind it kept apart.
    ///
    /// The seeded rate is a planning assumption for the district - read <see cref="SeededSource"/>,
    /// which says so in words. The local rate is measured from the adverts around the property.
    /// The blend is what the projection actually runs on. All three are here on purpose: a forecast
    /// nobody can take apart is a forecast nobody should act on.
    /// </summary>
    public class GrowthForecastResponse
    {
        /// <summary>The seeded long run rate for this district, percent per year.</summary>
        public decimal SeededAnnualPercent { get; set; }

        /// <summary>Where the seeded rate came from. Print it - it is not a measurement.</summary>
        public string SeededSource { get; set; } = string.Empty;

        /// <summary>The district the seeded rate was looked up for.</summary>
        public string SeededDistrict { get; set; } = string.Empty;

        /// <summary>What the local adverts say, percent per year. Null when too little history.</summary>
        public decimal? LocalAnnualPercent { get; set; }

        /// <summary>How much of the blend the local rate got, in percent. Never above 50.</summary>
        public decimal LocalWeightPercent { get; set; }

        /// <summary>True when the local rate hit its cap and was held there.</summary>
        public bool LocalWasCapped { get; set; }

        /// <summary>How many price observations the local rate rests on.</summary>
        public int LocalSnapshotCount { get; set; }

        /// <summary>How many days the local series runs across.</summary>
        public int LocalSpanDays { get; set; }

        /// <summary>How many separate months the local series has points in.</summary>
        public int LocalMonthsObserved { get; set; }

        /// <summary>What was measured locally and what was not, in a sentence.</summary>
        public string LocalReason { get; set; } = string.Empty;

        /// <summary>The rate the projection runs on: the two above, weighted together.</summary>
        public decimal BlendedAnnualPercent { get; set; }

        /// <summary>How many years the paths below run for.</summary>
        public int Years { get; set; }

        /// <summary>Conservative, Base and Optimistic, in that order.</summary>
        public List<GrowthScenarioResponse> Scenarios { get; set; } = new();
    }

    /// <summary>One projected path.</summary>
    public class GrowthScenarioResponse
    {
        /// <summary>Conservative, Base or Optimistic.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The rate this path compounds at, percent per year.</summary>
        public decimal AnnualPercent { get; set; }

        /// <summary>Value at the end of the first year.</summary>
        public decimal NextYearValue { get; set; }

        /// <summary>Value at the end of the last year projected.</summary>
        public decimal FinalYearValue { get; set; }

        /// <summary>Value at the end of each year, index 0 being today.</summary>
        public List<decimal> Values { get; set; } = new();
    }
}
