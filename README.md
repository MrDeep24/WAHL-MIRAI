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

## 🔐 Credenciales de Acceso y Usuarios de Prueba

Al importar el script de base de datos **`docs/wahl_mirai_db_v2_9_completo.sql`**, la base de datos se cargará con los siguientes usuarios de prueba listos para ingresar:

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
- **Contraseña:** `1001234567.2026`
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

## ⚙️ Configuración y Primer Arranque

### 1. Requisitos del Sistema

- **[.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** (requerido para compilar y ejecutar la solución).
- **MySQL 8.0+** (servidor local MySQL o MariaDB 8.0+, por ejemplo mediante servicio nativo o XAMPP).
- **PowerShell** (utilizado por las herramientas de verificación y el target de compilación de internacionalización).
- **Conexión a Internet:** Requerida **únicamente durante la primera compilación** si el ejecutable independiente de Tailwind CSS (`tailwindcss.exe`) aún no está presente en `WahlMirai.Web/`. Las compilaciones y ejecuciones posteriores operan de forma completamente desconectada (offline).

#### Opcional
- **Graphify:** Para consultar el grafo de conocimiento del repositorio. Instala [`uv`](https://docs.astral.sh/uv/getting-started/installation/) y ejecuta en PowerShell:
  ```powershell
  uv tool install graphifyy --with tree-sitter-sql
  graphify --version
  graphify query "How is the graph refreshed in this repository?"
  ```
  El paquete se llama `graphifyy` y el comando, `graphify`. `graphify-out/` ya está versionado; para actualizarlo, la única ruta soportada es:
  ```powershell
  .\scripts\graphify-refresh.ps1
  ```

---

### 2. Base de Datos (Importación y Semillas)

1. Inicia el servicio de base de datos MySQL en tu entorno local (ej. panel de control de XAMPP o servicio del sistema).
2. Abre tu cliente MySQL preferido (phpMyAdmin en `http://localhost/phpmyadmin/`, MySQL Workbench, o la CLI de MySQL).
3. Ejecuta el script unificado y consolidado:
   ```text
   docs/wahl_mirai_db_v2_9_completo.sql
   ```
   > ⚠️ **ADVERTENCIA:** Este script incluye las sentencias `DROP DATABASE IF EXISTS wahl_mirai_db;` y `CREATE DATABASE wahl_mirai_db;`. Al ejecutarlo, **eliminará y recreará por completo** la base de datos `wahl_mirai_db`, descartando cualquier información previa e inicializando las 17 tablas, vistas y datos semilla.

4. **Auto-cifrado de documentos al arranque:** En el script SQL, los datos semilla contienen documentos con el prefijo provisional `PENDIENTE_CIFRAR:<documento>`. Durante el primer arranque de la aplicación web, el proceso de inicio (`Program.cs`) detecta automáticamente estos registros en texto plano y los cifra de manera idempotente usando ASP.NET Core Data Protection (`IDocumentEncryptionService`), sin requerir ningún comando manual de migración.
5. **Contraseñas semilla:** Las credenciales de acceso iniciales de los usuarios sembrados se encuentran descritas en el encabezado y en los comentarios de la sección final del script `docs/wahl_mirai_db_v2_9_completo.sql`.

---

### 3. Configuración Local

#### Cadena de Conexión a la Base de Datos
La cadena de conexión predeterminada reside en `WahlMirai.Web/appsettings.Development.json` bajo la clave `ConnectionStrings:WahlMiraiDb`:
```json
"ConnectionStrings": {
  "WahlMiraiDb": "Server=localhost;Port=3306;Database=wahl_mirai_db;User=root;Password=;AllowUserVariables=True;"
}
```
*(El archivo `appsettings.json` base no contiene cadenas de conexión).*

Puedes editar este valor directamente en `appsettings.Development.json` para adaptarlo a tu instalación local de MySQL. Si tus credenciales locales difieren y prefieres no modificar ni comprometer archivos versionados en el repositorio, puedes sobreescribirla localmente mediante User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:WahlMiraiDb" "Server=localhost;Port=3306;Database=wahl_mirai_db;User=root;Password=TU_PASSWORD;AllowUserVariables=True;"
```

En el entorno de desarrollo (`Development`), el sistema de configuración de ASP.NET Core otorga mayor precedencia al proveedor de User Secrets sobre los archivos `appsettings.Development.json` y `appsettings.json`, permitiendo que el valor local tome prioridad sin alterar el repositorio.

#### Secretos de Usuario Requeridos (`dotnet user-secrets`)

La configuración confidencial de desarrollo se gestiona mediante el administrador de secretos de usuario de .NET (`UserSecretsId` configurado en `WahlMirai.Web.csproj`). Todos los comandos deben ejecutarse posicionándose dentro de la carpeta `WahlMirai.Web`:

```powershell
cd WahlMirai.Web
```

Para el servicio de correo, configura exclusivamente las claves que no poseen un valor utilizable por defecto en `appsettings.json` y que resultan indispensables para el despacho:

```powershell
# Dirección de correo emisor autorizado
dotnet user-secrets set "EmailSettings:SenderEmail" "no-reply@tu-institucion.edu.co"

# Contraseña o clave de aplicación del correo emisor (secreto sensible)
dotnet user-secrets set "EmailSettings:SenderPassword" "TU_CONTRASEÑA_DE_APLICACION_O_SMTP"

# URL base para los enlaces de los correos (obligatoria para recuperación de acceso; puerto de launchSettings.json)
dotnet user-secrets set "EmailSettings:BaseUrl" "https://localhost:7062"
```

> ℹ️ **Requisito para cuentas Gmail:** Google no admite autenticación con contraseñas convencionales de cuenta para clientes SMTP externos; requiere activar la Verificación en dos pasos (2FA) y generar una [Contraseña de Aplicación](https://myaccount.google.com/apppasswords) de 16 caracteres para asignarla a `EmailSettings:SenderPassword`.
>
> ⚠️ **Importancia de BaseUrl:** Si no se define `EmailSettings:BaseUrl` o es inválida, el worker no puede generar el enlace seguro de restablecimiento y la fila en cola pasa a estado `FALLIDO` con el mensaje: *"EmailSettings:BaseUrl no está configurada o es inválida; no se envió el enlace."*.

Las demás claves de `EmailSettings` (`SmtpHost`, `SmtpPort`, `SenderDisplayName`, `UseStartTls`, `RateLimitPerMinute`) ya cuentan con valores predeterminados en `appsettings.json` y únicamente requieren sobreescritura si utilizas un proveedor SMTP diferente.

Cualquier clave de configuración puede suministrarse alternativamente a través de variables de entorno del sistema operativo utilizando la sintaxis estándar de ASP.NET Core `Section__Key` (por ejemplo, `ConnectionStrings__WahlMiraiDb="Server=..."` o `EmailSettings__BaseUrl="https://localhost:7062"`).

---

### 4. Primer Arranque de la Aplicación

1. Ejecuta la aplicación desde la carpeta `WahlMirai.Web`:
   ```powershell
   cd WahlMirai.Web
   dotnet run
   ```
   *(O desde la raíz del repositorio: `dotnet run --project WahlMirai.Web`)*.

2. Abre tu navegador web en la dirección local asignada según el perfil de `Properties/launchSettings.json`:
   - **HTTPS:** `https://localhost:7062` *(recomendado para probar recuperación de acceso)*
   - **HTTP:** `http://localhost:5166`

3. **Elementos creados y regenerados automáticamente:**
   - **`keys/` (`WahlMirai.Web/keys`):** Almacén de persistencia local para las llaves criptográficas XML de Data Protection API. Si la carpeta está vacía o no existe, ASP.NET Core genera automáticamente las llaves al iniciar.
   - **`App_Data/candidacy-uploads/` (`WahlMirai.Web/App_Data/candidacy-uploads/`):** Carpeta destinada al almacenamiento físico de soportes y planes de gobierno de candidaturas; su contenido está excluido de control de versiones vía `.gitignore`.
   - **`wwwroot/css/site.css` (`WahlMirai.Web/wwwroot/css/site.css`):** Hoja de estilos minificada compilada automáticamente por el target MSBuild de Tailwind CSS antes de la compilación (`tailwindcss.exe -i Styles/input.css -o wwwroot/css/site.css --minify`). Este archivo no se versiona y se regenera en cada compilación.
   - **`logs/` (`WahlMirai.Web/logs/`):** Archivos de registro diario estructurado (`log-*.txt`) administrados por Serilog (excluidos en `.gitignore`).

---

### 5. Verificación del Entorno

Para certificar que el entorno local se encuentra íntegro y listo para desarrollo:

1. **Compilación estricta sin advertencias:**
   ```powershell
   dotnet build WahlMirai.Web --warnaserror
   ```
   Compila el proyecto web tratando cualquier advertencia como error y ejecutando tanto la compilación de estilos Tailwind como la verificación de i18n.

2. **Ejecución de la suite completa de pruebas:**
   ```powershell
   dotnet test WahlMirai.Tests
   ```
   Ejecuta la suite completa de pruebas unitarias y de integración de la solución. *(Nota: al no existir un archivo `.sln` en la raíz, se debe apuntar al proyecto `WahlMirai.Tests`)*.

3. **Auditoría estricta de cobertura de internacionalización:**
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File "scripts/check-i18n-coverage.ps1"
   ```
   Escanea todas las vistas Razor del proyecto y verifica que cada texto en español cuente con clave y traducción exacta para los idiomas soportados en `locale.js`.

---

### 6. Resolución de Problemas Frecuentes (Troubleshooting)

- **Error SMTP 5.7.8 (`Authentication failed` / `Username and Password not accepted`):**
  - **Causa:** Se suministró la contraseña personal estándar de la cuenta Gmail en lugar de una Contraseña de Aplicación, o la cuenta no tiene activada la verificación en dos pasos (2FA).
  - **Solución:** Ve a la configuración de seguridad de Google, asegúrate de activar la verificación en dos pasos, genera una Contraseña de Aplicación en [Google App Passwords](https://myaccount.google.com/apppasswords) y asígnala con `dotnet user-secrets set "EmailSettings:SenderPassword" "xxxx xxxx xxxx xxxx"`.

- **Fila en cola de correo en estado `FALLIDO` por `BaseUrl`:**
  - **Causa:** `EmailSettings:BaseUrl` está vacía o no es una URL absoluta válida (HTTPS o HTTP loopback). Para proteger contra enlaces rotos, el worker marca el correo como `FALLIDO`, registra *"EmailSettings:BaseUrl no está configurada o es inválida; no se envió el enlace."* y purga el token de memoria.
  - **Solución:** Configura la clave localmente apuntando a tu puerto activo de desarrollo:
    ```powershell
    dotnet user-secrets set "EmailSettings:BaseUrl" "https://localhost:7062"
    ```

- **Cooldown de recuperación de contraseña (5 minutos en memoria):**
  - **Causa:** Para mitigar ataques de denegación de servicio y saturación de correo, solicitudes sucesivas de recuperación para un mismo número de documento dentro de una ventana de 5 minutos son retenidas en memoria (`IMemoryCache`, clave `rec:<hash>`). Por diseño anti-enumeración, el sistema muestra siempre la pantalla de éxito genérica, pero descarta en silencio el envío reiterado.
  - **Solución:** Esperar 5 minutos entre solicitudes de prueba. Al ser un almacén en memoria, reiniciar la aplicación web (`dotnet run`) restablece el temporizador inmediatamente.

- **Fallo de compilación por la compuerta de internacionalización (`CheckI18nCoverage`):**
  - **Causa:** Al ejecutar `dotnet build --warnaserror`, cualquier texto visible agregado en vistas Razor que carezca de traducción en `WahlMirai.Web/wwwroot/js/locale.js` hace que el script de auditoría retorne código 1, transformando la advertencia del target de MSBuild en un error de compilación.
  - **Solución:** Ejecuta `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/check-i18n-coverage.ps1` directamente para identificar las cadenas faltantes y sus ubicaciones, e incorpora sus traducciones en `locale.js`.

- **Fallo en la primera compilación por descarga de Tailwind CLI (`EnsureTailwindCli`):**
  - **Causa:** En una máquina sin compilaciones previas donde `tailwindcss.exe` aún no existe en `WahlMirai.Web/`, la ausencia de conexión a Internet provoca que la tarea `<DownloadFile>` de MSBuild falle.
  - **Solución:** Conéctate a Internet para la primera compilación a fin de permitir la descarga del binario `tailwindcss.exe`. Una vez descargado, las compilaciones subsecuentes operan sin conexión.

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

Para mayor detalle técnico, funcional y de diseño, consulta la carpeta [`docs/`](docs):
- [Especificación de Requerimientos de Software (ERS v2.9)](docs/ers_wahl_mirai_v2_9.md)
- [Arquitectura y Diseño de Software v2.9](docs/Arquitectura_y_Diseno_v2_9.md)
- [Script SQL Completo y Consolidado v2.9](docs/wahl_mirai_db_v2_9_completo.sql)