using System.Reflection;
using FluentAssertions;
using HireWire.Application.DTOs;
using HireWire.Application.Interfaces;
using HireWire.Application.Services;
using HireWire.Application.Services.AiStrategies;
using HireWire.Domain.Entities;
using HireWire.Domain.Enums;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HireWire.Tests.Application;

public class JobLeadServiceTests
{
    private readonly Mock<IJobLeadRepository> _repo = new();
    private readonly Dictionary<string, string?> _configValues;

    public JobLeadServiceTests()
    {
        _configValues = new Dictionary<string, string?>
        {
            ["UserProfile:FullName"] = "Francisco Arroyo"
        };
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
    }

    private IConfiguration Config() => new ConfigurationBuilder()
        .AddInMemoryCollection(_configValues)
        .Build();

    private JobLeadService BuildService(IAiStrategy? strategy = null)
    {
        strategy ??= new MockAiStrategy();
        var factory = new AiServiceFactory(
            new List<IAiStrategy> { strategy },
            new AiSettingsManager { ActiveProvider = strategy.ProviderName });
        return new JobLeadService(_repo.Object, factory, Config(), NullLogger<JobLeadService>.Instance);
    }

    private static Company MakeCompany() => new(Guid.NewGuid(), "Acme Corp", "Fintech", "ctx");

    private static JobLead MakeLead(Company company, JobLeadStatus status = JobLeadStatus.LeadExtraido)
    {
        var lead = new JobLead(Guid.NewGuid(), company.Id, "Senior .NET Developer",
            "https://example.com/job", "LinkedIn", "Descripción de la vacante", "$100k");
        SetCompany(lead, company);
        lead.ChangeStatus(status);
        return lead;
    }

    private static void SetCompany(JobLead lead, Company company)
    {
        typeof(JobLead).GetProperty(nameof(JobLead.Company))!
            .SetValue(lead, company);
    }

    private static ScrapedJobLeadDto MakeDto() => new(
        CompanyName: "Acme Corp",
        Industry: null,
        CompanyContext: null,
        JobTitle: "Senior .NET Developer",
        Url: "https://example.com/job",
        Platform: "LinkedIn",
        RawDescription: "Descripción de la vacante",
        SalaryRange: "$100k",
        RecruiterName: "Jeff Schneider");

    [Fact]
    public async Task ProcessScrapedJobLeadAsync_NullDto_Throws()
    {
        var service = BuildService();
        await FluentActions.Awaiting(() => service.ProcessScrapedJobLeadAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProcessScrapedJobLeadAsync_NewLead_CreatesCompanyAndLead_AndRunsAiEvaluation()
    {
        _repo.Setup(r => r.GetCompanyByNameAsync("Acme Corp")).ReturnsAsync((Company?)null);
        _repo.Setup(r => r.GetJobLeadByUrlOrTitleAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((JobLead?)null);
        JobLead? captured = null;
        _repo.Setup(r => r.AddJobLeadAsync(It.IsAny<JobLead>()))
            .Callback<JobLead>(l => { SetCompany(l, new Company(l.CompanyId, "Acme Corp", null, null)); captured = l; })
            .Returns(Task.CompletedTask);

        var service = BuildService();
        var id = await service.ProcessScrapedJobLeadAsync(MakeDto());

        id.Should().NotBe(Guid.Empty);
        captured.Should().NotBeNull();
        captured!.Status.Should().Be(JobLeadStatus.EvaluadoPorIa);
        captured.OE_Config.Should().NotBeNull();
        captured.OE_Config!.TargetRole.Should().Be(TargetRole.J2);
        captured.Communications.Should().ContainSingle();
        var comm = captured.Communications.Single();
        comm.ContactName.Should().Be("Jeff Schneider");
        comm.MessageBody.Should().Contain("Francisco Arroyo");
        comm.MessageBody.Should().NotContain("[Candidato]");
        _repo.Verify(r => r.AddCompanyAsync(It.IsAny<Company>()), Times.Once);
        _repo.Verify(r => r.AddJobLeadAsync(It.IsAny<JobLead>()), Times.Once);
    }

    [Fact]
    public async Task ProcessScrapedJobLeadAsync_ExistingCompany_ReusesCompany()
    {
        var company = MakeCompany();
        _repo.Setup(r => r.GetCompanyByNameAsync("Acme Corp")).ReturnsAsync(company);
        _repo.Setup(r => r.GetJobLeadByUrlOrTitleAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((JobLead?)null);
        _repo.Setup(r => r.AddJobLeadAsync(It.IsAny<JobLead>()))
            .Callback<JobLead>(l => SetCompany(l, company))
            .Returns(Task.CompletedTask);

        var service = BuildService();
        await service.ProcessScrapedJobLeadAsync(MakeDto());

        _repo.Verify(r => r.AddCompanyAsync(It.IsAny<Company>()), Times.Never);
    }

    [Fact]
    public async Task ProcessScrapedJobLeadAsync_DuplicateLead_UpdatesAndReturnsExistingId()
    {
        var company = MakeCompany();
        var existing = MakeLead(company);
        _repo.Setup(r => r.GetCompanyByNameAsync("Acme Corp")).ReturnsAsync(company);
        _repo.Setup(r => r.GetJobLeadByUrlOrTitleAsync(existing.Url, company.Id, existing.JobTitle))
            .ReturnsAsync(existing);

        var service = BuildService();
        var dto = MakeDto();
        var id = await service.ProcessScrapedJobLeadAsync(dto);

        id.Should().Be(existing.Id);
        _repo.Verify(r => r.AddJobLeadAsync(It.IsAny<JobLead>()), Times.Never);
    }

    [Fact]
    public async Task ProcessScrapedJobLeadAsync_WhenAiFails_LeadRemainsLeadExtraido()
    {
        var company = MakeCompany();
        _repo.Setup(r => r.GetCompanyByNameAsync("Acme Corp")).ReturnsAsync(company);
        _repo.Setup(r => r.GetJobLeadByUrlOrTitleAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((JobLead?)null);
        JobLead? captured = null;
        _repo.Setup(r => r.AddJobLeadAsync(It.IsAny<JobLead>()))
            .Callback<JobLead>(l => { SetCompany(l, company); captured = l; })
            .Returns(Task.CompletedTask);

        var service = BuildService(new FailingStrategy());
        var id = await service.ProcessScrapedJobLeadAsync(MakeDto());

        id.Should().NotBe(Guid.Empty);
        captured!.Status.Should().Be(JobLeadStatus.LeadExtraido);
        captured.OE_Config.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateLeadWithAiAsync_NotFound_ThrowsKeyNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((JobLead?)null);
        var service = BuildService();
        await FluentActions.Awaiting(() => service.EvaluateLeadWithAiAsync(Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task EvaluateLeadWithAiAsync_ValidLead_SavesConfigAndAdvancesStatus()
    {
        var company = MakeCompany();
        var lead = MakeLead(company, JobLeadStatus.LeadExtraido);
        _repo.Setup(r => r.GetByIdAsync(lead.Id)).ReturnsAsync(lead);

        var service = BuildService();
        var result = await service.EvaluateLeadWithAiAsync(lead.Id);

        result.Should().NotBeNull();
        lead.Status.Should().Be(JobLeadStatus.EvaluadoPorIa);
        _repo.Verify(r => r.SaveOEConfigDirectAsync(
            lead.Id, result.TargetRole, result.AiMatchScore, result.IsAsync,
            result.DetectedRedFlags, It.Is<string>(m => m.Contains("Francisco Arroyo")),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public void GetManualAiPrompt_ContainsLeadDetails()
    {
        var company = MakeCompany();
        var lead = MakeLead(company);
        var service = BuildService();
        var prompt = service.GetManualAiPrompt(lead);
        prompt.Should().Contain(lead.JobTitle);
        prompt.Should().Contain(company.Name);
    }

    [Fact]
    public void GetManualAiPrompt_Null_Throws()
    {
        var service = BuildService();
        FluentActions.Invoking(() => service.GetManualAiPrompt(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ChangeJobLeadStatusAsync_NotFound_ThrowsKeyNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((JobLead?)null);
        var service = BuildService();
        await FluentActions.Awaiting(() => service.ChangeJobLeadStatusAsync(Guid.NewGuid(), JobLeadStatus.Contactado))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ChangeJobLeadStatusAsync_Valid_ChangesStatus()
    {
        var company = MakeCompany();
        var lead = MakeLead(company);
        _repo.Setup(r => r.GetByIdAsync(lead.Id)).ReturnsAsync(lead);
        var service = BuildService();
        await service.ChangeJobLeadStatusAsync(lead.Id, JobLeadStatus.Contactado);
        lead.Status.Should().Be(JobLeadStatus.Contactado);
    }

    [Fact]
    public async Task DeleteJobLeadAsync_CallsRepository()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.DeleteJobLeadAsync(id)).Returns(Task.CompletedTask);
        var service = BuildService();
        await service.DeleteJobLeadAsync(id);
        _repo.Verify(r => r.DeleteJobLeadAsync(id), Times.Once);
    }

    [Fact]
    public async Task SaveOEConfigAsync_CallsRepository()
    {
        var id = Guid.NewGuid();
        var service = BuildService();
        await service.SaveOEConfigAsync(id, TargetRole.J1, 88, false, "flags");
        _repo.Verify(r => r.SaveOEConfigDirectAsync(id, TargetRole.J1, 88, false, "flags", null, null, null), Times.Once);
    }

    [Fact]
    public async Task GetJobLeadsAsync_ReturnsRepositoryResult()
    {
        var company = MakeCompany();
        var leads = new List<JobLead> { MakeLead(company) };
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(leads);
        var service = BuildService();
        (await service.GetJobLeadsAsync()).Should().BeSameAs(leads);
    }

    [Fact]
    public async Task GetJobLeadByIdAsync_ReturnsRepositoryResult()
    {
        var company = MakeCompany();
        var lead = MakeLead(company);
        _repo.Setup(r => r.GetByIdAsync(lead.Id)).ReturnsAsync(lead);
        var service = BuildService();
        (await service.GetJobLeadByIdAsync(lead.Id)).Should().BeSameAs(lead);
    }

    [Fact]
    public async Task GenerateStarStoryAsync_NotFound_ThrowsKeyNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((JobLead?)null);
        var service = BuildService();
        await FluentActions.Awaiting(() => service.GenerateStarStoryAsync(Guid.NewGuid(), "dolor"))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GenerateStarStoryAsync_ValidLead_ReturnsStarScript()
    {
        var company = MakeCompany();
        var lead = MakeLead(company);
        _repo.Setup(r => r.GetByIdAsync(lead.Id)).ReturnsAsync(lead);
        var service = BuildService();
        var result = await service.GenerateStarStoryAsync(lead.Id, "latencia alta");
        result.Should().Contain("(STAR)");
        result.Should().Contain("latencia alta");
    }

    [Fact]
    public async Task GenerateStackPivotAsync_ValidLead_ReturnsPivotScript()
    {
        var company = MakeCompany();
        var lead = MakeLead(company);
        _repo.Setup(r => r.GetByIdAsync(lead.Id)).ReturnsAsync(lead);
        var service = BuildService();
        var result = await service.GenerateStackPivotAsync(lead.Id, "Kubernetes");
        result.Should().Contain("(Pivot)");
        result.Should().Contain("Kubernetes");
    }

    [Fact]
    public async Task GeneratePanicAnswerAsync_ValidLead_ReturnsAnswer()
    {
        var company = MakeCompany();
        var lead = MakeLead(company);
        _repo.Setup(r => r.GetByIdAsync(lead.Id)).ReturnsAsync(lead);
        var service = BuildService();
        var result = await service.GeneratePanicAnswerAsync(lead.Id, "Redis vs SQL");
        result.Should().Contain("(Copilot)");
    }

    [Fact]
    public async Task GenerateSalaryScriptAsync_ValidLead_ReturnsSalaryScript()
    {
        var company = MakeCompany();
        var lead = MakeLead(company);
        _repo.Setup(r => r.GetByIdAsync(lead.Id)).ReturnsAsync(lead);
        var service = BuildService();
        var result = await service.GenerateSalaryScriptAsync(lead.Id);
        result.Should().Contain("(Salario)");
    }

    [Fact]
    public async Task GenerateCheatSheetJsonAsync_StripsMarkdownFences()
    {
        var company = MakeCompany();
        var lead = MakeLead(company);
        _repo.Setup(r => r.GetByIdAsync(lead.Id)).ReturnsAsync(lead);
        var service = BuildService(new FencedJsonStrategy());
        var result = await service.GenerateCheatSheetJsonAsync(lead.Id);
        result.Should().NotContain("```");
        result.Should().Contain("techStack");
    }

    private sealed class FailingStrategy : IAiStrategy
    {
        public string ProviderName => "Mock";
        public Task<AiEvaluationResultDto> EvaluateJobAsync(string jobTitle, string companyName, string description)
            => throw new HttpRequestException("AI provider unavailable");
        public Task<string> EvaluateFastPromptAsync(string systemPrompt, string userPrompt)
            => throw new HttpRequestException("AI provider unavailable");
    }

    private sealed class FencedJsonStrategy : IAiStrategy
    {
        public string ProviderName => "Fenced";
        public Task<AiEvaluationResultDto> EvaluateJobAsync(string jobTitle, string companyName, string description)
            => Task.FromResult(new AiEvaluationResultDto(TargetRole.J2, 90, true, "", "msg", null));
        public Task<string> EvaluateFastPromptAsync(string systemPrompt, string userPrompt)
            => Task.FromResult("```json\n{\"techStack\": [\"C#\"], \"businessPains\": [\"p\"], \"questions\": [\"q\"]}\n```");
    }
}
