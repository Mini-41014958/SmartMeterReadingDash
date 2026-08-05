
// Loads all dashboard components
async function loadDashboard() {

    $("#dashboardSkeleton").show();
    $("#dashboardContent").hide();

    try {

        await Promise.all([
            loadMeterSummary(),
            loadMeterDownloadSummary(),
            loadDepartmentDistribution(),
            loadReadingTrend()
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

// Initial Page Load
document.addEventListener("DOMContentLoaded", function () {

    // Set current month
    const today = new Date();

    const currentMonth =
        `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}`;

    $("#readingMonth").val(currentMonth);

    // Initial Dashboard Load
    loadDashboard();

    // Reload dashboard when month changes
    $("#readingMonth").on("change", function () {

        loadDashboard();

    });

});