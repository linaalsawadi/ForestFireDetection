using Microsoft.AspNetCore.Mvc;

namespace ForestFireDetection.Controllers
{
    public class ApplicationUsersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
