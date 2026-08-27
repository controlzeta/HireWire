using System;

namespace HireWire.Domain.Entities;

/// <summary>
/// Stores generated interview material (CVs, study sheets, calendar).
/// </summary>
public class InterviewAsset
{
    public Guid Id { get; private set; }
    public Guid JobLeadId { get; private set; }
    public string? DynamicCvPath { get; private set; }
    public string? CheatSheetData { get; private set; } // Expected to hold serialized JSON
    public DateTime? ScheduledDate { get; private set; }

    // Navigation properties
    public virtual JobLead JobLead { get; private set; } = null!;

    // Required by Entity Framework Core
#pragma warning disable CS8618
    protected InterviewAsset() { }
#pragma warning restore CS8618

    public InterviewAsset(
        Guid id,
        Guid jobLeadId,
        string? dynamicCvPath,
        string? cheatSheetData,
        DateTime? scheduledDate)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;

        if (jobLeadId == Guid.Empty)
            throw new ArgumentException("Job Lead ID is required.", nameof(jobLeadId));
        JobLeadId = jobLeadId;

        DynamicCvPath = dynamicCvPath;
        CheatSheetData = cheatSheetData;
        ScheduledDate = scheduledDate;
    }

    /// <summary>
    /// Updates the dynamic CV path and the cheat sheet content.
    /// </summary>
    public void UpdateAssets(string? dynamicCvPath, string? cheatSheetData)
    {
        DynamicCvPath = dynamicCvPath;
        CheatSheetData = cheatSheetData;
    }

    /// <summary>
    /// Schedules or reschedules the interview date.
    /// </summary>
    public void ScheduleInterview(DateTime? scheduledDate)
    {
        ScheduledDate = scheduledDate;
    }
}
