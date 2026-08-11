// Global reading month shared by all API calls
let currentReadingMonth = "";

// Loads all dashboard components
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

// Returns the reading month for all APIs
function getReadingMonth() {
    return currentReadingMonth;
}

// Initial Page Load
document.addEventListener("DOMContentLoaded", function () {

    const today = new Date();

    // Show current month in picker
    $("#readingMonth").val("");

    // Current month
    const current =
        today.getFullYear().toString() +
        String(today.getMonth() + 1).padStart(2, "0");

    // Previous month
    const previousDate = new Date(today.getFullYear(), today.getMonth() - 1, 1);

    const previous =
        previousDate.getFullYear().toString() +
        String(previousDate.getMonth() + 1).padStart(2, "0");

    // Default dashboard = previous + current month
    currentReadingMonth = previous + "," + current;
    updateReadingMonthStrip();
    // Initial Load
    loadDashboard();

    // Apply button click
    $("#btnLoadDashboard").on("click", function () {

        const selected = $("#readingMonth").val();

        if (!selected)
            return;

        // After Apply -> only selected month
        currentReadingMonth = selected.replace("-", "");
        updateReadingMonthStrip();
        loadDashboard();

    });

});
function updateReadingMonthStrip() {

    const strip = $("#readingMonthStrip");

    if (!currentReadingMonth) {
        strip.html("");
        return;
    }

    const months = currentReadingMonth
        .split(",")
        .filter(x => x);


    const formattedMonths = months.map(month => {

        if (month.length !== 6)
            return month;

        const year = Number(month.substring(0, 4));
        const monthNumber = Number(month.substring(4, 6));

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