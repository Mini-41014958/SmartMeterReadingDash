let readingTrendChart = null;


// =====================================================
// BYPL FAILURE CATEGORY
// =====================================================

function getBYPLFailureCategory(message) {

    const msg = String(message || "")
        .toUpperCase()
        .replace(/\s+/g, " ")
        .trim();

    if (msg.includes("SYSTEM TITLE")) {
        return "System Title Mismatch";
    }

    if (msg.includes("TCP")) {
        return "TCP Connection Failed";
    }

    if (
        msg.includes("NO DATA") ||
        msg.includes("DATA NOT FOUND") ||
        msg.includes("DATA NOT AVAILABLE")
    ) {
        return "No Data Found in HES";
    }

    // Actual error:
    // Smart Meter date is older then FormY
    // SAP_MRO_DOWNLOAD_DATE for the meter no
    if (
        msg.includes("DATE IS OLDER THEN FORMY") ||
        msg.includes("DATE IS OLDER THAN FORMY") ||
        msg.includes("DATE IS OLDER") ||
        (
            msg.includes("SMART METER") &&
            msg.includes("OLDER") &&
            msg.includes("FORMY") &&
            msg.includes("SAP_MRO_DOWNLOAD_DATE")
        )
    ) {
        return "Date Older Then FormY";
    }

    if (
        msg.includes("TIMEOUT") ||
        msg.includes("TIME OUT")
    ) {
        return "Timeout";
    }

    return "Other";
}


// =====================================================
// BUILD BYPL COUNTS FROM DETAILED SUMMARY
// =====================================================

function buildBYPLCountsFromDetail(data) {

    const grouped = {};

    (data || []).forEach(item => {

        const reason = getBYPLFailureCategory(
            item.schedulerMessage
        );

        grouped[reason] =
            (grouped[reason] || 0) + 1;
    });


    return Object.entries(grouped)
        .map(([reason, count]) => ({
            reason: reason,
            count: count
        }))
        .filter(item => item.count > 0);
}


// =====================================================
// NORMALIZE BRPL API RESPONSE
// =====================================================

function normalizeFailureData(data) {

    return (data || [])
        .map(item => ({

            reason:
                item.failureReason ??
                item.feilureReason ??
                item.FAILURE_REASON ??
                item.reason ??
                "Unknown Failure",

            count: Number(
                item.count ??
                item.totalCount ??
                item.TOTAL_COUNT ??
                item.COUNT ??
                0
            )

        }))
        .filter(item => item.count > 0);
}


// =====================================================
// FAILURE REASON COLORS
// =====================================================

function getFailureColor(reason) {

    const msg = String(reason || "")
        .toUpperCase()
        .replace(/\s+/g, " ")
        .trim();

    if (msg.includes("SYSTEM TITLE")) {
        return "#dc3545"; // Red
    }

    if (msg.includes("TCP")) {
        return "#fd7e14"; // Orange
    }

    if (
        msg.includes("NO DATA") ||
        msg.includes("DATA NOT FOUND")
    ) {
        return "#ffc107"; // Yellow
    }

    if (
        msg.includes("DATE OLDER") ||
        msg.includes("DATE IS OLDER")
    ) {
        return "#20C997"; // Teal
    }

    return "#6c757d"; // Gray - Timeout / Other
}


// =====================================================
// LOAD FAILURE REASON CHART
// =====================================================

function loadFailureReasonChart(readingMonth) {

    const month = readingMonth || getReadingMonth();


    $.when(

        // BRPL API
        $.ajax({
            url: "/api/dashboardApi/failure-reason-count",
            type: "GET",
            data: {
                ReadingMonth: month
            }
        }),

        // BYPL detailed summary API
        $.ajax({
            url: "/api/dashboardapi/meter-download-detailed-summary-bypl",
            type: "GET",
            data: {
                readingMonth: month
            }
        })

    )

        .done(function (brplResponse, byplResponse) {


            const brplData = brplResponse[0] || [];
            const byplDetailData = byplResponse[0] || [];


            // Normalize BRPL
            const brpl =
                normalizeFailureData(brplData);


            // Build BYPL from exact detailed table data
            const bypl =
                buildBYPLCountsFromDetail(
                    Array.isArray(byplDetailData)
                        ? byplDetailData
                        : []
                );


            console.log("BRPL Failure Data:", brpl);
            console.log("BYPL Detail Rows:", byplDetailData.length);
            console.log("BYPL Chart Counts:", bypl);


            // Sort each company by count
            brpl.sort((a, b) => b.count - a.count);
            bypl.sort((a, b) => b.count - a.count);


            // =====================================================
            // COMBINE DATA
            // Y-AXIS SHOWS ONLY COMPANY NAME
            // =====================================================

            const chartData = [

                ...brpl.map(item => ({

                    company: "BRPL",
                    reason: item.reason,
                    count: item.count,
                    label: "BRPL",

                    color:
                        getFailureColor(item.reason)

                })),

                ...bypl.map(item => ({

                    company: "BYPL",
                    reason: item.reason,
                    count: item.count,
                    label: "BYPL",

                    color:
                        getFailureColor(item.reason)

                }))

            ];


            // Destroy previous chart
            if (readingTrendChart) {

                readingTrendChart.destroy();
                readingTrendChart = null;
            }


            const chartCanvas =
                document.getElementById("readingTrendChart");


            if (!chartCanvas) {

                console.error(
                    "Canvas #readingTrendChart not found."
                );

                return;
            }


            // Empty data
            if (chartData.length === 0) {

                const ctx =
                    chartCanvas.getContext("2d");

                ctx.clearRect(
                    0,
                    0,
                    chartCanvas.width,
                    chartCanvas.height
                );

                return;
            }


            // =====================================================
            // CREATE CHART
            // =====================================================

            readingTrendChart = new Chart(
                chartCanvas,
                {

                    type: "bar",

                    data: {

                        labels:
                            chartData.map(
                                item => item.label
                            ),

                        datasets: [
                            {

                                label: "Failure Count",

                                data:
                                    chartData.map(
                                        item => item.count
                                    ),

                                backgroundColor:
                                    chartData.map(
                                        item => item.color
                                    ),

                                borderRadius: 6,

                                borderSkipped: false,

                                barThickness: 26,

                                maxBarThickness: 30

                            }
                        ]

                    },


                    options: {

                        indexAxis: "y",

                        responsive: true,

                        maintainAspectRatio: false,


                        interaction: {

                            mode: "nearest",

                            axis: "y",

                            intersect: false

                        },


                        plugins: {

                            // =========================================
                            // ONLY FAILURE REASON LEGEND
                            // NO BRPL / BYPL FILTER
                            // =========================================

                            legend: {

                                display: true,

                                position: "top",

                                align: "center",

                                labels: {

                                    usePointStyle: true,

                                    pointStyle: "circle",

                                    padding: 16,

                                    font: {
                                        size: 11,
                                        weight: "600"
                                    },


                                    generateLabels: function () {

                                        return [

                                            {
                                                text: "System Title Mismatch",
                                                fillStyle: "#dc3545",
                                                strokeStyle: "#dc3545",
                                                pointStyle: "circle"
                                            },

                                            {
                                                text: "TCP Connection Failed",
                                                fillStyle: "#fd7e14",
                                                strokeStyle: "#fd7e14",
                                                pointStyle: "circle"
                                            },

                                            {
                                                text: "No Data Found in HES",
                                                fillStyle: "#ffc107",
                                                strokeStyle: "#ffc107",
                                                pointStyle: "circle"
                                            },

                                            {
                                                text: "Date Older Then FormY",
                                                fillStyle: "#20C997",
                                                strokeStyle: "#20C997",
                                                pointStyle: "circle"
                                            },

                                            {
                                                text: "Timeout / Other",
                                                fillStyle: "#6c757d",
                                                strokeStyle: "#6c757d",
                                                pointStyle: "circle"
                                            }

                                        ];

                                    }

                                }

                            },


                            title: {

                                display: true,

                                text:
                                    "HES Download Failure Analysis",

                                align: "start",

                                color: "#4B5563",

                                font: {
                                    size: 17,
                                    weight: "700"
                                },

                                padding: {
                                    bottom: 4
                                }

                            },


                            subtitle: {

                                display: true,

                                text:
                                    `Failure reasons for ${month}`,

                                align: "start",

                                color: "#6B7280",

                                font: {
                                    size: 12,
                                    weight: "400"
                                },

                                padding: {
                                    bottom: 18
                                }

                            },


                            tooltip: {

                                enabled: true,

                                backgroundColor: "#111827",

                                titleColor: "#FFFFFF",

                                bodyColor: "#FFFFFF",

                                padding: 12,

                                cornerRadius: 8,

                                displayColors: true,


                                callbacks: {

                                    title: function (context) {

                                        const index =
                                            context[0].dataIndex;

                                        const item =
                                            chartData[index];

                                        if (!item) {
                                            return "";
                                        }

                                        return (
                                            item.company +
                                            " • " +
                                            item.reason
                                        );

                                    },


                                    label: function (context) {

                                        return (
                                            "Failed meters: " +
                                            Number(
                                                context.raw
                                            ).toLocaleString()
                                        );

                                    }

                                }

                            }

                        },


                        scales: {

                            x: {

                                beginAtZero: true,

                                ticks: {
                                    precision: 0
                                },

                                grid: {
                                    color:
                                        "rgba(0,0,0,0.06)"
                                }

                            },


                            y: {

                                grid: {
                                    display: false
                                },

                                ticks: {

                                    font: {
                                        size: 12,
                                        weight: "600"
                                    }

                                }

                            }

                        }

                    }

                }
            );

        })

        .fail(function (brplError, byplError) {

            console.error(
                "Failure reason chart load error."
            );

            console.error(
                "BRPL Error:",
                brplError
            );

            console.error(
                "BYPL Error:",
                byplError
            );

        });
}