# Plan de Respaldo y Recuperación ante Desastres — Wahl Mirai

Este documento define la estrategia operativa de copias de seguridad, protección de datos personales (Ley 1581 de 2012 de Colombia, datos de estudiantes y menores de edad) y recuperación ante fallos para **Wahl Mirai** en servidor **Windows + ASP.NET Core 9.0 + MySQL 8.0+ (XAMPP)**.

---

## 1. Activos Críticos y Principio de Separación

| Activo | Ruta / Origen | Criticidad | Riesgo si se pierde |
| :--- | :--- | :--- | :--- |
| **Base de datos `wahl_mirai_db`** | MySQL (`localhost:3306`) | **Máxima** | Pérdida irreconstruible de votos (`votes`), participaciones (`event_participations`), censo y auditoría (`audit_log`). |
| **Llaves Data Protection** | `WahlMirai.Web/keys/` (o `DataProtection:KeysPath`) | **Máxima** | Sin estas llaves XML no se pueden descifrar documentos de identidad (`encrypted_document`) ni los archivos `.enc` ni tokens móviles. |
| **Archivos de candidaturas** | `WahlMirai.Web/App_Data/candidacy-uploads/` | **Alta** | Pérdida de fotos, planes de gobierno PDF y soportes documentales cifrados (`*.enc`). |

### Regla de Separación Criptográfica
El script [`scripts/backup-wahlmirai.ps1`](../scripts/backup-wahlmirai.ps1) genera **dos archivos independientes**, ambos cifrados con AES-256 y encabezados ocultos (`-mhe=on`):
1. `db_YYYYMMDD_HHmm.7z` — contiene exclusivamente el dump SQL de `wahl_mirai_db`.
2. `files_YYYYMMDD_HHmm.7z` — contiene `keys/` y `App_Data/`.

Al separarlos (y eliminar de inmediato el `.sql` temporal incluso en caso de error), la exposición de un solo archivo no permite reconstruir o descifrar por sí solo la información completa.

---

## 2. Preparación Única del Servidor Windows

Ejecuta estos pasos una sola vez **con la misma cuenta de Windows** que ejecutará la tarea programada de respaldo (DPAPI vincula el cifrado al usuario y al equipo).

### 2.1. Usuario MySQL dedicado de solo lectura
En MySQL (`mysql -u root` o phpMyAdmin):
```sql
CREATE USER 'backup'@'localhost' IDENTIFIED BY 'REEMPLAZAR_CLAVE_LARGA_Y_ALEATORIA';
GRANT SELECT, SHOW VIEW, TRIGGER, LOCK TABLES ON wahl_mirai_db.* TO 'backup'@'localhost';
FLUSH PRIVILEGES;
```

### 2.2. Carpeta segura de credenciales (`C:\wahl-backup`)
1. Crea la carpeta `C:\wahl-backup` y restringe permisos NTFS únicamente a `SYSTEM`, `Administrators` y la cuenta ejecutora:
   ```powershell
   New-Item -ItemType Directory -Force -Path C:\wahl-backup | Out-Null
   icacls C:\wahl-backup /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "$env:USERNAME:(OI)(CI)F"
   ```
2. Crea `C:\wahl-backup\mysql.cnf`:
   ```ini
   [client]
   user=backup
   password=REEMPLAZAR_CLAVE_LARGA_Y_ALEATORIA
   ```
3. Guarda la contraseña de los `.7z` cifrada con DPAPI:
   ```powershell
   Read-Host "Clave 7z" -AsSecureString | ConvertFrom-SecureString | Set-Content C:\wahl-backup\7zpass.txt
   ```
   > ⚠️ **Respaldo fuera del servidor:** Guarda una copia física o en gestor de contraseñas de la clave del `.7z`. Si el disco del servidor falla, DPAPI ya no existirá y necesitarás esa clave para abrir los `.7z`.

### 2.3. Ajustar variables de [`scripts/backup-wahlmirai.ps1`](../scripts/backup-wahlmirai.ps1)
Verifica al inicio del script:
- `$AppDir`: ruta publicada real de `WahlMirai.Web` (por defecto `C:\WahlMirai\WahlMirai.Web`).
- `$LocalBackups`: disco local secundario (`D:\Backups\WahlMirai`).
- `$ExternalDest`: disco USB externo o carpeta sincronizada cifrada (`E:\WahlMirai-backups`).
- `$MysqlDump`: `C:\xampp\mysql\bin\mysqldump.exe`.
- `$SevenZip`: `C:\Program Files\7-Zip\7z.exe`.

---

## 3. Automatización en el Programador de Tareas de Windows

Programar ejecución diaria (y cada 1–2 horas durante jornadas electorales activas):

```powershell
$action  = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -File C:\WahlMirai\scripts\backup-wahlmirai.ps1"
$trigger = New-ScheduledTaskTrigger -Daily -At 02:00AM
Register-ScheduledTask -TaskName "WahlMirai-Backup-Diario" -Action $action -Trigger $trigger -Description "Respaldo cifrado BD + keys + App_Data de Wahl Mirai"
```

Prueba manual inmediata:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\backup-wahlmirai.ps1
Get-Content D:\Backups\WahlMirai\backup.log -Tail 20
```

---

## 4. Procedimiento de Recuperación ante Desastres (Restore)

Ante caída del servidor, corrupción de BD o migración a un equipo nuevo:

1. **Instalar prerrequisitos en el servidor destino:** `.NET 9.0 Hosting Bundle / SDK`, `MySQL 8.0+ / XAMPP`, `7-Zip`.
2. **Desplegar el código fuente / binario** de `WahlMirai.Web` desde Git (sin `keys/`, `App_Data/` ni secretos).
3. **Restaurar llaves y archivos cifrados** desde `files_YYYYMMDD_HHmm.7z`:
   ```powershell
   & "C:\Program Files\7-Zip\7z.exe" x "E:\WahlMirai-backups\files_YYYYMMDD_HHmm.7z" -o"C:\WahlMirai\WahlMirai.Web"
   ```
   *(Confirma que queden restauradas las carpetas `keys\` y `App_Data\candidacy-uploads\` exactamente con sus archivos).*
4. **Restaurar la base de datos** desde `db_YYYYMMDD_HHmm.7z`:
   ```powershell
   & "C:\Program Files\7-Zip\7z.exe" x "E:\WahlMirai-backups\db_YYYYMMDD_HHmm.7z" -o"C:\temp-restore"
   cmd.exe /c '"C:\xampp\mysql\bin\mysql.exe" -u root -p < C:\temp-restore\db_YYYYMMDD_HHmm.sql'
   Remove-Item "C:\temp-restore" -Recurse -Force
   ```
5. **Restaurar variables de entorno de producción** (`ConnectionStrings__WahlMiraiDb`, `EmailSettings__SenderEmail`, `EmailSettings__SenderPassword`, `EmailSettings__BaseUrl`, `DataProtection__KeysPath`).
6. **Arrancar y validar:** iniciar `WahlMirai.Web` y comprobar que un elector registrado puede iniciar sesión y que una candidatura existente muestra su foto y plan PDF (esto certifica que `keys/` descifra correctamente `encrypted_document` y `.enc`).

---

## 5. Verificación Periódica

- **Diaria:** revisar que `backup.log` termine en `OK <timestamp> (db + files verificados)`.
- **Pre-jornada electoral:** ejecutar un respaldo manual y validar un simulacro de extracción con `7z t` en un entorno aislado antes de abrir votaciones.
