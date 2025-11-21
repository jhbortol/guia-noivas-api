using System.ComponentModel.DataAnnotations;

namespace GuiaNoivas.Api.Models;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UsuarioId { get; set; }

    [Required]
    public string Token { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }

    public bool Revoked { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
