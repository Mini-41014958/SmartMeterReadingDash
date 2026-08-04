document.addEventListener("DOMContentLoaded", async function () {

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

});