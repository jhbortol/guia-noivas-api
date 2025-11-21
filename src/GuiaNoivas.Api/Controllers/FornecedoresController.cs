using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Models;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/fornecedores")]
public class FornecedoresController : ControllerBase
{
    private readonly AppDbContext _db;

    public FornecedoresController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 12;

        var query = _db.Fornecedores.AsNoTracking().OrderByDescending(f => f.Destaque).ThenByDescending(f => f.Rating);
        var total = await query.CountAsync();
        var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new { data, meta = new { total, page, pageSize } });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _db.Fornecedores.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var item = await _db.Fornecedores.FirstOrDefaultAsync(f => f.Slug == slug);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost("{id:guid}/visit")]
    public async Task<IActionResult> Visit(Guid id)
    {
        var item = await _db.Fornecedores.FindAsync(id);
        if (item == null) return NotFound();
        item.Visitas += 1;
        _db.Fornecedores.Update(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/contact")]
    public async Task<IActionResult> Contact(Guid id, [FromBody] ContactDto dto)
    {
        var fornecedor = await _db.Fornecedores.FindAsync(id);
        if (fornecedor == null) return NotFound();

        var submission = new GuiaNoivas.Api.Models.ContatoSubmission
        {
            Id = Guid.NewGuid(),
            FornecedorId = id,
            Nome = dto.Nome,
            Email = dto.Email,
            Telefone = dto.Telefone,
            Mensagem = dto.Mensagem,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.ContatoSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        // In production, enqueue a background job to send email
        return Accepted(new { id = submission.Id });
    }
}

public record ContactDto(string Nome, string Email, string? Telefone, string Mensagem);
