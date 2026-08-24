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

// =============================================================
// DEPARTMENT CHART INSTANCE
// =============================================================

let departmentChart = null;


// =============================================================
// LOAD DEPARTMENT DISTRIBUTION
// =============================================================

async function loadDepartmentDistribution() {

    const month = getReadingMonth();

    try {

        const [brplResponse, byplResponse] = await Promise.all([

            // BRPL API
            fetch(
                `${getApiUrl("DashboardApi/department-wise-data")}?readingMonth=${encodeURIComponent(month)}`
            ),

            // BYPL API
            fetch(
                `${getApiUrl("DashboardApi/department-wise-data-bypl")}?readingMonth=${encodeURIComponent(month)}`
            )

        ]);


        // =====================================================
        // CHECK API RESPONSES
        // =====================================================

        if (!brplResponse.ok) {

            throw new Error(
                `Failed to load BRPL Department Distribution. Status: ${brplResponse.status}`
            );

        }


        if (!byplResponse.ok) {

            throw new Error(
                `Failed to load BYPL Department Distribution. Status: ${byplResponse.status}`
            );

        }


        // =====================================================
        // GET JSON DATA
        // =====================================================

        const brplData = await brplResponse.json();
        const byplData = await byplResponse.json();


        console.log("BRPL Department Data:", brplData);
        console.log("BYPL Department Data:", byplData);


        // =====================================================
        // GET UNIQUE DEPARTMENTS
        // =====================================================

        const departments = [

            ...new Set([

                ...brplData.map(x => x.department),

                ...byplData.map(x => x.department)

            ])

        ];


        // =====================================================
        // CREATE BRPL DATA MAP
        // =====================================================

        const brplMap = Object.fromEntries(

            brplData.map(x => [

                x.department,
                x

            ])

        );


        // =====================================================
        // CREATE BYPL DATA MAP
        // =====================================================

        const byplMap = Object.fromEntries(

            byplData.map(x => [

                x.department,
                x

            ])

        );


        // =====================================================
        // BRPL HES DOWNLOAD DATA
        // =====================================================

        const brplHes = departments.map(department => {

            return Number(
                brplMap[department]?.hesDownload || 0
            );

        });


        // =====================================================
        // BRPL FAILED DATA
        // =====================================================

        const brplFailed = departments.map(department => {

            return Number(
                brplMap[department]?.failed || 0
            );

        });


        // =====================================================
        // BYPL HES DOWNLOAD DATA
        // =====================================================

        const byplHes = departments.map(department => {

            return Number(
                byplMap[department]?.hesDownload || 0
            );

        });


        // =====================================================
        // BYPL FAILED DATA
        // =====================================================

        const byplFailed = departments.map(department => {

            return Number(
                byplMap[department]?.failed || 0
            );

        });


        // =====================================================
        // GET CHART CANVAS
        // =====================================================

        const ctx = document.getElementById(
            "departmentChart"
        );


        if (!ctx) {

            console.error(
                "departmentChart canvas not found."
            );

            return;

        }


        // =====================================================
        // DESTROY OLD CHART
        // =====================================================

        if (departmentChart) {

            departmentChart.destroy();

            departmentChart = null;

        }


        // =====================================================
        // CREATE CHART
        // =====================================================

        departmentChart = new Chart(ctx, {

            type: "bar",

            data: {

                labels: departments,

                datasets: [

                    // BRPL HES DOWNLOAD
                    {
                        label: "BRPL - HES Download",

                        data: brplHes,

                        backgroundColor: "#198754",

                        borderColor: "#146c43",

                        borderWidth: 1,

                        borderRadius: 4,

                        barPercentage: 0.95,

                        categoryPercentage: 0.85
                    },


                    // BRPL FAILED
                    {
                        label: "BRPL - Download Failed",

                        data: brplFailed,

                        backgroundColor: "#dc3545",

                        borderColor: "#b02a37",

                        borderWidth: 1,

                        borderRadius: 4,

                        barPercentage: 0.95,

                        categoryPercentage: 0.85
                    },


                    // BYPL HES DOWNLOAD
                    {
                        label: "BYPL - HES Download",

                        data: byplHes,

                        backgroundColor: "#20c997",

                        borderColor: "#198f6a",

                        borderWidth: 1,

                        borderRadius: 4,

                        barPercentage: 0.95,

                        categoryPercentage: 0.85
                    },


                    // BYPL FAILED
                    {
                        label: "BYPL - Download Failed",

                        data: byplFailed,

                        backgroundColor: "#e85d75",

                        borderColor: "#c43d55",

                        borderWidth: 1,

                        borderRadius: 4,

                        barPercentage: 0.95,

                        categoryPercentage: 0.85
                    }

                ]

            },


            options: {

                responsive: true,

                maintainAspectRatio: false,

                indexAxis: "y",


                // =================================================
                // INTERACTION
                // =================================================

                interaction: {

                    mode: "nearest",

                    intersect: true

                },


                plugins: {


                    // =============================================
                    // LEGEND
                    // =============================================

                    legend: {

                        position: "top",

                        labels: {

                            usePointStyle: true,

                            padding: 15

                        }

                    },


                    // =============================================
                    // TOOLTIP
                    // =============================================

                    tooltip: {

                        enabled: true,

                        mode: "nearest",

                        intersect: true,

                        displayColors: true,

                        backgroundColor:
                            "rgba(33, 37, 41, 0.95)",

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


                // =================================================
                // SCALES
                // =================================================

                scales: {


                    // X AXIS
                    x: {

                        beginAtZero: true,

                        ticks: {

                            precision: 0,

                            callback: function (value) {

                                return Number(value)
                                    .toLocaleString();

                            }

                        }

                    },


                    // Y AXIS
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

        });

    }
    catch (error) {

        console.error(
            "Department Distribution Error:",
            error
        );

    }

}