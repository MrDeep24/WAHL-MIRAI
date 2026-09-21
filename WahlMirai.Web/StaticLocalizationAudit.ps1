$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$viewsRoot = Join-Path $root 'Views'
$localeFile = Join-Path $root 'wwwroot/js/locale.js'

function Get-LocaleKeys {
    param([string]$Path, [string]$Language)

    $content = Get-Content -Path $Path -Raw
    $startMarker = '"' + $Language + '"\s*:\s*\{'
    $match = [regex]::Match($content, $startMarker)
    if (-not $match.Success) { return @() }

    $start = $match.Index + $match.Length
    $after = $content.Substring($start)
    $endMatch = [regex]::Match($after, '(?s)\n\s*\},\s*\n\s*"(en|de|fr|ja|zh)"\s*:\s*\{')
    if ($endMatch.Success) {
        $body = $after.Substring(0, $endMatch.Index)
    } else {
        $body = $after
    }

    $keys = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($m in [regex]::Matches($body, '(?s)"([^"]+)"\s*:\s*"')) {
        $keys.Add($m.Groups[1].Value) | Out-Null
    }
    return $keys
}

function Get-VisibleStrings {
    param([string]$ViewPath)

    $content = Get-Content -Path $ViewPath -Raw
    $strings = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($m in [regex]::Matches($content, '(?s)>\s*([^<>]{2,}?)\s*<')) {
        $value = $m.Groups[1].Value.Trim()
        if ($value -and $value -notmatch '^(?:\d+|[\W_]+)$') {
            $strings.Add($value) | Out-Null
        }
    }

    foreach ($m in [regex]::Matches($content, '(?i)(?:title|aria-label|placeholder|value)\s*=\s*"([^"]+)"')) {
        $value = $m.Groups[1].Value.Trim()
        if ($value -and $value -notmatch '^(?:\d+|[\W_]+)$') {
            $strings.Add($value) | Out-Null
        }
    }

    return $strings
}

$viewFiles = Get-ChildItem -Path $viewsRoot -Recurse -Filter *.cshtml | Sort-Object FullName
$localeKeysByLang = @{}
foreach ($lang in @('en','de','fr','ja','zh')) {
    $localeKeysByLang[$lang] = Get-LocaleKeys -Path $localeFile -Language $lang
}

$allVisible = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($file in $viewFiles) {
    foreach ($s in Get-VisibleStrings -ViewPath $file.FullName) {
        $allVisible.Add($s) | Out-Null
    }
}

$missingByLang = @{}
foreach ($lang in @('en','de','fr','ja','zh')) {
    $missingByLang[$lang] = @()
    foreach ($s in $allVisible) {
        if (-not $localeKeysByLang[$lang].Contains($s)) {
            $missingByLang[$lang] += $s
        }
    }
}

$allVisibleCount = $allVisible.Count
Write-Host "Total extracted unique strings: $allVisibleCount"
foreach ($lang in @('en','de','fr','ja','zh')) {
    $count = $missingByLang[$lang].Count
    Write-Host "[$lang] missing: $count"
    if ($count -gt 0) {
        $missingByLang[$lang] | Sort-Object | Select-Object -First 30 | ForEach-Object { Write-Host "  - $_" }
    }
}

Write-Host ""
Write-Host "Coverage by language:"
foreach ($lang in @('en','de','fr','ja','zh')) {
    $covered = $allVisibleCount - $missingByLang[$lang].Count
    Write-Host "  $lang: covered $covered / $allVisibleCount"
}
