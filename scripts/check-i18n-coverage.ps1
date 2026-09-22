# Run this after adding any new user-facing text, before considering the feature done.
[CmdletBinding()]
param(
    [string]$RepoRoot = ''
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}
$viewsRoot = Join-Path $RepoRoot 'WahlMirai.Web\Views'
$localePath = Join-Path $RepoRoot 'WahlMirai.Web\wwwroot\js\locale.js'
$targetLanguages = @('en', 'de', 'fr', 'ja', 'zh')

if (-not (Test-Path $viewsRoot)) { throw "Views directory not found: $viewsRoot" }
if (-not (Test-Path $localePath)) { throw "locale.js not found: $localePath" }

function Get-DictionaryKeys {
    param(
        [string]$Content,
        [string]$Language,
        [string]$ObjectName = 'DICTIONARY'
    )

    $objectPattern = '(?s)const\s+' + [regex]::Escape($ObjectName) + '\s*=\s*\{(?<body>.*?)\n\s*\};'
    $object = [regex]::Match($Content, $objectPattern)
    if (-not $object.Success) { return @{} }

    $objectContent = $object.Groups['body'].Value
    $startPattern = '(?s)"?' + [regex]::Escape($Language) + '"?\s*:\s*\{'
    $start = [regex]::Match($objectContent, $startPattern)
    if (-not $start.Success) { return @{} }

    $bodyStart = $start.Index + $start.Length
    $remaining = $objectContent.Substring($bodyStart)
    $end = [regex]::Match($remaining, '(?s)\n\s*\},\s*\n\s*"?(?:es|en|de|fr|ja|zh)"?\s*:\s*\{')
    $body = if ($end.Success) { $remaining.Substring(0, $end.Index) } else { $remaining }

    $keys = @{}
    foreach ($match in [regex]::Matches($body, '(?m)^\s*"((?:\\.|[^"\\])*)"\s*:\s*"')) {
        $key = $match.Groups[1].Value -replace '\\"', '"'
        $keys[$key] = $true
    }
    return $keys
}

function Test-UserFacingString {
    param([string]$Value)

    $value = ($Value -replace '\s+', ' ').Trim()
    if ($value.Length -lt 2) { return $false }
    if ($value -match '[@{};]|=>|==|\b(?:var|const|let|Model|ViewBag|ViewContext|Request|User|Context)\b') { return $false }
    if ($value -match '^(?:[a-z_]+|[a-z_]+\s+[a-z_]+)$' -and $value -notmatch '[\u00C0-\u00FF]') { return $false }
    if ($value -match '^[@$%{}()\[\];=<>/\\]+$') { return $false }
    if ($value -match '^(?:if|else|for|foreach|var|return|function|class|true|false|null)$') { return $false }
    if ($value -match '^(?:[\w-]+\s*:\s*[\w-]+(?:\s+[\w-]+)*)$' -and $value -notmatch '[\u00C0-\u00FF]') { return $false }
    return $value -match '[A-Za-z\u00C0-\u00FF]'
}

function Add-ExtractedString {
    param(
        [hashtable]$Strings,
        [string]$Value,
        [string]$File,
        [int]$Line
    )

    $normalized = ($Value -replace '\s+', ' ').Trim()
    if (-not (Test-UserFacingString $normalized)) { return }

    $key = $normalized.ToLowerInvariant()
    if (-not $Strings.ContainsKey($key)) {
        $Strings[$key] = [pscustomobject]@{
            Text = $normalized
            Locations = New-Object System.Collections.Generic.List[string]
        }
    }
    $location = "$File`:$Line"
    if (-not $Strings[$key].Locations.Contains($location)) {
        $Strings[$key].Locations.Add($location)
    }
}

function Get-LineNumber {
    param(
        [string]$Content,
        [int]$Index
    )
    return (($Content.Substring(0, $Index) -split "`n").Count)
}

$localeContent = Get-Content -Path $localePath -Raw -Encoding UTF8
$dictionaryKeys = @{}
foreach ($language in $targetLanguages) {
    $dictionaryKeys[$language] = Get-DictionaryKeys -Content $localeContent -Language $language -ObjectName 'DICTIONARY'
    $extraKeys = Get-DictionaryKeys -Content $localeContent -Language $language -ObjectName 'EXTRA_TRANSLATIONS'
    foreach ($key in $extraKeys.Keys) {
        $dictionaryKeys[$language][$key] = $true
    }
}

$strings = @{}
$viewFiles = Get-ChildItem -Path $viewsRoot -Recurse -Filter '*.cshtml' | Sort-Object FullName
foreach ($view in $viewFiles) {
    $content = Get-Content -Path $view.FullName -Raw -Encoding UTF8
    $content = [regex]::Replace($content, '(?s)@\*.*?\*@', '')
    $content = [regex]::Replace($content, '(?s)<!--.*?-->', '')
    $content = [regex]::Replace($content, '(?is)<script\b.*?</script>', '')
    $content = [regex]::Replace($content, '(?is)<style\b.*?</style>', '')
    $relative = $view.FullName.Substring($RepoRoot.Length).TrimStart('\', '/') -replace '\\', '/'

    foreach ($match in [regex]::Matches($content, '(?s)>\s*([^<>]+?)\s*<')) {
        Add-ExtractedString -Strings $strings -Value $match.Groups[1].Value -File $relative -Line (Get-LineNumber $content $match.Index)
    }

    foreach ($match in [regex]::Matches($content, '(?i)(?:title|aria-label|placeholder|value)\s*=\s*"([^"]+)"')) {
        Add-ExtractedString -Strings $strings -Value $match.Groups[1].Value -File $relative -Line (Get-LineNumber $content $match.Index)
    }
}

$orderedStrings = @($strings.Values | Sort-Object Text)
$total = $orderedStrings.Count
Write-Output "i18n coverage audit"
Write-Output "Views scanned: $($viewFiles.Count)"
Write-Output "Unique extracted strings: $total"
Write-Output ""

$hasMissing = $false
foreach ($language in $targetLanguages) {
    $missing = @($orderedStrings | Where-Object { -not $dictionaryKeys[$language].ContainsKey($_.Text) })
    $covered = $total - $missing.Count
    $status = if ($missing.Count -eq 0) { 'PASS' } else { 'FAIL' }
    Write-Output "[$language] $status - covered: $covered / $total; missing: $($missing.Count)"
    foreach ($item in $missing) {
        $hasMissing = $true
        Write-Output "  - $($item.Text) [$($item.Locations -join ', ')]"
    }
    Write-Output ""
}

if ($hasMissing) {
    Write-Output 'RESULT: FAIL - one or more target languages have missing exact-match dictionary keys.'
    exit 1
}

Write-Output 'RESULT: PASS - every extracted string has an exact-match key in all target languages.'
exit 0
