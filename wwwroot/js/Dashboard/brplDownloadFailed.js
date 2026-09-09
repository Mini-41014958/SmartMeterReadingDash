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
                    <td colspan="11" class="text-center py-4">
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
                    <td colspan="11"
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


// ============================================================
// METER MAKE FILTER
// ============================================================

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


// ============================================================
// FILTER EVENTS
// ============================================================

$("#departmentFilter").on("change", applyFilters);

$("#reasonFilter").on("change",applyFilters);

$("#phaseFilter").on("change",applyFilters);

$("#meterMakeFilter").on("change",applyFilters);

$("#entryDateFrom").on("change",applyFilters);

$("#entryDateTo").on("change", applyFilters);


// ============================================================
// CLEAR FILTER BUTTON
// ============================================================

$("#btnClearDownloadFilters").on("click",function ()
    {
        clearFilters();
        filteredData = [...allDownloadData];
        renderTable( filteredData );
    }
);


// ============================================================
// CLEAR FILTERS
// ============================================================

function clearFilters() {

    $("#departmentFilter").val("");

    $("#reasonFilter").val("");

    $("#phaseFilter").val("");

    $("#meterMakeFilter").val("");

    $("#entryDateFrom").val("");

    $("#entryDateTo").val("");

    $("#entryDateTo").removeClass(
        "is-invalid"
    );
}


// ============================================================
// APPLY FILTERS
// ============================================================

function applyFilters()
{

    const department = ($("#departmentFilter").val() || "").toUpperCase();

    const reason = ($("#reasonFilter").val() || "").toUpperCase();

    const phase = ($("#phaseFilter").val() || "").toUpperCase();

    const meterMake = ($("#meterMakeFilter").val() || "").toUpperCase();

    const dateFrom = $("#entryDateFrom").val();

    const dateTo = $("#entryDateTo").val();


    // --------------------------------------------------------
    // DATE VALIDATION
    // --------------------------------------------------------

    if (dateFrom && dateTo && dateFrom > dateTo)
    {

        $("#entryDateTo").addClass( "is-invalid");

        filteredData = [];

        renderTable(filteredData);

        return;
    }

    $("#entryDateTo").removeClass("is-invalid");


    // --------------------------------------------------------
    // FILTER DATA
    // --------------------------------------------------------

    filteredData = allDownloadData.filter(item =>
    {

            // DEPARTMENT
            const itemDepartment =(item.sapDepartment || "").toUpperCase();

            const departmentMatch = department === "" || itemDepartment === department;

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

            return (departmentMatch && reasonMatch && phaseMatch && meterMakeMatch && dateMatch);

        });

    renderTable( filteredData);
}


// RENDER TABLE

function renderTable(data)
{

    const tbody =  $("#downloadSummaryBody");

    tbody.empty();

    if ( !data ||data.length === 0)
    {

        tbody.html(`
                <tr>
                    <td colspan="11"
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

        let textColor ="#fff";

        const message =  (x.schedulerMessage || "") .toUpperCase();

        // STATUS COLORS

        if (message.includes("SYSTEM TITLE"))
        {

            badgeColor ="#dc3545";

        }
        else if (message.includes("TCP"))
        {

            badgeColor ="#fd7e14";

        }
        else if (message.includes("NO DATA"))
        {

            badgeColor = "#ffc107";

            textColor ="#000";

        }
        else if (message.includes("TIMEOUT"))
        {
            badgeColor = "#6c757d";
        }

        // ENTRY DATE

        let entryDate ="--";

        if (x.entryDate)
        {

            const parsedDate =new Date( x.entryDate);

            if (!isNaN(parsedDate.getTime()))
            {

                entryDate = parsedDate.toLocaleString("en-GB");
            }
        }


        // TABLE ROW

        tbody.append(`
                <tr>

                    <td class="text-center fw-semibold"> ${index + 1}</td>

                    <td>${escapeHtml( x.consRef ?? "")}</td>

                    <td> ${escapeHtml(x.meterNumber ?? "")} </td>

                    <td class="text-center">${escapeHtml( x.phase ?? "" )}</td>

                    <td>${escapeHtml( x.sapDepartment ?? "")}</td>

                    <td> ${escapeHtml( x.sapDivision ?? "" )}</td>

                    <td> ${escapeHtml(x.sapSeqNo ?? "")} </td>

                    <td>${escapeHtml(x.address ?? "--")} </td>

                    <td> ${escapeHtml( x.meterType ?? "" )} </td>

                    <td>
                        <span class="badge" style=" background:${badgeColor};color:${textColor}; ">

                            ${escapeHtml(x.schedulerMessage ?? "--")}

                        </span>
                    </td>

                    <td>  ${escapeHtml(entryDate )} </td>

                </tr>
            `);

    });
}


// EXPORT TO EXCEL
 
function exportTableToExcel()
{

    const table = document.getElementById( "downloadSummaryTable");

    const wb = XLSX.utils.book_new();

    const ws = XLSX.utils.table_to_sheet( table);


    ws["!cols"] = [

        { wch: 7 },     // S.No
        { wch: 15 },    // Cons Ref
        { wch: 18 },    // Meter Number
        { wch: 10 },    // Phase
        { wch: 15 },    // Department
        { wch: 15 },    // Division
        { wch: 10 },    // Seq No
        { wch: 45 },    // Address
        { wch: 15 },    // Meter Make
        { wch: 40 },    // Status
        { wch: 20 }     // Entry Date

    ];

    ws["!autofilter"] = { ref: "A1:K1" };

    ws["!freeze"] = {xSplit: 0, ySplit: 1 };

    XLSX.utils.book_append_sheet( wb, ws, "Download Failed");

    const today = new Date()
            .toISOString()
            .split("T")[0];

    XLSX.writeFile( wb,"Download_Failed_Summary_BRPL_" + today + ".xlsx");
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