let departmentChart = null;

async function loadDepartmentDistribution() {

    const [downloadRes, failedRes] = await Promise.all([
        fetch("/api/dashboardapi/temp-hes-wise-data"),
        fetch("/api/dashboardapi/temp-hes-failed-wise-data")
    ]);

    const d = (await downloadRes.json())[0];
    const f = (await failedRes.json())[0];

    const data = [
        {
            department: "SLCC",
            hesDownload: d.slcCount,
            nonCom: f.slcCount
        },
        {
            department: "MLCC",
            hesDownload: d.mlccCount,
            nonCom: f.mlccCount
        },
        {
            department: "GCC",
            hesDownload: d.gcCount,
            nonCom: f.gcCount
        },
        {
            department: "KCC",
            hesDownload: d.kcCount,
            nonCom: f.kcCount
        }
    ];

    const ctx = document.getElementById("departmentChart");

    if (departmentChart)
        departmentChart.destroy();

    departmentChart = new Chart(ctx, {

        type: "bar",

        data: {

            labels: data.map(x => x.department),

            datasets: [
                {
                    label: "Downloaded",
                    data: data.map(x => x.hesDownload),
                    backgroundColor: "#28a745"
                },
                {
                    label: "Not Downloaded",
                    data: data.map(x => x.nonCom),
                    backgroundColor: "#dc3545"
                }
            ]
        },

        options: {
            responsive: true,
            maintainAspectRatio: false,
            indexAxis: "y"
        }
    });
}