using FluentAssertions;
using HireWire.Application.Services;
using HireWire.Application.Services.AiStrategies;
using HireWire.Domain.Enums;

namespace HireWire.Tests.Application;

public class MockAiStrategyTests
{
    private readonly MockAiStrategy _sut = new();

    [Fact]
    public void ProviderName_IsMock() => _sut.ProviderName.Should().Be("Mock");

    [Theory]
    [InlineData("Lead .NET Architect", TargetRole.J1)]
    [InlineData("Software Architect", TargetRole.J1)]
    [InlineData("Engineering Manager", TargetRole.J1)]
    [InlineData("Director of Engineering", TargetRole.J1)]
    [InlineData("Principal Engineer", TargetRole.J1)]
    [InlineData("Senior .NET Developer", TargetRole.J2)]
    public async Task EvaluateJobAsync_ClassifiesRole(string title, TargetRole expected)
    {
        var result = await _sut.EvaluateJobAsync(title, "Acme", "Descripción genérica");
        result.TargetRole.Should().Be(expected);
    }

    [Fact]
    public async Task EvaluateJobAsync_Leadership_NotAsyncAndScore85()
    {
        var result = await _sut.EvaluateJobAsync("Lead Architect", "Acme", "Backend");
        result.AiMatchScore.Should().Be(85.0);
        result.IsAsync.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateJobAsync_IndividualContributor_IsAsyncAndScore93()
    {
        var result = await _sut.EvaluateJobAsync("Senior Developer", "Acme", "Backend");
        result.AiMatchScore.Should().Be(93.0);
        result.IsAsync.Should().BeTrue();
    }

    [Theory]
    [InlineData("We require US citizenship")]
    [InlineData("active security clearance required")]
    [InlineData("green card preferred")]
    public async Task EvaluateJobAsync_LegalRequirement_AddsLegalFlag(string description)
    {
        var result = await _sut.EvaluateJobAsync("Developer", "Acme", description);
        result.DetectedRedFlags.Should().Contain("🚨 [REQUISITO LEGAL]");
    }

    [Theory]
    [InlineData("Raytheon")]
    [InlineData("Lockheed Martin")]
    public async Task EvaluateJobAsync_DefenseCompany_AddsLegalFlag(string company)
    {
        var result = await _sut.EvaluateJobAsync("Developer", company, "Backend role");
        result.DetectedRedFlags.Should().Contain("🚨 [REQUISITO LEGAL]");
    }

    [Fact]
    public async Task EvaluateJobAsync_NoLegalRequirement_NoLegalFlag()
    {
        var result = await _sut.EvaluateJobAsync("Developer", "Acme", "Trabajo remoto asíncrono");
        result.DetectedRedFlags.Should().NotContain("🚨 [REQUISITO LEGAL]");
    }

    [Theory]
    [InlineData("We are a digital bank", "Servicios Financieros / Fintech")]
    [InlineData("software for the financial sector", "Servicios Financieros / Fintech")]
    [InlineData("health insurance platform", "Salud / HealthTech")]
    [InlineData("medical devices", "Salud / HealthTech")]
    [InlineData("generic software shop", "Tecnología / Consultoría de Software")]
    public async Task EvaluateJobAsync_InfersIndustry(string description, string expected)
    {
        var result = await _sut.EvaluateJobAsync("Developer", "Acme", description);
        result.Industry.Should().Be(expected);
    }

    [Fact]
    public async Task EvaluateJobAsync_ReturnsCandidatePlaceholder()
    {
        var result = await _sut.EvaluateJobAsync("Developer", "Acme", "desc");
        result.OutreachMessage.Should().Contain("[Candidato]");
        result.RecruiterName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task EvaluateFastPromptAsync_StarPrompt_ReturnsStarStory()
    {
        var result = await _sut.EvaluateFastPromptAsync("Genera una anécdota STAR", "Dolor de Negocio: \"latencia en producción\"");
        result.Should().Contain("(STAR)");
        result.Should().Contain("latencia en producción");
    }

    [Fact]
    public async Task EvaluateFastPromptAsync_PivotPrompt_ReturnsPivotScript()
    {
        var result = await _sut.EvaluateFastPromptAsync("Pivota la tecnología homologando", "Tecnología solicitada: Kubernetes");
        result.Should().Contain("(Pivot)");
        result.Should().Contain("Kubernetes");
    }

    [Fact]
    public async Task EvaluateFastPromptAsync_SalaryPrompt_ReturnsSalaryScript()
    {
        var result = await _sut.EvaluateFastPromptAsync("Guion Salarial", "consulta de salario");
        result.Should().Contain("(Salario)");
    }

    [Fact]
    public async Task EvaluateFastPromptAsync_GenericPrompt_ReturnsCopilotAnswer()
    {
        var result = await _sut.EvaluateFastPromptAsync("Otra cosa completamente distinta", "pregunta técnica sobre caching");
        result.Should().Contain("(Copilot)");
    }
}

public class AiServiceFactoryTests
{
    private static AiServiceFactory MakeFactory(string activeProvider, out IList<HireWire.Application.Interfaces.IAiStrategy> strategies)
    {
        strategies = new List<HireWire.Application.Interfaces.IAiStrategy>
        {
            new MockAiStrategy()
        };
        var settings = new AiSettingsManager { ActiveProvider = activeProvider };
        return new AiServiceFactory(strategies, settings);
    }

    [Fact]
    public void GetActiveStrategy_KnownProvider_ReturnsThatProvider()
    {
        var factory = MakeFactory("Mock", out _);
        factory.GetActiveStrategy().ProviderName.Should().Be("Mock");
    }

    [Fact]
    public void GetActiveStrategy_CaseInsensitive_Matches()
    {
        var factory = MakeFactory("MOCK", out _);
        factory.GetActiveStrategy().ProviderName.Should().Be("Mock");
    }

    [Fact]
    public void GetActiveStrategy_UnknownProvider_FallsBackToMock()
    {
        var factory = MakeFactory("NoExisteEsteProvider", out _);
        factory.GetActiveStrategy().ProviderName.Should().Be("Mock");
    }
}

public class AiSettingsManagerTests
{
    [Fact]
    public void ActiveProvider_DefaultsToMock()
    {
        new AiSettingsManager().ActiveProvider.Should().Be("Mock");
    }

    [Fact]
    public void ActiveProvider_CanBeSet()
    {
        var manager = new AiSettingsManager { ActiveProvider = "Gemini" };
        manager.ActiveProvider.Should().Be("Gemini");
    }
}
