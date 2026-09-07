# ESPECIFICACIÓN DE REQUERIMIENTOS DE SOFTWARE
**(ERS — IEEE Std 830-1998)**

## Sistema de Votaciones Digitales Estudiantiles
**Wahl Mirai — Versión 2.8**

* **Programa:** Análisis y Desarrollo de Software
* **Servicio Nacional de Aprendizaje — SENA**
* **Ficha:** 228118
* **Colombia, 2026**
* **Control de versión:** v2.8 — Cambio estructural mayor respecto a v2.7:
  1. **Auto-registro de electores mediante lista blanca:** se elimina el alta directa de cuentas por parte del Administrador (RN-1 anterior). El Administrador ahora carga una **lista blanca** del censo autorizado (documento, nombre, grado); el elector completa su propio registro (correo de contacto y contraseña) siempre que su documento figure en dicha lista. La tabla `voters` se renombra a **`users`** para reflejar que ahora también aloja cuentas administrativas con distintos roles.
  2. **Autopostulación de candidatos con aprobación administrativa obligatoria:** el elector se postula a sí mismo como candidato, carga su plan de gobierno y los documentos de requisito exigidos por el cargo electoral al que aspira; ninguna candidatura es visible ni válida hasta que el Administrador la aprueba.
  3. **Catálogo de Cargos Electorales y requisitos de elegibilidad:** cada elección se asocia a un cargo (Personero, Contralor, Representante de Curso, etc.) con requisitos documentales predefinidos (ej. certificado de haber cursado y aprobado 10° para Personero).
  4. **Etapas del proceso electoral:** toda elección se divide ahora en tres etapas con ventanas de fecha/hora independientes — Inscripción de Candidatos, Consulta de Propuestas y Votación — con transición automática de estado.
  5. **Jerarquía administrativa:** se incorpora el rol `SUPER_ADMIN`, con los mismos permisos operativos que `ADMIN`, pero exclusivo para crear y gestionar otras cuentas administrativas, incluyendo un cargo institucional descriptivo en texto libre (ej. "Orientador").
  6. **Chatbot de Ayuda basado en reglas:** el módulo de Ayuda (M08) incorpora un asistente conversacional por palabras clave/menú guiado (sin IA generativa) que escala a la creación de una PQR si no resuelve la duda del usuario.
  * *Versión anterior (v2.7): incorporación del módulo M08 (Ayuda, Tutorial y PQR) en su forma estática original.*

---

## TABLA DE CONTENIDO
1. [Introducción](#1-introducción)
   1.1 [Propósito](#11-propósito)
   1.2 [Alcance del Sistema](#12-alcance-del-sistema)
   1.3 [Definiciones, Acrónimos y Abreviaturas](#13-definiciones-acrónimos-y-abreviaturas)
   1.4 [Referencias](#14-referencias)
2. [Descripción General del Sistema](#2-descripción-general-del-sistema)
   2.1 [Perspectiva del Producto](#21-perspectiva-del-producto)
   2.2 [Funciones Principales del Sistema](#22-funciones-principales-del-sistema)
3. [Reglas de Negocio Transversales](#3-reglas-de-negocio-transversales)
4. [Requerimientos Específicos por Módulo](#4-requerimientos-específicos-por-módulo)
   4.1 [M01 — Gestión de Acceso, Auto-registro y Sesión](#41-m01--gestión-de-acceso-auto-registro-y-sesión)
   4.2 [M02 — Gestión del Censo Electoral (Exclusivo Administrador)](#42-m02--gestión-del-censo-electoral-exclusivo-administrador)
   4.3 [M03 — Gestión de Elecciones, Cargos y Etapas](#43-m03--gestión-de-elecciones-cargos-y-etapas)
   4.4 [M04 — Autopostulación y Aprobación de Candidatos](#44-m04--autopostulación-y-aprobación-de-candidatos)
   4.5 [M05 — Proceso de Votación y Control de Voto Único](#45-m05--proceso-de-votación-y-control-de-voto-único)
   4.6 [M06 — Escrutinio y Resultados en Tiempo Real (Acceso Condicionado)](#46-m06--escrutinio-y-resultados-en-tiempo-real-acceso-condicionado)
   4.7 [M07 — Perfil de Usuario y Autogestión de Credenciales](#47-m07--perfil-de-usuario-y-autogestión-de-credenciales)
   4.8 [M08 — Ayuda, Tutorial, PQR y Chatbot](#48-m08--ayuda-tutorial-pqr-y-chatbot)
   4.9 [M09 — Gestión de Cuentas Administrativas (Exclusivo Súper Administrador)](#49-m09--gestión-de-cuentas-administrativas-exclusivo-súper-administrador)
   4.10 [Requerimientos No Funcionales (RNF)](#410-requerimientos-no-funcionales-rnf)
5. [Referencias Bibliográficas](#5-referencias-bibliográficas)

---

## 1. Introducción

### 1.1 Propósito
Este documento define los requerimientos para el sistema **'Wahl Mirai' Versión 2.8**, que incorpora los siguientes cambios respecto a la versión anterior (2.7):

1. **Eliminación del alta centralizada de cuentas de acceso.** El Administrador ya no crea directamente las credenciales de los electores. En su lugar, carga una **lista blanca** (documento, nombre, grado) que autoriza a cada estudiante a completar su propio registro.
2. **Auto-registro del elector.** Todo elector cuyo documento figure en la lista blanca puede crear su propia cuenta, definiendo él mismo su correo de contacto y su contraseña, sin intervención del Administrador.
3. **Renombramiento de la tabla `voters` a `users`.** Dado que el sistema ahora aloja formalmente varios roles administrativos además de electores, se adopta una nomenclatura neutral para la entidad central de cuentas.
4. **Autopostulación de candidatos.** Cualquier elector habilitado puede postularse a sí mismo como candidato durante la etapa de inscripción de una elección, sin que el Administrador deba asignarlo manualmente.
5. **Aprobación administrativa obligatoria de candidaturas.** Ninguna postulación es visible para otros electores ni válida para el tarjetón hasta que el Administrador la revisa y aprueba explícitamente, evitando candidatos sin propuestas o con propuestas sin sentido.
6. **Plan de gobierno y documentos de soporte.** El candidato debe cargar un documento de plan de gobierno y los soportes documentales exigidos por el cargo al que aspira (ej. certificado de haber cursado y aprobado determinado grado para postularse a Personería).
7. **Catálogo de Cargos Electorales.** Se introduce un catálogo reutilizable de cargos (Personero, Contralor, Representante de Curso, etc.), cada uno con sus propios requisitos de elegibilidad predefinidos.
8. **Aprobación con excepción.** El Administrador puede aprobar una candidatura aunque falten documentos de requisito, siempre dejando constancia explícita de dicha decisión en el sistema.
9. **Etapas del proceso electoral.** Toda elección se estructura ahora en tres etapas con ventanas de fecha/hora propias: Inscripción de Candidatos, Consulta de Propuestas y Votación, con transición automática entre estados.
10. **Jerarquía administrativa de dos niveles.** Se incorpora el rol `SUPER_ADMIN`, con idénticos permisos operativos a `ADMIN`, pero con la facultad exclusiva de crear y administrar otras cuentas administrativas, incluyendo un campo de cargo institucional en texto libre.
11. **Chatbot de Ayuda basado en reglas.** El módulo de Ayuda incorpora un asistente conversacional guiado por palabras clave o menú (sin inteligencia artificial generativa), que escala a la creación de una PQR si no logra resolver la duda del usuario.

### 1.2 Alcance del Sistema
Wahl Mirai permite gestionar elecciones estudiantiles mediante un censo cerrado y persistente cuya activación de cuentas ocurre por auto-registro validado contra una lista blanca administrada por el colegio, garantizando voto único, anonimato y trazabilidad de cambios administrativos. El sistema cubre la gestión de acceso y auto-registro, la administración del censo y su lista blanca, la configuración de elecciones por etapas con cargos electorales y requisitos, la autopostulación y aprobación de candidatos con sus propuestas y planes de gobierno, la emisión del voto, el escrutinio en tiempo real, la gestión jerárquica de cuentas administrativas y un canal de ayuda con chatbot y PQR.

### 1.3 Definiciones, Acrónimos y Abreviaturas

| Término / Acrónimo | Definición |
| :--- | :--- |
| **ERS** | Especificación de Requerimientos de Software. |
| **RN** | Regla de Negocio. |
| **RF** | Requerimiento Funcional. |
| **RNF** | Requerimiento No Funcional. |
| **JWT** | JSON Web Token, mecanismo de autenticación basado en tokens firmados. |
| **BCrypt** | Algoritmo de hashing seguro utilizado para almacenar contraseñas. |
| **`users`** | Tabla central de cuentas del sistema (antes `voters`); aloja tanto electores como cuentas administrativas (`ADMIN`, `SUPER_ADMIN`). |
| **Censo electoral** | Conjunto de estudiantes habilitados para votar, reflejado primero en la lista blanca y luego, tras el auto-registro, como cuenta activa en `users`. |
| **Lista blanca (`census_whitelist`)** | Listado precargado por el Administrador (documento, nombre, grado) que autoriza a un estudiante a completar su propio registro; por sí sola no constituye una cuenta de acceso ni permite iniciar sesión. |
| **Auto-registro** | Mecanismo por el cual un elector, tras verificar que su documento figura en la lista blanca, crea su propia cuenta definiendo su correo de contacto y su contraseña. |
| **Grado** | Nivel académico del elector (ej. 6°, 7°, ..., 11°). |
| **Promoción automática** | Mecanismo que avanza masivamente el grado de todos los usuarios electores activos al iniciar un nuevo año lectivo. |
| **Egresado** | Estado de un elector que completó el último grado y ya no pertenece al censo activo. |
| **Eliminación lógica** | Cambio de estado de un registro a 'Eliminado' sin borrarlo físicamente de la base de datos. |
| **WebSocket** | Protocolo de comunicación bidireccional utilizado para actualizar resultados en tiempo real. |
| **Correo de contacto** | Correo electrónico obligatorio registrado por cada elector (propio o de su acudiente) durante el auto-registro; usado exclusivamente para recuperación de acceso y notificaciones, nunca como identificador de login. |
| **Cola de envío progresivo** | Mecanismo que distribuye en el tiempo el envío de correos (recuperación, reasignación, respuesta de PQR, notificación de candidatura), respetando límites de tasa del proveedor de correo. |
| **PQR** | Petición, Queja o Reclamo. Solicitud en texto libre que un usuario autenticado radica ante el Administrador, con ciclo de vida de dos estados (Abierto/Resuelto) y una única respuesta administrativa. |
| **Cargo electoral** | Categoría de una elección (ej. Personero, Contralor, Representante de Curso) que define un conjunto de requisitos de elegibilidad predefinidos que debe cumplir todo aspirante a candidato. |
| **Autopostulación** | Acción del propio elector de inscribirse como candidato a una elección durante su etapa de Inscripción, sin intervención previa del Administrador. |
| **Plan de gobierno** | Documento formal cargado por el candidato con su propuesta integral de gestión, complementario a la lista breve de propuestas mostrada en el tarjetón. |
| **Aprobación con excepción** | Decisión del Administrador de aprobar una candidatura pese a no contar con la totalidad de los documentos de requisito exigidos, dejando constancia explícita de dicha situación en el sistema. |
| **Etapa electoral** | Cada una de las tres fases secuenciales de un proceso electoral — Inscripción de Candidatos, Consulta de Propuestas, Votación — delimitada por su propia ventana de fecha y hora, con transición automática. |
| **SUPER_ADMIN** | Rol administrativo con los mismos permisos operativos que `ADMIN`, más la facultad exclusiva de crear, editar o eliminar lógicamente otras cuentas administrativas. |
| **Cargo institucional** | Campo descriptivo en texto libre (ej. "Orientador", "Coordinador Académico") asignado a una cuenta administrativa por el Súper Administrador; es metadata informativa y no otorga ni restringe permisos. |
| **Chatbot de Ayuda** | Asistente conversacional basado en reglas y palabras clave (sin inteligencia artificial generativa) integrado al módulo de Ayuda, que guía al usuario hacia contenido existente o hacia la creación de una PQR si no resuelve su duda. |

### 1.4 Referencias
Ver [sección 5 — Referencias Bibliográficas](#5-referencias-bibliográficas).

---

## 2. Descripción General del Sistema

### 2.1 Perspectiva del Producto
Wahl Mirai es una aplicación web cliente-servidor de uso interno institucional, dirigida a colegios que requieren digitalizar sus procesos de votación estudiantil (personerías, contralorías, representantes de curso u otras figuras de gobierno escolar). El sistema define tres roles: **Elector** (estudiante habilitado para votar y postularse como candidato), **Administrador** (`ADMIN`, personal del colegio a cargo de la configuración, el censo, las candidaturas y las PQR) y **Súper Administrador** (`SUPER_ADMIN`, con los mismos permisos operativos que `ADMIN`, más la facultad exclusiva de gestionar otras cuentas administrativas).

### 2.2 Funciones Principales del Sistema
* Auto-registro de electores validado contra una lista blanca precargada por el Administrador, con definición propia de correo de contacto y contraseña.
* Autenticación segura por identificador único y contraseña.
* Gestión de la lista blanca y del censo activo: carga, consulta, modificación y eliminación lógica de usuarios.
* Promoción automática anual del grado de los electores.
* Configuración de elecciones por etapas (Inscripción, Propuestas, Votación), asociadas a un cargo electoral con requisitos predefinidos.
* Autopostulación de candidatos con carga de plan de gobierno y documentos de soporte, sujeta a aprobación administrativa obligatoria.
* Emisión de voto único, secreto y anónimo, con confirmación explícita tras revisar las propuestas del candidato.
* Escrutinio y visualización de resultados en tiempo real, condicionados al estado de la elección y al rol del usuario.
* Gestión jerárquica de cuentas administrativas por parte del Súper Administrador.
* Canal de ayuda con contenido estático ilustrado, chatbot guiado por reglas y radicación/gestión de PQR.

---

## 3. Reglas de Negocio Transversales

* **RN-1 — Auto-registro Mediante Lista Blanca:** No existe alta directa de cuentas de acceso por parte del Administrador. El Administrador únicamente carga y mantiene una **lista blanca** del censo autorizado (documento, nombre, grado) mediante `census_whitelist`. Todo elector cuyo documento figure en la lista blanca puede completar su propio registro, definiendo su correo de contacto y su contraseña. Un documento que no figure en la lista blanca no puede completar el auto-registro bajo ninguna circunstancia.
* **RN-1.1 — Unicidad del Auto-registro:** Cada entrada de la lista blanca solo puede reclamarse una única vez. Al completar el auto-registro, la entrada correspondiente de `census_whitelist` queda marcada como reclamada (`claimed_at`, `claimed_by_user_id`) y no admite un segundo auto-registro con el mismo documento.
* **RN-2 — Credenciales Definidas por el Propio Usuario:** El login continúa siendo por identificador único (documento) y contraseña, nunca por correo. La contraseña inicial ya no es asignada por el sistema: el propio elector la define durante su auto-registro (RF-M01-00), cumpliendo los mismos requisitos de complejidad exigidos en el cambio de contraseña desde el Perfil (mínimo 8 caracteres, al menos una mayúscula y al menos un símbolo especial). El sistema conserva la generación aleatoria de contraseña únicamente para los flujos de recuperación de acceso (RF-M01-02) y reasignación administrativa (RF-M07-02), entregada exclusivamente por correo.
* **RN-2.1 — Correo de Contacto Obligatorio:** Todo elector debe definir un correo de contacto (propio o de su acudiente) durante su auto-registro. Un mismo correo de contacto puede estar asociado a más de un elector (por ejemplo, hermanos); la unicidad del sistema se garantiza por documento (`document_hash`), no por correo. Este correo se usa única y exclusivamente para recuperación de acceso y notificaciones del sistema; en ningún caso se usa como identificador de inicio de sesión.
* **RN-3 — Voto Único y Bloqueo Seguro:** Cada elector puede votar únicamente una vez por evento electoral. Al confirmar el sufragio, su estado se actualiza irreversiblemente.
* **RN-4 — Resultados en Tiempo Real Condicionados al Voto (Elección Activa):** Mientras la elección se encuentra en la etapa de Votación (estado `ACTIVA`) o en `PROGRAMADA`, los electores tienen permitido ver los gráficos y estadísticas de escrutinio en vivo, siempre y cuando hayan ejercido previamente su derecho al voto en dicha elección. Si no han votado, el acceso al panel de visualización estará estrictamente bloqueado.
* **RN-4.1 — Apertura de Resultados al Finalizar la Elección:** Al pasar `voting_events.status` a `FINALIZADA`, el acceso a resultados se habilita automáticamente para todo elector cuyo `grade_id` pertenezca a los grados registrados en `event_grades` para dicha elección, sin condición de haber votado. Esta verificación se realiza en la capa de aplicación (`ResultsController`), sin cambios en el esquema de resultados.
* **RN-5 — Excepción de Roles Administrativos en Escrutinio:** Tanto `ADMIN` como `SUPER_ADMIN` pueden visualizar los resultados en tiempo real de forma irrestricta en cualquier momento y etapa, sin necesidad de cumplir la condición de voto.
* **RN-6 — Persistencia del Censo Electoral:** Ningún registro de `users` ni de `census_whitelist` se elimina físicamente de la base de datos. La identidad del elector (documento, nombre) se conserva de un año lectivo a otro; el único dato académico que varía es el grado, actualizado mediante el mecanismo de promoción automática (RF-M02-02).
* **RN-7 — Edición Administrativa con Inmutabilidad del Voto:** El Administrador puede modificar cualquier dato de un usuario elector (nombre, documento, grado, estado) y eliminarlo de forma lógica en cualquier momento. Sin embargo, los registros de votación ya emitidos son absolutamente inmutables y no se ven afectados por ninguna modificación o eliminación lógica del perfil del elector.
* **RN-7.1 — Eliminación Lógica de Procesos Electorales:** El Administrador puede realizar la eliminación lógica de cualquier proceso electoral cambiando su estado a 'Eliminado' y registrando la fecha de baja (`deleted_at`). Sus candidatos, propuestas, planes de gobierno y votos ya emitidos permanecen estrictamente inmutables e íntegros para trazabilidad y auditoría.
* **RN-8 — Trazabilidad de Cambios Administrativos:** Toda modificación, eliminación lógica, restauración, promoción masiva, carga de lista blanca, aprobación o rechazo de candidatura, y gestión de cuentas administrativas queda registrada en el log de auditoría (usuario responsable, campo afectado, valor anterior, valor nuevo y fecha).
* **RN-9 — Entrega Progresiva de Notificaciones por Correo:** Cuando el sistema deba enviar múltiples notificaciones en una sola operación, no se envían todas de manera simultánea. El sistema las procesa mediante una cola con control de tasa, evitando saturar al proveedor de correo institucional. Aplica a recuperación de acceso, reasignación de contraseña, respuesta de PQR y notificación de aprobación/rechazo de candidatura.
* **RN-10 — Autopostulación y Aprobación Obligatoria de Candidaturas:** Cualquier elector habilitado puede postularse a sí mismo como candidato durante la etapa de Inscripción de una elección, sin intervención previa del Administrador. Ninguna candidatura es visible para el resto de electores ni aparece en el tarjetón hasta que el Administrador la aprueba explícitamente. El Administrador puede rechazar una candidatura, indicando obligatoriamente el motivo en texto libre, notificado al elector por correo (`email_type = 'CANDIDATURA_RECHAZADA'`).
* **RN-10.1 — Aprobación con Excepción:** El Administrador puede aprobar una candidatura aunque no cuente con la totalidad de los documentos de requisito exigidos para el cargo, siempre que dicha decisión quede registrada explícitamente como "Aprobación con Excepción", indicando qué requisitos quedaron pendientes.
* **RN-11 — Requisitos de Candidatura por Cargo Electoral:** Cada elección se asocia a un cargo electoral (`election_positions`) del catálogo institucional (ej. Personero, Contralor, Representante de Curso), el cual define un conjunto de requisitos de elegibilidad (`position_requirements`, ej. certificado de haber cursado y aprobado determinado grado). El candidato debe cargar un documento de soporte por cada requisito obligatorio del cargo al postularse.
* **RN-12 — Etapas del Proceso Electoral:** Todo proceso electoral se compone de tres etapas secuenciales y no superpuestas: (1) Inscripción de Candidatos, (2) Consulta de Propuestas, (3) Votación. Cada etapa tiene su propia ventana de fecha y hora, configurada por el Administrador al crear la elección. El sistema transiciona automáticamente el estado (`PROGRAMADA → INSCRIPCION → PROPUESTAS → ACTIVA → FINALIZADA`) según dichas fechas, sin intervención manual.
* **RN-13 — Jerarquía de Roles Administrativos:** El sistema reconoce dos roles con capacidad administrativa: `ADMIN` y `SUPER_ADMIN`. Ambos poseen idénticos permisos operativos sobre censo, elecciones, candidaturas y PQR. La única distinción es que solo `SUPER_ADMIN` puede crear, editar o eliminar lógicamente otras cuentas administrativas, incluyendo la asignación de un cargo institucional descriptivo en texto libre (ej. "Orientador"), campo que es únicamente informativo y no otorga ni restringe permisos.

---

## 4. Requerimientos Específicos por Módulo

### 4.1 M01 — Gestión de Acceso, Auto-registro y Sesión

#### RF-M01-00 — Auto-registro de Electores Contra Lista Blanca
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M01-00 |
| **Nombre** | Auto-registro de Electores Contra Lista Blanca |
| **Descripción** | Permite a un estudiante crear su propia cuenta de acceso, siempre que su documento figure en la lista blanca cargada previamente por el Administrador (`census_whitelist`). El estudiante define su propio correo de contacto y contraseña; no existe entrega de credenciales por parte del sistema en este flujo. |
| **Prioridad** | Alta |
| **Precondición** | Existe una entrada en `census_whitelist` con el documento del estudiante, no reclamada previamente (`claimed_at IS NULL`). |
| **Postcondición** | Se crea un nuevo registro en `users` con rol `ELECTOR`, estado `ACTIVO`, y la entrada de `census_whitelist` queda marcada como reclamada. |
| **Flujo normal** | 1. El estudiante accede a la pantalla pública 'Crear mi cuenta'.<br>2. Ingresa su número de documento.<br>3. El sistema verifica su existencia y disponibilidad en `census_whitelist`.<br>4. Si es válido, el estudiante completa correo de contacto, contraseña y confirmación de contraseña, cumpliendo los requisitos de complejidad (mínimo 8 caracteres, una mayúscula, un símbolo especial).<br>5. El sistema crea el registro en `users` (nombre y grado heredados de la lista blanca, no editables en este paso), marca la entrada de la lista blanca como reclamada y otorga acceso directo. |
| **Flujo alternativo** | 3a. Si el documento no figura en la lista blanca, o ya fue reclamado, el sistema muestra un mensaje genérico sin confirmar ni negar el motivo exacto (evita enumeración de usuarios) y sugiere contactar al Administrador.<br>4a. Si la contraseña no cumple los requisitos de complejidad, el sistema impide continuar y señala las reglas faltantes. |
| **Condición especial** | Este flujo reemplaza por completo la generación de contraseñas aleatorias que existía en versiones anteriores para el alta inicial (ver RN-2). El nombre y el grado nunca son editables por el estudiante en este flujo; provienen exclusivamente de la lista blanca. |

#### RF-M01-01 — Autenticación de Usuarios por Identificador Único
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M01-01 |
| **Nombre** | Autenticación de Usuarios por Identificador Único |
| **Descripción** | Permite el acceso seguro de Electores, Administradores y Súper Administradores utilizando su documento o código registrado y contraseña, prescindiendo de correos institucionales para el login. |
| **Prioridad** | Alta |
| **Precondición** | El usuario cuenta con una cuenta activa en `users`, creada mediante auto-registro (elector) o mediante RF-M09-01 (cuentas administrativas). |
| **Postcondición** | Se genera una sesión autenticada y se redirige según el rol asignado. |
| **Flujo normal** | 1. El usuario ingresa su identificador único y contraseña.<br>2. El sistema valida las credenciales contra el hash almacenado.<br>3. Otorga acceso directo al panel respectivo según su rol (`ELECTOR`, `ADMIN`, `SUPER_ADMIN`). |
| **Flujo alternativo** | 2a. Si los datos no coinciden, se muestra el mensaje de error: 'Identificador o contraseña incorrectos'. |
| **Condición especial** | La contraseña se almacena usando hashing seguro con BCrypt y nunca se transmite ni se almacena en texto plano. |

#### RF-M01-02 — Recuperación de Acceso por Correo de Contacto
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M01-02 |
| **Nombre** | Recuperación de Acceso por Correo de Contacto |
| **Descripción** | Permite a un usuario recuperar el acceso a su cuenta cuando olvida su contraseña, solicitando el envío de una nueva contraseña aleatoria a su correo de contacto registrado. |
| **Prioridad** | Alta |
| **Precondición** | El usuario cuenta con una cuenta activa en `users` con correo de contacto registrado. |
| **Postcondición** | Se genera y almacena (hash) una nueva contraseña aleatoria; la anterior queda invalidada. |
| **Flujo normal** | 1. El usuario ingresa su documento en la pantalla 'Recuperar acceso'.<br>2. El sistema genera una nueva contraseña aleatoria y actualiza el hash almacenado.<br>3. Envía la nueva contraseña al correo de contacto registrado, respetando la cola de envío progresivo (RN-9).<br>4. Se registra la solicitud en el log de auditoría (RN-8). |
| **Flujo alternativo** | 1a. Si el documento no existe en `users` o el usuario está en estado 'Eliminado', el sistema muestra un mensaje genérico sin confirmar ni negar la existencia del registro. |
| **Condición especial** | Este es el único flujo, junto con RF-M07-02, donde el sistema vuelve a generar una contraseña aleatoria tras el auto-registro inicial. |

### 4.2 M02 — Gestión del Censo Electoral (Exclusivo Administrador)

#### RF-M02-00 — Carga y Mantenimiento de la Lista Blanca del Censo
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M02-00 |
| **Nombre** | Carga y Mantenimiento de la Lista Blanca del Censo |
| **Descripción** | Permite al Administrador registrar, de forma individual o masiva mediante archivo CSV, las entradas de la lista blanca (documento, nombre, grado) que autorizan a cada estudiante a completar su propio auto-registro (RF-M01-00). Reemplaza el alta directa de cuentas que existía en versiones anteriores. |
| **Prioridad** | Alta |
| **Precondición** | El Administrador ha iniciado sesión de forma correcta. |
| **Postcondición** | Las entradas quedan indexadas en `census_whitelist`, disponibles para ser reclamadas mediante auto-registro. |
| **Flujo normal** | 1. El Administrador accede a 'Gestión de Censo'.<br>2. Carga un archivo CSV (documento, nombre, grado) o rellena el formulario individual.<br>3. El sistema valida que no exista duplicado por documento en la lista blanca.<br>4. Persiste las entradas en `census_whitelist`, sin generar ni enviar ninguna credencial. |
| **Flujo alternativo** | 3a. Si un documento ya existe en la lista blanca, se reporta el error y se omite dicho registro. |
| **Condición especial** | La lista blanca no constituye una cuenta de acceso: no tiene correo, contraseña ni permite iniciar sesión hasta que el propio estudiante complete RF-M01-00. El sistema bloquea por completo cualquier ruta que intente crear directamente una cuenta activa desde este módulo. |

#### RF-M02-01 — Consulta, Modificación y Eliminación Lógica de Usuarios
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M02-01 |
| **Nombre** | Consulta, Modificación y Eliminación Lógica de Usuarios |
| **Descripción** | Permite al Administrador consultar el listado completo de usuarios electores ya auto-registrados, modificar sus datos (nombre, documento, grado, correo de contacto, estado) y eliminarlos de forma lógica. También permite consultar y editar entradas de la lista blanca aún no reclamadas. |
| **Prioridad** | Alta |
| **Precondición** | Sesión de Administrador o Súper Administrador activa. |
| **Postcondición** | Los cambios quedan reflejados y registrados en el log de auditoría; ningún registro se elimina físicamente. |
| **Flujo normal** | 1. El Administrador busca o filtra un usuario en el listado.<br>2. Selecciona 'Editar' y modifica los campos requeridos, o selecciona 'Eliminar'.<br>3. Si elige 'Eliminar', el sistema cambia el `status` a 'Eliminado' (con `deleted_at`) sin borrar el registro.<br>4. El sistema registra el cambio en el log de auditoría. |
| **Flujo alternativo** | 3a. El Administrador puede restaurar un usuario eliminado, devolviendo su `status` a 'Activo'. |
| **Condición especial** | Los registros de votación ya emitidos por un elector son inmutables y no se ven afectados por ninguna modificación o eliminación lógica de su cuenta (RN-7). |

#### RF-M02-02 — Promoción Automática de Año Lectivo
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M02-02 |
| **Nombre** | Promoción Automática de Año Lectivo |
| **Descripción** | Permite al Administrador ejecutar, una sola vez por año lectivo, la promoción masiva y automática del grado de todos los usuarios electores activos, y de las entradas de la lista blanca aún no reclamadas. |
| **Prioridad** | Alta |
| **Precondición** | Sesión de Administrador activa; existe una tabla de grados ordenada secuencialmente. |
| **Postcondición** | Cada elector activo, y cada entrada pendiente de la lista blanca, avanza al siguiente grado; quienes se encuentren en el último grado pasan al estado 'Egresado' (o se marcan como tal en la lista blanca si aún no se auto-registraron). |
| **Flujo normal** | 1. El Administrador marca previamente (opcional) excepciones mediante `excluir_de_promocion`.<br>2. Ejecuta 'Iniciar Promoción de Año Lectivo'.<br>3. El sistema presenta una vista previa (total a promover, excluidos y a egresar, tanto en `users` como en `census_whitelist`).<br>4. El Administrador confirma explícitamente.<br>5. El sistema actualiza el grado correspondiente.<br>6. Se registra la operación en el log de auditoría. |
| **Flujo alternativo** | 4a. Si el Administrador cancela la confirmación, no se aplica ningún cambio. |
| **Condición especial** | El sistema impide ejecutar esta acción más de una vez dentro del mismo año lectivo, salvo confirmación adicional explícita. |

### 4.3 M03 — Gestión de Elecciones, Cargos y Etapas

#### RF-M03-00 — Catálogo de Cargos Electorales y sus Requisitos
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M03-00 |
| **Nombre** | Catálogo de Cargos Electorales y sus Requisitos |
| **Descripción** | Permite al Administrador mantener un catálogo institucional de cargos electorales (ej. Personero, Contralor, Representante de Curso), cada uno con un conjunto de requisitos de elegibilidad predefinidos (ej. certificado de haber cursado y aprobado 10° para Personero) que se exigirán a todo aspirante a candidato de ese cargo. |
| **Prioridad** | Alta |
| **Precondición** | Sesión de Administrador activa. |
| **Postcondición** | El cargo y sus requisitos quedan disponibles para asociarse a una o más elecciones (RF-M03-01). |
| **Flujo normal** | 1. El Administrador accede a 'Cargos Electorales'.<br>2. Crea un nuevo cargo (`election_positions`) indicando su nombre.<br>3. Define uno o más requisitos (`position_requirements`) asociados, marcando cuáles son obligatorios.<br>4. Guarda el cargo, quedando disponible para su reutilización en futuras elecciones. |
| **Flujo alternativo** | 3a. Un cargo puede crearse sin requisitos obligatorios, si el proceso electoral no exige documentación especial. |
| **Condición especial** | El catálogo es reutilizable año tras año; no es necesario redefinir los requisitos de un mismo cargo en cada nueva elección. |

#### RF-M03-01 — Creación y Parametrización de Elecciones por Etapas
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M03-01 |
| **Nombre** | Creación y Parametrización de Elecciones por Etapas |
| **Descripción** | Habilita al Administrador para configurar eventos electorales definiendo Título, Tipo, Cargo electoral asociado (RF-M03-00), Descripción, Grados que pueden votar, y las tres ventanas de fecha/hora correspondientes a las etapas de Inscripción de Candidatos, Consulta de Propuestas y Votación. |
| **Prioridad** | Alta |
| **Precondición** | Sesión de Administrador activa. |
| **Postcondición** | Elección registrada en estado 'Programada', con sus tres ventanas de etapa configuradas. |
| **Flujo normal** | 1. El Administrador ingresa los datos generales del evento y selecciona el cargo electoral.<br>2. Configura las fechas y horas de inicio/fin de cada una de las tres etapas, en orden cronológico y sin superposición.<br>3. Guarda el registro. |
| **Flujo alternativo** | 2a. Si alguna ventana de etapa se superpone con otra, o su fecha de cierre es menor a la de inicio, el sistema solicita corregir los campos. |
| **Condición especial** | El paso de estados (`PROGRAMADA → INSCRIPCION → PROPUESTAS → ACTIVA → FINALIZADA`) ocurre de manera automática en el servidor según las fechas configuradas (RN-12). |

#### RF-M03-02 — Edición y Eliminación Lógica de Procesos Electorales
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M03-02 |
| **Nombre** | Edición y Eliminación Lógica de Procesos Electorales |
| **Descripción** | Permite al Administrador modificar los parámetros de una elección o ejecutar su eliminación lógica en cualquier momento, inhabilitando su visibilidad y operación para los electores pero garantizando la inmutabilidad de los votos históricos (RN-7.1). |
| **Prioridad** | Alta |
| **Precondición** | Sesión de Administrador activa. |
| **Postcondición** | El proceso electoral queda actualizado o en estado 'Eliminado' (`deleted_at` registrado), y la acción queda asentada en la auditoría (RN-8). |
| **Flujo normal** | 1. El Administrador selecciona un proceso en el panel de control.<br>2. Selecciona 'Editar' para modificar parámetros o 'Eliminar' para darle de baja.<br>3. Al confirmar la eliminación, el sistema cambia el estado a 'Eliminado' y almacena `deleted_at`.<br>4. Se registra la acción en el log de auditoría. |
| **Flujo alternativo** | 3a. Si el Administrador cancela la eliminación en el modal de confirmación, no se aplica ningún cambio. |
| **Condición especial** | La eliminación es exclusivamente lógica; ningún voto, candidato, documento o plan de gobierno asociado se elimina físicamente (RN-7.1). |

### 4.4 M04 — Autopostulación y Aprobación de Candidatos

#### RF-M04-01 — Autopostulación de Candidatos con Documentos de Requisito
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M04-01 |
| **Nombre** | Autopostulación de Candidatos con Documentos de Requisito |
| **Descripción** | Permite a cualquier elector habilitado postularse a sí mismo como candidato a una elección que se encuentre en su etapa de Inscripción, cargando su foto, sus propuestas (lista de puntos), su plan de gobierno y un documento de soporte por cada requisito obligatorio definido por el cargo electoral de dicha elección (RF-M03-00). |
| **Prioridad** | Alta |
| **Precondición** | La elección debe estar en etapa 'Inscripción de Candidatos'; el elector no debe tener ya una candidatura registrada para esa elección. |
| **Postcondición** | Se crea un registro en `candidates` con `status = 'PENDIENTE'`, junto con sus `candidate_proposals`, su plan de gobierno y sus `candidacy_documents`. La candidatura no es visible para otros electores hasta su aprobación (RF-M04-02). |
| **Flujo normal** | 1. El elector accede a 'Postularme como candidato' desde una elección en etapa de Inscripción.<br>2. Carga su foto, registra sus propuestas en formato de lista y adjunta su plan de gobierno.<br>3. El sistema muestra los requisitos documentales obligatorios del cargo asociado; el elector carga un documento de soporte por cada uno.<br>4. Envía la postulación, quedando en estado 'Pendiente' de revisión administrativa. |
| **Flujo alternativo** | 3a. Si el elector no carga alguno de los documentos obligatorios, puede enviar igualmente la postulación, quedando expuesto a que el Administrador la rechace o la apruebe con excepción (RN-10.1).<br>4a. Si el elector ya cuenta con una postulación previa para esa misma elección, el sistema impide una segunda postulación. |
| **Condición especial** | El sistema autogenera una opción por defecto para el 'Voto en Blanco', no asociada a ningún elector. Las propuestas y el plan de gobierno se muestran obligatoriamente al elector antes de confirmar su voto (ver RF-M05-01). Ninguna candidatura en estado 'Pendiente' o 'Rechazado' aparece en el tarjetón de votación. |

#### RF-M04-02 — Aprobación o Rechazo de Candidaturas por el Administrador
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M04-02 |
| **Nombre** | Aprobación o Rechazo de Candidaturas por el Administrador |
| **Descripción** | Permite al Administrador revisar cada postulación pendiente, verificar sus propuestas, plan de gobierno y documentos de requisito, y decidir si la aprueba (con o sin excepción), o la rechaza indicando obligatoriamente el motivo. |
| **Prioridad** | Alta |
| **Precondición** | Sesión de Administrador activa; existe al menos una candidatura en estado 'Pendiente'. |
| **Postcondición** | La candidatura pasa a estado 'Aprobado' (visible en el tarjetón) o 'Rechazado' (no visible); el elector recibe una notificación por correo con el resultado. |
| **Flujo normal** | 1. El Administrador accede al listado de candidaturas pendientes de una elección.<br>2. Revisa las propuestas, el plan de gobierno y los documentos cargados por el elector.<br>3. Si todos los requisitos obligatorios están cumplidos, aprueba la candidatura.<br>4. El sistema actualiza `status = 'APROBADO'` y encola una notificación por correo. |
| **Flujo alternativo** | 3a. Si faltan documentos obligatorios, el Administrador puede aprobar de todas formas seleccionando 'Aprobar con Excepción', indicando qué requisitos quedaron pendientes; el sistema registra `approved_with_exceptions = 1` junto con dicho detalle (RN-10.1).<br>3b. Si el Administrador decide rechazar la candidatura, debe ingresar obligatoriamente el motivo en texto libre. El sistema actualiza `status = 'RECHAZADO'`, almacena el motivo y encola la notificación (`email_type = 'CANDIDATURA_RECHAZADA'`) (RN-10). |
| **Condición especial** | Toda aprobación o rechazo queda registrada en el log de auditoría (RN-8), a diferencia de la gestión de PQR que queda excluida de dicho log por decisión de diseño (ver M08). |

### 4.5 M05 — Proceso de Votación y Control de Voto Único

#### RF-M05-01 — Emisión de Voto y Control de Sufragio Único
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M05-01 |
| **Nombre** | Emisión de Voto y Control de Sufragio Único |
| **Descripción** | Garantiza que un elector emita un voto secreto, mostrando previamente una ventana con las propuestas y el plan de gobierno del candidato seleccionado, e impide de forma absoluta un segundo intento de sufragio. |
| **Prioridad** | Alta |
| **Precondición** | Elector autenticado, elección en etapa de Votación (`status = 'ACTIVA'`) y sin registros previos de sufragio. |
| **Postcondición** | El voto se cuenta de forma anónima y el elector pasa a estado 'Sufragó' en la elección. |
| **Flujo normal** | 1. El elector visualiza el tarjetón con todos los candidatos aprobados.<br>2. Al seleccionar un candidato, el sistema abre una ventana emergente con sus propuestas y el enlace a su plan de gobierno.<br>3. Desde la ventana, el elector puede elegir 'Volver' o 'Confirmar Voto'.<br>4. Si confirma, el sistema disocia la identidad, cuenta el voto y actualiza su estado. |
| **Flujo alternativo** | 1a. Si el sistema detecta que el elector ya cuenta con estado 'Sufragó', bloquea el tarjetón de inmediato.<br>1b. Si la elección aún no llegó a la etapa de Votación (se encuentra en Inscripción o Propuestas), el tarjetón permanece bloqueado.<br>3a. Si el elector selecciona 'Volver', ningún voto se registra. |
| **Condición especial** | El anonimato se asegura mediante la disociación estructural completa de las tablas. Únicamente los candidatos en estado 'Aprobado' aparecen en el tarjetón. |

### 4.6 M06 — Escrutinio y Resultados en Tiempo Real (Acceso Condicionado)

#### RF-M06-01 — Visualización de Resultados en Tiempo Real Condicionada
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M06-01 |
| **Nombre** | Visualización de Resultados en Tiempo Real Condicionada |
| **Descripción** | Permite visualizar las gráficas de resultados dinámicos sujeto a tres condiciones de acceso: (a) el elector ha emitido su voto en una elección en etapa 'Votación' o 'Programada'; (b) la elección tiene estado 'Finalizada' y el `grade_id` del elector figura en `event_grades`; (c) el usuario posee rol `ADMIN` o `SUPER_ADMIN` (acceso irrestricto en cualquier etapa). |
| **Prioridad** | Alta |
| **Precondición** | Se cumple al menos una de las condiciones (a), (b) o (c) descritas. |
| **Postcondición** | Despliegue interactivo de las estadísticas mediante WebSockets. |
| **Flujo normal** | 1. El elector finaliza su votación en una elección en etapa 'Votación'.<br>2. El sistema verifica la participación en `voter_event_participations` y le otorga acceso al Dashboard de Resultados.<br>3. Las gráficas se actualizan en vivo. |
| **Flujo alternativo** | 1a. Si un elector intenta entrar sin haber votado durante la etapa de Votación, el sistema deniega el acceso con el mensaje 'Debe votar para ver los resultados'.<br>1b. Si la elección está 'Finalizada', se verifica el grado del elector contra `event_grades`; si no figura, se deniega con 'No pertenece a un grado habilitado para esta elección'.<br>1c. Si la elección tiene estado 'Eliminado', el sistema deniega el acceso siempre a los electores (403 Forbidden). |
| **Condición especial** | `ADMIN` y `SUPER_ADMIN` están exentos de todas las condiciones y ven los resultados continuamente (RN-5). |

### 4.7 M07 — Perfil de Usuario y Autogestión de Credenciales

#### RF-M07-01 — Consulta y Edición de Perfil Propio
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M07-01 |
| **Nombre** | Consulta y Edición de Perfil Propio |
| **Descripción** | Permite a cualquier usuario autenticado (Elector, Administrador o Súper Administrador) consultar su información básica y modificar su correo de contacto o su contraseña. Los datos oficiales del censo (documento, nombre, grado) y, en el caso de cuentas administrativas, el cargo institucional, permanecen en modo solo lectura, editables únicamente por el Súper Administrador (RF-M09-01). |
| **Prioridad** | Media |
| **Precondición** | Usuario autenticado con sesión activa. |
| **Postcondición** | El correo de contacto y/o la contraseña quedan actualizados; el cambio queda registrado en `audit_log` y se notifica al usuario. |
| **Flujo normal** | 1. El usuario accede a 'Mi Perfil'.<br>2. **Actualización de Correo:** Modifica el correo de contacto y guarda directamente.<br>3. **Cambio de Contraseña:** Hace clic en 'Cambiar contraseña', desplegando el modal interactivo de 2 pasos.<br>4. **Paso 1:** Ingresa su contraseña actual, validada de forma asíncrona (AJAX).<br>5. **Paso 2:** Ingresa la nueva contraseña con validación en tiempo real de complejidad (mínimo 8 caracteres, una mayúscula, un símbolo especial).<br>6. Al cumplirse las reglas, envía la solicitud AJAX.<br>7. El modal muestra la confirmación del resultado y asienta el evento en auditoría. |
| **Flujo alternativo** | 4a. Si la contraseña actual es incorrecta, el modal muestra el error sin cerrarse.<br>5a. Si la nueva contraseña no cumple los requisitos, el botón 'Guardar' permanece deshabilitado. |
| **Condición especial** | Todo cambio de correo de contacto o contraseña desde el perfil se registra en auditoría (RN-8). |

#### RF-M07-02 — Reasignación de Contraseña por el Administrador
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M07-02 |
| **Nombre** | Reasignación de Contraseña por el Administrador |
| **Descripción** | Permite al Administrador o Súper Administrador, desde el Censo Electoral, forzar la generación de una nueva contraseña aleatoria para un usuario elector, sin necesidad de que el propio usuario la solicite. |
| **Prioridad** | Media |
| **Precondición** | Sesión de Administrador o Súper Administrador activa; el usuario debe tener un correo de contacto vigente. |
| **Postcondición** | Se genera una nueva contraseña aleatoria, se envía al correo de contacto registrado y la anterior queda invalidada. |
| **Flujo normal** | 1. El Administrador selecciona un usuario en el censo.<br>2. Ejecuta 'Reasignar contraseña'.<br>3. El sistema genera y envía la nueva contraseña por correo (RN-9).<br>4. Se registra la acción en el log de auditoría. |
| **Flujo alternativo** | 1a. Si el usuario no tiene correo de contacto registrado, el sistema exige capturarlo antes de continuar. |
| **Condición especial** | El Administrador nunca visualiza la contraseña generada; el sistema solo confirma si el envío fue exitoso. |

### 4.8 M08 — Ayuda, Tutorial, PQR y Chatbot

#### RF-M08-00 — Sección de Ayuda Estática Ilustrada
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M08-00 |
| **Nombre** | Sección de Ayuda Estática Ilustrada |
| **Descripción** | Provee a cualquier usuario (esté o no autenticado) un panel de preguntas frecuentes tipo acordeón, con un tema por cada flujo relevante del sistema (auto-registro, inicio de sesión, recuperación de acceso, autopostulación de candidatos, votación, perfil, resultados). Cada tema incluye una ilustración de pasos y una explicación breve en texto. |
| **Prioridad** | Media |
| **Precondición** | Ninguna; accesible a cualquier usuario, incluyendo visitantes no autenticados. La radicación de una PQR (RF-M08-01) sí continúa exigiendo autenticación. |
| **Postcondición** | El usuario visualiza el contenido de ayuda sin necesidad de contactar al Administrador. |
| **Flujo normal** | 1. El usuario accede a la sección 'Ayuda' (ruta pública `/Ayuda`).<br>2. Expande el tema de su interés.<br>3. Si su duda no se resuelve y está autenticado, utiliza el enlace hacia 'Crear PQR' o inicia el Chatbot (RF-M08-03); si no está autenticado, ve una invitación a iniciar sesión para crear una PQR. |
| **Flujo alternativo** | 3a. Si el usuario no encuentra un tema relacionado, procede directamente a crear una PQR (requiere sesión iniciada). |
| **Condición especial** | El contenido es estático (no editable desde el sistema ni persistido en base de datos). Al primer inicio de sesión se muestra un banner de una sola aparición, controlado únicamente mediante `localStorage`, sin impacto en el esquema. |

> **Nota de corrección (v2.8):** el módulo de Ayuda (RF-M08-00) pasó a ser accesible sin necesidad de sesión iniciada, respondiendo tanto a `/Ayuda` como a `/Pqr`. La creación y el historial de PQR (RF-M08-01) mantienen su exigencia de autenticación sin cambios. La precondición original decía “accesible a cualquier usuario autenticado”; esta corrección la actualiza para reflejar el estado implementado.

#### RF-M08-01 — Creación de PQR por el Usuario
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M08-01 |
| **Nombre** | Creación de PQR por el Usuario |
| **Descripción** | Permite a cualquier usuario autenticado radicar una Petición, Queja o Reclamo mediante un asunto y un mensaje en texto libre. |
| **Prioridad** | Media |
| **Precondición** | Usuario autenticado. |
| **Postcondición** | Se crea un registro en estado 'Abierto', visible para el Administrador en su panel de gestión. |
| **Flujo normal** | 1. El usuario accede a 'Crear PQR' (desde el menú, el final de la sección de Ayuda, o escalado desde el Chatbot).<br>2. Ingresa un asunto y describe su solicitud.<br>3. Envía el formulario.<br>4. El sistema registra el ticket en estado 'Abierto' y confirma la radicación.<br>5. Al volver a Ayuda, el elector visualiza el listado de sus propias PQR previas, con su estado y respuesta administrativa si aplica. |
| **Flujo alternativo** | 3a. Si el asunto o el mensaje están vacíos, el sistema impide el envío. |
| **Condición especial** | El usuario no puede editar ni eliminar una PQR una vez radicada. El listado de historial propio se filtra exclusivamente por el `user_id` del usuario autenticado. |

#### RF-M08-02 — Gestión y Resolución de PQR por el Administrador
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M08-02 |
| **Nombre** | Gestión y Resolución de PQR por el Administrador |
| **Descripción** | Permite al Administrador o Súper Administrador consultar el listado de PQR filtrando por estado, revisar el detalle de cada solicitud y registrar una única respuesta que la marca como resuelta. |
| **Prioridad** | Media |
| **Precondición** | Sesión administrativa activa; existe al menos una PQR en estado 'Abierto'. |
| **Postcondición** | La PQR pasa a estado 'Resuelto', queda registrada la respuesta administrativa, y se encola una notificación (`email_type = 'RESPUESTA_PQR'`). |
| **Flujo normal** | 1. El Administrador accede al listado de PQR y filtra por estado 'Abierto'.<br>2. Selecciona una solicitud y revisa su asunto y mensaje.<br>3. Redacta una respuesta y confirma.<br>4. El sistema actualiza el estado a 'Resuelto', almacena la respuesta y encola la notificación (RN-9). |
| **Flujo alternativo** | 3a. Si el Administrador cancela antes de confirmar, la PQR permanece en estado 'Abierto'. |
| **Condición especial** | La respuesta del Administrador es única por PQR. La creación y resolución de PQR **no** se registra en `audit_log`; su trazabilidad se limita a los campos de `pqr_tickets`. |

#### RF-M08-03 — Chatbot de Ayuda Basado en Reglas
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M08-03 |
| **Nombre** | Chatbot de Ayuda Basado en Reglas |
| **Descripción** | Ofrece un asistente conversacional dentro del módulo de Ayuda, guiado por palabras clave o menú de opciones predefinidas (sin inteligencia artificial generativa ni llamadas a servicios externos), que orienta al usuario hacia el tema de ayuda correspondiente. Si ninguna opción resuelve la duda, el chatbot ofrece escalar directamente a la creación de una PQR, precargando el mensaje de la conversación como punto de partida del asunto y del mensaje del formulario. |
| **Prioridad** | Media |
| **Precondición** | Usuario autenticado dentro de la sección de Ayuda. |
| **Postcondición** | El usuario recibe orientación relevante o es redirigido al formulario de creación de PQR con contexto precargado. |
| **Flujo normal** | 1. El usuario abre el Chatbot desde la sección de Ayuda.<br>2. Selecciona una opción de un menú guiado, o escribe una palabra clave (ej. 'contraseña', 'votar', 'candidato').<br>3. El sistema responde con contenido predefinido asociado a esa palabra clave o categoría, similar al de RF-M08-00.<br>4. El usuario puede continuar navegando el menú o indicar que su duda no fue resuelta. |
| **Flujo alternativo** | 2a. Si ninguna palabra clave coincide con las reglas definidas, el chatbot responde con un mensaje por defecto y ofrece el botón 'Crear PQR con esta conversación'.<br>4a. Si el usuario indica que la respuesta no resolvió su duda, el chatbot ofrece la misma opción de escalamiento a PQR. |
| **Condición especial** | El motor de reglas y su contenido son estáticos, embebidos en el cliente (similar a RF-M08-00), sin persistencia de la conversación en base de datos ni dependencia de servicios de inteligencia artificial externos. |

### 4.9 M09 — Gestión de Cuentas Administrativas (Exclusivo Súper Administrador)

#### RF-M09-01 — Creación y Gestión de Cuentas Administrativas
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M09-01 |
| **Nombre** | Creación y Gestión de Cuentas Administrativas |
| **Descripción** | Permite exclusivamente al Súper Administrador crear nuevas cuentas con rol `ADMIN` o `SUPER_ADMIN`, definiendo para cada una un cargo institucional descriptivo en texto libre (ej. "Orientador", "Coordinador Académico"), así como editarlas o eliminarlas de forma lógica. Las cuentas `ADMIN` no tienen acceso a este módulo. |
| **Prioridad** | Alta |
| **Precondición** | Sesión de `SUPER_ADMIN` activa. |
| **Postcondición** | La cuenta administrativa queda creada, editada o eliminada lógicamente, y la acción registrada en `audit_log`. |
| **Flujo normal** | 1. El Súper Administrador accede a 'Gestión de Administradores'.<br>2. Registra el documento, nombre, correo de contacto, rol (`ADMIN` o `SUPER_ADMIN`) y cargo institucional (texto libre) de la nueva cuenta.<br>3. El sistema genera una contraseña aleatoria inicial y la envía por correo (mismo mecanismo que RF-M07-02), o bien la nueva cuenta completa su propio primer acceso mediante recuperación de acceso (RF-M01-02).<br>4. Se registra la acción en el log de auditoría. |
| **Flujo alternativo** | 2a. El Súper Administrador puede editar el cargo institucional o el rol de una cuenta existente, o eliminarla lógicamente (`status = 'ELIMINADO'`).<br>2b. Si intenta eliminar lógicamente su propia cuenta, el sistema lo impide para evitar dejar el sistema sin ningún `SUPER_ADMIN` activo. |
| **Condición especial** | El campo de cargo institucional es puramente descriptivo; no otorga ni restringe permisos, los cuales dependen exclusivamente del rol (`ADMIN` o `SUPER_ADMIN`) según RN-13. |

### 4.10 Requerimientos No Funcionales (RNF)

| Categoría | Especificación |
| :--- | :--- |
| **Seguridad** | Contraseñas almacenadas con BCrypt; sesiones basadas en cookies de autenticación con claims; toda acción administrativa sobre censo, candidaturas y cuentas administrativas queda auditada (RN-8). El auto-registro valida estrictamente contra la lista blanca antes de crear cualquier cuenta. |
| **Rendimiento** | El dashboard de resultados debe reflejar un nuevo voto en un lapso no mayor a 3 segundos mediante WebSockets. La validación de documento contra la lista blanca durante el auto-registro debe responder en menos de 2 segundos. |
| **Usabilidad** | La ventana de propuestas y los flujos de votación y autopostulación deben ser operables desde dispositivos móviles y de escritorio. |
| **Disponibilidad** | El sistema debe estar disponible durante todo el horario configurado de cada etapa de una elección (RF-M03-01). |
| **Escalabilidad** | El proceso de promoción automática (RF-M02-02) y la carga de lista blanca (RF-M02-00) deben soportar la actualización masiva de al menos 2000 registros en una sola operación. |
| **Compatibilidad** | Compatible con los navegadores modernos más utilizados en entornos educativos (Chrome, Edge, Firefox). |
| **Confiabilidad de notificaciones** | El envío de correos (recuperación, reasignación, PQR, candidatura) debe procesarse mediante una cola con control de tasa (RN-9), sin bloquear la operación que lo origina. |
| **Integridad documental** | Los documentos de soporte de candidatura y los planes de gobierno deben almacenarse de forma persistente y quedar asociados de manera inmutable a la candidatura que los originó, incluso si esta es posteriormente rechazada. |

---

## 5. Referencias Bibliográficas
* IEEE Std 830-1998 — *Recommended Practice for Software Requirements Specifications*.
* OWASP Foundation — *Password Storage Cheat Sheet* (recomendaciones de hashing con BCrypt).
* Documentación oficial del protocolo WebSocket (RFC 6455).
* Servicio Nacional de Aprendizaje — SENA, Programa de Análisis y Desarrollo de Software, Ficha 228118.
