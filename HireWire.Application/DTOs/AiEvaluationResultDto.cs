using HireWire.Domain.Enums;

namespace HireWire.Application.DTOs;

/// <summary>
/// Data Transfer Object containing structured evaluation results from AI providers.
/// </summary>
public record AiEvaluationResultDto(
    TargetRole TargetRole,
    double AiMatchScore,
    bool IsAsync,
    string DetectedRedFlags,
    string OutreachMessage,
    string? RecruiterName,
    string? Industry = null
);
