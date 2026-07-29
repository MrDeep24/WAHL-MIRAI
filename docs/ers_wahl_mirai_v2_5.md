# ESPECIFICACIÓN DE REQUERIMIENTOS DE SOFTWARE
**(ERS — IEEE Std 830-1998)**

## Sistema de Votaciones Digitales Estudiantiles
**Wahl Mirai — Versión 2.5**

* **Programa:** Análisis y Desarrollo de Software
* **Servicio Nacional de Aprendizaje — SENA**
* **Ficha:** 228118
* **Colombia, 2026**
* **Control de versión:** v2.5 — Corrección de restricción de unicidad en correo de contacto (soporte para acudientes/hermanos), alineación completa de arquitectura y diseño técnico (SPA + ASP.NET Core EF Core + Tailwind CSS + JWT), y actualización del ERD a 12 tablas.

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
   4.1 [M01 — Gestión de Acceso y Sesión](#41-m01--gestión-de-acceso-y-sesión)  
   4.2 [M02 — Gestión del Censo Electoral (Exclusivo Administrador)](#42-m02--gestión-del-censo-electoral-exclusivo-administrador)  
   4.3 [M03 — Gestión de Elecciones](#43-m03--gestión-de-elecciones)  
   4.4 [M04 — Inscripción y Gestión de Candidatos](#44-m04--inscripción-y-gestión-de-candidatos)  
   4.5 [M05 — Proceso de Votación y Control de Voto Único](#45-m05--proceso-de-votación-y-control-de-voto-único)  
   4.6 [M06 — Escrutinio y Resultados en Tiempo Real (Acceso Condicionado)](#46-m06--escrutinio-y-resultados-en-tiempo-real-acceso-condicionado)  
   4.7 [M07 — Perfil de Usuario y Autogestión de Credenciales](#47-m07--perfil-de-usuario-y-autogestión-de-credenciales)  
   4.8 [Requerimientos No Funcionales (RNF)](#48-requerimientos-no-funcionales-rnf)  
5. [Referencias Bibliográficas](#5-referencias-bibliográficas)

---

## 1. Introducción

### 1.1 Propósito
Este documento define los requerimientos para el sistema **'Wahl Mirai' Versión 2.5**, que incorpora los siguientes cambios respecto a las versiones anteriores:
1. Un censo electoral persistente que elimina el salón/curso paralelo como atributo y conserva la identidad del elector año tras año, actualizando únicamente su grado mediante un mecanismo de promoción automática.
2. La eliminación lógica (no física) de electores, con edición completa de sus datos y trazabilidad mediante auditoría, preservando siempre la inmutabilidad de los votos ya emitidos.
3. Una ventana emergente de propuestas del candidato durante el proceso de votación, con opciones explícitas para volver al tarjetón o confirmar el voto.
4. Un **correo de contacto obligatorio** para cada elector (del propio estudiante o de su acudiente), utilizado exclusivamente para la entrega de credenciales y la recuperación de acceso — nunca como mecanismo de inicio de sesión.
5. Una **contraseña asignada por el sistema de forma aleatoria** (no predecible, a diferencia del esquema documento + año lectivo de versiones anteriores) que se entrega al elector únicamente por correo, tanto en su alta inicial como en cualquier recuperación de acceso posterior. Se elimina el requisito de cambio obligatorio de contraseña en el primer inicio de sesión.
6. Un módulo de **Perfil de Usuario** donde cualquier usuario autenticado puede consultar su información y modificar su correo de contacto y su contraseña, sin poder alterar datos oficiales del censo (documento, nombre, grado).
7. Un mecanismo de **envío progresivo (en cola, con control de tasa)** de correos de credenciales, para soportar cargas masivas de electores sin saturar al proveedor de correo institucional.
8. La **eliminación lógica (no física) de procesos electorales** (`voting_events`), inhabilitando su visibilidad y operabilidad pero preservando intactos sus candidatos, propuestas y votos emitidos para auditoría e integridad histórica.
9. La **flexibilización del correo de contacto** permitiendo que un mismo correo (ej. acudiente) sea compartido entre múltiples electores (ej. hermanos), garantizando la unicidad del sistema exclusivamente mediante el documento de identidad (`document_hash`).

### 1.2 Alcance del Sistema
Wahl Mirai permite gestionar elecciones estudiantiles mediante un censo cerrado y persistente, cargado y administrado exclusivamente por el Administrador, garantizando voto único, anonimato y trazabilidad de cambios administrativos. El sistema cubre la gestión de acceso, la administración del censo, la configuración de elecciones, la inscripción de candidatos con sus propuestas, la emisión del voto y el escrutinio en tiempo real.

### 1.3 Definiciones, Acrónimos y Abreviaturas

| Término / Acrónimo | Definición |
| :--- | :--- |
| **ERS** | Especificación de Requerimientos de Software. |
| **RN** | Regla de Negocio. |
| **RF** | Requerimiento Funcional. |
| **RNF** | Requerimiento No Funcional. |
| **JWT** | JSON Web Token, mecanismo de autenticación basado en tokens firmados. |
| **BCrypt** | Algoritmo de hashing seguro utilizado para almacenar contraseñas. |
| **Censo electoral** | Listado oficial y persistente de electores habilitados para votar. |
| **Grado** | Nivel académico del elector (ej. 6°, 7°, ..., 11°); reemplaza el concepto de salón/curso paralelo. |
| **Promoción automática** | Mecanismo que avanza masivamente el grado de todos los electores activos al iniciar un nuevo año lectivo. |
| **Egresado** | Estado de un elector que completó el último grado y ya no pertenece al censo activo. |
| **Eliminación lógica** | Cambio de estado de un registro a 'Eliminado' sin borrarlo físicamente de la base de datos. |
| **WebSocket** | Protocolo de comunicación bidireccional utilizado para actualizar resultados en tiempo real. |
| **Correo de contacto** | Correo electrónico obligatorio registrado para cada elector (propio o de su acudiente), usado exclusivamente para entrega de credenciales y recuperación de acceso; nunca como identificador de login. |
| **Contraseña asignada** | Contraseña generada aleatoriamente por el sistema y enviada por correo al elector, tanto en su alta inicial como en cualquier recuperación de acceso. |
| **Cola de envío progresivo** | Mecanismo que distribuye en el tiempo el envío masivo de correos de credenciales, respetando límites de tasa del proveedor de correo. |

### 1.4 Referencias
Ver [sección 5 — Referencias Bibliográficas](#5-referencias-bibliográficas).

---

## 2. Descripción General del Sistema

### 2.1 Perspectiva del Producto
Wahl Mirai es una aplicación web cliente-servidor de uso interno institucional, dirigida a colegios que requieren digitalizar sus procesos de votación estudiantil (personerías, contralorías, representantes de curso u otras figuras de gobierno escolar). El sistema define dos roles principales: **Administrador** (personal del colegio a cargo de la configuración y el censo) y **Elector** (estudiante habilitado para votar).

### 2.2 Funciones Principales del Sistema
* Autenticación segura por identificador único y contraseña, con esquema de clave inicial autogenerada para electores.
* Gestión persistente del censo electoral: alta, consulta, modificación y eliminación lógica de electores.
* Promoción automática anual del grado de los electores, sin actualización manual uno por uno.
* Configuración de elecciones con parámetros de tiempo, tipo y grados habilitados para votar.
* Inscripción de candidatos con foto, tarjetón y propuestas visibles para el elector antes de votar.
* Emisión de voto único, secreto y anónimo, con confirmación explícita tras revisar las propuestas del candidato.
* Escrutinio y visualización de resultados en tiempo real, condicionados al hecho de haber votado.

---

## 3. Reglas de Negocio Transversales

* **RN-1 — Registro Centralizado Exclusivo:** No existe auto-registro. El Administrador es el único actor facultado para dar de alta a los electores mediante carga masiva o manual en el censo electoral escolar.
* **RN-2 — Credenciales de Acceso Asignadas por el Sistema:** El login continúa siendo por identificador único (documento) y contraseña — nunca por correo. Sin embargo, la contraseña ya no se deriva de un patrón predecible (documento + año lectivo); el sistema la genera de forma aleatoria y la entrega exclusivamente por correo al **correo de contacto** registrado del elector (ver RN-2.1), tanto en el alta inicial como en cualquier recuperación de acceso posterior. No existe cambio obligatorio de contraseña en el primer inicio de sesión: el elector puede seguir usando la contraseña asignada o cambiarla voluntariamente desde su Perfil de Usuario (RF-M07-01).
* **RN-2.1 — Correo de Contacto Obligatorio:** Todo elector debe tener registrado un correo de contacto (propio o de su acudiente) al momento de su alta en el censo. Un mismo correo de contacto puede estar asociado a más de un elector (por ejemplo, hermanos que comparten el correo del mismo acudiente); la unicidad del sistema se garantiza por documento (`document_hash`), no por correo. Este correo se usa única y exclusivamente para la entrega de credenciales y la recuperación de acceso; en ningún caso se usa como identificador de inicio de sesión ni se expone al Administrador en texto plano la contraseña que se envía a través de él.
* **RN-3 — Voto Único y Bloqueo Seguro:** Cada elector puede votar únicamente una vez por evento electoral. Al confirmar el sufragio, su estado se actualiza irreversiblemente.
* **RN-4 — Resultados en Tiempo Real Condicionados al Voto:** Los electores tienen permitido ver los gráficos y estadísticas de escrutinio en vivo y en tiempo real, siempre y cuando hayan ejercido previamente su derecho al voto en dicha elección. Si no han votado, el acceso al panel de visualización estará estrictamente bloqueado.
* **RN-5 — Excepción del Administrador en Escrutinio:** El Administrador puede visualizar los resultados en tiempo real de forma irrestricta en cualquier momento, sin necesidad de cumplir la condición de voto.
* **RN-6 — Persistencia del Censo Electoral:** Ningún registro del censo se elimina físicamente de la base de datos. La identidad del elector (documento, nombre) se conserva de un año lectivo a otro; el único dato académico que varía es el grado, el cual se actualiza mediante el mecanismo de promoción automática (RF-M02-03) y no requiere el concepto de salón/curso paralelo.
* **RN-7 — Edición Administrativa con Inmutabilidad del Voto:** El Administrador puede modificar cualquier dato de un elector (nombre, documento, grado, estado) y eliminarlo de forma lógica en cualquier momento. Sin embargo, los registros de votación ya emitidos son absolutamente inmutables y no se ven afectados por ninguna modificación o eliminación lógica del perfil del elector.
* **RN-7.1 — Eliminación Lógica de Procesos Electorales:** El Administrador puede realizar la eliminación lógica de cualquier proceso electoral cambiando su estado a 'Eliminado' y registrando la fecha de baja (`deleted_at`). Un proceso eliminado deja de estar visible y operable para los electores en el sistema. Sin embargo, sus candidatos, opciones, propuestas y todos los votos ya emitidos permanecen estrictamente inmutables e íntegros en la base de datos para trazabilidad y auditoría (RN-7, RN-8).
* **RN-8 — Trazabilidad de Cambios Administrativos:** Toda modificación, eliminación lógica, restauración o promoción masiva realizada sobre el censo electoral queda registrada en un log de auditoría (usuario responsable, campo afectado, valor anterior, valor nuevo y fecha).
* **RN-9 — Entrega Progresiva de Notificaciones por Correo:** Cuando el sistema deba enviar credenciales a múltiples electores en una sola operación (carga masiva, RF-M02-01), los correos no se envían todos de manera simultánea. El sistema los procesa mediante una cola con control de tasa (envío gradual, por ejemplo un número limitado de correos por minuto), evitando saturar o ser bloqueado por el proveedor de correo institucional. El Administrador puede consultar cuáles credenciales fueron entregadas exitosamente y cuáles quedaron pendientes o fallidas, con opción de reenvío individual.

---

## 4. Requerimientos Específicos por Módulo

### 4.1 M01 — Gestión de Acceso y Sesión

#### RF-M01-01 — Autenticación de Usuarios por Identificador Único
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M01-01 |
| **Nombre** | Autenticación de Usuarios por Identificador Único |
| **Descripción** | Permite el acceso seguro de Administradores y Electores utilizando su documento o código registrado y contraseña, prescindiendo de correos institucionales para el login. Para los electores, la contraseña es asignada por el sistema de forma aleatoria y entregada por correo de contacto (RN-2, RN-2.1), evitando que el Administrador deba asignar o conocer claves manualmente. |
| **Prioridad** | Alta |
| **Precondición** | El usuario debe haber sido registrado previamente por el Administrador en la base de datos, con su correo de contacto ya validado. |
| **Postcondición** | Se genera un token JWT seguro y se redirige según el rol asignado. |
| **Flujo normal** | 1. El usuario ingresa su identificador único y contraseña.<br>2. El sistema valida las credenciales contra el hash almacenado.<br>3. Otorga acceso directo al panel respectivo, sin pasos intermedios de cambio de contraseña. |
| **Flujo alternativo** | 2a. Si los datos no coinciden, se muestra el mensaje de error: 'Identificador o contraseña incorrectos'. |
| **Condición especial** | La contraseña se almacena usando hashing seguro con BCrypt.<br>La contraseña asignada nunca se almacena ni se transmite en texto plano, únicamente su hash; ni siquiera el Administrador puede consultarla, solo desencadenar su reasignación (ver RF-M07-02). |

#### RF-M01-02 — Recuperación de Acceso por Correo de Contacto
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M01-02 |
| **Nombre** | Recuperación de Acceso por Correo de Contacto |
| **Descripción** | Permite a un elector recuperar el acceso a su cuenta cuando olvida su contraseña, solicitando el envío de una nueva contraseña aleatoria a su correo de contacto registrado, sin intervención manual del Administrador. |
| **Prioridad** | Alta |
| **Precondición** | El elector cuenta con un correo de contacto registrado y activo en el censo. |
| **Postcondición** | Se genera y almacena (hash) una nueva contraseña aleatoria; la anterior queda invalidada. |
| **Flujo normal** | 1. El elector ingresa su documento en la pantalla 'Recuperar acceso'.<br>2. El sistema genera una nueva contraseña aleatoria y actualiza el hash almacenado.<br>3. Envía la nueva contraseña al correo de contacto registrado, respetando la cola de envío progresivo (RN-9) si hay otras solicitudes en curso.<br>4. Se registra la solicitud en el log de auditoría (RN-8). |
| **Flujo alternativo** | 1a. Si el documento no existe en el censo o el elector está en estado 'Eliminado', el sistema muestra un mensaje genérico sin confirmar ni negar la existencia del registro (evita enumeración de usuarios). |
| **Condición especial** | Este flujo reutiliza el mismo mecanismo de asignación aleatoria de contraseña de RF-M02-01; no existe una tabla ni un enlace de restablecimiento con token, para mantener la superficie de ataque mínima. |

### 4.2 M02 — Gestión del Censo Electoral (Exclusivo Administrador)

#### RF-M02-01 — Carga del Censo Electoral y Restricción de Registro
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M02-01 |
| **Nombre** | Carga del Censo Electoral y Restricción de Registro |
| **Descripción** | Permite al Administrador registrar electores de forma individual o masiva mediante archivos planos (documento, nombre, grado, **correo de contacto**), inhabilitando por completo la opción de auto-inscripción. El censo es persistente: los registros no se eliminan al finalizar el año lectivo, únicamente se actualiza su grado mediante RF-M02-03. |
| **Prioridad** | Alta |
| **Precondición** | El Administrador ha iniciado sesión de forma correcta. |
| **Postcondición** | Los electores quedan indexados de manera definitiva en el censo con estado 'Activo', correo de contacto registrado y una contraseña aleatoria asignada por el sistema, enviada por correo. |
| **Flujo normal** | 1. El Administrador accede a 'Gestión de Censo'.<br>2. Carga un archivo CSV (documento, nombre, grado, correo de contacto) o rellena el formulario individual, donde el correo de contacto es un campo obligatorio.<br>3. El sistema valida que no exista duplicado por documento; el correo de contacto puede repetirse entre distintos electores.<br>4. Persiste los datos, genera una contraseña aleatoria por cada elector y encola su envío por correo (RN-9). |
| **Flujo alternativo** | 3a. Si un identificador ya existe en el sistema, se reporta el error y se omite dicho registro, sugiriendo usar RF-M02-02 para modificarlo en vez de duplicarlo.<br>3b. Si falta el correo de contacto en una fila del CSV, dicho registro se marca como fallido en el reporte de carga y no se persiste. |
| **Condición especial** | El sistema no maneja el concepto de salón/curso paralelo; el único atributo académico del elector es el grado.<br>El sistema bloquea por completo cualquier ruta pública que intente realizar un registro autónomo.<br>El Administrador nunca ve la contraseña asignada en texto plano; solo puede confirmar si la notificación fue entregada o solicitar su reenvío (ver RN-9). |

#### RF-M02-02 — Consulta, Modificación y Eliminación Lógica de Electores
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M02-02 |
| **Nombre** | Consulta, Modificación y Eliminación Lógica de Electores |
| **Descripción** | Permite al Administrador consultar el listado completo de electores, modificar sus datos (nombre, documento, grado, correo de contacto, estado) y eliminarlos de forma lógica, preservando la información para fines de auditoría e integridad de votos históricos. |
| **Prioridad** | Alta |
| **Precondición** | Sesión de Administrador activa. |
| **Postcondición** | Los cambios quedan reflejados en el censo y registrados en el log de auditoría; ningún registro se elimina físicamente de la base de datos. |
| **Flujo normal** | 1. El Administrador busca o filtra un elector en el listado del censo.<br>2. Selecciona 'Editar' y modifica los campos requeridos, o selecciona 'Eliminar'.<br>3. Si elige 'Eliminar', el sistema cambia el campo estado_registro a 'Eliminado' (con fecha de baja) sin borrar el registro.<br>4. El sistema registra el cambio (usuario, campo, valor anterior, valor nuevo, fecha) en el log de auditoría. |
| **Flujo alternativo** | 3a. El Administrador puede restaurar un elector eliminado, devolviendo su estado_registro a 'Activo'. |
| **Condición especial** | Los registros de votación ya emitidos por un elector son inmutables y no se ven afectados por ninguna modificación o eliminación lógica de su perfil (ver RN-7). |

#### RF-M02-03 — Promoción Automática de Año Lectivo
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M02-03 |
| **Nombre** | Promoción Automática de Año Lectivo |
| **Descripción** | Permite al Administrador ejecutar, una sola vez por año lectivo, la promoción masiva y automática del grado de todos los electores activos, evitando la actualización manual uno por uno. |
| **Prioridad** | Alta |
| **Precondición** | Sesión de Administrador activa; existe una tabla de grados ordenada secuencialmente. |
| **Postcondición** | Cada elector activo avanza al siguiente grado según el orden definido; quienes se encuentren en el último grado pasan al estado 'Egresado'. |
| **Flujo normal** | 1. El Administrador marca previamente (opcional) las excepciones — por ejemplo, estudiantes repitentes — mediante el indicador 'excluir_de_promocion'.<br>2. Ejecuta la acción 'Iniciar Promoción de Año Lectivo'.<br>3. El sistema presenta una vista previa (total de electores a promover, excluidos y a egresar).<br>4. El Administrador confirma explícitamente la operación.<br>5. El sistema actualiza el grado de cada elector activo no excluido y marca como 'Egresado' a quienes correspondían al último grado.<br>6. Se registra la operación completa en el log de auditoría. |
| **Flujo alternativo** | 4a. Si el Administrador cancela la confirmación, no se aplica ningún cambio. |
| **Condición especial** | El sistema impide ejecutar esta acción más de una vez dentro del mismo año lectivo, salvo confirmación adicional explícita del Administrador. |

### 4.3 M03 — Gestión de Elecciones

#### RF-M03-01 — Creación y Parametrización de Elecciones
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M03-01 |
| **Nombre** | Creación y Parametrización de Elecciones |
| **Descripción** | Habilita al Administrador para configurar eventos electorales definiendo Título, Tipo (Personas o Temas/objetos), Fecha inicio, Fecha fin, Descripción, Grados que pueden votar, Hora inicio, Hora fin. |
| **Prioridad** | Alta |
| **Precondición** | Sesión de Administrador activa. |
| **Postcondición** | Elección registrada en estado 'Programada'. |
| **Flujo normal** | 1. El Administrador ingresa los datos del evento.<br>2. Configura los límites temporales.<br>3. Guarda el registro. |
| **Flujo alternativo** | 2a. Si la fecha de cierre es menor a la de inicio, el sistema solicita corregir los campos. |
| **Condición especial** | El paso de estados ('Programada' → 'Activa' → 'Finalizada') ocurre de manera automática en el servidor. |

#### RF-M03-02 — Edición y Eliminación Lógica de Procesos Electorales
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M03-02 |
| **Nombre** | Edición y Eliminación Lógica de Procesos Electorales |
| **Descripción** | Permite al Administrador modificar los parámetros de una elección o ejecutar su eliminación lógica en cualquier momento, inhabilitando su visibilidad y operación para los electores pero garantizando la inmutabilidad de los votos históricos (RN-7.1). |
| **Prioridad** | Alta |
| **Precondición** | Sesión de Administrador activa. |
| **Postcondición** | El proceso electoral queda actualizado o en estado 'Eliminado' (`deleted_at` registrado), y la acción queda asentada en la auditoría (RN-8). |
| **Flujo normal** | 1. El Administrador selecciona un proceso en el panel de control.<br>2. Selecciona 'Editar' para modificar parámetros o 'Eliminar' para darle de baja.<br>3. Al confirmar la eliminación, el sistema cambia el estado a 'Eliminado' y almacena la fecha de baja en `deleted_at`.<br>4. Se registra la acción en el log de auditoría. |
| **Flujo alternativo** | 3a. Si el Administrador cancela la eliminación en el modal de confirmación, no se aplica ningún cambio. |
| **Condición especial** | La eliminación es exclusivamente lógica; ningún voto ni candidato asociado se elimina físicamente de la base de datos (RN-7.1). |

### 4.4 M04 — Inscripción y Gestión de Candidatos

#### RF-M04-01 — Inscripción de Candidatos
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M04-01 |
| **Nombre** | Inscripción de Candidatos |
| **Descripción** | Permite al Administrador asociar electores específicos como candidatos a una determinada elección, incluyendo foto, tarjetón y propuestas (lista de puntos que el candidato presenta a los votantes). |
| **Prioridad** | Media |
| **Precondición** | La elección debe estar en estado 'Programada'. |
| **Postcondición** | Candidato asignado a la elección, visible en el tarjetón junto con sus propuestas. |
| **Flujo normal** | 1. El Administrador busca el elector en el censo.<br>2. Lo asigna como candidato a una elección.<br>3. Carga el elemento gráfico del tarjetón.<br>4. Registra las propuestas del candidato en formato de lista. |
| **Flujo alternativo** | 1a. Si el elector no existe en el censo, no se puede postular. |
| **Condición especial** | El sistema autogenera una opción por defecto para el 'Voto en Blanco'.<br>Las propuestas registradas se muestran obligatoriamente al elector antes de confirmar su voto (ver RF-M05-01). |

### 4.5 M05 — Proceso de Votación y Control de Voto Único

#### RF-M05-01 — Emisión de Voto y Control de Sufragio Único
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M05-01 |
| **Nombre** | Emisión de Voto y Control de Sufragio Único |
| **Descripción** | Garantiza que un elector emita un voto secreto, mostrando previamente una ventana con las propuestas del candidato seleccionado, e impide de forma absoluta un segundo intento de sufragio. |
| **Prioridad** | Alta |
| **Precondición** | Elector autenticado, elección activa y sin registros previos de sufragio. |
| **Postcondición** | El voto se cuenta de forma anónima y el elector pasa a estado 'Sufragó' en la elección. |
| **Flujo normal** | 1. El elector visualiza el tarjetón con todos los candidatos disponibles.<br>2. Al seleccionar un candidato, el sistema abre una ventana emergente con sus propuestas.<br>3. Desde la ventana, el elector puede elegir 'Volver' (regresa al tarjetón general sin registrar nada) o 'Confirmar Voto'.<br>4. Si confirma, el sistema disocia la identidad, cuenta el voto y actualiza su estado. |
| **Flujo alternativo** | 1a. Si el sistema detecta que el elector ya cuenta con estado 'Sufragó', bloquea el tarjetón de inmediato.<br>3a. Si el elector selecciona 'Volver', ningún voto se registra y puede elegir otro candidato o el mismo nuevamente. |
| **Condición especial** | El anonimato se asegura mediante la disociación estructural completa de las tablas.<br>La ventana de propuestas debe mostrarse obligatoriamente antes de habilitar el botón 'Confirmar Voto'. |

### 4.6 M06 — Escrutinio y Resultados en Tiempo Real (Acceso Condicionado)

#### RF-M06-01 — Visualización de Resultados en Tiempo Real Condicionada
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M06-01 |
| **Nombre** | Visualización de Resultados en Tiempo Real Condicionada |
| **Descripción** | Permite a los electores ver las gráficas de resultados dinámicos única y exclusivamente después de haber votado. |
| **Prioridad** | Alta |
| **Precondición** | El elector ha votado en la elección o el usuario posee el rol de Administrador. |
| **Postcondición** | Despliegue interactivo de las estadísticas mediante WebSockets. |
| **Flujo normal** | 1. El elector finaliza su votación.<br>2. El sistema le otorga acceso al Dashboard de Resultados.<br>3. Las gráficas se actualizan en vivo. |
| **Flujo alternativo** | 1a. Si un elector intenta entrar al módulo de resultados sin haber votado, el sistema deniega el acceso y redirige al tarjetón. |
| **Condición especial** | El Administrador está exento de la condición y ve los resultados continuamente. |

### 4.7 M07 — Perfil de Usuario y Autogestión de Credenciales

#### RF-M07-01 — Consulta y Edición de Perfil Propio
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M07-01 |
| **Nombre** | Consulta y Edición de Perfil Propio |
| **Descripción** | Permite a cualquier usuario autenticado (Administrador o Elector) consultar su información básica y modificar únicamente su correo de contacto y su contraseña. Los datos oficiales del censo (documento, nombre, grado, estado) permanecen bajo control exclusivo del Administrador (ver M02) y no son editables desde el perfil propio. |
| **Prioridad** | Media |
| **Precondición** | Usuario autenticado con sesión activa. |
| **Postcondición** | El correo de contacto y/o la contraseña quedan actualizados; el cambio queda registrado en el log de auditoría. |
| **Flujo normal** | 1. El usuario accede a 'Mi Perfil'.<br>2. Visualiza sus datos (nombre, documento, grado si aplica, correo de contacto).<br>3. Edita el correo de contacto y/o la contraseña.<br>4. El sistema exige la contraseña actual como confirmación antes de aplicar el cambio.<br>5. Persiste el cambio y notifica al correo de contacto anterior y/o nuevo que hubo una modificación de seguridad en la cuenta. |
| **Flujo alternativo** | 4a. Si la contraseña actual ingresada no coincide, el sistema rechaza el cambio sin revelar cuál campo fue incorrecto. |
| **Condición especial** | El documento, nombre y grado se muestran en modo solo lectura.<br>Todo cambio de correo de contacto o contraseña desde el perfil se registra en auditoría (RN-8), igual que si lo hiciera el Administrador. |

#### RF-M07-02 — Reasignación de Contraseña por el Administrador
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M07-02 |
| **Nombre** | Reasignación de Contraseña por el Administrador |
| **Descripción** | Permite al Administrador, desde el Censo Electoral, forzar la generación de una nueva contraseña aleatoria para un elector (por ejemplo, si el correo original no llegó o el elector perdió acceso a su correo), sin necesidad de que el propio elector la solicite. |
| **Prioridad** | Media |
| **Precondición** | Sesión de Administrador activa; el elector debe tener un correo de contacto vigente. |
| **Postcondición** | Se genera una nueva contraseña aleatoria, se envía al correo de contacto registrado y la anterior queda invalidada. |
| **Flujo normal** | 1. El Administrador selecciona un elector en el censo.<br>2. Ejecuta la acción 'Reasignar contraseña'.<br>3. El sistema genera y envía la nueva contraseña por correo (respetando RN-9 si es una operación masiva).<br>4. Se registra la acción en el log de auditoría. |
| **Flujo alternativo** | 1a. Si el elector no tiene correo de contacto registrado, el sistema exige capturarlo antes de continuar. |
| **Condición especial** | El Administrador nunca visualiza la contraseña generada; el sistema solo confirma si el envío fue exitoso. |

### 4.8 Requerimientos No Funcionales (RNF)

| Categoría | Especificación |
| :--- | :--- |
| **Seguridad** | Contraseñas almacenadas con BCrypt; sesiones basadas en JWT con expiración; toda acción administrativa sobre el censo queda auditada (RN-8). |
| **Rendimiento** | El dashboard de resultados debe reflejar un nuevo voto en un lapso no mayor a 3 segundos mediante WebSockets. |
| **Usabilidad** | La ventana de propuestas y los flujos de votación deben ser operables desde dispositivos móviles y de escritorio, con máximo 3 clics para emitir un voto. |
| **Disponibilidad** | El sistema debe estar disponible durante todo el horario configurado de una elección activa (RF-M03-01). |
| **Escalabilidad** | El proceso de promoción automática (RF-M02-03) debe soportar la actualización masiva de al menos 2000 electores en una sola operación. |
| **Compatibilidad** | Compatible con los navegadores modernos más utilizados en entornos educativos (Chrome, Edge, Firefox). |
| **Confiabilidad de notificaciones** | El envío masivo de correos de credenciales debe procesarse mediante una cola con control de tasa (RN-9), sin bloquear la operación de carga masiva del Administrador; el reporte de entregas fallidas debe estar disponible en un tiempo razonable tras finalizar el proceso. |

---

## 5. Referencias Bibliográficas
* IEEE Std 830-1998 — *Recommended Practice for Software Requirements Specifications*.
* OWASP Foundation — *Password Storage Cheat Sheet* (recomendaciones de hashing con BCrypt).
* RFC 7519 — *JSON Web Token (JWT)*.
* Documentación oficial del protocolo WebSocket (RFC 6455).
* Servicio Nacional de Aprendizaje — SENA, Programa de Análisis y Desarrollo de Software, Ficha 228118.
