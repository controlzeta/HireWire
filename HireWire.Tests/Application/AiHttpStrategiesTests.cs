using System.Net;
using System.Text;
using FluentAssertions;
using HireWire.Application.Interfaces;
using HireWire.Application.Services.AiStrategies;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace HireWire.Tests.Application;

public class AiHttpStrategiesTests
{
    private static IConfiguration ConfigWith(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

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

    [Fact]
    public async Task Gemini_NoApiKey_FallsBackToMock()
    {
        var logger = NullLogger<GeminiAiStrategy>.Instance;
        var sut = new GeminiAiStrategy(new HttpClient(new StubHandler()),
            ConfigWith(new Dictionary<string, string?>()), logger);

        var result = await sut.EvaluateJobAsync("Senior Developer", "Acme", "desc");
        result.TargetRole.Should().Be(HireWire.Domain.Enums.TargetRole.J2);
    }

    [Fact]
    public async Task Gemini_PlaceholderApiKey_FallsBackToMock()
    {
        var logger = NullLogger<GeminiAiStrategy>.Instance;
        var sut = new GeminiAiStrategy(new HttpClient(new StubHandler()),
            ConfigWith(new Dictionary<string, string?> { ["AiSettings:GeminiApiKey"] = "YOUR_GEMINI_API_KEY" }), logger);

        var result = await sut.EvaluateJobAsync("Lead Architect", "Acme", "desc");
        result.TargetRole.Should().Be(HireWire.Domain.Enums.TargetRole.J1);
    }

    [Fact]
    public async Task OpenAi_NoApiKey_FallsBackToMock()
    {
        var logger = NullLogger<OpenAiStrategy>.Instance;
        var sut = new OpenAiStrategy(new HttpClient(new StubHandler()),
            ConfigWith(new Dictionary<string, string?>()), logger);

        var result = await sut.EvaluateJobAsync("Senior Developer", "Acme", "desc");
        result.TargetRole.Should().Be(HireWire.Domain.Enums.TargetRole.J2);
    }

    [Fact]
    public async Task OpenAi_PlaceholderApiKey_FallsBackToMock()
    {
        var logger = NullLogger<OpenAiStrategy>.Instance;
        var sut = new OpenAiStrategy(new HttpClient(new StubHandler()),
            ConfigWith(new Dictionary<string, string?> { ["AiSettings:OpenAiApiKey"] = "YOUR_OPENAI_API_KEY" }), logger);

        var result = await sut.EvaluateJobAsync("Lead Architect", "Acme", "desc");
        result.TargetRole.Should().Be(HireWire.Domain.Enums.TargetRole.J1);
    }

    [Fact]
    public async Task Claude_NoApiKey_FallsBackToMock()
    {
        var logger = NullLogger<ClaudeAiStrategy>.Instance;
        var sut = new ClaudeAiStrategy(new HttpClient(new StubHandler()),
            ConfigWith(new Dictionary<string, string?>()), logger);

        var result = await sut.EvaluateJobAsync("Senior Developer", "Acme", "desc");
        result.TargetRole.Should().Be(HireWire.Domain.Enums.TargetRole.J2);
    }

    [Fact]
    public async Task Claude_PlaceholderApiKey_FallsBackToMock()
    {
        var logger = NullLogger<ClaudeAiStrategy>.Instance;
        var sut = new ClaudeAiStrategy(new HttpClient(new StubHandler()),
            ConfigWith(new Dictionary<string, string?> { ["AiSettings:ClaudeApiKey"] = "YOUR_CLAUDE_API_KEY" }), logger);

        var result = await sut.EvaluateJobAsync("Lead Architect", "Acme", "desc");
        result.TargetRole.Should().Be(HireWire.Domain.Enums.TargetRole.J1);
    }

    [Theory]
    [InlineData("Gemini")]
    [InlineData("OpenAI")]
    [InlineData("Claude")]
    public void ProviderNames_AreStable(string provider)
    {
        IAiStrategy strategy = provider switch
        {
            "Gemini" => new GeminiAiStrategy(new HttpClient(new StubHandler()), ConfigWith(new Dictionary<string, string?>()), NullLogger<GeminiAiStrategy>.Instance),
            "OpenAI" => new OpenAiStrategy(new HttpClient(new StubHandler()), ConfigWith(new Dictionary<string, string?>()), NullLogger<OpenAiStrategy>.Instance),
            _ => new ClaudeAiStrategy(new HttpClient(new StubHandler()), ConfigWith(new Dictionary<string, string?>()), NullLogger<ClaudeAiStrategy>.Instance)
        };
        strategy.ProviderName.Should().Be(provider);
    }
}
