$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$viewsRoot = Join-Path $root 'Views'
function Get-LocaleKeys {
    param([string]$Path)

    if (-not (Test-Path $Path)) { throw "Dictionary file not found: $Path" }
    $content = Get-Content -Path $Path -Raw -Encoding UTF8
    $match = [regex]::Match($content, '(?s)\bDICTIONARY\s*:\s*\{(?<body>.*?)(?:\n\s*\},|\n\s*\}\s*;\s*$|\}\s*,|\}\s*$)')
    if (-not $match.Success) { return New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase) }

    $body = $match.Groups['body'].Value
    $keys = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($m in [regex]::Matches($body, '(?m)^\s*"((?:\\.|[^"\\])*)"\s*:\s*"')) {
        $key = $m.Groups[1].Value -replace '\\"', '"'
        $keys.Add($key) | Out-Null
    }
    return $keys
}

function Get-VisibleStrings {
    param([string]$ViewPath)

    $content = Get-Content -Path $ViewPath -Raw -Encoding UTF8
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
    $langPath = Join-Path $root "wwwroot/js/locale.$lang.js"
    $localeKeysByLang[$lang] = Get-LocaleKeys -Path $langPath
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
    Write-Host "  ${lang}: covered $covered / $allVisibleCount"
}
