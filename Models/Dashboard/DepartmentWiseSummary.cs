namespace SmartMeterReadingDash.Models.Dashboard
{
    public class DepartmentWiseSummary
    {
        public string Department { get; set; } 
        public int TotalMeters { get; set; }
        public int HesDownload { get; set; }
        public int Pending {  get; set; }
        public int Manual {  get; set; }
        public int Mismatch { get; set; }
        public int NonCom {  get; set; }
    }
}
