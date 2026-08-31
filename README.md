# Wahl Mirai

**Sistema de Votaciones Digitales Estudiantiles (ASP.NET Core 9.0 MVC)**  
Plataforma institucional de votaciones con censo cerrado validado por lista blanca, autopostulación de candidatos con aprobación administrativa, parametrización de elecciones por etapas cronológicas, escrutinio en tiempo real vía WebSockets, canal de ayuda asistido por chatbot y gestión jerárquica de roles.

---

## 📌 Novedades y Cambios Clave en la Versión 2.8

1. **Auto-registro mediante Lista Blanca (`census_whitelist`):**
   - Se elimina el alta centralizada de cuentas por parte del Administrador.
   - El Administrador carga la lista blanca del censo escolar (documento, nombre, grado).
   - El estudiante completa su propio registro (definiendo su correo de contacto y contraseña segura) siempre que su documento figure en dicha lista (RN-1, RN-1.1, RF-M01-00).
2. **Renombramiento de entidad central (`users`):**
   - La tabla `voters` pasa a ser `users` para alojar de forma unificada tanto a los electores como a las cuentas del personal administrativo con distintos niveles jerárquicos.
3. **Autopostulación y Aprobación Administrativa Obligatoria:**
   - Los electores se postulan a sí mismos como candidatos durante la etapa de inscripción de la elección.
   - Adjuntan propuestas, plan de gobierno y documentos soporte según los requisitos del cargo.
   - Las candidaturas no se publican en el tarjetón hasta que el Administrador las revisa y aprueba (con soporte para *Aprobación con Excepción* o *Rechazo con motivo obligatorio*).
4. **Catálogo de Cargos Electorales y Requisitos (`election_positions` & `position_requirements`):**
   - Cada proceso electoral se asocia a un cargo formal (ej. Personero, Contralor, Representante) con sus propios requisitos documentales preconfigurados y reutilizables.
5. **Etapas Secuenciales del Proceso Electoral:**
   - Cada elección cuenta con tres ventanas independientes de fecha y hora: **Inscripción de Candidatos**, **Consulta de Propuestas** y **Votación**, con transición automática de estados.
6. **Jerarquía Administrativa de Dos Niveles:**
   - Incorporación del rol `SUPER_ADMIN`, facultado para crear, editar y gestionar otras cuentas administrativas y asignar cargos institucionales en texto libre (`position_title`), sin permitir autoeliminación.
7. **Chatbot de Ayuda Basado en Reglas (M08):**
   - Asistente conversacional interactivo guiado por palabras clave y menús de opciones en el módulo de Ayuda, con capacidad de derivar a un ticket PQR precargado.

---

## 🔐 Credenciales de Acceso y Usuarios de Prueba (Versión 2.8)

Al importar el script de base de datos **`docs/wahl_mirai_db_v2_8_completo.sql`** (o al iniciar el semillero automático), la base de datos se cargará con los siguientes usuarios de prueba listos para ingresar:

### 👑 1. Súper Administrador (Rol `SUPER_ADMIN`)
- **Nombre:** Coordinación Electoral
- **Documento:** `1020304050`
- **Contraseña:** `Admin#2026!`
- **Cargo Institucional:** Súper Administrador
- **Correo de contacto:** `coordinacion.electoral@colegio.edu.co`
- **Estado:** Activo

---

### 🎓 2. Estudiante / Elector Ya Registrado (Rol `ELECTOR`)
*(Simula un estudiante que ya completó su auto-registro)*
- **Nombre:** Ana María López Pérez
- **Documento:** `1001234567`
- **Contraseña real verificada:** `1001234567.2026`
- **Correo de contacto:** `acudiente.ana.lopez@example.com`
- **Grado:** 6°
- **Estado:** Activo

---

### 📋 3. Elector en Lista Blanca (Aún no reclamado — Para probar Auto-registro)
*(Permite probar el flujo público de **Crear mi cuenta / Auto-registro**)*
- **Documento:** `1015998877`
- **Nombre precargado:** Sofía Ramírez Torres
- **Grado asignado:** 6°
- **Flujo:** Ingresar a *Crear mi cuenta* (`/Auth/SelfRegister`), ingresar el documento `1015998877` y definir su propio correo y contraseña.

> ℹ️ **Mecanismos de Seguridad:**
> - El inicio de sesión es **exclusivamente por número de documento** y contraseña (RN-2).
> - El correo de contacto se utiliza para notificaciones y recuperación, permitiendo compartirse entre hermanos/acudientes (RN-2.1).
> - Contraseñas protegidas mediante hashing **BCrypt**.
> - Documentos resguardados con hash **SHA-256** determinístico para búsquedas y cifrado reversible **AES-256** mediante Data Protection API para visualización administrativa.

---

## 🛠️ Requisitos Previos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [XAMPP](https://www.apachefriends.org/) (Apache y MySQL / MariaDB 8.0+)

---

## 🚀 Pasos para Ejecutar Localmente

1. **Iniciar Servicios en XAMPP**
   - Abre el panel de control de XAMPP e inicia **Apache** y **MySQL**.

2. **Crear Schema e Importar Datos Semilla**
   - Ingresa a phpMyAdmin (`http://localhost/phpmyadmin/`) o a tu cliente MySQL preferido.
   - Ejecuta el script unificado **`docs/wahl_mirai_db_v2_8_completo.sql`**. Este script creará la base de datos `wahl_mirai_db`, las 17 tablas, vistas y los datos semilla iniciales.

3. **Configurar la Cadena de Conexión**
   - Revisa la configuración en `WahlMirai.Web/appsettings.Development.json`. Por defecto se conecta a `localhost:3306` con usuario `root` sin contraseña:
   ```json
   "ConnectionStrings": {
     "WahlMiraiDb": "Server=localhost;Port=3306;Database=wahl_mirai_db;User=root;Password=;AllowUserVariables=True;"
   }
   ```

4. **Configurar el Servicio de Correo SMTP (User Secrets)**
   - El sistema despacha notificaciones y recuperaciones de acceso a través de una cola progresiva (`EmailQueueService`). Configura tus credenciales de desarrollo en consola:
   ```bash
   cd WahlMirai.Web
   dotnet user-secrets init
   dotnet user-secrets set "EmailSettings:SenderEmail" "TU_CORREO@gmail.com"
   dotnet user-secrets set "EmailSettings:SenderPassword" "TU_APP_PASSWORD"
   ```
   > ℹ️ **Nota:** Si usas Gmail, requiere una [Contraseña de Aplicación](https://myaccount.google.com/apppasswords).

5. **Compilar y Ejecutar**
   - Desde la raíz del proyecto o desde `WahlMirai.Web`:
   ```bash
   cd WahlMirai.Web
   dotnet build
   dotnet run
   ```
   - Abre tu navegador en la URL asignada (ej. `http://localhost:5166` o `https://localhost:7166`).

---

## 🏗️ Arquitectura y Tecnologías

- **Backend:** C# / ASP.NET Core 9.0 MVC (Controladores, Servicios, ViewModels, Vistas Razor).
- **Autenticación & Autorización:** Cookie Authentication con Claims (`Microsoft.AspNetCore.Authentication.Cookies`) y control de roles (`ELECTOR`, `ADMIN`, `SUPER_ADMIN`).
- **Persistencia & ORM:** Entity Framework Core con `Pomelo.EntityFrameworkCore.MySql` (Database First).
- **Seguridad Criptográfica:** Hashing BCrypt para contraseñas, SHA-256 para búsqueda determinística de documentos y AES-256 (Data Protection API) para almacenamiento reversible.
- **Frontend & Estilos:** Tailwind CSS v4 (compilado localmente mediante binario standalone) con sistema de tokens semánticos institucionales y JavaScript ES6+.
- **Tiempo Real:** WebSockets / SignalR Hub (`ElectionResultsHub`) para transmisión de escrutinio en vivo.
- **Worker en Segundo Plano:** `EmailQueueBackgroundService` con `System.Threading.Channels` para despacho progresivo y controlado de correos.

---

## 📚 Documentación del Proyecto

Para mayor detalle técnico, funcional y de diseño, consulta la carpeta [`docs/`](file:///c:/Users/APRENDIZ/Documents/tun.wh/WAHL-MIRAI/docs):
- [Especificación de Requerimientos de Software (ERS v2.8)](file:///c:/Users/APRENDIZ/Documents/tun.wh/WAHL-MIRAI/docs/ers_wahl_mirai_v2_8.md)
- [Arquitectura y Diseño de Software v2.8](file:///c:/Users/APRENDIZ/Documents/tun.wh/WAHL-MIRAI/docs/Arquitectura_y_Diseno_v2_8.md)
- [Script SQL Completo y Consolidado v2.8](file:///c:/Users/APRENDIZ/Documents/tun.wh/WAHL-MIRAI/docs/wahl_mirai_db_v2_8_completo.sql)