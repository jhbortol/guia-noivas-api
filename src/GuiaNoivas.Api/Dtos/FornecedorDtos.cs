using System;
using System.Collections.Generic;

namespace GuiaNoivas.Api.Dtos;

public record FornecedorListDto(Guid Id, string Nome, string Slug, string? Descricao, string? Cidade, decimal? Rating, bool Destaque, bool SeloFornecedor);

public record FornecedorDetailDto(Guid Id, string Nome, string Slug, string? Descricao, string? Cidade, string? Telefone, string? Email, string? Website, bool Destaque, bool SeloFornecedor, decimal? Rating, int Visitas, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, IEnumerable<object>? Imagens = null, IEnumerable<object>? Categorias = null);
