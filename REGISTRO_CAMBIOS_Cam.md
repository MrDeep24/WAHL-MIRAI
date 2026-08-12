# Registro de Cambios y Desarrollo — Wahl Mirai

**Proyecto:** Wahl Mirai — Sistema de Votaciones Digitales Estudiantiles (ASP.NET Core MVC)  
**Developer:** `Cam`

---

## 📅 12 de Agosto de 2026 — Actualizaciones de UI, Vistas de Autenticación, Votación e Index

### 📌 Resumen General
Registro inicial de cambios realizados por **Cam**. Se realizaron optimizaciones de diseño, estructuración visual y mejoras de interfaz en múltiples vistas Razor del proyecto (`Index`, `Login`, `CambiarClave`, `Votar`, `Recuperar`, `Exito`), así como ajustes en las hojas de estilo CSS e interfaz de servicios.

---

### 🚀 Detalle de Cambios

#### 1. Vistas de Autenticación y Acceso
- **[MODIFICADO] [CambiarClave.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Auth/CambiarClave.cshtml)**:
  - Ajustes de diseño e interfaz en el formulario de cambio de clave.
- **[MODIFICADO] [Login.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Auth/Login.cshtml)**:
  - Actualizaciones visuales y mejoras de estilos en la vista de inicio de sesión.
- **[MODIFICADO] [Recuperar.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/RecuperacionAcceso/Recuperar.cshtml)**:
  - Rediseño y optimizaciones visuales en el flujo de recuperación de acceso.
- **[MODIFICADO] [Exito.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/RecuperacionAcceso/Exito.cshtml)**:
  - Ajustes de presentación en la pantalla de confirmación exitosa de recuperación.

#### 2. Vistas de Inicio y Votación
- **[MODIFICADO] [Index.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Home/Index.cshtml)**:
  - Reestructuración de la página principal del sistema con mejoras de maquetación UI/UX.
- **[MODIFICADO] [Votar.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Elector/Votar.cshtml)**:
  - Refactorización de la pantalla de votación para electores.

#### 3. Estilos y Servicios
- **[MODIFICADO] [site.css](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/wwwroot/css/site.css)**:
  - Ajustes generales en los estilos globales de la aplicación.
- **[MODIFICADO] [IVotingService.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Services/IVotingService.cs)**:
  - Actualización en las definiciones de contrato del servicio de votación.
- **[NUEVO] Archivos multimedia en `wwwroot/images/`**:
  - Incorporación de assets visuales (`imagen_voto.png`, `login-background.jpg.jpg`).

---
