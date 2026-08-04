namespace SmartMeterReadingDash.Models.Dashboard
{
    public class MeterDownloadDetailedSummary
    {
        public string MeterNumber { get; set; }
        public string SapDepartment { get; set; }
        public string MeterType { get; set; }
        public string Status { get; set; }
        public string SchedulerMessage { get; set; }
        public DateTime ? EntryDate { get; set; }
    }
}
