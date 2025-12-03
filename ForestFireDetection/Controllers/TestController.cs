using ForestFireDetection.Services;
using Microsoft.AspNetCore.Mvc;

public class TestController : Controller
{
    [HttpGet("api/test-fuzzy")]
    public IActionResult TestFuzzy()
    {
        var fuzzy = new FuzzyEngine();

        double temperature = 59.9;  // max of tHigh
        double humidity = 1.0;      // max of hDry
        double smoke = 10.0;        // max of sHigh


        double fireScore = fuzzy.ComputeFireScore(temperature, humidity, smoke);

        return Ok($"🔥 Fire Score: {fireScore:F2}");
    }
}
