using Microsoft.AspNetCore.Mvc;
using WeatherApi.Services;

namespace WeatherApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherController(WeatherService weatherService) : ControllerBase
{
    [HttpGet("/weather")]
    public async Task<IActionResult> GetWeather([FromQuery] string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return BadRequest(new { error = "city é obrigatório" });

        var result = await weatherService.GetWeatherAsync(city);
        return Ok(result);
    }

    [HttpGet("/forecast")]
    public async Task<IActionResult> GetForecast([FromQuery] string city, [FromQuery] int days = 5)
    {
        if (string.IsNullOrWhiteSpace(city))
            return BadRequest(new { error = "city é obrigatório" });

        days = Math.Clamp(days, 1, 14);
        var result = await weatherService.GetForecastAsync(city, days);
        return Ok(result);
    }

    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "healthy", service = "api-weather", timestamp = DateTime.UtcNow });
}
