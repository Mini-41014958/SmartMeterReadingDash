using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartMeterReadingDash.Services;

namespace SmartMeterReadingDash.Controllers.API
{
    [Authorize]
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
        public IActionResult GetMeterSummary(string ReadingMonth)
        {
            try
            {
                var summary = _dashboard.GetMeterSummary(ReadingMonth);
                return Ok(summary);

            }catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
           
        }
        //BYPL
        [HttpGet("meter-type-wise-summary-bypl")]
        public IActionResult GetMeterSummaryBypl(string ReadingMonth)
        {
            try
            {
                var summary = _dashboard.GetByplTotalMeterSummary(ReadingMonth);
                return Ok(summary);

            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }

        }

        [HttpGet("meter-download-summary")]
        public IActionResult GetMeterDownloadSummary(string ReadingMonth)
        {
            try
            {
                var summary = _dashboard.GetMeterReceivedDownloadSummary(ReadingMonth);

                return Ok(summary);
            }
            catch(Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }

        }
        //BYPL
        [HttpGet("meter-download-summary-bypl")]
        public IActionResult GetMeterDownloadSummaryBypl(string ReadingMonth)
        {
            try
            {
                var summary = _dashboard.GetMeterReceivedSummaryBypl(ReadingMonth);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpGet("meter-download-detailed-summary")]
        public IActionResult GetMeterDownloadDetailedSummary(string ReadingMonth)
        {
            try
            {
                var summaryList = _dashboard.MeterDetailedSummary(ReadingMonth);
                return Ok(summaryList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        //BYPL
        [HttpGet("meter-download-detailed-summary-bypl")]
        public IActionResult GetMeterDownloadDetailedSummaryBypl(string ReadingMonth)
        {
            try
            {
                var summaryList = _dashboard.GetMeterDownloadDetailedSummaryBypl(ReadingMonth);
                return Ok(summaryList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpGet("reading_trend_date_wise")]
        public IActionResult GetReadingTrend(string ReadingMonth)
        {
            try
            {
                var readingDateWiseList = _dashboard.GetReadingTrend(ReadingMonth);
                return Ok(readingDateWiseList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }
        [HttpGet("department-wise-data")]
        public IActionResult GetDepartmentWiseData(string ReadingMonth)
        {
            try
            {
                var departmentWiseData = _dashboard.GetDepartmentSummary(ReadingMonth);

                return Ok(departmentWiseData);
            }
            catch( Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        //BYPL
        [HttpGet("department-wise-data-bypl")]
        public IActionResult GetDepartmentWiseSummaryBYPL(string ReadingMonth)
        {
            try
            {
                var summary = _dashboard.GetDepartmentWiseSummaryBypl(ReadingMonth);
                return Ok(summary);
            }
            catch(Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpGet("failure-reason-count")]
        public IActionResult GetFailureReasonCount(string ReadingMonth)
        {
            try
            {
                var failureReasonCount = _dashboard.FailureReasonCounts(ReadingMonth);
                return Ok(failureReasonCount);
            }
            catch(Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        //BYPL

        [HttpGet("failure-reason-count-bypl")]
        public IActionResult GetFailureReasonCountBypl(string ReadingMonth)
        {
            try
            {
                var failureReasonCount = _dashboard.FailureReasonCountsBYPL(ReadingMonth);
                return Ok(failureReasonCount);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }


        //[HttpGet("temp-hes-wise-data")]
        //public IActionResult TempGetHESDATA()
        //{
        //    try
        //    {
        //        var tempdata = _dashboard.TempDashBoardHESCount();

        //        return Ok(tempdata);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "Internal Server Error");
        //    }
        //}
        //[HttpGet("temp-hes-failed-wise-data")]
        //public IActionResult TempGetHESDATAFailed()
        //{
        //    try
        //    {
        //        var tempdatafailed = _dashboard.TempHESFailed();

        //        return Ok(tempdatafailed);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "Internal Server Error");
        //    }
        //}
    } 
}
