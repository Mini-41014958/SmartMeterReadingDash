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

        // =====================================================
        // BRPL ONLY MODE
        // BYPL IS TEMPORARILY DISABLED
        // =====================================================

        // const brplAllowed = canAccessCompany("BRPL");
        // const byplAllowed = canAccessCompany("BYPL");

        const brplAllowed = true;
        const byplAllowed = false;


        // =====================================================
        // BRPL API
        // =====================================================

        if (brplAllowed) {
            const brplUrl =
                `${getApiUrl("DashboardApi/department-wise-data")}` +
                `?readingMonth=${encodeURIComponent(month)}`;

            const brplResponse = await fetch(
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
                const errorText = await brplResponse.text()
                    .catch(() => "");

                throw new Error(
                    `Failed to load BRPL Department Distribution. ` +
                    `Status: ${brplResponse.status}. ${errorText}`
                );
            }

            brplData = await brplResponse.json();

            if (!Array.isArray(brplData)) {
                brplData = [];
            }
        }


        // =====================================================
        // BYPL API
        // =====================================================

        if (byplAllowed) {
            const byplUrl =
                `${getApiUrl("DashboardApi/department-wise-data-bypl")}` +
                `?readingMonth=${encodeURIComponent(month)}`;

            const byplResponse = await fetch(
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
                const errorText = await byplResponse.text()
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

            byplData = await byplResponse.json();

            if (!Array.isArray(byplData)) {
                byplData = [];
            }
        }


        // =====================================================
        // BUILD UNIQUE DEPARTMENT LIST
        // =====================================================

        const departments = [
            ...new Set([
                ...brplData
                    .map(x => String(x.department || "").trim())
                    .filter(Boolean),

                ...byplData
                    .map(x => String(x.department || "").trim())
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

        const brplMap = Object.fromEntries(
            brplData.map(x => [
                String(x.department || "").trim(),
                x
            ])
        );


        // =====================================================
        // BUILD BYPL MAP
        // =====================================================

        const byplMap = Object.fromEntries(
            byplData.map(x => [
                String(x.department || "").trim(),
                x
            ])
        );


        // =====================================================
        // BRPL HES DOWNLOAD DATA
        // =====================================================

        const brplHes = departments.map(department => {
            if (!brplAllowed) {
                return 0;
            }

            return Number(
                brplMap[department]?.hesDownload || 0
            );
        });


        // =====================================================
        // BRPL CMRI DOWNLOAD DATA
        // API MUST RETURN cmriDownload FOR EACH DEPARTMENT
        // =====================================================

        const brplCmri = departments.map(department => {
            if (!brplAllowed) {
                return 0;
            }

            return Number(
                brplMap[department]?.cmriDownload || 0
            );
        });


        // =====================================================
        // BRPL DOWNLOAD FAILED DATA
        // =====================================================

        const brplFailed = departments.map(department => {
            if (!brplAllowed) {
                return 0;
            }

            return Number(
                brplMap[department]?.failed || 0
            );
        });


        // =====================================================
        // BYPL HES DOWNLOAD DATA
        // =====================================================

        const byplHes = departments.map(department => {
            if (!byplAllowed) {
                return 0;
            }

            return Number(
                byplMap[department]?.hesDownload || 0
            );
        });


        // =====================================================
        // BYPL DOWNLOAD FAILED DATA
        // =====================================================

        const byplFailed = departments.map(department => {
            if (!byplAllowed) {
                return 0;
            }

            return Number(
                byplMap[department]?.failed || 0
            );
        });


        // =====================================================
        // GET CHART CANVAS
        // =====================================================

        const ctx = document.getElementById("departmentChart");

        if (!ctx) {
            console.error(
                "departmentChart canvas not found."
            );

            return;
        }


        // =====================================================
        // DESTROY PREVIOUS CHART
        // =====================================================

        if (departmentChart) {
            departmentChart.destroy();
            departmentChart = null;
        }


        // =====================================================
        // HANDLE EMPTY DATA
        // =====================================================

        if (departments.length === 0) {
            const context = ctx.getContext("2d");

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


        // =====================================================
        // RESPONSIVE BAR SIZING
        // =====================================================

        const departmentCount = departments.length;

        const visibleDatasetCount =
            (brplAllowed ? 3 : 0) +
            (byplAllowed ? 2 : 0);

        let BAR_THICKNESS;
        let BAR_PERCENTAGE;
        let CATEGORY_PERCENTAGE;

        if (departmentCount <= 2) {
            if (visibleDatasetCount <= 3) {
                BAR_THICKNESS = 11;
                BAR_PERCENTAGE = 0.60;
                CATEGORY_PERCENTAGE = 0.70;
            }
            else {
                BAR_THICKNESS = 9;
                BAR_PERCENTAGE = 0.65;
                CATEGORY_PERCENTAGE = 0.75;
            }
        }
        else {
            BAR_THICKNESS = 13;
            BAR_PERCENTAGE = 0.65;
            CATEGORY_PERCENTAGE = 0.70;
        }


        // =====================================================
        // COMMON BAR OPTIONS
        // =====================================================

        const getBarOptions = () => ({
            barPercentage: BAR_PERCENTAGE,
            categoryPercentage: CATEGORY_PERCENTAGE,

            ...(BAR_THICKNESS !== undefined
                ? {
                    barThickness: BAR_THICKNESS,
                    maxBarThickness: BAR_THICKNESS
                }
                : {
                    maxBarThickness: 18
                })
        });


        // =====================================================
        // BUILD DATASETS
        // =====================================================

        const datasets = [];


        // =====================================================
        // BRPL DATASETS
        // =====================================================

        if (brplAllowed) {

            // -------------------------------------------------
            // BRPL HES DOWNLOAD
            // -------------------------------------------------

            datasets.push({
                label: "BRPL - HES Download",

                data: brplHes,

                backgroundColor: "#198754",
                borderColor: "#146c43",

                borderWidth: 1,
                borderRadius: 4,

                ...getBarOptions()
            });


            // -------------------------------------------------
            // BRPL CMRI DOWNLOAD
            // -------------------------------------------------

            datasets.push({
                label: "BRPL - CMRI Download",

                data: brplCmri,

                backgroundColor: "#0dcaf0",
                borderColor: "#087990",

                borderWidth: 1,
                borderRadius: 4,

                ...getBarOptions()
            });


            // -------------------------------------------------
            // BRPL DOWNLOAD FAILED
            // -------------------------------------------------

            datasets.push({
                label: "BRPL - Download Failed",

                data: brplFailed,

                backgroundColor: "#dc3545",
                borderColor: "#b02a37",

                borderWidth: 1,
                borderRadius: 4,

                ...getBarOptions()
            });
        }


        // =====================================================
        // BYPL DATASETS
        // =====================================================

        if (byplAllowed) {

            // -------------------------------------------------
            // BYPL HES DOWNLOAD
            // -------------------------------------------------

            datasets.push({
                label: "BYPL - HES Download",

                data: byplHes,

                backgroundColor: "#20c997",
                borderColor: "#198f6a",

                borderWidth: 1,
                borderRadius: 4,

                ...getBarOptions()
            });


            // -------------------------------------------------
            // BYPL DOWNLOAD FAILED
            // -------------------------------------------------

            datasets.push({
                label: "BYPL - Download Failed",

                data: byplFailed,

                backgroundColor: "#e85d75",
                borderColor: "#c43d55",

                borderWidth: 1,
                borderRadius: 4,

                ...getBarOptions()
            });
        }


        // =====================================================
        // CREATE HORIZONTAL BAR CHART
        // =====================================================

        departmentChart = new Chart(
            ctx,
            {
                type: "bar",

                data: {
                    labels: departments,
                    datasets: datasets
                },

                options: {
                    responsive: true,
                    maintainAspectRatio: false,

                    indexAxis: "y",

                    // =========================================
                    // INTERACTION
                    // =========================================

                    interaction: {
                        mode: "nearest",
                        intersect: true
                    },

                    // =========================================
                    // PLUGINS
                    // =========================================

                    plugins: {

                        // -------------------------------------
                        // LEGEND
                        // -------------------------------------

                        legend: {
                            position: "top",

                            labels: {
                                usePointStyle: true,
                                padding: 15
                            }
                        },


                        // -------------------------------------
                        // TOOLTIP
                        // -------------------------------------

                        tooltip: {
                            enabled: true,

                            mode: "nearest",
                            intersect: true,

                            displayColors: true,

                            backgroundColor: "rgba(33, 37, 41, 0.95)",

                            titleColor: "#ffffff",
                            bodyColor: "#ffffff",

                            padding: 12,
                            cornerRadius: 8,

                            callbacks: {

                                title: function (tooltipItems) {
                                    return tooltipItems.length
                                        ? `Department: ${tooltipItems[0].label}`
                                        : "";
                                },

                                label: function (context) {
                                    const value = Number(
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


                    // =========================================
                    // AXES
                    // =========================================

                    scales: {

                        // -------------------------------------
                        // X AXIS
                        // -------------------------------------

                        x: {
                            beginAtZero: true,

                            ticks: {
                                precision: 0,

                                callback: function (value) {
                                    return Number(value).toLocaleString();
                                }
                            }
                        },


                        // -------------------------------------
                        // Y AXIS
                        // -------------------------------------

                        y: {
                            stacked: false,

                            ticks: {
                                font: {
                                    size: 13,
                                    weight: "600"
                                }
                            },

                            grid: {
                                display: false
                            }
                        }
                    }
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