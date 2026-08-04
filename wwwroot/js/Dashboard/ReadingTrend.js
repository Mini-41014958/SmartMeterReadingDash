let readingTrendChart = null;

async function loadReadingTrend() {

    const response = await fetch("/api/dashboardapi/reading_trend_date_wise");

    if (!response.ok) {
        throw new Error("Failed to load Reading Trend.");
    }

    const data = await response.json();

    const labels = data.map(x => {

        const d = new Date(x.readingDate);

        return d.toLocaleDateString("en-GB", {
            day: "2-digit",
            month: "short"
        });

    });

    const values = data.map(x => x.readingCount);

    const ctx = document.getElementById("readingTrendChart");

    if (readingTrendChart) {
        readingTrendChart.destroy();
    }

    readingTrendChart = new Chart(ctx, {

        type: "line",

        data: {

            labels: labels,

            datasets: [{

                label: "Readings",

                data: values,

                borderColor: "#0d6efd",
                backgroundColor: "#0d6efd",

                fill: false,

                tension: 0.35

            }]

        },

        options: {

            responsive: true,

            maintainAspectRatio: false,

            plugins: {

                legend: {
                    display: false
                }

            },

            scales: {

                y: {

                    beginAtZero: true,

                    ticks: {
                        precision: 0
                    }

                }

            }

        }

    });

}