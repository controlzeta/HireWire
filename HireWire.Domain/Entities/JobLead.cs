using HireWire.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HireWire.Domain.Entities;

/// <summary>
/// Represents a job lead (vacante) in the pipeline.
/// </summary>
public class JobLead
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string JobTitle { get; private set; }
    public string Url { get; private set; }
    public string Platform { get; private set; }
    public string RawDescription { get; private set; }
    public string? SalaryRange { get; private set; }
    public JobLeadStatus Status { get; private set; } = JobLeadStatus.LeadExtraido;

    // Navigation properties
    public virtual Company Company { get; private set; } = null!;
    public virtual OE_Config? OE_Config { get; private set; }
    
    private readonly List<Communication> _communications = [];
    public virtual IReadOnlyCollection<Communication> Communications => _communications.AsReadOnly();
    
    private readonly List<InterviewAsset> _interviewAssets = [];
    public virtual IReadOnlyCollection<InterviewAsset> InterviewAssets => _interviewAssets.AsReadOnly();

    // Required by Entity Framework Core
#pragma warning disable CS8618
    protected JobLead() { }
#pragma warning restore CS8618

    public JobLead(
        Guid id,
        Guid companyId,
        string jobTitle,
        string url,
        string platform,
        string rawDescription,
        string? salaryRange)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;

        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID is required.", nameof(companyId));
        CompanyId = companyId;

        if (string.IsNullOrWhiteSpace(jobTitle))
            throw new ArgumentException("Job title cannot be empty.", nameof(jobTitle));
        JobTitle = jobTitle;

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));
        Url = url;

        if (string.IsNullOrWhiteSpace(platform))
            throw new ArgumentException("Platform cannot be empty.", nameof(platform));
        Platform = platform;

        if (string.IsNullOrWhiteSpace(rawDescription))
            throw new ArgumentException("Raw description cannot be empty.", nameof(rawDescription));
        RawDescription = rawDescription;

        SalaryRange = salaryRange;
        Status = JobLeadStatus.LeadExtraido;
    }

    /// <summary>
    /// Changes the status of the Job Lead in the Kanban funnel.
    /// </summary>
    public void ChangeStatus(JobLeadStatus newStatus)
    {
        Status = newStatus;
    }

    /// <summary>
    /// Updates the job lead core details.
    /// </summary>
    public void UpdateDetails(string jobTitle, string url, string platform, string rawDescription, string? salaryRange)
    {
        if (string.IsNullOrWhiteSpace(jobTitle)) throw new ArgumentException("Job title cannot be empty.", nameof(jobTitle));
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL cannot be empty.", nameof(url));
        if (string.IsNullOrWhiteSpace(platform)) throw new ArgumentException("Platform cannot be empty.", nameof(platform));
        if (string.IsNullOrWhiteSpace(rawDescription)) throw new ArgumentException("Raw description cannot be empty.", nameof(rawDescription));

        JobTitle = jobTitle;
        Url = url;
        Platform = platform;
        RawDescription = rawDescription;
        SalaryRange = salaryRange;
    }

    /// <summary>
    /// Associates an Overemployment configuration to this job lead.
    /// </summary>
    public void SetOEConfig(OE_Config config)
    {
        ArgumentNullException.ThrowIfNull(config);
        OE_Config = config;
    }

    /// <summary>
    /// Adds a recruitment communication record.
    /// </summary>
    public void AddCommunication(Communication communication)
    {
        ArgumentNullException.ThrowIfNull(communication);
        if (_communications.Any(c => c.Id == communication.Id))
            return;
        _communications.Add(communication);
    }

    /// <summary>
    /// Adds an interview asset (CV, cheat sheet).
    /// </summary>
    public void AddInterviewAsset(InterviewAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (_interviewAssets.Any(a => a.Id == asset.Id))
            return;
        _interviewAssets.Add(asset);
    }
}
