using System.ComponentModel.DataAnnotations;

namespace GuiaNoivas.Api.Models;

public class Fornecedor
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = null!;

    public string? Descricao { get; set; }
    public string? Cidade { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public bool Destaque { get; set; }
    public bool SeloFornecedor { get; set; }
    public decimal? Rating { get; set; }
    public int Visitas { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
