<#
.SYNOPSIS
    Strict equivalence verification between monolithic locale.js and split locale.<lang>.js files.
.PARAMETER BaseRef
    Git reference for original locale.js (default: 374c8a5).
#>
[CmdletBinding()]
param(
    [string]$BaseRef = '374c8a5'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

# Compile C# tokenizer for robust, escape-aware JS object parsing
Add-Type -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.Text;

namespace WahlMirai.Tools {
    public static class SplitEquivalenceTokenizer {
        public static Dictionary<string, string> Parse(string text) {
            var result = new Dictionary<string, string>();
            int i = 0;
            int n = text.Length;

            while (i < n) {
                SkipWhitespaceAndComments(text, ref i, n);
                if (i >= n) break;

                char c = text[i];
                if (c == '{' || c == '}' || c == ',' || c == ';') {
                    i++;
                    continue;
                }

                string key = null;
                if (c == '"' || c == '\'') {
                    key = ReadString(text, ref i, n);
                } else if (char.IsLetter(c) || c == '_' || c == '$') {
                    key = ReadIdentifier(text, ref i, n);
                } else {
                    i++;
                    continue;
                }

                SkipWhitespaceAndComments(text, ref i, n);
                if (i >= n || text[i] != ':') continue;
                i++; // skip colon

                SkipWhitespaceAndComments(text, ref i, n);
                if (i >= n) break;

                if (text[i] == '"' || text[i] == '\'') {
                    string val = ReadString(text, ref i, n);
                    result[key] = val;
                } else if (text[i] == '{') {
                    i++; // enter nested object
                }
            }

            return result;
        }

        private static void SkipWhitespaceAndComments(string text, ref int i, int n) {
            while (i < n) {
                char c = text[i];
                if (char.IsWhiteSpace(c)) {
                    i++;
                } else if (c == '/' && i + 1 < n && text[i + 1] == '/') {
                    i += 2;
                    while (i < n && text[i] != '\n') i++;
                    if (i < n && text[i] == '\n') i++;
                } else if (c == '/' && i + 1 < n && text[i + 1] == '*') {
                    i += 2;
                    while (i + 1 < n && !(text[i] == '*' && text[i + 1] == '/')) i++;
                    if (i + 1 < n) i += 2;
                } else {
                    break;
                }
            }
        }

        private static string ReadIdentifier(string text, ref int i, int n) {
            int start = i;
            while (i < n && (char.IsLetterOrDigit(text[i]) || text[i] == '_' || text[i] == '$')) {
                i++;
            }
            return text.Substring(start, i - start);
        }

        private static string ReadString(string text, ref int i, int n) {
            char quote = text[i];
            i++; // skip opening quote
            var sb = new StringBuilder();

            while (i < n) {
                char c = text[i];
                if (c == '\\' && i + 1 < n) {
                    char next = text[i + 1];
                    if (next == quote) { sb.Append(quote); i += 2; }
                    else if (next == '\\') { sb.Append('\\'); i += 2; }
                    else if (next == 'n') { sb.Append('\n'); i += 2; }
                    else if (next == 'r') { sb.Append('\r'); i += 2; }
                    else if (next == 't') { sb.Append('\t'); i += 2; }
                    else if (next == 'u' && i + 5 < n) {
                        string hex = text.Substring(i + 2, 4);
                        int code = Convert.ToInt32(hex, 16);
                        sb.Append((char)code);
                        i += 6;
                    } else {
                        sb.Append(next);
                        i += 2;
                    }
                } else if (c == quote) {
                    i++; // skip closing quote
                    break;
                } else {
                    sb.Append(c);
                    i++;
                }
            }
            return sb.ToString();
        }
    }
}
"@

# Read original locale.js via git show at BaseRef
$gitProcess = [System.Diagnostics.Process]::Start([System.Diagnostics.ProcessStartInfo]@{
    FileName = "git"
    Arguments = "show ${BaseRef}:WahlMirai.Web/wwwroot/js/locale.js"
    WorkingDirectory = $repoRoot
    RedirectStandardOutput = $true
    UseShellExecute = $false
})
$ms = [System.IO.MemoryStream]::new()
$gitProcess.StandardOutput.BaseStream.CopyTo($ms)
$gitProcess.WaitForExit()

if ($gitProcess.ExitCode -ne 0) {
    Write-Error "Failed to retrieve ${BaseRef}:WahlMirai.Web/wwwroot/js/locale.js from git."
    exit 1
}

$origRaw = [System.Text.Encoding]::UTF8.GetString($ms.ToArray())

function Get-ObjectSection([string]$content, [string]$objName) {
    $pattern = '(?s)const\s+' + [regex]::Escape($objName) + '\s*=\s*\{(.*?)\n\s*\};'
    $m = [regex]::Match($content, $pattern)
    if (-not $m.Success) { throw "Object $objName not found in original locale.js" }
    return $m.Groups[1].Value
}

function Get-LangSlice([string]$objBody, [string]$lang) {
    $pattern = '(?s)\b' + [regex]::Escape($lang) + '\s*:\s*\{(.*?)\}(?:\s*,|\s*$)'
    $m = [regex]::Match($objBody, $pattern)
    if (-not $m.Success) { throw "Lang $lang not found in object block" }
    return $m.Groups[1].Value
}

function Get-SplitSlice([string]$content, [string]$name, [string]$file) {
    $p = '(?s)\b' + [regex]::Escape($name) + '\s*:\s*\{(.*?)\}(?:\s*,|\s*;\s*$|\s*\}\s*;\s*$)'
    $m = [regex]::Match($content, $p)
    if (-not $m.Success) { throw "$name not found in $file" }
    return $m.Groups[1].Value
}

function Get-Sha256([string]$text) {
    $normalized = $text.Replace("`r`n", "`n")
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($normalized)
    $hasher = [System.Security.Cryptography.SHA256]::Create()
    $hash = $hasher.ComputeHash($bytes)
    return [BitConverter]::ToString($hash).Replace('-', '').ToLower()
}

$origDictBody = Get-ObjectSection $origRaw 'DICTIONARY'
$origExtraBody = Get-ObjectSection $origRaw 'EXTRA_TRANSLATIONS'
$origChatBody = Get-ObjectSection $origRaw 'chatbotDraftTranslations'
$origAjaxBody = Get-ObjectSection $origRaw 'ajaxAndModalTranslations'

$languages = @('en', 'de', 'fr', 'ja', 'zh')
$sections = @('DICTIONARY', 'EXTRA_TRANSLATIONS', 'chatbotDraftTranslations', 'ajaxAndModalTranslations')

$hasErrors = $false

Write-Output "=== Strict Equivalence Verification (BaseRef: $BaseRef) ==="
Write-Output ""

foreach ($lang in $languages) {
    $filePath = Join-Path $repoRoot "WahlMirai.Web\wwwroot\js\locale.$lang.js"
    if (-not (Test-Path $filePath)) {
        Write-Error "Target file not found: $filePath"
        $hasErrors = $true
        continue
    }

    $splitContent = [System.IO.File]::ReadAllText($filePath, [System.Text.Encoding]::UTF8)

    # 1. SHA-256 byte-level slice checks
    $origSlices = @{
        'DICTIONARY' = Get-LangSlice $origDictBody $lang
        'EXTRA_TRANSLATIONS' = Get-LangSlice $origExtraBody $lang
        'chatbotDraftTranslations' = Get-LangSlice $origChatBody $lang
        'ajaxAndModalTranslations' = Get-LangSlice $origAjaxBody $lang
    }

    $splitSlices = @{
        'DICTIONARY' = Get-SplitSlice $splitContent 'DICTIONARY' $filePath
        'EXTRA_TRANSLATIONS' = Get-SplitSlice $splitContent 'EXTRA_TRANSLATIONS' $filePath
        'chatbotDraftTranslations' = Get-SplitSlice $splitContent 'chatbotDraftTranslations' $filePath
        'ajaxAndModalTranslations' = Get-SplitSlice $splitContent 'ajaxAndModalTranslations' $filePath
    }

    $allShaMatch = $true
    foreach ($sec in $sections) {
        $hashOrig = Get-Sha256 $origSlices[$sec]
        $hashSplit = Get-Sha256 $splitSlices[$sec]
        if ($hashOrig -ne $hashSplit) {
            Write-Host "  [FAIL] $lang $sec SHA-256 mismatch! Orig: $hashOrig, Split: $hashSplit" -ForegroundColor Red
            $allShaMatch = $false
            $hasErrors = $true
        }
    }

    # 2. Key-value maps comparison across all sections
    $origMap = [ordered]@{}
    $splitMap = [ordered]@{}

    foreach ($sec in $sections) {
        $secOrig = [WahlMirai.Tools.SplitEquivalenceTokenizer]::Parse($origSlices[$sec])
        foreach ($k in $secOrig.Keys) { $origMap[$k] = $secOrig[$k] }

        $secSplit = [WahlMirai.Tools.SplitEquivalenceTokenizer]::Parse($splitSlices[$sec])
        foreach ($k in $secSplit.Keys) { $splitMap[$k] = $secSplit[$k] }
    }

    $missingKeys = @()
    $extraKeys = @()
    $changedValues = @()

    foreach ($k in $origMap.Keys) {
        if (-not $splitMap.Contains($k)) {
            $missingKeys += $k
        } elseif ($origMap[$k] -ne $splitMap[$k]) {
            $changedValues += "$k (Orig: '$($origMap[$k])' vs Split: '$($splitMap[$k])')"
        }
    }

    foreach ($k in $splitMap.Keys) {
        if (-not $origMap.Contains($k)) {
            $extraKeys += $k
        }
    }

    # Verify duplicate count check (DICTIONARY + EXTRA_TRANSLATIONS count)
    $dMap = [WahlMirai.Tools.SplitEquivalenceTokenizer]::Parse($splitSlices['DICTIONARY'])
    $eMap = [WahlMirai.Tools.SplitEquivalenceTokenizer]::Parse($splitSlices['EXTRA_TRANSLATIONS'])
    
    # Check verify-duplicates regex simulation
    . (Join-Path $repoRoot 'scripts\i18n-common.ps1')
    $dictKeys = Get-DictionaryKeys -Content $splitContent -Language $lang -ObjectName 'DICTIONARY'
    $extraKeysGate = Get-DictionaryKeys -Content $splitContent -Language $lang -ObjectName 'EXTRA_TRANSLATIONS'
    $gateTotalKeys = $dictKeys.Count + $extraKeysGate.Count

    $status = if ($allShaMatch -and $missingKeys.Count -eq 0 -and $extraKeys.Count -eq 0 -and $changedValues.Count -eq 0 -and $gateTotalKeys -eq 888) { 'PASS' } else { 'FAIL' }
    if ($status -eq 'FAIL') { $hasErrors = $true }

    Write-Output "[$lang] $status"
    Write-Output "  - SHA-256 slices (4/4): $(if ($allShaMatch) { 'MATCH' } else { 'MISMATCH' })"
    Write-Output "  - Key counts: $($splitMap.Count) total keys parsed | Gate total (DICT+EXTRA): $gateTotalKeys (expected: 888)"
    Write-Output "  - Discrepancies: missing=$($missingKeys.Count), extra=$($extraKeys.Count), changed=$($changedValues.Count)"
    if ($missingKeys.Count -gt 0) { Write-Output "    Missing: $($missingKeys -join ', ')" }
    if ($extraKeys.Count -gt 0) { Write-Output "    Extra: $($extraKeys -join ', ')" }
    if ($changedValues.Count -gt 0) { Write-Output "    Changed: $($changedValues -join ', ')" }
    Write-Output ""
}

if ($hasErrors) {
    Write-Output "RESULT: FAIL - Equivalence check detected discrepancies."
    exit 1
} else {
    Write-Output "RESULT: PASS - Total parity verified across all 5 languages (0 missing, 0 extra, 0 changed)."
    exit 0
}
