using System;

namespace HireWire.Domain.Entities;

/// <summary>
/// Tracks outreach and follow-up communication with recruiters.
/// </summary>
public class Communication
{
    public Guid Id { get; private set; }
    public Guid JobLeadId { get; private set; }
    public string? ContactName { get; private set; }
    public string MessageBody { get; private set; }
    public DateTime SentDate { get; private set; }
    public bool RequiresFollowUp { get; private set; }
    public DateTime? FollowUpDate { get; private set; }

    // Navigation properties
    public virtual JobLead JobLead { get; private set; } = null!;

    // Required by Entity Framework Core
#pragma warning disable CS8618
    protected Communication() { }
#pragma warning restore CS8618

    public Communication(
        Guid id,
        Guid jobLeadId,
        string? contactName,
        string messageBody,
        DateTime sentDate,
        bool requiresFollowUp,
        DateTime? followUpDate)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;

        if (jobLeadId == Guid.Empty)
            throw new ArgumentException("Job Lead ID is required.", nameof(jobLeadId));
        JobLeadId = jobLeadId;

        if (string.IsNullOrWhiteSpace(messageBody))
            throw new ArgumentException("Message body cannot be empty.", nameof(messageBody));
        MessageBody = messageBody;

        ContactName = contactName;
        SentDate = sentDate;
        RequiresFollowUp = requiresFollowUp;
        FollowUpDate = followUpDate;
    }

    /// <summary>
    /// Updates the follow-up requirements.
    /// </summary>
    public void SetFollowUp(DateTime? followUpDate)
    {
        RequiresFollowUp = followUpDate.HasValue;
        FollowUpDate = followUpDate;
    }

    /// <summary>
    /// Updates the communication log content.
    /// </summary>
    public void UpdateMessage(string messageBody, string? contactName)
    {
        if (string.IsNullOrWhiteSpace(messageBody))
            throw new ArgumentException("Message body cannot be empty.", nameof(messageBody));
        
        MessageBody = messageBody;
        ContactName = contactName;
    }
}
