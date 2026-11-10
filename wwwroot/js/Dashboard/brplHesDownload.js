
/* ============================================================
   HES DOWNLOAD SUMMARY - BRPL
   Complete 13-column implementation
   ============================================================ */

let allHesDownloadData = [];
let filteredHesDownloadData = [];
let hesCurrentUserAccess = null;
let hesDepartmentRestricted = false;


/* ============================================================
   COMMON HELPERS
   ============================================================ */

function getHesValue(item, camelName, pascalName) {
    return item?.[camelName] ??
        item?.[pascalName] ??
        null;
}

function getHesText(value, fallback = "--") {
    if (value === null || value === undefined ||
        String(value).trim() === "") {
        return fallback;
    }

    return String(value).trim();
}

function normalizeHesValue(value) {
    return String(value ?? "").trim().toUpperCase();
}

function escapeHesHtml(value, fallback = "--") {

    if (value === null || value === undefined ||
        String(value) === "") {
        return fallback;
    }

    return String(value)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

function getHesDate(value) {

    if (!value) {
        return "--";
    }

    const date = new Date(value);

    if (isNaN(date.getTime())) {
        return escapeHesHtml(value);
    }

    const day = String(date.getDate()).padStart(2, "0");
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const year = date.getFullYear();

    const hours = String(date.getHours()).padStart(2, "0");
    const minutes = String(date.getMinutes()).padStart(2, "0");
    const seconds = String(date.getSeconds()).padStart(2, "0");

    return `${day}-${month}-${year} ${hours}:${minutes}:${seconds}`;
}


/* ============================================================
   API MODEL PROPERTY MAPPING
   Supports camelCase and PascalCase JSON responses.
   ============================================================ */

function getHesRow(item) {

    return {
        consRef: getHesValue(item, "consRef", "ConsRef"),

        meterNumber: getHesValue(item, "meterNumber", "MeterNumber"),

        kwh: getHesValue(item, "kwh", "Kwh"),

        kvah: getHesValue(item, "kvah", "Kvah"),

        readingDate: getHesValue(item, "readingDate", "ReadingDate"),

        phase: getHesValue(item, "phase", "Phase"),

        sapDepartment: getHesValue(item, "sapDepartment", "SapDepartment"),

        cycle: getHesValue(item, "cycle", "Cycle"),

        sapDivision: getHesValue(item, "sapDivision", "SapDivision"),

        sapSeqNo: getHesValue(item, "sapSeqNo", "SapSeqNo"),

        address: getHesValue(item, "address", "Address"),

        meterType: getHesValue(item, "meterType", "MeterType")
    };
}


/* ============================================================
   CURRENT USER ACCESS
   ============================================================ */

async function getHesUserAccess() {

    if (hesCurrentUserAccess?.authenticated) {
        return hesCurrentUserAccess;
    }

    const response = await fetch(
        getApiUrl("AuthApi/my-access"),
        {
            method: "GET",
            credentials: "same-origin",
            cache: "no-store",
            headers: {
                Accept: "application/json"
            }
        }
    );

    if (!response.ok) {
        throw new Error(
            `Unable to determine user access. Status: ${response.status}`
        );
    }

    hesCurrentUserAccess = await response.json();

    return hesCurrentUserAccess;
}


/* ============================================================
   ACCESS VALIDATION
   ============================================================ */

function isHesSuperAdmin(access) {

    const role = normalizeHesValue(access?.role);

    return role === "SUPERADMIN" ||
        access?.isSuperAdmin === true;
}

function hasHesBrplAccess(access) {

    if (!access) {
        return false;
    }

    if (isHesSuperAdmin(access)) {
        return true;
    }

    return normalizeHesValue(access.company) === "BRPL";
}

function hasHesDepartmentRestriction(access) {

    return !isHesSuperAdmin(access) &&
        normalizeHesValue(access?.department) !== "";
}

function getHesUserDepartment(access) {

    return normalizeHesValue(access?.department);
}


/* ============================================================
   ACCESS ERROR
   ============================================================ */

function showHesAccessError(message) {

    $("#hesDownloadSummaryBody").html(`
        <tr>
            <td colspan="13" class="text-center text-danger py-4">
                <div class="fw-bold mb-1">Access Denied</div>
                <div>${escapeHesHtml(message)}</div>
            </td>
        </tr>
    `);
}


/* ============================================================
   OPEN HES MODAL
   ============================================================ */

$("#hesDownload")
    .off("click.hesDownload")
    .on("click.hesDownload", async function (e) {

        e.preventDefault();

        const modalElement = document.getElementById(
            "HesDownloadSummaryModal"
        );

        if (!modalElement) {
            console.error("HES Download modal not found.");
            return;
        }

        bootstrap.Modal.getOrCreateInstance(
            modalElement
        ).show();

        await loadHesDownloadSummary();
    });


/* ============================================================
   LOAD HES DOWNLOAD DATA
   ============================================================ */

async function loadHesDownloadSummary() {

    const tbody = $("#hesDownloadSummaryBody");

    try {

        tbody.html(`
            <tr>
                <td colspan="13" class="text-center py-4">
                    <div class="spinner-border spinner-border-sm text-primary"></div>
                    <span class="ms-2">Loading HES Download Data...</span>
                </td>
            </tr>
        `);

        const access = await getHesUserAccess();

        if (!hasHesBrplAccess(access)) {

            showHesAccessError(
                "You do not have access to BRPL HES download data."
            );

            return;
        }

        const readingMonth = getReadingMonth();

        if (!readingMonth) {

            showHesAccessError(
                "Reading month is not selected."
            );

            return;
        }

        const apiUrl =
            `${getApiUrl("DashboardApi/hes-download-meters-details")}` +
            `?ReadingMonth=${encodeURIComponent(readingMonth)}`;

        const response = await fetch(
            apiUrl,
            {
                method: "GET",
                credentials: "same-origin",
                cache: "no-store",
                headers: {
                    Accept: "application/json"
                }
            }
        );

        if (response.status === 401) {
            throw new Error(
                "Your session has expired. Please login again."
            );
        }

        if (response.status === 403) {
            throw new Error(
                "You are not authorized to access BRPL HES download data."
            );
        }

        if (!response.ok) {
            throw new Error(
                `Unable to load HES data. HTTP Status: ${response.status}`
            );
        }

        const result = await response.json();

        // Supports both array and common API wrapper responses.
        const records = Array.isArray(result)
            ? result
            : Array.isArray(result?.data)
                ? result.data
                : Array.isArray(result?.items)
                    ? result.items
                    : Array.isArray(result?.result)
                        ? result.result
                        : [];

        allHesDownloadData = records;

        hesDepartmentRestricted =
            hasHesDepartmentRestriction(access);

        const userDepartment =
            getHesUserDepartment(access);

        // Hard department restriction.
        if (hesDepartmentRestricted) {

            allHesDownloadData =
                allHesDownloadData.filter(item => {

                    const row = getHesRow(item);

                    return normalizeHesValue(row.sapDepartment) ===
                        userDepartment;
                });
        }

        // Initialize filtered data.
        filteredHesDownloadData = [
            ...allHesDownloadData
        ];

        // Populate dropdowns.
        loadHesDepartmentFilter(access);
        loadHesDivisionFilter();
        loadHesPhaseFilter();
        loadHesMeterMakeFilter();
        loadHesCycleFilter();

        // Reset filters.
        clearHesFilters(hesDepartmentRestricted);

        // Render complete data.
        renderHesDownloadTable(
            filteredHesDownloadData
        );

        console.log(
            "HES Download Records:",
            allHesDownloadData.length
        );

    }
    catch (err) {

        console.error(
            "HES Download Summary Error:",
            err
        );

        tbody.html(`
            <tr>
                <td colspan="13" class="text-center text-danger py-4">
                    <div class="fw-bold mb-1">Failed to load data</div>
                    <div>${escapeHesHtml(err.message)}</div>
                </td>
            </tr>
        `);
    }
}


/* ============================================================
   COMMON DROPDOWN POPULATION
   ============================================================ */

function populateHesDropdown(selector, values, defaultText) {

    const ddl = $(selector);

    if (!ddl.length) {
        return;
    }

    const uniqueValues = [
        ...new Set(
            values
                .map(value => getHesText(value, ""))
                .filter(Boolean)
        )
    ];

    uniqueValues.sort((a, b) =>
        a.localeCompare(b, undefined, { numeric: true })
    );

    ddl.empty();

    ddl.append(
        $("<option>", {
            value: "",
            text: defaultText
        })
    );

    uniqueValues.forEach(value => {

        ddl.append(
            $("<option>", {
                value: value,
                text: value
            })
        );
    });
}


/* ============================================================
   DEPARTMENT FILTER
   ============================================================ */

function loadHesDepartmentFilter(access = hesCurrentUserAccess) {

    const ddl = $("#hesDepartmentFilter");

    if (!ddl.length) {
        return;
    }

    if (hasHesDepartmentRestriction(access)) {

        const department = getHesUserDepartment(access);

        ddl.empty();

        ddl.append(
            $("<option>", {
                value: department,
                text: department
            })
        );

        ddl.val(department).prop("disabled", true);

        return;
    }

    ddl.prop("disabled", false);

    populateHesDropdown(
        "#hesDepartmentFilter",
        allHesDownloadData.map(
            item => getHesRow(item).sapDepartment
        ),
        "All Departments"
    );
}


/* ============================================================
   DIVISION FILTER
   ============================================================ */

function loadHesDivisionFilter() {

    populateHesDropdown(
        "#hesDivisionFilter",
        allHesDownloadData.map(
            item => getHesRow(item).sapDivision
        ),
        "All Divisions"
    );
}


/* ============================================================
   PHASE FILTER
   ============================================================ */

function loadHesPhaseFilter() {

    populateHesDropdown(
        "#hesPhaseFilter",
        allHesDownloadData.map(
            item => getHesRow(item).phase
        ),
        "All Phases"
    );
}


/* ============================================================
   METER MAKE FILTER
   ============================================================ */

function loadHesMeterMakeFilter() {

    populateHesDropdown(
        "#hesMeterMakeFilter",
        allHesDownloadData.map(
            item => getHesRow(item).meterType
        ),
        "All Meter Makes"
    );
}


/* ============================================================
   CYCLE FILTER
   ============================================================ */

function loadHesCycleFilter() {

    const currentCycle = String(
        $("#hesCycleFilter").val() || ""
    );

    const selectedDepartment = normalizeHesValue(
        $("#hesDepartmentFilter").val()
    );

    let sourceData = allHesDownloadData;

    if (selectedDepartment) {

        sourceData = allHesDownloadData.filter(item => {

            return normalizeHesValue(
                getHesRow(item).sapDepartment
            ) === selectedDepartment;
        });
    }

    const cycles = sourceData.map(
        item => getHesRow(item).cycle
    );

    populateHesDropdown(
        "#hesCycleFilter",
        cycles,
        "All Cycles"
    );

    const exists = [...$("#hesCycleFilter option")].some(
        option => normalizeHesValue(option.value) ===
            normalizeHesValue(currentCycle)
    );

    $("#hesCycleFilter").val(exists ? currentCycle : "");
}


/* ============================================================
   FILTER EVENTS
   ============================================================ */

$("#hesDepartmentFilter")
    .off("change.hesDepartment")
    .on("change.hesDepartment", function () {

        loadHesCycleFilter();
        applyHesFilters();
    });


$("#hesDivisionFilter")
    .off("change.hesDivision")
    .on("change.hesDivision", applyHesFilters);


$("#hesPhaseFilter")
    .off("change.hesPhase")
    .on("change.hesPhase", applyHesFilters);


$("#hesMeterMakeFilter")
    .off("change.hesMeterMake")
    .on("change.hesMeterMake", applyHesFilters);


$("#hesCycleFilter")
    .off("change.hesCycle")
    .on("change.hesCycle", applyHesFilters);


/* ============================================================
   CLEAR FILTERS BUTTON
   ============================================================ */

$("#btnClearHesDownloadFilters")
    .off("click.hesClear")
    .on("click.hesClear", function () {

        clearHesFilters(hesDepartmentRestricted);

        applyHesFilters();
    });


function clearHesFilters(departmentRestricted = false) {

    if (departmentRestricted) {

        $("#hesDepartmentFilter").val(
            getHesUserDepartment(hesCurrentUserAccess)
        );

    } else {

        $("#hesDepartmentFilter").val("");
    }

    $("#hesDivisionFilter").val("");
    $("#hesPhaseFilter").val("");
    $("#hesMeterMakeFilter").val("");
    $("#hesCycleFilter").val("");
}


/* ============================================================
   APPLY FILTERS
   ============================================================ */

function applyHesFilters() {

    const department = normalizeHesValue(
        $("#hesDepartmentFilter").val()
    );

    const division = normalizeHesValue(
        $("#hesDivisionFilter").val()
    );

    const phase = normalizeHesValue(
        $("#hesPhaseFilter").val()
    );

    const meterMake = normalizeHesValue(
        $("#hesMeterMakeFilter").val()
    );

    const cycle = normalizeHesValue(
        $("#hesCycleFilter").val()
    );

    const userDepartment =
        getHesUserDepartment(hesCurrentUserAccess);

    const effectiveDepartment =
        hesDepartmentRestricted
            ? userDepartment
            : department;

    filteredHesDownloadData =
        allHesDownloadData.filter(item => {

            const row = getHesRow(item);

            return (
                (!effectiveDepartment ||
                    normalizeHesValue(row.sapDepartment) === effectiveDepartment) &&

                (!division ||
                    normalizeHesValue(row.sapDivision) === division) &&

                (!phase ||
                    normalizeHesValue(row.phase) === phase) &&

                (!meterMake ||
                    normalizeHesValue(row.meterType) === meterMake) &&

                (!cycle ||
                    normalizeHesValue(row.cycle) === cycle)
            );
        });

    renderHesDownloadTable(
        filteredHesDownloadData
    );
}


/* ============================================================
   RENDER COMPLETE 13-COLUMN TABLE
   ============================================================ */

function renderHesDownloadTable(data) {

    const tbody = $("#hesDownloadSummaryBody");

    tbody.empty();

    if (!Array.isArray(data) || data.length === 0) {

        tbody.html(`
            <tr>
                <td colspan="13" class="text-center py-4 text-muted">
                    No Records Found
                </td>
            </tr>
        `);

        return;
    }

    const fragment = document.createDocumentFragment();

    data.forEach((item, index) => {

        const row = getHesRow(item);

        const tr = document.createElement("tr");

        const columns = [
            {
                value: index + 1,
                className: "text-center fw-semibold"
            },
            {
                value: row.consRef
            },
            {
                value: row.meterNumber,
                className: "meter-number-cell"
            },
            {
                value: row.kwh,
                className: "text-end"
            },
            {
                value: row.kvah,
                className: "text-end"
            },
            {
                value: getHesDate(row.readingDate),
                className: "text-center reading-date-cell"
            },
            {
                value: row.phase,
                className: "text-center"
            },
            {
                value: row.sapDepartment
            },
            {
                value: row.cycle,
                className: "text-center"
            },
            {
                value: row.sapDivision
            },
            {
                value: row.sapSeqNo,
                className: "text-center"
            },
            {
                value: row.address,
                className: "address-cell"
            },
            {
                value: row.meterType
            }
        ];

        columns.forEach(column => {

            const td = document.createElement("td");

            td.textContent = getHesText(column.value);

            if (column.className) {
                td.className = column.className;
            }

            tr.appendChild(td);
        });

        fragment.appendChild(tr);
    });

    tbody[0].appendChild(fragment);

    console.log(
        "HES table rendered:",
        data.length,
        "records"
    );
}


/* ============================================================
   EXCEL EXPORT - COMPLETE 13 COLUMNS
   Requires SheetJS XLSX.
   ============================================================ */

function exportHesDownloadBrplTableToExcel() {

    if (typeof XLSX === "undefined") {

        alert("Excel library (SheetJS) is not loaded.");

        return;
    }

    if (!filteredHesDownloadData.length) {

        alert("No HES Download data available to export.");

        return;
    }

    const headers = [
        "S.No.",
        "Cons Ref",
        "Meter Number",
        "KWH",
        "KVAH",
        "Reading Date",
        "Phase",
        "Department",
        "Cycle",
        "Division",
        "Seq No",
        "Address",
        "Meter Make"
    ];

    const rows = filteredHesDownloadData.map((item, index) => {

        const x = getHesRow(item);

        return [
            index + 1,
            getHesText(x.consRef, ""),
            getHesText(x.meterNumber, ""),
            x.kwh ?? "",
            x.kvah ?? "",
            getHesDate(x.readingDate) === "--"
                ? ""
                : getHesDate(x.readingDate),
            getHesText(x.phase, ""),
            getHesText(x.sapDepartment, ""),
            getHesText(x.cycle, ""),
            getHesText(x.sapDivision, ""),
            getHesText(x.sapSeqNo, ""),
            getHesText(x.address, ""),
            getHesText(x.meterType, "")
        ];
    });

    const wb = XLSX.utils.book_new();

    const ws = XLSX.utils.aoa_to_sheet([
        headers,
        ...rows
    ]);

    const border = {
        top: { style: "thin", color: { rgb: "7F7F7F" } },
        bottom: { style: "thin", color: { rgb: "7F7F7F" } },
        left: { style: "thin", color: { rgb: "7F7F7F" } },
        right: { style: "thin", color: { rgb: "7F7F7F" } }
    };

    const headerStyle = {
        fill: {
            patternType: "solid",
            fgColor: { rgb: "17365D" }
        },
        font: {
            name: "Calibri",
            sz: 11,
            bold: true,
            color: { rgb: "FFFFFF" }
        },
        alignment: {
            horizontal: "center",
            vertical: "center",
            wrapText: true
        },
        border
    };

    const normalStyle = {
        font: {
            name: "Calibri",
            sz: 10,
            color: { rgb: "000000" }
        },
        alignment: {
            vertical: "center",
            wrapText: true
        },
        border
    };

    const centerStyle = {
        ...normalStyle,
        alignment: {
            horizontal: "center",
            vertical: "center",
            wrapText: true
        }
    };

    // Header styling.
    for (let col = 0; col < 13; col++) {

        const address = XLSX.utils.encode_cell({
            r: 0,
            c: col
        });

        ws[address].s = headerStyle;
    }

    // Data styling.
    for (let r = 1; r <= rows.length; r++) {

        for (let c = 0; c < 13; c++) {

            const address = XLSX.utils.encode_cell({
                r,
                c
            });

            if (!ws[address]) {
                continue;
            }

            ws[address].s =
                [0, 3, 4, 5, 6, 8, 10].includes(c)
                    ? centerStyle
                    : normalStyle;
        }
    }

    ws["!cols"] = [
        { wch: 8 },
        { wch: 18 },
        { wch: 20 },
        { wch: 13 },
        { wch: 13 },
        { wch: 22 },
        { wch: 12 },
        { wch: 20 },
        { wch: 12 },
        { wch: 20 },
        { wch: 14 },
        { wch: 55 },
        { wch: 20 }
    ];

    ws["!rows"] = [
        { hpt: 32 },
        ...rows.map(() => ({ hpt: 28 }))
    ];

    ws["!autofilter"] = {
        ref: `A1:M${rows.length + 1}`
    };

    // Freeze first row.
    ws["!freeze"] = {
        xSplit: 0,
        ySplit: 1
    };

    ws["!sheetViews"] = [
        {
            showGridLines: false
        }
    ];

    XLSX.utils.book_append_sheet(
        wb,
        ws,
        "HES Download"
    );

    const date = new Date()
        .toISOString()
        .split("T")[0];

    XLSX.writeFile(
        wb,
        `HES_Download_Summary_BRPL_${date}.xlsx`
    );
}


/* ============================================================
   CSV EXPORT - COMPLETE 13 COLUMNS
   ============================================================ */

function exportHesDownloadBrplTableToCSV() {

    if (!filteredHesDownloadData.length) {

        alert("No HES Download data available to export.");

        return;
    }

    const headers = [
        "S.No.",
        "Cons Ref",
        "Meter Number",
        "KWH",
        "KVAH",
        "Reading Date",
        "Phase",
        "Department",
        "Cycle",
        "Division",
        "Seq No",
        "Address",
        "Meter Make"
    ];

    function csvEscape(value) {

        let text = String(value ?? "");

        // Prevent spreadsheet formula injection.
        if (/^[=+\-@\t\r]/.test(text)) {
            text = "'" + text;
        }

        return `"${text.replace(/"/g, '""')}"`;
    }

    const csvRows = [headers.map(csvEscape).join(",")];

    filteredHesDownloadData.forEach((item, index) => {

        const x = getHesRow(item);

        const row = [
            index + 1,
            x.consRef,
            x.meterNumber,
            x.kwh,
            x.kvah,
            getHesDate(x.readingDate) === "--"
                ? ""
                : getHesDate(x.readingDate),
            x.phase,
            x.sapDepartment,
            x.cycle,
            x.sapDivision,
            x.sapSeqNo,
            x.address,
            x.meterType
        ];

        csvRows.push(
            row.map(csvEscape).join(",")
        );
    });

    // UTF-8 BOM supports Excel Hindi/Unicode text.
    const blob = new Blob(
        ["\uFEFF" + csvRows.join("\r\n")],
        {
            type: "text/csv;charset=utf-8;"
        }
    );

    const url = URL.createObjectURL(blob);

    const link = document.createElement("a");

    link.href = url;

    const date = new Date()
        .toISOString()
        .split("T")[0];

    link.download =
        `HES_Download_Summary_BRPL_${date}.csv`;

    document.body.appendChild(link);

    link.click();

    link.remove();

    setTimeout(() => URL.revokeObjectURL(url), 1000);
}


/* ============================================================
   PRINT - HES REPORT ONLY
   ============================================================ */

function printHesDownloadReport() {

    if (!filteredHesDownloadData.length) {

        alert("No HES Download data available to print.");

        return;
    }

    const headers = [
        "S.No.",
        "Cons Ref",
        "Meter Number",
        "KWH",
        "KVAH",
        "Reading Date",
        "Phase",
        "Department",
        "Cycle",
        "Division",
        "Seq No",
        "Address",
        "Meter Make"
    ];

    const headerHtml = headers.map(
        header => `<th>${escapeHesHtml(header)}</th>`
    ).join("");

    const bodyHtml = filteredHesDownloadData.map((item, index) => {

        const x = getHesRow(item);

        const values = [
            index + 1,
            x.consRef,
            x.meterNumber,
            x.kwh,
            x.kvah,
            getHesDate(x.readingDate),
            x.phase,
            x.sapDepartment,
            x.cycle,
            x.sapDivision,
            x.sapSeqNo,
            x.address,
            x.meterType
        ];

        return `<tr>${values.map(value =>
            `<td>${escapeHesHtml(value)}</td>`
        ).join("")}</tr>`;

    }).join("");

    const printWindow = window.open("", "_blank");

    if (!printWindow) {

        alert("Please allow pop-ups to print the HES report.");

        return;
    }

    printWindow.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>HES Download Summary</title>
            <style>
                @page {
                    size: landscape;
                    margin: 8mm;
                }

                body {
                    font-family: Arial, sans-serif;
                    font-size: 9px;
                }

                h3 {
                    text-align: center;
                    margin-bottom: 12px;
                }

                table {
                    width: 100%;
                    border-collapse: collapse;
                }

                th, td {
                    border: 1px solid #777;
                    padding: 5px;
                    text-align: left;
                    overflow-wrap: anywhere;
                }

                th {
                    background: #17365D;
                    color: white;
                }

                tr:nth-child(even) {
                    background: #f2f2f2;
                }
            </style>
        </head>
        <body>
            <h3>Meter Download Summary - BRPL</h3>

            <table>
                <thead>
                    <tr>${headerHtml}</tr>
                </thead>

                <tbody>
                    ${bodyHtml}
                </tbody>
            </table>

            <script>
                window.onload = function() {
                    window.print();
                };
            <\/script>
        </body>
        </html>
    `);

    printWindow.document.close();
}
