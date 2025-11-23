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
    public DbSet<ContatoSubmission> ContatoSubmissions { get; set; } = null!;
    public DbSet<InstitucionalContent> InstitucionalContents { get; set; } = null!;

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

        // Configure relationships and delete behaviors explicitly
        modelBuilder.Entity<Fornecedor>(b =>
        {
            b.HasOne(f => f.Categoria)
             .WithMany(c => c.Fornecedores)
             .HasForeignKey(f => f.CategoriaId)
             .OnDelete(DeleteBehavior.SetNull);

            b.Navigation(f => f.Categoria);
            b.Navigation(f => f.Medias);
        });

        modelBuilder.Entity<Media>(b =>
        {
            b.HasOne(m => m.Fornecedor)
             .WithMany(f => f.Medias)
             .HasForeignKey(m => m.FornecedorId)
             .OnDelete(DeleteBehavior.Cascade);

            b.Navigation(m => m.Fornecedor);
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

        // Relacionamento entre Categoria e Media (uma categoria possui no máximo uma mídia)
        modelBuilder.Entity<Media>(b =>
        {
            b.HasOne(m => m.Categoria)
             .WithOne(c => c.Media)
             .HasForeignKey<Media>(m => m.CategoriaId)
             .OnDelete(DeleteBehavior.SetNull);

            b.Navigation(m => m.Categoria);
        });

        	// Configure relationships and delete behaviors explicitly
        	modelBuilder.Entity<Fornecedor>(b =>
        	{
        	    b.HasOne(f => f.Categoria)
        	     .WithMany(c => c.Fornecedores)
        	     .HasForeignKey(f => f.CategoriaId)
        	     .OnDelete(DeleteBehavior.SetNull);

        	    b.HasMany(f => f.Medias)
        	     .WithOne(m => m.Fornecedor)
        	     .HasForeignKey(m => m.FornecedorId)
        	     .OnDelete(DeleteBehavior.Cascade);
        	});

        modelBuilder.Entity<Usuario>(b =>
        {
            b.Property(u => u.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            b.HasIndex(u => u.Email).IsUnique(false);
        });
    }
}
