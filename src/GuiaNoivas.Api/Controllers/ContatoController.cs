using Microsoft.AspNetCore.Mvc;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Models;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class ContatoController : ControllerBase
{
    private readonly AppDbContext _db;

    public ContatoController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("contato")]
    public async Task<IActionResult> SubmitContato([FromBody] ContatoDto dto)
    {
        var s = new ContatoSubmission
        {
            Id = Guid.NewGuid(),
            FornecedorId = dto.FornecedorId,
            Nome = dto.Nome,
            Email = dto.Email,
            Telefone = dto.Telefone,
            Mensagem = dto.Mensagem,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.ContatoSubmissions.Add(s);
        await _db.SaveChangesAsync();
        return Accepted(new { id = s.Id });
    }

    [HttpPost("anuncie")]
    public async Task<IActionResult> Anuncie([FromBody] ContatoDto dto)
    {
        // Reuse the same model for simplicity
        var s = new ContatoSubmission
        {
            Id = Guid.NewGuid(),
            FornecedorId = dto.FornecedorId,
            Nome = dto.Nome,
            Email = dto.Email,
            Telefone = dto.Telefone,
            Mensagem = dto.Mensagem,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.ContatoSubmissions.Add(s);
        await _db.SaveChangesAsync();
        return Accepted(new { id = s.Id });
    }
}

public record ContatoDto(Guid? FornecedorId, string Nome, string Email, string? Telefone, string Mensagem);
