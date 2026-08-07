let readingTrendChart = null;

function loadFailureReasonChart(readingMonth) {
    const month = getReadingMonth();
    $.ajax({

        url: "api/dashboardApi/failure-reason-count",

        type: "GET",

        data: {
            ReadingMonth: month
        },

        success: function (result) {

            const labels = result.map(x => x.feilureReason);
            const counts = result.map(x => x.count);

            const colors = labels.map(label => {

                switch (label.toUpperCase()) {

                    case "SYSTEM TITLE MISMATCH":
                        return "#DC3545";      // Red (Critical)

                    case "TCP CONNECTION FAILED":
                        return "#FD7E14";      // Orange

                    case "NO DATA FOUND":
                        return "#FFC107";      // Yellow


                    default:
                        return "#6C757D";      // Gray

                }

            });

            if (readingTrendChart != null) {
                readingTrendChart.destroy();
            }

            readingTrendChart = new Chart(
                document.getElementById("readingTrendChart"),
                {

                    type: "bar",

                    data: {

                        labels: labels,

                        datasets: [{

                            label: "Failed Meters",

                            data: counts,

                            backgroundColor: colors,

                            borderRadius: 8,

                            maxBarThickness: 60

                        }]

                    },

                    options: {

                        responsive: true,

                        maintainAspectRatio: false,

                        plugins: {

                            legend: {
                                display: false
                            },

                            title: {

                                display: true,

                                text: "Failure Reason Wise Count"

                            }

                        },

                        scales: {

                            x: {

                                grid: {
                                    display: false
                                }

                            },

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

    });

}