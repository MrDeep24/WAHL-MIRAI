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

Push-Location $repoRoot
try {
    $before = Get-GraphSnapshot $graphPath
    Write-Host ("Before: {0} nodes, {1} edges" -f $before.Nodes, $before.Edges)

    & uv tool run --from graphifyy python -m graphify update . --force
    $updateExitCode = $LASTEXITCODE
    $afterUpdate = Get-GraphSnapshot $graphPath

    if ($updateExitCode -ne 0) {
        Write-Warning ("graphify update exited with code {0}." -f $updateExitCode)
    }

    $timestampChanged = $null -ne $before.LastWriteTimeUtc -and
        $null -ne $afterUpdate.LastWriteTimeUtc -and
        $afterUpdate.LastWriteTimeUtc -gt $before.LastWriteTimeUtc

    if (-not $timestampChanged) {
        Write-Host 'graphify update did not change graph.json; running the rebuild fallback.'
        & uv tool run --from graphifyy python -c "from pathlib import Path; from graphify.watch import _rebuild_code; raise SystemExit(0 if _rebuild_code(Path('.'), force=True) else 1)"
        if ($LASTEXITCODE -ne 0) {
            throw "The graphify rebuild fallback failed with exit code $LASTEXITCODE."
        }
    }

    $after = Get-GraphSnapshot $graphPath
    Write-Host ("After:  {0} nodes, {1} edges" -f $after.Nodes, $after.Edges)
    Write-Host ("Delta:  {0:+#;-#;0} nodes, {1:+#;-#;0} edges" -f ($after.Nodes - $before.Nodes), ($after.Edges - $before.Edges))
}
finally {
    Pop-Location
}