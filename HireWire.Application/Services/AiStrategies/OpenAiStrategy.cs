using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HireWire.Application.DTOs;
using HireWire.Application.Interfaces;
using HireWire.Domain.Enums;

namespace HireWire.Application.Services.AiStrategies;

/// <summary>
/// OpenAI API implementation of the IAiStrategy.
/// </summary>
public class OpenAiStrategy(
    HttpClient httpClient, 
    IConfiguration configuration, 
    ILogger<OpenAiStrategy> logger) : IAiStrategy
{
    public string ProviderName => "OpenAI";

    public async Task<AiEvaluationResultDto> EvaluateJobAsync(string jobTitle, string companyName, string description)
    {
        var apiKey = configuration["AiSettings:OpenAiApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_OPENAI_API_KEY")
        {
            logger.LogWarning("OpenAI API key is not configured or placeholder remains. Falling back to Mock strategy.");
            return await new MockAiStrategy().EvaluateJobAsync(jobTitle, companyName, description);
        }

        var systemPrompt = $$"""
Actúa como un reclutador de TI senior y un analista experto en Overemployment (OE).
REGLA DE REQUISITOS LEGALES: Revisa si exige Ciudadanía (ej. 'US Citizen required'), Security Clearance activa, o Residencia en cierto país. Si existen, INCLÚYELOS PRIMERO en "detectedRedFlags" con "🚨 [REQUISITO LEGAL]".

Analiza la oferta y responde ÚNICAMENTE con un objeto JSON válido con las siguientes propiedades:
{
  "targetRole": "J1" o "J2",
  "aiMatchScore": un número entre 0 y 100,
  "isAsync": true o false,
  "detectedRedFlags": "descripción de las red flags y requisitos de ciudadanía/residencia",
  "outreachMessage": "cuerpo del mensaje redactado para reclutador",
  "recruiterName": "nombre del reclutador" o null,
  "industry": "sector de la empresa" o null
}
""";

        var userPrompt = $"""
Vacante: {jobTitle}
Empresa: {companyName}
Descripción:
{description}
""";

        var payload = new
        {
            model = "gpt-4o-mini",
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent.Create(payload);

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            var text = responseJson
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("OpenAI returned an empty content response.");
            }

            logger.LogInformation("🤖 Respuesta recibida de OpenAI API para '{JobTitle}':\n{OpenAiResponse}", jobTitle, text);

            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            var roleStr = root.GetProperty("targetRole").GetString();
            var targetRole = roleStr != null && roleStr.Equals("J2", StringComparison.OrdinalIgnoreCase) 
                ? TargetRole.J2 
                : TargetRole.J1;

            var score = root.GetProperty("aiMatchScore").GetDouble();
            var isAsync = root.GetProperty("isAsync").GetBoolean();
            var redFlags = root.GetProperty("detectedRedFlags").GetString() ?? "";
            var outreachMessage = root.GetProperty("outreachMessage").GetString() ?? "";
            var recruiterName = root.TryGetProperty("recruiterName", out var recProp) ? recProp.GetString() : null;
            var industry = root.TryGetProperty("industry", out var indProp) ? indProp.GetString() : null;

            return new AiEvaluationResultDto(targetRole, score, isAsync, redFlags, outreachMessage, recruiterName, industry);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error communicating with OpenAI API. Falling back to Mock strategy.");
            return await new MockAiStrategy().EvaluateJobAsync(jobTitle, companyName, description);
        }
    }
}
