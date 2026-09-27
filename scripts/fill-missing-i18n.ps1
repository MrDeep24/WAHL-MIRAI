[CmdletBinding()]
param(
    [string]$RepoRoot = ''
)

$ErrorActionPreference = 'Stop'
$commonPath = Join-Path $PSScriptRoot 'i18n-common.ps1'
. $commonPath

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}

$coverageScript = Join-Path $PSScriptRoot 'check-i18n-coverage.ps1'
$localePath = Join-Path $RepoRoot 'WahlMirai.Web\wwwroot\js\locale.js'
$pythonPath = Join-Path $RepoRoot 'venv\Scripts\python.exe'
$translatorPath = Join-Path $PSScriptRoot 'translate_offline.py'
$targetLanguages = @('en', 'de', 'fr', 'ja', 'zh')

foreach ($path in @($coverageScript, $localePath, $pythonPath, $translatorPath)) {
    if (-not (Test-Path $path)) { throw "Required file not found: $path" }
}

$tempDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("wahl-mirai-i18n-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDirectory | Out-Null

try {
    $exportPath = Join-Path $tempDirectory 'missing-strings.json'
    $powershellPath = (Get-Command powershell.exe -ErrorAction Stop).Source
    $coverageOutput = @(& $powershellPath -NoProfile -ExecutionPolicy Bypass -File $coverageScript -RepoRoot $RepoRoot -ExportPath $exportPath 2>&1)
    $coverageExitCode = $LASTEXITCODE
    $coverageOutput | ForEach-Object { Write-Output $_ }
    if ($coverageExitCode -notin @(0, 1)) {
        throw "Coverage extraction failed with exit code $coverageExitCode."
    }
    if (-not (Test-Path $exportPath)) {
        Write-Output 'No missing translations found.'
        return
    }

    $export = Get-Content -Path $exportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $staticStrings = @($export.static)
    if ($staticStrings.Count -eq 0) {
        Write-Output 'No static missing translations are eligible for automatic translation.'
        return
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

    $missingPairs = foreach ($language in $targetLanguages) {
        foreach ($item in (Get-MissingTranslations -Strings $staticStrings -DictionaryKeys $dictionaryKeys -Language $language)) {
            [pscustomobject]@{
                Text = $item.text
                Language = $language
            }
        }
    }
    $missingPairs = @($missingPairs)
    if ($missingPairs.Count -eq 0) {
        Write-Output 'No static missing translations are eligible for automatic translation.'
        return
    }

    $translatedPairs = New-Object System.Collections.Generic.List[object]
    $previousConsoleEncoding = [Console]::OutputEncoding
    try {
        [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
        foreach ($pair in $missingPairs) {
            Write-Host "Translating [$($pair.Language)] $($pair.Text)"
            $translationOutput = @(& $pythonPath $translatorPath $pair.Text $pair.Language)
            $translationExitCode = $LASTEXITCODE
            if ($translationExitCode -ne 0) {
                throw "Offline translation failed for language '$($pair.Language)' and text '$($pair.Text)'."
            }
            $translation = ($translationOutput -join "`n").TrimEnd("`r", "`n")
            if ([string]::IsNullOrWhiteSpace($translation)) {
                throw "Offline translation returned an empty result for language '$($pair.Language)' and text '$($pair.Text)'."
            }
            $translatedPairs.Add([pscustomobject]@{
                Text = $pair.Text
                Language = $pair.Language
                Translation = $translation
            })
        }
    }
    finally {
        [Console]::OutputEncoding = $previousConsoleEncoding
    }

    $newline = if ($localeContent.Contains("`r`n")) { "`r`n" } else { "`n" }
    foreach ($languageGroup in ($translatedPairs | Group-Object Language)) {
        $extraObject = [regex]::Match($localeContent, '(?s)const\s+EXTRA_TRANSLATIONS\s*=\s*\{(?<body>.*?)\n\s*\};')
        if (-not $extraObject.Success) { throw 'EXTRA_TRANSLATIONS object was not found in locale.js.' }

        $body = $extraObject.Groups['body'].Value
        $languagePattern = '(?m)^(\s{4}' + [regex]::Escape($languageGroup.Name) + '\s*:\s*\{\r?\n)'
        $languageMatch = [regex]::Match($body, $languagePattern)
        if (-not $languageMatch.Success) { throw "Language block '$($languageGroup.Name)' was not found in EXTRA_TRANSLATIONS." }

        $entries = foreach ($pair in $languageGroup.Group) {
            $keyJson = ConvertTo-Json -InputObject $pair.Text -Compress
            $valueJson = ConvertTo-Json -InputObject $pair.Translation -Compress
            '      {0}: {1}, /* auto-mt */' -f $keyJson, $valueJson
        }
        $insertion = ($entries -join $newline) + $newline
        $insertionIndex = $extraObject.Groups['body'].Index + $languageMatch.Index + $languageMatch.Length
        $localeContent = $localeContent.Insert($insertionIndex, $insertion)
    }

    $localeBytes = [System.IO.File]::ReadAllBytes($localePath)
    $hasUtf8Bom = $localeBytes.Length -ge 3 -and $localeBytes[0] -eq 0xEF -and $localeBytes[1] -eq 0xBB -and $localeBytes[2] -eq 0xBF
    $encoding = [System.Text.UTF8Encoding]::new($hasUtf8Bom)
    [System.IO.File]::WriteAllText($localePath, $localeContent, $encoding)
    Write-Output "Inserted $($translatedPairs.Count) auto-translated entries into EXTRA_TRANSLATIONS. Review them before release."
}
finally {
    Remove-Item -Path $tempDirectory -Recurse -Force -ErrorAction SilentlyContinue
}
