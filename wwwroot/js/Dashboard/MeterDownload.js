async function loadMeterDownloadSummary() {

    const [downloadRes, failedRes] = await Promise.all([
        fetch("/api/dashboardapi/temp-hes-wise-data"),
        fetch("/api/dashboardapi/temp-hes-failed-wise-data")
    ]);

    const download = (await downloadRes.json())[0];
    const failed = (await failedRes.json())[0];

    const hesDownload = download.hesDownload;
    const downloadFailed = failed.hesFailed;
    const totalMeters = hesDownload + downloadFailed;

    const percentage =
        totalMeters === 0
            ? 0
            : (hesDownload / totalMeters) * 100;

    document.getElementById("totalMeters").textContent =
        totalMeters.toLocaleString();

    document.getElementById("hesDownload").textContent =
        hesDownload.toLocaleString();

    document.getElementById("downloadFailed").textContent =
        downloadFailed.toLocaleString();

    document.getElementById("downloadPercentage").textContent =
        percentage.toFixed(2) + "%";

    document.getElementById("summaryDate").textContent =
        new Date().toLocaleDateString("en-GB");
}