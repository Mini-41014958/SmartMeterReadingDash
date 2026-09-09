

let allHesDownloadData = [];
let filteredHesDownloadData = [];


let hesCurrentUserAccess = null;


// ============================================================
// GET CURRENT USER ACCESS
// ============================================================

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


// ============================================================
// CHECK BRPL ACCESS
// ============================================================

function hasHesBrplAccess(access)
{

    if (!access)
    {
        return false;
    }


    const role = String(access.role || "" )
            .trim()
            .toUpperCase();


    const company =
        String(
            access.company || ""
        )
            .trim()
            .toUpperCase();


    // SUPERADMIN
    if (
        role === "SUPERADMIN" ||
        access.isSuperAdmin === true
    ) {
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


function getHesUserDepartment(access) {

    return String(
        access?.department || ""
    )
        .trim()
        .toUpperCase();
}


// ============================================================
// SHOW HES ACCESS ERROR
// ============================================================

function showHesAccessError(message) {

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


// ============================================================
// OPEN HES DOWNLOAD MODAL
// ============================================================

$("#hesDownload")
    .off("click.hesDownload")
    .on(
        "click.hesDownload",
        async function (e) {

            e.preventDefault();


            const modalElement =
                document.getElementById(
                    "HesDownloadSummaryModal"
                );


            if (!modalElement) {

                console.error(
                    "HES Download modal not found."
                );

                return;
            }


            const modal =
                bootstrap.Modal.getOrCreateInstance(
                    modalElement
                );


            modal.show();


            await loadHesDownloadSummary();

        }
    );


// ============================================================
// LOAD HES DOWNLOAD SUMMARY
// ============================================================

async function loadHesDownloadSummary() {

    try {

        // -----------------------------------------------------
        // Loading state
        // -----------------------------------------------------

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


        // -----------------------------------------------------
        // Get user access
        // -----------------------------------------------------

        const access =
            await getHesUserAccess();


        console.log(
            "HES BRPL Access:",
            {
                role:
                    access?.role,

                company:
                    access?.company,

                department:
                    access?.department,

                isSuperAdmin:
                    access?.isSuperAdmin
            }
        );


        // -----------------------------------------------------
        // Verify BRPL access BEFORE API CALL
        // -----------------------------------------------------

        if (
            !hasHesBrplAccess(access)
        ) {

            showHesAccessError(
                "You do not have access to BRPL HES download data."
            );

            return;
        }


        // -----------------------------------------------------
        // Reading month
        // -----------------------------------------------------

        const readingMonth =
            getReadingMonth();


        if (!readingMonth) {

            showHesAccessError(
                "Reading month is not selected."
            );

            return;
        }


        // -----------------------------------------------------
        // API URL
        // -----------------------------------------------------

        const apiUrl =
            `${getApiUrl(
                "DashboardApi/hes-download-meters-details"
            )}?ReadingMonth=${encodeURIComponent(
                readingMonth
            )}`;


        console.log(
            "HES Download API:",
            apiUrl
        );


        // -----------------------------------------------------
        // API CALL
        // -----------------------------------------------------

        const response =
            await fetch(
                apiUrl,
                {
                    method: "GET",

                    credentials: "same-origin",

                    cache: "no-store",

                    headers: {
                        "Accept":
                            "application/json"
                    }
                }
            );


        // -----------------------------------------------------
        // Unauthorized
        // -----------------------------------------------------

        if (
            response.status === 401
        ) {

            throw new Error(
                "Your session has expired. Please login again."
            );
        }


        // -----------------------------------------------------
        // Forbidden
        // -----------------------------------------------------

        if (
            response.status === 403
        ) {

            throw new Error(
                "You are not authorized to access BRPL HES download data."
            );
        }


        // -----------------------------------------------------
        // Other HTTP errors
        // -----------------------------------------------------

        if (!response.ok) {

            const errorText =
                await response.text();


            console.error(
                "HES API Error:",
                response.status,
                errorText
            );


            throw new Error(
                `Unable to load HES download data (${response.status})`
            );
        }


        // -----------------------------------------------------
        // Parse response
        // -----------------------------------------------------

        const result =
            await response.json();


        allHesDownloadData =
            Array.isArray(result)
                ? result
                : [];


        // -----------------------------------------------------
        // IMPORTANT:
        // Enforce department restriction on frontend
        // -----------------------------------------------------

        const userDepartment =
            getHesUserDepartment(
                access
            );


        const departmentRestricted =
            (
                access?.isSuperAdmin !== true &&
                userDepartment !== ""
            );


        if (
            departmentRestricted
        ) {

            allHesDownloadData =
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
                            userDepartment
                        );

                    }
                );

        }


        // -----------------------------------------------------
        // Initialize filtered data
        // -----------------------------------------------------

        filteredHesDownloadData =
            [
                ...allHesDownloadData
            ];


        // -----------------------------------------------------
        // Populate filters
        // -----------------------------------------------------

        loadHesDepartmentFilter(
            access
        );


        loadHesDivisionFilter();

        loadHesPhaseFilter();

        loadHesMeterMakeFilter();


        // -----------------------------------------------------
        // Apply default department restriction
        // -----------------------------------------------------

        if (
            departmentRestricted
        ) {

            $("#hesDepartmentFilter")
                .val(
                    userDepartment
                )
                .prop(
                    "disabled",
                    true
                );

        }
        else {

            $("#hesDepartmentFilter")
                .prop(
                    "disabled",
                    false
                );

        }


        // -----------------------------------------------------
        // Clear all other filters
        // -----------------------------------------------------

        clearHesFilters(
            departmentRestricted
        );


        // -----------------------------------------------------
        // Render
        // -----------------------------------------------------

        renderHesDownloadTable(
            filteredHesDownloadData
        );


        console.log(
            "HES Download Data Loaded:",
            {
                totalRecords:
                    allHesDownloadData.length,

                role:
                    access?.role,

                company:
                    access?.company,

                department:
                    access?.department
            }
        );

    }
    catch (err) {

        console.error(
            "HES Download Summary Error:",
            err
        );


        $("#hesDownloadSummaryBody").html(`
            <tr>
                <td colspan="9"
                    class="text-center text-danger py-4">

                    <div class="fw-semibold mb-1">
                        Failed to load data
                    </div>

                    <div>
                        ${escapeHesHtml(
            err.message
        )}
                    </div>

                </td>
            </tr>
        `);

    }
}


// ============================================================
// DEPARTMENT FILTER
// ============================================================

function loadHesDepartmentFilter(
    access = hesCurrentUserAccess
) {

    const ddl =
        $("#hesDepartmentFilter");


    ddl.empty();


    const role =
        String(
            access?.role || ""
        )
            .trim()
            .toUpperCase();


    const department =
        String(
            access?.department || ""
        )
            .trim()
            .toUpperCase();


    const isSuperAdmin =
        role === "SUPERADMIN" ||
        access?.isSuperAdmin === true;


    // ---------------------------------------------------------
    // Department restricted user
    // ---------------------------------------------------------

    if (
        !isSuperAdmin &&
        department !== ""
    ) {

        ddl.append(
            $("<option>", {
                value:
                    department,

                text:
                    department
            })
        );


        ddl.val(
            department
        );


        ddl.prop(
            "disabled",
            true
        );


        return;
    }


    // ---------------------------------------------------------
    // SUPERADMIN / unrestricted BRPL admin
    // ---------------------------------------------------------

    ddl.prop(
        "disabled",
        false
    );


    ddl.append(`
        <option value="">
            All Departments
        </option>
    `);


    const departments = [

        ...new Set(

            allHesDownloadData

                .map(
                    x =>
                        String(
                            x.sapDepartment ||
                            ""
                        )
                            .trim()
                )

                .filter(Boolean)

        )

    ];


    departments.sort(
        (a, b) =>
            a.localeCompare(b)
    );


    departments.forEach(
        departmentName => {

            ddl.append(
                $("<option>", {

                    value:
                        departmentName,

                    text:
                        departmentName

                })
            );

        }
    );

}


// ============================================================
// DIVISION FILTER
// ============================================================

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


// ============================================================
// PHASE FILTER
// ============================================================

function loadHesPhaseFilter() {

    const ddl =
        $("#hesPhaseFilter");


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


    phases.sort(
        (a, b) =>
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


// ============================================================
// METER MAKE FILTER
// ============================================================

function loadHesMeterMakeFilter() {

    const ddl =
        $("#hesMeterMakeFilter");


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

function exportHesDownloadBrplTableToExcel() {

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


    const wb =
        XLSX.utils.book_new();


    const ws =
        XLSX.utils.table_to_sheet(
            table
        );


    ws["!cols"] = [

        { wch: 7 },

        { wch: 15 },

        { wch: 18 },

        { wch: 10 },

        { wch: 15 },

        { wch: 15 },

        { wch: 10 },

        { wch: 45 },

        { wch: 18 }

    ];


    ws["!autofilter"] = {
        ref: "A1:I1"
    };


    ws["!freeze"] = {
        xSplit: 0,
        ySplit: 1
    };


    XLSX.utils.book_append_sheet(
        wb,
        ws,
        "HES Download"
    );


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