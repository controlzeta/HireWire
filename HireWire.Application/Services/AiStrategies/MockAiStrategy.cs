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

Espero que estés teniendo un excelente día. Te escribo porque analicé detenidamente la posición de {jobTitle} en {companyName} y considero que mi perfil como Senior .NET Solutions Architect & Developer encaja idealmente con las metas del equipo.

En mis proyectos recientes he liderado el diseño de microservicios asíncronos y desacoplados sobre C# 13, .NET 9, SQL Server, Redis y Azure Cloud, reduciendo las latencias de producción en un 45% y asegurando entregas continuas sin bloqueos sincrónicos.

¿Tendrías 10 minutos esta semana para explorar si existe alineación táctica para esta posición?

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

    public Task<string> EvaluateFastPromptAsync(string systemPrompt, string userPrompt)
    {
        var promptLower = userPrompt.ToLower();
        
        if (systemPrompt.Contains("STAR") || promptLower.Contains("dolor") || promptLower.Contains("star"))
        {
            // Extract pain topic if possible
            var painTopic = "este dolor de producción";
            if (userPrompt.Contains(": \""))
            {
                var parts = userPrompt.Split(": \"");
                if (parts.Length > 1) painTopic = parts[1].TrimEnd('"').Trim();
            }
            return Task.FromResult($"⚡ (STAR): Ante el reto de '{painTopic}', en Control Zeta rediseñé la capa de persisted cache con Redis y SQL Server sobre .NET 9. Logré reducir el tiempo de respuesta en un 45% y eliminar los bloqueos sincrónicos en producción.");
        }
        
        if (systemPrompt.Contains("homologando") || promptLower.Contains("pivot") || promptLower.Contains("solicitada:"))
        {
            var tech = "este stack";
            if (userPrompt.Contains("solicitada: "))
            {
                tech = userPrompt.Split("solicitada: ")[1].Trim();
            }
            else if (userPrompt.Contains("Tecnología: "))
            {
                tech = userPrompt.Split("Tecnología: ")[1].Trim();
            }
            return Task.FromResult($"⚡ (Pivot): En Azure / .NET 9 he implementado patrones de arquitectura equivalentes a {tech} utilizando Azure Service Bus, Entity Framework Core y Redis. Por lo tanto, mi curva de adaptación a {tech} es prácticamente nula.");
        }

        if (systemPrompt.Contains("Salarial") || promptLower.Contains("salario") || promptLower.Contains("presupuesto"))
        {
            return Task.FromResult("⚡ (Salario):\n1. \"Actualmente me encuentro evaluando ofertas B2B en el rango de $4,500 - $6,500 USD/mes para posiciones asíncronas. Me gustaría confirmar cuál es el presupuesto asignado para este rol antes de avanzar.\"\n2. \"Dado que opero como contratista independiente B2B, ¿cuál es la banda salarial máxima aprobada para esta posición?\"");
        }

        return Task.FromResult($"⚡ (Copilot): Basado en mi experiencia real en Control Zeta y Tech Enterprise Solutions con C# 13, .NET 9 y SQL Server: Para '{userPrompt.Replace("\n", " ")}', la estrategia recomendada es aplicar un patrón asíncrono con colas de mensajes y caché en Redis.");
    }
}
