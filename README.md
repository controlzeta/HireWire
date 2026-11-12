# ⚡ HireWire — Suite de Automatización B2B & Gestión Multicarrera

**HireWire** es una plataforma integral de grado empresarial diseñada para la adquisición, evaluación y gestión de oportunidades de contratación B2B (Contractor) y la administración eficiente de múltiples trayectorias profesionales concurrentes. Funciona como un copiloto inteligente que automatiza desde la captura de vacantes en tiempo real hasta la generación de CVs dinámicos ATS-proof y mensajes de postulación personalizados.

---

## 🛠️ Stack Tecnológico
- **Backend / API**: .NET 9, ASP.NET Core Minimal APIs, Swagger / OpenAPI
- **Lenguaje**: C# 13 (Primary Constructors, Pattern Matching, Record Types)
- **Frontend / UI**: Blazor Web App (Interactive Server Mode) con diseño responsivo, Tablero Kanban dinámico y Cheat Sheets de alto contraste
- **Persistencia**: Entity Framework Core 9.0, Microsoft SQL Server (Transacciones, Concurrency Resolvers, Soft-Delete)
- **Extracción de Datos**: Extensión de Google Chrome (Manifest V3, JavaScript, Toast Injection) con suite de pruebas en Jest + JSDOM
- **Integración de IA Modular**: Pattern Strategy con soporte para **Google Gemini 1.5 Flash**, **OpenAI GPT-4o-mini**, **Anthropic Claude 3.5 Sonnet** y **Mock AI**
- **Generación de Documentos**: QuestPDF (Motor de renderizado de CVs bilingües ATS en formato A4 de 1 sola página)
- **Auditoría & Logs**: Serilog (Archivos rotativos de log y consola web de auditoría en tiempo real)

---

## 📐 Arquitectura del Sistema (Clean Architecture & DDD)

El repositorio está organizado en capas desacopladas que siguen los principios de **Domain-Driven Design (DDD)**:

```text
HireWire/
├── HireWire.Domain/           # Entidades ricas (JobLead, Company, OE_Config, Communication), Enums y Reglas de Negocio
├── HireWire.Application/      # Interfaces, DTOs, Servicios de Dominio (JobLeadService), Estrategias de IA
├── HireWire.Infrastructure/   # EF Core DbContext, Repositorios, Servicios de PDF (QuestPDF), Binder de Configuración
├── HireWire.Web/              # Portal Blazor Server, Endpoints Minimal API, Servidor de PDFs, Worker de Follow-Up
├── HireWire.Tests/            # Suite de pruebas xUnit + Moq + FluentAssertions + Coverlet (120 pruebas)
├── chrome-extension/          # Extractor Manifest V3 para LinkedIn & Manfred con Jest Testing
├── appsettings.template.json  # Plantilla de configuración libre de secretos para Git
└── profile.template.json      # Plantilla de perfil bilingüe libre de secretos para Git
```

---

## 🚀 Funcionalidades Principales

### 1. Extractor Web Inteligente (Chrome Extension Manifest V3)
* **Captura Completa de Datos**: Extrae Título, Empresa, URL limpia, Descripción completa, Rango Salarial, Plataforma y el nombre del Reclutador (*Hiring Manager*).
* **Feedback Visual Inmediato**: Inyecta un sistema de notificaciones Toast animadas en la página activa de LinkedIn o Manfred.
* **Branding LinkedIn-Style**: Popup interactivo con la insignia corporativa `hw` y fallback automático HTTPS ➡️ HTTP.

### 2. Evaluación Multicarrera & Detección de Requisitos Legales por IA
* **Clasificación de Perfil**: Rutea la vacante hacia **J1** (Liderazgo Técnico / Arquitectura Cloud) o **J2** (Contribuidor Individual / Desarrollo Asíncrono).
* **Auditoría de Requisitos Legales (`🚨 [REQUISITO LEGAL]`)**: Detecta automáticamente cláusulas de Ciudadanía obligatoria (*US Citizen required*), Green Card, Security Clearance activa o restricciones de residencia geográfica.
* **Inferencia del Sector / Industria**: Identifica y registra el sector empresarial de la compañía (*Fintech*, *Salud / HealthTech*, *Defensa & Aeroespacial*, *E-Commerce*, *Consultoría TI*).

### 3. Mensajes de Postulación Personalizados
* **Personalización del Candidato**: Lee el perfil bilingüe (`profile.json`) y sustituye automáticamente los comodines `[Candidato]` / `[Candidate]` por tu nombre real (**Francisco Arroyo**).
* **Saludo al Reclutador**: Incorpora automáticamente el nombre del reclutador capturado (*"Hola [RecruiterName]"*).

### 4. Generador de CVs Dinámicos ATS-Proof (1 Página Strict)
* **Layout Unificado de 1 Página**: Tipografía ajustada proporcionalmente y márgenes equilibrados (1.5 cm) para garantizar que todo el contenido quepa en una sola hoja A4.
* **Auto-Detección Bilingüe**: Alterna automáticamente entre Inglés y Español según la redacción de la oferta de trabajo (o fuerza un idioma específico mediante `DefaultLanguage`).
* **Matriz de Experiencia Estructurada**: Renderiza logros, viñetas clave, resúmenes orientados al rol objetivo y tecnologías utilizadas.

### 5. Portal Administrativo & Herramientas de Gestión
* **Command Center Kanban**: Embudo interactivo drag-and-drop con filtros combinados por texto, estado y plataforma.
* **Bajas Lógicas (Soft-Delete)**: El botón de eliminar traslada la vacante al estado `JobLeadStatus.Descartado`, ocultándola del tablero pero conservando el historial completo en SQL Server.
* **Descarga Directa de PDFs**: Endpoint servidor optimizado que sirve el PDF estático de disco con encabezados `Content-Disposition: attachment` para evitar bloqueos SSL en navegadores.
* **Visor de Logs en Tiempo Real (`/logs`)**: Consola interactiva para auditar peticiones, llamadas a la API de IA y eventos del sistema.
* **Cheat Sheets de Entrevista (`/interview-cheat-sheet/{id}`)**: Pantalla completa Dark Mode de alto contraste diseñada para lectura rápida durante videollamadas.
* **Servicio en Segundo Plano (`FollowUpWorker`)**: Tarea cron que identifica vacantes estancadas (más de 4 días sin interacción) para sugerir seguimientos.

---

## 📝 Bitácora de Funcionalidades (Changelog)

> **Nota**: Cada nueva funcionalidad construida en el proyecto se registra en esta sección.

### [2026-11-12] - Suite de pruebas automatizadas (Fase 1 y 2) & Publicación bajo subpath
- **Suite de pruebas `HireWire.Tests` (xUnit + Moq + FluentAssertions + Coverlet)**:
  - Recreación del proyecto de pruebas y agregación a la solución.
  - 120 pruebas unitarias y de integración sobre Domain (entidades, 100% líneas), Application (JobLeadService, AiServiceFactory, AiSettingsManager, MockAiStrategy, fallback a Mock de Gemini/OpenAI/Claude sin API key) e Infrastructure (`JobLeadRepository` contra SQLite in-memory, `DynamicCvGeneratorService` generando PDFs reales con QuestPDF).
  - Cobertura de líneas: **~56%** (Application 61.7%, Domain 100%, Infrastructure 43.1%). Ejecutar con `dotnet test HireWire.Tests --collect:"XPlat Code Coverage"`.
- **Publicación bajo subpath (controlzeta.io/hirewire)**: `<base href>` dinámico desde `NavigationManager.BaseUri`, links relativos, `UsePathBase` opcional por config `PathBase`, redirección HTTPS solo con puerto conocido.

### [2026-08-27] - Branding, Auditoría Legal, Inferencia de Industria & Descargas Estables
- **Identidad de Marca & Branding**:
  - Unificación de títulos de página bajo la convención `<PageTitle>HireWire — [Componente]</PageTitle>`.
  - Creación e integración del favicon corporativo SVG (`favicon.svg`) en la cabecera de la aplicación Web.
  - Creación de la familia de iconos para la Extensión de Chrome (`icon16.png`, `icon48.png`, `icon128.png`, `icon.png`) con insignia estilo LinkedIn `hw`.
- **Auditoría Legal de Ciudadanía & Residencia en IA**:
  - Inserción de reglas de inspección en los prompts de IA (Gemini, OpenAI, Claude y Mock) para alertar sobre requisitos de Ciudadanía Americana, Security Clearance y Residencia en Red Flags con el prefijo `🚨 [REQUISITO LEGAL]`.
- **Inferencia Automática de Industria**:
  - La IA analiza el contexto de la empresa y su descripción para deducir el sector (*Fintech*, *Salud*, *Defensa*, *E-Commerce*) y guardarlo en la entidad `Company`.
  - Refresco en tiempo real del objeto `Lead` en el modal tras ejecutar la evaluación de IA.
- **Reemplazo Automático del Candidato**:
  - Integrada la lectura de `UserProfile:FullName` en `JobLeadService` para reemplazar automáticamente `[Candidato]` / `[Candidate]` por el nombre real del postulante.
- **Optimización de PDF de 1 Sola Página**:
  - Ajuste de tipografía a escala proporcional de 1pt y restauración de márgenes a `1.5 cm`, asegurando un diseño A4 de una sola hoja.
- **Descargas Estables de PDFs**:
  - Endpoint `/cvs/{id:guid}.pdf` actualizado para servir directamente los archivos `.pdf` pre-generados en disco con headers `application/pdf`, eliminando bloqueos de descarga en Chrome/Brave.
- **UX & Avance de Estatus en Tiempo Real en Modal**:
  - Unificados los campos de enrutamiento OE (*Rol Recomendado*, *Match Score %*, *Es Asíncrono*, *Red Flags*) y la barra de insignias de resumen directamente dentro de la tarjeta principal **🤖 Evaluación Automática con IA (Fase 2)**.
  - Al presionar **`⚡ Executar Evaluación IA`**, el modal puebla al instante las insignias de resumen, la caja del *Mensaje de Outreach*, el formulario de edición y el selector desplegable `<select>`, todo en vivo y de forma transparente en un solo bloque.
- **Extracción de Reclutadores en Tarjetas LinkedIn SPA**:
  - Actualizado `extractRecruiterName` en `domExtractor.js` con soporte para tarjetas con clases CSS obfuscadas dinámicas y context-aware matching (*"Conoce al equipo de contratación"*, *"Anunciante del empleo"*), extrayendo limpiamente nombres como **Jeff Schneider** y **Frankie Pham** (eliminando insignias de verificación SVG y titulares).
- **Rediseño Táctico HUD de Cheat Sheet (`/interview-cheat-sheet/{id}`)**:
  - Rediseñada la vista de Cheat Sheet con interfaz HUD de alto contraste (Dark Glassmorphism, 4 cuadrantes con código de color: *Tech Stack*, *Dolores de Negocio*, *Preguntas Incisivas* y *Requisitos Legales / Red Flags*), optimizado para lectura periférica de reojo durante videollamadas.
- **Fase 4 - Real-Time Interview Copilot (Motor de Respuesta Dinámica)**:
  - Implementado `EvaluateFastPromptAsync` en `IAiStrategy` y todas las estrategias (Gemini 1.5 Flash, OpenAI, Claude, Mock) optimizado para respuestas de baja latencia (< 1.5s).
  - Integrado el **Generador STAR (`🎬 Anécdota STAR`)**, **Stack Pivoter (`🔀 Pivot Script`)**, **Spotlight Panic Search (`🚨 Ctrl+K`)** y **Script Salarial (`💵 Script Salarial`)**, **fundamentados 100% en las experiencias y proyectos reales del candidato extraídos de `profile.json`**.
- **Generación Dinámica de Cheat Sheet por IA (`GenerateCheatSheetJsonAsync`)**:
  - Implementado `GenerateCheatSheetJsonAsync` en `JobLeadService.cs` y conectado con el botón **`⚡ Regenerar Cheat Sheet`** para analizar la descripción específica de cada vacante y generar un JSON único con *Dolores de Negocio*, *Tech Stack* y *Preguntas Incisivas* personalizadas por empresa.
  - Actualizado `MockAiStrategy.cs` para inyectar dinámicamente el tema específico de cada dolor de negocio y tecnología solicitada en las respuestas de la IA.
- **Plantillas Seguras para Git**:
  - Creación de `appsettings.template.json` y `profile.template.json` y actualización de `.gitignore` para prevenir la fuga de llaves de API o contraseñas.

### [2026-08-26] - Extractor Chrome, Bajas Lógicas & Matriz Bilingüe
- **Bajas Lógicas (Soft-Delete)**:
  - Implementación del estado `JobLeadStatus.Descartado` (`StatusId = 8`) para borrado lógico preservando el historial.
- **Captura del Reclutador**:
  - Creación de `extractRecruiterName(doc)` en `domExtractor.js` para raspar el nombre del reclutador en LinkedIn.
- **Esquema Bilingüe QuestPDF**:
  - Implementación del motor de generación bilingüe de CVs con la clase `WorkExperienceItem`.

---

## 🚀 Inicio Rápido

### Prerrequisitos
- .NET 9 SDK
- SQL Server o LocalDB instalado
- Herramienta `dotnet-ef` instalada globalmente (`dotnet tool install -g dotnet-ef`)

### 1. Configuración de Archivos de Entorno
Copia las plantillas sin secretos a los archivos de configuración local:
```bash
cp appsettings.template.json HireWire.Web/appsettings.json
cp profile.template.json HireWire.Web/profile.json
```
Edita `HireWire.Web/appsettings.json` con tu cadena de conexión SQL Server y tus llaves de API de IA (`GeminiApiKey`, `OpenAiApiKey`, etc.).

### 2. Migración de Base de Datos
En la raíz del repositorio, ejecuta:
```bash
dotnet ef database update --project HireWire.Infrastructure --startup-project HireWire.Web
```

### 3. Ejecución del Servidor Web
```bash
dotnet run --project HireWire.Web
```
Acceso disponible en:
* **HTTPS**: `https://localhost:7165/`
* **HTTP**: `http://localhost:5033/`
* **Swagger UI**: `https://localhost:7165/swagger`

### 4. Pruebas Unitarias y Cobertura
```bash
dotnet test HireWire.Tests --collect:"XPlat Code Coverage"
```
La cobertura actual es ~56% de líneas (Domain 100%). El reporte se genera en `HireWire.Tests/TestResults/`.

---

## 🤖 Instalación de la Extensión de Chrome (Extractor)
1. Abre Google Chrome y ve a `chrome://extensions`.
2. Activa el **Modo de desarrollador** (esquina superior derecha).
3. Haz clic en **Cargar descomprimida** y selecciona la carpeta `/chrome-extension`.
4. Para ejecutar las pruebas unitarias:
   ```bash
   cd chrome-extension
   npm install
   npm test
   ```
