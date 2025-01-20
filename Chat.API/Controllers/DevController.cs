using Chat.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Chat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevController : ControllerBase
{
    private readonly DatabaseSeeder _seeder;
    private readonly IWebHostEnvironment _environment;

    public DevController(DatabaseSeeder seeder, IWebHostEnvironment environment)
    {
        _seeder = seeder;
        _environment = environment;
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedDatabase()
    {
        // Only allow seeding in development environment
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            await _seeder.SeedAsync();
            return Ok("Database seeded successfully!");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error seeding database: {ex.Message}");
        }
    }
}