# WinUI 3 Compatibility Runtime — Updated Rules and Codex Plan

Bu paket, son tartışmadaki güncellenmiş kararı merkeze alır:

> Headless runtime macOS'a özel olmamalı. Ana hedef **portable headless WinUI compatibility runtime** olmalı.
> Varsayılan CI hedefi `ubuntu-latest` veya `windows-latest` olmalı.
> macOS desteği local developer experience ve opsiyonel windowed/AX doğrulama için gerekli kalmalı, fakat default PR CI lane olmamalı.

## Dosyalar

- `RUNTIME_RULES.md`
  Runtime mimarisi ve zorunlu kurallar.

- `ARCHITECTURE_DECISIONS.md`
  ADR formatında ana kararlar.

- `CODEX_WORK_RULES.md`
  Codex'in patch üretirken uyması gereken çalışma kuralları.

- `CODEX_PHASE_PLAN.md`
  Portable headless, Windows native reference, macOS windowed/AX ve optional provider fazları.

- `CI_STRATEGY.md`
  Linux/Windows default CI, macOS optional/manual/nightly stratejisi.

- `LOCAL_DEVELOPMENT_MODES.md`
  Mac, Linux ve Windows local geliştirme modları.

- `SCENARIO_DRIVER_SPEC.md`
  Ortak scenario/action/assertion formatı.

- `AUTOMATION_ADAPTERS.md`
  Internal driver, FlaUI/UIA3, macOS AX, optional Windows UIA provider ayrımı.

- `RENDERING_TEXT_LAYOUT_NOTES.md`
  Skia, Metal, offscreen rendering ve text layout notları.

- `CONTROL_SUPPORT_MATRIX_SEED.md`
  MVP control/pattern/support matrisi.

- `PRODUCT_POSITIONING.md`
  Projenin değer önerisi, hedef kullanıcılar ve yapılmaması gereken vaatler.

- `ALL_IN_ONE_RULES_AND_PLAN.md`
  Yukarıdaki bütün içeriklerin tek dosyada birleşmiş hali.

## En kritik güncelleme

Eski düşünce:

```text
macOS headless runtime
    -> macos-latest CI
    -> skia-offscreen
```

Yeni karar:

```text
portable headless runtime
    -> ubuntu-latest / windows-latest / local macOS
    -> internal automation
    -> skia-offscreen
```

macOS:

```text
local developer experience: gerekli
windowed debug: gerekli
AX/NSAccessibility validation: opsiyonel/ileri faz
default PR CI: hayır
```
