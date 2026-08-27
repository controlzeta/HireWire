using HireWire.Application.DTOs;
using HireWire.Application.Interfaces;
using HireWire.Domain.Enums;

using Microsoft.Extensions.Logging;

namespace HireWire.Application.Services.AiStrategies;

/// <summary>
/// Mock AI provider strategy generating deterministic responses for testing and local development.
/// </summary>
public class MockAiStrategy(ILogger<MockAiStrategy>? logger = null) : IAiStrategy
{
    public string ProviderName => "Mock";

    public async Task<AiEvaluationResultDto> EvaluateJobAsync(string jobTitle, string companyName, string description)
    {
        // Simulate network delay
        await Task.Delay(400);

        logger?.LogInformation("🤖 Respuesta generada por Mock AI Strategy para '{JobTitle}' de '{CompanyName}'", jobTitle, companyName);

        var isLeadership = jobTitle.Contains("lead", StringComparison.OrdinalIgnoreCase) || 
                           jobTitle.Contains("architect", StringComparison.OrdinalIgnoreCase) || 
                           jobTitle.Contains("manager", StringComparison.OrdinalIgnoreCase) || 
                           jobTitle.Contains("director", StringComparison.OrdinalIgnoreCase) ||
                           jobTitle.Contains("principal", StringComparison.OrdinalIgnoreCase);

        var targetRole = isLeadership ? TargetRole.J1 : TargetRole.J2;
        var score = isLeadership ? 85.0 : 93.0;
        var isAsync = !isLeadership;
        
        var hasCitizenshipReq = description.Contains("citizen", StringComparison.OrdinalIgnoreCase) || 
                                description.Contains("clearance", StringComparison.OrdinalIgnoreCase) ||
                                description.Contains("green card", StringComparison.OrdinalIgnoreCase) ||
                                companyName.Contains("Raytheon", StringComparison.OrdinalIgnoreCase) ||
                                companyName.Contains("Lockheed", StringComparison.OrdinalIgnoreCase);

        var legalFlag = hasCitizenshipReq ? "🚨 [REQUISITO LEGAL]: Requiere Ciudadanía de EE.UU., Green Card o Security Clearance activa. " : "";

        var redFlags = legalFlag + (isLeadership 
            ? "Reuniones de sincronización diarias estimadas; alta visibilidad estratégica. Posible colisión de agendas." 
            : "Trabajo asíncrono e individual. Pocas llamadas grupales. Ideal para J2.");

        var recruiterName = "Sofía Martínez (Inferred)";

        var outreachMessage = $"""
Hola Sofía,

Espero que estés muy bien. Te escribo porque encontré la vacante de {jobTitle} en {companyName} y considero que mi stack técnico (.NET Core, C# y Arquitectura de Software) encaja idealmente con lo que están buscando. 

Me encantaría coordinar una breve llamada para comentar los detalles.

Atentamente,
[Candidato]
""";

        var industry = description.Contains("bank", StringComparison.OrdinalIgnoreCase) || description.Contains("finan", StringComparison.OrdinalIgnoreCase)
            ? "Servicios Financieros / Fintech"
            : (description.Contains("health", StringComparison.OrdinalIgnoreCase) || description.Contains("med", StringComparison.OrdinalIgnoreCase)
                ? "Salud / HealthTech"
                : "Tecnología / Consultoría de Software");

        return new AiEvaluationResultDto(
            targetRole,
            score,
            isAsync,
            redFlags,
            outreachMessage,
            recruiterName,
            industry
        );
    }
}
