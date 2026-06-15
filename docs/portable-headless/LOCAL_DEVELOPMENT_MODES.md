# Local Development Modes

## 1. Local Mac — portable headless

Mac kullanan geliştiricinin en hızlı döngüsü:

```sh
winui3-compat-runner run \
  --mode portable-headless \
  --driver internal \
  --renderer skia-offscreen \
  --project src/MyApp/MyApp.csproj \
  --scenario tests/scenarios/login.json \
  --output artifacts/local/login
```

Bu mod:

```text
NSWindow açmaz
AX kullanmaz
Metal gerektirmez
Windows gerektirmez
Linux/Windows CI ile aynı davranışı hedefler
```

## 2. Local Mac — windowed debug

Görsel/manual debug için:

```sh
winui3-mac-runner macos-windowed-host \
  --artifacts artifacts/portable-headless/strict-scenario-sweep/login-light \
  --output artifacts/macos-windowed/login-light \
  --scenario login-light \
  --launch
```

Bu mod:

```text
NSApplication çalıştırır
NSWindow açar
Skia runtime PNG artifact'ını pencerede gösterir
mouse/keyboard/scroll eventlerini alır
tree.json üzerinden coordinate conversion ve hit-test event log'u üretir
click/key eventlerini local live state'e uygular
focus, press, checkbox/toggle, selection ve text input overlay'lerini redraw eder
macos-windowed-live-state.json üretir
AX/NSAccessibility adapter değildir
default PR CI değildir
```

CI default değildir.

## 3. Local Mac — AX debug

Accessibility/automation bridge için:

```sh
winui3-mac-runner macos-ax-adapter \
  --automation artifacts/portable-headless/strict-scenario-sweep/login-light/automation-core.json \
  --output artifacts/macos-windowed-ax/login-light \
  --scenario login-light
```

Bu mod:

```text
AutomationCore -> NSAccessibility mapping
AXPress -> Invoke
AXValue -> ValuePattern
AXSelected -> SelectionPattern
macos-ax-tree.json ve MacOsAxAdapter.swift üretir
portable-headless değildir
default PR CI değildir
```

Doğrular. `automation-core.json` yolu, scenario driver veya portable-headless
artifact exportu tarafından üretilen AutomationCore dosyasını göstermelidir.

## 4. Local Linux — portable headless

Linux geliştiricisi veya CI:

```sh
winui3-compat-runner run \
  --mode portable-headless \
  --driver internal \
  --renderer skia-offscreen \
  --project src/MyApp/MyApp.csproj \
  --scenario tests/scenarios/login.json
```

Bu gerçek Linux native desktop UI test değildir. Portable WinUI compatibility runtime testidir.

## 5. Local Windows — native reference

Gerçek WinUI reference için:

```sh
winui3-compat-runner run \
  --mode windows-reference \
  --driver flaui-uia3 \
  --renderer native-winui \
  --project src/MyApp/MyApp.csproj \
  --scenario tests/scenarios/login.json
```

Bu gerçek Windows WinUI app çalıştırır.

## 6. Local Windows — optional custom runtime

Custom runtime UIA provider scaffold için:

```sh
winui3-mac-runner windows-custom-runtime-uia \
  --automation artifacts/portable-headless/strict-scenario-sweep/login-light/automation-core.json \
  --output artifacts/windows-custom-runtime/login-light \
  --scenario login-light
```

Bu native WinUI reference değildir. Bizim custom runtime'ın Windows UIA
provider'ını FlaUI.UIA3 ile test etmek için `windows-custom-runtime` lane
artifactleri üretir.

## Local developer promise

Mac developer için doğru vaat:

```text
Windows'a geçmeden hızlı source-level validation ve screenshot.
Gerekirse macOS windowed debug.
Gerçek Windows truth için windows-reference lane.
```

Linux developer için doğru vaat:

```text
Cheap portable headless UI compatibility tests and screenshots.
```

Windows developer için doğru vaat:

```text
Native WinUI reference and optional custom-runtime provider validation.
```
