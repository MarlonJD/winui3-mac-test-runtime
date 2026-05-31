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
```

## Current Fixtures

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/XamlTinyWinUIApp.MacTest/XamlTinyWinUIApp.MacTest.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj
```

`XamlTinyWinUIApp.MacTest` exercises the Phase 1 XAML compiler path:
`x:Class`, `x:Name`, text content, event hookup, and a simple static resource.

`SampleAdminShell.MacTest` is a headless shell compatibility fixture with a
desktop admin navigation shape. It validates the current `NavigationView`,
`NavigationViewItem`, `Frame`, `Grid`, `Border`, and `FontIcon` facade subset
and exports admin navigation visibility in `tree.json`.

The runner writes artifacts to `artifacts/winui3-mac/` by default:

- `run.json`: runtime, Wine dependency, project, assembly, and status metadata.
- `tree.json`: logical UI tree exported from the facade-backed app.

Wine is optional diagnostic context only. The primary runtime path is a managed
macOS .NET process.

## Compatibility Status

This is still a constrained test runtime. It supports a small source-level
WinUI-style subset for automated macOS testing and intentionally does not claim
binary compatibility, arbitrary `.exe` execution, or full WinUI 3 behavior.
