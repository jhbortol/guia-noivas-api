using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace GuiaNoivas.Api.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        // Prefer explicit environment variable (useful in CI/containers)
        var connection = Environment.GetEnvironmentVariable("CONNECTION_STRING");

        // If not provided via env, try to read from appsettings.json in the project directory
        if (string.IsNullOrEmpty(connection))
        {
            var basePath = Directory.GetCurrentDirectory();
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();

            var config = configBuilder.Build();
            connection = config.GetConnectionString("DefaultConnection");
        }

        // Fallback to local SQL Express for development
        connection ??= "Server=.\\SQLEXPRESS;Database=GuiaNoivas;Trusted_Connection=True;MultipleActiveResultSets=true;";

        builder.UseSqlServer(connection);
        return new AppDbContext(builder.Options);
    }
}
