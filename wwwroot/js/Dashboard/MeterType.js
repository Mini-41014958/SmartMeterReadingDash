
function getApiUrl(endpoint) {

    const basePath = window.location.pathname
        .toLowerCase()
        .startsWith("/smartmeter/")
        ? "/SmartMeter"
        : "";

    return `${basePath}/api/${endpoint}`;
}
let brplMeterTypeChart = null;
let byplMeterTypeChart = null;


async function loadMeterSummary() {

    const month = getReadingMonth();

    try {

        // =====================================================
        // LOAD BRPL + BYPL
        // =====================================================

        const [
            brplResponse,
            byplResponse
        ] = await Promise.all([

            fetch(
                `${getApiUrl("DashboardApi/meter-type-wise-summary")}?readingMonth=${encodeURIComponent(month)}`
            ),

            fetch(
                `${getApiUrl("DashboardApi/meter-type-wise-summary-bypl")}?readingMonth=${encodeURIComponent(month)}`
            )

        ]);


        if (!brplResponse.ok) {
            throw new Error(
                `Failed to load BRPL Meter Summary. Status: ${brplResponse.status}`
            );
        }

        if (!byplResponse.ok) {
            throw new Error(
                `Failed to load BYPL Meter Summary. Status: ${byplResponse.status}`
            );
        }


        const brplData =
            await brplResponse.json();

        const byplData =
            await byplResponse.json();


        // =====================================================
        // BRPL TABLE
        // =====================================================

        $("#brplAlliedCount").text(
            Number(
                brplData.alliedCount || 0
            ).toLocaleString()
        );

        $("#brplAllied1Ph").text(
            Number(
                brplData.allied_1PhCount || 0
            ).toLocaleString()
        );

        $("#brplAllied3Ph").text(
            Number(
                brplData.allied_3PhCount || 0
            ).toLocaleString()
        );

        $("#brplKimbalCount").text(
            Number(
                brplData.kimbalCount || 0
            ).toLocaleString()
        );

        $("#brplKimbal1Ph").text(
            Number(
                brplData.kimbal_1PhCount || 0
            ).toLocaleString()
        );

        $("#brplKimbal3Ph").text(
            Number(
                brplData.kimbal_3PhCount || 0
            ).toLocaleString()
        );

        $("#brplTotalMeter").text(
            Number(
                brplData.totalMeter || 0
            ).toLocaleString()
        );


        // =====================================================
        // BYPL TABLE
        // =====================================================

        $("#byplAlliedCount").text(
            Number(
                byplData.alliedCount || 0
            ).toLocaleString()
        );

        $("#byplAllied1Ph").text(
            Number(
                byplData.allied_1PhCount || 0
            ).toLocaleString()
        );

        $("#byplAllied3Ph").text(
            Number(
                byplData.allied_3PhCount || 0
            ).toLocaleString()
        );

        $("#byplKimbalCount").text(
            Number(
                byplData.kimbalCount || 0
            ).toLocaleString()
        );

        $("#byplKimbal1Ph").text(
            Number(
                byplData.kimbal_1PhCount || 0
            ).toLocaleString()
        );

        $("#byplKimbal3Ph").text(
            Number(
                byplData.kimbal_3PhCount || 0
            ).toLocaleString()
        );

        $("#byplTotalMeter").text(
            Number(
                byplData.totalMeter || 0
            ).toLocaleString()
        );


        // =====================================================
        // DRAW BRPL PIE CHART
        // =====================================================

        drawMeterTypeChart(
            brplData,
            "brplMeterTypeChart",
            "BRPL"
        );


        // =====================================================
        // DRAW BYPL PIE CHART
        // =====================================================

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

    }
}


// =============================================================
// METER TYPE PIE CHART
// =============================================================

function drawMeterTypeChart(
    data,
    canvasId,
    company
) {

    const canvas =
        document.getElementById(canvasId);


    if (!canvas) {

        console.error(
            `Canvas not found: ${canvasId}`
        );

        return;
    }


    // =====================================================
    // DESTROY EXISTING CHART
    // =====================================================

    if (
        company === "BRPL" &&
        brplMeterTypeChart
    ) {

        brplMeterTypeChart.destroy();

        brplMeterTypeChart = null;
    }


    if (
        company === "BYPL" &&
        byplMeterTypeChart
    ) {

        byplMeterTypeChart.destroy();

        byplMeterTypeChart = null;
    }


    // =====================================================
    // DATA
    // =====================================================

    const allied =
        Number(
            data.alliedCount || 0
        );

    const kimbal =
        Number(
            data.kimbalCount || 0
        );

    const total =
        allied + kimbal;


    // =====================================================
    // COLORS
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


    // =====================================================
    // CREATE PIE CHART
    // =====================================================

    const chart =
        new Chart(canvas, {

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

                    borderWidth: 2,

                    hoverOffset: 6

                }]

            },


            options: {

                responsive: true,

                maintainAspectRatio: false,

                animation: {

                    animateRotate: true,

                    animateScale: true,

                    duration: 700

                },


                plugins: {

                    legend: {

                        position: "bottom",

                        labels: {

                            usePointStyle: true,

                            pointStyle: "circle",

                            padding: 6,

                            boxWidth: 8,

                            boxHeight: 8,

                            font: {

                                size: 9,

                                weight: "600"

                            }

                        }

                    },


                    tooltip: {

                        backgroundColor:
                            "rgba(33, 37, 41, 0.95)",

                        padding: 8,

                        cornerRadius: 6,


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


    // =====================================================
    // SAVE CHART INSTANCE
    // =====================================================

    if (company === "BRPL") {

        brplMeterTypeChart = chart;

    }
    else if (company === "BYPL") {

        byplMeterTypeChart = chart;

    }

}