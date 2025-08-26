namespace BidBoutApi.DTOs;

public class RefreshRequest
{
    public string AccessToken { get; set; } = null;

    public string RefreshToken { get; set; } = null;
}