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
winui3-compat-runner run \
  --mode macos-windowed \
  --driver internal \
  --renderer skia-metal \
  --project src/MyApp/MyApp.csproj
```

Bu mod:

```text
NSApplication çalıştırır
NSWindow açar
Skia/Metal yüzeyine çizer
mouse/keyboard/scroll eventlerini alır
hit-test ve input routing'i gerçek eventlerle dener
```

CI default değildir.

## 3. Local Mac — AX debug

Accessibility/automation bridge için:

```sh
winui3-compat-runner run \
  --mode macos-windowed-ax \
  --driver ax \
  --renderer skia-metal \
  --project src/MyApp/MyApp.csproj \
  --scenario tests/scenarios/login.json
```

Bu mod:

```text
AutomationCore -> NSAccessibility mapping
AXPress -> Invoke
AXValue -> ValuePattern
AXSelected -> SelectionPattern
```

Doğrular.

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

Phase 13 sonrası:

```sh
winui3-compat-runner run \
  --mode windows-custom-runtime \
  --driver flaui-uia3 \
  --renderer skia-offscreen \
  --project src/MyApp/MyApp.csproj \
  --scenario tests/scenarios/login.json
```

Bu native WinUI reference değildir. Bizim custom runtime'ın Windows UIA provider'ını test eder.

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
