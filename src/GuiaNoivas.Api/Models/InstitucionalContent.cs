using System.ComponentModel.DataAnnotations;

namespace GuiaNoivas.Api.Models;

public class InstitucionalContent
{
    [Key]
    [MaxLength(100)]
    public string Key { get; set; } = null!;

    [MaxLength(200)]
    public string? Title { get; set; }

    public string? ContentHtml { get; set; }

    public int Version { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
