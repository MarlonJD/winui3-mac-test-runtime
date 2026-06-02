# Threat Model And Supply-Chain Policy

Date: 2026-06-02

Scope: the public Wine-free WinUI 3 macOS compatibility runtime, runner,
renderer, XAML compiler, fixture corpus, Windows reference workflow, and NuGet
packages in this repository.

## Trust Boundaries

- The runner builds and executes user-provided source projects. Treat projects
  as untrusted code unless they come from the current repository fixture set.
- macOS runtime artifacts may contain application text, resource keys, file
  paths, and screenshots. Do not upload artifacts from private apps to public
  CI.
- Native Windows reference artifacts must come from public clean-room fixture
  content. Private product screenshots, private repositories, secrets, and
  customer data are not allowed as evidence.
- The Windows App SDK runtime installer, NuGet restore, GitHub Actions runner
  image, and .NET SDK are external supply-chain inputs.

## Primary Risks

| Risk | Mitigation |
| --- | --- |
| Untrusted app source executes during local runner or CI smoke tests. | Run only public fixtures in public CI; run private consumers in their own isolated CI; document that the runner is not a sandbox. |
| Artifact privacy leak through screenshots, tree JSON, accessibility JSON, SARIF, or logs. | Keep public evidence clean-room only; run `tools/private-name-denylist/private-name-scan.sh`; avoid uploading private artifacts. |
| Unsupported WinUI APIs silently pass and create false production claims. | Catalog statuses and strict diagnostics fail unknown or unavailable behavior. |
| Native reference drift from hosted Windows runner or Windows App SDK changes. | Preserve `windows-reference.json` provenance with run ID, commit SHA, runner image, viewport, and theme. Review drift before changing thresholds. |
| Dependency or package tampering. | Use NuGet lock/provenance review before release, keep package metadata complete, and retain CI build artifacts for release candidates. |
| Package publishing without release evidence. | `release-check` requires package dry-run artifacts, release/security docs, metadata, and provenance workflow references before the dry run passes. |

## Dependency Policy

- Prefer framework and SDK dependencies already used by the repository.
- New runtime dependencies require a public license compatible with
  `LGPL-3.0-or-later`, a clear maintenance owner, and test coverage for the
  behavior they enable.
- Native Windows workflow dependencies must be public and versioned. The current
  Windows App Runtime installer URL is pinned to Windows App SDK `1.7`.
- Production release candidates must retain `dotnet pack` outputs and
  `release-readiness.json` as provenance artifacts.

## Safe CI Usage

- Public GitHub Actions may run only clean-room fixture projects in this repo.
- Private downstream apps should run on private/self-hosted macOS runners and
  decide their own artifact retention.
- The runner should be invoked from a clean checkout or disposable workspace
  when evaluating untrusted source.
- Do not grant the runner secrets that are not needed for the test. The runner
  does not require cloud credentials for local compatibility checks.

## Artifact Privacy

Generated artifacts can include UI text, selected item values, accessibility
labels, resource keys, file paths, and screenshots. Public artifacts must be
reviewed before they are checked into the repository or attached to public
issues. Use clean-room fixture data for public evidence.

## Release Security Gate

Before publishing a production package, release owners must have:

- passing `dotnet build`, `dotnet test`, corpus ingestion, private-name scan,
  benchmark/flake gate, package dry run, and `release-check`;
- public native WinUI reference workflow run IDs for changed visual scenarios;
- package artifacts with repository URL, license expression, readme, version,
  and release notes;
- signing/provenance evidence for the package publishing channel, or an
  explicit alpha-only unsigned package decision recorded in release notes;
- a rollback plan that identifies the previous package version and affected
  compatibility contract changes.
