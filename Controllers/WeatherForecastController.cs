using Contracts;
using Microsoft.AspNetCore.Mvc;

namespace BecaWebService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IGenericRepository _repo;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IGenericRepository repo)
        {
            _logger = logger;
            _repo = repo;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            _logger.LogInformation("Here is info message from the controller.");
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("now")]
        public IActionResult GetNow()
        {
            return Ok(DateTime.Now);
        }
    }
}
