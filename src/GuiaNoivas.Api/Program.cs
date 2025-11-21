using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Hangfire;
using Hangfire.SqlServer;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Register a custom authorization result handler that can skip the fallback policy for some paths
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.Policy.IAuthorizationMiddlewareResultHandler, GuiaNoivas.Api.SkipPathAuthorizationResultHandler>();

builder.Configuration.AddEnvironmentVariables();

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// DB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? "Server=.\\SQLEXPRESS;Database=GuiaNoivas;Trusted_Connection=True;MultipleActiveResultSets=true;";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// Controllers
builder.Services.AddControllers().AddNewtonsoftJson();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// Hangfire
builder.Services.AddHangfire(configuration =>
    configuration.UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();

// Storage (Azure Blob) configuration
var storageConnection = builder.Configuration["Storage:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING");
var storageContainer = builder.Configuration["Storage:Container"] ?? "media";
if (!string.IsNullOrWhiteSpace(storageConnection))
{
    builder.Services.AddSingleton<IBlobService>(sp => new BlobService(storageConnection, storageContainer));
    // Register BlobServiceClient for direct upload proxy
    builder.Services.AddSingleton(new Azure.Storage.Blobs.BlobServiceClient(storageConnection));
}

// Authentication (JWT)
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? "please-change-this-secret";
var key = Encoding.ASCII.GetBytes(jwtSecret);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// Make all endpoints require authentication by default, except where [AllowAnonymous] is used
// Only enable the global fallback policy in non-development environments so tools
// like Swagger UI and the Hangfire dashboard remain accessible while debugging.
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });
}
else
{
    // In Development register AddAuthorization without a fallback policy so endpoints
    // without metadata (Swagger, Hangfire) remain accessible.
    builder.Services.AddAuthorization();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSerilogRequestLogging();

// Apply migrations and seed DB on startup (will create DB if needed)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        // Apply pending migrations only for relational providers; use EnsureCreated for in-memory/testing
        if (db.Database.IsRelational())
        {
            db.Database.Migrate();
        }
        else
        {
            db.Database.EnsureCreated();
        }
        // Seed categories idempotently
        DatabaseSeeder.SeedAsync(db).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred while migrating or seeding the database.");
        throw;
    }
}


app.UseRouting();
app.UseAuthentication();
// Serve static files before authorization so Swagger UI files are accessible
app.UseStaticFiles();

// Register Swagger UI before authorization so it is not blocked by the fallback policy
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    // Swagger UI endpoint - allow anonymous
    // Removed custom redirect to avoid infinite loop. Swagger static files are served by app.UseSwaggerUI().
    // Hangfire dashboard endpoint - allow anonymous
    endpoints.MapHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
    {
        Authorization = new[] { new GuiaNoivas.Api.AllowAnonymousAuthorizationFilter() }
    }).AllowAnonymous();
});

app.Run();
