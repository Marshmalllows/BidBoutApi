using System.Security.Claims;
using BidBoutApi.Data;
using BidBoutApi.DTOs;
using BidBoutApi.Models;
using Microsoft.AspNetCore.Authorization;
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
            .Where(p => p.Status == 0)
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
                p.CreatorId,
                CategoryName = p.Category.Name,
                CurrentBid = context.BidsHistory.Where(b => b.LotId == p.Id).Max(b => (int?)b.Amount) ?? 0,
                FirstImage = p.Images.OrderBy(i => i.Id).Select(i => new { i.Id, i.ImageData }).FirstOrDefault()
            })
            .ToList();

        var response = rawProducts.Select(p => new ProductResponse
        {
            Id = p.Id,
            CreatorId = p.CreatorId,
            Title = p.Title,
            PickupPlace = p.PickupPlace,
            Description = p.Description,
            StartDate = DateTime.SpecifyKind(p.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(p.EndDate, DateTimeKind.Utc),
            ReservePrice = p.ReservePrice,
            CurrentBid = p.CurrentBid,
            Category = new CategoryResponse { Id = p.CategoryId, Name = p.CategoryName },
            Images = p.FirstImage != null
                ?
                [
                    new ImageResponse
                        { Id = p.FirstImage.Id, ImageData = Convert.ToBase64String(p.FirstImage.ImageData) }
                ]
                : []
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
            .Include(p => p.Creator)
            .FirstOrDefault(p => p.Id == id);

        if (product == null) return NotFound();

        if (product.Status == 1) return NotFound("This lot has been deleted.");

        var sellerReviews = context.Reviews.Where(r => r.TargetUserId == product.CreatorId).ToList();
        double sellerRating = sellerReviews.Any() ? sellerReviews.Average(r => r.Rating) : 0;
        int reviewCount = sellerReviews.Count;

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
                b.Bidder.FirstName,
                b.Bidder.LastName,
                b.Bidder.Email
            })
            .ToList();

        var bidsDto = rawBids.Select(b => new BidResponse
        {
            Id = b.Id,
            Amount = b.Amount,
            CreatedAt = DateTime.SpecifyKind(b.CreatedAt, DateTimeKind.Utc),
            BidderId = b.BidderId,
            BidderName = (!string.IsNullOrEmpty(b.FirstName) && !string.IsNullOrEmpty(b.LastName))
                ? $"{b.FirstName} {b.LastName}" : b.Email.Split('@')[0]
        }).ToList();

        var currentBid = bidsDto.FirstOrDefault()?.Amount ?? 0;
        var winnerId = bidsDto.FirstOrDefault()?.BidderId;

        var sellerName = (!string.IsNullOrEmpty(product.Creator.FirstName) && !string.IsNullOrEmpty(product.Creator.LastName))
            ? $"{product.Creator.FirstName} {product.Creator.LastName}" : product.Creator.Email.Split('@')[0];

        var isEnded = product.EndDate <= DateTime.UtcNow;
        var isWinner = isEnded && currentUserId.HasValue && currentUserId.Value == winnerId;
        var isOwner = currentUserId.HasValue && currentUserId.Value == product.CreatorId;

        var response = new ProductResponse
        {
            Id = product.Id,
            CreatorId = product.CreatorId,
            Title = product.Title,
            PickupPlace = product.PickupPlace,
            Description = product.Description,
            StartDate = DateTime.SpecifyKind(product.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(product.EndDate, DateTimeKind.Utc),
            ReservePrice = product.ReservePrice,
            CurrentBid = currentBid,
            SellerName = sellerName,
            SellerRating = Math.Round(sellerRating, 1),
            SellerReviewCount = reviewCount,
            Bids = bidsDto,
            SellerEmail = (isWinner || isOwner) ? product.Creator.Email : null,
            SellerPhone = (isWinner || isOwner) ? product.Creator.Phone : null,
            Category = new CategoryResponse { Id = product.Category.Id, Name = product.Category.Name },
            Images = product.Images.Select(i => new ImageResponse { Id = i.Id, ImageData = Convert.ToBase64String(i.ImageData) }).ToList()
        };

        return Ok(response);
    }

    [HttpGet("my")]
    [Authorize]
    public IActionResult GetMyLots()
    {
        var userId = GetUserId();
        if (userId == -1) return Unauthorized();

        var rawProducts = context.Products
            .Include(p => p.Category)
            .Where(p => p.CreatorId == userId && p.Status == 0)
            .OrderByDescending(p => p.CreatedAt)
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
                p.CreatorId,
                CategoryName = p.Category.Name,
                CurrentBid = context.BidsHistory.Where(b => b.LotId == p.Id).Max(b => (int?)b.Amount) ?? 0,
                FirstImage = p.Images.OrderBy(i => i.Id).Select(i => new { i.Id, i.ImageData }).FirstOrDefault()
            })
            .ToList();

        var response = rawProducts.Select(p => new ProductResponse
        {
            Id = p.Id,
            CreatorId = p.CreatorId,
            Title = p.Title,
            PickupPlace = p.PickupPlace,
            Description = p.Description,
            StartDate = DateTime.SpecifyKind(p.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(p.EndDate, DateTimeKind.Utc),
            ReservePrice = p.ReservePrice,
            CurrentBid = p.CurrentBid,
            Category = new CategoryResponse { Id = p.CategoryId, Name = p.CategoryName },
            Images = p.FirstImage != null
                ?
                [
                    new ImageResponse
                        { Id = p.FirstImage.Id, ImageData = Convert.ToBase64String(p.FirstImage.ImageData) }
                ]
                : []
        }).ToList();

        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (userId == -1) return Unauthorized();

        var product = await context.Products.FindAsync(id);
        if (product == null) return NotFound();

        if (product.CreatorId != userId) return Forbid();

        if (product.EndDate <= DateTime.UtcNow)
            return BadRequest("Cannot delete closed auction");

        product.Status = 1;
        await context.SaveChangesAsync();

        return Ok(new { message = "Lot cancelled successfully" });
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromForm] CreateProductRequest dto, [FromForm] List<int> deletedImageIds)
    {
        var userId = GetUserId();
        if (userId == -1) return Unauthorized();

        if (string.IsNullOrEmpty(dto.Title) || dto.Title.Length > 100) return BadRequest("Title must be between 1 and 100 characters");
        if (string.IsNullOrEmpty(dto.PickupPlace) || dto.PickupPlace.Length > 100) return BadRequest("Pickup place must be under 100 characters");
        if (dto.ReservePrice < 0) return BadRequest("Reserve price cannot be negative");

        if (dto.Duration is <= 0 or > 30) return BadRequest("Duration must be between 1 and 30 days");

        var product = await context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        if (product.CreatorId != userId) return Forbid();

        if (product.StartDate <= DateTime.UtcNow)
            return BadRequest("Cannot edit lot that has already started");

        var utcStart = dto.StartDate.ToUniversalTime();
        if (utcStart < DateTime.UtcNow.AddMinutes(-5)) return BadRequest("Start date cannot be in the past");
        if (utcStart > DateTime.UtcNow.AddYears(1)) return BadRequest("Start date cannot be more than 1 year in the future");

        product.Title = dto.Title;
        product.CategoryId = dto.CategoryId;
        product.PickupPlace = dto.PickupPlace;
        product.Description = dto.Description;

        product.StartDate = utcStart;
        product.EndDate = utcStart.AddDays(dto.Duration);
        product.ReservePrice = dto.ReservePrice;
        product.UpdatedAt = DateTime.UtcNow;

        if (deletedImageIds != null && deletedImageIds.Any())
        {
            var imagesToDelete = product.Images.Where(i => deletedImageIds.Contains(i.Id)).ToList();
            if (imagesToDelete.Count != 0) context.Images.RemoveRange(imagesToDelete);
        }

        if (dto.Images.Length > 0)
        {
            foreach (var file in dto.Images)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                product.Images.Add(new Image { ImageData = ms.ToArray() });
            }
        }

        await context.SaveChangesAsync();
        return Ok(new { message = "Lot updated" });
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

        switch (dto.Images.Length)
        {
            case 0:
                return BadRequest("No images uploaded");
            case > 50:
                return BadRequest("Too many images.");
        }

        if (string.IsNullOrEmpty(dto.Title) || dto.Title.Length > 100) return BadRequest("Title must be between 1 and 100 characters");
        if (string.IsNullOrEmpty(dto.PickupPlace) || dto.PickupPlace.Length > 100) return BadRequest("Pickup place must be under 100 characters");
        if (dto.ReservePrice < 0) return BadRequest("Reserve price cannot be negative");

        if (dto.Duration <= 0 || dto.Duration > 30) return BadRequest("Duration must be between 1 and 30 days");

        var utcStart = dto.StartDate.ToUniversalTime();
        if (utcStart < DateTime.UtcNow.AddMinutes(-5)) return BadRequest("Start date cannot be in the past");
        if (utcStart > DateTime.UtcNow.AddYears(1)) return BadRequest("Start date cannot be more than 1 year in the future");

        var product = new Product
        {
            Title = dto.Title,
            CategoryId = dto.CategoryId,
            PickupPlace = dto.PickupPlace,
            Description = dto.Description,
            StartDate = utcStart,
            EndDate = utcStart.AddDays(dto.Duration),
            ReservePrice = dto.ReservePrice,
            CreatorId = user.Id,
            Images = new List<Image>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = 0
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
            CreatorId = product.CreatorId,
            Title = product.Title,
            PickupPlace = product.PickupPlace,
            Description = product.Description,
            StartDate = DateTime.SpecifyKind(product.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(product.EndDate, DateTimeKind.Utc),
            ReservePrice = product.ReservePrice,
            CurrentBid = 0,
            Category = context.Categories.Where(c => c.Id == product.CategoryId).Select(c => new CategoryResponse { Id = c.Id, Name = c.Name }).FirstOrDefault()!,
            Images = product.Images.Select(i => new ImageResponse { Id = i.Id, ImageData = Convert.ToBase64String(i.ImageData) }).ToList()
        };

        return Ok(response);
    }

    private int GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : -1;
    }
}