using Microsoft.Extensions.Primitives;

namespace SmartMeterReadingDash.Models.Dashboard
{
    public class CMRIDownload
    {
        public string cons_ref { get; set; }
        public string meterno { get; set; }
        public string Cycle { get; set; }
        public string Division { get; set; }
        public string Department { get; set; }
        public string Address { get; set; }
        public string SapSeqNo { get; set; }
        public string KWH { get; set; }
        public string KVAH { get; set; }
        public string MeterType { get; set; }
        public string Phase { get; set; }
        public DateTime? ReadingDate { get; set; }

    }
}
