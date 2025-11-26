using System;
using System.Collections.Generic;

namespace GuiaNoivas.Api.Dtos;

public record MediaDto(Guid Id, string? Url, string? Filename, string? ContentType, bool IsPrimary);

public record CategoriaDto(Guid Id, string Nome, string Slug);

public record FornecedorListDto(Guid Id, string Nome, string Slug, string? Descricao, string? Cidade, decimal? Rating, bool Destaque, bool SeloFornecedor, bool Ativo, CategoriaDto? Categoria = null, MediaDto? PrimaryImage = null, IEnumerable<MediaDto>? Imagens = null);

public record FornecedorDetailDto(Guid Id, string Nome, string Slug, string? Descricao, string? Cidade, string? Telefone, string? Email, string? Website, string? Instagram, bool Destaque, bool SeloFornecedor, bool Ativo, decimal? Rating, int Visitas, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, IEnumerable<MediaDto>? Imagens = null, CategoriaDto? Categoria = null, IEnumerable<TestemunhoListDto>? Testemunhos = null);

public record FornecedorCreateDto(string Nome, string Slug, Guid? CategoriaId, string? Descricao, string? Cidade, string? Telefone, string? Email, string? Website, string? Instagram, bool Destaque = false, bool SeloFornecedor = false, bool Ativo = true, decimal? Rating = null);

public record FornecedorUpdateDto(string Nome, string Slug, Guid? CategoriaId, string? Descricao, string? Cidade, string? Telefone, string? Email, string? Website, string? Instagram, bool Destaque = false, bool SeloFornecedor = false, bool Ativo = true, decimal? Rating = null);
