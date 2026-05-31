# WinUI3 Mac Test Runtime

Wine-free feasibility harness for running a constrained subset of WinUI-style
C# application code on macOS for automated tests.

This repository does not run arbitrary Windows binaries and does not claim full
WinUI 3 compatibility. Phase 0 runs managed .NET assemblies against clean-room
`Microsoft.UI.Xaml` facade types, hosts the app in a macOS .NET process, and
emits structured artifacts for test inspection.

## Phase 0 Commands

```sh
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-doctor
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj --renderer skia
```

## Current Fixtures

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/XamlTinyWinUIApp.MacTest/XamlTinyWinUIApp.MacTest.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/InteractionBindingApp.MacTest/InteractionBindingApp.MacTest.csproj --script ./fixtures/InteractionBindingApp.MacTest/interactions.json
```

`XamlTinyWinUIApp.MacTest` exercises the Phase 1 XAML compiler path:
`x:Class`, `x:Name`, text content, event hookup, and a simple static resource.

`SampleAdminShell.MacTest` is a headless shell compatibility fixture with a
desktop admin navigation shape. It validates the current `NavigationView`,
`NavigationViewItem`, `Frame`, `Grid`, `Border`, and `FontIcon` facade subset
and exports admin navigation visibility in `tree.json`.

`InteractionBindingApp.MacTest` exercises page navigation, binding refresh,
button click simulation, focus, accessibility export, binding failure export,
and a deterministic snapshot artifact.

The runner writes artifacts to `artifacts/winui3-mac/` by default:

- `run.json`: runtime, Wine dependency, project, assembly, and status metadata.
- `tree.json`: logical UI tree exported from the facade-backed app.
- `accessibility.json`: role/name/label tree derived from the logical UI tree.
- `binding-failures.json`: binding failures observed while refreshing the tree.
- `resource-failures.json`: static or theme resource lookup misses.
- `unsupported-apis.json`: placeholder facade APIs touched by the app.
- `diagnostics.sarif`: warning diagnostics for bindings, resources, and
  unsupported APIs.
- `interactions.json`: optional scripted interaction results.
- `snapshot.json` and `screenshots/snapshot.svg`: deterministic nonblank
  snapshot output for smoke and regression tests. Passing `--renderer skia`
  writes `screenshots/snapshot.png` with the Skia renderer.

Wine is optional diagnostic context only. The primary runtime path is a managed
macOS .NET process.

## Compatibility Status

This is still a constrained test runtime. It supports a small source-level
WinUI-style subset for automated macOS testing and intentionally does not claim
binary compatibility, arbitrary `.exe` execution, or full WinUI 3 behavior.

See `docs/compatibility/matrix.md` for the current supported subset.

## Package Smoke

```sh
dotnet pack src/WinUI3.MacTest.Sdk/WinUI3.MacTest.Sdk.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacCompat/WinUI3.MacCompat.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacRuntime/WinUI3.MacRuntime.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacXaml/WinUI3.MacXaml.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacRenderer.Skia/WinUI3.MacRenderer.Skia.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacRunner/WinUI3.MacRunner.csproj --configuration Release --output ./artifacts/packages
```

`MarlonJD.WinUI3.MacRunner` is packaged as a .NET tool with the
`winui3-mac-runner` command. `winui3-mac-doctor` remains available as a source
checkout wrapper and as `winui3-mac-runner doctor`.
