using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Models;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/admin/fornecedores")]
[Authorize(Roles = "Admin")]
public class AdminFornecedoresController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminFornecedoresController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Fornecedor dto)
    {
        dto.Id = Guid.NewGuid();
        dto.CreatedAt = DateTimeOffset.UtcNow;
        _db.Fornecedores.Add(dto);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Fornecedor dto)
    {
        var existing = await _db.Fornecedores.FindAsync(id);
        if (existing == null) return NotFound();

        // Simple update - map fields
        existing.Nome = dto.Nome;
        existing.Descricao = dto.Descricao;
        existing.Cidade = dto.Cidade;
        existing.Telefone = dto.Telefone;
        existing.Email = dto.Email;
        existing.Website = dto.Website;
        existing.Destaque = dto.Destaque;
        existing.SeloFornecedor = dto.SeloFornecedor;
        existing.Rating = dto.Rating;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Fornecedores.Update(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _db.Fornecedores.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPatch("{id:guid}/destaque")]
    public async Task<IActionResult> PatchDestaque(Guid id, [FromBody] DestaqueDto dto)
    {
        var existing = await _db.Fornecedores.FindAsync(id);
        if (existing == null) return NotFound();
        existing.Destaque = dto.Destaque;
        _db.Fornecedores.Update(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _db.Fornecedores.FindAsync(id);
        if (existing == null) return NotFound();
        _db.Fornecedores.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record DestaqueDto(bool Destaque);
