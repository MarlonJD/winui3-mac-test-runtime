# WinUI3 Mac Test Runtime Plans

This index tracks active plan artifacts in this folder. Completed or superseded
historical plans live in `complete/`.

## Active

| Status | Plan | Owner | Next todo |
| --- | --- | --- | --- |
| active | [Portable headless runtime roadmap](../architecture/portable-headless-roadmap.md) | `tools/winui3-mac-test-runtime` | Phase 6 internal scenario driver checks are in place for JSON scenarios, AutomationCore-backed actions/assertions, state mutation, wait, and screenshot recording. Next: start Phase 7 Skia offscreen renderer hardening. |
| active | [Direct WinUI app project runtime ingestion](2026-06-15-direct-winui-app-project-runtime-ingestion-plan.md) | `tools/winui3-mac-test-runtime` | Track B direct render/interactions is verified. Next: Track C UIA/FlaUI-compatible artifact adapter over `tree.json`, `accessibility.json`, and `interactions.json`. |
| active | [EMSI Windows direct runtime UI gap closure](2026-06-15-emsi-windows-direct-runtime-ui-gap-closure-plan.md) | `tools/winui3-mac-test-runtime` + read-only `apps/windows` evidence | Phase 0/1 resource and localization gate is complete. Next: add fail-first Phase 2 direct Login `DataContext` bootstrap tests and implementation without backend calls or credentials. |

## Complete

Historical plan files have been moved to [complete/](complete/README.md). Keep
new plan artifacts in this directory while active, then move them to
`complete/` after completion or supersession.
