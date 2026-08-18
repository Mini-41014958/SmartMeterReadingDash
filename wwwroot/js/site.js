// =========================================================
// USER MENU
// =========================================================

document.addEventListener("DOMContentLoaded", function () {

    const userButton =
        document.getElementById("userMenuButton");

    const userDropdown =
        document.getElementById("userDropdown");


    if (!userButton || !userDropdown)
        return;


    userButton.addEventListener(
        "click",
        function (event) {

            event.stopPropagation();

            userDropdown.classList.toggle("show");

        }
    );


    // Close when clicking outside

    document.addEventListener(
        "click",
        function (event) {

            if (
                !userDropdown.contains(event.target) &&
                !userButton.contains(event.target)
            ) {

                userDropdown.classList.remove(
                    "show"
                );

            }

        }
    );

});


// =========================================================
// LOGOUT
// =========================================================

async function logout() {

    try {

        const response =
            await fetch(
                "/api/AuthApi/logout",
                {
                    method: "POST"
                }
            );


        if (!response.ok) {

            console.error(
                "Logout API failed."
            );

        }

    }
    catch (error) {

        console.error(
            "Logout error:",
            error
        );

    }
    finally {

        // Remove UI-only information
        localStorage.removeItem("username");
        localStorage.removeItem("fullName");
        localStorage.removeItem("role");

        // Go to login
        window.location.href =
            "/Account/Login";
    }
}