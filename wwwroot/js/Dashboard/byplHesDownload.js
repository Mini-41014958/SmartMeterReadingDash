let allHesDownloadDataBypl = [];
let filteredHesDownloadDataBypl = [];


$("#byplHesDownload").off("click").on("click", function ()
{

        const modalElement = document.getElementById( "HesDownloadSummaryModalBypl");

        if (!modalElement)
        {
             console.error(  "BYPL HES modal not found" );
              return;
        }

        const modal =  bootstrap.Modal.getOrCreateInstance(modalElement);

        modal.show();

        loadHesDownloadSummaryBypl();

    });


async function loadHesDownloadSummaryBypl()
{
    try
    {
        $("#hesDownloadSummaryBodyBypl").html(`
            <tr>
                <td colspan="9" class="text-center py-4">
                    <div class="spinner-border spinner-border-sm text-primary"></div>
                    <span class="ms-2">Loading...</span>
                </td>
            </tr>
        `);

        const readingMonth = getReadingMonth();

        const apiUrl = `${getApiUrl("dashboardapi/hes-download-meters-details-bypl")}` + `?ReadingMonth=${encodeURIComponent(readingMonth)}`;


        const response = await fetch(apiUrl);

        if (!response.ok)
        {
            const errorText = await response.text();

            throw new Error(
                `Unable to load BYPL HES download data (${response.status})`
            );
        }

        allHesDownloadDataBypl = await response.json();

        if (!Array.isArray(allHesDownloadDataBypl))
        {
            allHesDownloadDataBypl = [];
        }

        filteredHesDownloadDataBypl = [ ...allHesDownloadDataBypl];

        // Load dropdowns
        loadHesDepartmentFilterBypl();
        loadHesDivisionFilterBypl();
        loadHesPhaseFilterBypl();
        loadHesMeterMakeFilterBypl();

        // Clear previous selections
        clearHesFiltersBypl();

        // Render
        renderHesDownloadTableBypl(filteredHesDownloadDataBypl);
    }
    catch (err)
    {
        console.error("BYPL HES Download Summary Error:",err);

        $("#hesDownloadSummaryBodyBypl").html(`
            <tr>
                <td colspan="9"
                    class="text-center text-danger py-4">

                    Failed to load data.

                    Error: ${escapeHesHtmlBypl(err.message)}

                </td>
            </tr>
        `);
    }
}

function loadHesDepartmentFilterBypl()
{

    const ddl = $("#hesDepartmentFilterBypl");

    ddl.empty();

    ddl.append(` <option value=""> All Departments </option>`);

    const departments = [...new Set( allHesDownloadDataBypl.map(x => String(  x.sapDepartment || "" ).trim()).filter(Boolean)) ];


    departments.sort((a, b) =>  a.localeCompare(b));


    departments.forEach(department =>
    {
        ddl.append(
                $("<option>", {
                    value: department,
                    text: department
                })
            );

        }
    );
}

// DIVISION FILTER

function loadHesDivisionFilterBypl()
{

    const ddl =$("#hesDivisionFilterBypl");
    ddl.empty();

    ddl.append(` <option value=""> All Divisions </option> `);

    const divisions = [...new Set(allHesDownloadDataBypl .map(x => String( x.sapDivision || "").trim()).filter(Boolean))];

    divisions.sort( (a, b) =>  a.localeCompare(b));

    divisions.forEach(division =>
    {
            ddl.append( $("<option>", 
            {
                    value: division,
                    text: division
                })
            );

        }
    );
}


// PHASE FILTER

function loadHesPhaseFilterBypl()
{

    const ddl = $("#hesPhaseFilterBypl");
    ddl.empty();

    ddl.append(` <option value="">  All Phases </option> `);

    const phases = [...new Set( allHesDownloadDataBypl.map(x => String( x.phase || "").trim()).filter(Boolean))];

    phases.sort((a, b) => a.localeCompare(b, undefined,
                {
                    numeric: true
                }
            )
    );


    phases.forEach(phase =>
    {

        ddl.append($("<option>",
            {
                    value: phase,
                    text: phase
                })
            );

        }
    );
}

function loadHesMeterMakeFilterBypl()
{

    const ddl = $("#hesMeterMakeFilterBypl");

    ddl.empty();

    ddl.append(` <option value="">All Meter Makes </option>`);

    const meterMakes = [...new Set(allHesDownloadDataBypl.map(x =>String( x.meterType || "").trim()).filter(Boolean))];

    meterMakes.sort((a, b) => a.localeCompare(b));

    meterMakes.forEach(meterMake =>
    {

        ddl.append($("<option>",
            {
                    value: meterMake,
                    text: meterMake
                })
            );

        }
    );
}

// FILTER EVENTS

$("#hesDepartmentFilterBypl").off("change").on( "change", applyHesFiltersBypl);

$("#hesDivisionFilterBypl").off("change").on("change",applyHesFiltersBypl);

$("#hesPhaseFilterBypl").off("change").on("change",applyHesFiltersBypl);

$("#hesMeterMakeFilterBypl").off("change").on("change",applyHesFiltersBypl);

// CLEAR FILTER BUTTON


$("#btnClearHesDownloadFiltersBypl").off("click").on("click",function ()
        {

            clearHesFiltersBypl();

            filteredHesDownloadDataBypl = [...allHesDownloadDataBypl];

            renderHesDownloadTableBypl( filteredHesDownloadDataBypl);

        }
    );


// CLEAR FILTERS

function clearHesFiltersBypl()
{

    $("#hesDepartmentFilterBypl").val("");


    $("#hesDivisionFilterBypl").val("");


    $("#hesPhaseFilterBypl").val("");


    $("#hesMeterMakeFilterBypl").val("");
}

// APPLY FILTERS

function applyHesFiltersBypl()
{

    const department = String($("#hesDepartmentFilterBypl").val() || "" ).trim().toUpperCase();

    const division = String($("#hesDivisionFilterBypl").val() || "").trim().toUpperCase();

    const phase = String($("#hesPhaseFilterBypl").val() || "").trim().toUpperCase();

    const meterMake = String($("#hesMeterMakeFilterBypl").val() || "").trim().toUpperCase();

    filteredHesDownloadDataBypl = allHesDownloadDataBypl.filter(item =>
    {

                const itemDepartment = String(item.sapDepartment || "" ).trim().toUpperCase();

                const departmentMatch = department === "" || itemDepartment === department;

                const itemDivision = String( item.sapDivision || "").trim().toUpperCase();

                const divisionMatch = division === "" || itemDivision === division;


                const itemPhase = String( item.phase || "").trim().toUpperCase();

                const phaseMatch = phase === "" || itemPhase === phase;

                const itemMeterMake = String( item.meterType || "" ).trim().toUpperCase();

                const meterMakeMatch = meterMake === "" || itemMeterMake === meterMake;

                return ( departmentMatch && divisionMatch && phaseMatch && meterMakeMatch);

            }
        );


    // Render filtered data
    renderHesDownloadTableBypl(filteredHesDownloadDataBypl);
}


// RENDER TABLE

function renderHesDownloadTableBypl(data)
{

    const tbody = $("#hesDownloadSummaryBodyBypl");

    tbody.empty();

    // No records
    if (!data || data.length === 0)
    {

        tbody.html(`
                <tr>
                    <td colspan="9"
                        class="text-center py-4">

                        No Records Found

                    </td>
                </tr>
            `);

        return;
    }


    // Render rows
    data.forEach((x, index) =>
    {

            tbody.append(`
                    <tr>

                        <!-- S.No -->
                        <td class="text-center fw-semibold"> ${index + 1} </td>

                        <!-- Cons Ref -->
                        <td>${escapeHesHtmlBypl( x.consRef)} </td>


                        <!-- Meter Number -->
                        <td>${escapeHesHtmlBypl( x.meterNumber)}</td>


                        <!-- Phase -->
                        <td class="text-center"> ${escapeHesHtmlBypl( x.phase)}</td>


                        <!-- Department -->
                        <td> ${escapeHesHtmlBypl( x.sapDepartment)}</td>


                        <!-- Division -->
                        <td>${escapeHesHtmlBypl( x.sapDivision)}</td>


                        <!-- Seq No -->
                        <td> ${escapeHesHtmlBypl(x.sapSeqNo )} </td>


                        <!-- Address -->
                        <td class="address-cell"> ${escapeHesHtmlBypl(x.address, "--" )} </td>


                        <!-- Meter Make -->
                        <td> ${escapeHesHtmlBypl(x.meterType )} </td>

                    </tr>
                `);

        }
    );
}


// EXCEL EXPORT - BYPL HES

// ============================================================
// EXCEL EXPORT - BYPL HES DOWNLOAD
// ============================================================

function exportHesDownloadByplTableToExcel() {
    const table =
        document.getElementById(
            "hesDownloadSummaryTableBypl"
        );


    if (!table) {
        console.error(
            "BYPL HES table not found"
        );

        alert(
            "BYPL HES Download table not found."
        );

        return;
    }


    // ============================================================
    // CHECK DATA
    // ============================================================

    const bodyRows =
        table.querySelectorAll(
            "tbody tr"
        );


    let hasData = false;


    bodyRows.forEach(row => {
        const cells =
            row.querySelectorAll("td");


        if (cells.length === 9) {
            const text =
                row.innerText
                    .trim()
                    .toLowerCase();


            if (
                !text.includes(
                    "no records found"
                )
            ) {
                hasData = true;
            }
        }
    });


    if (!hasData) {
        alert(
            "No BYPL HES Download data available to export."
        );

        return;
    }


    // ============================================================
    // CREATE WORKBOOK
    // ============================================================

    const wb =
        XLSX.utils.book_new();


    // ============================================================
    // CREATE WORKSHEET
    // ============================================================

    const ws =
        XLSX.utils.table_to_sheet(
            table
        );


    // ============================================================
    // REPORT COLORS
    // ============================================================

    const NAVY_BLUE =
        "17365D";

    const WHITE =
        "FFFFFF";

    const BLACK =
        "000000";

    const BORDER_COLOR =
        "7F7F7F";


    // ============================================================
    // BORDER
    // ============================================================

    const allBorders =
    {
        top:
        {
            style: "thin",
            color:
            {
                rgb: BORDER_COLOR
            }
        },

        bottom:
        {
            style: "thin",
            color:
            {
                rgb: BORDER_COLOR
            }
        },

        left:
        {
            style: "thin",
            color:
            {
                rgb: BORDER_COLOR
            }
        },

        right:
        {
            style: "thin",
            color:
            {
                rgb: BORDER_COLOR
            }
        }
    };


    // ============================================================
    // HEADER STYLE
    // ============================================================

    const headerStyle =
    {
        fill:
        {
            patternType: "solid",
            fgColor:
            {
                rgb: NAVY_BLUE
            }
        },

        font:
        {
            name: "Calibri",
            sz: 11,
            bold: true,
            color:
            {
                rgb: WHITE
            }
        },

        alignment:
        {
            horizontal: "center",
            vertical: "center",
            wrapText: true
        },

        border: allBorders
    };


    // ============================================================
    // NORMAL DATA STYLE
    // ============================================================

    const normalStyle =
    {
        font:
        {
            name: "Calibri",
            sz: 10,
            color:
            {
                rgb: BLACK
            }
        },

        alignment:
        {
            vertical: "center",
            wrapText: true
        },

        border: allBorders
    };


    // ============================================================
    // CENTER DATA STYLE
    // ============================================================

    const centerStyle =
    {
        font:
        {
            name: "Calibri",
            sz: 10,
            color:
            {
                rgb: BLACK
            }
        },

        alignment:
        {
            horizontal: "center",
            vertical: "center",
            wrapText: true
        },

        border: allBorders
    };


    // ============================================================
    // STYLE HEADER
    // ============================================================

    for (
        let col = 0;
        col < 9;
        col++
    ) {
        const address =
            XLSX.utils.encode_cell(
                {
                    r: 0,
                    c: col
                });


        if (ws[address]) {
            ws[address].s =
                headerStyle;
        }
    }


    // ============================================================
    // STYLE DATA
    // ============================================================

    for (
        let row = 1;
        row <= bodyRows.length;
        row++
    ) {
        for (
            let col = 0;
            col < 9;
            col++
        ) {
            const address =
                XLSX.utils.encode_cell(
                    {
                        r: row,
                        c: col
                    });


            if (!ws[address]) {
                continue;
            }


            // ----------------------------------------------------
            // CENTER COLUMNS
            // ----------------------------------------------------
            // A = S.No
            // D = Phase
            // G = Seq No
            // ----------------------------------------------------

            if (
                col === 0 ||
                col === 3 ||
                col === 6
            ) {
                ws[address].s =
                    centerStyle;
            }
            else {
                ws[address].s =
                    normalStyle;
            }
        }
    }


    // ============================================================
    // COLUMN WIDTHS
    // ============================================================

    ws["!cols"] =
        [
            { wch: 8 },     // S.No
            { wch: 17 },    // Cons Ref
            { wch: 20 },    // Meter Number
            { wch: 10 },    // Phase
            { wch: 17 },    // Department
            { wch: 18 },    // Division
            { wch: 12 },    // Seq No
            { wch: 55 },    // Address
            { wch: 18 }     // Meter Make
        ];


    // ============================================================
    // ROW HEIGHTS
    // ============================================================

    ws["!rows"] = [];


    // Header
    ws["!rows"][0] =
    {
        hpt: 32
    };


    // Data
    for (
        let i = 1;
        i <= bodyRows.length;
        i++
    ) {
        ws["!rows"][i] =
        {
            hpt: 30
        };
    }


    // ============================================================
    // AUTOFILTER
    // ============================================================

    ws["!autofilter"] =
    {
        ref:
            `A1:I${bodyRows.length + 1}`
    };


    // ============================================================
    // FREEZE HEADER
    // ============================================================

    ws["!freeze"] =
    {
        xSplit: 0,
        ySplit: 1
    };


    // ============================================================
    // HIDE GRIDLINES
    // ============================================================

    ws["!sheetViews"] =
        [
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
        "HES Download"
    );


    // ============================================================
    // FILE NAME
    // ============================================================

    const date =
        new Date()
            .toISOString()
            .split("T")[0];


    XLSX.writeFile(
        wb,
        "HES_Download_Summary_BYPL_" +
        date +
        ".xlsx"
    );
}


// ============================================================
// CSV EXPORT - BYPL HES
// ============================================================

function exportHesDownloadByplTableToCSV() {

    const table = document.getElementById("hesDownloadSummaryTableBypl");

    if (!table)
    {
        console.error("BYPL HES table not found" );
        return;
    }

    const csv = [];

    table
        .querySelectorAll("tr")
        .forEach(row =>
        {
            if (row.style.display === "none")
            {
                return;
            }

            const cols = row.querySelectorAll( "th, td" );

            const data = [];

            cols.forEach(col =>
            {

                const value =
                    (
                        col.innerText || ""
                    )
                        .replace(/\r?\n|\r/g, " ")
                        .trim()
                        .replace(/"/g,'""' );

                data.push(`"${value}"`);

            });


            csv.push(data.join(","));

        });


    const blob = new Blob( [csv.join("\n")],
            {
                type:
                    "text/csv;charset=utf-8;"
            }
        );


    const url =URL.createObjectURL( blob );

    const link =document.createElement("a");

    link.href = url;

    const date = new Date()
            .toISOString()
            .split("T")[0];

    link.download = "HES_Download_Summary_BYPL_" + date + ".csv";

    document.body.appendChild(link);

    link.click();

    document.body.removeChild(link);

    URL.revokeObjectURL( url);
}


// ============================================================
// ESCAPE HTML
// ============================================================

function escapeHesHtmlBypl(value,fallback = "")
{

    if (value === null || value === undefined || value === "")
    {

        return fallback;
    }


    return String(value)

        .replace( /&/g, "&amp;")

        .replace( /</g,"&lt;")

        .replace(/>/g,"&gt;")

        .replace(/"/g, "&quot;")

        .replace(/'/g, "&#039;");
}