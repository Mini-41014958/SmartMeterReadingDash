

let readingTrendChart = null;

function getReadingTrendApiUrl(endpoint) {

    const path =
        window.location.pathname.toLowerCase();

    const basePath =
        path === "/smartmeter" ||
            path.startsWith("/smartmeter/")
            ? "/SmartMeter"
            : "";

    return `${basePath}/api/${endpoint}`;
}


function normalizeFailureAccessValue(value) {

    return String(value ?? "")
        .trim()
        .toUpperCase();

}


async function getCurrentUserAccessForFailureChart() {

    // ---------------------------------------------------------
    // Dashboard.js global
    // ---------------------------------------------------------

    if (
        typeof currentUserAccess !== "undefined" &&
        currentUserAccess
    ) {

        return currentUserAccess;

    }


    // ---------------------------------------------------------
    // Window fallback
    // ---------------------------------------------------------

    if (
        window.currentUserAccess
    ) {

        return window.currentUserAccess;

    }


    // ---------------------------------------------------------
    // Server fallback
    // ---------------------------------------------------------

    const response =
        await fetch(
            getReadingTrendApiUrl(
                "AuthApi/my-access"
            ),
            {
                method: "GET",

                credentials:
                    "same-origin",

                cache:
                    "no-store",

                headers: {
                    "Accept":
                        "application/json"
                }
            }
        );


    if (!response.ok) {

        throw new Error(
            `Failed to load user access. HTTP ${response.status}`
        );

    }


    const access =
        await response.json();


    if (!access) {

        throw new Error(
            "User access information is empty."
        );

    }


    return access;

}


// =============================================================
// COMPANY ACCESS
// =============================================================

function canAccessFailureCompany(
    access,
    company
) {

    if (!access) {

        return false;

    }


    const role =
        normalizeFailureAccessValue(
            access.role ??
            access.Role
        );


    const userCompany =
        normalizeFailureAccessValue(
            access.company ??
            access.Company
        );


    const requestedCompany =
        normalizeFailureAccessValue(
            company
        );


    // ---------------------------------------------------------
    // SUPERADMIN
    // ---------------------------------------------------------

    if (
        role === "SUPERADMIN" ||
        access.isSuperAdmin === true ||
        access.IsSuperAdmin === true
    ) {

        return true;

    }


    // ---------------------------------------------------------
    // COMPANY USER
    // ---------------------------------------------------------

    return (
        userCompany ===
        requestedCompany
    );

}


// =============================================================
// EXTRACT ARRAY FROM API RESPONSE
//
// Supports:
//
// [
//    {...}
// ]
//
// OR
//
// {
//    data: [...]
// }
//
// OR
//
// {
//    result: [...]
// }
//
// OR
//
// {
//    items: [...]
// }
//
// OR
//
// {
//    records: [...]
// }
// =============================================================

function extractFailureArray(response) {

    // ---------------------------------------------------------
    // Direct array
    // ---------------------------------------------------------

    if (
        Array.isArray(response)
    ) {

        return response;

    }


    // ---------------------------------------------------------
    // Empty response
    // ---------------------------------------------------------

    if (
        !response ||
        typeof response !== "object"
    ) {

        return [];

    }


    // ---------------------------------------------------------
    // Common wrapper names
    // ---------------------------------------------------------

    const candidates = [

        response.data,

        response.Data,

        response.result,

        response.Result,

        response.items,

        response.Items,

        response.records,

        response.Records,

        response.rows,

        response.Rows,

        response.response,

        response.Response

    ];


    for (
        const candidate of candidates
    ) {

        if (
            Array.isArray(candidate)
        ) {

            return candidate;

        }

    }


    // ---------------------------------------------------------
    // Some APIs return:
    //
    // {
    //     data: {
    //         items: [...]
    //     }
    // }
    // ---------------------------------------------------------

    for (
        const candidate of candidates
    ) {

        if (
            candidate &&
            typeof candidate === "object"
        ) {

            const nested =
                extractFailureArray(
                    candidate
                );


            if (
                nested.length > 0
            ) {

                return nested;

            }

        }

    }


    return [];

}


// =============================================================
// GET RAW FAILURE REASON
// =============================================================

function getRawFailureReason(item) {

    if (!item) {

        return "";

    }


    return (
        item.failureReason ??
        item.feilureReason ??
        item.FAILURE_REASON ??
        item.FeilureReason ??
        item.failure_reason ??
        item.FailureReason ??
        item.reason ??
        item.REASON ??
        item.Reason ??
        item.failure ??
        item.FAILURE ??
        item.Failure ??
        item.schedulerMessage ??
        item.SCHEDULER_MESSAGE ??
        item.scheduler_message ??
        item.description ??
        item.DESCRIPTION ??
        item.Description ??
        item.message ??
        item.MESSAGE ??
        item.Message ??
        item.errorMessage ??
        item.ERROR_MESSAGE ??
        item.error ??
        item.ERROR ??
        ""
    );

}


// =============================================================
// GET FAILURE COUNT
// =============================================================

function getRawFailureCount(item) {

    if (!item) {

        return 0;

    }


    const value =
        item.count ??
        item.COUNT ??
        item.failureCount ??
        item.FAILURE_COUNT ??
        item.failure_count ??
        item.FailureCount ??
        item.totalCount ??
        item.TOTAL_COUNT ??
        item.total ??
        item.TOTAL ??
        item.failed ??
        item.FAILED ??
        0;


    return Number(value) || 0;

}


// =============================================================
// NORMALIZE FAILURE CATEGORY
//
// Previous working categories/colors:
//
// System Title
// TCP Connection
// No Data Found
// Date Older Than FormY
// Timeout
// Other
// =============================================================

function normalizeFailureCategory(reason) {

    const msg =
        String(reason || "")
            .toUpperCase()
            .replace(/\s+/g, " ")
            .trim();


    // ---------------------------------------------------------
    // SYSTEM TITLE
    // ---------------------------------------------------------

    if (
        msg.includes("SYSTEM TITLE")
    ) {

        return "System Title";

    }


    // ---------------------------------------------------------
    // TCP
    // ---------------------------------------------------------

    if (
        msg.includes("TCP")
    ) {

        return "TCP Connection";

    }


    // ---------------------------------------------------------
    // NO DATA
    // ---------------------------------------------------------

    if (
        msg.includes("NO DATA") ||
        msg.includes("DATA NOT FOUND") ||
        msg.includes("DATA NOT AVAILABLE")
    ) {

        return "No Data Found";

    }


    // ---------------------------------------------------------
    // DATE OLDER THAN FORMY
    // ---------------------------------------------------------

    if (
        msg.includes("DATE IS OLDER THEN FORMY") ||
        msg.includes("DATE IS OLDER THAN FORMY") ||
        msg.includes("DATE IS OLDER") ||
        msg.includes("DATE OLDER") ||
        (
            msg.includes("SMART METER") &&
            msg.includes("OLDER") &&
            msg.includes("FORMY")
        ) ||
        msg.includes("SAP_MRO_DOWNLOAD_DATE")
    ) {

        return "Date Older Than FormY";

    }


    // ---------------------------------------------------------
    // TIMEOUT
    // ---------------------------------------------------------

    if (
        msg.includes("TIMEOUT") ||
        msg.includes("TIME OUT")
    ) {

        return "Timeout";

    }


    // ---------------------------------------------------------
    // UNKNOWN / OTHER
    // ---------------------------------------------------------

    if (
        !msg ||
        msg === "UNKNOWN" ||
        msg === "N/A" ||
        msg === "NA" ||
        msg === "-"
    ) {

        return "Other";

    }


    return String(reason).trim();

}


// =============================================================
// NORMALIZE BRPL FAILURE DATA
// =============================================================

function normalizeBRPLFailureData(response) {

    const data =
        extractFailureArray(
            response
        );


    console.log(
        "BRPL Failure Raw Records:",
        data.length,
        data
    );


    const grouped = {};


    data.forEach(
        function (item) {

            const rawReason =
                getRawFailureReason(
                    item
                );


            const count =
                getRawFailureCount(
                    item
                );


            if (
                count <= 0
            ) {

                return;

            }


            const reason =
                normalizeFailureCategory(
                    rawReason
                );


            grouped[reason] =
                (
                    grouped[reason] ||
                    0
                ) + count;

        }
    );


    return Object.entries(
        grouped
    )
        .map(
            function (
                [reason, count]
            ) {

                return {

                    reason:
                        reason,

                    count:
                        Number(count)

                };

            }
        )
        .filter(
            function (item) {

                return (
                    item.count > 0
                );

            }
        );

}


// =============================================================
// BUILD BYPL FAILURE DATA
// =============================================================

function buildBYPLFailureCounts(response) {

    const data =
        extractFailureArray(
            response
        );


    console.log(
        "BYPL Failure Detail Records:",
        data.length,
        data
    );


    const grouped = {};


    data.forEach(
        function (item) {

            const rawReason =
                getRawFailureReason(
                    item
                );


            const text =
                String(
                    rawReason || ""
                )
                    .trim();


            if (!text) {

                return;

            }


            const upper =
                text.toUpperCase();


            // -------------------------------------------------
            // Successful records
            // -------------------------------------------------

            if (
                upper === "SUCCESS" ||
                upper === "SUCCESSFUL" ||
                upper === "DOWNLOADED"
            ) {

                return;

            }


            if (
                upper === "OK"
            ) {

                return;

            }


            // -------------------------------------------------
            // Normalize category
            // -------------------------------------------------

            const reason =
                normalizeFailureCategory(
                    text
                );


            // -------------------------------------------------
            // Detailed API = one meter per record
            // -------------------------------------------------

            const count =
                getRawFailureCount(
                    item
                );


            const finalCount =
                count > 0
                    ? count
                    : 1;


            grouped[reason] =
                (
                    grouped[reason] ||
                    0
                ) +
                finalCount;

        }
    );


    return Object.entries(
        grouped
    )
        .map(
            function (
                [reason, count]
            ) {

                return {

                    reason:
                        reason,

                    count:
                        Number(count)

                };

            }
        )
        .filter(
            function (item) {

                return (
                    item.count > 0
                );

            }
        );

}


// =============================================================
// MERGE FAILURE REASONS
// =============================================================

function mergeFailureReasons(
    data
) {

    if (
        !Array.isArray(data)
    ) {

        return [];

    }


    const grouped = {};


    data.forEach(
        function (item) {

            if (!item) {

                return;

            }


            const reason =
                normalizeFailureCategory(
                    item.reason ??
                    item.failureReason ??
                    item.feilureReason ??
                    "Other"
                );


            const count =
                Number(
                    item.count ??
                    item.failureCount ??
                    0
                ) || 0;


            if (
                count <= 0
            ) {

                return;

            }


            grouped[reason] =
                (
                    grouped[reason] ||
                    0
                ) + count;

        }
    );


    return Object.entries(
        grouped
    )
        .map(
            function (
                [reason, count]
            ) {

                return {

                    reason:
                        reason,

                    count:
                        Number(count)

                };

            }
        )
        .filter(
            function (item) {

                return item.count > 0;

            }
        );

}


// =============================================================
// GET FAILURE COUNT FOR CATEGORY
// =============================================================

function getFailureCount(
    data,
    reason
) {

    if (
        !Array.isArray(data)
    ) {

        return 0;

    }


    const target =
        normalizeFailureCategory(
            reason
        )
            .toUpperCase();


    const item =
        data.find(
            function (x) {

                return (
                    normalizeFailureCategory(
                        x.reason
                    )
                        .toUpperCase() ===
                    target
                );

            }
        );


    return item
        ? Number(item.count) || 0
        : 0;

}


// =============================================================
// PREVIOUS WORKING COLORS
// =============================================================

function getFailureColor(
    reason
) {

    const key =
        normalizeFailureCategory(
            reason
        );


    switch (
    key.toUpperCase()
    ) {

        // -----------------------------------------------------
        // RED
        // -----------------------------------------------------

        case "SYSTEM TITLE":

            return "#dc3545";


        // -----------------------------------------------------
        // ORANGE
        // -----------------------------------------------------

        case "TCP CONNECTION":

            return "#fd7e14";


        // -----------------------------------------------------
        // YELLOW
        // -----------------------------------------------------

        case "NO DATA FOUND":

            return "#ffc107";


        // -----------------------------------------------------
        // TEAL
        // -----------------------------------------------------

        case "DATE OLDER THAN FORMY":

            return "#20c997";


        // -----------------------------------------------------
        // PURPLE
        // -----------------------------------------------------

        case "TIMEOUT":

            return "#6f42c1";


        // -----------------------------------------------------
        // GRAY
        // -----------------------------------------------------

        default:

            return "#6c757d";

    }

}


// =============================================================
// ROW HOVER PLUGIN
// =============================================================

const rowHoverPlugin = {

    id:
        "rowHoverPlugin",


    afterEvent(
        chart,
        args
    ) {

        const event =
            args.event;


        if (!event) {

            return;

        }


        if (
            event.type ===
            "mouseout"
        ) {

            chart.setActiveElements(
                []
            );


            if (
                chart.tooltip
            ) {

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


            args.changed =
                true;


            return;

        }


        if (
            event.type !==
            "mousemove" &&
            event.type !==
            "touchstart" &&
            event.type !==
            "touchmove"
        ) {

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


        if (
            event.x < chartArea.left ||
            event.x > chartArea.right ||
            event.y < chartArea.top ||
            event.y > chartArea.bottom
        ) {

            chart.setActiveElements(
                []
            );


            chart.canvas.style.cursor =
                "default";


            args.changed =
                true;


            return;

        }


        const rowCount =
            chart.data.labels.length;


        let companyIndex =
            -1;


        let nearestDistance =
            Infinity;


        for (
            let i = 0;
            i < rowCount;
            i++
        ) {

            const rowY =
                yScale.getPixelForValue(
                    i
                );


            const distance =
                Math.abs(
                    event.y -
                    rowY
                );


            if (
                distance <
                nearestDistance
            ) {

                nearestDistance =
                    distance;

                companyIndex =
                    i;

            }

        }


        if (
            companyIndex < 0
        ) {

            return;

        }


        const rowHeight =
            rowCount > 1
                ? Math.abs(
                    yScale.getPixelForValue(
                        1
                    ) -
                    yScale.getPixelForValue(
                        0
                    )
                )
                : 80;


        if (
            nearestDistance >
            Math.max(
                rowHeight / 2,
                30
            )
        ) {

            chart.setActiveElements(
                []
            );


            chart.canvas.style.cursor =
                "default";


            args.changed =
                true;


            return;

        }


        const activeElements =
            chart.data.datasets
                .map(
                    function (
                        dataset,
                        datasetIndex
                    ) {

                        const value =
                            Number(
                                dataset.data[
                                companyIndex
                                ] || 0
                            );


                        if (
                            value <= 0
                        ) {

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
                .filter(
                    Boolean
                );


        chart.setActiveElements(
            activeElements
        );


        if (
            chart.tooltip
        ) {

            chart.tooltip.setActiveElements(
                activeElements,
                {
                    x:
                        event.x,

                    y:
                        event.y
                }
            );

        }


        chart.canvas.style.cursor =
            activeElements.length > 0
                ? "pointer"
                : "default";


        args.changed =
            true;

    }

};


// =============================================================
// LOAD FAILURE REASON CHART
// =============================================================

async function loadFailureReasonChart(
    readingMonth
) {

    const month =
        readingMonth ||
        (
            typeof getReadingMonth ===
                "function"
                ? getReadingMonth()
                : ""
        );


    if (!month) {

        console.warn(
            "Failure chart: reading month is empty."
        );

        return;

    }


    try {

        // =====================================================
        // USER ACCESS
        // =====================================================

        const access =
            await getCurrentUserAccessForFailureChart();


        const brplAllowed =
            canAccessFailureCompany(
                access,
                "BRPL"
            );


        const byplAllowed =
            canAccessFailureCompany(
                access,
                "BYPL"
            );


        console.log(
            "Failure Chart Access:",
            {
                role:
                    access?.role ??
                    access?.Role,

                company:
                    access?.company ??
                    access?.Company,

                department:
                    access?.department ??
                    access?.Department,

                BRPL:
                    brplAllowed,

                BYPL:
                    byplAllowed
            }
        );


        // =====================================================
        // RESET DATA
        // =====================================================

        let brplData = [];

        let byplData = [];


        // =====================================================
        // BRPL API
        // =====================================================

        if (
            brplAllowed
        ) {

            const response =
                await $.ajax({

                    url:
                        getReadingTrendApiUrl(
                            "DashboardApi/failure-reason-count"
                        ),

                    type:
                        "GET",

                    data: {

                        ReadingMonth:
                            month

                    },

                    cache:
                        false

                });


            console.log(
                "BRPL Failure API Raw Response:",
                response
            );


            brplData =
                normalizeBRPLFailureData(
                    response
                );

        }


        // =====================================================
        // BYPL API
        // =====================================================

        if (
            byplAllowed
        ) {

            const response =
                await $.ajax({

                    url:
                        getReadingTrendApiUrl(
                            "DashboardApi/meter-download-detailed-summary-bypl"
                        ),

                    type:
                        "GET",

                    data: {

                        ReadingMonth:
                            month

                    },

                    cache:
                        false

                });


            console.log(
                "BYPL Failure API Raw Response:",
                response
            );


            byplData =
                buildBYPLFailureCounts(
                    response
                );

        }


        // =====================================================
        // MERGE
        // =====================================================

        const brpl =
            mergeFailureReasons(
                brplData
            );


        const bypl =
            mergeFailureReasons(
                byplData
            );


        console.log(
            "Normalized Failure Data:",
            {
                BRPL:
                    brpl,

                BYPL:
                    bypl
            }
        );


        // =====================================================
        // FAILURE REASONS
        // =====================================================

        const failureReasonSet =
            new Set();


        if (
            brplAllowed
        ) {

            brpl.forEach(
                function (item) {

                    failureReasonSet.add(
                        item.reason
                    );

                }
            );

        }


        if (
            byplAllowed
        ) {

            bypl.forEach(
                function (item) {

                    failureReasonSet.add(
                        item.reason
                    );

                }
            );

        }


        const validFailureReasons =
            Array.from(
                failureReasonSet
            )
                .filter(
                    function (reason) {

                        return (
                            String(reason)
                                .trim()
                                .length > 0
                        );

                    }
                );


        // =====================================================
        // SORT BY TOTAL COUNT
        // =====================================================

        validFailureReasons.sort(
            function (a, b) {

                const totalA =
                    getFailureCount(
                        brpl,
                        a
                    ) +
                    getFailureCount(
                        bypl,
                        a
                    );


                const totalB =
                    getFailureCount(
                        brpl,
                        b
                    ) +
                    getFailureCount(
                        bypl,
                        b
                    );


                return (
                    totalB -
                    totalA
                );

            }
        );


        // =====================================================
        // DESTROY EXISTING CHART
        // =====================================================

        if (
            readingTrendChart
        ) {

            readingTrendChart.destroy();

            readingTrendChart =
                null;

        }


        // =====================================================
        // CANVAS
        // =====================================================

        const canvas =
            document.getElementById(
                "readingTrendChart"
            );


        if (!canvas) {

            console.error(
                "Canvas #readingTrendChart not found."
            );

            return;

        }


        // =====================================================
        // NO DATA
        // =====================================================

        if (
            validFailureReasons.length === 0
        ) {

            const ctx =
                canvas.getContext(
                    "2d"
                );


            ctx.clearRect(
                0,
                0,
                canvas.width,
                canvas.height
            );


            console.warn(
                "No failure reason data available.",
                {
                    BRPLAllowed:
                        brplAllowed,

                    BYPLAllowed:
                        byplAllowed,

                    BRPLRecords:
                        brpl.length,

                    BYPLRecords:
                        bypl.length
                }
            );


            return;

        }


        // =====================================================
        // COMPANY LABELS
        // =====================================================

        const companyLabels = [];


        if (
            brplAllowed
        ) {

            companyLabels.push(
                "BRPL"
            );

        }


        if (
            byplAllowed
        ) {

            companyLabels.push(
                "BYPL"
            );

        }


        // =====================================================
        // DATASETS
        // =====================================================

        const datasets =
            validFailureReasons.map(
                function (reason) {

                    const values = [];


                    if (
                        brplAllowed
                    ) {

                        values.push(
                            getFailureCount(
                                brpl,
                                reason
                            )
                        );

                    }


                    if (
                        byplAllowed
                    ) {

                        values.push(
                            getFailureCount(
                                bypl,
                                reason
                            )
                        );

                    }


                    return {

                        label:
                            reason,

                        data:
                            values,

                        backgroundColor:
                            getFailureColor(
                                reason
                            ),

                        borderColor:
                            "#ffffff",

                        borderWidth:
                            1,

                        borderRadius:
                            2,

                        borderSkipped:
                            false,

                        barThickness:
                            34,

                        maxBarThickness:
                            38

                    };

                }
            );


        // =====================================================
        // CREATE CHART
        // =====================================================

        readingTrendChart =
            new Chart(
                canvas,
                {

                    type:
                        "bar",


                    data: {

                        labels:
                            companyLabels,

                        datasets:
                            datasets

                    },


                    plugins: [

                        rowHoverPlugin

                    ],


                    options: {

                        indexAxis:
                            "y",

                        responsive:
                            true,

                        maintainAspectRatio:
                            false,

                        animation: {

                            duration:
                                450

                        },

                        events: [

                            "mousemove",

                            "mouseout",

                            "touchstart",

                            "touchmove"

                        ],

                        interaction: {

                            mode:
                                "index",

                            intersect:
                                false

                        },


                        plugins: {

                            legend: {

                                display:
                                    true,

                                position:
                                    "top",

                                align:
                                    "start",

                                labels: {

                                    usePointStyle:
                                        true,

                                    pointStyle:
                                        "circle",

                                    padding:
                                        14,

                                    boxWidth:
                                        11,

                                    boxHeight:
                                        11,

                                    font: {

                                        size:
                                            12,

                                        weight:
                                            "600"

                                    },

                                    color:
                                        "#4b5563"

                                }

                            },


                            title: {

                                display:
                                    true,

                                text:
                                    "HES Download Failure Reason",

                                align:
                                    "start",

                                color:
                                    "#374151",

                                font: {

                                    size:
                                        14,

                                    weight:
                                        "700"

                                },

                                padding: {

                                    top:
                                        2,

                                    bottom:
                                        4

                                }

                            },


                            subtitle: {

                                display:
                                    true,

                                text:
                                    `${companyLabels.join(" vs ")} • ${month}`,

                                align:
                                    "start",

                                color:
                                    "#6b7280",

                                font: {

                                    size:
                                        11,

                                    weight:
                                        "600"

                                },

                                padding: {

                                    bottom:
                                        10

                                }

                            },


                            tooltip: {

                                enabled:
                                    true,

                                backgroundColor:
                                    "#111827",

                                titleColor:
                                    "#ffffff",

                                bodyColor:
                                    "#ffffff",

                                footerColor:
                                    "#d1d5db",

                                padding:
                                    8,

                                cornerRadius:
                                    7,

                                displayColors:
                                    true,

                                boxWidth:
                                    10,

                                boxHeight:
                                    10,

                                boxPadding:
                                    5,

                                callbacks: {

                                    title:
                                        function (
                                            context
                                        ) {

                                            if (
                                                !context ||
                                                !context.length
                                            ) {

                                                return "";

                                            }


                                            return (
                                                context[0].label +
                                                " Breakdown"
                                            );

                                        },


                                    label:
                                        function (
                                            context
                                        ) {

                                            const value =
                                                Number(
                                                    context.raw ||
                                                    0
                                                );


                                            if (
                                                value <= 0
                                            ) {

                                                return null;

                                            }


                                            return (
                                                context.dataset.label +
                                                ": " +
                                                value.toLocaleString()
                                            );

                                        },


                                    footer:
                                        function (
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
                                                                item.raw ||
                                                                0
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


                        scales: {

                            x: {

                                stacked:
                                    true,

                                beginAtZero:
                                    true,

                                title: {

                                    display:
                                        true,

                                    text:
                                        "Number of Failed Meters",

                                    color:
                                        "#4b5563",

                                    font: {

                                        size:
                                            12,

                                        weight:
                                            "600"

                                    },

                                    padding: {

                                        top:
                                            8

                                    }

                                },

                                ticks: {

                                    precision:
                                        0,

                                    color:
                                        "#4b5563",

                                    font: {

                                        size:
                                            11,

                                        weight:
                                            "500"

                                    },

                                    padding:
                                        5

                                },

                                grid: {

                                    color:
                                        "rgba(0,0,0,0.08)",

                                    drawBorder:
                                        false

                                }

                            },


                            y: {

                                stacked:
                                    true,

                                grid: {

                                    display:
                                        false

                                },

                                ticks: {

                                    color:
                                        "#374151",

                                    font: {

                                        size:
                                            14,

                                        weight:
                                            "700"

                                    },

                                    padding:
                                        8

                                }

                            }

                        }

                    }

                }
            );


        // =====================================================
        // FINAL DEBUG
        // =====================================================

        console.log(
            "Failure Reason Chart Created:",
            {

                role:
                    access?.role ??
                    access?.Role,

                company:
                    access?.company ??
                    access?.Company,

                department:
                    access?.department ??
                    access?.Department,

                month:
                    month,

                companies:
                    companyLabels,

                failureReasons:
                    validFailureReasons,

                BRPL:
                    brpl,

                BYPL:
                    bypl,

                datasets:
                    datasets.map(
                        function (dataset) {

                            return {

                                label:
                                    dataset.label,

                                color:
                                    dataset.backgroundColor,

                                data:
                                    dataset.data

                            };

                        }
                    )

            }
        );

    }
    catch (error) {

        console.error(
            "Failure reason chart load error:",
            error
        );

    }

}