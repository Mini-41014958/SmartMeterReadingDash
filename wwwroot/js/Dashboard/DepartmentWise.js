// =============================================================
// API BASE URL
// =============================================================

function getApiUrl(endpoint) {
    const basePath = window.location.pathname
        .toLowerCase()
        .startsWith("/smartmeter/")
        ? "/SmartMeter"
        : "";

    return `${basePath}/api/${endpoint}`;
}

let departmentChart = null;


// =============================================================
// LOAD DEPARTMENT DISTRIBUTION
// =============================================================

async function loadDepartmentDistribution() {

    const month = getReadingMonth();

    try {

        let brplData = [];
        let byplData = [];

        const brplAllowed = canAccessCompany("BRPL");
        const byplAllowed = canAccessCompany("BYPL");

        console.log("Department Distribution Access:", {
            BRPL: brplAllowed,
            BYPL: byplAllowed,
            ReadingMonth: month
        });


        // =====================================================
        // BRPL
        // =====================================================

        if (brplAllowed) {

            const brplUrl =
                `${getApiUrl("DashboardApi/department-wise-data")}` +
                `?readingMonth=${encodeURIComponent(month)}`;

            console.log(
                "BRPL Department API URL:",
                brplUrl
            );

            const brplResponse =
                await fetch(
                    brplUrl,
                    {
                        method: "GET",
                        credentials: "same-origin",
                        cache: "no-store",
                        headers: {
                            "Accept": "application/json"
                        }
                    }
                );

            if (!brplResponse.ok) {

                const errorText =
                    await brplResponse.text()
                        .catch(() => "");

                console.error(
                    "BRPL Department API Error:",
                    {
                        status: brplResponse.status,
                        url: brplUrl,
                        response: errorText
                    }
                );

                throw new Error(
                    `Failed to load BRPL Department Distribution. ` +
                    `Status: ${brplResponse.status}. ${errorText}`
                );
            }

            brplData =
                await brplResponse.json();

            if (!Array.isArray(brplData)) {
                brplData = [];
            }
        }


        // =====================================================
        // BYPL
        // =====================================================

        if (byplAllowed) {

            const byplUrl =
                `${getApiUrl("DashboardApi/department-wise-data-bypl")}` +
                `?readingMonth=${encodeURIComponent(month)}`;

            console.log(
                "BYPL Department API URL:",
                byplUrl
            );

            const byplResponse =
                await fetch(
                    byplUrl,
                    {
                        method: "GET",
                        credentials: "same-origin",
                        cache: "no-store",
                        headers: {
                            "Accept": "application/json"
                        }
                    }
                );

            if (!byplResponse.ok) {

                const errorText =
                    await byplResponse.text()
                        .catch(() => "");

                console.error(
                    "BYPL Department API Error:",
                    {
                        status: byplResponse.status,
                        url: byplUrl,
                        response: errorText
                    }
                );

                throw new Error(
                    `Failed to load BYPL Department Distribution. ` +
                    `Status: ${byplResponse.status}. ${errorText}`
                );
            }

            byplData =
                await byplResponse.json();

            if (!Array.isArray(byplData)) {
                byplData = [];
            }
        }


        // =====================================================
        // DEBUG
        // =====================================================

        console.log(
            "BRPL Department Records:",
            brplData.length
        );

        console.log(
            "BYPL Department Records:",
            byplData.length
        );


        // =====================================================
        // BUILD UNIQUE DEPARTMENT LIST
        // =====================================================

        const departments = [
            ...new Set([

                ...brplData
                    .map(
                        x =>
                            String(
                                x.department || ""
                            ).trim()
                    )
                    .filter(Boolean),

                ...byplData
                    .map(
                        x =>
                            String(
                                x.department || ""
                            ).trim()
                    )
                    .filter(Boolean)

            ])
        ];


        // =====================================================
        // SORT DEPARTMENTS
        // =====================================================

        departments.sort(
            (a, b) =>
                a.localeCompare(
                    b,
                    undefined,
                    {
                        numeric: true
                    }
                )
        );


        // =====================================================
        // BUILD BRPL MAP
        // =====================================================

        const brplMap =
            Object.fromEntries(

                brplData.map(
                    x => [

                        String(
                            x.department || ""
                        ).trim(),

                        x

                    ]
                )

            );


        // =====================================================
        // BUILD BYPL MAP
        // =====================================================

        const byplMap =
            Object.fromEntries(

                byplData.map(
                    x => [

                        String(
                            x.department || ""
                        ).trim(),

                        x

                    ]
                )

            );


        // =====================================================
        // BRPL HES DOWNLOAD
        // =====================================================

        const brplHes =
            departments.map(
                department => {

                    if (!brplAllowed) {
                        return 0;
                    }

                    return Number(
                        brplMap[
                            department
                        ]?.hesDownload || 0
                    );

                }
            );


        // =====================================================
        // BRPL FAILED
        // =====================================================

        const brplFailed =
            departments.map(
                department => {

                    if (!brplAllowed) {
                        return 0;
                    }

                    return Number(
                        brplMap[
                            department
                        ]?.failed || 0
                    );

                }
            );

        const byplHes =
            departments.map(
                department => {

                    if (!byplAllowed) {
                        return 0;
                    }

                    return Number(
                        byplMap[
                            department
                        ]?.hesDownload || 0
                    );

                }
            );

        const byplFailed =
            departments.map(
                department => {

                    if (!byplAllowed) {
                        return 0;
                    }

                    return Number(
                        byplMap[
                            department
                        ]?.failed || 0
                    );

                }
            );


        const ctx =
            document.getElementById(
                "departmentChart"
            );

        if (!ctx) {

            console.error(
                "departmentChart canvas not found."
            );

            return;
        }

        if (departmentChart) {

            departmentChart.destroy();

            departmentChart = null;
        }


        if (departments.length === 0) {

            const context =
                ctx.getContext("2d");

            context.clearRect(
                0,
                0,
                ctx.width,
                ctx.height
            );

            console.warn(
                "No department distribution data available."
            );

            return;
        }

        const departmentCount = departments.length;

        const visibleDatasetCount =
            (brplAllowed ? 2 : 0) +
            (byplAllowed ? 2 : 0);

        let BAR_THICKNESS;
        let BAR_PERCENTAGE;
        let CATEGORY_PERCENTAGE;

        if (departmentCount <= 2) {

            // One company = 2 bars
            if (visibleDatasetCount <= 2) {

                BAR_THICKNESS = 12;
                BAR_PERCENTAGE = 0.55;
                CATEGORY_PERCENTAGE = 0.60;

            }

            // BRPL + BYPL = 4 bars
            else {

                BAR_THICKNESS = 10;
                BAR_PERCENTAGE = 0.65;
                CATEGORY_PERCENTAGE = 0.75;

            }

        }

        else {

            BAR_THICKNESS = 13;
            BAR_PERCENTAGE = 0.65;
            CATEGORY_PERCENTAGE = 0.70;

        }



        const getBarOptions = () => ({

            barPercentage:
                BAR_PERCENTAGE,

            categoryPercentage:
                CATEGORY_PERCENTAGE,

            ...(BAR_THICKNESS !== undefined
                ? {
                    barThickness:
                        BAR_THICKNESS,

                    maxBarThickness:
                        BAR_THICKNESS
                }
                : {
                    maxBarThickness:
                        18
                })

        });


        // =====================================================
        // DATASETS
        // =====================================================

        const datasets = [];


        // =====================================================
        // BRPL DATASETS
        // =====================================================

        if (brplAllowed) {

            datasets.push({

                label:
                    "BRPL - HES Download",

                data:
                    brplHes,

                backgroundColor:
                    "#198754",

                borderColor:
                    "#146c43",

                borderWidth:
                    1,

                borderRadius:
                    4,

                ...getBarOptions()

            });


            datasets.push({

                label:
                    "BRPL - Download Failed",

                data:
                    brplFailed,

                backgroundColor:
                    "#dc3545",

                borderColor:
                    "#b02a37",

                borderWidth:
                    1,

                borderRadius:
                    4,

                ...getBarOptions()

            });

        }


        // =====================================================
        // BYPL DATASETS
        // =====================================================

        if (byplAllowed) {

            datasets.push({

                label:
                    "BYPL - HES Download",

                data:
                    byplHes,

                backgroundColor:
                    "#20c997",

                borderColor:
                    "#198f6a",

                borderWidth:
                    1,

                borderRadius:
                    4,

                ...getBarOptions()

            });


            datasets.push({

                label:
                    "BYPL - Download Failed",

                data:
                    byplFailed,

                backgroundColor:
                    "#e85d75",

                borderColor:
                    "#c43d55",

                borderWidth:
                    1,

                borderRadius:
                    4,

                ...getBarOptions()

            });

        }


        // =====================================================
        // CREATE CHART
        // =====================================================

        departmentChart =
            new Chart(
                ctx,
                {

                    type:
                        "bar",


                    data: {

                        labels:
                            departments,

                        datasets:
                            datasets

                    },


                    options: {

                        responsive:
                            true,


                        maintainAspectRatio:
                            false,


                        indexAxis:
                            "y",


                        // =================================================
                        // INTERACTION
                        // =================================================

                        interaction: {

                            mode:
                                "nearest",

                            intersect:
                                true

                        },


                        // =================================================
                        // PLUGINS
                        // =================================================

                        plugins: {

                            // =============================================
                            // LEGEND
                            // =============================================

                            legend: {

                                position:
                                    "top",

                                labels: {

                                    usePointStyle:
                                        true,

                                    padding:
                                        15

                                }

                            },


                            // =============================================
                            // TOOLTIP
                            // =============================================

                            tooltip: {

                                enabled:
                                    true,

                                mode:
                                    "nearest",

                                intersect:
                                    true,

                                displayColors:
                                    true,

                                backgroundColor:
                                    "rgba(33, 37, 41, 0.95)",

                                titleColor:
                                    "#ffffff",

                                bodyColor:
                                    "#ffffff",

                                padding:
                                    12,

                                cornerRadius:
                                    8,

                                callbacks: {

                                    title:
                                        function (
                                            tooltipItems
                                        ) {

                                            return (
                                                tooltipItems.length
                                                    ? `Department: ${tooltipItems[0].label}`
                                                    : ""
                                            );

                                        },


                                    label:
                                        function (
                                            context
                                        ) {

                                            const value =
                                                Number(
                                                    context.raw || 0
                                                );

                                            return (
                                                `${context.dataset.label}: ` +
                                                `${value.toLocaleString()}`
                                            );

                                        }

                                }

                            }

                        },


                        scales: {

                            x: {

                                beginAtZero:
                                    true,

                                ticks: {

                                    precision:
                                        0,

                                    callback:
                                        function (
                                            value
                                        ) {

                                            return Number(
                                                value
                                            ).toLocaleString();

                                        }

                                }

                            },


                            y: {

                                stacked:
                                    false,

                                ticks: {

                                    font: {

                                        size:
                                            13,

                                        weight:
                                            "600"

                                    }

                                },

                                grid: {

                                    display:
                                        false

                                }

                            }

                        }

                    }

                }
            );


        console.log(
            "Department Chart Created:",
            {

                departments:
                    departments,

                departmentCount:
                    departmentCount,

                datasets:
                    datasets.map(
                        x => x.label
                    ),

                barSettings: {

                    barPercentage:
                        BAR_PERCENTAGE,

                    categoryPercentage:
                        CATEGORY_PERCENTAGE,

                    maxBarThickness:
                        MAX_BAR_THICKNESS,

                    barThickness:
                        FIXED_BAR_THICKNESS

                }

            }
        );

    }
    catch (error) {

        console.error(
            "Department Distribution Error:",
            error
        );

    }
}