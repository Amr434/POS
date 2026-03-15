using Microsoft.AspNetCore.Mvc;

namespace POS.Controllers
{
    public class InstallmentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
