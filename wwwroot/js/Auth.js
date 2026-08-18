document.addEventListener("DOMContentLoaded", function () {

    const loginForm =
        document.getElementById("loginForm");

    const registerForm =
        document.getElementById("registerForm");


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

});


// ============================================================
// LOGIN
// ============================================================

async function handleLogin(event) {

    event.preventDefault();

    const username =
        document.getElementById("username")
            .value
            .trim();

    const password =
        document.getElementById("password")
            .value;

    const errorElement =
        document.getElementById("loginError");

    const button =
        document.getElementById("loginButton");

    const buttonText =
        document.getElementById("loginButtonText");

    const loader =
        document.getElementById("loginLoader");


    errorElement.style.display = "none";


    // --------------------------------------------------------
    // Basic validation
    // --------------------------------------------------------

    if (!username || !password) {

        errorElement.textContent =
            "Username and password are required.";

        errorElement.style.display =
            "block";

        return;
    }


    button.disabled = true;

    buttonText.style.display =
        "none";

    loader.style.display =
        "inline-block";


    try {

        const response = await fetch(
            "/api/AuthApi/login",
            {
                method: "POST",

                headers: {
                    "Content-Type":
                        "application/json"
                },

                body: JSON.stringify({
                    username: username,
                    password: password
                })
            }
        );


        const data =
            await response.json();


        // ----------------------------------------------------
        // API error
        // ----------------------------------------------------

        if (!response.ok) {

            throw new Error(
                data.message ||
                "Login failed."
            );
        }


        // ----------------------------------------------------
        // Validate response
        // ----------------------------------------------------
        if (!data.success) {

            throw new Error(
                data.message ||
                "Invalid login response."
            );
        }


        // ----------------------------------------------------
        // Store JWT
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


        // ----------------------------------------------------
        // Calculate token expiry
        // ----------------------------------------------------

        const expiresIn =
            Number(data.expiresIn) || 60;


        window.location.href =
            "/Dashboard/Index";

    }
    catch (error) {

        errorElement.textContent =
            error.message ||
            "Unable to login.";

        errorElement.style.display =
            "block";

    }
    finally {

        button.disabled = false;

        buttonText.style.display =
            "inline";

        loader.style.display =
            "none";
    }

}


function togglePassword() {

    const password =
        document.getElementById(
            "password"
        );

    const button =
        document.querySelector(
            "#loginForm .password-toggle"
        );


    if (!password || !button) {
        return;
    }


    if (password.type === "password") {

        password.type = "text";

        button.textContent =
            "Hide";

    }
    else {

        password.type = "password";

        button.textContent =
            "Show";
    }

}

function toggleRegisterPassword() {

    const password =
        document.getElementById(
            "registerPassword"
        );

    const button =
        document.querySelector(
            "#registerForm .password-toggle"
        );


    if (!password || !button) {
        return;
    }


    if (password.type === "password") {

        password.type = "text";

        button.textContent =
            "Hide";

    }
    else {

        password.type = "password";

        button.textContent =
            "Show";
    }

}

async function handleRegister(event) {

    event.preventDefault();


    const username =
        document.getElementById(
            "registerUsername"
        ).value.trim();


    const fullName =
        document.getElementById(
            "fullName"
        ).value.trim();


    const password =
        document.getElementById(
            "registerPassword"
        ).value;


    const confirmPassword =
        document.getElementById(
            "confirmPassword"
        ).value;


    const message =
        document.getElementById(
            "registerMessage"
        );


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


    message.style.display =
        "none";


    // --------------------------------------------------------
    // Validation
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


    button.disabled = true;

    buttonText.style.display =
        "none";

    loader.style.display =
        "inline-block";


    try {

        // ----------------------------------------------------
        // FIRST USER REGISTRATION
        // No JWT required
        // ----------------------------------------------------

        const response = await fetch(
            "/api/AuthApi/register",
            {
                method: "POST",

                headers: {
                    "Content-Type":
                        "application/json"
                },

                body: JSON.stringify({

                    username: username,

                    password: password,

                    fullName: fullName

                })
            }
        );


        const data =
            await response.json();


        // ----------------------------------------------------
        // API error
        // ----------------------------------------------------

        if (!response.ok) {

            throw new Error(
                data.message ||
                "Registration failed."
            );
        }


        // ----------------------------------------------------
        // Validate response
        // ----------------------------------------------------

        if (!data.success) {

            throw new Error(
                data.message ||
                "Registration failed."
            );
        }


        // ----------------------------------------------------
        // Success
        // ----------------------------------------------------

        showRegisterMessage(
            "Administrator created successfully. Redirecting to login...",
            "success"
        );


        document
            .getElementById("registerForm")
            .reset();


        // ----------------------------------------------------
        // Redirect to login
        // ----------------------------------------------------

        setTimeout(function () {

            window.location.href =
                "/Account/Login";

        }, 1500);

    }
    catch (error) {

        showRegisterMessage(
            error.message ||
            "Unable to create administrator.",
            "error"
        );

    }
    finally {

        button.disabled = false;

        buttonText.style.display =
            "inline";

        loader.style.display =
            "none";
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
// LOGOUT
// ============================================================

function logout() {

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


    window.location.href =
        "/Account/Login";
}