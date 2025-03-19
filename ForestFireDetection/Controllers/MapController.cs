using Microsoft.AspNetCore.Mvc;

namespace ForestFireDetection.Controllers
{
    public class MapController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
