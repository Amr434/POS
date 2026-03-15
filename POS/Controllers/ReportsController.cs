using Microsoft.AspNetCore.Mvc;

namespace POS.Controllers
{
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
