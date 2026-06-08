using SkiaSharp;

namespace WinUI3.MacRenderer.Skia;

public sealed record WinUITextMetrics(SKFontMetrics Metrics, float BaselineOffset, float LineHeight)
{
    public static WinUITextMetrics For(SKFont font)
    {
        ArgumentNullException.ThrowIfNull(font);

        var metrics = font.Metrics;
        var lineHeight = MathF.Ceiling(metrics.Descent - metrics.Ascent + metrics.Leading);
        var baselineOffset = MathF.Ceiling(-metrics.Ascent);
        return new WinUITextMetrics(metrics, baselineOffset, lineHeight);
    }

    public static float MeasureText(SKFont font, string text)
    {
        ArgumentNullException.ThrowIfNull(font);
        return font.MeasureText(text ?? string.Empty);
    }

    public static double MeasureControlWidth(SKFont font, string text, double chromePadding)
    {
        return Math.Ceiling(MeasureText(font, text) + chromePadding);
    }

    public float BaselineFor(SKRect rect)
    {
        var textHeight = Metrics.Descent - Metrics.Ascent;
        var centeredTop = rect.Top + MathF.Max(0, (rect.Height - textHeight) / 2);
        return MathF.Round(centeredTop - Metrics.Ascent, 3);
    }

    public float TopAlignedBaseline(SKRect rect, float topPadding = 0)
    {
        return MathF.Round(rect.Top + topPadding + BaselineOffset, 3);
    }
}
