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
        var scenarioName = string.IsNullOrWhiteSpace(options.ScenarioName)
            ? Path.GetFileName(artifactDirectory)
            : options.ScenarioName;
        var windowTitle = string.IsNullOrWhiteSpace(options.WindowTitle)
            ? $"WinUI3 macOS Windowed - {scenarioName}"
            : options.WindowTitle;

        File.WriteAllText(
            hostSourcePath,
            BuildSwiftHost(windowTitle, runtimeImagePath, treePath, eventLogPath));
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
        string eventLogPath)
    {
        return $$"""
            import AppKit
            import Foundation

            let runtimeImagePath = "{{EscapeSwift(runtimeImagePath)}}"
            let treePath = "{{EscapeSwift(treePath)}}"
            let eventLogPath = "{{EscapeSwift(eventLogPath)}}"
            let windowTitle = "{{EscapeSwift(windowTitle)}}"

            struct RuntimeNode {
                let name: String
                let automationId: String?
                let bounds: CGRect
                let children: [RuntimeNode]
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

            func readNode(_ value: Any) -> RuntimeNode? {
                guard let dictionary = value as? [String: Any] else {
                    return nil
                }
                let properties = dictionary["properties"] as? [String: Any] ?? [:]
                let boundsDictionary = properties["bounds"] as? [String: Any] ?? dictionary["bounds"] as? [String: Any] ?? [:]
                let children = (dictionary["children"] as? [Any] ?? []).compactMap(readNode)
                return RuntimeNode(
                    name: dictionary["name"] as? String ?? "node",
                    automationId: properties["automationId"] as? String ?? dictionary["automationId"] as? String,
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

            final class RuntimePreviewView: NSView {
                let image: NSImage
                let runtimeRoot: RuntimeNode?
                let eventLog = URL(fileURLWithPath: eventLogPath)

                init(frame: CGRect, image: NSImage, runtimeRoot: RuntimeNode?) {
                    self.image = image
                    self.runtimeRoot = runtimeRoot
                    super.init(frame: frame)
                    wantsLayer = true
                    layer?.backgroundColor = NSColor.windowBackgroundColor.cgColor
                }

                required init?(coder: NSCoder) {
                    fatalError("init(coder:) has not been implemented")
                }

                override var acceptsFirstResponder: Bool { true }

                override func draw(_ dirtyRect: NSRect) {
                    NSColor.windowBackgroundColor.setFill()
                    dirtyRect.fill()
                    image.draw(in: bounds)
                }

                override func mouseDown(with event: NSEvent) {
                    logEvent("mouseDown", event)
                }

                override func keyDown(with event: NSEvent) {
                    logEvent("keyDown", event)
                }

                override func scrollWheel(with event: NSEvent) {
                    logEvent("scrollWheel", event)
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

                func logEvent(_ type: String, _ event: NSEvent) {
                    let runtimePoint = convertToRuntimePoint(event)
                    let target = hitTestRuntimeNode(runtimePoint, in: runtimeRoot)
                    let payload: [String: Any] = [
                        "mode": "macos-windowed",
                        "driver": "internal",
                        "type": type,
                        "x": runtimePoint.x,
                        "y": runtimePoint.y,
                        "key": event.charactersIgnoringModifiers ?? "",
                        "deltaX": event.scrollingDeltaX,
                        "deltaY": event.scrollingDeltaY,
                        "target": target?.automationId ?? target?.name ?? ""
                    ]
                    guard let data = try? JSONSerialization.data(withJSONObject: payload),
                          let line = String(data: data, encoding: .utf8) else {
                        return
                    }
                    if let handle = try? FileHandle(forWritingTo: eventLog) {
                        defer { try? handle.close() }
                        try? handle.seekToEnd()
                        try? handle.write(contentsOf: Data((line + "\n").utf8))
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
