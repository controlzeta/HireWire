namespace HireWire.Application.DTOs;

/// <summary>
/// Data Transfer Object representing a job lead sent by the Chrome extension scraper.
/// </summary>
public record ScrapedJobLeadDto(
    string CompanyName,
    string? Industry,
    string? CompanyContext,
    string JobTitle,
    string Url,
    string Platform,
    string RawDescription,
    string? SalaryRange,
    string? RecruiterName = null
);
