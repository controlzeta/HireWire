namespace HireWire.Application.Interfaces;

/// <summary>
/// Service contract for dynamically generating tailored resumes (CV) as PDF files.
/// </summary>
public interface ICvGeneratorService
{
    /// <summary>
    /// Generates a PDF resume byte array tailored to the job lead OE recommendations.
    /// </summary>
    Task<byte[]> GenerateCvAsync(Guid jobLeadId);
}
