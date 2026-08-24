// ============================================================
// SMART METER AUTHENTICATION
// COMPLETE UPDATED SCRIPT
// Supports:
// - Localhost
// - IIS virtual directory: /SmartMeter
// - Login
// - First Admin Registration
// - Logout
// - Password Toggle
// - Safe API Error Handling
// ============================================================


// ============================================================
// APPLICATION BASE PATH
// ============================================================

function getAppBasePath() {

    // --------------------------------------------------------
    // 1. SERVER-PROVIDED BASE PATH
    // Example:
    // Local:       /
    // Production:  /SmartMeter/
    // --------------------------------------------------------

    if (
        typeof window.appBasePath === "string" &&
        window.appBasePath.trim() !== ""
    ) {

        let basePath =
            window.appBasePath.trim();


        if (!basePath.startsWith("/")) {

            basePath =
                "/" + basePath;
        }


        return (
            basePath.replace(/\/+$/, "") +
            "/"
        );
    }


    // --------------------------------------------------------
    // 2. DETECT BASE TAG
    // --------------------------------------------------------

    const baseElement =
        document.querySelector("base[href]");


    if (baseElement) {

        try {

            const baseUrl =
                new URL(
                    baseElement.href,
                    window.location.origin
                );


            let basePath =
                baseUrl.pathname;


            return (
                basePath.replace(/\/+$/, "") +
                "/"
            );

        }
        catch (error) {

            console.warn(
                "Unable to read base tag:",
                error
            );
        }
    }


    // --------------------------------------------------------
    // 3. AUTO-DETECT FROM CURRENT URL
    //
    // Example:
    // /SmartMeter/Account/Login
    // → /SmartMeter/
    // --------------------------------------------------------

    const currentPath =
        window.location.pathname;


    const accountIndex =
        currentPath.toLowerCase()
            .indexOf("/account/");


    if (accountIndex > 0) {

        return (
            currentPath.substring(
                0,
                accountIndex
            ).replace(/\/+$/, "") +
            "/"
        );
    }


    // --------------------------------------------------------
    // 4. DASHBOARD PATH FALLBACK
    // --------------------------------------------------------

    const dashboardIndex =
        currentPath.toLowerCase()
            .indexOf("/dashboard/");


    if (dashboardIndex > 0) {

        return (
            currentPath.substring(
                0,
                dashboardIndex
            ).replace(/\/+$/, "") +
            "/"
        );
    }


    // --------------------------------------------------------
    // 5. ROOT APPLICATION FALLBACK
    // --------------------------------------------------------

    return "/";
}


const appBasePath =
    getAppBasePath();


console.log(
    "Application Base Path:",
    appBasePath
);


// ============================================================
// BUILD APPLICATION URL
// ============================================================

function buildAppUrl(path) {

    if (!path) {

        return appBasePath;
    }


    return (
        appBasePath +
        String(path).replace(/^\/+/, "")
    );
}


// ============================================================
// DOM READY
// ============================================================

document.addEventListener(
    "DOMContentLoaded",
    function () {

        const loginForm =
            document.getElementById(
                "loginForm"
            );


        const registerForm =
            document.getElementById(
                "registerForm"
            );


        if (loginForm) {

            loginForm.addEventListener(
                "submit",
                handleLogin
            );
        }


        if (registerForm) {

            registerForm.addEventListener(
                "submit",
                handleRegister
            );
        }

    }
);


// ============================================================
// SAFE API RESPONSE READER
// IMPORTANT:
// NEVER DISPLAY IIS HTML ERROR PAGES TO USER
// ============================================================

async function getResponseData(response) {

    const contentType =
        (
            response.headers.get(
                "content-type"
            ) || ""
        ).toLowerCase();


    // --------------------------------------------------------
    // JSON RESPONSE
    // --------------------------------------------------------

    if (
        contentType.includes(
            "application/json"
        )
    ) {

        try {

            return await response.json();

        }
        catch {

            return {
                success: false,

                message:
                    "The server returned an invalid JSON response."
            };
        }
    }


    // --------------------------------------------------------
    // HTTP STATUS BASED ERRORS
    // --------------------------------------------------------

    switch (response.status) {

        case 400:

            return {
                success: false,

                message:
                    "Invalid request."
            };


        case 401:

            return {
                success: false,

                message:
                    "Invalid username or password."
            };


        case 403:

            return {
                success: false,

                message:
                    "You do not have permission to perform this action."
            };


        case 404:

            return {
                success: false,

                message:
                    "The requested API endpoint was not found."
            };


        case 409:

            return {
                success: false,

                message:
                    "The requested record already exists."
            };


        case 500:

            return {
                success: false,

                message:
                    "A server error occurred. Please contact the administrator."
            };


        default:

            return {
                success: false,

                message:
                    `Request failed. HTTP ${response.status}.`
            };
    }
}


// ============================================================
// LOGIN
// ============================================================

async function handleLogin(event) {

    event.preventDefault();


    const username =
        document.getElementById(
            "username"
        )
            ?.value
            ?.trim();


    const password =
        document.getElementById(
            "password"
        )
            ?.value;


    const errorElement =
        document.getElementById(
            "loginError"
        );


    const button =
        document.getElementById(
            "loginButton"
        );


    const buttonText =
        document.getElementById(
            "loginButtonText"
        );


    const loader =
        document.getElementById(
            "loginLoader"
        );


    // --------------------------------------------------------
    // CLEAR PREVIOUS ERROR
    // --------------------------------------------------------

    if (errorElement) {

        errorElement.style.display =
            "none";

        errorElement.textContent =
            "";
    }


    // --------------------------------------------------------
    // VALIDATION
    // --------------------------------------------------------

    if (!username || !password) {

        if (errorElement) {

            errorElement.textContent =
                "Username and password are required.";

            errorElement.style.display =
                "block";
        }

        return;
    }


    // --------------------------------------------------------
    // LOADING STATE
    // --------------------------------------------------------

    setButtonLoading(
        button,
        buttonText,
        loader,
        true
    );


    try {

        const apiUrl =
            buildAppUrl(
                "api/AuthApi/login"
            );


        console.log(
            "Login API URL:",
            apiUrl
        );


        const response =
            await fetch(
                apiUrl,
                {
                    method: "POST",

                    headers: {
                        "Content-Type":
                            "application/json",

                        "Accept":
                            "application/json"
                    },

                    credentials:
                        "same-origin",

                    body: JSON.stringify(
                        {
                            username: username,
                            password: password
                        }
                    )
                }
            );


        const data =
            await getResponseData(
                response
            );


        // ----------------------------------------------------
        // HTTP ERROR
        // ----------------------------------------------------

        if (!response.ok) {

            throw new Error(
                data.message ||
                `Login failed. HTTP ${response.status}.`
            );
        }


        // ----------------------------------------------------
        // VALIDATE RESPONSE
        // ----------------------------------------------------

        if (!data.success) {

            throw new Error(
                data.message ||
                "Invalid login response."
            );
        }


        // ----------------------------------------------------
        // STORE USER INFORMATION
        // JWT COOKIE IS MANAGED BY SERVER
        // ----------------------------------------------------

        localStorage.setItem(
            "username",
            data.username || username
        );


        localStorage.setItem(
            "fullName",
            data.fullName || ""
        );


        localStorage.setItem(
            "role",
            data.role || "USER"
        );


        // Remove old local JWT if any
        localStorage.removeItem(
            "accessToken"
        );


        localStorage.removeItem(
            "tokenExpiry"
        );


        // ----------------------------------------------------
        // REDIRECT TO DASHBOARD
        // ----------------------------------------------------

        window.location.href =
            buildAppUrl(
                "Dashboard/Index"
            );

    }
    catch (error) {

        console.error(
            "Login Error:",
            error
        );


        if (errorElement) {

            errorElement.textContent =
                error.message ||
                "Unable to login.";

            errorElement.style.display =
                "block";
        }

    }
    finally {

        setButtonLoading(
            button,
            buttonText,
            loader,
            false
        );
    }
}


// ============================================================
// TOGGLE LOGIN PASSWORD
// ============================================================

function togglePassword() {

    const password =
        document.getElementById(
            "password"
        );


    const button =
        document.querySelector(
            "#loginForm .password-toggle"
        );


    if (!password) {

        return;
    }


    if (password.type === "password") {

        password.type =
            "text";


        if (button) {

            button.textContent =
                "Hide";
        }

    }
    else {

        password.type =
            "password";


        if (button) {

            button.textContent =
                "Show";
        }
    }
}


// ============================================================
// TOGGLE REGISTER PASSWORD
// ============================================================

function toggleRegisterPassword() {

    const password =
        document.getElementById(
            "registerPassword"
        );


    const button =
        document.querySelector(
            "#registerForm .password-toggle"
        );


    if (!password) {

        return;
    }


    if (password.type === "password") {

        password.type =
            "text";


        if (button) {

            button.textContent =
                "Hide";
        }

    }
    else {

        password.type =
            "password";


        if (button) {

            button.textContent =
                "Show";
        }
    }
}


// ============================================================
// REGISTER FIRST ADMIN
// ============================================================

async function handleRegister(event) {

    event.preventDefault();


    const username =
        document.getElementById(
            "registerUsername"
        )
            ?.value
            ?.trim();


    const fullName =
        document.getElementById(
            "fullName"
        )
            ?.value
            ?.trim();


    const password =
        document.getElementById(
            "registerPassword"
        )
            ?.value;


    const confirmPassword =
        document.getElementById(
            "confirmPassword"
        )
            ?.value;


    const button =
        document.getElementById(
            "registerButton"
        );


    const buttonText =
        document.getElementById(
            "registerButtonText"
        );


    const loader =
        document.getElementById(
            "registerLoader"
        );


    // --------------------------------------------------------
    // CLEAR PREVIOUS MESSAGE
    // --------------------------------------------------------

    hideRegisterMessage();


    // --------------------------------------------------------
    // VALIDATION
    // --------------------------------------------------------

    if (!username) {

        showRegisterMessage(
            "Username is required.",
            "error"
        );

        return;
    }


    if (!fullName) {

        showRegisterMessage(
            "Full name is required.",
            "error"
        );

        return;
    }


    if (!password) {

        showRegisterMessage(
            "Password is required.",
            "error"
        );

        return;
    }


    if (password.length < 8) {

        showRegisterMessage(
            "Password must contain at least 8 characters.",
            "error"
        );

        return;
    }


    if (password !== confirmPassword) {

        showRegisterMessage(
            "Passwords do not match.",
            "error"
        );

        return;
    }


    // --------------------------------------------------------
    // LOADING STATE
    // --------------------------------------------------------

    setButtonLoading(
        button,
        buttonText,
        loader,
        true
    );


    try {

        const apiUrl =
            buildAppUrl(
                "api/AuthApi/register"
            );


        console.log(
            "Register API URL:",
            apiUrl
        );


        const response =
            await fetch(
                apiUrl,
                {
                    method: "POST",

                    headers: {
                        "Content-Type":
                            "application/json",

                        "Accept":
                            "application/json"
                    },

                    credentials:
                        "same-origin",

                    body: JSON.stringify(
                        {
                            username: username,
                            password: password,
                            fullName: fullName
                        }
                    )
                }
            );


        const data =
            await getResponseData(
                response
            );


        // ----------------------------------------------------
        // HTTP ERROR
        // ----------------------------------------------------

        if (!response.ok) {

            throw new Error(
                data.message ||
                `Registration failed. HTTP ${response.status}.`
            );
        }


        // ----------------------------------------------------
        // VALIDATE RESPONSE
        // ----------------------------------------------------

        if (!data.success) {

            throw new Error(
                data.message ||
                "Registration failed."
            );
        }


        // ----------------------------------------------------
        // SUCCESS
        // ----------------------------------------------------

        showRegisterMessage(
            "Administrator created successfully. Redirecting to login...",
            "success"
        );


        document
            .getElementById(
                "registerForm"
            )
            ?.reset();


        // ----------------------------------------------------
        // REDIRECT TO LOGIN
        // ----------------------------------------------------

        setTimeout(
            function () {

                window.location.href =
                    buildAppUrl(
                        "Account/Login"
                    );

            },
            1500
        );

    }
    catch (error) {

        console.error(
            "Registration Error:",
            error
        );


        showRegisterMessage(
            error.message ||
            "Unable to create administrator.",
            "error"
        );

    }
    finally {

        setButtonLoading(
            button,
            buttonText,
            loader,
            false
        );
    }
}


// ============================================================
// REGISTER MESSAGE
// ============================================================

function showRegisterMessage(
    text,
    type
) {

    const message =
        document.getElementById(
            "registerMessage"
        );


    if (!message) {

        return;
    }


    message.textContent =
        text;


    message.className =
        `auth-message ${type}`;


    message.style.display =
        "block";
}


// ============================================================
// HIDE REGISTER MESSAGE
// ============================================================

function hideRegisterMessage() {

    const message =
        document.getElementById(
            "registerMessage"
        );


    if (!message) {

        return;
    }


    message.textContent =
        "";


    message.style.display =
        "none";
}


// ============================================================
// COMMON BUTTON LOADING STATE
// ============================================================

function setButtonLoading(
    button,
    buttonText,
    loader,
    isLoading
) {

    if (button) {

        button.disabled =
            isLoading;
    }


    if (buttonText) {

        buttonText.style.display =
            isLoading
                ? "none"
                : "inline";
    }


    if (loader) {

        loader.style.display =
            isLoading
                ? "inline-block"
                : "none";
    }
}

async function logout() {

    try {

        const apiUrl =
            buildAppUrl(
                "api/AuthApi/logout"
            );


        console.log(
            "Logout API URL:",
            apiUrl
        );


        await fetch(
            apiUrl,
            {
                method: "POST",

                credentials:
                    "same-origin",

                headers: {
                    "Accept":
                        "application/json"
                }
            }
        );

    }
    catch (error) {

        console.error(
            "Logout API Error:",
            error
        );
    }
    finally {

        // ----------------------------------------------------
        // CLEAR LOCAL STORAGE
        // ----------------------------------------------------

        localStorage.removeItem(
            "accessToken"
        );


        localStorage.removeItem(
            "username"
        );


        localStorage.removeItem(
            "fullName"
        );


        localStorage.removeItem(
            "role"
        );


        localStorage.removeItem(
            "tokenExpiry"
        );


        // ----------------------------------------------------
        // REDIRECT TO LOGIN
        // ----------------------------------------------------

        window.location.href =
            buildAppUrl(
                "Account/Login"
            );
    }
}