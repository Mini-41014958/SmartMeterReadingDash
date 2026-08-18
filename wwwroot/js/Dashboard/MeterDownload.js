async function loadMeterDownloadSummary() {

    const month = getReadingMonth();

    try {

        const [brplResponse, byplResponse] = await Promise.all([

            fetch(
                `/api/dashboardapi/meter-download-summary?readingMonth=${encodeURIComponent(month)}`
            ),

            fetch(
                `/api/dashboardapi/meter-download-summary-bypl?readingMonth=${encodeURIComponent(month)}`
            )

        ]);

        if (!brplResponse.ok) {
            throw new Error("Failed to load BRPL Meter Summary.");
        }

        if (!byplResponse.ok) {
            throw new Error("Failed to load BYPL Meter Summary.");
        }

        const brplData = await brplResponse.json();

        const byplResult = await byplResponse.json();

        const byplData = Array.isArray(byplResult)
            ? byplResult[0]
            : byplResult;

        console.log("BRPL Meter Summary:", brplData);
        console.log("BYPL Meter Summary:", byplData);


        const brplFailed =
            Number(brplData?.manualForwardinCount || 0) +
            Number(brplData?.pendingCount || 0) +
            Number(brplData?.mismatchCount || 0);


        function setValue(id, value) {

            const element = document.getElementById(id);

            if (!element) {

                console.error(
                    `Missing HTML element: #${id}`
                );

                return;
            }

            element.textContent = value;
        }

        setValue(
            "totalMeters",
            Number(brplData?.totalMetersCount || 0)
                .toLocaleString()
        );

        setValue(
            "hesDownload",
            Number(brplData?.hesDownloadCount || 0)
                .toLocaleString()
        );

        setValue(
            "downloadFailed",
            brplFailed.toLocaleString()
        );

        setValue(
            "downloadPercentage",
            Number(brplData?.hesDownloadPercentage || 0)
                .toFixed(2) + "%"
        );

        setValue(
            "failedPercentage",
            Number(brplData?.hesFailedPercentage || 0)
                .toFixed(2) + "%"
        );

        setValue(
            "byplTotalMeters",
            Number(byplData?.totalMetersCount || 0)
                .toLocaleString()
        );

        setValue(
            "byplHesDownload",
            Number(byplData?.hesDownloadCount || 0)
                .toLocaleString()
        );

        setValue(
            "byplDownloadFailed",
            Number(byplData?.hesFailedCount || 0)
                .toLocaleString()
        );

        setValue(
            "byplDownloadPercentage",
            Number(byplData?.hesDownloadPercentage || 0)
                .toFixed(2) + "%"
        );

        setValue(
            "byplFailedPercentage",
            Number(byplData?.hesFailedPercentage || 0)
                .toFixed(2) + "%"
        );

    }
    catch (error) {

        console.error(
            "Meter Summary Error:",
            error
        );

    }
}