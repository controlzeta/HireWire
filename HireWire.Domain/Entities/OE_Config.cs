using HireWire.Domain.Enums;
using System;

namespace HireWire.Domain.Entities;

/// <summary>
/// Overemployment configuration for a Job Lead.
/// </summary>
public class OE_Config
{
    public Guid Id { get; private set; }
    public Guid JobLeadId { get; private set; }
    public TargetRole TargetRole { get; private set; }
    public double? AiMatchScore { get; private set; }
    public bool IsAsync { get; private set; }
    public string? DetectedRedFlags { get; private set; }

    // Navigation properties
    public virtual JobLead JobLead { get; private set; } = null!;

    // Required by Entity Framework Core
#pragma warning disable CS8618
    protected OE_Config() { }
#pragma warning restore CS8618

    public OE_Config(
        Guid id,
        Guid jobLeadId,
        TargetRole targetRole,
        double? aiMatchScore,
        bool isAsync,
        string? detectedRedFlags)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;

        if (jobLeadId == Guid.Empty)
            throw new ArgumentException("Job Lead ID is required.", nameof(jobLeadId));
        JobLeadId = jobLeadId;

        TargetRole = targetRole;
        AiMatchScore = aiMatchScore;
        IsAsync = isAsync;
        DetectedRedFlags = detectedRedFlags;
    }

    /// <summary>
    /// Updates the AI evaluation and Target Role routing logic.
    /// </summary>
    public void UpdateEvaluation(TargetRole targetRole, double? aiMatchScore, bool isAsync, string? detectedRedFlags)
    {
        TargetRole = targetRole;
        AiMatchScore = aiMatchScore;
        IsAsync = isAsync;
        DetectedRedFlags = detectedRedFlags;
    }
}
