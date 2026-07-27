# Wahl Mirai

Sistema de Votación Escolar (ASP.NET Core MVC)
Migrado de un prototipo HTML/Tailwind a un backend real con C#, Entity Framework Core y MySQL.

## 🔐 Credenciales de Acceso (Usuarios de Prueba — Versión 2.3)

Al importar el script consolidado de base de datos **`docs/wahl_mirai_db_v2.3_completo.sql`** (o al iniciar el semillero automático `DbInitializer`), la base de datos se cargará con los siguientes usuarios de prueba listos para ingresar:

### 👑 1. Administrador (Rol `ADMIN`)
- **Nombre:** Coordinación Electoral
- **Documento:** `admin.electoral`
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

> ℹ️ **Nota sobre Requerimientos v2.3 (RN-2 & RN-2.1):**
> En la versión 2.3 se incorporó el **correo de contacto obligatorio** para cada elector. El login continúa siendo mediante el documento de identidad. Las contraseñas en entorno de producción son autogeneradas aleatoriamente por el sistema y notificadas al correo registrado a través de la cola de notificaciones (`email_queue`). Se elimina la obligatoriedad de cambiar la clave en el primer inicio de sesión.

---

## 🛠️ Requisitos Previos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [XAMPP](https://www.apachefriends.org/) (Apache y MySQL)

---

## 🚀 Pasos para ejecutar localmente

1. **Levantar Base de Datos (XAMPP)**
   - Abre el panel de control de XAMPP.
   - Inicia los servicios de **Apache** y **MySQL**.
   
2. **Crear el Schema y Cargar Semilla de la Base de Datos**
   - Entra a phpMyAdmin (`http://localhost/phpmyadmin/`) o usa la consola MySQL.
   - Importa o ejecuta el archivo `docs/wahl_mirai_db_v2.3_completo.sql`. Esto creará la estructura completa de la base de datos `wahl_mirai_db` junto con los usuarios de prueba e información semilla.

3. **Configurar la Cadena de Conexión**
   - El proyecto asume por defecto que el usuario `root` de MySQL en XAMPP no tiene contraseña. Si tu XAMPP usa contraseña, edita `WahlMirai.Web/appsettings.Development.json`:
   ```json
   "ConnectionStrings": {
     "WahlMiraiDb": "Server=localhost;Port=3306;Database=wahl_mirai_db;User=root;Password=TU_CONTRASEÑA;AllowUserVariables=True;"
   }
   ```

4. **Compilar y Ejecutar**
   - Abre la terminal en la carpeta `WahlMirai.Web`:
     ```bash
     cd WahlMirai.Web
     dotnet build
     dotnet run
     ```
   - Abre el navegador e ingresa a la URL mostrada en consola (ej. `http://localhost:5030`).

---

## 🏗️ Arquitectura y Tecnologías

- **ASP.NET Core 8 MVC**: Arquitectura por capas (Controladores, Servicios, ViewModels, Vistas Razor).
- **Entity Framework Core (Pomelo MySQL)**: Database First.
- **BCrypt.Net-Next**: Hashing seguro de contraseñas.
- **Tailwind CSS**: Estilos compilados con tokens de diseño oficiales (`DESIGN.md`).