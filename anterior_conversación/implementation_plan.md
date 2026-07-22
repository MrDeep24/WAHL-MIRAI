# Plan de Implementación: Wahl Mirai (ASP.NET Core MVC)

El objetivo de este proyecto es construir una aplicación ASP.NET Core 8 MVC utilizando los archivos de diseño y los requerimientos de sistema de Wahl Mirai. La aplicación se conectará a una base de datos MySQL (XAMPP) existente, y utilizará Entity Framework Core con enfoque Database First.

## User Review Required

> [!IMPORTANT]
> **Base de Datos Local (XAMPP)**
> Para ejecutar el paso de **Database First (Scaffold-DbContext)**, la base de datos `wahl_mirai_db` DEBE estar corriendo localmente en el puerto 3306 (XAMPP). Por favor, asegúrate de haber ejecutado el script `wahl_mirai_db_v2.2.sql` en tu phpMyAdmin o consola MySQL antes de que apruebes este plan, ya que de lo contrario el scaffolding fallará.
> La cadena de conexión por defecto asumida es `Server=localhost;Port=3306;Database=wahl_mirai_db;User=root;Password=;AllowUserVariables=True;`.

## Open Questions

Ninguna por el momento. He localizado todos los archivos requeridos (`wahl_mirai_db_v2.2.sql`, `ers_wahl_mirai_v2_2.md`, `wahl_mirai_web.html` y `DESIGN.md`) en el repositorio.

## Proposed Changes

La implementación se dividirá en las siguientes fases:

### 1. Inicialización y Configuración Base

*   **Proyecto**: Creación de la solución `WahlMirai` y el proyecto MVC `WahlMirai.Web` con .NET 8.
*   **Paquetes NuGet**:
    *   `Pomelo.EntityFrameworkCore.MySql`
    *   `Microsoft.EntityFrameworkCore.Design`
    *   `BCrypt.Net-Next`
    *   `AutoMapper` y `AutoMapper.Extensions.Microsoft.DependencyInjection`
*   **Configuración de appsettings**: Inclusión de la cadena de conexión.
*   **Tailwind CSS**: Integración de Tailwind CLI en el archivo `.csproj` para que se compile automáticamente durante el build (`dotnet build`). Extracción de colores y tipografías desde `DESIGN.md`.

### 2. Capa de Datos (Database First)

*   **Scaffolding**: Ejecución del comando de Entity Framework Core (`dotnet ef dbcontext scaffold`) para generar las entidades (`Models`) y el `DbContext` a partir de la base de datos MySQL en vivo.

### 3. Capa de Servicios y Arquitectura

Creación de las interfaces e implementaciones de los servicios que contienen la lógica de negocio (según ERS):
*   `IAuthService` / `AuthService`: Hashing con BCrypt, validación de login, generación de contraseña base.
*   `ICensusService` / `CensusService`: CRUD del censo, soft-delete (RN-7).
*   `IPromotionService` / `PromotionService`: Lógica transaccional de promoción de estudiantes (RN-6).
*   `IVotingService` / `VotingService`: Voto anónimo y registro de participación atómico (RN-3).
*   `IAuditService` / `AuditService`: Registro de cambios sensibles (RN-8).

### 4. Seguridad y Autenticación

*   **Cookie Authentication**: Configuración en `Program.cs`.
*   **Autorización**: Políticas y Claims para los roles `ADMIN` y `ELECTOR`.
*   **Filtro de Cambio de Contraseña**: Middleware o ActionFilter global que intercepte peticiones de usuarios con `requiere_cambio_clave = true` para forzar el cambio antes de usar el sistema.

### 5. Controladores y ViewModels

Desarrollo de los controladores específicos con sus ViewModels e inyección de los servicios.
*   `AuthController`
*   `ElectorController`
*   `AdminCensusController`
*   `AdminEventsController`
*   `ResultsController` (con validación de participación RN-4 para electores).

### 6. Vistas (Razor y Tailwind)

Traducción del archivo `wahl_mirai_web.html` a vistas de Razor separadas por áreas y reuso de layouts:
*   `_AdminLayout.cshtml` y `_ElectorLayout.cshtml`
*   Integración de AutoMapper para alimentar las vistas correctamente sin exponer los Data Models directos.

## Verification Plan

1.  **Ejecución del Scaffolding**: Validar que el `WahlMiraiDbContext` se genere con todas las tablas.
2.  **Pruebas de Flujo**:
    *   Iniciar sesión como Administrador (crear cuenta manual directa en DB con BCrypt para arrancar si no hay seed, aunque el SQL debería tenerlo).
    *   Iniciar sesión como Elector y verificar el guard que exige cambio de contraseña (RN-2).
    *   Verificar el bloqueo del tarjetón si el estudiante ya votó (RN-3).
    *   Verificar que un estudiante no pueda ver resultados si no ha votado (RN-4).
3.  **Compilación de UI**: Validar que el comando `dotnet run` construya de manera automática el archivo final `app.css` con Tailwind.
