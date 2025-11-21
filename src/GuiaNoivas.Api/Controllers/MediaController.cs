using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/media")]
public class MediaController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly GuiaNoivas.Api.Services.IBlobService? _blobService;

    public MediaController(IConfiguration config, GuiaNoivas.Api.Services.IBlobService? blobService = null)
    {
        _config = config;
        _blobService = blobService;
    }

    [HttpPost("presign")]
    [Authorize]
    public IActionResult Presign([FromBody] PresignDto dto)
    {
        // If Blob service is registered (Storage configured), generate SAS URL.
        if (_blobService != null)
        {
            var fileName = dto.Filename ?? Guid.NewGuid().ToString();
            var safeName = fileName.Replace(" ", "_");
            var blobName = $"{Guid.NewGuid():N}_{safeName}";
            try
            {
                var result = _blobService.GetUploadSasUriAsync(blobName, TimeSpan.FromMinutes(15), dto.ContentType).GetAwaiter().GetResult();
                return Ok(new { uploadUrl = result.Url.ToString(), blobName = result.BlobName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate SAS URL.", details = ex.Message });
            }
        }

        // fallback: return a local upload URL (client should POST multipart to /uploads)
        var fallbackFile = dto.Filename ?? Guid.NewGuid().ToString();
        var publicUrl = $"/uploads/{fallbackFile}";
        return Ok(new { uploadUrl = publicUrl, publicUrl, blobName = fallbackFile });
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

    [HttpPost("{id:guid}/mark-primary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MarkPrimary(Guid id)
    {
        var db = HttpContext.RequestServices.GetRequiredService<GuiaNoivas.Api.Data.AppDbContext>();
        var media = await db.Media.FindAsync(id);
        if (media == null) return NotFound();
        if (media.FornecedorId == null) return BadRequest(new { message = "Media has no FornecedorId" });

        var fornecedorId = media.FornecedorId.Value;

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // unset other primary flags for this fornecedor
            var others = db.Media.Where(m => m.FornecedorId == fornecedorId && m.Id != id && m.IsPrimary);
            await others.ForEachAsync(m => m.IsPrimary = false);

            media.IsPrimary = true;
            db.Media.Update(media);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { error = "Failed to mark primary", details = ex.Message });
        }
    }
}

public record CreateMediaDto(Guid? FornecedorId, string? Url, string? Filename, string? ContentType, int? Width, int? Height, bool IsPrimary);

public record PresignDto(string Filename, string ContentType, Guid? FornecedorId);
