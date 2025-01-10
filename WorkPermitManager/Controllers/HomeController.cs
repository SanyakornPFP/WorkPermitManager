using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WorkPermitManager.Models;

namespace WorkPermitManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult DashBoard()
        {
            return View();
        }
    }
}
