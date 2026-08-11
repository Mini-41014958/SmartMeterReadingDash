let departmentChart = null;

async function loadDepartmentDistribution() {

    const month = getReadingMonth();

    try {

        const [brplResponse, byplResponse] = await Promise.all([

            fetch(
                `api/DashboardApi/department-wise-data?readingMonth=${encodeURIComponent(month)}`
            ),

            fetch(
                `api/DashboardApi/department-wise-data-bypl?readingMonth=${encodeURIComponent(month)}`
            )

        ]);

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

        const brplData = await brplResponse.json();
        const byplData = await byplResponse.json();

        console.log("BRPL Department Data:", brplData);
        console.log("BYPL Department Data:", byplData);

        const departments = [
            ...new Set([
                ...brplData.map(x => x.department),
                ...byplData.map(x => x.department)
            ])
        ];

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

        const ctx = document.getElementById(
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

        departmentChart = new Chart(ctx, {

            type: "bar",

            data: {

                labels: departments,

                datasets: [

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

                indexAxis: "y",

                interaction: {
                    mode: "nearest",
                    intersect: true
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

                                const value =
                                    Number(context.raw || 0);

                                return `${context.dataset.label}: ${value.toLocaleString()}`;
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

                                return Number(value)
                                    .toLocaleString();

                            }

                        }

                    },

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