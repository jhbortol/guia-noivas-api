using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;

    public AuthController(IConfiguration config, AppDbContext db)
    {
        _config = config;
        _db = db;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        var user = _db.Usuarios.FirstOrDefault(u => u.Email == dto.Email);
        if (user == null) return Unauthorized(new { message = "Invalid credentials" });

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        var roles = (user.Roles ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
        var accessToken = GenerateToken(user.Email, roles);
        var refreshToken = CreateRefreshTokenForUser(user.Id);

        return Ok(new { accessToken, refreshToken = refreshToken.Token, expiresIn = 3600, user = new { id = user.Id, email = user.Email, roles } });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (await _db.Usuarios.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Email already registered" });

        var user = new Usuario
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            DisplayName = dto.DisplayName,
            Roles = dto.Role ?? "User",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Register), new { id = user.Id }, new { id = user.Id, email = user.Email, displayName = user.DisplayName });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
    {
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == dto.RefreshToken && !r.Revoked && r.ExpiresAt > DateTimeOffset.UtcNow);
        if (existing == null) return Unauthorized(new { message = "Invalid refresh token" });

        var user = await _db.Usuarios.FindAsync(existing.UsuarioId);
        if (user == null) return Unauthorized(new { message = "Invalid refresh token" });

        // Revoke used token
        existing.Revoked = true;
        _db.RefreshTokens.Update(existing);

        var roles = (user.Roles ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
        var accessToken = GenerateToken(user.Email, roles);
        var newRefresh = CreateRefreshTokenForUser(user.Id);
        await _db.SaveChangesAsync();

        return Ok(new { accessToken, refreshToken = newRefresh.Token, expiresIn = 3600 });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutDto dto)
    {
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);
        if (existing != null)
        {
            existing.Revoked = true;
            _db.RefreshTokens.Update(existing);
            await _db.SaveChangesAsync();
        }

        return NoContent();
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

    private string GenerateToken(string email, string[] roles)
    {
        var secret = _config["Jwt:Secret"] ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? "please-change-this-secret";
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim> { new Claim(ClaimTypes.Name, email) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken CreateRefreshTokenForUser(Guid userId)
    {
        var rt = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = userId,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            Revoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.RefreshTokens.Add(rt);
        _db.SaveChanges();
        return rt;
    }
}

public record LoginDto(string Email, string Password);

public record RegisterDto(string Email, string Password, string? DisplayName, string? Role);

public record RefreshDto(string RefreshToken);

public record LogoutDto(string RefreshToken);
