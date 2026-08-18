using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartMeterReadingDash.Controllers
{
    public class AccountController : Controller
    {
      
         [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            // User is already authenticated
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

    }
}
