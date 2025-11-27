namespace BidBoutApi.DTOs;

public class BidResponse
{
    public int Id { get; set; }
    
    public int Amount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public int BidderId { get; set; }
    
    public string BidderName { get; set; } = string.Empty;
}