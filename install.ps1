param([string]$GameDir = "$env:USERPROFILE\BSManager\BSInstances\1.40.8")
$ErrorActionPreference = 'Stop'
if (Get-Process -Name 'Beat Saber' -ErrorAction SilentlyContinue) { throw 'Close Beat Saber before installing.' }
. (Join-Path $PSScriptRoot 'game-version.ps1')
$gameVersion = Get-BeatSaberVersion $GameDir
if ($gameVersion -notin '1.40.8','1.46.0') { throw "Unsupported game version: $gameVersion" }
$source = Join-Path $PSScriptRoot "bin\Release\bs$gameVersion\SteamSongPresence.dll"
if (!(Test-Path -LiteralPath $source)) { throw 'Build the plugin or extract the release ZIP first.' }
$destination = Join-Path $GameDir 'Plugins\SteamSongPresence.dll'
New-Item -ItemType Directory -Path (Join-Path $GameDir 'Plugins') -Force | Out-Null
if (Test-Path -LiteralPath $destination) {
    Copy-Item -LiteralPath $destination -Destination ($destination + '.' + (Get-Date -Format 'yyyyMMddHHmmss') + '.bak')
}
Copy-Item -LiteralPath $source -Destination $destination -Force
Write-Output "Installed: $destination"
Write-Output 'Start the selected game instance and check its Steam friends status.'
