# Manual del Desarrollador
## Sistema de Votaciones Digitales (Wahl Mirai)

Este manual proporciona las instrucciones técnicas necesarias para configurar, compilar, ejecutar y mantener el entorno de desarrollo del sistema de elecciones **Wahl Mirai**.

---

## 1. Requisitos de Entorno

Antes de comenzar, asegúrese de tener instalados los siguientes componentes en su máquina de desarrollo:

*   **SDK de .NET:** Versión 6.0, 7.0 u 8.0 (Recomendado .NET 8.0 LTS).
    *   Verifique la instalación ejecutando: `dotnet --version`
*   **Editor de Código / IDE:** 
    *   Visual Studio Code (con la extensión *C# Dev Kit*) o Visual Studio 2022.
*   **Consola de comandos:** PowerShell (Windows) o Terminal Bash (macOS/Linux).

---

## 2. Configuración y Ejecución del Proyecto

El repositorio incluye scripts automatizados en la raíz del proyecto para arrancar la aplicación sin pasos manuales complejos.

### Paso 1: Acceder al Proyecto
Abra su terminal y sitúese en la carpeta raíz del proyecto:
```bash
cd "C:\Users\HUAWEI\OneDrive - SENA\Escritorio\AAAA\Proyecto-Principal"
```

### Paso 2: Ejecución Rápida mediante Scripts
El proyecto contiene scripts autoejecutables que restauran paquetes, compilan el código fuente en C# e inician el servidor local.

*   **En Windows (PowerShell):**
    ```powershell
    ./run.ps1
    ```
*   **En macOS / Linux (Bash):**
    ```bash
    chmod +x run.sh
    ./run.sh
    ```

### Paso 3: Acceso a la Aplicación
Una vez que el host web se inicie, abra su navegador e ingrese a las direcciones configuradas (por defecto):
*   `http://localhost:5000` o `https://localhost:5001`

---

## 3. Lógica del Censo Electoral y Encriptación (Datos en Memoria)

Para el desarrollo rápido, la persistencia de datos está implementada temporalmente en memoria mediante la clase [DataService.cs](file:///c:/Users/HUAWEI/OneDrive - SENA/Escritorio/AAAA/Proyecto-Principal/Services/DataService.cs). 

### Implementación del Cifrado de Datos
> [!IMPORTANT]
> Los datos de los usuarios deben encriptarse. En la fase de desarrollo en memoria, el sistema simula y aplica la lógica de encriptación antes del almacenamiento:
*   **Contraseñas:** Son cifradas mediante hash irreversible utilizando **BCrypt** o el estándar de seguridad de ASP.NET Core (`Microsoft.AspNetCore.Identity.PasswordHasher`).
*   **Identificación (IDs de Votantes):** Se enmascaran o cifran utilizando un algoritmo de encriptación simétrica **AES-256** implementado en un servicio helper (`EncryptionService.cs`), garantizando que los archivos de base de datos o el almacenamiento no contengan IDs de estudiantes en texto plano.

### Inicialización del Censo (Votantes)
El constructor del servicio inicializa la lista de electores (`Voter`) con los siguientes datos del censo:
```csharp
new Voter 
{ 
    Id = 1,
    EncryptedId = "AES_ENCRYPTED_ID_001...", // Identificación única cifrada
    Name = "Alejandro Ruiz", 
    Grade = "9°", 
    Status = "Activo", 
    Email = "alejo.ruiz@wahlmirai.edu.co",
    PasswordHash = "$2a$11$e.kG9...", // BCrypt Hash de la contraseña
    VotedEventIds = new List<int>(), // Historial vacío inicialmente
    RegistrationDate = DateTime.Now
}
```

---

## 4. Guía para Migrar a una Base de Datos Real (SQL Server/MySQL)

Para llevar **Wahl Mirai** a producción, se recomienda sustituir el almacenamiento en memoria por una base de datos relacional persistente mediante **Entity Framework Core**.

### Paso 1: Instalar paquetes NuGet requeridos
Ejecute en la terminal de la raíz del proyecto:
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

### Paso 2: Crear el Contexto de Base de Datos (`ApplicationDbContext`)
Cree un archivo `Data/ApplicationDbContext.cs` con el siguiente contenido:
```csharp
using Microsoft.EntityFrameworkCore;
using mi_proyecto.Models;

namespace mi_proyecto.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Voter> Voters { get; set; }
        public DbSet<VotingEvent> VotingEvents { get; set; }
        public DbSet<VotingCandidate> VotingCandidates { get; set; }
    }
}
```

### Paso 3: Configurar la cadena de conexión en `appsettings.json`
Añada la cadena de conexión a su base de datos local o en la nube:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WahlMiraiDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### Paso 4: Registrar el contexto y el servicio en `Program.cs`
Modifique el archivo [Program.cs](file:///c:/Users/HUAWEI/OneDrive - SENA/Escritorio/AAAA/Proyecto-Principal/Program.cs):
```csharp
// Registrar el contexto EF Core apuntando a SQL Server:
builder.Services.AddDbContext<mi_proyecto.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurar el DataService como servicio Scoped
builder.Services.AddScoped<mi_proyecto.Services.DataService>();
```

### Paso 5: Generar y aplicar Migraciones
En la consola de comandos ejecute:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```
Con esto, la base de datos `WahlMiraiDb` se creará y la persistencia será permanente.
