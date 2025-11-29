namespace BidBoutApi.DTOs;

public class UpdateReviewRequest
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}