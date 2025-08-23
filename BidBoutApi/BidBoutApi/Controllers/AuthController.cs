using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BidBoutApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace BidBoutApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MyDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(MyDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] DTOs.LoginRequest request)
    {
        var user = _context.Users.SingleOrDefault(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return BadRequest("Wrong email or password!");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:ExpiryMinutes")),
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"],
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);

        user.RefreshToken = Guid.NewGuid().ToString();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("JwtSettings:ExpiryDays"));
        _context.SaveChanges();

        return Ok(new
        {
            token = jwtToken,
            refreshToken = user.RefreshToken
        });
    }
    
    [HttpPost("register")]
    public IActionResult Register([FromBody] DTOs.RegisterRequest request)
    {
        if (_context.Users.Any(u => u.Email == request.Email))
            return BadRequest("User with this email already exists!");

        var user = new Models.User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return Ok(new { message = "Registered successfully!" });
    }

    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] DTOs.RefreshTokenRequest request)
    {
        var user = _context.Users.SingleOrDefault(u => u.RefreshToken == request.RefreshToken);

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Unauthorized("Refresh token is not valid or expired!");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:ExpiryMinutes")),
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);
        
        _context.SaveChanges();

        return Ok(new
        {
            token = jwtToken,
            refreshToken = user.RefreshToken
        });
    }

}