namespace BidBoutApi.DTOs;

public class SellerProfileResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int ReviewsCount { get; set; }
    public List<ReviewResponse> Reviews { get; set; } = new();
}