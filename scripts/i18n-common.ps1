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
        $key = [regex]::Replace($key, '\\u([0-9a-fA-F]{4})', {
            param($unicodeMatch)
            [char][Convert]::ToInt32($unicodeMatch.Groups[1].Value, 16)
        })
        $keys[$key] = $true
    }
    return $keys
}

function Get-MissingTranslations {
    param(
        [object[]]$Strings,
        [hashtable]$DictionaryKeys,
        [string]$Language
    )

    return @($Strings | Where-Object { -not $DictionaryKeys[$Language].ContainsKey($_.Text) })
}
