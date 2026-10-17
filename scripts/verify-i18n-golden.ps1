# scripts/verify-i18n-golden.ps1
# Golden harness runner and verifier for client-side i18n
[CmdletBinding()]
param (
    [ValidateSet('old', 'new')]
    [string]$Engine = 'old',

    [string]$CustomEnginePath = $null,

    [switch]$UpdateBaseline
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$RepoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$BaselinePath = Join-Path $RepoRoot "scripts\golden\i18n-baseline.json"
$HarnessHtmlPath = Join-Path $RepoRoot "scripts\golden\harness.html"

if (-not (Test-Path $HarnessHtmlPath)) {
    throw "Harness HTML not found at: $HarnessHtmlPath"
}

# 1. Locate Microsoft Edge executable
$edgeCandidates = @(
    "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    "C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    (Join-Path $env:LOCALAPPDATA "Microsoft\Edge\Application\msedge.exe")
)

$edgePath = $null
foreach ($cand in $edgeCandidates) {
    if (Test-Path $cand) {
        $edgePath = $cand
        break
    }
}

if (-not $edgePath) {
    throw "Microsoft Edge executable (msedge.exe) not found in standard paths."
}

# 2. Prepare temporary directory for execution
$tempDir = Join-Path $env:TEMP ("i18n-golden-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path (Join-Path $tempDir "engine") -Force | Out-Null
Copy-Item $HarnessHtmlPath (Join-Path $tempDir "harness.html") -Force

# 3. Populate engine scripts
if ($CustomEnginePath) {
    Copy-Item $CustomEnginePath (Join-Path $tempDir "engine\locale.js") -Force
} elseif ($Engine -eq 'old') {
    # Extract original locale.js from base ref 374c8a5
    $targetLocaleJs = Join-Path $tempDir "engine\locale.js"
    & git -C $RepoRoot show "374c8a5:WahlMirai.Web/wwwroot/js/locale.js" | Set-Content -Path $targetLocaleJs -Encoding utf8
} else {
    # New engine (locale-core.js + locale.<lang>.js)
    $coreSrc = Join-Path $RepoRoot "WahlMirai.Web\wwwroot\js\locale-core.js"
    if (-not (Test-Path $coreSrc)) {
        throw "New engine file not found: $coreSrc"
    }
    Copy-Item $coreSrc (Join-Path $tempDir "engine\locale-core.js") -Force
    Get-ChildItem -Path (Join-Path $RepoRoot "WahlMirai.Web\wwwroot\js") -Filter "locale.*.js" | ForEach-Object {
        Copy-Item $_.FullName (Join-Path $tempDir "engine\$($_.Name)") -Force
    }
}

# 4. Find free ephemeral port and start Python http.server
$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()

$pyServer = Start-Process python -ArgumentList @("-m", "http.server", "$port", "--directory", $tempDir) -NoNewWindow -PassThru
Start-Sleep -Milliseconds 600

$languages = @('es', 'en', 'de', 'fr', 'ja', 'zh')
$runResults = [ordered]@{}

try {
    foreach ($lang in $languages) {
        $profileDir = Join-Path $tempDir "profile_$lang"
        $outFile = Join-Path $tempDir "out_$lang.html"
        $url = "http://127.0.0.1:$port/harness.html?lang=$lang&engine=$Engine"

        $edgeArgs = @(
            "--headless=new",
            "--user-data-dir=$profileDir",
            "--virtual-time-budget=12000",
            "--dump-dom",
            $url
        )

        $proc = Start-Process -FilePath $edgePath -ArgumentList $edgeArgs -NoNewWindow -Wait -PassThru -RedirectStandardOutput $outFile -RedirectStandardError (Join-Path $tempDir "err_$lang.txt")

        if (-not (Test-Path $outFile)) {
            throw "Edge dump output file not generated for language: $lang"
        }

        $htmlContent = Get-Content $outFile -Raw -Encoding utf8
        if ($htmlContent -match 'id="golden-result"[^>]*>([^<]+)<') {
            $b64 = $matches[1].Trim()
            $jsonBytes = [System.Convert]::FromBase64String($b64)
            $jsonText = [System.Text.Encoding]::UTF8.GetString($jsonBytes)
            $record = $jsonText | ConvertFrom-Json
            $runResults[$lang] = $record
        } else {
            throw "Failed to extract golden-result payload from DOM dump for language: $lang"
        }
    }
} finally {
    if ($pyServer -and -not $pyServer.HasExited) {
        Stop-Process -Id $pyServer.Id -Force -ErrorAction SilentlyContinue
    }
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}

# 5. Save or verify against baseline
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

if ($UpdateBaseline -or (-not (Test-Path $BaselinePath))) {
    $baselineJson = $runResults | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText($BaselinePath, $baselineJson, $utf8NoBom)
    $fi = Get-Item $BaselinePath
    Write-Host "Baseline generated successfully: $BaselinePath ($($fi.Length) bytes)"
    exit 0
}

# Load existing baseline
$baselineText = [System.IO.File]::ReadAllText($BaselinePath, [System.Text.Encoding]::UTF8)
$baseline = $baselineText | ConvertFrom-Json

$diffs = @()

$allowedDiffsPath = Join-Path $RepoRoot "scripts\golden\allowed-differences.json"
$allowedDifferences = @()
if (Test-Path $allowedDiffsPath) {
    $allowedDifferences = @(Get-Content $allowedDiffsPath -Raw -Encoding utf8 | ConvertFrom-Json)
    foreach ($entry in $allowedDifferences) {
        if ($entry.case -ne 'scenarioN') {
            throw "Invalid allowlist entry case '$($entry.case)'. Only 'scenarioN' is permitted in allowed-differences.json."
        }
    }
}
$allowedApplied = @()

foreach ($lang in $languages) {
    if (-not $baseline.$lang) {
        $diffs += "[DIFF] Lang: $lang missing from baseline"
        continue
    }

    $bLang = $baseline.$lang
    $rLang = $runResults[$lang]

    # Check domInitial
    if ($bLang.domInitial -ne $rLang.domInitial) {
        $diffs += "[DIFF] Lang: $lang | Case: domInitial mismatch`n  Expected: $($bLang.domInitial)`n  Actual:   $($rLang.domInitial)"
    }

    # Check domMutated (strictly identical, no allowlist allowed)
    if ($bLang.domMutated -ne $rLang.domMutated) {
        $diffs += "[DIFF] Lang: $lang | Case: domMutated mismatch`n  Expected: $($bLang.domMutated)`n  Actual:   $($rLang.domMutated)"
    }

    # Check scenarioN
    if ($bLang.scenarioN -and $rLang.scenarioN) {
        $bPh = $bLang.scenarioN.placeholderAfter
        $rPh = $rLang.scenarioN.placeholderAfter
        if ($bPh -ne $rPh) {
            $allowed = $false
            if ($Engine -eq 'new' -and $allowedDifferences) {
                $entry = $allowedDifferences | Where-Object { $_.lang -eq $lang -and $_.case -eq 'scenarioN' }
                if ($entry) {
                    if ($bPh -eq $entry.expectedOld -and $rPh -eq $entry.expectedNew) {
                        $allowed = $true
                        $allowedApplied += "[ALLOWED-DIFF] Lang: $lang | Case: scenarioN`n  Reason: $($entry.reason)"
                    }
                }
            }
            if (-not $allowed) {
                $diffs += "[DIFF] Lang: $lang | Case: scenarioN.placeholderAfter mismatch`n  Expected: '$bPh'`n  Actual:   '$rPh'"
            }
        }
    }

    # Check alerts
    $bAlerts = @($bLang.dialogs.alerts)
    $rAlerts = @($rLang.dialogs.alerts)
    if ($bAlerts.Count -ne $rAlerts.Count) {
        $diffs += "[DIFF] Lang: $lang | Case: alert count mismatch ($($bAlerts.Count) vs $($rAlerts.Count))"
    } else {
        for ($i = 0; $i -lt $bAlerts.Count; $i++) {
            if ($bAlerts[$i] -ne $rAlerts[$i]) {
                $diffs += "[DIFF] Lang: $lang | Case: alert[$i]`n  Expected: '$($bAlerts[$i])'`n  Actual:   '$($rAlerts[$i])'"
            }
        }
    }

    # Check confirms
    $bConfirms = @($bLang.dialogs.confirms)
    $rConfirms = @($rLang.dialogs.confirms)
    if ($bConfirms.Count -ne $rConfirms.Count) {
        $diffs += "[DIFF] Lang: $lang | Case: confirm count mismatch ($($bConfirms.Count) vs $($rConfirms.Count))"
    } else {
        for ($i = 0; $i -lt $bConfirms.Count; $i++) {
            if ($bConfirms[$i] -ne $rConfirms[$i]) {
                $diffs += "[DIFF] Lang: $lang | Case: confirm[$i]`n  Expected: '$($bConfirms[$i])'`n  Actual:   '$($rConfirms[$i])'"
            }
        }
    }

    # Check translateText table
    $bTT = @($bLang.translateText)
    $rTT = @($rLang.translateText)
    if ($bTT.Count -ne $rTT.Count) {
        $diffs += "[DIFF] Lang: $lang | Case: translateText count mismatch ($($bTT.Count) vs $($rTT.Count))"
    } else {
        for ($i = 0; $i -lt $bTT.Count; $i++) {
            $key = $bTT[$i].input
            $expected = $bTT[$i].output
            $actual = $rTT[$i].output
            if ($expected -ne $actual) {
                $diffs += "[DIFF] Lang: $lang | Case: translateText('$key')`n  Expected: '$expected'`n  Actual:   '$actual'"
            }
        }
    }
}

if ($diffs.Count -gt 0) {
    Write-Host "FAILED: $($diffs.Count) difference(s) detected against baseline:" -ForegroundColor Red
    foreach ($d in $diffs) {
        Write-Host $d
    }
    exit 1
} else {
    if ($allowedApplied.Count -gt 0) {
        Write-Host "Allowed differences applied ($($allowedApplied.Count)):" -ForegroundColor Yellow
        foreach ($a in $allowedApplied) {
            Write-Host $a -ForegroundColor Yellow
        }
    }
    Write-Host "SUCCESS: Zero differences detected across all 6 languages against baseline (Engine: $Engine)." -ForegroundColor Green
    exit 0
}
