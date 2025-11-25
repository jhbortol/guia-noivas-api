namespace GuiaNoivas.Api.Models;

public class Testemunho
{
    public Guid Id { get; set; }
    
    public Guid FornecedorId { get; set; }
    
    public required string Nome { get; set; }
    
    public required string Descricao { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    // Navigation property
    public Fornecedor? Fornecedor { get; set; }
}
