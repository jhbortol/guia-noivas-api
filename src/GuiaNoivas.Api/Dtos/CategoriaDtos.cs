using System;
using System.ComponentModel.DataAnnotations;

namespace GuiaNoivas.Api.Dtos;

public record CategoriaListDto(Guid Id, string Nome, string Slug, string? Descricao);

public record CategoriaDetailDto(Guid Id, string Nome, string Slug, string? Descricao, int Order, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

public class CategoriaCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = null!;

    public string? Descricao { get; set; }

    public int Order { get; set; }
}

public class CategoriaUpdateDto
{
    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = null!;

    public string? Descricao { get; set; }

    public int Order { get; set; }
}
