using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Dtos;
using GuiaNoivas.Api.Models;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TestemunhosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<TestemunhosController> _logger;

    public TestemunhosController(AppDbContext context, ILogger<TestemunhosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os testemunhos de um fornecedor específico
    /// </summary>
    /// <param name="fornecedorId">ID do fornecedor</param>
    /// <param name="page">Número da página (1-based)</param>
    /// <param name="pageSize">Tamanho da página (máximo 100)</param>
    [HttpGet("fornecedor/{fornecedorId}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByFornecedor(
        Guid fornecedorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        // Verificar se o fornecedor existe
        var fornecedorExists = await _context.Fornecedores.AnyAsync(f => f.Id == fornecedorId);
        if (!fornecedorExists)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Fornecedor não encontrado",
                Detail = $"Fornecedor com ID {fornecedorId} não foi encontrado.",
                Status = 404
            });
        }

        var query = _context.Testemunhos
            .Where(t => t.FornecedorId == fornecedorId)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync();
        var testemunhos = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TestemunhoListDto
            {
                Id = t.Id,
                Nome = t.Nome,
                Descricao = t.Descricao,
                CreatedAt = t.CreatedAt
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

    /// <summary>
    /// Obtém um testemunho específico por ID
    /// </summary>
    /// <param name="id">ID do testemunho</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TestemunhoDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var testemunho = await _context.Testemunhos
            .Where(t => t.Id == id)
            .Select(t => new TestemunhoDto
            {
                Id = t.Id,
                Nome = t.Nome,
                Descricao = t.Descricao,
                FornecedorId = t.FornecedorId,
                CreatedAt = t.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (testemunho == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Testemunho não encontrado",
                Detail = $"Testemunho com ID {id} não foi encontrado.",
                Status = 404
            });
        }

        return Ok(testemunho);
    }

    /// <summary>
    /// Cria um novo testemunho para um fornecedor
    /// </summary>
    /// <param name="dto">Dados do testemunho</param>
    [HttpPost]
    [ProducesResponseType(typeof(TestemunhoDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Create([FromBody] CreateTestemunhoDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verificar se o fornecedor existe
        var fornecedorExists = await _context.Fornecedores.AnyAsync(f => f.Id == dto.FornecedorId);
        if (!fornecedorExists)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Fornecedor não encontrado",
                Detail = $"Fornecedor com ID {dto.FornecedorId} não foi encontrado.",
                Status = 404
            });
        }

        var testemunho = new Testemunho
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome.Trim(),
            Descricao = dto.Descricao.Trim(),
            FornecedorId = dto.FornecedorId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Testemunhos.Add(testemunho);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Testemunho {TestemunhoId} criado para o fornecedor {FornecedorId}", 
            testemunho.Id, testemunho.FornecedorId);

        var result = new TestemunhoDto
        {
            Id = testemunho.Id,
            Nome = testemunho.Nome,
            Descricao = testemunho.Descricao,
            FornecedorId = testemunho.FornecedorId,
            CreatedAt = testemunho.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = testemunho.Id }, result);
    }
}
