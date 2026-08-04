using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartMeterReadingDash.Services;

namespace SmartMeterReadingDash.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardApiController : ControllerBase
    {
        private readonly Dashboard _dashboard;
        public DashboardApiController(Dashboard dashboard)
        {
            _dashboard = dashboard;
        }

        [HttpGet("meter-type-wise-summary")]
        public IActionResult GetMeterSummary()
        {
            try
            {
                var summary = _dashboard.GetMeterSummary();
                return Ok(summary);

            }catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
           
        }

        [HttpGet("meter-download-summary")]
        public IActionResult GetMeterDownloadSummary()
        {
            try
            {
                var summary = _dashboard.GetMeterReceivedDownloadSummary();

                return Ok(summary);
            }
            catch(Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }

        }
        [HttpGet("meter-download-detailed-summary")]
        public IActionResult GetMeterDownloadDetailedSummary()
        {
            try
            {
                var summaryList = _dashboard.MeterDetailedSummary();
                return Ok(summaryList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }
        [HttpGet("reading_trend_date_wise")]
        public IActionResult GetReadingTrend()
        {
            try
            {
                var readingDateWiseList = _dashboard.GetReadingTrend();
                return Ok(readingDateWiseList);
            }
            catch(Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }
        [HttpGet("department-wise-data")]
        public IActionResult GetDepartmentWiseData()
        {
            try
            {
                var departmentWiseData = _dashboard.GetDepartmentSummary();

                return Ok(departmentWiseData);
            }
            catch( Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpGet("temp-hes-wise-data")]
        public IActionResult TempGetHESDATA()
        {
            try
            {
                var tempdata = _dashboard.TempDashBoardHESCount();

                return Ok(tempdata);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }
        [HttpGet("temp-hes-failed-wise-data")]
        public IActionResult TempGetHESDATAFailed()
        {
            try
            {
                var tempdatafailed = _dashboard.TempHESFailed();

                return Ok(tempdatafailed);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }
    } 
}
