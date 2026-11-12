using FluentAssertions;
using HireWire.Application.Interfaces;
using HireWire.Domain.Entities;
using HireWire.Domain.Enums;
using HireWire.Infrastructure.Data;
using HireWire.Infrastructure.Repositories;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HireWire.Tests.Infrastructure;

public sealed class JobLeadRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly HireWireDbContext _db;
    private readonly IJobLeadRepository _repo;

    public JobLeadRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<HireWireDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new HireWireDbContext(options);
        _db.Database.EnsureCreated();
        _repo = new JobLeadRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<Company> SeedCompanyAsync(string name = "Acme Corp")
    {
        var company = new Company(Guid.NewGuid(), name, "Fintech", "ctx");
        await _repo.AddCompanyAsync(company);
        await _repo.SaveChangesAsync();
        return company;
    }

    private async Task<JobLead> SeedLeadAsync(Company company, JobLeadStatus status = JobLeadStatus.LeadExtraido)
    {
        var lead = new JobLead(Guid.NewGuid(), company.Id, "Senior .NET Developer",
            "https://example.com/job", "LinkedIn", "Descripción de la vacante", "$100k");
        lead.ChangeStatus(status);
        await _repo.AddJobLeadAsync(lead);
        await _repo.SaveChangesAsync();
        return lead;
    }

    [Fact]
    public async Task GetCompanyByNameAsync_CaseInsensitive_FindsCompany()
    {
        await SeedCompanyAsync("Acme Corp");
        (await _repo.GetCompanyByNameAsync("ACME CORP")).Should().NotBeNull();
        (await _repo.GetCompanyByNameAsync("acme corp")).Should().NotBeNull();
    }

    [Fact]
    public async Task GetCompanyByNameAsync_NotFound_ReturnsNull()
    {
        (await _repo.GetCompanyByNameAsync("NoExiste")).Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsLeadWithRelations()
    {
        var company = await SeedCompanyAsync();
        var lead = await SeedLeadAsync(company);

        var loaded = await _repo.GetByIdAsync(lead.Id);
        loaded.Should().NotBeNull();
        loaded!.Company.Name.Should().Be("Acme Corp");
        loaded.Communications.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        (await _repo.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ExcludesDescartado()
    {
        var company = await SeedCompanyAsync();
        await SeedLeadAsync(company);
        var discarded = await SeedLeadAsync(company);
        await _repo.DeleteJobLeadAsync(discarded.Id);

        var all = await _repo.GetAllAsync();
        all.Should().HaveCount(1);
        all.Should().NotContain(l => l.Id == discarded.Id);
    }

    [Fact]
    public async Task DeleteJobLeadAsync_SoftDeletes()
    {
        var company = await SeedCompanyAsync();
        var lead = await SeedLeadAsync(company);
        await _repo.DeleteJobLeadAsync(lead.Id);

        var loaded = await _repo.GetByIdAsync(lead.Id);
        loaded!.Status.Should().Be(JobLeadStatus.Descartado);
    }

    [Fact]
    public async Task DeleteJobLeadAsync_NotFound_DoesNotThrow()
    {
        await FluentActions.Invoking(() => _repo.DeleteJobLeadAsync(Guid.NewGuid()))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetJobLeadByUrlOrTitleAsync_MatchesByUrl()
    {
        var company = await SeedCompanyAsync();
        var lead = await SeedLeadAsync(company);
        var found = await _repo.GetJobLeadByUrlOrTitleAsync(lead.Url, Guid.NewGuid(), "Otro Título");
        found!.Id.Should().Be(lead.Id);
    }

    [Fact]
    public async Task GetJobLeadByUrlOrTitleAsync_MatchesByCompanyAndTitle_CaseInsensitive()
    {
        var company = await SeedCompanyAsync();
        var lead = await SeedLeadAsync(company);
        var found = await _repo.GetJobLeadByUrlOrTitleAsync("https://otra.url", company.Id, "SENIOR .NET DEVELOPER");
        found!.Id.Should().Be(lead.Id);
    }

    [Fact]
    public async Task GetJobLeadByUrlOrTitleAsync_NoMatch_ReturnsNull()
    {
        var company = await SeedCompanyAsync();
        await SeedLeadAsync(company);
        (await _repo.GetJobLeadByUrlOrTitleAsync("https://otra.url", Guid.NewGuid(), "Otro"))
            .Should().BeNull();
    }

    [Fact]
    public async Task SaveOEConfigDirectAsync_NotFound_ThrowsKeyNotFound()
    {
        await FluentActions.Invoking(() =>
                _repo.SaveOEConfigDirectAsync(Guid.NewGuid(), TargetRole.J2, 90, true, null))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task SaveOEConfigDirectAsync_CreatesConfig_AndAdvancesStatus()
    {
        var company = await SeedCompanyAsync();
        var lead = await SeedLeadAsync(company);

        await _repo.SaveOEConfigDirectAsync(lead.Id, TargetRole.J2, 93, true, "sin flags");

        var loaded = await _repo.GetByIdAsync(lead.Id);
        loaded!.Status.Should().Be(JobLeadStatus.EvaluadoPorIa);
        loaded.OE_Config.Should().NotBeNull();
        loaded.OE_Config!.TargetRole.Should().Be(TargetRole.J2);
        loaded.OE_Config.AiMatchScore.Should().Be(93);
    }

    [Fact]
    public async Task SaveOEConfigDirectAsync_SecondCall_UpdatesExistingConfig_DoesNotDuplicate()
    {
        var company = await SeedCompanyAsync();
        var lead = await SeedLeadAsync(company);

        await _repo.SaveOEConfigDirectAsync(lead.Id, TargetRole.J2, 93, true, "flags-v1");
        await _repo.SaveOEConfigDirectAsync(lead.Id, TargetRole.J1, 80, false, "flags-v2");

        var count = await _db.OE_Configs.CountAsync(c => c.JobLeadId == lead.Id);
        count.Should().Be(1);
        var loaded = await _repo.GetByIdAsync(lead.Id);
        loaded!.OE_Config!.TargetRole.Should().Be(TargetRole.J1);
        loaded.OE_Config.DetectedRedFlags.Should().Be("flags-v2");
    }

    [Fact]
    public async Task SaveOEConfigDirectAsync_WithOutreachMessage_AddsCommunication()
    {
        var company = await SeedCompanyAsync();
        var lead = await SeedLeadAsync(company);

        await _repo.SaveOEConfigDirectAsync(lead.Id, TargetRole.J2, 90, true, null,
            outreachMessage: "Hola Jeff, te escribo...", recruiterName: "Jeff Schneider");

        var loaded = await _repo.GetByIdAsync(lead.Id);
        loaded!.Communications.Should().ContainSingle();
        loaded.Communications.Single().ContactName.Should().Be("Jeff Schneider");
        loaded.Communications.Single().MessageBody.Should().Contain("Hola Jeff");
    }

    [Fact]
    public async Task SaveOEConfigDirectAsync_WithIndustry_UpdatesCompanyIndustry()
    {
        var company = await SeedCompanyAsync();
        var lead = await SeedLeadAsync(company);

        await _repo.SaveOEConfigDirectAsync(lead.Id, TargetRole.J2, 90, true, null, industry: "Defensa & Aeroespacial");

        var loaded = await _repo.GetByIdAsync(lead.Id);
        loaded!.Company.Industry.Should().Be("Defensa & Aeroespacial");
    }

    [Fact]
    public async Task SaveInterviewAssetDirectAsync_NotFound_ThrowsKeyNotFound()
    {
        await FluentActions.Invoking(() =>
                _repo.SaveInterviewAssetDirectAsync(Guid.NewGuid(), "cvs/x.pdf"))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task SaveInterviewAssetDirectAsync_CreatesAsset_AndAdvancesStatusToCvGenerado()
    {
        var company = await SeedCompanyAsync();
        var lead = await SeedLeadAsync(company, JobLeadStatus.EvaluadoPorIa);

        await _repo.SaveInterviewAssetDirectAsync(lead.Id, "cvs/x.pdf", "{\"techStack\":[]}");

        var loaded = await _repo.GetByIdAsync(lead.Id);
        loaded!.Status.Should().Be(JobLeadStatus.CvGenerado);
        loaded.InterviewAssets.Should().ContainSingle();
        loaded.InterviewAssets.Single().DynamicCvPath.Should().Be("cvs/x.pdf");
    }

    [Fact]
    public async Task SaveInterviewAssetDirectAsync_SecondCall_UpdatesSameAsset()
    {
        var company = await SeedCompanyAsync();
        var lead = await SeedLeadAsync(company, JobLeadStatus.EvaluadoPorIa);

        await _repo.SaveInterviewAssetDirectAsync(lead.Id, "cvs/v1.pdf");
        await _repo.SaveInterviewAssetDirectAsync(lead.Id, "cvs/v2.pdf", "{\"updated\":true}");

        var count = await _db.InterviewAssets.CountAsync(a => a.JobLeadId == lead.Id);
        count.Should().Be(1);
        var loaded = await _repo.GetByIdAsync(lead.Id);
        loaded!.InterviewAssets.Single().DynamicCvPath.Should().Be("cvs/v2.pdf");
        loaded.InterviewAssets.Single().CheatSheetData.Should().Be("{\"updated\":true}");
    }
}
