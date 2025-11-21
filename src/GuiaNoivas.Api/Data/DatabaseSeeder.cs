using GuiaNoivas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GuiaNoivas.Api.Data;

public static class DatabaseSeeder
{
    private static readonly (string Nome, string Slug, int Order)[] SeedCategorias = new[]
    {
        ("Cerimonial", "cerimonial", 1),
        ("Fotografia", "fotografia", 2),
        ("Buffet", "buffet", 3),
        ("Confeitaria", "confeitaria", 4),
        ("Vestidos", "vestidos", 5)
    };

    public static async Task SeedAsync(AppDbContext db)
    {
        if (db == null) throw new ArgumentNullException(nameof(db));

        // Ensure database is created (migrations should be applied before calling seed in production flow)
        await db.Database.EnsureCreatedAsync();

        foreach (var (Nome, Slug, Order) in SeedCategorias)
        {
            var existing = await db.Categorias.FirstOrDefaultAsync(c => c.Slug == Slug);
            if (existing == null)
            {
                db.Categorias.Add(new Categoria
                {
                    Id = Guid.NewGuid(),
                    Nome = Nome,
                    Slug = Slug,
                    Order = Order,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                // Update fields if changed (idempotent)
                var changed = false;
                if (existing.Nome != Nome) { existing.Nome = Nome; changed = true; }
                if (existing.Order != Order) { existing.Order = Order; changed = true; }
                if (changed) db.Categorias.Update(existing);
            }
        }

        await db.SaveChangesAsync();
    }
}
