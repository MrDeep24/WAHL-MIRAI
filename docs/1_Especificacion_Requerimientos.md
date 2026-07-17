# Especificación de Requerimientos de Software (SRS)
## Sistema de Votaciones Digitales (Wahl Mirai)

Este documento detalla los requerimientos funcionales, no funcionales, reglas de negocio y especificaciones tecnológicas indispensables para el desarrollo y validación del nuevo sistema de elecciones **Wahl Mirai**.

---

## 1. Introducción
El propósito de **Wahl Mirai** es proporcionar una plataforma web segura, intuitiva y transparente para la gestión y ejecución de procesos electorales en entornos educativos. El sistema centraliza la organización de elecciones (como Personero y Representante de Estudiantes), permitiendo a los electores votar de manera digital y confidencial, y visualizando los resultados parciales y finales en tiempo real para todos los participantes.

---

## 2. Actores del Sistema

El sistema reconoce dos perfiles de usuario bien delimitados:

1.  **Administrador (Gestor Electoral):**
    *   Personal administrativo responsable de planificar y supervisar las elecciones.
    *   Tiene control total sobre la creación, modificación y cierre de elecciones.
    *   Gestiona los candidatos y supervisa las estadísticas del censo electoral.
2.  **Elector (Votante / Estudiante):**
    *   Estudiantes matriculados que ejercen su derecho al voto.
    *   Pueden ver las convocatorias electorales correspondientes a su grado.
    *   Tienen la opción de postularse como candidatos (inscripción de candidaturas).
    *   Emiten un voto secreto y de carácter único por elección.

---

## 3. Requerimientos Funcionales (RF)

### RF-1: Gestión de Acceso y Sesión
*   El sistema debe permitir el inicio de sesión seguro mediante credenciales de usuario (correo electrónico y contraseña).
*   El sistema debe redirigir dinámicamente al usuario según su rol:
    *   **Administrador:** Redirección al Dashboard de Elecciones del Administrador.
    *   **Elector:** Redirección al Panel de Elecciones del Estudiante.

### RF-2: Gestión de Elecciones (Administrador)
*   **Crear Elección:** El administrador debe poder crear un evento electoral especificando:
    *   Título (ej. *Elección de Personero 2026*).
    *   Fecha y hora de inicio.
    *   Fecha y hora de finalización (cierre automático).
    *   Censo Electoral (grados escolares habilitados para votar).
*   **Historial de Elecciones:** Panel con buscador y filtrado por estado (Activa, Pendiente, Finalizada) para supervisar eventos pasados y en curso.
*   **Eliminar/Suspender Elección:** Capacidad de cancelar un proceso electoral antes de su inicio o durante el transcurso en caso de anomalías.

### RF-3: Inscripción y Gestión de Candidatos
*   El sistema debe permitir registrar candidatos asociados a una elección específica.
*   Cada candidato tendrá:
    *   Nombre completo.
    *   Avatar basado en iniciales (ej. `CT` para Camilo Torres) o foto.
    *   Lema de campaña.
    *   Propuestas de trabajo detalladas.
*   **Inscripción Autónoma (Elector):** Un elector puede postular su propia candidatura en elecciones configuradas para auto-inscripción, la cual requerirá la aprobación del Administrador.

### RF-4: Proceso de Votación Activa (Elector)
*   **Visualización de Tarjetón:** El elector visualizará los candidatos disponibles para su grado con sus respectivas propuestas.
*   **Voto en Blanco:** El tarjetón debe incluir siempre la opción de "Voto en Blanco" como opción válida de sufragio.
*   **Confirmación de Voto:** Al seleccionar una opción, el elector debe confirmar su selección antes de que el voto sea contabilizado definitivamente para evitar clics accidentales.

### RF-5: Control de Voto Único (Garantía de Sufragio)
> [!IMPORTANT]
> **Regla de Negocio Crítica:** El sistema debe garantizar estrictamente que un elector pueda votar **únicamente una vez** por evento electoral.
*   Una vez que el elector confirma su voto, el sistema debe marcar al elector como "Sufragó" en esa elección específica.
*   Si un elector intenta ingresar nuevamente al tarjetón de una elección en la que ya votó, el sistema debe restringir el acceso y mostrar un comprobante digital de votación (sin revelar la opción elegida para mantener el voto secreto).

### RF-6: Escrutinio en Tiempo Real
> [!IMPORTANT]
> **Resultados Visibles para Todos:** Los resultados de las elecciones deben ser visibles en tiempo real para todos los actores (Administradores y Electores) en todo momento del proceso electoral.
*   **Escrutinio Público Continuo:** El sistema actualizará las gráficas de votación y el conteo de votos de forma instantánea conforme se registren. 
*   Tanto estudiantes como administradores tendrán acceso a pantallas con barras de porcentaje o gráficos actualizados para seguir el avance electoral en vivo.

---

## 4. Requerimientos No Funcionales (RNF)

*   **RNF-1: Seguridad, Privacidad e Integridad de Datos:**
    *   El voto debe guardarse de forma disociada de la identidad del votante para asegurar que el sufragio sea 100% confidencial y anónimo.
    *   **Encriptación de Datos de Usuario:** Toda información sensible de los usuarios (como contraseñas, correos y tokens de identificación) debe estar encriptada. Las contraseñas deben cifrarse usando algoritmos de hashing seguros (ej. BCrypt o PBKDF2), y los datos de identidad sensibles se protegerán utilizando algoritmos de cifrado simétrico estándar como AES-256 en la persistencia.
    *   Se debe implementar una suma de verificación o simulación de auditoría criptográfica (tipo Blockchain) para certificar que el conteo no ha sido alterado.
*   **RNF-2: Interfaz Premium y Responsividad (Aesthetics):**
    *   La interfaz debe ser visualmente moderna, con micro-animaciones en botones, tarjetas con efectos de hover, y una paleta de colores limpia (predominando azules institucionales oscuros `#062542` y acentos contrastantes).
    *   Debe ser totalmente responsiva para que los estudiantes puedan votar desde smartphones, tablets o computadoras de escritorio.
*   **RNF-3: Rendimiento y Concurrencia:**
    *   El sistema debe soportar picos de concurrencia al inicio de la jornada electoral cuando múltiples aulas ingresan a votar simultáneamente. El almacenamiento en memoria o base de datos debe resolver las solicitudes en menos de 500 ms.

---

## 5. Definición Tecnológica y Lenguajes de Programación

Para el desarrollo del proyecto **Wahl Mirai** se han definido estrictamente las siguientes tecnologías:

*   **Backend (Lógica de Servidor y Negocio):**
    *   **Lenguaje:** C#
    *   **Framework:** .NET (ASP.NET Core MVC)
*   **Frontend (Interfaz de Usuario):**
    *   **Estructura y Contenido:** HTML5 Semántico
    *   **Estilizado (Diseño Visual):** CSS3 (Vanilla CSS estructurado y ordenado mediante variables/tokens)
    *   **Interactividad:** JavaScript (Vanilla JS para dinamismo reactivo en el tarjetón, filtros y carga de resultados en vivo)

---

## 6. Datos Necesarios para el Registro del Votante

El censo electoral o el registro individual de cada elector en **Wahl Mirai** debe capturar y mantener la siguiente información obligatoria:

1.  **Identificación Única (ID):** Matrícula o cédula escolar que identifica inequívocamente al elector (almacenada de forma encriptada/protegida).
2.  **Nombre Completo:** Nombre(s) y apellidos del elector para fines de auditoría del censo.
3.  **Correo Electrónico:** Correo institucional que actúa como nombre de usuario para iniciar sesión.
4.  **Contraseña Encriptada:** Hash seguro del password para autenticación.
5.  **Grado Escolar:** Grado o curso al que pertenece (ej. *11-A*), utilizado para determinar las elecciones correspondientes.
6.  **Estado:** Condición del elector (ej. *Activo* para poder votar, *Suspendido* o *Inactivo*).
7.  **Historial de Participación (VotedEventIds):** Lista de identificadores de las elecciones en las que el usuario ya emitió su voto.
8.  **Fecha de Registro:** Marca de tiempo de cuándo fue dado de alta el votante.
