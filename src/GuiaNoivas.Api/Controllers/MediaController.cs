using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

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


    [HttpPost("upload")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Upload([FromForm] MediaUploadDto dto)
    {
        if (dto.File == null || dto.File.Length == 0)
            return BadRequest(new { message = "Arquivo não enviado." });

        // FornecedorId is optional: allow uploading media not tied to a Fornecedor (e.g., category or generic media).
        // When provided, controller will persist the association; other operations that require FornecedorId (like marking primary)
        // still validate its presence.

        // Salvar arquivo no storage (exemplo: local ou Azure Blob)
        string publicUrl;
        string filename = dto.Filename ?? dto.File.FileName;
        if (_blobService != null)
        {
            var blobName = $"media/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}_{filename}";
            using var stream = dto.File.OpenReadStream();
            publicUrl = await _blobService.UploadAsync(blobName, stream, dto.ContentType ?? dto.File.ContentType);
        }
        else
        {
            // fallback: salvar localmente em wwwroot/uploads
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploads);
            var filePath = Path.Combine(uploads, filename);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }
            publicUrl = $"/uploads/{filename}";
        }

        // Persistir metadados
        var db = HttpContext.RequestServices.GetRequiredService<GuiaNoivas.Api.Data.AppDbContext>();

        // If associating to a Categoria, unset previous association to guarantee one-to-one
        if (dto.CategoriaId != null)
        {
            var existing = await db.Media.Where(m => m.CategoriaId == dto.CategoriaId).ToListAsync();
            if (existing.Any())
            {
                foreach (var e in existing)
                {
                    e.CategoriaId = null;
                    db.Media.Update(e);
                }
                await db.SaveChangesAsync();
            }
        }

        var media = new GuiaNoivas.Api.Models.Media
        {
            Id = Guid.NewGuid(),
            FornecedorId = dto.FornecedorId,
            CategoriaId = dto.CategoriaId,
            Url = publicUrl,
            Filename = filename,
            ContentType = dto.ContentType ?? dto.File.ContentType,
            Width = dto.Width,
            Height = dto.Height,
            IsPrimary = dto.IsPrimary,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Media.Add(media);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = media.Id }, media);
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
            CategoriaId = dto.CategoriaId,
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
            await db.Media
                .Where(m => m.FornecedorId == fornecedorId && m.Id != id && m.IsPrimary)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsPrimary, m => false));

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

public record CreateMediaDto(Guid? FornecedorId, Guid? CategoriaId, string? Url, string? Filename, string? ContentType, int? Width, int? Height, bool IsPrimary);

public class MediaUploadDto
{
    public Guid? FornecedorId { get; set; }
    public string? Filename { get; set; }
    public string? ContentType { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool IsPrimary { get; set; }
    public IFormFile? File { get; set; }
    public Guid? CategoriaId { get; set; }
}
