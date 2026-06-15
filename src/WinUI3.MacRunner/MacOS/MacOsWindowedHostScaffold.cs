using System.Text.Json;

namespace WinUI3.MacRunner.MacOS;

public sealed record MacOsWindowedHostOptions(
    string ArtifactDirectory,
    string OutputDirectory,
    string ScenarioName,
    string WindowTitle);

public sealed record MacOsWindowedHostScaffold(
    string SchemaVersion,
    string Mode,
    string Driver,
    string Renderer,
    string CiPolicy,
    string DefaultPrCi,
    string ScenarioName,
    string ArtifactDirectory,
    string RuntimeImagePath,
    string TreePath,
    string EventLogPath,
    string LiveStatePath,
    bool LiveInteraction,
    string HostSourcePath,
    string LaunchScriptPath,
    string MetadataPath)
{
    public static MacOsWindowedHostScaffold Write(MacOsWindowedHostOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ArtifactDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputDirectory);

        var artifactDirectory = Path.GetFullPath(options.ArtifactDirectory);
        var outputDirectory = Path.GetFullPath(options.OutputDirectory);
        var runtimeImagePath = ResolveRuntimeImagePath(artifactDirectory);
        var treePath = Path.Combine(artifactDirectory, "tree.json");
        if (!File.Exists(treePath))
        {
            throw new FileNotFoundException("macOS windowed host requires tree.json for coordinate hit testing.", treePath);
        }

        Directory.CreateDirectory(outputDirectory);
        var hostSourcePath = Path.Combine(outputDirectory, "MacOsWindowedHost.swift");
        var launchScriptPath = Path.Combine(outputDirectory, "launch-macos-windowed.sh");
        var metadataPath = Path.Combine(outputDirectory, "macos-windowed-host.json");
        var eventLogPath = Path.Combine(outputDirectory, "macos-windowed-events.jsonl");
        var liveStatePath = Path.Combine(outputDirectory, "macos-windowed-live-state.json");
        var scenarioName = string.IsNullOrWhiteSpace(options.ScenarioName)
            ? Path.GetFileName(artifactDirectory)
            : options.ScenarioName;
        var windowTitle = string.IsNullOrWhiteSpace(options.WindowTitle)
            ? $"WinUI3 macOS Windowed - {scenarioName}"
            : options.WindowTitle;

        File.WriteAllText(
            hostSourcePath,
            BuildSwiftHost(windowTitle, runtimeImagePath, treePath, eventLogPath, liveStatePath));
        File.WriteAllText(
            launchScriptPath,
            BuildLaunchScript());
        TryMarkExecutable(launchScriptPath);

        var scaffold = new MacOsWindowedHostScaffold(
            SchemaVersion: "0.1",
            Mode: "macos-windowed",
            Driver: "internal",
            Renderer: "skia-offscreen-windowed-preview",
            CiPolicy: "local-manual",
            DefaultPrCi: "not-default-pr-ci",
            ScenarioName: scenarioName,
            ArtifactDirectory: artifactDirectory,
            RuntimeImagePath: RelativeArtifactPath(artifactDirectory, runtimeImagePath),
            TreePath: RelativeArtifactPath(artifactDirectory, treePath),
            EventLogPath: Path.GetFileName(eventLogPath),
            LiveStatePath: Path.GetFileName(liveStatePath),
            LiveInteraction: true,
            HostSourcePath: hostSourcePath,
            LaunchScriptPath: launchScriptPath,
            MetadataPath: metadataPath);
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(scaffold, JsonOptions));
        return scaffold;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string ResolveRuntimeImagePath(string artifactDirectory)
    {
        var candidates = new[]
        {
            Path.Combine(artifactDirectory, "screenshots", "mac-runtime.png"),
            Path.Combine(artifactDirectory, "visual", "mac-runtime.png"),
            Path.Combine(artifactDirectory, "mac-runtime.png"),
            Path.Combine(artifactDirectory, "screenshots", "snapshot.png")
        };
        var existing = candidates.FirstOrDefault(File.Exists);
        return existing ?? throw new FileNotFoundException(
            "macOS windowed host requires a Skia PNG artifact such as screenshots/mac-runtime.png.",
            candidates[0]);
    }

    private static string RelativeArtifactPath(string artifactDirectory, string path)
    {
        return Path.GetRelativePath(artifactDirectory, path).Replace('\\', '/');
    }

    private static string BuildLaunchScript()
    {
        return """
            #!/usr/bin/env bash
            set -euo pipefail
            cd "$(dirname "$0")"
            : > macos-windowed-events.jsonl
            swift MacOsWindowedHost.swift "$@"
            """;
    }

    private static string BuildSwiftHost(
        string windowTitle,
        string runtimeImagePath,
        string treePath,
        string eventLogPath,
        string liveStatePath)
    {
        return $$"""
            import AppKit
            import Foundation

            let runtimeImagePath = "{{EscapeSwift(runtimeImagePath)}}"
            let treePath = "{{EscapeSwift(treePath)}}"
            let eventLogPath = "{{EscapeSwift(eventLogPath)}}"
            let liveStatePath = "{{EscapeSwift(liveStatePath)}}"
            let windowTitle = "{{EscapeSwift(windowTitle)}}"

            final class RuntimeNode {
                let runtimeId: String
                let name: String
                let automationId: String?
                let type: String
                let bounds: CGRect
                let children: [RuntimeNode]

                init(runtimeId: String, name: String, automationId: String?, type: String, bounds: CGRect, children: [RuntimeNode]) {
                    self.runtimeId = runtimeId
                    self.name = name
                    self.automationId = automationId
                    self.type = type
                    self.bounds = bounds
                    self.children = children
                }
            }

            func readDouble(_ dictionary: [String: Any], _ key: String) -> CGFloat {
                if let value = dictionary[key] as? Double {
                    return CGFloat(value)
                }
                if let value = dictionary[key] as? Int {
                    return CGFloat(value)
                }
                return 0
            }

            func readBool(_ value: Any?, fallback: Bool = false) -> Bool {
                if let value = value as? Bool {
                    return value
                }
                if let value = value as? String {
                    return value.caseInsensitiveCompare("true") == .orderedSame
                }
                return fallback
            }

            func simpleType(_ type: String) -> String {
                return type.split(separator: ".").last.map(String.init) ?? type
            }

            func readNode(_ value: Any, runtimeId: String = "root") -> RuntimeNode? {
                guard let dictionary = value as? [String: Any] else {
                    return nil
                }
                let properties = dictionary["properties"] as? [String: Any] ?? [:]
                let boundsDictionary = properties["bounds"] as? [String: Any] ?? dictionary["bounds"] as? [String: Any] ?? [:]
                let children = (dictionary["children"] as? [Any] ?? []).enumerated().compactMap { index, child in
                    readNode(child, runtimeId: "\(runtimeId)/\(index)")
                }
                return RuntimeNode(
                    runtimeId: runtimeId,
                    name: dictionary["name"] as? String ?? "node",
                    automationId: properties["automationId"] as? String ?? dictionary["automationId"] as? String,
                    type: dictionary["type"] as? String ?? "Group",
                    bounds: CGRect(
                        x: readDouble(boundsDictionary, "x"),
                        y: readDouble(boundsDictionary, "y"),
                        width: readDouble(boundsDictionary, "width"),
                        height: readDouble(boundsDictionary, "height")),
                    children: children)
            }

            func loadRuntimeTree() -> RuntimeNode? {
                guard let data = FileManager.default.contents(atPath: treePath),
                      let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
                      let root = json["root"] else {
                    return nil
                }
                return readNode(root)
            }

            func flattenNodes(_ node: RuntimeNode?) -> [RuntimeNode] {
                guard let node else { return [] }
                return [node] + node.children.flatMap(flattenNodes)
            }

            final class RuntimePreviewView: NSView {
                let image: NSImage
                let runtimeRoot: RuntimeNode?
                let eventLog = URL(fileURLWithPath: eventLogPath)
                let liveState = URL(fileURLWithPath: liveStatePath)
                var focusedRuntimeId: String?
                var pressedRuntimeId: String?
                var textValues: [String: String] = [:]
                var toggleState: [String: Bool] = [:]
                var selectedRuntimeIds: Set<String> = []

                init(frame: CGRect, image: NSImage, runtimeRoot: RuntimeNode?) {
                    self.image = image
                    self.runtimeRoot = runtimeRoot
                    super.init(frame: frame)
                    wantsLayer = true
                    layer?.backgroundColor = NSColor.windowBackgroundColor.cgColor
                    seedLiveState(from: runtimeRoot)
                    writeLiveState()
                }

                required init?(coder: NSCoder) {
                    fatalError("init(coder:) has not been implemented")
                }

                override var acceptsFirstResponder: Bool { true }

                override func draw(_ dirtyRect: NSRect) {
                    NSColor.windowBackgroundColor.setFill()
                    dirtyRect.fill()
                    image.draw(in: bounds)
                    drawLiveStateOverlay(runtimeRoot)
                }

                override func mouseDown(with event: NSEvent) {
                    window?.makeFirstResponder(self)
                    let target = logEvent("mouseDown", event)
                    applyAutomationCoreAction(target: target, event: event)
                }

                override func keyDown(with event: NSEvent) {
                    logEvent("keyDown", event)
                    guard let focusedRuntimeId else { return }
                    let characters = event.charactersIgnoringModifiers ?? ""
                    if characters == "\u{7f}" {
                        var value = textValues[focusedRuntimeId] ?? ""
                        if !value.isEmpty {
                            value.removeLast()
                        }
                        textValues[focusedRuntimeId] = value
                    } else if !characters.isEmpty {
                        textValues[focusedRuntimeId, default: ""] += characters
                    }
                    writeLiveState()
                    setNeedsDisplay(bounds)
                }

                override func scrollWheel(with event: NSEvent) {
                    logEvent("scrollWheel", event)
                }

                func seedLiveState(from node: RuntimeNode?) {
                    for node in flattenNodes(node) {
                        let kind = simpleType(node.type)
                        if kind == "TextBox" || kind == "PasswordBox" || kind == "AutoSuggestBox" {
                            textValues[node.runtimeId] = ""
                        }
                        if kind == "CheckBox" || kind == "ToggleButton" || kind == "ToggleSwitch" || kind == "ToggleSplitButton" {
                            toggleState[node.runtimeId] = false
                        }
                    }
                }

                func convertToRuntimePoint(_ event: NSEvent) -> CGPoint {
                    let local = convert(event.locationInWindow, from: nil)
                    let imageSize = image.size
                    let scaleX = imageSize.width == 0 ? 1 : imageSize.width / bounds.width
                    let scaleY = imageSize.height == 0 ? 1 : imageSize.height / bounds.height
                    return CGPoint(x: local.x * scaleX, y: (bounds.height - local.y) * scaleY)
                }

                func hitTestRuntimeNode(_ point: CGPoint, in node: RuntimeNode?) -> RuntimeNode? {
                    guard let node else { return nil }
                    for child in node.children.reversed() {
                        if let hit = hitTestRuntimeNode(point, in: child) {
                            return hit
                        }
                    }
                    return node.bounds.contains(point) ? node : nil
                }

                func applyAutomationCoreAction(target: RuntimeNode?, event: NSEvent) {
                    guard let target else {
                        focusedRuntimeId = nil
                        writeLiveState()
                        setNeedsDisplay(bounds)
                        return
                    }

                    let kind = simpleType(target.type)
                    pressedRuntimeId = target.runtimeId
                    if kind == "TextBox" || kind == "PasswordBox" || kind == "AutoSuggestBox" {
                        focusedRuntimeId = target.runtimeId
                    } else if kind == "CheckBox" || kind == "ToggleButton" || kind == "ToggleSwitch" || kind == "ToggleSplitButton" {
                        toggleState[target.runtimeId] = !(toggleState[target.runtimeId] ?? false)
                        focusedRuntimeId = target.runtimeId
                    } else if kind == "RadioButton" || kind == "NavigationViewItem" || kind == "ListViewItem" {
                        selectedRuntimeIds.insert(target.runtimeId)
                        focusedRuntimeId = target.runtimeId
                    } else if kind == "Button" || kind == "RepeatButton" || kind == "HyperlinkButton" || kind == "DropDownButton" || kind == "SplitButton" || kind == "AppBarButton" {
                        focusedRuntimeId = target.runtimeId
                    }

                    writeLiveState()
                    setNeedsDisplay(bounds)
                    DispatchQueue.main.asyncAfter(deadline: .now() + 0.16) { [weak self] in
                        guard self?.pressedRuntimeId == target.runtimeId else { return }
                        self?.pressedRuntimeId = nil
                        self?.setNeedsDisplay(self?.bounds ?? .zero)
                    }
                }

                func logEvent(_ type: String, _ event: NSEvent) -> RuntimeNode? {
                    let runtimePoint = convertToRuntimePoint(event)
                    let target = hitTestRuntimeNode(runtimePoint, in: runtimeRoot)
                    let payload: [String: Any] = [
                        "mode": "macos-windowed",
                        "driver": "internal",
                        "type": type,
                        "x": Double(runtimePoint.x),
                        "y": Double(runtimePoint.y),
                        "key": event.charactersIgnoringModifiers ?? "",
                        "deltaX": Double(event.scrollingDeltaX),
                        "deltaY": Double(event.scrollingDeltaY),
                        "target": target?.automationId ?? target?.name ?? ""
                    ]
                    guard let data = try? JSONSerialization.data(withJSONObject: payload),
                          let line = String(data: data, encoding: .utf8) else {
                        return target
                    }
                    if let handle = try? FileHandle(forWritingTo: eventLog) {
                        defer { try? handle.close() }
                        try? handle.seekToEnd()
                        try? handle.write(contentsOf: Data((line + "\n").utf8))
                    }
                    return target
                }

                func viewRect(for node: RuntimeNode) -> CGRect {
                    let imageSize = image.size
                    let scaleX = imageSize.width == 0 ? 1 : bounds.width / imageSize.width
                    let scaleY = imageSize.height == 0 ? 1 : bounds.height / imageSize.height
                    return CGRect(
                        x: node.bounds.minX * scaleX,
                        y: bounds.height - node.bounds.maxY * scaleY,
                        width: node.bounds.width * scaleX,
                        height: node.bounds.height * scaleY)
                }

                func drawLiveStateOverlay(_ node: RuntimeNode?) {
                    guard let node else { return }
                    for child in node.children {
                        drawLiveStateOverlay(child)
                    }

                    let rect = viewRect(for: node)
                    let kind = simpleType(node.type)
                    if pressedRuntimeId == node.runtimeId {
                        NSColor.systemBlue.withAlphaComponent(0.18).setFill()
                        rect.fill()
                    }
                    if focusedRuntimeId == node.runtimeId {
                        NSColor.keyboardFocusIndicatorColor.setStroke()
                        let path = NSBezierPath(roundedRect: rect.insetBy(dx: 1, dy: 1), xRadius: 4, yRadius: 4)
                        path.lineWidth = 3
                        path.stroke()
                    }
                    if kind == "CheckBox" || kind == "ToggleButton" || kind == "ToggleSwitch" || kind == "ToggleSplitButton" {
                        let checked = toggleState[node.runtimeId] ?? false
                        NSColor.systemBlue.withAlphaComponent(checked ? 0.35 : 0.08).setFill()
                        NSBezierPath(roundedRect: rect.insetBy(dx: 3, dy: 5), xRadius: 4, yRadius: 4).fill()
                        if checked {
                            let mark = NSBezierPath()
                            mark.move(to: CGPoint(x: rect.minX + 10, y: rect.midY))
                            mark.line(to: CGPoint(x: rect.minX + 18, y: rect.midY - 8))
                            mark.line(to: CGPoint(x: rect.minX + 34, y: rect.midY + 9))
                            NSColor.white.setStroke()
                            mark.lineWidth = 4
                            mark.stroke()
                        }
                    }
                    if kind == "RadioButton" || kind == "NavigationViewItem" || kind == "ListViewItem" {
                        if selectedRuntimeIds.contains(node.runtimeId) {
                            NSColor.systemBlue.withAlphaComponent(0.20).setFill()
                            rect.fill()
                        }
                    }
                    if kind == "TextBox" || kind == "PasswordBox" || kind == "AutoSuggestBox" {
                        let value = textValues[node.runtimeId] ?? ""
                        if !value.isEmpty {
                            NSColor.textBackgroundColor.withAlphaComponent(0.90).setFill()
                            rect.insetBy(dx: 4, dy: 4).fill()
                            let attributes: [NSAttributedString.Key: Any] = [
                                .font: NSFont.systemFont(ofSize: 14),
                                .foregroundColor: NSColor.labelColor
                            ]
                            value.draw(in: rect.insetBy(dx: 8, dy: 8), withAttributes: attributes)
                        }
                    }
                }

                func writeLiveState() {
                    var nodes: [[String: Any]] = []
                    for node in flattenNodes(runtimeRoot) {
                        nodes.append([
                            "runtimeId": node.runtimeId,
                            "automationId": node.automationId ?? "",
                            "name": node.name,
                            "type": simpleType(node.type),
                            "focused": focusedRuntimeId == node.runtimeId,
                            "pressed": pressedRuntimeId == node.runtimeId,
                            "textValue": textValues[node.runtimeId] ?? "",
                            "toggleState": toggleState[node.runtimeId] ?? false,
                            "selected": selectedRuntimeIds.contains(node.runtimeId)
                        ])
                    }
                    let payload: [String: Any] = [
                        "schemaVersion": "0.1",
                        "mode": "macos-windowed",
                        "driver": "internal",
                        "liveInteraction": true,
                        "focusedRuntimeId": focusedRuntimeId ?? "",
                        "nodes": nodes
                    ]
                    if let data = try? JSONSerialization.data(withJSONObject: payload, options: [.prettyPrinted]) {
                        try? data.write(to: liveState)
                    }
                }
            }

            let app = NSApplication.shared
            app.setActivationPolicy(.regular)
            let image = NSImage(contentsOfFile: runtimeImagePath) ?? NSImage(size: NSSize(width: 800, height: 600))
            let root = loadRuntimeTree()
            let size = image.size.width > 0 && image.size.height > 0 ? image.size : NSSize(width: 800, height: 600)
            let window = NSWindow(
                contentRect: NSRect(x: 0, y: 0, width: size.width, height: size.height),
                styleMask: [.titled, .closable, .miniaturizable, .resizable],
                backing: .buffered,
                defer: false)
            window.title = windowTitle
            let view = RuntimePreviewView(frame: window.contentView?.bounds ?? NSRect(x: 0, y: 0, width: size.width, height: size.height), image: image, runtimeRoot: root)
            view.autoresizingMask = [.width, .height]
            window.contentView = view
            window.center()
            window.makeKeyAndOrderFront(nil)
            app.activate(ignoringOtherApps: true)
            app.run()
            """;
    }

    private static string EscapeSwift(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static void TryMarkExecutable(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
        catch (PlatformNotSupportedException)
        {
        }
    }
}
