

let allCmriDownloadData = [];
let filteredCmriDownloadData = [];

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
            "sapDepartment",
            "sap_department"
        ),

        cycle: value(
            "cycle",
            "Cycle"
        ),

        division: value(
            "division",
            "sapDivision",
            "sap_division"
        ),

        sap_seq_no: value(
            "sap_seq_no",
            "sapSeqNo",
            "seq_no",
            "seqNo"
        ),

        address: value(
            "address",
            "Address"
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

async function loadCmriDownloadSummary()
{

    const tbody = $("#cmriDownloadSummaryBody");

    try {

        tbody.html(`
            <tr>
                <td colspan="8" class="text-center py-4">
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
            }
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

        console.log(
            "First CMRI record:",
            Array.isArray(result) ? result[0] : result
        );

        /*
         * Support either a direct array or common
         * wrapped API response formats.
         */
        let records = [];

        if (Array.isArray(result)) {

            records = result;

        } else if (Array.isArray(result?.data)) {

            records = result.data;

        } else if (Array.isArray(result?.result)) {

            records = result.result;

        } else if (Array.isArray(result?.items)) {

            records = result.items;
        }

        allCmriDownloadData = records.map(
            normalizeCmriRecord
        );

        filteredCmriDownloadData = [
            ...allCmriDownloadData
        ];

        console.log(
            "Normalized CMRI record count:",
            allCmriDownloadData.length
        );

        console.log(
            "Normalized first record:",
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
                <td colspan="8"
                    class="text-center text-danger py-4">
                    <i class="bi bi-exclamation-triangle me-1"></i>
                    Failed to load CMRI data.
                    <br>
                    ${escapeHtml(err.message)}
                </td>
            </tr>
        `);
    }
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
   Depends on selected department and division.
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

            const itemDepartment = String(
                item.department ?? ""
            ).trim().toUpperCase();

            return itemDepartment === selectedDepartment;
        });
    }

    if (selectedDivision !== "") {

        sourceData = sourceData.filter(item => {

            const itemDivision = String(
                item.division ?? ""
            ).trim().toUpperCase();

            return itemDivision === selectedDivision;
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

            const departmentMatch =
                department === "" ||
                itemDepartment === department;

            const divisionMatch =
                division === "" ||
                itemDivision === division;

            const cycleMatch =
                cycle === "" ||
                itemCycle === cycle;

            return (
                departmentMatch &&
                divisionMatch &&
                cycleMatch
            );
        });

    renderCmriDownloadTable(
        filteredCmriDownloadData
    );
}


/* ============================================================
   RENDER CMRI DOWNLOAD TABLE
   Exactly 8 columns.
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
                <td colspan="8"
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

                <td>
                    ${escapeHtml(x.department || "--")}
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
   EXPORT TO EXCEL
   Requires SheetJS (XLSX).
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

    const excelData = [];

    excelData.push([
        "S.No.",
        "Cons Ref",
        "Meter Number",
        "Department",
        "Cycle",
        "Division",
        "Seq No",
        "Address"
    ]);

    filteredCmriDownloadData.forEach((x, index) => {

        excelData.push([
            index + 1,
            x.cons_ref || "--",
            x.meterno || "--",
            x.department || "--",
            x.cycle || "--",
            x.division || "--",
            x.sap_seq_no || "--",
            x.address || "--"
        ]);

    });

    const worksheet = XLSX.utils.aoa_to_sheet(
        excelData
    );

    worksheet["!cols"] = [
        { wch: 8 },
        { wch: 18 },
        { wch: 20 },
        { wch: 18 },
        { wch: 12 },
        { wch: 20 },
        { wch: 12 },
        { wch: 50 }
    ];

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
   Prevents spreadsheet formula injection.
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
   Exports filtered records only.
   ============================================================ */

function exportCmriTableToCSV() {

    if (filteredCmriDownloadData.length === 0) {

        alert("No CMRI records available to export.");

        return;
    }

    const headers = [
        "S.No.",
        "Cons Ref",
        "Meter Number",
        "Department",
        "Cycle",
        "Division",
        "Seq No",
        "Address"
    ];

    const csv = [
        headers.map(csvEscape).join(",")
    ];

    filteredCmriDownloadData.forEach((x, index) => {

        csv.push([
            index + 1,
            x.cons_ref || "--",
            x.meterno || "--",
            x.department || "--",
            x.cycle || "--",
            x.division || "--",
            x.sap_seq_no || "--",
            x.address || "--"
        ].map(csvEscape).join(","));

    });

    const blob = new Blob(
        ["\uFEFF" + csv.join("\r\n")],
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

    const table = document.getElementById(
        "cmriDownloadSummaryTable"
    );

    if (!table) {

        alert("CMRI download summary table not found.");

        return;
    }

    if (filteredCmriDownloadData.length === 0) {

        alert("No CMRI records available to print.");

        return;
    }

    window.print();
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