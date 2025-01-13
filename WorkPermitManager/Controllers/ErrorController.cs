using Microsoft.AspNetCore.Mvc;

namespace WorkPermitManager.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult Error400()
        {
            return View();
        }

        public IActionResult Error401()
        {
            return View();
        }

        public IActionResult Error403()
        {
            return View();
        }

        public IActionResult Error404()
        {
            return View();
        }

        public IActionResult Error500()
        {
            return View();
        }

        public IActionResult Error503()
        {
            return View();
        }
    }
}
