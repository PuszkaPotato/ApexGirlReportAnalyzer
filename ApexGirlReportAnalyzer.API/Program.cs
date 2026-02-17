using System.Security.Cryptography;
using ApexGirlReportAnalyzer.API.Authentication;
using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Infrastructure.Data;
using ApexGirlReportAnalyzer.Infrastructure.Services;
using ApexGirlReportAnalyzer.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpClient for OpenAI service
builder.Services.AddHttpClient<IOpenAIService, OpenAIService>();

// Add UploadService
builder.Services.AddScoped<IUploadService, UploadService>();

// Add UserService
builder.Services.AddScoped<IUserService, UserService>();

// Add BattleReportService
builder.Services.AddScoped<IBattleReportService, BattleReportService>();

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "ApexGirlReportAnalyzer API", Version = "v1" });
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key authentication. Enter your API key below.",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Scheme = "apikey"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("ApiKey", document)] = new List<string>()
    });
});

// Add API key authentication
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);

var app = builder.Build();

// CLI: Generate API key and exit
if (args.Contains("--generate-apikey"))
{
    var name = GetArgValue(args, "--name");
    if (name is null)
    {
        Console.Error.WriteLine("Error: --name is required. Usage: --generate-apikey --name \"Key Name\" [--scope admin] [--expires-in-days 365]");
        return;
    }

    var scope = GetArgValue(args, "--scope") ?? "admin";
    var expiresInDaysStr = GetArgValue(args, "--expires-in-days");
    DateTime? expiresAt = expiresInDaysStr is not null && int.TryParse(expiresInDaysStr, out var days)
        ? DateTime.SpecifyKind(DateTime.UtcNow.AddDays(days), DateTimeKind.Utc)
        : null;

    var keyBytes = RandomNumberGenerator.GetBytes(32);
    var plainTextKey = "agra_" + Convert.ToBase64String(keyBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');

    using var dbScope = app.Services.CreateScope();
    var db = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.ApiKeys.Add(new ApiKey
    {
        Key = plainTextKey,
        Name = name,
        Scope = scope,
        IsActive = true,
        ExpiresAt = expiresAt,
        CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
    });
    await db.SaveChangesAsync();

    Console.WriteLine($"API key generated successfully:");
    Console.WriteLine($"  Name:    {name}");
    Console.WriteLine($"  Scope:   {scope}");
    Console.WriteLine($"  Expires: {expiresAt?.ToString("yyyy-MM-dd") ?? "never"}");
    Console.WriteLine($"  Key:     {plainTextKey}");
    Console.WriteLine();
    Console.WriteLine("Save this key now — it will not be shown again.");
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "openapi/{documentName}.json";
    });
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ApexGirlReportAnalyzer API v1");
    });
    // Default route to swagger in development
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

// Initialize and seed the database in development environment
await app.Services.InitializeDatabaseAsync(app.Configuration, app.Environment.EnvironmentName);


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static string? GetArgValue(string[] args, string flag)
{
    var index = Array.IndexOf(args, flag);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}