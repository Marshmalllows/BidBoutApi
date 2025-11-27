namespace BidBoutApi.DTOs;

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
}