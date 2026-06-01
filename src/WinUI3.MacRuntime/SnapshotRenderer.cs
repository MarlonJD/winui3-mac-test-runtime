using System.Security;
using System.Text;

namespace WinUI3.MacRuntime;

public sealed record SnapshotResult(
    string SchemaVersion,
    string Renderer,
    string FilePath,
    int Width,
    int Height,
    bool IsNonBlank);

public interface ISnapshotRenderer
{
    Task<SnapshotResult> RenderAsync(
        UiTreeDocument tree,
        string screenshotsDirectory,
        SnapshotRenderOptions? options = null,
        CancellationToken cancellationToken = default);
}

public sealed class SnapshotRenderer : ISnapshotRenderer
{
    public async Task<SnapshotResult> RenderAsync(
        UiTreeDocument tree,
        string screenshotsDirectory,
        SnapshotRenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(screenshotsDirectory);
        var width = 960;
        var rowHeight = 30;
        var nodes = Flatten(tree.Root).ToArray();
        var height = Math.Max(180, 40 + nodes.Length * rowHeight);
        var path = Path.Combine(screenshotsDirectory, "snapshot.svg");
        var svg = BuildSvg(nodes, width, height, rowHeight);
        await File.WriteAllTextAsync(path, svg, cancellationToken);

        return new SnapshotResult(ArtifactSchemas.Snapshot, "managed-svg-snapshot", path, width, height, IsNonBlank: nodes.Length > 0);
    }

    private static string BuildSvg(IReadOnlyList<(UiNode Node, int Depth)> nodes, int width, int height, int rowHeight)
    {
        var source = new StringBuilder();
        source.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">""");
        source.AppendLine("""  <rect width="100%" height="100%" fill="#101820" />""");
        source.AppendLine("""  <text x="24" y="28" fill="#f6f7f9" font-family="Menlo, monospace" font-size="16">WinUI3 Mac Test Snapshot</text>""");

        for (var index = 0; index < nodes.Count; index++)
        {
            var (node, depth) = nodes[index];
            var y = 56 + index * rowHeight;
            var x = 24 + depth * 28;
            var label = node.Name is null ? node.Type : $"{node.Type} #{node.Name}";
            source.AppendLine($"""  <rect x="{x}" y="{y - 18}" width="{Math.Max(120, width - x - 24)}" height="24" rx="4" fill="{FillForDepth(depth)}" />""");
            source.AppendLine($"""  <text x="{x + 8}" y="{y}" fill="#f6f7f9" font-family="Menlo, monospace" font-size="13">{SecurityElement.Escape(label)}</text>""");
        }

        source.AppendLine("</svg>");
        return source.ToString();
    }

    private static IEnumerable<(UiNode Node, int Depth)> Flatten(UiNode root, int depth = 0)
    {
        yield return (root, depth);
        foreach (var child in root.Children)
        {
            foreach (var nested in Flatten(child, depth + 1))
            {
                yield return nested;
            }
        }
    }

    private static string FillForDepth(int depth)
    {
        return (depth % 3) switch
        {
            0 => "#24435c",
            1 => "#2f5f53",
            _ => "#5b4b73"
        };
    }
}
