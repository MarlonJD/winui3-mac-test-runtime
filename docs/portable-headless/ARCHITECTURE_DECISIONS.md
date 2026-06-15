# Architecture Decisions

## ADR-001 — Headless runtime portable olacak

### Karar

Headless runtime macOS'a özel olmayacak. Ana hedef:

```text
portable-headless
```

Bu mod `ubuntu-latest`, `windows-latest`, local macOS, local Linux ve local Windows üzerinde çalışabilmelidir.

### Gerekçe

macOS hosted CI pahalıdır ve default lane olursa adoption düşer. Headless'in asıl değeri macOS runner kullanmak değil, macOS runner'a ihtiyaç duymadan source-level WinUI test/screenshot alabilmektir.

### Sonuç

Eski isimler değiştirilmelidir:

```text
macos-headless -> portable-headless
WinUI3.MacRunner headless assumptions -> platform-neutral assumptions
```

---

## ADR-002 — macOS support local developer experience için korunacak

### Karar

macOS local development desteklenecek:

```text
portable-headless local
macos-windowed local
macos-windowed-ax manual/local
```

### Gerekçe

Mac kullanan geliştirici Windows'a geçmeden WinUI source app'i doğrulamak, screenshot almak ve gerekirse pencereyle debug etmek ister.

### Sonuç

macOS windowed host yapılacaktır, fakat default PR CI lane yapılmayacaktır.

---

## ADR-003 — Linux portable UI test supported olacak

### Karar

Linux üzerinde portable-headless UI compatibility tests supported olacaktır.

### Gerekçe

Linux CI ucuz ve yaygındır. Headless mod OS-level UI automation kullanmadığı için Linux'ta çalışabilir.

### Sınır

Linux lane native Windows UIA/FlaUI veya macOS AX doğrulaması yapmaz.

---

## ADR-004 — AutomationCore platform bağımsız olacak

### Karar

Automation tree ve pattern modeli platform bağımsız `AutomationCore` içinde kalacaktır.

```text
AutomationCore
    AutomationNode
    AutomationPattern
    Invoke
    Value
    Toggle
    SelectionItem
    Scroll
    Focus
```

Platform adapter'ları sadece mapping yapacaktır.

### Adapter'lar

```text
Internal driver:
    portable-headless

FlaUI/UIA3 client:
    windows-reference

macOS AX provider:
    macos-windowed-ax

Windows UIA provider:
    optional windows-custom-runtime
```

---

## ADR-005 — Skia default offscreen renderer olacak

### Karar

Portable-headless screenshot için default renderer:

```text
skia-offscreen
```

Metal sadece macOS windowed/onscreen performance path için kullanılacaktır.

### Gerekçe

Skia CPU/offscreen Linux/Windows/macOS CI için uygundur. Metal headless default olmamalıdır.

---

## ADR-006 — Windows native remains source of truth

### Karar

Gerçek WinUI davranışı için Windows native reference lane korunacaktır:

```text
windows-latest
real native WinUI app
FlaUI.UIA3
native screenshot
native automation tree
```

Portable-headless output native Windows'un yerine geçmez; hızlı regression signal verir.

---

## ADR-007 — Same scenario, multiple drivers

### Karar

Scenario dosyaları driver bağımsız olmalıdır.

```text
same scenario
    -> internal driver
    -> FlaUI.UIA3 driver
    -> AX driver
```

### Gerekçe

Tek test tanımı ile portable runtime, Windows native reference ve macOS AX output karşılaştırılabilir.

---

## ADR-008 — Product positioning developer tool olacak

### Karar

Proje production app framework iddiası yerine developer tool / CI evidence platform olarak konumlanacaktır.

Doğru hedef:

```text
Mac/Linux/Windows CI'da WinUI source-level compatibility evidence.
Windows native reference ile karşılaştırmalı artifact.
Local Mac developer loop.
```
