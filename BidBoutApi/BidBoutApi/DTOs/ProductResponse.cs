namespace BidBoutApi.DTOs;

public class ProductResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public CategoryResponse Category { get; set; } = null!;
    public string PickupPlace { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? ReservePrice { get; set; }

    public int CurrentBid { get; set; } 
    
    public string SellerName { get; set; } = string.Empty;
    
    public string? SellerEmail { get; set; }
    
    public string? SellerPhone { get; set; }

    public List<BidResponse> Bids { get; set; } = new();
    
    public List<ImageResponse> Images { get; set; } = null!;
}