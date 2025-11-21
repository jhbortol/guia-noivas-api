using System.ComponentModel.DataAnnotations;

namespace GuiaNoivas.Api.Models;

public class ContatoSubmission
{
    [Key]
    public Guid Id { get; set; }

    public Guid? FornecedorId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = null!;

    [MaxLength(50)]
    public string? Telefone { get; set; }

    [Required]
    public string Mensagem { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}
