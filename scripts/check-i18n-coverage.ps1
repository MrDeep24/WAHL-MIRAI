# Run this after adding any new user-facing text, before considering the feature done.
[CmdletBinding()]
param(
    [string]$RepoRoot = '',
    [string]$ExportPath = ''
)

$ErrorActionPreference = 'Stop'
$commonPath = Join-Path $PSScriptRoot 'i18n-common.ps1'
. $commonPath

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}
$viewsRoot = Join-Path $RepoRoot 'WahlMirai.Web\Views'
$targetLanguages = @('en', 'de', 'fr', 'ja', 'zh')

if (-not (Test-Path $viewsRoot)) { throw "Views directory not found: $viewsRoot" }
foreach ($language in $targetLanguages) {
    $langLocalePath = Join-Path $RepoRoot "WahlMirai.Web\wwwroot\js\locale.$language.js"
    if (-not (Test-Path $langLocalePath)) { throw "Dictionary file not found: $langLocalePath" }
}

function Test-UserFacingString {
    param([string]$Value)

    $value = ($Value -replace '\s+', ' ').Trim()
    if ($value.Length -lt 2) { return $false }
    if ($value -match '[@{};]|=>|==|\b(?:var|const|let|Model|ViewBag|ViewContext|Request|User|Context)\b') { return $false }
    if ($value -cmatch '^(?:[a-z_]+|[a-z_]+\s+[a-z_]+)$' -and $value -notmatch '[\u00C0-\u00FF]') { return $false }
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
        [int]$Line,
        [switch]$IsExplicitKey
    )

    $normalized = ($Value -replace '\s+', ' ').Trim()
    if (-not $IsExplicitKey) {
        if (-not (Test-UserFacingString $normalized)) { return }
        if (-not [string]::IsNullOrWhiteSpace((Get-FilterCategory -Value $normalized))) { return }
    } else {
        if ($normalized.Length -lt 2) { return }
    }

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

function Test-DynamicString {
    param([string]$Value)

    return $Value -match '\$\{[^}]+\}|\{\{[^}]+\}\}|<\/?(?:count|value|date|time)>|\b(?:count|total|quantity|cantidad)\s*[:=]\s*\{[^}]+\}'
}

function Get-SourceLineText {
    param(
        [hashtable]$SourceContents,
        [string]$Location
    )

    if ($Location -notmatch '^(.*):(\d+)$') { return '' }
    $content = $SourceContents[$Matches[1]]
    if ($null -eq $content) { return '' }
    $lineNumber = [int]$Matches[2]
    $lines = $content -split "`r?`n"
    if ($lineNumber -lt 1 -or $lineNumber -gt $lines.Count) { return '' }
    return (($lines[$lineNumber - 1] -replace '\s+', ' ').Trim())
}

function Get-FilterCategory {
    param([string]$Value)

    if ($Value -match '^[¡¿©]') {
        return ''
    }
    if ($Value -match '\?\?|=>|\b(?:var|const|let|return|new)\b|\([^()]*\([^()]*\)|\[[^\]]*$|^[^\[]*\]|\?\s*[A-Za-z_]\w*\.|\b[A-Za-z_]\w*\.[A-Za-z_]\w*\s*\(') {
        return 'codeFragments'
    }
    if ($Value -match '^(?:https?://|/[A-Za-z0-9_-]+/[A-Za-z0-9_-]+(?:[/?#].*)?)') {
        return 'routeLike'
    }
    if ($Value -match '^[\s\.,;:?|]' -or $Value -cmatch '^[a-záéíóúñ]') {
        return 'fragments'
    }
    return ''
}

$dictionaryKeys = @{}
$extraObjects = @('EXTRA_TRANSLATIONS', 'chatbotDraftTranslations', 'ajaxAndModalTranslations')
foreach ($language in $targetLanguages) {
    $langLocalePath = Join-Path $RepoRoot "WahlMirai.Web\wwwroot\js\locale.$language.js"
    $contentToUse = Get-Content -Path $langLocalePath -Raw -Encoding UTF8

    $dictionaryKeys[$language] = Get-DictionaryKeys -Content $contentToUse -Language $language -ObjectName 'DICTIONARY'
    foreach ($extraObj in $extraObjects) {
        $extraKeys = Get-DictionaryKeys -Content $contentToUse -Language $language -ObjectName $extraObj
        foreach ($key in $extraKeys.Keys) {
            $dictionaryKeys[$language][$key] = $true
        }
    }
}

$strings = @{}
$sourceContents = @{}
$viewFiles = Get-ChildItem -Path $viewsRoot -Recurse -Filter '*.cshtml' | Sort-Object FullName
foreach ($view in $viewFiles) {
    $originalContent = Get-Content -Path $view.FullName -Raw -Encoding UTF8
    $relative = $view.FullName.Substring($RepoRoot.Length).TrimStart('\', '/') -replace '\\', '/'
    $sourceContents[$relative] = $originalContent

    # Escanear llamadas a translateText('...'), t('...'), alert('...'), confirm('...') en bloques <script>
    foreach ($scriptBlock in [regex]::Matches($originalContent, '(?is)<script\b[^>]*>(.*?)</script>')) {
        $scriptText = $scriptBlock.Groups[1].Value
        $scriptOffset = $scriptBlock.Groups[1].Index
        foreach ($ttMatch in [regex]::Matches($scriptText, '(?s)\b(?:translateText|t|alert|confirm)\s*\(\s*([''"`])((?:\\.|(?!\1).)*?)\1\s*\)')) {
            $rawVal = $ttMatch.Groups[2].Value
            $extractedVal = $rawVal -replace '\\([''"`])', '$1'
            $lineNum = Get-LineNumber $originalContent ($scriptOffset + $ttMatch.Index)
            Add-ExtractedString -Strings $strings -Value $extractedVal -File $relative -Line $lineNum -IsExplicitKey
        }
    }

    # Atributos inline onsubmit / onclick con confirm('...') o alert('...')
    foreach ($inlineMatch in [regex]::Matches($originalContent, '(?i)\bon\w+\s*=\s*"[^"]*?\b(?:confirm|alert)\s*\(\s*''([^'']+)''\s*\)[^"]*"')) {
        $extractedVal = $inlineMatch.Groups[1].Value
        $lineNum = Get-LineNumber $originalContent $inlineMatch.Index
        Add-ExtractedString -Strings $strings -Value $extractedVal -File $relative -Line $lineNum -IsExplicitKey
    }

    $content = $originalContent
    $content = [regex]::Replace($content, '(?s)@\*.*?\*@', '')
    $content = [regex]::Replace($content, '(?s)<!--.*?-->', '')
    $content = [regex]::Replace($content, '(?is)<script\b.*?</script>', '')
    $content = [regex]::Replace($content, '(?is)<style\b.*?</style>', '')

    foreach ($match in [regex]::Matches($content, '(?s)>\s*([^<>]+?)\s*<')) {
        Add-ExtractedString -Strings $strings -Value $match.Groups[1].Value -File $relative -Line (Get-LineNumber $content $match.Index)
    }

    foreach ($match in [regex]::Matches($content, '(?i)(?:title|aria-label|placeholder|value)\s*=\s*"([^"]+)"')) {
        Add-ExtractedString -Strings $strings -Value $match.Groups[1].Value -File $relative -Line (Get-LineNumber $content $match.Index)
    }
}

# Escaneo de archivos JavaScript en wwwroot/js (excluyendo locale*.js)
$jsRoot = Join-Path $RepoRoot 'WahlMirai.Web\wwwroot\js'
$jsFiles = @()
if (Test-Path $jsRoot) {
    $jsFiles = Get-ChildItem -Path $jsRoot -Recurse -Filter '*.js' | Where-Object { $_.Name -notlike 'locale*.js' } | Sort-Object FullName
    foreach ($jsFile in $jsFiles) {
        $originalContent = Get-Content -Path $jsFile.FullName -Raw -Encoding UTF8
        $relative = $jsFile.FullName.Substring($RepoRoot.Length).TrimStart('\', '/') -replace '\\', '/'
        $sourceContents[$relative] = $originalContent

        foreach ($ttMatch in [regex]::Matches($originalContent, '(?s)\b(?:translateText|t|alert|confirm)\s*\(\s*([''"`])((?:\\.|(?!\1).)*?)\1\s*\)')) {
            $rawVal = $ttMatch.Groups[2].Value
            $extractedVal = $rawVal -replace '\\([''"`])', '$1'
            $lineNum = Get-LineNumber $originalContent $ttMatch.Index
            Add-ExtractedString -Strings $strings -Value $extractedVal -File $relative -Line $lineNum -IsExplicitKey
        }
    }
}

# Escaneo de mensajes AJAX en controladores MVC (WahlMirai.Web\Controllers\*.cs)
$controllersRoot = Join-Path $RepoRoot 'WahlMirai.Web\Controllers'
$controllerFiles = @()
if (Test-Path $controllersRoot) {
    $controllerFiles = Get-ChildItem -Path $controllersRoot -Recurse -Filter '*.cs' | Sort-Object FullName
    foreach ($ctrlFile in $controllerFiles) {
        $originalContent = Get-Content -Path $ctrlFile.FullName -Raw -Encoding UTF8
        $relative = $ctrlFile.FullName.Substring($RepoRoot.Length).TrimStart('\', '/') -replace '\\', '/'
        $sourceContents[$relative] = $originalContent

        foreach ($jsonMatch in [regex]::Matches($originalContent, '(?s)\bJson\s*\(\s*new\s*\{[^}]*?\bmessage\s*=\s*"([^"]+)"')) {
            $rawVal = $jsonMatch.Groups[1].Value
            if ($rawVal -notmatch '\{|\}' -and $rawVal -match '[A-Za-z\u00C0-\u00FF]' -and $rawVal.Length -ge 3) {
                $lineNum = Get-LineNumber $originalContent $jsonMatch.Index
                Add-ExtractedString -Strings $strings -Value $rawVal -File $relative -Line $lineNum -IsExplicitKey
            }
        }
    }
}

$orderedStrings = @($strings.Values | Sort-Object Text)
$total = $orderedStrings.Count
Write-Output "i18n coverage audit"
Write-Output "Views scanned: $($viewFiles.Count)"
Write-Output "JS files scanned: $($jsFiles.Count)"
Write-Output "Controllers scanned: $($controllerFiles.Count)"
Write-Output "Unique extracted strings: $total"
Write-Output ""

$hasMissing = $false
foreach ($language in $targetLanguages) {
    $missing = Get-MissingTranslations -Strings $orderedStrings -DictionaryKeys $dictionaryKeys -Language $language
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
    if (-not [string]::IsNullOrWhiteSpace($ExportPath)) {
        $candidateItems = @($orderedStrings | ForEach-Object {
            $item = $_
            $missingIn = @($targetLanguages | Where-Object {
                $language = $_
                (Get-MissingTranslations -Strings @($item) -DictionaryKeys $dictionaryKeys -Language $language).Count -gt 0
            })
            [pscustomobject]@{
                text = $item.Text
                sources = @($item.Locations)
                missingIn = $missingIn
                dynamic = Test-DynamicString -Value $item.Text
            }
        } | Where-Object { $_.missingIn.Count -gt 0 })

        $filtered = @{
            codeFragments = @()
            routeLike = @()
            fragments = @()
        }
        $exportItems = @($candidateItems | ForEach-Object {
            $item = $_
            $category = Get-FilterCategory -Value $item.text
            $sourceLines = @($item.sources | ForEach-Object {
                [pscustomobject]@{
                    location = $_
                    fullSource = Get-SourceLineText -SourceContents $sourceContents -Location $_
                }
            })
            if ([string]::IsNullOrWhiteSpace($category) -and @($sourceLines | Where-Object {
                $_.fullSource -match '@(?:Model|ViewBag|[A-Za-z_][A-Za-z0-9_]*)' -and $_.fullSource -notmatch [regex]::Escape($item.text)
            }).Count -gt 0) {
                $category = 'fragments'
            }
            if ([string]::IsNullOrWhiteSpace($category)) {
                $item
                return
            }

            $filtered[$category] += ,([pscustomobject]@{
                text = $item.text
                sources = $sourceLines
                missingIn = $item.missingIn
            })
        })

        $filteredExport = [pscustomobject]@{
            codeFragments = @($filtered.codeFragments)
            routeLike = @($filtered.routeLike)
            fragments = @($filtered.fragments)
        }
        $filteredPath = Join-Path (Split-Path -Parent $ExportPath) 'missing-strings-filtered-out.json'
        $filteredExport | ConvertTo-Json -Depth 6 | Set-Content -Path $filteredPath -Encoding UTF8

        $export = [pscustomobject]@{
            static = @($exportItems | Where-Object { -not $_.dynamic })
            dynamic = @($exportItems | Where-Object { $_.dynamic })
        }
        $exportDirectory = Split-Path -Parent $ExportPath
        if (-not [string]::IsNullOrWhiteSpace($exportDirectory)) {
            New-Item -ItemType Directory -Path $exportDirectory -Force | Out-Null
        }
        $export | ConvertTo-Json -Depth 5 | Set-Content -Path $ExportPath -Encoding UTF8
        Write-Output "Exported missing strings: $ExportPath"
        Write-Output "Filtered out: $($candidateItems.Count - $exportItems.Count)"
        Write-Output "Static: $(@($export.static).Count); Dynamic: $(@($export.dynamic).Count); Total: $($exportItems.Count)"
    }
    Write-Output 'RESULT: FAIL - one or more target languages have missing exact-match dictionary keys.'
    exit 1
}

Write-Output 'RESULT: PASS - every extracted string has an exact-match key in all target languages.'
exit 0
