using Microsoft.AspNetCore.Mvc;

namespace WorkPermitManager.Controllers
{
    public class EmployersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
