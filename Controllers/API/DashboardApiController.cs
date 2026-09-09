using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMeterReadingDash.Models.Dashboard;
using SmartMeterReadingDash.Services;

namespace SmartMeterReadingDash.Controllers.API
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardApiController : ControllerBase
    {
        private readonly Dashboard _dashboard;
        private readonly UserAccess _userAccess;

        public DashboardApiController( Dashboard dashboard, UserAccess userAccess)
        {
            _dashboard = dashboard;
            _userAccess = userAccess;
        }

        // AUTHORIZATION HELPERS

        private UserAccessScope GetCurrentScope()
        {
            return _userAccess.GetScope(User);
        }

        private IActionResult? CheckCompanyAccess( UserAccessScope scope,string company)
        {
            // SUPERADMIN can access everything
            if (scope.IsSuperAdmin)
            {
                return null;
            }

            // Non-superadmin must have a company
            if (string.IsNullOrWhiteSpace(scope.Company))
            {
                return Forbid();
            }

            // Company mismatch
            if (!string.Equals(scope.Company, company,StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            return null;
        }


        // BRPL

        [HttpGet("meter-type-wise-summary")]
        public IActionResult GetMeterSummary(string ReadingMonth)
        {
            try
            {
                var scope = GetCurrentScope();

                var access = CheckCompanyAccess(scope, "BRPL");

                if (access != null) return access;

                var summary =_dashboard.GetMeterSummary( ReadingMonth,scope);

                return Ok(summary);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid user authentication."
                });
            }
            catch (Exception)
            {
                return StatusCode( StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Internal Server Error"
                    });
            }
        }

        // BYPL

        [HttpGet("meter-type-wise-summary-bypl")]
        public IActionResult GetMeterSummaryBypl(
            string ReadingMonth)
        {
            try
            {
                var scope = GetCurrentScope();

                var access =
                    CheckCompanyAccess(scope, "BYPL");

                if (access != null)
                    return access;

                var summary =
                    _dashboard.GetByplTotalMeterSummary(
                        ReadingMonth,
                        scope);

                return Ok(summary);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal Server Error"
                });
            }
        }


        // ============================================================
        // BRPL DOWNLOAD SUMMARY
        // ============================================================

        [HttpGet("meter-download-summary")]
        public IActionResult GetMeterDownloadSummary(
            string ReadingMonth)
        {
            try
            {
                var scope = GetCurrentScope();

                var access =
                    CheckCompanyAccess(scope, "BRPL");

                if (access != null)
                    return access;

                var summary =
                    _dashboard.GetMeterReceivedDownloadSummary(
                        ReadingMonth,
                        scope);

                return Ok(summary);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal Server Error"
                });
            }
        }


        // ============================================================
        // BYPL DOWNLOAD SUMMARY
        // ============================================================

        [HttpGet("meter-download-summary-bypl")]
        public IActionResult GetMeterDownloadSummaryBypl(
            string ReadingMonth)
        {
            try
            {
                var scope = GetCurrentScope();

                var access =
                    CheckCompanyAccess(scope, "BYPL");

                if (access != null)
                    return access;

                var summary =
                    _dashboard.GetMeterReceivedSummaryBypl(
                        ReadingMonth,
                        scope);

                return Ok(summary);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal Server Error"
                });
            }
        }


        // ============================================================
        // BRPL DETAILED
        // ============================================================

        [HttpGet("meter-download-detailed-summary")]
        public IActionResult GetMeterDownloadDetailedSummary(
            string ReadingMonth)
        {
            try
            {
                var scope = GetCurrentScope();

                var access =
                    CheckCompanyAccess(scope, "BRPL");

                if (access != null)
                    return access;

                var summaryList =
                    _dashboard.MeterDetailedSummary(
                        ReadingMonth,
                        scope);

                return Ok(summaryList);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal Server Error"
                });
            }
        }


        // ============================================================
        // BYPL DETAILED
        // ============================================================

        [HttpGet("meter-download-detailed-summary-bypl")]
        public IActionResult GetMeterDownloadDetailedSummaryBypl(
            string ReadingMonth)
        {
            try
            {
                var scope = GetCurrentScope();

                var access =
                    CheckCompanyAccess(scope, "BYPL");

                if (access != null)
                    return access;

                var summaryList =
                    _dashboard.GetMeterDownloadDetailedSummaryBypl(
                        ReadingMonth,
                        scope);

                return Ok(summaryList);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal Server Error"
                });
            }
        }


        // ============================================================
        // BRPL READING TREND
        // ============================================================

        [HttpGet("reading_trend_date_wise")]
        public IActionResult GetReadingTrend(
            string ReadingMonth)
        {
            try
            {
                var scope = GetCurrentScope();

                var access =
                    CheckCompanyAccess(scope, "BRPL");

                if (access != null)
                    return access;

                var result =
                    _dashboard.GetReadingTrend(
                        ReadingMonth,
                        scope);

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal Server Error"
                });
            }
        }


        // ============================================================
        // BRPL DEPARTMENT
        // ============================================================

        [HttpGet("department-wise-data")]
        public IActionResult GetDepartmentWiseData(
            string ReadingMonth)
        {
            try
            {
                var scope = GetCurrentScope();

                var access =
                    CheckCompanyAccess(scope, "BRPL");

                if (access != null)
                    return access;

                var result =
                    _dashboard.GetDepartmentSummary(
                        ReadingMonth,
                        scope);

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal Server Error"
                });
            }
        }


        // ============================================================
        // BYPL DEPARTMENT
        // ============================================================

        [HttpGet("department-wise-data-bypl")]
        public IActionResult GetDepartmentWiseSummaryBYPL(
            string ReadingMonth)
        {
            try
            {
                var scope = GetCurrentScope();

                var access =
                    CheckCompanyAccess(scope, "BYPL");

                if (access != null)
                    return access;

                var result =
                    _dashboard.GetDepartmentWiseSummaryBypl(
                        ReadingMonth,
                        scope);

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal Server Error"
                });
            }
        }


        // ============================================================
        // BRPL FAILURE REASON
        // ============================================================

        [HttpGet("failure-reason-count")]
        public IActionResult GetFailureReasonCount(
            string ReadingMonth)
        {
            try
            {
                var scope = GetCurrentScope();

                var access =
                    CheckCompanyAccess(scope, "BRPL");

                if (access != null)
                    return access;

                var result =
                    _dashboard.FailureReasonCounts(
                        ReadingMonth,
                        scope);

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal Server Error"
                });
            }
        }

        [HttpGet("failure-reason-count-bypl")]
        public IActionResult GetFailureReasonCountBypl(
            string ReadingMonth)
        {
            try
            {
                var scope = GetCurrentScope();

                var access =
                    CheckCompanyAccess(scope, "BYPL");

                if (access != null)
                    return access;

                var result =
                    _dashboard.FailureReasonCountsBYPL(
                        ReadingMonth,
                        scope);

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal Server Error"
                });
            }
        }


        // ============================================================
        // BRPL HES DOWNLOAD DETAILS
        // ============================================================

        [HttpGet("hes-download-meters-details")]
        public IActionResult GetHESDownloadMetersDetails(
            string ReadingMonth)
        {
            try
            {
                var scope = GetCurrentScope();

                var access =
                    CheckCompanyAccess(scope, "BRPL");

                if (access != null)
                    return access;

                var result =
                    _dashboard.HesDownloadMeterList(
                        ReadingMonth,
                        scope);

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal Server Error"
                });
            }
        }


        // ============================================================
        // BYPL HES DOWNLOAD DETAILS
        // ============================================================

        [HttpGet("hes-download-meters-details-bypl")]
        public IActionResult GetHESDownloadMetersDetailsBypl(
            string ReadingMonth)
        {
            try
            {
                var scope = GetCurrentScope();

                var access =
                    CheckCompanyAccess(scope, "BYPL");

                if (access != null)
                    return access;

                var result =
                    _dashboard.GetHesDownloadBypl(
                        ReadingMonth,
                        scope);

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal Server Error"
                });
            }
        }
    }
}