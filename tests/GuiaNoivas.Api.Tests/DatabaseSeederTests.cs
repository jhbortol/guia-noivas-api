using System.Threading.Tasks;
using GuiaNoivas.Api.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GuiaNoivas.Api.Tests;

public class DatabaseSeederTests
{
    [Fact]
    public async Task Seed_CreatesCategories()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("seed_db")
            .Options;

        using var db = new AppDbContext(options);
        await DatabaseSeeder.SeedAsync(db);

        var count = await db.Categorias.CountAsync();
        Assert.True(count >= 1);
    }
}
