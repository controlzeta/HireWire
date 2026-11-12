using FluentAssertions;
using HireWire.Application.Interfaces;
using HireWire.Application.Services;
using HireWire.Application.Services.AiStrategies;
using HireWire.Domain.Entities;
using HireWire.Domain.Enums;
using HireWire.Infrastructure.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HireWire.Tests.Infrastructure;

public class DynamicCvGeneratorServiceTests
{
    private readonly Mock<IJobLeadRepository> _repo = new();

    private DynamicCvGeneratorService BuildService(IConfiguration? config = null, IAiStrategy? strategy = null)
    {
        strategy ??= new MockAiStrategy();
        var factory = new AiServiceFactory(
            new List<IAiStrategy> { strategy },
            new AiSettingsManager { ActiveProvider = strategy.ProviderName });

        var hostEnv = new Mock<IHostEnvironment>();
        hostEnv.Setup(e => e.ContentRootPath).Returns(Path.GetTempPath());
        hostEnv.Setup(e => e.EnvironmentName).Returns("Production");

        config ??= new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        return new DynamicCvGeneratorService(
            _repo.Object,
            config,
            factory,
            hostEnv.Object,
            NullLogger<DynamicCvGeneratorService>.Instance);
    }

    private static JobLead MakeLeadWithCompany(TargetRole? role = null)
    {
        var company = new Company(Guid.NewGuid(), "Acme Corp", "Fintech", "ctx");
        var lead = new JobLead(Guid.NewGuid(), company.Id, "Senior .NET Developer",
            "https://example.com/job", "LinkedIn",
            "We are looking for a .NET engineer with experience in microservices, remote work, full-time, CI/CD.",
            "$100k");
        typeof(JobLead).GetProperty(nameof(JobLead.Company))!.SetValue(lead, company);
        if (role.HasValue)
        {
            lead.SetOEConfig(new OE_Config(Guid.NewGuid(), lead.Id, role.Value, 90, true, null));
        }
        return lead;
    }

    [Fact]
    public async Task GenerateCvAsync_NotFound_ThrowsKeyNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((JobLead?)null);
        var service = BuildService();

        await FluentActions.Invoking(() => service.GenerateCvAsync(Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GenerateCvAsync_ValidLead_ReturnsNonEmptyPdf()
    {
        var lead = MakeLeadWithCompany(TargetRole.J2);
        _repo.Setup(r => r.GetByIdAsync(lead.Id)).ReturnsAsync(lead);
        var service = BuildService();

        var bytes = await service.GenerateCvAsync(lead.Id);

        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task GenerateCvAsync_LeadWithoutOEConfig_UsesDefaultJ2()
    {
        var lead = MakeLeadWithCompany(role: null);
        _repo.Setup(r => r.GetByIdAsync(lead.Id)).ReturnsAsync(lead);
        var service = BuildService();

        var bytes = await service.GenerateCvAsync(lead.Id);
        bytes.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task GenerateCvAsync_SpanishDefaultLanguage_GeneratesPdf()
    {
        var lead = MakeLeadWithCompany(TargetRole.J1);
        _repo.Setup(r => r.GetByIdAsync(lead.Id)).ReturnsAsync(lead);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UserProfile:DefaultLanguage"] = "es",
                ["UserProfile:FullName"] = "Francisco Arroyo"
            })
            .Build();
        var service = BuildService(config);

        var bytes = await service.GenerateCvAsync(lead.Id);
        bytes.Length.Should().BeGreaterThan(1000);
    }
}
