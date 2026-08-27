using HireWire.Application.DTOs;
using HireWire.Application.Interfaces;
using HireWire.Domain.Entities;
using HireWire.Domain.Enums;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HireWire.Application.Services;

/// <summary>
/// Domain-driven service implementation handling job lead use cases.
/// </summary>
public class JobLeadService(
    IJobLeadRepository repository, 
    AiServiceFactory aiServiceFactory, 
    IConfiguration configuration,
    ILogger<JobLeadService> logger) : IJobLeadService
{
    private string GetCandidateName()
    {
        var name = configuration["UserProfile:FullName"];
        return !string.IsNullOrWhiteSpace(name) ? name : "Francisco Arroyo";
    }
    public async Task<Guid> ProcessScrapedJobLeadAsync(ScrapedJobLeadDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // 1. Resolve Company (case-insensitive lookup)
        var company = await repository.GetCompanyByNameAsync(dto.CompanyName);
        if (company == null)
        {
            company = new Company(
                Guid.NewGuid(), 
                dto.CompanyName, 
                dto.Industry, 
                dto.CompanyContext
            );
            await repository.AddCompanyAsync(company);
            await repository.SaveChangesAsync();
        }
        else
        {
            // Enrich details if they were null
            company.UpdateDetails(
                company.Name, 
                dto.Industry ?? company.Industry, 
                dto.CompanyContext ?? company.ScrapedContext
            );
            await repository.SaveChangesAsync();
        }

        // 2. Check for duplicate lead
        var existingLead = await repository.GetJobLeadByUrlOrTitleAsync(dto.Url, company.Id, dto.JobTitle);
        if (existingLead != null)
        {
            // Update existing lead details instead of creating a duplicate
            existingLead.UpdateDetails(dto.JobTitle, dto.Url, dto.Platform, dto.RawDescription, dto.SalaryRange ?? existingLead.SalaryRange);
            await repository.SaveChangesAsync();
            return existingLead.Id;
        }

        // 3. Instantiate and add the Job Lead
        var jobLead = new JobLead(
            Guid.NewGuid(),
            company.Id,
            dto.JobTitle,
            dto.Url,
            dto.Platform,
            dto.RawDescription,
            dto.SalaryRange
        );

        await repository.AddJobLeadAsync(jobLead);
        company.AddJobLead(jobLead);

        // 4. Persist initial transaction
        await repository.SaveChangesAsync();

        // 4. Automatic AI evaluation in Phase 2
        try
        {
            var aiStrategy = aiServiceFactory.GetActiveStrategy();
            var aiResult = await aiStrategy.EvaluateJobAsync(jobLead.JobTitle, company.Name, jobLead.RawDescription);

            // Enrich company industry from AI inference if available
            if (!string.IsNullOrWhiteSpace(aiResult.Industry))
            {
                company.UpdateDetails(company.Name, aiResult.Industry, company.ScrapedContext);
            }

            // Create and associate OE Config
            var oeConfig = new OE_Config(
                Guid.NewGuid(),
                jobLead.Id,
                aiResult.TargetRole,
                aiResult.AiMatchScore,
                aiResult.IsAsync,
                aiResult.DetectedRedFlags
            );
            jobLead.SetOEConfig(oeConfig);

            // Determine recruiter name (scraped recruiter name takes highest priority)
            string recruiterName = !string.IsNullOrWhiteSpace(dto.RecruiterName)
                ? dto.RecruiterName
                : (!string.IsNullOrWhiteSpace(aiResult.RecruiterName) ? aiResult.RecruiterName : "Reclutador");

            string outreachMsg = aiResult.OutreachMessage;
            if (!string.IsNullOrWhiteSpace(dto.RecruiterName))
            {
                if (outreachMsg.Contains("Hola Sofía"))
                {
                    outreachMsg = outreachMsg.Replace("Hola Sofía", $"Hola {dto.RecruiterName}");
                }
                else if (outreachMsg.Contains("Hola Reclutador"))
                {
                    outreachMsg = outreachMsg.Replace("Hola Reclutador", $"Hola {dto.RecruiterName}");
                }
            }

            // Replace candidate placeholder with candidate's actual FullName
            var candidateName = GetCandidateName();
            outreachMsg = outreachMsg
                .Replace("[Candidato]", candidateName)
                .Replace("[Candidate]", candidateName)
                .Replace("[Nombre del Candidato]", candidateName);

            // Create and associate initial Communication log (outreach message)
            var communication = new Communication(
                Guid.NewGuid(),
                jobLead.Id,
                recruiterName,
                outreachMsg,
                DateTime.UtcNow,
                requiresFollowUp: false,
                followUpDate: null
            );
            jobLead.AddCommunication(communication);

            // Advance state
            jobLead.ChangeStatus(JobLeadStatus.EvaluadoPorIa);

            // Save automated evaluation & communication
            await repository.SaveChangesAsync();
        }
        catch
        {
            // Reset unpersisted evaluation status to keep state clean if AI fails on scrape
            jobLead.ChangeStatus(JobLeadStatus.LeadExtraido);
        }

        return jobLead.Id;
    }

    public async Task<IReadOnlyList<JobLead>> GetJobLeadsAsync()
    {
        return await repository.GetAllAsync();
    }

    public async Task<JobLead?> GetJobLeadByIdAsync(Guid id)
    {
        return await repository.GetByIdAsync(id);
    }

    public async Task SaveOEConfigAsync(Guid jobLeadId, TargetRole targetRole, double? aiMatchScore, bool isAsync, string? detectedRedFlags)
    {
        await repository.SaveOEConfigDirectAsync(jobLeadId, targetRole, aiMatchScore, isAsync, detectedRedFlags);
    }

    public async Task<AiEvaluationResultDto> EvaluateLeadWithAiAsync(Guid jobLeadId)
    {
        var jobLead = await repository.GetByIdAsync(jobLeadId)
            ?? throw new KeyNotFoundException($"JobLead with ID {jobLeadId} not found.");

        var aiStrategy = aiServiceFactory.GetActiveStrategy();
        logger.LogInformation("🚀 Iniciando evaluación IA ({ProviderName}) para la vacante '{JobTitle}' de '{CompanyName}'", 
            aiStrategy.ProviderName, jobLead.JobTitle, jobLead.Company.Name);

        var aiResult = await aiStrategy.EvaluateJobAsync(jobLead.JobTitle, jobLead.Company.Name, jobLead.RawDescription);

        // Enrich company industry from AI inference if available
        if (!string.IsNullOrWhiteSpace(aiResult.Industry))
        {
            jobLead.Company.UpdateDetails(jobLead.Company.Name, aiResult.Industry, jobLead.Company.ScrapedContext);
        }

        logger.LogInformation("✅ Evaluación completada ({ProviderName}): Rol={TargetRole}, MatchScore={AiMatchScore}%, IsAsync={IsAsync}, RedFlags={DetectedRedFlags}, Industry={Industry}",
            aiStrategy.ProviderName,
            aiResult.TargetRole,
            aiResult.AiMatchScore,
            aiResult.IsAsync,
            aiResult.DetectedRedFlags,
            aiResult.Industry);

        var candidateName = GetCandidateName();
        var outreachMessageFormatted = (aiResult.OutreachMessage ?? "")
            .Replace("[Candidato]", candidateName)
            .Replace("[Candidate]", candidateName)
            .Replace("[Nombre del Candidato]", candidateName);

        await repository.SaveOEConfigDirectAsync(
            jobLeadId,
            aiResult.TargetRole,
            aiResult.AiMatchScore,
            aiResult.IsAsync,
            aiResult.DetectedRedFlags,
            outreachMessageFormatted,
            aiResult.RecruiterName
        );

        return aiResult;
    }

    public string GetManualAiPrompt(JobLead lead)
    {
        ArgumentNullException.ThrowIfNull(lead);

        return $"""
Actúa como un reclutador experto y analista de Overemployment (OE).
Por favor, analiza la siguiente vacante para evaluar si califica para:
- J1 (Rol de liderazgo técnico / arquitectura de sistemas / diseño de sistemas cloud-native, alta visibilidad, reuniones estratégicas).
- J2 (Rol de contribuidor individual, altamente asíncrono, desarrollo de APIs, pocas reuniones, cero liderazgo).

Detalles de la Vacante:
Puesto: {lead.JobTitle}
Empresa: {lead.Company.Name}
Plataforma: {lead.Platform}
Rango Salarial: {lead.SalaryRange ?? "No especificado"}

Descripción Completa:
{lead.RawDescription}

Responde EXACTAMENTE con la siguiente estructura de datos en tu respuesta:
1. Rol recomendado: (J1 o J2) y explicación detallada de por qué.
2. Puntuación de coincidencia (AiMatchScore): (Valor del 0 al 100) según el perfil.
3. Asíncrono (IsAsync): (Sí/No) y descripción de la carga de reuniones estimada.
4. Red Flags detectadas: (Listado de alarmas como micromanagement, guardias obligatorias, reuniones excesivas, etc.).
""";
    }

    public async Task ChangeJobLeadStatusAsync(Guid id, JobLeadStatus newStatus)
    {
        var jobLead = await repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"JobLead with ID {id} not found.");

        jobLead.ChangeStatus(newStatus);
        await repository.SaveChangesAsync();
    }

    public async Task DeleteJobLeadAsync(Guid id)
    {
        await repository.DeleteJobLeadAsync(id);
    }
}
