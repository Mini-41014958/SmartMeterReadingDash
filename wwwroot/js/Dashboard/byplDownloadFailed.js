let allDownloadDataBypl = [];
let filteredDataBypl = [];


$("#btnViewHesFailedDetailsBYPL")
    .off("click")
    .on("click", function () {

        const modalElement =
            document.getElementById(
                "downloadSummaryModalBYPL"
            );

        if (!modalElement) {

            console.error(
                "BYPL modal not found."
            );

            return;
        }

        const modal =
            bootstrap.Modal.getOrCreateInstance(
                modalElement
            );

        modal.show();

        loadDownloadSummaryBYPL();
    });


async function loadDownloadSummaryBYPL() {

    try {

        $("#downloadSummaryBodyBYPL").html(`
            <tr>
                <td colspan="13"
                    class="text-center py-4">

                    <div class="spinner-border spinner-border-sm text-primary"></div>

                    <span class="ms-2">
                        Loading...
                    </span>

                </td>
            </tr>
        `);


        const readingMonth =
            getReadingMonth();


        const apiUrl =
            `${getApiUrl(
                "dashboardapi/meter-download-detailed-summary-bypl"
            )}` +
            `?readingMonth=${encodeURIComponent(
                readingMonth
            )}`;


        const response =
            await fetch(apiUrl);


        if (!response.ok) {

            const errorText =
                await response.text();


            console.error(
                "BYPL API Error:",
                response.status,
                errorText
            );


            throw new Error(
                "Unable to load data. HTTP " +
                response.status
            );
        }


        const result =
            await response.json();


        allDownloadDataBypl =
            Array.isArray(result)
                ? result
                : [];


        filteredDataBypl =
            [
                ...allDownloadDataBypl
            ];


        loadDepartmentFilterBYPL();

        loadReasonFilterBYPL();

        loadPhaseFilterBYPL();

        loadMeterMakeFilterBYPL();

        clearFiltersBYPL();

        renderTableBYPL(
            filteredDataBypl
        );

    }
    catch (err) {

        console.error(
            "BYPL Download Summary Error:",
            err
        );


        $("#downloadSummaryBodyBYPL").html(`
            <tr>
                <td colspan="13"
                    class="text-center text-danger py-4">

                    Failed to load data:
                    ${escapeHtml(err.message)}

                </td>
            </tr>
        `);
    }
}


/* ============================================================
   DEPARTMENT FILTER
   ============================================================ */

function loadDepartmentFilterBYPL() {

    const ddl =
        $("#departmentFilterBYPL");


    ddl.empty();


    ddl.append(`
        <option value="">
            All Departments
        </option>
    `);


    const departments =
        [
            ...new Set(
                allDownloadDataBypl
                    .map(x =>
                        String(
                            x.sapDepartment || ""
                        ).trim()
                    )
                    .filter(Boolean)
            )
        ];


    departments.sort(
        (a, b) =>
            a.localeCompare(b)
    );


    departments.forEach(
        department => {

            ddl.append(
                $("<option>", {
                    value: department,
                    text: department
                })
            );

        }
    );
}


/* ============================================================
   REASON FILTER
   ============================================================ */

function loadReasonFilterBYPL() {

    const ddl =
        $("#reasonFilterBYPL");


    ddl.empty();


    ddl.append(`
        <option value="">
            All Reasons
        </option>

        <option value="SYSTEM_TITLE">
            System Title Mismatch
        </option>

        <option value="TCP">
            TCP Connection Failed
        </option>

        <option value="NO_DATA">
            Data Not Found in HES
        </option>

        <option value="DATE_OLDER">
            Date Older Then FormY
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

function loadPhaseFilterBYPL() {

    const ddl =
        $("#phaseFilterBYPL");


    ddl.empty();


    ddl.append(`
        <option value="">
            All Phases
        </option>
    `);


    const phases =
        [
            ...new Set(
                allDownloadDataBypl
                    .map(x => x.phase)
                    .filter(
                        x =>
                            x !== null &&
                            x !== undefined &&
                            x !== ""
                    )
            )
        ];


    phases.sort(
        (a, b) =>
            String(a).localeCompare(
                String(b),
                undefined,
                {
                    numeric: true
                }
            )
    );


    phases.forEach(
        phase => {

            ddl.append(
                $("<option>", {
                    value: String(phase),
                    text: String(phase)
                })
            );

        }
    );
}


/* ============================================================
   METER MAKE FILTER
   ============================================================ */

function loadMeterMakeFilterBYPL() {

    const ddl =
        $("#meterMakeFilterBYPL");


    ddl.empty();


    ddl.append(`
        <option value="">
            All Meter Makes
        </option>
    `);


    const meterMakes =
        [
            ...new Set(
                allDownloadDataBypl
                    .map(x => x.meterType)
                    .filter(
                        x =>
                            x !== null &&
                            x !== undefined &&
                            x !== ""
                    )
            )
        ];


    meterMakes.sort(
        (a, b) =>
            String(a).localeCompare(
                String(b)
            )
    );


    meterMakes.forEach(
        make => {

            ddl.append(
                $("<option>", {
                    value: String(make),
                    text: String(make)
                })
            );

        }
    );
}


/* ============================================================
   FAILURE CATEGORY
   ============================================================ */

function getFailureCategoryBYPL(reason) {

    const message =
        String(reason || "")
            .toUpperCase()
            .replace(/\s+/g, " ")
            .trim();


    if (
        message.includes(
            "SYSTEM TITLE"
        )
    ) {

        return "SYSTEM_TITLE";
    }


    if (
        message.includes("TCP")
    ) {

        return "TCP";
    }


    if (
        message.includes("DATA NOT FOUND") ||
        message.includes("NO DATA") ||
        message.includes("DATA NOT AVAILABLE")
    ) {

        return "NO_DATA";
    }


    if (
        message.includes("DATE IS OLDER") ||
        message.includes("DATE OLDER") ||
        (
            message.includes("SMART METER") &&
            message.includes("OLDER") &&
            message.includes("FORMY") &&
            message.includes(
                "SAP_MRO_DOWNLOAD_DATE"
            )
        )
    ) {

        return "DATE_OLDER";
    }


    if (
        message.includes("TIMEOUT") ||
        message.includes("TIME OUT")
    ) {

        return "TIMEOUT";
    }


    return "OTHER";
}


/* ============================================================
   FILTER EVENTS
   ============================================================ */

$("#departmentFilterBYPL")
    .off("change")
    .on(
        "change",
        applyFiltersBYPL
    );


$("#reasonFilterBYPL")
    .off("change")
    .on(
        "change",
        applyFiltersBYPL
    );


$("#phaseFilterBYPL")
    .off("change")
    .on(
        "change",
        applyFiltersBYPL
    );


$("#meterMakeFilterBYPL")
    .off("change")
    .on(
        "change",
        applyFiltersBYPL
    );


$("#entryDateFromBYPL")
    .off("change")
    .on(
        "change",
        applyFiltersBYPL
    );


$("#entryDateToBYPL")
    .off("change")
    .on(
        "change",
        applyFiltersBYPL
    );


/* ============================================================
   CLEAR FILTER BUTTON
   ============================================================ */

$("#btnClearDownloadFiltersBYPL")
    .off("click")
    .on("click", function () {

        clearFiltersBYPL();


        filteredDataBypl =
            [
                ...allDownloadDataBypl
            ];


        renderTableBYPL(
            filteredDataBypl
        );
    });


/* ============================================================
   CLEAR FILTERS
   ============================================================ */

function clearFiltersBYPL() {

    $("#departmentFilterBYPL")
        .val("");


    $("#reasonFilterBYPL")
        .val("");


    $("#phaseFilterBYPL")
        .val("");


    $("#meterMakeFilterBYPL")
        .val("");


    $("#entryDateFromBYPL")
        .val("");


    $("#entryDateToBYPL")
        .val("");


    $("#entryDateToBYPL")
        .removeClass(
            "is-invalid"
        );
}


/* ============================================================
   APPLY FILTERS
   ============================================================ */

function applyFiltersBYPL() {

    const selectedDepartment =
        String(
            $("#departmentFilterBYPL")
                .val() || ""
        )
            .trim()
            .toUpperCase();


    const selectedReason =
        String(
            $("#reasonFilterBYPL")
                .val() || ""
        )
            .trim()
            .toUpperCase();


    const selectedPhase =
        String(
            $("#phaseFilterBYPL")
                .val() || ""
        )
            .trim()
            .toUpperCase();


    const selectedMeterMake =
        String(
            $("#meterMakeFilterBYPL")
                .val() || ""
        )
            .trim()
            .toUpperCase();


    const dateFrom =
        $("#entryDateFromBYPL")
            .val();


    const dateTo =
        $("#entryDateToBYPL")
            .val();


    /* DATE VALIDATION */

    if (
        dateFrom &&
        dateTo &&
        dateFrom > dateTo
    ) {

        $("#entryDateToBYPL")
            .addClass(
                "is-invalid"
            );


        filteredDataBypl = [];


        renderTableBYPL(
            filteredDataBypl
        );


        return;
    }


    $("#entryDateToBYPL")
        .removeClass(
            "is-invalid"
        );


    /* FILTER */

    filteredDataBypl =
        allDownloadDataBypl.filter(
            item => {

                /* DEPARTMENT */

                const itemDepartment =
                    String(
                        item.sapDepartment || ""
                    )
                        .trim()
                        .toUpperCase();


                const departmentMatch =
                    selectedDepartment === "" ||
                    itemDepartment ===
                    selectedDepartment;


                /* FAILURE REASON */

                const itemCategory =
                    getFailureCategoryBYPL(
                        item.schedulerMessage
                    );


                const reasonMatch =
                    selectedReason === "" ||
                    itemCategory ===
                    selectedReason;


                /* PHASE */

                const itemPhase =
                    String(
                        item.phase || ""
                    )
                        .trim()
                        .toUpperCase();


                const phaseMatch =
                    selectedPhase === "" ||
                    itemPhase ===
                    selectedPhase;


                /* METER MAKE */

                const itemMeterMake =
                    String(
                        item.meterType || ""
                    )
                        .trim()
                        .toUpperCase();


                const meterMakeMatch =
                    selectedMeterMake === "" ||
                    itemMeterMake ===
                    selectedMeterMake;


                /* ENTRY DATE */

                let dateMatch = true;


                if (
                    dateFrom ||
                    dateTo
                ) {

                    if (
                        !item.entryDate
                    ) {

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
                                formatDateForFilterBYPL(
                                    item.entryDate
                                );


                            if (
                                dateFrom &&
                                entryDateString <
                                dateFrom
                            ) {

                                dateMatch = false;
                            }


                            if (
                                dateTo &&
                                entryDateString >
                                dateTo
                            ) {

                                dateMatch = false;
                            }
                        }
                    }
                }


                return (
                    departmentMatch &&
                    reasonMatch &&
                    phaseMatch &&
                    meterMakeMatch &&
                    dateMatch
                );
            }
        );


    renderTableBYPL(
        filteredDataBypl
    );
}


/* ============================================================
   BADGE STYLE
   ============================================================ */

function getFailureBadgeStyleBYPL(
    reason
) {

    const category =
        getFailureCategoryBYPL(
            reason
        );


    switch (category) {

        case "SYSTEM_TITLE":

            return {
                background: "#dc3545",
                color: "#ffffff"
            };


        case "TCP":

            return {
                background: "#fd7e14",
                color: "#ffffff"
            };


        case "NO_DATA":

            return {
                background: "#ffc107",
                color: "#000000"
            };


        case "DATE_OLDER":

            return {
                background: "#20c997",
                color: "#ffffff"
            };


        case "TIMEOUT":

            return {
                background: "#6c757d",
                color: "#ffffff"
            };


        default:

            return {
                background: "#6c757d",
                color: "#ffffff"
            };
    }
}


/* ============================================================
   FORMAT ENTRY DATE
   ============================================================ */

function formatEntryDateBYPL(value) {

    if (!value) {

        return "--";
    }


    const date =
        new Date(value);


    if (
        isNaN(
            date.getTime()
        )
    ) {

        return String(value);
    }


    return date.toLocaleString(
        "en-GB",
        {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit",
            second: "2-digit"
        }
    );
}


/* ============================================================
   FORMAT FAILED SINCE DATE
   ============================================================ */

function formatDownloadFailedSinceBYPL(
    value
) {

    if (!value) {

        return "--";
    }


    const date =
        new Date(value);


    if (
        isNaN(
            date.getTime()
        )
    ) {

        return String(value);
    }


    return date.toLocaleDateString(
        "en-GB",
        {
            day: "2-digit",
            month: "2-digit",
            year: "numeric"
        }
    );
}


/* ============================================================
   GET FAILED DAYS
   Handles camelCase + PascalCase JSON
   ============================================================ */

function getDownloadFailedDaysBYPL(
    item
) {

    const value =
        item.downloadFailedDays ??
        item.DOWNLOAD_FAILED_DAYS ??
        item.DownloadFailedDays;


    if (
        value === null ||
        value === undefined ||
        value === ""
    ) {

        return null;
    }


    const number =
        Number(value);


    return Number.isFinite(number)
        ? number
        : null;
}


/* ============================================================
   GET FAILED SINCE
   Handles camelCase + PascalCase JSON
   ============================================================ */

function getDownloadFailedSinceBYPL(
    item
) {

    return (
        item.downloadFailedSince ??
        item.DOWNLOAD_FAILED_SINCE ??
        item.DownloadFailedSince ??
        null
    );
}


/* ============================================================
   RENDER BYPL TABLE
   ============================================================ */

function renderTableBYPL(data) {

    const tbody =
        $("#downloadSummaryBodyBYPL");


    tbody.empty();


    if (
        !Array.isArray(data) ||
        data.length === 0
    ) {

        tbody.html(`
            <tr>
                <td colspan="13"
                    class="text-center py-4 text-muted">

                    No Records Found

                </td>
            </tr>
        `);

        return;
    }


    data.forEach(
        (x, index) => {

            /* =================================================
               STATUS
               ================================================= */

            const badgeStyle =
                getFailureBadgeStyleBYPL(
                    x.schedulerMessage
                );


            /* =================================================
               LAST ENTRY DATE
               ================================================= */

            const entryDate =
                formatEntryDateBYPL(
                    x.entryDate
                );


            /* =================================================
               FAILED SINCE
               ================================================= */

            const failedSince =
                getDownloadFailedSinceBYPL(
                    x
                );


            const failedSinceFormatted =
                formatDownloadFailedSinceBYPL(
                    failedSince
                );


            /* =================================================
               FAILED DAYS
               ================================================= */

            const failedDays =
                getDownloadFailedDaysBYPL(
                    x
                );


            /*
             * IMPORTANT:
             * Red only when Total Failed Days > 5.
             */

            const failedMoreThan5 =
                failedDays !== null &&
                failedDays > 5;


            const failedSinceClass =
                failedMoreThan5
                    ? "download-failed-danger"
                    : "";


            const failedDaysClass =
                failedMoreThan5
                    ? "download-failed-danger"
                    : "";


            /* =================================================
               TABLE ROW

               ORDER:
               S.No.
               Cons Ref
               Meter Number
               Phase
               Department
               Division
               Seq No
               Address
               Meter Make
               Failed Since
               Total Failed Days
               Status
               Last Entry Date
               ================================================= */

            tbody.append(`
                <tr>

                    <!-- S.NO -->
                    <td class="text-center">
                        ${index + 1}
                    </td>


                    <!-- CONS REF -->
                    <td>
                        ${escapeHtml(
                x.consRef || ""
            )}
                    </td>


                    <!-- METER NUMBER -->
                    <td>
                        ${escapeHtml(
                x.meterNumber || ""
            )}
                    </td>


                    <!-- PHASE -->
                    <td class="text-center">
                        ${escapeHtml(
                x.phase || ""
            )}
                    </td>


                    <!-- DEPARTMENT -->
                    <td>
                        ${escapeHtml(
                x.sapDepartment || ""
            )}
                    </td>


                    <!-- DIVISION -->
                    <td>
                        ${escapeHtml(
                x.sapDivision || ""
            )}
                    </td>


                    <!-- SEQ NO -->
                    <td>
                        ${escapeHtml(
                x.sapSeqNo || ""
            )}
                    </td>


                    <!-- ADDRESS -->
                    <td class="address-cell">
                        ${escapeHtml(
                x.address || "--"
            )}
                    </td>


                    <!-- METER MAKE -->
                    <td class="text-center">
                        ${escapeHtml(
                x.meterType || ""
            )}
                    </td>


                    <!-- FAILED SINCE -->
                    <td
                        class="text-center ${failedSinceClass}">

                        ${escapeHtml(
                failedSinceFormatted
            )}

                    </td>


                    <!-- TOTAL FAILED DAYS -->
                    <td
                        class="text-center ${failedDaysClass}">

                        ${failedDays !== null
                    ? escapeHtml(
                        String(
                            failedDays
                        )
                    )
                    : "--"
                }

                    </td>


                    <!-- STATUS -->
                    <td class="status-cell">

                        <span
                            class="badge"
                            style="
                                background-color:${badgeStyle.background};
                                color:${badgeStyle.color};
                                white-space:normal;
                                display:inline-block;
                                line-height:1.35;
                                padding:6px 10px;
                                max-width:100%;
                            "
                        >

                            ${escapeHtml(
                    x.schedulerMessage ||
                    "--"
                )}

                        </span>

                    </td>


                    <!-- LAST ENTRY DATE -->
                    <td class="text-center">

                        ${escapeHtml(
                    entryDate
                )}

                    </td>

                </tr>
            `);
        }
    );
}


/* ============================================================
   ESCAPE HTML
   ============================================================ */

function escapeHtml(value) {

    if (
        value === null ||
        value === undefined
    ) {

        return "";
    }


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
   EXPORT EXCEL
   ============================================================ */

function exportTableToExcelBYPL() {

    let table = null;


    /* =========================================================
       FIND ACTUAL BYPL TABLE
       ========================================================= */

    const possibleTables =
        document.querySelectorAll(
            'table[id*="downloadSummary"], table[id*="DownloadSummary"]'
        );


    for (
        const tbl of possibleTables
    ) {

        const rows =
            tbl.querySelectorAll(
                "tbody tr"
            );


        let hasData = false;


        rows.forEach(
            row => {

                const cells =
                    row.querySelectorAll(
                        "td"
                    );


                if (
                    cells.length >= 13
                ) {

                    hasData = true;
                }
            }
        );


        if (hasData) {

            table = tbl;

            break;
        }
    }


    /* =========================================================
       FALLBACK
       ========================================================= */

    if (!table) {

        table =
            document.getElementById(
                "downloadSummaryTableBYPL"
            );
    }


    if (!table) {

        console.error(
            "BYPL Download Failed table not found."
        );


        alert(
            "Download Failed table not found."
        );


        return;
    }


    /* =========================================================
       HEADER
       ========================================================= */

    const headerCells =
        table.querySelectorAll(
            "thead th"
        );


    if (
        headerCells.length === 0
    ) {

        alert(
            "Table header not found."
        );


        return;
    }


    const headers = [];


    headerCells.forEach(
        th => {

            headers.push(
                th.innerText
                    .replace(
                        /\s+/g,
                        " "
                    )
                    .trim()
            );

        }
    );


    /* =========================================================
       DATA ROWS
       ========================================================= */

    const tableRows =
        table.querySelectorAll(
            "tbody tr"
        );


    const data = [];


    let sno = 1;


    tableRows.forEach(
        row => {

            const cells =
                row.querySelectorAll(
                    "td"
                );


            if (
                cells.length === 0 ||
                cells.length < 13
            ) {

                return;
            }


            const completeText =
                row.innerText
                    .trim()
                    .toLowerCase();


            if (
                completeText.includes(
                    "no records found"
                )
            ) {

                return;
            }


            const rowData = [];


            /* S.NO */

            rowData.push(
                sno
            );


            sno++;


            /* OTHER COLUMNS */

            for (
                let i = 1;
                i < 13;
                i++
            ) {

                rowData.push(
                    cells[i]
                        ? cells[i].innerText
                            .replace(
                                /\s+/g,
                                " "
                            )
                            .trim()
                        : ""
                );
            }


            data.push(
                rowData
            );
        }
    );


    console.log(
        "BYPL export table:",
        table
    );


    console.log(
        "BYPL export rows:",
        data.length
    );


    if (
        data.length === 0
    ) {

        alert(
            "No Download Failed data available to export."
        );


        return;
    }


    /* =========================================================
       CREATE WORKBOOK
       ========================================================= */

    const wb =
        XLSX.utils.book_new();


    /* =========================================================
       CREATE SHEET
       ========================================================= */

    const ws =
        XLSX.utils.aoa_to_sheet(
            [
                headers,
                ...data
            ]
        );


    /* =========================================================
       COLORS
       ========================================================= */

    const NAVY_BLUE =
        "17365D";


    const WHITE =
        "FFFFFF";


    const BLACK =
        "000000";


    const RED =
        "DC3545";


    const BORDER_COLOR =
        "7F7F7F";


    /* =========================================================
       BORDER
       ========================================================= */

    const allBorders = {

        top: {
            style: "thin",
            color: {
                rgb: BORDER_COLOR
            }
        },

        bottom: {
            style: "thin",
            color: {
                rgb: BORDER_COLOR
            }
        },

        left: {
            style: "thin",
            color: {
                rgb: BORDER_COLOR
            }
        },

        right: {
            style: "thin",
            color: {
                rgb: BORDER_COLOR
            }
        }

    };


    /* =========================================================
       HEADER STYLE
       ========================================================= */

    const headerStyle = {

        fill: {
            patternType: "solid",
            fgColor: {
                rgb: NAVY_BLUE
            }
        },

        font: {
            name: "Calibri",
            sz: 11,
            bold: true,
            color: {
                rgb: WHITE
            }
        },

        alignment: {
            horizontal: "center",
            vertical: "center",
            wrapText: true
        },

        border: allBorders
    };


    /* =========================================================
       NORMAL STYLE
       ========================================================= */

    const normalStyle = {

        font: {
            name: "Calibri",
            sz: 10,
            color: {
                rgb: BLACK
            }
        },

        alignment: {
            vertical: "center",
            wrapText: true
        },

        border: allBorders
    };


    /* =========================================================
       CENTER STYLE
       ========================================================= */

    const centerStyle = {

        font: {
            name: "Calibri",
            sz: 10,
            color: {
                rgb: BLACK
            }
        },

        alignment: {
            horizontal: "center",
            vertical: "center",
            wrapText: true
        },

        border: allBorders
    };


    /* =========================================================
       RED FAILED DAYS STYLE
       ========================================================= */

    const failedDaysStyle = {

        fill: {
            patternType: "solid",
            fgColor: {
                rgb: RED
            }
        },

        font: {
            name: "Calibri",
            sz: 10,
            bold: true,
            color: {
                rgb: WHITE
            }
        },

        alignment: {
            horizontal: "center",
            vertical: "center"
        },

        border: allBorders
    };


    /* =========================================================
       HEADER
       ========================================================= */

    for (
        let col = 0;
        col < 13;
        col++
    ) {

        const address =
            XLSX.utils.encode_cell({
                r: 0,
                c: col
            });


        if (ws[address]) {

            ws[address].s =
                headerStyle;
        }
    }


    /* =========================================================
       DATA FORMATTING

       A = S.No.
       B = Cons Ref
       C = Meter Number
       D = Phase
       E = Department
       F = Division
       G = Seq No
       H = Address
       I = Meter Make
       J = Failed Since
       K = Failed Days
       L = Status
       M = Last Entry Date
       ========================================================= */

    for (
        let row = 1;
        row <= data.length;
        row++
    ) {

        /* =====================================================
           S.NO
           ===================================================== */

        const snoAddress =
            `A${row + 1}`;


        if (
            ws[snoAddress]
        ) {

            ws[snoAddress].t =
                "n";


            ws[snoAddress].v =
                row;


            ws[snoAddress].z =
                "0";


            ws[snoAddress].s =
                centerStyle;
        }


        /* =====================================================
           OTHER COLUMNS
           ===================================================== */

        for (
            let col = 1;
            col < 13;
            col++
        ) {

            const address =
                XLSX.utils.encode_cell({
                    r: row,
                    c: col
                });


            if (!ws[address]) {

                continue;
            }


            /*
             * Center:
             *
             * D = Phase
             * I = Meter Make
             * J = Failed Since
             * K = Failed Days
             * M = Last Entry Date
             */

            if (
                col === 3 ||
                col === 8 ||
                col === 9 ||
                col === 10 ||
                col === 12
            ) {

                ws[address].s =
                    centerStyle;

            }
            else {

                ws[address].s =
                    normalStyle;
            }
        }


        /* =====================================================
           FAILED DAYS

           K = Total Failed Days

           Red only when > 5 days.
           ===================================================== */

        const failedDaysAddress =
            `K${row + 1}`;


        if (
            ws[failedDaysAddress]
        ) {

            const failedDays =
                parseFloat(
                    String(
                        ws[
                            failedDaysAddress
                        ].v
                    )
                        .replace(
                            /[^0-9.-]/g,
                            ""
                        )
                );


            if (
                !isNaN(failedDays) &&
                failedDays > 5
            ) {

                /*
                 * Highlight both:
                 *
                 * J = Failed Since
                 * K = Failed Days
                 */

                const failedSinceAddress =
                    `J${row + 1}`;


                if (
                    ws[
                    failedSinceAddress
                    ]
                ) {

                    ws[
                        failedSinceAddress
                    ].s =
                        failedDaysStyle;
                }


                ws[
                    failedDaysAddress
                ].s =
                    failedDaysStyle;
            }
        }
    }


    /* =========================================================
       COLUMN WIDTHS

       A = S.No.
       B = Cons Ref
       C = Meter Number
       D = Phase
       E = Department
       F = Division
       G = Seq No
       H = Address
       I = Meter Make
       J = Failed Since
       K = Failed Days
       L = Status
       M = Last Entry Date
       ========================================================= */

    ws["!cols"] = [

        {
            wch: 7
        },

        {
            wch: 17
        },

        {
            wch: 20
        },

        {
            wch: 10
        },

        {
            wch: 17
        },

        {
            wch: 18
        },

        {
            wch: 12
        },

        {
            wch: 55
        },

        {
            wch: 16
        },

        {
            wch: 23
        },

        {
            wch: 22
        },

        {
            wch: 50
        },

        {
            wch: 22
        }
    ];


    /* =========================================================
       ROW HEIGHT
       ========================================================= */

    ws["!rows"] = [];


    ws["!rows"][0] = {
        hpt: 32
    };


    for (
        let i = 1;
        i <= data.length;
        i++
    ) {

        ws["!rows"][i] = {
            hpt: 30
        };
    }


    /* =========================================================
       AUTOFILTER
       ========================================================= */

    ws["!autofilter"] = {

        ref:
            `A1:M${data.length + 1}`
    };


    /* =========================================================
       FREEZE HEADER
       ========================================================= */

    ws["!freeze"] = {

        xSplit: 0,

        ySplit: 1
    };


    /* =========================================================
       HIDE GRIDLINES
       ========================================================= */

    ws["!sheetViews"] = [

        {
            showGridLines: false
        }
    ];


    /* =========================================================
       ADD SHEET
       ========================================================= */

    XLSX.utils.book_append_sheet(
        wb,
        ws,
        "Download Failed"
    );


    /* =========================================================
       FILE NAME
       ========================================================= */

    const today =
        new Date()
            .toISOString()
            .split("T")[0];


    XLSX.writeFile(
        wb,
        `Download_Failed_Summary_BYPL_${today}.xlsx`
    );
}


/* ============================================================
   EXPORT CSV
   ============================================================ */

function exportTableToCSVBYPL() {

    const table =
        document.getElementById(
            "downloadSummaryTableBYPL"
        );


    if (!table) {

        console.error(
            "BYPL download table not found."
        );


        return;
    }


    const rows =
        table.querySelectorAll(
            "tr"
        );


    const csv = [];


    rows.forEach(
        row => {

            const columns =
                row.querySelectorAll(
                    "th, td"
                );


            const rowData = [];


            columns.forEach(
                column => {

                    const value =
                        column.innerText
                            .replace(
                                /"/g,
                                '""'
                            )
                            .replace(
                                /\r?\n|\r/g,
                                " "
                            )
                            .trim();


                    rowData.push(
                        `"${value}"`
                    );
                }
            );


            csv.push(
                rowData.join(",")
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


    const url =
        URL.createObjectURL(
            blob
        );


    const link =
        document.createElement(
            "a"
        );


    link.href = url;


    link.download =
        "Download_Failed_Summary_BYPL_" +
        new Date()
            .toISOString()
            .split("T")[0] +
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
   FORMAT DATE FOR FILTER
   ============================================================ */

function formatDateForFilterBYPL(
    value
) {

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