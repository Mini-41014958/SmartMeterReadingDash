async function loadMeterDownloadSummary() {

    try {

        const response = await fetch("api/dashboardapi/meter-download-summary");

        if (!response.ok) {
            throw new Error("Unable to load meter download summary.");
        }

        const data = await response.json();

        // Total Meter
        document.getElementById("totalMeters").textContent =
            data.totalMetersCount.toLocaleString();

        // HES Download
        document.getElementById("hesDownload").textContent =
            data.hesDownloadCount.toLocaleString();

        // Manual / Forwarding = Manual + Pending + Mismatch
        const manualForwarding =
            data.manualForwardinCount +
            data.pendingCount +
            data.mismatchCount;

        document.getElementById("manualForwarding").textContent =
            manualForwarding.toLocaleString();

        // Download Percentage
        document.getElementById("downloadPercentage").textContent =
            data.hesDownloadPercentage.toFixed(1) + "%";

    }
    catch (error) {

        console.error("Meter Download Summary Error:", error);

    }
}