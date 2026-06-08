using SkiaSharp;

namespace WinUI3.MacRenderer.Skia;

public sealed record SkiaV2Theme(
    SKColor AppBackground,
    SKColor PaneBackground,
    SKColor Surface,
    SKColor SubtleSurface,
    SKColor DisabledSurface,
    SKColor Stroke,
    SKColor SubtleStroke,
    SKColor TextPrimary,
    SKColor TextSecondary,
    SKColor TextDisabled,
    SKColor Accent,
    SKColor AccentSoft,
    SKColor Success,
    SKColor Warning,
    SKColor Error,
    SKColor PopupShadow,
    float TitleFontSize,
    float BodyFontSize,
    float CaptionFontSize,
    float IconFontSize,
    float ControlCornerRadius,
    float ContainerCornerRadius,
    float PopupCornerRadius,
    float FocusStrokeWidth,
    float StrokeWidth,
    float PopupShadowOffset)
{
    public static SkiaV2Theme For(string theme)
    {
        if (string.Equals(theme, "high-contrast", StringComparison.OrdinalIgnoreCase))
        {
            return new SkiaV2Theme(
                AppBackground: new SKColor(0x00, 0x00, 0x00),
                PaneBackground: new SKColor(0x00, 0x00, 0x00),
                Surface: new SKColor(0x00, 0x00, 0x00),
                SubtleSurface: new SKColor(0x00, 0x00, 0x00),
                DisabledSurface: new SKColor(0x20, 0x20, 0x20),
                Stroke: new SKColor(0xff, 0xff, 0xff),
                SubtleStroke: new SKColor(0x66, 0x66, 0x66),
                TextPrimary: new SKColor(0xff, 0xff, 0xff),
                TextSecondary: new SKColor(0xff, 0xff, 0x00),
                TextDisabled: new SKColor(0x99, 0x99, 0x99),
                Accent: new SKColor(0x00, 0xff, 0xff),
                AccentSoft: new SKColor(0x00, 0x33, 0x33),
                Success: new SKColor(0x00, 0xff, 0x00),
                Warning: new SKColor(0xff, 0xff, 0x00),
                Error: new SKColor(0xff, 0x66, 0x66),
                PopupShadow: new SKColor(0xff, 0xff, 0xff, 0x55),
                TitleFontSize: 22,
                BodyFontSize: 14,
                CaptionFontSize: 12,
                IconFontSize: 15,
                ControlCornerRadius: 4,
                ContainerCornerRadius: 8,
                PopupCornerRadius: 8,
                FocusStrokeWidth: 2,
                StrokeWidth: 1,
                PopupShadowOffset: 0);
        }

        if (string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase))
        {
            return new SkiaV2Theme(
                AppBackground: new SKColor(0x20, 0x22, 0x26),
                PaneBackground: new SKColor(0x2a, 0x2d, 0x33),
                Surface: new SKColor(0x31, 0x34, 0x3a),
                SubtleSurface: new SKColor(0x2b, 0x2e, 0x34),
                DisabledSurface: new SKColor(0x3a, 0x3d, 0x43),
                Stroke: new SKColor(0x4b, 0x50, 0x59),
                SubtleStroke: new SKColor(0x3a, 0x3f, 0x48),
                TextPrimary: new SKColor(0xf5, 0xf6, 0xf8),
                TextSecondary: new SKColor(0xbd, 0xc3, 0xcd),
                TextDisabled: new SKColor(0x8d, 0x94, 0xa0),
                Accent: new SKColor(0x74, 0xa7, 0xff),
                AccentSoft: new SKColor(0x2f, 0x44, 0x69),
                Success: new SKColor(0x6c, 0xd8, 0x6c),
                Warning: new SKColor(0xff, 0xc4, 0x66),
                Error: new SKColor(0xff, 0x99, 0x8f),
                PopupShadow: new SKColor(0x00, 0x00, 0x00, 0x88),
                TitleFontSize: 22,
                BodyFontSize: 14,
                CaptionFontSize: 12,
                IconFontSize: 15,
                ControlCornerRadius: 4,
                ContainerCornerRadius: 8,
                PopupCornerRadius: 8,
                FocusStrokeWidth: 2,
                StrokeWidth: 1,
                PopupShadowOffset: 4);
        }

        return new SkiaV2Theme(
            AppBackground: new SKColor(0xf3, 0xf3, 0xf3),
            PaneBackground: new SKColor(0xf9, 0xf9, 0xf9),
            Surface: new SKColor(0xff, 0xff, 0xff),
            SubtleSurface: new SKColor(0xf9, 0xf9, 0xf9),
            DisabledSurface: new SKColor(0xf0, 0xf0, 0xf0),
            Stroke: new SKColor(0xe0, 0xe0, 0xe0),
            SubtleStroke: new SKColor(0xf0, 0xf0, 0xf0),
            TextPrimary: new SKColor(0x1a, 0x1a, 0x1a),
            TextSecondary: new SKColor(0x42, 0x42, 0x42),
            TextDisabled: new SKColor(0x9a, 0x9a, 0x9a),
            Accent: new SKColor(0x00, 0x67, 0xc0),
            AccentSoft: new SKColor(0xe5, 0xf1, 0xfb),
            Success: new SKColor(0x0f, 0x7b, 0x0f),
            Warning: new SKColor(0x9d, 0x5d, 0x00),
            Error: new SKColor(0xc4, 0x2b, 0x1c),
            PopupShadow: new SKColor(0x00, 0x00, 0x00, 0x33),
            TitleFontSize: 22,
            BodyFontSize: 14,
            CaptionFontSize: 12,
            IconFontSize: 15,
            ControlCornerRadius: 4,
            ContainerCornerRadius: 8,
            PopupCornerRadius: 8,
            FocusStrokeWidth: 2,
            StrokeWidth: 1,
            PopupShadowOffset: 4);
    }
}
