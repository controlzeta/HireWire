using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HireWire.Application.DTOs;
using HireWire.Application.Interfaces;
using HireWire.Domain.Enums;

namespace HireWire.Application.Services.AiStrategies;

/// <summary>
/// Google Gemini API implementation of the IAiStrategy.
/// </summary>
public class GeminiAiStrategy(
    HttpClient httpClient, 
    IConfiguration configuration, 
    ILogger<GeminiAiStrategy> logger) : IAiStrategy
{
    public string ProviderName => "Gemini";

    public async Task<AiEvaluationResultDto> EvaluateJobAsync(string jobTitle, string companyName, string description)
    {
        var apiKey = configuration["AiSettings:GeminiApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GEMINI_API_KEY")
        {
            logger.LogWarning("Gemini API key is not configured or placeholder remains. Falling back to Mock strategy.");
            return await new MockAiStrategy().EvaluateJobAsync(jobTitle, companyName, description);
        }

        var prompt = $$"""
Actúa como un reclutador de TI senior y un analista experto en Overemployment (OE).
Analiza la siguiente oferta de trabajo y determina si clasifica como:
- J1 (Rol de liderazgo/arquitectura de sistemas, alta visibilidad, reuniones estratégicas frecuentes).
- J2 (Rol de contribuidor individual, asíncrono, desarrollo de APIs, pocas reuniones, cero liderazgo).

REGLA DE RED FLAGS & REQUISITOS LEGALES:
Examina minuciosamente si la vacante exige requisitos estrictos de Ciudadanía (ej. 'US Citizenship Required', 'US Citizen only'), Residencia geográfica obligatoria (ej. 'Must reside in the US/Canada/LATAM'), Security Clearance activa, o incapacidad de patrocinar visa. Si encuentras algún requisito legal o de residencia, INCLÚYELO PRIMERO Y EXPLÍCITAMENTE en "detectedRedFlags" con el prefijo "🚨 [REQUISITO LEGAL]".

Vacante: {{jobTitle}}
Empresa: {{companyName}}
Descripción:
{{description}}

Genera además un mensaje de outreach (LinkedIn/correo) hiper-personalizado, directo y profesional en español dirigido al reclutador (si detectas su nombre, salúdalo directamente por su nombre, ej. 'Hola Jeff,'). El mensaje debe:
1. Mencionar la empresa ({{companyName}}) y el puesto ({{jobTitle}}).
2. Presentar al candidato como Senior .NET Solutions Architect & Developer especializado en C# 13, .NET 9, SQL Server, Redis y Azure Cloud.
3. Destacar brevemente su experiencia real en microservicios asíncronos y entrega continua con alto rendimiento.
4. Concluir proponiendo una breve conversación de 10 minutos para explorar alineación táctica.
5. Firmar al final con "[Candidato]".

Debes responder ÚNICAMENTE con un objeto JSON válido con la siguiente estructura de propiedades:
{
  "targetRole": "J1" o "J2",
  "aiMatchScore": un número entre 0 y 100,
  "isAsync": true o false,
  "detectedRedFlags": "descripción detallada de red flags y requisitos de ciudadanía/residencia",
  "outreachMessage": "cuerpo del mensaje redactado",
  "recruiterName": "nombre del reclutador" o null,
  "industry": "sector de la empresa (ej. Fintech, E-Commerce, Salud, Defensa/Aeroespacial)" o null
}
""";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json"
            }
        };

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

        try
        {
            var response = await httpClient.PostAsJsonAsync(endpoint, payload);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            // Extract the generated text block candidates[0].content.parts[0].text
            var text = responseJson
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Gemini returned empty text completion.");
            }

            logger.LogInformation("🤖 Respuesta recibida de Google Gemini API para '{JobTitle}':\n{GeminiResponse}", jobTitle, text);

            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            var targetRoleStr = root.GetProperty("targetRole").GetString();
            var targetRole = targetRoleStr != null && targetRoleStr.Equals("J2", StringComparison.OrdinalIgnoreCase) 
                ? TargetRole.J2 
                : TargetRole.J1;

            var score = root.GetProperty("aiMatchScore").GetDouble();
            var isAsync = root.GetProperty("isAsync").GetBoolean();
            var redFlags = root.GetProperty("detectedRedFlags").GetString() ?? "";
            var message = root.GetProperty("outreachMessage").GetString() ?? "";
            var recruiter = root.TryGetProperty("recruiterName", out var recProp) ? recProp.GetString() : null;
            var industry = root.TryGetProperty("industry", out var indProp) ? indProp.GetString() : null;

            return new AiEvaluationResultDto(targetRole, score, isAsync, redFlags, message, recruiter, industry);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error communicating with Gemini API. Falling back to Mock strategy.");
            return await new MockAiStrategy().EvaluateJobAsync(jobTitle, companyName, description);
        }
    }

    public async Task<string> EvaluateFastPromptAsync(string systemPrompt, string userPrompt)
    {
        var apiKey = configuration["AiSettings:GeminiApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GEMINI_API_KEY")
        {
            return await new MockAiStrategy().EvaluateFastPromptAsync(systemPrompt, userPrompt);
        }

        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = $"{systemPrompt}\n\n{userPrompt}" }
                        }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = 180,
                    temperature = 0.2f
                }
            };

            var response = await httpClient.PostAsJsonAsync(url, payload);

            if (!response.IsSuccessStatusCode)
            {
                return await new MockAiStrategy().EvaluateFastPromptAsync(systemPrompt, userPrompt);
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text?.Trim() ?? "No se obtuvo respuesta de la IA.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al invocar Gemini Fast Prompt.");
            return await new MockAiStrategy().EvaluateFastPromptAsync(systemPrompt, userPrompt);
        }
    }
}
