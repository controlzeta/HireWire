using FluentAssertions;
using HireWire.Domain.Entities;
using HireWire.Domain.Enums;

namespace HireWire.Tests.Domain;

public class JobLeadTests
{
    private static JobLead MakeLead(Guid? id = null) => new(
        id ?? Guid.NewGuid(), Guid.NewGuid(), "Senior .NET Developer",
        "https://example.com/job/1", "LinkedIn", "Descripción del puesto", "$100k");

    [Fact]
    public void Constructor_ValidData_InitializesWithLeadExtraido()
    {
        var lead = MakeLead();
        lead.Status.Should().Be(JobLeadStatus.LeadExtraido);
        lead.SalaryRange.Should().Be("$100k");
    }

    [Fact]
    public void Constructor_EmptyCompanyId_Throws()
    {
        FluentActions.Invoking(() => new JobLead(Guid.NewGuid(), Guid.Empty, "Dev", "url", "LinkedIn", "desc", null))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyJobTitle_Throws(string title)
    {
        FluentActions.Invoking(() => new JobLead(Guid.NewGuid(), Guid.NewGuid(), title, "url", "LinkedIn", "desc", null))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyUrl_Throws(string url)
    {
        FluentActions.Invoking(() => new JobLead(Guid.NewGuid(), Guid.NewGuid(), "Dev", url, "LinkedIn", "desc", null))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyPlatform_Throws(string platform)
    {
        FluentActions.Invoking(() => new JobLead(Guid.NewGuid(), Guid.NewGuid(), "Dev", "url", platform, "desc", null))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyDescription_Throws(string desc)
    {
        FluentActions.Invoking(() => new JobLead(Guid.NewGuid(), Guid.NewGuid(), "Dev", "url", "LinkedIn", desc, null))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyId_GeneratesNewGuid()
    {
        MakeLead(Guid.Empty).Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void UpdateDetails_ValidData_UpdatesAllFields()
    {
        var lead = MakeLead();
        lead.UpdateDetails("Nueva Vacante", "https://nueva.url", "Manfred", "Nueva desc", null);
        lead.JobTitle.Should().Be("Nueva Vacante");
        lead.Url.Should().Be("https://nueva.url");
        lead.Platform.Should().Be("Manfred");
        lead.RawDescription.Should().Be("Nueva desc");
        lead.SalaryRange.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_EmptyTitle_Throws()
    {
        var lead = MakeLead();
        FluentActions.Invoking(() => lead.UpdateDetails("", "url", "LinkedIn", "desc", null))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChangeStatus_UpdatesStatus()
    {
        var lead = MakeLead();
        lead.ChangeStatus(JobLeadStatus.Descartado);
        lead.Status.Should().Be(JobLeadStatus.Descartado);
    }

    [Fact]
    public void SetOEConfig_Null_Throws()
    {
        var lead = MakeLead();
        FluentActions.Invoking(() => lead.SetOEConfig(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetOEConfig_Valid_SetsConfig()
    {
        var lead = MakeLead();
        var config = new OE_Config(Guid.NewGuid(), lead.Id, TargetRole.J2, 90, true, null);
        lead.SetOEConfig(config);
        lead.OE_Config.Should().BeSameAs(config);
    }

    [Fact]
    public void AddCommunication_DuplicateId_IsIgnored()
    {
        var lead = MakeLead();
        var comm = new Communication(Guid.NewGuid(), lead.Id, "Sofia", "Hola", DateTime.UtcNow, false, null);
        lead.AddCommunication(comm);
        lead.AddCommunication(comm);
        lead.Communications.Should().HaveCount(1);
    }

    [Fact]
    public void AddCommunication_Null_Throws()
    {
        var lead = MakeLead();
        FluentActions.Invoking(() => lead.AddCommunication(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddInterviewAsset_DuplicateId_IsIgnored()
    {
        var lead = MakeLead();
        var asset = new InterviewAsset(Guid.NewGuid(), lead.Id, "cvs/x.pdf", null, null);
        lead.AddInterviewAsset(asset);
        lead.AddInterviewAsset(asset);
        lead.InterviewAssets.Should().HaveCount(1);
    }
}

public class CompanyTests
{
    [Fact]
    public void Constructor_ValidData_SetsProperties()
    {
        var company = new Company(Guid.NewGuid(), "Acme", "Fintech", "ctx");
        company.Name.Should().Be("Acme");
        company.Industry.Should().Be("Fintech");
        company.ScrapedContext.Should().Be("ctx");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string name)
    {
        FluentActions.Invoking(() => new Company(Guid.NewGuid(), name, null, null))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyId_GeneratesGuid()
    {
        new Company(Guid.Empty, "Acme", null, null).Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void UpdateDetails_EmptyName_Throws()
    {
        var company = new Company(Guid.NewGuid(), "Acme", null, null);
        FluentActions.Invoking(() => company.UpdateDetails(" ", "Fintech", "ctx"))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddJobLead_DuplicateId_IsIgnored()
    {
        var company = new Company(Guid.NewGuid(), "Acme", null, null);
        var lead = new JobLead(Guid.NewGuid(), company.Id, "Dev", "url", "LinkedIn", "desc", null);
        company.AddJobLead(lead);
        company.AddJobLead(lead);
        company.JobLeads.Should().HaveCount(1);
    }

    [Fact]
    public void AddJobLead_Null_Throws()
    {
        var company = new Company(Guid.NewGuid(), "Acme", null, null);
        FluentActions.Invoking(() => company.AddJobLead(null!)).Should().Throw<ArgumentNullException>();
    }
}

public class OEConfigTests
{
    [Fact]
    public void Constructor_EmptyJobLeadId_Throws()
    {
        FluentActions.Invoking(() => new OE_Config(Guid.NewGuid(), Guid.Empty, TargetRole.J2, 90, true, null))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateEvaluation_UpdatesAllFields()
    {
        var config = new OE_Config(Guid.NewGuid(), Guid.NewGuid(), TargetRole.J2, 90, true, "none");
        config.UpdateEvaluation(TargetRole.J1, 70, false, "riesgo");
        config.TargetRole.Should().Be(TargetRole.J1);
        config.AiMatchScore.Should().Be(70);
        config.IsAsync.Should().BeFalse();
        config.DetectedRedFlags.Should().Be("riesgo");
    }
}

public class CommunicationTests
{
    [Fact]
    public void Constructor_EmptyJobLeadId_Throws()
    {
        FluentActions.Invoking(() => new Communication(Guid.NewGuid(), Guid.Empty, "x", "msg", DateTime.UtcNow, false, null))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyMessage_Throws(string msg)
    {
        FluentActions.Invoking(() => new Communication(Guid.NewGuid(), Guid.NewGuid(), "x", msg, DateTime.UtcNow, false, null))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetFollowUp_WithDate_EnablesFlag()
    {
        var comm = new Communication(Guid.NewGuid(), Guid.NewGuid(), "x", "msg", DateTime.UtcNow, false, null);
        var date = DateTime.UtcNow.AddDays(3);
        comm.SetFollowUp(date);
        comm.RequiresFollowUp.Should().BeTrue();
        comm.FollowUpDate.Should().Be(date);
    }

    [Fact]
    public void SetFollowUp_Null_DisablesFlag()
    {
        var comm = new Communication(Guid.NewGuid(), Guid.NewGuid(), "x", "msg", DateTime.UtcNow, true, DateTime.UtcNow.AddDays(3));
        comm.SetFollowUp(null);
        comm.RequiresFollowUp.Should().BeFalse();
        comm.FollowUpDate.Should().BeNull();
    }

    [Fact]
    public void UpdateMessage_EmptyMessage_Throws()
    {
        var comm = new Communication(Guid.NewGuid(), Guid.NewGuid(), "x", "msg", DateTime.UtcNow, false, null);
        FluentActions.Invoking(() => comm.UpdateMessage("", "y")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateMessage_Valid_Updates()
    {
        var comm = new Communication(Guid.NewGuid(), Guid.NewGuid(), "x", "msg", DateTime.UtcNow, false, null);
        comm.UpdateMessage("nuevo", "Sofia");
        comm.MessageBody.Should().Be("nuevo");
        comm.ContactName.Should().Be("Sofia");
    }
}

public class InterviewAssetTests
{
    [Fact]
    public void Constructor_EmptyJobLeadId_Throws()
    {
        FluentActions.Invoking(() => new InterviewAsset(Guid.NewGuid(), Guid.Empty, null, null, null))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateAssets_SetsPaths()
    {
        var asset = new InterviewAsset(Guid.NewGuid(), Guid.NewGuid(), null, null, null);
        asset.UpdateAssets("cvs/x.pdf", "{\"a\":1}");
        asset.DynamicCvPath.Should().Be("cvs/x.pdf");
        asset.CheatSheetData.Should().Be("{\"a\":1}");
    }

    [Fact]
    public void ScheduleInterview_SetsDate()
    {
        var asset = new InterviewAsset(Guid.NewGuid(), Guid.NewGuid(), null, null, null);
        var date = DateTime.UtcNow.AddDays(5);
        asset.ScheduleInterview(date);
        asset.ScheduledDate.Should().Be(date);
    }
}
