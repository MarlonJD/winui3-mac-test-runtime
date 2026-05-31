namespace WinUI3.MacCompat.Diagnostics
{
    public sealed record UnsupportedApiEntry(
        string Api,
        string Kind,
        string Status,
        string? FirstSeenIn);

    public static class UnsupportedApiRegistry
    {
        private static readonly object Gate = new();
        private static readonly List<UnsupportedApiEntry> Entries = new();

        public static IReadOnlyList<UnsupportedApiEntry> Current
        {
            get
            {
                lock (Gate)
                {
                    return Entries.ToArray();
                }
            }
        }

        public static void Clear()
        {
            lock (Gate)
            {
                Entries.Clear();
            }
        }

        public static void Report(string api, string kind, string? firstSeenIn = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(api);
            ArgumentException.ThrowIfNullOrWhiteSpace(kind);

            lock (Gate)
            {
                if (Entries.Any(entry =>
                        entry.Api == api &&
                        entry.Kind == kind &&
                        entry.FirstSeenIn == firstSeenIn))
                {
                    return;
                }

                Entries.Add(new UnsupportedApiEntry(api, kind, "unsupported", firstSeenIn));
            }
        }
    }
}

namespace Microsoft.UI.Xaml.Media
{
    public sealed class MicaBackdrop
    {
        public MicaBackdrop()
        {
            WinUI3.MacCompat.Diagnostics.UnsupportedApiRegistry.Report(
                "Microsoft.UI.Xaml.Media.MicaBackdrop",
                "compat-api",
                nameof(MicaBackdrop));
        }

        public override string ToString()
        {
            return "Unsupported Microsoft.UI.Xaml.Media.MicaBackdrop";
        }
    }

    public sealed class AcrylicBrush
    {
        public AcrylicBrush()
        {
            WinUI3.MacCompat.Diagnostics.UnsupportedApiRegistry.Report(
                "Microsoft.UI.Xaml.Media.AcrylicBrush",
                "compat-api",
                nameof(AcrylicBrush));
        }

        public override string ToString()
        {
            return "Unsupported Microsoft.UI.Xaml.Media.AcrylicBrush";
        }
    }
}
