using Microsoft.AspNetCore.Mvc;
using SmartMeterReadingDash.Models;
using SmartMeterReadingDash.Services;
using System.Diagnostics;

namespace SmartMeterReadingDash.Controllers
{
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

        //get total metter Allied + Kimbal including all department for the current month till day - 1
        public IActionResult MeterSummary()
        {
            return View ();
        }
    }
}
