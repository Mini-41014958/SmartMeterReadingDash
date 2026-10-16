
let currentReadingMonth = "";
let currentUserAccess = null;

let activeDashboardReadingMonth = "";

function getApiUrl(endpoint) {
    const basePath = window.location.pathname
        .toLowerCase()
        .startsWith("/smartmeter/")
        ? "/SmartMeter"
        : "";

    return `${basePath}/api/${endpoint}`;
}


function normalizeAccessValue(value) {
    return String(value ?? "").trim().toUpperCase();
}

function isSuperAdmin() {
    return normalizeAccessValue(currentUserAccess?.role) === "SUPERADMIN";
}

function getUserCompany() {
    return normalizeAccessValue(currentUserAccess?.company);
}

function getUserDepartment() {
    return normalizeAccessValue(currentUserAccess?.department);
}

function hasDepartmentRestriction() {
    if (!currentUserAccess || isSuperAdmin()) {
        return false;
    }

    return getUserDepartment() !== "";
}

function canAccessCompany(company) {
    if (!currentUserAccess) {
        return false;
    }

    const requestedCompany = normalizeAccessValue(company);

    if (!requestedCompany) {
        return false;
    }

    if (isSuperAdmin()) {
        return true;
    }

    return getUserCompany() === requestedCompany;
}

function canAccessBrpl() {
    return canAccessCompany("BRPL");
}

function canAccessBypl() {
    return canAccessCompany("BYPL");
}


async function loadCurrentUserAccess() {
    const url = getApiUrl("AuthApi/my-access");

    const response = await fetch(url, {
        method: "GET",
        credentials: "same-origin",
        headers: {
            "Accept": "application/json"
        },
        cache: "no-store"
    });

    if (!response.ok) {
        throw new Error(
            `Unable to load user access. HTTP ${response.status}`
        );
    }

    const access = await response.json();

    if (!access) {
        throw new Error("User access information is empty.");
    }

    currentUserAccess = {
        userId: access.userId ?? access.UserId,
        username: access.username ?? access.Username ?? "",
        role: normalizeAccessValue(access.role ?? access.Role),
        company: normalizeAccessValue(access.company ?? access.Company),
        department: normalizeAccessValue(
            access.department ?? access.Department
        )
    };
}

function applyDashboardAccess() {
    const brplAllowed = canAccessBrpl();
    const byplAllowed = canAccessBypl();

    $(".brpl-section").toggle(brplAllowed);
    $("#brplDashboardSection").toggle(brplAllowed);
    $("#brplSection").toggle(brplAllowed);

    $(".bypl-section").toggle(byplAllowed);
    $("#byplDashboardSection").toggle(byplAllowed);
    $("#byplSection").toggle(byplAllowed);

    $("[data-company='BRPL']").toggle(brplAllowed);
    $("[data-company='BYPL']").toggle(byplAllowed);
}

function getReadingMonth() {
    return activeDashboardReadingMonth || currentReadingMonth;
}

function getPreviousReadingMonth() {
    const today = new Date();

    const previousDate = new Date(
        today.getFullYear(),
        today.getMonth() - 1,
        1
    );

    const year = previousDate.getFullYear().toString();
    const month = String(previousDate.getMonth() + 1).padStart(2, "0");

    return year + month;
}

function formatReadingMonth(month) {
    if (!month || !/^\d{6}$/.test(month)) {
        return "";
    }

    const year = month.substring(0, 4);
    const monthNumber = month.substring(4, 6);
    const numericMonth = Number(monthNumber);

    if (numericMonth < 1 || numericMonth > 12) {
        return month;
    }

    const date = new Date(
        Number(year),
        numericMonth - 1,
        1
    );

    if (isNaN(date.getTime())) {
        return month;
    }

    return date.toLocaleString("en-US", {
        month: "long",
        year: "numeric"
    });
}

function updateReadingMonthDisplay() {
    const formatted = formatReadingMonth(currentReadingMonth);

    $("#readingMonthDisplay").text(formatted);
    $("#selectedReadingMonth").text(formatted);
}

function setReadingMonthFromInput() {
    const input = document.getElementById("readingMonth");

    if (!input || !input.value) {
        return false;
    }

    const value = input.value.trim();

    if (!/^\d{4}-\d{2}$/.test(value)) {
        return false;
    }

    const monthNumber = Number(value.substring(5, 7));

    if (monthNumber < 1 || monthNumber > 12) {
        return false;
    }

    currentReadingMonth = value.replace("-", "");

    updateReadingMonthDisplay();

    return true;
}


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
            failureReason: String(reason).trim(),
            count: Number(count) || 0
        };
    });
}


async function fetchDashboardApi(endpoint, readingMonth) {
    if (!readingMonth) {
        throw new Error("Reading month is required.");
    }

    const url =
        `${getApiUrl(endpoint)}` +
        `?ReadingMonth=${encodeURIComponent(readingMonth)}`;

    console.log("Dashboard API request:", {
        endpoint: endpoint,
        readingMonth: readingMonth
    });

    const response = await fetch(url, {
        method: "GET",
        credentials: "same-origin",
        headers: {
            "Accept": "application/json"
        },
        cache: "no-store"
    });

    if (response.status === 401) {
        throw new Error("Session expired. Please login again.");
    }

    if (response.status === 403) {
        throw new Error("Access denied for this company.");
    }

    if (!response.ok) {
        throw new Error(
            `Dashboard API failed. HTTP ${response.status}`
        );
    }

    let data;

    try {
        data = await response.json();
    } catch (error) {
        console.error(
            "Dashboard API returned invalid JSON:",
            error
        );

        throw new Error("Dashboard API returned invalid JSON.");
    }

    if (data && data.success === false) {
        throw new Error(
            data.message ||
            "Dashboard API returned success=false."
        );
    }

    return data;
}

let dashboardLoading = false;
let dashboardReloadPending = false;
let dashboardRetryCount = 0;

function delay(ms) {
    return new Promise(function (resolve) {
        setTimeout(resolve, ms);
    });
}


async function loadDashboard() {
    // Queue requests that arrive while a dashboard load is active.
    if (dashboardLoading) {
        dashboardReloadPending = true;
        return;
    }

    dashboardLoading = true;
    dashboardReloadPending = false;

    $("#dashboardSkeleton")
        .stop(true, true)
        .show();

    $("#dashboardContent")
        .stop(true, true)
        .hide();

    try {
        // Load permissions before rendering company sections.
        await loadCurrentUserAccess();

        applyDashboardAccess();

        if (!currentReadingMonth) {
            currentReadingMonth = getPreviousReadingMonth();
        }

        updateReadingMonthDisplay();

        // Use one consistent reading month for this load cycle.
        const readingMonth = currentReadingMonth;

        activeDashboardReadingMonth = readingMonth;

        console.log(
            "Loading dashboard for reading month:",
            readingMonth
        );

        const results = await Promise.allSettled([
            loadMeterSummary(readingMonth),
            loadMeterDownloadSummary(readingMonth),
            loadDepartmentDistribution(readingMonth),
            loadFailureReasonChart(readingMonth)
        ]);

        const names = [
            "Meter Summary",
            "Meter MRO Summary",
            "Department Distribution",
            "Failure Reason Chart"
        ];

        let hasError = false;

        results.forEach(function (result, index) {
            if (result.status === "rejected") {
                hasError = true;

                console.error(
                    `${names[index]} failed for reading month ` +
                    `${readingMonth}:`,
                    result.reason
                );
            } else {
                console.log(
                    `${names[index]} loaded successfully for reading month ` +
                    `${readingMonth}.`
                );
            }
        });

        // Retry the failed dashboard cycle once.
        if (hasError && dashboardRetryCount < 1) {
            dashboardRetryCount++;

            console.warn(
                "One or more dashboard components failed. Retrying once."
            );

            await delay(1000);

            dashboardReloadPending = true;
        } else {
            dashboardRetryCount = 0;
        }
    } catch (err) {
        console.error("Dashboard Loading Error:", err);

        if (
            err.message &&
            (
                err.message.includes("Session expired") ||
                err.message.includes("Unable to load user access")
            )
        ) {
            window.location.href = "/Account/Login";
            return;
        }

        console.error(
            "Dashboard failed to load:",
            err.message
        );
    } finally {
        dashboardLoading = false;
        activeDashboardReadingMonth = "";

        $("#dashboardSkeleton")
            .stop(true, true)
            .fadeOut(200, function () {
                $("#dashboardContent")
                    .stop(true, true)
                    .fadeIn(200);
            });

        // Process any load request queued during the active cycle.
        if (dashboardReloadPending) {
            dashboardReloadPending = false;

            setTimeout(function () {
                loadDashboard();
            }, 0);
        } else {
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

document.addEventListener("DOMContentLoaded", function () {
    // Default to the previous reading month.
    currentReadingMonth = getPreviousReadingMonth();

    const monthInput = document.getElementById("readingMonth");

    if (monthInput) {
        monthInput.value =
            `${currentReadingMonth.substring(0, 4)}-` +
            `${currentReadingMonth.substring(4, 6)}`;
    }

    updateReadingMonthDisplay();

    // Initial dashboard load.
    loadDashboard().catch(function (err) {
        console.error("Initial dashboard load failed:", err);
    });

    // Apply selected reading month.
    $("#btnLoadDashboard")
        .off("click.dashboard")
        .on("click.dashboard", async function (e) {
            e.preventDefault();

            if (!setReadingMonthFromInput()) {
                console.warn(
                    "Invalid reading month. Select a valid month."
                );
                return;
            }

            try {
                await loadDashboard();
            } catch (err) {
                console.error("Dashboard Apply Error:", err);
            }
        });

    // Allow Enter to apply the selected reading month.
    $("#readingMonth")
        .off("keydown.dashboard")
        .on("keydown.dashboard", function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
                $("#btnLoadDashboard").trigger("click");
            }
        });
});
