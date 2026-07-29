namespace StayPilot.Application.Contracts.Response.SubResponse
{
    public class EquitySummary
    {
        public decimal PurchasePrice { get; set; }  // what you paid
        public decimal CurrentEstimate { get; set; }  // = MidPrice, copied here for a self-contained block
        public decimal GainAmount { get; set; }  // CurrentEstimate − PurchasePrice
        public decimal GainPercent { get; set; }  // GainAmount / PurchasePrice × 100
        public int YearsHeld { get; set; }  // today − PurchaseDate
        public decimal RoiPerYear { get; set; }  // annualised return: GainPercent ÷ years held (0 if held < 1 year)
        public decimal RoiPerMonth { get; set; }  // monthly return: GainPercent ÷ months held (0 if not held yet)
    }
}
