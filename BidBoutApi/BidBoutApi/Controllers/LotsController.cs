using BidBoutApi.Data;
using BidBoutApi.DTOs;
using BidBoutApi.Models;
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
        var products = context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .ToList();

        var response = products.Select(p => new ProductResponse
        {
            Id = p.Id,
            Title = p.Title,
            PickupPlace = p.PickupPlace,
            Description = p.Description,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            ReservePrice = p.ReservePrice,
            Category = new CategoryResponse
            {
                Id = p.Category.Id,
                Name = p.Category.Name
            },
            Images = p.Images.Select(i => new ImageResponse
            {
                Id = i.Id,
                ImageData = Convert.ToBase64String(i.ImageData)
            }).ToList()
        }).ToList();

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var product = context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefault(p => p.Id == id);

        if (product == null)
            return NotFound();

        var response = new ProductResponse
        {
            Id = product.Id,
            Title = product.Title,
            PickupPlace = product.PickupPlace,
            Description = product.Description,
            StartDate = product.StartDate,
            EndDate = product.EndDate,
            ReservePrice = product.ReservePrice,
            Category = new CategoryResponse
            {
                Id = product.Category.Id,
                Name = product.Category.Name
            },
            Images = product.Images.Select(i => new ImageResponse
            {
                Id = i.Id,
                ImageData = Convert.ToBase64String(i.ImageData)
            }).ToList()
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateProductRequest dto)
    {
        var refreshTokenValue = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshTokenValue))
            return Unauthorized("Refresh token missing!");

        var refreshToken = context.RefreshTokens.SingleOrDefault(rt => rt.Token == refreshTokenValue);
        if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow)
            return Unauthorized("Refresh token invalid or expired!");

        var user = context.Users.SingleOrDefault(u => u.Id == refreshToken.UserId);
        if (user == null)
            return Unauthorized("User not found!");

        if (dto.Images.Length == 0)
            return BadRequest("No images uploaded");
        if (dto.Images.Length > 50)
            return BadRequest("Too many images. Maximum is 50.");

        var product = new Product
        {
            Title = dto.Title,
            CategoryId = dto.CategoryId,
            PickupPlace = dto.PickupPlace,
            Description = dto.Description,
            StartDate = dto.StartDate,
            EndDate = dto.StartDate.AddDays(dto.Duration),
            ReservePrice = dto.ReservePrice,
            CreatorId = user.Id,
            Images = new List<Image>()
        };

        foreach (var file in dto.Images)
        {
            if (file.Length > 10 * 1024 * 1024)
                return BadRequest($"File {file.FileName} is too large. Max 10MB.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            product.Images.Add(new Image
            {
                ImageData = ms.ToArray()
            });
        }

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var response = new ProductResponse
        {
            Id = product.Id,
            Title = product.Title,
            PickupPlace = product.PickupPlace,
            Description = product.Description,
            StartDate = product.StartDate,
            EndDate = product.EndDate,
            ReservePrice = product.ReservePrice,
            Category = context.Categories.Where(c => c.Id == product.CategoryId)
                .Select(c => new CategoryResponse
                {
                    Id = c.Id,
                    Name = c.Name
                }).FirstOrDefault()!,
            Images = product.Images.Select(i => new ImageResponse
            {
                Id = i.Id,
                ImageData = Convert.ToBase64String(i.ImageData)
            }).ToList()
        };

        return Ok(response);
    }
}
