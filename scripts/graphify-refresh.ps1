$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$graphPath = Join-Path $repoRoot 'graphify-out\graph.json'

function Get-GraphSnapshot {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return [pscustomobject]@{ Nodes = 0; Edges = 0; LastWriteTimeUtc = $null }
    }

    $graph = Get-Content -Raw -Encoding UTF8 $Path | ConvertFrom-Json
    $edges = if ($null -ne $graph.edges) { @($graph.edges).Count } else { @($graph.links).Count }
    $item = Get-Item $Path

    return [pscustomobject]@{
        Nodes = @($graph.nodes).Count
        Edges = $edges
        LastWriteTimeUtc = $item.LastWriteTimeUtc
    }
}

function Invoke-GraphifyRefreshStep {
    $before = Get-GraphSnapshot $graphPath

    & uv tool run --from graphifyy python -m graphify update . --force
    $updateExitCode = $LASTEXITCODE
    if ($updateExitCode -ne 0) {
        Write-Warning ("graphify update exited with code {0}." -f $updateExitCode)
    }

    $afterUpdate = Get-GraphSnapshot $graphPath
    $timestampChanged = $null -ne $before.LastWriteTimeUtc -and
        $null -ne $afterUpdate.LastWriteTimeUtc -and
        $afterUpdate.LastWriteTimeUtc -gt $before.LastWriteTimeUtc

    if (-not $timestampChanged) {
        & uv tool run --from graphifyy python -c "from pathlib import Path; from graphify.watch import _rebuild_code; raise SystemExit(0 if _rebuild_code(Path('.'), force=True) else 1)"
        if ($LASTEXITCODE -ne 0) {
            throw "The graphify rebuild fallback failed with exit code $LASTEXITCODE."
        }
    }
}

Push-Location $repoRoot
try {
    $status = 'OK'
    try {
        Invoke-GraphifyRefreshStep
    }
    catch {
        Start-Sleep -Seconds 2
        try {
            Invoke-GraphifyRefreshStep
            $status = 'OK-after-retry'
        }
        catch {
            $status = "FAILED: $($_.Exception.Message)"
            Write-Host $status
            exit 1
        }
    }

    Write-Host $status
}
finally {
    Pop-Location
}