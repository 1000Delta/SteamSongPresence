param([Parameter(Mandatory)][string]$GameDir, [string]$CecilPath = '')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dependencies = Get-Content (Join-Path $PSScriptRoot 'dependencies.json') -Raw | ConvertFrom-Json
$metadata = Get-Content (Join-Path $root 'manifest.json') -Raw | ConvertFrom-Json
$referenceCommit = (git -C $GameDir rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $referenceCommit -ne $dependencies.referenceCommit) { throw 'Reference commit mismatch.' }
$packageName = "SteamSongPresence-$($metadata.version)-bs$($dependencies.gameVersion).zip"
$packagePath = Join-Path $root "dist\$packageName"
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (!$CecilPath) { $CecilPath = Join-Path $GameDir 'Libs\Mono.Cecil.dll' }
Add-Type -Path $CecilPath
$zip = [IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    if ($zip.Entries.Count -ne 1 -or $zip.Entries[0].FullName -cne 'Plugins/SteamSongPresence.dll') { throw 'Unexpected package contents.' }
    $memory = [IO.MemoryStream]::new()
    $entryStream = $zip.Entries[0].Open()
    try { $entryStream.CopyTo($memory) } finally { $entryStream.Dispose() }
    $memory.Position = 0
    $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($memory)
    try {
        $resource = $assembly.MainModule.Resources | Where-Object Name -eq 'SteamSongPresence.manifest.json'
        if (!$resource) { throw 'Embedded manifest is missing.' }
        $embedded = [Text.Encoding]::UTF8.GetString($resource.GetResourceData()) | ConvertFrom-Json
        if ($embedded.version -ne $metadata.version -or $embedded.gameVersion -ne $dependencies.gameVersion -or $assembly.Name.Version.ToString(3) -ne $metadata.version) { throw 'Package version metadata mismatch.' }
    } finally { $assembly.Dispose(); $memory.Dispose() }
} finally { $zip.Dispose() }
$references = Get-ChildItem -LiteralPath (Join-Path $GameDir 'Beat Saber_Data\Managed'),(Join-Path $GameDir 'Libs') -Filter '*.dll' -File | Sort-Object FullName | ForEach-Object {
    [ordered]@{ path = $_.FullName.Substring($GameDir.Length + 1).Replace('\','/'); sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$dllPath = Join-Path $root "dist\bs$($dependencies.gameVersion)\Plugins\SteamSongPresence.dll"
$report = [ordered]@{
    sourceCommit = if ($env:GITHUB_SHA) { $env:GITHUB_SHA } else { (git -C $root rev-parse HEAD).Trim() }
    buildUrl = if ($env:GITHUB_RUN_ID) { "https://github.com/$env:GITHUB_REPOSITORY/actions/runs/$env:GITHUB_RUN_ID" } else { 'local validation' }
    gameVersion = $dependencies.gameVersion
    pluginVersion = $metadata.version
    sdkVersion = (dotnet --version).Trim()
    runnerImage = $env:ImageVersion
    referenceRepository = $dependencies.referenceRepository
    referenceCommit = $referenceCommit
    testDependency = $dependencies.testDependency
    cecilSha256 = (Get-FileHash $CecilPath -Algorithm SHA256).Hash.ToLowerInvariant()
    referenceHashes = @($references)
    dllSha256 = (Get-FileHash $dllPath -Algorithm SHA256).Hash.ToLowerInvariant()
    zipSha256 = (Get-FileHash $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
    packageEntries = @('Plugins/SteamSongPresence.dll')
}
$report | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $root 'dist\BUILDINFO.json') -Encoding utf8NoBOM
$checksums = @($packagePath, (Join-Path $root 'dist\BUILDINFO.json')) | ForEach-Object { "{0}  {1}" -f (Get-FileHash $_ -Algorithm SHA256).Hash.ToLowerInvariant(), [IO.Path]::GetFileName($_) }
[IO.File]::WriteAllText((Join-Path $root 'dist\SHA256SUMS.txt'), ($checksums -join "`n") + "`n")
Write-Output "Verified ${packageName}: one plugin DLL, matching embedded manifest and assembly version."
Write-Output "Source: $($report.sourceCommit); references: $referenceCommit; SDK: $($report.sdkVersion)"
