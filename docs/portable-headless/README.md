# Portable Headless Architecture Plan

This folder is the repo-native source for the updated portable headless
architecture. Future work should start here; the external zip is no longer
needed.

It is intentionally kept separate from release evidence and current
implementation docs so Phase 0 can record the architectural direction without
implying that every later phase is already implemented.

## Manifest

`MANIFEST.md` lists every planning file, explains how the source documents map
to repo-facing summaries, and records the phase status.

## Reading Order

1. `MANIFEST.md`
2. `RUNTIME_RULES.md`
3. `ARCHITECTURE_DECISIONS.md`
4. `CI_STRATEGY.md`
5. `AUTOMATION_ADAPTERS.md`
6. `SCENARIO_DRIVER_SPEC.md`
7. `RENDERING_TEXT_LAYOUT_NOTES.md`
8. `LOCAL_DEVELOPMENT_MODES.md`
9. `PRODUCT_POSITIONING.md`
10. `CONTROL_SUPPORT_MATRIX_SEED.md`
11. `CODEX_PHASE_PLAN.md`
12. `CODEX_WORK_RULES.md`

`ALL_IN_ONE_RULES_AND_PLAN.md` preserves the package as a single-file reference.

## Phase 0 Boundary

Phase 0 records the updated architecture and CI rules. It does not rename public
commands, replace the current runner implementation, add platform adapters, or
change the existing release gates.
