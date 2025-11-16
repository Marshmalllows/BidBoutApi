using BidBoutApi.Data;
using Microsoft.AspNetCore.Mvc;

namespace BidBoutApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(MyDbContext context) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        var categories = context.Categories.Select(c => new { c.Id, c.Name }).ToList();

        return Ok(categories);
    }
}
    