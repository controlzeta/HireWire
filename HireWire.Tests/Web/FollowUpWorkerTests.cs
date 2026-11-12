using FluentAssertions;
using HireWire.Application.Interfaces;
using HireWire.Domain.Entities;
using HireWire.Domain.Enums;
using HireWire.Web.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HireWire.Tests.Web;

public class FollowUpWorkerTests
{
    private static JobLead MakeLead(JobLeadStatus status)
    {
        var companyId = Guid.NewGuid();
        var lead = new JobLead(Guid.NewGuid(), companyId, "Senior .NET Developer",
            "https://example.com/job", "LinkedIn", "Descripción de la vacante", null);
        lead.ChangeStatus(status);
        return lead;
    }

    private static IServiceScopeFactory BuildScopeFactory(IJobLeadRepository repo)
    {
        var services = new ServiceCollection();
        services.AddSingleton(repo);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task Worker_FlagsStagnantContactadoLead_ForFollowUp()
    {
        var lead = MakeLead(JobLeadStatus.Contactado);
        var oldComm = new Communication(Guid.NewGuid(), lead.Id, "Jeff", "Hola",
            DateTime.UtcNow.AddDays(-5), requiresFollowUp: false, followUpDate: null);
        lead.AddCommunication(oldComm);

        var repo = new Mock<IJobLeadRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<JobLead> { lead });
        repo.Setup(r => r.GetByIdAsync(lead.Id)).ReturnsAsync(lead);
        repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var worker = new FollowUpWorker(BuildScopeFactory(repo.Object), NullLogger<FollowUpWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(cts.Token);
        // Give the first cycle time to run (it then sleeps 12h, so cancellation stops it)
        await Task.Delay(500);
        await worker.StopAsync(CancellationToken.None);

        oldComm.RequiresFollowUp.Should().BeTrue();
        oldComm.FollowUpDate.Should().NotBeNull();
        repo.Verify(r => r.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Worker_RecentCommunication_NotFlagged()
    {
        var lead = MakeLead(JobLeadStatus.Contactado);
        var recentComm = new Communication(Guid.NewGuid(), lead.Id, "Jeff", "Hola",
            DateTime.UtcNow.AddDays(-1), requiresFollowUp: false, followUpDate: null);
        lead.AddCommunication(recentComm);

        var repo = new Mock<IJobLeadRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<JobLead> { lead });
        repo.Setup(r => r.GetByIdAsync(lead.Id)).ReturnsAsync(lead);
        repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var worker = new FollowUpWorker(BuildScopeFactory(repo.Object), NullLogger<FollowUpWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(cts.Token);
        await Task.Delay(500);
        await worker.StopAsync(CancellationToken.None);

        recentComm.RequiresFollowUp.Should().BeFalse();
    }

    [Fact]
    public async Task Worker_NonContactadoLead_NotFlagged()
    {
        var lead = MakeLead(JobLeadStatus.EvaluadoPorIa);
        var oldComm = new Communication(Guid.NewGuid(), lead.Id, "Jeff", "Hola",
            DateTime.UtcNow.AddDays(-10), requiresFollowUp: false, followUpDate: null);
        lead.AddCommunication(oldComm);

        var repo = new Mock<IJobLeadRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<JobLead> { lead });
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(lead);
        repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var worker = new FollowUpWorker(BuildScopeFactory(repo.Object), NullLogger<FollowUpWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(cts.Token);
        await Task.Delay(500);
        await worker.StopAsync(CancellationToken.None);

        oldComm.RequiresFollowUp.Should().BeFalse();
        repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Worker_RepositoryThrows_LogsAndContinues()
    {
        var repo = new Mock<IJobLeadRepository>();
        repo.Setup(r => r.GetAllAsync()).ThrowsAsync(new InvalidOperationException("db down"));

        var worker = new FollowUpWorker(BuildScopeFactory(repo.Object), NullLogger<FollowUpWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(cts.Token);
        await Task.Delay(500);
        // Should not crash the host; worker swallows the error and keeps looping
        var stop = FluentActions.Awaiting(() => worker.StopAsync(CancellationToken.None));
        await stop.Should().NotThrowAsync();
    }
}
