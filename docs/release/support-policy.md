# Production Support Policy

Date: 2026-06-02

This policy defines the production support boundary for the source-level WinUI 3
macOS compatibility runtime. It is intentionally narrower than arbitrary WinUI
3 application compatibility.

## Supported Production Scope

The production-ready claim applies only to the documented public source-level
subset:

- clean-room public fixture and corpus projects in `fixtures/corpus.json`;
- APIs and XAML constructs cataloged as `supported` or `partial` in
  `docs/compatibility/winui-api-compatibility.catalog.json`;
- production-ring components listed in
  `docs/compatibility/production-component-targets.md` when their scenario
  requirements meet the declared minimum visual grade;
- deterministic runner artifacts documented in `docs/architecture/artifacts.md`;
- native WinUI reference provenance from public `windows-native-screenshot.yml`
  runs;
- release, benchmark, flake, package, security, and private-name gates recorded
  in CI.

Production support means consumers can use the runtime as a source-level
compatibility, diagnostics, artifact, interaction, accessibility-export, and
visual evidence gate for that subset. It does not mean the runtime can execute
Windows binaries or replace Windows App SDK validation.

## Unsupported Or Excluded Scope

The following remain excluded from production support until they are explicitly
promoted through catalog, fixture, renderer, native-reference, and release
evidence gates:

- arbitrary WinUI 3 apps outside the documented corpus and production-ring
  scenarios;
- `.exe`, `.msix`, packaged Windows App SDK execution, Wine-backed execution,
  and real Windows App SDK target execution on macOS;
- uncataloged APIs and any catalog entries marked `planned`, `windows-only`, or
  `not supported`;
- Mica, Acrylic, system backdrops, compositor effects, shadows, transforms,
  animation parity, and broad Fluent interaction-state parity;
- full templates, visual states, dynamic resources, virtualization, advanced
  text input, IME, WebView2, media, ink, and platform integration APIs;
- private product screenshots, private repositories, private app names, secrets,
  customer data, or proprietary workflow evidence.

Unsupported usage must fail or report through the catalog diagnostics and
versioned artifacts instead of being silently promoted.

## Compatibility And Versioning

- The package version is `0.1.0-alpha.1`; the production claim is a support
  boundary for the documented source-level subset, not a semantic-version
  guarantee for the broader WinUI 3 ecosystem.
- Additive support for new catalog entries may ship in minor or prerelease
  updates.
- Removing or weakening a documented supported/partial behavior requires a
  release note, migration note, and compatibility matrix update.
- Scenario thresholds are compatibility contracts. Threshold changes require a
  before/after artifact comparison and an explanation in release notes or the
  relevant visual evidence doc.

## Support And Triage

Issues should include:

- package version or commit SHA;
- host OS and .NET SDK version;
- project or fixture path using public-safe names;
- runner command;
- relevant artifacts, especially `run.json`, `project-ingestion.json`,
  `unsupported-apis.json`, `diagnostics.sarif`, `visual-run.json`,
  `component-evidence.json`, `interactions.json`, and PNG diff artifacts.

Triage levels:

| Level | Meaning |
| --- | --- |
| Production subset regression | A documented supported/partial production-ring behavior regressed with public-safe reproduction artifacts. |
| Unsupported scope request | The issue touches planned, windows-only, not-supported, or uncataloged behavior. It should be tracked as roadmap or diagnostics work. |
| Evidence hygiene issue | Provenance, private-name safety, artifact retention, or support-policy evidence is missing or stale. |
| Downstream-specific issue | The behavior depends on private app source or unsupported app structure. Keep evidence in the downstream repository. |

## Release Conditions

A production-supporting release candidate must have:

- passing `dotnet build`, `dotnet test`, corpus ingestion, private-name scan,
  benchmark/flake, package dry-run, and release-check gates;
- public native WinUI reference workflow evidence for changed production
  scenarios;
- package artifacts, `benchmark.json`, `release-readiness.json`, and package
  dry-run artifacts retained with the release candidate;
- signing/provenance evidence or an explicit unsigned-alpha decision recorded in
  the release notes;
- a rollback note identifying the previous package version and any compatibility
  contract changes.

`release-check` is a CI dry run and keeps `publishAllowed` false. Publishing
requires a human release owner to attach signing/provenance evidence and confirm
this support policy.
