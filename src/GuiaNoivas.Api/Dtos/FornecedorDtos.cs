using System;
using System.Collections.Generic;

namespace GuiaNoivas.Api.Dtos;

public record MediaDto(Guid Id, string? Url, string? Filename, string? ContentType, bool IsPrimary);

public record CategoriaDto(Guid Id, string Nome, string Slug);

public record FornecedorListDto(Guid Id, string Nome, string Slug, string? Descricao, string? Cidade, decimal? Rating, bool Destaque, bool SeloFornecedor, CategoriaDto? Categoria = null, MediaDto? PrimaryImage = null);

public record FornecedorDetailDto(Guid Id, string Nome, string Slug, string? Descricao, string? Cidade, string? Telefone, string? Email, string? Website, bool Destaque, bool SeloFornecedor, decimal? Rating, int Visitas, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, IEnumerable<MediaDto>? Imagens = null, CategoriaDto? Categoria = null);
