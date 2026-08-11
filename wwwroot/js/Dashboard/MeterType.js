let brplMeterTypeChart = null;
let byplMeterTypeChart = null;


async function loadMeterSummary() {

    const month = getReadingMonth();

    try {

        const [
            brplResponse,
            byplResponse
        ] = await Promise.all([

            fetch(
                `api/DashboardApi/meter-type-wise-summary?readingMonth=${encodeURIComponent(month)}`
            ),

            fetch(
                `api/DashboardApi/meter-type-wise-summary-bypl?readingMonth=${encodeURIComponent(month)}`
            )

        ]);


        if (!brplResponse.ok) {
            throw new Error("Failed to load BRPL Meter Summary.");
        }

        if (!byplResponse.ok) {
            throw new Error("Failed to load BYPL Meter Summary.");
        }


        const brplData = await brplResponse.json();
        const byplData = await byplResponse.json();


        // IMPORTANT DEBUG
        console.log("BRPL Meter Summary:", brplData);
        console.log("BYPL Meter Summary:", byplData);


        $("#brplAlliedCount").text(
            Number(brplData.alliedCount || 0).toLocaleString()
        );

        $("#brplKimbalCount").text(
            Number(brplData.kimbalCount || 0).toLocaleString()
        );

        $("#brplTotalMeter").text(
            Number(brplData.totalMeter || 0).toLocaleString()
        );


        $("#byplAlliedCount").text(
            Number(byplData.alliedCount || 0).toLocaleString()
        );

        $("#byplKimbalCount").text(
            Number(byplData.kimbalCount || 0).toLocaleString()
        );

        $("#byplTotalMeter").text(
            Number(byplData.totalMeter || 0).toLocaleString()
        );

        drawMeterTypeChart(
            brplData,
            "brplMeterTypeChart",
            "BRPL"
        );


        drawMeterTypeChart(
            byplData,
            "byplMeterTypeChart",
            "BYPL"
        );

    }
    catch (error) {

        console.error("Meter Summary Error:", error);

        throw error;

    }
}
function drawMeterTypeChart(data, canvasId, company) {

    const canvas = document.getElementById(canvasId);

    if (!canvas) {
        console.error(`Canvas not found: ${canvasId}`);
        return;
    }


    // Destroy existing chart
    if (company === "BRPL" && brplMeterTypeChart) {

        brplMeterTypeChart.destroy();
        brplMeterTypeChart = null;

    }

    if (company === "BYPL" && byplMeterTypeChart) {

        byplMeterTypeChart.destroy();
        byplMeterTypeChart = null;

    }


    const allied = Number(data.alliedCount || 0);
    const kimbal = Number(data.kimbalCount || 0);


    const chart = new Chart(canvas, {

        type: "doughnut",

        data: {

            labels: [
                "Allied",
                "Kimbal"
            ],

            datasets: [{

                data: [
                    allied,
                    kimbal
                ],

                backgroundColor: [
                    "#4F46E5",
                    "#10B981"
                ],

                borderColor: "#ffffff",

                borderWidth: 2,

                hoverOffset: 6

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

                        padding: 8,

                        font: {
                            size: 10
                        }

                    }

                },


                tooltip: {

                    callbacks: {

                        label: function (context) {

                            const value = Number(context.raw);

                            const total =
                                context.dataset.data.reduce(
                                    (a, b) => a + Number(b),
                                    0
                                );


                            const percentage =
                                total > 0
                                    ? ((value / total) * 100).toFixed(1)
                                    : "0.0";


                            return `${context.label}: ${value.toLocaleString()} (${percentage}%)`;

                        }

                    }

                }

            }

        }

    });


    // Store chart instance

    if (company === "BRPL") {

        brplMeterTypeChart = chart;

    }
    else if (company === "BYPL") {

        byplMeterTypeChart = chart;

    }

}