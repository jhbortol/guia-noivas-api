using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/media")]
public class MediaController : ControllerBase
{
    private readonly IConfiguration _config;

    public MediaController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("presign")]
    [Authorize]
    public IActionResult Presign([FromBody] PresignDto dto)
    {
        // Minimal implementation: return a fake presign when Azure is not configured
        var storageConnection = _config["Azure:BlobConnectionString"]; // optional
        if (string.IsNullOrEmpty(storageConnection))
        {
            // fallback: return a local upload URL (client should POST multipart to /uploads)
            var fileName = dto.Filename ?? Guid.NewGuid().ToString();
            var publicUrl = $"/uploads/{fileName}";
            return Ok(new { uploadUrl = publicUrl, publicUrl, blobName = fileName });
        }

        // If Azure configured, real implementation would generate SAS URL here.
        return Ok(new { uploadUrl = "", publicUrl = "", blobName = "" });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateMediaDto dto)
    {
        // For simplicity, store metadata only
        var media = new GuiaNoivas.Api.Models.Media
        {
            Id = Guid.NewGuid(),
            FornecedorId = dto.FornecedorId,
            Url = dto.Url,
            Filename = dto.Filename,
            ContentType = dto.ContentType,
            Width = dto.Width,
            Height = dto.Height,
            IsPrimary = dto.IsPrimary,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Save via DbContext
        // Resolve DbContext from DI
        var db = HttpContext.RequestServices.GetRequiredService<GuiaNoivas.Api.Data.AppDbContext>();
        db.Media.Add(media);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = media.Id }, media);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var db = HttpContext.RequestServices.GetRequiredService<GuiaNoivas.Api.Data.AppDbContext>();
        var m = await db.Media.FindAsync(id);
        if (m == null) return NotFound();
        return Ok(m);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var db = HttpContext.RequestServices.GetRequiredService<GuiaNoivas.Api.Data.AppDbContext>();
        var m = await db.Media.FindAsync(id);
        if (m == null) return NotFound();
        db.Media.Remove(m);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateMediaDto(Guid? FornecedorId, string? Url, string? Filename, string? ContentType, int? Width, int? Height, bool IsPrimary);

public record PresignDto(string Filename, string ContentType, Guid? FornecedorId);
