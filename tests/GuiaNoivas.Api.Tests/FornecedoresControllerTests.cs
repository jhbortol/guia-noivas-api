using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Models;
using Xunit;

namespace GuiaNoivas.Api.Tests;

public class FornecedoresControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FornecedoresControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Filter_Delete_CascadeMedia_Works()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, o => { });
            });
        }).CreateClient();

        // Seed a category via scope
        Guid categoriaId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cat = new Categoria { Id = Guid.NewGuid(), Nome = "Cat Test", Slug = "cat-test", CreatedAt = DateTimeOffset.UtcNow, Order = 0 };
            db.Categorias.Add(cat);
            await db.SaveChangesAsync();
            categoriaId = cat.Id;
        }

        var createDto = new
        {
            Nome = "Fornecedor Test",
            Slug = "fornecedor-test",
            Descricao = "desc",
            CategoriaId = categoriaId
        };

        // Act - create
        var createResp = await client.PostAsJsonAsync("/api/v1/fornecedores", createDto);
        createResp.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        // Act - filter
        var filterResp = await client.GetAsync($"/api/v1/fornecedores?categoriaId={categoriaId}");
        filterResp.EnsureSuccessStatusCode();
        var listObj = await filterResp.Content.ReadFromJsonAsync<dynamic>();
        ((object)listObj).Should().NotBeNull();

        // Find created fornecedor id
        Guid fornecedorId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var f = await db.Fornecedores.FirstOrDefaultAsync(ff => ff.Slug == "fornecedor-test");
            f.Should().NotBeNull();
            fornecedorId = f!.Id;

            // Add a media linked to fornecedor
            var m = new Media { Id = Guid.NewGuid(), FornecedorId = fornecedorId, Url = "http://x", Filename = "a.jpg", ContentType = "image/jpeg", CreatedAt = DateTimeOffset.UtcNow };
            db.Media.Add(m);
            await db.SaveChangesAsync();
        }

        // Act - delete fornecedor
        var delResp = await client.DeleteAsync($"/api/v1/fornecedores/{fornecedorId}");
        delResp.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        // Assert - media should be deleted (cascade)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var medias = await db.Media.Where(m => m.FornecedorId == fornecedorId).ToListAsync();
            medias.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Create_WithAtivoFlag_RespondsOnAllAndNotOnAtivos()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, o => { });
            });
        }).CreateClient();

        // seed category
        Guid categoriaId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cat = new Categoria { Id = Guid.NewGuid(), Nome = "Cat A", Slug = "cat-a", CreatedAt = DateTimeOffset.UtcNow, Order = 0 };
            db.Categorias.Add(cat);
            await db.SaveChangesAsync();
            categoriaId = cat.Id;
        }

        var createDto = new
        {
            Nome = "Fornecedor Inativo",
            Slug = "fornecedor-inativo",
            Descricao = "desc",
            CategoriaId = categoriaId,
            Ativo = false
        };

        var createResp = await client.PostAsJsonAsync("/api/v1/fornecedores", createDto);
        createResp.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        // GET all should contain it
        var allResp = await client.GetAsync("/api/v1/fornecedores/all");
        allResp.EnsureSuccessStatusCode();
        var allList = await allResp.Content.ReadFromJsonAsync<GuiaNoivas.Api.Dtos.FornecedorListDto[]>();
        allList.Should().NotBeNull();
        allList!.Any(f => f.Slug == "fornecedor-inativo").Should().BeTrue();

        // GET ativos should NOT contain it
        var ativosResp = await client.GetAsync("/api/v1/fornecedores/ativos");
        ativosResp.EnsureSuccessStatusCode();
        var ativosList = await ativosResp.Content.ReadFromJsonAsync<GuiaNoivas.Api.Dtos.FornecedorListDto[]>();
        ativosList.Should().NotBeNull();
        ativosList!.Any(f => f.Slug == "fornecedor-inativo").Should().BeFalse();
    }
}
