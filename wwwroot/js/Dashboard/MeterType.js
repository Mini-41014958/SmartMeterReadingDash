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

        console.log("BRPL Meter Summary:", brplData);
        console.log("BYPL Meter Summary:", byplData);

        // =====================================================
        // BRPL COUNTS
        // =====================================================

        $("#brplAlliedCount").text(
            Number(brplData.alliedCount || 0).toLocaleString()
        );

        $("#brplKimbalCount").text(
            Number(brplData.kimbalCount || 0).toLocaleString()
        );

        $("#brplTotalMeter").text(
            Number(brplData.totalMeter || 0).toLocaleString()
        );

        // =====================================================
        // BYPL COUNTS
        // =====================================================

        $("#byplAlliedCount").text(
            Number(byplData.alliedCount || 0).toLocaleString()
        );

        $("#byplKimbalCount").text(
            Number(byplData.kimbalCount || 0).toLocaleString()
        );

        $("#byplTotalMeter").text(
            Number(byplData.totalMeter || 0).toLocaleString()
        );

        // =====================================================
        // DRAW CHARTS
        // =====================================================

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

        console.error(
            "Meter Summary Error:",
            error
        );

        throw error;
    }
}


function drawMeterTypeChart(data, canvasId, company) {

    const canvas = document.getElementById(canvasId);

    if (!canvas) {

        console.error(
            `Canvas not found: ${canvasId}`
        );

        return;
    }

    // =====================================================
    // DESTROY EXISTING CHART
    // =====================================================

    if (company === "BRPL" && brplMeterTypeChart) {

        brplMeterTypeChart.destroy();
        brplMeterTypeChart = null;
    }

    if (company === "BYPL" && byplMeterTypeChart) {

        byplMeterTypeChart.destroy();
        byplMeterTypeChart = null;
    }

    // =====================================================
    // DATA
    // =====================================================

    const allied = Number(
        data.alliedCount || 0
    );

    const kimbal = Number(
        data.kimbalCount || 0
    );

    const total = allied + kimbal;

    // =====================================================
    // DIFFERENT SHADES FOR BRPL / BYPL
    // =====================================================

    let alliedColor;
    let kimbalColor;

    if (company === "BRPL") {

        alliedColor = "#4F46E5";
        kimbalColor = "#10B981";

    }
    else {

        alliedColor = "#6366F1";
        kimbalColor = "#14B8A6";
    }

    const chart = new Chart(canvas, {

        type: "pie",

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
                    alliedColor,
                    kimbalColor
                ],

                borderColor: "#ffffff",

                borderWidth: 3,

                hoverOffset: 10

            }]

        },

        options: {

            responsive: true,

            maintainAspectRatio: false,
            animation: {

                animateRotate: true,

                animateScale: true,

                duration: 900
            },

            plugins: {

                legend: {

                    position: "bottom",

                    labels: {

                        usePointStyle: true,

                        pointStyle: "circle",

                        padding: 12,

                        boxWidth: 10,

                        boxHeight: 10,

                        font: {

                            size: 11,

                            weight: "600"
                        }
                    }
                },

                tooltip: {

                    backgroundColor:
                        "rgba(33, 37, 41, 0.95)",

                    padding: 10,

                    cornerRadius: 7,

                    callbacks: {

                        label: function (context) {

                            const value =
                                Number(
                                    context.raw || 0
                                );

                            const percentage =
                                total > 0
                                    ? (
                                        (value / total) * 100
                                    ).toFixed(1)
                                    : "0.0";

                            return (
                                `${context.label}: ` +
                                `${value.toLocaleString()} ` +
                                `(${percentage}%)`
                            );
                        }
                    }
                }
            }
        }
    });

    if (company === "BRPL") {

        brplMeterTypeChart = chart;

    }
    else if (company === "BYPL") {

        byplMeterTypeChart = chart;
    }
}