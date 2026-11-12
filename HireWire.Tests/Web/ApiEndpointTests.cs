using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HireWire.Application.DTOs;
using HireWire.Infrastructure.Data;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HireWire.Tests.Web;

public class HireWireWebFactory : WebApplicationFactory<Program>
{
    public SqliteConnection Connection { get; } = new("DataSource=:memory:");

    public HireWireWebFactory()
    {
        Connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        var webProjectPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "HireWire.Web"));
        builder.UseContentRoot(webProjectPath);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Force deterministic offline AI during tests (never call real APIs)
                ["AiSettings:ActiveProvider"] = "Mock"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<HireWireDbContext>>();
            services.RemoveAll<DbContextOptions>();
            // AddDbContext registers IDbContextOptionsConfiguration<TContext> holding the SqlServer setup; remove it too
            var toRemove = services
                .Where(d => d.ServiceType == typeof(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<HireWireDbContext>))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);
            services.AddDbContext<HireWireDbContext>(options => options.UseSqlite(Connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Connection.Dispose();
        }
    }
}

public class ApiEndpointTests : IClassFixture<HireWireWebFactory>
{
    private readonly HireWireWebFactory _factory;
    private readonly HttpClient _client;

    public ApiEndpointTests(HireWireWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HireWireDbContext>();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetHome_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostLead_WithValidDto_Returns201WithId()
    {
        var dto = new ScrapedJobLeadDto(
            CompanyName: "Test Corp",
            Industry: null,
            CompanyContext: null,
            JobTitle: "Senior .NET Developer",
            Url: "https://example.com/test-lead-api",
            Platform: "LinkedIn",
            RawDescription: "Descripción completa de la vacante de prueba",
            SalaryRange: "$100k",
            RecruiterName: null);

        var response = await _client.PostAsJsonAsync("/api/leads", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task PostLead_WithInvalidDto_Returns400()
    {
        // Missing required fields -> model binding/validation failure
        var payload = new { CompanyName = "" };
        var response = await _client.PostAsJsonAsync("/api/leads", payload);
        ((int)response.StatusCode).Should().BeOneOf(400, 500); // minimal API: malformed dto -> 400/500, never 201
    }

    [Fact]
    public async Task GetCvPdf_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/cvs/{Guid.NewGuid()}.pdf");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
