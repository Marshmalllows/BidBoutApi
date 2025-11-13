namespace BidBoutApi.DTOs;

public class RegisterRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string DeviceType { get; set; } = null!;
    public string Browser { get; set; } = null!;
    public string OS { get; set; } = null!;
}
