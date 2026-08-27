namespace HireWire.Application.Interfaces;

/// <summary>
/// Interface for managing active AI provider configuration in memory.
/// </summary>
public interface IAiSettingsManager
{
    string ActiveProvider { get; set; }
}
