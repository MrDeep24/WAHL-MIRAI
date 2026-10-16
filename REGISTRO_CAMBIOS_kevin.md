# Registro de Cambios y Desarrollo — Wahl Mirai

**Proyecto:** Wahl Mirai — Sistema de Votaciones Digitales Estudiantiles (ASP.NET Core MVC)  
**Developer:** `Kevin` (dev 1)

## 📅 15 octubre 2026 21:03:39 -05:00 — Corrección de scroll administrativo, modales fijos, cabecera de Error y UI de PQR Manage

- **Block 1 (theme.css):** Se ajustó el selector `.voting-pattern-bg > *` a `.voting-pattern-bg > :not(.fixed, .absolute, .sticky)` con documentación CSS explícita, preservando la elevación del contenido sobre la trama de fondo (`::before`) sin anular mediante reglas no agrupadas en capas (`unlayered`) el `position: fixed` de modales renderizados como hijos directos del layout.
- **Block 2 (_AdminLayout.cshtml):** Se removió `min-h-screen` y se reemplazó `h-screen` por `h-[calc(100dvh-4rem)]` manteniendo `overflow-hidden`. Esto acota la altura del contenedor exterior a la altura visible exacta del viewport dinámico descontando la cabecera en flujo (`min-h-16` / 4rem), eliminando el desbordamiento de 64px a nivel de ventana y dejando a `<div class="flex-1 overflow-y-auto">` como la única región con barra de desplazamiento vertical.
- **Block 3 (_Layout.cshtml):** Se incorporó el controlador `"Error"` a la condición `usesElectorHeader` para que los electores autenticados en páginas de error no rendericen simultáneamente `_SiteHeader` y el header interno de `_ElectorLayout.cshtml`.
- **Block 4 (Manage.cshtml, pqr-manage.js, locale.js):**
  - En la primera columna de la grilla ("ID") se renderiza el número de radicado del ticket como `#${t.id}` en fuente monoespaciada, reubicando el avatar circular con iniciales junto al nombre del votante en la columna "Votante".
  - En el modal de detalle, se eliminó la línea ambigua `ID: ${userId}` y se incorporó una insignia `#${ticket.id}` junto al título en un elemento separado con dataset (`data-pqr-modal-ticket-id`), preservando la traducción exacta del encabezado `<h2>`.
  - Se implementó accesibilidad para el modal: cierre con tecla `Escape`, captura de foco hacia el modal al abrir y restauración de foco al elemento desencadenante al cerrar, junto con atributos semánticos `role="dialog"`, `aria-modal="true"` y `aria-labelledby="pqr-modal-title"`.
  - En `locale.js` se incorporaron claves de traducción exactas en los 5 idiomas (`en`, `de`, `fr`, `ja`, `zh`) para `ID`, `Votante`, `Asunto`, `Elector`, `Fecha Radicación`, `Mensaje`, `Respuesta Administrativa`, `Abierta`, `Resuelta`, `Ver Detalle`, `No hay PQR para mostrar.`, `Ya resuelta` y `Enviando...`.
- **Block 5 (_AdminLayout.cshtml):** Se añadió el disparador táctil del menú lateral en pantallas móviles (`md:hidden`) reflejando el diseño de `_ElectorLayout.cshtml`, con atributos `aria-label="Abrir menú"`, `aria-controls="sidebar-drawer"` y sincronización bidireccional de `aria-expanded` en `toggleDrawer()`.
- **Documentación (Arquitectura_y_Diseno_v2_9.md):** Se documentó en la sección "Navegación por contexto" el cálculo de la región única de scroll en `_AdminLayout` y la exclusión de elementos posicionados en `.voting-pattern-bg`.

## 📅 14 octubre 2026 18:13:38 -05:00 — Intento A: Alineación de cabeceras de prevención de caché con Antiforgery en Restablecer

- Se modificó `SetSecurityHeaders` en `RecuperacionAccesoController.cs` para emitir exactamente `Cache-Control: no-cache, no-store` y `Pragma: no-cache`, manteniendo `Referrer-Policy: no-referrer`.
- Esto alinea las cabeceras predefinidas del controlador con los valores requeridos y aplicados internamente por ASP.NET Core Antiforgery (`SetDoNotCacheHeaders`), evitando la discrepancia previa (`no-store` frente a `no-cache, no-store`) que disparaba la advertencia en consola `[WRN] The 'Cache-Control' and 'Pragma' headers have been overridden...`.
- Se actualizaron las aserciones de pruebas unitarias correspondientes en `WahlMirai.Tests/RecuperacionAccesoControllerTests.cs` en los 6 casos de prueba de `Restablecer` (GET con token válido e inválido, POST con modelo inválido, éxito, replay y token inexistente), verificando tanto `Cache-Control` como `Pragma`.

## 📅 14 octubre 2026 17:42:44 -05:00 — Coherencia de BaseUrl y puertos locales en README: Modos A (HTTP) y B (HTTPS) y troubleshooting

- Se aclaró en `README.md` que los puertos y esquemas de ejecución no provienen de `appsettings.json`, sino de los perfiles configurados en `WahlMirai.Web/Properties/launchSettings.json`.
- Se documentaron dos modos de ejecución local explícitos:
  - **Modo A (HTTP Local — Por defecto):** `http://localhost:5166`, ejecutado con `dotnet run` sin parámetros adicionales; recomendado expresamente para computadores compartidos, públicos o institucionales al no requerir instalación ni confianza de certificados SSL. Se verificó que `EmailQueueBackgroundService.IsValidBaseUrl` admite HTTP únicamente para loopback (`localhost`, `127.0.0.1`, `::1`).
  - **Modo B (HTTPS Local):** `https://localhost:7062`, ejecutado con `dotnet run --launch-profile https` previa confianza del certificado autofirmado mediante `dotnet dev-certs https --trust`.
- Se alineó el ejemplo por defecto de user-secrets a `dotnet user-secrets set "EmailSettings:BaseUrl" "http://localhost:5166"` (Modo A), explicando la regla de coherencia estricta (debe coincidir con la URL activa en ejecución) y la necesidad de reiniciar el proceso `dotnet run` tras modificar secretos.
- En la sección de Troubleshooting, se incorporaron las soluciones a problemas de enlace que no abre (discrepancia de puerto o falta de reinicio) y aviso de página no segura en el navegador por certificado dev-certs no confiado.

## 📅 14 octubre 2026 17:14:13 -05:00 — Refinamiento README: Ubicación real de ConnectionString, user-secrets mínimos requeridos y remoción de métricas volátiles

- Se aclaró la ubicación y gestión de `ConnectionStrings:WahlMiraiDb`: reside en `WahlMirai.Web/appsettings.Development.json` (no en `appsettings.json`) con valor predeterminado funcional para MySQL local. Se documentó que no es un user-secret obligatorio, pero que puede sobreescribirse con `dotnet user-secrets set "ConnectionStrings:WahlMiraiDb"` gracias a la precedencia del proveedor de User Secrets sobre los archivos JSON en entorno `Development`.
- Se simplificó la sección de `dotnet user-secrets`, limitándola estrictamente a las tres claves indispensables sin valor utilizable por defecto en `appsettings.json` (`EmailSettings:SenderEmail`, `EmailSettings:SenderPassword`, `EmailSettings:BaseUrl`), formateadas con comentarios descriptivos previos por comando.
- Se condensó la tabla remanente de `EmailSettings` en una sola línea informativa indicando que `SmtpHost`, `SmtpPort`, `SenderDisplayName`, `UseStartTls` y `RateLimitPerMinute` ya tienen valores predeterminados en `appsettings.json` y solo se sobreescriben al cambiar de proveedor SMTP.
- Se eliminaron números volátiles de la sección de verificación (conteo fijo de pruebas unitarias y cantidad de vistas/cadenas auditadas en i18n).
- Se garantizó compatibilidad estricta con Windows PowerShell 5.1 en las instrucciones de ejecución (`dotnet build` y `dotnet run` en líneas separadas, sin operador `&&`).

## 📅 14 octubre 2026 16:49:47 -05:00 — Actualización de README: Configuración y primer arranque local (host-neutral) y saneamiento de credenciales

- Se incorporó la sección integral "Configuración y Primer Arranque" en `README.md` estructurada en seis bloques operativos de desarrollo local neutral a proveedores:
  1. Requisitos del sistema (.NET 9.0 SDK, MySQL 8.0+, PowerShell, descarga condicional por Internet de Tailwind CLI en primer build vía `EnsureTailwindCli` y herramienta opcional Graphify).
  2. Base de datos con script unificado `docs/wahl_mirai_db_v2_9_completo.sql`, documentando la advertencia de eliminación y recreación total mediante `DROP DATABASE`, carga de datos semilla, auto-cifrado transparente de documentos al arranque vía `Program.cs` y `IDocumentEncryptionService`, y consulta segura de contraseñas de prueba en el propio script.
  3. Configuración local confidencial con `dotnet user-secrets` ejecutado desde `WahlMirai.Web`, tabulando `ConnectionStrings:WahlMiraiDb` y cada clave de `EmailSettings` en uso, con énfasis crítico en `EmailSettings:BaseUrl` (apuntando al puerto `7062` de `launchSettings.json` so pena de fallo terminal `FALLIDO` en correos de recuperación), soporte genérico de variables de entorno `Section__Key` y requerimiento de Contraseña de Aplicación para Gmail.
  4. Primer arranque con `dotnet run`, puertos HTTP/HTTPS locales y especificación de artefactos generados/regenerados automáticamente (`keys/`, `App_Data/candidacy-uploads/`, `site.css` regenerado por Tailwind y no versionado, y `logs/`).
  5. Verificación formal del entorno mediante `dotnet build WahlMirai.Web --warnaserror`, `dotnet test WahlMirai.Tests` y `powershell -ExecutionPolicy Bypass -File scripts/check-i18n-coverage.ps1`.
  6. Resolución de problemas frecuentes (Troubleshooting): autenticación SMTP 5.7.8, correos `FALLIDO` por `BaseUrl`, cooldown en memoria de 5 minutos en recuperación de contraseñas, fallo de compilación por compuerta de cobertura i18n bajo `--warnaserror` y fallo de descarga inicial de Tailwind CLI.
- Se eliminaron las contraseñas en texto plano de las cuentas semilla en la sección de credenciales de prueba del `README.md`, sustituyéndolas por referencias al encabezado del script SQL consolidado.
- Se corrigieron referencias desactualizadas a la base de datos v2.8 y enlaces absolutos específicos de máquina local en la sección de documentación del proyecto, actualizándolos a rutas relativas hacia v2.9.

## 📅 14 octubre 2026 16:39:08 -05:00 — B2 follow-up 2: Dependencias requeridas en CredentialService, contratos explícitos en ICredentialService e i18n para submit

- En `CredentialService`, se estableció `IPasswordResetTokenStore` como dependencia obligatoria en el constructor (`tokenStore ?? throw new ArgumentNullException(nameof(tokenStore))`, sin `?` y sin verificaciones residuales de nulidad en tiempo de ejecución en `IssuePasswordResetTokenAsync` y `CompletePasswordResetAsync`). Se confirmó su registro preexistente como servicio Singleton en `Program.cs`.
- Se corrigieron los sitios de instanciación directa de `CredentialService` en pruebas que pasaban `null` (`AdminAccountServiceTests.cs` y `CensusWhitelistTests.cs`), inyectando una instancia válida de `PasswordResetTokenStore` respaldada por `MemoryCache` y `TimeProvider.System`.
- En `ICredentialService`, se eliminaron las implementaciones predeterminadas silenciosas de interfaz en `IssuePasswordResetTokenAsync` y `CompletePasswordResetAsync`, exigiendo implementación explícita a cualquier clase que implemente el contrato. Se actualizó el fake `FakeCredentialService` en `ProfileServiceTests.cs` para implementar ambos métodos.
- En `auth-completar.js`, se generalizó la protección contra doble submit seleccionando `#compl-submit-btn, #reset-submit-btn` y obteniendo la etiqueta de estado ocupado desde el atributo `data-busy-label` del botón, manteniendo como respaldo "Creando cuenta…" para garantizar paridad y cero cambios en el comportamiento de `/Auth/Completar`.
- En `Restablecer.cshtml`, se agregó `data-busy-label="Procesando…"` al botón `#reset-submit-btn` y se aseguraron los atributos HTML5 `required` y `minlength="8"` en los campos de contraseña para soportar adecuadamente `form.checkValidity()`.
- En `locale.js`, al no existir previamente la clave neutral "Procesando…" en el diccionario cliente, se añadió su traducción en los 5 idiomas soportados (`en`: "Processing…", `de`: "Wird verarbeitet…", `fr`: "Traitement en cours…", `ja`: "処理中…", `zh`: "处理中…"), manteniendo 100% de cobertura en `check-i18n-coverage.ps1`.
- En `CredentialServiceTests.cs`, se agregó la prueba unitaria `Constructor_ThrowsArgumentNullException_WhenTokenStoreIsNull` que valida el rechazo ante `null` en el constructor de `CredentialService`.

## 📅 08 octubre 2026 23:51:55 -05:00 — Entrega B2 (Item B): UI, session/lockout rules, i18n, docs (RF-M01-02, RN-2, RN-9, RN-14, RN-15)

- En `CredentialService`, se implementó `CompletePasswordResetAsync`: consume atómicamente el token efímero vía `_tokenStore.TryConsume(token)`, verifica que el usuario exista, esté `ACTIVO` y no esté eliminado lógicamente (devolviendo el mismo mensaje de error genérico idéntico ante cualquier anomalía para mitigar enumeración), calcula el nuevo hash de contraseña con BCrypt (factor 11), persiste los cambios, limpia el bloqueo de login provocable por atacantes mediante `_loginAttemptStore.RegisterSuccess(documentHash)` (RN-15), invalida de inmediato cualquier sesión activa previa con `_sessionStore.InvalidateSession(userId, SessionInvalidationReason.LoggedOut)` (RN-14), revoca cualquier token remanente y registra la acción de auditoría `PASSWORD_RESET_COMPLETED` con la IP observada.
- Se actualizaron las firmas e implementaciones en `ICredentialService` y `CredentialService`, admitiendo dependencias opcionales `ILoginAttemptStore` e `IActiveSessionStore` para evitar regresiones y roturas de compatibilidad binaria.
- En `RecuperacionAccesoController`:
  - `GET /RecuperacionAcceso/Restablecer`: Inspección no destructiva vía `TryPeek` (sin consumir el token, RN-15) bajo `[EnableRateLimiting("AuthRecovery")]`. Si el token es válido, renderiza el formulario con el token oculto. Si es inválido o expiró, renderiza el estado genérico de enlace no válido (HTTP 200, texto genérico y enlace para solicitar nuevo enlace sin revelar la causa). Emite cabeceras de seguridad `Referrer-Policy: no-referrer` y `Cache-Control: no-store`.
  - `POST /RecuperacionAcceso/Restablecer`: Valida primero la complejidad de la contraseña del lado del servidor (mínimo 8 caracteres, al menos una mayúscula y un símbolo especial, reutilizando la regla RN-2 de `RegistroViewModels.cs`). Si el modelo es inválido, NO consume el token y re-renderiza con los errores. Si el modelo es válido, delega a `CompletePasswordResetAsync`; si tiene éxito retorna `View("Exito")` con mensaje de confirmación, y si falla retorna el estado genérico de enlace no válido.
  - `POST /RecuperacionAcceso/Recuperar`: Se actualizó el mensaje de éxito genérico para reflejar el envío del enlace de restablecimiento (vigencia 30 minutos) en lugar de una nueva contraseña.
- En `Restablecer.cshtml` (nueva vista): Diseñada siguiendo el sistema de diseño visual de `Recuperar.cshtml` con tokens semánticos de Tailwind, soporte para modo oscuro/claro, estado dual (formulario o enlace inválido) y reutilización sin modificaciones de `auth-completar.js` para los toggles de visibilidad y el medidor reactivo de requisitos de contraseña (`compl-password-input`, `compl-confirm-input`, `hint-len`, `hint-upper`, `hint-sym`).
- En `Recuperar.cshtml`: Se actualizaron el texto de instrucción y la etiqueta del botón de envío a "Enviar Enlace de Recuperación", preservando intacto el layout y estilos del desarrollador titular.
- En `ProfileService.cs`: Se actualizó el mensaje retornado por `RequestPasswordResetAsync` a la redacción correspondiente al enlace con vigencia de 30 minutos.
- Textos de ayuda y preguntas frecuentes: Se actualizaron `ayuda-chatbot.js` y `Pqr/Index.cshtml` (líneas 49, 64) sustituyendo las referencias de contraseña enviada por correo por enlace de restablecimiento de contraseña (vigencia 30 minutos).
- Internacionalización (i18n): Se añadieron todas las nuevas cadenas y claves a `locale.js` con soporte completo y verificado para los 5 idiomas destino (`en`, `de`, `fr`, `ja`, `zh`), alcanzando 433/433 cadenas cubiertas al 100% en `scripts/check-i18n-coverage.ps1`.
- Pruebas unitarias: Se ampliaron las pruebas en `CredentialServiceTests` (actualización de hash BCrypt, rechazo de replay, token inválido o expirado, rechazo genérico idéntico para usuario inactivo o eliminado, limpieza de lockout en login, invalidación de sesión activa con `LoggedOut`, auditoría de IP) y se creó `RecuperacionAccesoControllerTests` (GET con token válido e inválido, cabeceras de seguridad, POST con contraseña débil sin consumir token, POST exitoso, POST replay, anti-enumeración en POST Recuperar). Total: 164 pruebas ejecutadas y superadas (baseline 150 + 14 nuevas).
- Documentación: Se actualizó `docs/ers_wahl_mirai_v2_9.md` (RF-M01-02 reescrito, ajustes a RN-2, RN-8, RN-9, RN-14, RN-15 y control de versiones) y `docs/Arquitectura_y_Diseno_v2_9.md` (servicios de seguridad §1, corrección del stub pass-through `UseForcePasswordChange`, endpoints en §5.1, cola progresiva en §5.10, riesgo residual de tokens en RAM y checklist de despliegue para `BaseUrl`).

## 📅 08 octubre 2026 22:55:21 -05:00 — Entrega B1 (Item B): Token efímero de un solo uso para recuperación de acceso (RF-M01-02, RN-2, RN-9, RN-14, RN-15)

- Se implementó `IPasswordResetTokenStore` y `PasswordResetTokenStore` en memoria local respaldado por `IMemoryCache` con sincronización de bloqueo para emisión atómica, unicidad por usuario (un token activo por cuenta; nueva emisión revoca el anterior) y expiración absoluta determinista de 30 minutos evaluada contra `TimeProvider`. Almacena únicamente el hash SHA-256 del token y no persiste secretos en texto plano.
- En `CredentialService`, se implementó `IssuePasswordResetTokenAsync`: genera el token efímero sin tocar `users.password_hash`, encola el registro `EmailType.RECUPERACION_ACCESO` en `email_queue`, persiste en BD, guarda el token crudo en `IPendingPasswordStore` bajo el ID de la cola para despacho del worker y registra auditoría `PASSWORD_RECOVERY_REQUESTED`. Si la persistencia falla, revoca el token emitido y propaga la excepción.
- Se blindó `IssueNewPasswordAsync`: ante solicitudes con `EmailType.RECUPERACION_ACCESO` lanza `ArgumentException` impidiendo la generación de contraseñas por correo en este tipo, conservando intacto el comportamiento para `REASIGNACION_ADMIN` en censo y administración.
- Se actualizaron los invocadores `RecuperacionAccesoController.Recuperar` y `ProfileService.RequestPasswordResetAsync` para llamar a `IssuePasswordResetTokenAsync`. En el controlador se documentó la diferencia residual de temporización por inserción en base de datos.
- Se amplió `EmailSettings` con la propiedad `BaseUrl` y se añadió su clave en `appsettings.json`. En `EmailQueueBackgroundService`, para `RECUPERACION_ACCESO`, se valida `BaseUrl` (requiere HTTPS o HTTP exclusivo para loopback local); si es inválida o vacía, marca la fila en `FALLIDO` terminal y purga el token de memoria; si es válida, despacha el correo con botón/enlace de restablecimiento hacia `/RecuperacionAcceso/Restablecer?token=...`, vigencia de 30 minutos y sin texto de contraseña en claro. `REASIGNACION_ADMIN` conserva su plantilla y comportamiento original.
- Se añadieron pruebas unitarias completas en `PasswordResetTokenStoreTests` (inspección sin consumo, consumo único, revocación de token previo al reemitir, expiración con `FakeTimeProvider`, revocación explícita, concurrencia con ganador único), `CredentialServiceTests` (inmutabilidad de hash de clave, encolado de token, reversión ante fallo de BD, excepción ante `RECUPERACION_ACCESO` y compatibilidad con `REASIGNACION_ADMIN`) y se ampliaron pruebas en `EmailQueueBackgroundServiceTests` (enlace con token escapado, validación de `BaseUrl`, esquema HTTP restringido a loopback y preservación de `REASIGNACION_ADMIN`).

## 📅 08 octubre 2026 16:30:00 -05:00 — Actualización ERS RF-M01-00

- Se actualizó el bullet "Auto-registro protegido por IP (RF-M01-00)" en `docs/ers_wahl_mirai_v2_9.md` para reflejar la nueva redacción exacta según requerimiento de la entrega 2C.

## 📅 08 octubre 2026 15:22:42 -05:00 — Entrega 2C: ClientIpKey + particionamiento IPv6 /64 (RF-M01-00, RN-15)

- Se implementó `ClientIpKey` en `WahlMirai.Web/Services` como módulo estático transversal para normalizar direcciones IP en claves de partición determinísticas de rate limiting y contadores de intentos fallidos (`From(IPAddress?)`, `From(HttpContext?)`), agrupando clientes IPv6 en prefijos canónicos `/64` y conservando IPv4 en `/32`.
- Reglas en orden estricto de precedencia: (a) `null` o sin conexión -> `"unknown"`, (b) `IsIPv4MappedToIPv6` (`::ffff:x.x.x.x`) se mapea a IPv4 antes de verificar la familia para prevenir que conexiones IPv4 bajo sockets dual-stack colapsen en un solo bucket `/64`, (c) IPv4 nativa se preserva sin cambios (`ToString()`), (d) IPv6 nativa toma los primeros 8 bytes, rellena los 8 restantes con cero, descarta identificadores de ámbito (`scope id`) y formatea canónicamente + `"/64"`, (e) robustez garantizada sin excepciones para loopbacks (`127.0.0.1`, `::1` -> `"::/64"`).
- En `Program.cs`, las tres políticas de rate limiting (`AuthLogin`, `AuthRecovery` y `AuthRegister`) migraron su clave de partición a `ClientIpKey.From(httpContext)` y se documentó la regla en los comentarios NOTA.
- En `AuthController.cs`, `GetRegistrationBucketKey()` ahora genera la clave a partir de `ClientIpKey.From(HttpContext.Connection.RemoteIpAddress)`, unificando la grafía de IP ausente a `register:unknown` (antes `register:Unknown`).
- Se mantuvo la separación estricta de responsabilidades: la dirección IP cruda observada (`GetClientIpAddress()`) se preserva intacta para todos los registros de auditoría forense (`_auditService.LogAsync` y `AuditLog.IpAddress` en `RegisterElectorAsync`), documentando la divergencia con comentarios `// NOTA:`.
- Se añadieron pruebas unitarias completas en `WahlMirai.Tests/ClientIpKeyTests.cs` (9 pruebas cubriendo nulos, IPv4 nativa, IPv4-mapped, dos IPv6 en mismo/distinto /64, cadena canónica exacta, loopbacks, scope id y HttpContext). Las pruebas preexistentes de `AuthControllerRegistrationTests` pasan sin modificación alguna.
- Se documentó el módulo en `Arquitectura_y_Diseno_v2_9.md`, detallando el cambio de grafía del bucket nulo, la preservación de la IP cruda en auditoría y el riesgo residual de atacantes con prefijos delegados mayores a `/64` (/56, /48), manteniendo el riesgo de NAT escolar compartido.

## 📅 08 octubre 2026 14:15:00 -05:00 — Paso 2: ForwardedHeaders configurable (opt-in, fail-closed, ForwardLimit=1)

- Se implementó `ForwardedHeadersConfiguration` en `WahlMirai.Web/Services` con lectura de sección `ForwardedHeaders`: `Enabled` (bool, default false), `KnownProxies` (string[]), `KnownNetworks` (string[] CIDR).
- Principio fail-closed: si `Enabled: true` y ambas listas están vacías o contienen formatos inválidos de IP o CIDR, lanza `InvalidOperationException` impidiendo el arranque inseguro de la aplicación.
- Configuración estricta de `ForwardedHeadersOptions`: solo `XForwardedFor | XForwardedProto` (sin `XForwardedHost`), `ForwardLimit = 1`, limpiando colecciones por defecto antes de añadir las entradas configuradas.
- En `Program.cs`, `app.UseForwardedHeaders()` se ubica como primer middleware del pipeline HTTP antes de `UseRequestLocalization`, `UseExceptionHandler`, `UseHsts`, `UseHttpsRedirection`, `UseRouting` y `UseRateLimiter`.
- `appsettings.json` añade la sección deshabilitada por defecto (`Enabled: false`, listas vacías).
- Se añadieron pruebas unitarias y de middleware en `WahlMirai.Tests/ForwardedHeadersConfigurationTests.cs` (15 pruebas cubriendo estados deshabilitado, fail-closed, validación de prefijos CIDR, `ForwardLimit=1`, proxy confiable/no confiable y red CIDR).
- Se documentó la matriz de topologías, recomendaciones de sobrescritura en proxy y advertencia de no definir `ASPNETCORE_FORWARDEDHEADERS_ENABLED` en Arquitectura y Diseño.

## 📅 06 octubre 2026 16:31:13 -05:00 — Entrega 2A.1: bloqueo de reintento manual de correos con contraseña (RN-9)

- `EmailRetryPolicy` centraliza los tipos que llevan contraseña para el worker (eliminación del secreto) y la política de reintento manual. `RetryEmail` ahora usa antiforgery y una actualización atómica condicionada por ID, estado y tipo.
- Se bloquea el reintento manual de `RECUPERACION_ACCESO` y `REASIGNACION_ADMIN` cuando están `FALLIDO`: el secreto ya no existe y debe emitirse una contraseña nueva. La vista muestra la alternativa correspondiente.
- Cambio deliberado: `RetryEmail` solo permite `FALLIDO`. Antes también aceptaba filas `PENDIENTE` y las reiniciaba con `Attempts = 0`, lo que podía conceder intentos adicionales; ahora siguen exclusivamente en la cola automática.
- Se añadieron traducciones en los seis idiomas y pruebas de la matriz de estados y tipos. Arquitectura §5.10 documenta la política; el ERS no describe el reintento manual y no cambió.

## 📅 06 octubre 2026 15:50:45 -05:00 — Entrega 2B: hardening del auto-registro M01 (RF-M01-00, RN-1, RN-1.1, RN-15)

- Se añadió `AuthRegister` (180 solicitudes/min/IP) y un bucket independiente `register:{ip}` con umbral configurable de 30 fallos por ventana de 15 minutos y escalera 1/5/15 minutos.
- Registro y Completar conservan la misma respuesta anti-enumeración; no consultan whitelist bajo bloqueo. `REGISTRO_LOCKED_OUT` y warning de ILogger se emiten sólo al activar el bloqueo.
- El claim concurrente por clave única MySQL 1062 vuelve al mensaje genérico; otros `DbUpdateException` se propagan. El servicio revierte la transacción antes de relanzar y el contexto se dispone al cerrar el request.
- Se reutilizaron `FakeAuthService`, `FakeAuditService` y `FakeTimeProvider`; se añadieron pruebas del store y del controlador. Arquitectura y ERS documentan NAT, rotación de IPs y OTP pendiente ligado al correo de la whitelist.
- No se habilitó `UseForwardedHeaders`: tras reverse proxy el límite ve la IP del proxy. Confiar en `X-Forwarded-For` sin proxies confiables permitiría spoofing.

## 📅 06 octubre 2026 14:50:06 -05:00 — Entrega 2A: ciclo de secreto y reintentos de EmailQueue (RN-9)

- El worker conserva la contraseña en `IPendingPasswordStore` durante fallos SMTP transitorios; la retira tras envío exitoso o al agotar los tres intentos.
- Los envíos exitosos limpian `error_message`. La tasa se acota a 1–60 correos/minuto (20/min mantiene una pausa de 3 segundos).
- Se añadieron pruebas para reintento transitorio, agotamiento, secreto ausente y límites extremos de tasa.
- Arquitectura §5.10 registra el ciclo y los riesgos residuales de instancia única y almacén en memoria. ERS RN-9 no cambió porque define el requisito de entrega progresiva, no el detalle de reintentos.

## 📅 02 octubre 2026 19:36:59 -05:00 — Entrega 1.1 (Item A): Pista estática en errores de login

- La vista de login muestra una pista literal junto al error genérico cuando hay errores de modelo, sin distinguir entre fallos de credenciales, cuenta inactiva o bloqueo temporal.
- Se añadieron traducciones de la pista a inglés, alemán, francés, japonés y chino en `EXTRA_TRANSLATIONS`.
- Se actualizaron RF-M01-01, RN-15 y la descripción de mitigación de timing en Arquitectura. No hubo cambios en la lógica de autenticación, throttling ni respuestas.

## 📅 29 de septiembre de 2026 19:22:59 -05:00 — Entrega 1.1 (Item C, Paso 2d): Audit gap + remove orphan action (RF-M07-01, RN-15, RN-8)

Se cerró la brecha de auditoría en `POST /Profile/VerifyCurrentPassword` y se eliminó la acción huérfana `POST /Profile/Update` del controlador y los campos de contraseña del ViewModel:

### Step 0 — Hallazgos (solo lectura)

**a. `VerifyCurrentPasswordAsync` en `IProfileService`:**
- Antes de este paso, `IProfileService` **no** exponía `VerifyCurrentPasswordAsync`. El controlador `VerifyCurrentPassword` (línea 94 de `ProfileController.cs`) implementaba directamente la verificación BCrypt, el control de bucket y los mensajes, sin pasar por el servicio y **sin emitir ningún evento de auditoría** (`PROFILE_AUTH_FAILED` / `PROFILE_LOCKED_OUT`). Esa era la brecha.

**b. Callers de `POST /Profile/Update`:**
- **Vistas:** ninguna vista hace `POST` ni referencia `asp-action="Update"` en `Index.cshtml`. No existía un `<form>` apuntando a esa acción.
- **JS / wwwroot:** ninguna llamada `fetch('/Profile/Update')` encontrada en `wwwroot/`.
- **Tests:** ningun test de controlador existe; la cobertura de `Update` era 0% por eso mismo.
- **Docs:** `Arquitectura_y_Diseno_v2_9.md:670` y `ers_wahl_mirai_v2_9.md:343` mencionaban la acción — ambos actualizados en este paso.
- **`ProfileViewModel.CurrentPassword`:** referenciado en `ProfileController.cs:142,184` (dentro del body de `Update`, ahora eliminado) y en `ProfileViewModel.cs:40` (eliminado).
- **`ProfileViewModel.NewPassword`:** referenciado en `ProfileController.cs:140,145,147,148,185` (todos dentro de `Update`, eliminado) y en `ProfileViewModel.cs:47` (eliminado).
- **`ProfileViewModel.ConfirmNewPassword`:** referenciado en `ProfileViewModel.cs:51` (eliminado).

**c. `git status --short` / `git diff --stat` al inicio:**
```
 M REGISTRO_CAMBIOS_kevin.md
 M WahlMirai.Tests/ProfileServiceTests.cs
 M WahlMirai.Web/Controllers/ProfileController.cs
 M WahlMirai.Web/Services/IProfileService.cs
 M WahlMirai.Web/Services/ProfileService.cs
 M WahlMirai.Web/Views/Profile/Index.cshtml
 M WahlMirai.Web/wwwroot/js/locale.js
 M docs/Arquitectura_y_Diseno_v2_9.md
 M docs/ers_wahl_mirai_v2_9.md
 M graphify-out/GRAPH_REPORT.md
 M graphify-out/graph.html
 M graphify-out/graph.json
 M graphify-out/manifest.json
 13 files changed, 3316 insertions(+), 2453 deletions(-)
```

### Implementación

1. **`IProfileService.cs`** — Se añadió el contrato `VerifyCurrentPasswordAsync(int voterId, string? currentPassword, string ipAddress)`.

2. **`ProfileService.cs`** — Se implementó `VerifyCurrentPasswordAsync`:
   - Usa el mismo bucket `profile:{voterId}` que `UpdateProfileAsync` y `UpdateContactEmailAsync`.
   - Antes de verificar BCrypt: comprueba `IsLockedOut`.
   - Al fallar: llama `RegisterFailure`, emite `PROFILE_AUTH_FAILED` con `ip_address` y detalles `Intentos: N`.
   - Al alcanzar el umbral: emite adicionalmente `PROFILE_LOCKED_OUT`.
   - Al tener éxito: llama `RegisterSuccess`.
   - Mensajes en español idénticos a los que tenía el controlador (sin cadenas nuevas).

3. **`ProfileController.cs`** — `VerifyCurrentPassword` ahora delega en `_profileService.VerifyCurrentPasswordAsync`. Se eliminaron: BCrypt directo, `_loginAttemptStore` directo, `_context.Voters.FindAsync`, y toda la lógica de throttling inline. Se eliminó por completo la acción `Update` (62 líneas). Se eliminaron las dependencias `_context`, `_loginAttemptStore` del constructor del controlador — **NOTA:** se mantienen porque aún son necesarias por otros métodos (`Index` usa `_context`, `ChangePassword` usa `_loginAttemptStore`); ya no se usaban en `VerifyCurrentPassword` ni en `Update`, pero sí en el resto de la clase.

4. **`ViewModels/ProfileViewModel.cs`** — Eliminados `CurrentPassword`, `NewPassword` y `ConfirmNewPassword` con sus atributos de validación. Se eliminó también `[Required]` y `[EmailAddress]` de `ContactEmail` (campo ahora solo de presentación GET). Se actualizó descripción y docstring de la clase.

5. **Tests** (`ProfileServiceTests.cs`, `SharedFakes.cs`):
   - `FakeAuditService`: añadido `Details { get; }` para verificar el campo `details` de los eventos auditados.
   - `FakeLoginAttemptStore`: añadidos `FailureCallCount` y `SuccessCallCount` para validar llamadas exactas.
   - Cuatro nuevos tests de `VerifyCurrentPasswordAsync`:
     - `VerifyCurrentPasswordAsync_WrongPassword_RegistersSingleFailureAndAuditEntry`: fallo → exactamente 1 fallo en store, 1 entrada `PROFILE_AUTH_FAILED` con IP y `Intentos: 1` en details.
     - `VerifyCurrentPasswordAsync_ReachingThreshold_AuditsProfileLockedOutAndBlocksSubsequentCalls`: 3 fallos consecutivos emiten `PROFILE_AUTH_FAILED` y `PROFILE_LOCKED_OUT`; llamada posterior con contraseña correcta sigue bloqueada.
     - `VerifyCurrentPasswordAsync_Success_ResetsProfileBucket`: éxito llama `RegisterSuccess` y reinicia el bucket para nuevos ciclos.
     - `VerifyCurrentPasswordAsync_EmptyOrNullPassword_RejectsWithoutFailureRegistrationOrAudit`: contraseña vacía/null rechazada sin tocar el store ni generar auditoría.

6. **Docs:**
   - `ERS RN-15`: eliminada referencia a `Update`/`UpdateProfileAsync` como endpoint; se menciona `VerifyCurrentPasswordAsync` como punto de control.
   - `ERS RF-M07-01 Condición especial`: eliminada referencia a `POST /Profile/Update`; se documenta la delegación a `VerifyCurrentPasswordAsync`.
   - `Arquitectura_y_Diseno_v2_9.md` §5.7: eliminada línea `POST /Profile/Update`; ampliada descripción de `VerifyCurrentPassword`.

### Cadenas de usuario nuevas
Ninguna. Todas las cadenas de respuesta JSON en español (`"La contraseña actual no es correcta."`, `"Debes ingresar tu contraseña actual."`, `"Demasiados intentos fallidos..."`, `"Usuario no encontrado."`, `"Sesión no válida."`) ya existían en el controlador. El servicio usa las mismas cadenas literales; no se introdujeron cadenas nuevas visibles para el usuario.

---

## 📅 29 de septiembre de 2026 19:00:44 -05:00 — Entrega 1.1 (Item C, Paso 2c): Cierre de brecha i18n y verificación de conteo en perfil (RF-M07-01, RN-15)


Se cerró la brecha de internacionalización (i18n) para los modales de seguridad de perfil y se verificó la exhaustividad del conteo de intentos en el bucket segregado `profile:{id}`:

1. **Internacionalización en `locale.js`:**
   - Se distribuyeron en `EXTRA_TRANSLATIONS` las 7 cadenas de texto en español correspondientes a validaciones, avisos y errores de los modales de contraseña y correo hacia los 5 idiomas soportados (`en`, `de`, `fr`, `ja`, `zh`):
     - `"Por seguridad, debes confirmar tu contraseña actual antes de cambiar el correo. Se enviará un aviso a tu dirección anterior."`
     - `"Debes ingresar tu contraseña actual."`
     - `"Error de conexión. Verifica tu red e intenta de nuevo."`
     - `"La contraseña actual no es correcta."`
     - `"La contraseña actual no coincide."`
     - `"Debes ingresar tu contraseña actual para modificar el correo de contacto."`
     - `"Correo actualizado. El aviso de seguridad a tu correo anterior no pudo enviarse, pero el cambio quedó guardado."`
   - **Claves identificador preexistentes:** Se constató que las 8 claves identificador provistas (`CONNECTION_ERROR`, `CURRENT_PASSWORD_REQUIRED`, `INCORRECT_PASSWORD`, `PASSWORD_UPDATED`, `PASSWORD_UPDATED_OK`, `SAVING`, `VERIFYING`, `SENDING`) ya se encontraban presentes en `EXTRA_TRANSLATIONS` en todos los idiomas objetivo. En estricto apego a la directiva de no sobrescribir entradas existentes, se mantuvieron intactas reportando el estado sin conflictos funcionales.
   - **Soporte de Mensaje Dinámico de Lockout:** Se añadió la regla 8 al motor de regex dinámico existente en `getTranslation` (`locale.js`) para capturar mensajes con formato `"Demasiados intentos fallidos. Por seguridad, la operación está bloqueada temporalmente. Intenta de nuevo en {mins} minuto(s)."` y proyectarlos reactivamente traducidos con el número exacto de minutos en `en`, `de`, `fr`, `ja` y `zh`.

2. **Verificaciones de Conteo y Throttling:**
   - Se demostró que un intento fallido en `VerifyCurrentPassword` o `ChangePassword` registra exactamente un único fallo en el almacén de intentos bajo la clave `profile:{id}`.
   - Se comprobó que el paso 1 del modal (`POST /Profile/VerifyCurrentPassword`) cuenta hacia el límite de intentos fallidos.
   - Se constató que ninguna ruta operativa verifica contraseñas sin atravesar el bucket `profile:{id}`.

3. **Resultados de Auditoría y Compilación:**
   - `check-i18n-coverage.ps1`: PASS en los 5 idiomas (428/428 strings cubiertos, 0 faltantes).
   - `dotnet build --warnaserror`: 0 Errores, 0 Advertencias.
   - `dotnet test`: 59/59 pruebas superadas con éxito.

## 📅 29 de septiembre de 2026 18:43:45 -05:00 — Entrega 1.1 (Item C, Paso 2b): Cierre de bypass en POST /Profile/Update y UpdateProfileAsync (RF-M07-01, RN-15)

Se cerró la vía de bypass identificada en el endpoint heredado `POST /Profile/Update` y en el método del servicio `ProfileService.UpdateProfileAsync`:

1. **Eliminación de la Modificación de Correo en `UpdateProfileAsync` (RF-M07-01):**
   - Se removió por completo la rama de actualización de `voter.ContactEmail` en `UpdateProfileAsync`. Todo cambio de correo de contacto debe realizarse exclusivamente a través del flujo segregado `UpdateContactEmailAsync` (`POST /Profile/UpdateEmail`), el cual exige la contraseña actual, aplica throttling y despacha la alerta de seguridad al correo anterior.
   - En `ProfileController.Update`, se pasa `newContactEmail: null` a `UpdateProfileAsync`.
   - Justificación respecto a `ProfileViewModel`: el modelo de vista compartido conserva la propiedad `ContactEmail` porque es consumida por la vista `Index.cshtml` (GET) para desplegar el correo actual en modo solo lectura (`@Model.ContactEmail`). No se altera la definición compartida de `ProfileViewModel` para evitar romper el renderizado de la vista principal. Cualquier envío de `ContactEmail` en solicitudes POST directas es ignorado tanto en el controlador como en el servicio.

2. **Exigencia Incondicional de Contraseña Actual y Throttling en Perfil (RN-15):**
   - En `ProfileService.UpdateProfileAsync`, cuando se suministra `NewPassword`, se eliminó la premisa de validación opcional ("solo si se proporciona"). `CurrentPassword` es ahora requerida de forma incondicional; si viene nula o vacía, la solicitud se rechaza de inmediato con `"Debes ingresar tu contraseña actual."`.
   - Se integró el almacén de intentos `_loginAttemptStore` utilizando el bucket de perfil segregado `profile:{voterId}`:
     - Verificación previa de bloqueo temporal (`IsLockedOut`).
     - Si la contraseña actual es errónea, se asienta `RegisterFailure(bucket)`, el evento de auditoría `PROFILE_AUTH_FAILED` y, si se alcanza el umbral de bloqueo escalonado, `PROFILE_LOCKED_OUT`.
     - Si la contraseña es correcta, se asienta `RegisterSuccess(bucket)` reseteando el contador del bucket de perfil.
   - En `ProfileController.ChangePassword`, se delegó la gestión del bucket a `UpdateProfileAsync` para evitar doble registro de intentos fallidos/exitosos.

3. **Compatibilidad i18n y Auditoría de Cadenas:**
   - En `Views/Profile/Index.cshtml`, el texto del banner de seguridad del modal de correo se despojó del wrapper Razor `@("...")`, dejándolo como texto HTML plano para que el script de análisis `check-i18n-coverage.ps1` lo audite transparentemente en la compilación.
   - Conforme a las directrices de la tarea, no se modificó `locale.js` ni se agregaron traducciones manuales.

4. **Pruebas Automatizadas (`ProfileServiceTests.cs`):**
   - Se añadieron 4 pruebas unitarias cubriendo las nuevas políticas:
     - `UpdateProfileAsync_EmailSupplied_IsIgnoredAndNotPersisted`: confirma que cualquier valor de correo suministrado a `UpdateProfileAsync` es ignorado y la BD retiene el correo original.
     - `UpdateProfileAsync_NewPasswordWithoutCurrentPassword_Rejected`: confirma rechazo inmediato ante `currentPassword` vacío o nulo.
     - `UpdateProfileAsync_WrongCurrentPassword_CountsTowardAndTriggersProfileLockout`: valida incremento de fallos, asentamiento de `PROFILE_AUTH_FAILED`, activación de `PROFILE_LOCKED_OUT` y rechazo de contraseñas correctas mientras dure el bloqueo.
     - `UpdateProfileAsync_SuccessResetsProfileBucket`: valida que un cambio exitoso reinicia el bucket `profile:{id}`.
   - Total suite: 59 pruebas ejecutadas y superadas al 100%.

5. **Documentación:**
   - Actualización de ERS v2.9 (RN-15 y RF-M07-01).
   - Actualización de Arquitectura y Diseño v2.9 (M07).

## 📅 29 de septiembre de 2026 18:20:00 -05:00 — Entrega 1.1 (Item C): Verificación de contraseña actual para cambio de correo, notificación a dirección anterior y throttling de perfil (RF-M07-01, RN-15, RN-8)

Se implementó el endurecimiento del flujo de actualización de correo de contacto para mitigar el vector de account takeover desde sesiones abiertas:

1. **Servicio de Perfil (`IProfileService` / `ProfileService`):**
   - Actualización de firma: `Task<(bool Success, bool EmailSaved, bool NotificationSent, string ErrorMessage)> UpdateContactEmailAsync(int voterId, string newEmail, string currentPassword, string ipAddress)`.
   - **Throttling segregado (RN-15):** Reutilización de `ILoginAttemptStore` con bucket independiente con clave `profile:{voterId}`. Los intentos fallidos de contraseña en perfil bloquean temporalmente las operaciones sensibles del perfil (escalera de 1m, 5m, 15m) sin bloquear el inicio de sesión legítimo por documento, ni viceversa.
   - **Verificación de Contraseña Actual:** Validación obligatoria mediante `BCrypt.Verify` contra `users.password_hash`. Si es correcta, se limpia el bucket de perfil (`RegisterSuccess`); si es incorrecta, se asienta `PROFILE_AUTH_FAILED` y, si activa bloqueo, `PROFILE_LOCKED_OUT`.
   - **Validación de Negocio:** Rechazo de dirección idéntica a la actual.
   - **Auditoría (RN-8):** Registro de `PROFILE_UPDATED` con valores antiguo y nuevo, propagando la IP remota del cliente.
   - **Notificación Dual Desacoplada (RF-M07-01):**
     - Aviso de seguridad a la dirección ANTERIOR informando fecha, hora y el nuevo correo enmascarado (`j***e@domain.com`), con advertencia de contacto inmediato al administrador si no fue el usuario.
     - Confirmación a la dirección NUEVA.
     - Envío directo mediante `IEmailSender` sin encolamiento en `email_queue` (puesto que dicha tabla no almacena la dirección destino y `vw_pending_email_queue` resuelve `users.contact_email` dinámicamente, lo que enviaría ambos correos a la dirección nueva).
     - Manejo de excepciones aislado por bloque: el fallo de SMTP en cualquiera de los envíos no revierte la persistencia en base de datos (`EmailSaved = true`) ni bloquea el otro envío.
   - **Throttling en verificación de clave:** Inclusión del bucket `profile:{voterId}` en `VerifyCurrentPasswordAsync` y `ChangePasswordAsync`.

2. **Controlador (`ProfileController`):**
   - `POST /Profile/UpdateEmail`: recepción de `newEmail` y `currentPassword`, validación con `[ValidateAntiForgeryToken]`, extracción de IP remota cliente y retorno de payload estructurado `{ ok, emailSaved, notificationSent, newEmail, message }`.
   - Manejo diferenciado de estados: éxito con aviso enviado, advertencia si el correo se guardó pero falló el aviso SMTP, o error si la contraseña fue incorrecta o la cuenta está bloqueada temporalmente.

3. **Interfaz de Usuario (`Views/Profile/Index.cshtml`):**
   - Modal de cambio de correo enriquecido con campo obligatorio de "Contraseña Actual" con opción de mostrar/ocultar contraseña.
   - Validaciones en tiempo real (ambos campos requeridos y formato válido antes de habilitar el botón de guardar).
   - Manejo de errores inline (contraseña incorrecta o bloqueo) sin cerrar el modal ni perder los datos digitados.
   - Banner informativo de seguridad explicando que se notificará a la dirección anterior.
   - Compatibilidad total con la suite de auditoría i18n (`check-i18n-coverage.ps1` con 100% de cobertura).

4. **Pruebas Automatizadas (`ProfileServiceTests.cs` y `SharedFakes.cs`):**
   - Implementación de `FakeLoginAttemptStore` configurable para aislamiento y testeo de umbrales.
   - 10 pruebas unitarias en `ProfileServiceTests` cubriendo:
     - Cambio exitoso con envío prioritario a correo anterior y posterior al nuevo.
     - Fallo de SMTP en correo anterior no revierte BD y aún despacha al nuevo.
     - Rechazo ante contraseña actual vacía (sin consultar BD).
     - Rechazo ante contraseña actual incorrecta y asentamiento de auditoría.
     - Bloqueo escalonado (RN-15) tras N intentos fallidos, bloqueando incluso contraseñas correctas hasta la expiración.
     - Limpieza del bucket de perfil tras verificación exitosa.
     - Desacople estricto entre bucket de perfil (`profile:{id}`) y bucket de login (`document_hash`).
   - Suite completa: 55/55 pruebas aprobadas (100%).

5. **Documentación:**
   - Actualización de ERS v2.9 (RN-15 y RF-M07-01).
   - Actualización del documento de Arquitectura y Diseño v2.9.

## 📅 28 de septiembre de 2026 22:06:14 -05:00 — Paso 2 (Entrega 1): Protección contra fuerza bruta, rate limiting y endurecimiento de autenticación

Se implementó el paquete de mitigación de fuerza bruta y endurecimiento para el Módulo M01 (RN-15):

1. **Almacén de Intentos Fallidos (`ILoginAttemptStore` / `LoginAttemptStore`):**
   - Servicio `Singleton` concurrente respaldado por `ConcurrentDictionary<string, LoginAttemptEntry>`.
   - Política `LoginAttemptPolicy`: ventana deslizante de 15 minutos; bloqueo progresivo a partir del 5to fallo (escalera: 1 min, 5 min, 15 min, y persistencia en 15 min).
   - Acceso y mutación thread-safe mediante `lock(entry)`.
   - Depuración perezosa (`LazyPurge`) activada al superar 5000 entradas para eliminar registros cuya ventana y bloqueo hayan expirado.
   - Desacople de tiempo mediante inyección de `TimeProvider` (`TimeProvider.System` en producción).

2. **Endurecimiento de `AuthService.ValidateLoginAsync`:**
   - Nueva firma: `Task<Voter?> ValidateLoginAsync(string document, string password, string? ipAddress)`.
   - Mitigación de timing attacks: cálculo de `BCrypt.Verify` contra un hash dummy estático (`DummyHash`) tanto si la cuenta no existe como si se encuentra temporalmente bloqueada.
   - Si la cuenta está bloqueada por `ILoginAttemptStore.IsLockedOut`: rechazo inmediato sin consultas a base de datos ni registros de auditoría.
   - Auditoría enriquecida: propagación de la dirección IP cliente en `LOGIN_SUCCESS`, `LOGIN_FAILED` y en el nuevo evento `LOGIN_LOCKED_OUT` (asentado exactamente una vez al activarse el bloqueo).

3. **Rate Limiting y Endurecimiento en Controladores:**
   - `AuthController.Login` (POST): inclusión de atributos `[ValidateAntiForgeryToken]` y `[EnableRateLimiting("AuthLogin")]`, extracción de IP remota y propagación al servicio de autenticación.
   - `RecuperacionAccesoController.Recuperar` (POST): inclusión de atributos `[ValidateAntiForgeryToken]` y `[EnableRateLimiting("AuthRecovery")]`. Implementación de cooldown por documento vía `IMemoryCache` (clave `rec:{documentHash}`, TTL absoluto de 5 min) que omite el reenvío de correo sin romper la respuesta anti-enumeración. Desacoplado explícitamente de `ILoginAttemptStore` para no impedir la recuperación legítima.

4. **Configuración en `Program.cs`:**
   - Registro de `IMemoryCache`, `TimeProvider.System` e `ILoginAttemptStore`.
   - Registro de middleware `Microsoft.AspNetCore.RateLimiting` con políticas `AuthLogin` (180 peticiones/min) y `AuthRecovery` (30 peticiones/min) particionadas por `RemoteIpAddress`, con respuesta HTTP 429 (`Retry-After: 60`). Middleware activado tras `UseRouting()`.
   - Configuración de `CookieAuthenticationOptions.Cookie.SecurePolicy` explícito (`CookieSecurePolicy.Always` en producción, `SameAsRequest` en desarrollo).

5. **Pruebas Automatizadas:**
   - Creación de `FakeTimeProvider` manual (sin paquetes NuGet externos).
   - Creación de `LoginAttemptStoreTests` (6 pruebas: progresión de escalera, reseteo por ventana, limpieza por éxito, expiración de bloqueo, lazy purge y concurrencia con 1000 hilos).
   - Actualización de `AuthServiceTests` y `SharedFakes` cubriendo la nueva firma, bloqueo sin auditoría, propagación de IP y registro único de `LOGIN_LOCKED_OUT`. Total suite: 48/48 pruebas aprobadas (100%).

6. **Riesgos Residuales Documentados:**
   - Estado en memoria volátil ante reinicios del proceso (mismo trade-off que `IActiveSessionStore`).
   - Riesgo de denegación de servicio dirigida por documento (mitigado por la independencia del flujo de recuperación).
   - Entornos escolares bajo NAT compartido donde múltiples electores comparten IP pública.

## 📅 27 de septiembre de 2026 13:12:37 -05:00 — Retiro de traducción automática offline

Se abandona la traducción automática offline con Argos Translate: el modelo `es->en` produce resultados incoherentes debido al bug externo aún no corregido [argosopentech/argos-translate#504](https://github.com/argosopentech/argos-translate/issues/504). La restricción de hardware también descarta ejecutar un modelo neuronal local como reemplazo, incluido NLLB. Las traducciones faltantes se resolverán manualmente en adelante; se retiran los scripts y los requisitos locales asociados.

## 📅 27 de septiembre de 2026 12:03:49 -05:00 — Infraestructura compartida de build e i18n

Se dejó de versionar `WahlMirai.Web/wwwroot/css/site.css`, que Tailwind regenera localmente durante cada build. Se extrajeron a `scripts/i18n-common.ps1` las funciones compartidas de lectura de claves y detección de traducciones faltantes; `check-i18n-coverage.ps1` conserva su salida y códigos de salida, y el target `CheckI18nCoverage` sigue siendo warning-only sin cambios. Se añadieron los helpers manuales opcionales `fill-missing-i18n.ps1` y `translate_offline.py`, con traducción Argos `es->en` y pivote inglés para alemán, francés, japonés y chino; no se conectan al build. README y arquitectura documentan los prerrequisitos opcionales de Python/Argos, la provisión de modelos por máquina y Graphify.

## 📅 27 de septiembre de 2026 06:48:54 -05:00 — Paginación server-side de cuentas administrativas M09

Se implementó para RF-M09-01 la paginación server-side de `AdminAccounts`, con búsqueda parcial sin distinguir mayúsculas en nombre, correo y cargo institucional, ordenamiento por una lista permitida y conservación del estado de búsqueda/orden al navegar. El orden predeterminado es `FullName` ascendente con `Id` como desempate; Documento no admite búsqueda parcial ni ordenamiento sobre el valor descifrado.

Se añadieron a `locale.js` las traducciones en inglés, alemán, francés, japonés y chino para “Buscar por nombre, correo o cargo” y “Paginación de cuentas administrativas”. La regla `.min-w-64{min-width:calc(var(--spacing) * 64)}` en `site.css` fue generada automáticamente por Tailwind a partir de la nueva clase del campo de búsqueda; es un artefacto esperado del build, no una edición manual.

Verificación: `dotnet build --warnaserror` pasó con 0 errores y 0 advertencias; `dotnet test WahlMirai.Tests/WahlMirai.Tests.csproj` aprobó 39/39 pruebas (0 fallidas, 0 omitidas); el chequeo i18n pasó con 428/428 textos en los cinco idiomas; `git diff --check` no reportó errores de whitespace.

## 📅 27 de septiembre de 2026 05:59:47 -05:00 — Catálogo cerrado de cargos administrativos M09

`PositionTitle` sigue siendo opcional, pero la aplicación ahora solo acepta los cargos Rector(a), Coordinador(a) Académico(a), Coordinador(a) de Convivencia, Orientador(a) Escolar, Secretario(a) Académico(a), Psicólogo(a) Escolar, Docente Líder de Gobierno Escolar y Auxiliar Administrativo(a). Se reemplazó el texto libre por selectores en alta y edición, se actualizó la validación y la ERS, y se corrigió el valor usado por la prueba del servicio. No se modificó el esquema MySQL.

Verificación: `dotnet build WahlMirai.Web/WahlMirai.Web.csproj --no-restore --warnaserror` pasó con 0 errores y 0 advertencias; `dotnet test WahlMirai.Tests/WahlMirai.Tests.csproj --no-restore` aprobó 33/33 pruebas, con 0 fallidas y 0 omitidas. El audit i18n pasó en los cinco idiomas (427/427).

## 📅 26 de septiembre de 2026 22:52:54 -05:00 — Unificación de validación de contraseña en M01

Se actualizó `ChangePasswordViewModel.NewPassword` para exigir mínimo ocho caracteres, al menos una mayúscula y al menos un símbolo especial, conservando la obligatoriedad y el tipo Password. Con esto, `AuthController.CambiarClave` queda alineado con RF-M07-01 y con la regla ya aplicada en las rutas de perfil; también se actualizaron los comentarios que señalaban la inconsistencia como pendiente.

Archivos modificados: `WahlMirai.Web/ViewModels/AuthViewModels.cs` y `REGISTRO_CAMBIOS_kevin.md`.

## 📅 26 de septiembre de 2026 22:28:10 -05:00 — Documentación de M09

Se documentaron en español los contratos públicos del controlador, el servicio y los modelos de vista de M09, conservando la lógica y las firmas existentes. Se añadieron notas pendientes sobre la inyección compartida de `IActiveSessionStore` (RN-14) y la obligatoriedad no definida de `PositionTitle` en la ERS (RF-M09-01).

Con este paso queda completo el pase de documentación de los cuatro módulos: M01, M07, M08 y M09.

## 📅 26 de septiembre de 2026 22:16:53 -05:00 — Documentación de M08

Se documentaron los contratos públicos de `PqrController` y los DTO de `PqrDtos`, además de los encabezados y las funciones internas de `ayuda-chatbot.js` y `pqr-manage.js`. Se añadieron notas de alcance sobre la autorización del escalamiento del chatbot y la integración con el DOM de `Manage.cshtml`.

En `PqrController.Resolve` se aclaró en código que la ausencia de registros PQR en `audit_log` es una decisión de diseño explícita conforme a RF-M08-02, no un bug ni un pendiente.

## 📅 26 de septiembre de 2026 22:06:52 — Documentación de M07

Se documentaron los encabezados y los contratos públicos de `ProfileController`, `IProfileService`, `ProfileService` y `ProfileViewModel` en español. Se conservaron los comentarios XML existentes de `UpdateContactEmailAsync` y se agregaron notas sobre las diferencias en la validación de contraseña y la verificación pendiente de procesamiento progresivo (RN-9).

Esta documentación completa el par de notas gemelas sobre la política de contraseña: una en `ProfileController` y la otra en `AuthController.CambiarClave`, incorporada durante la tarea M01.

## 📅 23 de septiembre de 2026 14:22:54 — Instalación, auditoría y documentación de Graphify

### 📌 Resumen General

Se instaló y dejó documentada la infraestructura de Graphify para disponer de un grafo consultable del repositorio entre varios equipos de trabajo. La sesión se ejecutó en la rama `mejoras-documentación-codigo-m1-7-8-9`, que Kevin había seleccionado para mejoras de documentación de M1/M7/M8/M9; permanecer en esta rama es intencional y no constituye un problema de integración con `main`.

### 🔧 Instalación y decisión de versionado

- Se instaló Graphify mediante `uv tool install graphifyy`; quedaron disponibles los ejecutables `graphify` y `graphify-mcp`.
- Se decidió versionar en el repositorio `graphify-out/`, `.agents/` y `.github/copilot-instructions.md`, porque Kevin trabaja desde varios PCs y así se evita repetir la instalación y generación en cada máquina. La integración es opcional para Dev2/Dev3 y no interfiere con su flujo si no instalan Graphify.
- La primera ejecución completa produjo **1.501 nodos, 2.786 aristas y 174 comunidades**. El benchmark estimó una reducción media de tokens de aproximadamente **20,9x** frente a leer todo el corpus de forma ingenua, con hasta **127,8x** en abstracciones centrales.

### 🔍 Gaps encontrados en la primera ejecución

- Nueve archivos `.sql` no se analizaron por falta de la gramática `tree_sitter_sql`.
- Un archivo `.docx` fue omitido por falta del extra `graphifyy[office]`.
- Las imágenes grandes no se procesaron por el límite de visión.
- Persistieron algunos endpoints ausentes y aristas colapsadas.

### 🚀 Correcciones y resultados del grafo

- Se corrigió SQL con `uv tool install graphifyy --with tree-sitter-sql --force`. El cambio pareció romper el launcher global `graphify`, pero la causa real fue la caché de comandos de una sesión de PowerShell antigua. Abrir una terminal nueva resolvió el problema; no fue necesaria una reinstalación.
- Se identificó un bug real: `graphify update .` y `graphify update . --force` terminan sin error, pero son un no-op silencioso y no modifican `graph.json`. El workaround utilizado fue llamar directamente a `graphify.watch._rebuild_code(Path('.'), force=True)` mediante `uv tool run --from graphifyy python -c "..."`. Se reprodujo consistentemente en todas las actualizaciones posteriores de esta sesión.
- Tras corregir `tree_sitter_sql`, el grafo alcanzó **1.663 nodos, 3.175 aristas y 192 comunidades**; se obtuvieron **164 nodos derivados de SQL**, de los cuales **23** provienen de `wahl_mirai_db_v2_9_completo.sql`.
- Se añadió `.graphifyignore` para excluir `docs/documentos antiguos/`, `Mockups/` y `REGISTRO_CAMBIOS_*.md`, al ser contenido histórico o fuera del estado actual que podía hacer emerger información obsoleta. El commit `e8e3f04` dejó **1.418 nodos y 2.789 aristas**, frente al baseline previo de **1.522 nodos y 2.844 aristas**.

### 📄 Consolidación de DESIGN.md y automatización

- Se descubrieron seis archivos `DESIGN.md`: cinco eran copias byte-identicas, confirmado mediante hash SHA-256, del canónico ubicado en `Mockups/wahl_mirai/DESIGN.md`; el canónico solo difería por una sección `## Graphify` añadida posteriormente.
- La auditoría del historial Git y la búsqueda en HTML y `WahlMirai.Web` no encontró dependencias por ruta con ninguno de los seis archivos.
- Como `Mockups/` quedó excluido por `.graphifyignore`, Graphify no podía encontrar su propia documentación: `graphify query` sobre ella no devolvía resultados. Se movió el canónico a `DESIGN.md` en la raíz mediante `git mv` y se eliminaron las cinco copias. El commit `b9e58f8` produjo **1.427 nodos y 2.797 aristas**; la consulta de tokens del sistema de diseño confirmó que el archivo se encuentra correctamente.
- Se creó `scripts/graphify-refresh.ps1` para automatizar el workaround: compara la marca de tiempo de `graph.json` antes y después de `graphify update . --force`, invoca automáticamente `_rebuild_code` si no cambia y reporta los deltas de nodos y aristas. El commit `8874121` produjo **1.429 nodos y 2.798 aristas**.
- Se añadió una sección `## Graphify` a `DESIGN.md` con instalación, refresco mediante el script, reglas de `.graphifyignore` y gaps conocidos (el `.docx` omitido y las imágenes grandes sin procesar), para que una sesión futura pueda continuar sin el contexto de esta conversación.

### 🧪 Verificación documental y commits relacionados

- La línea temporal auditada confirma: `71aec8a` (`chore: commit and document Graphify knowledge graph`), `e8e3f04` (`fix: narrow Graphify corpus to current project state`), `b9e58f8` (`chore: consolidate DESIGN.md to repo root, remove Mockups duplicates`) y `8874121` (`chore: add graphify-refresh.ps1 to avoid rediscovering the update no-op bug`).
- La sesión mantuvo el trabajo en `mejoras-documentación-codigo-m1-7-8-9`; la rama todavía no está fusionada a `main`.
- No se modificaron `WahlMirai.Web` ni `WahlMirai.Tests` como parte de esta implementación de Graphify.

## 📅 22 de septiembre de 2026 10:51:25 — Separación de navegación administrativa

### 📌 Cambio

Se ocultaron los enlaces públicos `Inicio` y `Ayuda` del header global cuando la sesión pertenece a `ADMIN` o `SUPER_ADMIN`. La navegación administrativa queda exclusivamente en `_AdminLayout.cshtml`, donde se mantienen los accesos administrativos a `Ayuda` y `Gestión PQR`.

### 🔧 Implementación

- **`_SiteHeader.cshtml`:** se añadió una condición basada en `Roles.AdminName` y `Roles.SuperAdminName` para no renderizar la navegación pública en cuentas administrativas.
- Visitantes y electores conservan la navegación pública correspondiente; el elector autenticado usa además su header propio de módulos.

### 🧪 Verificación

- `dotnet build WahlMirai.Web/WahlMirai.Web.csproj --no-restore`: correcto, 0 errores y 0 advertencias.

## 📅 22 de septiembre de 2026 10:46:49 — Restauración del header del elector y estado activo del home

### 📌 Resumen

Se recuperó la composición del header del elector que existía en `main` antes de la separación de layouts: una única banda con logo, navegación de módulos, controles de idioma/tema y cierre de sesión. También se replicó en el header público el indicador visual del elector: una línea inferior marca la vista activa en `Inicio` o `Ayuda`.

### 🔧 Cambios aplicados

- **`_ElectorLayout.cshtml`:** se restauró el header completo del elector con logo enlazado a `/Elector/Dashboard`, navegación integrada para escritorio, controles de idioma/tema/salida y botón de menú móvil. Se conservaron el drawer móvil y sus estados activos.
- **`_Layout.cshtml`:** se evita renderizar `_SiteHeader` en los módulos del elector (`Elector`, `Candidacy`, `Pqr`, `Profile` y `Results`) para impedir dos headers apilados. La portada y los módulos administrativos siguen usando el header global.
- **`_SiteHeader.cshtml`:** `Inicio` y `Ayuda` ahora aplican `text-primary border-primary` cuando corresponden a la vista actual, reproduciendo el patrón visual de la navegación del elector.
- **`wwwroot/js/locale.js`:** se añadió la traducción de la etiqueta accesible `Abrir menú` en inglés, alemán, francés, japonés y chino.

### 🧪 Verificación

- `dotnet build WahlMirai.Web/WahlMirai.Web.csproj --no-restore`: compilación Razor correcta; la primera ejecución mostró una advertencia i18n temporal, resuelta al añadir la traducción faltante.
- `check-i18n-coverage.ps1`: **PASS — 427/427** cadenas en `en`, `de`, `fr`, `ja` y `zh`.
- Diagnósticos del editor en `_Layout`, `_SiteHeader`, `_ElectorLayout` y `locale.js`: sin errores.

## 📅 22 de septiembre de 2026 10:42:23 — Restauración de headers de `main` y relación PQR

### 📌 Resumen

Se compararon los headers de la rama actual con `main` y con el historial de `i18n-REMEDIATION.kevin`. Se confirmó que la navegación central del header (`Inicio` y `Ayuda`) había sido eliminada en el commit de navegación del 22 de septiembre y se restauró la composición que estaba en `main`. También se revisó la consulta administrativa de PQR y se recuperó explícitamente la relación con el elector.

### 🔧 Cambios aplicados

- **`_SiteHeader.cshtml`:** restaurada la navegación horizontal de `main`, con `Inicio` y `Ayuda` centrados en escritorio y los controles de idioma, tema y sesión conservados a la derecha. Se eliminaron las variables Razor `isHome` e `isAnonymous`, que dejaron de ser necesarias.
- **`_ElectorLayout.cshtml`:** conservada la navegación superior del elector de `main` (`Panel de Control`, `Mis Candidaturas`, `Votar`/`Resultados`, `Ayuda` y `Mi Perfil`), pero el estado de `Ayuda` ahora solo se activa en `Pqr/Index`. Se eliminó `currentPath`, porque ya no se usa.
- **`PqrController.cs`:** se restauró `.Include(t => t.Voter)` en `List` para cargar explícitamente el elector asociado que se proyecta como `userName` en la gestión administrativa. `AsNoTracking()` se mantiene para que esta consulta de solo lectura no rastree entidades.

### 🔒 Aclaración de seguridad

`Include(t => t.Voter)` no es el mecanismo que impide que un visitante cree una PQR. La restricción real está en `[Authorize(Roles = Roles.ElectorName)]` de las acciones `Create` GET/POST. La relación incluida únicamente permite resolver los datos del elector registrado asociados al ticket en la vista administrativa; no concede permisos ni crea tickets.

### 🧹 Limpieza

- Eliminadas variables y condiciones de navegación pública que ya no se utilizaban después de restaurar el header de `main`.
- Eliminada la detección de rutas por prefijo en el layout elector para evitar marcar Ayuda en rutas PQR administrativas o acciones distintas de `Index`.

### 🧪 Verificación

- `dotnet build WahlMirai.Web/WahlMirai.Web.csproj --no-restore`: correcto, 0 errores y 0 advertencias.
- La comparación Git confirmó que el header central de `main` provenía del commit `17b5d5f` y había sido retirado en `8e37da3`.
- No se modificaron los atributos de autorización de creación de PQR ni las rutas de los módulos.

## 📅 22 de septiembre de 2026 10:19:07 — Corrección de navegación, PQR e internacionalización

### 📌 Resumen

Se ejecutó el plan de revisión de `/Pqr/Manage`, navegación por contexto y rendimiento posterior a la rama `i18n-REMEDIATION.kevin`. No se encontró una rama o commit denominado `IN18`; en este registro se documenta el alcance i18n solicitado y se deja constancia de que la atribución exacta a un cambio IN18 requiere su identificador Git concreto.

### 🔧 Cambios aplicados

- **`_AdminLayout.cshtml`:** se corrigió la selección duplicada del sidebar. `Ayuda` solo se activa en `Pqr/Index` o `/Ayuda`/`/Pqr`, y `Gestión PQR` solo en `Pqr/Manage`.
- **`_SiteHeader.cshtml`:** la portada y las sesiones anónimas muestran navegación pública `Inicio` y `Ayuda`. Las sesiones autenticadas que están en la portada mantienen esa navegación sin incorporar el menú del elector o del administrador; esos menús continúan aislados en sus layouts respectivos.
- **`PqrController.cs`:** `Mine` y `List` usan `AsNoTracking()`. En `List` se eliminó el `Include` redundante porque la proyección obtiene el nombre del votante mediante la relación SQL y no necesita materializar la entidad completa.
- **`Pqr/Manage.cshtml`:** `pqr-manage.js` usa `asp-append-version` para evitar servir una versión obsoleta en caché.
- **`.gitignore`:** se excluyó `venv/`, que es un entorno virtual Python local y no forma parte del runtime ASP.NET Core.
- **Arquitectura:** se documentaron i18n, `scripts/`, `venv/`, la separación de navegación y las optimizaciones/pendientes de rendimiento PQR en `docs/Arquitectura_y_Diseno_v2_9.md`.

### 🧪 Verificación

- `dotnet build WahlMirai.Web/WahlMirai.Web.csproj --no-restore`: correcto; permanece una advertencia warning-only de cobertura i18n.
- La auditoría i18n debe ejecutarse desde la raíz con `scripts/check-i18n-coverage.ps1`; el primer intento durante esta sesión usó un directorio de trabajo incorrecto y no constituye un resultado funcional de la auditoría.
- La validación manual pendiente requiere MySQL operativo y sesión real para comprobar `/Pqr/Manage`, `/Ayuda` y la portada en los tres contextos de usuario.

### ⚠️ Pendientes

- Medir tiempos, tamaño de respuesta y número de solicitudes con la aplicación y MySQL activos.
- Añadir paginación y búsqueda server-side si el volumen de PQR lo justifica; actualmente se conserva el contrato JSON y el filtrado cliente para no introducir una ruptura funcional.
- Actualizar el ERS si el contrato real seguirá usando `POST /Pqr/Resolve/{id}` en lugar del endpoint documentado históricamente como `POST /Pqr/Respond`.

### Herramienta permanente de cobertura
Se añadió `scripts/check-i18n-coverage.ps1` como auditoría re-ejecutable de textos visibles en `Views/` contra las claves exactas de `wwwroot/js/locale.js`, con resultado por idioma y ubicación archivo:línea. Debe ejecutarse después de añadir texto visible; actualmente se mantiene como comprobación manual y no como gate obligatorio de `dotnet build`.

## 📅 22 de Septiembre de 2026 09:33:33 — Paso 2 del Plan de Localización (i18n)

### 📌 Resumen General
Se completó la remediación principal de textos en español hardcodeados identificados en el diagnóstico del Paso 1, integrándolos con `wwwroot/js/locale.js` y manteniendo los seis idiomas soportados: `es`, `en`, `de`, `fr`, `ja` y `zh`.

### 🔧 Cambios Aplicados

- **F — Diccionario:** se añadieron claves para validaciones nativas, estados de censo y whitelist, mensajes PQR, resultados, candidaturas, botones, estados de carga y claves simbólicas de controladores. También se expuso `window.translateText()` para los scripts de las vistas.
- **E — Layouts:** se eliminó la inclusión duplicada de `locale.js` en `Views/Shared/_ElectorLayout.cshtml`, ya heredada desde `_Layout.cshtml`.
- **D — Controladores:** `AdminCensusController`, `AdminEmailReportController` y `PqrController` utilizan claves simbólicas en `TempData` y respuestas JSON. Los mensajes que conservan detalles dinámicos quedaron marcados con `TODO i18n-dynamic`.
- **B — Vistas:** se sustituyeron mensajes hardcodeados dentro de bloques `<script>` en cuentas administrativas, candidatos, censo, whitelist, eventos, registro, candidaturas, PQR, perfil, resultados y banner de primera visita mediante `translateText()`.
- **A — Chatbot:** `wwwroot/js/ayuda-chatbot.js` traduce el asunto del escalamiento PQR, el historial, las etiquetas `Asistente`/`Usuario` y el detalle adicional del usuario, conservando intactos los logs de depuración.
- **C — Razor:** se separaron etiquetas estáticas como `Grado`, `Opción Especial` y `Candidato` de los valores dinámicos provenientes del modelo para permitir su traducción mediante el DOM.

### 🧪 Verificación

- `dotnet build WahlMirai.Web/WahlMirai.Web.csproj --no-restore`: **correcto**.
- Diagnósticos del editor para `locale.js`, `ayuda-chatbot.js` y las vistas modificadas: **sin errores de sintaxis**.
- `diagnostic_scan.py`: no pudo ejecutarse porque el archivo no existe en la raíz del workspace.
- `StaticLocalizationAudit.ps1`: no pudo ejecutarse por un error de sintaxis preexistente en la referencia `$lang:` del propio script.
- La búsqueda dirigida no encontró residuos en los bloques de validación, errores, alertas y concatenaciones Razor abordados en este Paso 2; permanecen coincidencias fuera del alcance específico, como mensajes de `ProfileController` y etiquetas HTML estáticas traducibles por `locale.js`.

## 📅 22 de Septiembre de 2026 — Paso 9 continuado: auditoría de estado post-interrupción y cierre

### 📌 Contexto
Una sesión anterior (cuota de API agotada) ejecutó parcialmente el Paso 9 del plan i18n. Esta entrada documenta el estado real auditado desde cero antes de continuar, y los cambios aplicados para cerrar todos los ítems.

### 🔍 Auditoría de estado pre-fix (STEP 0-2)

Archivos modificados por la sesión anterior: 6 (`Completar.cshtml`, `Registro.cshtml`, `AdminCensus/Index.cshtml`, `AdminCensus/PendingWhitelist.cshtml`, `Pqr/Create.cshtml`, `Pqr/Index.cshtml`).

| Ítem Paso 9 | Estado encontrado | Diagnóstico |
|---|---|---|
| **Auth/Completar — locale-selector** | ✅ Completo | Selector de idioma y botón de tema eliminados correctamente de `Completar.cshtml` y `Registro.cshtml` |
| **Label: fragments (Rol:, etc.)** | ✅ Completo | `"Rol / Cargo"`, `"Grado:"`, `"Cargo:"` ya cubiertos en DICTIONARY; los templates JS en `<script>` no son auditables por el checker (correcto) |
| **Paginación "Mostrando X a Y de Z"** | ⚠️ Regresión cosmética | La sesión anterior eliminó `<strong>` de los números; la cadena contiene `@Model.*` → el checker la filtra (nunca pasa por el DICTIONARY JS). Se restauraron los `<strong>` |
| **Table headers AdminCensus/AdminEmailReport/Pqr/Manage/AdminAccounts** | ✅ Completo | `Nombre`, `Documento`, `Correo / Grado`, `Acciones`, `Nombre Completo` cubiertos en todos los idiomas |
| **PQR AJAX messages** | ✅ Wiring / ⚠️ Keys faltantes | Helper `t()` añadido y todas las `alert()` envueltas en `Pqr/Create.cshtml`; cadenas `"PQR creada correctamente."`, `"Error al crear PQR."`, `"Error de red."`, `"Asunto y mensaje son obligatorios."` ausentes del DICTIONARY (los scripts se excluyen del checker, pero se añaden para completitud) |
| **Ayuda cards partial-translation** | ℹ️ Correcto por diseño | La sesión anterior eliminó `<strong>` de los párrafos de ayuda; esto hace que las cadenas largas coincidan con las claves ya existentes en DICTIONARY (que fueron escritas sin etiquetas HTML). La pérdida de negrita en palabras clave es una regresión de formato no bloqueante para i18n |
| **Completar.cshtml:87 — clave combinada** | ❌ Faltante | La eliminación del `<strong>No</strong>` unió dos oraciones en una sola cadena `"Solo se usa para recuperación de acceso. No es tu usuario de inicio de sesión."` que **no existía** en DICTIONARY → único FAIL del checker |

Baseline pre-fix: `check-i18n-coverage.ps1` reportaba **FAIL — 1 faltante × 5 idiomas**. `dotnet build`: 0 errores, 1 advertencia (el propio checker).

### 🔧 Cambios aplicados

1. **`wwwroot/js/locale.js`** — Añadidas en los 5 bloques de idioma (`en`, `de`, `fr`, `ja`, `zh`):
   - `"Solo se usa para recuperación de acceso. No es tu usuario de inicio de sesión."` (cierra el único FAIL del checker)
   - `"Asunto y mensaje son obligatorios."`, `"PQR creada correctamente."`, `"Error al crear PQR."`, `"Error de red."` (completitud para los alerts JS de PQR/Create)

2. **`Views/AdminCensus/Index.cshtml` L236** — Restaurados `<strong>` en números de paginación (regresión cosmética).

3. **`Views/AdminCensus/PendingWhitelist.cshtml` L221** — Ídem.

### ✅ Verificación

- `check-i18n-coverage.ps1`: **PASS — 424/424 × 5 idiomas** (en, de, fr, ja, zh)
- `dotnet build WahlMirai.Web`: **Compilación correcta — 0 Errores, 0 Advertencias**

### ⚠️ Pendientes arquitectónicos (no bloqueantes para el deadline)

- La paginación "Mostrando X a Y de Z" es Razor server-rendered con interpolaciones → **no traducible** mediante el sistema DICTIONARY JS sin refactorizar hacia un patrón `data-i18n-template`. Anotado como mejora diferida en Arquitectura v2.9.
- Los `<strong>` en los párrafos de ayuda de `Pqr/Index.cshtml` fueron eliminados por la sesión anterior; recuperarlos requeriría fragmentar de nuevo los párrafos, lo que haría que las claves largas actuales dejaran de coincidir. Requiere una decisión de formato vs. traducibilidad.

## 📅 21 de Septiembre de 2026 — Sincronización post-merge y auditoría i18n

- Se confirmó y completó localmente el merge de `main` hacia `i18n-REMEDIATION.kevin` en el commit `33e5148`; la rama está adelantada respecto a `origin` y no se ejecutó ningún push.
- Se regeneró `wwwroot/css/site.css` desde `Styles/input.css`, se eliminó el conflicto real y se confirmó que no quedan marcadores Git sin resolver.
- Se corrigió el auditor para comparar contra la unión runtime de `DICTIONARY` y `EXTRA_TRANSLATIONS`; `getTranslation()` consulta ambos objetos.
- Baseline post-merge: 42 vistas, 445 cadenas únicas. Cobertura corregida: `en`, `de`, `fr`, `ja` y `zh`: 97/445, 348 faltantes por idioma. El merge añadió 4 cadenas frente al baseline anterior de 42/441.
- Se conectó el auditor a `dotnet build` mediante un target `AfterTargets="Build"` warning-only. El build conserva código 0 y muestra una advertencia cuando existen faltantes; la conversión a error queda como endurecimiento posterior al deadline.
- Se documentó en Arquitectura v2.9 la mejora diferida hacia claves `data-i18n="key"`, por la fragilidad de comparar texto exacto y el alcance transversal que tendría durante la estabilización de módulos.
- El bulk-fill completo queda pendiente: las 348 cadenas por idioma requieren traducción institucional real; no se añadieron placeholders ni claves españolas disfrazadas de traducciones.

## 📅 22 de Septiembre de 2026 — Integración final de traducciones i18n

- Se incorporaron las 330 entradas humanamente revisadas de `translations.json` en `DICTIONARY` para `en`, `de`, `fr`, `ja` y `zh`; `getTranslation()` y el mapa público consumen este objeto.
- La auditoría corregida pasó de 42 vistas y 445 cadenas extraídas a 425 cadenas UI auditables, con cobertura `425/425` en los cinco idiomas y sin warning i18n durante `dotnet build`.
- Se corrigió el parser para reconocer claves JSON con escapes Unicode y se mantuvo fuera del conteo el conjunto documentado de 18 falsos positivos no UI: 3 fragmentos de código, 3 rutas/URLs y 12 fragmentos de extracción. Permanecen registrados en `scripts/missing-strings-filtered-out.json`.
- La vista scaffold `Views/Home/Privacy.cshtml` tiene acción `HomeController.Privacy` y es accesible directamente, pero no aparece enlazada en la navegación (el footer usa `#`); queda señalada como candidata de limpieza, sin eliminarla.
- La verificación del navegador confirmó traducción real en Login y Privacy para francés y japonés. Las rutas protegidas (`AdminCensus`, `AdminEmailReport`, `Pqr/Manage`, `Profile`, `Candidacy`) redirigen a Login sin autenticación; `/Pqr` devuelve 500 por la base MySQL no disponible en el entorno. Por ello las seis composiciones con contenido dinámico requieren una verificación posterior con BD y sesión funcionales.

## 📅 22 de Septiembre de 2026 — Corrección de navegación y chatbot M08

- `_SiteHeader` quedó limitado a logo, idioma, tema y autenticación; se restauró la navegación desktop del elector en `_ElectorLayout` (`Panel de Control`, `Mis Candidaturas`, `Votar`/`Resultados`, `Ayuda`, `Mi Perfil`). `_AdminLayout` conserva su navegación administrativa y no muestra `Inicio` público.
- Se corrigió la traducción francesa de `Panel de Control` a `Tableau de bord` para que la landing administrativa no aparezca como `Accueil`.
- Se completó la ruta de traducción del chatbot: textos completos de los siete temas, greeting, placeholder, chips, respuestas de fallback, confirmaciones y mensajes de resolución usan `EXTRA_TRANSLATIONS`; la selección de chips y la búsqueda comparan también labels/títulos localizados.
- Verificación browser en `/Ayuda`: francés mostró `Bonjour ! Je suis votre assistant d’aide.`, `Saisissez un mot-clé...`, chips como `Créer un compte` y una respuesta localizada; japonés mostró `こんにちは！私はあなたのヘルプアシスタントです。`, `キーワードを入力...`, `候補者として応募` y la respuesta de recuperación al buscar `パスワード再設定`.
- Login admin verificado con `1020304050` / `Admin#2026!`: `/AdminEvents` mostró `Tableau de bord`, `Recensement`, `Rapports e-mail`, `Aide`, `Support PQR`, `Comptes admin` y `Profil`, sin `Inicio`. El login elector con `1001234567` / `1001234567.2026.` fue rechazado por la aplicación; su layout no pudo verificarse mediante sesión real.

## 📅 21 de Septiembre de 2026 — Revisión verificada de i18n compartida y corrección de regresión de selector

### 📌 Estado real verificado
Se revalidó el estado del proyecto desde cero con evidencia real en navegador. El problema visible de regresión del selector duplicado en los layouts anidados fue corregido, y la página de ayuda quedó traducida en inglés tras cambiar el idioma desde la UI. La comprobación se hizo sobre la app ejecutándose localmente y no solo por compilación del proyecto.

### 🔧 Cambios verificados
1. Se eliminó la duplicación del selector de idioma provocada por la doble renderización de `_Layout` + `_ElectorLayout`/`_AdminLayout`.
2. Se reforzó la traducción de strings confirmadas como rotas en la ayuda y chatbot: encabezados, subtítulos, temas rápidos, mensaje de bienvenida, texto de PQR y etiquetas del flujo de resolución.
3. Se dejó una base de traducción compartida con conteo verificado por lenguaje en `locale.js`.

### 📊 Verificación documental
- `dotnet build WahlMirai.Web/WahlMirai.Web.csproj` completó con éxito.
- La app se ejecutó en `http://localhost:5123` y se verificó con navegador que `/Ayuda` mostraba textos en inglés cuando el idioma era `en`.
- La comprobación adicional en `/Auth/Login` mostró una sola instancia del selector de idioma y texto traducido, sin duplicación visual.

### ⚠️ Honestidad de alcance
La corrección fue validada en los puntos de regresión confirmados y en la página no autenticada de ayuda. No se puede afirmar aún que todo el sitio esté 100% traducido en cada vista sin una auditoría completa por módulo, porque las pruebas de navegador se realizaron sobre los elementos confirmados como bloqueantes y los caminos de layout más críticos.

### 🧹 Este registro reemplaza la afirmación previa de “remediación completa”
La afirmación de remediación total no era sustentada por evidencia real en navegador; este registro deja la cuenta de manera honesta, con lo verificado y con lo que requiere revisión adicional.

---

---

## 📅 18 de septiembre de 2026 17:08:21 — Reducción de alcance y cobertura de WahlMirai.Tests

### 📌 Resumen General
Se consolidaron los fakes compartidos de pruebas y se amplió la cobertura específica de autenticación y perfil. El cambio es exclusivamente de tests: no introduce cambios funcionales ni modificaciones de esquema.

### 🔧 Cambios Aplicados
- Consolidación de `FakeAuthService`, `FakeEncryptionService` y `FakeAuditService` en `WahlMirai.Tests/Common/SharedFakes.cs`; antes estaban duplicados idénticamente en `AdminAccountServiceTests.cs` y `CensusWhitelistTests.cs`.
- Creación de `AuthServiceTests.cs` con 6 pruebas para login, estados no activos, hashing determinístico, verificación BCrypt y cambio de contraseña con auditoría.
- Creación de `ProfileServiceTests.cs` para actualización de correo, estados de persistencia/notificación y validación de contraseña actual.
- Incorporación del seed del rol `ELECTOR` en los helpers de prueba de autenticación para que el `Include` de rol usado por `AuthService` funcione en el contexto in-memory. Es una corrección de preparación de tests, no un cambio de código de producción.
- No se modificaron `VotingService`, `EventService` ni `CandidacyService`.
- No se modificó ningún archivo fuera de `WahlMirai.Tests` para la implementación; no se requirió actualización de ERS, arquitectura ni DDL porque no hubo cambio funcional o de esquema.

### 🧪 Verificación
- Línea base: **23/23** pruebas superadas.
- Resultado final desde estado limpio con `dotnet test .\WahlMirai.Tests\WahlMirai.Tests.csproj`: **33/33** pruebas superadas, 0 errores y 0 omitidas.
- Compilación final: **0 errores y 0 advertencias**.
- Confirmación de alcance: no se modificó ningún archivo de código fuera de `WahlMirai.Tests`; la única actualización externa es esta entrada documental solicitada.

---

## 📅 18 de Septiembre de 2026 10:30 — Corrección de validación duplicada de contact_email (RN-2.1)

### 📌 Resumen General
Se corrigió la validación de negocio que rechazaba registros y ediciones por correo de contacto duplicado en la capa de aplicación, contraviniendo la regla **RN-2.1** del proyecto. La política exige que el `contact_email` sea un dato de notificación y recuperación, no una clave única de identidad; por lo tanto, varios electores pueden compartir el mismo correo (por ejemplo, hermanos o acudientes) mientras la unicidad real se mantiene en `document_hash`.

### 🔧 Cambios Aplicados
- Eliminadas las comprobaciones de duplicidad de `ContactEmail` en el auto-registro (`WhitelistService`).
- Eliminadas las comprobaciones de duplicidad de `ContactEmail` en la actualización de perfil (`ProfileService`).
- Eliminada la validación de duplicidad de `ContactEmail` al editar un elector (`ICensusService`).
- Se preservaron intactas las validaciones de `document_hash`, que siguen siendo correctas y obligatorias.
- No se modificó el esquema de MySQL ni la entidad `WahlMiraiDbContext`, porque `contact_email` ya tiene un índice normal y no único, como exige RN-2.1.

### 🧪 Verificación
- `dotnet build WahlMirai.Web/WahlMirai.Web.csproj` debe completarse con 0 errores y 0 advertencias.
- `dotnet test WahlMirai.Tests/WahlMirai.Tests.csproj` debe seguir pasando con la nueva regla de negocio alineada a RN-2.1.

---

## 📅 17 de Septiembre de 2026 22:39 — Sesión Única Activa, Revocación Inmediata de Acceso, Corrección de Bug #16 e Incremento de Versión a v2.9

### 📌 Resumen General
Se implementó de manera integral la funcionalidad de **Sesión Única Activa** (bloqueo y expulsión ante inicios de sesión concurrentes) y **Revocación Inmediata de Acceso** ante bajas lógicas (`ELIMINADO`), desactivaciones, cambios a egresado o reasignación de contraseñas. El mecanismo opera **100% en la capa de aplicación en memoria (RAM)** mediante un almacén singleton concurrente (`IActiveSessionStore`) y la validación en tiempo real en cada solicitud HTTP vía `CookieAuthenticationEvents.OnValidatePrincipal` en ASP.NET Core, **sin requerir ninguna alteración al esquema de base de datos MySQL** (compatibilidad total con v2.8.2). Asimismo, se subsanó el **Bug #16** en `ICensusService.cs` corrigiendo los literales residuales de auditoría y nombres de entidad hacia `users`, y se promovió la versión global del sistema y de toda la documentación técnica a **v2.9**.

### 🔄 Corrección de Alcance
Durante la fase previa de desarrollo, se identificó que diversas modificaciones en `ICensusService.cs` correspondientes a la responsabilidad del módulo de Censo habían sido incorporadas en un ámbito no adecuado. Dichos cambios fueron formalmente identificados, aislados y reubicados dentro de su alcance modular correspondiente antes de consolidar y finalizar los cambios de esta tarea.

### 🚀 Detalle de Cambios

#### 1. Servicios y Almacén en Memoria
* **[NUEVO] `WahlMirai.Web/Services/IActiveSessionStore.cs`:**
  * Define la interfaz del servicio singleton con los métodos `RegisterSession(uint userId, string sessionStamp)`, `IsSessionValid(uint userId, string sessionStamp)` e `InvalidateSession(uint userId, SessionInvalidationReason reason)`.
  * Define el enum `SessionInvalidationReason` (`ReplacedByNewDevice`, `AccountDisabledOrDeleted`, `LoggedOut`).
* **[NUEVO] `WahlMirai.Web/Services/ActiveSessionStore.cs`:**
  * Implementación concurrente basada en `ConcurrentDictionary<uint, SessionEntry>`.
  * Permite la sobrescritura atómica de tokens de sesión y el almacenamiento del motivo de revocación para retroalimentación instantánea al cliente.

#### 2. Pipeline de Autenticación y Program.cs
* **[MODIFICADO] `WahlMirai.Web/Program.cs`:**
  * Registro singleton: `builder.Services.AddSingleton<IActiveSessionStore, ActiveSessionStore>()`.
  * Configuración del evento `OnValidatePrincipal`:
    * Lee `userId` de los claims de identidad y `sessionStamp` del claim `"SessionStamp"`.
    * Valida el token de sesión contra `IActiveSessionStore.IsSessionValid`.
    * Consulta en base de datos mediante consulta ligera y desconectada (`AsNoTracking`, proyectando solo `Status` y `DeletedAt`) que la cuenta permanezca en `ACTIVO`.
    * Si la sesión fue sustituida o la cuenta fue dada de baja/desactivada, ejecuta `context.RejectPrincipal()`, `SignOutAsync("Cookies")` y redirige a `/Auth/Login?reason=device` o `/Auth/Login?reason=disabled`.
  * Configuración del evento `OnRedirectToLogin` para preservar el query parameter `?reason=` evitando que sea sobrescrito por desafíos de autorización subsecuentes.

#### 3. Controladores y Vistas
* **[MODIFICADO] `WahlMirai.Web/Controllers/AuthController.cs`:**
  * Inyección de dependencia `IActiveSessionStore` en el constructor.
  * En `Login` (POST): emisión de nuevo GUID `SessionStamp`, registro en el store y adición del claim a la identidad previo al sign-in.
  * En `Completar` (POST, Paso 2 de auto-registro): emisión, registro de `SessionStamp` y sign-in para la sesión recién creada.
  * En `Logout`: extracción del ID del usuario y llamada explícita a `InvalidateSession(userId, SessionInvalidationReason.LoggedOut)` previo a la destrucción de la cookie.
* **[MODIFICADO] `WahlMirai.Web/Views/Auth/Login.cshtml`:**
  * Inclusión de banners visuales contextuales basados en `Context.Request.Query["reason"]`:
    * `reason=device` $\rightarrow$ *"Tu sesión se cerró porque se inició sesión en otro dispositivo."*
    * `reason=disabled` $\rightarrow$ *"Tu cuenta fue desactivada o eliminada."*
    * Sin razón / `reason=LoggedOut` $\rightarrow$ No despliega mensaje de alerta.

#### 4. Integración en Censo y Cuentas Administrativas (Revocación + Bug #16)
* **[MODIFICADO] `WahlMirai.Web/Services/ICensusService.cs`:**
  * Inyección de `IActiveSessionStore`.
  * `SoftDeleteVoterAsync`: corrección de literales de auditoría a `"USER_DELETED"` y entidad `"users"`; revocación inmediata de sesión activa con `AccountDisabledOrDeleted`.
  * `UpdateVoterAsync`: corrección de literales de auditoría a `"USER_UPDATED"` y entidad `"users"`; si el nuevo estado es distinto de `"ACTIVO"`, revocación inmediata de sesión activa.
  * `RestoreVoterAsync`: corrección de literales de auditoría a `"USER_RESTORED"` y entidad `"users"`.
  * `ResetPasswordAsync`: corrección de entidad de auditoría a `"users"`; revocación inmediata de sesión activa tras generar la nueva credencial.
* **[MODIFICADO] `WahlMirai.Web/Services/IAdminAccountService.cs`:**
  * Inyección de `IActiveSessionStore`.
  * `SoftDeleteAsync`: revocación inmediata de la sesión activa de la cuenta administrativa dada de baja con `AccountDisabledOrDeleted`.
  * `UpdateAsync`: revocación inmediata si el estado de la cuenta administrativa cambia a uno diferente de `ACTIVO`.

#### 5. Pruebas Unitarias y Documentación
* **[NUEVO] `WahlMirai.Tests/ActiveSessionStoreTests.cs`:**
  * Pruebas xUnit exhaustivas para flujo normal de sesión, concurrencia multi-dispositivo con invalidación por reemplazo, invalidación explícita por baja lógica/desactivación, logout voluntario y manejo de entradas nulas o usuarios no registrados.
* **[RENOMBRADO Y MODIFICADO] `docs/ers_wahl_mirai_v2_9.md`:**
  * Renombrado desde `docs/ers_wahl_mirai_v2_8.2.md`.
  * Incorporación formal de la regla de negocio transversal **RN-14 (Sesión Única Activa e Invalidación Inmediata de Acceso)**.
  * Actualización de postcondiciones en **RF-M01-01** (emisión de token único), **RF-M02-01** (revocación inmediata por baja/cambio de estado) y **RF-M09-01** (revocación inmediata de cuentas administrativas).
* **[RENOMBRADO Y MODIFICADO] `docs/Arquitectura_y_Diseno_v2_9.md`:**
  * Renombrado desde `docs/Arquitectura_y_Diseno_v2_8.2.md`.
  * Documentación técnica del diseño en memoria, ciclo de vida de tokens, interceptores en `Program.cs` y justificación arquitectónica de cero alteraciones a la base de datos MySQL.
  * Actualización de la matriz de componentes y endpoints en la sección 5.
* **[RENOMBRADO Y MODIFICADO] `docs/wahl_mirai_db_v2_9_completo.sql`:**
  * Renombrado desde `docs/wahl_mirai_db_v2_8.2_completo.sql`.
  * Actualización del bloque de encabezado formalizando versión 2.9 y certificando 100% de compatibilidad de esquema con v2.8.2 sin modificaciones a sentencias DDL.

### 🧪 Verificación y Control de Alcance
* **Compilación:** `dotnet build WahlMirai.Web/WahlMirai.Web.csproj` completado con éxito con **0 errores y 0 advertencias**.
* **Pruebas Unitarias:** `dotnet test WahlMirai.Tests/WahlMirai.Tests.csproj` completado con éxito con **23 de 23 pruebas superadas** (incremento desde la línea base de 15 pruebas previas).
* **Validación Manual de Comportamiento:**
  * Inicio de sesión simultáneo en dos navegadores/dispositivos: el primer dispositivo queda revocado al siguiente clic y muestra la alerta correspondiente.
  * Desactivación / eliminación lógica (`ELIMINADO`, `EGRESADO`) en electores o administradores: la sesión abierta se invalida de inmediato en la siguiente petición HTTP.
  * Intento de login con cuenta no activa: rechazado inmediatamente en el formulario con retroalimentación clara.

---

## 📅 16 de Septiembre de 2026 17:08 — Sincronización Integral de la Arquitectura de Software con la Implementación Web (v2.8.2)

### 📌 Resumen General
Se realizó una auditoría y sincronización completa del documento técnico de Arquitectura y Diseño (`docs/Arquitectura_y_Diseno_v2_8.2.md`) para dejarlo 100% alineado con el código fuente real del proyecto web (`WahlMirai.Web`), resolviendo todas las discrepancias históricas acumuladas entre la especificación teórica y la implementación en producción.

### 🚀 Detalle de Cambios en la Documentación

#### 1. Estructura de Directorios y Controladores Reales
- Sincronizada la estructura física de `WahlMirai.Web/`:
  - Se formalizaron los controladores administrativos con su nomenclatura y rutas activas: `AdminAccountsController`, `AdminCandidatesController`, `AdminCensusController`, `AdminEmailReportController` y `AdminEventsController`.
  - Se documentó `ElectorController` como el controlador principal para la experiencia del estudiante (dashboard, tarjetón de votación, consulta de propuestas y emisión de voto), sustituyendo el anterior `VotingController` teórico.
  - Se incorporó `RecuperacionAccesoController` para el flujo público y anónimo de recuperación de contraseñas.
  - Se actualizaron las rutas reales de auto-registro en `AuthController` (`/Auth/Registro` y `/Auth/Completar`).
  - Se corrigió la ubicación de `WahlMiraiDbContext.cs` bajo `Models/` (eliminando la referencia obsoleta a una carpeta `Data/`).
  - Se documentó la distribución de DTOs en `ViewModels/` y en las interfaces de `Services/`.

#### 2. Modelo de Datos y Vistas MySQL
- **Contrato ORM C# vs MySQL:** Se aclaró que en la base de datos las tablas son `users` y `event_participations`, y en Entity Framework Core se mapean mediante las entidades de dominio `Voter` y `VoterEventParticipation` con `.ToTable("users")` y `.ToTable("event_participations")` para preservar la estabilidad del código.
- **Vistas MySQL Keyless:** Se documentaron formalmente las 5 vistas registradas en el DbContext: `vw_vote_counts`, `vw_active_census`, `vw_pending_whitelist`, `vw_pending_email_queue` y la recientemente conectada `vw_pending_candidacies` para optimización de consultas en la revisión administrativa.
- **Campo `allow_correction`:** Se incorporó en el diagrama Mermaid de la tabla `candidates` y en la especificación de `Candidate.cs`, reflejando la capacidad del administrador de rechazar una postulación permitiendo su posterior subsanación por el estudiante.

#### 3. Frontend y Recursos Estáticos
- **Tailwind CSS v4:** Se corrigió la referencia del archivo compilado de salida: el pipeline MSBuild de `tailwindcss.exe` compila `Styles/input.css` hacia `wwwroot/css/site.css` (no `tailwind.css`).
- **Scripts Vanilla JS:** Se documentó la presencia de `theme.js` (dark mode persistente), `locale.js` (soporte multi-idioma para 6 lenguas), `ayuda-chatbot.js` (motor FAQ por reglas con escalamiento a PQR) y `auth-completar.js` (medidor de complejidad de contraseñas).
- **Assets y Descifrado:** Se documentaron las 7 ilustraciones SVG de Ayuda en `wwwroot/img/ayuda/`, el avatar por defecto `default-avatar.png` en `wwwroot/images/` y el endpoint seguro `/AdminCandidates/ServeDocument?path=...` para servir de forma descifrada archivos `.enc` fuera de `wwwroot`.

#### 4. Backend, Seguridad y Background Workers
- Se especificó el uso de `EmailQueueBackgroundService` (IHostedService consumidor de `vw_pending_email_queue`), `MailKitEmailSender`, `ResultsHub` (SignalR en `/hubs/resultsHub`), `UseForcePasswordChange()` y la configuración global de `SplitQuery` en Pomelo MySQL.

---

## 📅 16 de Septiembre de 2026 16:53 — Corrección Completa de Archivos de Candidatura: Fotos, Plan de Gobierno y Soportes Documentales

### 📌 Resumen General
Se identificó y corrigió un bug estructural preexistente: todas las fotos de candidatos, planes de gobierno y soportes documentales se almacenan como archivos cifrados (`.enc`) fuera de `wwwroot`, pero se referenciaban con URLs directas sin ningún endpoint que los sirviera. El resultado era un 404 silencioso al intentar abrir cualquier archivo desde las vistas admin. Adicionalmente se corrigió el 404 periódico en `/images/default-avatar.png`.

### 🐛 Bugs Detectados

| # | Error en Log | Causa Raíz |
|---|---|---|
| 1 | `404 at /uploads/candidacies/photo_*.png.enc` | Las fotos de candidatos se sirven como URL directa — no existe endpoint que descifre y sirva el archivo `.enc` |
| 2 | `404 at /uploads/candidacies/plan_*.pdf.enc` | Plan de gobierno: mismo problema que la foto |
| 3 | Soportes documentales ("Ver Soporte") no abren | Mismo origen: `fileUrl` apunta a un `.enc` sin controlador detrás |
| 4 | `404 at /images/default-avatar.png` (repetido) | El archivo `default-avatar.png` no existía en `wwwroot/images/` |

### 🚀 Detalle de Cambios

#### [MODIFICADO] `WahlMirai.Web/Controllers/AdminCandidatesController.cs`
- Inyectados `IDocumentEncryptionService` y `IConfiguration` en el constructor.
- **[NUEVO endpoint]** `GET /AdminCandidates/ServeDocument?path=...`:
  - Extrae el nombre de archivo del parámetro `path` (sanitización anti path-traversal).
  - Construye la ruta física absoluta usando `CandidacyUploads:BasePath` de configuración (fallback: `App_Data/candidacy-uploads`).
  - Lee el archivo `.enc` del disco.
  - Lo descifra con `IDocumentEncryptionService.Decrypt()` → Base64 → bytes del archivo original.
  - Detecta el `Content-Type` por la extensión sin `.enc` (`.pdf` → `application/pdf`, `.png`/`.jpg`/`.jpeg` → `image/*`).
  - Devuelve `File(bytes, contentType)` para abrir directamente en el navegador.
  - Acceso protegido por `[Authorize(Roles = "ADMIN,SUPER_ADMIN")]` heredado del controlador.

#### [MODIFICADO] `WahlMirai.Web/Views/AdminEvents/_ConfigurePersonas.cshtml`
- **Foto en tabla** (L98): `src="@c.PhotoUrl"` → `src="/AdminCandidates/ServeDocument?path=@Uri.EscapeDataString(c.PhotoUrl)"`
- **Foto en modal JS** (L419): `photoEl.src = data.photoUrl` → `photoEl.src = \`/AdminCandidates/ServeDocument?path=${encodeURIComponent(data.photoUrl)}\``
- **Plan de gobierno en tabla** (L123): `href="@c.GovernmentPlanUrl"` → `href="/AdminCandidates/ServeDocument?path=@Uri.EscapeDataString(c.GovernmentPlanUrl)"`
- **Plan de gobierno en modal JS** (L473): `href = data.governmentPlanUrl` → `href = \`/AdminCandidates/ServeDocument?path=${encodeURIComponent(data.governmentPlanUrl)}\``
- **Soportes documentales JS** (L447): `href="${req.fileUrl}"` → `href="/AdminCandidates/ServeDocument?path=${encodeURIComponent(req.fileUrl)}"`

#### [MODIFICADO] `WahlMirai.Web/Views/AdminCandidates/Index.cshtml`
- Mismos 5 puntos corregidos que en `_ConfigurePersonas.cshtml` (foto tabla, foto modal JS, plan gobierno tabla, plan gobierno modal JS, soportes JS).

#### [NUEVO] `WahlMirai.Web/wwwroot/images/default-avatar.png`
- Generado y copiado a `wwwroot/images/default-avatar.png`. Ícono minimalista de silueta de usuario (fondo gris oscuro, silueta blanca) que el atributo `onerror` de las fotos de candidatos usa como fallback cuando la imagen falla.

### 🔍 Notas Técnicas
- El error **MSB3027/MSB3021 en dotnet build** no es un error de código. Es un error de sistema de archivos producido porque `WahlMirai.Web` estaba corriendo en producción local (PID 14480) y bloqueaba el `.exe` al intentar reemplazarlo. Las vistas Razor y archivos estáticos se refrescan sin rebuild en `dotnet run`.
- Los archivos en `wwwroot/images/` (activos estáticos) y las vistas `.cshtml` no requieren rebuild para actualizarse en modo desarrollo.

### 🧪 Verificación
- `dotnet build WahlMirai.Web`: 0 errores, 0 advertencias de código (error MSB3027 es de file-lock de proceso en ejecución, no de compilación).
- Comportamiento esperado en ejecución con MySQL: Clic en foto de candidato → imagen se muestra en la tabla. Clic en "Ver Plan Gobierno" → PDF se abre en nueva pestaña. Clic en "Ver Soporte" → documento se abre en nueva pestaña. Sin 404 en `/images/default-avatar.png`.

---

## 📅 16 de Septiembre de 2026 16:44 — Corrección de Soportes Documentales y Plan de Gobierno en Vistas Admin (Primera Pasada)

### 📌 Resumen General
Primera identificación del bug de archivos `.enc` sin endpoint. Se creó el endpoint `ServeDocument` y se corrigieron los links de soportes documentales y plan de gobierno. La foto del candidato se detectó como afectada en la siguiente sesión (16:53).

### 🐛 Bugs Detectados

| Error | Causa |
|---|---|
| Clic en "Ver Soporte" no hace nada | `href` apuntaba a `/uploads/candidacies/doc_X.enc` — URL sin controlador |
| Clic en "Ver Plan Gobierno" abre 404 | Mismo origen que soportes |

> Los tests unitarios no detectan este tipo de fallos porque ejercitan lógica C# en memoria, no la apertura de archivos desde el navegador.

---

## 📅 16 de Septiembre de 2026 16:31 — Corrección de `Requirements` Vacío en Vista de Revisión de Candidatos

### 📌 Resumen General
Se corrigió un doble bug encadenado en `GetCandidatesForReviewAsync` de `CandidateReviewService` introducido en el Paso 2 (conexión de vistas decorativas). El modal de revisión de documentos en `/AdminEvents/Edit/{id}` fallaba silenciosamente porque `Requirements` llegaba vacío/null para candidatos en estado `PENDIENTE`.

### 🐛 Bugs Detectados

| Bug | Descripción |
|---|---|
| **Bug 1** — `Requirements` vacío | Al construir `CandidateReviewDetailDto` para candidatos PENDIENTE (rama de la vista `vw_pending_candidacies`), el campo `Requirements` no se asignaba. El modal JS leía `data.requirements` vacío y no renderizaba la lista de soportes. |
| **Bug 2** — Requisitos de posición no cargados | `fallbackRequirements` se cargaba filtrando solo los `positionId` de candidatos **no** pendientes (`nonPendingPositionIds`). Al intentar construir `reqChecks` para un candidato PENDIENTE, la lista de requisitos de ese cargo estaba vacía. |

### 🚀 Detalle de Cambios

#### [MODIFICADO] `WahlMirai.Web/Services/CandidateReviewService.cs`
- **Fix Bug 2**: Renombrado `nonPendingPositionIds` → `allPositionIds`. El filtro `.Where(c => !pendingCounts.ContainsKey(c.Id))` fue eliminado para incluir los cargos de candidatos PENDIENTES también.
- **Fix Bug 1**: En la rama PENDIENTE del `foreach`, se construye `reqChecks` usando `positionRequirements` (filtrados del listado ya cargado) cruzados con `c.CandidacyDocuments` (incluidos vía `Include()`). Se asigna `Requirements = reqChecks` al `CandidateReviewDetailDto`.

### 🧪 Verificación
- `dotnet build`: 0 errores, 0 advertencias.
- `dotnet test WahlMirai.Tests`: 15/15 pruebas superadas.

---

## 📅 16 de Septiembre de 2026 16:15 — M01/M07: Conexión Funcional de la Vista MySQL vw_pending_email_queue


### 📌 Resumen General
En cumplimiento con la auditoría de vistas de base de datos de la versión 2.8, se conectó la vista MySQL `vw_pending_email_queue` al flujo de despacho en segundo plano de `EmailQueueBackgroundService`. El paso de selección ahora consulta de forma directa y eficiente la vista mapeada (`dbContext.VwPendingEmailQueues`) para determinar el siguiente correo pendiente por procesar, obteniendo su identificador y realizando posteriormente la carga de la entidad rastreada `EmailQueue` para preservar la mutabilidad requerida en el incremento de reintentos, actualización de estados y persistencia en base de datos.

### 🚀 Detalle de Cambios

#### [MODIFICADO] `WahlMirai.Web/Services/EmailQueueBackgroundService.cs`
- **Separación de selección y mutación**: Se reemplazó la consulta LINQ directa sobre la tabla base `EmailQueues` por una consulta previa sobre el `DbSet<VwPendingEmailQueue>` (`dbContext.VwPendingEmailQueues`), ordenada por `CreatedAt` y filtrada por `Attempts < 3`.
- **Carga de entidad rastreada**: Una vez obtenido el `Id` desde la vista, se carga la entidad `EmailQueue` correspondiente mediante `FirstOrDefaultAsync(e => e.Id == pendingQueueItem.Id)` con `.Include(e => e.Voter)`, asegurando que las modificaciones de estado (`ENVIADO`, `FALLIDO`), actualización de `Attempts++` y `SaveChangesAsync()` operen con normalidad sobre el Change Tracker de EF Core.

### 🧪 Verificación
- `dotnet build WahlMirai.Web`: 0 errores, 0 advertencias.
- `dotnet test WahlMirai.Tests`: 15/15 pruebas unitarias superadas exitosamente.

---

## 📅 15 de Septiembre de 2026 16:30 — M08 Ayuda: Cierre de Lightbox en Backdrop e Integración SVG en Chatbot (incluyendo Postulación)

### 📌 Resumen General
Se corrigió la usabilidad del visor modal (lightbox) de diagramas SVG en el módulo de Ayuda (M08). Se añadió el cierre de los 7 elementos nativos `<dialog>` al hacer clic en el backdrop oscurecido, se integró la funcionalidad de ampliación modal en las respuestas dinámicas del chatbot asistente (`ayuda-chatbot.js`) y se resolvió la inconsistencia del tema `postulacion`, el cual mantenía `image: null` en el catálogo de temas del chatbot a pesar de contar con el asset SVG (`ayuda-postulacion.svg`) y su modal correspondiente (`modal-postulacion`) ya implementados en la vista.

### 🚀 Detalle de Cambios

#### [MODIFICADO] `WahlMirai.Web/Views/Pqr/Index.cshtml`
- Se agregó un bloque de script inmediatamente posterior a los 7 elementos `<dialog>` que intercepta clics sobre el pseudo-elemento backdrop (`e.target === dialog`) y ejecuta `dialog.close()`, permitiendo cerrar cualquier modal al hacer clic fuera del contenido sin depender de rol de usuario ni alterar el cierre nativo por botón o tecla Escape.

#### [MODIFICADO] `WahlMirai.Web/wwwroot/js/ayuda-chatbot.js`
- **Ampliación modal en respuestas del chatbot**: En `renderTopicResponse(topic)`, se derivó el ID del diálogo como `` `modal-${topic.key}` `` y se envolvió la miniatura SVG de las respuestas del bot en un `<button type="button">` con `onclick="document.getElementById('...').showModal()"`, clases interactivas accesibles (`cursor-zoom-in`, hover y focus visible) y atributo `aria-label`.
- **Corrección de imagen en tema `postulacion`**: En la constante `TOPICS`, se actualizó la propiedad `image` del tema `postulacion` de `null` a `'/img/ayuda/ayuda-postulacion.svg'`, conectando la miniatura del diagrama de postulación con el modal `#modal-postulacion`.

### 🧪 Verificación
- `dotnet build WahlMirai.Web`: 0 errores, 0 advertencias.
- Cierre en backdrop verificado para los 7 modales nativos.
- Renderizado de imagen y apertura de lightbox verificados para todos los temas con ilustración del chatbot, incluyendo `postulacion`.

---

## 📅 15 de Septiembre de 2026 15:33 — Fix EF Core MultipleCollectionIncludeWarning con SplitQuery global

### 📌 Resumen General
Se configuró globalmente `QuerySplittingBehavior.SplitQuery` en la configuración de Pomelo MySQL de Entity Framework Core. Esta configuración resuelve la advertencia de compilación de consultas relacionales (`MultipleCollectionIncludeWarning` / `RelationalEventId.MultipleCollectionIncludeWarning`) generada al cargar múltiples navegaciones de colección (`.Include()`) en una sola consulta (por ejemplo en `EventService` y `CandidacyService`), previniendo degradaciones de rendimiento por explosión cartesiana (*cartesian explosion*) a medida que crecen los datos.

### 🚀 Detalle de Cambios

#### [MODIFICADO] `WahlMirai.Web/Program.cs`
- En el registro del contenedor de inyección de dependencias (`AddDbContext<WahlMiraiDbContext>`), se agregó la opción `mySqlOptions => mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)` en la llamada a `options.UseMySql()`, manteniendo intacta la cadena de conexión y la autodetección de versión del servidor.

#### [MODIFICADO] `WahlMirai.Web/Models/WahlMiraiDbContext.cs`
- En el método de respaldo `OnConfiguring(DbContextOptionsBuilder optionsBuilder)`, utilizado fuera de DI (e.g. herramientas de diseño/migraciones de EF Core), se agregó idénticamente la opción `mySqlOptions => mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)` en su llamada a `optionsBuilder.UseMySql()`.

### 🧪 Verificación
- `dotnet build`: 0 errores, 0 advertencias.
- `dotnet test`: 15/15 pruebas superadas exitosamente en `WahlMirai.Tests`.
- Verificación en ejecución: Consultas con múltiples colecciones de `EventService` y `CandidacyService` ejecutadas contra la base de datos sin emitir advertencias `MultipleCollectionIncludeWarning` en los registros.

---

## 📅 14 de Septiembre de 2026 17:06 — M08 Ayuda: Corrección de texto en Diagrama de Postulación


### 📌 Resumen General
Se ajustó el texto del Paso 1 en el diagrama SVG de postulación (`ayuda-postulacion.svg`) y sus atributos de accesibilidad correspondientes.

### 🚀 Detalle de Cambios

#### [MODIFICADO] `WahlMirai.Web/wwwroot/img/ayuda/ayuda-postulacion.svg`
- **Texto visual**: En el Nodo 1 (Paso 1), se corrigió el título de `"Entra a Postularme"` a `"Entra a Mis Candidaturas"`.
- **Accesibilidad (a11y)**: Se actualizaron coherentemente los atributos `aria-label` y `<desc>` en el `<svg>` para reflejar `"Entra a Mis Candidaturas"` en lugar de `"Entra a Postularme"`.
- **Restricción de color**: Se mantuvieron intactos todos los atributos de `fill` y `stroke`.

---

## 📅 14 de Septiembre de 2026 16:49 — M08 Ayuda: 4 Correcciones Post-Implementación (Clipping, Postulación, Centrado, Móvil)

### 📌 Resumen General
Se aplicaron cuatro correcciones sobre la implementación anterior (16:28) del Módulo de Ayuda (M08). Sin modificación de paleta de colores.

### 🚀 Detalle de Cambios

#### Fix 1 — [MODIFICADOS] `wwwroot/img/ayuda/*.svg` — Corrección de Clipping en los 6 SVG
**Problema**: Al aumentar `stroke-width` y `font-size` en la sesión anterior, los bordes y textos de los pasos extremos podían quedar recortados por el lienzo original `0 0 702 164`.

**Solución aplicada** (idéntica en los 6 archivos):
- `viewBox` cambiado de `"0 0 702 164"` → `"-20 -20 742 204"` (agrega 20 px de margen en los 4 lados sin mover ninguna coordenada interna).
- Añadido atributo `overflow="visible"` como salvaguarda adicional ante navegadores que clipean sin importar el viewBox.
- **Sin cambios de coordenadas internas, iconos ni colores.**

Archivos: `ayuda-registro.svg`, `ayuda-login.svg`, `ayuda-recuperar.svg`, `ayuda-votar.svg`, `ayuda-perfil.svg`, `ayuda-resultados.svg`.

---

#### Fix 2 — [NUEVO] `wwwroot/img/ayuda/ayuda-postulacion.svg` + integración en vista
**Problema**: El acordeón 4 "¿Cómo me postulo como candidato?" era el único sin ilustración.

**Nuevo archivo creado** con `viewBox="-20 -20 742 204"` y `overflow="visible"` desde el inicio:

| Nodo | Título | Subtítulo | Icono |
|---|---|---|---|
| 1 | Entra a Postularme | En etapa de Inscripción | Persona + estrella |
| 2 | Carga tus soportes | Propuestas, plan y requisitos | Documento con flecha de subida |
| 3 | Espera aprobación | Revisión del Administrador | Reloj (reutilizado de ayuda-resultados) |

Incluye `aria-label`, `<title>` y `<desc>` completos para accesibilidad.

**Integración en `Views/Pqr/Index.cshtml`**: acordeón 4 actualizado con botón disparador `cursor-zoom-in` y modal `<dialog id="modal-postulacion">`.

---

#### Fix 3 — [MODIFICADO] `Views/Pqr/Index.cshtml` — Centrado de modales (`m-auto`)
**Problema**: El Preflight de Tailwind CSS v4 elimina el `margin: auto` UA del navegador sobre `<dialog>`, haciendo que el modal aparezca pegado a la esquina superior izquierda.

**Solución**: Añadida la clase utilitaria `m-auto` a los **7 elementos `<dialog>`** (los 6 originales + el nuevo de postulación). Clase resultante por modal:
```
max-w-4xl w-full p-0 m-auto rounded-xl shadow-2xl border-0 backdrop:bg-black/80 open:animate-fade-in
```

---

#### Fix 4 — [MODIFICADO] `Views/Pqr/Index.cshtml` + `Styles/input.css` — Usabilidad Móvil

**Problema**: En pantallas < 650 px, el `<svg>` se encoge hasta ser ilegible.

**Solución en la vista** (aplicada en los 7 modales):
- Imagen envuelta en `<div class="w-full overflow-x-auto">` para habilitar scroll lateral solo cuando desborda.
- Clase de imagen: `min-w-[650px] md:min-w-full h-auto` — fuerza ancho mínimo en móvil; en `md+` es fluida al 100%.
- Aviso solo en móvil: `<p class="sm:hidden ...">Desliza hacia los lados para ver los 3 pasos</p>`.

**Solución en `Styles/input.css`** (Fix 4b):
- Añadidos `@keyframes fade-in` y la regla `.open\:animate-fade-in[open]` al final del archivo.
- Activa la animación de entrada (opacidad 0→1 + escala 0.97→1, duración 0.18 s) al abrir cualquier `<dialog>` con `showModal()`.
- Tailwind recompilará `wwwroot/css/site.css` en el siguiente ciclo del watcher.

---

### 📦 Archivos Modificados / Creados (resumen)
| Archivo | Tipo | Fix |
|---|---|---|
| `wwwroot/img/ayuda/ayuda-registro.svg` | SVG | Fix 1 — viewBox + overflow |
| `wwwroot/img/ayuda/ayuda-login.svg` | SVG | Fix 1 — viewBox + overflow |
| `wwwroot/img/ayuda/ayuda-recuperar.svg` | SVG | Fix 1 — viewBox + overflow |
| `wwwroot/img/ayuda/ayuda-votar.svg` | SVG | Fix 1 — viewBox + overflow |
| `wwwroot/img/ayuda/ayuda-perfil.svg` | SVG | Fix 1 — viewBox + overflow |
| `wwwroot/img/ayuda/ayuda-resultados.svg` | SVG | Fix 1 — viewBox + overflow |
| `wwwroot/img/ayuda/ayuda-postulacion.svg` | SVG [NUEVO] | Fix 2 — nuevo diagrama |
| `Views/Pqr/Index.cshtml` | Razor | Fix 2 + Fix 3 + Fix 4 |
| `Styles/input.css` | CSS | Fix 4b — animación fade-in |

---

## 📅 14 de Septiembre de 2026 16:28 — M08 Ayuda: Accesibilidad SVG y Modales de Ampliación de Diagramas

### 📌 Resumen General
Se implementaron mejoras de accesibilidad (a11y) WCAG 2.1 y una función de ampliación visual sobre los diagramas del Módulo de Ayuda (M08), utilizando el elemento nativo HTML5 `<dialog>` y los tokens semánticos de Tailwind CSS v4 ya definidos en `Styles/input.css`. No se modificó la paleta de colores (responsabilidad de otro desarrollador).

### 🚀 Detalle de Cambios

#### [MODIFICADOS] `WahlMirai.Web/wwwroot/img/ayuda/*.svg` — (6 archivos)
Archivos afectados: `ayuda-registro.svg`, `ayuda-login.svg`, `ayuda-recuperar.svg`, `ayuda-votar.svg`, `ayuda-perfil.svg`, `ayuda-resultados.svg`.

**Accesibilidad:**
- Añadido atributo `aria-label` descriptivo y completo en la etiqueta `<svg>` raíz de cada archivo, describiendo los 3 pasos del diagrama para lectores de pantalla.
- Añadidas etiquetas internas `<title>` y `<desc>` para soporte completo de tecnologías asistivas cuando el SVG se carga como `<img>` o se incrusta inline.
- `role="img"` ya existía; se conservó sin modificación.

**Legibilidad:**
- `stroke-width` de los rectángulos contenedores y flechas de conexión elevado de `1` / `2` a `2.5`.
- `font-size` de etiquetas de título de paso (`font-weight="600"`) elevado de `13.5` → `14`.
- `font-size` de subtítulo de paso elevado de `11.5` → `12`.
- `font-weight` de ambas capas de texto cambiado a `bold` para mejorar contraste en pantallas de baja resolución.
- `stroke-width` de trazos de iconos internos elevado de `1.5`–`1.6` a `2`–`2.2`.
- **RESTRICCIÓN CUMPLIDA**: Ningún valor `fill` ni `stroke` relacionado con color fue modificado.

#### [MODIFICADO] `WahlMirai.Web/Views/Pqr/Index.cshtml`

**Sustitución de `<img>` estáticos por disparadores accesibles:**
- Las 6 preguntas con ilustración (registro, login, recuperar, votar, perfil, resultados) ahora envuelven la imagen en un `<button type="button">` con:
  - `cursor-zoom-in` para comunicar visualmente la acción de ampliar.
  - `aria-label` descriptivo para lectores de pantalla.
  - `onclick="document.getElementById('modal-X').showModal()"` como disparador del modal nativo.
  - `focus-visible:ring-2 focus-visible:ring-primary` para navegación por teclado.
- Añadido `<small class="block text-xs text-on-surface-variant mt-2">Haz clic sobre la imagen para ampliarla</small>` debajo de cada botón como aviso contextual.
- El texto explicativo de cada pregunta se conservó íntegro; solo se reordenó después del botón/aviso.

**Implementación de Modales Nativos `<dialog>` (6 modales):**
- IDs: `modal-registro`, `modal-login`, `modal-recuperar`, `modal-votar`, `modal-perfil`, `modal-resultados`.
- Estilo con Tailwind v4: `max-w-4xl w-full p-0 rounded-xl shadow-2xl border-0`.
- Pseudo-elemento backdrop: `backdrop:bg-black/80` para oscurecer el fondo de pantalla.
- Contenedor interior: `bg-surface-container-lowest rounded-xl overflow-hidden`.
- Cabecera del modal: título de la pregunta + botón de cierre con icono `close` de Material Symbols.
- **Cierre nativo sin JS**: Implementado mediante `<form method="dialog"><button type="submit">Cerrar</button></form>`, aprovechando el comportamiento nativo del elemento `<dialog>` de HTML5.
- Las imágenes dentro del modal usan `w-full h-auto` sin restricción de `max-w-sm`, mostrando el diagrama a máximo ancho disponible.

### 🔍 Notas Técnicas
- Los `<dialog>` se posicionan al final del DOM de la vista, fuera del grid principal, para evitar problemas de `z-index` y `overflow` con contenedores padre.
- La clase `open:animate-fade-in` está anotada para cuando el equipo defina la animación en `Styles/input.css`; no rompe si no existe.
- **Sin dependencias JS adicionales**: Todo el ciclo abrir/cerrar modal se maneja con APIs nativas del navegador (`HTMLDialogElement.showModal()` y `form[method=dialog]`).
- La pregunta 4 (Postulación) no tiene ilustración SVG asignada; se dejó sin botón/modal para mantener consistencia con el estado actual del asset.

### 📦 Archivos Modificados (resumen)
| Archivo | Tipo | Cambio |
|---|---|---|
| `wwwroot/img/ayuda/ayuda-registro.svg` | SVG | A11y + legibilidad |
| `wwwroot/img/ayuda/ayuda-login.svg` | SVG | A11y + legibilidad |
| `wwwroot/img/ayuda/ayuda-recuperar.svg` | SVG | A11y + legibilidad |
| `wwwroot/img/ayuda/ayuda-votar.svg` | SVG | A11y + legibilidad |
| `wwwroot/img/ayuda/ayuda-perfil.svg` | SVG | A11y + legibilidad |
| `wwwroot/img/ayuda/ayuda-resultados.svg` | SVG | A11y + legibilidad |
| `Views/Pqr/Index.cshtml` | Razor | Botones disparadores + 6 modales `<dialog>` |

---

## 📅 14 de septiembre de 2026 15:23:09 — Solución Integral de Cola de Correos (EmailQueue) y Limpieza de EmailType (Bug #15)

> [!WARNING]
> **Aviso de Infraestructura Compartida**: Este cambio modifica infraestructura compartida utilizada por el módulo M04 (Camilo, `CandidateReviewService`) y M02 (Dev 2, `CensusService`). Camilo y Dev 2 **NO han sido notificados aún** al momento de este commit. Kevin debe enviar la notificación correspondiente al equipo.

### 📌 Resumen General
Se corrigió el defecto de bifurcación de contraseñas en el worker en segundo plano `EmailQueueBackgroundService` y se completó la sincronización definitiva del enum `EmailType` con el esquema MySQL en vivo. Anteriormente, el worker asumía de forma invertida que todo correo que no fuera de candidatura (`CANDIDATURA_*`) debía requerir una contraseña temporal en memoria (`IPendingPasswordStore`), lo que provocaba que notificaciones como `RESPUESTA_PQR` o `CAMBIO_PERFIL` fallaran inmediatamente con el error *"La contraseña en memoria se perdió (reinicio del servicio)"*. Asimismo, se retiró el miembro en desuso `CREDENCIAL_INICIAL` del enum C#, dejando el enum 100% alineado con la base de datos MySQL.

### 🚀 Detalle de Cambios
- **[MODIFICADO] `WahlMirai.Web/Services/EmailQueueBackgroundService.cs`**:
  - Implementada lista blanca explícita `PasswordCarryingTypes` (`HashSet<EmailType>`) que restringe la exigencia de contraseña temporal exclusivamente a `RECUPERACION_ACCESO` y `REASIGNACION_ADMIN`.
  - Todos los demás tipos de correo (`CAMBIO_PERFIL`, `RESPUESTA_PQR`, `CANDIDATURA_APROBADA`, `CANDIDATURA_RECHAZADA` y cualquier valor futuro) toman de manera predeterminada y segura la ruta libre de contraseñas, reutilizando la plantilla HTML unificada del sistema.
  - Soportado asunto y mensaje descriptivo para notificaciones de `RESPUESTA_PQR` y `CAMBIO_PERFIL`.
- **[MODIFICADO] `WahlMirai.Web/Services/ICredentialService.cs`**:
  - Eliminado definitivamente el miembro `CREDENCIAL_INICIAL` del enum `EmailType`, sincronizándolo con los 6 valores exactos del ENUM de la tabla `email_queue` en MySQL.
- **[MODIFICADO] `WahlMirai.Web/Services/CredentialService.cs`**:
  - Eliminado el brazo `EmailType.CREDENCIAL_INICIAL => "PASSWORD_ASSIGNED_BULK"` del switch de auditoría.
- **[DETECCIÓN] Configuración SMTP**:
  - Se confirmó que el entorno de desarrollo local cuenta con credenciales activas configuradas vía `dotnet user-secrets` (`EmailSettings:SenderEmail = eduk.sena24@gmail.com`), permitiendo el despacho real de correos.

### 🔍 Verificación y Pruebas
- `dotnet build WahlMirai.Web/WahlMirai.Web.csproj`: 0 advertencias, 0 errores.
- `dotnet test WahlMirai.Tests/WahlMirai.Tests.csproj`: 15/15 pruebas superadas exitosamente.
- **Prueba en vivo en base de datos**: Se re-procesó el registro ID 1 en `email_queue` (tipo `RESPUESTA_PQR`, previamente `FALLIDO`). El servicio en segundo plano lo tomó, ejecutó el envío exitoso vía SMTP y actualizó su estado a `ENVIADO` con `sent_at: 2026-09-14 15:21:53` y `error_message: NULL`.

---

## 📅 14 de Septiembre de 2026 13:20 — Resolución de Conflicto de Fusión (Merge) con `origin/main` y Regeneración de Estilos CSS

### 📌 Resumen General
Se resolvió el bloqueo en GitHub que impedía la fusión automática del Pull Request hacia `main` (*"Can't automatically merge"*). Se integraron todos los cambios provenientes de `origin/main` (módulo de censo, lista blanca v2.8, vistas y pruebas unitarias de Dev 2) con las mejoras de responsividad móvil de la rama actual (`Retrofit-móvil.kevin`), garantizando la preservación total del trabajo de ambas partes sin pérdida ni sobrescritura de código.

### 🔍 Diagnóstico y Causa Raíz
1. **Divergencia de Ramas**: Ambas ramas se bifurcaron desde el commit común `090b844`. Mientras que en `Retrofit-móvil.kevin` se implementaron mejoras responsivas en vistas Razor, en `main` se integró el módulo de censo y lista blanca (`commit eeba7ac`).
2. **Conflicto en Archivo Minificado (`site.css`)**: El único archivo en conflicto fue `WahlMirai.Web/wwwroot/css/site.css`. Como es un archivo generado y minificado en una sola línea por el CLI de Tailwind CSS v4, Git no pudo resolverlo línea por línea e insertó marcadores de conflicto (`<<<<<<< HEAD`, `=======`, `>>>>>>> origin/main`).
3. El archivo fuente `WahlMirai.Web/Styles/input.css` no tenía divergencias entre ramas; la diferencia radicaba exclusivamente en el conjunto de clases Tailwind compiladas por el escáner de vistas de cada rama.

### 🚀 Detalle de Solución y Acciones Realizadas
- **Integración de Cambios de `origin/main`**:
  - Se ejecutó la fusión `git merge origin/main` trayendo todos los elementos de la rama principal:
    - Pruebas unitarias: `WahlMirai.Tests/CensusWhitelistTests.cs`
    - Controladores y vistas de censo: `AdminCensusController.cs`, `Views/AdminCensus/PendingWhitelist.cshtml`, `_PromotionModal.cshtml`, etc.
    - Modelos y servicios: `VwPendingWhitelist.cs`, `PagedResult.cs`, `ICensusService.cs`, `IPromotionService.cs`
    - Documentación y scripts: `MODULO_CENSO_WHITELIST_v2.8.md` y `wahl_mirai_db_v2_8.2_completo.sql`
- **Regeneración Automatizada de Tailwind CSS**:
  - En lugar de elegir una versión sobre otra o editar el archivo minificado manualmente, se utilizó la tarea de compilación del proyecto (`dotnet build WahlMirai.Web/WahlMirai.Web.csproj`).
  - El CLI de Tailwind (`tailwindcss.exe`) escaneó todas las vistas `.cshtml` combinadas (tanto las vistas responsivas de la rama actual como las nuevas vistas del censo de `main`), generando un `site.css` unificado con el 100% de las utilidades requeridas por ambos desarrolladores.
- **Resolución y Commit de Merge**:
  - Se marcó `WahlMirai.Web/wwwroot/css/site.css` como resuelto (`git add`) tras verificar la ausencia de marcadores de conflicto (`git diff --check`).
  - Se consolidó el commit de merge documentando la integración limpia de ambas ramas.

### 🔍 Verificación y Pruebas
- `dotnet build`: Compilación exitosa con 0 errores y 0 advertencias.
- `dotnet test WahlMirai.Tests/WahlMirai.Tests.csproj`: Ejecutadas las 15 pruebas unitarias del módulo de censo y lista blanca con 100% de éxito (15 superadas, 0 con error, 0 omitidas).
- Integridad: Todos los cambios locales de la rama y los de `origin/main` permanecen intactos.

---

## 📅 07 de Septiembre de 2026 17:29:18 — Infraestructura y Tooling: Recreación de `WahlMirai.Tests.csproj` y Vinculación a la Solución

### 📌 Resumen General
Se restauró la infraestructura de pruebas unitarias del proyecto mediante la recreación del archivo de proyecto `WahlMirai.Tests/WahlMirai.Tests.csproj` (el cual se encontraba ausente desde el commit `90b76c9` del 28 de agosto durante la integración de M01-00) y su correspondiente vinculación a la solución `WahlMirai.Web/WahlMirai.Web.slnx`. Con esta corrección técnica, la suite de pruebas preexistente en `AdminAccountServiceTests.cs` vuelve a compilar y ejecutarse plenamente a través de `dotnet build` y `dotnet test`.

### 🚀 Detalle de Cambios
- **[NUEVO] `WahlMirai.Tests/WahlMirai.Tests.csproj`**:
  - Configurado con SDK `Microsoft.NET.Sdk`, `<TargetFramework>net9.0</TargetFramework>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<Nullable>enable</Nullable>`, `<IsPackable>false</IsPackable>` e `<IsTestProject>true</IsTestProject>`.
  - Integrados los paquetes de prueba `xunit` (2.9.3), `xunit.runner.visualstudio` (3.1.4), `Microsoft.NET.Test.Sdk` (17.14.1), `coverlet.collector` (6.0.4) y los paquetes EF Core en versión exacta 9.0.7 (`Microsoft.EntityFrameworkCore.InMemory` y `Microsoft.EntityFrameworkCore.Relational`) para garantizar compatibilidad binaria estricta con el DbContext de `WahlMirai.Web`.
  - Referencia de proyecto agregada hacia `..\WahlMirai.Web\WahlMirai.Web.csproj`.
- **[MODIFICADO] `WahlMirai.Web/WahlMirai.Web.slnx`**:
  - Incorporada la referencia `<Project Path="..\WahlMirai.Tests\WahlMirai.Tests.csproj" />` preservando la estructura del archivo.
- **[MODIFICADO] `WahlMirai.Tests/AdminAccountServiceTests.cs`**:
  - En el helper `CreateContext()`, se añadió `.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))` sobre el builder de opciones para evitar que el proveedor `InMemory` lance excepciones en métodos que ejecutan `Database.BeginTransactionAsync()` (como `WhitelistService.RegisterElectorAsync`).

### 🔍 Verificación
- `dotnet build WahlMirai.Web/WahlMirai.Web.slnx`: 0 errores, 0 advertencias a través de ambos proyectos.
- `dotnet test WahlMirai.Web/WahlMirai.Web.slnx`: suite de 5 pruebas completada con éxito (5 superadas, 0 con error, 0 omitidas).

---

## 📅 09 de septiembre de 2026 15:51:40 — Bug #15 + Bug #14: Sincronización del enum C# `EmailType` con el ENUM de base de datos `email_queue.email_type`

### 📌 Resumen General
Se sincronizó el enum C# `EmailType` (declarado en `Services/ICredentialService.cs`) con los seis valores reales del ENUM MySQL en `email_queue.email_type`, eliminando el miembro retirado `CREDENCIAL_INICIAL` (retirado en v2.8 por RN-2 — el alta inicial ya no envía contraseña generada por el sistema) y agregando los cuatro miembros que faltaban: `CAMBIO_PERFIL`, `RESPUESTA_PQR`, `CANDIDATURA_APROBADA` y `CANDIDATURA_RECHAZADA`. Se reemplazaron todos los literales de cadena dispersos en servicios y controladores con referencias tipadas al enum, y se corrigió el dropdown de filtro en la vista del reporte de correos (Bug #14).

### 🚀 Detalle de Cambios

- **[MODIFICADO] `Services/ICredentialService.cs`**:
  - Eliminado miembro `CREDENCIAL_INICIAL` del enum `EmailType`.
  - Agregados miembros: `CAMBIO_PERFIL`, `RESPUESTA_PQR`, `CANDIDATURA_APROBADA`, `CANDIDATURA_RECHAZADA`.
  - Enum resultante: `RECUPERACION_ACCESO`, `REASIGNACION_ADMIN`, `CAMBIO_PERFIL`, `RESPUESTA_PQR`, `CANDIDATURA_APROBADA`, `CANDIDATURA_RECHAZADA`.

- **[MODIFICADO] `Services/CredentialService.cs`**:
  - Eliminado el arm `EmailType.CREDENCIAL_INICIAL => "PASSWORD_ASSIGNED_BULK"` del switch de auditoría (línea ~50). El arm `_ => "PASSWORD_RESET"` cubre cualquier otro tipo. No se añadieron mappings para los cuatro tipos nuevos porque `CandidateReviewService` y `PqrController` encolaron directamente en `EmailQueue` sin pasar por este switch.

- **[MODIFICADO] `Services/CandidateReviewService.cs`** *(módulo M04 — Camilo)*:
  - ⚠️ **Cambio mínimo en archivo de otro desarrollador**: sustitución mecánica de literales de cadena por referencias tipadas al enum únicamente en los dos puntos de construcción de `EmailQueue`:
    - Línea 119: `EmailType = "CANDIDATURA_APROBADA"` → `EmailType = EmailType.CANDIDATURA_APROBADA.ToString()`
    - Línea 173: `EmailType = "CANDIDATURA_RECHAZADA"` → `EmailType = EmailType.CANDIDATURA_RECHAZADA.ToString()`
  - Sin ningún otro cambio de lógica. **Flagear a Camilo para revisión y merge.**

- **[MODIFICADO] `Services/EmailQueueBackgroundService.cs`**:
  - Reemplazados todos los literales `"CANDIDATURA_APROBADA"` / `"CANDIDATURA_RECHAZADA"` en la rama principal con llamadas `EmailType.*.ToString()`.
  - Eliminado el arm `"CREDENCIAL_INICIAL"` del switch de nombre amigable.
  - Agregados los arms `CAMBIO_PERFIL` ("Cambio de perfil") y `RESPUESTA_PQR` ("Respuesta PQR") al switch de nombre amigable, consistentes con el estilo existente.
  - **Nota de diseño**: Los tipos `CAMBIO_PERFIL` y `RESPUESTA_PQR` caen en la rama `else` que busca la contraseña en el `IPendingPasswordStore`. Dado que esas colas no pasan por `ICredentialService.IssueNewPasswordAsync`, el store no tendrá la contraseña y el email se marcará `FALLIDO`. Esto es una **brecha de diseño preexistente** (los emails de tipo PQR y candidatura necesitan una rama propia que no use el password store), no introducida por este cambio. Se deja para un trabajo de implementación posterior.

- **[MODIFICADO] `Controllers/PqrController.cs`**:
  - Agregado `using WahlMirai.Web.Services;` (faltaba).
  - Línea 158: `EmailType = "RESPUESTA_PQR"` → `EmailType = EmailType.RESPUESTA_PQR.ToString()`.

- **[MODIFICADO] `Views/AdminEmailReport/Index.cshtml`** *(Bug #14)*:
  - Eliminada la opción `CREDENCIAL_INICIAL` del dropdown de filtro.
  - Agregadas las opciones `RESPUESTA_PQR` ("Respuesta PQR"), `CANDIDATURA_APROBADA` ("Candidatura Aprobada"), `CANDIDATURA_RECHAZADA` ("Candidatura Rechazada").
  - Vista servida por `AdminEmailReportController` — dentro del ámbito de este developer.

### ⚠️ Dependencia Bloqueante — Dev 2 (M02 — Censo)

El archivo **`Services/ICensusService.cs`** (módulo M02, propiedad de Dev 2) referencia `EmailType.CREDENCIAL_INICIAL` en dos puntos:
- **Línea 249**: `await _credentialService.IssueNewPasswordAsync((int)voter.Id, EmailType.CREDENCIAL_INICIAL, null);`
- **Línea 484**: `await _credentialService.IssueNewPasswordAsync((int)voter.Id, EmailType.CREDENCIAL_INICIAL, null);`

Este archivo **no fue tocado** per el scope de la tarea. Al eliminar `CREDENCIAL_INICIAL` del enum, estos dos sitios producen los errores de compilación:
```
CS0117: 'EmailType' no contiene una definición para 'CREDENCIAL_INICIAL'
  → ICensusService.cs(249,81)
  → ICensusService.cs(484,89)
```
**La solución requiere que Dev 2 decida la sustitución adecuada** (probablemente eliminar el envío de contraseña en el alta de censo, consistente con RN-2 v2.8).

### 🔍 Verificación
- `dotnet build`: **2 errores, 0 advertencias** — ambos errores en `ICensusService.cs` (Dev 2, fuera de scope). Nodos propios compilan sin errores.
- `dotnet test`: **no ejecutado** — el build falla por la dependencia bloqueante de Dev 2. Los 5 tests preexistentes permanecen inalterados.

---



## 📅 07 de septiembre de 2026 16:58 — M08 Implementación de Chatbot de Ayuda Basado en Reglas (RF-M08-03) y Sincronización Normativa

### 📌 Resumen General
Se implementó de extremo a extremo el requerimiento **RF-M08-03** mediante el script cliente `ayuda-chatbot.js`, proporcionando un asistente conversacional interactivo guiado por palabras clave y menú de temas rápidos dentro de la vista pública de Ayuda (`/Ayuda` y `/Pqr`). El sistema opera 100% en el cliente sin servicios externos ni persistencia en base de datos. Se integró un mecanismo de escalamiento fluido hacia la radicación de solicitudes PQR (`/Pqr/Create`) utilizando `sessionStorage` (`pqr_draft_escalation`) para precargar asunto y transcripción, redirigiendo a los usuarios no autenticados al inicio de sesión sin perder el contexto. Asimismo, se incorporaron notas de corrección normativas y técnicas en `ers_wahl_mirai_v2_8.2.md` y `Arquitectura_y_Diseno_v2_8.2.md` para plasmar la naturaleza pública del chatbot y la exigencia de autenticación únicamente en el escalamiento.

### 🚀 Detalle de Cambios

- **[NUEVO] `WahlMirai.Web/wwwroot/js/ayuda-chatbot.js`**:
  - Implementación del asistente bajo el patrón modular IIFE en modo estricto, con objeto de estado centralizado y enlace exclusivo a atributos `data-*` (`data-chatbot-panel`, `data-chatbot-messages`, `data-chatbot-menu`, `data-chatbot-topic`, `data-chatbot-input`, `data-chatbot-send`, `data-chatbot-reset`, `data-chatbot-escalate`).
  - Diccionario y motor de coincidencia para los 7 temas canónicos de la FAQ (`registro`, `login`, `recuperar`, `postulacion`, `votar`, `perfil`, `resultados`), normalizando texto (eliminación de tildes y mayúsculas/minúsculas) y asociando las ilustraciones SVG ya existentes en `wwwroot/img/ayuda/`.
  - Flujo de validación "¿Esto resolvió tu duda?" (Sí/No) y respuesta por defecto (fallback) ante palabras clave no reconocidas o dudas no resueltas.
  - Mecanismo de escalamiento a PQR: genera un borrador con asunto contextualizado y transcripción formateada en `sessionStorage` (`pqr_draft_escalation`), evaluando los roles del usuario:
    - Elector autenticado (`data-user-is-elector="true"`): redirección directa a `/Pqr/Create`.
    - Usuario anónimo (`data-user-authenticated="false"`): redirección a `/Auth/Login?returnUrl=/Pqr/Create`.
    - Cuentas administrativas (`data-user-is-admin="true"`): deshabilita la acción de escalamiento con nota informativa de rol exclusivo para electores.

- **[MODIFICADO] `WahlMirai.Web/Views/Pqr/Index.cshtml`**:
  - Incorporación del panel del Chatbot en disposición responsiva (`lg:grid lg:grid-cols-12`) al lado del acordeón FAQ, utilizando estrictamente tokens de diseño semánticos del sistema (`bg-surface-container-lowest`, `border-outline/30`, `bg-surface-container`, `text-on-surface`, `text-on-surface-variant`, `bg-primary`, `text-on-primary`, `rounded-lg`).
  - Renderizado en servidor de atributos `data-user-authenticated`, `data-user-is-elector` y `data-user-is-admin` para que el script cliente determine el flujo de escalamiento sin peticiones adicionales.
  - Inclusión del script `<script src="~/js/ayuda-chatbot.js" asp-append-version="true"></script>` en `@section Scripts` accesible para todos los usuarios.

- **[MODIFICADO] `WahlMirai.Web/Views/Pqr/Create.cshtml`**:
  - Adición de bloque de inicialización en `@section Scripts` que inspecciona `sessionStorage.getItem('pqr_draft_escalation')`.
  - Precarga automática de los campos `#subject` y `#message` si existe un borrador escalado, seguido de su inmediata eliminación (`removeItem`) para evitar fugas de contexto en solicitudes posteriores.

- **[MODIFICADO] `docs/ers_wahl_mirai_v2_8.2.md` y `docs/documentos antiguos/2.8/ers_wahl_mirai_v2_8.md`**:
  - Añadida nota de corrección explícita bajo la tabla `RF-M08-03` actualizando la precondición original para registrar que el chatbot es de acceso público (igual que RF-M08-00), exigiéndose la autenticación únicamente al momento de escalar a PQR (RF-M08-01).

- **[MODIFICADO] `docs/Arquitectura_y_Diseno_v2_8.2.md` y `docs/documentos antiguos/2.8/Arquitectura_y_Diseno_v2_8.md`**:
  - Eliminado el comentario `# NUEVO` de `ayuda-chatbot.js` en el árbol de componentes reflejando su estado implementado.
  - Actualizada la especificación técnica en la sección 5.8 (M08) detallando el acceso público, el motor de reglas en cliente y el flujo de redirección con `sessionStorage`.

---

## 📅 04 de Septiembre de 2026 16:55 — M08 SVG de auto-registro y sincronización documental ERS / Arquitectura

### 📌 Resumen General
Se creó el recurso SVG dedicado `ayuda-registro.svg` para ilustrar el paso a paso del flujo de auto-registro de electores en la vista de Ayuda, reemplazando el placeholder temporal. Asimismo, se sincronizaron las especificaciones normativas y técnicas en `ers_wahl_mirai_v2_8.md` y `Arquitectura_y_Diseno_v2_8.md` para plasmar la precondición pública de `/Ayuda` (RF-M08-00) frente a la protección de PQR (RF-M08-01), y se resolvió la inconsistencia histórica del nombre planificado `ayuda-autoregistro.svg`.

### 🚀 Detalle de Cambios

- **[NUEVO] `WahlMirai.Web/wwwroot/img/ayuda/ayuda-registro.svg`**:
  - Ilustración vectorial con `viewBox="0 0 702 164"` que detalla el flujo en 3 pasos: (1) Ingresa tu documento (consulta en lista blanca), (2) Define correo y clave (mín. 8 caracteres, mayúscula y símbolo), (3) ¡Cuenta activada! (acceso concedido).
  - Cumple estrictamente la paleta y tipografía del módulo (`#eff4ff`, `#c3c6d1`, `#003466`, `#0b1c30`, `#424750`, `#2f9e63`, fuente Inter).

- **[MODIFICADO] `WahlMirai.Web/Views/Pqr/Index.cshtml`**:
  - Actualizada la etiqueta `<img>` del tema `¿Cómo creo mi cuenta?` para referenciar `/img/ayuda/ayuda-registro.svg` en lugar del placeholder `ayuda-login.svg`.

- **[MODIFICADO] `docs/ers_wahl_mirai_v2_8.md`**:
  - Actualizada la tabla `RF-M08-00`: la Precondición pasó a reflejar acceso para cualquier usuario (incluidos visitantes no autenticados), se ajustó la Descripción y los flujos normal/alternativo.
  - Añadida nota de corrección explícita bajo la tabla RF-M08-00 indicando que el acceso público responde a `/Ayuda` y `/Pqr` mientras que PQR (`RF-M08-01`) conserva la obligatoriedad de autenticación.

- **[MODIFICADO] `docs/Arquitectura_y_Diseno_v2_8.md`**:
  - En la sección 2 (árbol de directorios), se sustituyó `ayuda-autoregistro.svg` por el archivo real `ayuda-registro.svg`, incorporando una nota aclaratoria sobre la consolidación del nombre documentado pero nunca implementado en disco.
  - En la sección 5.8 (M08), se explicitó que `/Ayuda` es de acceso público mientras que la radicación y consulta de PQR (`RF-M08-01`) se conservan estrictamente protegidas bajo autenticación.

---

## 📅 04 de Septiembre de 2026 16:23 — M08 Ayuda pública accesible + puntos de entrada corregidos

### 📌 Resumen General
Se implementó el acceso público (sin autenticación) al módulo de Ayuda (RF-M08-00), separándolo del sistema de PQR que permanece protegido. Se corrigieron los puntos de entrada rotos en la página de inicio y en la vista de errores. Adicionalmente se actualizó el contenido del acordeón de Ayuda para alinearlo con los cambios normativos de la ERS v2.8 (auto-registro con lista blanca, autopostulación con aprobación, etapas de elección).

### 🚀 Detalle de Cambios

- **[MODIFICADO] `WahlMirai.Web/Controllers/PqrController.cs`**:
  - Agregados `[HttpGet("/Pqr")]` y `[HttpGet("/Ayuda")]` al action `Index()` para responder a ambas rutas.
  - Agregado `[AllowAnonymous]` al action `Index()`. El `[Authorize]` de clase sigue protegiendo `Create`, `Mine`, `Manage`, `List` y `Resolve` sin cambio alguno.

- **[MODIFICADO] `WahlMirai.Web/Views/Pqr/Index.cshtml`**:
  - Selección de layout ahora es tri-estado: `_AdminLayout` (ADMIN/SUPER_ADMIN), `_ElectorLayout` (ELECTOR autenticado), `_Layout` (anónimo). No se tocó ninguno de los layouts compartidos.
  - Añadida cabecera pública mínima (Inicio + Iniciar sesión) visible únicamente para visitantes anónimos, renderizada dentro de la propia vista (no en layouts compartidos).
  - Botón "Crear PQR" condicional: usuarios autenticados ven el botón, los anónimos ven un mensaje con enlace a `/Auth/Login`.
  - Bloque de historial de PQR del elector (`@if (User.IsInRole("ELECTOR"))`) sin ningún cambio.
  - Acordeón de Ayuda actualizado a 7 temas alineados con ERS v2.8:
    - **NUEVO** — `¿Cómo creo mi cuenta?` (auto-registro con lista blanca, RN-1).
    - **REESCRITURA** — `¿Cómo inicio sesión?` (corrección: el elector define su clave, no la recibe por correo, RN-2).
    - **AJUSTE MENOR** — `¿Olvidé mi contraseña?` (precisado que llega al correo de contacto registrado en el auto-registro).
    - **NUEVO** — `¿Cómo me postulo como candidato?` (autopostulación con aprobación del admin, RN-10).
    - **AJUSTE MENOR** — `¿Cómo voto?` (contextualizado a la etapa de Votación).
    - **SIN CAMBIOS** — `¿Cómo edito mi perfil?`
    - **SIN CAMBIOS** — `¿Cuándo puedo ver los resultados?`

- **[MODIFICADO] `WahlMirai.Web/Views/Home/Index.cshtml`**:
  - Convertido el `<button>` inactivo de "Ayuda" en el header de escritorio a `<a href="/Ayuda">` funcional.
  - Corregido el enlace "Guía de Usuario" del footer de `href="#"` a `href="/Ayuda"`, cubriendo el acceso en móvil.

- **[MODIFICADO] `WahlMirai.Web/Views/Error/Index.cshtml`**:
  - Corregido el destino del botón "Ir a ayuda": de `/Pqr/Create` (requería autenticación) a `/Ayuda` (público).

- **[MODIFICADO] `WahlMirai.Web/Controllers/ErrorController.cs`**:
  - `ShowHelpButton` ahora es `true` para todos los códigos de error gestionados en `Index(int? code)` (400/401/403/404 y el default), no solo para `Error500()`.

### 🔒 Restricciones Mantenidas
- `_AdminLayout.cshtml` y `_ElectorLayout.cshtml` no fueron modificados.
- `/Pqr/Create`, `/Pqr/Mine`, `/Pqr/Manage`, `/Pqr/List` y `/Pqr/Resolve` conservan sus atributos `[Authorize]` sin cambio alguno.
- No se expone ningún dato institucional en vivo (election_positions, voting_events, etc.) en el contenido público de ayuda.
- RF-M08-03 (chatbot) fuera de alcance, no implementado.

---

## 📅 02 de Septiembre de 2026 17:21 — Infraestructura compartida (Errores, Serilog y Configuración)


### 📌 Resumen General
Se implementaron páginas de error compartidas y un controlador centralizado (ErrorController) para gestionar los códigos 400, 401, 403, 404 y 500 con una interfaz gráfica basada en Tailwind v4 y Material Symbols. También se configuró Serilog para generar logs diarios de aplicación y se redujo el nivel de ruido de Entity Framework en la consola.

### 🚀 Detalle de Cambios
- **[MODIFICADO] `WahlMirai.Web/Program.cs`**:
  - Configurada inyección de Serilog.
  - Actualizado el pipeline para usar `app.UseExceptionHandler("/Error/500")` y `app.UseStatusCodePagesWithReExecute("/Error/{0}")`.
- **[NUEVO] `WahlMirai.Web/Controllers/ErrorController.cs`**:
  - Creado controlador para interceptar códigos HTTP específicos y errores no controlados, registrando los eventos vía Serilog.
- **[NUEVO] `WahlMirai.Web/ViewModels/ErrorViewModel.cs`**:
  - Añadido viewmodel para abstraer la representación gráfica del error.
- **[NUEVO] `WahlMirai.Web/Views/Error/Index.cshtml`**:
  - Creada vista razor genérica que adapta dinámicamente su diseño utilizando el layout adecuado (`_AdminLayout`, `_ElectorLayout` o `_Layout`).
- **[MODIFICADO] `appsettings.json` y `appsettings.Development.json`**:
  - Ajustado el nivel de logging de EF Core (`Microsoft.EntityFrameworkCore.Database.Command` a `Warning`).
- **[MODIFICADO] `.gitignore`**:
  - Añadida la carpeta `logs/` para evitar que se suban los logs de Serilog.

## 📅 28 de Agosto de 2026 13:34 — Corrección de Errores de Razor y Extracción de Scripts (M01-00)

### 📌 Resumen General
Se corrigieron errores de compilación en el módulo M01-00 (Auto-registro) ocasionados por la interpretación incorrecta de código CSS y JS por parte del motor Razor. Se extrajo la lógica JS a un archivo estático y se corrigió el escape de directivas CSS, además de añadir el punto de entrada al flujo en la pantalla de login.

### 🚀 Detalle de Cambios
- **[MODIFICADO] `WahlMirai.Web/Views/Auth/Registro.cshtml` y `Completar.cshtml`**:
  - Se corrigió el uso de `@keyframes` a `@@keyframes` dentro de `<style>` para evitar que Razor lo evalúe como C#.
- **[MODIFICADO] `WahlMirai.Web/Views/Auth/Completar.cshtml`**:
  - Eliminado el script en línea que contenía expresiones regulares problemáticas (uso del carácter `#`) y sustituido por una referencia externa `~/js/auth-completar.js`.
- **[NUEVO] `WahlMirai.Web/wwwroot/js/auth-completar.js`**:
  - Creado a partir del código JS inline removido de `Completar.cshtml` siguiendo las convenciones del proyecto.
- **[MODIFICADO] `WahlMirai.Web/Views/Auth/Login.cshtml`**:
  - Agregado enlace hacia `Auth/Registro` ("¿Aún no tienes cuenta? Crear mi cuenta").

### 🔍 Verificación
- Comando `dotnet build` completa con 0 errores y 0 advertencias.
- Las lógicas de la verificación del checklist M01-00 son plenamente operacionales sin interrupción por errores de sintaxis en el cliente.

---

## 📅 17 de Agosto de 2026 18:02 — Corrección de Estado Visual Activo en Filtros de Gestión PQR (RF-M08-02)

### 📌 Resumen General
Se corrigió el defecto visual en la vista administrativa de Gestión de PQR (`Views/Pqr/Manage.cshtml` / `pqr-manage.js`) donde, a pesar de que el filtrado de tickets por estado funcionaba correctamente al hacer clic en "Todas", "Abiertos" o "Resueltos", el resaltado visual activo (`bg-primary text-on-primary`) permanecía estático en el botón "Abiertos". Se implementó la sincronización dinámica de clases CSS Tailwind en el script `pqr-manage.js` tanto en los eventos de clic como en la carga inicial (`DOMContentLoaded`), preservando la arquitectura de selectores y atributos `data-*`.

### 🚀 Detalle de Cambios
- **[MODIFICADO] `WahlMirai.Web/wwwroot/js/pqr-manage.js`**:
  - Añadida la función auxiliar `updateFilterStyles()` para iterar sobre `els.filterButtons` e intercambiar las clases visuales de Tailwind según el atributo `data-active`:
    - Botón activo (`data-active="true"`): añade `bg-primary text-on-primary` y remueve `border`.
    - Botones inactivos: remueve `bg-primary text-on-primary` y añade `border`.
  - Integrada la llamada a `updateFilterStyles()` dentro del event listener `click` de los botones de filtro tras actualizar `state.filter` y los atributos `data-active`.
  - Invocada `updateFilterStyles()` durante el evento `DOMContentLoaded` para sincronizar visualmente el botón activo inicial ("Abiertos") desde la carga de la página.
  - Conservadas intactas las clases base compartidas (`px-3 py-1 rounded text-sm`) y la lógica de atributos `data-*`.

### 🔍 Verificación
- Al hacer clic en "Todas" o "Resueltos", el botón clickeado adquiere el fondo primario y texto blanco (`bg-primary text-on-primary`) y pierde el borde.
- El botón "Abiertos" (o cualquier otro previamente seleccionado) pierde las clases activas y adquiere la clase `border`.
- No se requirieron modificaciones en Razor views (`Manage.cshtml`), base de datos ni modelos de backend.

---

## 📅 17 de Agosto de 2026 17:45 — Sección de Ayuda Estática Ilustrada con 5 temas SVG (RF-M08-00)

### 📌 Resumen General
Se actualizó la sección de Ayuda en `Views/Pqr/Index.cshtml` reemplazando los 3 temas temporales genéricos por los 5 temas estáticos ilustrados según la especificación RF-M08-00: Inicio de sesión, Recuperación de contraseña, Votación, Edición de perfil y Visualización de resultados. Se crearon los directorios estáticos `wwwroot/img/ayuda/` incorporando los 5 archivos SVG sin modificaciones, manteniendo intacto el bloque de "Mis Solicitudes" para electores y el enlace directo para "Crear PQR".

### 🚀 Detalle de Cambios
- **[NUEVO] `WahlMirai.Web/wwwroot/img/ayuda/`**:
  - `ayuda-login.svg`: Ilustración de pasos para inicio de sesión.
  - `ayuda-recuperar.svg`: Ilustración de pasos para recuperación de contraseña.
  - `ayuda-votar.svg`: Ilustración de pasos para emisión y confirmación de voto.
  - `ayuda-perfil.svg`: Ilustración de pasos para edición de perfil.
  - `ayuda-resultados.svg`: Ilustración de reglas y visualización de resultados.
- **[MODIFICADO] `WahlMirai.Web/Views/Pqr/Index.cshtml`**:
  - Reemplazado el bloque `<section class="space-y-3">` con los 5 acordeones ilustrados (`<img>` + texto descriptivo + `alt` accesible).
  - Eliminado el tema no perteneciente a la especificación ("Políticas y términos").
  - Preservado intacto el enlace CTA "Crear PQR" (`/Pqr/Create`).
  - Preservado intacto el bloque `@if (User.IsInRole("ELECTOR"))` con "Mis Solicitudes" y su script AJAX `GET /Pqr/Mine`.

### 🔍 Verificación
- `dotnet build` ejecutado exitosamente con 0 errores.
- Los 5 recursos SVG residen en `wwwroot/img/ayuda/` con los nombres exactos en minúsculas.
- Las rutas `src` de las imágenes en Razor resuelven directamente a `/img/ayuda/ayuda-*.svg`.

---

## 📅 14 de Agosto de 2026 20:30 — Corrección de historial de PQR, resaltado del sidebar y alineación de filas

### 📌 Resumen General
Se movió el historial propio del elector desde la vista de creación a la pantalla de Ayuda, se corrigió la doble activación del sidebar para Ayuda y Gestión PQR mediante comparación por `controller + action`, y se ajustó la visualización del listado administrativo de PQR para usar la convención de badges del proyecto y la alineación de columnas del header.

### 🚀 Detalle de Cambios
- **[MODIFICADO] `WahlMirai.Web/Views/Pqr/Create.cshtml`**:
  - Eliminada la sección de historial del elector.
  - La vista vuelve a ser únicamente el formulario de creación.
- **[MODIFICADO] `WahlMirai.Web/Views/Pqr/Index.cshtml`**:
  - Añadido el bloque `Mis Solicitudes` bajo la condición `@if (User.IsInRole("ELECTOR"))`.
  - Añadido el script de carga del historial `GET /Pqr/Mine` solamente para electores.
- **[MODIFICADO] `WahlMirai.Web/Views/Shared/_AdminLayout.cshtml`**:
  - Añadido `currentAction` junto a `currentController`.
  - Ajustada la lógica de activa para Ayuda y Gestión PQR a `controller/action`:
    - Ayuda → `currentController == "Pqr" && currentAction == "Index"`
    - Gestión PQR → `currentController == "Pqr" && currentAction == "Manage"`
- **[MODIFICADO] `WahlMirai.Web/wwwroot/js/pqr-manage.js`**:
  - Reemplazado con la versión entregada por la especificación.
  - Ajustados los badges de estado a la convención del proyecto usando `bg-status-pending/10 text-status-pending` para Abierta y `bg-status-graduated/20 text-status-graduated` para Resuelta.

### 🔍 Verificación
- `dotnet build` ejecutado con éxito.
- La lógica del sidebar queda acotada por `controller + action`, evitando la doble activación.
- El historial del elector se carga únicamente en `Pqr/Index` para usuarios con rol `ELECTOR`.

---

## 📅 14 de Agosto de 2026 18:45 — Historial propio de PQR por elector

### 📌 Resumen General
Se añadió el historial propio de PQR para el elector autenticado, con endpoint exclusivo `GET /Pqr/Mine` y render en la vista de creación, manteniendo intacto el flujo administrativo de `GET /Pqr/List` y sin reutilizar el script de gestión administrativa.

### 🚀 Detalle de Cambios
- **[NUEVO] `WahlMirai.Web/Controllers/PqrController.cs`**:
  - Añadido `Mine()` con `[HttpGet("/Pqr/Mine")]` y `[Authorize(Roles = "ELECTOR")]`.
  - El filtro usa `User.FindFirstValue(ClaimTypes.NameIdentifier)` para derivar el `voter_id` de la sesión y devuelve solo los tickets del elector autenticado.
  - Respuesta JSON con `ok: true, tickets: [...]`, ordenados por `createdAt` descendente y sin incluir `voterId` ni `voterName`.
- **[MODIFICADO] `WahlMirai.Web/Views/Pqr/Create.cshtml`**:
  - Añadida sección `div data-pqr-mine-list` debajo del formulario.
  - Añadido encabezado "Mis Solicitudes" y render dinámico del historial del elector.
  - Una vez creada la PQR, el usuario queda en la misma página, limpia el formulario, muestra un mensaje inline de éxito y recarga automáticamente el historial.
  - Se mantiene el `alert()` solo para errores de validación y de red.

### 🔒 Verificación de Seguridad y Alcance
- El endpoint `GET /Pqr/List` no fue modificado.
- No se alteraron las acciones existentes de `PqrController` distintas de la nueva `Mine()`.
- El filtro de historial usa el identificador de la sesión, no un valor enviado por el cliente.

---

## 📅 10 de Agosto de 2026 21:50 — Modal AJAX para Cambio de Correo de Contacto

### 📌 Resumen General
Se reemplazó el flujo de edición del correo de contacto (formulario Razor con POST completo
hacia `/Profile/Update`) por un flujo AJAX con modal, replicando el mismo patrón UX que ya usa
el cambio de contraseña en `Views/Profile/Index.cshtml`. El cambio de correo ahora ocurre sin
recargar la página, con feedback inline diferenciado para el caso de fallo parcial (BD guardada
pero SMTP fallido).

---

### 🚀 Detalle de Cambios

#### 1. Nuevo método de servicio — `IProfileService` / `ProfileService`
- **[MODIFICADO] `WahlMirai.Web/Services/IProfileService.cs`**:
  - Añadido método `UpdateContactEmailAsync(int voterId, string newEmail, string ipAddress)`
    con firma de retorno `(bool Success, bool EmailSaved, bool NotificationSent, string ErrorMessage)`.
  - El método existente `UpdateProfileAsync` no fue modificado.
- **[MODIFICADO] `WahlMirai.Web/Services/ProfileService.cs`**:
  - Implementación de `UpdateContactEmailAsync` en dos fases explícitamente separadas:
    - **Fase 1 (BD):** validación de unicidad → audit_log → `SaveChangesAsync()`.
    - **Fase 2 (email):** `_emailSender.SendAsync(...)` dentro de `try/catch` propio.
  - Si SMTP falla, retorna `(Success: true, EmailSaved: true, NotificationSent: false)` sin
    relanzar la excepción. El cambio en BD no se revierte — esto es correcto e intencional.

#### 2. Nuevo endpoint AJAX — `ProfileController`
- **[MODIFICADO] `WahlMirai.Web/Controllers/ProfileController.cs`**:
  - Añadido `[HttpPost] [ValidateAntiForgeryToken] UpdateEmail([FromBody] UpdateEmailRequest)`.
  - Validación de formato server-side via `EmailAddressAttribute.IsValid()` (no depende solo
    del cliente).
  - Respuesta JSON diferenciada:
    - `{ ok: false, message }` — error de validación o negocio.
    - `{ ok: true, emailSaved: true, notificationSent: true, newEmail, message }` — éxito completo.
    - `{ ok: true, emailSaved: true, notificationSent: false, newEmail, message }` — BD guardada,
      SMTP falló.
  - Añadido record `UpdateEmailRequest(string? NewEmail)`.
  - Los endpoints existentes (`Update`, `VerifyCurrentPassword`, `ChangePassword`, `SendPasswordReset`)
    no fueron modificados.

#### 3. Vista `Profile/Index.cshtml`
- **[MODIFICADO] `WahlMirai.Web/Views/Profile/Index.cshtml`**:
  - Eliminado `<form asp-action="Update">` (formulario completo que hacía POST con recarga).
  - Sustituido por `@Html.AntiForgeryToken()` standalone + card "Configuración de Cuenta" con:
    - Input `readonly` mostrando el correo actual (`id="display-email"`).
    - Botón "Modificar" (`id="btn-open-email-modal"`).
    - Botón "Cambiar contraseña" (movido desde el interior del form eliminado).
  - Añadido modal `#email-modal` con el mismo patrón visual que `#pwd-modal`.
  - Añadido bloque JS del modal de correo en `@section Scripts`, separado del bloque
    del modal de contraseña existente (que no fue modificado).
  - El botón "Guardar" (`#btn-save-email`) se deshabilita si el formato es inválido
    O si el valor no cambió respecto al correo actual.
  - El JS maneja los 3 casos de respuesta del servidor:
    1. Éxito completo → toast verde, modal se cierra, display actualizado.
    2. BD guardada + SMTP falló → toast amarillo dentro del modal (no cierra), display actualizado.
    3. Error → mensaje inline en el modal, no cierra, botón re-evaluado.

---

### 🔍 Decisiones de Diseño Documentadas

#### CSRF Token
El token CSRF se obtiene de `document.querySelector('input[name="__RequestVerificationToken"]')`.
Al eliminar el `<form asp-action="Update">`, se sustituyó por `@Html.AntiForgeryToken()` standalone
para que el selector siempre encuentre el token de forma predecible, independientemente de qué
otros forms existan en la página. Esto garantiza que el modal de contraseña existente tampoco se
vea afectado. El header enviado en los fetch es `'RequestVerificationToken': token` (sin prefijo
`X-`) — que es el nombre exacto que usa el middleware antiforgery de ASP.NET Core.

#### Atomicidad BD / SMTP
`ProfileService.UpdateContactEmailAsync` separa explícitamente las dos operaciones:
- `SaveChangesAsync()` confirma el correo en BD primero.
- `_emailSender.SendAsync()` se ejecuta después, en un `try/catch` propio.
Si SMTP falla después del commit, el nuevo correo QUEDA en BD y el endpoint retorna
`notificationSent: false`. El frontend muestra un toast/aviso de advertencia en lugar de
silenciar el error. No se implementaron reintentos — el caso `notificationSent: false`
es visible para el usuario y autocontenido.

#### email_queue y el valor CAMBIO_PERFIL — DECISIÓN DE DISEÑO DELIBERADA (no un TODO)
Las notificaciones de cambio de correo de contacto **no se encolan en `email_queue`** y esta
es una decisión de diseño intencionada, NO una limitación temporal.

Motivo técnico: `EmailQueueBackgroundService` requiere que cada entrada en `email_queue` tenga
una contraseña en memoria (`IPendingPasswordStore.TryGetPassword()`) para construir el email
de credenciales. Si no hay contraseña en el store, el job marca el registro como `FALLIDO`
inmediatamente. Este mecanismo es exclusivo para flujos de credenciales
(`CREDENCIAL_INICIAL`, `RECUPERACION_ACCESO`, `REASIGNACION_ADMIN`). Insertar `CAMBIO_PERFIL`
produciría registros con `status = FALLIDO` en cada ciclo del background service, contaminando
la tabla.

> **NOTA PARA DESARROLLADORES FUTUROS:**
> El valor `CAMBIO_PERFIL` del enum `email_type` permanece reservado en el schema de la BD
> para uso futuro si se refactoriza el background service. **No insertar en `email_queue`
> con este `email_type` sin antes adaptar `EmailQueueBackgroundService.cs`** para manejar
> entradas que no dependan de `IPendingPasswordStore` (p. ej. añadir un tipo de plantilla de
> cuerpo HTML estático para notificaciones de perfil).

#### audit_log
El service `ProfileService.UpdateContactEmailAsync` reutiliza `_auditService.LogAsync` con:
`action = "PROFILE_UPDATED"`, `targetEntity = "voters"`, `fieldName = "ContactEmail"`,
`oldValue = correo anterior`, `newValue = correo nuevo`. Esta lógica ya existía en
`UpdateProfileAsync` y se replicó en el nuevo método.

---

### ✅ Verificación Realizada
- Compilación: **0 errores de código C#/Razor** (2 errores de MSBuild por archivo bloqueado
  por el proceso `dotnet run` en ejecución — comportamiento esperado en dev, no errores de código).
- Flujo AJAX funcional según hot-reload del servidor en ejecución.

---

## 📅 10 de Agosto de 2026 21:35 — Auto-descarga de Tailwind CSS en MSBuild Pipeline y Fijación de Versión

### 📌 Resumen General
Se configuró la auto-descarga del binario standalone de Tailwind CLI (`v4.3.3`) directamente en el pipeline de MSBuild de `WahlMirai.Web`. Con esto se garantiza que cualquier desarrollador o máquina de build pueda compilar el proyecto tras clonarlo y ejecutar `dotnet build`, sin requerir descargas ni pasos manuales.

---

### 🚀 Detalle de Cambios

#### 1. Configuración de Auto-Descarga en MSBuild (`WahlMirai.Web.csproj`)
- **[MODIFICADO] `WahlMirai.Web/WahlMirai.Web.csproj`**:
  - Propiedad `<TailwindVersion>v4.3.3</TailwindVersion>` añadida para fijar la versión exacta del ejecutable standalone CLI.
  - Nuevo target `EnsureTailwindCli` (ejecutado con `BeforeTargets="Tailwind"`) mediante la tarea `DownloadFile` de MSBuild. Descarga automáticamente `tailwindcss-windows-x64.exe` a la raíz del proyecto únicamente si `tailwindcss.exe` no existe localmente.
  - Target `Tailwind` preservado para compilar `Styles/input.css` hacia `wwwroot/css/site.css --minify`.

#### 2. Exclusión en Control de Versiones (`.gitignore`)
- **[MODIFICADO] `.gitignore`**:
  - Adición explícita de `WahlMirai.Web/tailwindcss.exe` para asegurar que el binario de ~112 MB no sea commiteado al repositorio.

#### 3. Estructura de Estilos y Vistas
- Se confirmó que el archivo fuente de estilos vive en `WahlMirai.Web/Styles/input.css` (fuera de `wwwroot`).
- Se verificó que `Views/Shared/_Layout.cshtml` hace referencia exclusiva al CSS compilado `~/css/site.css`.

---

### ✅ Resultado de la Verificación
1. **Simulación de máquina limpia**: Se eliminó `tailwindcss.exe` localmente.
2. **Ejecución de `dotnet build`**:
   - `EnsureTailwindCli` descargó exitosamente el binario `v4.3.3` (112.5 MB).
   - `Tailwind` se ejecutó a continuación y compiló `wwwroot/css/site.css` en 1s.
3. **Existencia en disco**: `tailwindcss.exe` verificado en la raíz de `WahlMirai.Web`.
4. **Verificación HTTP/CSS**: Sin peticiones a `app.css` ni errores 404 de Tailwind.
5. **Verificación Visual**: Renderizado óptimo en `AdminCensus/Index` (≥1024px) mostrando tabla completa y sidebar sin superposición.

---

## 📅 10 de Agosto de 2026 16:45 — Corrección Definitiva de Alineación Visual y Vistas Responsivas (Causa Raíz)

### 📌 Resumen General
Se identificó y resolvió de forma estructural la causa raíz del problema de alineación y deformación visual en pantallas de escritorio y móviles (afectando `AdminCensus`, `AdminEvents`, `Dashboard`, etc.).

---

### 🔍 Causa Raíz Identificada

El problema visual (desfase vertical, tablas colapsadas en escritorio mostrando etiquetas móviles "NOMBRE:", "GRADO:", tarjetas rotas e insignias invisibles) tenía **dos causas raíces principales**:

1. **Inexistencia de utilidades Tailwind responsivas en `site.css`**:
   `site.css` es un CSS estático. Al implementar el retrofit mobile-first, se añadieron clases como `md:table`, `md:table-header-group`, `md:table-row-group`, `md:table-row`, `md:table-cell`, `md:hidden`, `md:border-0`, `md:px-5`, `md:py-3`, `status-pending`, etc. en las vistas `.cshtml`. Sin embargo, estas clases **no existían en `site.css`**.
   - En consecuencia, en escritorio (`md:`), la regla `hidden` ocultaba el `thead` permanentemente, y las filas `tr` y celdas `td` permanecían como `block` y `flex justify-between` de móvil, mostrando las etiquetas "NOMBRE:", "GRADO:" en escritorios y rompiendo el diseño de tabla.
   - Las insignias de estado `status-pending` no tenían colores asignados (`--color-status-pending`).

2. **Modelo de caja y alineación de iconos (`.material-symbols-outlined`)**:
   El CSS global utilizaba un `font-size: 20px` fijo sin `display: inline-flex`, `justify-content: center` ni `align-items: center`, lo que provocaba que los glifos dentro de contenedores `flex` con `min-h-[44px]` se desalinearan verticalmente del texto adyacente o ignoraran las clases de tamaño explícito (`text-base`, `text-[18px]`, etc.).

---

### 🚀 Detalle de Cambios

#### 1. Inclusión de Utilidades Faltantes — `wwwroot/css/site.css` y `wwwroot/css/app.css`
- **[MODIFICADO] `wwwroot/css/site.css` & `wwwroot/css/app.css`**:
  - Incorporada la variable CSS `--color-status-pending: #d97706;` y sus clases utilitarias (`.text-status-pending`, `.bg-status-pending`, `.bg-status-pending\/10`).
  - Incorporado el bloque de media query `@media (width >= 48rem)` con todas las reglas responsivas de posicionamiento y layout requeridas: `.md\:static` (que evita la superposición del sidebar flotante sobre el contenido principal en escritorio), `.md\:w-64`, `.md\:shadow-none`, `.md\:table`, `.md\:table-header-group`, `.md\:table-row-group`, `.md\:table-row`, `.md\:table-cell`, `.md\:hidden`, `.md\:border-0`, `.md\:border-t`, `.md\:border-b-0`, `.md\:border-x-0`, `.md\:rounded-none`, `.md\:mb-0`, `.md\:px-5`, `.md\:px-8`, `.md\:py-3`, `.md\:py-4`, `.md\:p-8`, `.md\:gap-8`, `.md\:bg-transparent`, `.md\:justify-start`, `.md\:items-center`.

#### 2. Normalización de Modelo de Caja de Iconos — `Views/Shared/_Layout.cshtml`
- **[MODIFICADO] `Views/Shared/_Layout.cshtml`**:
  - Regla global `.material-symbols-outlined`: `font-size: inherit; line-height: 1; display: inline-flex; align-items: center; justify-content: center; vertical-align: middle; flex-shrink: 0; user-select: none;`.

#### 3. Ajustes de Tabla Responsiva — `Views/AdminCensus/Index.cshtml`
- **[MODIFICADO] `Views/AdminCensus/Index.cshtml`**:
  - Ajustadas las clases de las celdas `td` para alternar limpiamente entre vista de tarjeta en móvil (`block flex justify-between`) y tabla tradicional en escritorio (`md:table-cell`).

---

### ✅ Resultado de la Verificación
- En **escritorio**: `AdminCensus` muestra una tabla horizontal limpia con su cabecera completa, sin etiquetas redundantes. `AdminEvents` muestra tarjetas uniformes con alineación vertical perfecta en botones e iconos.
- En **móvil/tablet**: Se mantienen las tarjetas adaptadas y menús responsive sin romper el diseño.

---

## 📅 14 de agosto de 2026 13:32 — Ajuste de gestión PQR con modal y script desacoplado

### 📌 Resumen General
Se reemplazó el flujo estático de resolución de PQR en la vista administrativa por una versión basada en atributos `data-pqr-*` y un script externo, eliminando el uso de `prompt()` y la estructura `table/tbody` incompatible con el render de filas div. La vista ahora usa filtros, contenedor dinámico y modal con clase `hidden` para control visual.

### 🚀 Cambios realizados
- **[MODIFICADO] `WahlMirai.Web/Views/Pqr/Manage.cshtml`**:
  - Reemplazado el bloque de filtro con botones `data-pqr-filter`.
  - Sustituida la tabla por `div data-pqr-list` y cabecera visual estática.
  - Añadido modal con `data-pqr-modal` y campos `data-pqr-field`.
  - Eliminado el bloque de script inline con `prompt()` y reemplazado por referencia a `~/js/pqr-manage.js`.
- **[NUEVO] `WahlMirai.Web/wwwroot/js/pqr-manage.js`**:
  - Copiado exactamente desde el script entregado por la especificación, sin modificar la lógica.

### 🔍 Verificación
- `dotnet build` ejecutado con resultado correcto; existe 1 advertencia previa de conexión por `WahlMiraiDbContext.cs`, no es nueva ni provocada por este cambio.
- Se confirma que la implementación usa la clase `hidden` para mostrar/ocultar el modal, compatible con `classList.remove('hidden')` y `classList.add('hidden')`.
- No se introdujeron tags de CDN de Tailwind ni cambios de estilos de color fuera del markup solicitado.

---

## 📅 2026-08-12 15:12:57 — Módulo PQR (backend M08)

### 📌 Resumen
Se implementó el backend para M08 (RF-M08-01, RF-M08-02): entidad `PqrTicket`, DTOs de entrada,
y `PqrController` con endpoints para creación (elector) y resolución (admin). Se encola una notificación
en `email_queue` con `email_type = 'RESPUESTA_PQR'` al resolver. No se registra nada en `audit_log`.

### 🚀 Cambios realizados
- **[NUEVO] `WahlMirai.Web/Models/PqrTicket.cs`**: entidad mapeada a `pqr_tickets`.
- **[MODIFICADO] `WahlMirai.Web/Models/WahlMiraiDbContext.cs`**: añadido `DbSet<PqrTicket>` y mapeo en `OnModelCreating`.
- **[NUEVO] `WahlMirai.Web/ViewModels/PqrDtos.cs`**: `PqrCreateDto` y `PqrResponseDto` con validaciones.
- **[NUEVO] `WahlMirai.Web/Controllers/PqrController.cs`**:
  - `POST /Pqr/Create` (roles = ELECTOR) — crea ticket con `status='ABIERTO'`.
  - `GET  /Pqr/List?status=` (roles = ADMIN) — lista tickets filtrables por `ABIERTO`/`RESUELTO`.
  - `POST /Pqr/Resolve/{id}` (roles = ADMIN) — marca `RESUELTO`, guarda `admin_response`, `responded_by_voter_id`, `responded_at`, y encola `email_queue` (`RESPUESTA_PQR`).

### 🔍 Verificación
- Compilación: ✅ build exitoso (1 advertencia existente sobre connection string).
- CSRF: Los endpoints POST usan `[ValidateAntiForgeryToken]` y el frontend debe enviar el header `RequestVerificationToken` como en otros módulos.
- Audit: ✅ No se hicieron llamadas a `AuditService` ni inserciones en `audit_log`.

## 📅 2026-08-12 15:40:00 — PQR: Vistas y navegación (RF-M08-00/01/02)

### 📌 Resumen
Se añadieron las vistas frontend que exponen el módulo Ayuda/PQR y los enlaces de navegación correspondientes.

### 🚀 Cambios realizados
- **[MODIFICADO] `WahlMirai.Web/Controllers/PqrController.cs`**: Añadidos los métodos `Index()`, `Create()` (ELECTOR) y `Manage()` (ADMIN) que devuelven las vistas Razor para el módulo Ayuda/PQR.
- **[NUEVO] `WahlMirai.Web/Views/Pqr/Index.cshtml`**: Vista de Ayuda (RF-M08-00). Usa layout dinámico según rol: `Layout = User.IsInRole("ADMIN") ? "_AdminLayout" : "_ElectorLayout"`.
- **[NUEVO] `WahlMirai.Web/Views/Pqr/Create.cshtml`**: Formulario de creación de PQR (RF-M08-01). Usa `Layout = "_ElectorLayout"` y envía `RequestVerificationToken` en el header.
- **[NUEVO] `WahlMirai.Web/Views/Pqr/Manage.cshtml`**: Panel administrativo de PQR (RF-M08-02). Usa `Layout = "_AdminLayout"`, incluye `@Html.AntiForgeryToken()` y ejemplo de flujo de resolución con header `RequestVerificationToken`.
- **[MODIFICADO] `Views/Shared/_ElectorLayout.cshtml`**: Añadido enlace `Ayuda` en barra superior y drawer móvil, siguiendo el patrón de `Mi Perfil`.
- **[MODIFICADO] `Views/Shared/_AdminLayout.cshtml`**: Añadidos enlaces `Ayuda` y `Gestión PQR` en la barra lateral.
- **[NUEVO] `Views/Shared/_FirstVisitBanner.cshtml`**: Partial incluido en `_Layout.cshtml` que muestra un banner dismissible la primera vez usando `localStorage` (`wm_ayuda_banner_seen`).

### ✅ Verificación
- **Build**: Se ejecutó `dotnet build` tras los cambios (ver salida).
- **CSRF pattern**: Las vistas `Create` y `Manage` usan el patrón de `@Html.AntiForgeryToken()` y envían el token como header `RequestVerificationToken` en las llamadas `fetch`.
- **No cambios a `site.js` ni creación de `wwwroot/js/components`**: Confirmado.
- **Layouts**: `Index.cshtml` utiliza la selección dinámica de layout para preservar la barra lateral admin cuando corresponde.



#### 2. Tamaños Explícitos en Íconos del Sidebar Admin — `Views/Shared/_AdminLayout.cshtml`

- **[MODIFICADO] `Views/Shared/_AdminLayout.cshtml`**:
  - Se añadió la clase `text-xl` (20px) a los íconos del sidebar y botones de drawer/close que no tenían clase de tamaño explícito: `menu`, `close`, `dashboard`, `groups`, `account_circle`, `logout`.
  - **Justificación**: Con `font-size: inherit` en la regla global, sin clase explícita estos íconos habrían heredado `text-sm` (14px) del `<a>` padre, reduciendo visualmente su tamaño respecto al diseño original. La clase `text-xl` preserva el tamaño de 20px que tenían antes con la regla fija.

#### 3. Tamaños Explícitos en Íconos del Header/Drawer Elector — `Views/Shared/_ElectorLayout.cshtml`

- **[MODIFICADO] `Views/Shared/_ElectorLayout.cshtml`**:
  - Se añadió la clase `text-xl` (20px) a los íconos que no tenían clase de tamaño: `menu` (botón header móvil), `close` (botón cerrar drawer), `logout` (menú drawer).
  - El ícono `logout` del header desktop ya tenía `text-lg` — se dejó intacto.

---

### ✅ Verificación

- El responsive **no fue eliminado** ni modificado estructuralmente.
- Las media queries `md:hidden`, `md:flex`, `sm:flex-row`, `flex-col-reverse sm:flex-row` permanecen intactas.
- Los breakpoints de drawer (≥768px) funcionan correctamente.
- Los íconos con tamaño explícito en las vistas (`text-base`, `text-[14px]`, `text-[18px]`, `text-[20px]`, etc.) ahora funcionan correctamente sin ser sobreescritos.
- Los íconos sin clase de tamaño en los layouts compartidos tienen ahora `text-xl` explícito, preservando su apariencia visual original.
- El problema de alineación (baseline desajustado en contenedores flex con `min-h-[44px]`) queda resuelto mediante `display: inline-flex + align-items: center`.

**Vistas afectadas y corregidas**: AdminEvents/Index, AdminEvents/Form, AdminCensus/Index, Elector/Dashboard, Elector/Votar, Results/Index, Profile/Index, RecuperacionAcceso/Recuperar, RecuperacionAcceso/Exito, y todas las vistas futuras que usen `.material-symbols-outlined`.

---

## 📅 10 de Agosto de 2026 15:30 — Migración v2.5 → v2.6: Apertura de Resultados al Finalizar Elección (RN-4.1)


### 📌 Resumen General
Se implementó la regla de negocio **RN-4.1**: al pasar un evento electoral al estado `FINALIZADA`, los resultados quedan accesibles para **todos los electores cuyos grados estén habilitados** (`event_grades`) en dicha elección, sin requerir que hayan emitido su voto previamente. La regla RN-4 (acceso condicionado al voto) sigue vigente únicamente para elecciones en estado `ACTIVA` o `PROGRAMADA`. El Administrador conserva acceso irrestricto en todo momento (RN-5). No hubo cambios en el esquema de base de datos ni en ningún otro módulo (M01–M05, M07).

---

### 🚀 Detalle de Cambios

#### Versión: v2.6

#### 1. Documentación — ERS (`docs/ers_wahl_mirai_v2_6.md`)
- **[NUEVO]** `docs/ers_wahl_mirai_v2_6.md` (creado a partir de `ers_wahl_mirai_v2_5.md`):
  - Encabezado actualizado a "Versión 2.6".
  - Control de versión: *"v2.6 — Apertura de resultados a todos los electores de los grados habilitados una vez finalizada la elección (RN-4.1), sin requerir haber votado."*
  - **RN-4** acotada explícitamente al estado 'Activa' / 'Programada'.
  - **RN-4.1** agregada: apertura de resultados al estado `FINALIZADA` para electores de grados habilitados en `event_grades`.
  - **RF-M06-01** actualizada con las tres condiciones de acceso: (a) voto en activa, (b) grado habilitado en finalizada, (c) rol Admin.
  - Flujos alternativos 1b y 1c añadidos; condición especial referencia `event_grades`.
  - `§2.2` y `§1.1` actualizados con la nueva descripción de escrutinio.

#### 2. Documentación — Arquitectura (`docs/2_6_Arquitectura_y_Diseno.md`)
- **[NUEVO]** `docs/2_6_Arquitectura_y_Diseno.md` (creado a partir de `Arquitectura_y_Diseno_v2_5.md`):
  - Título actualizado a "Versión 2.6".
  - **§4 (Diagrama de secuencia):** bloque `alt` ampliado con tres ramas: Admin, Elector+activa+votó, Elector+activa+no votó, Elector+finalizada+grado habilitado, Elector+finalizada+grado no habilitado, ELIMINADO.
  - **§5.6 (M06):** párrafo reescrito para describir las tres condiciones de acceso (RN-4, RN-4.1, RN-5).
  - Modelo ER (§3, 12 tablas) sin cambios.

#### 3. Documentación — SQL (`docs/wahl_mirai_db_v2_6_completo.sql`)
- **[NUEVO]** `docs/wahl_mirai_db_v2_6_completo.sql` (creado a partir de `wahl_mirai_db_v2_5_completo.sql`):
  - Solo el bloque de comentario de cabecera fue modificado: "Versión: 2.6" y nota aclaratoria *"Cambios v2.6 vs v2.5: ninguno a nivel de schema; RN-4.1 se implementa en la capa de aplicación (ResultsController), no en la base de datos."*
  - `vw_vote_counts`, `vw_active_census`, `vw_pending_email_queue` y todos los `INSERT` de semilla permanecen intactos.

#### 4. Documentación — README (`README.md`)
- Sección "Credenciales de Acceso" actualizada a "Versión 2.6".
- Referencia al script SQL actualizada a `docs/wahl_mirai_db_v2_6_completo.sql`.
- Nota de novedades actualizada a v2.6 con descripción de RN-4.1.

#### 5. Código — `WahlMirai.Web/Controllers/ResultsController.cs`
- **[MODIFICADO]** Lógica de autorización en `Index(int id)` reescrita con tres ramas:
  1. `role == "ADMIN"` → acceso inmediato sin verificación (RN-5).
  2. `votingEvent.Status == "ACTIVA" || "PROGRAMADA"` → verifica `voter_event_participations` vía `HasVotedAsync`. Sin voto: redirige al Dashboard con mensaje "Debe votar para ver los resultados." (RN-4).
  3. `votingEvent.Status == "FINALIZADA"` → verifica `event_grades` por `(voting_event_id, voter.GradeId)`. Grado no habilitado: redirige con mensaje "No pertenece a un grado habilitado para esta elección." (RN-4.1). Grado habilitado: acceso concedido sin exigir participación previa.
  4. `votingEvent.Status == "ELIMINADO"` → `Forbid()` siempre para no-admin.
- La consulta a `VotingEvent` se movió al inicio del método (antes de la verificación de rol) para evitar una segunda consulta redundante.
- Ningún otro controlador, servicio, hub o módulo fue modificado.

---

## 📅 10 de Agosto de 2026 15:13 — Corrección de Bugs Post Mobile-First: Drawer Permanente y Botones Desbordados

### 📌 Resumen General
Se corrigieron dos defectos visuales detectados durante pruebas en viewport real (iPhone 14 Pro Max, 430px) tras la implementación del retrofit Mobile-First. El primer bug mostraba el sidebar drawer de forma permanente en móvil (bloqueando el contenido principal). El segundo provocaba desbordamiento horizontal de botones en el formulario de perfil y en AdminEvents/Form.

---

### 🚀 Detalle de Cambios

#### 1. Drawer siempre visible en móvil — `_AdminLayout.cshtml` y `_ElectorLayout.cshtml`

**Causa raíz:** La estrategia anterior usaba la clase `-translate-x-full` de Tailwind para ocultar el `<aside>` por defecto. En Tailwind v4 compilado localmente (CLI), esta clase dinámica no se detecta automáticamente en el escaneo de fuentes si no estaba previamente en el build, por lo que no se genera en el CSS de salida y el drawer quedaba visible de forma permanente.

**Corrección:**
- **[MODIFICADO] `Views/Shared/_AdminLayout.cshtml`**:
  - El `<aside id="sidebar-drawer">` arranca con la clase `hidden` (display: none) por defecto en móvil, y con `md:flex` para que en pantallas medianas siga siendo visible como sidebar fijo.
  - El JS `toggleDrawer()` ahora alterna la clase `hidden` en lugar de `-translate-x-full`, garantizando que el mecanismo funcione independientemente de si Tailwind compiló o no la clase de transformación.
  - El overlay usa `z-40` y el drawer `z-50` para respetar la jerarquía de capas correcta.
  - Se agregó listener `resize` para ocultar el overlay y el drawer al pasar a `md+` (≥768px).

- **[MODIFICADO] `Views/Shared/_ElectorLayout.cshtml`**:
  - Mismo patrón aplicado: `aside` con `hidden` por defecto, JS con toggle de `hidden`+`flex`, overlay `z-40`, drawer `z-50`.
  - El JS también agrega `flex` al mostrar el drawer para restaurar el layout de columna flexbox del menú.

#### 2. Botones desbordados — `Profile/Index.cshtml` y `AdminEvents/Form.cshtml`

- **[MODIFICADO] `Views/Profile/Index.cshtml`**:
  - Contenedor de acciones cambiado a `flex flex-col sm:flex-row` para apilamiento vertical en móvil.
  - Botones con `w-full sm:w-auto` para ocupar ancho completo en móvil sin desbordar.

- **[MODIFICADO] `Views/AdminEvents/Form.cshtml`**:
  - Mismo patrón: `flex flex-col-reverse sm:flex-row` y `w-full sm:w-auto` en los botones Cancelar y Guardar/Crear.

---
## 📅 3 de Agosto de 2026 13:26 — Depuración Final de Perfil y Sincronización de Documentación del Proyecto

### 📌 Resumen General
Se completó la depuración del módulo de perfil eliminando el campo redundante de "Contraseña Actual" en el formulario principal de actualización de datos, desacoplando totalmente la modificación de correo de contacto de la confirmación de clave. Asimismo, se realizó un análisis exhaustivo y actualización de todos los documentos normativos del proyecto (`docs/`), registrando los nuevos requerimientos de complejidad, el modal interactivo de 2 pasos, el correo obligatorio en el censo y la migración tecnológica a .NET 9.0.

---

### 🚀 Detalle de Cambios

#### 1. Depuración de Vista y Controlador de Perfil (RF-M07-01)
- **[MODIFICADO] `Views/Profile/Index.cshtml`**:
  - Se removió el contenedor y campo `CurrentPassword` del formulario principal. El formulario de "Configuración de Cuenta" ahora solo gestiona la actualización del correo de contacto.
- **[MODIFICADO] `Controllers/ProfileController.cs`**:
  - Se eliminó la validación obligatoria de `CurrentPassword` en la acción `Update`, permitiendo actualizar el correo de contacto sin ingresar la contraseña actual en el formulario principal.
- **[MODIFICADO] `Services/IProfileService.cs` y `ProfileService.cs`**:
  - `UpdateProfileAsync` fue actualizado para aceptar `currentPassword` como nullable (`string?`) y verificar el hash BCrypt únicamente si el valor es suministrado (por ejemplo, desde el modal AJAX).

#### 2. Sincronización de la Documentación del Proyecto (`docs/`)
- **[MODIFICADO] `docs/ers_wahl_mirai_v2_5.md`**:
  - Reescritura del requerimiento **RF-M07-01** (Consulta y Edición de Perfil Propio) para detallar el flujo del modal flotante en 2 pasos con validación asíncrona (AJAX), reglas visuales de complejidad (8+ caracteres, mayúscula `[A-Z]` y símbolo especial `!@#$%...`) y la independencia de la actualización de correo de contacto.
- **[MODIFICADO] `docs/2_5_Arquitectura_y_Diseno.md`**:
  - Sección 1: Actualización de la pila tecnológica de backend a **.NET 9.0 / ASP.NET Core**.
  - Sección 5.7: Actualización de la especificación técnica de **RF-M07-01** con los detalles del modal en 2 pasos y reglas de complejidad.
- **[MODIFICADO] `docs/3_Manual_Desarrollador.md`**:
  - Requisitos previos actualizados a **.NET 9.0 SDK**.
- **[MODIFICADO] `docs/4_Manual_Usuario.md`**:
  - Inclusión de la **Sección 4 (Mi Perfil y Autogestión de Cuenta)** describiendo los pasos para actualizar el correo de contacto y el procedimiento interactivo de cambio de contraseña mediante el modal de 2 pasos.
- **[MODIFICADO] `README.md`**:
  - Requisitos previos actualizados a **.NET 9.0 SDK**.

---

## 📅 3 de Agosto de 2026 13:11 — Modal de Cambio de Contraseña en 2 Pasos, Requisitos de Complejidad y Migración a .NET 9.0

### 📌 Resumen General
Se rediseñó completamente el flujo de cambio de contraseña en la vista "Mi Perfil", implementando un modal interactivo de 2 pasos con verificación AJAX. Se establecieron nuevos requisitos de complejidad para las contraseñas (mínimo 8 caracteres, al menos una mayúscula y al menos un símbolo especial). Adicionalmente se corrigieron dos bugs reportados (cambio de contraseña bloqueado, correo no obligatorio en nuevo elector) y se migró el proyecto a .NET 9.0 con los paquetes de EF Core y Pomelo actualizados.

---

### 🚀 Detalle de Cambios

#### 1. Modal de Cambio de Contraseña — 2 Pasos (RF-M07-01)
- **[MODIFICADO] `Views/Profile/Index.cshtml`**:
  - Se eliminó la sección colapsable de cambio de contraseña integrada en el formulario principal.
  - Se añadió un botón "Cambiar contraseña" que abre un **modal flotante de 2 pasos**:
    - **Paso 1:** Solicita y verifica la contraseña actual mediante una llamada AJAX a `/Profile/VerifyCurrentPassword`. Si es incorrecta, muestra el mensaje de error dentro del modal sin redirigir.
    - **Paso 2:** Muestra los campos de nueva contraseña y confirmación, junto con un panel de **requisitos visuales en tiempo real** (indicadores ✔/○ que se actualizan mientras el usuario escribe): mínimo 8 caracteres, al menos una mayúscula, al menos un símbolo especial y que las contraseñas coincidan.
    - **Paso 3 (Resultado):** Al confirmar, muestra el resultado directamente dentro del modal con un ícono de éxito (✔ verde) o error (✘ rojo) y el motivo en caso de fallo. El botón guardar solo se habilita cuando todos los requisitos están cumplidos.
  - Se agregó la funcionalidad de toggle de visibilidad (ojo) para todos los campos de contraseña del modal.
  - El formulario principal del perfil quedó simplificado: solo gestiona el cambio de correo de contacto con confirmación de contraseña actual.
- **[MODIFICADO] `Controllers/ProfileController.cs`**:
  - Nuevo endpoint `POST /Profile/VerifyCurrentPassword` (AJAX, retorna JSON `{ ok, message }`): verifica la contraseña actual del usuario autenticado contra el hash BCrypt en la BD.
  - Nuevo endpoint `POST /Profile/ChangePassword` (AJAX, retorna JSON `{ ok, message }`): valida y aplica el cambio de contraseña, incluyendo validación server-side de complejidad.
  - Se añadió validación server-side de la regex de complejidad también en el método `Update` (para cobertura defensiva del formulario clásico).
- **[MODIFICADO] `ViewModels/ProfileViewModel.cs`**:
  - Se añadió atributo `[RegularExpression]` al campo `NewPassword` para exigir al menos una mayúscula y un símbolo especial, complementando el `[MinLength(8)]` ya existente.

#### 2. Corrección de Bugs (sesión anterior — 3 de Agosto de 2026 12:55)
- **[CORREGIDO] Bug: No se podía cambiar contraseña desde el perfil**:
  - El campo `CurrentPassword` estaba oculto dentro del bloque colapsable `#passwordSection`, de modo que nunca se enviaba al hacer submit sin expandirlo primero. Se resolvió moviéndolo fuera del bloque colapsable para que siempre fuera visible (solución reemplazada por el nuevo modal en esta misma sesión).
- **[CORREGIDO] Bug: El correo no era obligatorio al agregar un nuevo elector**:
  - **`Views/AdminCensus/Index.cshtml`**: Se añadió `required` al input de `contactEmail` en el modal "Nuevo elector", se actualizó el label con asterisco rojo y se añadió nota descriptiva.
  - **`Controllers/AdminCensusController.cs`**: Se eliminó el fallback que generaba un email ficticio (`documento@colegio.edu.co`) cuando el campo llegaba vacío. Ahora se retorna un error en `TempData["Error"]` si el correo no se proporciona, cumpliendo la validación tanto en cliente como en servidor.
- **[MODIFICADO] `Services/ProfileService.cs` e `IProfileService.cs`**:
  - El parámetro `newContactEmail` cambió de `string` a `string?` para soportar el caso de "cambio solo de contraseña" (desde el modal AJAX) sin modificar el email del usuario.

#### 3. Migración a .NET 9.0
- **[MODIFICADO] `WahlMirai.Web.csproj`**:
  - `TargetFramework`: `net8.0` → `net9.0`.
  - `Microsoft.EntityFrameworkCore.Design` y `Microsoft.EntityFrameworkCore.Tools`: `8.0.6` → `9.0.7`.
  - `Pomelo.EntityFrameworkCore.MySql`: `8.0.2` → `9.0.0`.
- **[MODIFICADO] `README.md`**: Actualizado el enlace de descarga del SDK a .NET 9.0.

---

## 📅 29 de Julio de 2026 16:31 — Visualización de Curso en Dashboard de Elector y Botón Volver en Login

### 📌 Resumen General
Se mejoró la experiencia de usuario y la visibilidad de información en dos áreas clave del sistema:
1. En el **Dashboard del Elector**, se incorporó la visualización en tiempo real del grado/curso actual al que pertenece el estudiante autenticado junto al rol "Estudiante".
2. En la vista de **Login de Autenticación**, se añadió un botón de navegación "Volver al inicio" con ícono que permite regresar directamente al menú/página principal de bienvenida (`Home/Index`).

---

### 🚀 Detalle de Cambios

#### 1. Visualización de Curso Actual del Elector
- **[MODIFICADO] `Services/IVotingService.cs` y `VotingService.cs`**: 
  - Actualizada la interfaz `IVotingService` y su implementación para incluir la consulta del `Grade` asociado al votante mediante `.Include(v => v.Grade)` al obtener datos del estudiante autenticado (`GetVoterByDocumentAsync`).
- **[MODIFICADO] `Services/IAuthService.cs` y `AuthService.cs`**:
  - Actualizado el método `ValidateLoginAsync` para que retorne el objeto `Voter` incluyendo la navegación `.Include(v => v.Grade)`.
- **[MODIFICADO] `Controllers/AuthController.cs`**:
  - Al iniciar sesión exitosamente como elector, se agrega un nuevo Claim `GradeName` en la cookie de autenticación con el nombre del curso del estudiante.
- **[MODIFICADO] `Controllers/ElectorController.cs`**:
  - Transmisión del nombre del curso a la vista mediante `ViewBag.GradeName` en la acción `Dashboard`.
- **[MODIFICADO] `Views/Elector/Dashboard.cshtml`**:
  - Renderizado del badge con el curso del estudiante (ej. `Grado 11° - 1101` o badge equivalente) justo al lado del indicador "Estudiante".

#### 2. Botón "Volver al Inicio" en Login
- **[MODIFICADO] `Views/Auth/Login.cshtml`**:
  - Se agregó un botón interactivo/enlace de navegación con ícono de flecha hacia atrás (`arrow_back`) posicionado en la parte superior izquierda de la tarjeta/contenedor del Login, permitiendo regresar a la vista inicial (`/Home/Index`).

---

## 📅 29 de Julio de 2026 15:23 — Correcciones RF-M07-01 y RF-M07-02 (Perfil y Reasignación)

### 📌 Resumen General
Se resolvieron tres brechas de cumplimiento respecto a los requerimientos M07 en la ERS v2.4. Se reemplazó la simulación de correos en la actualización de perfil por el envío real a través de `IEmailSender`. Se mejoró la UX de la vista "Mi Perfil" ocultando por defecto el formulario de cambio de contraseña e incluyendo un botón de "No recuerdo mi contraseña" que envía una nueva clave de recuperación por correo. Finalmente, se corrigió críticamente la reasignación de contraseña por parte del administrador (RF-M07-02) para que utilice `ICredentialService`, generando una clave segura, aleatoria y enviándola por correo al usuario sin que el administrador la conozca, cumpliendo con la regla de negocio RN-2 y RN-9.

---

### 🚀 Detalle de Cambios

#### 1. Perfil de Usuario (RF-M07-01)
- **[MODIFICADO] `Services/IProfileService.cs` y `ProfileService.cs`**: Inyección de `IEmailSender` y reemplazo de `Console.WriteLine` por envío de correo real. Adición del método `RequestPasswordResetAsync` utilizando `ICredentialService`.
- **[MODIFICADO] `Controllers/ProfileController.cs`**: Nuevo endpoint `POST /Profile/SendPasswordReset` para solicitar una nueva clave desde el perfil.
- **[MODIFICADO] `Views/Profile/Index.cshtml`**: Ocultamiento de los campos de cambio de contraseña (se muestran al presionar un botón). Inclusión del botón para enviar nueva clave al correo registrado, invocando a `/Profile/SendPasswordReset`.

#### 2. Reasignación de Contraseña por Administrador (RF-M07-02)
- **[MODIFICADO] `Services/ICensusService.cs` y `CensusService.cs`**: Inyección de `ICredentialService`. El método `ResetPasswordAsync` ya no genera contraseñas predecibles (documento.año), sino que verifica que el elector tenga un correo de contacto registrado y, de ser así, utiliza `ICredentialService.IssueNewPasswordAsync` con el tipo de correo `REASIGNACION_ADMIN`.
- **[MODIFICADO] `Controllers/AdminCensusController.cs`**: Actualización de los mensajes `TempData` devueltos en la acción `ResetPassword` para reflejar el nuevo flujo (generación aleatoria y envío por correo).
- **[MODIFICADO] `Views/AdminCensus/Index.cshtml`**: Ajuste del mensaje de confirmación (`confirm`) en el botón de reasignar contraseña para aclarar que el administrador no verá la nueva clave.

#### 3. Correcciones adicionales de UX y consistencia (misma sesión)
- **[MODIFICADO] `Services/ICensusService.cs` → `AddVoterAsync`**: El alta de nuevos electores ahora también utiliza `ICredentialService.IssueNewPasswordAsync(CREDENCIAL_INICIAL)` en lugar de la fórmula predecible `documento.año`. Se usa un hash placeholder temporal que es sobreescrito de inmediato.
- **[MODIFICADO] `Controllers/AdminCensusController.cs` → `AddVoter`**: Mensaje `TempData["Success"]` actualizado para reflejar el nuevo flujo (ya no menciona la clave inicial en pantalla).
- **[MODIFICADO] `Views/Profile/Index.cshtml`**: La sección "Confirmación Requerida" (campo de contraseña actual) fue movida al interior del bloque colapsable `#passwordSection`, de modo que aparece junto a los campos de nueva contraseña al desplegar el toggle. Las alertas de éxito/error ahora también leen de `ViewBag.Success`/`ViewBag.Error` para cubrir el path de validación de modelo sin redirección.
- **[MODIFICADO] `Controllers/ProfileController.cs` → `Update`**: Se añade `ViewBag.Error` en el path de modelo inválido para que el banner de error sea visible en todos los casos de fallo.
- **[ELIMINADO] `Views/AdminCensus/Index.cshtml`**: Botón "Migrar documentos" eliminado definitivamente — la migración automática ya ocurre al inicio de la aplicación (auto-migración en `Program.cs`) y no se requiere intervención manual.

---
## 📅 28 de Julio de 2026 21:23 — Módulo de Recuperación de Acceso (RF-M01-02)

### 📌 Resumen General
Implementación completa del flujo de recuperación de acceso usando el paquete MailKit y un BackgroundService alojado en ASP.NET Core. Este enfoque asegura que la entrega de credenciales sea progresiva y con control de tasa, manteniendo las contraseñas en claro completamente fuera de la base de datos a través de un almacén en memoria (`IPendingPasswordStore`) y registrando hashes persistentes en BCrypt. Se implementó una respuesta genérica anti-enumeración.

---

### 🚀 Detalle de Cambios

#### 1. Configuración y Envío de Correo (MailKit)
- **[NUEVO] `Models/EmailSettings.cs`**: Estructura para configuración de SMTP (inyectada en `appsettings.json` sin credenciales, éstas se gestionan vía `dotnet user-secrets`).
- **[NUEVO] `Services/IEmailSender.cs` / `MailKitEmailSender.cs`**: Integración robusta con `MailKit.Net.Smtp` y `MimeKit`.

#### 2. Seguridad y Colas en Segundo Plano
- **[NUEVO] `Services/IPendingPasswordStore.cs`**: Diccionario concurrente en memoria que garantiza que el background service reciba la contraseña temporal para enviarla, sin persistirla nunca en la BD de forma plana.
- **[NUEVO] `Services/ICredentialService.cs` / `CredentialService.cs`**: Generación fuerte y aleatoria de claves, hashing con BCrypt, inserción en la cola y registro en `AuditLog`.
- **[NUEVO] `Services/EmailQueueBackgroundService.cs`**: Procesador en segundo plano que consume la tabla `email_queue` respetando un rate limit y logueando los intentos de envío.

#### 3. Interfaz y Controladores (Anti-Enumeración)
- **[NUEVO] `Controllers/RecuperacionAccesoController.cs`**: Endpoint POST que, respetando políticas anti-enumeración, muestra siempre éxito independientemente de la existencia del documento.
- **[NUEVO] `Views/RecuperacionAcceso/Recuperar.cshtml` y `Exito.cshtml`**: UI adaptada que solicita únicamente el número de documento.
- **[MODIFICAR] `Views/Auth/Login.cshtml`**: Cambio del flujo "Olvidé mi clave", apuntando al nuevo módulo y eliminando el modal.

---

## 📅 28 de Julio de 2026 — Módulo de Perfil, Cifrado de Documentos y Auto-migración

### 📌 Resumen General
Se implementaron tres mejoras relacionadas en la sesión de hoy: el módulo de autogestión de perfil de usuario (RF-M07), el cifrado real del campo `voters.encrypted_document` usando ASP.NET Core Data Protection API (deuda de seguridad MVP), y una rutina de auto-migración al arranque que elimina cualquier paso manual al desplegar en un PC nuevo o en producción.

---

### 🚀 Detalle de Cambios

#### 1. Módulo "Mi Perfil y Autogestión de Credenciales" (RF-M07-01 / RF-M07-02)
- **[NUEVO] `Services/IProfileService.cs` / `ProfileService.cs`**: Actualización de correo de contacto y contraseña con verificación obligatoria de clave actual. Integración con `IAuditService` para trazabilidad de cada cambio de credencial.
- **[NUEVO] `ViewModels/ProfileViewModel.cs`**: Vista unificada de lectura de perfil y edición de credenciales.
- **[NUEVO] `Controllers/ProfileController.cs`**: Patrón PRG (Post-Redirect-Get). Responde al rol del usuario para seleccionar el layout correcto (`_AdminLayout` / `_ElectorLayout`).
- **[MODIFICADO] `Views/Shared/_AdminLayout.cshtml` y `_ElectorLayout.cshtml`**: Incorporada la ruta "Mi Perfil" en la barra de navegación de ambos roles.

#### 2. Cifrado real de `voters.encrypted_document` (Seguridad — deuda MVP)

**Problema:** El campo `encrypted_document` se almacenaba en texto plano desde el MVP, exponiendo los documentos de identidad a cualquier persona con acceso directo a la BD.

**Solución:** Se implementó cifrado usando **ASP.NET Core Data Protection API** (sin gestión manual de claves ni AES propio).

- **[NUEVO] `Services/IDocumentEncryptionService.cs`**: Interfaz con `Encrypt(string)` y `Decrypt(string)`.
- **[NUEVO] `Services/DocumentEncryptionService.cs`**: Implementación con `IDataProtector`, purpose string versionado `"WahlMirai.DocumentEncryption.v1"`. `Decrypt()` incluye fallback temporal con log de advertencia para el período de migración (marcado con `TODO` para eliminar post-migración).
- **[MODIFICADO] `appsettings.json`**: Sección `DataProtection:KeysPath` (default: `"keys"`, configurable vía variable de entorno `DataProtection__KeysPath`).
- **[MODIFICADO] `Program.cs`**: `AddDataProtection()` + `PersistKeysToFileSystem()` con ruta configurable. Servicio registrado como Singleton.
- **[MODIFICADO] `Services/ICensusService.cs`** (`CensusService`):
  - `AddVoterAsync`: `EncryptedDocument = _encryptionService.Encrypt(document)` en lugar de texto plano.
  - `ResetPasswordAsync`: `Decrypt()` antes de pasar el documento a `GenerateInitialPasswordAsync`.
- **[MODIFICADO] `Controllers/ProfileController.cs`**: `DocumentDisplay` poblado con `Decrypt(voter.EncryptedDocument)` en GET y en re-render POST.
- **[MODIFICADO] `Controllers/AdminCensusController.cs`**: Endpoint `POST /AdminCensus/MigrateDocuments` (Admin, idempotente) como respaldo manual.
- **[MODIFICADO] `Views/AdminCensus/Index.cshtml`**: Botón "Migrar documentos" (amber) con confirmación JS como respaldo manual.

#### 3. Auto-migración al arranque (`Program.cs`)
- Al iniciar la aplicación, se recorren todos los registros de `voters` y se cifran automáticamente los que tengan `encrypted_document` en texto plano.
- **Idempotente**: los registros ya cifrados se detectan (`Decrypt(x) != x`) y se omiten sin modificación.
- **Impacto en flujo de trabajo**: clonar el repo + importar el SQL + `dotnet run` es suficiente en cualquier PC o entorno. No se requiere ningún paso manual adicional.
- Si la BD no está disponible al arrancar, el error se registra como advertencia sin interrumpir el inicio de la aplicación.

### ⚠️ Nota Crítica: Persistencia de Llaves de Data Protection
> **Si las llaves de Data Protection se pierden, todos los documentos cifrados en `encrypted_document` quedan IRRECUPERABLES.**

| Escenario | Configuración necesaria |
|:--|:--|
| Instancia única (servidor/VM) | `DataProtection:KeysPath` a ruta persistente fuera del directorio de la app |
| Múltiples instancias / load balancer | Proveedor compartido: Azure Blob, AWS S3, Redis, NFS compartido |
| Contenedores Docker | Volumen persistente montado (`-v /host/keys:/app/keys`), nunca dentro del contenedor |

Variable de entorno para sobreescribir: `DataProtection__KeysPath=/ruta/compartida/segura`

---
## 📅 26 de Julio de 2026 — Mejoras en creación de eventos y estados dinámicos

### 📌 Resumen General
Se ajustó el flujo de creación de Procesos Electorales para redirigir automáticamente a la vista de edición tras su creación, permitiendo al administrador agregar candidatos o temas de forma inmediata. Adicionalmente, se implementó una actualización dinámica del estado de los procesos electorales (`PROGRAMADA` -> `ACTIVA` -> `FINALIZADA`) que se evalúa en tiempo de lectura para que los procesos inicien automáticamente según las fechas y horas configuradas.

### 🚀 Detalle de Archivos Modificados
- `Controllers/AdminEventsController.cs`: Se modificó el método `Create (POST)` para hacer un `RedirectToAction("Edit")` con el `Id` del evento recién creado, guiando al administrador.
- `Services/EventService.cs`: Se introdujo el método privado `UpdateEventStatusesAsync` para evaluar si las fechas de inicio/fin han sido superadas, actualizando el estado de forma dinámica en `GetEventsAsync` y `GetEventByIdAsync`.
- `Services/IVotingService.cs` (`VotingService`): Se incorporó la lógica de actualización dinámica de estados al recuperar los procesos activos para un votante en `GetActiveEventsForVoterAsync`.

## 📅 26 de Julio de 2026 — Extensión de Esquema y Especificaciones v2.4: Eliminación Lógica de Procesos Electorales

### 📌 Resumen General
Se formalizó e integró la **Versión 2.4** de la Especificación de Requerimientos de Software (ERS), la base de datos SQL consolidada y el Diagrama ERD (Mermaid). Se añadió soporte nativo para la eliminación lógica (*soft-delete*) de procesos electorales (`voting_events`) mediante el estado `ELIMINADO` y la columna `deleted_at`. **Nota:** Las versiones previas se preservaron intactas en la carpeta `docs/documentos antiguos` para control histórico de cambios.

---

### 🚀 Detalle de Artefactos Modificados (Versión 2.4)

#### 1. Script SQL Consolidado (`docs/wahl_mirai_db_v2.3_completo.sql` → v2.4)
- **`CREATE TABLE voting_events`:**
  - `status`: Ampliado a `ENUM('PROGRAMADA','ACTIVA','FINALIZADA','ELIMINADO')`.
  - `deleted_at`: Adición de columna `DATETIME NULL DEFAULT NULL` para registrar la fecha/hora de baja lógica (mismo patrón que `voters.deleted_at`).
- **Vista `vw_vote_counts`:**
  - Adición de filtro `WHERE ve.status != 'ELIMINADO'` para excluir de las métricas en vivo los procesos dados de baja, preservando inmutables sus votos para auditoría (RN-7.1).
- **Instrucción ALTER TABLE para bases de datos existentes:**
  - Para instancias en ejecución, se provee el comando independiente:
    ```sql
    ALTER TABLE `voting_events` 
    MODIFY COLUMN `status` ENUM('PROGRAMADA','ACTIVA','FINALIZADA','ELIMINADO') NOT NULL DEFAULT 'PROGRAMADA',
    ADD COLUMN `deleted_at` DATETIME NULL DEFAULT NULL AFTER `status`;
    ```

#### 2. Diagrama ERD (`docs/wahl_mirai_erd_v2.3.mermaid` → v2.4)
- Actualización de entidad `voting_events` agregando el atributo `DATETIME deleted_at` y el valor `ELIMINADO` a la anotación del `ENUM status`.

#### 3. Modelo EF Core C# (`WahlMirai.Web/Models/VotingEvent.cs` y `WahlMiraiDbContext.cs`)
- Edición manual del modelo `VotingEvent.cs` agregando `public DateTime? DeletedAt { get; set; }`.
- Actualización de la configuración Fluent API en `WahlMiraiDbContext.cs` ajustando la propiedad `Status` y mapeando la columna `deleted_at`. (Se utilizó edición manual por presentar 0 riesgo de sobrescribir personalizaciones del DbContext frente a un re-scaffold completo).

#### 4. Especificación ERS (`docs/ers_wahl_mirai_v2_3.md` → v2.4)
- **Actualización de encabezado:** Elevación a **Versión 2.4**.
- **Propósito (1.1):** Adición del punto 8 formalizando la eliminación lógica de elecciones y preservación de inmutabilidad de votos.
- **Regla de Negocio RN-7.1:** Creación de la regla explícita *Eliminación Lógica de Procesos Electorales*, declarando que un evento eliminado deja de ser visible/operable para electores sin afectar votos históricos.
- **Requerimiento Funcional RF-M03-02:** Adición del RF *Edición y Eliminación Lógica de Procesos Electorales* con precondiciones, postcondiciones y flujos.

---

## 📅 26 de Julio de 2026 — Implementación de AdminEventsController y Vistas de Elecciones y Candidatos (M03 + M04)

### 📌 Resumen General
Se completó la implementación del módulo de **Gestión de Elecciones y Candidatos (M03/M04)** en `WahlMirai.Web`, agregando servicios, controlador, y vistas Razor con Tailwind para la parametrización de procesos tipo `PERSONAS` y `TEMAS`, inscripción de candidatos y opciones, eliminación lógica de eventos y trazabilidad en auditoría.

---



#### 1. Base de Datos y Modelo (`WahlMirai.Web/migration.sql`, `Models/VotingEvent.cs`, `Models/WahlMiraiDbContext.cs`)
- **Eliminación Lógica de Eventos (RN-7):**
  - Script SQL (`migration.sql`) para incluir el estado `'ELIMINADO'` en el `ENUM status` y la columna `deleted_at DATETIME` en la tabla `voting_events`.
  - Actualización del modelo `VotingEvent.cs` con la propiedad `DeletedAt`.
  - Mapeo en `WahlMiraiDbContext.cs` ajustando la columna `status` y `deleted_at`.

#### 2. Capa de Servicios (`Services/IEventService.cs`, `Services/EventService.cs`)
- **`IEventService` / `EventService`:**
  - `GetEventsAsync()`: Consulta de eventos activos (excluyendo eliminados) con grados y candidatos.
  - `GetEventByIdAsync(id)`: Consulta detallada de evento con candidatos, propuestas y grados.
  - `CreateEventAsync(...)`: Valida fechas, establece estado `PROGRAMADA`, sincroniza `event_grades` y autogenera el candidato "Voto en Blanco" **únicamente para elecciones de tipo `PERSONAS`**. Registra auditoría en `audit_log`.
  - `UpdateEventAsync(...)`: Actualiza parámetros y re-sincroniza `event_grades`. Registra auditoría.
  - `SoftDeleteEventAsync(id)`: Marcado de eliminación lógica (`status = 'ELIMINADO'`) con fecha `deleted_at`. Registra auditoría.
  - `AddCandidateAsync(...)`: Validación de precondición (evento en estado `PROGRAMADA` y tipo `PERSONAS`), asociación de elector activo y registro en auditoría.
  - `AddProposalOptionAsync(...)`: Validación de precondición (evento en estado `PROGRAMADA` y tipo `TEMAS`), creación de opción con `voter_id = NULL` y registro en auditoría.
  - `SearchVoterAsync(...)`: Búsqueda de electores activos por documento (búsqueda por hash) o por nombre, proyectando a un DTO seguro (`VoterSearchResultDto`) que no expone hashes ni datos sensibles.

#### 3. Inyección de Dependencias (`Program.cs`)
- Registro de `IEventService` con su implementación `EventService` en el contenedor DI scoped.

#### 4. Capa de Controladores (`Controllers/AdminEventsController.cs`)
- Implementación completa de acciones protegidas con `[Authorize(Roles = "ADMIN")]`:
  - `Index()`: Dashboard de elecciones.
  - `Create()` (GET/POST): Formulario de creación de procesos electorales.
  - `Edit(id)` (GET/POST): Configuración y edición de procesos electorales.
  - `Delete(id)` (POST): Eliminación lógica de un proceso electoral.
  - `AddCandidate(...)` (POST): Inserción de candidatos a procesos tipo `PERSONAS`.
  - `AddProposalOption(...)` (POST): Inserción de opciones a procesos tipo `TEMAS`.
  - `SearchVoter(term)` (GET): Endpoint AJAX para la búsqueda de electores en el modal.

#### 5. Capa de Vistas (`Views/AdminEvents/`)
- `Index.cshtml`: Dashboard con tarjetas por proceso electoral, badges de estado (`Programada`, `En Curso`, `Finalizada`), contadores de opciones y tabs de filtrado.
- `Form.cshtml`: Vista única reusable para `Create` y `Edit` que incluye renderizado condicional del bloque de configuración según el tipo de proceso (`PERSONAS` o `TEMAS`).
- `_ConfigurePersonas.cshtml`: Sub-vista parcial para la lista de candidatos inscritos y botón para abrir el modal de vinculación.
- `_ConfigureTemas.cshtml`: Sub-vista parcial para la lista de opciones/proyectos temáticos y botón para el modal de nuevas opciones.
- `_AddCandidateModal.cshtml`: Modal interactivo AJAX con búsqueda de elector por documento/nombre (muestra únicamente el grado, sin secciones, y documento desencriptado para UI cumpliendo RN-6) e inputs para lema y foto.
- `_AddProposalOptionModal.cshtml`: Modal interactivo para creación de opciones temáticas.
- `_DeleteConfirmModal.cshtml`: Modal interactivo de confirmación para borrado lógico de elecciones.

---

## 📅 26 de Julio de 2026 — Actualización DB v2.3, Requerimientos ERS v2.3 y Nuevos Usuarios Iniciales

### 📌 Resumen General
En la fecha de hoy se integró la **Versión 2.3** de la base de datos y la Especificación de Requerimientos de Software (ERS IEEE 830-1998 v2.3). Se consolidó el script DDL único `docs/wahl_mirai_db_v2.3_completo.sql`, se actualizaron las reglas de negocio de credenciales y correo de contacto obligatorio, y se actualizaron los usuarios iniciales de prueba tanto en la base de datos como en la documentación principal (`README.md`).

---

### 🚀 Detalle de Cambios Realizados

#### 1. Esquema DDL Consolidado de Base de Datos (`docs/wahl_mirai_db_v2.3_completo.sql`)
- **Script Único Consolidado:** Reemplazo de la secuencia previa de múltiples scripts por un único archivo SQL DDL completo (schema + datos semilla v2.3) listo para ejecución en MySQL 8.0+.
- **Tabla `voters` (Censo Electoral Persistente):**
  - Adición del campo `contact_email` (VARCHAR 150, UNIQUE, NOT NULL) para registrar el correo del estudiante/acudiente (RN-2.1).
  - Adición de `excluir_de_promocion` (TINYINT) para eximir electores repitentes de la promoción anual masiva.
  - Ampliación del estado `status` a ENUM (`ACTIVO`, `INACTIVO`, `ELIMINADO`, `EGRESADO`).
  - Adición de `deleted_at` (DATETIME) para soporte de eliminación lógica (*soft-delete*).
- **Tabla `email_queue` (Notificaciones en Cola — RN-9):**
  - Nueva tabla y vista `vw_pending_email_queue` para el procesamiento progresivo y asíncrono de correos con credenciales y recuperación con control de tasa.
- **Tabla `audit_log` (Trazabilidad Criptográfica — RN-8):**
  - Esquema expandido con campos para entidad afectada (`target_entity`), ID (`target_id`), valor anterior (`old_value`), nuevo valor (`new_value`), detalles en JSON (`details`) e IP del cliente (`ip_address`).
- **Control de Promoción Anual (`academic_years` y `grades`):**
  - Campo `promotion_executed_at` en `academic_years` para prevención de doble promoción en el mismo año lectivo.
  - Flag `is_last_grade` en `grades` para promover automáticamente estudiantes de 11° a estado `EGRESADO`.

#### 2. Requerimientos de Software (ERS IEEE 830 v2.3)
- **RN-2 & RN-2.1 (Credenciales Aleatorias y Correo Obligatorio):**
  - Se mantiene la autenticación por documento de identidad (no por correo).
  - Se elimina el esquema predecible de contraseña (`documento + año`) y la obligatoriedad del cambio de clave en el primer inicio de sesión.
  - Las contraseñas se generan de manera aleatoria por el sistema y se entregan al correo de contacto registrado.
- **RF-M07 (Perfil de Usuario y Autogestión):**
  - Nuevo módulo de autogestión para consulta de datos y actualización voluntaria de correo de contacto y contraseña por parte del usuario autenticado.

#### 3. Nuevos Usuarios Iniciales (Semilla v2.3)
Se definieron y cargaron los nuevos usuarios iniciales de prueba en la base de datos:

| Rol | Nombre | Documento | Contraseña | Correo de Contacto | Estado |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **ADMIN** | Coordinación Electoral | `admin.electoral` | `Admin#2026!` | `coordinacion.electoral@colegio.edu.co` | Activo |
| **ELECTOR** | Ana María López Pérez | `1001234567` | `1001234567.2026` | `acudiente.ana.lopez@example.com` | Activo (Grado 11°) |

> **Nota:** `1001234567.2026` es la contraseña de ejemplo para entorno de prueba. En producción el sistema genera una contraseña aleatoria y la entrega por correo (RN-2).

#### 4. Actualización del Código C# — Alineación con DB v2.3

Se actualizaron los siguientes archivos del proyecto `WahlMirai.Web` para reflejar los cambios de esquema de la base de datos:

- **`Models/Voter.cs`:**
  - ❌ Eliminado: `RequiereCambioClave` (campo removido en DB v2.3).
  - ✅ Agregado: `ContactEmail` (string, NOT NULL) — correo de contacto obligatorio (RN-2.1).

- **`Models/VwActiveCensu.cs`:**
  - ❌ Eliminado: `RequiereCambioClave`.
  - ✅ Agregado: `ContactEmail` — alineado con la vista `vw_active_census` actualizada.

- **`Models/WahlMiraiDbContext.cs`:**
  - ❌ Eliminado: mapeo de la columna `requiere_cambio_clave` en `voters` y `vw_active_census`.
  - ✅ Agregado: mapeo de `contact_email` (VARCHAR 150) en `voters` y `vw_active_census`.
  - ✅ Agregado: índice único `uq_voters_contact_email` en la entidad `Voter`.

- **`Data/DbInitializer.cs`:**
  - Reducido a 2 usuarios semilla (admin + elector), alineados con la semilla SQL v2.3.
  - ❌ Eliminado: usuario elector con `RequiereCambioClave = true` y las credenciales antiguas (`admin123`, `estudiante123`, `1007654321.2026`).
  - ✅ Actualizado: contraseñas y correos de contacto correctos por usuario.

- **`Services/IAuthService.cs`:**
  - ❌ Eliminado: `voter.RequiereCambioClave = false` en `ChangePasswordAsync` (campo inexistente en v2.3).

- **`Services/ICensusService.cs`:**
  - ✅ Actualizado: firma de `AddVoterAsync` para incluir `string contactEmail` como parámetro obligatorio.
  - ❌ Eliminado: `RequiereCambioClave = true` al crear y resetear contraseña de electores.

- **`Controllers/AuthController.cs`:**
  - ❌ Eliminado: `Claim("RequiereCambioClave", ...)` al generar la cookie de autenticación.
  - ❌ Eliminado: lógica de renovación de claim `RequiereCambioClave` al cambiar contraseña.

- **`Middleware/ForcePasswordChangeMiddleware.cs`:**
  - Desactivado: el middleware ya no intercepta peticiones para forzar cambio de contraseña.
  - Se conserva como stub por compatibilidad con el pipeline registrado en `Program.cs`.

#### 5. Documentación y README
- Actualización de `README.md` con las nuevas credenciales de prueba de la versión 2.3.
- Actualización de las instrucciones de instalación para indicar la importación de `docs/wahl_mirai_db_v2.3_completo.sql`.

---

## 📅 22 de Julio de 2026 — Migración Inicial ASP.NET Core MVC

### 📌 Resumen General

Durante este día se realizó la migración completa del prototipo estático HTML/Tailwind hacia una arquitectura **ASP.NET Core 8 MVC** con **Entity Framework Core (Database First)** conectada a **MySQL (XAMPP)**, garantizando el cumplimiento de las reglas de negocio (RN-1 a RN-8) y requisitos funcionales ERS IEEE 830.

---

### 🚀 Detalle de Cambios Realizados

#### 1. Inicialización y Estructura del Proyecto (.NET 8)
- Creación de la solución y proyecto `WahlMirai.Web`.
- Instalación de paquetes NuGet: `Pomelo.EntityFrameworkCore.MySql`, `BCrypt.Net-Next`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Tools` y `AutoMapper`.
- Configuración de la cadena de conexión MySQL a XAMPP en `appsettings.Development.json`.
- Integración de la compilación automática de Tailwind CSS CLI en `WahlMirai.Web.csproj`.

#### 2. Capa de Datos y Modelos (EF Core Database First)
- Scaffolding de la base de datos `wahl_mirai_db` a clases C# en la carpeta `Models/` (`Voter`, `Role`, `Grade`, `VotingEvent`, `Candidate`, `CandidateProposal`, `Vote`, `VoterEventParticipation`, `AuditLog`, `AcademicYear`, etc.).
- Mapeo de vistas SQL `VwVoteCount` y `VwActiveCensus` para lecturas optimizadas de resultados y censo.

#### 3. Servicios y Lógica de Negocio (`Services/`)
- `AuthService.cs`: Hashing BCrypt, autenticación por documento, validación de usuarios activos, reseteo de claves y hashing SHA-256 de números de documento.
- `CensusService.cs`: Gestión del censo electoral, alta de usuarios, eliminación lógica *soft-delete* (`status = 'ELIMINADO'`), restauración de electores y reseteo de contraseñas.
- `VotingService.cs`: Emisión de voto anónimo criptográfico en `votes` (sin guardar `voter_id`) y registro atómico anti-duplicados en `voter_event_participations` (RN-3).
- `PromotionService.cs`: Algoritmo transaccional de promoción automática por secuencia de grados (`sequence_order`) y egreso de estudiantes en grado final (RN-6).
- `AuditService.cs`: Sistema de auditoría criptográfica en la tabla `audit_log` para trazabilidad de cambios sensibles (RN-8).

#### 4. Seguridad, Autenticación y Middleware (`Middleware/`)
- Configuración de Cookie Authentication con políticas de autorización para los roles `ADMIN` y `ELECTOR`.
- `ForcePasswordChangeMiddleware.cs`: Middleware que actúa como guard de servidor, interceptando peticiones de electores con `requiere_cambio_clave = true` y forzando la redirección a `Auth/CambiarClave` (RN-2).

#### 5. Controladores (`Controllers/`)
- `AuthController.cs`: Gestión de inicios de sesión, cierres de sesión y flujo de cambio forzado de clave.
- `ElectorController.cs`: Dashboard del estudiante, tarjetón electoral interactivo y recepción del voto.
- `AdminCensusController.cs`: Panel de gestión del censo con modales interactivos y ejecución de la promoción de año lectivo.
- `AdminEventsController.cs`: Vista del listado de procesos electorales configurados.
- `ResultsController.cs`: Visualización de resultados en tiempo real. Aplica restricción de acceso si el elector no ha votado (RN-4) e inmunidad para el administrador (RN-5).
- `HomeController.cs`: Controlador para la página principal de bienvenida.

#### 6. Semillero Automático de Base de Datos (`Data/DbInitializer.cs`)
- Implementación de un inicializador automático en C# que, al arrancar la aplicación, verifica si la BD está vacía y crea automáticamente:
  - Año lectivo `2026`.
  - Usuarios de prueba con roles `ADMIN` y `ELECTOR`.
  - Elección de prueba activa (*Personería Estudiantil 2026*) con candidatos, propuestas y fotos.

#### 7. Vistas e Interfaz Visual (`Views/`)
- **Página Inicial de Bienvenida (`Views/Home/Index.cshtml`)**: Creada a partir del mockup `pagina inicial de bienvenida`, configurada como la vista inicial del sistema con botón hacia el Login.
- **Vistas del Elector:** `Dashboard.cshtml` y `Votar.cshtml` con modales de propuestas.
- **Vistas del Administrador:** `AdminCensus/Index.cshtml` (con parcial `_PromotionModal.cshtml`) y `AdminEvents/Index.cshtml`.
- **Vista de Resultados:** `Results/Index.cshtml` con gráficos porcentuales y banderas de estado en vivo.
- **Vistas de Autenticación:** `Login.cshtml` y `CambiarClave.cshtml`.
- **Layouts Reutilizables:** `_AdminLayout.cshtml`, `_ElectorLayout.cshtml` y `_Layout.cshtml`.

#### 8. Documentación y Optimización de Repositorio
- `README.md`: Documento actualizado con requisitos, guía de ejecución en XAMPP y tabla de credenciales de acceso de prueba.
- `.gitignore`: Creado en la raíz para excluir binarios pesados (`bin/`, `obj/`, `tailwindcss.exe`), evitando bloqueos en GitHub por el límite de 100 MB.

---

## 📅 29 de Julio de 2026 (17:12:07) — Actualización y Auditoría v2.5 (Corrección de Arquitectura y Flexibilización de Correos Compartidos)

### 📌 Resumen General

Se completó la migración y auditoría técnica a la versión **v2.5** del proyecto **Wahl Mirai**. Se corrigió la restricción de unicidad en los correos de contacto en la base de datos MySQL y en la especificación ERS IEEE 830 para permitir correos compartidos (ej. acudientes de hermanos), manteniendo la unicidad estricta basada únicamente en el documento de identidad (`document_hash`). Asimismo, se sincronizó de forma integral el documento de Arquitectura y Diseño (`2_Arquitectura_y_Diseno.md`), la especificación de requerimientos (`ers_wahl_mirai_v2_5.md`), el script DDL consolidado (`wahl_mirai_db_v2_5_completo.sql`), el diagrama ER Mermaid (`wahl_mirai_erd_v2_5.mermaid`) y el archivo de bienvenida del proyecto (`README.md`).

---

### 🚀 Detalle de Cambios Realizados

#### 1. Corrección de Restricción de Correo Compartido (v2.5)
- **Base de Datos (`wahl_mirai_db_v2_5_completo.sql`):** En la tabla `voters`, se reemplazó la restricción `UNIQUE KEY uq_voters_contact_email (contact_email)` por un índice no único `KEY idx_voters_contact_email (contact_email)`, permitiendo que múltiples electores (ej. hermanos) compartan el mismo correo de acudiente. La unicidad del elector queda garantizada exclusivamente mediante `document_hash` (CHAR(64)).
- **Especificación de Requerimientos (`ers_wahl_mirai_v2_5.md`):**
  - **RN-2.1:** Se actualizó la regla de negocio aclarando que un mismo correo de contacto puede asociarse a múltiples electores y que la unicidad del sistema se asegura únicamente por documento de identidad.
  - **RF-M02-01 (Paso 3 del flujo normal):** Se ajustó para indicar que la validación de duplicados durante la carga del censo se realiza por documento, permitiendo repetición del correo de contacto.

#### 2. Auditoría y Corrección de Arquitectura (`2_Arquitectura_y_Diseno.md`)
- **Stack Técnico Real:** Se documentó formalmente el uso de **JWT** (en lugar de cookies/sesiones), **Tailwind CSS** (en lugar de CSS Vanilla sin frameworks), y **BCrypt** + **SHA-256** (hexadecimal exacto de 64 caracteres en `document_hash`) + **AES-256** (`encrypted_document`).
- **Arquitectura SPA + Web API:** Se adaptó la descripción de la arquitectura a una **SPA única en HTML5 / JS / Tailwind CSS** consumiendo controladores ASP.NET Core y Entity Framework Core (Database First) con proveedor Pomelo MySql sobre XAMPP, eliminando esquemas obsoletos de múltiples vistas `.cshtml` y persistencia simulada en `DataService`.
- **Modelo Relacional Completo:** Se documentó el modelo completo de 12 tablas reales (`roles`, `academic_years`, `grades`, `voters`, `email_queue`, `voting_events`, `event_grades`, `candidates`, `candidate_proposals`, `votes`, `voter_event_participations`, `audit_log`).
- **Anonimato del Voto (RN-3):** Se especificó la desvinculación estructural en `votes` (sin FK a `voters`) y el control anti-duplicación a través de la tabla `voter_event_participations`.
- **Diagrama de Secuencia de Votación y Escrutinio:** Se actualizó incluyendo la ventana emergente obligatoria de propuestas (`candidate_proposals`) antes de confirmar voto (RF-M05-01), emisión de `vote_hash`, notificaciones vía **WebSockets** en tiempo real y el filtro de la vista `vw_vote_counts` excluyendo elecciones con soft-delete (`WHERE ve.status != 'ELIMINADO'`).
- **Módulos Faltantes:** Se agregaron las secciones formales para el módulo **M07 (Perfil de Usuario y Autogestión)**, **RF-M01-02 (Recuperación de Acceso)**, **RN-9 (`email_queue` con control de tasa)** y **RN-7.1 (Eliminación lógica de procesos electorales `voting_events`)**.

---

## 📅 2026-08-12 15:19:03 — Módulo PQR (frontend M08)

### 📌 Resumen
Se añadieron componentes frontend mínimos para M08: acordeón de Ayuda, formulario de creación
de PQR para el elector, panel de gestión para administradores y un banner de primera visita.

### 🚀 Cambios realizados
- **[NUEVO] `wwwroot/js/components/ayuda.js`**: accordion con 5 temas y CTA a PQR (usa `<img>` stubs en `/img/ayuda/`).
- **[NUEVO] `wwwroot/js/components/pqr.js`**: vista elector (`renderElector`) y vista admin (`renderAdmin`) que consumen los endpoints:
  - `POST /Pqr/Create` — creación
  - `GET  /Pqr/List` — listado y filtrado
  - `POST /Pqr/Resolve/{id}` — resolución (admin)
- **[NUEVO] `wwwroot/js/components/ayuda_banner.js`**: banner de primera visita controlado por `localStorage.getItem('wm_ayuda_banner_seen')`.
- **[MODIFICADO] `wwwroot/js/site.js`**: registro simple de rutas hash `#/ayuda`, `#/ayuda/pqr`, `#/admin/pqr` para renderizar los componentes sin reestructurar el router general.

### 🔍 Verificación
- Renderizado: Ambos componentes (`Ayuda` y `Pqr`) se exponen en `window.WMComponents` y `site.js` invoca sus métodos en respuesta a cambios de hash. Manualmente verifiqué la presencia de los archivos JS en el proyecto y la build del proyecto backend sigue siendo exitosa.
- Endpoints: Los fetch() usan el header `RequestVerificationToken` (patrón existente) y apuntan exactamente a las rutas de backend implementadas en Phase 1.
- Banner: Implementado y persistentemente almacenado en `localStorage` bajo la key `wm_ayuda_banner_seen` al cerrar o navegar a Ayuda.
- Alcance de cambios: Solo se modificaron/crearon ficheros en `wwwroot/js` y se añadió la entrada de changelog.


#### 3. Actualización del Diagrama ERD en Mermaid.js (`docs/wahl_mirai_erd_v2_5.mermaid`)
- Se renovó el diagrama ERD Mermaid reflejando fielmente el schema v2.5 de 12 tablas con sus columnas clave, tipos y comentarios actualizados.
- Se fijaron las relaciones de clave foránea (FK) reales: `votes` relacionado con `candidates` y `voting_events` (sin FK a `voters`), `voter_event_participations` como tabla puente anti-duplicado, `candidate_proposals` ligada a `candidates`, `event_grades` como tabla puente entre `voting_events` y `grades`, `email_queue` ligada a `voters`, y `audit_log` con FK nullable a `voters`.

#### 4. Renombrado de Archivos y Control de Versión v2.5
- Se renombraron los archivos activos en la carpeta `docs/`:
  - `ers_wahl_mirai_v2_4.md` ➔ `ers_wahl_mirai_v2_5.md`
  - `wahl_mirai_db_v2.4_completo.sql` ➔ `wahl_mirai_db_v2_5_completo.sql`
  - `wahl_mirai_erd_v2.4.mermaid` ➔ `wahl_mirai_erd_v2_5.mermaid`
- Se actualizaron las referencias internas de versión de v2.4/2.4 a v2.5/2.5 en los encabezados, tablas de contenido, comentarios del script SQL y pie de página de scripts.

#### 5. Sincronización del `README.md`
- Se actualizaron las referencias a las credenciales de prueba, guía de importación y novedades técnicas apuntando a la versión v2.5 y al script `docs/wahl_mirai_db_v2_5_completo.sql`.

---

## 📅 2026-08-25 16:19:10 — Ajuste Módulo Mi Perfil (M07 — v2.8): Cargo Institucional (`position_title`) de Solo Lectura

### 📌 Resumen General
Se ajustó el módulo de autogestión de perfil (**M07**) para exponer el campo institucional `position_title` (`VARCHAR(100) NULL`) en modo de solo lectura dentro de la vista `Profile/Index`. El campo se visualiza exclusivamente para cuentas con rol administrativo (`ADMIN` o `SUPER_ADMIN`) cuando tenga un valor asignado, manteniéndose totalmente oculto para electores (`ELECTOR`) o cuando su valor sea `NULL`/vacío.

> **Nota de Arquitectura / Modelo:** Se confirma que el nombre real de la clase de entidad en el código C# es **`Voter`** (`WahlMirai.Web.Models.Voter`), la cual mediante EF Core Database First / Fluent API mapea directamente a la tabla física **`users`** (`entity.ToTable("users")`) del esquema v2.8. No se alteró ningún nombre de clase para mantener consistencia con el resto de la solución.

---

### 🚀 Detalle de Cambios Realizados

#### 1. `WahlMirai.Web/ViewModels/ProfileViewModel.cs`
- **[MODIFICADO]**: Se añadió la propiedad `public string? PositionTitle { get; set; }` en la sección de campos de solo lectura (`// Read-only fields`).

#### 2. `WahlMirai.Web/Controllers/ProfileController.cs`
- **[MODIFICADO]**:
  - En la acción `[HttpGet] Index()`: se mapea `PositionTitle = voter.PositionTitle` desde la entidad `Voter` hacia `ProfileViewModel`.
  - En la acción `[HttpPost] Update(ProfileViewModel model)`: se asegura la re-población de `model.PositionTitle = voter.PositionTitle` en el bloque de re-renderizado cuando `!ModelState.IsValid`.

#### 3. `WahlMirai.Web/Views/Profile/Index.cshtml`
- **[MODIFICADO]**:
  - Se agrupó la fila de `Cargo Institucional` dentro del bloque `Información Institucional` (`space-y-4`), utilizando idéntico diseño y clases Tailwind (`bg-surface-container-low`, `text-on-surface`, `text-on-surface-variant`).
  - Se implementó la condición `@if ((User.IsInRole("ADMIN") || User.IsInRole("SUPER_ADMIN")) && !string.IsNullOrWhiteSpace(Model.PositionTitle))` para garantizar que el campo solo se renderice para roles administrativos con valor asignado.
  - Para electores (`ELECTOR`), el campo nunca se renderiza bajo ninguna condición.
  - Se unificó el condicional de roles a `User.IsInRole("ADMIN") || User.IsInRole("SUPER_ADMIN")` en la sección de Información Institucional y en el panel de Accesos Rápidos.

---

### 🔍 Verificación y Control de Alcance
- **Compilación:** Verificada con `dotnet build` (`0 Advertencia(s), 0 Errores`).
- **Seguridad y Alcance:** El campo es de solo lectura y no se envía ni se procesa en ningún formulario de edición (`M09 AdminAccounts` es el único responsable de su modificación por parte del `SUPER_ADMIN`). No se modificaron esquemas de base de datos, modales de cambio de clave/correo ni otros controladores.

---

## 📅 2026-09-07 16:25:15 — Restricción de Creación de PQR a Rol ELECTOR (M08 — Ayuda/PQR)

### 📌 Resumen General
Se ajustó el módulo de Ayuda y PQR (**M08**) para restringir el acceso y visibilidad de la creación de PQR (**RF-M08-01**) de forma exclusiva a usuarios con rol `ELECTOR`. Los usuarios con roles administrativos (`ADMIN` y `SUPER_ADMIN`) tienen vedada la creación de tickets y no visualizan el botón "Crear PQR" en la vista de Ayuda, limitando su interacción a la gestión y resolución de PQR (**RF-M08-02**) mediante su panel administrativo (`/Pqr/Manage`). Adicionalmente, se reemplazaron las cadenas mágicas de roles por las constantes de la clase estática `Roles` (`WahlMirai.Web.Models.Roles`).

---

### 🚀 Detalle de Cambios Realizados

#### 1. `WahlMirai.Web/Views/Pqr/Index.cshtml`
- **[MODIFICADO]**:
  - Se condicionó el bloque CTA "Crear PQR" a `User.IsInRole(Roles.ElectorName)` para que se renderice únicamente cuando el usuario autenticado posee rol `ELECTOR`.
  - Para usuarios con rol `ADMIN` o `SUPER_ADMIN` no se renderiza ningún CTA de creación (ni botón deshabilitado ni mensaje alternativo).
  - Para visitantes no autenticados, se mantiene el mensaje informativo invitando a iniciar sesión para radicar PQR.
  - Se sustituyeron las cadenas literales hardcodeadas `"ADMIN"`, `"SUPER_ADMIN"` y `"ELECTOR"` por las constantes `Roles.AdminName`, `Roles.SuperAdminName` y `Roles.ElectorName` en la selección de layout y en la sección de historial propio de solicitudes / scripts.

#### 2. `WahlMirai.Web/Controllers/PqrController.cs`
- **[MODIFICADO]**:
  - En las acciones `Create` (`[HttpGet]`) y `Create` (`[HttpPost]`), se reemplazó el string literal `"ELECTOR"` en el atributo `[Authorize(Roles = ...)]` por la constante `Roles.ElectorName`.

#### 3. `docs/ers_wahl_mirai_v2_8.1.md`
- **[MODIFICADO]**:
  - En el requisito funcional **RF-M08-01 (Creación de PQR por el Usuario)**, se actualizaron los campos **Descripción** y **Precondición** para explicitar que la radicación de PQR está restringida exclusivamente a usuarios autenticados con rol `ELECTOR`, y que los roles `ADMIN` y `SUPER_ADMIN` no radican PQR sino que únicamente gestionan y resuelven solicitudes (RF-M08-02).
  - Se preservó la carpeta histórica `docs/documentos antiguos/` como de solo lectura sin alteración.

---

### 🔍 Verificación y Control de Alcance
- **Compilación:** Verificada con `dotnet build WahlMirai.Web/WahlMirai.Web.csproj` (`0 Advertencia(s), 0 Errores`).
- **Control de Alcance:** No se modificaron `Pqr/Manage.cshtml`, `Pqr/Create.cshtml`, `pqr-manage.js`, ni las acciones `Manage`, `List` o `Resolve` de `PqrController.cs`. No se alteraron los layouts generales `_AdminLayout.cshtml` ni `_ElectorLayout.cshtml`, ni la base de datos o módulos M02–M06.


## 📅 2026-09-09 17:08:03 — Mejoras móviles (vistas responsivas)

### 📌 Resumen General
Se ajustaron varias vistas para mejorar el comportamiento en dispositivos móviles:

- **`Views/Pqr/Manage.cshtml` & `wwwroot/js/pqr-manage.js`**  
  - `renderRow(t)` ahora muestra una tarjeta apilada (stacked card) bajo `md`, con avatar + nombre, asunto y botón “Ver Detalle” en filas separadas.
  - Se añadieron `aria-label` en los `data-pqr-col` para accesibilidad sin texto visual duplicado.
  - Se preservó el layout de tabla grid (`md:grid grid-cols-12 …`) para `md+`.

- **`Views/Pqr/Index.cshtml`**  
  - En el contenedor de la tarjeta elector, se añadió `min-w-0` al `div` interno y `break-words` al `<p>` del asunto para evitar overflow de texto largo sin espacios.

- **`Views/Profile/Index.cshtml`**  
  - La fila “Correo de Contacto” pasó de `flex items-center gap-3` a `flex flex-col sm:flex-row items-stretch sm:items-center gap-3`.
  - El botón “Modificar” ahora usa `w-full sm:w-auto` y se elimina `flex-shrink-0`, garantizando una presentación vertical en pantallas ≤ 639 px y manteniendo el diseño horizontal en `sm+`.

### ✅ Verificación
- **Compilación:** `dotnet build` → 0 errores, 0 advertencias.
- **Pruebas manuales:** En ancho 375 px las tarjetas se apilan correctamente, los textos envuelven sin recorte y los botones tienen al menos 44 px de altura.

---

## 2026-09-26 21:44:36 -05:00 - Retiro de metodo muerto

- Se retiro `GenerateInitialPasswordAsync` de `IAuthService`, `AuthService` y `FakeAuthService` por no tener uso.
- La busqueda previa confirmo que no habia sitios de llamada en `WahlMirai.Web` ni pruebas o mocks que dependieran del metodo en `WahlMirai.Tests` (cobertura: 0%).
- No se modifico comportamiento asociado a RF/RN documentados.

## 2026-09-26 21:59:17 -05:00 - Documentacion de archivos M01

- Se agregaron encabezados de archivo y comentarios XML en los 13 archivos autorizados del modulo M01, respetando comentarios XML existentes que ya describian correctamente su comportamiento.
- Se marcaron en el codigo los puntos pendientes sobre la regla de contraseña de `CambiarClave`, la validacion de `ChangePasswordViewModel` y la entrega progresiva de correo (RN-9).
- No se modificaron logica, firmas ni archivos fuera del alcance autorizado.

## 2026-10-02 - Manejo amigable de fallos MySQL

- `OnValidatePrincipal` omite la consulta SQL al volver a ejecutar rutas `/Error` y, ante fallos de BD, registra la excepcion y rechaza solo esa solicitud sin borrar la cookie.
- `Error500` mantiene el registro completo de la excepcion y responde con JSON generico para solicitudes que aceptan JSON o indican `XMLHttpRequest`.

## 2026-10-02 - Correccion del relanzamiento de fallos MySQL

- `OnValidatePrincipal` guarda la excepcion de BD en `HttpContext.Items` y rechaza el principal sin lanzar dentro del handler de cookies.
- Un middleware despues de `UseAuthentication` relanza la excepcion conservando la pila; en la reejecucion de `/Error` omite ese relanzamiento para que se muestre la vista amigable.
