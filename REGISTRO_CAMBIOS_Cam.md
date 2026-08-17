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
  - Inyección de `IHubContext<ResultsHub>` y emisión de eventos `ReceiveResultsUpdate` en tiempo real tras la confirmación de cada voto.
- **[NUEVO] Archivos multimedia en `wwwroot/images/`**:
  - Incorporación de assets visuales (`imagen_voto.png`, `login-background.jpg.jpg`).

---

## 📅 12 de Agosto de 2026 — Implementación de Conteo y Resultados en Tiempo Real (SignalR)

### 📌 Resumen General
Se implementó el conteo de votos y la actualización en vivo de la gráfica de resultados utilizando **ASP.NET Core SignalR**. Cada vez que un elector emite su voto, el sistema notifica en tiempo real a los usuarios viendo los resultados, actualizando el conteo total, porcentajes y barras animadas sin recargar la página.

---

### 🚀 Detalle de Cambios

#### 1. SignalR Hub & Configuración
- **[NUEVO] [ResultsHub.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Hubs/ResultsHub.cs)**:
  - Implementación de Hub SignalR para gestionar salas por elección (`JoinEventGroup` / `LeaveEventGroup`).
- **[MODIFICADO] [Program.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Program.cs)**:
  - Registro del servicio SignalR (`AddSignalR`) y mapeo de ruta `/hubs/resultsHub`.

#### 2. Controlador y Servicios
- **[MODIFICADO] [ResultsController.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Controllers/ResultsController.cs)**:
  - Adición del endpoint `GET /Results/GetLiveData/{id}` con verificación de permisos para obtener el conteo dinámico en formato JSON.
  - Inclusión de `ViewBag.EventId` en el action `Index`.

#### 3. Vista Frontend
- **[MODIFICADO] [Index.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Results/Index.cshtml)**:
  - Integración del cliente JS de SignalR, eventos reactivos para actualizar el DOM en tiempo real y mecanismo de respaldo por polling inteligente cada 5 segundos.

---

## 📅 12 de Agosto de 2026 — Botón para Mostrar/Ocultar Contraseña en el Login

### 📌 Resumen General
Se agregó un botón toggle interactivo con icono de ojo (`visibility` / `visibility_off`) dentro del campo de entrada de contraseña en la vista de **Login**, permitiendo a los usuarios visualizar u ocultar su contraseña ingresada antes de enviar el formulario.

---

### 🚀 Detalle de Cambios

#### 1. Vista de Inicio de Sesión
- **[MODIFICADO] [Login.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Auth/Login.cshtml)**:
  - Incorporación de botón con posicionamiento absoluto e icono de Google Material Symbols (`visibility`).
  - Implementación de script JS cliente para la alternancia del atributo `type` entre `password` y `text`.

---


