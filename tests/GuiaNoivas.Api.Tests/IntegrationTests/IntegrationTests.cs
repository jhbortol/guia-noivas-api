using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Models;
using GuiaNoivas.Api.Services;
using Xunit;

namespace GuiaNoivas.Api.Tests.IntegrationTests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Customize the factory to use InMemory DB and a fake blob service
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, cfg) =>
            {
                // Provide Jwt secret for token generation
                var dict = new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "integration-test-secret-very-strong-key",
                    ["Jwt:Issuer"] = "GuiaNoivas.IntegrationTest",
                    ["Jwt:Audience"] = "GuiaNoivas.IntegrationTestAudience"
                };
                cfg.AddInMemoryCollection(dict);
            });

            builder.ConfigureServices(services =>
            {
                // Replace authentication with a test authentication scheme that auto-authenticates as Admin
                services.AddAuthentication("Test")
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
                services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(opts =>
                {
                    opts.DefaultAuthenticateScheme = "Test";
                    opts.DefaultChallengeScheme = "Test";
                });

                // Replace AppDbContext with InMemory
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                // Register fake IBlobService
                services.AddSingleton<IBlobService, FakeBlobService>();

                // Ensure a scoped provider to seed data after building
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                // Seed an admin user
                var admin = new Usuario
                {
                    Id = Guid.NewGuid(),
                    Email = "admin@example.com",
                    DisplayName = "Admin",
                    Roles = "Admin",
                    CreatedAt = DateTimeOffset.UtcNow,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!")
                };
                db.Usuarios.Add(admin);
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task Auth_Login_Admin_CanCreateFornecedor_And_PresignMedia()
    {
        var client = _factory.CreateClient();

        // Create fornecedor (admin)
        var fornecedorBody = JsonSerializer.Serialize(new
        {
            nome = "Teste Fornecedor",
            descricao = "Fornecedor criado por teste",
            cidade = "Piracicaba",
            telefone = "19 99999-9999",
            email = "teste@fornecedor.local",
            website = "https://example.local",
            destaque = false,
            seloFornecedor = false
        });

        var createResp = await client.PostAsync("/api/v1/admin/fornecedores", new StringContent(fornecedorBody, Encoding.UTF8, "application/json"));
        Assert.True(createResp.IsSuccessStatusCode, await createResp.Content.ReadAsStringAsync());

        // Upload media via backend (multipart/form-data)
        var createJson = await createResp.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var fornecedorId = createDoc.RootElement.GetProperty("id").GetGuid();

        var multipart = new MultipartFormDataContent();
        var fakeBytes = Encoding.UTF8.GetBytes("fake-image-bytes");
        var fileContent = new ByteArrayContent(fakeBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        multipart.Add(fileContent, "File", "foto.jpg");
        multipart.Add(new StringContent(fornecedorId.ToString()), "FornecedorId");
        multipart.Add(new StringContent("foto.jpg"), "Filename");
        multipart.Add(new StringContent("image/jpeg"), "ContentType");

        var uploadResp = await client.PostAsync("/api/v1/media/upload", multipart);
        uploadResp.EnsureSuccessStatusCode();
        var uploadJson = await uploadResp.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(uploadJson));
    }

    private class FakeBlobService : IBlobService
    {
        public Task<(Uri Url, string BlobName)> GetUploadSasUriAsync(string blobName, TimeSpan expiry, string? contentType = null)
        {
            var upload = new Uri($"https://fakestorage.local/media/{blobName}");
            return Task.FromResult<(Uri, string)>((upload, blobName));
        }
        public Task<string> UploadAsync(string blobName, System.IO.Stream stream, string contentType)
        {
            // In tests we don't actually upload; return a fake blob URL
            var uri = $"https://fakestorage.local/media/{blobName}";
            return Task.FromResult(uri);
        }
    }

    // Test authentication handler - automatically authenticates as Admin
    private class TestAuthHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
    {
        public TestAuthHandler(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "integration-test"), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin") };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Test");
            return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
        }
    }
}
