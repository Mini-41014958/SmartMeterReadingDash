async function loadReadingTrend() {

    const response =
        await fetch("api/dashboardapi/reading_trend_date_wise");

    const data = await response.json();

    const labels = data.map(x => {

        const d = new Date(x.readingDate);

        return d.toLocaleDateString("en-GB", {
            day: "2-digit",
            month: "short"
        });

    });

    const values = data.map(x => x.readingCount);

    new Chart(document.getElementById("readingTrendChart"), {

        type: "line",

        data: {

            labels: labels,

            datasets: [{

                label: "Readings",

                data: values,

                fill: false,

                tension: 0.35

            }]

        },

        options: {

            responsive: true,

            plugins: {

                legend: {

                    display: false

                }

            }

        }

    });

}