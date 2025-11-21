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
