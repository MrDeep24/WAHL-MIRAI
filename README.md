# Wahl Mirai

Sistema de Votación Escolar (ASP.NET Core MVC)
Migrado de un prototipo HTML/Tailwind a un backend real con C#, Entity Framework Core y MySQL.

## 🔐 Credenciales de Acceso (Usuarios de Prueba)

Al iniciar el proyecto por primera vez, el sistema ejecutará un **semillero de base de datos automático (`DbInitializer`)** que creará los siguientes usuarios de prueba listos para ingresar:

### 👑 1. Administrador (Rol `ADMIN`)
- **Documento:** `admin.electoral`
- **Contraseña:** `admin123`

---

### 🎓 2. Estudiante / Elector (Rol `ELECTOR`)

#### **Caso A: Estudiante activo (Contraseña lista)**
- **Documento:** `1001234567`
- **Contraseña:** `estudiante123`

#### **Caso B: Nuevo estudiante (Formato RN-2: `{documento}.{año}`)**
- **Documento:** `1007654321`
- **Contraseña inicial:** `1007654321.2026`
*(Requiere cambio de clave obligatorio al ingresar).*

---

## 🛠️ Requisitos Previos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [XAMPP](https://www.apachefriends.org/) (Apache y MySQL)

---

## 🚀 Pasos para ejecutar localmente

1. **Levantar Base de Datos (XAMPP)**
   - Abre el panel de control de XAMPP.
   - Inicia los servicios de **Apache** y **MySQL**.
   
2. **Crear el Schema de la Base de Datos**
   - Entra a phpMyAdmin (`http://localhost/phpmyadmin/`) o usa la consola MySQL.
   - Importa o ejecuta el archivo `docs/wahl_mirai_db_v2.2.sql`. Esto creará la estructura de la base de datos `wahl_mirai_db`.

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