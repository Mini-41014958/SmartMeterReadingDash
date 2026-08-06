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

    // Initial Load
    loadDashboard();

    // Apply button click
    $("#btnLoadDashboard").on("click", function () {

        const selected = $("#readingMonth").val();

        if (!selected)
            return;

        // After Apply -> only selected month
        currentReadingMonth = selected.replace("-", "");

        loadDashboard();

    });

});