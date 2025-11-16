using BidBoutApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BidBoutApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LotsController(MyDbContext context) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        var products = context.Products.Include(p => p.Category).ToList();

        return Ok(products);
    }
    
    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var product = context.Products
            .Include(p => p.Category)
            .FirstOrDefault(p => p.Id == id);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

}