async function loadDepartmentDistribution() {

    const response = await fetch("/api/DashboardApi/department-wise-data");
    const data = await response.json();

    new Chart(document.getElementById("departmentChart"), {
        type: "bar",
        data: {
            labels: data.map(x => x.department),
            datasets: [
                {
                    label: "Total Meters",
                    data: data.map(x => x.totalMeters),
                    backgroundColor: "#28a745",   // Green
                    borderColor: "#28a745",
                    borderWidth: 1
                },
                {
                    label: "Non Communication",
                    data: data.map(x => x.nonCom),
                    backgroundColor: "#dc3545",   // Red
                    borderColor: "#dc3545",
                    borderWidth: 1
                }
            ]
        },
        options: {
            indexAxis: "y",
            responsive: true,
            plugins: {
                legend: {
                    position: "top"
                }
            },
            scales: {
                x: {
                    beginAtZero: true
                }
            }
        }
    });
}