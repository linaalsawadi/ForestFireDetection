using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForestFireDetection.Controllers
{
    public class MapController : Controller
    {
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }
    }
}
