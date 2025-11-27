using System.Security.Claims; // Не забудь додати цей юзінг
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
        var rawProducts = context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.PickupPlace,
                p.Description,
                p.StartDate,
                p.EndDate,
                p.ReservePrice,
                p.CategoryId,
                CategoryName = p.Category.Name,
                CurrentBid = context.BidsHistory
                    .Where(b => b.LotId == p.Id)
                    .Max(b => (int?)b.Amount) ?? 0,
                Images = p.Images.Select(i => new { i.Id, i.ImageData }).ToList()
            })
            .ToList();

        var response = rawProducts.Select(p => new ProductResponse
        {
            Id = p.Id,
            Title = p.Title,
            PickupPlace = p.PickupPlace,
            Description = p.Description,
            StartDate = DateTime.SpecifyKind(p.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(p.EndDate, DateTimeKind.Utc),
            ReservePrice = p.ReservePrice,
            CurrentBid = p.CurrentBid,
            Category = new CategoryResponse
            {
                Id = p.CategoryId,
                Name = p.CategoryName
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
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? currentUserId = int.TryParse(userIdString, out int uid) ? uid : null;

        var product = context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Creator) // Потрібно для контактів
            .FirstOrDefault(p => p.Id == id);

        if (product == null)
            return NotFound();

        var rawBids = context.BidsHistory
            .Include(b => b.Bidder)
            .Where(b => b.LotId == id)
            .OrderByDescending(b => b.Amount)
            .Select(b => new 
            {
                b.Id,
                b.Amount,
                b.CreatedAt,
                b.BidderId,
                FirstName = b.Bidder.FirstName,
                LastName = b.Bidder.LastName,
                Email = b.Bidder.Email
            })
            .ToList();

        var bidsDto = rawBids.Select(b => new BidResponse
        {
            Id = b.Id,
            Amount = b.Amount,
            CreatedAt = DateTime.SpecifyKind(b.CreatedAt, DateTimeKind.Utc),
            BidderId = b.BidderId,
            BidderName = (!string.IsNullOrEmpty(b.FirstName) && !string.IsNullOrEmpty(b.LastName))
                ? $"{b.FirstName} {b.LastName}"
                : b.Email.Split('@')[0]
        }).ToList();

        var currentBid = bidsDto.FirstOrDefault()?.Amount ?? 0;
        var winnerId = bidsDto.FirstOrDefault()?.BidderId; 

        var sellerName = (!string.IsNullOrEmpty(product.Creator.FirstName) && !string.IsNullOrEmpty(product.Creator.LastName))
            ? $"{product.Creator.FirstName} {product.Creator.LastName}"
            : product.Creator.Email.Split('@')[0];

        var isEnded = product.EndDate <= DateTime.UtcNow;
        var isWinner = isEnded && currentUserId.HasValue && currentUserId.Value == winnerId;

        var response = new ProductResponse
        {
            Id = product.Id,
            Title = product.Title,
            PickupPlace = product.PickupPlace,
            Description = product.Description,
            StartDate = DateTime.SpecifyKind(product.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(product.EndDate, DateTimeKind.Utc),
            ReservePrice = product.ReservePrice,
            CurrentBid = currentBid,
            SellerName = sellerName,
            Bids = bidsDto,
            
            SellerEmail = isWinner ? product.Creator.Email : null,
            SellerPhone = isWinner ? product.Creator.Phone : null,

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
        if (string.IsNullOrEmpty(refreshTokenValue)) return Unauthorized("Refresh token missing!");

        var refreshToken = context.RefreshTokens.SingleOrDefault(rt => rt.Token == refreshTokenValue);
        if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow) return Unauthorized("Token expired!");

        var user = context.Users.SingleOrDefault(u => u.Id == refreshToken.UserId);
        if (user == null) return Unauthorized("User not found!");

        if (dto.Images.Length == 0) return BadRequest("No images uploaded");
        if (dto.Images.Length > 50) return BadRequest("Too many images.");

        var utcStartDate = dto.StartDate.ToUniversalTime();

        var product = new Product
        {
            Title = dto.Title,
            CategoryId = dto.CategoryId,
            PickupPlace = dto.PickupPlace,
            Description = dto.Description,
            StartDate = utcStartDate,
            EndDate = utcStartDate.AddDays(dto.Duration),
            ReservePrice = dto.ReservePrice,
            CreatorId = user.Id,
            Images = new List<Image>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var file in dto.Images)
        {
            if (file.Length > 10 * 1024 * 1024) return BadRequest($"File {file.FileName} too large.");
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            product.Images.Add(new Image { ImageData = ms.ToArray() });
        }

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var response = new ProductResponse
        {
            Id = product.Id,
            Title = product.Title,
            PickupPlace = product.PickupPlace,
            Description = product.Description,
            StartDate = DateTime.SpecifyKind(product.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(product.EndDate, DateTimeKind.Utc),
            ReservePrice = product.ReservePrice,
            CurrentBid = 0,
            Category = context.Categories.Where(c => c.Id == product.CategoryId)
                .Select(c => new CategoryResponse { Id = c.Id, Name = c.Name })
                .FirstOrDefault()!,
            Images = product.Images.Select(i => new ImageResponse
            {
                Id = i.Id,
                ImageData = Convert.ToBase64String(i.ImageData)
            }).ToList()
        };

        return Ok(response);
    }
}