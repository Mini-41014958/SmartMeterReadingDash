let currentReadingMonth = "";
let currentUserAccess = null;

function getApiUrl(endpoint)
{

    const basePath = window.location.pathname
            .toLowerCase()
            .startsWith("/smartmeter/")
            ? "/SmartMeter"
            : "";

    return `${basePath}/api/${endpoint}`;
}


// NORMALIZE ACCESS VALUE

function normalizeAccessValue(value)
{

    return String(value ?? "")
        .trim()
        .toUpperCase();

}

// CHECK SUPER ADMIN

function isSuperAdmin()
{

    return normalizeAccessValue( currentUserAccess?.role ) === "SUPERADMIN";

}

// GET USER COMPANY

function getUserCompany()
{

    return normalizeAccessValue( currentUserAccess?.company);

}

// GET USER DEPARTMENT

function getUserDepartment()
{

    return normalizeAccessValue( currentUserAccess?.department );

}

// CHECK DEPARTMENT RESTRICTION

function hasDepartmentRestriction()
{

    if (!currentUserAccess)
    {
        return false;
    }

    if (isSuperAdmin())
    {
        return false;
    }

    return getUserDepartment() !== "";

}

function canAccessCompany(company)
{

    if (!currentUserAccess)
    {
        return false;
    }

    const requestedCompany =  normalizeAccessValue(company);

    if (!requestedCompany)
    {
        return false;
    }

    if (isSuperAdmin())
    {
        return true;
    }

    return getUserCompany() === requestedCompany;

}


function canAccessBrpl()
{
    return canAccessCompany("BRPL");
}

// BYPL ACCESS

function canAccessBypl()
{
    return canAccessCompany("BYPL");
}

// LOAD CURRENT USER ACCESS

async function loadCurrentUserAccess()
{

    const url = getApiUrl("AuthApi/my-access");
    const response = await fetch( url,
            {
                method: "GET",
                credentials: "same-origin",
                headers: { "Accept": "application/json"},
                cache: "no-store"
            }
        );

    if (!response.ok)
    {
        throw new Error( `Unable to load user access. HTTP ${response.status}` );
    }

    const access = await response.json();

    if (!access)
    {
        throw new Error( "User access information is empty.");
    }


    currentUserAccess = {

        userId: access.userId ??access.UserId,

        username: access.username ??  access.Username ??  "",

        role:normalizeAccessValue( access.role ?? access.Role ),

        company:  normalizeAccessValue(access.company ?? access.Company),

        department: normalizeAccessValue( access.department ?? access.Department)

    };

}

function applyDashboardAccess()
{

    const brplAllowed =  canAccessBrpl();

    const byplAllowed = canAccessBypl();


    // BRPL

    $(".brpl-section").toggle( brplAllowed );

    $("#brplDashboardSection").toggle( brplAllowed);

    $("#brplSection").toggle( brplAllowed);

    // BYPL

    $(".bypl-section").toggle( byplAllowed );

    $("#byplDashboardSection").toggle( byplAllowed);

    $("#byplSection").toggle(byplAllowed );

    // COMPANY SPECIFIC ELEMENTS

    $("[data-company='BRPL']").toggle( brplAllowed);

    $("[data-company='BYPL']").toggle( byplAllowed );

}

function getReadingMonth()
{
    return currentReadingMonth;
}

// GET PREVIOUS READING MONTH

function getPreviousReadingMonth()
{

    const today = new Date();

    const previousDate =  new Date(  today.getFullYear(),today.getMonth() - 1,  1 );

    const year = previousDate
            .getFullYear()
            .toString();

    const month = String(previousDate.getMonth() + 1).padStart(2, "0");

    return year + month;

}

// FORMAT READING MONTH


function formatReadingMonth(month)
{

    if (!month || month.length !== 6)
    {
        return "";
    }

    const year = month.substring(0, 4);

    const monthNumber = month.substring(4, 6);

    const date =new Date( Number(year), Number(monthNumber) - 1, 1);

    if (isNaN(date.getTime()))
    {
        return month;
    }

    return date.toLocaleString("en-US",
        {
            month: "long",
            year: "numeric"
        }
    );

}

// UPDATE READING MONTH DISPLAY

function updateReadingMonthDisplay()
{

    const formatted =  formatReadingMonth(
            currentReadingMonth
        );

    $("#readingMonthDisplay")
        .text(formatted);

    $("#selectedReadingMonth")
        .text(formatted);

}


// ============================================================
// SET READING MONTH FROM INPUT
// ============================================================

function setReadingMonthFromInput() {

    const input =
        document.getElementById(
            "readingMonth"
        );


    if (
        !input ||
        !input.value
    ) {
        return false;
    }


    const value =
        input.value.trim();


    if (
        !/^\d{4}-\d{2}$/.test(value)
    ) {
        return false;
    }


    currentReadingMonth =
        value.replace(
            "-",
            ""
        );


    updateReadingMonthDisplay();


    return true;

}


// ============================================================
// NORMALIZE FAILURE DATA
// ============================================================

function normalizeFailureData(data) {

    if (!Array.isArray(data)) {
        return [];
    }


    return data.map(function (item) {

        const reason =
            item.failureReason ??
            item.FAILURE_REASON ??
            item.failure_reason ??
            item.reason ??
            item.REASON ??
            item.name ??
            item.NAME ??
            "Unknown";


        const count =
            item.count ??
            item.COUNT ??
            item.failureCount ??
            item.FAILURE_COUNT ??
            item.failure_count ??
            item.total ??
            item.TOTAL ??
            0;


        return {

            failureReason:
                String(reason).trim(),

            count:
                Number(count) || 0

        };

    });

}


// ============================================================
// SAFE JSON FETCH
// ============================================================

async function fetchDashboardApi(
    endpoint,
    readingMonth
) {

    if (!readingMonth) {

        throw new Error(
            "Reading month is required."
        );

    }


    const url =
        `${getApiUrl(endpoint)}` +
        `?ReadingMonth=${encodeURIComponent(readingMonth)}`;



    const response =
        await fetch(
            url,
            {
                method: "GET",
                credentials: "same-origin",
                headers: {
                    "Accept": "application/json"
                },
                cache: "no-store"
            }
        );


    if (response.status === 401) {

        throw new Error(
            "Session expired. Please login again."
        );

    }


    if (response.status === 403) {

        throw new Error(
            "Access denied for this company."
        );

    }


    if (!response.ok) {

        throw new Error(
            `Dashboard API failed. HTTP ${response.status}`
        );

    }


    let data;

    try {

        data =
            await response.json();

    }
    catch (error) {

        console.error(
            "Dashboard API returned invalid JSON:",
            error
        );

        throw new Error(
            "Dashboard API returned invalid JSON."
        );

    }


    if (
        data &&
        data.success === false
    ) {

        throw new Error(
            data.message ||
            "Dashboard API returned success=false."
        );

    }


    return data;

}


// ============================================================
// DASHBOARD LOAD STATE
// ============================================================

let dashboardLoading = false;

let dashboardReloadPending = false;

let dashboardRetryCount = 0;


// ============================================================
// DELAY
// ============================================================

function delay(ms) {

    return new Promise(
        function (resolve) {

            setTimeout(
                resolve,
                ms
            );

        }
    );

}


// ============================================================
// LOAD DASHBOARD
// ============================================================

async function loadDashboard() {

    // --------------------------------------------------------
    // IMPORTANT:
    // Never silently discard another load request.
    // Queue it instead.
    // --------------------------------------------------------

    if (dashboardLoading) {

        dashboardReloadPending = true;

        return;
    }


    dashboardLoading = true;

    dashboardReloadPending = false;


    // --------------------------------------------------------
    // SHOW LOADING STATE
    // --------------------------------------------------------

    $("#dashboardSkeleton")
        .stop(true, true)
        .show();

    $("#dashboardContent")
        .stop(true, true)
        .hide();


    try {

        // ----------------------------------------------------
        // LOAD USER ACCESS
        // ----------------------------------------------------

        await loadCurrentUserAccess();

        applyDashboardAccess();


        // ----------------------------------------------------
        // ENSURE READING MONTH
        // ----------------------------------------------------

        if (!currentReadingMonth) {

            currentReadingMonth =
                getPreviousReadingMonth();

        }


        updateReadingMonthDisplay();


        // ----------------------------------------------------
        // CAPTURE MONTH
        //
        // Every API call in this cycle works with the
        // same reading month.
        // ----------------------------------------------------

        const readingMonth = currentReadingMonth;

        // ----------------------------------------------------
        // LOAD DASHBOARD COMPONENTS
        // ----------------------------------------------------

        const results =
            await Promise.allSettled(
                [

                    loadMeterSummary(),

                    loadMeterDownloadSummary(),

                    loadDepartmentDistribution(),

                    loadFailureReasonChart()

                ]
            );


        // ----------------------------------------------------
        // CHECK API RESULTS
        // ----------------------------------------------------

        const names = [

            "Meter Summary",

            "Meter MRO Summary",

            "Department Distribution",

            "Failure Reason Chart"

        ];


        let hasError = false;


        results.forEach(
            function (result, index) {

                if (
                    result.status === "rejected"
                ) {

                    hasError = true;


                    console.error(
                        `${names[index]} failed:`,
                        result.reason
                    );

                }
                else {

                    console.log(
                        `${names[index]} loaded successfully.`
                    );

                }

            }
        );


        // ----------------------------------------------------
        // RETRY ONCE
        // ----------------------------------------------------

        if (
            hasError &&
            dashboardRetryCount < 1
        ) {

            dashboardRetryCount++;


            console.warn(
                "One or more dashboard APIs failed. " +
                "Retrying once..."
            );


            await delay(500);


            dashboardReloadPending = true;

        }
        else {

            dashboardRetryCount = 0;

        }


    }
    catch (err) {

        console.error(
            "Dashboard Loading Error:",
            err
        );


        if (
            err.message &&
            (
                err.message.includes(
                    "Session expired"
                ) ||

                err.message.includes(
                    "Unable to load user access"
                )
            )
        ) {

            window.location.href =
                "/Account/Login";

            return;
        }


        console.error(
            "Dashboard failed to load:",
            err.message
        );

    }
    finally {

        dashboardLoading = false;


        // ----------------------------------------------------
        // HIDE LOADING
        // ----------------------------------------------------

        $("#dashboardSkeleton")
            .stop(true, true)
            .fadeOut(
                200,
                function () {

                    $("#dashboardContent")
                        .stop(true, true)
                        .fadeIn(200);

                }
            );


        // ----------------------------------------------------
        // RUN QUEUED LOAD
        // ----------------------------------------------------

        if (
            dashboardReloadPending
        ) {

            dashboardReloadPending = false;



            setTimeout(
                function () {

                    loadDashboard();

                },
                0
            );

        }
        else {

            dashboardRetryCount = 0;

        }

    }

}


// ============================================================
// REFRESH DASHBOARD
// ============================================================

async function refreshDashboard() {


    await loadDashboard();

}


// ============================================================
// DOCUMENT READY
// ============================================================

document.addEventListener(
    "DOMContentLoaded",
    function () {

        // ----------------------------------------------------
        // DEFAULT MONTH = PREVIOUS MONTH
        // ----------------------------------------------------

        currentReadingMonth =
            getPreviousReadingMonth();


        // ----------------------------------------------------
        // SET MONTH INPUT
        // ----------------------------------------------------

        const monthInput =
            document.getElementById(
                "readingMonth"
            );


        if (monthInput) {

            monthInput.value =
                `${currentReadingMonth.substring(0, 4)}-` +
                `${currentReadingMonth.substring(4, 6)}`;

        }


        // ----------------------------------------------------
        // DISPLAY MONTH
        // ----------------------------------------------------

        updateReadingMonthDisplay();


        // ----------------------------------------------------
        // INITIAL DASHBOARD LOAD
        // ----------------------------------------------------

        loadDashboard()
            .catch(
                function (err) {

                    console.error(
                        "Initial dashboard load failed:",
                        err
                    );

                }
            );


        // ----------------------------------------------------
        // APPLY BUTTON
        // ----------------------------------------------------

        $("#btnLoadDashboard")
            .off("click.dashboard")
            .on(
                "click.dashboard",
                async function (e) {

                    e.preventDefault();


                    // ----------------------------------------
                    // GET SELECTED MONTH
                    // ----------------------------------------

                    if (
                        !setReadingMonthFromInput()
                    ) {

                        console.warn(
                            "Invalid reading month."
                        );

                        return;
                    }


                    // ----------------------------------------
                    // LOAD DASHBOARD
                    // ----------------------------------------

                    try {

                        await loadDashboard();

                    }
                    catch (err) {

                        console.error(
                            "Dashboard Apply Error:",
                            err
                        );

                    }

                }
            );


        // ----------------------------------------------------
        // ENTER KEY
        // ----------------------------------------------------

        $("#readingMonth")
            .off("keydown.dashboard")
            .on(
                "keydown.dashboard",
                function (e) {

                    if (
                        e.key === "Enter"
                    ) {

                        e.preventDefault();


                        $("#btnLoadDashboard")
                            .trigger("click");

                    }

                }
            );

    }
);