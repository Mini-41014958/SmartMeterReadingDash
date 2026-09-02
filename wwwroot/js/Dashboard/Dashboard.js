let currentReadingMonth = "";

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

        console.error(
            "Dashboard Loading Error:",
            err
        );

    }
    finally {

        $("#dashboardSkeleton").fadeOut(
            300,
            function () {

                $("#dashboardContent").fadeIn(300);

            }
        );

    }

}

function getReadingMonth() {

    return currentReadingMonth;

}

function getPreviousReadingMonth() {

    const today = new Date();

    const previousDate = new Date(
        today.getFullYear(),
        today.getMonth() - 1,
        1
    );

    const year =
        previousDate.getFullYear().toString();

    const month =
        String(
            previousDate.getMonth() + 1
        ).padStart(2, "0");

    return year + month;

}

function formatReadingMonth(month) {

    if (
        !month ||
        month.length !== 6
    ) {
        return "";
    }

    const year =
        Number(
            month.substring(0, 4)
        );

    const monthNumber =
        Number(
            month.substring(4, 6)
        );

    const date = new Date(
        year,
        monthNumber - 1,
        1
    );

    return date.toLocaleString(
        "en-US",
        {
            month: "short",
            year: "numeric"
        }
    );

}

function updateReadingMonthDisplay() {

    const display =
        $("#readingMonthDisplay");

    if (!display.length) {
        return;
    }

    display.text(
        "Data: " +
        formatReadingMonth(
            currentReadingMonth
        )
    );

}

document.addEventListener(
    "DOMContentLoaded",
    function () {

        currentReadingMonth =
            getPreviousReadingMonth();

        $("#readingMonth").val(
            currentReadingMonth.substring(0, 4) +
            "-" +
            currentReadingMonth.substring(4, 6)
        );

        updateReadingMonthDisplay();

        loadDashboard();


        $("#btnLoadDashboard").on(
            "click",
            function () {

                const selected =
                    $("#readingMonth").val();


                if (!selected) {

                    return;

                }

                currentReadingMonth =
                    selected.replace("-", "");




                updateReadingMonthDisplay();

                loadDashboard();

            }
        );

    }
);