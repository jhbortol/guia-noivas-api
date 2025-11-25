using System.ComponentModel.DataAnnotations;

namespace GuiaNoivas.Api.Dtos;

public record CreateTestemunhoDto
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres")]
    public string Nome { get; init; } = string.Empty;

    [Required(ErrorMessage = "A descrição é obrigatória")]
    [StringLength(2000, ErrorMessage = "A descrição deve ter no máximo 2000 caracteres")]
    public string Descricao { get; init; } = string.Empty;

    [Required(ErrorMessage = "O ID do fornecedor é obrigatório")]
    public Guid FornecedorId { get; init; }
}

public record TestemunhoDto
{
    public Guid Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Descricao { get; init; } = string.Empty;
    public Guid FornecedorId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public record TestemunhoListDto
{
    public Guid Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Descricao { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
