

let currentReadingMonth = "";

let currentUserAccess = null;


// ============================================================
// API URL HELPER
// ============================================================

function getApiUrl(endpoint) {

    const basePath = window.location.pathname
        .toLowerCase()
        .startsWith("/smartmeter/")
        ? "/SmartMeter"
        : "";

    return `${basePath}/api/${endpoint}`;
}


// ============================================================
// NORMALIZE ACCESS VALUE
// ============================================================

function normalizeAccessValue(value) {

    return String(value ?? "")
        .trim()
        .toUpperCase();

}


// ============================================================
// CHECK SUPER ADMIN
// ============================================================

function isSuperAdmin() {

    return normalizeAccessValue(
        currentUserAccess?.role
    ) === "SUPERADMIN";

}


// ============================================================
// GET USER COMPANY
// ============================================================

function getUserCompany() {

    return normalizeAccessValue(
        currentUserAccess?.company
    );

}


// ============================================================
// GET USER DEPARTMENT
// ============================================================

function getUserDepartment() {

    return normalizeAccessValue(
        currentUserAccess?.department
    );

}


// ============================================================
// CHECK DEPARTMENT RESTRICTION
// ============================================================

function hasDepartmentRestriction() {

    if (!currentUserAccess) {
        return false;
    }

    if (isSuperAdmin()) {
        return false;
    }

    return getUserDepartment() !== "";

}


// ============================================================
// COMPANY ACCESS CHECK
//
// SUPERADMIN -> BRPL + BYPL
// BRPL       -> BRPL only
// BYPL       -> BYPL only
// ============================================================

function canAccessCompany(company) {

    if (!currentUserAccess) {
        return false;
    }

    const requestedCompany =
        normalizeAccessValue(company);

    if (!requestedCompany) {
        return false;
    }

    // SUPERADMIN has access to both companies
    if (isSuperAdmin()) {
        return true;
    }

    return getUserCompany() === requestedCompany;

}


// ============================================================
// BRPL ACCESS
// ============================================================

function canAccessBrpl() {

    return canAccessCompany("BRPL");

}


// ============================================================
// BYPL ACCESS
// ============================================================

function canAccessBypl() {

    return canAccessCompany("BYPL");

}


// ============================================================
// LOAD CURRENT USER ACCESS
// ============================================================

async function loadCurrentUserAccess() {

    const url =
        getApiUrl("AuthApi/my-access");

    const response = await fetch(
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

    if (!response.ok) {

        throw new Error(
            `Unable to load user access. HTTP ${response.status}`
        );

    }

    const access = await response.json();

    if (!access) {

        throw new Error(
            "User access information is empty."
        );

    }

    currentUserAccess = {

        userId:
            access.userId ??
            access.UserId,

        username:
            access.username ??
            access.Username ??
            "",

        role:
            normalizeAccessValue(
                access.role ??
                access.Role
            ),

        company:
            normalizeAccessValue(
                access.company ??
                access.Company
            ),

        department:
            normalizeAccessValue(
                access.department ??
                access.Department
            )

    };

    console.log(
        "Dashboard User Access:",
        currentUserAccess
    );

}


// ============================================================
// APPLY COMPANY ACCESS TO DASHBOARD UI
// ============================================================

function applyDashboardAccess() {

    const brplAllowed =
        canAccessBrpl();

    const byplAllowed =
        canAccessBypl();


    console.log(
        "Dashboard Access:",
        {
            role: currentUserAccess?.role,
            company: currentUserAccess?.company,
            department: currentUserAccess?.department,
            BRPL: brplAllowed,
            BYPL: byplAllowed
        }
    );


    // --------------------------------------------------------
    // BRPL SECTIONS
    // --------------------------------------------------------

    $(".brpl-section").toggle(
        brplAllowed
    );

    $("#brplDashboardSection").toggle(
        brplAllowed
    );

    $("#brplSection").toggle(
        brplAllowed
    );


    // --------------------------------------------------------
    // BYPL SECTIONS
    // --------------------------------------------------------

    $(".bypl-section").toggle(
        byplAllowed
    );

    $("#byplDashboardSection").toggle(
        byplAllowed
    );

    $("#byplSection").toggle(
        byplAllowed
    );


    // --------------------------------------------------------
    // COMPANY SPECIFIC ELEMENTS
    // --------------------------------------------------------

    $("[data-company='BRPL']").toggle(
        brplAllowed
    );

    $("[data-company='BYPL']").toggle(
        byplAllowed
    );

}


// ============================================================
// GET CURRENT READING MONTH
// ============================================================

function getReadingMonth() {

    return currentReadingMonth;

}


// ============================================================
// GET PREVIOUS READING MONTH
// ============================================================

function getPreviousReadingMonth() {

    const today = new Date();

    const previousDate = new Date(
        today.getFullYear(),
        today.getMonth() - 1,
        1
    );

    const year =
        previousDate
            .getFullYear()
            .toString();

    const month =
        String(
            previousDate.getMonth() + 1
        ).padStart(2, "0");

    return year + month;

}


// ============================================================
// FORMAT READING MONTH
// Example: 202608 -> August, 2026
// ============================================================

function formatReadingMonth(month) {

    if (!month || month.length !== 6) {
        return "";
    }

    const year =
        month.substring(0, 4);

    const monthNumber =
        month.substring(4, 6);

    const date =
        new Date(
            Number(year),
            Number(monthNumber) - 1,
            1
        );

    if (isNaN(date.getTime())) {
        return month;
    }

    return date.toLocaleString(
        "en-US",
        {
            month: "long",
            year: "numeric"
        }
    );

}


// ============================================================
// UPDATE READING MONTH DISPLAY
// ============================================================

function updateReadingMonthDisplay() {

    const formatted =
        formatReadingMonth(
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

    if (!input || !input.value) {
        return false;
    }

    const value =
        input.value.trim();

    // HTML month input returns YYYY-MM
    if (
        !/^\d{4}-\d{2}$/.test(value)
    ) {
        return false;
    }

    currentReadingMonth =
        value.replace("-", "");

    updateReadingMonthDisplay();

    return true;

}


// ============================================================
// NORMALIZE FAILURE DATA
//
// This is intentionally global because ReadingTrend.js
// uses it.
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

    const url =
        `${getApiUrl(endpoint)}` +
        `?ReadingMonth=${encodeURIComponent(readingMonth)}`;

    console.log(
        "Dashboard API:",
        url
    );

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


    // --------------------------------------------------------
    // AUTHORIZATION FAILURE
    // --------------------------------------------------------

    if (response.status === 401) {

        throw new Error(
            "Session expired. Please login again."
        );

    }


    // --------------------------------------------------------
    // FORBIDDEN
    // --------------------------------------------------------

    if (response.status === 403) {

        throw new Error(
            "Access denied for this company."
        );

    }


    // --------------------------------------------------------
    // OTHER HTTP ERRORS
    // --------------------------------------------------------

    if (!response.ok) {

        throw new Error(
            `Dashboard API failed. HTTP ${response.status}`
        );

    }


    return await response.json();

}


// ============================================================
// LOAD DASHBOARD
// ============================================================

async function loadDashboard() {

    $("#dashboardSkeleton").show();

    $("#dashboardContent").hide();


    try {

        // ----------------------------------------------------
        // STEP 1
        // Load authorization FIRST
        // ----------------------------------------------------

        await loadCurrentUserAccess();


        // ----------------------------------------------------
        // STEP 2
        // Apply UI authorization
        // ----------------------------------------------------

        applyDashboardAccess();


        // ----------------------------------------------------
        // STEP 3
        // Validate reading month
        // ----------------------------------------------------

        if (!currentReadingMonth) {

            currentReadingMonth =
                getPreviousReadingMonth();

        }


        updateReadingMonthDisplay();


        // ----------------------------------------------------
        // STEP 4
        // Load only authorized dashboard data
        //
        // Each individual loader is responsible for checking
        // BRPL/BYPL access.
        // ----------------------------------------------------

        const results =
            await Promise.allSettled([

                loadMeterSummary(),

                loadMeterDownloadSummary(),

                loadDepartmentDistribution(),

                loadFailureReasonChart()

            ]);


        // ----------------------------------------------------
        // LOG FAILED COMPONENTS
        // ----------------------------------------------------

        results.forEach(
            function (result, index) {

                if (
                    result.status === "rejected"
                ) {

                    const names = [

                        "Meter Summary",

                        "Meter MRO Summary",

                        "Department Distribution",

                        "Failure Reason Chart"

                    ];

                    console.error(
                        `${names[index]} Error:`,
                        result.reason
                    );

                }

            }
        );

    }
    catch (err) {

        console.error(
            "Dashboard Loading Error:",
            err
        );


        // ----------------------------------------------------
        // Session/authentication failure
        // ----------------------------------------------------

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

    }
    finally {

        $("#dashboardSkeleton")
            .fadeOut(
                300,
                function () {

                    $("#dashboardContent")
                        .fadeIn(300);

                }
            );

    }

}


// ============================================================
// REFRESH DASHBOARD
// ============================================================

async function refreshDashboard() {

    try {

        await loadDashboard();

    }
    catch (err) {

        console.error(
            "Dashboard Refresh Error:",
            err
        );

    }

}


// ============================================================
// DOCUMENT READY
// ============================================================

document.addEventListener(
    "DOMContentLoaded",
    function () {

        // ----------------------------------------------------
        // Default month = previous month
        // ----------------------------------------------------

        currentReadingMonth =
            getPreviousReadingMonth();


        // ----------------------------------------------------
        // Set month input
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
        // Display month
        // ----------------------------------------------------

        updateReadingMonthDisplay();


        // ----------------------------------------------------
        // Load dashboard
        // ----------------------------------------------------

        loadDashboard();


        // ----------------------------------------------------
        // APPLY BUTTON
        // ----------------------------------------------------

        $("#btnLoadDashboard")
            .off("click.dashboard")
            .on(
                "click.dashboard",
                async function (e) {

                    e.preventDefault();


                    // -------------------------------
                    // Read selected month
                    // -------------------------------

                    if (
                        !setReadingMonthFromInput()
                    ) {

                        console.warn(
                            "Invalid reading month."
                        );

                        return;

                    }


                    // -------------------------------
                    // Reload dashboard
                    // -------------------------------

                    await loadDashboard();

                }
            );


        // ----------------------------------------------------
        // ENTER KEY ON MONTH INPUT
        // ----------------------------------------------------

        $("#readingMonth")
            .off("keydown.dashboard")
            .on(
                "keydown.dashboard",
                function (e) {

                    if (e.key === "Enter") {

                        e.preventDefault();

                        $("#btnLoadDashboard")
                            .trigger("click");

                    }

                }
            );

    }
);