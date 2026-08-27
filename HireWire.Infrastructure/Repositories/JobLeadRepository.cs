using Microsoft.EntityFrameworkCore;
using HireWire.Application.Interfaces;
using HireWire.Domain.Entities;
using HireWire.Domain.Enums;
using HireWire.Infrastructure.Data;

namespace HireWire.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the IJobLeadRepository.
/// </summary>
public class JobLeadRepository(HireWireDbContext dbContext) : IJobLeadRepository
{
    public async Task<JobLead?> GetByIdAsync(Guid id)
    {
        dbContext.ChangeTracker.Clear();
        return await dbContext.JobLeads
            .Include(j => j.Company)
            .Include(j => j.OE_Config)
            .Include(j => j.Communications)
            .Include(j => j.InterviewAssets)
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<IReadOnlyList<JobLead>> GetAllAsync()
    {
        dbContext.ChangeTracker.Clear();
        return await dbContext.JobLeads
            .Include(j => j.Company)
            .Include(j => j.OE_Config)
            .Where(j => j.Status != JobLeadStatus.Descartado)
            .ToListAsync();
    }

    public async Task<Company?> GetCompanyByNameAsync(string name)
    {
        return await dbContext.Companies
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
    }

    public async Task AddJobLeadAsync(JobLead jobLead)
    {
        await dbContext.JobLeads.AddAsync(jobLead);
    }

    public async Task AddCompanyAsync(Company company)
    {
        await dbContext.Companies.AddAsync(company);
    }

    public async Task<JobLead?> GetJobLeadByUrlOrTitleAsync(string url, Guid companyId, string jobTitle)
    {
        var lowerTitle = jobTitle.ToLower();
        return await dbContext.JobLeads
            .AsNoTracking()
            .Include(j => j.Company)
            .Include(j => j.OE_Config)
            .FirstOrDefaultAsync(j => j.Url == url || (j.CompanyId == companyId && j.JobTitle.ToLower() == lowerTitle));
    }

    public async Task DeleteJobLeadAsync(Guid id)
    {
        dbContext.ChangeTracker.Clear();
        var lead = await dbContext.JobLeads.FirstOrDefaultAsync(j => j.Id == id);
        if (lead != null)
        {
            lead.ChangeStatus(JobLeadStatus.Descartado);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task SaveOEConfigDirectAsync(
        Guid jobLeadId, 
        TargetRole targetRole, 
        double? aiMatchScore, 
        bool isAsync, 
        string? detectedRedFlags,
        string? outreachMessage = null,
        string? recruiterName = null,
        string? industry = null)
    {
        // Wipes any stale tracked entities from long-lived Blazor SignalR circuit scope
        dbContext.ChangeTracker.Clear();

        var jobLead = await dbContext.JobLeads
            .Include(j => j.Company)
            .Include(j => j.OE_Config)
            .Include(j => j.Communications)
            .FirstOrDefaultAsync(j => j.Id == jobLeadId)
            ?? throw new KeyNotFoundException($"JobLead with ID {jobLeadId} not found.");

        if (!string.IsNullOrWhiteSpace(industry))
        {
            jobLead.Company.UpdateDetails(jobLead.Company.Name, industry, jobLead.Company.ScrapedContext);
            dbContext.Entry(jobLead.Company).State = EntityState.Modified;
        }

        var existingConfigInDb = await dbContext.OE_Configs
            .FirstOrDefaultAsync(c => c.JobLeadId == jobLeadId);

        if (existingConfigInDb == null)
        {
            if (jobLead.OE_Config != null)
            {
                dbContext.Entry(jobLead.OE_Config).State = EntityState.Detached;
            }

            var newConfig = new OE_Config(
                Guid.NewGuid(),
                jobLeadId,
                targetRole,
                aiMatchScore,
                isAsync,
                detectedRedFlags
            );
            await dbContext.OE_Configs.AddAsync(newConfig);
            dbContext.Entry(newConfig).State = EntityState.Added;
            jobLead.SetOEConfig(newConfig);
        }
        else
        {
            existingConfigInDb.UpdateEvaluation(targetRole, aiMatchScore, isAsync, detectedRedFlags);
            dbContext.Entry(existingConfigInDb).State = EntityState.Modified;
            jobLead.SetOEConfig(existingConfigInDb);
        }

        if (!string.IsNullOrEmpty(outreachMessage))
        {
            var comm = new Communication(
                Guid.NewGuid(),
                jobLeadId,
                recruiterName ?? "Reclutador",
                outreachMessage,
                DateTime.UtcNow,
                requiresFollowUp: false,
                followUpDate: null
            );
            await dbContext.Communications.AddAsync(comm);
            dbContext.Entry(comm).State = EntityState.Added;
            jobLead.AddCommunication(comm);
        }

        if (jobLead.Status == JobLeadStatus.LeadExtraido)
        {
            jobLead.ChangeStatus(JobLeadStatus.EvaluadoPorIa);
        }
        dbContext.Entry(jobLead).State = EntityState.Modified;

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Resolve optimistic concurrency exception by re-fetching database values for modified entries
            foreach (var entry in dbContext.ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Modified)
                {
                    var databaseValues = await entry.GetDatabaseValuesAsync();
                    if (databaseValues != null)
                    {
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                }
            }
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task SaveInterviewAssetDirectAsync(Guid jobLeadId, string cvRelativePath, string? cheatSheetData = null)
    {
        // Wipes any stale tracked entities from long-lived Blazor SignalR circuit scope
        dbContext.ChangeTracker.Clear();

        var jobLead = await dbContext.JobLeads
            .Include(j => j.InterviewAssets)
            .FirstOrDefaultAsync(j => j.Id == jobLeadId)
            ?? throw new KeyNotFoundException($"JobLead with ID {jobLeadId} not found.");

        var existingAssetInDb = await dbContext.InterviewAssets
            .FirstOrDefaultAsync(a => a.JobLeadId == jobLeadId);

        if (existingAssetInDb == null)
        {
            var newAsset = new InterviewAsset(
                Guid.NewGuid(),
                jobLeadId,
                cvRelativePath,
                cheatSheetData,
                scheduledDate: null
            );
            await dbContext.InterviewAssets.AddAsync(newAsset);
            dbContext.Entry(newAsset).State = EntityState.Added;
            jobLead.AddInterviewAsset(newAsset);
        }
        else
        {
            existingAssetInDb.UpdateAssets(cvRelativePath, cheatSheetData ?? existingAssetInDb.CheatSheetData);
            dbContext.Entry(existingAssetInDb).State = EntityState.Modified;
        }

        if (jobLead.Status == JobLeadStatus.LeadExtraido || jobLead.Status == JobLeadStatus.EvaluadoPorIa)
        {
            jobLead.ChangeStatus(JobLeadStatus.CvGenerado);
            dbContext.Entry(jobLead).State = EntityState.Modified;
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            foreach (var entry in dbContext.ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Modified)
                {
                    var databaseValues = await entry.GetDatabaseValuesAsync();
                    if (databaseValues != null)
                    {
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                }
            }
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}
