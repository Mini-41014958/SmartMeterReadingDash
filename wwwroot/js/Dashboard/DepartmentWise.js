let departmentChart = null;

async function loadDepartmentDistribution() {

    const month = getReadingMonth();

    try {

        // =========================================================
        // LOAD BRPL + BYPL API DATA
        // =========================================================

        const [brplResponse, byplResponse] = await Promise.all([

            fetch(
                `api/DashboardApi/department-wise-data?readingMonth=${encodeURIComponent(month)}`
            ),

            fetch(
                `api/DashboardApi/department-wise-data-bypl?readingMonth=${encodeURIComponent(month)}`
            )

        ]);

        // =========================================================
        // API VALIDATION
        // =========================================================

        if (!brplResponse.ok) {
            throw new Error(
                "Failed to load BRPL Department Distribution."
            );
        }

        if (!byplResponse.ok) {
            throw new Error(
                "Failed to load BYPL Department Distribution."
            );
        }

        // =========================================================
        // READ API RESPONSE
        // =========================================================

        const brplData = await brplResponse.json();
        const byplData = await byplResponse.json();

        console.log("BRPL Department Data:", brplData);
        console.log("BYPL Department Data:", byplData);

        // =========================================================
        // GET UNIQUE DEPARTMENTS
        // =========================================================

        const departments = [
            ...new Set([
                ...brplData.map(x => x.department),
                ...byplData.map(x => x.department)
            ])
        ];

        // =========================================================
        // CREATE LOOKUP MAPS
        // Avoid repeated .find()
        // =========================================================

        const brplMap = Object.fromEntries(
            brplData.map(x => [
                x.department,
                x
            ])
        );

        const byplMap = Object.fromEntries(
            byplData.map(x => [
                x.department,
                x
            ])
        );

        // =========================================================
        // BRPL DATA
        // =========================================================

        const brplHes = departments.map(department => {

            return Number(
                brplMap[department]?.hesDownload || 0
            );

        });

        const brplFailed = departments.map(department => {

            return Number(
                brplMap[department]?.failed || 0
            );

        });

        // =========================================================
        // BYPL DATA
        // =========================================================

        const byplHes = departments.map(department => {

            return Number(
                byplMap[department]?.hesDownload || 0
            );

        });

        const byplFailed = departments.map(department => {

            return Number(
                byplMap[department]?.failed || 0
            );

        });

        // =========================================================
        // GET CANVAS
        // =========================================================

        const ctx = document.getElementById(
            "departmentChart"
        );

        if (!ctx) {

            console.error(
                "departmentChart canvas not found."
            );

            return;
        }

        // =========================================================
        // DESTROY OLD CHART
        // =========================================================

        if (departmentChart) {

            departmentChart.destroy();

            departmentChart = null;
        }

        // =========================================================
        // CREATE CHART
        // =========================================================

        departmentChart = new Chart(ctx, {

            type: "bar",

            data: {

                labels: departments,

                datasets: [

                    // =================================================
                    // BRPL HES DOWNLOAD
                    // =================================================

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

                    // =================================================
                    // BRPL DOWNLOAD FAILED
                    // =================================================

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

                    // =================================================
                    // BYPL HES DOWNLOAD
                    // =================================================

                    {
                        label: "BYPL - HES Download",

                        data: byplHes,

                        backgroundColor: "#20c997",   // Teal/green shade

                        borderColor: "#198f6a",

                        borderWidth: 1,

                        borderRadius: 4,

                        barPercentage: 0.95,

                        categoryPercentage: 0.85
                    },

                    {
                        label: "BYPL - Download Failed",

                        data: byplFailed,

                        backgroundColor: "#e85d75",   // Different red shade

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

                // Horizontal bars
                indexAxis: "y",

                interaction: {
                    mode: "index",
                    intersect: false
                },

                plugins: {

                    legend: {

                        position: "top",

                        labels: {
                            usePointStyle: true,
                            padding: 15
                        }

                    },

                    tooltip: {

                        enabled: true,

                        callbacks: {

                            label: function (context) {

                                const value =
                                    Number(
                                        context.raw || 0
                                    ).toLocaleString();

                                return `${context.dataset.label}: ${value}`;

                            }

                        }

                    }

                },

                scales: {

                    x: {

                        beginAtZero: true,

                        ticks: {

                            precision: 0,

                            callback: function (value) {

                                return Number(
                                    value
                                ).toLocaleString();

                            }

                        },

                        grid: {
                            display: true
                        }

                    },

                    y: {

                        stacked: false,

                        grid: {
                            display: false
                        },

                        ticks: {

                            font: {
                                size: 13,
                                weight: "600"
                            }

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