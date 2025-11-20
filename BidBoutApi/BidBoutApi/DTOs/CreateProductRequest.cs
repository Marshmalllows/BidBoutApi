namespace BidBoutApi.DTOs;

public class CreateProductRequest
{
    public string Title { get; set; } = null!;
    public int CategoryId { get; set; }
    public string PickupPlace { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public int Duration { get; set; }
    public int ReservePrice { get; set; }
    public IFormFile[] Images { get; set; } = [];
}
