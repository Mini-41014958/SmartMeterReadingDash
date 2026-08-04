async function loadDashboardTemporary() {

    try {

        const [downloadResponse, failedResponse] = await Promise.all([
            fetch("/api/dashboardapi/temp-hes-wise-data"),
            fetch("/api/dashboardapi/temp-hes-failed-wise-data")
        ]);

        const downloadJson = await downloadResponse.json();
        const failedJson = await failedResponse.json();

        const d = downloadJson[0];
        const f = failedJson[0];

        // Overall Counts
        const totalMeters = d.hesDownload + f.hesFailed;

        const alliedTotal = d.alliedCount + f.alliedCount;
        const kimbalTotal = d.kimbalCount + f.kimbalCount;
        const slccTotal = d.slcCount + f.slcCount;
        const mlccTotal = d.mlccCount + f.mlccCount;
        const gccTotal = d.gcCount + f.gcCount;
        const kccTotal = d.kcCount + f.kcCount;

        const downloadPercentage =
            totalMeters == 0 ? 0 :
                ((d.hesDownload / totalMeters) * 100).toFixed(2);

        const failedPercentage =
            totalMeters == 0 ? 0 :
                ((f.hesFailed / totalMeters) * 100).toFixed(2);

        // Meter Summary
        $("#alliedCount").text(alliedTotal.toLocaleString());
        $("#kimbalCount").text(kimbalTotal.toLocaleString());
        $("#totalMeter").text(totalMeters.toLocaleString());

        // Meter Received
        $("#totalMeters").text(totalMeters.toLocaleString());
        $("#hesDownload").text(d.hesDownload.toLocaleString());
        $("#downloadFailed").text(f.hesFailed.toLocaleString());
        $("#downloadPercentage").text(downloadPercentage + "%");

        // If you have department cards
        $("#slccCount").text(slccTotal.toLocaleString());
        $("#mlccCount").text(mlccTotal.toLocaleString());
        $("#gccCount").text(gccTotal.toLocaleString());
        $("#kccCount").text(kccTotal.toLocaleString());

        // Update Doughnut Chart
        updateMeterTypeChart(alliedTotal, kimbalTotal);

        // Update Department Chart
        updateDepartmentChart(slccTotal, mlccTotal, gccTotal, kccTotal);

    }
    catch (e) {
        console.error("Dashboard Load Failed", e);
    }

}