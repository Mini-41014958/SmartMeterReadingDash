using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using SmartMeterReadingDash.Models.Dashboard;

namespace SmartMeterReadingDash.Services
{
    public class ReportGenService
    {
        private readonly Dashboard _dashboard;
        private readonly ILogger<ReportGenService> _logger;

        public ReportGenService(Dashboard dashboard, ILogger<ReportGenService> logger)
        {
            _dashboard = dashboard;
            _logger = logger;
        }

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

        public async Task<DashboardReport> GenerateReportAsync(string readingMonth, UserAccessScope userScope)
        {
            if (string.IsNullOrWhiteSpace(readingMonth))
            {
                throw new ArgumentException("Reading month is required.", nameof(readingMonth));
            }

            if (userScope == null)
            {
                throw new ArgumentNullException(nameof(userScope));
            }

            try
            {
                bool isSuperAdmin = userScope.IsSuperAdmin;
                string company = userScope.Company?.Trim().ToUpperInvariant() ?? "";

                // ============================================================
                // BRPL ONLY
                // BYPL report generation is intentionally disabled.
                // ============================================================
                bool showBrpl = isSuperAdmin || company == "BRPL";
                bool showBypl = false;

                if (!showBrpl)
                {
                    throw new UnauthorizedAccessException(
                        $"Invalid company scope for BRPL report: {company}");
                }

                TotalMeterSummary? meterSummary = null;
                MeterReceivedSummary? brplReceived = null;
                List<MeterDownloadDetailedSummary>? brplFailed = null;
                List<HesDownloadMeter>? brplDownloaded = null;

                // BYPL report data intentionally disabled.
                // TotalMeterSummaryBypl? meterSummaryBypl = null;
                // List<MeterReceivedSummaryBypl>? byplReceived = null;
                // List<MeterDownloadDetailedSummaryBypl>? byplFailed = null;
                // List<HesDownloadMeterBypl>? byplDownloaded = null;

                if (showBrpl)
                {
                    meterSummary = _dashboard.GetMeterSummary(readingMonth, userScope);

                    brplReceived = _dashboard.GetMeterReceivedDownloadSummary(readingMonth, userScope);

                    brplFailed = _dashboard.MeterDetailedSummary(readingMonth, userScope);

                    brplDownloaded = _dashboard.HesDownloadMeterList(readingMonth, userScope);
                }

                /*
                // BYPL DATA DISABLED
                if (showBypl)
                {
                    meterSummaryBypl = _dashboard.GetByplTotalMeterSummary(readingMonth, userScope);
                    byplReceived = _dashboard.GetMeterReceivedSummaryBypl(readingMonth, userScope);
                    byplFailed = _dashboard.GetMeterDownloadDetailedSummaryBypl(readingMonth, userScope);
                    byplDownloaded = _dashboard.GetHesDownloadBypl(readingMonth, userScope);
                }
                */

                using var workbook = new XLWorkbook();

                CreateDashboardSheet(workbook, readingMonth, meterSummary, brplReceived, userScope);

                // BRPL

                if (showBrpl)
                {
                    CreateBrplDownloadedSheet(workbook, brplDownloaded ?? new List<HesDownloadMeter>());
                    CreateBrplFailedSheet(workbook, brplFailed ?? new List<MeterDownloadDetailedSummary>());
                }


                /*
                // BYPL SHEETS DISABLED
                if (showBypl)
                {
                    CreateByplDownloadedSheet(workbook, byplDownloaded ?? new List<HesDownloadMeterBypl>());
                    CreateByplFailedSheet(workbook, byplFailed ?? new List<MeterDownloadDetailedSummaryBypl>());
                }
                */

                var dashboardSheet = workbook.Worksheets.Worksheet("Dashboard");
                dashboardSheet.SetTabActive();
                dashboardSheet.Position = 1;

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                byte[] fileBytes = stream.ToArray();


                var safeMonth = readingMonth
                        .Replace(",", "_")
                        .Replace("/", "_")
                        .Replace("\\", "_")
                        .Replace(" ", "");

                var companySuffix = "BRPL";
                var fileName =
                    $"Smart_Meter_Reading_Dashboard_{companySuffix}_{safeMonth}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return new DashboardReport
                {
                    FileBytes = fileBytes,
                    FileName = fileName,
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard report generation failed for reading month {ReadingMonth}", readingMonth);
                throw;
            }
        }

        private void CreateDashboardSheet(
            XLWorkbook workbook,
            string readingMonth,
            TotalMeterSummary? brplMeter,
            MeterReceivedSummary? brplReceived,
            UserAccessScope scope)
        {
            var ws = workbook.Worksheets.Add("Dashboard");

            bool isSuperAdmin = scope.IsSuperAdmin;
            string company = scope.Company?.Trim().ToUpperInvariant() ?? "";

            // BRPL ONLY
            bool showBrpl = true;
            bool showBypl = false;

            var brplDepartment =
                _dashboard.GetDepartmentSummary(readingMonth, scope) ?? new();

            var brplFailureReasons =
                _dashboard.FailureReasonCounts(readingMonth, scope) ?? new();

            ws.TabColor = Palette.Blue;
            ws.ShowGridLines = false;
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.Margins.Top = 0.15;
            ws.PageSetup.Margins.Bottom = 0.15;
            ws.PageSetup.Margins.Left = 0.15;
            ws.PageSetup.Margins.Right = 0.15;
            ws.PageSetup.FitToPages(1, 0);

            // Keep B:F and G:K visually balanced.
            ws.Column("A").Width = 2;
            ws.Column("B").Width = 16;
            ws.Column("C").Width = 14;
            ws.Column("D").Width = 14;
            ws.Column("E").Width = 14;
            ws.Column("F").Width = 14;
            ws.Column("G").Width = 14;
            ws.Column("H").Width = 14;
            ws.Column("I").Width = 14;
            ws.Column("J").Width = 14;
            ws.Column("K").Width = 14;

            ws.Range("B1:K200")
                .Style
                .Font
                .FontColor = Palette.Dark;

            // TITLE

            ws.Range("B2:K4").Merge();

            ws.Cell("B2").Value =
                "RCM - SMART METER READING DASHBOARD";

            ws.Range("B2:K4").Style.Fill.BackgroundColor =
                Palette.Navy;

            ws.Range("B2:K4").Style.Font.FontColor =
                Palette.White;

            ws.Range("B2:K4").Style.Font.Bold = true;

            ws.Range("B2:K4").Style.Font.FontSize = 17;

            // CENTER TITLE
            ws.Range("B2:K4").Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            ws.Range("B2:K4").Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            // IMPORTANT: remove indentation
            ws.Range("B2:K4").Style.Alignment.Indent = 0;

            ws.Range("B2:K4").Style.Border.BottomBorder =
                XLBorderStyleValues.Medium;

            ws.Range("B2:K4").Style.Border.BottomBorderColor =
                Palette.Blue;

            // Make the merged title area taller
            ws.Row(2).Height = 20;
            ws.Row(3).Height = 20;
            ws.Row(4).Height = 20;


            string logoPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "images",
                "bses-logo.png");

            if (System.IO.File.Exists(logoPath))
            {
                var picture = ws.AddPicture(logoPath)
                    .WithPlacement(XLPicturePlacement.FreeFloating)
                    .WithSize(70, 60);

                picture.MoveTo(ws.Cell("B2"), 5, 4);
            }

            ws.Range("B5:K5").Merge();
            ws.Cell("B5").Value =
                $"READING MONTH: {readingMonth}   |   " +
                $"REPORT GENERATED: {DateTime.Now:dd MMM yyyy HH:mm}";

            ws.Range("B5:K5")
                .Style
                .Font
                .FontColor = Palette.Grey;

            ws.Range("B5:K5")
                .Style
                .Font
                .FontSize = 9;

            ws.Range("B5:K5")
                .Style
                .Font
                .Italic = true;

            ws.Range("B5:K5")
                .Style
                .Alignment
                .Horizontal = XLAlignmentHorizontalValues.Left;

            ws.Range("B5:K5")
                .Style
                .Alignment
                .Indent = 1;

            ws.Row(5).Height = 18;

            // ============================================================
            // READING OVERVIEW
            // ============================================================
            int brplTotal = brplReceived?.totalMetersCount ?? 0;
            int brplDownloaded = brplReceived?.hesDownloadCount ?? 0;
            int brplFailed = brplReceived?.manualForwardinCount ?? 0;

            int totalMeters = brplTotal;
            int totalDownloaded = brplDownloaded;
            int totalFailed = brplFailed;

            double downloadRate =
                totalMeters > 0
                    ? (double)totalDownloaded / totalMeters
                    : 0;

            double failureRate =
                totalMeters > 0
                    ? (double)totalFailed / totalMeters
                    : 0;

            WriteDashboardBanner(
                ws,
                7,
                "READING OVERVIEW – BRPL",
                Palette.Navy,
                Palette.White);

            CreateModernKpiCard(
                ws, 8, 2, 3,
                "TOTAL METERS",
                totalMeters.ToString("#,##0"),
                Palette.Blue);

            CreateModernKpiCard(
                ws, 8, 4, 5,
                "DOWNLOADED",
                totalDownloaded.ToString("#,##0"),
                Palette.Green);

            CreateModernKpiCard(
                ws, 8, 6, 7,
                "FAILED",
                totalFailed.ToString("#,##0"),
                Palette.Red);

            CreateModernKpiCard(
                ws, 8, 8, 9,
                "DOWNLOAD RATE",
                downloadRate.ToString("0.0%"),
                Palette.Blue);

            CreateModernKpiCard(
                ws, 8, 10, 11,
                "FAILURE RATE",
                failureRate.ToString("0.0%"),
                Palette.Orange);

            const int analysisHeaderRow = 12;
            const int analysisSubHeaderRow = 13;
            const int tableHeaderRow = 14;

            WriteDashboardPanelHeader(
                ws,
                analysisHeaderRow,
                2,
                6,
                "DEPARTMENT PERFORMANCE",
                Palette.Navy,
                Palette.White);

            WriteDashboardPanelHeader(
                ws,
                analysisHeaderRow,
                7,
                11,
                "FAILURE ANALYSIS",
                Palette.Red,
                Palette.White);

            WriteDashboardPanelHeader(
                ws,
                analysisSubHeaderRow,
                2,
                6,
                "BRPL",
                Palette.Blue,
                Palette.White);

            WriteDashboardPanelHeader(
                ws,
                analysisSubHeaderRow,
                7,
                11,
                "FAILURE REASON BREAKDOWN",
                Palette.Red,
                Palette.White);

            // Department table: B:F
            SetCompactHeader(
                ws,
                tableHeaderRow,
                2,
                new[]
                {
                    "DEPARTMENT",
                    "DOWNLOADED",
                    "FAILED",
                    "DOWNLOAD RATE",
                    "FAILURE RATE"
                });

            // Failure table: G:K
            SetCompactHeader(
                ws,
                tableHeaderRow,
                7,
                new[]
                {
                    "FAILURE REASON",
                    "",
                    "",
                    "COUNT",
                    "FAILURE %"
                });

            // Merge the 3-column failure-reason header.
            ws.Range(
                tableHeaderRow,
                7,
                tableHeaderRow,
                9).Merge();

            ws.Cell(
                tableHeaderRow,
                7).Value = "FAILURE REASON";

            // ============================================================
            // DEPARTMENT DATA
            // ============================================================
            int brplDeptRow = tableHeaderRow + 1;

            foreach (var item in brplDepartment)
            {
                int total = item.HesDownload + item.Failed;

                ws.Cell(brplDeptRow, 2).Value =
                    string.IsNullOrWhiteSpace(item.Department)
                        ? "Unassigned"
                        : item.Department;

                ws.Cell(brplDeptRow, 3).Value = item.HesDownload;
                ws.Cell(brplDeptRow, 4).Value = item.Failed;

                ws.Cell(brplDeptRow, 5).Value =
                    total > 0
                        ? (double)item.HesDownload / total
                        : 0;

                ws.Cell(brplDeptRow, 6).Value =
                    total > 0
                        ? (double)item.Failed / total
                        : 0;

                brplDeptRow++;
            }

            int departmentEndRow = brplDeptRow - 1;

            // ============================================================
            // FAILURE DATA
            // ============================================================
            int brplFailureRow = tableHeaderRow + 1;

            int brplFailureTotal =
                brplFailureReasons.Sum(x => x.count ?? 0);

            foreach (var item in
                brplFailureReasons.OrderByDescending(x => x.count ?? 0))
            {
                int count = item.count ?? 0;

                // Failure reason spans G:I so the right-side panel is
                // wide enough for real-world reason text.
                ws.Range(
                    brplFailureRow,
                    7,
                    brplFailureRow,
                    9).Merge();

                ws.Cell(
                    brplFailureRow,
                    7).Value =
                    string.IsNullOrWhiteSpace(item.FeilureReason)
                        ? "Unknown"
                        : item.FeilureReason;

                ws.Cell(brplFailureRow, 10).Value = count;

                ws.Cell(brplFailureRow, 11).Value =
                    brplFailureTotal > 0
                        ? (double)count / brplFailureTotal
                        : 0;

                brplFailureRow++;
            }

            int failureEndRow = brplFailureRow - 1;

            int analysisEndRow =
                Math.Max(departmentEndRow, failureEndRow);

            // ============================================================
            // TABLE STYLING
            // ============================================================
            if (departmentEndRow >= tableHeaderRow + 1)
            {
                StyleModernTable(
                    ws,
                    tableHeaderRow,
                    departmentEndRow,
                    2,
                    6);

                ws.Range(
                    tableHeaderRow + 1,
                    5,
                    departmentEndRow,
                    6)
                    .Style
                    .NumberFormat
                    .Format = "0.0%";
            }

            if (failureEndRow >= tableHeaderRow + 1)
            {
                StyleModernTable(
                    ws,
                    tableHeaderRow,
                    failureEndRow,
                    7,
                    11);

                // Restore the merged reason cells after table styling.
                for (int row = tableHeaderRow + 1;
                     row <= failureEndRow;
                     row++)
                {
                    ws.Range(row, 7, row, 9).Merge();

                    ws.Cell(row, 7)
                        .Style
                        .Alignment
                        .Horizontal =
                        XLAlignmentHorizontalValues.Left;

                    ws.Cell(row, 7)
                        .Style
                        .Alignment
                        .Indent = 1;
                }

                ws.Range(
                    tableHeaderRow + 1,
                    10,
                    failureEndRow,
                    10)
                    .Style
                    .NumberFormat
                    .Format = "#,##0";

                ws.Range(
                    tableHeaderRow + 1,
                    11,
                    failureEndRow,
                    11)
                    .Style
                    .NumberFormat
                    .Format = "0.0%";
            }

            // ============================================================
            // VISUAL BALANCING
            // ============================================================
            // Give both panels the same visual bottom edge even when one
            // has fewer rows than the other.
            if (analysisEndRow >= tableHeaderRow + 1)
            {
                for (int row = tableHeaderRow + 1;
                     row <= analysisEndRow;
                     row++)
                {
                    if (row % 2 == 0)
                    {
                        ws.Range(row, 2, row, 6)
                            .Style
                            .Fill
                            .BackgroundColor =
                            XLColor.FromHtml("#F8FAFC");

                        ws.Range(row, 7, row, 11)
                            .Style
                            .Fill
                            .BackgroundColor =
                            XLColor.FromHtml("#F8FAFC");
                    }
                }
            }

            // Keep a compact but readable dashboard.
            for (int row = analysisHeaderRow;
                 row <= analysisEndRow;
                 row++)
            {
                if (ws.Row(row).Height < 20)
                {
                    ws.Row(row).Height = 20;
                }
            }

            // ============================================================
            // MANAGEMENT SUMMARY
            // ============================================================
            int footerRow = analysisEndRow + 2;

            ws.Range(
                footerRow,
                2,
                footerRow,
                11).Merge();

            ws.Cell(
                footerRow,
                2).Value =
                "MANAGEMENT SUMMARY – BRPL  |  " +
                $"{totalDownloaded:#,##0} of " +
                $"{totalMeters:#,##0} meters successfully downloaded " +
                $"({downloadRate:0.0%})  |  " +
                $"{totalFailed:#,##0} failed " +
                $"({failureRate:0.0%}).";

            var footer =
                ws.Range(
                    footerRow,
                    2,
                    footerRow,
                    11);

            footer.Style
                .Fill
                .BackgroundColor = Palette.LightGrey;

            footer.Style
                .Font
                .FontColor = Palette.Dark;

            footer.Style
                .Font
                .Bold = true;

            footer.Style
                .Font
                .FontSize = 9.5;

            footer.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Center;

            footer.Style
                .Alignment
                .Vertical =
                XLAlignmentVerticalValues.Center;

            footer.Style
                .Border
                .OutsideBorder =
                XLBorderStyleValues.Thin;

            footer.Style
                .Border
                .OutsideBorderColor =
                Palette.Border;

            ws.Row(footerRow).Height = 28;

            // ============================================================
            // FINAL FORMATTING
            // ============================================================
            var usedRange = ws.RangeUsed();

            if (usedRange != null)
            {
                usedRange.Style
                    .Font
                    .FontName = BodyFont;

                usedRange.Style
                    .Alignment
                    .Vertical =
                    XLAlignmentVerticalValues.Center;
            }

            ws.SheetView.FreezeRows(5);
        }

        // ============================================================
        // PANEL HEADER
        // ============================================================
        private static void WriteDashboardPanelHeader(
            IXLWorksheet ws,
            int row,
            int firstColumn,
            int lastColumn,
            string title,
            XLColor background,
            XLColor fontColor)
        {
            var range =
                ws.Range(
                    row,
                    firstColumn,
                    row,
                    lastColumn);

            range.Merge();

            range.FirstCell().Value = title;

            range.Style
                .Fill
                .BackgroundColor = background;

            range.Style
                .Font
                .FontColor = fontColor;

            range.Style
                .Font
                .Bold = true;

            range.Style
                .Font
                .FontSize = 10;

            range.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Left;

            range.Style
                .Alignment
                .Vertical =
                XLAlignmentVerticalValues.Center;

            range.Style
                .Alignment
                .Indent = 1;

            range.Style
                .Border
                .OutsideBorder =
                XLBorderStyleValues.Thin;

            range.Style
                .Border
                .OutsideBorderColor =
                background;

            ws.Row(row).Height = 20;
        }

        // ============================================================
        // COMPANY SUMMARY LABEL
        // ============================================================

        private static string CompanySummaryLabel(
            bool isSuperAdmin,
            string company)
        {
            return "MANAGEMENT SUMMARY – BRPL";
        }


        // ============================================================
        // DASHBOARD BANNER
        // ============================================================

        private static void WriteDashboardBanner(
            IXLWorksheet ws,
            int row,
            string title,
            XLColor background,
            XLColor fontColor)
        {
            var range =
                ws.Range(
                    row,
                    2,
                    row,
                    11);

            range.Merge();

            range.FirstCell().Value =
                title;

            range.Style
                .Fill
                .BackgroundColor =
                background;

            range.Style
                .Font
                .FontColor =
                fontColor;

            range.Style
                .Font
                .Bold = true;

            range.Style
                .Font
                .FontSize = 10.5;

            range.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Left;

            range.Style
                .Alignment
                .Vertical =
                XLAlignmentVerticalValues.Center;

            range.Style
                .Alignment
                .Indent = 1;

            ws.Row(row).Height =
                20;
        }


        // ============================================================
        // KPI CARD
        // ============================================================

        private static void CreateModernKpiCard(
            IXLWorksheet ws,
            int row,
            int firstColumn,
            int lastColumn,
            string label,
            string value,
            XLColor accent)
        {
            var card =
                ws.Range(
                    row,
                    firstColumn,
                    row + 2,
                    lastColumn);

            card.Style
                .Fill
                .BackgroundColor =
                Palette.White;

            card.Style
                .Border
                .OutsideBorder =
                XLBorderStyleValues.Thin;

            card.Style
                .Border
                .OutsideBorderColor =
                Palette.Border;


            var accentRange =
                ws.Range(
                    row,
                    firstColumn,
                    row,
                    lastColumn);

            accentRange.Style
                .Fill
                .BackgroundColor =
                accent;


            var labelRange =
                ws.Range(
                    row + 1,
                    firstColumn,
                    row + 1,
                    lastColumn);

            labelRange.Merge();

            labelRange.FirstCell().Value =
                label;

            labelRange.Style
                .Font
                .FontColor =
                Palette.Grey;

            labelRange.Style
                .Font
                .Bold = true;

            labelRange.Style
                .Font
                .FontSize = 8.5;

            labelRange.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Center;

            labelRange.Style
                .Alignment
                .Vertical =
                XLAlignmentVerticalValues.Center;


            var valueRange =
                ws.Range(
                    row + 2,
                    firstColumn,
                    row + 2,
                    lastColumn);

            valueRange.Merge();

            valueRange.FirstCell().Value =
                value;

            valueRange.Style
                .Font
                .FontColor =
                accent;

            valueRange.Style
                .Font
                .Bold = true;

            valueRange.Style
                .Font
                .FontSize = 19;

            valueRange.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Center;

            valueRange.Style
                .Alignment
                .Vertical =
                XLAlignmentVerticalValues.Center;


            ws.Row(row).Height = 5;
            ws.Row(row + 1).Height = 17;
            ws.Row(row + 2).Height = 29;
        }


        // ============================================================
        // COMPANY PANEL
        // ============================================================

        private static void WriteCompanyPanel(
            IXLWorksheet ws,
            int row,
            int firstColumn,
            int lastColumn,
            string company,
            int total,
            int downloaded,
            int failed,
            XLColor accent,
            XLColor background)
        {
            var title =
                ws.Range(
                    row,
                    firstColumn,
                    row,
                    lastColumn);

            title.Merge();

            title.FirstCell().Value =
                company;

            title.Style
                .Fill
                .BackgroundColor =
                accent;

            title.Style
                .Font
                .FontColor =
                Palette.White;

            title.Style
                .Font
                .Bold = true;

            title.Style
                .Font
                .FontSize = 12;

            title.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Left;

            title.Style
                .Alignment
                .Indent = 1;


            string[] headers =
            {
                "TOTAL",
                "DOWNLOADED",
                "FAILED",
                "DOWNLOAD RATE",
                "FAILURE RATE"
            };


            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(
                    row + 1,
                    firstColumn + i)
                    .Value =
                    headers[i];
            }


            var headerRange =
                ws.Range(
                    row + 1,
                    firstColumn,
                    row + 1,
                    lastColumn);

            headerRange.Style
                .Fill
                .BackgroundColor =
                background;

            headerRange.Style
                .Font
                .Bold = true;

            headerRange.Style
                .Font
                .FontSize = 8;

            headerRange.Style
                .Font
                .FontColor =
                Palette.Dark;

            headerRange.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Center;

            headerRange.Style
                .Alignment
                .Vertical =
                XLAlignmentVerticalValues.Center;

            headerRange.Style
                .Alignment
                .WrapText = true;


            var valueRange =
                ws.Range(
                    row + 2,
                    firstColumn,
                    row + 2,
                    lastColumn);


            ws.Cell(
                row + 2,
                firstColumn)
                .Value =
                total;

            ws.Cell(
                row + 2,
                firstColumn + 1)
                .Value =
                downloaded;

            ws.Cell(
                row + 2,
                firstColumn + 2)
                .Value =
                failed;


            double downloadRate =
                total > 0
                    ? (double)downloaded / total
                    : 0;

            double failureRate =
                total > 0
                    ? (double)failed / total
                    : 0;


            ws.Cell(
                row + 2,
                firstColumn + 3)
                .Value =
                downloadRate;

            ws.Cell(
                row + 2,
                firstColumn + 4)
                .Value =
                failureRate;


            valueRange.Style
                .Fill
                .BackgroundColor =
                Palette.White;

            valueRange.Style
                .Font
                .Bold = true;

            valueRange.Style
                .Font
                .FontSize = 12;

            valueRange.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Center;

            valueRange.Style
                .Border
                .OutsideBorder =
                XLBorderStyleValues.Thin;

            valueRange.Style
                .Border
                .OutsideBorderColor =
                Palette.Border;

            valueRange.Style
                .NumberFormat
                .Format =
                "#,##0";


            var percentageRange =
                ws.Range(
                    row + 2,
                    firstColumn + 3,
                    row + 2,
                    firstColumn + 4);


            percentageRange.Style
                .Fill
                .BackgroundColor =
                Palette.White;

            percentageRange.Style
                .Font
                .FontColor =
                Palette.Dark;

            percentageRange.Style
                .Font
                .Bold = true;

            percentageRange.Style
                .NumberFormat
                .Format =
                "0.0%";

            percentageRange.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Center;

            percentageRange.Style
                .Alignment
                .Vertical =
                XLAlignmentVerticalValues.Center;


            var summary =
                ws.Range(
                    row + 3,
                    firstColumn,
                    row + 3,
                    lastColumn);

            summary.Merge();

            summary.FirstCell().Value =
                $"{downloaded:#,##0} downloaded  •  " +
                $"{failed:#,##0} failed  •  " +
                $"Download {downloadRate:0.0%}  •  " +
                $"Failure {failureRate:0.0%}";


            summary.Style
                .Font
                .FontSize = 8.5;

            summary.Style
                .Font
                .FontColor =
                Palette.Grey;

            summary.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Center;

            summary.Style
                .Alignment
                .Vertical =
                XLAlignmentVerticalValues.Center;

            summary.Style
                .Fill
                .BackgroundColor =
                background;


            ws.Row(row).Height = 20;
            ws.Row(row + 1).Height = 26;
            ws.Row(row + 2).Height = 27;
            ws.Row(row + 3).Height = 18;
        }


        // ============================================================
        // COMPACT HEADER
        // ============================================================

        private static void SetCompactHeader(
            IXLWorksheet ws,
            int row,
            int firstColumn,
            string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(
                    row,
                    firstColumn + i)
                    .Value =
                    headers[i];
            }


            var range =
                ws.Range(
                    row,
                    firstColumn,
                    row,
                    firstColumn + headers.Length - 1);


            range.Style
                .Fill
                .BackgroundColor =
                XLColor.FromHtml("#E8EEF5");

            range.Style
                .Font
                .Bold = true;

            range.Style
                .Font
                .FontSize = 8.5;

            range.Style
                .Font
                .FontColor =
                Palette.Dark;

            range.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Center;

            range.Style
                .Alignment
                .Vertical =
                XLAlignmentVerticalValues.Center;

            range.Style
                .Alignment
                .WrapText = true;

            range.Style
                .Border
                .OutsideBorder =
                XLBorderStyleValues.Thin;

            range.Style
                .Border
                .InsideBorder =
                XLBorderStyleValues.Hair;

            range.Style
                .Border
                .OutsideBorderColor =
                Palette.Border;

            range.Style
                .Border
                .InsideBorderColor =
                XLColor.FromHtml("#E2E8F0");


            ws.Row(row).Height =
                28;
        }


        // ============================================================
        // MODERN TABLE
        // ============================================================

        private static void StyleModernTable(
            IXLWorksheet ws,
            int headerRow,
            int lastRow,
            int firstColumn,
            int lastColumn)
        {
            if (lastRow <
                headerRow + 1)
            {
                return;
            }


            var range =
                ws.Range(
                    headerRow + 1,
                    firstColumn,
                    lastRow,
                    lastColumn);


            range.Style
                .Font
                .FontSize = 8.5;

            range.Style
                .Border
                .InsideBorder =
                XLBorderStyleValues.Hair;

            range.Style
                .Border
                .InsideBorderColor =
                XLColor.FromHtml("#E2E8F0");

            range.Style
                .Border
                .OutsideBorder =
                XLBorderStyleValues.Thin;

            range.Style
                .Border
                .OutsideBorderColor =
                Palette.Border;

            range.Style
                .Alignment
                .Vertical =
                XLAlignmentVerticalValues.Center;

            range.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Center;


            var firstColRange =
                ws.Range(
                    headerRow + 1,
                    firstColumn,
                    lastRow,
                    firstColumn);


            firstColRange.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Left;

            firstColRange.Style
                .Alignment
                .Indent = 1;


            for (int row = headerRow + 1;
                 row <= lastRow;
                 row++)
            {
                if ((row - headerRow) % 2 == 0)
                {
                    ws.Range(
                        row,
                        firstColumn,
                        row,
                        lastColumn)
                        .Style
                        .Fill
                        .BackgroundColor =
                        XLColor.FromHtml("#F8FAFC");
                }
            }
        }


        // ============================================================
        // BRPL DOWNLOADED
        // ============================================================

        // ============================================================
        // BRPL DOWNLOADED
        // ============================================================

        private static void CreateBrplDownloadedSheet(
            XLWorkbook workbook,
            List<HesDownloadMeter> data)
        {
            var ws =
                workbook.Worksheets.Add(
                    "BRPL Downloaded");

            ws.TabColor =
                Palette.Green;


            string[] headers =
            {
        "S.No.",
        "Meter Number",
        "Cons Ref",
        "Phase",
        "Department",
        "Division",
        "Seq No",
        "Address",
        "Meter Type"
    };


            SetHeaderRow(
                ws,
                1,
                headers);


            int row = 2;
            int serial = 1;


            foreach (var item in data)
            {
                ws.Cell(row, 1).Value =
                    serial++;

                ws.Cell(row, 2).Value =
                    item.MeterNumber;

                ws.Cell(row, 3).Value =
                    item.ConsRef;

                ws.Cell(row, 4).Value =
                    item.Phase;

                ws.Cell(row, 5).Value =
                    item.SapDepartment;

                ws.Cell(row, 6).Value =
                    item.SapDivision;

                ws.Cell(row, 7).Value =
                    item.SapSeqNo;

                ws.Cell(row, 8).Value =
                    item.Address;

                ws.Cell(row, 9).Value =
                    item.MeterType;

                row++;
            }


            FormatDataSheet(
                ws,
                headers.Length,
                "BRPL_Downloaded");
        }

        // BRPL FAILED

        private static void CreateBrplFailedSheet(  XLWorkbook workbook, List<MeterDownloadDetailedSummary> data)
        {
            var ws = workbook.Worksheets.Add( "BRPL Failed");

            ws.TabColor =  Palette.Red;

            // COLUMN ORDER
            string[] headers =
            {
                "S.No.",
                "Meter Number",
                "Cons Ref",
                "Phase",
                "Department",
                "Division",
                "Seq No",
                "Meter Type",
                "Download Failed Since",
                "Download Failed Days",
                "Failure Reason",
                "Scheduler Message",
                "Entry Date",
                "Address"
            };

            SetHeaderRow( ws, 1,headers);
            int row = 2;
            int serial = 1;

            foreach (var item in data)
            {
                // ========================================================
                // S.NO.
                // ========================================================

                ws.Cell(row, 1).Value =
                    serial++;


                // ========================================================
                // METER NUMBER
                // ========================================================

                ws.Cell(row, 2).Value =
                    item.MeterNumber;


                // ========================================================
                // CONS REF
                // ========================================================

                ws.Cell(row, 3).Value =
                    item.ConsRef;


                // ========================================================
                // PHASE
                // ========================================================

                ws.Cell(row, 4).Value =
                    item.Phase;


                // ========================================================
                // DEPARTMENT
                // ========================================================

                ws.Cell(row, 5).Value =
                    item.SapDepartment;


                // ========================================================
                // DIVISION
                // ========================================================

                ws.Cell(row, 6).Value =
                    item.SapDivision;


                // ========================================================
                // SEQ NO
                // ========================================================

                ws.Cell(row, 7).Value =
                    item.SapSeqNo;


                // ========================================================
                // METER TYPE
                // ========================================================

                ws.Cell(row, 8).Value =
                    item.MeterType;


                // ========================================================
                // DOWNLOAD FAILED SINCE
                // ========================================================

                if (item.DownloadFailedSince.HasValue)
                {
                    ws.Cell(row, 9).Value =
                        item.DownloadFailedSince.Value;

                    ws.Cell(row, 9)
                        .Style
                        .DateFormat
                        .Format =
                        "dd-MM-yyyy";
                }


                // ========================================================
                // DOWNLOAD FAILED DAYS
                // ========================================================

                ws.Cell(row, 10).Value =
                    item.DownloadFailedDays;


                // ========================================================
                // RED IF FAILED DAYS >= 5
                // ========================================================

                if (item.DownloadFailedDays >= 5)
                {
                    ws.Cell(row, 10)
                        .Style
                        .Fill
                        .SetBackgroundColor(
                            XLColor.Red);

                    ws.Cell(row, 10)
                        .Style
                        .Font
                        .SetBold();

                    ws.Cell(row, 10)
                        .Style
                        .Font
                        .SetFontColor(
                            XLColor.White);

                    ws.Cell(row, 10)
                        .Style
                        .Alignment
                        .Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    ws.Cell(row, 10)
                        .Style
                        .Alignment
                        .Vertical =
                        XLAlignmentVerticalValues.Center;
                }


                // ========================================================
                // FAILURE REASON
                // ========================================================

                ws.Cell(row, 11).Value =
                    GetBrplFailureReason(
                        item.SchedulerMessage);


                // ========================================================
                // SCHEDULER MESSAGE
                // ========================================================

                ws.Cell(row, 12).Value =
                    item.SchedulerMessage;


                // ========================================================
                // ENTRY DATE
                // ========================================================

                if (item.EntryDate.HasValue)
                {
                    ws.Cell(row, 13).Value =
                        item.EntryDate.Value;

                    ws.Cell(row, 13)
                        .Style
                        .DateFormat
                        .Format =
                        "dd-MM-yyyy HH:mm:ss";
                }


                // ========================================================
                // ADDRESS
                // ========================================================

                ws.Cell(row, 14).Value =
                    item.Address;


                row++;
            }


            // ============================================================
            // FORMAT SHEET
            // ============================================================

            FormatDataSheet(
                ws,
                headers.Length,
                "BRPL_Failed");
        }


        // ============================================================
        // BYPL DOWNLOADED
        // ============================================================

        /*
        private static void CreateByplDownloadedSheet(
            XLWorkbook workbook,
            List<HesDownloadMeterBypl> data)
        {
            var ws =
                workbook.Worksheets.Add(
                    "BYPL Downloaded");

            ws.TabColor =
                Palette.Green;


            string[] headers =
            {
        "S.No.",
        "Meter Number",
        "Cons Ref",
        "Phase",
        "Department",
        "Division",
        "Seq No",
        "Address",
        "Meter Type"
    };


            SetHeaderRow(
                ws,
                1,
                headers);


            int row = 2;
            int serial = 1;


            foreach (var item in data)
            {
                ws.Cell(row, 1).Value =
                    serial++;

                ws.Cell(row, 2).Value =
                    item.MeterNumber;

                ws.Cell(row, 3).Value =
                    item.ConsRef;

                ws.Cell(row, 4).Value =
                    item.Phase;

                ws.Cell(row, 5).Value =
                    item.SapDepartment;

                ws.Cell(row, 6).Value =
                    item.SapDivision;

                ws.Cell(row, 7).Value =
                    item.SapSeqNo;

                ws.Cell(row, 8).Value =
                    item.Address;

                ws.Cell(row, 9).Value =
                    item.MeterType;

                row++;
            }


            FormatDataSheet(
                ws,
                headers.Length,
                "BYPL_Downloaded");
        }


        */

        // ============================================================
        // BYPL FAILED
        // ============================================================

        /*
        private static void CreateByplFailedSheet(
            XLWorkbook workbook,
            List<MeterDownloadDetailedSummaryBypl> data)
        {
            var ws =
                workbook.Worksheets.Add(
                    "BYPL Failed");

            ws.TabColor =
                Palette.Red;


            string[] headers =
            {
        "S.No.",
        "Meter Number",
        "Cons Ref",
        "Phase",
        "Department",
        "Division",
        "Seq No",
        "Address",
        "Meter Type",
        "Failure Reason",
        "Entry Date",
        "Download Failed Since",
        "Download Failed Days"
    };


            SetHeaderRow(
                ws,
                1,
                headers);


            int row = 2;
            int serial = 1;


            foreach (var item in data)
            {
                ws.Cell(row, 1).Value =
                    serial++;

                ws.Cell(row, 2).Value =
                    item.MeterNumber;

                ws.Cell(row, 3).Value =
                    item.ConsRef;

                ws.Cell(row, 4).Value =
                    item.Phase;

                ws.Cell(row, 5).Value =
                    item.SapDepartment;

                ws.Cell(row, 6).Value =
                    item.SapDivision;

                ws.Cell(row, 7).Value =
                    item.SapSeqNo;

                ws.Cell(row, 8).Value =
                    item.Address;

                ws.Cell(row, 9).Value =
                    item.MeterType;

                ws.Cell(row, 10).Value =
                    item.SchedulerMessage;


                // ========================================================
                // ENTRY DATE
                // ========================================================

                if (item.EntryDate.HasValue)
                {
                    ws.Cell(row, 11).Value =
                        item.EntryDate.Value;

                    ws.Cell(row, 11)
                        .Style
                        .DateFormat
                        .Format =
                        "dd-MM-yyyy HH:mm:ss";
                }


                // ========================================================
                // DOWNLOAD FAILED SINCE
                // ========================================================

                if (item.DownloadFailedSince.HasValue)
                {
                    ws.Cell(row, 12).Value =
                        item.DownloadFailedSince.Value;

                    ws.Cell(row, 12)
                        .Style
                        .DateFormat
                        .Format =
                        "dd-MM-yyyy";
                }


                // ========================================================
                // DOWNLOAD FAILED DAYS
                // ========================================================

                ws.Cell(row, 13).Value =
                    item.DownloadFailedDays;


                // ========================================================
                // RED IF FAILED DAYS >= 5
                // ========================================================

                if (item.DownloadFailedDays >= 5)
                {
                    ws.Cell(row, 13)
                        .Style
                        .Fill
                        .SetBackgroundColor(
                            XLColor.Red);

                    ws.Cell(row, 13)
                        .Style
                        .Font
                        .SetBold();

                    ws.Cell(row, 13)
                        .Style
                        .Font
                        .SetFontColor(
                            XLColor.White);

                    ws.Cell(row, 13)
                        .Style
                        .Alignment
                        .Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    ws.Cell(row, 13)
                        .Style
                        .Alignment
                        .Vertical =
                        XLAlignmentVerticalValues.Center;
                }


                row++;
            }


            FormatDataSheet(
                ws,
                headers.Length,
                "BYPL_Failed");
        }


        */

        // ============================================================
        // BRPL FAILURE REASON
        // ============================================================

        private static string GetBrplFailureReason(
            string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "Others Failure Reason";
            }


            var value =
                message.ToUpperInvariant();


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


        // ============================================================
        // DETAIL HEADER
        // ============================================================

        private static void SetHeaderRow(
            IXLWorksheet ws,
            int row,
            string[] headers)
        {
            for (int i = 0;
                 i < headers.Length;
                 i++)
            {
                ws.Cell(
                    row,
                    i + 1)
                    .Value =
                    headers[i];
            }


            var range =
                ws.Range(
                    row,
                    1,
                    row,
                    headers.Length);


            range.Style
                .Font
                .Bold = true;

            range.Style
                .Font
                .FontColor =
                Palette.White;

            range.Style
                .Font
                .FontSize = 10.5;

            range.Style
                .Fill
                .BackgroundColor =
                Palette.Navy;

            range.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Center;

            range.Style
                .Alignment
                .Vertical =
                XLAlignmentVerticalValues.Center;

            range.Style
                .Border
                .OutsideBorder =
                XLBorderStyleValues.Thin;

            range.Style
                .Border
                .InsideBorder =
                XLBorderStyleValues.Thin;

            range.Style
                .Border
                .OutsideBorderColor =
                Palette.Navy;

            range.Style
                .Border
                .InsideBorderColor =
                XLColor.FromHtml("#335F91");


            ws.SheetView.FreezeRows(row);

            ws.Row(row).Height =
                22;
        }


        // ============================================================
        // DEPARTMENT COMPANY HEADER
        // ============================================================

        private static void WriteDepartmentCompanyHeader(
            IXLWorksheet ws,
            int row,
            int firstColumn,
            int lastColumn,
            string company,
            XLColor accent)
        {
            var range =
                ws.Range(
                    row,
                    firstColumn,
                    row,
                    lastColumn);


            range.Merge();


            range.FirstCell().Value =
                company;


            range.Style
                .Fill
                .BackgroundColor =
                accent;

            range.Style
                .Font
                .FontColor =
                Palette.White;

            range.Style
                .Font
                .Bold = true;

            range.Style
                .Font
                .FontSize = 10.5;

            range.Style
                .Alignment
                .Horizontal =
                XLAlignmentHorizontalValues.Left;

            range.Style
                .Alignment
                .Vertical =
                XLAlignmentVerticalValues.Center;

            range.Style
                .Alignment
                .Indent = 1;

            range.Style
                .Border
                .OutsideBorder =
                XLBorderStyleValues.Thin;

            range.Style
                .Border
                .OutsideBorderColor =
                accent;


            ws.Row(row).Height =
                21;
        }


        // ============================================================
        // DETAIL SHEET FORMATTING
        // ============================================================

        private static void FormatDataSheet(
            IXLWorksheet ws,
            int columnCount,
            string tableName)
        {
            ws.ShowGridLines =
                false;


            var usedRange =
                ws.RangeUsed();


            if (usedRange == null)
            {
                return;
            }


            usedRange.Style
                .Font
                .FontName =
                BodyFont;

            usedRange.Style
                .Font
                .FontSize = 10;

            usedRange.Style
                .Border
                .OutsideBorder =
                XLBorderStyleValues.Thin;

            usedRange.Style
                .Border
                .OutsideBorderColor =
                Palette.Border;

            usedRange.Style
                .Border
                .InsideBorder =
                XLBorderStyleValues.Hair;

            usedRange.Style
                .Border
                .InsideBorderColor =
                Palette.Border;

            usedRange.Style
                .Alignment
                .Vertical =
                XLAlignmentVerticalValues.Center;


            if (usedRange.RowCount() >= 1)
            {
                var table =
                    usedRange.CreateTable();

                table.Name =
                    MakeSafeTableName(tableName);

                table.Theme =
                    XLTableTheme.TableStyleLight9;

                table.ShowAutoFilter =
                    true;
            }


            ws.Columns()
                .AdjustToContents();


            for (int i = 1;
                 i <= columnCount;
                 i++)
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


            ws.Row(1).Height =
                22;
        }


        // ============================================================
        // SAFE TABLE NAME
        // ============================================================

        private static string MakeSafeTableName(
            string value)
        {
            var chars =
                value
                    .Where(c =>
                        char.IsLetterOrDigit(c) ||
                        c == '_')
                    .ToArray();


            var result =
                new string(chars);


            if (string.IsNullOrWhiteSpace(result))
            {
                result =
                    "DashboardData";
            }


            if (char.IsDigit(result[0]))
            {
                result =
                    "_" + result;
            }


            if (result.Length > 200)
            {
                result =
                    result[..200];
            }


            return result;
        }
    }
}