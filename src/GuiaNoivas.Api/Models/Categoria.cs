using System.ComponentModel.DataAnnotations;

namespace GuiaNoivas.Api.Models;

public class Categoria
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
    public int Order { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navegação inversa: uma categoria pode ter vários fornecedores
    public ICollection<Fornecedor> Fornecedores { get; set; } = new List<Fornecedor>();

    // Uma categoria possui no máximo uma imagem
    public Media? Media { get; set; }
}
