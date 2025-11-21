using System.ComponentModel.DataAnnotations;

namespace GuiaNoivas.Api.Models;

public class Media
{
    [Key]
    public Guid Id { get; set; }

    public Guid? FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }
    public string? Url { get; set; }
    public string? Filename { get; set; }
    public string? ContentType { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
