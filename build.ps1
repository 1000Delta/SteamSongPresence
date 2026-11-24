param(
    [string]$GameDir = "$env:USERPROFILE\BSManager\BSInstances\1.40.8",
    [ValidateSet('', '1.40.8', '1.46.0')][string]$TargetVersion = '',
    [string]$TestTargetFramework = 'net6.0',
    [string]$CecilPath = ''
)
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.build-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
. (Join-Path $PSScriptRoot 'game-version.ps1')
$gameVersion = if ($TargetVersion) { $TargetVersion } else { Get-BeatSaberVersion $GameDir }
if ($TargetVersion -and (Test-Path -LiteralPath (Join-Path $GameDir 'Beat Saber_Data\globalgamemanagers'))) {
    if ((Get-BeatSaberVersion $GameDir) -ne $TargetVersion) { throw 'Explicit version does not match the game assets.' }
}
if ($gameVersion -notin '1.40.8','1.46.0') { throw "Unsupported build target: $gameVersion" }
$metadata = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
$metadata.gameVersion = $gameVersion
if ($gameVersion -eq '1.46.0') { $metadata.dependsOn.BSIPA = '^4.3.7' }
$generatedDir = Join-Path $PSScriptRoot "obj\bs$gameVersion"
New-Item -ItemType Directory -Path $generatedDir -Force | Out-Null
$manifestPath = Join-Path $generatedDir 'manifest.json'
[IO.File]::WriteAllText($manifestPath, ($metadata | ConvertTo-Json -Depth 5))
dotnet build (Join-Path $PSScriptRoot 'SteamSongPresence.csproj') -c Release --nologo "-p:GameDir=$GameDir" "-p:TargetGameVersion=$gameVersion" "-p:ManifestPath=$manifestPath" "-p:PathMap=$PSScriptRoot=/_/SteamSongPresence" -p:ContinuousIntegrationBuild=true -p:UseSharedCompilation=false -p:RestoreIgnoreFailedSources=true
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }
if (!$CecilPath) { $CecilPath = Join-Path $GameDir 'Libs\Mono.Cecil.dll' }
dotnet run --project (Join-Path $PSScriptRoot 'tests\SteamSongPresence.Tests.csproj') -c Release "-p:GameDir=$GameDir" "-p:TargetFramework=$TestTargetFramework" "-p:CecilPath=$CecilPath" -p:UseSharedCompilation=false -- $GameDir
if ($LASTEXITCODE -ne 0) { throw 'Validation failed.' }
$stage = Join-Path $PSScriptRoot "dist\bs$gameVersion"
New-Item -ItemType Directory -Path (Join-Path $stage 'Plugins') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "bin\Release\bs$gameVersion\SteamSongPresence.dll") -Destination (Join-Path $stage 'Plugins\SteamSongPresence.dll') -Force
# BeatMods requires all package files to be under Plugins or Libs.
$zipPath = Join-Path $PSScriptRoot "dist\SteamSongPresence-$($metadata.version)-bs$gameVersion.zip"
Add-Type -AssemblyName System.IO.Compression
$output = [IO.File]::Open($zipPath, [IO.FileMode]::Create)
$zip = [IO.Compression.ZipArchive]::new($output, [IO.Compression.ZipArchiveMode]::Create)
try {
    $entry = $zip.CreateEntry('Plugins/SteamSongPresence.dll', [IO.Compression.CompressionLevel]::Optimal)
    $entry.LastWriteTime = [DateTimeOffset]::new(2000,1,1,0,0,0,[TimeSpan]::Zero)
    $stream = $entry.Open()
    try {
        $bytes = [IO.File]::ReadAllBytes((Join-Path $stage 'Plugins\SteamSongPresence.dll'))
        $stream.Write($bytes, 0, $bytes.Length)
    } finally { $stream.Dispose() }
} finally { $zip.Dispose(); $output.Dispose() }
Get-FileHash -LiteralPath (Join-Path $stage 'Plugins\SteamSongPresence.dll') -Algorithm SHA256
