namespace SmartMeterReadingDash.Models.Dashboard
{
    public class DepartmentWiseSummaryBypl
    {
        public string Department { get; set; }
        public int TotalMeters { get; set; }
        public int HesDownload { get; set; }
        public int Pending { get; set; }
        public int Failed { get; set; }
        public int Mismatch { get; set; }
        public int DownlaodFailed { get; set; }
    }
}
