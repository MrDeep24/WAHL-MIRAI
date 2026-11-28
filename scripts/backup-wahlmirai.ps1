<#
  Respaldo de Wahl Mirai (Windows + XAMPP/MySQL)
  Genera DOS archivos .7z cifrados y SEPARADOS:
    1) db_YYYYMMDD_HHmm.7z    -> dump de wahl_mirai_db
    2) files_YYYYMMDD_HHmm.7z -> keys\ + App_Data\ (llaves Data Protection y archivos .enc)
  Guardarlos separados evita que una sola copia comprometida permita descifrar todo.

  PREPARACION UNICA (en PowerShell, con el mismo usuario que ejecutara la tarea):
    1. Crear usuario MySQL dedicado de solo lectura:
         CREATE USER 'backup'@'localhost' IDENTIFIED BY 'UNA_CLAVE_LARGA';
         GRANT SELECT, SHOW VIEW, TRIGGER, LOCK TABLES ON wahl_mirai_db.* TO 'backup'@'localhost';
         FLUSH PRIVILEGES;
    2. Crear C:\wahl-backup\mysql.cnf con:
         [client]
         user=backup
         password=UNA_CLAVE_LARGA
       Restringir permisos NTFS de C:\wahl-backup exclusivamente al usuario de respaldo y SYSTEM.
    3. Guardar la clave del .7z cifrada con DPAPI (solo la descifra este usuario en este PC):
         Read-Host "Clave 7z" -AsSecureString | ConvertFrom-SecureString | Set-Content C:\wahl-backup\7zpass.txt
       IMPORTANTE: guarda esa misma clave en un gestor de contrasenas o en papel fuera del servidor.
       Sin esta clave no podras abrir los respaldos en caso de desastre.
#>

$ErrorActionPreference = 'Stop'

# ---------- CONFIGURACION (ajustar segun entorno del servidor) ----------
$AppDir        = 'C:\WahlMirai\WahlMirai.Web'        # Carpeta publicada de WahlMirai.Web
$LocalBackups  = 'D:\Backups\WahlMirai'              # Ideal: disco distinto al del sistema
$ExternalDest  = 'E:\WahlMirai-backups'              # Disco externo / carpeta sincronizada (opcional)
$ConfigDir     = 'C:\wahl-backup'
$MysqlDump     = 'C:\xampp\mysql\bin\mysqldump.exe'
$SevenZip      = 'C:\Program Files\7-Zip\7z.exe'
$Database      = 'wahl_mirai_db'
$RetentionDays = 30
# ------------------------------------------------------------------------

$stamp = Get-Date -Format 'yyyyMMdd_HHmm'
$null  = New-Item -ItemType Directory -Force -Path $LocalBackups
$log   = Join-Path $LocalBackups 'backup.log'

function Log([string]$msg) {
    "$(Get-Date -Format 'u')  $msg" | Tee-Object -FilePath $log -Append
}

$sql  = Join-Path $LocalBackups "db_$stamp.sql"
$pass = $null

try {
    $mysqlCnf = Join-Path $ConfigDir 'mysql.cnf'
    $passFile = Join-Path $ConfigDir '7zpass.txt'
    $keysDir  = Join-Path $AppDir 'keys'
    $appData  = Join-Path $AppDir 'App_Data'

    foreach ($req in @($MysqlDump, $SevenZip, $mysqlCnf, $passFile, $keysDir, $appData)) {
        if (-not (Test-Path $req)) {
            throw "Recurso requerido no encontrado: $req"
        }
    }

    # Leer clave del 7z desde DPAPI (sin hardcodear secretos)
    $sec  = Get-Content -Path $passFile -Raw | ConvertTo-SecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
    try {
        $pass = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
    if ([string]::IsNullOrWhiteSpace($pass)) {
        throw "La clave DPAPI en $passFile esta vacia."
    }

    # 1) Dump consistente de la base de datos y compresion cifrada (-mhe=on oculta nombres)
    & $MysqlDump "--defaults-extra-file=$mysqlCnf" `
        --single-transaction --no-tablespaces --default-character-set=utf8mb4 `
        --databases $Database --result-file=$sql
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $sql) -or (Get-Item $sql).Length -lt 10KB) {
        throw "mysqldump fallo o genero un archivo sospechosamente pequeno."
    }

    $dbArchive = Join-Path $LocalBackups "db_$stamp.7z"
    & $SevenZip a -t7z -mhe=on "-p$pass" $dbArchive $sql | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "7z (db) fallo." }

    Remove-Item -Path $sql -Force

    # 2) Llaves Data Protection + archivos cifrados (.enc) de candidaturas
    $filesArchive = Join-Path $LocalBackups "files_$stamp.7z"
    & $SevenZip a -t7z -mhe=on "-p$pass" $filesArchive $keysDir $appData | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "7z (files) fallo." }

    # 3) Verificar integridad de ambos archivos cifrados
    foreach ($archive in @($dbArchive, $filesArchive)) {
        & $SevenZip t "-p$pass" $archive | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Verificacion de integridad fallo: $archive" }
    }

    # 4) Copia secundaria fuera del disco principal
    if (-not [string]::IsNullOrWhiteSpace($ExternalDest)) {
        $qualifier = Split-Path $ExternalDest -Qualifier -ErrorAction SilentlyContinue
        $canCopy = if ($qualifier) { Test-Path $qualifier } else { Test-Path (Split-Path $ExternalDest -Parent) }
        if ($canCopy) {
            $null = New-Item -ItemType Directory -Force -Path $ExternalDest
            Copy-Item -Path $dbArchive, $filesArchive -Destination $ExternalDest -Force
            Log "Copiado a $ExternalDest"
        } else {
            Log "ADVERTENCIA: destino externo no disponible ($ExternalDest), solo copia local."
        }
    }

    # 5) Retencion automatica
    foreach ($dir in @($LocalBackups, $ExternalDest)) {
        if (-not [string]::IsNullOrWhiteSpace($dir) -and (Test-Path $dir)) {
            Get-ChildItem -Path $dir -Filter '*.7z' -File |
                Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$RetentionDays) } |
                Remove-Item -Force
        }
    }

    Log "OK  $stamp  (db + files verificados)"
}
catch {
    Log "ERROR  $($_.Exception.Message)"
    exit 1
}
finally {
    # Garantizar que nunca quede un dump SQL sin cifrar en disco ante fallos intermedios
    if (Test-Path $sql) {
        Remove-Item -Path $sql -Force -ErrorAction SilentlyContinue
    }
    $pass = $null
}
