# Build transparency and security

## What runs in the game

The plugin reads current song metadata and pause state, then calls the game's existing Steam Rich Presence API. It also exposes read-only song diagnostics on `127.0.0.1:6749` to programs on the same computer. It does not fetch remote code, self-update, start external programs, request administrator privileges, collect credentials, initialize another Steam login, or modify Steam's game identity/playtime.

The source is public and unobfuscated. Install only a package for a supported game version. Remove `Plugins/SteamSongPresence.dll` while the game is closed to uninstall.

## How release files are produced

The complete workflow is [.github/workflows/release.yml](.github/workflows/release.yml). It runs on GitHub-hosted runners and:

1. Checks out the plugin source at the triggering commit.
2. Fetches compile-only references for each game target from `beat-forge/beatsaber-stripped` (1.40.8) or `nicoco007/BeatSaberReferenceAssemblies` (1.46.0), at exact commits in [ci/dependencies.json](ci/dependencies.json). These contain type signatures with implementations stripped; they are not included in release ZIPs.
3. Uses a pinned Microsoft .NET SDK. Only the test harness targets modern .NET; the game plugin remains .NET Framework 4.7.2.
4. Downloads the official BSIPA 4.3.7 release ZIP over HTTPS and checks its pinned SHA-256 **before extraction**. Its IPA, Harmony and JSON libraries are explicit compiler references; its Mono.Cecil metadata library runs the checks. The installer is never executed. The checked-out game reference trees remain unchanged.
5. Compiles the plugin from source and runs state, HTTP and game-hook metadata checks. Implicit scanning of DLL candidates in the working directory is disabled: references resolve from explicit HintPaths and framework reference directories. There are no NuGet package references in the plugin or test projects; SDK and reference sources are explicit.
6. Creates a ZIP with exactly one entry: `Plugins/SteamSongPresence.dll`. It rejects extra entries and verifies the embedded game/plugin versions against the assembly metadata. ZIP entry timestamps are fixed and compiler paths are normalized.
7. Records source commit, workflow URL, SDK, runner image, reference commit, dependency hashes and output hashes in a separate `BUILDINFO-bs<game-version>.json` for each build, then generates `SHA256SUMS-bs<game-version>.txt`.
8. Transfers only the verified outputs to a separate publishing job, generates signed GitHub artifact attestations and creates the GitHub Release.

Third-party Actions are pinned to full commit hashes. The build job has read-only repository permissions. Only the separate publishing job has release and attestation permissions; pull requests cannot publish. No personal access token or additional repository secret is required. Releases fail rather than overwrite an existing release with the same version.

No locally built binary is uploaded as a release asset, and no game assemblies, dependency DLLs, installers or scripts are bundled with the plugin.

## Verify a download

Check the public workflow's source commit and successful checks. Compare the ZIP with its matching `SHA256SUMS-bs<game-version>.txt`:

```powershell
Get-FileHash ./SteamSongPresence-0.4.0-bs1.46.0.zip -Algorithm SHA256
```

With the official GitHub CLI, verify its build provenance:

```sh
gh attestation verify SteamSongPresence-0.4.0-bs1.46.0.zip --repo 1000Delta/SteamSongPresence
```

Checksums establish file integrity. Attestations establish which repository/workflow produced a file; neither replaces code review or guarantees the absence of vulnerabilities.

## Reproduce a build

Check out the source commit listed in `BUILDINFO.json`, install the SDK version from `ci/dependencies.json`, and use the same Windows runner image for closest toolchain parity. Obtain only the pinned reference commit:

```powershell
$deps = Get-Content ./ci/dependencies.json -Raw | ConvertFrom-Json
$gameVersion = '1.46.0'
$target = $deps.targets.PSObject.Properties[$gameVersion].Value
git clone --no-checkout --filter=blob:none "https://github.com/$($target.referenceRepository).git" Refs
git -C Refs checkout --detach $target.referenceCommit
$cecil = ./ci/prepare-test-dependencies.ps1
$refs = Join-Path (Join-Path (Get-Location) Refs) $target.subdirectory
$bsipa = Join-Path (Get-Location) '.ci-deps/BSIPA-4.3.7'
./build.ps1 -GameDir $refs -TargetVersion $gameVersion -TestTargetFramework net10.0 -CecilPath $cecil -BsipaDir $bsipa
./ci/verify-package.ps1 -GameDir $refs -GameVersion $gameVersion -CecilPath $cecil -BsipaDir $bsipa
```

The project does not promise byte-identical builds across different SDKs or operating systems. `BUILDINFO.json` intentionally records the specific workflow run. Compare the plugin/ZIP hashes from the same source and toolchain, rather than expecting that report itself to be identical.

## Report a concern

Open a repository issue describing the affected version and behavior. Do not include Steam credentials, tokens, private logs or other sensitive information.
