function Get-BeatSaberVersion([string]$GameDir) {
    # BeatSaberVersion.txt belongs to BSIPA and can survive a Steam update.
    $asset = Join-Path $GameDir 'Beat Saber_Data\globalgamemanagers'
    if (!(Test-Path -LiteralPath $asset)) { throw 'Beat Saber installation not found.' }
    $text = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($asset))
    $match = [regex]::Match($text, '\b(1\.\d+\.\d+)_\d+\b')
    if (!$match.Success) { throw 'Could not determine the game version from Unity assets.' }
    return $match.Groups[1].Value
}
