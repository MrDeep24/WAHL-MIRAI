# Wahl Mirai

Sistema de Votación Escolar (ASP.NET Core MVC)
Migrado de un prototipo HTML/Tailwind a un backend real con C#, Entity Framework Core y MySQL.

## 🔐 Credenciales de Acceso (Usuarios de Prueba — Versión 2.6)

Al importar el script de base de datos **`docs/wahl_mirai_db_v2_6_completo.sql`** (o al iniciar el semillero automático `DbInitializer`), la base de datos se cargará con los siguientes usuarios de prueba listos para ingresar:

### 👑 1. Administrador (Rol `ADMIN`)
- **Nombre:** Coordinación Electoral
- **Documento:** `1020304050`
- **Contraseña:** `Admin#2026!`
- **Correo de contacto:** `coordinacion.electoral@colegio.edu.co`
- **Estado:** Activo

---

### 🎓 2. Estudiante / Elector (Rol `ELECTOR`)
- **Nombre:** Ana María López Pérez
- **Documento:** `1001234567`
- **Contraseña:** `1001234567.2026`
- **Correo de contacto:** `acudiente.ana.lopez@example.com`
- **Grado:** 6°
- **Estado:** Activo

> ℹ️ **Novedades en v2.6 (Apertura de Resultados al Finalizar Elección — RN-4.1):**
> **Versión activa: v2.6.** A partir de esta versión, cuando un evento electoral pasa a estado `FINALIZADA`, los resultados se abren automáticamente a **todos los electores cuyos grados estén habilitados** (`event_grades`) para esa elección, sin importar si votaron o no. La verificación se realiza en `ResultsController` contra `voters.grade_id` y `event_grades`, sin ningún cambio en el schema de base de datos. El login sigue siendo **exclusivamente por número de documento** (RN-2). El correo de contacto puede compartirse entre varios electores (ej. acudientes de hermanos, RN-2.1). Las contraseñas temporales no se almacenan en texto claro en la base de datos, sino que se gestionan con hashing BCrypt. Un procesador en segundo plano (`EmailQueueBackgroundService`) lee la cola `email_queue` y despacha progresivamente los correos de credenciales y recuperación usando SMTP, con protección anti-enumeración en los formularios.

---

## 🛠️ Requisitos Previos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [XAMPP](https://www.apachefriends.org/) (Apache y MySQL)

---

## 🚀 Pasos para ejecutar localmente

1. **Levantar Base de Datos (XAMPP)**
   - Abre el panel de control de XAMPP.
   - Inicia los servicios de **Apache** y **MySQL**.
   
2. **Crear el Schema y Cargar Semilla de la Base de Datos**
   - Entra a phpMyAdmin (`http://localhost/phpmyadmin/`) o usa la consola MySQL.
   - Importa o ejecuta el archivo `docs/wahl_mirai_db_v2_6_completo.sql`. Esto creará la estructura completa de la base de datos `wahl_mirai_db` junto con los usuarios de prueba e información semilla.

3. **Configurar la Cadena de Conexión**
   - El proyecto asume por defecto que el usuario `root` de MySQL en XAMPP no tiene contraseña. Si tu XAMPP usa contraseña, edita `WahlMirai.Web/appsettings.Development.json`:
   ```json
   "ConnectionStrings": {
     "WahlMiraiDb": "Server=localhost;Port=3306;Database=wahl_mirai_db;User=root;Password=TU_CONTRASEÑA;AllowUserVariables=True;"
   }
   ```

4. **Configurar el Servicio de Correo (User Secrets)**
   - El sistema requiere credenciales SMTP para enviar correos (como la recuperación de contraseñas). En desarrollo, esto se maneja con User Secrets para evitar subir claves al repositorio:
   ```bash
   cd WahlMirai.Web
   dotnet user-secrets init
   dotnet user-secrets set "EmailSettings:SenderEmail" "TU_CORREO@gmail.com"
   dotnet user-secrets set "EmailSettings:SenderPassword" "TU_APP_PASSWORD"
   ```
   > ℹ️ **Nota:** Si usas Gmail, debes generar una [Contraseña de Aplicación](https://myaccount.google.com/apppasswords).

5. **Compilar y Ejecutar**
   - Asegúrate de estar en la carpeta `WahlMirai.Web` y ejecuta:
     cd WahlMirai.Web
     dotnet build
     dotnet run
     ```
   - Abre el navegador e ingresa a la URL mostrada en consola (ej. `http://localhost:5166`).

---

## 🏗️ Arquitectura y Tecnologías

- **ASP.NET Core 9 MVC**: Arquitectura por capas (Controladores, Servicios, ViewModels, Vistas Razor).
- **Entity Framework Core (Pomelo MySQL)**: Database First.
- **BCrypt.Net-Next**: Hashing seguro de contraseñas.
- **Tailwind CSS**: Estilos compilados con tokens de diseño oficiales (`DESIGN.md`).