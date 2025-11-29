namespace BidBoutApi.DTOs;

public class CreateReviewRequest
{
    public int TargetUserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}