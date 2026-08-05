namespace SmartMeterReadingDash.Models.Dashboard
{
    public class MeterReceivedSummary
    {
        public int totalMetersCount { get; set; }
        public int hesDownloadCount { get; set; }
        public int manualForwardinCount { get; set; }
        public int pendingCount { get; set; }
        public int mismatchCount { get; set; }
        public decimal hesDownloadPercentage { get; set; }
        public decimal hesFailedPercentage { get; set; }
    }
}
