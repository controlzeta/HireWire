using HireWire.Application.DTOs;
using HireWire.Domain.Entities;
using HireWire.Domain.Enums;

namespace HireWire.Application.Interfaces;

/// <summary>
/// Service interface for processing job leads.
/// </summary>
public interface IJobLeadService
{
    Task<Guid> ProcessScrapedJobLeadAsync(ScrapedJobLeadDto dto);
    Task<IReadOnlyList<JobLead>> GetJobLeadsAsync();
    Task<JobLead?> GetJobLeadByIdAsync(Guid id);
    Task SaveOEConfigAsync(Guid jobLeadId, TargetRole targetRole, double? aiMatchScore, bool isAsync, string? detectedRedFlags);
    Task<AiEvaluationResultDto> EvaluateLeadWithAiAsync(Guid jobLeadId);
    string GetManualAiPrompt(JobLead lead);
    Task ChangeJobLeadStatusAsync(Guid id, JobLeadStatus newStatus);
    Task DeleteJobLeadAsync(Guid id);
}
