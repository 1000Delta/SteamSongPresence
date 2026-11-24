# Build transparency and security

## What runs in the game

The plugin reads the current song name, difficulty and pause state on Unity's main thread, then updates Rich Presence through the game's Steamworks context. Runtime work consists of four Harmony postfixes, scene callbacks and a retry ticker. BSIPA logs contain song text and operation errors.

v0.5.0 removes the HTTP diagnostic service and its progress-data hook. Assembly checks reject HTTP/socket/JSON type references and obsolete diagnostic types. Callback guards isolate managed exceptions, including logging failures, and exit cleanup runs as independent stages. These checks do not establish that every possible game or native failure is recoverable.

The source is public and unobfuscated. Install only a package for a supported game version. Remove `Plugins/SteamSongPresence.dll` while the game is closed to uninstall.

## How release files are produced

The complete workflow is [.github/workflows/release.yml](.github/workflows/release.yml). It runs on GitHub-hosted runners and:

1. Checks out the plugin source at the triggering commit.
2. Fetches compile-only references for each game target from `beat-forge/beatsaber-stripped` (1.40.8) or `nicoco007/BeatSaberReferenceAssemblies` (1.46.0), at exact commits in [ci/dependencies.json](ci/dependencies.json). These contain type signatures with implementations stripped; they are not included in release ZIPs.
3. Uses a pinned Microsoft .NET SDK. Only the test harness targets modern .NET; the game plugin remains .NET Framework 4.7.2.
4. Downloads the official BSIPA 4.3.7 release ZIP over HTTPS and checks its pinned SHA-256 **before extraction**. Its IPA and Harmony libraries are explicit compiler references; its Mono.Cecil metadata library runs the checks. The build uses these extracted library files.
5. Compiles the plugin from source and runs state, fault-injection, Steam-adapter and game-hook metadata checks. References resolve from explicit HintPaths and framework reference directories; SDK and reference sources are explicit.
6. Creates a ZIP with exactly one entry: `Plugins/SteamSongPresence.dll`. It rejects extra entries and verifies the embedded game/plugin versions against the assembly metadata. ZIP entry timestamps are fixed and compiler paths are normalized.
7. Records source commit, workflow URL, SDK, runner image, reference commit, dependency hashes and output hashes in a separate `BUILDINFO-bs<game-version>.json` for each build, then generates `SHA256SUMS-bs<game-version>.txt`.
8. Transfers the verified 1.40.8 outputs to a separate publishing job, generates signed GitHub artifact attestations and creates the GitHub Release. Candidate releases are marked Pre-release while live validation is pending. The same attachments are promoted after regression and submitted to BeatMods. The 1.46.0 build remains a compile-check target while its live validation is deferred.

Third-party Actions are pinned to full commit hashes. The build job has read-only repository permissions. The separate publishing job uses the run-scoped GitHub token with release and attestation permissions; pull-request runs are limited to building and checking. Releases fail if a release with that version already exists.

The release ZIP contains the plugin DLL compiled by that Actions run. Build reports and checksums are published as separate assets.

## Verify a download

Check the public workflow's source commit and successful checks. Compare the ZIP with its matching `SHA256SUMS-bs<game-version>.txt`:

```powershell
Get-FileHash ./SteamSongPresence-0.5.0-bs1.40.8.zip -Algorithm SHA256
```

With the official GitHub CLI, verify its build provenance:

```sh
gh attestation verify SteamSongPresence-0.5.0-bs1.40.8.zip --repo 1000Delta/SteamSongPresence
```

Checksums establish file integrity. Attestations establish which repository/workflow produced a file; neither replaces code review or guarantees the absence of vulnerabilities.

## Reproduce a build

Check out the source commit listed in `BUILDINFO-bs<game-version>.json`, install the SDK version from `ci/dependencies.json`, and use the same Windows runner image for closest toolchain parity. Obtain the pinned reference commit:

```powershell
$deps = Get-Content ./ci/dependencies.json -Raw | ConvertFrom-Json
$gameVersion = '1.40.8'
$target = $deps.targets.PSObject.Properties[$gameVersion].Value
git clone --no-checkout --filter=blob:none "https://github.com/$($target.referenceRepository).git" Refs
git -C Refs checkout --detach $target.referenceCommit
$cecil = ./ci/prepare-test-dependencies.ps1
$refs = Join-Path (Join-Path (Get-Location) Refs) $target.subdirectory
$bsipa = Join-Path (Get-Location) '.ci-deps/BSIPA-4.3.7'
./build.ps1 -GameDir $refs -TargetVersion $gameVersion -TestTargetFramework net10.0 -CecilPath $cecil -BsipaDir $bsipa
./ci/verify-package.ps1 -GameDir $refs -GameVersion $gameVersion -CecilPath $cecil -BsipaDir $bsipa
```

Build hashes depend on the source and toolchain. `BUILDINFO-bs<game-version>.json` records the specific workflow run; compare plugin/ZIP hashes using the corresponding inputs.

## Report a concern

Open a repository issue describing the affected version and behavior. Do not include Steam credentials, tokens, private logs or other sensitive information.
