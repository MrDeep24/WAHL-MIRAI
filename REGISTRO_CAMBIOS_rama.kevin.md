# Registro de Cambios y Desarrollo — Wahl Mirai

**Fecha:** 22 de Julio de 2026  
**Proyecto:** Wahl Mirai — Sistema de Votaciones Digitales Estudiantiles (ASP.NET Core MVC)

---

## 📌 Resumen General

Durante el día de hoy se realizó la migración completa del prototipo estático HTML/Tailwind hacia una arquitectura **ASP.NET Core 8 MVC** con **Entity Framework Core (Database First)** conectada a **MySQL (XAMPP)**, garantizando el cumplimiento de las reglas de negocio (RN-1 a RN-8) y requisitos funcionales ERS IEEE 830.

---

## 🚀 Detalle de Cambios Realizados

### 1. Inicialización y Estructura del Proyecto (.NET 8)
- Creación de la solución y proyecto `WahlMirai.Web`.
- Instalación de paquetes NuGet: `Pomelo.EntityFrameworkCore.MySql`, `BCrypt.Net-Next`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Tools` y `AutoMapper`.
- Configuración de la cadena de conexión MySQL a XAMPP en `appsettings.Development.json`.
- Integración de la compilación automática de Tailwind CSS CLI en `WahlMirai.Web.csproj`.

### 2. Capa de Datos y Modelos (EF Core Database First)
- Scaffolding de la base de datos `wahl_mirai_db` a clases C# en la carpeta `Models/` (`Voter`, `Role`, `Grade`, `VotingEvent`, `Candidate`, `CandidateProposal`, `Vote`, `VoterEventParticipation`, `AuditLog`, `AcademicYear`, etc.).
- Mapeo de vistas SQL `VwVoteCount` y `VwActiveCensus` para lecturas optimizadas de resultados y censo.

### 3. Servicios y Lógica de Negocio (`Services/`)
- `AuthService.cs`: Hashing BCrypt, autenticación por documento, validación de usuarios activos, reseteo de claves y hashing SHA-256 de números de documento.
- `CensusService.cs`: Gestión del censo electoral, alta de usuarios, eliminación lógica *soft-delete* (`status = 'ELIMINADO'`), restauración de electores y reseteo de contraseñas.
- `VotingService.cs`: Emisión de voto anónimo criptográfico en `votes` (sin guardar `voter_id`) y registro atómico anti-duplicados en `voter_event_participations` (RN-3).
- `PromotionService.cs`: Algoritmo transaccional de promoción automática por secuencia de grados (`sequence_order`) y egreso de estudiantes en grado final (RN-6).
- `AuditService.cs`: Sistema de auditoría criptográfica en la tabla `audit_log` para trazabilidad de cambios sensibles (RN-8).

### 4. Seguridad, Autenticación y Middleware (`Middleware/`)
- Configuración de Cookie Authentication con políticas de autorización para los roles `ADMIN` y `ELECTOR`.
- `ForcePasswordChangeMiddleware.cs`: Middleware que actúa como guard de servidor, interceptando peticiones de electores con `requiere_cambio_clave = true` y forzando la redirección a `Auth/CambiarClave` (RN-2).

### 5. Controladores (`Controllers/`)
- `AuthController.cs`: Gestión de inicios de sesión, cierres de sesión y flujo de cambio forzado de clave.
- `ElectorController.cs`: Dashboard del estudiante, tarjetón electoral interactivo y recepción del voto.
- `AdminCensusController.cs`: Panel de gestión del censo con modales interactivos y ejecución de la promoción de año lectivo.
- `AdminEventsController.cs`: Vista del listado de procesos electorales configurados.
- `ResultsController.cs`: Visualización de resultados en tiempo real. Aplica restricción de acceso si el elector no ha votado (RN-4) e inmunidad para el administrador (RN-5).
- `HomeController.cs`: Controlador para la página principal de bienvenida.

### 6. Semillero Automático de Base de Datos (`Data/DbInitializer.cs`)
- Implementación de un inicializador automático en C# que, al arrancar la aplicación, verifica si la BD está vacía y crea automáticamente:
  - Año lectivo `2026`.
  - Usuarios de prueba con roles `ADMIN` y `ELECTOR`.
  - Elección de prueba activa (*Personería Estudiantil 2026*) con candidatos, propuestas y fotos.

### 7. Vistas e Interfaz Visual (`Views/`)
- **Página Inicial de Bienvenida (`Views/Home/Index.cshtml`)**: Creada a partir del mockup `pagina inicial de bienvenida`, configurada como la vista inicial del sistema con botón hacia el Login.
- **Vistas del Elector:** `Dashboard.cshtml` y `Votar.cshtml` con modales de propuestas.
- **Vistas del Administrador:** `AdminCensus/Index.cshtml` (con parcial `_PromotionModal.cshtml`) y `AdminEvents/Index.cshtml`.
- **Vista de Resultados:** `Results/Index.cshtml` con gráficos porcentuales y banderas de estado en vivo.
- **Vistas de Autenticación:** `Login.cshtml` y `CambiarClave.cshtml`.
- **Layouts Reutilizables:** `_AdminLayout.cshtml`, `_ElectorLayout.cshtml` y `_Layout.cshtml`.

### 8. Documentación y Optimización de Repositorio
- `README.md`: Documento actualizado con requisitos, guía de ejecución en XAMPP y tabla de **credenciales de acceso de prueba**.
- `.gitignore`: Creado en la raíz para excluir binarios pesados (`bin/`, `obj/`, `tailwindcss.exe`), evitando bloqueos en GitHub por el límite de 100 MB.

---

## 🔐 Credenciales de Prueba Configuradas Hoy

| Rol | Documento | Contraseña | Estado |
| :--- | :--- | :--- | :--- |
| **Administrador** | `admin.electoral` | `admin123` | Activo |
| **Elector** | `1001234567` | `estudiante123` | Activo |
| **Elector (Nuevo)** | `1007654321` | `1007654321.2026` | Requiere cambio de clave |
