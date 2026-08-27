using System;
using System.Collections.Generic;
using System.Linq;

namespace HireWire.Domain.Entities;

/// <summary>
/// Represents a company scraped or created in the system.
/// </summary>
public class Company
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Industry { get; private set; }
    public string? ScrapedContext { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private readonly List<JobLead> _jobLeads = [];
    public virtual IReadOnlyCollection<JobLead> JobLeads => _jobLeads.AsReadOnly();

    // Required by Entity Framework Core
#pragma warning disable CS8618
    protected Company() { }
#pragma warning restore CS8618

    public Company(Guid id, string name, string? industry, string? scrapedContext)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name cannot be empty.", nameof(name));
        Name = name;

        Industry = industry;
        ScrapedContext = scrapedContext;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the company details.
    /// </summary>
    public void UpdateDetails(string name, string? industry, string? scrapedContext)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name cannot be empty.", nameof(name));
        
        Name = name;
        Industry = industry;
        ScrapedContext = scrapedContext;
    }

    /// <summary>
    /// Adds a new Job Lead associated with this company.
    /// </summary>
    public void AddJobLead(JobLead jobLead)
    {
        ArgumentNullException.ThrowIfNull(jobLead);
        if (_jobLeads.Any(j => j.Id == jobLead.Id))
            return;
        
        _jobLeads.Add(jobLead);
    }
}
