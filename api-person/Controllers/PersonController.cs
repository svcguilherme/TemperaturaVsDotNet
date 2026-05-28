using Microsoft.AspNetCore.Mvc;
using PersonApi.Models;
using PersonApi.Services;

namespace PersonApi.Controllers;

[ApiController]
public class PersonController(PersonService personService) : ControllerBase
{
    [HttpPost("/person")]
    public async Task<IActionResult> Calculate([FromBody] PersonRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "name é obrigatório" });

        if (!DateOnly.TryParse(req.Birthdate, out _))
            return BadRequest(new { error = "birthdate inválido (use yyyy-MM-dd)" });

        var result = await personService.CalculateAsync(req);
        return Ok(result);
    }

    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "healthy", service = "api-person", timestamp = DateTime.UtcNow });
}
