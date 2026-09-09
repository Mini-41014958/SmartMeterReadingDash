
let allDownloadDataBypl = [];
let filteredDataBypl = [];


$("#btnViewHesFailedDetails")
    .off("click")
    .on("click", function () {

        const modalElement = document.getElementById("downloadSummaryModalBYPL");

        if (!modalElement)
        {
            console.error( "BYPL modal not found." );
            return;
        }

        const modal = bootstrap.Modal.getOrCreateInstance( modalElement );

        modal.show();

        loadDownloadSummaryBYPL();

    });


// LOAD BYPL DATA

async function loadDownloadSummaryBYPL()
{

    try
    {

        $("#downloadSummaryBodyBYPL").html(`
                <tr>
                    <td colspan="11"
                        class="text-center py-4">

                        <div class="spinner-border spinner-border-sm text-primary"></div>

                        <span class="ms-2">
                            Loading...
                        </span>

                    </td>
                </tr>
            `);


        const readingMonth = getReadingMonth();


        console.log("BYPL Reading Month:", readingMonth);


        const apiUrl = `${getApiUrl("dashboardapi/meter-download-detailed-summary-bypl")}?readingMonth=${encodeURIComponent(readingMonth)}`;


        console.log("BYPL Download Summary API:", apiUrl);


        const response = await fetch(apiUrl);


        if (!response.ok)
        {

            const errorText = await response.text();

            console.error("BYPL API Error:",response.status,errorText );

            throw new Error( "Unable to load data. HTTP " + response.status);
        }


        const result = await response.json();


        allDownloadDataBypl = Array.isArray(result) ? result: [];

        filteredDataBypl = [...allDownloadDataBypl];

        // Load filters
        loadDepartmentFilterBYPL();

        loadReasonFilterBYPL();

        loadPhaseFilterBYPL();

        loadMeterMakeFilterBYPL();


        // Clear date filters
        clearFiltersBYPL();


        // Render data
        renderTableBYPL( filteredDataBypl);

    }
    catch (err)
    {

        console.error("BYPL Download Summary Error:",err);

        $("#downloadSummaryBodyBYPL").html(`
                <tr>
                    <td colspan="11"
                        class="text-center text-danger py-4">

                        Failed to load data: ${escapeHtml(err.message)}

                    </td>
                </tr>
            `);
    }
}

// DEPARTMENT FILTER

function loadDepartmentFilterBYPL()
{

    const ddl = $("#departmentFilterBYPL");

    ddl.empty();

    ddl.append(` <option value=""> All Departments </option>`);

    const departments = [...new Set( allDownloadDataBypl.map(x => String( x.sapDepartment || "").trim()).filter(Boolean))];

    departments.sort( (a, b) => a.localeCompare(b));

    departments.forEach(department =>
    {

        ddl.append($("<option>",
            {
                    value: department,
                    text: department
                })
            );

        }
    );
}

// FAILED REASON FILTER


function loadReasonFilterBYPL() {

    const ddl = $("#reasonFilterBYPL");

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


// PHASE FILTER

function loadPhaseFilterBYPL() {

    const ddl = $("#phaseFilterBYPL");

    ddl.empty();

    ddl.append(`  <option value="">  All Phases  </option>`);


    const phases = [ ...new Set( allDownloadDataBypl.map(x => x.phase).filter(x =>
                    x !== null &&
                    x !== undefined &&
                    x !== ""
                )
        )
    ];


    phases.sort( (a, b) => String(a).localeCompare(String(b), undefined,
                {
                    numeric: true
                }
            )
    );


    phases.forEach(phase =>
    {

        ddl.append($("<option>",
            {
                value: String(phase),
                text: String(phase)
            })
        );

    });
}

// METER MAKE FILTER
function loadMeterMakeFilterBYPL()
{

    const ddl = $("#meterMakeFilterBYPL");

    ddl.empty();

    ddl.append(`<option value="">  All Meter Makes </option>`);


    const meterMakes = [ ...new Set( allDownloadDataBypl.map(x => x.meterType).filter(x =>
                    x !== null &&
                    x !== undefined &&
                    x !== ""
                )
        )
    ];


    meterMakes.sort( (a, b) => String(a).localeCompare(String(b)));

    meterMakes.forEach(make => {

        ddl.append(
            $("<option>", {
                value: String(make),
                text: String(make)
            })
        );

    });
}


// FAILURE CATEGORY

function getFailureCategoryBYPL(reason)
{

    const message = String(reason || "").toUpperCase().replace(/\s+/g, " ").trim();


    // SYSTEM TITLE
    if (message.includes("SYSTEM TITLE"))
    {
        return "SYSTEM_TITLE";
    }

    // TCP
    if (message.includes("TCP"))
    {

        return "TCP";
    }


    // NO DATA
    if (message.includes("DATA NOT FOUND") || message.includes("NO DATA") || message.includes("DATA NOT AVAILABLE"))
    {
        return "NO_DATA";
    }


    // DATE OLDER
    if (message.includes("DATE IS OLDER") || message.includes("DATE OLDER") || (message.includes("SMART METER") && message.includes("OLDER")
        && message.includes("FORMY") &&message.includes("SAP_MRO_DOWNLOAD_DATE")))
    {

        return "DATE_OLDER";
    }


    // TIMEOUT
    if (message.includes("TIMEOUT") || message.includes("TIME OUT"))
    {

        return "TIMEOUT";
    }

    return "OTHER";
}


// FILTER EVENTS

$("#departmentFilterBYPL")
    .off("change")
    .on("change", applyFiltersBYPL);


$("#reasonFilterBYPL")
    .off("change")
    .on("change", applyFiltersBYPL);


$("#phaseFilterBYPL")
    .off("change")
    .on("change", applyFiltersBYPL);


$("#meterMakeFilterBYPL")
    .off("change")
    .on("change", applyFiltersBYPL);


$("#entryDateFromBYPL")
    .off("change")
    .on("change", applyFiltersBYPL);


$("#entryDateToBYPL")
    .off("change")
    .on("change", applyFiltersBYPL);


// CLEAR FILTERS BUTTON

$("#btnClearDownloadFiltersBYPL")
    .off("click")
    .on("click", function ()
    {

        clearFiltersBYPL();

        filteredDataBypl = [...allDownloadDataBypl];


        renderTableBYPL( filteredDataBypl);

    });


// CLEAR FILTERS

function clearFiltersBYPL()
{

    $("#departmentFilterBYPL").val("");


    $("#reasonFilterBYPL").val("");


    $("#phaseFilterBYPL").val("");


    $("#meterMakeFilterBYPL").val("");


    $("#entryDateFromBYPL").val("");


    $("#entryDateToBYPL").val("");


    $("#entryDateToBYPL").removeClass
    (
            "is-invalid"
     );
}

// APPLY FILTERS

function applyFiltersBYPL()
{

    const selectedDepartment = String($("#departmentFilterBYPL").val() || "").trim().toUpperCase();

    const selectedReason =String($("#reasonFilterBYPL").val() || "").trim().toUpperCase();

    const selectedPhase = String($("#phaseFilterBYPL").val() || "").trim().toUpperCase();

    const selectedMeterMake = String( $("#meterMakeFilterBYPL").val() || "").trim().toUpperCase();

    const dateFrom = $("#entryDateFromBYPL").val();

    const dateTo = $("#entryDateToBYPL").val();

    // DATE VALIDATION


    if (dateFrom && dateTo && dateFrom > dateTo)
    {

        $("#entryDateToBYPL").addClass(
                "is-invalid"
            );

        filteredDataBypl = [];

        renderTableBYPL(filteredDataBypl );

        return;
    }

    $("#entryDateToBYPL")
        .removeClass(
            "is-invalid"
        );

    // FILTER DATA

    filteredDataBypl = allDownloadDataBypl.filter(item =>
    {

            // DEPARTMENT

            const itemDepartment = String(item.sapDepartment || "" ).trim().toUpperCase();

            const departmentMatch = selectedDepartment === "" ||  itemDepartment === selectedDepartment;

            // FAILURE REASON

            const itemCategory = getFailureCategoryBYPL(item.schedulerMessage);

            const reasonMatch = selectedReason === "" ||  itemCategory === selectedReason;

            // PHASE

            const itemPhase =String( item.phase || "" ).trim().toUpperCase();

            const phaseMatch = selectedPhase === "" || itemPhase === selectedPhase;

            // METER MAKE

            const itemMeterMake = String( item.meterType || "" ).trim().toUpperCase();

            const meterMakeMatch = selectedMeterMake === "" || itemMeterMake === selectedMeterMake;

            // ENTRY DATE

        let dateMatch = true;

        if (dateFrom || dateTo)
        {
            if (!item.entryDate)
            {
                dateMatch = false;
            }
            else
            {
                const entryDate = new Date( item.entryDate);

                if (isNaN(entryDate.getTime()))
                {
                    dateMatch = false;
                }
                else
                {
                    const entryDateString = formatDateForFilterBYPL( item.entryDate);

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

            // FINAL MATCH

            return ( departmentMatch && reasonMatch && phaseMatch && meterMakeMatch && dateMatch);

        });

    renderTableBYPL(filteredDataBypl);
}


// BADGE STYLE

function getFailureBadgeStyleBYPL(reason)
{

    const category = getFailureCategoryBYPL( reason);


    switch (category)
    {

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

// FORMAT ENTRY DATE

function formatEntryDateBYPL(value)
{

    if (!value)
    {

        return "--";
    }


    const date = new Date(value);


    if (isNaN(date.getTime()))
    {

        return String(value);
    }


    return date.toLocaleString("en-GB",
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

// RENDER BYPL TABLE

function renderTableBYPL(data)
{

    const tbody =$("#downloadSummaryBodyBYPL");

    tbody.empty();

    // NO RECORDS

    if (!Array.isArray(data) || data.length === 0)
    {

        tbody.html(`
                <tr>
                    <td colspan="11"
                        class="text-center py-4 text-muted">

                        No Records Found

                    </td>
                </tr>
            `);

        return;
    }

    // RENDER ROWS

    data.forEach((x, index) =>
    {
        const badgeStyle = getFailureBadgeStyleBYPL( x.schedulerMessage);

        const entryDate =  formatEntryDateBYPL( x.entryDate );

        tbody.append(`
                <tr>

                    <td class="text-center"> ${index + 1} </td>

                    <td> ${escapeHtml(x.consRef || "" )} </td>

                    <td> ${escapeHtml(  x.meterNumber || "" )} </td>

                    <td class="text-center"> ${escapeHtml(x.phase || "")}</td>

                    <td> ${escapeHtml( x.sapDepartment || "")} </td>

                    <td>${escapeHtml(x.sapDivision || "" )}</td>

                    <td> ${escapeHtml(x.sapSeqNo || "")} </td>

                    <td class="address-cell"> ${escapeHtml( x.address || "--" )}</td>

                    <td class="text-center"> ${escapeHtml(x.meterType || "")} </td>

                    <td class="status-cell">

                        <span class="badge" style=" background-color:${badgeStyle.background}; color:${badgeStyle.color}; white-space:normal;
                                  display:inline-block; line-height:1.35; padding:6px 10px; max-width:100%; ">
                         ${escapeHtml(x.schedulerMessage || "--")} </span>

                    </td>

                    <td class="text-center">${escapeHtml( entryDate )}</td>

                </tr>
            `);

    });
}


// ESCAPE HTML

function escapeHtml(value) {

    if (value === null || value === undefined)
    {
        return "";
    }

    return String(value)
        .replace(/&/g, "&amp;")

        .replace(/</g, "&lt;")

        .replace(/>/g, "&gt;")

        .replace(/"/g, "&quot;")

        .replace( /'/g, "&#039;");
}

// EXPORT EXCEL

function exportTableToExcelBYPL()
{

    const table = document.getElementById( "downloadSummaryTableBYPL");

    if (!table)
    {

        console.error("BYPL download table not found.");
        return;
    }

    const wb = XLSX.utils.book_new();

    const ws = XLSX.utils.table_to_sheet( table );

    ws["!cols"] = [

        { wch: 7 },
        { wch: 15 },
        { wch: 18 },
        { wch: 10 },
        { wch: 15 },
        { wch: 15 },
        { wch: 18 },
        { wch: 50 },
        { wch: 15 },
        { wch: 55 },
        { wch: 22 }

    ];

    ws["!autofilter"] = { ref: "A1:K1"};

    ws["!freeze"] = { xSplit: 0, ySplit: 1};

    XLSX.utils.book_append_sheet( wb, ws, "Download Failed");

    const fileDate = new Date()
            .toISOString()
            .split("T")[0];


    XLSX.writeFile( wb, "Download_Failed_Summary_BYPL_" + fileDate + ".xlsx");
}


// EXPORT CSV

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


    rows.forEach(row => {

        const columns =
            row.querySelectorAll(
                "th, td"
            );


        const rowData = [];


        columns.forEach(column => {

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

        });


        csv.push(
            rowData.join(",")
        );

    });


    const blob =
        new Blob(
            [csv.join("\n")],
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


// FORMAT DATE FOR FILTER


function formatDateForFilterBYPL(value) {

    if (!value) {
        return "";
    }

    const date = new Date(value);

    if (isNaN(date.getTime())) {
        return "";
    }

    const year = date.getFullYear();

    const month = String(
        date.getMonth() + 1
    ).padStart(2, "0");

    const day = String(
        date.getDate()
    ).padStart(2, "0");

    return `${year}-${month}-${day}`;
}