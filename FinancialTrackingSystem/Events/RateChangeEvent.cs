namespace FinancialTrackingSystem.Events
{
    public class RateChangeEvent
    {
        public string Symbol { get; set; }
        public decimal OldRate { get; set; }
        public decimal NewRate { get; set; }
    }
}
