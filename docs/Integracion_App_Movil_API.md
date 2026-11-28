# Integración de la App Móvil Android con WAHL-MIRAI (`MobileApiController`)

## 1. Arquitectura de Conexión

La aplicación móvil Android (**Jetpack Compose + Retrofit/OkHttp**) se conecta al backend **ASP.NET Core (`WahlMirai.Web`)** a través del dominio HTTPS publicado mediante **Cloudflare Tunnel**:

```text
[ App Android (Retrofit / OkHttp) ]
        │  HTTPS + Authorization: Bearer <token>
        ▼
[ Cloudflare Tunnel (https://wahl-mirai.lat/) ]
        │  HTTP/HTTPS local hacia Kestrel
        ▼
[ WahlMirai.Web -> MobileApiController (/api/mobile/*) ]
        │  IAuthService / IVotingService / IActiveSessionStore
        ▼
[ MySQL (wahl_mirai_db) ]
```

---

## 2. Endpoints Implementados (`WahlMirai.Web/Controllers/MobileApiController.cs`)

| Método | Ruta | Autenticación | Descripción |
|---|---|---|---|
| `GET` | `/api/mobile/health` | Pública | Verifica que el backend y el túnel Cloudflare responden correctamente. |
| `POST` | `/api/mobile/login` | Pública (`AuthLogin` rate limit) | Valida documento/contraseña (`IAuthService.ValidateLoginAsync`), registra sesión única (RN-14) y emite token Bearer cifrado con `IDataProtectionProvider`. |
| `POST` | `/api/mobile/logout` | Bearer Token | Invalida la sesión activa en `IActiveSessionStore`. |
| `GET` | `/api/mobile/profile` | Bearer Token | Devuelve el perfil del usuario autenticado (descifrando `EncryptedDocument`). |
| `GET` | `/api/mobile/events` | Bearer Token | Lista las votaciones visibles para el grado del elector (`INSCRIPCION`, `PROPUESTAS`, `ACTIVA`, `FINALIZADA`) con estado `hasVoted` y candidatos aprobados. |
| `GET` | `/api/mobile/events/{id}` | Bearer Token | Devuelve el detalle actualizado de una votación y sus candidatos. |
| `POST` | `/api/mobile/vote` | Bearer Token | Registra el voto anónimo (RN-3) mediante `IVotingService.CastVoteAsync`. |
| `GET` | `/api/mobile/media` | Bearer Token | Descifra y entrega archivos `.enc` de candidaturas (`photo_*` y `plan_*` para `ELECTOR`; cualquier `.enc` para `ADMIN`/`SUPER_ADMIN`). |

---

## 3. Reglas de Negocio Aplicadas

- **Autenticación y bloqueo por fuerza bruta (RN-15)**: `POST /api/mobile/login` usa `[EnableRateLimiting("AuthLogin")]` e `IAuthService.ValidateLoginAsync` (hash SHA-256 del documento + verificación BCrypt + `ILoginAttemptStore`).
- **Sesión única por usuario (RN-14)**: Al iniciar sesión en móvil se registra un nuevo `sessionStamp` en `IActiveSessionStore`. Si el usuario inicia sesión en otro dispositivo o en la web, las peticiones con el token anterior reciben `401 Unauthorized` (`code: "SESSION_EXPIRED"`) y la app cierra sesión automáticamente mediante `SessionManager`.
- **Secreto del voto y control anti-duplicado (RN-3)**: `POST /api/mobile/vote` delega en `IVotingService.CastVoteAsync`, registrando la participación en `event_participations` y el voto anónimo con hash SHA-256 en `votes` dentro de una transacción.
- **Visibilidad de resultados (RN-4 / RN-4.1)**: En `GET /api/mobile/events` y `GET /api/mobile/events/{id}`, los conteos y porcentajes de `vw_vote_counts` solo se exponen si la elección está `FINALIZADA`, si el elector ya emitió su voto en etapa `ACTIVA`, o si el usuario tiene rol `ADMIN` / `SUPER_ADMIN`.

---

## 4. Contratos JSON Principales

### `POST /api/mobile/login`
**Request:**
```json
{
  "document": "1001234567",
  "password": "TuPassword"
}
```
**Response `200 OK`:**
```json
{
  "token": "<token-cifrado-dataprotection>",
  "expiresAt": "2026-11-26T23:28:00.0000000+00:00",
  "user": {
    "id": 2,
    "documento": "1001234567",
    "nombre": "Ana María López Pérez",
    "grado": "11°",
    "rol": "Elector",
    "status": "ACTIVO"
  }
}
```

### `POST /api/mobile/vote`
**Request:**
```json
{
  "eventId": 1,
  "candidateId": 3
}
```
**Response `200 OK`:**
```json
{
  "success": true,
  "message": "¡Tu voto ha sido registrado exitosamente!"
}
```

### `GET /api/mobile/media`
Entrega en texto/bytes descifrados las fotografías y planes de gobierno almacenados como `.enc` en `<CandidacyUploads:BasePath>/candidacies/<fileName>` (por defecto `App_Data/candidacy-uploads/candidacies/<fileName>`). Los DTOs de `GET /api/mobile/events` y `GET /api/mobile/events/{id}` reescriben automáticamente las URLs de `photoUrl` y `pdfUrl` que terminen en `.enc` o inicien con `/uploads/` hacia `/api/mobile/media?path={Uri.EscapeDataString(rawUrl)}`.

* **Autenticación:** Requiere encabezado `Authorization: Bearer <token>` validado mediante `AuthenticateBearerAsync()` (`401 Unauthorized` con código `TOKEN_MISSING`, `TOKEN_INVALID`, `SESSION_EXPIRED` por expiración o RN-14, o `ACCOUNT_DISABLED`).
* **Parámetro de consulta (`query`):** `path` (ejemplo: `?path=photo_123.jpg.enc` o `?path=/uploads/candidacies/plan_123.pdf.enc`). El servidor aplica `Path.GetFileName(path)` para neutralizar secuencias de *path traversal*.
* **Validaciones y códigos de error:**
  * `400 Bad Request` (`INVALID_MEDIA_PATH`): `path` nulo, vacío o solo espacios.
  * `400 Bad Request` (`INVALID_MEDIA_FILE`): nombre de archivo vacío o sin extensión `.enc` (insensible a mayúsculas).
  * `403 Forbidden` (`FORBIDDEN`): usuarios sin rol `ADMIN` o `SUPER_ADMIN` (por ejemplo, `ELECTOR`) solo pueden consultar archivos cuyo nombre inicie con `photo_` o `plan_` (insensible a mayúsculas); cualquier otro prefijo como `doc_*.enc` es rechazado con `403`. Las cuentas `ADMIN` y `SUPER_ADMIN` pueden acceder a cualquier archivo `.enc`.
  * `404 Not Found` (`MEDIA_NOT_FOUND`): el archivo no existe en disco.
  * `500 Internal Server Error` (`MEDIA_DECRYPT_ERROR`): error al descifrar (`IDocumentEncryptionService.Decrypt`) o decodificar el contenido Base64.
* **Respuesta `200 OK`:** Devuelve los bytes crudos descifrados (`File(fileBytes, contentType)`) con `Content-Type` derivado de la extensión original tras remover `.enc`:
  * `.pdf` → `application/pdf`
  * `.png` → `image/png`
  * `.jpg` / `.jpeg` → `image/jpeg`
  * `.webp` → `image/webp`
  * Cualquier otra extensión → `application/octet-stream`

---

## 5. Configuración en la App Android (`MyApplication`)

- **URL base**: Definida en `app/build.gradle.kts` mediante `buildConfigField`:
  ```kotlin
  buildConfigField("String", "API_BASE_URL", "\"https://wahl-mirai.lat/\"")
  ```
  Si deseas apuntar temporalmente a un entorno local desde el emulador Android, cambia este valor a `"http://10.0.2.2:<PUERTO>/"` (habilitando tráfico HTTP cleartext solo en desarrollo si no usas HTTPS).
- **Archivos principales agregados/actualizados**:
  - `app/src/main/AndroidManifest.xml`: Permisos `INTERNET` y `ACCESS_NETWORK_STATE`.
  - `app/src/main/java/com/example/myapplication/data/remote/ApiDtos.kt`: DTOs y mapeo hacia modelos de dominio.
  - `app/src/main/java/com/example/myapplication/data/remote/WahlMiraiApiService.kt`: Interfaz Retrofit.
  - `app/src/main/java/com/example/myapplication/data/remote/ApiClient.kt`: Cliente OkHttp con interceptor `Bearer` y detección automática de sesión expirada (`401`).
  - `app/src/main/java/com/example/myapplication/data/repository/UserRepository.kt` y `VotingRepository.kt`: Conectados al backend real.

---

## 6. Verificación de Despliegue con Cloudflare Tunnel

1. Arranca el backend local (`dotnet run` dentro de `WahlMirai.Web`) asegurando que MySQL esté activo.
2. Verifica que el túnel de Cloudflare apunte al puerto local donde escucha Kestrel.
3. Prueba el endpoint de salud desde cualquier navegador o terminal:
   ```bash
   curl https://wahl-mirai.lat/api/mobile/health
   ```
   Debe responder `{"status":"ok","service":"WAHL-MIRAI Mobile API",...}`.
4. **Nota sobre Data Protection**: Si despliegas en múltiples instancias o reinicias contenedores, conserva la carpeta `keys/` definida en `DataProtection:KeysPath` (`appsettings.json`) para que los tokens y documentos cifrados sigan siendo válidos tras reinicios.
