using System.Collections.Generic;

namespace HireWire.Application.DTOs;

/// <summary>
/// Structured work experience item for ATS-scannable CV generation.
/// </summary>
public class WorkExperienceItem
{
    public string Company { get; set; } = "";
    public string RoleEs { get; set; } = "";
    public string RoleEn { get; set; } = "";
    public string StartDate { get; set; } = "";
    public string EndDate { get; set; } = "";
    public string SummaryEs { get; set; } = "";
    public string SummaryEn { get; set; } = "";
    public List<string> ContributionsEs { get; set; } = new();
    public List<string> ContributionsEn { get; set; } = new();
    public string KeywordsOrTechnologies { get; set; } = "";
}

/// <summary>
/// Options class for configuring personal candidate details used in CV generation with bilingual support.
/// </summary>
public class UserProfileOptions
{
    public string FullName { get; set; } = "Francisco Arroyo";
    public string TitleEs { get; set; } = "Ingeniero de Software Senior | Especialista .NET 9 & Arquitectura Cloud";
    public string TitleEn { get; set; } = "Senior Software Engineer | .NET 9 & Cloud Architecture Specialist";
    public string Email { get; set; } = "francisco.arroyo@controlzeta.com.mx";
    public string Phone { get; set; } = "+52 220 274 1952";
    public string LocationEs { get; set; } = "Puebla, México";
    public string LocationEn { get; set; } = "Puebla, Mexico (Remote)";
    public string LinkedInUrl { get; set; } = "linkedin.com/in/francisco-arroyo-dev/";
    public string DefaultLanguage { get; set; } = "es"; // "es" or "en"

    public string SummaryJ1Es { get; set; } = "Ingeniero de Software Senior y Arquitecto de Soluciones con más de 12 años de experiencia liderando el diseño y desarrollo de sistemas cloud-native sobre .NET 9, C# y Azure. Especialista en la orquestación de equipos técnicos, definición de directrices de Clean Architecture y migraciones complejas de sistemas monolíticos a microservicios.";
    public string SummaryJ1En { get; set; } = "Senior Software Engineer and Solutions Architect with over 12 years of experience leading the design and development of cloud-native systems using .NET 9, C#, and Azure. Specialist in technical team orchestration, Clean Architecture guidelines, and complex monolithic to microservice migrations.";

    public string SummaryJ2Es { get; set; } = "Desarrollador Senior .NET altamente enfocado en el desarrollo ágil y asíncrono. Especialista en C# 13, .NET 9, APIs RESTful de alto rendimiento y optimización de bases de datos relacionales (SQL Server, Oracle). Orientado a la resolución eficiente de requerimientos técnicos complejos con mínima supervisión.";
    public string SummaryJ2En { get; set; } = "Senior .NET Developer focused on agile and asynchronous development. Specialist in C# 13, .NET 9, high-performance RESTful APIs, and relational database optimization (SQL Server, Oracle). Driven by autonomous technical problem solving with minimal supervision.";

    public string LanguagesAndFrameworks { get; set; } = "C# 13, .NET 9, ASP.NET Core, Angular 18, React, Microservices";
    public string Databases { get; set; } = "Microsoft SQL Server, PostgreSQL, Oracle Database, Redis";
    public string CloudAndDevOps { get; set; } = "Microsoft Azure, Docker, CI/CD Pipelines, GitHub Actions";

    public string DegreeEs { get; set; } = "Licenciatura en Informática";
    public string DegreeEn { get; set; } = "B.S. in Computer Science & Information Technology";
    public string University { get; set; } = "Instituto Tecnológico de Puebla";
    public string GraduationYears { get; set; } = "2006 - 2012";

    public List<WorkExperienceItem> Experience { get; set; } = new()
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
