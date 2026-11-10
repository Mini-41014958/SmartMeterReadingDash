
let allCmriDownloadData = [];
let filteredCmriDownloadData = [];

/* ============================================================
   NORMALIZE CMRI RECORD
   ============================================================ */

function normalizeCmriRecord(item) {

    const value = (...keys) => {

        for (const key of keys) {

            if (
                item &&
                item[key] !== null &&
                item[key] !== undefined
            ) {
                return item[key];
            }
        }

        return "";
    };

    return {
        ...item,

        cons_ref: value(
            "cons_ref",
            "consRef",
            "consRefNo"
        ),

        meterno: value(
            "meterno",
            "meterNumber",
            "meter_no",
            "meterNo"
        ),

        department: value(
            "department",
            "Department",
            "sapDepartment",
            "sap_department",
            "SAP_DEPARTMENT"
        ),

        cycle: value(
            "cycle",
            "Cycle"
        ),

        division: value(
            "division",
            "Division",
            "sapDivision",
            "sap_division",
            "SAP_DIVISION"
        ),

        sap_seq_no: value(
            "sap_seq_no",
            "sapSeqNo",
            "seq_no",
            "seqNo",
            "SapSeqNo"
        ),

        address: value(
            "address",
            "Address"
        ),

        kwh: value(
            "KWH",
            "Kwh",
            "kwh"
        ),

        kvah: value(
            "KVAH",
            "Kvah",
            "kvah"
        ),

        MeterType: value(
            "METER_TYPE",
            "MeterType",
            "meterType",
            "Meter_Make"
        ),

        Phase: value(
            "PHASE_TYPE",
            "Phase",
            "phase",
            "PhaseType"
        ),

        ReadingDate: value(
            "READING_DATE",
            "ReadingDate",
            "readingDate",
            "reading_date"
        )
    };
}


/* ============================================================
   OPEN CMRI DOWNLOAD MODAL
   ============================================================ */

$("#cmriDownload").on("click", function (e) {

    e.preventDefault();

    const modalElement = document.getElementById(
        "downloadCmriSummaryModal"
    );

    if (!modalElement) {

        console.error(
            "CMRI modal not found: downloadCmriSummaryModal"
        );

        return;
    }

    const modal = bootstrap.Modal.getOrCreateInstance(
        modalElement
    );

    modal.show();

    loadCmriDownloadSummary();
});


/* ============================================================
   LOAD CMRI DOWNLOAD SUMMARY
   ============================================================ */

async function loadCmriDownloadSummary() {

    const tbody = $("#cmriDownloadSummaryBody");

    try {

        tbody.html(`
            <tr>
                <td colspan="13" class="text-center py-4">
                    <div class="spinner-border spinner-border-sm text-primary"></div>
                    <span class="ms-2">Loading CMRI data...</span>
                </td>
            </tr>
        `);

        const readingMonth = String(
            getReadingMonth() || ""
        ).trim();

        $("#currentMonth").text(
            readingMonth || "Current Month"
        );

        if (!readingMonth) {

            throw new Error(
                "Reading month is empty. Please select a reading month."
            );
        }

        const apiUrl =
            `${getApiUrl("dashboardapi/cmri-downloads")}` +
            `?readingMonth=${encodeURIComponent(readingMonth)}`;

        console.log("CMRI API URL:", apiUrl);

        const response = await fetch(apiUrl, {
            method: "GET",
            headers: {
                "Accept": "application/json"
            },
            credentials: "same-origin"
        });

        if (!response.ok) {

            const errorText = await response.text();

            console.error(
                "CMRI API Error:",
                response.status,
                errorText
            );

            throw new Error(
                `Unable to load CMRI data. HTTP ${response.status}`
            );
        }

        const result = await response.json();

        console.log(
            "CMRI API Response:",
            result
        );

        let records = [];

        if (Array.isArray(result)) {

            records = result;

        } else if (Array.isArray(result?.data)) {

            records = result.data;

        } else if (Array.isArray(result?.result)) {

            records = result.result;

        } else if (Array.isArray(result?.items)) {

            records = result.items;

        } else if (Array.isArray(result?.Data)) {

            records = result.Data;

        } else if (Array.isArray(result?.Result)) {

            records = result.Result;
        }

        allCmriDownloadData = records.map(
            normalizeCmriRecord
        );

        filteredCmriDownloadData = [
            ...allCmriDownloadData
        ];

        console.log(
            "Total CMRI records:",
            allCmriDownloadData.length
        );

        console.log(
            "First normalized record:",
            allCmriDownloadData[0]
        );

        loadCmriDepartmentFilter();

        loadCmriDivisionFilter();

        loadCmriCycleFilter();

        clearCmriDownloadFilters();

        filteredCmriDownloadData = [
            ...allCmriDownloadData
        ];

        renderCmriDownloadTable(
            filteredCmriDownloadData
        );

        updateCmriRecordCount(
            filteredCmriDownloadData.length
        );

    }
    catch (err) {

        console.error(
            "loadCmriDownloadSummary error:",
            err
        );

        allCmriDownloadData = [];

        filteredCmriDownloadData = [];

        tbody.html(`
            <tr>
                <td colspan="13"
                    class="text-center text-danger py-4">
                    <i class="bi bi-exclamation-triangle me-1"></i>
                    Failed to load CMRI data.
                    <br>
                    ${escapeHtml(err.message)}
                </td>
            </tr>
        `);

        updateCmriRecordCount(0);
    }
}


/* ============================================================
   RECORD COUNT
   ============================================================ */

function updateCmriRecordCount(count) {

    const elements = [
        "cmriDownloadRecordCount",
        "cmriTotalRecords"
    ];

    elements.forEach(id => {

        const element = document.getElementById(id);

        if (element) {
            element.textContent =
                Number(count).toLocaleString("en-IN");
        }
    });
}


/* ============================================================
   DEPARTMENT FILTER
   ============================================================ */

function loadCmriDepartmentFilter() {

    const ddl = $("#cmriDepartmentFilter");

    if (!ddl.length) {
        return;
    }

    ddl.empty();

    ddl.append(
        $("<option>", {
            value: "",
            text: "All Departments"
        })
    );

    const departments = [
        ...new Set(
            allCmriDownloadData
                .map(x => String(x.department ?? "").trim())
                .filter(Boolean)
        )
    ];

    departments.sort((a, b) =>
        a.localeCompare(b, undefined, {
            numeric: true,
            sensitivity: "base"
        })
    );

    departments.forEach(department => {

        ddl.append(
            $("<option>", {
                value: department,
                text: department
            })
        );

    });
}


/* ============================================================
   DIVISION FILTER
   ============================================================ */

function loadCmriDivisionFilter() {

    const ddl = $("#cmriDivisionFilter");

    if (!ddl.length) {
        return;
    }

    ddl.empty();

    ddl.append(
        $("<option>", {
            value: "",
            text: "All Divisions"
        })
    );

    const divisions = [
        ...new Set(
            allCmriDownloadData
                .map(x => String(x.division ?? "").trim())
                .filter(Boolean)
        )
    ];

    divisions.sort((a, b) =>
        a.localeCompare(b, undefined, {
            numeric: true,
            sensitivity: "base"
        })
    );

    divisions.forEach(division => {

        ddl.append(
            $("<option>", {
                value: division,
                text: division
            })
        );

    });
}


/* ============================================================
   CYCLE FILTER
   DEPENDS ON DEPARTMENT AND DIVISION
   ============================================================ */

function loadCmriCycleFilter() {

    const ddl = $("#cmriCycleFilter");

    if (!ddl.length) {
        return;
    }

    const previousCycle = String(
        ddl.val() || ""
    ).trim().toUpperCase();

    const selectedDivision = String(
        $("#cmriDivisionFilter").val() || ""
    ).trim().toUpperCase();

    const selectedDepartment = String(
        $("#cmriDepartmentFilter").val() || ""
    ).trim().toUpperCase();

    ddl.empty();

    ddl.append(
        $("<option>", {
            value: "",
            text: "All Cycles"
        })
    );

    let sourceData = allCmriDownloadData;

    if (selectedDepartment !== "") {

        sourceData = sourceData.filter(item => {

            return String(
                item.department ?? ""
            ).trim().toUpperCase() === selectedDepartment;

        });
    }

    if (selectedDivision !== "") {

        sourceData = sourceData.filter(item => {

            return String(
                item.division ?? ""
            ).trim().toUpperCase() === selectedDivision;

        });
    }

    const cycles = [
        ...new Set(
            sourceData
                .map(item =>
                    String(item.cycle ?? "")
                        .trim()
                        .toUpperCase()
                )
                .filter(Boolean)
        )
    ];

    cycles.sort((a, b) =>
        a.localeCompare(b, undefined, {
            numeric: true,
            sensitivity: "base"
        })
    );

    cycles.forEach(cycle => {

        ddl.append(
            $("<option>", {
                value: cycle,
                text: cycle
            })
        );

    });

    if (cycles.includes(previousCycle)) {

        ddl.val(previousCycle);

    } else {

        ddl.val("");
    }
}


/* ============================================================
   FILTER EVENTS
   ============================================================ */

$("#cmriDepartmentFilter").on(
    "change",
    function () {

        loadCmriCycleFilter();

        applyCmriDownloadFilters();
    }
);


$("#cmriDivisionFilter").on(
    "change",
    function () {

        loadCmriCycleFilter();

        applyCmriDownloadFilters();
    }
);


$("#cmriCycleFilter").on(
    "change",
    function () {

        applyCmriDownloadFilters();
    }
);


$("#btnClearCmriDownloadFilters").on(
    "click",
    function (e) {

        e.preventDefault();

        clearCmriDownloadFilters();

        applyCmriDownloadFilters();
    }
);


/* ============================================================
   CLEAR FILTERS
   ============================================================ */

function clearCmriDownloadFilters() {

    $("#cmriDepartmentFilter").val("");

    $("#cmriDivisionFilter").val("");

    $("#cmriCycleFilter").val("");

    loadCmriCycleFilter();
}


/* ============================================================
   APPLY FILTERS
   ============================================================ */

function applyCmriDownloadFilters() {

    const department = String(
        $("#cmriDepartmentFilter").val() || ""
    ).trim().toUpperCase();

    const division = String(
        $("#cmriDivisionFilter").val() || ""
    ).trim().toUpperCase();

    const cycle = String(
        $("#cmriCycleFilter").val() || ""
    ).trim().toUpperCase();

    filteredCmriDownloadData =
        allCmriDownloadData.filter(item => {

            const itemDepartment = String(
                item.department ?? ""
            ).trim().toUpperCase();

            const itemDivision = String(
                item.division ?? ""
            ).trim().toUpperCase();

            const itemCycle = String(
                item.cycle ?? ""
            ).trim().toUpperCase();

            return (
                (!department || itemDepartment === department) &&
                (!division || itemDivision === division) &&
                (!cycle || itemCycle === cycle)
            );
        });

    renderCmriDownloadTable(
        filteredCmriDownloadData
    );

    updateCmriRecordCount(
        filteredCmriDownloadData.length
    );
}


/* ============================================================
   FORMAT READING DATE
   ============================================================ */

function formatCmriReadingDate(value) {

    if (!value) {
        return "--";
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
        return String(value);
    }

    const day = String(date.getDate()).padStart(2, "0");

    const month = String(date.getMonth() + 1).padStart(2, "0");

    const year = date.getFullYear();

    return `${day}-${month}-${year}`;
}


/* ============================================================
   RENDER CMRI DOWNLOAD TABLE
   EXACTLY 13 COLUMNS
   ============================================================ */

function renderCmriDownloadTable(data) {

    const tbody = $("#cmriDownloadSummaryBody");

    if (!tbody.length) {

        console.error(
            "CMRI table body not found: cmriDownloadSummaryBody"
        );

        return;
    }

    tbody.empty();

    if (!Array.isArray(data) || data.length === 0) {

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

    const rowsHtml = data.map((x, index) => {

        return `
            <tr>
                <td class="text-center fw-semibold">
                    ${index + 1}
                </td>

                <td>
                    ${escapeHtml(x.cons_ref || "--")}
                </td>

                <td class="meter-number-cell">
                    ${escapeHtml(x.meterno || "--")}
                </td>

                <td class="text-end">
                    ${escapeHtml(x.kwh || "--")}
                </td>

                <td class="text-end">
                    ${escapeHtml(x.kvah || "--")}
                </td>

                <td class="text-center">
                    ${escapeHtml(formatCmriReadingDate(x.ReadingDate))}
                </td>

                <td>
                    ${escapeHtml(x.department || "--")}
                </td>

                <td class="text-center">
                    ${escapeHtml(x.Phase || "--")}
                </td>

                <td>
                    ${escapeHtml(x.MeterType || "--")}
                </td>

                <td class="text-center">
                    ${escapeHtml(x.cycle || "--")}
                </td>

                <td>
                    ${escapeHtml(x.division || "--")}
                </td>

                <td class="text-center">
                    ${escapeHtml(x.sap_seq_no || "--")}
                </td>

                <td class="address-cell">
                    ${escapeHtml(x.address || "--")}
                </td>
            </tr>
        `;

    }).join("");

    tbody.html(rowsHtml);
}


/* ============================================================
   COMMON EXPORT HEADERS
   ============================================================ */

function getCmriExportHeaders() {

    return [
        "S.No.",
        "Cons Ref",
        "Meter Number",
        "KWH",
        "KVAH",
        "Reading Date",
        "Department",
        "Phase",
        "Meter Make",
        "Cycle",
        "Division",
        "Seq No",
        "Address"
    ];
}


/* ============================================================
   COMMON EXPORT ROWS
   ALL 13 COLUMNS
   ============================================================ */

function getCmriExportRows() {

    return filteredCmriDownloadData.map((x, index) => {

        return [
            index + 1,
            x.cons_ref || "--",
            x.meterno || "--",
            x.kwh || "--",
            x.kvah || "--",
            formatCmriReadingDate(x.ReadingDate),
            x.department || "--",
            x.Phase || "--",
            x.MeterType || "--",
            x.cycle || "--",
            x.division || "--",
            x.sap_seq_no || "--",
            x.address || "--"
        ];

    });
}


/* ============================================================
   EXPORT TO EXCEL
   13 COLUMNS
   ============================================================ */

function exportCmriTableToExcel() {

    if (filteredCmriDownloadData.length === 0) {

        alert("No CMRI records available to export.");

        return;
    }

    if (typeof XLSX === "undefined") {

        alert(
            "Excel library XLSX is not loaded. " +
            "Please include the SheetJS library."
        );

        return;
    }

    const excelData = [
        getCmriExportHeaders(),
        ...getCmriExportRows()
    ];

    const worksheet = XLSX.utils.aoa_to_sheet(
        excelData
    );

    worksheet["!cols"] = [
        { wch: 8 },
        { wch: 18 },
        { wch: 20 },
        { wch: 14 },
        { wch: 14 },
        { wch: 16 },
        { wch: 18 },
        { wch: 12 },
        { wch: 16 },
        { wch: 12 },
        { wch: 20 },
        { wch: 16 },
        { wch: 50 }
    ];

    worksheet["!autofilter"] = {
        ref: `A1:M${excelData.length}`
    };

    const workbook = XLSX.utils.book_new();

    XLSX.utils.book_append_sheet(
        workbook,
        worksheet,
        "CMRI Summary"
    );

    const timestamp = getCmriTimestamp();

    XLSX.writeFile(
        workbook,
        `CMRI_Download_Summary_${timestamp}.xlsx`
    );
}


/* ============================================================
   CSV ESCAPE
   PREVENT FORMULA INJECTION
   ============================================================ */

function csvEscape(value) {

    let text = String(value ?? "");

    if (/^[=+\-@\t\r]/.test(text)) {

        text = "'" + text;
    }

    return `"${text.replace(/"/g, '""')}"`;
}


/* ============================================================
   EXPORT TO CSV
   13 COLUMNS
   ============================================================ */

function exportCmriTableToCSV() {

    if (filteredCmriDownloadData.length === 0) {

        alert("No CMRI records available to export.");

        return;
    }

    const csvRows = [
        getCmriExportHeaders().map(csvEscape).join(",")
    ];

    getCmriExportRows().forEach(row => {

        csvRows.push(
            row.map(csvEscape).join(",")
        );

    });

    const blob = new Blob(
        ["\uFEFF" + csvRows.join("\r\n")],
        {
            type: "text/csv;charset=utf-8;"
        }
    );

    const url = URL.createObjectURL(blob);

    const link = document.createElement("a");

    link.href = url;

    link.download =
        `CMRI_Download_Summary_${getCmriTimestamp()}.csv`;

    document.body.appendChild(link);

    link.click();

    document.body.removeChild(link);

    setTimeout(() => {

        URL.revokeObjectURL(url);

    }, 1000);
}


/* ============================================================
   COMMON TIMESTAMP
   ============================================================ */

function getCmriTimestamp() {

    const now = new Date();

    return (
        now.getFullYear() +
        String(now.getMonth() + 1).padStart(2, "0") +
        String(now.getDate()).padStart(2, "0") +
        "_" +
        String(now.getHours()).padStart(2, "0") +
        String(now.getMinutes()).padStart(2, "0") +
        String(now.getSeconds()).padStart(2, "0")
    );
}


/* ============================================================
   PRINT CMRI SUMMARY
   ============================================================ */

function printCmriDownloadSummary() {

    if (filteredCmriDownloadData.length === 0) {

        alert("No CMRI records available to print.");

        return;
    }

    const table = document.getElementById(
        "cmriDownloadSummaryTable"
    );

    if (!table) {

        alert("CMRI download summary table not found.");

        return;
    }

    const printWindow = window.open(
        "",
        "_blank",
        "width=1200,height=800"
    );

    if (!printWindow) {

        alert("Please allow pop-ups to print the CMRI report.");

        return;
    }

    const month = escapeHtml(
        String(getReadingMonth() || "")
    );

    printWindow.document.open();

    printWindow.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>CMRI Download Summary</title>

            <meta charset="UTF-8">

            <style>
                body {
                    font-family: Arial, sans-serif;
                    padding: 15px;
                }

                h3 {
                    text-align: center;
                }

                .month {
                    text-align: center;
                    margin-bottom: 15px;
                }

                table {
                    width: 100%;
                    border-collapse: collapse;
                    font-size: 9px;
                }

                th, td {
                    border: 1px solid #777;
                    padding: 5px;
                    text-align: left;
                    vertical-align: top;
                }

                th {
                    background: #e9ecef;
                }

                @page {
                    size: landscape;
                    margin: 8mm;
                }
            </style>
        </head>

        <body>

            <h3>CMRI Download Summary</h3>

            <div class="month">
                Reading Month: ${month}
            </div>

            ${table.outerHTML}

            <script>
                window.onload = function () {
                    window.print();
                    window.onafterprint = function () {
                        window.close();
                    };
                };
            <\/script>

        </body>
        </html>
    `);

    printWindow.document.close();
}


/* ============================================================
   ESCAPE HTML
   ============================================================ */

function escapeHtml(value) {

    return String(value ?? "")

        .replace(/&/g, "&amp;")

        .replace(/</g, "&lt;")

        .replace(/>/g, "&gt;")

        .replace(/"/g, "&quot;")

        .replace(/'/g, "&#039;");
}
