# Product Positioning

## Correct value proposition

```text
Portable WinUI 3 source-level compatibility runner
for headless automation, screenshot evidence, and Windows native reference comparison.
```

## Target users

### 1. Mac-first .NET developers

Problem:

```text
Android/iOS/macOS/Linux development is available from Mac.
WinUI/Windows path requires Windows machine/runner/VM.
```

Value:

```text
Local Mac source-level WinUI validation without switching to Windows.
```

### 2. Cross-platform .NET / MAUI teams

Problem:

```text
Windows target is the odd lane in otherwise Mac/Linux-friendly development.
```

Value:

```text
Portable headless checks on Linux/Windows CI plus Windows native truth lane.
```

### 3. WinUI control/library developers

Problem:

```text
Need to validate XAML/control behavior/screenshots across many states.
```

Value:

```text
Component-level automation and screenshot artifacts with documented support matrix.
```

### 4. CI-focused teams

Problem:

```text
Windows UI automation is slower/flakier; macOS runners are expensive.
```

Value:

```text
Cheap Linux portable-headless lane catches many errors before native Windows reference.
```

## What this is not

```text
Not a full Windows App SDK replacement.
Not a way to run arbitrary .exe/.msix.
Not guaranteed full WinUI 3 API compatibility.
Not guaranteed pixel-perfect Fluent rendering for all controls.
Not a replacement for Windows native reference validation.
```

## Product language

Use:

```text
source-level
documented subset
portable headless
internal automation
Skia offscreen
Windows native reference
evidence artifacts
compatibility matrix
```

Avoid:

```text
runs any WinUI app
full WinUI on macOS
drop-in Windows replacement
100% pixel perfect
no Windows needed ever
```

## Most valuable product shape

```text
Every PR:
    ubuntu-latest portable-headless
    windows-latest native-reference smoke

Local Mac:
    portable-headless + windowed debug

Nightly/release:
    full Windows native reference
    optional macOS AX/windowed
```

## Adoption risk

If default workflow requires `macos-latest`, adoption decreases.

If default workflow runs on `ubuntu-latest` and Windows reference runs on `windows-latest`, adoption improves.

## Demo that matters

```sh
winui3-compat-runner run \
  --mode portable-headless \
  --driver internal \
  --renderer skia-offscreen \
  --project samples/LoginApp/LoginApp.csproj \
  --scenario scenarios/login.json \
  --output artifacts/login
```

Outputs:

```text
scenario-result.json
automation-tree.json
layout-bounds.json
after-login.png
diagnostics.md
```

Then:

```sh
winui3-compat-runner run \
  --mode windows-reference \
  --driver flaui-uia3 \
  --renderer native-winui \
  --project samples/LoginApp/LoginApp.csproj \
  --scenario scenarios/login.json \
  --output artifacts/windows-login
```

Outputs native truth.

## Final positioning sentence

> Test and visually validate WinUI 3 source code without making Windows the only feedback loop, while keeping real Windows WinUI as the source of truth.
