# Arquitectura y Diseño de Software
## Sistema de Votaciones Digitales (Wahl Mirai)

Este documento describe la arquitectura técnica, la estructura del proyecto en ASP.NET Core MVC, el modelo de datos y el flujo de control del sistema de elecciones **Wahl Mirai**.

---

## 1. Arquitectura del Sistema
El sistema se desarrolla utilizando el patrón de arquitectura **Model-View-Controller (MVC)** provisto por ASP.NET Core y se fundamenta en tecnologías estándares y lenguajes fuertemente definidos:

```mermaid
graph LR
    User([Usuario / Elector / Admin]) <--> Views[Capa de Vistas - HTML5 / CSS3 / Vanilla JS]
    Views <--> Controllers[Capa de Controladores - C# / .NET]
    Controllers <--> Services[Capa de Servicios / Cifrado]
    Services <--> Models[Modelos / Entidades de Dominio]
```

*   **Tecnologías de Frontend:**
    *   **HTML5** semántico para estructurar el contenido de forma accesible.
    *   **Vanilla CSS (CSS3)** estructurado con variables y tokens para lograr una interfaz premium y coherente con el manual de marca sin depender de frameworks CSS adicionales.
    *   **Vanilla JS** para interactividad reactiva en tiempo real (por ejemplo, recarga automática de gráficos e inputs del tarjetón).
*   **Tecnologías de Backend:**
    *   **C#** como lenguaje único de backend.
    *   **.NET (ASP.NET Core MVC)** para la lógica de servidor, enrutamiento y autenticación basada en cookies/sesiones.
*   **Servicio de Seguridad y Datos (Services):**
    *   `DataService.cs` y helper de encriptación encargado de realizar hashing de contraseñas y cifrado simétrico (AES-256) sobre datos de identidad sensibles (como la identificación única de los votantes).

---

## 2. Estructura de Directorios del Proyecto Simplificado

Tras remover los módulos no deseados, la estructura del proyecto quedará organizada de la siguiente manera:

```
Proyecto-Principal/
│
├── Controllers/
│   ├── AccountController.cs       # Control de Login, Registro y Roles
│   ├── HomeController.cs          # Redirección inicial
│   ├── VotingController.cs        # Gestión Electoral y Escrutinio en Vivo (Administrador)
│   └── EleccionesController.cs    # Interfaz de Votación y Consulta de Resultados (Elector)
│
├── Models/
│   ├── VotingEvent.cs             # Modelo para Eventos Electorales
│   ├── VotingCandidate.cs         # Modelo para Candidatos
│   ├── Voter.cs                   # Modelo completo de Elector (Votante)
│   └── ErrorViewModel.cs          # Errores globales del sistema
│
├── Services/
│   ├── DataService.cs             # Persistencia simulada y lógica de negocio
│   └── EncryptionService.cs       # Cifrado de datos sensibles (AES-256 y BCrypt)
│
├── Views/
│   ├── Account/
│   │   └── Login.cshtml           # Vista de inicio de sesión
│   ├── Home/
│   │   └── Index.cshtml           # Landing de redirección
│   ├── Voting/                    # Vistas para el Administrador
│   │   ├── Index.cshtml           # Dashboard electoral y KPIs en vivo
│   │   ├── Crear.cshtml           # Formulario para nueva elección
│   │   ├── Registro.cshtml        # Historial de elecciones
│   │   └── Detalle.cshtml         # Escrutinio y ganadores
│   ├── Elecciones/                # Vistas para el Elector
│   │   ├── Index.cshtml           # Elecciones disponibles
│   │   ├── Inscripcion.cshtml     # Postularse como candidato
│   │   ├── Votar.cshtml           # Tarjetón interactivo
│   │   └── Resultados.cshtml      # Resultados estudiantiles en tiempo real
│   └── Shared/
│       ├── _Layout.cshtml         # Estructura HTML base
│       ├── _AdminLayout.cshtml    # Menú lateral y barra superior (Admin)
│       └── _StudentLayout.cshtml  # Menú lateral y barra superior (Estudiante)
│
└── wwwroot/                       # Archivos estáticos (CSS, JS, Imágenes, Fuentes)
```

---

## 3. Modelo de Datos (Diagrama Entidad-Relación)

A continuación se detalla el modelo de datos de **Wahl Mirai**, incluyendo los campos del registro del votante y el cifrado de información:

```mermaid
erDiagram
    VotingEvent ||--o{ VotingCandidate : "contiene"
    Voter ||--o{ VotingEvent : "participa_en"
    
    VotingEvent {
        int Id PK
        string Title
        DateTime StartDate
        DateTime EndDate
        bool IsClosed
    }
    
    VotingCandidate {
        int Id PK
        int VotingEventId FK
        string Name
        string Proposal
        int VotesCount
    }
    
    Voter {
        int Id PK
        string EncryptedId "AES-256 Encrypted ID escolar"
        string Name "Nombre completo"
        string Email "Correo institucional"
        string PasswordHash "BCrypt / PBKDF2 Password Hash"
        string Grade "Grado del alumno"
        string Status "Activo / Inactivo"
        List_int VotedEventIds "Historial de elecciones donde ya sufragó"
        DateTime RegistrationDate
    }
```

---

## 4. Diagrama de Flujo del Proceso de Votación y Consulta en Vivo

El siguiente diagrama de secuencia detalla cómo los electores emiten su voto y cómo todos los actores visualizan los resultados en tiempo real durante la elección:

```mermaid
sequenceDiagram
    actor Voter as Elector
    participant App as Aplicación (EleccionesController)
    participant DB as Almacenamiento (DataService)
    actor Admin as Administrador

    Note over Voter, Admin: Consulta de Resultados en Tiempo Real (En todo momento)
    Voter->>App: Solicita ver resultados parciales (Resultados.cshtml)
    App->>DB: Consultar conteo actual de votos en tiempo real
    DB-->>App: Retorna votos acumulados de la elección activa
    App-->>Voter: Renderiza gráfico en vivo con barras actualizadas
    
    Admin->>App: Monitorea Dashboard electoral (Index / Detalle)
    App->>DB: Consultar votos y KPI de participación
    DB-->>App: Retorna métricas en vivo
    App-->>Admin: Muestra resultados y ganadores en vivo

    Note over Voter, DB: Flujo de Votación Segura
    Voter->>App: Solicita ingresar al tarjetón (Index / Votar)
    App->>DB: Validar si Voter ya votó en esta Elección (VotedEventIds)
    alt Voter ya votó
        DB-->>App: Retorna: "Ya votó"
        App-->>Voter: Muestra mensaje de error / Comprobante de Voto
    else Voter NO ha votado
        DB-->>App: Retorna: "Apto para votar"
        App->>DB: Obtener lista de candidatos para la elección
        DB-->>App: Retorna candidatos
        App-->>Voter: Renderiza Tarjetón Electoral (Votar.cshtml)
        Voter->>App: Selecciona Candidato y presiona "Votar"
        App->>DB: Registrar voto e incorporar Id de Elección al Voter
        DB-->>App: Confirmación de registro de voto exitoso
        App-->>Voter: Redirige automáticamente a Resultados.cshtml en Tiempo Real
    end
```
