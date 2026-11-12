
"use strict";

// =============================================================
// API URL
// =============================================================

function getApiUrl(endpoint) {

    const basePath = window.location.pathname
        .toLowerCase()
        .startsWith("/smartmeter/")
        ? "/SmartMeter"
        : "";

    return `${basePath}/api/${endpoint}`;
}


// =============================================================
// CHART INSTANCES
// =============================================================

let brplMeterTypeChart = null;
let byplMeterTypeChart = null;


// =============================================================
// READ API NUMBER SAFELY
// =============================================================

function getApiNumber(data, ...keys) {

    if (!data) {
        return 0;
    }

    for (const key of keys) {

        if (
            Object.prototype.hasOwnProperty.call(data, key) &&
            data[key] !== null &&
            data[key] !== undefined &&
            data[key] !== ""
        ) {

            const value = Number(data[key]);

            return Number.isFinite(value) ? value : 0;
        }
    }

    return 0;
}


// =============================================================
// FORMAT NUMBER
// =============================================================

function formatMeterNumber(value) {

    return Number(value || 0).toLocaleString("en-IN");
}


// =============================================================
// LOAD METER SUMMARY
// =============================================================

async function loadMeterSummary() {

    const month = getReadingMonth();

    if (!month) {

        console.warn("Reading month is not selected.");

        return;
    }

    try {

        // =====================================================
        // BRPL
        // =====================================================

        if (canAccessCompany("BRPL")) {

            const response = await fetch(
                `${getApiUrl(
                    "DashboardApi/meter-type-wise-summary"
                )}?readingMonth=${encodeURIComponent(month)}`,
                {
                    cache: "no-store"
                }
            );

            if (!response.ok) {

                throw new Error(
                    `Failed to load BRPL Meter Summary. Status: ${response.status}`
                );
            }

            const brplData = await response.json();

            console.log("BRPL Meter Summary:", brplData);


            // =================================================
            // ALLIED
            // =================================================

            $("#brplAllied1Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        brplData,
                        "allied_1PhCount",
                        "Allied_1PhCount",
                        "ALLIED_1PH"
                    )
                )
            );


            $("#brplAllied3Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        brplData,
                        "allied_3PhCount",
                        "Allied_3PhCount",
                        "ALLIED_3PH"
                    )
                )
            );


            // =================================================
            // KIMBAL
            // =================================================

            $("#brplKimbal1Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        brplData,
                        "kimbal_1PhCount",
                        "Kimbal_1PhCount",
                        "KIMBAL_1PH"
                    )
                )
            );


            $("#brplKimbal3Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        brplData,
                        "kimbal_3PhCount",
                        "Kimbal_3PhCount",
                        "KIMBAL_3PH"
                    )
                )
            );


            // =================================================
            // SECURE: 3-PHASE
            // =================================================

            $("#brplSecure3Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        brplData,
                        "secure_3PhCount",
                        "Secure_3PhCount",
                        "secure3PhCount",
                        "Secure3PhCount",
                        "SECURE_3PH"
                    )
                )
            );


            // =================================================
            // GENUS: 3-PHASE
            // =================================================

            $("#brplGenus3Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        brplData,
                        "genus_3PhCount",
                        "Genus_3PhCount",
                        "genus_3PHCount",
                        "Genus_3PHCount",
                        "genus3PhCount",
                        "Genus3PhCount",
                        "GENUS_3PH"
                    )
                )
            );


            // =================================================
            // TOTAL METERS
            // =================================================

            $("#brplTotalMeter").text(
                formatMeterNumber(
                    getApiNumber(
                        brplData,
                        "totalMeter",
                        "TotalMeter",
                        "TOTALMETERS"
                    )
                )
            );


            // =================================================
            // BRPL CHART
            // =================================================

            drawMeterTypeChart(
                brplData,
                "brplMeterTypeChart",
                "BRPL"
            );
        }


        // =====================================================
        // BYPL
        // =====================================================

        if (canAccessCompany("BYPL")) {

            const response = await fetch(
                `${getApiUrl(
                    "DashboardApi/meter-type-wise-summary-bypl"
                )}?readingMonth=${encodeURIComponent(month)}`,
                {
                    cache: "no-store"
                }
            );

            if (!response.ok) {

                throw new Error(
                    `Failed to load BYPL Meter Summary. Status: ${response.status}`
                );
            }

            const byplData = await response.json();

            console.log("BYPL Meter Summary:", byplData);


            // =================================================
            // ALLIED
            // =================================================

            $("#byplAllied1Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        byplData,
                        "allied_1PhCount",
                        "Allied_1PhCount",
                        "ALLIED_1PH"
                    )
                )
            );


            $("#byplAllied3Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        byplData,
                        "allied_3PhCount",
                        "Allied_3PhCount",
                        "ALLIED_3PH"
                    )
                )
            );


            // =================================================
            // KIMBAL
            // =================================================

            $("#byplKimbal1Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        byplData,
                        "kimbal_1PhCount",
                        "Kimbal_1PhCount",
                        "KIMBAL_1PH"
                    )
                )
            );


            $("#byplKimbal3Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        byplData,
                        "kimbal_3PhCount",
                        "Kimbal_3PhCount",
                        "KIMBAL_3PH"
                    )
                )
            );


            // =================================================
            // SECURE: 3-PHASE
            // =================================================

            $("#byplSecure3Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        byplData,
                        "secure_3PhCount",
                        "Secure_3PhCount",
                        "secure3PhCount",
                        "Secure3PhCount",
                        "SECURE_3PH"
                    )
                )
            );


            // =================================================
            // GENUS: 3-PHASE
            // =================================================

            $("#byplGenus3Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        byplData,
                        "genus_3PhCount",
                        "Genus_3PhCount",
                        "genus_3PHCount",
                        "Genus_3PHCount",
                        "genus3PhCount",
                        "Genus3PhCount",
                        "GENUS_3PH"
                    )
                )
            );


            // =================================================
            // TOTAL METERS
            // =================================================

            $("#byplTotalMeter").text(
                formatMeterNumber(
                    getApiNumber(
                        byplData,
                        "totalMeter",
                        "TotalMeter",
                        "TOTALMETERS"
                    )
                )
            );


            // =================================================
            // BYPL CHART
            // =================================================

            drawMeterTypeChart(
                byplData,
                "byplMeterTypeChart",
                "BYPL"
            );
        }

    } catch (error) {

        console.error("Meter Summary Error:", error);
    }
}


// =============================================================
// METER TYPE PIE CHART
// Allied + Kimbal + Secure + Genus
// =============================================================

function drawMeterTypeChart(data, canvasId, company) {

    const canvas = document.getElementById(canvasId);

    if (!canvas) {

        console.error(`Canvas not found: ${canvasId}`);

        return;
    }


    // =========================================================
    // DESTROY EXISTING CHART
    // =========================================================

    if (company === "BRPL" && brplMeterTypeChart) {

        brplMeterTypeChart.destroy();

        brplMeterTypeChart = null;
    }

    if (company === "BYPL" && byplMeterTypeChart) {

        byplMeterTypeChart.destroy();

        byplMeterTypeChart = null;
    }


    // =========================================================
    // VENDOR COUNTS
    // =========================================================

    const allied = getApiNumber(
        data,
        "alliedCount",
        "AlliedCount",
        "ALLIEDCOUNT"
    );


    const kimbal = getApiNumber(
        data,
        "kimbalCount",
        "KimbalCount",
        "KIMBALCOUNT"
    );


    const secure = getApiNumber(
        data,
        "secureCount",
        "SecureCount",
        "SECURECOUNT"
    );


    // =========================================================
    // GENUS COUNT
    // =========================================================

    const genus = getApiNumber(
        data,
        "genusCount",
        "GenusCount",
        "GENUSCOUNT"
    );


    // =========================================================
    // TOTAL FOR PERCENTAGE CALCULATION
    // =========================================================

    const total = allied + kimbal + secure + genus;


    // =========================================================
    // COLORS
    // =========================================================

    const alliedColor =
        company === "BRPL" ? "#4F46E5" : "#6366F1";

    const kimbalColor =
        company === "BRPL" ? "#10B981" : "#14B8A6";

    const secureColor = "#F59E0B";

    const genusColor = "#EF4444";


    // =========================================================
    // CREATE PIE CHART
    // =========================================================

    const chart = new Chart(canvas, {

        type: "pie",

        data: {

            labels: [
                "Allied",
                "Kimbal",
                "Secure",
                "Genus"
            ],

            datasets: [
                {

                    data: [
                        allied,
                        kimbal,
                        secure,
                        genus
                    ],

                    backgroundColor: [
                        alliedColor,
                        kimbalColor,
                        secureColor,
                        genusColor
                    ],

                    borderColor: "#ffffff",

                    borderWidth: 2,

                    hoverOffset: 6
                }
            ]
        },


        // =====================================================
        // CHART OPTIONS
        // =====================================================

        options: {

            responsive: true,

            maintainAspectRatio: false,


            animation: {

                animateRotate: true,

                animateScale: true,

                duration: 700
            },


            plugins: {

                // =================================================
                // LEGEND
                // =================================================

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


                // =================================================
                // TOOLTIP
                // =================================================

                tooltip: {

                    backgroundColor: "rgba(33, 37, 41, 0.95)",

                    padding: 8,

                    cornerRadius: 6,


                    callbacks: {

                        label: function (context) {

                            const value = Number(
                                context.raw || 0
                            );

                            const percentage = total > 0

                                ? ((value / total) * 100).toFixed(1)

                                : "0.0";


                            return (
                                `${context.label}: ` +
                                `${value.toLocaleString("en-IN")} ` +
                                `(${percentage}%)`
                            );
                        }
                    }
                }
            }
        }
    });


    // =========================================================
    // SAVE CHART INSTANCE
    // =========================================================

    if (company === "BRPL") {

        brplMeterTypeChart = chart;

    } else if (company === "BYPL") {

        byplMeterTypeChart = chart;
    }
}


// =============================================================
// OPTIONAL: RELOAD SUMMARY WHEN READING MONTH CHANGES
// Call loadMeterSummary() from your existing month-change event.
// =============================================================
