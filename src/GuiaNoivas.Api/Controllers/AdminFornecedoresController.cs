using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GuiaNoivas.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
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
    public async Task<IActionResult> Create([FromBody] CreateFornecedorDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // generate slug if not provided
        var slug = string.IsNullOrWhiteSpace(dto.Slug) ? GenerateSlug(dto.Nome) : dto.Slug.Trim();

        // ensure slug uniqueness
        var exists = await _db.Fornecedores.AnyAsync(f => f.Slug == slug);
        if (exists) return Conflict(new ProblemDetails { Title = "Slug already in use", Detail = "A fornecedor with this slug already exists.", Status = StatusCodes.Status409Conflict });

        if (dto.CategoriaId != null)
        {
            var catExists = await _db.Categorias.AnyAsync(c => c.Id == dto.CategoriaId);
            if (!catExists) return BadRequest(new ProblemDetails { Title = "CategoriaId does not exist", Status = StatusCodes.Status400BadRequest });
        }

        var entity = new Fornecedor
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            Slug = slug,
            Descricao = dto.Descricao,
            Cidade = dto.Cidade,
            Telefone = dto.Telefone,
            Email = dto.Email,
            Website = dto.Website,
            Instagram = dto.Instagram,
            Destaque = dto.Destaque,
            SeloFornecedor = dto.SeloFornecedor,
            Rating = dto.Rating,
            CategoriaId = dto.CategoriaId,
            Ativo = dto.Ativo ?? true,
            Visitas = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Fornecedores.Add(entity);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFornecedorDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var existing = await _db.Fornecedores.FindAsync(id);
        if (existing == null) return NotFound();

        // If slug changed, ensure uniqueness
        if (!string.IsNullOrWhiteSpace(dto.Slug) && dto.Slug.Trim() != existing.Slug)
        {
            var slugExists = await _db.Fornecedores.AnyAsync(f => f.Slug == dto.Slug.Trim() && f.Id != id);
            if (slugExists) return Conflict(new ProblemDetails { Title = "Slug already in use", Detail = "A fornecedor with this slug already exists.", Status = StatusCodes.Status409Conflict });
            existing.Slug = dto.Slug.Trim();
        }

        // Map other fields
        existing.Nome = dto.Nome ?? existing.Nome;
        existing.Descricao = dto.Descricao;
        existing.Cidade = dto.Cidade;
        existing.Telefone = dto.Telefone;
        existing.Email = dto.Email;
        existing.Website = dto.Website;
        existing.Instagram = dto.Instagram;
        existing.Destaque = dto.Destaque;
        existing.SeloFornecedor = dto.SeloFornecedor;
        existing.Rating = dto.Rating;
        if (dto.Ativo.HasValue)
        {
            existing.Ativo = dto.Ativo.Value;
        }
        // CategoriaId update
        if (dto.CategoriaId != null)
        {
            var catExists = await _db.Categorias.AnyAsync(c => c.Id == dto.CategoriaId);
            if (!catExists) return BadRequest(new ProblemDetails { Title = "CategoriaId does not exist", Status = StatusCodes.Status400BadRequest });
        }
        existing.CategoriaId = dto.CategoriaId;
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

    private static string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Guid.NewGuid().ToString("N");
        var s = input.ToLowerInvariant().Trim();
        // remove invalid chars
        var chars = s.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-') .ToArray();
        var cleaned = new string(chars);
        var slug = string.Join('-', cleaned.Split(new[] { ' ' , '-'}, StringSplitOptions.RemoveEmptyEntries));
        return slug;
    }
}

public record DestaqueDto(bool Destaque);

public class CreateFornecedorDto
{
    [Required, MaxLength(200)]
    public string Nome { get; set; } = null!;

    [MaxLength(200)]
    public string? Slug { get; set; }

    [MaxLength(4000)]
    public string? Descricao { get; set; }

    [MaxLength(100)]
    public string? Cidade { get; set; }

    [MaxLength(50)]
    public string? Telefone { get; set; }

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    [Url, MaxLength(250)]
    public string? Website { get; set; }

    [MaxLength(250)]
    public string? Instagram { get; set; }

    public bool Destaque { get; set; }

    public bool SeloFornecedor { get; set; }

    [Range(0, 5)]
    public decimal? Rating { get; set; }
    public Guid? CategoriaId { get; set; }
    public bool? Ativo { get; set; }
}

public class UpdateFornecedorDto
{
    [MaxLength(200)]
    public string? Nome { get; set; }

    [MaxLength(200)]
    public string? Slug { get; set; }

    [MaxLength(4000)]
    public string? Descricao { get; set; }

    [MaxLength(100)]
    public string? Cidade { get; set; }

    [MaxLength(50)]
    public string? Telefone { get; set; }

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    [Url, MaxLength(250)]
    public string? Website { get; set; }

    [MaxLength(250)]
    public string? Instagram { get; set; }

    public bool Destaque { get; set; }

    public bool SeloFornecedor { get; set; }

    [Range(0, 5)]
    public decimal? Rating { get; set; }
    public Guid? CategoriaId { get; set; }
    public bool? Ativo { get; set; }
}


