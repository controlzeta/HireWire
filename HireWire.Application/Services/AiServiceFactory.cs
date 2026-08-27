using HireWire.Application.Interfaces;

namespace HireWire.Application.Services;

/// <summary>
/// Factory service to dynamically retrieve the currently active AI strategy based on memory settings.
/// </summary>
public class AiServiceFactory(IEnumerable<IAiStrategy> strategies, IAiSettingsManager settingsManager)
{
    public IAiStrategy GetActiveStrategy()
    {
        var active = settingsManager.ActiveProvider;
        return strategies.FirstOrDefault(s => s.ProviderName.Equals(active, StringComparison.OrdinalIgnoreCase))
               ?? strategies.First(s => s.ProviderName == "Mock");
    }
}
