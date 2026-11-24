param([string]$GameDir = "$env:USERPROFILE\BSManager\BSInstances\1.40.8")
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.build-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
. (Join-Path $PSScriptRoot 'game-version.ps1')
$gameVersion = Get-BeatSaberVersion $GameDir
if ($gameVersion -notin '1.40.8','1.46.0') { throw "Unsupported build target: $gameVersion" }
$metadata = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
$metadata.gameVersion = $gameVersion
if ($gameVersion -eq '1.46.0') { $metadata.dependsOn.BSIPA = '^4.3.7' }
$generatedDir = Join-Path $PSScriptRoot "obj\bs$gameVersion"
New-Item -ItemType Directory -Path $generatedDir -Force | Out-Null
$manifestPath = Join-Path $generatedDir 'manifest.json'
[IO.File]::WriteAllText($manifestPath, ($metadata | ConvertTo-Json -Depth 5))
dotnet build (Join-Path $PSScriptRoot 'SteamSongPresence.csproj') -c Release --nologo "-p:GameDir=$GameDir" "-p:TargetGameVersion=$gameVersion" "-p:ManifestPath=$manifestPath" -p:UseSharedCompilation=false -p:RestoreIgnoreFailedSources=true
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }
dotnet run --project (Join-Path $PSScriptRoot 'tests\SteamSongPresence.Tests.csproj') -c Release "-p:GameDir=$GameDir" -p:UseSharedCompilation=false -- $GameDir
if ($LASTEXITCODE -ne 0) { throw 'Validation failed.' }
$stage = Join-Path $PSScriptRoot "dist\bs$gameVersion"
New-Item -ItemType Directory -Path (Join-Path $stage 'Plugins') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "bin\Release\bs$gameVersion\SteamSongPresence.dll") -Destination (Join-Path $stage 'Plugins\SteamSongPresence.dll') -Force
# BeatMods requires all package files to be under Plugins or Libs.
Compress-Archive -Path (Join-Path $stage 'Plugins') -DestinationPath (Join-Path $PSScriptRoot "dist\SteamSongPresence-$($metadata.version)-bs$gameVersion.zip") -Force
Get-FileHash -LiteralPath (Join-Path $stage 'Plugins\SteamSongPresence.dll') -Algorithm SHA256
