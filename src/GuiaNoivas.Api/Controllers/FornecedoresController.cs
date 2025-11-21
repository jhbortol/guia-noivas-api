using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Models;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/fornecedores")]
[Authorize]
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

        var query = _db.Fornecedores
            .AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.Medias)
            .OrderByDescending(f => f.Destaque)
            .ThenByDescending(f => f.Rating);

        var total = await query.CountAsync();

        var data = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(f => new GuiaNoivas.Api.Dtos.FornecedorListDto(
                f.Id,
                f.Nome,
                f.Slug,
                f.Descricao,
                f.Cidade,
                f.Rating,
                f.Destaque,
                f.SeloFornecedor,
                f.Categoria == null ? null : new GuiaNoivas.Api.Dtos.CategoriaDto(f.Categoria.Id, f.Categoria.Nome, f.Categoria.Slug),
                f.Medias.OrderByDescending(m => m.IsPrimary).ThenByDescending(m => m.CreatedAt).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary)).FirstOrDefault()
            ))
            .ToListAsync();

        return Ok(new { data, meta = new { total, page, pageSize } });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _db.Fornecedores
            .AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.Medias)
            .Where(f => f.Id == id)
            .Select(f => new GuiaNoivas.Api.Dtos.FornecedorDetailDto(
                f.Id,
                f.Nome,
                f.Slug,
                f.Descricao,
                f.Cidade,
                f.Telefone,
                f.Email,
                f.Website,
                f.Destaque,
                f.SeloFornecedor,
                f.Rating,
                f.Visitas,
                f.CreatedAt,
                f.UpdatedAt,
                f.Medias.OrderByDescending(m => m.IsPrimary).ThenByDescending(m => m.CreatedAt).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary)),
                f.Categoria == null ? null : new GuiaNoivas.Api.Dtos.CategoriaDto(f.Categoria.Id, f.Categoria.Nome, f.Categoria.Slug)
            ))
            .FirstOrDefaultAsync();

        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var item = await _db.Fornecedores
            .AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.Medias)
            .Where(f => f.Slug == slug)
            .Select(f => new GuiaNoivas.Api.Dtos.FornecedorDetailDto(
                f.Id,
                f.Nome,
                f.Slug,
                f.Descricao,
                f.Cidade,
                f.Telefone,
                f.Email,
                f.Website,
                f.Destaque,
                f.SeloFornecedor,
                f.Rating,
                f.Visitas,
                f.CreatedAt,
                f.UpdatedAt,
                f.Medias.OrderByDescending(m => m.IsPrimary).ThenByDescending(m => m.CreatedAt).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary)),
                f.Categoria == null ? null : new GuiaNoivas.Api.Dtos.CategoriaDto(f.Categoria.Id, f.Categoria.Nome, f.Categoria.Slug)
            ))
            .FirstOrDefaultAsync();

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
