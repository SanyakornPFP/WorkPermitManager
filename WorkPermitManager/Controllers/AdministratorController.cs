using Microsoft.AspNetCore.Mvc;

namespace WorkPermitManager.Controllers
{
    public class AdministratorController : Controller
    {
        public IActionResult ManageUser()
        {
            return View();
        }

        public IActionResult ManagePosition()
        {
            return View();
        }

    }
}
