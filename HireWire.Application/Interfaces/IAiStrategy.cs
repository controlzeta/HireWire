using HireWire.Application.DTOs;

namespace HireWire.Application.Interfaces;

/// <summary>
/// Common contract for all AI provider integration strategies.
/// </summary>
public interface IAiStrategy
{
    string ProviderName { get; }
    Task<AiEvaluationResultDto> EvaluateJobAsync(string jobTitle, string companyName, string description);
}
