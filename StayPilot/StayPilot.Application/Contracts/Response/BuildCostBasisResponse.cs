using System.Text.Json.Serialization;
using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Every rate the Build Cost screen needs, worked out for today rather than stored.
    ///
    /// No figure here is a saved price. Each is a 2021 anchor times INE's current construction
    /// cost index, so when INE publishes a new month the screen moves on its own. Read
    /// <see cref="IndexPeriod"/> first: when it is empty INE could not be reached and every rate
    /// below is at 2021 prices, which the screen says out loud rather than quietly presenting as
    /// current.
    /// </summary>
    public class BuildCostBasisResponse : ResponseBase
    {
        /// <summary>The month behind these rates, as INE writes it: "Junho de 2026". Empty when unavailable.</summary>
        public string IndexPeriod { get; set; } = string.Empty;

        /// <summary>How much dearer building is than in 2021, from INE's index. Zero when unavailable.</summary>
        public decimal SinceBasePercent { get; set; }

        /// <summary>Build quality tiers, € per m² of built area.</summary>
        public List<BuildCostOption> Tiers { get; set; } = [];

        /// <summary>Pool types, € per m² of water surface, each with the floor it stops falling below.</summary>
        public List<BuildCostOption> Pools { get; set; } = [];

        /// <summary>Pool equipment, flat prices.</summary>
        public List<BuildCostOption> PoolAddons { get; set; } = [];

        /// <summary>Garage bays by car count.</summary>
        public List<BuildCostOption> Garages { get; set; } = [];

        /// <summary>Lifts by number of stops.</summary>
        public List<BuildCostOption> Elevators { get; set; } = [];

        /// <summary>Home automation levels, € per m² of built area.</summary>
        public List<BuildCostOption> Automation { get; set; } = [];

        /// <summary>Garden sizes, each carrying its area and what that area comes to.</summary>
        public List<BuildCostOption> Gardens { get; set; } = [];

        /// <summary>Solar kits. The only prices here that do not escalate.</summary>
        public List<BuildCostOption> Solar { get; set; } = [];

        /// <summary>The checkbox extras, some per m² and some flat.</summary>
        public List<BuildCostOption> Extras { get; set; } = [];

        /// <summary>A finished garden, € per m². The rate behind every garden size above.</summary>
        public decimal GardenRatePerM2 { get; set; }

        /// <summary>Standard rate on new construction in mainland Portugal.</summary>
        public decimal VatPercent { get; set; }
    }

    /// <summary>
    /// One priced choice, already escalated to today.
    ///
    /// Deliberately one shape for tiers, pools, garages, lifts, gardens, solar and the checkbox
    /// extras - they differ only in which fields are filled, and nine near-identical classes
    /// would buy nothing. Read whichever of <see cref="RatePerM2"/> and <see cref="Cost"/> is
    /// present; never both.
    /// </summary>
    public class BuildCostOption
    {
        /// <summary>The stable key the screen selects on, for example "concrete".</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>What the dropdown reads.</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Price per m², where the item is priced by area.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? RatePerM2 { get; set; }

        /// <summary>Flat price, where the item is a thing rather than an area.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? Cost { get; set; }

        /// <summary>Pools only: the price stops falling below this, because the plant room does not.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? MinCost { get; set; }

        /// <summary>The area the option stands for. Garden sizes and garage bays.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? AreaM2 { get; set; }

        /// <summary>State support against this item. Solar only, and never netted off Cost.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? Grant { get; set; }

        /// <summary>A short qualifier, for example "incl. automatic gate". Empty when there is none.</summary>
        public string Note { get; set; } = string.Empty;
    }
}
