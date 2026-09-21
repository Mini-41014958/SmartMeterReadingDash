using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
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

        public DashboardExportApiController(Dashboard dashboard, UserAccess userAccess, ILogger<DashboardExportApiController> logger)
        {
            _dashboard = dashboard;
            _userAccess = userAccess;
            _logger = logger;
        }

        private UserAccessScope GetCurrentScope() => _userAccess.GetScope(User);

        private static class Palette
        {
            public static readonly XLColor Navy = XLColor.FromHtml("#003B7A");
            public static readonly XLColor Blue = XLColor.FromHtml("#0066CC");
            public static readonly XLColor LightBlue = XLColor.FromHtml("#EAF4FF");

            public static readonly XLColor Green = XLColor.FromHtml("#149447");
            public static readonly XLColor LightGreen = XLColor.FromHtml("#EAF8EF");

            public static readonly XLColor Red = XLColor.FromHtml("#D71920");
            public static readonly XLColor LightRed = XLColor.FromHtml("#FDECEC");

            public static readonly XLColor Orange = XLColor.FromHtml("#F39C12");
            public static readonly XLColor LightOrange = XLColor.FromHtml("#FFF5E3");

            public static readonly XLColor Dark = XLColor.FromHtml("#243447");
            public static readonly XLColor Grey = XLColor.FromHtml("#64748B");
            public static readonly XLColor LightGrey = XLColor.FromHtml("#F1F5F9");
            public static readonly XLColor Border = XLColor.FromHtml("#CBD5E1");

            public static readonly XLColor White = XLColor.White;
            public static readonly XLColor Black = XLColor.FromHtml("#0F172A");
        }

        private const string BodyFont = "Aptos";

        [HttpGet("export-report")]
        public IActionResult ExportDashboard([FromQuery] string readingMonth)
        {
            if (string.IsNullOrWhiteSpace(readingMonth))
            {
                return BadRequest("Reading month is required.");
            }

            try
            {
                var scope = GetCurrentScope();
                bool isSuperAdmin = scope.IsSuperAdmin;
                string company = scope.Company?.Trim().ToUpperInvariant() ?? "";
                bool showBrpl = isSuperAdmin || company == "BRPL";
                bool showBypl = isSuperAdmin || company == "BYPL";

                if (!isSuperAdmin && !showBrpl && !showBypl)
                {
                    _logger.LogWarning("Dashboard export denied. User {User} has invalid company scope {Company}.", User.Identity?.Name, company);
                    return Forbid();
                }

                TotalMeterSummary? meterSummary = null;
                TotalMeterSummaryBypl? meterSummaryBypl = null;

                MeterReceivedSummary? brplReceived = null;
                List<MeterReceivedSummaryBypl>? byplReceived = null;

                List<MeterDownloadDetailedSummary>? brplFailed = null;
                List<MeterDownloadDetailedSummaryBypl>? byplFailed = null;

                List<HesDownloadMeter>? brplDownloaded = null;
                List<HesDownloadMeterBypl>? byplDownloaded = null;

                // BRPL
                if (showBrpl)
                {
                    meterSummary = _dashboard.GetMeterSummary(readingMonth, scope);
                    brplReceived = _dashboard.GetMeterReceivedDownloadSummary(readingMonth, scope);
                    brplFailed = _dashboard.MeterDetailedSummary(readingMonth, scope);
                    brplDownloaded = _dashboard.HesDownloadMeterList(readingMonth, scope);
                }

                // BYPL
                if (showBypl)
                {
                    meterSummaryBypl = _dashboard.GetByplTotalMeterSummary(readingMonth, scope);
                    byplReceived = _dashboard.GetMeterReceivedSummaryBypl(readingMonth, scope);
                    byplFailed = _dashboard.GetMeterDownloadDetailedSummaryBypl(readingMonth, scope);
                    byplDownloaded = _dashboard.GetHesDownloadBypl(readingMonth, scope);
                }

                // CREATE WORKBOOK
                using var workbook = new XLWorkbook();

                CreateDashboardSheet(workbook, readingMonth, meterSummary, meterSummaryBypl, brplReceived, byplReceived, scope);

                if (showBrpl)
                {
                    CreateBrplDownloadedSheet(workbook, brplDownloaded ?? new List<HesDownloadMeter>());
                    CreateBrplFailedSheet(workbook, brplFailed ?? new List<MeterDownloadDetailedSummary>());
                }

                if (showBypl)
                {
                    CreateByplDownloadedSheet(workbook, byplDownloaded ?? new List<HesDownloadMeterBypl>());
                    CreateByplFailedSheet(workbook, byplFailed ?? new List<MeterDownloadDetailedSummaryBypl>());
                }

                var dashboardSheet = workbook.Worksheets.Worksheet("Dashboard");
                dashboardSheet.SetTabActive();
                dashboardSheet.Position = 1;

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var safeMonth = readingMonth
                    .Replace(",", "_")
                    .Replace("/", "_")
                    .Replace("\\", "_")
                    .Replace(" ", "");

                var companySuffix = isSuperAdmin ? "BRPL_BYPL" : company;
                var fileName = $"Smart_Meter_Reading_Dashboard_{companySuffix}_{safeMonth}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return base.File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Smart meter dashboard export failed for readingMonth {ReadingMonth}.", readingMonth);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Unable to export dashboard. Check the application log for the underlying exception."
                });
            }
        }

        private void CreateDashboardSheet(XLWorkbook workbook, string readingMonth, TotalMeterSummary? brplMeter,TotalMeterSummaryBypl? byplMeter, MeterReceivedSummary? brplReceived,
            List<MeterReceivedSummaryBypl>? byplReceived, UserAccessScope scope)
        {
            var ws = workbook.Worksheets.Add("Dashboard");

            bool isSuperAdmin = scope.IsSuperAdmin;
            string company = scope.Company?.Trim().ToUpperInvariant() ?? "";
            bool showBrpl = isSuperAdmin || company == "BRPL";
            bool showBypl = isSuperAdmin || company == "BYPL";
            var brplDepartment = showBrpl ? _dashboard.GetDepartmentSummary(readingMonth, scope) ?? new() : new();
            var byplDepartment = showBypl ? _dashboard.GetDepartmentWiseSummaryBypl(readingMonth, scope) ?? new() : new();
            var brplFailureReasons = showBrpl ? _dashboard.FailureReasonCounts(readingMonth, scope) ?? new() : new();
            var byplFailureReasons = showBypl ? _dashboard.FailureReasonCountsBYPL(readingMonth, scope) ?? new() : new();

            ws.TabColor = Palette.Blue;
            ws.ShowGridLines = false;
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.Margins.Top = 0.15;
            ws.PageSetup.Margins.Bottom = 0.15;
            ws.PageSetup.Margins.Left = 0.15;
            ws.PageSetup.Margins.Right = 0.15;
            ws.PageSetup.FitToPages(1, 0);

            ws.Column("A").Width = 2;
            ws.Column("B").Width = 16;
            ws.Column("C").Width = 14;
            ws.Column("D").Width = 16;
            ws.Column("E").Width = 14;
            ws.Column("F").Width = 16;
            ws.Column("G").Width = 14;
            ws.Column("H").Width = 16;
            ws.Column("I").Width = 14;
            ws.Column("J").Width = 16;
            ws.Column("K").Width = 14;

            ws.Range("B1:K200").Style.Font.FontColor = Palette.Dark;

            ws.Range("B2:K4").Merge();
            ws.Cell("B2").Value = isSuperAdmin  ? "SMART METER READING DASHBOARD – BRPL & BYPL" : $"SMART METER READING DASHBOARD – {company}";

            ws.Range("B2:K4").Style.Fill.BackgroundColor = Palette.Navy;
            ws.Range("B2:K4").Style.Font.FontColor = Palette.White;
            ws.Range("B2:K4").Style.Font.Bold = true;
            ws.Range("B2:K4").Style.Font.FontSize = 17;
            ws.Range("B2:K4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Range("B2:K4").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range("B2:K4").Style.Alignment.Indent = 5;
            ws.Range("B2:K4").Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            ws.Range("B2:K4").Style.Border.BottomBorderColor = Palette.Blue;

            ws.Row(2).Height = 20;
            ws.Row(3).Height = 20;
            ws.Row(4).Height = 20;

            string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "bses-logo.png");

            if (System.IO.File.Exists(logoPath))
            {
                var picture = ws.AddPicture(logoPath).WithPlacement(XLPicturePlacement.FreeFloating).WithSize(55, 32);
                picture.MoveTo(ws.Cell("B2"), 5, 4);
            }

            ws.Range("B5:K5").Merge();
            ws.Cell("B5").Value = $"READING MONTH: {readingMonth}   |   REPORT GENERATED: {DateTime.Now:dd MMM yyyy HH:mm}";
            ws.Range("B5:K5").Style.Font.FontColor = Palette.Grey;
            ws.Range("B5:K5").Style.Font.FontSize = 9;
            ws.Range("B5:K5").Style.Font.Italic = true;
            ws.Range("B5:K5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Range("B5:K5").Style.Alignment.Indent = 1;
            ws.Row(5).Height = 18;

            // TOTAL

            var bypl = byplReceived?.FirstOrDefault();

            int brplTotal = showBrpl ? brplReceived?.totalMetersCount ?? 0 : 0;
            int brplDownloaded = showBrpl ? brplReceived?.hesDownloadCount ?? 0 : 0;
            int brplFailed = showBrpl ? brplReceived?.manualForwardinCount ?? 0 : 0;

            int byplTotal = showBypl ? bypl?.totalMetersCount ?? 0 : 0;
            int byplDownloaded = showBypl ? bypl?.hesDownloadCount ?? 0 : 0;
            int byplFailed = showBypl ? bypl?.hesFailedCount ?? 0 : 0;

            int totalMeters = brplTotal + byplTotal;
            int totalDownloaded = brplDownloaded + byplDownloaded;
            int totalFailed = brplFailed + byplFailed;

            double downloadRate = totalMeters > 0 ? (double)totalDownloaded / totalMeters : 0;
            double failureRate = totalMeters > 0 ? (double)totalFailed / totalMeters : 0;

            // READING OVERVIEW

            WriteDashboardBanner(ws,7, isSuperAdmin ? "READING OVERVIEW – BRPL + BYPL" : $"READING OVERVIEW – {company}", Palette.Navy, Palette.White);

            CreateModernKpiCard(ws, 8, 2, 3, "TOTAL METERS", totalMeters.ToString("#,##0"), Palette.Blue);
            CreateModernKpiCard(ws, 8, 4, 5, "DOWNLOADED", totalDownloaded.ToString("#,##0"), Palette.Green);
            CreateModernKpiCard(ws, 8, 6, 7, "FAILED", totalFailed.ToString("#,##0"), Palette.Red);
            CreateModernKpiCard(ws, 8, 8, 9, "DOWNLOAD RATE", downloadRate.ToString("0.0%"), Palette.Blue);
            CreateModernKpiCard(ws, 8, 10, 11, "FAILURE RATE", failureRate.ToString("0.0%"), Palette.Orange);

            // COMPANY PERFORMANCE

            WriteDashboardBanner(ws, 12, isSuperAdmin ? "COMPANY PERFORMANCE " : $"COMPANY PERFORMANCE – {company}", Palette.Navy, Palette.White);

            if (isSuperAdmin)
            {
                WriteCompanyPanel(ws, 13, 2, 6, "BRPL", brplTotal, brplDownloaded, brplFailed, Palette.Blue, Palette.LightBlue);
                WriteCompanyPanel(ws, 13, 7, 11, "BYPL", byplTotal, byplDownloaded, byplFailed, Palette.Green, Palette.LightGreen);
            }
            else if (showBrpl)
            {
                WriteCompanyPanel(ws, 13, 2, 6, "BRPL", brplTotal, brplDownloaded, brplFailed, Palette.Blue, Palette.LightBlue);
            }
            else if (showBypl)
            {
                WriteCompanyPanel(ws, 13, 2, 6, "BYPL", byplTotal, byplDownloaded, byplFailed, Palette.Green, Palette.LightGreen);
            }

            // DEPARTMENT PERFORMANCE

            int departmentSectionRow = 18;

            WriteDashboardBanner(ws, departmentSectionRow, "DEPARTMENT PERFORMANCE", Palette.Navy, Palette.White);

            // Company headers
            int departmentCompanyHeaderRow = departmentSectionRow + 1;

            if (showBrpl)
            {
                WriteDepartmentCompanyHeader( ws, departmentCompanyHeaderRow,2,6,"BRPL",  Palette.Blue);
            }

            if (showBypl)
            {
                WriteDepartmentCompanyHeader( ws, departmentCompanyHeaderRow, 7, 11, "BYPL",Palette.Green);
            }

            // Column headers
            int departmentHeaderRow = departmentCompanyHeaderRow + 1;

            if (showBrpl)
            {
                SetCompactHeader( ws, departmentHeaderRow, 2,new[]
                    {
                    "DEPARTMENT",
                    "DOWNLOADED",
                    "FAILED",
                    "DOWNLOAD RATE",
                    "FAILURE RATE"
                    }
                );
            }

            if (showBypl)
            {
                SetCompactHeader( ws, departmentHeaderRow, 7, new[]
                    {
                    "DEPARTMENT",
                    "DOWNLOADED",
                    "FAILED",
                    "DOWNLOAD RATE",
                    "FAILURE RATE"
                    }
                );
            }

            // BRPL DEPARTMENT DATA

            int brplDeptRow = departmentHeaderRow + 1;

            if (showBrpl)
            {
                foreach (var item in brplDepartment)
                {
                    int total = item.HesDownload + item.Failed;

                    ws.Cell(brplDeptRow, 2).Value = string.IsNullOrWhiteSpace(item.Department)
                            ? "Unassigned"
                            : item.Department;

                    ws.Cell(brplDeptRow, 3).Value = item.HesDownload;
                    ws.Cell(brplDeptRow, 4).Value = item.Failed;

                    ws.Cell(brplDeptRow, 5).Value = total > 0
                            ? (double)item.HesDownload / total
                            : 0;

                    ws.Cell(brplDeptRow, 6).Value = total > 0
                            ? (double)item.Failed / total
                            : 0;

                    brplDeptRow++;
                }
            }

            // BYPL DEPARTMENT DATA

            int byplDeptRow = departmentHeaderRow + 1;

            if (showBypl)
            {
                foreach (var item in byplDepartment)
                {
                    int total = item.HesDownload + item.Failed;

                    ws.Cell(byplDeptRow, 7).Value = string.IsNullOrWhiteSpace(item.Department)
                            ? "Unassigned"
                            : item.Department;

                    ws.Cell(byplDeptRow, 8).Value = item.HesDownload;
                    ws.Cell(byplDeptRow, 9).Value = item.Failed;

                    ws.Cell(byplDeptRow, 10).Value = total > 0
                            ? (double)item.HesDownload / total
                            : 0;

                    ws.Cell(byplDeptRow, 11).Value =  total > 0
                            ? (double)item.Failed / total
                            : 0;

                    byplDeptRow++;
                }
            }

            int departmentEndRow = Math.Max(brplDeptRow, byplDeptRow) - 1;


            // STYLE BRPL TABLE

            if (showBrpl)
            {
                StyleModernTable( ws, departmentHeaderRow, departmentEndRow,2, 6 );
            }

            // STYLE BYPL TABLE

            if (showBypl)
            {
                StyleModernTable( ws, departmentHeaderRow, departmentEndRow, 7, 11);
            }

            // DEPARTMENT PERCENTAGES

            if (departmentEndRow >= departmentHeaderRow + 1)
            {
                if (showBrpl)
                {
                    var brplPercentageRange = ws.Range( departmentHeaderRow + 1, 5, departmentEndRow, 6);

                    brplPercentageRange.Style.NumberFormat.Format = "0.0%";
                    brplPercentageRange.Style.Fill.BackgroundColor = Palette.White;
                    brplPercentageRange.Style.Font.FontColor = Palette.Dark;
                }

                if (showBypl)
                {
                    var byplPercentageRange = ws.Range(departmentHeaderRow + 1, 10,
                        departmentEndRow,
                        11
                    );

                    byplPercentageRange.Style.NumberFormat.Format = "0.0%";
                    byplPercentageRange.Style.Fill.BackgroundColor = Palette.White;
                    byplPercentageRange.Style.Font.FontColor = Palette.Dark;
                }
            }

            // FAILURE ANALYSIS

            int failureSectionRow = departmentEndRow + 3;
            WriteDashboardBanner(ws, failureSectionRow, "FAILURE ANALYSIS", Palette.Red, Palette.White);

            int failureHeaderRow = failureSectionRow + 1;

            if (showBrpl)
            {
                SetCompactHeader( ws,failureHeaderRow, 2, new[] { "BRPL FAILURE REASON", "COUNT", "FAILURE %" });
            }

            if (showBypl)
            {
                SetCompactHeader( ws, failureHeaderRow,7, new[] { "BYPL FAILURE REASON", "COUNT", "FAILURE %" } );
            }

            int brplFailureRow = failureHeaderRow + 1;
            int brplFailureTotal = showBrpl ? brplFailureReasons.Sum(x => x.count ?? 0) : 0;

            if (showBrpl)
            {
                foreach (var item in brplFailureReasons.OrderByDescending(x => x.count ?? 0))
                {
                    int count = item.count ?? 0;

                    ws.Cell(brplFailureRow, 2).Value =  string.IsNullOrWhiteSpace(item.FeilureReason) ? "Unknown" : item.FeilureReason;

                    ws.Cell(brplFailureRow, 3).Value = count;

                    ws.Cell(brplFailureRow, 4).Value = brplFailureTotal > 0 ? (double)count / brplFailureTotal : 0;

                    brplFailureRow++;
                }
            }

            int byplFailureRow = failureHeaderRow + 1;

            int byplFailureTotal = showBypl ? byplFailureReasons.Sum(x => x.count ?? 0) : 0;

            if (showBypl)
            {
                foreach (var item in byplFailureReasons.OrderByDescending(x => x.count ?? 0))
                {
                    int count = item.count ?? 0;

                    ws.Cell(byplFailureRow, 7).Value = string.IsNullOrWhiteSpace(item.FeilureReason) ? "Unknown" : item.FeilureReason;

                    ws.Cell(byplFailureRow, 8).Value = count;

                    ws.Cell(byplFailureRow, 9).Value =  byplFailureTotal > 0  ? (double)count / byplFailureTotal : 0;

                    byplFailureRow++;
                }
            }

            int failureEndRow = Math.Max(brplFailureRow, byplFailureRow) - 1;

            if (showBrpl)
            {
                StyleModernTable(ws,failureHeaderRow,failureEndRow, 2,4);
            }

            if (showBypl)
            {
                StyleModernTable( ws, failureHeaderRow, failureEndRow, 7,  9);
            }

            if (failureEndRow >= failureHeaderRow + 1)
            {
                if (showBrpl)
                {
                    ws.Range( failureHeaderRow + 1,3, failureEndRow,3 ).Style.NumberFormat.Format = "#,##0";

                    ws.Range( failureHeaderRow + 1, 4,failureEndRow, 4).Style.NumberFormat.Format = "0.0%";

                    // Plain numeric failure counts
                    ws.Range(failureHeaderRow + 1, 3,failureEndRow, 3 ).Style.Fill.BackgroundColor = Palette.White;

                    ws.Range( failureHeaderRow + 1, 3,failureEndRow,3).Style.Font.FontColor = Palette.Dark;
                }

                if (showBypl)
                {
                    ws.Range( failureHeaderRow + 1, 8,failureEndRow, 8 ).Style.NumberFormat.Format = "#,##0";

                    ws.Range(failureHeaderRow + 1, 9,failureEndRow,9).Style.NumberFormat.Format = "0.0%";

                    // Plain numeric failure counts
                    ws.Range( failureHeaderRow + 1, 8,failureEndRow, 8 ).Style.Fill.BackgroundColor = Palette.White;

                    ws.Range(failureHeaderRow + 1, 8, failureEndRow, 8 ).Style.Font.FontColor = Palette.Dark;
                }
            }

            // MANAGEMENT SUMMARY

            int footerRow = failureEndRow + 2;

            ws.Range(footerRow, 2, footerRow, 11).Merge();

            ws.Cell(footerRow, 2).Value = $"{CompanySummaryLabel(isSuperAdmin, company)}  |  " + 
                $"{totalDownloaded:#,##0} of {totalMeters:#,##0} meters successfully downloaded ({downloadRate:0.0%})  |  " +
                $"{totalFailed:#,##0} failed ({failureRate:0.0%}).";

            var footer = ws.Range(footerRow, 2, footerRow, 11);
            footer.Style.Fill.BackgroundColor = Palette.LightGrey;
            footer.Style.Font.FontColor = Palette.Dark;
            footer.Style.Font.Bold = true;
            footer.Style.Font.FontSize = 9.5;
            footer.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            footer.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            footer.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            footer.Style.Border.OutsideBorderColor = Palette.Border;

            ws.Row(footerRow).Height = 28;

            // FINAL FORMATTING

            var usedRange = ws.RangeUsed();

            if (usedRange != null)
            {
                usedRange.Style.Font.FontName = BodyFont;
                usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            ws.SheetView.FreezeRows(5);
        }

        private static string CompanySummaryLabel(bool isSuperAdmin, string company)
        {
            return isSuperAdmin
                ? "MANAGEMENT SUMMARY – BRPL + BYPL"
                : $"MANAGEMENT SUMMARY – {company}";
        }


        // DASHBOARD BANNER
        private static void WriteDashboardBanner(IXLWorksheet ws, int row, string title, XLColor background, XLColor fontColor)
        {
            var range = ws.Range(row, 2, row, 11);
            range.Merge();
            range.FirstCell().Value = title;

            range.Style.Fill.BackgroundColor = background;
            range.Style.Font.FontColor = fontColor;
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 10.5;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Alignment.Indent = 1;

            ws.Row(row).Height = 20;
        }

        // KPI CARD
        private static void CreateModernKpiCard(IXLWorksheet ws, int row, int firstColumn, int lastColumn, string label, string value, XLColor accent)
        {
            var card = ws.Range(row, firstColumn, row + 2, lastColumn);
            card.Style.Fill.BackgroundColor = Palette.White;
            card.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            card.Style.Border.OutsideBorderColor = Palette.Border;

            var accentRange = ws.Range(row, firstColumn, row, lastColumn);
            accentRange.Style.Fill.BackgroundColor = accent;

            var labelRange = ws.Range(row + 1, firstColumn, row + 1, lastColumn);
            labelRange.Merge();
            labelRange.FirstCell().Value = label;
            labelRange.Style.Font.FontColor = Palette.Grey;
            labelRange.Style.Font.Bold = true;
            labelRange.Style.Font.FontSize = 8.5;
            labelRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            labelRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var valueRange = ws.Range(row + 2, firstColumn, row + 2, lastColumn);
            valueRange.Merge();
            valueRange.FirstCell().Value = value;
            valueRange.Style.Font.FontColor = accent;
            valueRange.Style.Font.Bold = true;
            valueRange.Style.Font.FontSize = 19;
            valueRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            valueRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Row(row).Height = 5;
            ws.Row(row + 1).Height = 17;
            ws.Row(row + 2).Height = 29;
        }

        // COMPANY PANEL

        private static void WriteCompanyPanel( IXLWorksheet ws,int row,int firstColumn, int lastColumn, string company,int total, int downloaded, int failed, XLColor accent,  XLColor background)
        {
            var title = ws.Range(row, firstColumn, row, lastColumn);
            title.Merge();
            title.FirstCell().Value = company;
            title.Style.Fill.BackgroundColor = accent;
            title.Style.Font.FontColor = Palette.White;
            title.Style.Font.Bold = true;
            title.Style.Font.FontSize = 12;
            title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            title.Style.Alignment.Indent = 1;

            string[] headers = { "TOTAL", "DOWNLOADED", "FAILED", "DOWNLOAD RATE", "FAILURE RATE" };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row + 1, firstColumn + i).Value = headers[i];
            }

            var headerRange = ws.Range(row + 1, firstColumn, row + 1, lastColumn);
            headerRange.Style.Fill.BackgroundColor = background;
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontSize = 8;
            headerRange.Style.Font.FontColor = Palette.Dark;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Alignment.WrapText = true;

            var valueRange = ws.Range(row + 2, firstColumn, row + 2, lastColumn);

            ws.Cell(row + 2, firstColumn).Value = total;
            ws.Cell(row + 2, firstColumn + 1).Value = downloaded;
            ws.Cell(row + 2, firstColumn + 2).Value = failed;

            double downloadRate = total > 0 ? (double)downloaded / total : 0;
            double failureRate = total > 0 ? (double)failed / total : 0;

            ws.Cell(row + 2, firstColumn + 3).Value = downloadRate;
            ws.Cell(row + 2, firstColumn + 4).Value = failureRate;

            valueRange.Style.Fill.BackgroundColor = Palette.White;
            valueRange.Style.Font.Bold = true;
            valueRange.Style.Font.FontSize = 12;
            valueRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            valueRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            valueRange.Style.Border.OutsideBorderColor = Palette.Border;
            valueRange.Style.NumberFormat.Format = "#,##0";

            var percentageRange = ws.Range(row + 2, firstColumn + 3, row + 2, firstColumn + 4);
            percentageRange.Style.Fill.BackgroundColor = Palette.White;
            percentageRange.Style.Font.FontColor = Palette.Dark;
            percentageRange.Style.Font.Bold = true;
            percentageRange.Style.NumberFormat.Format = "0.0%";
            percentageRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            percentageRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var summary = ws.Range(row + 3, firstColumn, row + 3, lastColumn);
            summary.Merge();
            summary.FirstCell().Value = $"{downloaded:#,##0} downloaded  •  {failed:#,##0} failed  •  Download {downloadRate:0.0%}  •  Failure {failureRate:0.0%}";
            summary.Style.Font.FontSize = 8.5;
            summary.Style.Font.FontColor = Palette.Grey;
            summary.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            summary.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            summary.Style.Fill.BackgroundColor = background;

            ws.Row(row).Height = 20;
            ws.Row(row + 1).Height = 26;
            ws.Row(row + 2).Height = 27;
            ws.Row(row + 3).Height = 18;
        }

        // COMPACT TABLE HEADER

        private static void SetCompactHeader(IXLWorksheet ws, int row, int firstColumn, string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, firstColumn + i).Value = headers[i];
            }

            var range = ws.Range(row, firstColumn, row, firstColumn + headers.Length - 1);
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EEF5");
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 8.5;
            range.Style.Font.FontColor = Palette.Dark;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Alignment.WrapText = true;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            range.Style.Border.OutsideBorderColor = Palette.Border;
            range.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");

            ws.Row(row).Height = 28;
        }

        // MODERN TABLE
        private static void StyleModernTable(IXLWorksheet ws, int headerRow, int lastRow, int firstColumn, int lastColumn)
        {
            if (lastRow < headerRow + 1)
            {
                return;
            }

            var range = ws.Range(headerRow + 1, firstColumn, lastRow, lastColumn);
            range.Style.Font.FontSize = 8.5;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            range.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.OutsideBorderColor = Palette.Border;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var firstColRange = ws.Range(headerRow + 1, firstColumn, lastRow, firstColumn);
            firstColRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            firstColRange.Style.Alignment.Indent = 1;

            for (int row = headerRow + 1; row <= lastRow; row++)
            {
                if ((row - headerRow) % 2 == 0)
                {
                    ws.Range(row, firstColumn, row, lastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }
            }
        }

        // BRPL DOWNLOADED

        private static void CreateBrplDownloadedSheet(XLWorkbook workbook, List<HesDownloadMeter> data)
        {
            var ws = workbook.Worksheets.Add("BRPL Downloaded");
            ws.TabColor = Palette.Green;

            string[] headers = { "S.No.", "Meter Number", "Cons Ref", "Phase", "Department", "Division", "Seq No", "Address", "Meter Type" };
            SetHeaderRow(ws, 1, headers);

            int row = 2;
            int serial = 1;

            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = serial++;
                ws.Cell(row, 2).Value = item.MeterNumber;
                ws.Cell(row, 3).Value = item.ConsRef;
                ws.Cell(row, 4).Value = item.Phase;
                ws.Cell(row, 5).Value = item.SapDepartment;
                ws.Cell(row, 6).Value = item.SapDivision;
                ws.Cell(row, 7).Value = item.SapSeqNo;
                ws.Cell(row, 8).Value = item.Address;
                ws.Cell(row, 9).Value = item.MeterType;

                row++;
            }

            FormatDataSheet(ws, headers.Length, "BRPL_Downloaded");
        }

        // BRPL FAILED
        private static void CreateBrplFailedSheet(XLWorkbook workbook, List<MeterDownloadDetailedSummary> data)
        {
            var ws = workbook.Worksheets.Add("BRPL Failed");
            ws.TabColor = Palette.Red;

            string[] headers =
            {
                "S.No.", "Meter Number", "Cons Ref", "Phase", "Department", "Division",
                "Seq No", "Address", "Meter Type", "Failure Reason", "Scheduler Message", "Entry Date"
            };

            SetHeaderRow(ws, 1, headers);

            int row = 2;
            int serial = 1;

            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = serial++;
                ws.Cell(row, 2).Value = item.MeterNumber;
                ws.Cell(row, 3).Value = item.ConsRef;
                ws.Cell(row, 4).Value = item.Phase;
                ws.Cell(row, 5).Value = item.SapDepartment;
                ws.Cell(row, 6).Value = item.SapDivision;
                ws.Cell(row, 7).Value = item.SapSeqNo;
                ws.Cell(row, 8).Value = item.Address;
                ws.Cell(row, 9).Value = item.MeterType;
                ws.Cell(row, 10).Value = GetBrplFailureReason(item.SchedulerMessage);
                ws.Cell(row, 11).Value = item.SchedulerMessage;

                if (item.EntryDate.HasValue)
                {
                    ws.Cell(row, 12).Value = item.EntryDate.Value;
                    ws.Cell(row, 12).Style.DateFormat.Format = "dd-MM-yyyy HH:mm:ss";
                }

                row++;
            }

            FormatDataSheet(ws, headers.Length, "BRPL_Failed");
        }

        // BYPL DOWNLOADED

        private static void CreateByplDownloadedSheet(XLWorkbook workbook, List<HesDownloadMeterBypl> data)
        {
            var ws = workbook.Worksheets.Add("BYPL Downloaded");
            ws.TabColor = Palette.Green;

            string[] headers = { "S.No.", "Meter Number", "Cons Ref", "Phase", "Department", "Division", "Seq No", "Address", "Meter Type" };
            SetHeaderRow(ws, 1, headers);

            int row = 2;
            int serial = 1;

            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = serial++;
                ws.Cell(row, 2).Value = item.MeterNumber;
                ws.Cell(row, 3).Value = item.ConsRef;
                ws.Cell(row, 4).Value = item.Phase;
                ws.Cell(row, 5).Value = item.SapDepartment;
                ws.Cell(row, 6).Value = item.SapDivision;
                ws.Cell(row, 7).Value = item.SapSeqNo;
                ws.Cell(row, 8).Value = item.Address;
                ws.Cell(row, 9).Value = item.MeterType;

                row++;
            }

            FormatDataSheet(ws, headers.Length, "BYPL_Downloaded");
        }

        // BYPL FAILED

        private static void CreateByplFailedSheet(XLWorkbook workbook, List<MeterDownloadDetailedSummaryBypl> data)
        {
            var ws = workbook.Worksheets.Add("BYPL Failed");
            ws.TabColor = Palette.Red;

            string[] headers =
            {
                "S.No.", "Meter Number", "Cons Ref", "Phase", "Department", "Division",
                "Seq No", "Address", "Meter Type", "Failure Reason", "Entry Date"
            };

            SetHeaderRow(ws, 1, headers);

            int row = 2;
            int serial = 1;

            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = serial++;
                ws.Cell(row, 2).Value = item.MeterNumber;
                ws.Cell(row, 3).Value = item.ConsRef;
                ws.Cell(row, 4).Value = item.Phase;
                ws.Cell(row, 5).Value = item.SapDepartment;
                ws.Cell(row, 6).Value = item.SapDivision;
                ws.Cell(row, 7).Value = item.SapSeqNo;
                ws.Cell(row, 8).Value = item.Address;
                ws.Cell(row, 9).Value = item.MeterType;
                ws.Cell(row, 10).Value = item.SchedulerMessage;

                if (item.EntryDate.HasValue)
                {
                    ws.Cell(row, 11).Value = item.EntryDate.Value;
                    ws.Cell(row, 11).Style.DateFormat.Format = "dd-MM-yyyy HH:mm:ss";
                }

                row++;
            }

            FormatDataSheet(ws, headers.Length, "BYPL_Failed");
        }

        // BRPL FAILURE REASON MAPPING

        private static string GetBrplFailureReason(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "Others Failure Reason";
            }

            var value = message.ToUpperInvariant();

            if (value.Contains("SYSTEM TITLE"))
            {
                return "System Title Mismatch";
            }

            if (value.Contains("TCP"))
            {
                return "TCP Connection Failed";
            }

            if (value.Contains("NO DATA"))
            {
                return "No Data Found";
            }

            return "Others Failure Reason";
        }

        // DETAIL SHEET HEADER

        private static void SetHeaderRow(IXLWorksheet ws, int row, string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
            }

            var range = ws.Range(row, 1, row, headers.Length);
            range.Style.Font.Bold = true;
            range.Style.Font.FontColor = Palette.White;
            range.Style.Font.FontSize = 10.5;
            range.Style.Fill.BackgroundColor = Palette.Navy;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.OutsideBorderColor = Palette.Navy;
            range.Style.Border.InsideBorderColor = XLColor.FromHtml("#335F91");

            ws.SheetView.FreezeRows(row);
            ws.Row(row).Height = 22;
        }


        // DEPARTMENT COMPANY HEADER

        private static void WriteDepartmentCompanyHeader(IXLWorksheet ws, int row, int firstColumn, int lastColumn, string company, XLColor accent)
        {
            var range = ws.Range(row, firstColumn, row, lastColumn);

            range.Merge();

            range.FirstCell().Value = company;

            range.Style.Fill.BackgroundColor = accent;
            range.Style.Font.FontColor = Palette.White;
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 10.5;

            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            range.Style.Alignment.Indent = 1;

            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            range.Style.Border.OutsideBorderColor = accent;

            ws.Row(row).Height = 21;
        }

        // DETAIL SHEET FORMATTING

        private static void FormatDataSheet(IXLWorksheet ws, int columnCount, string tableName)
        {
            ws.ShowGridLines = false;

            var usedRange = ws.RangeUsed();

            if (usedRange == null)
            {
                return;
            }

            usedRange.Style.Font.FontName = BodyFont;
            usedRange.Style.Font.FontSize = 10;
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.OutsideBorderColor = Palette.Border;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            usedRange.Style.Border.InsideBorderColor = Palette.Border;
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            if (usedRange.RowCount() >= 1)
            {
                var table = usedRange.CreateTable();
                table.Name = MakeSafeTableName(tableName);
                table.Theme = XLTableTheme.TableStyleLight9;
                table.ShowAutoFilter = true;
            }

            ws.Columns().AdjustToContents();

            for (int i = 1; i <= columnCount; i++)
            {
                if (ws.Column(i).Width > 45)
                {
                    ws.Column(i).Width = 45;
                }

                if (ws.Column(i).Width < 12)
                {
                    ws.Column(i).Width = 12;
                }
            }

            ws.Row(1).Height = 22;
        }

        private static string MakeSafeTableName(string value)
        {
            var chars = value.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray();
            var result = new string(chars);

            if (string.IsNullOrWhiteSpace(result))
            {
                result = "DashboardData";
            }

            if (char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            if (result.Length > 200)
            {
                result = result[..200];
            }

            return result;
        }
    }
}
