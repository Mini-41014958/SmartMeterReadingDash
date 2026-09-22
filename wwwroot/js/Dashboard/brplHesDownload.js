

let allHesDownloadData = [];
let filteredHesDownloadData = [];


let hesCurrentUserAccess = null;

// GET CURRENT USER ACCESS

async function getHesUserAccess()
{

    if (hesCurrentUserAccess && hesCurrentUserAccess.authenticated)
    {
        return hesCurrentUserAccess;
    }

    const response = await fetch( getApiUrl("AuthApi/my-access"),
        {
            method: "GET",

            credentials: "same-origin",

            cache: "no-store",

            headers: {
                "Accept": "application/json"
            }
        }
    );


    if (!response.ok)
    {

        throw new Error( `Unable to determine user access. Status: ${response.status}`);
    }

    hesCurrentUserAccess = await response.json();

    return hesCurrentUserAccess;
}


// CHECK BRPL ACCESS

function hasHesBrplAccess(access)
{

    if (!access)
    {
        return false;
    }


    const role = String(access.role || "" )
            .trim()
            .toUpperCase();


    const company = String( access.company || "" )
            .trim()
            .toUpperCase();


    // SUPERADMIN
    if (role === "SUPERADMIN" || access.isSuperAdmin === true)
    {
        return true;
    }


    // BRPL ADMIN / USER
    return company === "BRPL";
}


function hasHesDepartmentRestriction(access)
{

    if (!access)
    {
        return false;
    }

    // SUPERADMIN can access all departments
    if (access.isSuperAdmin === true)
    {
        return false;
    }

    return String( access.department || "")
        .trim()
        .length > 0;
}


function getHesUserDepartment(access)
{

    return String( access?.department || "")
        .trim()
        .toUpperCase();
}


// SHOW HES ACCESS ERROR

function showHesAccessError(message)
{

    $("#hesDownloadSummaryBody").html(`
        <tr>
            <td colspan="9"
                class="text-center text-danger py-4">

                <div class="fw-semibold mb-1">
                    Access Denied
                </div>

                <div>
                    ${escapeHesHtml(message)}
                </div>

            </td>
        </tr>
    `);
}

// OPEN HES DOWNLOAD MODAL

$("#hesDownload")
    .off("click.hesDownload")
    .on(
        "click.hesDownload",
        async function (e)
        {
            e.preventDefault();
            const modalElement =  document.getElementById( "HesDownloadSummaryModal");

            if (!modalElement)
            {
                 console.error(  "HES Download modal not found." );
                return;
            }
            const modal =  bootstrap.Modal.getOrCreateInstance( modalElement );
            modal.show();
            await loadHesDownloadSummary();

        }
    );

// LOAD HES DOWNLOAD SUMMARY

async function loadHesDownloadSummary()
{

    try
    {
        // Loading state

        $("#hesDownloadSummaryBody").html(`
            <tr>
                <td colspan="9"
                    class="text-center py-4">

                    <div class="spinner-border spinner-border-sm text-primary">
                    </div>

                    <span class="ms-2">
                        Loading...
                    </span>

                </td>
            </tr>
        `);

        // Get user access

        const access = await getHesUserAccess();
-
        // Verify BRPL access BEFORE API CALL

        if (!hasHesBrplAccess(access))
        {

            showHesAccessError( "You do not have access to BRPL HES download data.");

            return;
        }

        // Reading month


        const readingMonth = getReadingMonth();


        if (!readingMonth)
        {
            showHesAccessError( "Reading month is not selected."  );
            return;
        }

        // API URL

        const apiUrl =  `${getApiUrl( "DashboardApi/hes-download-meters-details" )}?ReadingMonth=${encodeURIComponent( readingMonth)}`;

        // API CALL


        const response =  await fetch(
                apiUrl,
                {
                    method: "GET",

                    credentials: "same-origin",

                    cache: "no-store",

                    headers: { "Accept":  "application/json"}
                }
            );

        // Unauthorized


        if (response.status === 401)
        {

            throw new Error(  "Your session has expired. Please login again." );
        }

        // Forbidden

        if (response.status === 403)
        {

            throw new Error( "You are not authorized to access BRPL HES download data." );
        }


        // Other HTTP errors

        if (!response.ok)
        {

            const errorText =  await response.text();

            throw new Error(  `Unable to load HES download data (${response.status})` );
        }

        // Parse response

        const result =  await response.json();


        allHesDownloadData =  Array.isArray(result) ? result  : [];

        const userDepartment =  getHesUserDepartment( access );

        const departmentRestricted =  (  access?.isSuperAdmin !== true &&  userDepartment !== "" );

        if (departmentRestricted)
        {

            allHesDownloadData = allHesDownloadData.filter(item =>
            {

                        const itemDepartment =
                            String(
                                item.sapDepartment ||
                                ""
                            )
                                .trim()
                                .toUpperCase();


                        return (
                            itemDepartment ===
                            userDepartment
                        );

                    }
                );

        }

        filteredHesDownloadData = [...allHesDownloadData  ];

        loadHesDepartmentFilter(access );


        loadHesDivisionFilter();

        loadHesPhaseFilter();

        loadHesMeterMakeFilter();

        // Apply default department restriction

        if (departmentRestricted)
        {
            $("#hesDepartmentFilter")
                .val(  userDepartment )
                .prop(  "disabled",  true);
        }
        else
        {

            $("#hesDepartmentFilter")
                .prop(
                    "disabled",
                    false
                );

        }

        // Clear all other filters

        clearHesFilters(departmentRestricted );
        // Render
        renderHesDownloadTable( filteredHesDownloadData );
    }
    catch (err)
    {

        console.error( "HES Download Summary Error:", err );

        $("#hesDownloadSummaryBody").html(`
            <tr>
                <td colspan="9"
                    class="text-center text-danger py-4">

                    <div class="fw-semibold mb-1">
                        Failed to load data
                    </div>

                    <div>
                        ${escapeHesHtml( err.message)}
                    </div>

                </td>
            </tr>
        `);

    }
}

function loadHesDepartmentFilter(access = hesCurrentUserAccess)
{

    const ddl = $("#hesDepartmentFilter");

    ddl.empty();

    const role =  String(  access?.role || "" )
            .trim()
            .toUpperCase();


    const department = String(  access?.department || "" )
            .trim()
            .toUpperCase();


    const isSuperAdmin = role === "SUPERADMIN" || access?.isSuperAdmin === true;


    // Department restricted user

    if (!isSuperAdmin && department !== "")
    {

        ddl.append($("<option>",
            {
                value:  department,
                text: department
            })
        );


        ddl.val( department );

        ddl.prop( "disabled",  true );


        return;
    }

    // SUPERADMIN / unrestricted BRPL admin

    ddl.prop( "disabled",  false);


    ddl.append(`
        <option value="">
            All Departments
        </option>
    `);


    const departments = [ ...new Set(

            allHesDownloadData .map( x =>String(  x.sapDepartment || "" ).trim()
                ) .filter(Boolean)

        )
    ];

    departments.sort( (a, b) =>  a.localeCompare(b) );

    departments.forEach(departmentName =>
    {

        ddl.append($("<option>",
            {
                    value:   departmentName,

                    text: departmentName

                })
            );

        }
    );

}

// DIVISION FILTER

function loadHesDivisionFilter() {

    const ddl =
        $("#hesDivisionFilter");


    ddl.empty();


    ddl.append(`
        <option value="">
            All Divisions
        </option>
    `);


    const divisions = [

        ...new Set(

            allHesDownloadData

                .map(
                    x =>
                        String(
                            x.sapDivision ||
                            ""
                        )
                            .trim()
                )

                .filter(Boolean)

        )

    ];


    divisions.sort(
        (a, b) =>
            a.localeCompare(b)
    );


    divisions.forEach(
        division => {

            ddl.append(
                $("<option>", {

                    value:
                        division,

                    text:
                        division

                })
            );

        }
    );

}

// PHASE FILTER

function loadHesPhaseFilter()
{

    const ddl = $("#hesPhaseFilter");

    ddl.empty();

    ddl.append(`
        <option value="">
            All Phases
        </option>
    `);

    const phases = [

        ...new Set(

            allHesDownloadData

                .map(
                    x =>
                        String(
                            x.phase ||
                            ""
                        )
                            .trim()
                )

                .filter(Boolean)

        )

    ];


    phases.sort( (a, b) =>
            a.localeCompare(
                b,
                undefined,
                {
                    numeric:
                        true
                }
            )
    );


    phases.forEach(
        phase => {

            ddl.append(
                $("<option>", {

                    value:
                        phase,

                    text:
                        phase

                })
            );

        }
    );

}

// METER MAKE FILTER

function loadHesMeterMakeFilter()
{

    const ddl = $("#hesMeterMakeFilter");

    ddl.empty();

    ddl.append(`
        <option value="">
            All Meter Makes
        </option>
    `);


    const meterMakes = [

        ...new Set(

            allHesDownloadData

                .map(
                    x =>
                        String(
                            x.meterType ||
                            ""
                        )
                            .trim()
                )

                .filter(Boolean)

        )

    ];


    meterMakes.sort(
        (a, b) =>
            a.localeCompare(b)
    );


    meterMakes.forEach(
        make => {

            ddl.append(
                $("<option>", {

                    value:
                        make,

                    text:
                        make

                })
            );

        }
    );

}


// ============================================================
// FILTER EVENTS
// ============================================================

$("#hesDepartmentFilter")
    .off(
        "change.hesDepartment"
    )
    .on(
        "change.hesDepartment",
        applyHesFilters
    );


$("#hesDivisionFilter")
    .off(
        "change.hesDivision"
    )
    .on(
        "change.hesDivision",
        applyHesFilters
    );


$("#hesPhaseFilter")
    .off(
        "change.hesPhase"
    )
    .on(
        "change.hesPhase",
        applyHesFilters
    );


$("#hesMeterMakeFilter")
    .off(
        "change.hesMeterMake"
    )
    .on(
        "change.hesMeterMake",
        applyHesFilters
    );


// ============================================================
// CLEAR FILTER BUTTON
// ============================================================

$("#btnClearHesDownloadFilters")
    .off(
        "click.hesClear"
    )
    .on(
        "click.hesClear",
        function () {

            const access =
                hesCurrentUserAccess;


            const department =
                getHesUserDepartment(
                    access
                );


            const departmentRestricted =
                (
                    access?.isSuperAdmin !== true &&
                    department !== ""
                );


            clearHesFilters(
                departmentRestricted
            );


            if (
                departmentRestricted
            ) {

                filteredHesDownloadData =
                    allHesDownloadData.filter(
                        item => {

                            const itemDepartment =
                                String(
                                    item.sapDepartment ||
                                    ""
                                )
                                    .trim()
                                    .toUpperCase();


                            return (
                                itemDepartment ===
                                department
                            );

                        }
                    );

            }
            else {

                filteredHesDownloadData =
                    [
                        ...allHesDownloadData
                    ];

            }


            renderHesDownloadTable(
                filteredHesDownloadData
            );

        }
    );


// ============================================================
// CLEAR FILTERS
// ============================================================

function clearHesFilters(
    departmentRestricted = false
) {

    if (
        departmentRestricted
    ) {

        const department =
            getHesUserDepartment(
                hesCurrentUserAccess
            );


        $("#hesDepartmentFilter")
            .val(
                department
            );

    }
    else {

        $("#hesDepartmentFilter")
            .val("");

    }


    $("#hesDivisionFilter")
        .val("");


    $("#hesPhaseFilter")
        .val("");


    $("#hesMeterMakeFilter")
        .val("");

}


// ============================================================
// APPLY FILTERS
// ============================================================

function applyHesFilters() {

    const department =
        String(
            $("#hesDepartmentFilter")
                .val() || ""
        )
            .trim()
            .toUpperCase();


    const division =
        String(
            $("#hesDivisionFilter")
                .val() || ""
        )
            .trim()
            .toUpperCase();


    const phase =
        String(
            $("#hesPhaseFilter")
                .val() || ""
        )
            .trim()
            .toUpperCase();


    const meterMake =
        String(
            $("#hesMeterMakeFilter")
                .val() || ""
        )
            .trim()
            .toUpperCase();


    // ---------------------------------------------------------
    // HARD DEPARTMENT RESTRICTION
    // ---------------------------------------------------------

    const userDepartment =
        getHesUserDepartment(
            hesCurrentUserAccess
        );


    const departmentRestricted =
        (
            hesCurrentUserAccess?.isSuperAdmin !== true &&
            userDepartment !== ""
        );


    const effectiveDepartment =
        departmentRestricted
            ? userDepartment
            : department;


    // ---------------------------------------------------------
    // FILTER
    // ---------------------------------------------------------

    filteredHesDownloadData =
        allHesDownloadData.filter(
            item => {

                const itemDepartment =
                    String(
                        item.sapDepartment ||
                        ""
                    )
                        .trim()
                        .toUpperCase();


                const departmentMatch =
                    effectiveDepartment === "" ||
                    itemDepartment ===
                    effectiveDepartment;


                const itemDivision =
                    String(
                        item.sapDivision ||
                        ""
                    )
                        .trim()
                        .toUpperCase();


                const divisionMatch =
                    division === "" ||
                    itemDivision ===
                    division;


                const itemPhase =
                    String(
                        item.phase ||
                        ""
                    )
                        .trim()
                        .toUpperCase();


                const phaseMatch =
                    phase === "" ||
                    itemPhase ===
                    phase;


                const itemMeterMake =
                    String(
                        item.meterType ||
                        ""
                    )
                        .trim()
                        .toUpperCase();


                const meterMakeMatch =
                    meterMake === "" ||
                    itemMeterMake ===
                    meterMake;


                return (
                    departmentMatch &&
                    divisionMatch &&
                    phaseMatch &&
                    meterMakeMatch
                );

            }
        );


    renderHesDownloadTable(
        filteredHesDownloadData
    );

}


// ============================================================
// RENDER TABLE
// ============================================================

function renderHesDownloadTable(
    data
) {

    const tbody =
        $("#hesDownloadSummaryBody");


    tbody.empty();


    if (
        !data ||
        data.length === 0
    ) {

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


    data.forEach(
        (x, index) => {

            tbody.append(`
                <tr>

                    <td class="text-center fw-semibold">
                        ${index + 1}
                    </td>

                    <td>
                        ${escapeHesHtml(
                x.consRef
            )}
                    </td>

                    <td>
                        ${escapeHesHtml(
                x.meterNumber
            )}
                    </td>

                    <td class="text-center">
                        ${escapeHesHtml(
                x.phase
            )}
                    </td>

                    <td>
                        ${escapeHesHtml(
                x.sapDepartment
            )}
                    </td>

                    <td>
                        ${escapeHesHtml(
                x.sapDivision
            )}
                    </td>

                    <td>
                        ${escapeHesHtml(
                x.sapSeqNo
            )}
                    </td>

                    <td class="address-cell">
                        ${escapeHesHtml(
                x.address,
                "--"
            )}
                    </td>

                    <td>
                        ${escapeHesHtml(
                x.meterType
            )}
                    </td>

                </tr>
            `);

        }
    );

}


// ============================================================
// EXCEL EXPORT
// ============================================================

// ============================================================
// EXCEL EXPORT - BRPL HES DOWNLOAD
// ============================================================

function exportHesDownloadBrplTableToExcel() {

    const table =
        document.getElementById(
            "hesDownloadSummaryTable"
        );


    if (!table) {

        console.error(
            "HES Download table not found."
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
            hasData = true;
        }

    });


    if (!hasData) {

        alert(
            "No HES Download data available to export."
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


    // ============================================================
    // HEADER STYLE
    // ============================================================

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


    // ============================================================
    // NORMAL DATA STYLE
    // ============================================================

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


    // ============================================================
    // CENTER DATA STYLE
    // ============================================================

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


    // ============================================================
    // STYLE HEADER
    // ============================================================

    for (
        let col = 0;
        col < 9;
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


    // ============================================================
    // STYLE ALL DATA CELLS
    // ============================================================

    for (
        let row = 1;
        row < bodyRows.length + 1;
        row++
    ) {

        for (
            let col = 0;
            col < 9;
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


            // ----------------------------------------------------
            // CENTER:
            // S.No.
            // Phase
            // Seq No
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

    ws["!cols"] = [

        {
            wch: 8
        },      // S.No.

        {
            wch: 17
        },     // Cons Ref

        {
            wch: 20
        },     // Meter Number

        {
            wch: 10
        },     // Phase

        {
            wch: 17
        },     // Department

        {
            wch: 18
        },     // Division

        {
            wch: 12
        },     // Seq No

        {
            wch: 55
        },     // Address

        {
            wch: 18
        }      // Meter Make

    ];


    // ============================================================
    // ROW HEIGHTS
    // ============================================================

    ws["!rows"] = [];


    // Header
    ws["!rows"][0] = {
        hpt: 32
    };


    // Data rows
    for (
        let i = 1;
        i < bodyRows.length + 1;
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

        ref:
            `A1:I${bodyRows.length + 1}`

    };


    // ============================================================
    // FREEZE HEADER
    // ============================================================

    ws["!freeze"] = {

        xSplit: 0,
        ySplit: 1

    };


    // ============================================================
    // HIDE GRIDLINES
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
        `HES_Download_Summary_BRPL_${date}.xlsx`
    );

}


// ============================================================
// CSV EXPORT
// ============================================================

function exportHesDownloadBrplTableToCSV() {

    const table =
        document.getElementById(
            "hesDownloadSummaryTable"
        );


    if (!table) {

        console.error(
            "HES table not found."
        );

        return;
    }


    const csv = [];


    table.querySelectorAll(
        "tr"
    ).forEach(
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


            const data = [];


            cols.forEach(
                col => {

                    const value =
                        (
                            col.innerText ||
                            ""
                        )
                            .replace(
                                /\r?\n|\r/g,
                                " "
                            )
                            .trim()
                            .replace(
                                /"/g,
                                '""'
                            );


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


    const url =
        URL.createObjectURL(
            blob
        );


    const link =
        document.createElement(
            "a"
        );


    link.href =
        url;


    const date =
        new Date()
            .toISOString()
            .split("T")[0];


    link.download =
        `HES_Download_Summary_BRPL_${date}.csv`;


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


// ============================================================
// HTML ESCAPE
// ============================================================

function escapeHesHtml(
    value,
    fallback = ""
) {

    if (
        value === null ||
        value === undefined ||
        value === ""
    ) {

        return fallback;
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