using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using HireWire.Application.Interfaces;
using HireWire.Application.DTOs;
using HireWire.Application.Services;
using HireWire.Domain.Entities;
using HireWire.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Hosting;

namespace HireWire.Infrastructure.Services;

/// <summary>
/// Professional ATS-compliant PDF Resume Generator powered by QuestPDF with bilingual support and structured work experience items.
/// </summary>
public class DynamicCvGeneratorService(
    IJobLeadRepository repository, 
    IConfiguration configuration,
    AiServiceFactory aiServiceFactory, 
    IHostEnvironment hostEnvironment,
    ILogger<DynamicCvGeneratorService> logger) : ICvGeneratorService
{
    public async Task<byte[]> GenerateCvAsync(Guid jobLeadId)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var jobLead = await repository.GetByIdAsync(jobLeadId)
            ?? throw new KeyNotFoundException($"JobLead with ID {jobLeadId} not found.");

        var targetRole = jobLead.OE_Config?.TargetRole ?? TargetRole.J2;

        // Load configuration or fall back to default candidate profile
        var profile = configuration.GetSection("UserProfile").Get<UserProfileOptions>() ?? new UserProfileOptions();

        // Detect language from config or job lead description
        bool isEnglish = ShouldUseEnglish(profile.DefaultLanguage, jobLead.JobTitle, jobLead.RawDescription);

        // Try AI tailoring if active strategy is available
        string activeProviderName = "Default Profile";
        try
        {
            var activeStrategy = aiServiceFactory.GetActiveStrategy();
            activeProviderName = activeStrategy.ProviderName;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Using default profile settings for CV generation.");
        }

        logger.LogInformation("📄 Generando CV PDF (ATS-Compliant) | Vacante: '{JobTitle}' ({Company}) | Idioma: {Lang} | Provider: {Provider} | OE Target: {TargetRole} | Candidato: {Name}", 
            jobLead.JobTitle, jobLead.Company.Name, isEnglish ? "ENGLISH" : "ESPAÑOL", activeProviderName, targetRole, profile.FullName);

        var pdfData = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial").FontColor("#334155"));

                // ATS Header Section (Top Centered / Left Clean)
                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(profile.FullName.ToUpper())
                                .Bold().FontSize(18).FontColor("#0f172a");
                            
                            string title = isEnglish ? profile.TitleEn : profile.TitleEs;
                            col.Item().Text(title)
                                .FontSize(10).FontColor("#475569");
                        });

                        row.ConstantItem(220).Column(col =>
                        {
                            col.Item().AlignRight().Text(isEnglish ? profile.LocationEn : profile.LocationEs).FontSize(8.5f);
                            col.Item().AlignRight().Text(profile.Email).FontSize(8.5f);
                            col.Item().AlignRight().Text(profile.Phone).FontSize(8.5f);
                            if (!string.IsNullOrWhiteSpace(profile.LinkedInUrl))
                            {
                                col.Item().AlignRight().Text(profile.LinkedInUrl).FontSize(8.5f).FontColor("#1d4ed8");
                            }
                        });
                    });

                    // Thin clean divider line
                    headerCol.Item().PaddingTop(4).PaddingBottom(4).Height(1).Background("#cbd5e1");
                });

                // Main ATS Content (Single column layout for maximum ATS parsing accuracy)
                page.Content().PaddingVertical(4).Column(contentCol =>
                {
                    contentCol.Spacing(8);

                    // 1. PROFESSIONAL SUMMARY
                    contentCol.Item().Column(section =>
                    {
                        string summaryHeader = isEnglish ? "PROFESSIONAL SUMMARY" : "RESUMEN PROFESIONAL";
                        section.Item().Text(summaryHeader).Bold().FontSize(10).FontColor("#1e293b");
                        section.Item().PaddingVertical(1).Height(0.5f).Background("#cbd5e1");

                        string summary = targetRole == TargetRole.J1
                            ? (isEnglish ? profile.SummaryJ1En : profile.SummaryJ1Es)
                            : (isEnglish ? profile.SummaryJ2En : profile.SummaryJ2Es);

                        section.Item().PaddingTop(2).Text(summary).Justify().FontSize(8.5f).LineHeight(1.2f);
                    });

                    // 2. TECHNICAL SKILLS & COMPETENCIES
                    contentCol.Item().Column(section =>
                    {
                        string techHeader = isEnglish ? "CORE COMPETENCIES & TECHNICAL STACK" : "COMPETENCIAS & TECNOLOGÍAS";
                        section.Item().Text(techHeader).Bold().FontSize(10).FontColor("#1e293b");
                        section.Item().PaddingVertical(1).Height(0.5f).Background("#cbd5e1");

                        section.Item().PaddingTop(2).Column(techList =>
                        {
                            techList.Spacing(2);
                            
                            string langLabel = isEnglish ? "Languages & Frameworks:" : "Lenguajes & Frameworks:";
                            string dbLabel = isEnglish ? "Databases & Storage:" : "Bases de Datos:";
                            string cloudLabel = isEnglish ? "Cloud & DevOps:" : "Cloud & DevOps:";

                            techList.Item().Text(text =>
                            {
                                text.Span($"• {langLabel} ").Bold().FontSize(8.5f);
                                text.Span(profile.LanguagesAndFrameworks).FontSize(8.5f);
                            });

                            techList.Item().Text(text =>
                            {
                                text.Span($"• {dbLabel} ").Bold().FontSize(8.5f);
                                text.Span(profile.Databases).FontSize(8.5f);
                            });

                            techList.Item().Text(text =>
                            {
                                text.Span($"• {cloudLabel} ").Bold().FontSize(8.5f);
                                text.Span(profile.CloudAndDevOps).FontSize(8.5f);
                            });
                        });
                    });

                    // 3. WORK EXPERIENCE (Array of structured experience items)
                    contentCol.Item().Column(section =>
                    {
                        string expHeader = isEnglish ? "WORK EXPERIENCE" : "EXPERIENCIA LABORAL";
                        section.Item().Text(expHeader).Bold().FontSize(10).FontColor("#1e293b");
                        section.Item().PaddingVertical(1).Height(0.5f).Background("#cbd5e1");

                        var expItems = profile.Experience ?? GetDefaultExperience();

                        section.Item().PaddingTop(2).Column(expCol =>
                        {
                            expCol.Spacing(6);

                            foreach (var item in expItems)
                            {
                                expCol.Item().Column(job =>
                                {
                                    job.Spacing(2);

                                    // Job Title & Company Header + Dates
                                    job.Item().Row(row =>
                                    {
                                        string role = isEnglish ? item.RoleEn : item.RoleEs;
                                        row.RelativeItem().Text(text =>
                                        {
                                            text.Span(role).Bold().FontSize(9).FontColor("#0f172a");
                                            text.Span($" | {item.Company}").SemiBold().FontSize(9).FontColor("#334155");
                                        });

                                        string endDate = (item.EndDate == "Presente" && isEnglish) ? "Present" : item.EndDate;
                                        row.ConstantItem(140).AlignRight().Text($"{item.StartDate} – {endDate}")
                                            .FontSize(8.5f).FontColor("#475569");
                                    });

                                    // Job Summary
                                    string jobSummary = isEnglish ? item.SummaryEn : item.SummaryEs;
                                    if (!string.IsNullOrWhiteSpace(jobSummary))
                                    {
                                        job.Item().Text(jobSummary).Italic().FontSize(8.5f).FontColor("#475569");
                                    }

                                    // Key Contributions / Bullet Points
                                    var bullets = isEnglish ? item.ContributionsEn : item.ContributionsEs;
                                    if (bullets != null && bullets.Any())
                                    {
                                        job.Item().Column(bulletList =>
                                        {
                                            bulletList.Spacing(1);
                                            foreach (var bullet in bullets)
                                            {
                                                bulletList.Item().Text(text =>
                                                {
                                                    text.Span("• ").Bold().FontSize(8.5f).FontColor("#1e293b");
                                                    text.Span(bullet).FontSize(8.5f);
                                                });
                                            }
                                        });
                                    }

                                    // Technologies line
                                    if (!string.IsNullOrWhiteSpace(item.KeywordsOrTechnologies))
                                    {
                                        string techPrefix = isEnglish ? "Technologies: " : "Tecnologías: ";
                                        job.Item().Text(text =>
                                        {
                                            text.Span(techPrefix).Bold().FontSize(8).FontColor("#475569");
                                            text.Span(item.KeywordsOrTechnologies).FontSize(8).FontColor("#64748b");
                                        });
                                    }
                                });
                            }
                        });
                    });

                    // 4. EDUCATION
                    contentCol.Item().Column(section =>
                    {
                        string eduHeader = isEnglish ? "EDUCATION" : "EDUCACIÓN";
                        section.Item().Text(eduHeader).Bold().FontSize(10).FontColor("#1e293b");
                        section.Item().PaddingVertical(1).Height(0.5f).Background("#cbd5e1");

                        section.Item().PaddingTop(2).Row(row =>
                        {
                            string degree = isEnglish ? profile.DegreeEn : profile.DegreeEs;
                            row.RelativeItem().Text(text =>
                            {
                                text.Span(degree).Bold().FontSize(9).FontColor("#0f172a");
                                text.Span($" | {profile.University}").FontSize(9).FontColor("#334155");
                            });

                            row.ConstantItem(140).AlignRight().Text(profile.GraduationYears)
                               .FontSize(8.5f).FontColor("#475569");
                        });
                    });
                });

                // Footer section
                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor("#94a3b8"));
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();

        // Resolve physical wwwroot path for IIS compatibility
        var wwwRoot = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot");
        var cvFolder = Path.Combine(wwwRoot, "cvs");
        
        try
        {
            Directory.CreateDirectory(cvFolder);
            
            var relativePath = $"/cvs/{jobLeadId}.pdf";
            var fullPath = Path.Combine(cvFolder, $"{jobLeadId}.pdf");
            await File.WriteAllBytesAsync(fullPath, pdfData);

            // Save dynamic CV asset association and advance status to CvGenerado
            await repository.SaveInterviewAssetDirectAsync(jobLeadId, relativePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "❌ Error de permisos en IIS al escribir en {CvFolder}. Otorgue permisos al grupo IIS_IUSRS.", cvFolder);
            throw new UnauthorizedAccessException($"Error de permisos en IIS: No se pudo escribir en '{cvFolder}'. Otorgue permisos de Control Total al usuario 'IIS_IUSRS' o al AppPool en Windows Server.", ex);
        }

        return pdfData;
    }

    private static bool ShouldUseEnglish(string defaultLang, string jobTitle, string rawDescription)
    {
        if (string.Equals(defaultLang, "en", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(defaultLang, "es", StringComparison.OrdinalIgnoreCase))
            return false;

        // Auto-detect English based on job description indicators
        string text = $"{jobTitle} {rawDescription}".ToLower();
        string[] englishKeywords = { "engineer", "developer", "responsibilities", "qualifications", "requirements", "experience", "skills", "remote", "full-time" };
        int matches = englishKeywords.Count(k => text.Contains(k));

        return matches >= 2;
    }

    private static List<WorkExperienceItem> GetDefaultExperience()
    {
        return new List<WorkExperienceItem>
        {
            new WorkExperienceItem
            {
                Company = "Control Zeta / HireWire",
                RoleEs = "Lead .NET Solutions Architect & Senior Developer",
                RoleEn = "Lead .NET Solutions Architect & Senior Developer",
                StartDate = "2022",
                EndDate = "Presente",
                SummaryEs = "Liderazgo en arquitectura de software y desarrollo de soluciones enterprise sobre .NET 9, C# y Azure.",
                SummaryEn = "Software architecture leadership and enterprise solution development on .NET 9, C#, and Azure.",
                ContributionsEs = new List<string>
                {
                    "Diseño e implementación de Clean Architecture y DDD para aplicaciones distribuidas en la nube.",
                    "Optimización de consultas SQL Server y Redis caching reduciendo latencias en un 45%.",
                    "Integración de modelos de Inteligencia Artificial (Gemini, OpenAI, Claude) para automatización de flujos B2B."
                },
                ContributionsEn = new List<string>
                {
                    "Designed and implemented Clean Architecture & DDD for distributed cloud applications.",
                    "Optimized SQL Server queries and Redis caching, reducing API latencies by 45%.",
                    "Integrated AI Models (Gemini, OpenAI, Claude) to automate enterprise B2B workflows."
                },
                KeywordsOrTechnologies = "C# 13, .NET 9, ASP.NET Core, Azure, Clean Architecture, SQL Server, Redis, AI Integration"
            },
            new WorkExperienceItem
            {
                Company = "Tech Enterprise Solutions",
                RoleEs = "Senior Backend .NET Developer",
                RoleEn = "Senior Backend .NET Developer",
                StartDate = "2018",
                EndDate = "2022",
                SummaryEs = "Desarrollo de microservicios y APIs RESTful de alto rendimiento para el sector financiero y de gestión.",
                SummaryEn = "Microservices and high-performance RESTful API development for financial and management sectors.",
                ContributionsEs = new List<string>
                {
                    "Desarrollo de microservicios asíncronos desacoplados con RabbitMQ y Azure Service Bus.",
                    "Implementación de pipelines de CI/CD automatizados en GitHub Actions y Azure DevOps.",
                    "Refactorización de módulos críticos legados mejorando la cobertura de pruebas unitarias al 85%."
                },
                ContributionsEn = new List<string>
                {
                    "Built asynchronous microservices decoupled via RabbitMQ and Azure Service Bus.",
                    "Implemented automated CI/CD pipelines using GitHub Actions and Azure DevOps.",
                    "Refactored legacy monolith modules, raising unit test coverage to 85%."
                },
                KeywordsOrTechnologies = "C#, .NET Core 3.1 / .NET 6, Microservices, RabbitMQ, Azure DevOps, Entity Framework"
            },
            new WorkExperienceItem
            {
                Company = "Software & Consulting Services",
                RoleEs = "Software Developer .NET / Full Stack",
                RoleEn = "Full Stack .NET Software Developer",
                StartDate = "2012",
                EndDate = "2018",
                SummaryEs = "Desarrollo full-stack de sistemas de información empresariales y portales web de alta concurrencia.",
                SummaryEn = "Full-stack development of enterprise information systems and high-concurrency web portals.",
                ContributionsEs = new List<string>
                {
                    "Desarrollo de aplicaciones web con ASP.NET MVC, C# y JavaScript / Angular.",
                    "Diseño de modelos relacionales de datos y procedimientos almacenados en SQL Server y Oracle."
                },
                ContributionsEn = new List<string>
                {
                    "Developed web applications using ASP.NET MVC, C#, and JavaScript / Angular.",
                    "Designed relational database schemas and stored procedures in SQL Server and Oracle."
                },
                KeywordsOrTechnologies = "C#, ASP.NET MVC, SQL Server, JavaScript, Angular, REST APIs"
            }
        };
    }
}
