using Microsoft.AspNetCore.Mvc;

namespace WorkPermitManager.Controllers
{
    public class AuthenicationController : Controller
    {
        public IActionResult SignIn()
        {
            return View();
        }
    }
}
