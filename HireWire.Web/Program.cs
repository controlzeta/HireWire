using Microsoft.EntityFrameworkCore;
using HireWire.Infrastructure.Data;
using HireWire.Infrastructure.Repositories;
using HireWire.Application.Interfaces;
using HireWire.Application.Services;
using HireWire.Application.DTOs;
using HireWire.Application.Services.AiStrategies;
using HireWire.Infrastructure.Services;
using Serilog;
using HireWire.Web.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/hirewire-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("profile.json", optional: true, reloadOnChange: true);
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Swagger/OpenAPI support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for Chrome Extension integration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowExtension", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Retrieve connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Register DbContext with SQL Server provider
builder.Services.AddDbContext<HireWireDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions => sqlOptions.MigrationsAssembly("HireWire.Infrastructure"))); // Migrations will live in Infrastructure

// Register Repositories and Services
builder.Services.AddScoped<IJobLeadRepository, JobLeadRepository>();
builder.Services.AddScoped<IJobLeadService, JobLeadService>();
builder.Services.AddScoped<ICvGeneratorService, DynamicCvGeneratorService>();

// Register HttpClient and AI modular API strategies
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAiSettingsManager, AiSettingsManager>();
builder.Services.AddScoped<IAiStrategy, MockAiStrategy>();
builder.Services.AddScoped<IAiStrategy, GeminiAiStrategy>();
builder.Services.AddScoped<IAiStrategy, OpenAiStrategy>();
builder.Services.AddScoped<IAiStrategy, ClaudeAiStrategy>();
builder.Services.AddScoped<AiServiceFactory>();
builder.Services.AddHostedService<FollowUpWorker>();

var app = builder.Build();

// Support hosting under a sub-path (e.g. controlzeta.io/hirewire).
// In IIS in-process hosting the path base arrives via the request automatically;
// for other hosts set "PathBase" in configuration (env var PathBase / ASPNETCORE_PathBase is picked up automatically).
var configuredPathBase = app.Configuration["PathBase"];
if (!string.IsNullOrWhiteSpace(configuredPathBase))
{
    app.UsePathBase(configuredPathBase.StartsWith('/') ? configuredPathBase : "/" + configuredPathBase);
}

// Enable Swagger UI in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Only redirect to HTTPS when an HTTPS port is actually known (avoids breaking HTTP-only deployments)
var httpsPort = app.Configuration["https_port"] ?? app.Configuration["HTTPS_PORTS"] ?? app.Configuration["ASPNETCORE_HTTPS_PORT"];
if (!string.IsNullOrEmpty(httpsPort))
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowExtension");
app.UseStaticFiles();
app.UseAntiforgery();

app.MapPost("/api/leads", async (ScrapedJobLeadDto dto, IJobLeadService service) =>
{
    try
    {
        var id = await service.ProcessScrapedJobLeadAsync(dto);
        return Results.Created($"/api/leads/{id}", new { Id = id });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("AddScrapedJobLead");

app.MapGet("/cvs/{id:guid}.pdf", async (Guid id, IHostEnvironment env, ICvGeneratorService cvService) =>
{
    try
    {
        var cvFilePath = Path.Combine(env.ContentRootPath, "wwwroot", "cvs", $"{id}.pdf");
        if (File.Exists(cvFilePath))
        {
            return Results.File(cvFilePath, "application/pdf", $"CV_HireWire_{id}.pdf");
        }

        var pdfBytes = await cvService.GenerateCvAsync(id);
        return Results.File(pdfBytes, "application/pdf", $"CV_HireWire_{id}.pdf");
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ Error al entregar el PDF del CV para {JobLeadId}", id);
        return Results.Problem(ex.Message);
    }
})
.WithName("DownloadCvPdf");

app.MapRazorComponents<HireWire.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
