using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace WindowsWindowCapture;

internal static class Program
{
    private const int SwRestore = 9;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const int DwmwaExtendedFrameBounds = 9;
    private const uint PwRenderFullContent = 2;

    public static int Main(string[] args)
    {
        try
        {
            var options = CaptureOptions.Parse(args);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.OutputPath))!);

            using var process = StartProcess(options);
            var window = WaitForWindow(
                options.Title,
                process,
                options.Timeout,
                options.ClientArea ? options.Viewport : null);
            if (window is null)
            {
                return Fail(
                    $"Could not find a visible window containing title '{options.Title}'. " +
                    $"Target process: {DescribeProcess(process)}. " +
                    $"Visible windows: {DescribeVisibleWindows()}");
            }

            if (options.RequireTitleMatch && !window.MatchedExpectedTitle)
            {
                return Fail(
                    $"Captured window title '{window.Title}' did not match expected title '{options.Title}'. " +
                    "Refusing to create a native reference from a fallback window.");
            }

            var captureWindow = window.Handle;
            ShowWindow(captureWindow, SwRestore);
            SetForegroundWindow(captureWindow);
            Thread.Sleep(750);
            if (options.ClientArea && options.Viewport is not null)
            {
                captureWindow = ResizeClientArea(
                    captureWindow,
                    options.Viewport,
                    () => WaitForWindow(
                        options.Title,
                        process,
                        TimeSpan.FromSeconds(2),
                        options.Viewport)?.Handle);
            }

            Thread.Sleep(options.SettleDelay);
            var captureRect = Capture(
                captureWindow,
                options.OutputPath,
                options.ClientArea,
                options.RejectBlackBorder);
            if (!string.IsNullOrWhiteSpace(options.MetadataOutputPath))
            {
                var workflowRunId = options.WorkflowRunId ?? Environment.GetEnvironmentVariable("GITHUB_RUN_ID");
                File.WriteAllText(options.MetadataOutputPath, JsonSerializer.Serialize(new
                {
                    schemaVersion = "0.2",
                    referenceSource = options.ReferenceSource,
                    fixtureProjectPath = options.FixtureProjectPath,
                    scenarioPath = options.ScenarioPath,
                    scenarioName = options.ScenarioName,
                    commitSha = options.CommitSha ?? Environment.GetEnvironmentVariable("GITHUB_SHA"),
                    workflowRunId,
                    captureRunId = workflowRunId,
                    runnerImage = options.RunnerImage ?? ReadRunnerImage(),
                    viewport = options.Viewport is null
                        ? null
                        : new
                        {
                            width = options.Viewport.Width,
                            height = options.Viewport.Height
                        },
                    scale = options.Scale,
                    theme = options.Theme,
                    windowTitle = options.Title,
                    actualWindowTitle = window.Title,
                    titleMatched = window.MatchedExpectedTitle,
                    settleDelayMs = (int)options.SettleDelay.TotalMilliseconds,
                    outputPath = Path.GetFullPath(options.OutputPath),
                    captureMode = options.ClientArea ? "client-area" : "window-frame",
                    blackBorderRejected = options.RejectBlackBorder,
                    dimensions = new
                    {
                        width = captureRect.Right - captureRect.Left,
                        height = captureRect.Bottom - captureRect.Top
                    },
                    capturedAt = DateTimeOffset.UtcNow
                }, new JsonSerializerOptions { WriteIndented = true }));
            }

            StopProcess(process);
            Console.WriteLine($"Captured window screenshot: {Path.GetFullPath(options.OutputPath)}");
            return 0;
        }
        catch (Exception error)
        {
            return Fail(error.Message);
        }
    }

    private static Process StartProcess(CaptureOptions options)
    {
        var fileName = options.Command[0];
        string? workingDirectory = null;
        if (File.Exists(fileName))
        {
            fileName = Path.GetFullPath(fileName);
            workingDirectory = Path.GetDirectoryName(fileName);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        foreach (var argument in options.Command.Skip(1))
        {
            startInfo.ArgumentList.Add(argument);
        }

        var process = Process.Start(startInfo) ??
            throw new InvalidOperationException("Could not start the target process.");
        process.OutputDataReceived += (_, eventArgs) => WriteChildLine(eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => WriteChildLine(eventArgs.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static WindowMatch? WaitForWindow(
        string title,
        Process process,
        TimeSpan timeout,
        CaptureViewport? expectedClientSize)
    {
        var stopAt = DateTimeOffset.UtcNow + timeout;
        var allowProcessFallbackAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(Math.Min(5, timeout.TotalSeconds));
        var launchedAt = ReadProcessStartTime(process);
        while (DateTimeOffset.UtcNow < stopAt)
        {
            var window = FindWindowByTitle(title, launchedAt, expectedClientSize);
            if (window is not null)
            {
                return window;
            }

            if (DateTimeOffset.UtcNow >= allowProcessFallbackAt)
            {
                var processWindow = TryGetProcessMainWindow(process);
                if (processWindow is not null)
                {
                    Console.WriteLine(
                        $"Using target process main window '{processWindow.Title}' while waiting for title '{title}'.");
                    return processWindow;
                }
            }

            Thread.Sleep(250);
        }

        return null;
    }

    private static WindowMatch? TryGetProcessMainWindow(Process process)
    {
        try
        {
            process.Refresh();
            if (process.HasExited ||
                process.MainWindowHandle == IntPtr.Zero ||
                !IsWindowVisible(process.MainWindowHandle))
            {
                return null;
            }

            return new WindowMatch(process.MainWindowHandle, ReadWindowText(process.MainWindowHandle), false);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static DateTimeOffset? ReadProcessStartTime(Process process)
    {
        try
        {
            process.Refresh();
            return process.HasExited ? null : new DateTimeOffset(process.StartTime);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static WindowMatch? FindWindowByTitle(
        string title,
        DateTimeOffset? launchedAt,
        CaptureViewport? expectedClientSize)
    {
        WindowMatch? result = null;
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window))
            {
                return true;
            }

            var text = ReadWindowText(window);
            if (!text.Contains(title, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!WindowStartedAfterLaunch(window, launchedAt) ||
                !CanReadWindowBounds(window, expectedClientSize))
            {
                return true;
            }

            result = new WindowMatch(window, text, true);
            return false;
        }, IntPtr.Zero);
        return result;
    }

    private static bool WindowStartedAfterLaunch(IntPtr window, DateTimeOffset? launchedAt)
    {
        if (launchedAt is null)
        {
            return true;
        }

        _ = GetWindowThreadProcessId(window, out var windowProcessId);
        try
        {
            using var windowProcess = Process.GetProcessById(windowProcessId);
            var windowStartedAt = new DateTimeOffset(windowProcess.StartTime);
            return windowStartedAt >= launchedAt.Value - TimeSpan.FromSeconds(2);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool CanReadWindowBounds(IntPtr window, CaptureViewport? expectedClientSize)
    {
        if (!GetClientRect(window, out var clientRect) ||
            clientRect.Width <= 0 ||
            clientRect.Height <= 0 ||
            !TryReadWindowRect(window, out var windowRect) ||
            windowRect.Width <= 0 ||
            windowRect.Height <= 0)
        {
            return false;
        }

        if (expectedClientSize is null)
        {
            return true;
        }

        var minimumWidth = Math.Max(320, expectedClientSize.Width / 2);
        var minimumHeight = Math.Max(240, expectedClientSize.Height / 2);
        return
            clientRect.Width > 0 &&
            clientRect.Height > 0 &&
            clientRect.Width >= minimumWidth &&
            clientRect.Height >= minimumHeight;
    }

    private static string ReadWindowText(IntPtr window)
    {
        var length = GetWindowTextLength(window);
        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        _ = GetWindowText(window, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string DescribeProcess(Process process)
    {
        try
        {
            process.Refresh();
            return process.HasExited
                ? $"exited={process.ExitCode}"
                : $"running pid={process.Id}, mainWindowHandle=0x{process.MainWindowHandle.ToInt64():X}, mainWindowTitle='{process.MainWindowTitle}'";
        }
        catch (InvalidOperationException)
        {
            return "not available";
        }
    }

    private static string DescribeVisibleWindows()
    {
        var titles = new List<string>();
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window))
            {
                return true;
            }

            var text = ReadWindowText(window);
            if (!string.IsNullOrWhiteSpace(text))
            {
                titles.Add(text);
            }

            return titles.Count < 12;
        }, IntPtr.Zero);

        return titles.Count == 0 ? "none with titles" : string.Join(" | ", titles);
    }

    private static Rect Capture(IntPtr window, string outputPath, bool clientArea, bool rejectBlackBorder)
    {
        var rect = clientArea ? GetClientCaptureRect(window) : GetCaptureRect(window);
        var width = Math.Max(1, rect.Right - rect.Left);
        var height = Math.Max(1, rect.Bottom - rect.Top);

        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        var hdc = graphics.GetHdc();
        try
        {
            if (clientArea || !PrintWindow(window, hdc, PwRenderFullContent))
            {
                graphics.ReleaseHdc(hdc);
                hdc = IntPtr.Zero;
                graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height));
            }
        }
        finally
        {
            if (hdc != IntPtr.Zero)
            {
                graphics.ReleaseHdc(hdc);
            }
        }

        if (rejectBlackBorder && TryFindTrailingBlackBorder(bitmap, out var border))
        {
            throw new InvalidOperationException(
                "Captured image has a trailing black border, which usually means the client area was clipped or copied off-screen. " +
                $"Right={border.Right}px; Bottom={border.Bottom}px.");
        }

        bitmap.Save(outputPath, ImageFormat.Png);
        return rect;
    }

    private static bool TryFindTrailingBlackBorder(Bitmap bitmap, out BlackBorder border)
    {
        var right = CountTrailingBlackColumns(bitmap);
        var bottom = CountTrailingBlackRows(bitmap);
        border = new BlackBorder(right, bottom);
        var minimumRight = Math.Max(8, bitmap.Width / 50);
        var minimumBottom = Math.Max(8, bitmap.Height / 50);
        return right >= minimumRight || bottom >= minimumBottom;
    }

    private static int CountTrailingBlackColumns(Bitmap bitmap)
    {
        var count = 0;
        for (var x = bitmap.Width - 1; x >= 0; x--)
        {
            if (!IsMostlyBlackColumn(bitmap, x))
            {
                break;
            }

            count++;
        }

        return count;
    }

    private static int CountTrailingBlackRows(Bitmap bitmap)
    {
        var count = 0;
        for (var y = bitmap.Height - 1; y >= 0; y--)
        {
            if (!IsMostlyBlackRow(bitmap, y))
            {
                break;
            }

            count++;
        }

        return count;
    }

    private static bool IsMostlyBlackColumn(Bitmap bitmap, int x)
    {
        var black = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            if (IsNearBlack(bitmap.GetPixel(x, y)))
            {
                black++;
            }
        }

        return black >= bitmap.Height * 98 / 100;
    }

    private static bool IsMostlyBlackRow(Bitmap bitmap, int y)
    {
        var black = 0;
        for (var x = 0; x < bitmap.Width; x++)
        {
            if (IsNearBlack(bitmap.GetPixel(x, y)))
            {
                black++;
            }
        }

        return black >= bitmap.Width * 98 / 100;
    }

    private static bool IsNearBlack(Color color)
    {
        return color.R <= 8 && color.G <= 8 && color.B <= 8;
    }

    private static Rect GetCaptureRect(IntPtr window)
    {
        if (TryReadWindowRect(window, out var extendedRect))
        {
            return extendedRect;
        }

        throw new InvalidOperationException("Could not read the target window bounds.");
    }

    private static Rect GetClientCaptureRect(IntPtr window)
    {
        if (!GetClientRect(window, out var clientRect))
        {
            throw new InvalidOperationException("Could not read the target client bounds.");
        }

        var point = new Point(0, 0);
        if (!ClientToScreen(window, ref point))
        {
            throw new InvalidOperationException("Could not map the target client bounds to screen coordinates.");
        }

        return new Rect(
            point.X,
            point.Y,
            point.X + Math.Max(1, clientRect.Right - clientRect.Left),
            point.Y + Math.Max(1, clientRect.Bottom - clientRect.Top));
    }

    private static IntPtr ResizeClientArea(
        IntPtr window,
        CaptureViewport viewport,
        Func<IntPtr?> resolveWindow)
    {
        var readBounds = false;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (!GetClientRect(window, out var clientRect) ||
                !GetWindowRect(window, out var windowRect))
            {
                var resolvedWindow = resolveWindow();
                if (resolvedWindow is { } handle && handle != IntPtr.Zero)
                {
                    window = handle;
                }

                Thread.Sleep(150);
                continue;
            }

            readBounds = true;
            if (clientRect.Width == viewport.Width && clientRect.Height == viewport.Height)
            {
                return window;
            }

            var nonClientWidth = Math.Max(0, windowRect.Width - clientRect.Width);
            var nonClientHeight = Math.Max(0, windowRect.Height - clientRect.Height);
            if (!SetWindowPos(
                    window,
                    IntPtr.Zero,
                    windowRect.Left,
                    windowRect.Top,
                    Math.Max(1, viewport.Width + nonClientWidth),
                    Math.Max(1, viewport.Height + nonClientHeight),
                    SwpNoZOrder | SwpNoActivate))
            {
                throw new InvalidOperationException("Could not resize the target window client area.");
            }

            Thread.Sleep(150);
        }

        if (!readBounds)
        {
            throw new InvalidOperationException("Could not read the target window bounds before resizing.");
        }

        if (!GetClientRect(window, out var finalClientRect))
        {
            throw new InvalidOperationException("Could not read the target bounds while resizing.");
        }

        throw new InvalidOperationException(
            "Target client area did not match requested viewport after resizing. " +
            $"Expected {viewport.Width}x{viewport.Height}; actual {finalClientRect.Width}x{finalClientRect.Height}.");
    }

    private static bool TryReadWindowRect(IntPtr window, out Rect rect)
    {
        if (DwmGetWindowAttribute(
                window,
                DwmwaExtendedFrameBounds,
                out rect,
                Marshal.SizeOf<Rect>()) == 0 &&
            rect.Right > rect.Left &&
            rect.Bottom > rect.Top)
        {
            return true;
        }

        return GetWindowRect(window, out rect);
    }

    private static void StopProcess(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        try
        {
            if (process.CloseMainWindow() && process.WaitForExit(3000))
            {
                return;
            }

            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static void WriteChildLine(string? line)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            Console.WriteLine("[target] " + line);
        }
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    private static string? ReadRunnerImage()
    {
        var imageOs = Environment.GetEnvironmentVariable("ImageOS");
        var imageVersion = Environment.GetEnvironmentVariable("ImageVersion");
        if (!string.IsNullOrWhiteSpace(imageOs) || !string.IsNullOrWhiteSpace(imageVersion))
        {
            return string.Join(
                " ",
                new[] { imageOs, imageVersion }.Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        return Environment.GetEnvironmentVariable("RUNNER_OS") ??
            Environment.GetEnvironmentVariable("OS") ??
            Environment.OSVersion.VersionString;
    }

    private sealed record WindowMatch(IntPtr Handle, string Title, bool MatchedExpectedTitle);

    private sealed record CaptureOptions(
        string Title,
        string OutputPath,
        string? MetadataOutputPath,
        TimeSpan Timeout,
        bool ClientArea,
        bool RequireTitleMatch,
        string ReferenceSource,
        string? FixtureProjectPath,
        string? ScenarioPath,
        string? ScenarioName,
        string? CommitSha,
        string? WorkflowRunId,
        string? RunnerImage,
        CaptureViewport? Viewport,
        double? Scale,
        string? Theme,
        TimeSpan SettleDelay,
        bool RejectBlackBorder,
        IReadOnlyList<string> Command)
    {
        public static CaptureOptions Parse(string[] args)
        {
            var separator = Array.IndexOf(args, "--");
            if (separator < 0 || separator == args.Length - 1)
            {
                throw new ArgumentException("Usage: WindowsWindowCapture --title <title> --output <png> [--metadata-output <json>] [--client-area] [--require-title-match] [--timeout-seconds 30] [--settle-ms 750] -- <command> [args...]");
            }

            string? title = null;
            string? outputPath = null;
            string? metadataOutputPath = null;
            var timeout = TimeSpan.FromSeconds(30);
            var clientArea = false;
            var requireTitleMatch = false;
            var referenceSource = "synthetic-probe";
            string? fixtureProjectPath = null;
            string? scenarioPath = null;
            string? scenarioName = null;
            string? commitSha = null;
            string? workflowRunId = null;
            string? runnerImage = null;
            CaptureViewport? viewport = null;
            double? scale = null;
            string? theme = null;
            var settleDelay = TimeSpan.FromMilliseconds(750);
            var rejectBlackBorder = false;

            for (var index = 0; index < separator; index++)
            {
                switch (args[index])
                {
                    case "--title":
                        title = ReadValue(args, ++index, "--title");
                        break;
                    case "--output":
                        outputPath = ReadValue(args, ++index, "--output");
                        break;
                    case "--metadata-output":
                        metadataOutputPath = ReadValue(args, ++index, "--metadata-output");
                        break;
                    case "--timeout-seconds":
                        timeout = TimeSpan.FromSeconds(double.Parse(ReadValue(args, ++index, "--timeout-seconds"), CultureInfo.InvariantCulture));
                        break;
                    case "--client-area":
                        clientArea = true;
                        break;
                    case "--reject-black-border":
                        rejectBlackBorder = true;
                        break;
                    case "--require-title-match":
                        requireTitleMatch = true;
                        break;
                    case "--reference-source":
                        referenceSource = ReadReferenceSource(args, ++index);
                        break;
                    case "--fixture-project":
                        fixtureProjectPath = ReadValue(args, ++index, "--fixture-project");
                        break;
                    case "--scenario":
                        scenarioPath = ReadValue(args, ++index, "--scenario");
                        break;
                    case "--scenario-name":
                        scenarioName = ReadValue(args, ++index, "--scenario-name");
                        break;
                    case "--commit-sha":
                        commitSha = ReadValue(args, ++index, "--commit-sha");
                        break;
                    case "--workflow-run-id":
                        workflowRunId = ReadValue(args, ++index, "--workflow-run-id");
                        break;
                    case "--runner-image":
                        runnerImage = ReadValue(args, ++index, "--runner-image");
                        break;
                    case "--viewport":
                        viewport = CaptureViewport.Parse(ReadValue(args, ++index, "--viewport"));
                        break;
                    case "--scale":
                        scale = ReadPositiveDouble(ReadValue(args, ++index, "--scale"), "--scale");
                        break;
                    case "--theme":
                        theme = ReadValue(args, ++index, "--theme");
                        break;
                    case "--settle-ms":
                        settleDelay = TimeSpan.FromMilliseconds(ReadNonNegativeInt(ReadValue(args, ++index, "--settle-ms"), "--settle-ms"));
                        break;
                    default:
                        throw new ArgumentException($"Unknown option '{args[index]}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Missing required option: --title <title>");
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Missing required option: --output <png>");
            }

            return new CaptureOptions(
                title,
                outputPath,
                metadataOutputPath,
                timeout,
                clientArea,
                requireTitleMatch,
                referenceSource,
                fixtureProjectPath,
                scenarioPath,
                scenarioName,
                commitSha,
                workflowRunId,
                runnerImage,
                viewport,
                scale,
                theme,
                settleDelay,
                rejectBlackBorder,
                args[(separator + 1)..]);
        }

        private static string ReadReferenceSource(string[] args, int index)
        {
            var value = ReadValue(args, index, "--reference-source");
            if (value is not ("native-winui" or "synthetic-probe"))
            {
                throw new ArgumentException("--reference-source must be native-winui or synthetic-probe.");
            }

            return value;
        }

        private static double ReadPositiveDouble(string value, string option)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ||
                number <= 0)
            {
                throw new ArgumentException($"{option} must be a positive number.");
            }

            return number;
        }

        private static int ReadNonNegativeInt(string value, string option)
        {
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ||
                number < 0)
            {
                throw new ArgumentException($"{option} must be a non-negative integer.");
            }

            return number;
        }

        private static string ReadValue(string[] args, int index, string option)
        {
            if (index >= args.Length || args[index] == "--")
            {
                throw new ArgumentException($"Missing value for {option}.");
            }

            return args[index];
        }
    }

    private sealed record CaptureViewport(int Width, int Height)
    {
        public static CaptureViewport Parse(string value)
        {
            var parts = value.Split('x', 'X');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var width) ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var height) ||
                width <= 0 ||
                height <= 0)
            {
                throw new ArgumentException("--viewport must use <width>x<height>, for example 1044x720.");
            }

            return new CaptureViewport(width, height);
        }
    }

    private sealed record BlackBorder(int Right, int Bottom);

    private delegate bool EnumWindowsProc(IntPtr window, IntPtr state);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr state);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out int processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr window, StringBuilder text, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr window, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr window, ref Point point);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr window,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out Rect rect, int size);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Rect
    {
        public Rect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;

        public int Width => Right - Left;

        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X;
        public int Y;
    }
}
