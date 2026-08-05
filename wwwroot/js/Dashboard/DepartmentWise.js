let departmentChart = null;

async function loadDepartmentDistribution() {
    const month = getReadingMonth();
    const response = await fetch(`api/DashboardApi/department-wise-data?readingMonth=${month}`);

    if (!response.ok) {
        throw new Error("Failed to load Department Distribution.");
    }

    const data = await response.json();

    const ctx = document.getElementById("departmentChart");

    if (departmentChart) {
        departmentChart.destroy();
    }

    departmentChart = new Chart(ctx, {
        type: "bar",

        data: {
            labels: data.map(x => x.department),

            datasets: [
                {
                    label: "HES Download",
                    data: data.map(x => x.hesDownload),
                    backgroundColor: "#198754"
                },
                {
                    label: "Download Failed",
                    data: data.map(x => x.failed),
                    backgroundColor: "#dc3545"
                }
            ]
        },

        options: {
            responsive: true,
            maintainAspectRatio: false,
            indexAxis: "y",

            plugins: {
                legend: {
                    position: "top"
                }
            },

            scales: {
                x: {
                    beginAtZero: true,
                    ticks: {
                        precision: 0
                    }
                }
            }
        }
    });
}