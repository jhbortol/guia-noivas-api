using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GuiaNoivas.Api.Data;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminTestemunhosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminTestemunhosController> _logger;

    public AdminTestemunhosController(AppDbContext context, ILogger<AdminTestemunhosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Remove um testemunho (Admin apenas)
    /// </summary>
    /// <param name="id">ID do testemunho</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var testemunho = await _context.Testemunhos.FindAsync(id);
        
        if (testemunho == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Testemunho não encontrado",
                Detail = $"Testemunho com ID {id} não foi encontrado.",
                Status = 404
            });
        }

        _context.Testemunhos.Remove(testemunho);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Testemunho {TestemunhoId} removido por admin", id);

        return NoContent();
    }

    /// <summary>
    /// Lista todos os testemunhos (Admin apenas) com filtros
    /// </summary>
    /// <param name="page">Número da página (1-based)</param>
    /// <param name="pageSize">Tamanho da página (máximo 100)</param>
    /// <param name="fornecedorId">Filtrar por fornecedor (opcional)</param>
    [HttpGet]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? fornecedorId = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _context.Testemunhos
            .Include(t => t.Fornecedor)
            .AsQueryable();

        if (fornecedorId.HasValue)
        {
            query = query.Where(t => t.FornecedorId == fornecedorId.Value);
        }

        var total = await query.CountAsync();
        var testemunhos = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.Nome,
                t.Descricao,
                t.FornecedorId,
                FornecedorNome = t.Fornecedor != null ? t.Fornecedor.Nome : "",
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            data = testemunhos,
            meta = new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }
}
