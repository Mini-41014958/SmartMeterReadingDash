let readingTrendChart = null;

function getBYPLFailureCategory(message) {

    const msg = String(message || "")
        .toUpperCase()
        .replace(/\s+/g, " ")
        .trim();


    if (msg.includes("SYSTEM TITLE")) {
        return "System Title";
    }


    if (msg.includes("TCP")) {
        return "TCP Connection";
    }


    if (
        msg.includes("NO DATA") ||
        msg.includes("DATA NOT FOUND") ||
        msg.includes("DATA NOT AVAILABLE")
    ) {
        return "No Data Found";
    }


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
        return "Date Older Than FormY";
    }


    if (
        msg.includes("TIMEOUT") ||
        msg.includes("TIME OUT")
    ) {
        return "Timeout";
    }


    return "Other";
}

function buildBYPLCountsFromDetail(data) {

    const grouped = {};


    (data || []).forEach(item => {

        const reason =
            getBYPLFailureCategory(
                item.schedulerMessage
            );


        grouped[reason] =
            (grouped[reason] || 0) + 1;

    });


    return Object.entries(grouped)
        .map(([reason, count]) => ({

            reason: reason,

            count: Number(count)

        }))
        .filter(item => item.count > 0);

}

function normalizeFailureData(data) {

    return (data || [])
        .map(item => ({

            reason:
                item.failureReason ??
                item.feilureReason ??
                item.FAILURE_REASON ??
                item.reason ??
                "Other",


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

function normalizeReasonName(reason) {

    const msg = String(reason || "")
        .toUpperCase()
        .replace(/\s+/g, " ")
        .trim();


    if (msg.includes("SYSTEM TITLE")) {
        return "System Title";
    }


    if (msg.includes("TCP")) {
        return "TCP Connection ";
    }


    if (
        msg.includes("NO DATA") ||
        msg.includes("DATA NOT FOUND") ||
        msg.includes("DATA NOT AVAILABLE")
    ) {
        return "No Data Found";
    }


    if (
        msg.includes("DATE OLDER") ||
        msg.includes("DATE IS OLDER") ||
        msg.includes("FORMY")
    ) {
        return "Date Older Than FormY";
    }


    if (
        msg.includes("TIMEOUT") ||
        msg.includes("TIME OUT")
    ) {
        return "Timeout";
    }


    if (
        msg.includes("OTHER") ||
        msg.includes("UNKNOWN")
    ) {
        return "Other";
    }


    return String(reason || "Other").trim();

}

function mergeFailureReasons(data) {

    const grouped = {};


    (data || []).forEach(item => {

        const reason =
            normalizeReasonName(item.reason);


        const count =
            Number(item.count || 0);


        grouped[reason] =
            (grouped[reason] || 0) + count;

    });


    return Object.entries(grouped)
        .map(([reason, count]) => ({

            reason: reason,

            count: Number(count)

        }))
        .filter(item => item.count > 0);

}

function getFailureColor(reason) {

    const msg = String(reason || "")
        .toUpperCase()
        .replace(/\s+/g, " ")
        .trim();


    if (msg.includes("SYSTEM TITLE")) {
        return "#dc3545";
    }


    if (msg.includes("TCP")) {
        return "#fd7e14";
    }


    if (
        msg.includes("NO DATA") ||
        msg.includes("DATA NOT FOUND")
    ) {
        return "#ffc107";
    }


    if (
        msg.includes("DATE OLDER") ||
        msg.includes("FORMY")
    ) {
        return "#20c997";
    }


    if (
        msg.includes("TIMEOUT") ||
        msg.includes("TIME OUT")
    ) {
        return "#6f42c1";
    }


    return "#6c757d";

}


// =====================================================
// GET FAILURE COUNT
// =====================================================

function getFailureCount(data, reason) {

    const item = (data || []).find(
        x => x.reason === reason
    );


    return Number(item?.count || 0);

}


// =====================================================
// GET TOTAL FAILURE COUNT
// =====================================================

function getTotalFailureCount(data) {

    return (data || []).reduce(
        (sum, item) =>
            sum + Number(item.count || 0),
        0
    );

}


// =====================================================
// FORCE TOOLTIP HOVER BY COMPANY ROW
//
// This is the important fix.
//
// It detects whether mouse is over BRPL or BYPL row
// and manually activates all stacked datasets for that row.
// =====================================================

const rowHoverPlugin = {

    id: "rowHoverPlugin",


    afterEvent(chart, args) {

        const event = args.event;


        if (!event) {
            return;
        }


        const chartArea =
            chart.chartArea;


        const yScale =
            chart.scales.y;


        if (
            !chartArea ||
            !yScale
        ) {
            return;
        }


        // =============================================
        // CLEAR TOOLTIP ON MOUSE OUT
        // =============================================

        if (
            event.type === "mouseout"
        ) {

            chart.setActiveElements([]);


            if (chart.tooltip) {

                chart.tooltip.setActiveElements(
                    [],
                    {
                        x: 0,
                        y: 0
                    }
                );

            }


            chart.canvas.style.cursor =
                "default";


            args.changed = true;

            return;

        }


        // =============================================
        // ONLY WORK INSIDE THE ACTUAL CHART AREA
        // =============================================

        if (
            event.x < chartArea.left ||
            event.x > chartArea.right ||
            event.y < chartArea.top ||
            event.y > chartArea.bottom
        ) {

            chart.setActiveElements([]);


            if (chart.tooltip) {

                chart.tooltip.setActiveElements(
                    [],
                    {
                        x: event.x,
                        y: event.y
                    }
                );

            }


            chart.canvas.style.cursor =
                "default";


            args.changed = true;

            return;

        }


        // =============================================
        // FIND NEAREST COMPANY ROW
        // =============================================

        const brplY =
            yScale.getPixelForValue(0);


        const byplY =
            yScale.getPixelForValue(1);


        const distanceToBrpl =
            Math.abs(event.y - brplY);


        const distanceToBypl =
            Math.abs(event.y - byplY);


        let companyIndex = null;


        if (
            distanceToBrpl <
            distanceToBypl
        ) {

            companyIndex = 0;

        } else {

            companyIndex = 1;

        }


        // =============================================
        // GET ROW SPACING
        // =============================================

        const rowDistance =
            Math.abs(byplY - brplY);


        // Large hover area.
        // Mouse can move around the entire row.
        const hoverRange =
            Math.max(
                rowDistance / 2,
                70
            );


        const nearestDistance =
            Math.min(
                distanceToBrpl,
                distanceToBypl
            );


        // =============================================
        // NOT CLOSE TO ANY ROW
        // =============================================

        if (
            nearestDistance > hoverRange
        ) {

            chart.setActiveElements([]);


            if (chart.tooltip) {

                chart.tooltip.setActiveElements(
                    [],
                    {
                        x: event.x,
                        y: event.y
                    }
                );

            }


            chart.canvas.style.cursor =
                "default";


            args.changed = true;

            return;

        }


        // =============================================
        // BUILD ACTIVE ELEMENTS
        //
        // Activate every visible stacked segment
        // for BRPL or BYPL.
        // =============================================

        const activeElements =
            chart.data.datasets
                .map(
                    (dataset, datasetIndex) => {

                        const value =
                            Number(
                                dataset.data[
                                companyIndex
                                ] || 0
                            );


                        if (value <= 0) {
                            return null;
                        }


                        return {

                            datasetIndex:
                                datasetIndex,

                            index:
                                companyIndex

                        };

                    }
                )
                .filter(Boolean);


        // =============================================
        // MANUALLY ACTIVATE THE ROW
        // =============================================

        chart.setActiveElements(
            activeElements
        );


        // =============================================
        // MANUALLY POSITION TOOLTIP
        // =============================================

        if (chart.tooltip) {

            chart.tooltip.setActiveElements(
                activeElements,
                {
                    x: event.x,
                    y: event.y
                }
            );

        }


        // =============================================
        // POINTER CURSOR
        // =============================================

        chart.canvas.style.cursor =
            activeElements.length > 0
                ? "pointer"
                : "default";


        // =============================================
        // FORCE CHART REDRAW
        // =============================================

        args.changed = true;

    }

};


// =====================================================
// LOAD FAILURE REASON CHART
// =====================================================

function loadFailureReasonChart(readingMonth) {

    const month =
        readingMonth || getReadingMonth();


    // =================================================
    // LOAD BRPL + BYPL APIs
    // =================================================

    $.when(

        // BRPL API
        $.ajax({

            url:
                "/api/dashboardApi/failure-reason-count",

            type: "GET",

            data: {

                ReadingMonth: month

            }

        }),


        // BYPL API
        $.ajax({

            url:
                "/api/dashboardapi/meter-download-detailed-summary-bypl",

            type: "GET",

            data: {

                readingMonth: month

            }

        })

    )

        .done(function (
            brplResponse,
            byplResponse
        ) {


            // =============================================
            // GET API DATA
            // =============================================

            const brplData =
                brplResponse[0] || [];


            const byplDetailData =
                byplResponse[0] || [];


            // =============================================
            // PROCESS BRPL DATA
            // =============================================

            const normalizedBrpl =
                normalizeFailureData(
                    brplData
                );


            const brpl =
                mergeFailureReasons(
                    normalizedBrpl
                );


            // =============================================
            // PROCESS BYPL DATA
            // =============================================

            const rawBypl =
                buildBYPLCountsFromDetail(
                    Array.isArray(
                        byplDetailData
                    )
                        ? byplDetailData
                        : []
                );


            const bypl =
                mergeFailureReasons(
                    rawBypl
                );


            console.log(
                "BRPL Failure Data:",
                brpl
            );


            console.log(
                "BYPL Failure Data:",
                bypl
            );


            // =============================================
            // GET ALL UNIQUE FAILURE REASONS
            // =============================================

            const failureReasons = [

                ...new Set([

                    ...brpl.map(
                        item => item.reason
                    ),

                    ...bypl.map(
                        item => item.reason
                    )

                ])

            ];


            // =============================================
            // SORT BY TOTAL COUNT
            // =============================================

            failureReasons.sort(
                (a, b) => {

                    const aTotal =
                        getFailureCount(
                            brpl,
                            a
                        ) +
                        getFailureCount(
                            bypl,
                            a
                        );


                    const bTotal =
                        getFailureCount(
                            brpl,
                            b
                        ) +
                        getFailureCount(
                            bypl,
                            b
                        );


                    return bTotal - aTotal;

                }
            );


            // =============================================
            // DESTROY OLD CHART
            // =============================================

            if (readingTrendChart) {

                readingTrendChart.destroy();

                readingTrendChart = null;

            }


            // =============================================
            // GET CANVAS
            // =============================================

            const chartCanvas =
                document.getElementById(
                    "readingTrendChart"
                );


            if (!chartCanvas) {

                console.error(
                    "Canvas #readingTrendChart not found."
                );

                return;

            }


            // =============================================
            // HANDLE EMPTY DATA
            // =============================================

            if (
                failureReasons.length === 0
            ) {

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


            // =============================================
            // CREATE STACKED DATASETS
            // =============================================

            const datasets =
                failureReasons.map(
                    reason => ({

                        label: reason,


                        data: [

                            getFailureCount(
                                brpl,
                                reason
                            ),

                            getFailureCount(
                                bypl,
                                reason
                            )

                        ],


                        backgroundColor:
                            getFailureColor(
                                reason
                            ),


                        borderColor:
                            "#ffffff",


                        borderWidth: 2,


                        borderRadius: 2,


                        borderSkipped: false,


                        barThickness: 52,


                        maxBarThickness: 58

                    })
                );


            // =============================================
            // CREATE CHART
            // =============================================

            readingTrendChart =
                new Chart(
                    chartCanvas,
                    {

                        type: "bar",


                        data: {

                            labels: [

                                "BRPL",

                                "BYPL"

                            ],


                            datasets:
                                datasets

                        },


                        plugins: [

                            rowHoverPlugin

                        ],


                        options: {


                            // =====================================
                            // HORIZONTAL BAR
                            // =====================================

                            indexAxis: "y",


                            responsive: true,


                            maintainAspectRatio: false,


                            animation: {

                                duration: 500

                            },


                            // =====================================
                            // ENABLE ALL REQUIRED EVENTS
                            // =====================================

                            events: [

                                "mousemove",

                                "mouseout",

                                "touchstart",

                                "touchmove"

                            ],


                            // =====================================
                            // DEFAULT INTERACTION
                            //
                            // Plugin above handles hover.
                            // =====================================

                            interaction: {

                                mode: "index",

                                intersect: false

                            },


                            plugins: {


                                // =================================
                                // LEGEND
                                // =================================

                                legend: {

                                    display: true,


                                    position: "top",


                                    align: "start",


                                    labels: {

                                        usePointStyle: true,


                                        pointStyle:
                                            "circle",


                                        padding: 20,


                                        boxWidth: 13,


                                        boxHeight: 13,


                                        font: {

                                            size: 13,

                                            weight: "700"

                                        },


                                        color:
                                            "#4b5563"

                                    }

                                },


                                // =================================
                                // TITLE
                                // =================================

                                title: {

                                    display: true,


                                    text:
                                        "HES Download Failure Comparison",


                                    align:
                                        "start",


                                    color:
                                        "#374151",


                                    font: {

                                        size: 20,

                                        weight: "700"

                                    },


                                    padding: {

                                        top: 5,

                                        bottom: 6

                                    }

                                },


                                // =================================
                                // SUBTITLE
                                // =================================

                                subtitle: {

                                    display: true,


                                    text:
                                        `BRPL vs BYPL • ${month}`,


                                    align:
                                        "start",


                                    color:
                                        "#6b7280",


                                    font: {

                                        size: 14,

                                        weight: "600"

                                    },


                                    padding: {

                                        bottom: 18

                                    }

                                },


                                // =================================
                                // TOOLTIP
                                // SMALL + READABLE
                                // =================================

                                tooltip: {

                                    enabled: true,


                                    backgroundColor:
                                        "#111827",


                                    titleColor:
                                        "#ffffff",


                                    bodyColor:
                                        "#ffffff",


                                    footerColor:
                                        "#d1d5db",


                                    padding: 10,


                                    cornerRadius: 8,


                                    displayColors: true,


                                    boxWidth: 11,


                                    boxHeight: 11,


                                    boxPadding: 6,


                                    caretSize: 5,


                                    titleFont: {

                                        size: 14,

                                        weight: "700"

                                    },


                                    bodyFont: {

                                        size: 13,

                                        weight: "600"

                                    },


                                    footerFont: {

                                        size: 13,

                                        weight: "700"

                                    },


                                    titleSpacing: 5,


                                    titleMarginBottom: 6,


                                    bodySpacing: 4,


                                    footerMarginTop: 7,


                                    footerSpacing: 4,


                                    callbacks: {


                                        // =========================
                                        // TITLE
                                        // =========================

                                        title: function (
                                            context
                                        ) {

                                            if (
                                                !context ||
                                                context.length === 0
                                            ) {

                                                return "";

                                            }


                                            return (
                                                context[0].label +
                                                " Breakdown"
                                            );

                                        },


                                        // =========================
                                        // LABEL
                                        // =========================

                                        label: function (
                                            context
                                        ) {

                                            const value =
                                                Number(
                                                    context.raw || 0
                                                );


                                            // Hide zero values
                                            if (value <= 0) {

                                                return null;

                                            }


                                            return (
                                                context.dataset.label +
                                                ": " +
                                                value.toLocaleString()
                                            );

                                        },


                                        // =========================
                                        // TOTAL
                                        // =========================

                                        footer: function (
                                            context
                                        ) {

                                            const total =
                                                context.reduce(
                                                    function (
                                                        sum,
                                                        item
                                                    ) {

                                                        return (
                                                            sum +
                                                            Number(
                                                                item.raw || 0
                                                            )
                                                        );

                                                    },
                                                    0
                                                );


                                            return (
                                                "Total: " +
                                                total.toLocaleString()
                                            );

                                        }

                                    }

                                }

                            },


                            // =====================================
                            // AXES
                            // =====================================

                            scales: {


                                // =================================
                                // X AXIS
                                // =================================

                                x: {

                                    stacked: true,


                                    beginAtZero: true,


                                    title: {

                                        display: true,


                                        text:
                                            "Number of Failed Meters",


                                        color:
                                            "#4b5563",


                                        font: {

                                            size: 14,

                                            weight: "700"

                                        },


                                        padding: {

                                            top: 12

                                        }

                                    },


                                    ticks: {

                                        precision: 0,


                                        color:
                                            "#4b5563",


                                        font: {

                                            size: 13,

                                            weight: "600"

                                        },


                                        padding: 8

                                    },


                                    grid: {

                                        color:
                                            "rgba(0,0,0,0.08)",


                                        drawBorder: false

                                    }

                                },

                                y: {

                                    stacked: true,


                                    grid: {

                                        display: false

                                    },


                                    ticks: {

                                        color:
                                            "#374151",


                                        font: {

                                            size: 16,

                                            weight: "700"

                                        },


                                        padding: 12

                                    }

                                }

                            }

                        }

                    }
                );

        })


        .fail(function (
            brplError,
            byplError
        ) {

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