# HireWire - MVP (Fase 1)

HireWire es un sistema automatizado diseñado para la adquisición y gestión de contratos B2B (Contractor) y la optimización de procesos de Overemployment (OE). Actúa como un copiloto inteligente para agilizar el embudo de conversión laboral de perfiles Tech Lead y Arquitectos de Software.

---

## 🛠️ Stack Tecnológico
- **Backend / API**: .NET 9, ASP.NET Core Minimal APIs
- **Lenguaje**: C# 13 (Uso de Primary Constructors, patrones avanzados)
- **Frontend / Portal**: Blazor Web App (Interactive Server render mode)
- **Persistencia**: Entity Framework Core 9.0, SQL Server (Remoto o LocalDB)
- **Extracción de datos**: Extensión de Google Chrome (Manifest V3, JavaScript)
- **Integración de IA**: OpenAI / Claude / Gemini (Evaluaciones manuales en Fase 1)

---

## 📐 Arquitectura de la Solución
El proyecto está estructurado con una arquitectura modular limpia:

1. **HireWire.Domain**: Biblioteca de clases que aloja las entidades de dominio ricas (DDD) y los enums que regulan las reglas de negocio (funnel, perfiles J1 y J2).
2. **HireWire.Application**: Lógica de aplicación, DTOs de integración y firmas de servicios.
3. **HireWire.Infrastructure**: Capa de datos, persistencia con EF Core, inicialización de migraciones y mapeos Fluent API.
4. **HireWire.Web**: Aplicación Blazor Web App que expone el portal administrativo y las APIs de consumo de la extensión de Chrome, integrado con Swagger UI para pruebas interactivas.

---

## 🚀 Inicio Rápido

### Prerrequisitos
- .NET 9 SDK
- SQL Server o LocalDB instalado
- Herramienta `dotnet-ef` instalada globalmente (`dotnet tool install -g dotnet-ef`)

### 1. Configuración de Base de Datos
Actualiza la cadena de conexión en el archivo `HireWire.Web/appsettings.json`.
Luego, en la raíz del proyecto, ejecuta el comando para aplicar la estructura en tu servidor de base de datos:
```bash
dotnet ef database update --project HireWire.Infrastructure --startup-project HireWire.Web
```

*(Opcional: Si prefieres aplicar el script manualmente, el archivo DDL estructurado se encuentra en [InitialCreate.sql](InitialCreate.sql))*

### 2. Ejecutar la Aplicación Web
Para iniciar el servidor de desarrollo de Blazor y la API (incluyendo Swagger):
```bash
dotnet run --project HireWire.Web
```
Una vez iniciado, podrás acceder a:
- Portal Blazor: `https://localhost:XXXX` (verificar puerto asignado)
- Swagger UI: `https://localhost:XXXX/swagger`

---

## 🤖 Extensión de Chrome (Extractor)
Para instalar y configurar el scraper local:
1. Abre Google Chrome y navega a `chrome://extensions`.
2. Activa el **Modo de desarrollador** (esquina superior derecha).
3. Haz clic en **Cargar descomprimida** y selecciona la carpeta `/chrome-extension` ubicada en la raíz de este repositorio.
4. Entra a LinkedIn o Manfred, abre una oferta de empleo y presiona el popup de la extensión para enviarla a tu API local.
