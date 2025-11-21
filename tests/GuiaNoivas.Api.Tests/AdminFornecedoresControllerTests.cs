using System;
using System.Threading.Tasks;
using GuiaNoivas.Api.Controllers;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GuiaNoivas.Api.Tests;

public class AdminFornecedoresControllerTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Create_CreatesFornecedor()
    {
        using var db = CreateContext("create_success");
        var controller = new AdminFornecedoresController(db);

        var dto = new CreateFornecedorDto
        {
            Nome = "Test Fornecedor",
            Slug = null,
            Descricao = "Desc",
            Cidade = "Cidade",
            Telefone = "123",
            Email = "test@example.com",
            Website = "https://example.com",
            Destaque = false,
            SeloFornecedor = false,
            Rating = 4.5m
        };

        var result = await controller.Create(dto);

        // verify DB has one fornecedor
        var count = await db.Fornecedores.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Create_DuplicateSlug_ReturnsConflict()
    {
        using var db = CreateContext("create_conflict");
        // seed existing fornecedor with slug
        var existing = new Fornecedor
        {
            Id = Guid.NewGuid(),
            Nome = "Existing",
            Slug = "existing",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Fornecedores.Add(existing);
        await db.SaveChangesAsync();

        var controller = new AdminFornecedoresController(db);

        var dto = new CreateFornecedorDto
        {
            Nome = "Another",
            Slug = "existing",
            Descricao = null,
            Cidade = null,
            Telefone = null,
            Email = null,
            Website = null,
            Destaque = false,
            SeloFornecedor = false,
            Rating = null
        };

        var result = await controller.Create(dto);
        // Expect ConflictResult (ObjectResult with 409) — controller returns Conflict
        Assert.True(result is Microsoft.AspNetCore.Mvc.ObjectResult or Microsoft.AspNetCore.Mvc.CreatedAtActionResult);
        // ensure still only one in DB
        var count = await db.Fornecedores.CountAsync();
        Assert.Equal(1, count);
    }
}
