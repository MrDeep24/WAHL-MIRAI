# Plan de Implementación: Sesión Única Activa y Revocación Inmediata de Acceso (v2.9)

Implementación del mecanismo de **Sesión Única Activa** (bloqueo de sesiones concurrentes) y **Revocación Inmediata de Acceso** ante eliminación lógica, desactivación o reseteo de cuentas, operando **100% a nivel de aplicación en memoria (sin modificar el esquema de base de datos MySQL)**.

Adicionalmente, el plan contempla la transición de versión de la documentación del proyecto de **v2.8.2** a **v2.9**, y el registro formal en `REGISTRO_CAMBIOS_kevin.md` con la fecha y hora actual.

---

## 1. Justificación Arquitectónica

1. **Cierre Inmediato de Cuentas Dadas de Baja:** Actualmente, ASP.NET Core valida la cookie de autenticación de forma autónoma en memoria hasta que expiran sus 2 horas de vigencia. Con `CookieAuthenticationEvents.OnValidatePrincipal`, cada petición HTTP valida en tiempo real que el usuario exista, su `status == "ACTIVO"` y su `deleted_at == null` (usando las columnas que ya existen en la tabla `users`). Si no cumple, la cookie se destruye de inmediato (`RejectPrincipal` + `SignOutAsync`).
2. **Sesión Única sin Alterar MySQL:** Siguiendo el patrón ya utilizado en el proyecto con `PendingPasswordStore`, se implementa un servicio singleton en memoria `IActiveSessionStore` respaldado por `ConcurrentDictionary<uint, string>`. Al iniciar sesión, se emite un nuevo GUID de sesión que sobreescribe cualquier sesión anterior. En cada petición, `OnValidatePrincipal` verifica en nanosegundos que el token de la cookie coincida con el registrado en memoria. Si un usuario abre sesión en otro dispositivo, el dispositivo anterior queda revocado al siguiente clic.
3. **Cero Impacto en el Modelo Relacional:** No requiere `ALTER TABLE`, no modifica el DDL de `wahl_mirai_db`, no altera el ERD, y no añade costo de I/O a disco para la validación de tokens concurrentes.

---

## 2. Cambios Propuestos

### Componente Backend (C# / ASP.NET Core)

#### [NEW] [IActiveSessionStore.cs](file:///c:/Users/APRENDIZ/Documents/proej/WAHL-MIRAI/WahlMirai.Web/Services/IActiveSessionStore.cs)
* Definir interfaz e implementación `ActiveSessionStore` como Singleton.
* Métodos:
  * `RegisterSession(uint userId, string sessionStamp)`: Registra o rota el token de sesión activa para el usuario.
  * `IsSessionValid(uint userId, string sessionStamp)`: Valida si el token enviado en la cookie coincide con el activo en memoria.
  * `InvalidateSession(uint userId)`: Remueve la sesión activa (usado en logout, eliminación y cambio de clave).

#### [MODIFY] [Program.cs](file:///c:/Users/APRENDIZ/Documents/proej/WAHL-MIRAI/WahlMirai.Web/Program.cs)
* Registrar `builder.Services.AddSingleton<IActiveSessionStore, ActiveSessionStore>();`.
* Enriquecer `builder.Services.AddAuthentication("Cookies").AddCookie(...)` incorporando `CookieAuthenticationEvents.OnValidatePrincipal`:
  * Extraer `userId` y claim `"SessionStamp"`.
  * Validar validez en `IActiveSessionStore`.
  * Validar en `WahlMiraiDbContext` que el usuario siga `status == "ACTIVO"` y `deleted_at == null` (consulta ligera con `.AsNoTracking()` y proyección de solo dos campos).
  * Si alguna validación falla: invocar `context.RejectPrincipal()` y `await context.HttpContext.SignOutAsync()`.

#### [MODIFY] [AuthController.cs](file:///c:/Users/APRENDIZ/Documents/proej/WAHL-MIRAI/WahlMirai.Web/Controllers/AuthController.cs)
* Inyectar `IActiveSessionStore`.
* En la acción `Login` (POST):
  * Generar `var sessionStamp = Guid.NewGuid().ToString();`.
  * Registrar la sesión en `_activeSessionStore.RegisterSession(voter.Id, sessionStamp);`.
  * Agregar el claim `new Claim("SessionStamp", sessionStamp)` a la identidad.
* En la acción `RegistroStep2` (POST, auto-registro):
  * Replicar la misma lógica de registro de sesión para el auto-login tras crear la cuenta.
* En la acción `Logout`:
  * Extraer el ID del usuario y llamar a `_activeSessionStore.InvalidateSession(userId);`.

#### [MODIFY] [ICensusService.cs](file:///c:/Users/APRENDIZ/Documents/proej/WAHL-MIRAI/WahlMirai.Web/Services/ICensusService.cs)
* Inyectar `IActiveSessionStore` en `CensusService`.
* En `SoftDeleteVoterAsync`: invalidar sesión activa del elector (`_sessionStore.InvalidateSession((uint)voterId)`).
* En `UpdateVoterAsync`: si el nuevo estado es distinto de `"ACTIVO"` o `"EGRESADO"`, invalidar sesión activa.
* En `ResetPasswordAsync`: invalidar sesión activa para forzar el re-ingreso con la nueva credencial.

#### [MODIFY] [IAdminAccountService.cs](file:///c:/Users/APRENDIZ/Documents/proej/WAHL-MIRAI/WahlMirai.Web/Services/IAdminAccountService.cs)
* Inyectar `IActiveSessionStore` en `AdminAccountService`.
* En `SoftDeleteAccountAsync`: invalidar sesión activa de la cuenta administrativa.
* En `UpdateAccountAsync`: si el estado cambia a inactivo o eliminado, invalidar sesión.

---

### Componente de Pruebas Unitarias

#### [NEW] [ActiveSessionStoreTests.cs](file:///c:/Users/APRENDIZ/Documents/proej/WAHL-MIRAI/WahlMirai.Tests/ActiveSessionStoreTests.cs)
* Pruebas de registro, validación, sobreescritura (sesión única / segundo login invalida el primero) e invalidación explícita (logout / soft-delete).

---

### Componente de Documentación (`docs/` - Transición v2.8.2 → v2.9)

#### [MODIFY] [docs/ers_wahl_mirai_v2_9.md](file:///c:/Users/APRENDIZ/Documents/proej/WAHL-MIRAI/docs/ers_wahl_mirai_v2_9.md) *(renombrado desde `ers_wahl_mirai_v2_8.2.md`)*
* Actualizar encabezados y control de versiones a **Versión 2.9**.
* Añadir la regla de negocio **`RN-14 — Sesión Única Activa e Invalidación Inmediata de Acceso`**:
  * Especifica la política de sesión única concurrente y la expulsión inmediata ante eliminación lógica o reseteo de clave.
* Actualizar requerimientos funcionales correspondientes:
  * **RF-M01-01 (Autenticación):** Postcondición de emisión de token de sesión única e invalidación de sesiones previas.
  * **RF-M02-01 / RF-M09-02 (Eliminación lógica de usuarios):** Postcondición de revocación inmediata de la sesión activa en el siguiente requerimiento HTTP.

#### [MODIFY] [docs/Arquitectura_y_Diseno_v2_9.md](file:///c:/Users/APRENDIZ/Documents/proej/WAHL-MIRAI/docs/Arquitectura_y_Diseno_v2_9.md) *(renombrado desde `Arquitectura_y_Diseno_v2_8.2.md`)*
* Actualizar encabezado a **Versión 2.9**.
* En la sección de Seguridad y Autenticación:
  * Documentar el patrón de sesión única mediante `IActiveSessionStore` y `CookieAuthenticationEvents.OnValidatePrincipal`.
  * Detallar el ciclo de vida de la cookie, validación de estado en BD (`status == 'ACTIVO' && deleted_at IS NULL`) y expulsión sin latencia.
  * Aclarar la decisión de diseño de preservar el esquema de base de datos intacto mediante gestión en memoria RAM concurrente.

#### [MODIFY] [docs/wahl_mirai_db_v2_9_completo.sql](file:///c:/Users/APRENDIZ/Documents/proej/WAHL-MIRAI/docs/wahl_mirai_db_v2_9_completo.sql) *(renombrado desde `wahl_mirai_db_v2_8.2_completo.sql`)*
* Actualizar encabezado del script DDL a **Versión 2.9**, documentando que mantiene total compatibilidad de esquema con v2.8.2.

---

### Registro de Cambios

#### [MODIFY] [REGISTRO_CAMBIOS_kevin.md](file:///c:/Users/APRENDIZ/Documents/proej/WAHL-MIRAI/REGISTRO_CAMBIOS_kevin.md)
* Agregar entrada con fecha y hora actual (`2026-09-16 17:25:00`):
  * **Título:** `## 📅 2026-09-16 17:25:00 — Sesión Única Activa, Revocación Inmediata de Acceso e Incremento de Versión a v2.9`
  * **Resumen General:** Explicación del bloqueo concurrente, solución al cierre de sesión para cuentas eliminadas y salto de versión sin alterar la base de datos MySQL.
  * **Detalle de Cambios:** Desglose exhaustivo archivo por archivo.
  * **Verificación y Control de Alcance:** Pruebas unitarias y compilación con `dotnet build`.

---

## 3. Plan de Verificación

### Pruebas Automatizadas
* Ejecutar la suite de pruebas unitarias en `WahlMirai.Tests`:
  ```powershell
  dotnet test WahlMirai.Tests/WahlMirai.Tests.csproj
  ```
* Verificar que tanto las pruebas existentes como las nuevas de `ActiveSessionStoreTests` pasen con éxito (0 fallos).

### Compilación y Linter
* Compilar todo el proyecto para validar ausencia de errores sintácticos y advertencias:
  ```powershell
  dotnet build WahlMirai.Web/WahlMirai.Web.csproj
  ```

### Verificación Funcional
1. Simular inicio de sesión -> Se genera token en cookie y en memoria.
2. Simular un segundo inicio de sesión con el mismo usuario -> El primer token queda sobreescrito en el store.
3. Simular validación del token viejo en `OnValidatePrincipal` -> Se rechaza (`RejectPrincipal`).
4. Simular eliminación lógica de cuenta (`Status = "ELIMINADO"`) -> `OnValidatePrincipal` detecta estado inválido y cierra la sesión.
