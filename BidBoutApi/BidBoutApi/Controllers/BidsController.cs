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
public class BidsController(MyDbContext context) : ControllerBase
{
    private const int MinStep = 10;

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> PlaceBid([FromBody] BidRequest request)
    {
        var userId = GetUserId();
        if (userId == -1) return Unauthorized();

        var result = await ProcessBidding(request.LotId, userId, request.Amount, isAutoBidSetup: false);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(new { newPrice = result.NewPrice });
    }

    [HttpPost("auto")]
    [Authorize]
    public async Task<IActionResult> SetAutoBid([FromBody] BidRequest request)
    {
        var userId = GetUserId();
        if (userId == -1) return Unauthorized();

        var existingAutoBid = await context.AutoBids
            .FirstOrDefaultAsync(ab => ab.LotId == request.LotId && ab.UserId == userId);

        if (existingAutoBid != null)
        {
            existingAutoBid.MaxAmount = request.Amount;
        }
        else
        {
            context.AutoBids.Add(new AutoBid
            {
                LotId = request.LotId,
                UserId = userId,
                MaxAmount = request.Amount
            });
        }
        await context.SaveChangesAsync();

        var result = await ProcessBidding(request.LotId, userId, request.Amount, isAutoBidSetup: true);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(new { newPrice = result.NewPrice, message = "Auto bid set successfully" });
    }

    private async Task<(bool Success, string Message, int NewPrice)> ProcessBidding(int lotId, int initialBidderId, int amount, bool isAutoBidSetup)
    {
        var lot = await context.Products.FindAsync(lotId);
        if (lot == null) return (false, "Lot not found", 0);
        if (lot.EndDate < DateTime.UtcNow) return (false, "Auction has ended", 0);
        if (lot.StartDate > DateTime.UtcNow) return (false, "Auction has not started yet", 0);

        var currentHighestBid = await context.BidsHistory
            .Where(b => b.LotId == lotId)
            .OrderByDescending(b => b.Amount)
            .FirstOrDefaultAsync();

        var currentPrice = currentHighestBid?.Amount ?? 0;
        var currentWinnerId = currentHighestBid?.BidderId ?? -1;

        if (!isAutoBidSetup)
        {
            if (amount <= currentPrice) return (false, $"Bid must be higher than {currentPrice}", currentPrice);
            
            await AddBidToHistory(lotId, initialBidderId, amount);
            currentPrice = amount;
            currentWinnerId = initialBidderId;
        }
        else 
        {
            if (currentWinnerId != initialBidderId)
            {
                var bidToPlace = currentPrice + MinStep;
                if (currentPrice == 0) bidToPlace = Math.Max(lot.ReservePrice ?? 10, 10);

                if (bidToPlace > amount) bidToPlace = amount;

                if (bidToPlace > currentPrice)
                {
                    await AddBidToHistory(lotId, initialBidderId, bidToPlace);
                    currentPrice = bidToPlace;
                    currentWinnerId = initialBidderId;
                }
            }
        }

        while (true)
        {
            var defender = await context.AutoBids
                .Where(ab => ab.LotId == lotId && ab.UserId != currentWinnerId && ab.MaxAmount > currentPrice)
                .OrderByDescending(ab => ab.MaxAmount)
                .FirstOrDefaultAsync();

            if (defender == null) 
            {
                break;
            }

            var defenseBid = currentPrice + MinStep;
            
            if (defenseBid > defender.MaxAmount) 
                defenseBid = defender.MaxAmount;

            if (defenseBid <= currentPrice) 
                break;

            await AddBidToHistory(lotId, defender.UserId, defenseBid);
            
            currentPrice = defenseBid;
            currentWinnerId = defender.UserId;
        }

        return (true, "Success", currentPrice);
    }

    private async Task AddBidToHistory(int lotId, int userId, int amount)
    {
        context.BidsHistory.Add(new Bid
        {
            LotId = lotId,
            BidderId = userId,
            Amount = amount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private int GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : -1;
    }
}