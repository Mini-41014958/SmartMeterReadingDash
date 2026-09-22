let allDownloadData = [];
let filteredData = [];


$("#btnViewDownloadDetails").on("click", function ()
{

    const modal = new bootstrap.Modal(document.getElementById("downloadSummaryModal"));
    modal.show();
    loadDownloadSummary();
});


async function loadDownloadSummary()
{

    try {

        $("#downloadSummaryBody").html(`
                <tr>
                    <td colspan="13" class="text-center py-4">
                        <div class="spinner-border spinner-border-sm text-primary"></div>
                        Loading...
                    </td>
                </tr>
            `);


        const readingMonth = getReadingMonth();

        const apiUrl =`${getApiUrl("dashboardapi/meter-download-detailed-summary")}?readingMonth=${encodeURIComponent(readingMonth)}`;

        const response = await fetch(apiUrl);

        if (!response.ok)
        {

            const errorText = await response.text();

            console.error( "API Error:", response.status, errorText);

            throw new Error(`Unable to load data (${response.status})`);
        }

        const result = await response.json();

        allDownloadData = Array.isArray(result)
            ? result
            : [];

        filteredData = [...allDownloadData];

        loadDepartmentFilter();
        loadDepartmentFilter();
        loadDivisionFilter();

        loadReasonFilter();

        loadPhaseFilter();

        loadMeterMakeFilter();

        clearFilters();

        renderTable(filteredData);

    }
    catch (err)
    {

        console.error( "loadDownloadSummary error:",err);

        $("#downloadSummaryBody").html(`
                <tr>
                    <td colspan="13"
                        class="text-center text-danger py-4">

                        Failed to load data.

                        Error:${escapeHtml(err.message)}

                    </td>
                </tr>
            `);
    }
}

// DEPARTMENT FILTER
function loadDepartmentFilter()
{

    const ddl = $("#departmentFilter");
    ddl.empty();
    ddl.append(`
            <option value="">
                All Departments
            </option>
        `);

    const departments = [...new Set(allDownloadData.map(x => x.sapDepartment).filter(x => x !== null && x !== undefined &&x !== ""))];

    departments.sort((a, b) => String(a).localeCompare(String(b)));

    departments.forEach(department =>
    {

        ddl.append(` <option value="${escapeHtml(String(department))}"> ${escapeHtml(String(department))}  </option>`);

    });
}

// DIVISION FILTER

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
                .filter(x =>
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

function loadReasonFilter()
{

    const ddl = $("#reasonFilter");

    ddl.empty();

    ddl.append(`
            <option value="">All Reasons</option>

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


// PHASE FILTER

function loadPhaseFilter()
{

    const ddl = $("#phaseFilter");
    ddl.empty();

    ddl.append(` <option value=""> All Phases  </option> `);


    const phases = [...new Set(allDownloadData.map(x => x.phase).filter(x =>
                    x !== null &&
                    x !== undefined &&
                    x !== ""
                )
        )
    ];


    phases.sort((a, b) =>String(a).localeCompare(String(b),
            undefined,
            {
                numeric: true
            }
        )
    );


    phases.forEach(phase =>
    {

        ddl.append(` <option value="${escapeHtml(String(phase))}">${escapeHtml(String(phase))} </option>`);

    });
}

// METER MAKE FILTER

function loadMeterMakeFilter() {

    const ddl = $("#meterMakeFilter");

    ddl.empty();

    ddl.append(` <option value=""> All Meter Makes </option>`);

    const meterMakes = [...new Set( allDownloadData .map(x => x.meterType).filter(x =>
                    x !== null &&
                    x !== undefined &&
                    x !== ""
                )
        )
    ];

    meterMakes.sort((a, b) => String(a).localeCompare(String(b)));

    meterMakes.forEach(make =>
    {

        ddl.append(`
                <option value="${escapeHtml(String(make))}">
                    ${escapeHtml(String(make))}
                </option>
            `);

    });
}

// FILTER EVENTS

$("#departmentFilter").on("change", applyFilters);

$("#divisionFilter").on("change", applyFilters);

$("#reasonFilter").on("change",applyFilters);

$("#phaseFilter").on("change",applyFilters);

$("#meterMakeFilter").on("change",applyFilters);

$("#entryDateFrom").on("change",applyFilters);

$("#entryDateTo").on("change", applyFilters);


// CLEAR FILTER BUTTON

$("#btnClearDownloadFilters").on("click",function ()
    {
        clearFilters();
        filteredData = [...allDownloadData];
        renderTable( filteredData );
    }
);

// CLEAR FILTERS

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

// APPLY FILTERS

function applyFilters()
{

    const department = ($("#departmentFilter").val() || "").toUpperCase();

    const division = ($("#divisionFilter").val() || "").toUpperCase();

    const reason = ($("#reasonFilter").val() || "").toUpperCase();

    const phase = ($("#phaseFilter").val() || "").toUpperCase();

    const meterMake = ($("#meterMakeFilter").val() || "").toUpperCase();

    const dateFrom = $("#entryDateFrom").val();

    const dateTo = $("#entryDateTo").val();

    // DATE VALIDATION

    if (dateFrom && dateTo && dateFrom > dateTo)
    {

        $("#entryDateTo").addClass( "is-invalid");

        filteredData = [];

        renderTable(filteredData);

        return;
    }

    $("#entryDateTo").removeClass("is-invalid");

    // FILTER DATA

    filteredData = allDownloadData.filter(item =>
    {

            // DEPARTMENT
            const itemDepartment =(item.sapDepartment || "").toUpperCase();

            const departmentMatch = department === "" || itemDepartment === department;

        // DIVISION

            const itemDivision = (item.sapDivision || "").toUpperCase();
            const divisionMatch =  division === "" || itemDivision === division;

            // FAILED REASON

            const message = (item.schedulerMessage || "").toUpperCase();

            let reasonMatch = true;

        if (reason !== "")
        {

            if (reason === "OTHER")
            {

             reasonMatch =!message.includes( "SYSTEM TITLE") && !message.includes("TCP") && !message.includes( "NO DATA" ) && !message.includes( "TIMEOUT" );

             }
            else
             {
                reasonMatch = message.includes(reason );
             }
        }


            // PHASE

            const itemPhase = (item.phase || "").toUpperCase();

            const phaseMatch = phase === "" || itemPhase === phase;

            // METER MAKE

            const itemMeterMake = (item.meterType || "").toUpperCase();


            const meterMakeMatch = meterMake === "" || itemMeterMake === meterMake;

            // ENTRY DATE

            let dateMatch = true;

        if (dateFrom || dateTo)
        {

            if (!item.entryDate)
            {
               dateMatch = false;

            }
            else {
                const entryDate = new Date(item.entryDate);

                if (isNaN(entryDate.getTime()))
                {
                    dateMatch = false;

                 }
                else {
                    const entryDateString = formatDateForFilter(item.entryDate);

                        if (dateFrom && entryDateString < dateFrom)
                        {
                             dateMatch = false;
                        }

                       if (dateTo && entryDateString > dateTo)
                       {
                            dateMatch = false;
                        }
                    }
                }
            }

        return ( departmentMatch && divisionMatch && reasonMatch && phaseMatch && meterMakeMatch &&dateMatch );

        });

    renderTable( filteredData);
}


// RENDER TABLE

function renderTable(data) { const tbody = $("#downloadSummaryBody");

    tbody.empty();

    if (!data || data.length === 0) {
        tbody.html(`
            <tr>
                <td colspan="13"
                    class="text-center py-4">

                    No Records Found

                </td>
            </tr>
        `);

        return;
    }


    data.forEach((x, index) =>
    {
        let badgeColor = "#6c757d";
        let textColor = "#fff";
        const message = (x.schedulerMessage || "").toUpperCase();

        // STATUS COLORS

        if (message.includes("SYSTEM TITLE"))
        {
            badgeColor = "#dc3545";
        }
        else if (message.includes("TCP"))
        {
            badgeColor = "#fd7e14";
        }
        else if (message.includes("NO DATA"))
        {
            badgeColor = "#ffc107";
            textColor = "#000";
        }
        else if (message.includes("TIMEOUT"))
        {
            badgeColor = "#6c757d";
        }

        // ENTRY DATE

        let entryDate = "--";

        if (x.entryDate)
        {
            const parsedDate = new Date(x.entryDate);

            if (!isNaN(parsedDate.getTime()))
            {
                entryDate = parsedDate.toLocaleString("en-GB");
            }
        }

        // DOWNLOAD FAILED SINCE

        let downloadFailedSince = "--";

        if (x.downloadFailedSince)
        {
            const failedSinceDate = new Date(x.downloadFailedSince);

            if (!isNaN(failedSinceDate.getTime()))
            {
                downloadFailedSince = failedSinceDate.toLocaleDateString("en-GB");
            }
        }

        // DOWNLOAD FAILED DAYS

        let downloadFailedDays = "--";

        if (x.downloadFailedDays !== null && x.downloadFailedDays !== undefined && x.downloadFailedDays !== "")
        {
            downloadFailedDays = Number(x.downloadFailedDays);
        }

        // HIGHLIGHT IF FAILED > 5 DAYS

        const failedDaysNumber = Number(x.downloadFailedDays);

        const isMoreThan5Days = !isNaN(failedDaysNumber) && failedDaysNumber > 5;

        let failedSinceStyle = "";
        let failedDaysStyle = "";

        if (isMoreThan5Days)
        {
            failedSinceStyle = `
                background-color: #dc3545;
                color: #fff;
                font-weight: 700;
                text-align: center;
            `;

            failedDaysStyle = `
                background-color: #dc3545;
                color: #fff;
                font-weight: 700;
                text-align: center;
            `;
        }
        else
        {
            failedSinceStyle = `
                text-align: center;
            `;

            failedDaysStyle = `
                text-align: center;
                font-weight: 600;
            `;
        }

        // TABLE ROW

        tbody.append(`
            <tr>

                <td class="text-center fw-semibold"> ${index + 1} </td>

                <td> ${escapeHtml(x.consRef ?? "")}  </td>

                <td>${escapeHtml(x.meterNumber ?? "")}  </td>

                <td class="text-center">${escapeHtml(x.phase ?? "")} </td>

                <td> ${escapeHtml(x.sapDepartment ?? "")}</td>

                <td> ${escapeHtml(x.sapDivision ?? "")}   </td>

                <td>  ${escapeHtml(x.sapSeqNo ?? "")}  </td>

                <td> ${escapeHtml(x.address ?? "--")} </td>

                <td> ${escapeHtml(x.meterType ?? "")}  </td>

                <td>
                    <span class="badge"
                          style="
                              background:${badgeColor};
                              color:${textColor};
                          ">

                        ${escapeHtml( x.schedulerMessage ?? "--" )}

                    </span>
                </td>

                <td> ${escapeHtml(entryDate)} </td>

                <td style="${failedSinceStyle}"> ${escapeHtml(downloadFailedSince)}  </td>

                <td style="${failedDaysStyle}">  ${escapeHtml(downloadFailedDays)} </td>

            </tr>
        `);

    });
}


function exportTableToExcel()
{

    const table = document.getElementById("downloadSummaryTable");

    if (!table)
    {
        console.error("Download summary table not found.");
        return;
    }

    // CREATE WORKBOOK

    const wb = XLSX.utils.book_new();
    const ws = XLSX.utils.table_to_sheet(table);
    const NAVY_BLUE = "17365D";
    const WHITE = "FFFFFF";
    const BLACK = "000000";
    const RED = "DC3545";
    const BORDER_COLOR = "7F7F7F";

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

    const headerStyle =
    {

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

    const normalStyle =
    {

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

    const centerStyle =
    {

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

    const failedDaysStyle =
    {

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

    /*
        A = S.No.
        B = Cons Ref
        C = Meter Number
        D = Phase
        E = Department
        F = Division
        G = Seq No
        H = Address
        I = Meter Make
        J = Status
        K = Entry Date
        L = Download Failed Since
        M = Download Failed Days
    */

    for (let col = 0; col < 13; col++) {

        const address =
            XLSX.utils.encode_cell({
                r: 0,
                c: col
            });

        if (ws[address]) {
            ws[address].s = headerStyle;
        }
    }

    // STYLE DATA ROWS
    const rows = table.querySelectorAll("tbody tr");


    rows.forEach((row, rowIndex) =>
    {

        const cells = row.querySelectorAll("td");


        if (cells.length !== 13)
        {
            return;
        }


        // Excel row number
        const excelRow =  rowIndex + 2;

        const snoCell = `A${excelRow}`;


        if (ws[snoCell])
        {

            ws[snoCell].t = "n";

            ws[snoCell].v =  rowIndex + 1;

            ws[snoCell].z = "0";

            ws[snoCell].s = centerStyle;
        }

        for (let col = 1; col < 13; col++)
        {

            const address =
                XLSX.utils.encode_cell({
                    r: excelRow - 1,
                    c: col
                });


            if (!ws[address]) {
                continue;
            }


            // Center columns
            if (
                col === 3 ||   // Phase
                col === 10 ||  // Entry Date
                col === 11 ||  // Failed Since
                col === 12     // Failed Days
            ) {

                ws[address].s =
                    centerStyle;

            }
            else {

                ws[address].s =
                    normalStyle;
            }
        }


        // ========================================================
        // GET DOWNLOAD FAILED DAYS
        // ========================================================

        const failedDaysText =
            cells[12]
                .innerText
                .trim();


        const failedDays =
            parseFloat(
                failedDaysText
            );


        // ========================================================
        // RED WHEN >= 5 DAYS
        // ========================================================

        if (
            !isNaN(failedDays) &&
            failedDays >= 5
        ) {

            const failedDaysCell =
                `M${excelRow}`;


            if (ws[failedDaysCell]) {

                ws[failedDaysCell].s =
                    failedDaysStyle;
            }
        }

    });


    // ============================================================
    // COLUMN WIDTHS
    // ============================================================

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
            wch: 50
        },

        {
            wch: 22
        },

        {
            wch: 23
        },

        {
            wch: 22
        }

    ];


    // ============================================================
    // ROW HEIGHTS
    // ============================================================

    ws["!rows"] = [];


    // Header height

    ws["!rows"][0] = {
        hpt: 32
    };


    // Data row height

    for (
        let i = 1;
        i < rows.length + 1;
        i++
    ) {

        ws["!rows"][i] = {
            hpt: 30
        };
    }


    // ============================================================
    // AUTOFILTER
    // ============================================================

    ws["!autofilter"] = {
        ref: "A1:M1"
    };


    // ============================================================
    // FREEZE HEADER
    // ============================================================

    ws["!freeze"] = {
        xSplit: 0,
        ySplit: 1
    };


    // ============================================================
    // SHEET VIEW
    // ============================================================

    ws["!sheetViews"] = [
        {
            showGridLines: false
        }
    ];


    // ============================================================
    // ADD WORKSHEET
    // ============================================================

    XLSX.utils.book_append_sheet(
        wb,
        ws,
        "Download Failed"
    );


    // ============================================================
    // FILE NAME
    // ============================================================

    const today =
        new Date()
            .toISOString()
            .split("T")[0];


    XLSX.writeFile(
        wb,
        "Download_Failed_Summary_BRPL_" +
        today +
        ".xlsx"
    );
}

// EXPORT TO CSV

function exportTableToCSV()
{

    let csv = [];

    document .querySelectorAll( "#downloadSummaryTable tr")
        .forEach(row =>
        {

            if (row.style.display === "none")
            {
                return;
            }

            const cols = row.querySelectorAll("th, td");

            let data = [];

            cols.forEach(col =>
            {

                const value =  col.innerText
                        .replace( /"/g, '""')
                        .replace( /\r?\n|\r/g, " " )
                        .trim();

                data.push( `"${value}"`);

            });


            csv.push( data.join(","));

        });


    const blob = new Blob( [csv.join("\n")],
            {
                type: "text/csv;charset=utf-8;"
            }
        );

    const link =  document.createElement( "a" );

    const url = URL.createObjectURL( blob);

    link.href = url;

    const today = new Date().toISOString().split("T")[0];


    link.download = "Download_Failed_Summary_BRPL_" + today + ".csv";


    document.body.appendChild(link); 

    link.click();

    document.body.removeChild(link);


    URL.revokeObjectURL(url);
}


function escapeHtml(value)
{

    return String(value)
        .replace(/&/g, "&amp;")

        .replace(/</g, "&lt;")

        .replace(/>/g, "&gt;")

        .replace(/"/g, "&quot;")

        .replace(/'/g, "&#039;");
}

function formatDateForFilter(value) {

    if (!value) {
        return "";
    }

    const date = new Date(value);

    if (isNaN(date.getTime())) {
        return "";
    }

    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");

    return `${year}-${month}-${day}`;
}