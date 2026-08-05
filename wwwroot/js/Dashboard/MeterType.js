let meterTypeChart = null;

async function loadMeterSummary() {
    const month = getReadingMonth();
    const response = await fetch(`api/DashboardApi/meter-type-wise-summary?readingMonth=${month}`);

    if (!response.ok) {
        throw new Error("Failed to load Meter Summary.");
    }

    const data = await response.json();

    // Update Summary
    document.getElementById("alliedCount").textContent =
        data.alliedCount.toLocaleString();

    document.getElementById("kimbalCount").textContent =
        data.kimbalCount.toLocaleString();

    document.getElementById("totalMeter").textContent =
        data.totalMeter.toLocaleString();

    drawMeterTypeChart(data);
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