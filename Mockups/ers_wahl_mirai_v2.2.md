# ESPECIFICACIÓN DE REQUERIMIENTOS DE SOFTWARE
**(ERS — IEEE Std 830-1998)**

## Sistema de Votaciones Digitales Estudiantiles
**Wahl Mirai — Versión 2.2**

* **Programa:** Análisis y Desarrollo de Software
* **Servicio Nacional de Aprendizaje — SENA**
* **Ficha:** 228118
* **Colombia, 2026**
* **Control de versión:** v2.2 — Persistencia del censo, promoción automática, eliminación lógica y visualización de propuestas en votación

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
   4.7 [Requerimientos No Funcionales (RNF)](#47-requerimientos-no-funcionales-rnf)  
5. [Referencias Bibliográficas](#5-referencias-bibliográficas)

---

## 1. Introducción

### 1.1 Propósito
Este documento define los requerimientos para el sistema **'Wahl Mirai' Versión 2.2**, que incorpora cuatro cambios principales respecto a la versión anterior:
1. Un esquema de credenciales autogeneradas para los electores basado en su número de documento y el año lectivo vigente, con cambio obligatorio en el primer inicio de sesión.
2. Un censo electoral persistente que elimina el salón/curso paralelo como atributo y conserva la identidad del elector año tras año, actualizando únicamente su grado mediante un mecanismo de promoción automática.
3. La eliminación lógica (no física) de electores, con edición completa de sus datos y trazabilidad mediante auditoría, preservando siempre la inmutabilidad de los votos ya emitidos.
4. Una ventana emergente de propuestas del candidato durante el proceso de votación, con opciones explícitas para volver al tarjetón o confirmar el voto.

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
* **RN-2 — Credenciales de Acceso Autogeneradas:** Se elimina la dependencia de un correo institucional. Los electores acceden con su identificador único (documento) y una contraseña inicial autogenerada mediante la combinación documento + año lectivo vigente (ej. `'1029384756.2026'`), la cual debe ser cambiada obligatoriamente en el primer inicio de sesión.
* **RN-3 — Voto Único y Bloqueo Seguro:** Cada elector puede votar únicamente una vez por evento electoral. Al confirmar el sufragio, su estado se actualiza irreversiblemente.
* **RN-4 — Resultados en Tiempo Real Condicionados al Voto:** Los electores tienen permitido ver los gráficos y estadísticas de escrutinio en vivo y en tiempo real, siempre y cuando hayan ejercido previamente su derecho al voto en dicha elección. Si no han votado, el acceso al panel de visualización estará estrictamente bloqueado.
* **RN-5 — Excepción del Administrador en Escrutinio:** El Administrador puede visualizar los resultados en tiempo real de forma irrestricta en cualquier momento, sin necesidad de cumplir la condición de voto.
* **RN-6 — Persistencia del Censo Electoral:** Ningún registro del censo se elimina físicamente de la base de datos. La identidad del elector (documento, nombre) se conserva de un año lectivo a otro; el único dato académico que varía es el grado, el cual se actualiza mediante el mecanismo de promoción automática (RF-M02-03) y no requiere el concepto de salón/curso paralelo.
* **RN-7 — Edición Administrativa con Inmutabilidad del Voto:** El Administrador puede modificar cualquier dato de un elector (nombre, documento, grado, estado) y eliminarlo de forma lógica en cualquier momento. Sin embargo, los registros de votación ya emitidos son absolutamente inmutables y no se ven afectados por ninguna modificación o eliminación lógica del perfil del elector.
* **RN-8 — Trazabilidad de Cambios Administrativos:** Toda modificación, eliminación lógica, restauración o promoción masiva realizada sobre el censo electoral queda registrada en un log de auditoría (usuario responsable, campo afectado, valor anterior, valor nuevo y fecha).

---

## 4. Requerimientos Específicos por Módulo

### 4.1 M01 — Gestión de Acceso y Sesión

#### RF-M01-01 — Autenticación de Usuarios por Identificador Único
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M01-01 |
| **Nombre** | Autenticación de Usuarios por Identificador Único |
| **Descripción** | Permite el acceso seguro de Administradores y Electores utilizando su documento o código registrado y contraseña, prescindiendo de correos institucionales. Para los electores, la contraseña inicial se genera automáticamente combinando su número de documento con el año lectivo vigente (documento.año), evitando que el Administrador deba asignar claves manualmente una por una. |
| **Prioridad** | Alta |
| **Precondición** | El usuario debe haber sido registrado previamente por el Administrador en la base de datos. |
| **Postcondición** | Se genera un token JWT seguro y se redirige según el rol asignado. |
| **Flujo normal** | 1. El usuario ingresa su identificador único y contraseña.<br>2. El sistema valida las credenciales contra el hash almacenado.<br>3. Si es el primer inicio de sesión del elector con la clave autogenerada, se ejecuta RF-M01-02 antes de continuar.<br>4. Otorga acceso al panel respectivo. |
| **Flujo alternativo** | 2a. Si los datos no coinciden, se muestra el mensaje de error: 'Identificador o contraseña incorrectos'. |
| **Condición especial** | La contraseña se almacena usando hashing seguro con BCrypt.<br>La contraseña autogenerada nunca se almacena ni se transmite en texto plano, únicamente su hash. |

#### RF-M01-02 — Cambio Obligatorio de Contraseña en Primer Inicio de Sesión
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M01-02 |
| **Nombre** | Cambio Obligatorio de Contraseña en Primer Inicio de Sesión |
| **Descripción** | Obliga al elector a establecer una contraseña personal la primera vez que inicia sesión con la clave autogenerada (documento.año), impidiendo el acceso al resto del sistema hasta completar el cambio. |
| **Prioridad** | Alta |
| **Precondición** | El elector se autenticó correctamente con su contraseña autogenerada y aún no ha realizado el cambio inicial. |
| **Postcondición** | La contraseña autogenerada queda invalidada y se almacena únicamente el hash BCrypt de la nueva contraseña. |
| **Flujo normal** | 1. El sistema detecta el indicador 'requiere_cambio_clave = true' en el perfil del elector.<br>2. Bloquea la navegación y presenta el formulario de cambio de contraseña.<br>3. El elector ingresa y confirma su nueva contraseña.<br>4. El sistema valida los requisitos mínimos de seguridad y actualiza el hash.<br>5. Marca 'requiere_cambio_clave = false' y redirige al panel del elector. |
| **Flujo alternativo** | 3a. Si la nueva contraseña no cumple los requisitos mínimos (longitud, no ser igual a la autogenerada), el sistema solicita corregirla. |
| **Condición especial** | Este flujo no aplica al Administrador, quien gestiona su propia contraseña de forma independiente. |

### 4.2 M02 — Gestión del Censo Electoral (Exclusivo Administrador)

#### RF-M02-01 — Carga del Censo Electoral y Restricción de Registro
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M02-01 |
| **Nombre** | Carga del Censo Electoral y Restricción de Registro |
| **Descripción** | Permite al Administrador registrar electores de forma individual o masiva mediante archivos planos (documento, nombre, grado), inhabilitando por completo la opción de auto-inscripción. El censo es persistente: los registros no se eliminan al finalizar el año lectivo, únicamente se actualiza su grado mediante RF-M02-03. |
| **Prioridad** | Alta |
| **Precondición** | El Administrador ha iniciado sesión de forma correcta. |
| **Postcondición** | Los electores quedan indexados de manera definitiva en el censo con estado 'Activo' y contraseña inicial autogenerada (documento.año). |
| **Flujo normal** | 1. El Administrador accede a 'Gestión de Censo'.<br>2. Carga un archivo CSV (documento, nombre, grado) o rellena el formulario individual.<br>3. El sistema valida que no existan duplicados por documento.<br>4. Persiste los datos y asigna la contraseña inicial autogenerada. |
| **Flujo alternativo** | 3a. Si un identificador ya existe en el sistema, se reporta el error y se omite dicho registro, sugiriendo usar RF-M02-02 para modificarlo en vez de duplicarlo. |
| **Condición especial** | El sistema no maneja el concepto de salón/curso paralelo; el único atributo académico del elector es el grado.<br>El sistema bloquea por completo cualquier ruta pública que intente realizar un registro autónomo. |

#### RF-M02-02 — Consulta, Modificación y Eliminación Lógica de Electores
| Campo | Detalle |
| :--- | :--- |
| **Identificador** | RF-M02-02 |
| **Nombre** | Consulta, Modificación y Eliminación Lógica de Electores |
| **Descripción** | Permite al Administrador consultar el listado completo de electores, modificar sus datos (nombre, documento, grado, estado) y eliminarlos de forma lógica, preservando la información para fines de auditoría e integridad de votos históricos. |
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

### 4.7 Requerimientos No Funcionales (RNF)

| Categoría | Especificación |
| :--- | :--- |
| **Seguridad** | Contraseñas almacenadas con BCrypt; sesiones basadas en JWT con expiración; toda acción administrativa sobre el censo queda auditada (RN-8). |
| **Rendimiento** | El dashboard de resultados debe reflejar un nuevo voto en un lapso no mayor a 3 segundos mediante WebSockets. |
| **Usabilidad** | La ventana de propuestas y los flujos de votación deben ser operables desde dispositivos móviles y de escritorio, con máximo 3 clics para emitir un voto. |
| **Disponibilidad** | El sistema debe estar disponible durante todo el horario configurado de una elección activa (RF-M03-01). |
| **Escalabilidad** | El proceso de promoción automática (RF-M02-03) debe soportar la actualización masiva de al menos 2000 electores en una sola operación. |
| **Compatibilidad** | Compatible con los navegadores modernos más utilizados en entornos educativos (Chrome, Edge, Firefox). |

---

## 5. Referencias Bibliográficas
* IEEE Std 830-1998 — *Recommended Practice for Software Requirements Specifications*.
* OWASP Foundation — *Password Storage Cheat Sheet* (recomendaciones de hashing con BCrypt).
* RFC 7519 — *JSON Web Token (JWT)*.
* Documentación oficial del protocolo WebSocket (RFC 6455).
* Servicio Nacional de Aprendizaje — SENA, Programa de Análisis y Desarrollo de Software, Ficha 228118.
