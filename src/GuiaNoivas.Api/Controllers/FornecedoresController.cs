using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Models;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/fornecedores")]
[Authorize]
public class FornecedoresController : ControllerBase
{
    /// <summary>
    /// Lista todos os fornecedores sem filtro de categoria.
    /// </summary>
    [HttpGet("all")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var fornecedores = await _db.Fornecedores
            .AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.Medias)
            .OrderByDescending(f => f.Destaque)
            .Select(f => new GuiaNoivas.Api.Dtos.FornecedorListDto(
                f.Id,
                f.Nome,
                f.Slug,
                f.Descricao,
                f.Cidade,
                f.Rating,
                f.Destaque,
                f.SeloFornecedor,
                f.Ativo,
                f.Categoria == null ? null : new GuiaNoivas.Api.Dtos.CategoriaDto(f.Categoria.Id, f.Categoria.Nome, f.Categoria.Slug),
                f.Medias.OrderByDescending(m => m.IsPrimary).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary)).FirstOrDefault(),
                f.Medias.OrderByDescending(m => m.IsPrimary).ThenByDescending(m => m.CreatedAt).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary))
            ))
            .ToListAsync();
        return Ok(fornecedores);
    }

    /// <summary>
    /// Lista todos os fornecedores ativos.
    /// </summary>
    [HttpGet("ativos")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAtivos()
    {
        var fornecedores = await _db.Fornecedores
            .AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.Medias)
            .Where(f => f.Ativo)
            .OrderByDescending(f => f.Destaque)
            .Select(f => new GuiaNoivas.Api.Dtos.FornecedorListDto(
                f.Id,
                f.Nome,
                f.Slug,
                f.Descricao,
                f.Cidade,
                f.Rating,
                f.Destaque,
                f.SeloFornecedor,
                f.Ativo,
                f.Categoria == null ? null : new GuiaNoivas.Api.Dtos.CategoriaDto(f.Categoria.Id, f.Categoria.Nome, f.Categoria.Slug),
                f.Medias.OrderByDescending(m => m.IsPrimary).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary)).FirstOrDefault(),
                f.Medias.OrderByDescending(m => m.IsPrimary).ThenByDescending(m => m.CreatedAt).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary))
            ))
            .ToListAsync();
        return Ok(fornecedores);
    }
    
    private readonly AppDbContext _db;

    public FornecedoresController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    /// <summary>
    /// Create a new fornecedor. Requires Admin role.
    /// </summary>
    /// <remarks>
    /// Example:
    /// {
    ///   "nome": "Fornecedor X",
    ///   "slug": "fornecedor-x",
    ///   "categoriaId": "00000000-0000-0000-0000-000000000000"
    /// }
    /// </remarks>
    [ProducesResponseType(typeof(object), 201)]
    [ProducesResponseType(409)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] GuiaNoivas.Api.Dtos.FornecedorCreateDto dto)
    {
        var exists = await _db.Fornecedores.AnyAsync(f => f.Slug == dto.Slug);
        if (exists) return Conflict(new { message = "Slug already in use" });

        if (dto.CategoriaId != null)
        {
            var catExists = await _db.Categorias.AnyAsync(c => c.Id == dto.CategoriaId);
            if (!catExists) return BadRequest(new { message = "CategoriaId does not exist" });
        }

        var f = new Fornecedor
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            Slug = dto.Slug,
            Descricao = dto.Descricao,
            Cidade = dto.Cidade,
            Telefone = dto.Telefone,
            Email = dto.Email,
            Website = dto.Website,
            Destaque = dto.Destaque,
            SeloFornecedor = dto.SeloFornecedor,
            Rating = dto.Rating,
            CategoriaId = dto.CategoriaId,
            Ativo = dto.Ativo,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Fornecedores.Add(f);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = f.Id }, new { f.Id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    /// <summary>
    /// Update an existing fornecedor. Requires Admin role.
    /// </summary>
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Update(Guid id, [FromBody] GuiaNoivas.Api.Dtos.FornecedorUpdateDto dto)
    {
        var f = await _db.Fornecedores.FindAsync(id);
        if (f == null) return NotFound();

        if (dto.CategoriaId != null)
        {
            var catExists = await _db.Categorias.AnyAsync(c => c.Id == dto.CategoriaId);
            if (!catExists) return BadRequest(new { message = "CategoriaId does not exist" });
        }

        f.Nome = dto.Nome;
        f.Slug = dto.Slug;
        f.Descricao = dto.Descricao;
        f.Cidade = dto.Cidade;
        f.Telefone = dto.Telefone;
        f.Email = dto.Email;
        f.Website = dto.Website;
        f.Destaque = dto.Destaque;
        f.SeloFornecedor = dto.SeloFornecedor;
        f.Rating = dto.Rating;
        f.CategoriaId = dto.CategoriaId;
        f.Ativo = dto.Ativo;
        f.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Fornecedores.Update(f);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet]
    /// <summary>
    /// List fornecedores with optional filter by categoriaId.
    /// </summary>
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 12, [FromQuery] Guid? categoriaId = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 12;

        IQueryable<Fornecedor> baseQuery = _db.Fornecedores
            .AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.Medias);

        if (categoriaId != null)
        {
            baseQuery = baseQuery.Where(f => f.CategoriaId == categoriaId);
        }

        // Count should be done on the base (unordered) query to avoid translation issues on some providers
        var total = await baseQuery.CountAsync();

        // Apply provider-specific ordering only for the paged data query
        IQueryable<Fornecedor> orderedQuery;
        if (_db.Database.ProviderName != null && _db.Database.ProviderName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            orderedQuery = baseQuery.OrderByDescending(f => f.Destaque);
        }
        else
        {
            orderedQuery = baseQuery.OrderByDescending(f => f.Destaque).ThenByDescending(f => f.Rating);
        }

        List<GuiaNoivas.Api.Dtos.FornecedorListDto> data;
        if (_db.Database.ProviderName != null && _db.Database.ProviderName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            data = await orderedQuery.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(f => new GuiaNoivas.Api.Dtos.FornecedorListDto(
                    f.Id,
                    f.Nome,
                    f.Slug,
                    f.Descricao,
                    f.Cidade,
                    f.Rating,
                    f.Destaque,
                    f.SeloFornecedor,
                    f.Ativo,
                    f.Categoria == null ? null : new GuiaNoivas.Api.Dtos.CategoriaDto(f.Categoria.Id, f.Categoria.Nome, f.Categoria.Slug),
                    f.Medias.OrderByDescending(m => m.IsPrimary).ThenByDescending(m => m.CreatedAt).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary)).FirstOrDefault(),
                    f.Medias.OrderByDescending(m => m.IsPrimary).ThenByDescending(m => m.CreatedAt).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary))
                ))
                .ToListAsync();
        }
        else
        {
            data = await orderedQuery.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(f => new GuiaNoivas.Api.Dtos.FornecedorListDto(
                    f.Id,
                    f.Nome,
                    f.Slug,
                    f.Descricao,
                    f.Cidade,
                    f.Rating,
                    f.Destaque,
                    f.SeloFornecedor,
                    f.Ativo,
                    f.Categoria == null ? null : new GuiaNoivas.Api.Dtos.CategoriaDto(f.Categoria.Id, f.Categoria.Nome, f.Categoria.Slug),
                    f.Medias.OrderByDescending(m => m.IsPrimary).ThenByDescending(m => m.CreatedAt).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary)).FirstOrDefault()
                ))
                .ToListAsync();
        }

        return Ok(new { data, meta = new { total, page, pageSize } });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        GuiaNoivas.Api.Dtos.FornecedorDetailDto? item;
        if (_db.Database.ProviderName != null && _db.Database.ProviderName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            item = await _db.Fornecedores
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
                    f.Ativo,
                    f.Rating,
                    f.Visitas,
                    f.CreatedAt,
                    f.UpdatedAt,
                    f.Medias.OrderByDescending(m => m.IsPrimary).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary)),
                    f.Categoria == null ? null : new GuiaNoivas.Api.Dtos.CategoriaDto(f.Categoria.Id, f.Categoria.Nome, f.Categoria.Slug)
                    f.Medias.OrderByDescending(m => m.IsPrimary).ThenByDescending(m => m.CreatedAt).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary)),
                .FirstOrDefaultAsync();
        }
        else
        {
            item = await _db.Fornecedores
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
                    f.Ativo,
                    f.Rating,
                    f.Visitas,
                    f.CreatedAt,
                    f.UpdatedAt,
                    f.Medias.OrderByDescending(m => m.IsPrimary).ThenByDescending(m => m.CreatedAt).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary)),
                    f.Categoria == null ? null : new GuiaNoivas.Api.Dtos.CategoriaDto(f.Categoria.Id, f.Categoria.Nome, f.Categoria.Slug)
                ))
                .FirstOrDefaultAsync();
        }

        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        GuiaNoivas.Api.Dtos.FornecedorDetailDto? itemBySlug;
        if (_db.Database.ProviderName != null && _db.Database.ProviderName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            itemBySlug = await _db.Fornecedores
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
                    f.Ativo,
                    f.Rating,
                    f.Visitas,
                    f.CreatedAt,
                    f.UpdatedAt,
                    f.Medias.OrderByDescending(m => m.IsPrimary).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary)),
                    f.Categoria == null ? null : new GuiaNoivas.Api.Dtos.CategoriaDto(f.Categoria.Id, f.Categoria.Nome, f.Categoria.Slug)
                ))
                .FirstOrDefaultAsync();
        }
        else
        {
            itemBySlug = await _db.Fornecedores
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
                    f.Ativo,
                    f.Rating,
                    f.Visitas,
                    f.CreatedAt,
                    f.UpdatedAt,
                    f.Medias.OrderByDescending(m => m.IsPrimary).ThenByDescending(m => m.CreatedAt).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary)),
                    f.Categoria == null ? null : new GuiaNoivas.Api.Dtos.CategoriaDto(f.Categoria.Id, f.Categoria.Nome, f.Categoria.Slug)
                ))
                .FirstOrDefaultAsync();
        }

        if (itemBySlug == null) return NotFound();
        return Ok(itemBySlug);
    }

    /// <summary>
    /// Search fornecedores by name (contains) with pagination.
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string nome, [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        if (string.IsNullOrWhiteSpace(nome)) return BadRequest(new { message = "Nome is required" });

        IQueryable<Fornecedor> baseQuery = _db.Fornecedores
            .AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.Medias)
            .Where(f => EF.Functions.Like(f.Nome, $"%{nome}%"));

        var total = await baseQuery.CountAsync();

        IQueryable<Fornecedor> orderedQuery;
        if (_db.Database.ProviderName != null && _db.Database.ProviderName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            orderedQuery = baseQuery.OrderByDescending(f => f.Destaque);
        }
        else
        {
            orderedQuery = baseQuery.OrderByDescending(f => f.Destaque).ThenByDescending(f => f.Rating);
        }

        var data = await orderedQuery.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(f => new GuiaNoivas.Api.Dtos.FornecedorListDto(
                f.Id,
                f.Nome,
                f.Slug,
                f.Descricao,
                f.Cidade,
                f.Rating,
                f.Destaque,
                f.SeloFornecedor,
                f.Ativo,
                f.Categoria == null ? null : new GuiaNoivas.Api.Dtos.CategoriaDto(f.Categoria.Id, f.Categoria.Nome, f.Categoria.Slug),
                f.Medias.OrderByDescending(m => m.IsPrimary).ThenByDescending(m => m.CreatedAt).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary)).FirstOrDefault(),
                f.Medias.OrderByDescending(m => m.IsPrimary).ThenByDescending(m => m.CreatedAt).Select(m => new GuiaNoivas.Api.Dtos.MediaDto(m.Id, m.Url, m.Filename, m.ContentType, m.IsPrimary))
            ))
            .ToListAsync();

        return Ok(new { data, meta = new { total, page, pageSize } });
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

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var f = await _db.Fornecedores.FindAsync(id);
        if (f == null) return NotFound();

        _db.Fornecedores.Remove(f);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record ContactDto(string Nome, string Email, string? Telefone, string Mensagem);
