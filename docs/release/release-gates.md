# Release, Benchmark, And Flake Gates

Date: 2026-06-02

This document defines the Sprint 6 operational gates for external consumers.
The gates are intentionally conservative: they create evidence and block
publishing when required artifacts are absent, but they do not claim complete
WinUI 3 production readiness.

## CI Gates

The main CI workflow runs:

- private-name denylist scan;
- `dotnet build -v minimal /m:1 /nr:false`;
- `dotnet test --no-build`;
- corpus ingestion with `--check`;
- benchmark and flake gate through `winui3-mac-runner benchmark`;
- package dry run through `dotnet pack`;
- release dry run through `winui3-mac-runner release-check`;
- artifact upload for corpus, benchmark, release readiness, and packages.

## Benchmark Gate

`winui3-mac-runner benchmark` writes `benchmark.json` with:

- corpus ingestion duration;
- XAML compile duration for public corpus fixture XAML;
- interaction script duration;
- `skia-v2` render duration;
- runtime artifact generation duration;
- managed memory after the gate;
- flake rate from repeated interaction execution.

The command exits non-zero when a measured duration exceeds its threshold or
when the repeated interaction flake rate is non-zero. Thresholds are broad
enough for hosted CI variance and are meant to detect large regressions, not to
serve as microbenchmark targets.

## Release Check Gate

`winui3-mac-runner release-check` writes `release-readiness.json` and verifies:

- security threat model exists;
- release gate documentation exists;
- production support policy exists;
- compatibility contract and Level 7 release docs exist;
- package metadata has semver, repository URL, and license expression;
- every expected package dry-run artifact exists;
- signing/provenance dry-run evidence can be attached to the release.

`publishAllowed` is always `false` in CI dry runs. A human release owner must
attach signing/provenance evidence and confirm the support policy before any
production publish.

## Package Dry Run

CI packs the public package set into `artifacts/packages`:

- `MarlonJD.WinUI3.MacRunner`;
- `MarlonJD.WinUI3.MacCompat`;
- `MarlonJD.WinUI3.MacRuntime`;
- `MarlonJD.WinUI3.MacXaml`;
- `MarlonJD.WinUI3.MacRenderer.Skia`;
- `MarlonJD.WinUI3.MacTest.Sdk`.

The release check fails if any package is missing.

## Retention And Triage

CI uploads production gate artifacts on every run. Failed benchmark, package,
or release-check artifacts should be inspected before re-running. A release
owner may raise thresholds only with a documented reason and a before/after
artifact comparison.

Hosted runner image changes can affect visual references and performance
timing. Keep run IDs, commit SHAs, runner image metadata, and package artifacts
with release candidate notes.
