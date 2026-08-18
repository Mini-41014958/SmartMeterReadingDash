using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMeterReadingDash.Models;
using SmartMeterReadingDash.Services;
using System.Diagnostics;

namespace SmartMeterReadingDash.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly Dashboard _dashboard;

        public DashboardController(ILogger<DashboardController> logger, Dashboard dashboard )
        {
            _logger = logger;
            _dashboard = dashboard;
        }

        public IActionResult Index()
        {
            ViewBag.status = _dashboard.Testconnection();
            return View();
        }

        public IActionResult MeterSummary()
        {
            return View ();
        }
    }
}
