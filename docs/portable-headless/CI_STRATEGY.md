# CI Strategy

## Ana karar

Default PR CI macOS runner kullanmamalıdır.

Neden:

```text
macOS hosted runners pahalıdır.
Windowed/AX tests flakier olabilir.
Portable headless macOS'a bağlı olmamalıdır.
```

## Default CI lanes

### 1. Portable headless lane

```yaml
jobs:
  portable-headless:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: marlonjd/winui3-compat-runner-action@v1
        with:
          mode: portable-headless
          driver: internal
          renderer: skia-offscreen
          project: src/MyApp/MyApp.csproj
          scenario: tests/scenarios/login.json
          output: artifacts/portable-headless
```

Amaç:

```text
fast source-level WinUI compatibility check
internal automation
layout/text/render regression
Skia offscreen screenshot
cheap CI
```

### 2. Windows native reference lane

```yaml
jobs:
  windows-native-reference:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: marlonjd/winui3-compat-runner-action@v1
        with:
          mode: windows-reference
          driver: flaui-uia3
          renderer: native-winui
          project: src/MyApp/MyApp.csproj
          scenario: tests/scenarios/login.json
          output: artifacts/windows-reference
```

Amaç:

```text
real native WinUI source of truth
Windows UIA/FlaUI behavior
native screenshot
reference artifacts
```

## Optional macOS lane

macOS hosted CI sadece şu durumlarda kullanılmalıdır:

```text
workflow_dispatch
nightly/scheduled
release validation
self-hosted Mac available
explicit platform integration changes
```

Örnek:

```yaml
jobs:
  macos-windowed-ax:
    runs-on: macos-latest
    if: github.event_name == 'workflow_dispatch' || github.event_name == 'schedule'
    steps:
      - uses: actions/checkout@v4
      - uses: marlonjd/winui3-compat-runner-action@v1
        with:
          mode: macos-windowed-ax
          driver: ax
          renderer: skia-metal
          project: src/MyApp/MyApp.csproj
          scenario: tests/scenarios/login.json
          output: artifacts/macos-windowed-ax
```

## Recommended matrix

```text
Every PR:
    ubuntu-latest portable-headless
    windows-latest windows-reference smoke

Main/nightly:
    ubuntu-latest full portable-headless matrix
    windows-latest full windows-reference matrix
    optional macos-latest windowed/AX smoke

Release:
    all of the above
    compatibility dashboard
    artifacts archived
```

## What not to do

Do not make this the default:

```yaml
runs-on: macos-latest
mode: portable-headless
```

Portable-headless should run on Linux/Windows CI. macOS CI is for macOS-specific host/AX validation, not for generic headless validation.

## Artifact naming

Every artifact must include lane:

```text
portable-headless/
windows-reference/
macos-windowed-ax/
windows-custom-runtime/
```

Do not mix native reference and custom runtime output.
