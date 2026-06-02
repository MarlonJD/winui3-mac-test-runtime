using SkiaSharp;

namespace WinUI3.MacRenderer.Skia;

public sealed record FluentControlState(
    bool IsEnabled = true,
    bool IsFocused = false,
    bool IsChecked = false,
    bool IsSelected = false);

public sealed record FluentControlColors(
    SKColor Fill,
    SKColor Stroke,
    SKColor Text);

public static class FluentDrawingPrimitives
{
    public static FluentControlColors ControlColors(
        SkiaV2Theme theme,
        FluentControlState state,
        bool accentWhenChecked = false)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(state);

        if (!state.IsEnabled)
        {
            return new FluentControlColors(theme.DisabledSurface, theme.Stroke, theme.TextDisabled);
        }

        if (accentWhenChecked && state.IsChecked)
        {
            return new FluentControlColors(theme.Accent, theme.Accent, theme.Surface);
        }

        if (state.IsSelected)
        {
            return new FluentControlColors(theme.AccentSoft, theme.Accent, theme.Accent);
        }

        return new FluentControlColors(theme.Surface, state.IsFocused ? theme.Accent : theme.Stroke, theme.TextPrimary);
    }

    public static void DrawControlChrome(
        SKCanvas canvas,
        SKPaint paint,
        SKRect rect,
        SkiaV2Theme theme,
        FluentControlState state,
        bool accentWhenChecked = false)
    {
        var colors = ControlColors(theme, state, accentWhenChecked);
        DrawRoundRect(canvas, paint, rect, theme.ControlCornerRadius, colors.Fill);
        DrawRoundRectStroke(
            canvas,
            paint,
            rect,
            theme.ControlCornerRadius,
            colors.Stroke,
            state.IsFocused ? theme.FocusStrokeWidth : theme.StrokeWidth);
    }

    public static void DrawCheckBox(
        SKCanvas canvas,
        SKPaint paint,
        SKRect box,
        SkiaV2Theme theme,
        FluentControlState state)
    {
        var fill = !state.IsEnabled ? theme.DisabledSurface : state.IsChecked ? theme.Accent : theme.Surface;
        var stroke = state.IsChecked && state.IsEnabled ? theme.Accent : theme.Stroke;
        DrawRoundRect(canvas, paint, box, 3, fill);
        DrawRoundRectStroke(canvas, paint, box, 3, stroke);
        if (state.IsChecked)
        {
            DrawCheckMark(canvas, paint, box, state.IsEnabled ? theme.Surface : theme.TextDisabled);
        }
    }

    public static void DrawRadioButton(
        SKCanvas canvas,
        SKPaint paint,
        float centerX,
        float centerY,
        SkiaV2Theme theme,
        FluentControlState state)
    {
        DrawCircle(canvas, paint, centerX, centerY, 10, state.IsEnabled ? theme.Surface : theme.DisabledSurface);
        DrawCircleStroke(canvas, paint, centerX, centerY, 10, theme.Stroke);
        if (state.IsChecked)
        {
            DrawCircle(canvas, paint, centerX, centerY, 5, state.IsEnabled ? theme.Accent : theme.TextDisabled);
        }
    }

    public static void DrawChevronDown(SKCanvas canvas, SKPaint paint, float x, float y, SKColor color)
    {
        using var path = new SKPath();
        path.MoveTo(x, y);
        path.LineTo(x + 5, y + 5);
        path.LineTo(x + 10, y);
        DrawPathStroke(canvas, paint, path, color, 1.6f);
    }

    public static void DrawPopupSurface(
        SKCanvas canvas,
        SKPaint paint,
        SKRect rect,
        SkiaV2Theme theme,
        float? cornerRadius = null)
    {
        DrawPopupShadow(canvas, paint, rect, theme, cornerRadius);
        var radius = cornerRadius ?? theme.PopupCornerRadius;
        DrawRoundRect(canvas, paint, rect, radius, theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, radius, theme.Stroke);
    }

    public static void DrawPopupShadow(
        SKCanvas canvas,
        SKPaint paint,
        SKRect rect,
        SkiaV2Theme theme,
        float? cornerRadius = null)
    {
        if (theme.PopupShadowOffset <= 0)
        {
            return;
        }

        DrawRoundRect(
            canvas,
            paint,
            new SKRect(
                rect.Left + theme.PopupShadowOffset,
                rect.Top + theme.PopupShadowOffset,
                rect.Right + theme.PopupShadowOffset,
                rect.Bottom + theme.PopupShadowOffset),
            cornerRadius ?? theme.PopupCornerRadius,
            theme.PopupShadow);
    }

    private static void DrawRoundRect(SKCanvas canvas, SKPaint paint, SKRect rect, float radius, SKColor color)
    {
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color;
        canvas.DrawRoundRect(rect, radius, radius, paint);
    }

    private static void DrawRoundRectStroke(SKCanvas canvas, SKPaint paint, SKRect rect, float radius, SKColor color, float strokeWidth = 1)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = strokeWidth;
        paint.Color = color;
        canvas.DrawRoundRect(rect, radius, radius, paint);
        paint.Style = SKPaintStyle.Fill;
    }

    private static void DrawCircle(SKCanvas canvas, SKPaint paint, float x, float y, float radius, SKColor color)
    {
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color;
        canvas.DrawCircle(x, y, radius, paint);
    }

    private static void DrawCircleStroke(SKCanvas canvas, SKPaint paint, float x, float y, float radius, SKColor color, float strokeWidth = 2)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = strokeWidth;
        paint.Color = color;
        canvas.DrawCircle(x, y, radius, paint);
        paint.Style = SKPaintStyle.Fill;
    }

    private static void DrawCheckMark(SKCanvas canvas, SKPaint paint, SKRect box, SKColor color)
    {
        using var path = new SKPath();
        path.MoveTo(box.Left + 5, box.Top + 11);
        path.LineTo(box.Left + 9, box.Top + 15);
        path.LineTo(box.Right - 5, box.Top + 6);
        DrawPathStroke(canvas, paint, path, color, 2.2f);
    }

    private static void DrawPathStroke(SKCanvas canvas, SKPaint paint, SKPath path, SKColor color, float strokeWidth)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = strokeWidth;
        paint.StrokeCap = SKStrokeCap.Round;
        paint.StrokeJoin = SKStrokeJoin.Round;
        paint.Color = color;
        canvas.DrawPath(path, paint);
        paint.StrokeCap = SKStrokeCap.Butt;
        paint.StrokeJoin = SKStrokeJoin.Miter;
        paint.Style = SKPaintStyle.Fill;
    }
}
