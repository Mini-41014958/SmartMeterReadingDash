
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

            // Allied
            $("#brplAllied1Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        brplData,
                        "allied_1PhCount",
                        "Allied_1PhCount"
                    )
                )
            );

            $("#brplAllied3Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        brplData,
                        "allied_3PhCount",
                        "Allied_3PhCount"
                    )
                )
            );

            // Kimbal
            $("#brplKimbal1Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        brplData,
                        "kimbal_1PhCount",
                        "Kimbal_1PhCount"
                    )
                )
            );

            $("#brplKimbal3Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        brplData,
                        "kimbal_3PhCount",
                        "Kimbal_3PhCount"
                    )
                )
            );

            // Secure: separate 3-phase count
            $("#brplSecure3Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        brplData,
                        "secure_3PhCount",
                        "Secure_3PhCount",
                        "secure3PhCount",
                        "Secure3PhCount"
                    )
                )
            );

            // Total meters
            $("#brplTotalMeter").text(
                formatMeterNumber(
                    getApiNumber(
                        brplData,
                        "totalMeter",
                        "TotalMeter"
                    )
                )
            );

            // BRPL chart
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

            // Allied
            $("#byplAllied1Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        byplData,
                        "allied_1PhCount",
                        "Allied_1PhCount"
                    )
                )
            );

            $("#byplAllied3Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        byplData,
                        "allied_3PhCount",
                        "Allied_3PhCount"
                    )
                )
            );

            // Kimbal
            $("#byplKimbal1Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        byplData,
                        "kimbal_1PhCount",
                        "Kimbal_1PhCount"
                    )
                )
            );

            $("#byplKimbal3Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        byplData,
                        "kimbal_3PhCount",
                        "Kimbal_3PhCount"
                    )
                )
            );

            // Secure
            $("#byplSecure3Ph").text(
                formatMeterNumber(
                    getApiNumber(
                        byplData,
                        "secure_3PhCount",
                        "Secure_3PhCount",
                        "secure3PhCount",
                        "Secure3PhCount"
                    )
                )
            );

            // Total meters
            $("#byplTotalMeter").text(
                formatMeterNumber(
                    getApiNumber(
                        byplData,
                        "totalMeter",
                        "TotalMeter"
                    )
                )
            );

            // BYPL chart
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
// Allied + Kimbal + Secure
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
        "AlliedCount"
    );

    const kimbal = getApiNumber(
        data,
        "kimbalCount",
        "KimbalCount"
    );

    const secure = getApiNumber(
        data,
        "secureCount",
        "SecureCount"
    );

    // Pie percentages are calculated from all three vendors.
    const total = allied + kimbal + secure;

    // =========================================================
    // COLORS
    // =========================================================

    const alliedColor =
        company === "BRPL" ? "#4F46E5" : "#6366F1";

    const kimbalColor =
        company === "BRPL" ? "#10B981" : "#14B8A6";

    const secureColor = "#F59E0B";

    // =========================================================
    // CREATE PIE CHART
    // =========================================================

    const chart = new Chart(canvas, {
        type: "pie",

        data: {
            labels: [
                "Allied",
                "Kimbal",
                "Secure"
            ],

            datasets: [
                {
                    data: [
                        allied,
                        kimbal,
                        secure
                    ],

                    backgroundColor: [
                        alliedColor,
                        kimbalColor,
                        secureColor
                    ],

                    borderColor: "#ffffff",
                    borderWidth: 2,
                    hoverOffset: 6
                }
            ]
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
