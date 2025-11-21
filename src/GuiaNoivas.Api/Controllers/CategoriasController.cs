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

    public CategoriasController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;

        var query = _db.Categorias.AsNoTracking().OrderBy(c => c.Order).ThenBy(c => c.Nome);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new CategoriaListDto(c.Id, c.Nome, c.Slug, c.Descricao))
            .ToListAsync();

        return Ok(new { data = items, meta = new { total, page, pageSize } });
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var c = await _db.Categorias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        return Ok(new CategoriaDetailDto(c.Id, c.Nome, c.Slug, c.Descricao, c.Order, c.CreatedAt, c.UpdatedAt));
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

        return CreatedAtAction(nameof(GetById), new { id = c.Id }, new { c.Id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CategoriaUpdateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var c = await _db.Categorias.FindAsync(id);
        if (c == null) return NotFound();

        // ensure slug uniqueness
        var slugInUse = await _db.Categorias.AnyAsync(x => x.Slug == dto.Slug && x.Id != id);
        if (slugInUse) return Conflict(new { message = "Slug already in use" });

        c.Nome = dto.Nome;
        c.Slug = dto.Slug;
        c.Descricao = dto.Descricao;
        c.Order = dto.Order;
        c.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Categorias.Update(c);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var c = await _db.Categorias.FindAsync(id);
        if (c == null) return NotFound();

        _db.Categorias.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
using Microsoft.AspNetCore.Mvc;
using GuiaNoivas.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/categorias")]
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriasController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var items = await _db.Categorias.AsNoTracking().OrderBy(c => c.Order).ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _db.Categorias.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }
}
