[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$localePath = Join-Path $repoRoot 'WahlMirai.Web\wwwroot\js\locale.js'

. (Join-Path $PSScriptRoot 'i18n-common.ps1')

$content = [System.IO.File]::ReadAllText($localePath, [System.Text.Encoding]::UTF8)

$hasDuplicates = $false
foreach ($lang in @('es', 'en', 'de', 'fr', 'ja', 'zh')) {
    $langPath = Join-Path $repoRoot "WahlMirai.Web\wwwroot\js\locale.$lang.js"
    $fileContent = if (Test-Path $langPath) {
        [System.IO.File]::ReadAllText($langPath, [System.Text.Encoding]::UTF8)
    } else {
        $content
    }

    $dict = Get-DictionaryKeys -Content $fileContent -Language $lang -ObjectName 'DICTIONARY'
    $extra = Get-DictionaryKeys -Content $fileContent -Language $lang -ObjectName 'EXTRA_TRANSLATIONS'
    
    $allKeys = @()
    foreach ($k in $dict.Keys) { $allKeys += $k }
    foreach ($k in $extra.Keys) { $allKeys += $k }

    $groups = $allKeys | Group-Object -CaseSensitive | Where-Object { $_.Count -gt 1 }
    if ($groups) {
        $hasDuplicates = $true
        Write-Host "[$lang] DUPLICATES FOUND: $($groups.Count)"
        foreach ($g in $groups) {
            Write-Host "  - $($g.Name) (count: $($g.Count))"
        }
    } else {
        Write-Host "[$lang] NO DUPLICATES (total keys: $($allKeys.Count))"
    }
}

if ($hasDuplicates) {
    Write-Host "FAIL: Duplicate keys detected."
    exit 1
} else {
    Write-Host "PASS: Zero duplicates across all languages."
    exit 0
}
