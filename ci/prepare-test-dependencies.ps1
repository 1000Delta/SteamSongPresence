$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$deps = Get-Content (Join-Path $PSScriptRoot 'dependencies.json') -Raw | ConvertFrom-Json
$directory = Join-Path $root '.ci-deps'
New-Item -ItemType Directory -Path $directory -Force | Out-Null
$archive = Join-Path $directory 'BSIPA-4.3.7.zip'
Invoke-WebRequest -Uri $deps.bsipaPackage.url -OutFile $archive
if ((Get-FileHash $archive -Algorithm SHA256).Hash -ne $deps.bsipaPackage.sha256) { throw 'Official BSIPA package checksum mismatch.' }
$extracted = Join-Path $directory 'BSIPA-4.3.7'
Expand-Archive -LiteralPath $archive -DestinationPath $extracted -Force
# Load only the metadata inspection library; never run IPA.exe or an installer.
Join-Path $extracted $deps.bsipaPackage.file
