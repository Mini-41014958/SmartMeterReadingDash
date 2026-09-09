function getApiUrl(endpoint) {

    const basePath = window.location.pathname
        .toLowerCase()
        .startsWith("/smartmeter/")
        ? "/SmartMeter"
        : "";

    return `${basePath}/api/${endpoint}`;
}
async function loadMeterDownloadSummary() {

    const month =
        getReadingMonth();


    try {

        // =====================================================
        // BRPL
        // =====================================================

        if (
            canAccessCompany("BRPL")
        ) {

            const response =
                await fetch(
                    `${getApiUrl(
                        "dashboardapi/meter-download-summary"
                    )}?readingMonth=${encodeURIComponent(month)}`,
                    {
                        cache: "no-store"
                    }
                );


            if (!response.ok) {

                throw new Error(
                    `Failed to load BRPL Meter Summary. Status: ${response.status}`
                );
            }


            const result =
                await response.json();


            const data =
                Array.isArray(result)
                    ? result[0] || {}
                    : result || {};


            const failed =
                Number(
                    data.manualForwardinCount || 0
                ) +
                Number(
                    data.pendingCount || 0
                ) +
                Number(
                    data.mismatchCount || 0
                );


            setMroValue(
                "hesDownload",
                data.hesDownloadCount
            );


            setMroPercentage(
                "downloadPercentage",
                data.hesDownloadPercentage
            );


            setMroValue(
                "downloadFailed",
                failed
            );


            setMroPercentage(
                "failedPercentage",
                data.hesFailedPercentage
            );


            setMroValue(
                "totalMeters",
                data.totalMetersCount
            );


            setMroValue(
                "billedMeters",
                data.billedCount
            );


            setMroPercentage(
                "billedPercentage",
                data.billedPercentage
            );


            setMroValue(
                "billedFailed",
                data.billedFailedCount
            );


            setMroPercentage(
                "billedFailedPercentage",
                data.billedFailedPercentage
            );
        }


        if (
            canAccessCompany("BYPL")
        ) {

            const response =
                await fetch(
                    `${getApiUrl(
                        "dashboardapi/meter-download-summary-bypl"
                    )}?readingMonth=${encodeURIComponent(month)}`,
                    {
                        cache: "no-store"
                    }
                );


            if (!response.ok) {

                throw new Error(
                    `Failed to load BYPL Meter Summary. Status: ${response.status}`
                );
            }


            const result =
                await response.json();


            const data =
                Array.isArray(result)
                    ? result[0] || {}
                    : result || {};


            setMroValue(
                "byplHesDownload",
                data.hesDownloadCount
            );


            setMroPercentage(
                "byplDownloadPercentage",
                data.hesDownloadPercentage
            );


            setMroValue(
                "byplDownloadFailed",
                data.hesFailedCount
            );


            setMroPercentage(
                "byplFailedPercentage",
                data.hesFailedPercentage
            );


            setMroValue(
                "byplTotalMeters",
                data.totalMetersCount
            );


            setMroValue(
                "byplBilledMeters",
                data.billedCount
            );


            setMroPercentage(
                "byplBilledPercentage",
                data.billedPercentage
            );


            setMroValue(
                "byplBilledFailed",
                data.billedFailedCount
            );


            setMroPercentage(
                "byplBilledFailedPercentage",
                data.billedFailedPercentage
            );
        }

    }
    catch (error) {

        console.error(
            "Meter MRO Summary Error:",
            error
        );

    }
}


// =============================================================
// MRO VALUE HELPER
// =============================================================

function setMroValue(id, value) {

    const element =
        document.getElementById(id);

    if (!element) {
        return;
    }

    element.textContent =
        Number(value || 0)
            .toLocaleString();
}


// =============================================================
// MRO PERCENTAGE HELPER
// =============================================================

function setMroPercentage(id, value) {

    const element =
        document.getElementById(id);

    if (!element) {
        return;
    }

    element.textContent =
        `(${Number(value || 0).toFixed(2)}%)`;
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
