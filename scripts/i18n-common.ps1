function Get-DictionaryKeys {
    param(
        [string]$Content,
        [string]$Language,
        [string]$ObjectName = 'DICTIONARY'
    )

    # Formato de archivo dividido (locale.<lang>.js)
    $splitPattern = '(?s)\b' + [regex]::Escape($ObjectName) + '\s*:\s*\{(?<body>.*?)(?:\n\s*\},|\n\s*\}\s*;\s*$|\}\s*,|\}\s*$)'
    $splitMatch = [regex]::Match($Content, $splitPattern)
    if (-not $splitMatch.Success) { return @{} }
    $body = $splitMatch.Groups['body'].Value

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
