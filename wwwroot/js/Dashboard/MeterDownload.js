function getApiUrl(endpoint) {

    const basePath = window.location.pathname
        .toLowerCase()
        .startsWith("/smartmeter/")
        ? "/SmartMeter"
        : "";

    return `${basePath}/api/${endpoint}`;
}

async function loadMeterDownloadSummary() {

    const month = getReadingMonth();

    try {

        const [
            brplResponse,
            byplResponse
        ] = await Promise.all([

            fetch(
                `${getApiUrl(
                    "dashboardapi/meter-download-summary"
                )}?readingMonth=${encodeURIComponent(month)}`
            ),

            fetch(
                `${getApiUrl(
                    "dashboardapi/meter-download-summary-bypl"
                )}?readingMonth=${encodeURIComponent(month)}`
            )

        ]);


        if (!brplResponse.ok) {

            throw new Error(
                `Failed to load BRPL Meter Summary. Status: ${brplResponse.status}`
            );

        }


        if (!byplResponse.ok) {

            throw new Error(
                `Failed to load BYPL Meter Summary. Status: ${byplResponse.status}`
            );

        }


        const brplResult =
            await brplResponse.json();

        const byplResult =
            await byplResponse.json();


        const brplData =
            Array.isArray(brplResult)
                ? brplResult[0] || {}
                : brplResult || {};


        const byplData =
            Array.isArray(byplResult)
                ? byplResult[0] || {}
                : byplResult || {};

        function setValue(id, value) {

            const element =
                document.getElementById(id);

            if (!element) {

                console.warn(
                    `Missing HTML element: #${id}`
                );

                return;
            }

            element.textContent = value;
        }


        const brplFailed =
            Number(brplData?.manualForwardinCount || 0) +
            Number(brplData?.pendingCount || 0) +
            Number(brplData?.mismatchCount || 0);


        setValue(
            "hesDownload",
            Number(
                brplData?.hesDownloadCount || 0
            ).toLocaleString()
        );


        setValue(
            "downloadPercentage",
            "(" +
            Number(
                brplData?.hesDownloadPercentage || 0
            ).toFixed(2) +
            "%)"
        );


        setValue(
            "downloadFailed",
            brplFailed.toLocaleString()
        );


        setValue(
            "failedPercentage",
            "(" +
            Number(
                brplData?.hesFailedPercentage || 0
            ).toFixed(2) +
            "%)"
        );


        setValue(
            "totalMeters",
            Number(
                brplData?.totalMetersCount || 0
            ).toLocaleString()
        );


        setValue(
            "billedMeters",
            Number(
                brplData?.billedCount || 0
            ).toLocaleString()
        );


        setValue(
            "billedPercentage",
            "(" +
            Number(
                brplData?.billedPercentage || 0
            ).toFixed(2) +
            "%)"
        );


        setValue(
            "billedFailed",
            Number(
                brplData?.billedFailedCount || 0
            ).toLocaleString()
        );


        setValue(
            "billedFailedPercentage",
            "(" +
            Number(
                brplData?.billedFailedPercentage || 0
            ).toFixed(2) +
            "%)"
        );


        const byplFailed =
            Number(
                byplData?.hesFailedCount || 0
            );


        setValue(
            "byplHesDownload",
            Number(
                byplData?.hesDownloadCount || 0
            ).toLocaleString()
        );


        setValue(
            "byplDownloadPercentage",
            "(" +
            Number(
                byplData?.hesDownloadPercentage || 0
            ).toFixed(2) +
            "%)"
        );


        setValue(
            "byplDownloadFailed",
            byplFailed.toLocaleString()
        );


        setValue(
            "byplFailedPercentage",
            "(" +
            Number(
                byplData?.hesFailedPercentage || 0
            ).toFixed(2) +
            "%)"
        );


        setValue(
            "byplTotalMeters",
            Number(
                byplData?.totalMetersCount || 0
            ).toLocaleString()
        );


        setValue(
            "byplBilledMeters",
            Number(
                byplData?.billedCount || 0
            ).toLocaleString()
        );


        setValue(
            "byplBilledPercentage",
            "(" +
            Number(
                byplData?.billedPercentage || 0
            ).toFixed(2) +
            "%)"
        );


        setValue(
            "byplBilledFailed",
            Number(
                byplData?.billedFailedCount || 0
            ).toLocaleString()
        );


        setValue(
            "byplBilledFailedPercentage",
            "(" +
            Number(
                byplData?.billedFailedPercentage || 0
            ).toFixed(2) +
            "%)"
        );

    }
    catch (error) {

        console.error(
            "Meter MRO Summary Error:",
            error
        );

    }
}

$(document)
    .off("click.mroBrplFailed", "#downloadFailed")
    .on(
        "click.mroBrplFailed",
        "#downloadFailed",
        function (e) {

            e.preventDefault();

            const modalElement =
                document.getElementById(
                    "downloadSummaryModal"
                );

            if (!modalElement) {

                console.error(
                    "BRPL detail modal not found."
                );

                return;
            }


            const modal =
                bootstrap.Modal.getOrCreateInstance(
                    modalElement
                );

            modal.show();


            if (typeof loadDownloadSummary === "function") {

                loadDownloadSummary();

            }

        }
    );


$(document)
    .off("click.mroBrplBilledFailed", "#billedFailed")
    .on(
        "click.mroBrplBilledFailed",
        "#billedFailed",
        function (e) {

            e.preventDefault();

            const modalElement =
                document.getElementById(
                    "downloadSummaryModal"
                );

            if (!modalElement) {

                console.error(
                    "BRPL detail modal not found."
                );

                return;
            }


            const modal =
                bootstrap.Modal.getOrCreateInstance(
                    modalElement
                );

            modal.show();


            if (typeof loadDownloadSummary === "function") {

                loadDownloadSummary();

            }

        }
    );


$(document)
    .off("click.mroByplFailed", "#byplDownloadFailed")
    .on(
        "click.mroByplFailed",
        "#byplDownloadFailed",
        function (e) {

            e.preventDefault();

            const modalElement =
                document.getElementById(
                    "downloadSummaryModalBYPL"
                );

            if (!modalElement) {

                console.error(
                    "BYPL detail modal not found."
                );

                return;
            }


            const modal =
                bootstrap.Modal.getOrCreateInstance(
                    modalElement
                );

            modal.show();


            if (
                typeof loadDownloadSummaryBYPL ===
                "function"
            ) {

                loadDownloadSummaryBYPL();

            }

        }
    );


$(document)
    .off("click.mroByplBilledFailed", "#byplBilledFailed")
    .on(
        "click.mroByplBilledFailed",
        "#byplBilledFailed",
        function (e) {

            e.preventDefault();

            const modalElement =
                document.getElementById(
                    "downloadSummaryModalBYPL"
                );

            if (!modalElement) {

                console.error(
                    "BYPL detail modal not found."
                );

                return;
            }


            const modal =
                bootstrap.Modal.getOrCreateInstance(
                    modalElement
                );

            modal.show();


            if (
                typeof loadDownloadSummaryBYPL ===
                "function"
            ) {

                loadDownloadSummaryBYPL();

            }

        }
    );
