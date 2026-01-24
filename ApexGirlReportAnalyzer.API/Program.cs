using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Infrastructure.Data;
using ApexGirlReportAnalyzer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Defaultconnection")));

// Add HttpClient for OpenAI service
builder.Services.AddHttpClient<IOpenAIService, OpenAIService>();

// Add UploadService
builder.Services.AddScoped<IUploadService, UploadService>();

// Add UserService
builder.Services.AddScoped<IUserService, UserService>();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
    // Default route to swagger in development
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

// Initialize and seed the database in development environment
await app.Services.InitializeDatabaseAsync(app.Configuration, app.Environment.EnvironmentName);


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();