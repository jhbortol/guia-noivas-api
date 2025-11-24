
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Models;
using GuiaNoivas.Api.Dtos;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/categorias")]
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly GuiaNoivas.Api.Services.IBlobService? _blobService;

    public CategoriasController(AppDbContext db, GuiaNoivas.Api.Services.IBlobService? blobService = null)
    {
        _db = db;
        _blobService = blobService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;

        var query = _db.Categorias
            .Include(c => c.Media)
            .AsNoTracking()
            .OrderBy(c => c.Order).ThenBy(c => c.Nome);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new CategoriaListDto(c.Id, c.Nome, c.Slug, c.Descricao, c.Media != null ? c.Media.Id : (Guid?)null, c.Media != null ? c.Media.Url : null))
            .ToListAsync();

        return Ok(new { data = items, meta = new { total, page, pageSize } });
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var c = await _db.Categorias.Include(ca => ca.Media).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        return Ok(new CategoriaDetailDto(c.Id, c.Nome, c.Slug, c.Descricao, c.Order, c.CreatedAt, c.UpdatedAt, c.Media != null ? c.Media.Id : (Guid?)null, c.Media != null ? c.Media.Url : null));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CategoriaCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var exists = await _db.Categorias.AnyAsync(x => x.Slug == dto.Slug);
        if (exists) return Conflict(new { message = "Slug already in use" });

        var c = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            Slug = dto.Slug,
            Descricao = dto.Descricao,
            Order = dto.Order,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Categorias.Add(c);
        await _db.SaveChangesAsync();

        // If image content is provided, upload it and create media associated to category
        if (!string.IsNullOrWhiteSpace(dto.ImageBase64))
        {
            try
            {
                var bytes = Convert.FromBase64String(dto.ImageBase64);
                using var ms = new System.IO.MemoryStream(bytes);
                var filename = dto.ImageFilename ?? $"image_{Guid.NewGuid():N}";
                string publicUrl;
                var contentType = dto.ImageContentType ?? "application/octet-stream";
                if (_blobService != null)
                {
                    var blobName = $"media/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}_{filename}";
                    publicUrl = await _blobService.UploadAsync(blobName, ms, contentType);
                }
                else
                {
                    // fallback: save locally
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    Directory.CreateDirectory(uploads);
                    var filePath = Path.Combine(uploads, filename);
                    await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                    publicUrl = $"/uploads/{filename}";
                }

                var media = new GuiaNoivas.Api.Models.Media
                {
                    Id = Guid.NewGuid(),
                    CategoriaId = c.Id,
                    Url = publicUrl,
                    Filename = dto.ImageFilename ?? filename,
                    ContentType = contentType,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _db.Media.Add(media);
                await _db.SaveChangesAsync();
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "ImageBase64 is not a valid base64 string." });
            }
        }

        // legacy MediaId removed — prefer uploading with CategoriaId or sending ImageBase64
        return CreatedAtAction(nameof(GetById), new { id = c.Id }, new { c.Id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CategoriaUpdateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var c = await _db.Categorias.FindAsync(id);
        if (c == null) return NotFound();

        var slugInUse = await _db.Categorias.AnyAsync(x => x.Slug == dto.Slug && x.Id != id);
        if (slugInUse) return Conflict(new { message = "Slug already in use" });

        c.Nome = dto.Nome;
        c.Slug = dto.Slug;
        c.Descricao = dto.Descricao;
        c.Order = dto.Order;
        c.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Categorias.Update(c);
        await _db.SaveChangesAsync();

        // Handle image content upload if provided
        if (!string.IsNullOrWhiteSpace(dto.ImageBase64))
        {
            try
            {
                var bytes = Convert.FromBase64String(dto.ImageBase64);
                using var ms = new System.IO.MemoryStream(bytes);
                var filename = dto.ImageFilename ?? $"image_{Guid.NewGuid():N}";
                string publicUrl;
                var contentType = dto.ImageContentType ?? "application/octet-stream";
                if (_blobService != null)
                {
                    var blobName = $"media/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}_{filename}";
                    publicUrl = await _blobService.UploadAsync(blobName, ms, contentType);
                }
                else
                {
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    Directory.CreateDirectory(uploads);
                    var filePath = Path.Combine(uploads, filename);
                    await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                    publicUrl = $"/uploads/{filename}";
                }

                // unlink previous media
                var currentMedia = await _db.Media.FirstOrDefaultAsync(m => m.CategoriaId == id);
                if (currentMedia != null)
                {
                    currentMedia.CategoriaId = null;
                    _db.Media.Update(currentMedia);
                }

                var media = new GuiaNoivas.Api.Models.Media
                {
                    Id = Guid.NewGuid(),
                    CategoriaId = id,
                    Url = publicUrl,
                    Filename = dto.ImageFilename ?? filename,
                    ContentType = contentType,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _db.Media.Add(media);
                await _db.SaveChangesAsync();
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "ImageBase64 is not a valid base64 string." });
            }
        }

        // Legacy MediaId linking removed — uploads should use CategoriaId or create/update may send ImageBase64.

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var c = await _db.Categorias.FindAsync(id);
        if (c == null) return NotFound();

        // Before deleting the category, unset CategoriaId on any Media that references it.
        var medias = await _db.Media.Where(m => m.CategoriaId == id).ToListAsync();
        if (medias.Any())
        {
            foreach (var m in medias)
            {
                m.CategoriaId = null;
                _db.Media.Update(m);
            }
        }

        _db.Categorias.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
