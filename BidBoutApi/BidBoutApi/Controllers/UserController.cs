using System.Security.Claims;
using BidBoutApi.Data;
using BidBoutApi.DTOs;
using BidBoutApi.Models;
using Microsoft.AspNetCore.Authorization; // Не забудь додати
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BidBoutApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(MyDbContext context) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

        var userId = int.Parse(userIdClaim.Value);

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        return Ok(new
        {
            user.FirstName,
            user.LastName,
            user.Phone,
            user.Region,
            user.City
        });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] DTOs.UpdateUserRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

        var userId = int.Parse(userIdClaim.Value);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.Region = request.Region;
        user.City = request.City;
    
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(new { message = "Data updated" });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Region,
            user.City,
            user.CreatedAt
        });
    }
}