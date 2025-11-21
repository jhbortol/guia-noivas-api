using Microsoft.EntityFrameworkCore;
using GuiaNoivas.Api.Models;

namespace GuiaNoivas.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Fornecedor> Fornecedores { get; set; } = null!;
    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<Media> Media { get; set; } = null!;
    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Fornecedor>(b =>
        {
            b.HasIndex(f => f.Slug).IsUnique();
            b.HasIndex(f => new { f.Destaque, f.Rating });
            b.HasIndex(f => f.Cidade);
            b.Property(f => f.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(f => f.Visitas).HasDefaultValue(0);
            b.Property(f => f.Destaque).HasDefaultValue(false);
            b.Property(f => f.SeloFornecedor).HasDefaultValue(false);
            b.Property(f => f.Rating).HasPrecision(5, 2);
        });

        modelBuilder.Entity<Categoria>(b =>
        {
            b.HasIndex(c => c.Slug).IsUnique();
            b.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Media>(b =>
        {
            b.Property(m => m.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Usuario>(b =>
        {
            b.Property(u => u.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            b.HasIndex(u => u.Email).IsUnique(false);
        });
    }
}
