let meterTypeChart = null;

async function loadMeterSummary() {

    const [downloadRes, failedRes] = await Promise.all([
        fetch("/api/dashboardapi/temp-hes-wise-data"),
        fetch("/api/dashboardapi/temp-hes-failed-wise-data")
    ]);

    const download = (await downloadRes.json())[0];
    const failed = (await failedRes.json())[0];

    const alliedCount = download.alliedCount + failed.alliedCount;
    const kimbalCount = download.kimbalCount + failed.kimbalCount;
    const totalMeter = alliedCount + kimbalCount;

    document.getElementById("alliedCount").textContent =
        alliedCount.toLocaleString();

    document.getElementById("kimbalCount").textContent =
        kimbalCount.toLocaleString();

    document.getElementById("totalMeter").textContent =
        totalMeter.toLocaleString();

    drawMeterTypeChart({
        alliedCount,
        kimbalCount
    });
}

function drawMeterTypeChart(data) {

    const ctx = document.getElementById("meterTypeChart");

    if (meterTypeChart) {
        meterTypeChart.destroy();
    }

    meterTypeChart = new Chart(ctx, {

        type: "doughnut",

        data: {

            labels: ["Allied", "Kimbal"],

            datasets: [{
                data: [
                    data.alliedCount,
                    data.kimbalCount
                ],

                backgroundColor: [
                    "#4F46E5",
                    "#10B981"
                ],

                borderColor: "#ffffff",
                borderWidth: 2,
                hoverOffset: 8
            }]
        },

        options: {

            responsive: true,
            maintainAspectRatio: false,

            cutout: "68%",

            plugins: {

                legend: {
                    position: "bottom",
                    labels: {
                        usePointStyle: true,
                        pointStyle: "circle",
                        padding: 20,
                        font: {
                            size: 12
                        }
                    }
                },

                tooltip: {

                    callbacks: {

                        label: function (context) {

                            const value = context.raw;

                            const total = context.dataset.data.reduce((a, b) => a + b, 0);

                            const percentage = ((value / total) * 100).toFixed(1);

                            return `${context.label}: ${value.toLocaleString()} (${percentage}%)`;
                        }
                    }
                }

            }

        }

    });

}