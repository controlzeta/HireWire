using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HireWire.Application.DTOs;
using HireWire.Application.Interfaces;
using HireWire.Domain.Enums;

namespace HireWire.Application.Services.AiStrategies;

/// <summary>
/// Anthropic Claude API implementation of the IAiStrategy.
/// </summary>
public class ClaudeAiStrategy(
    HttpClient httpClient, 
    IConfiguration configuration, 
    ILogger<ClaudeAiStrategy> logger) : IAiStrategy
{
    public string ProviderName => "Claude";

    public async Task<AiEvaluationResultDto> EvaluateJobAsync(string jobTitle, string companyName, string description)
    {
        var apiKey = configuration["AiSettings:ClaudeApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_CLAUDE_API_KEY")
        {
            logger.LogWarning("Claude API key is not configured or placeholder remains. Falling back to Mock strategy.");
            return await new MockAiStrategy().EvaluateJobAsync(jobTitle, companyName, description);
        }

        var systemPrompt = $$"""
Actúa como un reclutador de TI senior y un analista experto en Overemployment (OE).
Analiza la oferta y responde ÚNICAMENTE con un objeto JSON válido con las siguientes propiedades (sin markdown ni explicaciones adicionales):
{
  "targetRole": "J1" o "J2",
  "aiMatchScore": un número entre 0 y 100,
  "isAsync": true o false,
  "detectedRedFlags": "descripción de las red flags encontradas",
  "outreachMessage": "cuerpo del mensaje redactado para reclutador",
  "recruiterName": "nombre del reclutador" o null
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
            model = "claude-3-5-sonnet-20240620",
            max_tokens = 1500,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = JsonContent.Create(payload);

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            var text = responseJson
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Claude returned an empty content response.");
            }

            logger.LogInformation("🤖 Respuesta recibida de Claude API para '{JobTitle}':\n{ClaudeResponse}", jobTitle, text);

            var cleanText = text.Trim();
            if (cleanText.StartsWith("```json")) cleanText = cleanText.Substring(7);
            if (cleanText.StartsWith("```")) cleanText = cleanText.Substring(3);
            if (cleanText.EndsWith("```")) cleanText = cleanText.Substring(0, cleanText.Length - 3);
            cleanText = cleanText.Trim();

            using var doc = JsonDocument.Parse(cleanText);
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
            logger.LogError(ex, "Error communicating with Claude API. Falling back to Mock strategy.");
            return await new MockAiStrategy().EvaluateJobAsync(jobTitle, companyName, description);
        }
    }
}
