# Manual de Usuario
## Sistema de Votaciones Digitales (Wahl Mirai)

Este manual de usuario explica de manera sencilla cómo interactuar con el sistema de elecciones **Wahl Mirai**, detallando los flujos correspondientes tanto para el perfil **Administrador** como para el **Elector**.

---

## 1. Acceso al Sistema

1.  Abra su navegador de internet e ingrese a la dirección provista por la institución.
2.  Visualizará la pantalla de **Inicio de Sesión** ([Views/Account/Login.cshtml](file:///c:/Users/HUAWEI/OneDrive - SENA/Escritorio/AAAA/Proyecto-Principal/Views/Account/Login.cshtml)).
3.  Escriba su correo electrónico institucional y su contraseña.
4.  Presione el botón **Iniciar Sesión**.
    *   Si entra como **Administrador**, el sistema lo dirigirá al panel de gestión electoral.
    *   Si entra como **Elector (Votante)**, el sistema lo dirigirá a la pantalla de elecciones activas de su grado.

---

## 2. Guía para el Administrador (Gestor Electoral)

El administrador cuenta con un menú lateral izquierdo que contiene accesos directos al Dashboard de elecciones y al Historial de votación.

### A. Dashboard de Elecciones (`Views/Voting/Index.cshtml`)
Es la pantalla de control principal del Administrador. Muestra:
*   **KPIs Generales:** Estado del sistema de encriptación y auditoría criptográfica, conteo de elecciones activas, porcentaje global de participación y estado del próximo cierre.
*   **Accesos Rápidos:** 
    *   **Nueva Elección:** Botón para configurar un nuevo proceso de votación.
    *   **Registro de Elecciones:** Botón para navegar al historial.
*   **Procesos Recientes:** Tabla con los últimos eventos creados, su fecha límite y badges coloreados según su estado (ej. *En Curso*, *Cerrado*, *Borrador*).

### B. Crear un Proceso Electoral (`Views/Voting/Crear.cshtml`)
Para programar una nueva votación:
1.  Haga clic en **Nueva Elección** en el Dashboard.
2.  **Columna Izquierda (Información y Cronograma):**
    *   Complete el **Nombre de la Elección** (ej. *Elección Personero 2026*).
    *   Establezca la **Fecha y Hora de Apertura**.
    *   Establezca la **Fecha y Hora de Cierre** (el sistema bloqueará la recepción de nuevos votos tras expirar este plazo).
3.  **Columna Derecha (Censo Electoral y Registro):**
    *   Seleccione los grados escolares participantes (ej. Marcar *11°* o *Todos los Grados*).
    *   Marque si se permite la auto-inscripción de candidatos por parte de los estudiantes.
4.  Haga clic en **Crear Elección** para publicar el evento.

### C. Registro Histórico de Votaciones (`Views/Voting/Registro.cshtml`)
*   Muestra una tabla compacta con todas las elecciones que se han realizado.
*   Puede utilizar la **Barra de Búsqueda** y el **Filtro de Estado** para ubicar elecciones específicas.
*   Haga clic en el botón **Detalle** en cualquier fila para auditar los resultados en tiempo real.

### D. Escrutinio y Resultados en Tiempo Real (`Views/Voting/Detalle.cshtml`)
Al seleccionar una elección (esté activa o ya finalizada), se despliega el panel de auditoría:
*   **Ganador Parcial / Total (Destacado Izquierdo):** Tarjeta visual premium que resalta al candidato con mayor votación hasta el momento, mostrando su nombre, lema, porcentaje de aceptación y un avatar de iniciales (ej. `SM` para Santiago Muñoz).
*   **Resultados por Candidato (Columna Derecha):** Tabla con el listado completo de candidatos ordenados de mayor a menor votación, mostrando el número exacto de votos y barras de porcentaje visuales.
*   **Resumen del Censo:** Detalla la cantidad de votos en blanco, abstenciones y porcentaje final de participación del electorado. **Toda esta información se actualiza automáticamente en tiempo real con cada voto registrado.**

---

## 3. Guía para el Elector (Votante / Estudiante)

El elector tiene una interfaz adaptada para dispositivos móviles y computadoras de escritorio, enfocada exclusivamente en el ejercicio seguro y rápido del voto.

### A. Elecciones Disponibles (`Views/Elecciones/Index.cshtml`)
Al ingresar, el estudiante verá las tarjetas de elecciones en curso habilitadas para su grado:
*   **Tarjeta de Elección Activa:** Muestra el título, la fecha límite de cierre, el estado de postulación y un botón prominente:
    *   Si el estudiante no ha votado, el botón dirá **"Ir a Votar"**. Además, tendrá disponible el enlace **"Ver Resultados"** para seguir la elección en vivo.
    *   Si el estudiante ya ejerció su voto, el botón estará deshabilitado y dirá **"Voto Registrado"**, pero podrá seguir visualizando el progreso en tiempo real.

### B. Inscripción de Candidaturas (`Views/Elecciones/Inscripcion.cshtml`)
Si la elección lo permite, el estudiante puede postularse:
1.  Haga clic en **Postularse** en la tarjeta de la elección.
2.  Complete el formulario con su nombre, lema electoral y propuestas principales de campaña.
3.  Presione **Enviar Candidatura**. La postulación quedará en estado "Pendiente" hasta que el Administrador la apruebe.

### C. Ejercer el Voto (`Views/Elecciones/Votar.cshtml` - Tarjetón Digital)
1.  Haga clic en **Ir a Votar** desde su panel de inicio.
2.  Se desplegará el **Tarjetón Electoral Digital**, mostrando a los candidatos con sus fotos o avatares de iniciales, nombres y propuestas.
3.  Haga clic sobre la tarjeta del candidato de su preferencia (o la tarjeta de **Voto en Blanco**).
4.  Aparecerá un **Ventana de Confirmación (Modal)** que le preguntará: *"¿Está seguro que desea votar por [Nombre del Candidato]? Esta acción no se puede deshacer y su voto es secreto"*.
5.  Haga clic en **Confirmar Voto**. El sistema guardará el voto y registrará su participación de forma permanente, impidiendo cualquier reingreso.

### D. Consulta de Resultados en Vivo (`Views/Elecciones/Resultados.cshtml`)
*   **Resultados en Tiempo Real:** El estudiante puede ingresar en todo momento haciendo clic en el enlace **"Ver Resultados"** de la elección.
*   El estudiante visualizará gráficos interactivos limpios con los porcentajes de votación y el conteo de votos parciales/finales de cada candidato de su grado escolar en tiempo real.
