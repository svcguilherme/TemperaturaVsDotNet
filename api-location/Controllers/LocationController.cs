using Microsoft.AspNetCore.Mvc;
using LocationApi.Services;

namespace LocationApi.Controllers;

[ApiController]
public class LocationController(LocationService locationService) : ControllerBase
{
    [HttpGet("/location")]
    public async Task<IActionResult> GetLocation([FromQuery] string? ip)
    {
        var clientIp = ip ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "8.8.8.8";
        var result = await locationService.GetLocationAsync(clientIp);
        return Ok(result);
    }

    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "healthy", service = "api-location", timestamp = DateTime.UtcNow });
}
