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
            aiResult.RecruiterName,
            aiResult.Industry
        );

        // Advance status to EvaluadoPorIa if currently LeadExtraido
        if (jobLead.Status == JobLeadStatus.LeadExtraido)
        {
            jobLead.ChangeStatus(JobLeadStatus.EvaluadoPorIa);
            await repository.SaveChangesAsync();
        }

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

    private string GetRealProfileSummary()
    {
        var name = GetCandidateName();
        var title = configuration["UserProfile:TitleEs"] ?? "Senior Software Engineer";
        var summary = configuration["UserProfile:SummaryJ2Es"] ?? configuration["UserProfile:SummaryJ1Es"] ?? "";
        
        var experiences = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var company = configuration[$"UserProfile:Experience:{i}:Company"];
            if (string.IsNullOrWhiteSpace(company)) break;

            var role = configuration[$"UserProfile:Experience:{i}:RoleEs"] ?? "Senior Developer";
            var summaryExp = configuration[$"UserProfile:Experience:{i}:SummaryEs"] ?? "";
            var contrib = configuration[$"UserProfile:Experience:{i}:ContributionsEs:0"] ?? "";
            
            experiences.Add($"- Empresa: {company} | Rol: {role} | Logro: {contrib}");
        }

        var expText = experiences.Count > 0 ? string.Join("\n", experiences) : "- Empresa: Control Zeta / HireWire | Rol: Lead Architect & Senior Developer";

        return $"""
CANDIDATO REAL: {name}
TÍTULO PROFESIONAL: {title}
PERFIL TÁCTICO: {summary}

HISTORIAL DE EXPERIENCIA REAL (USA ÚNICAMENTE ESTOS DATOS):
{expText}

STACK TÉCNICO VERIFICADO: C# 13, .NET 9, ASP.NET Core, SQL Server, Entity Framework Core, Redis, Azure, Docker, Microservicios asíncronos, DDD, Clean Architecture.
""";
    }

    public async Task<string> GenerateStarStoryAsync(Guid jobLeadId, string businessPain)
    {
        var lead = await repository.GetByIdAsync(jobLeadId)
            ?? throw new KeyNotFoundException($"JobLead {jobLeadId} not found.");

        var aiStrategy = aiServiceFactory.GetActiveStrategy();
        var profile = GetRealProfileSummary();

        var systemPrompt = $$"""
Actúa como un Copiloto de Entrevista Técnica de Alto Rendimiento para {{GetCandidateName()}}.
Debes generar una anécdota concisa de 30 segundos usando la ESTRUCTURA STAR (Situación, Tarea, Acción, Resultado).

REGLAS CRÍTICAS DE CALIDAD:
1. Basado ÚNICAMENTE en la experiencia real del candidato expuesta abajo (Control Zeta, Tech Enterprise Solutions, etc.). NO inventes empresas ficticias.
2. Formato:
   - **Situación & Tarea**: El reto o dolor de producción enfrentado.
   - **Acción**: La arquitectura o código C# 13 / .NET 9 / SQL Server que implementaste.
   - **Resultado**: El impacto cuantificable logrado (ej. latencia reducida 45%, cero tiempo de inactividad, entregas asíncronas limpias).
3. Responde en MÁXIMO 3 viñetas cortas e hiper-legibles de reojo.

PERFIL REAL:
{{profile}}
""";

        var userPrompt = $"""
Empresa de la Vacante: {lead.Company.Name} ({lead.JobTitle})
Dolor de Negocio Específico: "{businessPain}"

Genera el guion STAR optimizado.
""";

        return await aiStrategy.EvaluateFastPromptAsync(systemPrompt, userPrompt);
    }

    public async Task<string> GenerateStackPivotAsync(Guid jobLeadId, string targetTech)
    {
        var lead = await repository.GetByIdAsync(jobLeadId)
            ?? throw new KeyNotFoundException($"JobLead {jobLeadId} not found.");

        var aiStrategy = aiServiceFactory.GetActiveStrategy();
        var profile = GetRealProfileSummary();

        var systemPrompt = $$"""
Actúa como un Copiloto de Entrevista Técnica para {{GetCandidateName()}}.
El entrevistador ha mencionado la tecnología "{{targetTech}}".
Instrucciones:
1. Genera un speech de 20 segundos homologando esa tecnología con el stack real del candidato en Azure, C#, SQL Server o Redis.
2. Formato directo: "En Azure / .NET 9 he implementado una solución arquitectónica equivalente utilizando... Por lo que la curva de adopción de {{targetTech}} es prácticamente nula."
3. Usa ÚNICAMENTE datos de su historial profesional real.
4. Máximo 2 oraciones concisas y de alto impacto.

PERFIL REAL:
{{profile}}
""";

        var userPrompt = $"""
Puesto: {lead.JobTitle} en {lead.Company.Name}
Tecnología solicitada: {targetTech}
""";

        return await aiStrategy.EvaluateFastPromptAsync(systemPrompt, userPrompt);
    }

    public async Task<string> GeneratePanicAnswerAsync(Guid jobLeadId, string query)
    {
        var lead = await repository.GetByIdAsync(jobLeadId)
            ?? throw new KeyNotFoundException($"JobLead {jobLeadId} not found.");

        var aiStrategy = aiServiceFactory.GetActiveStrategy();
        var profile = GetRealProfileSummary();

        var systemPrompt = $$"""
Actúa como un Copiloto de Respuesta Inmediata de Pánico para {{GetCandidateName()}}.
El candidato necesita responder verbalmente y de inmediato sobre: "{{query}}".
Instrucciones:
1. Devuelve 3 viñetas súper concisas y ejecutivas explicando la respuesta técnica perfecta.
2. Conéctala directamente con la experiencia real en C# 13, .NET 9, SQL Server, Redis o Azure.
3. Máximo 25 palabras por viñeta para lectura ultrarrápida.

PERFIL REAL:
{{profile}}
""";

        var userPrompt = $"""
Pregunta o tema de pánico: {query}
Vacante: {lead.JobTitle} ({lead.Company.Name})
""";

        return await aiStrategy.EvaluateFastPromptAsync(systemPrompt, userPrompt);
    }

    public async Task<string> GenerateSalaryScriptAsync(Guid jobLeadId)
    {
        var lead = await repository.GetByIdAsync(jobLeadId)
            ?? throw new KeyNotFoundException($"JobLead {jobLeadId} not found.");

        var aiStrategy = aiServiceFactory.GetActiveStrategy();

        var systemPrompt = $$"""
Actúa como un Asesor Táctico de Negociación Salarial B2B para {{GetCandidateName()}}.
La vacante en {{lead.Company.Name}} ({{lead.JobTitle}}) indica "Salario: No especificado".
Instrucciones:
1. Genera 2 guiones de respuesta verbal directa para forzar al reclutador a revelar la banda presupuestal primero sin quemar la negociación.
2. Enfocado en contratación de servicios B2B / Contratista independiente.
3. Redacta 2 guiones directos entre comillas.
""";

        var userPrompt = $"Vacante: {lead.JobTitle} en {lead.Company.Name}. Sin rango salarial visible.";

        return await aiStrategy.EvaluateFastPromptAsync(systemPrompt, userPrompt);
    }

    public async Task<string> GenerateCheatSheetJsonAsync(Guid jobLeadId)
    {
        var lead = await repository.GetByIdAsync(jobLeadId)
            ?? throw new KeyNotFoundException($"JobLead {jobLeadId} not found.");

        var aiStrategy = aiServiceFactory.GetActiveStrategy();

        var systemPrompt = """
Actúa como un analista técnico de entrevistas y arquitecto de software.
Analiza la descripción de la vacante y genera un JSON con la estructura exacta:
{
  "techStack": ["Tecnología 1", "Tecnología 2", "Tecnología 3", "Tecnología 4"],
  "businessPains": [
    "Dolor de negocio específico 1 que la empresa intenta resolver contratando esta posición",
    "Dolor de negocio específico 2",
    "Dolor de negocio específico 3"
  ],
  "questions": [
    "Pregunta incisiva y estratégica 1 para hacerle al entrevistador sobre la vacante",
    "Pregunta incisiva 2",
    "Pregunta incisiva 3"
  ]
}
Responde ÚNICAMENTE con el objeto JSON válido.
""";

        var userPrompt = $"""
Puesto: {lead.JobTitle}
Empresa: {lead.Company.Name} ({lead.Company.Industry ?? "Tecnología"})
Descripción de la Vacante:
{lead.RawDescription}
""";

        var jsonText = await aiStrategy.EvaluateFastPromptAsync(systemPrompt, userPrompt);
        
        // Clean out markdown backticks if returned by LLM
        if (jsonText.Contains("```json"))
        {
            jsonText = jsonText.Split("```json")[1].Split("```")[0].Trim();
        }
        else if (jsonText.Contains("```"))
        {
            jsonText = jsonText.Split("```")[1].Split("```")[0].Trim();
        }

        return jsonText;
    }
}
