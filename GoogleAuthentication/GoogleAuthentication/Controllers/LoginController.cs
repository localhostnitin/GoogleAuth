using Microsoft.AspNetCore.Mvc;

namespace GoogleAuthentication.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }else
                return View();
        }
    }
}
