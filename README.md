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

The runner writes artifacts to `artifacts/winui3-mac/` by default:

- `run.json`: runtime, Wine dependency, project, assembly, and status metadata.
- `tree.json`: logical UI tree exported from the facade-backed app.

Wine is optional diagnostic context only. The primary runtime path is a managed
macOS .NET process.
