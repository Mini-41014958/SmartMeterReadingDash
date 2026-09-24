let allDownloadData = [];
let filteredData = [];


/* ============================================================
   OPEN DOWNLOAD SUMMARY MODAL
   ============================================================ */

$("#btnViewDownloadDetails").on("click", function () {

    const modal = new bootstrap.Modal(
        document.getElementById("downloadSummaryModal")
    );

    modal.show();

    loadDownloadSummary();
});


/* ============================================================
   LOAD DOWNLOAD SUMMARY
   ============================================================ */

async function loadDownloadSummary() {

    try {

        $("#downloadSummaryBody").html(`
            <tr>
                <td colspan="13" class="text-center py-4">
                    <div class="spinner-border spinner-border-sm text-primary"></div>
                    <span class="ms-2">Loading...</span>
                </td>
            </tr>
        `);

        const readingMonth = getReadingMonth();

        const apiUrl =
            `${getApiUrl("dashboardapi/meter-download-detailed-summary")}` +
            `?readingMonth=${encodeURIComponent(readingMonth)}`;

        const response = await fetch(apiUrl);

        if (!response.ok) {

            const errorText = await response.text();

            console.error(
                "API Error:",
                response.status,
                errorText
            );

            throw new Error(
                `Unable to load data (${response.status})`
            );
        }

        const result = await response.json();

        allDownloadData = Array.isArray(result)
            ? result
            : [];

        filteredData = [...allDownloadData];


        /* LOAD FILTERS */

        loadDepartmentFilter();
        loadDivisionFilter();
        loadReasonFilter();
        loadPhaseFilter();
        loadMeterMakeFilter();


        /* RESET FILTER VALUES */

        clearFilters();


        /* RENDER */

        renderTable(filteredData);

    }
    catch (err) {

        console.error(
            "loadDownloadSummary error:",
            err
        );

        $("#downloadSummaryBody").html(`
            <tr>
                <td colspan="13"
                    class="text-center text-danger py-4">

                    Failed to load data.

                    <br>

                    Error:
                    ${escapeHtml(err.message)}

                </td>
            </tr>
        `);
    }
}


/* ============================================================
   DEPARTMENT FILTER
   ============================================================ */

function loadDepartmentFilter() {

    const ddl = $("#departmentFilter");

    ddl.empty();

    ddl.append(`
        <option value="">
            All Departments
        </option>
    `);

    const departments = [
        ...new Set(
            allDownloadData
                .map(x => x.sapDepartment)
                .filter(
                    x =>
                        x !== null &&
                        x !== undefined &&
                        String(x).trim() !== ""
                )
                .map(x => String(x).trim())
        )
    ];

    departments.sort((a, b) =>
        a.localeCompare(
            b,
            undefined,
            {
                numeric: true
            }
        )
    );

    departments.forEach(department => {

        ddl.append(`
            <option value="${escapeHtml(department)}">
                ${escapeHtml(department)}
            </option>
        `);

    });
}


/* ============================================================
   DIVISION FILTER
   ============================================================ */

function loadDivisionFilter() {

    const ddl = $("#divisionFilter");

    ddl.empty();

    ddl.append(`
        <option value="">
            All Divisions
        </option>
    `);

    const divisions = [
        ...new Set(
            allDownloadData
                .map(x => x.sapDivision)
                .filter(
                    x =>
                        x !== null &&
                        x !== undefined &&
                        String(x).trim() !== ""
                )
                .map(x => String(x).trim())
        )
    ];

    divisions.sort((a, b) =>
        a.localeCompare(
            b,
            undefined,
            {
                numeric: true
            }
        )
    );

    divisions.forEach(division => {

        ddl.append(`
            <option value="${escapeHtml(division)}">
                ${escapeHtml(division)}
            </option>
        `);

    });
}


/* ============================================================
   REASON FILTER
   ============================================================ */

function loadReasonFilter() {

    const ddl = $("#reasonFilter");

    ddl.empty();

    ddl.append(`
        <option value="">
            All Reasons
        </option>

        <option value="SYSTEM TITLE">
            System Title Mismatch
        </option>

        <option value="TCP">
            TCP Connection Failed
        </option>

        <option value="NO DATA">
            No Data Found ODR
        </option>

        <option value="TIMEOUT">
            Timeout
        </option>

        <option value="OTHER">
            Other
        </option>
    `);
}


/* ============================================================
   PHASE FILTER
   ============================================================ */

function loadPhaseFilter() {

    const ddl = $("#phaseFilter");

    ddl.empty();

    ddl.append(`
        <option value="">
            All Phases
        </option>
    `);

    const phases = [
        ...new Set(
            allDownloadData
                .map(x => x.phase)
                .filter(
                    x =>
                        x !== null &&
                        x !== undefined &&
                        String(x).trim() !== ""
                )
                .map(x => String(x).trim())
        )
    ];

    phases.sort((a, b) =>
        a.localeCompare(
            b,
            undefined,
            {
                numeric: true
            }
        )
    );

    phases.forEach(phase => {

        ddl.append(`
            <option value="${escapeHtml(phase)}">
                ${escapeHtml(phase)}
            </option>
        `);

    });
}


/* ============================================================
   METER MAKE FILTER
   ============================================================ */

function loadMeterMakeFilter() {

    const ddl = $("#meterMakeFilter");

    ddl.empty();

    ddl.append(`
        <option value="">
            All Meter Makes
        </option>
    `);

    const meterMakes = [
        ...new Set(
            allDownloadData
                .map(x => x.meterType)
                .filter(
                    x =>
                        x !== null &&
                        x !== undefined &&
                        String(x).trim() !== ""
                )
                .map(x => String(x).trim())
        )
    ];

    meterMakes.sort((a, b) =>
        a.localeCompare(
            b,
            undefined,
            {
                numeric: true
            }
        )
    );

    meterMakes.forEach(make => {

        ddl.append(`
            <option value="${escapeHtml(make)}">
                ${escapeHtml(make)}
            </option>
        `);

    });
}


/* ============================================================
   FILTER EVENTS
   ============================================================ */

$("#departmentFilter").on(
    "change",
    applyFilters
);

$("#divisionFilter").on(
    "change",
    applyFilters
);

$("#reasonFilter").on(
    "change",
    applyFilters
);

$("#phaseFilter").on(
    "change",
    applyFilters
);

$("#meterMakeFilter").on(
    "change",
    applyFilters
);

$("#entryDateFrom").on(
    "change",
    applyFilters
);

$("#entryDateTo").on(
    "change",
    applyFilters
);


/* ============================================================
   CLEAR FILTER BUTTON
   ============================================================ */

$("#btnClearDownloadFilters").on(
    "click",
    function () {

        clearFilters();

        filteredData = [
            ...allDownloadData
        ];

        renderTable(filteredData);
    }
);


/* ============================================================
   CLEAR FILTERS
   ============================================================ */

function clearFilters() {

    $("#departmentFilter").val("");

    $("#divisionFilter").val("");

    $("#reasonFilter").val("");

    $("#phaseFilter").val("");

    $("#meterMakeFilter").val("");

    $("#entryDateFrom").val("");

    $("#entryDateTo").val("");

    $("#entryDateTo").removeClass(
        "is-invalid"
    );
}


/* ============================================================
   APPLY FILTERS
   ============================================================ */

function applyFilters() {

    const department =
        String(
            $("#departmentFilter").val() || ""
        ).trim().toUpperCase();

    const division =
        String(
            $("#divisionFilter").val() || ""
        ).trim().toUpperCase();

    const reason =
        String(
            $("#reasonFilter").val() || ""
        ).trim().toUpperCase();

    const phase =
        String(
            $("#phaseFilter").val() || ""
        ).trim().toUpperCase();

    const meterMake =
        String(
            $("#meterMakeFilter").val() || ""
        ).trim().toUpperCase();

    const dateFrom =
        $("#entryDateFrom").val();

    const dateTo =
        $("#entryDateTo").val();


    /* DATE VALIDATION */

    if (
        dateFrom &&
        dateTo &&
        dateFrom > dateTo
    ) {

        $("#entryDateTo").addClass(
            "is-invalid"
        );

        filteredData = [];

        renderTable(filteredData);

        return;
    }

    $("#entryDateTo").removeClass(
        "is-invalid"
    );


    /* FILTER DATA */

    filteredData = allDownloadData.filter(
        item => {

            /* --------------------------------
               DEPARTMENT
            -------------------------------- */

            const itemDepartment =
                String(
                    item.sapDepartment || ""
                )
                    .trim()
                    .toUpperCase();

            const departmentMatch =
                department === "" ||
                itemDepartment === department;


            /* --------------------------------
               DIVISION
            -------------------------------- */

            const itemDivision =
                String(
                    item.sapDivision || ""
                )
                    .trim()
                    .toUpperCase();

            const divisionMatch =
                division === "" ||
                itemDivision === division;


            /* --------------------------------
               FAILED REASON
            -------------------------------- */

            const message =
                String(
                    item.schedulerMessage || ""
                )
                    .trim()
                    .toUpperCase();

            let reasonMatch = true;

            if (reason !== "") {

                if (reason === "OTHER") {

                    reasonMatch =
                        !message.includes("SYSTEM TITLE") &&
                        !message.includes("TCP") &&
                        !message.includes("NO DATA") &&
                        !message.includes("TIMEOUT");

                }
                else {

                    reasonMatch =
                        message.includes(reason);

                }
            }


            /* --------------------------------
               PHASE
            -------------------------------- */

            const itemPhase =
                String(
                    item.phase || ""
                )
                    .trim()
                    .toUpperCase();

            const phaseMatch =
                phase === "" ||
                itemPhase === phase;


            /* --------------------------------
               METER MAKE
            -------------------------------- */

            const itemMeterMake =
                String(
                    item.meterType || ""
                )
                    .trim()
                    .toUpperCase();

            const meterMakeMatch =
                meterMake === "" ||
                itemMeterMake === meterMake;


            /* --------------------------------
               ENTRY DATE
            -------------------------------- */

            let dateMatch = true;

            if (
                dateFrom ||
                dateTo
            ) {

                if (!item.entryDate) {

                    dateMatch = false;

                }
                else {

                    const entryDate =
                        new Date(
                            item.entryDate
                        );

                    if (
                        isNaN(
                            entryDate.getTime()
                        )
                    ) {

                        dateMatch = false;

                    }
                    else {

                        const entryDateString =
                            formatDateForFilter(
                                item.entryDate
                            );

                        if (
                            dateFrom &&
                            entryDateString < dateFrom
                        ) {

                            dateMatch = false;
                        }

                        if (
                            dateTo &&
                            entryDateString > dateTo
                        ) {

                            dateMatch = false;
                        }
                    }
                }
            }


            return (
                departmentMatch &&
                divisionMatch &&
                reasonMatch &&
                phaseMatch &&
                meterMakeMatch &&
                dateMatch
            );
        }
    );


    renderTable(
        filteredData
    );
}


function renderTable(data)
{

    const tbody = $("#downloadSummaryBody");

    tbody.empty();


    /* =========================================================
       NO DATA
       ========================================================= */

    if (!data || data.length === 0) {

        tbody.html(`
            <tr>
                <td colspan="13"
                    class="text-center text-muted py-4">

                    <i class="bi bi-inbox me-1"></i>
                    No Records Found

                </td>
            </tr>
        `);

        return;
    }


    /* =========================================================
       RENDER ROWS
       ========================================================= */

    data.forEach((x, index) => {


        /* =====================================================
           FAILED SINCE
           ===================================================== */

        let downloadFailedSince = "--";

        if (x.downloadFailedSince) {

            const failedSinceDate =
                new Date(x.downloadFailedSince);

            if (!isNaN(failedSinceDate.getTime())) {

                downloadFailedSince =
                    failedSinceDate.toLocaleDateString(
                        "en-GB"
                    );
            }
        }


        /* =====================================================
           TOTAL FAILED DAYS
           ===================================================== */

        let downloadFailedDays = "--";

        if (
            x.downloadFailedDays !== null &&
            x.downloadFailedDays !== undefined &&
            String(x.downloadFailedDays).trim() !== ""
        ) {

            const numericDays =
                Number(x.downloadFailedDays);

            downloadFailedDays =
                isNaN(numericDays)
                    ? String(x.downloadFailedDays)
                    : numericDays;
        }


        /* =====================================================
           FAILED DAYS NUMBER
           ===================================================== */

        const failedDaysNumber =
            Number(x.downloadFailedDays);

        const isMoreThan5Days =
            !isNaN(failedDaysNumber) &&
            failedDaysNumber > 5;


        /* =====================================================
           FAILED SINCE / FAILED DAYS CLASS
           ===================================================== */

        const failedSinceClass =
            isMoreThan5Days
                ? "download-failed-danger"
                : "download-failed-normal";

        const failedDaysClass =
            isMoreThan5Days
                ? "download-failed-danger"
                : "download-failed-normal";


        /* =====================================================
           STATUS
           ===================================================== */

        const message =
            String(x.schedulerMessage || "")
                .trim()
                .toUpperCase();

        let badgeClass =
            "download-status-other";


        if (message.includes("SYSTEM TITLE")) {

            badgeClass =
                "download-status-system";

        }
        else if (message.includes("TCP")) {

            badgeClass =
                "download-status-tcp";

        }
        else if (message.includes("NO DATA")) {

            badgeClass =
                "download-status-nodata";

        }
        else if (message.includes("TIMEOUT")) {

            badgeClass =
                "download-status-timeout";
        }


        /* =====================================================
           LAST ENTRY DATE
           ===================================================== */

        let entryDate = "--";

        if (x.entryDate) {

            const parsedDate =
                new Date(x.entryDate);

            if (!isNaN(parsedDate.getTime())) {

                entryDate =
                    parsedDate.toLocaleString(
                        "en-GB",
                        {
                            day: "2-digit",
                            month: "2-digit",
                            year: "numeric",
                            hour: "2-digit",
                            minute: "2-digit"
                        }
                    );
            }
        }


        /* =====================================================
           TABLE ROW

           1  S.No.
           2  Cons Ref
           3  Meter Number
           4  Phase
           5  Department
           6  Division
           7  Seq No
           8  Meter Make
           9  Failed Since
           10 Total Failed Days
           11 Status
           12 Last Entry Date
           13 Address
           ===================================================== */

        tbody.append(`
            <tr>

                <!-- 1. S.No. -->
                <td class="text-center fw-semibold">
                    ${index + 1}
                </td>


                <!-- 2. Cons Ref -->
                <td>
                    ${escapeHtml(x.consRef ?? "--")}
                </td>


                <!-- 3. Meter Number -->
                <td class="meter-number-cell">
                    ${escapeHtml(x.meterNumber ?? "--")}
                </td>


                <!-- 4. Phase -->
                <td class="text-center">
                    ${escapeHtml(x.phase ?? "--")}
                </td>


                <!-- 5. Department -->
                <td>
                    ${escapeHtml(x.sapDepartment ?? "--")}
                </td>


                <!-- 6. Division -->
                <td>
                    ${escapeHtml(x.sapDivision ?? "--")}
                </td>


                <!-- 7. Seq No -->
                <td class="text-center">
                    ${escapeHtml(x.sapSeqNo ?? "--")}
                </td>


                <!-- 8. Meter Make -->
                <td class="meter-make-cell">
                    ${escapeHtml(x.meterType ?? "--")}
                </td>


                <!-- 9. Failed Since -->
                <td class="${failedSinceClass} text-center">
                    ${escapeHtml(downloadFailedSince)}
                </td>


                <!-- 10. Total Failed Days -->
                <td class="${failedDaysClass} text-center">
                    ${escapeHtml(downloadFailedDays)}
                </td>


                <!-- 11. Status -->
                <td class="status-cell">

                    <span class="download-status-badge ${badgeClass}">
                        ${escapeHtml(
            x.schedulerMessage ?? "--"
        )}
                    </span>

                </td>


                <!-- 12. Last Entry Date -->
                <td class="entry-date-cell text-center">
                    ${escapeHtml(entryDate)}
                </td>


                <!-- 13. Address -->
                <td class="address-cell">
                    ${escapeHtml(x.address ?? "--")}
                </td>

            </tr>
        `);

    });
}


/* ============================================================
   EXPORT TO EXCEL
   ============================================================ */

function exportTableToExcel() {

    const table = document.getElementById("downloadSummaryTable");

    if (!table) {
        console.error("Download summary table not found.");
        return;
    }


    const tbody = table.querySelector("tbody");

    if (!tbody || tbody.rows.length === 0) {

        alert("No data available to export.");

        return;
    }


    /* =========================================================
       CHECK FOR NO RECORDS
       ========================================================= */

    if (
        tbody.rows.length === 1 &&
        tbody.rows[0].innerText
            .toLowerCase()
            .includes("no records found")
    ) {

        alert("No data available to export.");

        return;
    }


    /* =========================================================
       EXCEL DATA
       COLUMN ORDER MUST MATCH TABLE
       ========================================================= */

    const excelData = [];


    /* =========================================================
       HEADER
       ========================================================= */

    excelData.push([
        "S.No.",
        "Cons Ref",
        "Meter Number",
        "Phase",
        "Department",
        "Division",
        "Seq No",
        "Meter Make",
        "Failed Since",
        "Total Failed Days",
        "Status",
        "Last Entry Date",
        "Address"
    ]);


    /* =========================================================
       BODY
       ========================================================= */

    Array.from(tbody.rows).forEach((row) => {

        const cells = row.querySelectorAll("td");

        if (cells.length < 13) {
            return;
        }


        excelData.push([

            /* 1. S.No. */
            cells[0].innerText.trim(),

            /* 2. Cons Ref */
            cells[1].innerText.trim(),

            /* 3. Meter Number */
            cells[2].innerText.trim(),

            /* 4. Phase */
            cells[3].innerText.trim(),

            /* 5. Department */
            cells[4].innerText.trim(),

            /* 6. Division */
            cells[5].innerText.trim(),

            /* 7. Seq No */
            cells[6].innerText.trim(),

            /* 8. Meter Make */
            cells[7].innerText.trim(),

            /* 9. Failed Since */
            cells[8].innerText.trim(),

            /* 10. Total Failed Days */
            cells[9].innerText.trim(),

            /* 11. Status */
            cells[10].innerText.trim(),

            /* 12. Last Entry Date */
            cells[11].innerText.trim(),

            /* 13. Address */
            cells[12].innerText.trim()
        ]);

    });


    /* =========================================================
       CREATE WORKSHEET
       ========================================================= */

    const worksheet =
        XLSX.utils.aoa_to_sheet(excelData);


    /* =========================================================
       COLUMN WIDTHS
       ========================================================= */

    worksheet["!cols"] = [

        { wch: 7 },      // S.No.
        { wch: 18 },     // Cons Ref
        { wch: 20 },     // Meter Number
        { wch: 10 },     // Phase
        { wch: 18 },     // Department
        { wch: 18 },     // Division
        { wch: 10 },     // Seq No
        { wch: 18 },     // Meter Make
        { wch: 16 },     // Failed Since
        { wch: 20 },     // Total Failed Days
        { wch: 40 },     // Status
        { wch: 22 },     // Last Entry Date
        { wch: 45 }      // Address

    ];


    /* =========================================================
       HEADER STYLE
       ========================================================= */

    const headerRange =
        XLSX.utils.decode_range(
            worksheet["!ref"]
        );


    for (
        let col = headerRange.s.c;
        col <= headerRange.e.c;
        col++
    ) {

        const cellAddress =
            XLSX.utils.encode_cell({
                r: 0,
                c: col
            });

        const cell =
            worksheet[cellAddress];


        if (!cell) {
            continue;
        }


        cell.s = {

            font: {
                bold: true,
                color: {
                    rgb: "FFFFFF"
                }
            },

            fill: {
                fgColor: {
                    rgb: "1F4E78"
                }
            },

            alignment: {
                horizontal: "center",
                vertical: "center",
                wrapText: true
            },

            border: {

                top: {
                    style: "thin",
                    color: {
                        rgb: "000000"
                    }
                },

                bottom: {
                    style: "thin",
                    color: {
                        rgb: "000000"
                    }
                },

                left: {
                    style: "thin",
                    color: {
                        rgb: "000000"
                    }
                },

                right: {
                    style: "thin",
                    color: {
                        rgb: "000000"
                    }
                }
            }
        };
    }


    /* =========================================================
       BODY STYLE
       ========================================================= */

    for (
        let row = 1;
        row <= headerRange.e.r;
        row++
    ) {

        for (
            let col = 0;
            col <= headerRange.e.c;
            col++
        ) {

            const cellAddress =
                XLSX.utils.encode_cell({
                    r: row,
                    c: col
                });

            const cell =
                worksheet[cellAddress];


            if (!cell) {
                continue;
            }


            cell.s = {

                alignment: {
                    vertical: "top",
                    wrapText: true
                },

                border: {

                    top: {
                        style: "thin",
                        color: {
                            rgb: "D9D9D9"
                        }
                    },

                    bottom: {
                        style: "thin",
                        color: {
                            rgb: "D9D9D9"
                        }
                    },

                    left: {
                        style: "thin",
                        color: {
                            rgb: "D9D9D9"
                        }
                    },

                    right: {
                        style: "thin",
                        color: {
                            rgb: "D9D9D9"
                        }
                    }
                }
            };


            /* Center specific columns */

            if (
                col === 0 ||
                col === 3 ||
                col === 6 ||
                col === 8 ||
                col === 9 ||
                col === 11
            ) {

                cell.s.alignment.horizontal =
                    "center";
            }
        }
    }


    /* =========================================================
       WORKBOOK
       ========================================================= */

    const workbook =
        XLSX.utils.book_new();


    XLSX.utils.book_append_sheet(
        workbook,
        worksheet,
        "Failed Summary"
    );


    worksheet["!freeze"] = {
        xSplit: 0,
        ySplit: 1
    };


    /* =========================================================
       DOWNLOAD
       ========================================================= */

    const now =
        new Date();

    const timestamp =
        now.getFullYear() +
        String(now.getMonth() + 1).padStart(2, "0") +
        String(now.getDate()).padStart(2, "0") +
        "_" +
        String(now.getHours()).padStart(2, "0") +
        String(now.getMinutes()).padStart(2, "0") +
        String(now.getSeconds()).padStart(2, "0");


    const fileName =
        `Download_Failed_Summary_${timestamp}.xlsx`;


    XLSX.writeFile(
        workbook,
        fileName
    );
}


/* ============================================================
   EXPORT TO CSV
   ============================================================ */

function exportTableToCSV() {

    let csv = [];


    document
        .querySelectorAll(
            "#downloadSummaryTable tr"
        )
        .forEach(
            row => {

                if (
                    row.style.display ===
                    "none"
                ) {

                    return;
                }


                const cols =
                    row.querySelectorAll(
                        "th, td"
                    );


                let data = [];


                cols.forEach(
                    col => {

                        const value =
                            col.innerText
                                .replace(
                                    /"/g,
                                    '""'
                                )
                                .replace(
                                    /\r?\n|\r/g,
                                    " "
                                )
                                .trim();


                        data.push(
                            `"${value}"`
                        );
                    }
                );


                csv.push(
                    data.join(",")
                );
            }
        );


    const blob =
        new Blob(
            [
                csv.join("\n")
            ],
            {
                type:
                    "text/csv;charset=utf-8;"
            }
        );


    const link =
        document.createElement(
            "a"
        );


    const url =
        URL.createObjectURL(
            blob
        );


    link.href = url;


    const today =
        new Date()
            .toISOString()
            .split("T")[0];


    link.download =
        "Download_Failed_Summary_BRPL_" +
        today +
        ".csv";


    document.body.appendChild(
        link
    );


    link.click();


    document.body.removeChild(
        link
    );


    URL.revokeObjectURL(
        url
    );
}


/* ============================================================
   ESCAPE HTML
   ============================================================ */

function escapeHtml(value) {

    return String(value)

        .replace(
            /&/g,
            "&amp;"
        )

        .replace(
            /</g,
            "&lt;"
        )

        .replace(
            />/g,
            "&gt;"
        )

        .replace(
            /"/g,
            "&quot;"
        )

        .replace(
            /'/g,
            "&#039;"
        );
}


/* ============================================================
   FORMAT DATE FOR FILTER
   ============================================================ */

function formatDateForFilter(value) {

    if (!value) {

        return "";
    }


    const date =
        new Date(value);


    if (
        isNaN(
            date.getTime()
        )
    ) {

        return "";
    }


    const year =
        date.getFullYear();


    const month =
        String(
            date.getMonth() + 1
        ).padStart(
            2,
            "0"
        );


    const day =
        String(
            date.getDate()
        ).padStart(
            2,
            "0"
        );


    return `${year}-${month}-${day}`;
}