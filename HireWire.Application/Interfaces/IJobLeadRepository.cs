using HireWire.Domain.Entities;
using HireWire.Domain.Enums;

namespace HireWire.Application.Interfaces;

/// <summary>
/// Interface for repository operations related to Company and JobLead entities.
/// </summary>
public interface IJobLeadRepository
{
    Task<JobLead?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<JobLead>> GetAllAsync();
    Task<Company?> GetCompanyByNameAsync(string name);
    Task AddJobLeadAsync(JobLead jobLead);
    Task AddCompanyAsync(Company company);
    Task<JobLead?> GetJobLeadByUrlOrTitleAsync(string url, Guid companyId, string jobTitle);
    Task DeleteJobLeadAsync(Guid id);
    Task SaveOEConfigDirectAsync(Guid jobLeadId, TargetRole targetRole, double? aiMatchScore, bool isAsync, string? detectedRedFlags, string? outreachMessage = null, string? recruiterName = null, string? industry = null);
    Task SaveInterviewAssetDirectAsync(Guid jobLeadId, string cvRelativePath, string? cheatSheetData = null);
    Task SaveChangesAsync();
}
