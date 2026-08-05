async function loadMeterDownloadSummary() {
    const month = getReadingMonth();
    const response = await fetch(`api/dashboardapi/meter-download-summary?readingMonth=${month}`);

    if (!response.ok) {
        throw new Error("Failed to load Meter Download Summary.");
    }

    const data = await response.json();

    const failed =
        data.manualForwardinCount +
        data.pendingCount +
        data.mismatchCount;

    document.getElementById("totalMeters").textContent =
        data.totalMetersCount.toLocaleString();

    document.getElementById("hesDownload").textContent =
        data.hesDownloadCount.toLocaleString();

    document.getElementById("downloadFailed").textContent =
        failed.toLocaleString();

    document.getElementById("downloadPercentage").textContent =
        data.hesDownloadPercentage.toFixed(2) + "%";

    document.getElementById("failedPercentage").textContent =
        data.hesFailedPercentage.toFixed(2) + "%";

    document.getElementById("summaryDate").textContent =
        new Date().toLocaleDateString("en-GB");

}