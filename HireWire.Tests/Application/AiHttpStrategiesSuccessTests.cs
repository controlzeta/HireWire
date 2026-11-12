using System.Net;
using System.Text;
using FluentAssertions;
using HireWire.Application.Interfaces;
using HireWire.Application.Services.AiStrategies;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace HireWire.Tests.Application;

/// <summary>
/// Success-path tests for the HTTP AI strategies using a stubbed HttpMessageHandler.
/// </summary>
public class AiHttpStrategiesSuccessTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; init; } =
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(Responder(request));
    }

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private const string GeminiPayload =
        "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{\\\"targetRole\\\": \\\"J2\\\", \\\"aiMatchScore\\\": 88, \\\"isAsync\\\": true, \\\"detectedRedFlags\\\": \\\"none\\\", \\\"outreachMessage\\\": \\\"Hola Jeff\\\", \\\"recruiterName\\\": \\\"Jeff\\\", \\\"industry\\\": \\\"Fintech\\\"}\"}]}}]}";

    [Fact]
    public async Task Gemini_SuccessfulResponse_ParsesEvaluation()
    {
        var handler = new StubHandler { Responder = _ => JsonResponse(GeminiPayload) };
        var sut = new GeminiAiStrategy(new HttpClient(handler),
            Config(new Dictionary<string, string?> { ["AiSettings:GeminiApiKey"] = "test-key" }),
            NullLogger<GeminiAiStrategy>.Instance);

        var result = await sut.EvaluateJobAsync("Senior .NET Developer", "Acme", "desc");

        result.TargetRole.Should().Be(HireWire.Domain.Enums.TargetRole.J2);
        result.AiMatchScore.Should().Be(88);
        result.IsAsync.Should().BeTrue();
        result.RecruiterName.Should().Be("Jeff");
        result.Industry.Should().Be("Fintech");
    }

    [Fact]
    public async Task Gemini_ServerError_FallsBackToMock()
    {
        var handler = new StubHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        };
        var sut = new GeminiAiStrategy(new HttpClient(handler),
            Config(new Dictionary<string, string?> { ["AiSettings:GeminiApiKey"] = "test-key" }),
            NullLogger<GeminiAiStrategy>.Instance);

        var result = await sut.EvaluateJobAsync("Senior Developer", "Acme", "desc");
        result.Should().NotBeNull(); // Mock fallback produced an answer
    }

    [Fact]
    public async Task Gemini_FastPrompt_SuccessfulResponse_ReturnsText()
    {
        var handler = new StubHandler { Responder = _ => JsonResponse(GeminiPayload) };
        var sut = new GeminiAiStrategy(new HttpClient(handler),
            Config(new Dictionary<string, string?> { ["AiSettings:GeminiApiKey"] = "test-key" }),
            NullLogger<GeminiAiStrategy>.Instance);

        var text = await sut.EvaluateFastPromptAsync("system", "user");
        text.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task OpenAi_SuccessfulResponse_ParsesEvaluation()
    {
        var openAiPayload =
            "{\"choices\":[{\"message\":{\"content\":\"{\\\"targetRole\\\": \\\"J1\\\", \\\"aiMatchScore\\\": 72, \\\"isAsync\\\": false, \\\"detectedRedFlags\\\": \\\"micromanagement\\\", \\\"outreachMessage\\\": \\\"Hola\\\", \\\"recruiterName\\\": \\\"Ana\\\", \\\"industry\\\": \\\"Salud\\\"}\"}}]}";
        var handler = new StubHandler { Responder = _ => JsonResponse(openAiPayload) };
        var sut = new OpenAiStrategy(new HttpClient(handler),
            Config(new Dictionary<string, string?> { ["AiSettings:OpenAiApiKey"] = "test-key" }),
            NullLogger<OpenAiStrategy>.Instance);

        var result = await sut.EvaluateJobAsync("Lead Architect", "Acme", "desc");

        result.TargetRole.Should().Be(HireWire.Domain.Enums.TargetRole.J1);
        result.AiMatchScore.Should().Be(72);
        result.RecruiterName.Should().Be("Ana");
    }

    [Fact]
    public async Task Claude_SuccessfulResponse_ParsesEvaluation()
    {
        var claudePayload =
            "{\"content\":[{\"type\":\"text\",\"text\":\"{\\\"targetRole\\\": \\\"J2\\\", \\\"aiMatchScore\\\": 95, \\\"isAsync\\\": true, \\\"detectedRedFlags\\\": \\\"none\\\", \\\"outreachMessage\\\": \\\"Hola\\\", \\\"recruiterName\\\": \\\"Luis\\\", \\\"industry\\\": \\\"Retail\\\"}\"}]}";
        var handler = new StubHandler { Responder = _ => JsonResponse(claudePayload) };
        var sut = new ClaudeAiStrategy(new HttpClient(handler),
            Config(new Dictionary<string, string?> { ["AiSettings:ClaudeApiKey"] = "test-key" }),
            NullLogger<ClaudeAiStrategy>.Instance);

        var result = await sut.EvaluateJobAsync("Senior Developer", "Acme", "desc");

        result.TargetRole.Should().Be(HireWire.Domain.Enums.TargetRole.J2);
        result.AiMatchScore.Should().Be(95);
        result.RecruiterName.Should().Be("Luis");
    }
}
