using HireWire.Application.Interfaces;

namespace HireWire.Application.Services;

/// <summary>
/// Singleton service implementation to hold active AI provider selection in memory.
/// </summary>
public class AiSettingsManager : IAiSettingsManager
{
    public string ActiveProvider { get; set; } = "Mock";
}
