# Production Smoke Fixture

`ProductionSmoke.WinUI` is a public clean-room WinUI fixture for production-ring
smoke and E2E validation. It contains no private product data.

Run the local smoke scenario:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ProductionSmoke.WinUI/ProductionSmoke.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ProductionSmoke.WinUI/scenarios/production-smoke-light.json --strict-visual
```

Run the E2E-style workbench scenario:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ProductionSmoke.WinUI/ProductionSmoke.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ProductionSmoke.WinUI/scenarios/production-e2e-workbench-light.json --strict-visual
```

The scenarios cover launch, navigation, command invocation, form edit,
combo/list selection, status transitions, theme-resource rendering, and managed
popup decision actions. Each run emits `run.json`, `tree.json`,
`accessibility.json`, `interactions.json`, `visual-run.json`,
`component-evidence.json`, and `mac-runtime.png` under `artifacts/winui3-mac/`.
