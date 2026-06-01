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
    private const int DwmwaExtendedFrameBounds = 9;
    private const uint PwRenderFullContent = 2;

    public static int Main(string[] args)
    {
        try
        {
            var options = CaptureOptions.Parse(args);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.OutputPath))!);

            using var process = StartProcess(options);
            var window = WaitForWindow(options.Title, options.Timeout);
            if (window == IntPtr.Zero)
            {
                return Fail($"Could not find a visible window containing title '{options.Title}'.");
            }

            ShowWindow(window, SwRestore);
            SetForegroundWindow(window);
            Thread.Sleep(750);

            var captureRect = Capture(window, options.OutputPath, options.ClientArea);
            if (!string.IsNullOrWhiteSpace(options.MetadataOutputPath))
            {
                File.WriteAllText(options.MetadataOutputPath, JsonSerializer.Serialize(new
                {
                    schemaVersion = "0.1",
                    title = options.Title,
                    outputPath = Path.GetFullPath(options.OutputPath),
                    captureMode = options.ClientArea ? "client-area" : "window-frame",
                    width = captureRect.Right - captureRect.Left,
                    height = captureRect.Bottom - captureRect.Top,
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
        var startInfo = new ProcessStartInfo
        {
            FileName = options.Command[0],
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

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

    private static IntPtr WaitForWindow(string title, TimeSpan timeout)
    {
        var stopAt = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < stopAt)
        {
            var window = FindWindowByTitle(title);
            if (window != IntPtr.Zero)
            {
                return window;
            }

            Thread.Sleep(250);
        }

        return IntPtr.Zero;
    }

    private static IntPtr FindWindowByTitle(string title)
    {
        var result = IntPtr.Zero;
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window))
            {
                return true;
            }

            var text = ReadWindowText(window);
            if (text.Contains(title, StringComparison.OrdinalIgnoreCase))
            {
                result = window;
                return false;
            }

            return true;
        }, IntPtr.Zero);
        return result;
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

    private static Rect Capture(IntPtr window, string outputPath, bool clientArea)
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

        bitmap.Save(outputPath, ImageFormat.Png);
        return rect;
    }

    private static Rect GetCaptureRect(IntPtr window)
    {
        if (DwmGetWindowAttribute(
                window,
                DwmwaExtendedFrameBounds,
                out Rect extendedRect,
                Marshal.SizeOf<Rect>()) == 0 &&
            extendedRect.Right > extendedRect.Left &&
            extendedRect.Bottom > extendedRect.Top)
        {
            return extendedRect;
        }

        if (GetWindowRect(window, out var rect))
        {
            return rect;
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

    private sealed record CaptureOptions(
        string Title,
        string OutputPath,
        string? MetadataOutputPath,
        TimeSpan Timeout,
        bool ClientArea,
        IReadOnlyList<string> Command)
    {
        public static CaptureOptions Parse(string[] args)
        {
            var separator = Array.IndexOf(args, "--");
            if (separator < 0 || separator == args.Length - 1)
            {
                throw new ArgumentException("Usage: WindowsWindowCapture --title <title> --output <png> [--metadata-output <json>] [--client-area] [--timeout-seconds 30] -- <command> [args...]");
            }

            string? title = null;
            string? outputPath = null;
            string? metadataOutputPath = null;
            var timeout = TimeSpan.FromSeconds(30);
            var clientArea = false;

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
                args[(separator + 1)..]);
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

    private delegate bool EnumWindowsProc(IntPtr window, IntPtr state);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr state);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

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
