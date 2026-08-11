async function loadMeterDownloadSummary() {

    const month = getReadingMonth();
    try {
        const [
            brplResponse,
            byplResponse
        ] = await Promise.all([

            fetch(
                `api/dashboardapi/meter-download-summary?readingMonth=${encodeURIComponent(month)}`
            ),

            fetch(
                `api/dashboardapi/meter-download-summary-bypl?readingMonth=${encodeURIComponent(month)}`
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

        // BYPL API is returning an array
        const byplData = Array.isArray(byplResult)
            ? byplResult[0]
            : byplResult;

        console.log("BRPL Meter Summary:", brplData);
        console.log("BYPL Meter Summary:", byplData);

        const failed =
            (brplData.manualForwardinCount || 0) +
            (brplData.pendingCount || 0) +
            (brplData.mismatchCount || 0);

        document.getElementById("totalMeters").textContent =
            (brplData.totalMetersCount || 0).toLocaleString();

        // BYPL
        document.getElementById("byplTotalMeters").textContent =
            (byplData?.totalMetersCount || 0).toLocaleString();

        document.getElementById("hesDownload").textContent =
            (brplData.hesDownloadCount || 0).toLocaleString();

        // BYPL
        document.getElementById("byplHesDownload").textContent =
            (byplData?.hesDownloadCount || 0).toLocaleString();

        document.getElementById("downloadFailed").textContent =
            failed.toLocaleString();

        // BYPL
        document.getElementById("byplDownloadFailed").textContent =
            (byplData?.hesFailedCount || 0).toLocaleString();

        document.getElementById("downloadPercentage").textContent =
            (brplData.hesDownloadPercentage || 0).toFixed(2) + "%";

        // BYPL
        document.getElementById("byplDownloadPercentage").textContent =
            (byplData?.hesDownloadPercentage || 0).toFixed(2) + "%";

        document.getElementById("failedPercentage").textContent =
            (brplData.hesFailedPercentage || 0).toFixed(2) + "%";

        // BYPL
        document.getElementById("byplFailedPercentage").textContent =
            (byplData?.hesFailedPercentage || 0).toFixed(2) + "%";

        document.getElementById("summaryDate").textContent =
            new Date().toLocaleDateString("en-GB");
    }
    catch (error) {

        console.error("Meter Summary Error:", error);

        throw error;

    }

}