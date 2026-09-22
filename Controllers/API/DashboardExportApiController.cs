using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMeterReadingDash.Models.Dashboard;
using SmartMeterReadingDash.Services;

namespace SmartMeterReadingDash.Controllers.API
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardExportApiController : ControllerBase
    {
        private readonly Dashboard _dashboard;
        private readonly UserAccess _userAccess;
        private readonly ILogger<DashboardExportApiController> _logger;
        private readonly ReportGenService _reportGenService;

        public DashboardExportApiController( Dashboard dashboard,  UserAccess userAccess, ReportGenService reportGenService, ILogger<DashboardExportApiController> logger)
        {
            _dashboard = dashboard;
            _userAccess = userAccess;
            _reportGenService = reportGenService;
            _logger = logger;
        }

        [HttpGet("export-report")]
        public async Task<IActionResult> ExportDashboard( [FromQuery] string readingMonth)
        {
            if (string.IsNullOrWhiteSpace(readingMonth))
            {
                return BadRequest("Reading month is required.");
            }

            try
            {
                var scope = _userAccess.GetScope(User);

                if (scope == null)
                {
                    return Unauthorized();
                }

                var report = await _reportGenService.GenerateReportAsync( readingMonth, scope);

                return File(  report.FileBytes, report.ContentType,  report.FileName);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard export failed for reading month {ReadingMonth}.", readingMonth);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to export dashboard."
                });
            }
        }
    }
}