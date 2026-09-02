
// Global reading month shared by all API calls
let currentReadingMonth = "";


// ============================================================
// Loads all dashboard components
// ============================================================
async function loadDashboard() {

    $("#dashboardSkeleton").show();
    $("#dashboardContent").hide();

    try {

        await Promise.all([
            loadMeterSummary(),
            loadMeterDownloadSummary(),
            loadDepartmentDistribution(),
            loadFailureReasonChart()
        ]);

    }
    catch (err) {

        console.error(err);

    }
    finally {

        $("#dashboardSkeleton").fadeOut(300, function () {

            $("#dashboardContent").fadeIn(300);

        });

    }

}


// ============================================================
// Returns the reading month for all APIs
// ============================================================
function getReadingMonth() {

    return currentReadingMonth;

}


// ============================================================
// Initial Page Load
// ============================================================
document.addEventListener("DOMContentLoaded", function () {

    const today = new Date();


    // --------------------------------------------------------
    // Calculate N-1 month
    //
    // Example:
    // Current = September 2026
    // Default  = August 2026
    //
    // Also handles January correctly:
    // Current = January 2026
    // Default  = December 2025
    // --------------------------------------------------------
    const previousDate = new Date(
        today.getFullYear(),
        today.getMonth() - 1,
        1
    );


    const previous =
        previousDate.getFullYear().toString() +
        String(previousDate.getMonth() + 1).padStart(2, "0");


    // --------------------------------------------------------
    // Keep filter empty initially
    // Dashboard will still load N-1 month
    // --------------------------------------------------------
    $("#readingMonth").val("");


    // --------------------------------------------------------
    // Default dashboard = N-1 month only
    // --------------------------------------------------------
    currentReadingMonth = previous;


    // --------------------------------------------------------
    // Update reading month strip
    // --------------------------------------------------------
    updateReadingMonthStrip();


    // --------------------------------------------------------
    // Initial dashboard load
    // --------------------------------------------------------
    loadDashboard();


    // ========================================================
    // Apply button click
    // ========================================================
    $("#btnLoadDashboard").on("click", function () {

        const selected = $("#readingMonth").val();


        // No month selected
        if (!selected)
            return;


        // ----------------------------------------------------
        // Convert YYYY-MM to YYYYMM
        //
        // Example:
        // 2026-08 -> 202608
        // ----------------------------------------------------
        currentReadingMonth = selected.replace("-", "");


        // ----------------------------------------------------
        // Update reading month strip
        // ----------------------------------------------------
        updateReadingMonthStrip();


        // ----------------------------------------------------
        // Reload dashboard
        // ----------------------------------------------------
        loadDashboard();

    });

});


// ============================================================
// Updates the reading month display strip
// ============================================================
function updateReadingMonthStrip() {

    const strip = $("#readingMonthStrip");


    // Nothing selected
    if (!currentReadingMonth) {

        strip.html("");

        return;

    }


    // --------------------------------------------------------
    // Convert comma-separated months into an array
    //
    // Example:
    // "202607,202608"
    // ->
    // ["202607", "202608"]
    // --------------------------------------------------------
    const months = currentReadingMonth
        .split(",")
        .filter(x => x);


    // --------------------------------------------------------
    // Format YYYYMM into "Aug 2026"
    // --------------------------------------------------------
    const formattedMonths = months.map(month => {

        // Safety check
        if (month.length !== 6)
            return month;


        const year =
            Number(month.substring(0, 4));


        const monthNumber =
            Number(month.substring(4, 6));


        const date = new Date(
            year,
            monthNumber - 1,
            1
        );


        return date.toLocaleString("en-US", {

            month: "short",

            year: "numeric"

        });

    });

    strip.html(`
    <i class="bi bi-bar-chart-line-fill"></i>
    Data shown for: ${formattedMonths.join(" + ")}
`);

}
