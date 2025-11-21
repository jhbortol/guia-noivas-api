using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        // Demo placeholder: replace with real user validation
        if (dto.Email == "admin@local" && dto.Password == "Password123!")
        {
            var token = GenerateToken(dto.Email, "Admin");
            return Ok(new { accessToken = token, expiresIn = 3600, user = new { id = Guid.NewGuid(), email = dto.Email, roles = new[] { "Admin" } } });
        }

        return Unauthorized(new { message = "Invalid credentials" });
    }

    private string GenerateToken(string email, string role)
    {
        var secret = _config["Jwt:Secret"] ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? "please-change-this-secret";
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[] { new Claim(ClaimTypes.Name, email), new Claim(ClaimTypes.Role, role) };

        var token = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record LoginDto(string Email, string Password);
