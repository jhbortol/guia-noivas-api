using Microsoft.AspNetCore.Mvc;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/institucional")]
public class InstitucionalController : ControllerBase
{
    private readonly AppDbContext _db;

    public InstitucionalController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        var item = await _db.InstitucionalContents.FindAsync(key);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPut("{key}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string key, [FromBody] InstitucionalDto dto)
    {
        var existing = await _db.InstitucionalContents.FindAsync(key);
        if (existing == null)
        {
            existing = new InstitucionalContent { Key = key, Title = dto.Title, ContentHtml = dto.ContentHtml, Version = 1, UpdatedAt = DateTimeOffset.UtcNow };
            _db.InstitucionalContents.Add(existing);
        }
        else
        {
            existing.Title = dto.Title;
            existing.ContentHtml = dto.ContentHtml;
            existing.Version += 1;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            _db.InstitucionalContents.Update(existing);
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record InstitucionalDto(string? Title, string? ContentHtml);
