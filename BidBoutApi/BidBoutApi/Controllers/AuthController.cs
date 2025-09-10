using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BidBoutApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace BidBoutApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(MyDbContext context, IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] DTOs.LoginRequest request)
    {
        var user = context.Users.SingleOrDefault(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return BadRequest("Wrong email or password!");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("JwtSettings:ExpiryMinutes")),
            Issuer = configuration["JwtSettings:Issuer"],
            Audience = configuration["JwtSettings:Audience"],
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var accessToken = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(accessToken);

        var refreshTokenValue = Guid.NewGuid().ToString();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(configuration.GetValue<int>("JwtSettings:ExpiryDays"));

        var refreshToken = new Models.RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = refreshTokenExpiry
        };

        context.RefreshTokens.Add(refreshToken);
        context.SaveChanges();

        Response.Cookies.Append("refreshToken", refreshTokenValue, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshTokenExpiry
        });

        return Ok(new
        {
            token = jwtToken
        });
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] DTOs.RegisterRequest request)
    {
        if (context.Users.Any(u => u.Email == request.Email))
            return Conflict("User with this email already exists!");

        var user = new Models.User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.SaveChanges();

        return Ok(new { message = "Registered successfully!" });
    }

    [HttpPost("refresh")]
    public IActionResult Refresh()
    {
        var refreshTokenValue = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshTokenValue))
            return Unauthorized("Refresh token missing!");

        var refreshToken = context.RefreshTokens.SingleOrDefault(rt => rt.Token == refreshTokenValue);
        if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow)
            return Unauthorized("Refresh token invalid or expired!");

        var user = context.Users.SingleOrDefault(u => u.Id == refreshToken.UserId);
        if (user == null)
            return Unauthorized("User not found!");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("JwtSettings:ExpiryMinutes")),
            Issuer = configuration["JwtSettings:Issuer"],
            Audience = configuration["JwtSettings:Audience"],
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var newAccessToken = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(newAccessToken);

        refreshToken.Token = Guid.NewGuid().ToString();
        refreshToken.ExpiresAt = DateTime.UtcNow.AddDays(configuration.GetValue<int>("JwtSettings:ExpiryDays"));
        refreshToken.UpdatedAt = DateTime.UtcNow;

        context.SaveChanges();

        Response.Cookies.Append("refreshToken", refreshToken.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // локально
            SameSite = SameSiteMode.Strict,
            Expires = refreshToken.ExpiresAt
        });

        return Ok(new { token = jwtToken, user = new { user.Id, user.Email } });
    }
}