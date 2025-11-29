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
public class ReviewsController(MyDbContext context) : ControllerBase
{
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetUserReviews(int userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null) return NotFound("User not found");

        var reviews = await context.Reviews
            .Include(r => r.Reviewer)
            .Where(r => r.TargetUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var reviewDtos = reviews.Select(r => new ReviewResponse
        {
            Id = r.Id,
            ReviewerId = r.ReviewerId,
            ReviewerName = GetUserName(r.Reviewer),
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc)
        }).ToList();

        var avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

        return Ok(new SellerProfileResponse
        {
            Id = user.Id,
            Name = GetUserName(user),
            AverageRating = Math.Round(avgRating, 1),
            ReviewsCount = reviews.Count,
            Reviews = reviewDtos
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest dto)
    {
        var reviewerId = GetUserId();
        if (reviewerId == -1) return Unauthorized();

        if (reviewerId == dto.TargetUserId)
            return BadRequest("You cannot review yourself.");

        var targetUser = await context.Users.FindAsync(dto.TargetUserId);
        if (targetUser == null) return NotFound("Target user not found");

        var exists = await context.Reviews.AnyAsync(r => r.ReviewerId == reviewerId && r.TargetUserId == dto.TargetUserId);
        if (exists) return Conflict("You have already reviewed this user.");

        var review = new Review
        {
            ReviewerId = reviewerId,
            TargetUserId = dto.TargetUserId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        return Ok(new { message = "Review added" });
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewRequest dto)
    {
        var userId = GetUserId();
        var review = await context.Reviews.FindAsync(id);

        if (review == null) return NotFound();
        if (review.ReviewerId != userId) return Forbid();

        review.Rating = dto.Rating;
        review.Comment = dto.Comment;
        review.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return Ok(new { message = "Review updated" });
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var review = await context.Reviews.FindAsync(id);

        if (review == null) return NotFound();
        if (review.ReviewerId != userId) return Forbid();

        context.Reviews.Remove(review);
        await context.SaveChangesAsync();
        return Ok(new { message = "Review deleted" });
    }

    private int GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out int id) ? id : -1;
    }

    private string GetUserName(User u)
    {
        return (!string.IsNullOrEmpty(u.FirstName) && !string.IsNullOrEmpty(u.LastName))
            ? $"{u.FirstName} {u.LastName}"
            : u.Email.Split('@')[0];
    }
}