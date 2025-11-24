using System;
using System.ComponentModel.DataAnnotations;

namespace GuiaNoivas.Api.Dtos;

public record CategoriaListDto(Guid Id, string Nome, string Slug, string? Descricao, Guid? ImageId, string? ImageUrl);

public record CategoriaDetailDto(Guid Id, string Nome, string Slug, string? Descricao, int Order, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, Guid? ImageId, string? ImageUrl);

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

    // NOTE: legacy MediaId linking removed — prefer uploading directly (CategoriaId) or sending ImageBase64

    // Opcional: enviar a imagem embutida (base64) para que o backend faça o upload automaticamente
    // Se fornecido, backend irá criar uma nova Media e associá-la à categoria
    public string? ImageBase64 { get; set; }
    public string? ImageFilename { get; set; }
    public string? ImageContentType { get; set; }
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

    // NOTE: legacy MediaId linking removed — prefer uploading directly (CategoriaId) or sending ImageBase64

    // Opcional: enviar a imagem embutida (base64) para que o backend faça o upload automaticamente
    public string? ImageBase64 { get; set; }
    public string? ImageFilename { get; set; }
    public string? ImageContentType { get; set; }
}
