namespace SmartMeterReadingDash.Models.Dashboard
{
    public class MeterReceivedSummaryBypl
    {
        public int totalMetersCount { get; set; }
        public int hesDownloadCount { get; set; }
        public int hesFailedCount { get; set; }
        public decimal hesDownloadPercentage { get; set; }
        public decimal hesFailedPercentage { get; set; }
    }
}
