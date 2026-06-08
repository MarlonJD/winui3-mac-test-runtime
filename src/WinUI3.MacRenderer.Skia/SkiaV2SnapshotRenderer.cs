using System.Globalization;
using SkiaSharp;
using WinUI3.MacRuntime;

namespace WinUI3.MacRenderer.Skia;

public sealed class SkiaV2SnapshotRenderer : ISnapshotRenderer
{
    public async Task<SnapshotResult> RenderAsync(
        UiTreeDocument tree,
        string screenshotsDirectory,
        SnapshotRenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var viewport = options?.Viewport ?? new VisualViewport(1280, 800);
        var scale = options?.Scale ?? 1.0;
        var width = Math.Max(1, (int)Math.Round(viewport.Width * scale));
        var height = Math.Max(1, (int)Math.Round(viewport.Height * scale));
        var theme = SkiaV2Theme.For(options?.Theme ?? "light");
        var fileName = options?.PreferredFileName ?? "mac-runtime.png";
        var path = Path.Combine(screenshotsDirectory, fileName);

        Directory.CreateDirectory(screenshotsDirectory);

        using var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(theme.AppBackground);
        canvas.Scale((float)scale);

        using var fonts = FontResolver.Resolve();
        using var titleFont = new SKFont(fonts.TextTypeface, theme.TitleFontSize);
        using var bodyFont = new SKFont(fonts.TextTypeface, theme.BodyFontSize);
        using var smallFont = new SKFont(fonts.TextTypeface, theme.CaptionFontSize);
        using var iconFont = new SKFont(fonts.SymbolTypeface, theme.IconFontSize);
        using var paint = new SKPaint { IsAntialias = true };

        RenderNode(canvas, tree.Root, theme, paint, titleFont, bodyFont, smallFont, iconFont, isRoot: true);
        var imageIntegrity = RuntimeImageIntegrityAnalyzer.Analyze(bitmap);

        cancellationToken.ThrowIfCancellationRequested();
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        await using var stream = File.Create(path);
        data.SaveTo(stream);

        return new SnapshotResult(
            ArtifactSchemas.SkiaV2Snapshot,
            "skia-v2-png",
            path,
            width,
            height,
            IsNonBlank: imageIntegrity.IsNonBlank,
            FontDiagnostics: fonts.Diagnostics,
            RuntimeImageIntegrity: imageIntegrity);
    }

    private static void RenderNode(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        SKFont iconFont,
        bool isRoot = false)
    {
        if (node.Layout is null || node.Layout.Visibility == "Collapsed")
        {
            return;
        }

        switch (SimpleType(node))
        {
            case "Window":
                DrawRect(canvas, paint, Rect(node), theme.AppBackground);
                RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
                break;
            case "NavigationView":
                RenderNavigationView(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
                break;
            case "Page":
            case "Frame":
            case "ContentControl":
                if (isRoot)
                {
                    DrawRect(canvas, paint, Rect(node), theme.AppBackground);
                }

                RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
                break;
            case "Grid":
                RenderGrid(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont, isRoot);
                break;
            case "ScrollViewer":
                RenderScrollViewer(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont, isRoot);
                break;
            case "StackPanel":
                RenderStackPanel(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont, isRoot);
                break;
            case "Border":
                var borderRadius = ReadFloat(node, "cornerRadius", 8);
                DrawRoundRect(canvas, paint, Rect(node), borderRadius, ReadColor(node, "background", theme.Surface));
                DrawRoundRectStroke(canvas, paint, Rect(node), borderRadius, theme.Stroke);
                RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
                break;
            case "NavigationViewItem":
                RenderNavigationItem(canvas, node, theme, paint, bodyFont, iconFont);
                break;
            case "TextBlock":
                DrawTextBlock(canvas, paint, bodyFont, node, theme);
                break;
            case "CommandBar":
                RenderCommandBar(canvas, node, theme, paint, bodyFont, titleFont, smallFont, iconFont);
                break;
            case "CommandBarFlyout":
                RenderCommandBarFlyout(canvas, node, theme, paint, bodyFont, titleFont, smallFont, iconFont);
                break;
            case "MenuFlyout":
                RenderMenuFlyout(canvas, node, theme, paint, bodyFont, smallFont);
                break;
            case "MenuBar":
                RenderMenuBar(canvas, node, theme, paint, bodyFont);
                break;
            case "MenuBarItem":
                RenderMenuBarItem(canvas, node, theme, paint, bodyFont);
                break;
            case "Expander":
                RenderExpander(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
                break;
            case "AnnotatedScrollBar":
                RenderAnnotatedScrollBar(canvas, node, theme, paint);
                break;
            case "SemanticZoom":
                RenderSemanticZoom(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
                break;
            case "SplitView":
                RenderSplitView(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
                break;
            case "TwoPaneView":
                RenderTwoPaneView(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
                break;
            case "MenuFlyoutItem":
                DrawText(canvas, paint, bodyFont, ReadString(node, "text") ?? ReadControlLabel(node, "Menu item"), (float)node.Layout.X, (float)node.Layout.Y + 19, theme.TextPrimary);
                break;
            case "ContentDialog":
                RenderContentDialog(canvas, node, theme, paint, bodyFont, smallFont);
                break;
            case "Flyout":
            case "ToolTip":
            case "TeachingTip":
                RenderPopupSurface(canvas, node, theme, paint, bodyFont, smallFont);
                break;
            case "AppBarButton":
                RenderAppBarButton(canvas, node, theme, paint, bodyFont, iconFont);
                break;
            case "RepeatButton":
                RenderButton(canvas, node, theme, paint, bodyFont);
                break;
            case "HyperlinkButton":
                RenderHyperlinkButton(canvas, node, theme, paint, bodyFont);
                break;
            case "DropDownButton":
                RenderDropDownButton(canvas, node, theme, paint, bodyFont);
                break;
            case "SplitButton":
                RenderSplitButton(canvas, node, theme, paint, bodyFont, isToggle: false);
                break;
            case "ToggleSplitButton":
                RenderSplitButton(canvas, node, theme, paint, bodyFont, isToggle: true);
                break;
            case "ToggleButton":
                RenderToggleButton(canvas, node, theme, paint, bodyFont);
                break;
            case "CheckBox":
                RenderCheckBox(canvas, node, theme, paint, bodyFont);
                break;
            case "RadioButton":
                RenderRadioButton(canvas, node, theme, paint, bodyFont);
                break;
            case "Button":
                RenderButton(canvas, node, theme, paint, bodyFont);
                break;
            case "TextBox":
                RenderTextBox(canvas, node, theme, paint, bodyFont);
                break;
            case "PasswordBox":
                RenderPasswordBox(canvas, node, theme, paint, bodyFont);
                break;
            case "AutoSuggestBox":
                RenderAutoSuggestBox(canvas, node, theme, paint, bodyFont, iconFont);
                break;
            case "Slider":
                RenderSlider(canvas, node, theme, paint);
                break;
            case "ToggleSwitch":
                RenderToggleSwitch(canvas, node, theme, paint, bodyFont);
                break;
            case "RatingControl":
                RenderRatingControl(canvas, node, theme, paint);
                break;
            case "ComboBox":
                RenderComboBox(canvas, node, theme, paint, bodyFont);
                break;
            case "ListView":
            case "ItemsControl":
                RenderListView(canvas, node, theme, paint, bodyFont, smallFont);
                break;
            case "ProgressRing":
                RenderProgressRing(canvas, node, theme, paint);
                break;
            case "ProgressBar":
                RenderProgressBar(canvas, node, theme, paint);
                break;
            case "InfoBar":
                RenderInfoBar(canvas, node, theme, paint, bodyFont, smallFont);
                break;
            case "FontIcon":
                DrawText(canvas, paint, iconFont, ReadString(node, "glyph") ?? "*", (float)node.Layout.X, (float)node.Layout.Y + 18, theme.Accent);
                break;
            case "SymbolIcon":
                RenderSymbolIcon(canvas, node, theme, paint, iconFont);
                break;
            case "Image":
                RenderImagePlaceholder(canvas, node, theme, paint, smallFont);
                break;
            case "String":
                DrawText(canvas, paint, bodyFont, ReadText(node) ?? string.Empty, (float)node.Layout.X, (float)node.Layout.Y + 19, theme.TextPrimary);
                break;
        }
    }

    private static void RenderNavigationView(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        SKFont iconFont)
    {
        var rect = Rect(node);
        var paneWidth = Math.Clamp(ReadFloat(node, "openPaneLength", 248), 220, 320);
        DrawRect(canvas, paint, new SKRect(rect.Left, rect.Top, rect.Left + paneWidth, rect.Bottom), theme.PaneBackground);
        DrawLine(canvas, paint, rect.Left + paneWidth, rect.Top, rect.Left + paneWidth, rect.Bottom, theme.Stroke);
        DrawLine(canvas, paint, rect.Left + 20, rect.Top + 20, rect.Left + 31, rect.Top + 20, theme.TextPrimary);
        DrawLine(canvas, paint, rect.Left + 20, rect.Top + 24, rect.Left + 31, rect.Top + 24, theme.TextPrimary);
        DrawLine(canvas, paint, rect.Left + 20, rect.Top + 28, rect.Left + 31, rect.Top + 28, theme.TextPrimary);

        var selectedItem = ReadString(node, "selectedItem");
        foreach (var child in node.Children.Where(child => SimpleType(child) == "NavigationViewItem"))
        {
            RenderNavigationItem(canvas, child, theme, paint, bodyFont, iconFont, string.Equals(child.Name, selectedItem, StringComparison.Ordinal));
        }

        foreach (var child in node.Children.Where(child => SimpleType(child) != "NavigationViewItem"))
        {
            if (SimpleType(child) == "StackPanel")
            {
                RenderPaneFooter(canvas, child, theme, paint, bodyFont, smallFont);
            }
            else if (SimpleType(child) == "Frame")
            {
                RenderNode(canvas, child, theme, paint, titleFont, bodyFont, smallFont, iconFont);
            }
            else
            {
                RenderNode(canvas, child, theme, paint, titleFont, bodyFont, smallFont, iconFont);
            }
        }
    }

    private static void RenderNavigationItem(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont bodyFont,
        SKFont iconFont,
        bool selected = false)
    {
        if (node.Layout is null)
        {
            return;
        }

        var row = Rect(node);
        if (selected)
        {
            DrawRoundRect(canvas, paint, row, 4, theme.AccentSoft);
            DrawRoundRect(canvas, paint, new SKRect(row.Left, row.Top + 7, row.Left + 4, row.Bottom - 7), 2, theme.Accent);
        }

        var icon = node.Children.FirstOrDefault(child => SimpleType(child) is "FontIcon" or "SymbolIcon");
        var labelX = row.Left + 16;
        if (icon is not null)
        {
            var iconRect = new SKRect(row.Left + 13, row.Top + 9, row.Left + 33, row.Top + 31);
            if (SimpleType(icon) == "SymbolIcon")
            {
                DrawText(canvas, paint, iconFont, SymbolGlyph(ReadString(icon, "symbol")), iconRect.Left, iconRect.Top + 18, selected ? theme.Accent : ControlAffordanceColor(theme));
            }
            else if (DrawNavigationIcon(canvas, paint, ReadString(icon, "glyph"), iconRect, selected ? theme.Accent : ControlAffordanceColor(theme)))
            {
                // Drawn as a deterministic primitive so shell icons do not depend on Segoe Fluent Icons availability.
            }
            else
            {
                DrawText(canvas, paint, iconFont, ReadString(icon, "glyph") ?? "*", iconRect.Left, iconRect.Top + 18, selected ? theme.Accent : ControlAffordanceColor(theme));
            }

            labelX = row.Left + 42;
        }

        DrawText(canvas, paint, bodyFont, ReadControlLabel(node, ToTitle(ReadString(node, "tag") ?? node.Name ?? "Item")), labelX, row.Top + 26, theme.TextPrimary);
    }

    private static void RenderGrid(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        SKFont iconFont,
        bool isRoot)
    {
        var rect = Rect(node);
        if (isRoot)
        {
            DrawRect(canvas, paint, rect, theme.AppBackground);
        }

        RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
    }

    private static void RenderScrollViewer(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        SKFont iconFont,
        bool isRoot)
    {
        var rect = Rect(node);
        if (isRoot)
        {
            DrawRect(canvas, paint, rect, theme.AppBackground);
        }
        else
        {
            DrawRoundRectStroke(canvas, paint, rect, 8, theme.SubtleStroke);
            var track = new SKRect(rect.Right - 7, rect.Top + 10, rect.Right - 4, rect.Bottom - 10);
            DrawRoundRect(canvas, paint, track, 2, theme.DisabledSurface);
        }

        RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
    }

    private static void RenderStackPanel(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        SKFont iconFont,
        bool isRoot)
    {
        if (isRoot)
        {
            DrawRect(canvas, paint, Rect(node), theme.AppBackground);
        }

        RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
    }

    private static void RenderPaneFooter(
        SKCanvas canvas,
        UiNode footer,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont bodyFont,
        SKFont smallFont)
    {
        if (footer.Layout is null)
        {
            return;
        }

        var rect = Rect(footer);
        DrawLine(canvas, paint, rect.Left, rect.Top - 16, rect.Right, rect.Top - 16, theme.Stroke);

        foreach (var child in footer.Children)
        {
            RenderNode(canvas, child, theme, paint, bodyFont, bodyFont, smallFont, bodyFont);
        }
    }

    private static void RenderContentFrame(
        SKCanvas canvas,
        UiNode frame,
        UiNode navigationView,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont)
    {
        if (frame.Layout is null)
        {
            return;
        }

        var rect = Rect(frame);
        var title = ReadContentTitle(frame, navigationView);
        DrawText(canvas, paint, titleFont, title, rect.Left, rect.Top, theme.TextPrimary);
        DrawText(canvas, paint, bodyFont, "Reference visual scenario", rect.Left, rect.Top + 28, theme.TextSecondary);

        var card = new SKRect(rect.Left, rect.Top + 54, rect.Right, rect.Bottom);
        DrawRoundRect(canvas, paint, card, 10, theme.Surface);
        DrawRoundRectStroke(canvas, paint, card, 10, theme.Stroke);

        var y = card.Top + 42;
        var texts = Flatten(frame).Where(node => SimpleType(node) is "TextBlock" or "String").Take(8).ToArray();
        foreach (var text in texts)
        {
            DrawText(canvas, paint, bodyFont, ReadText(text) ?? text.Name ?? "Text", card.Left + 30, y, theme.TextPrimary);
            y += 28;
        }

        if (texts.Length == 0)
        {
            DrawText(canvas, paint, bodyFont, "Operational summary", card.Left + 30, y, theme.TextPrimary);
            DrawText(canvas, paint, smallFont, "Public fixture content rendered from the managed visual tree.", card.Left + 30, y + 26, theme.TextSecondary);
        }
    }

    private static void RenderButton(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        var enabled = ReadBool(node, "isEnabled", fallback: true);
        var focused = ReadBool(node, "isFocused", fallback: false);
        var state = new FluentControlState(IsEnabled: enabled, IsFocused: focused);
        FluentDrawingPrimitives.DrawButtonChrome(canvas, paint, rect, theme, state);
        var colors = FluentDrawingPrimitives.ButtonChromeColors(theme, state);
        var label = ReadControlLabel(node, "Button");
        var origin = FluentDrawingPrimitives.ButtonTextOrigin(font, label, rect);
        DrawText(canvas, paint, font, label, origin.X, origin.Y, enabled ? ReadColor(node, "foreground", colors.Text) : colors.Text);
        RenderChildren(canvas, node, theme, paint, font, font, font, font);
    }

    private static void RenderAppBarButton(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font, SKFont iconFont)
    {
        var rect = Rect(node);
        var enabled = ReadBool(node, "isEnabled", fallback: true) && ReadBool(node, "commandCanExecute", fallback: true);
        if (rect.Width <= 40 || ReadBool(node, "commandBarCompact", fallback: false))
        {
            DrawAppBarIcon(canvas, paint, iconFont, node, new SKRect(rect.Left + 9, rect.Top + 8, rect.Left + 23, rect.Top + 22), enabled ? theme.TextPrimary : theme.TextDisabled);
            return;
        }

        var foreground = enabled ? ReadColor(node, "foreground", theme.TextPrimary) : theme.TextDisabled;
        FluentDrawingPrimitives.DrawControlChrome(canvas, paint, rect, theme, new FluentControlState(IsEnabled: enabled));
        DrawAppBarIcon(canvas, paint, iconFont, node, new SKRect(rect.Left + 12, rect.Top + 8, rect.Left + 26, rect.Top + 22), enabled ? theme.Accent : theme.TextDisabled);
        DrawText(canvas, paint, font, ReadString(node, "label") ?? ReadControlLabel(node, "Command"), rect.Left + 30, rect.Top + 21, foreground);
    }

    private static void RenderToggleButton(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        var checkedState = ReadBool(node, "isChecked", fallback: false);
        var enabled = ReadBool(node, "isEnabled", fallback: true);
        var state = new FluentControlState(IsEnabled: enabled, IsChecked: checkedState);
        FluentDrawingPrimitives.DrawButtonChrome(canvas, paint, rect, theme, state, accentWhenChecked: true);
        var colors = FluentDrawingPrimitives.ButtonChromeColors(theme, state, accentWhenChecked: true);
        var label = ReadControlLabel(node, "Toggle");
        var origin = FluentDrawingPrimitives.ButtonTextOrigin(font, label, rect);
        DrawText(canvas, paint, font, label, origin.X, origin.Y, colors.Text);
    }

    private static void RenderHyperlinkButton(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        var enabled = ReadBool(node, "isEnabled", fallback: true);
        var color = enabled ? theme.Accent : theme.TextDisabled;
        var label = ReadControlLabel(node, "Link");
        font.MeasureText(label, out var bounds);
        var x = rect.Left + (rect.Width - bounds.Width) / 2 - bounds.Left;
        DrawText(canvas, paint, font, label, x, rect.Top + 21, color);
    }

    private static void RenderDropDownButton(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        var enabled = ReadBool(node, "isEnabled", fallback: true);
        FluentDrawingPrimitives.DrawControlChrome(canvas, paint, rect, theme, new FluentControlState(IsEnabled: enabled));
        DrawText(canvas, paint, font, ReadControlLabel(node, "Drop down"), rect.Left + 14, rect.Top + 21, enabled ? theme.TextPrimary : theme.TextDisabled);
        FluentDrawingPrimitives.DrawChevronDown(canvas, paint, rect.Right - 22, ChevronTop(rect), enabled ? ControlAffordanceColor(theme) : theme.TextDisabled);
    }

    private static void RenderSplitButton(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font, bool isToggle)
    {
        var rect = Rect(node);
        var enabled = ReadBool(node, "isEnabled", fallback: true);
        var checkedState = isToggle && ReadBool(node, "isChecked", fallback: false);
        var state = new FluentControlState(IsEnabled: enabled, IsChecked: checkedState);
        FluentDrawingPrimitives.DrawControlChrome(canvas, paint, rect, theme, state, accentWhenChecked: isToggle);
        var colors = FluentDrawingPrimitives.ControlColors(theme, state, accentWhenChecked: isToggle);
        DrawText(canvas, paint, font, ReadControlLabel(node, "Split"), rect.Left + 14, rect.Top + 21, colors.Text);
        var separatorX = rect.Right - 32;
        DrawLine(canvas, paint, separatorX, rect.Top + 5, separatorX, rect.Bottom - 5, SplitSeparatorColor(theme, checkedState && enabled));
        FluentDrawingPrimitives.DrawChevronDown(canvas, paint, rect.Right - 22, ChevronTop(rect), SplitAffordanceColor(theme, colors.Text, checkedState && enabled));
    }

    private static void RenderCheckBox(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        var checkedState = ReadBool(node, "isChecked", fallback: false);
        var enabled = ReadBool(node, "isEnabled", fallback: true);
        var box = new SKRect(rect.Left, rect.Top + 6, rect.Left + 20, rect.Top + 26);
        FluentDrawingPrimitives.DrawCheckBox(canvas, paint, box, theme, new FluentControlState(IsEnabled: enabled, IsChecked: checkedState));
        DrawText(canvas, paint, font, ReadControlLabel(node, "Check box"), rect.Left + 29, rect.Top + 20, enabled ? theme.TextPrimary : theme.TextDisabled);
    }

    private static void RenderRadioButton(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        var checkedState = ReadBool(node, "isChecked", fallback: false);
        var enabled = ReadBool(node, "isEnabled", fallback: true);
        FluentDrawingPrimitives.DrawRadioButton(canvas, paint, rect.Left + 10, rect.Top + 16, theme, new FluentControlState(IsEnabled: enabled, IsChecked: checkedState));
        DrawText(canvas, paint, font, ReadControlLabel(node, "Option"), rect.Left + 29, rect.Top + 21, enabled ? theme.TextPrimary : theme.TextDisabled);
    }

    private static void DrawTextBlock(SKCanvas canvas, SKPaint paint, SKFont font, UiNode node, SkiaV2Theme theme)
    {
        var text = ReadText(node) ?? node.Name ?? string.Empty;
        var layout = node.Layout!;
        var x = (float)layout.X;
        var metrics = WinUITextMetrics.For(font);
        var y = metrics.TopAlignedBaseline(Rect(node), 3);
        var color = ReadColor(node, "foreground", theme.TextPrimary);
        DrawText(canvas, paint, font, text, x, y, color);
        if (IsBoldText(node))
        {
            DrawText(canvas, paint, font, text, x + 0.35f, y, color);
        }
    }

    private static void RenderTextBox(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        var enabled = ReadBool(node, "isEnabled", fallback: true);
        var focused = ReadBool(node, "isFocused", fallback: false);
        FluentDrawingPrimitives.DrawControlChrome(canvas, paint, rect, theme, new FluentControlState(IsEnabled: enabled, IsFocused: focused));
        DrawTextBoxText(canvas, paint, font, ReadText(node) ?? string.Empty, rect, enabled ? theme.TextPrimary : theme.TextDisabled);
        if (enabled && !string.IsNullOrWhiteSpace(ReadText(node)))
        {
            DrawCloseIcon(
                canvas,
                paint,
                new SKRect(rect.Right - 23, rect.Top + 10, rect.Right - 11, rect.Top + 22),
                ControlAffordanceColor(theme));
        }
    }

    private static void RenderPasswordBox(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        var enabled = ReadBool(node, "isEnabled", fallback: true);
        var focused = ReadBool(node, "isFocused", fallback: false);
        var header = ReadString(node, "header");
        var inputRect = string.IsNullOrWhiteSpace(header)
            ? rect
            : new SKRect(rect.Left, rect.Top + 24, rect.Right, Math.Min(rect.Bottom, rect.Top + 56));
        var passwordLength = (int)Math.Round(ReadFloat(node, "passwordLength", 0));
        var text = passwordLength > 0
            ? new string('\u2022', Math.Clamp(passwordLength, 1, 12))
            : ReadString(node, "placeholderText") ?? string.Empty;
        var color = passwordLength > 0
            ? enabled ? theme.TextPrimary : theme.TextDisabled
            : theme.TextSecondary;

        if (!string.IsNullOrWhiteSpace(header))
        {
            var metrics = WinUITextMetrics.For(font);
            DrawText(canvas, paint, font, header, rect.Left, metrics.TopAlignedBaseline(rect, 2), enabled ? theme.TextPrimary : theme.TextDisabled);
        }

        FluentDrawingPrimitives.DrawControlChrome(canvas, paint, inputRect, theme, new FluentControlState(IsEnabled: enabled, IsFocused: focused));
        if (passwordLength > 0)
        {
            DrawPasswordMask(canvas, paint, inputRect, Math.Clamp(passwordLength, 1, 12), color);
        }
        else
        {
            DrawText(canvas, paint, font, text, inputRect.Left + 10, WinUITextMetrics.For(font).BaselineFor(inputRect), color);
        }
    }

    private static void RenderAutoSuggestBox(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font, SKFont iconFont)
    {
        var rect = Rect(node);
        var enabled = ReadBool(node, "isEnabled", fallback: true);
        var focused = ReadBool(node, "isFocused", fallback: false);
        FluentDrawingPrimitives.DrawControlChrome(canvas, paint, rect, theme, new FluentControlState(IsEnabled: enabled, IsFocused: focused));
        DrawText(canvas, paint, font, ReadText(node) ?? string.Empty, rect.Left + 34, WinUITextMetrics.For(font).BaselineFor(rect), enabled ? theme.TextPrimary : theme.TextDisabled);
        DrawSearchIcon(canvas, paint, new SKRect(rect.Left + 10, rect.Top + 8, rect.Left + 26, rect.Top + 24), enabled ? ControlAffordanceColor(theme) : theme.TextDisabled);
    }

    private static void RenderComboBox(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        var enabled = ReadBool(node, "isEnabled", fallback: true);
        FluentDrawingPrimitives.DrawControlChrome(canvas, paint, rect, theme, new FluentControlState(IsEnabled: enabled));
        var text = ReadString(node, "selectedItem") ?? ReadString(node, "placeholderText") ?? "Select";
        DrawText(canvas, paint, font, text, rect.Left + 10, rect.Top + 21, enabled ? theme.TextPrimary : theme.TextDisabled);
        FluentDrawingPrimitives.DrawChevronDown(canvas, paint, rect.Right - 22, ChevronTop(rect), enabled ? ControlAffordanceColor(theme) : theme.TextDisabled);
    }

    private static void RenderSlider(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint)
    {
        FluentDrawingPrimitives.DrawSlider(
            canvas,
            paint,
            Rect(node),
            theme,
            ReadFloat(node, "minimum", 0),
            ReadFloat(node, "maximum", 100),
            ReadFloat(node, "value", 0),
            ReadBool(node, "isEnabled", fallback: true));
    }

    private static void RenderToggleSwitch(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        var enabled = ReadBool(node, "isEnabled", fallback: true);
        DrawText(canvas, paint, font, ReadString(node, "header") ?? string.Empty, rect.Left, rect.Top + 15, enabled ? theme.TextPrimary : theme.TextDisabled);
        var track = new SKRect(rect.Left, rect.Top + 32, rect.Left + 40, rect.Top + 52);
        FluentDrawingPrimitives.DrawToggleSwitch(canvas, paint, track, theme, ReadBool(node, "isOn", fallback: false), enabled);
        DrawText(canvas, paint, font, ReadBool(node, "isOn", fallback: false) ? "On" : "Off", rect.Left + 50, rect.Top + 47, enabled ? theme.TextPrimary : theme.TextDisabled);
    }

    private static void RenderRatingControl(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint)
    {
        FluentDrawingPrimitives.DrawRatingStars(
            canvas,
            paint,
            Rect(node),
            theme,
            (int)Math.Round(ReadFloat(node, "maxRating", 5)),
            ReadFloat(node, "value", 0),
            ReadBool(node, "isEnabled", fallback: true));
    }

    private static void RenderSymbolIcon(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        DrawText(canvas, paint, font, SymbolGlyph(ReadString(node, "symbol")), rect.Left + 4, rect.Top + 21, theme.Accent);
    }

    private static void RenderListView(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont bodyFont, SKFont smallFont)
    {
        var rect = Rect(node);
        var selectedIndex = (int)Math.Round(ReadFloat(node, "selectedIndex", -1));
        DrawRoundRect(canvas, paint, rect, theme.ContainerCornerRadius, theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, theme.ContainerCornerRadius, theme.Stroke);
        for (var index = 0; index < node.Children.Count; index++)
        {
            var child = node.Children[index];
            if (child.Layout is null)
            {
                continue;
            }

            if (index == selectedIndex)
            {
                var selectedRow = new SKRect(rect.Left + 6, (float)child.Layout.Y - 4, rect.Right - 6, (float)(child.Layout.Y + child.Layout.Height + 4));
                DrawRoundRect(canvas, paint, selectedRow, 6, theme.AccentSoft);
                DrawRoundRect(canvas, paint, new SKRect(selectedRow.Left, selectedRow.Top + 6, selectedRow.Left + 3, selectedRow.Bottom - 6), 2, theme.Accent);
            }

            if (child.Children.Count > 0)
            {
                RenderNode(canvas, child, theme, paint, bodyFont, bodyFont, smallFont, bodyFont, isRoot: false);
            }
            else
            {
                DrawText(canvas, paint, bodyFont, ReadText(child) ?? child.Name ?? "Item", (float)child.Layout.X, (float)child.Layout.Y + 19, index == selectedIndex ? theme.Accent : theme.TextPrimary);
            }

            DrawLine(canvas, paint, rect.Left + 12, (float)(child.Layout.Y + child.Layout.Height + 3), rect.Right - 12, (float)(child.Layout.Y + child.Layout.Height + 3), theme.Stroke);
        }
    }

    private static void RenderImagePlaceholder(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont smallFont)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, theme.ContainerCornerRadius, theme.AccentSoft);
        DrawRoundRectStroke(canvas, paint, rect, theme.ContainerCornerRadius, theme.Stroke);
        DrawLine(canvas, paint, rect.Left + 10, rect.Bottom - 10, rect.Right - 10, rect.Top + 10, theme.Accent);
        DrawText(canvas, paint, smallFont, "Image", rect.Left + 14, rect.Top + 24, theme.TextSecondary);
    }

    private static void RenderProgressBar(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint)
    {
        var rect = Rect(node);
        var isIndeterminate = ReadBool(node, "isIndeterminate", fallback: false);
        var min = ReadFloat(node, "minimum", 0);
        var max = Math.Max(min + 1, ReadFloat(node, "maximum", 100));
        var value = Math.Clamp(ReadFloat(node, "value", min), min, max);
        var progress = (value - min) / (max - min);
        var center = rect.Top + rect.Height / 2;
        var track = new SKRect(rect.Left, center - 1, rect.Right, center + 1);
        DrawRoundRect(canvas, paint, track, 1, theme.DisabledSurface);
        if (isIndeterminate)
        {
            var segmentWidth = Math.Max(28, track.Width * 0.28f);
            DrawRoundRect(canvas, paint, new SKRect(track.Left + 8, track.Top, track.Left + 8 + segmentWidth, track.Bottom), 1, theme.Accent);
            DrawRoundRect(canvas, paint, new SKRect(track.Right - segmentWidth - 8, track.Top, track.Right - 8, track.Bottom), 1, theme.AccentSoft);
            return;
        }

        DrawRoundRect(canvas, paint, new SKRect(track.Left, track.Top, track.Left + track.Width * progress, track.Bottom), 1, theme.Accent);
    }

    private static void RenderProgressRing(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint)
    {
        var rect = Rect(node);
        var radius = Math.Min(rect.Width, rect.Height) / 2 - 4;
        var centerX = rect.Left + rect.Width / 2;
        var centerY = rect.Top + rect.Height / 2;
        DrawCircleStroke(canvas, paint, centerX, centerY, radius, theme.DisabledSurface, 2);
        DrawArc(canvas, paint, new SKRect(centerX - radius, centerY - radius, centerX + radius, centerY + radius), -80, 285, theme.Accent, 3);
    }

    private static void RenderInfoBar(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont bodyFont, SKFont smallFont)
    {
        if (!ReadBool(node, "isOpen", fallback: true))
        {
            return;
        }

        var rect = Rect(node);
        var severity = ReadString(node, "severity") ?? "Informational";
        var accent = severity switch
        {
            "Success" => theme.Success,
            "Warning" => theme.Warning,
            "Error" => theme.Error,
            _ => theme.Accent
        };
        DrawRoundRect(canvas, paint, rect, theme.ContainerCornerRadius, SeverityFill(severity, theme));
        DrawRoundRectStroke(canvas, paint, rect, theme.ContainerCornerRadius, SeverityStroke(severity, theme));
        DrawCircle(canvas, paint, rect.Left + 24, rect.Top + 26, 8, accent);
        if (severity == "Success")
        {
            DrawCheckMark(canvas, paint, new SKRect(rect.Left + 17, rect.Top + 19, rect.Left + 31, rect.Top + 33), theme.Surface);
        }
        else
        {
            DrawText(canvas, paint, smallFont, SeverityGlyph(severity), rect.Left + 21, rect.Top + 31, theme.Surface);
        }

        DrawText(canvas, paint, bodyFont, ReadString(node, "title") ?? severity, rect.Left + 42, rect.Top + 27, theme.TextPrimary);
        DrawText(canvas, paint, smallFont, ReadString(node, "message") ?? string.Empty, rect.Left + 42, rect.Top + 50, theme.TextSecondary);
        if (ReadBool(node, "isClosable", fallback: true))
        {
            DrawCloseIcon(
                canvas,
                paint,
                new SKRect(rect.Right - 28, rect.Top + 17, rect.Right - 16, rect.Top + 29),
                ControlAffordanceColor(theme));
        }
    }

    private static void RenderCommandBar(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont bodyFont,
        SKFont titleFont,
        SKFont smallFont,
        SKFont iconFont)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, theme.ContainerCornerRadius, theme.Surface);
        RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
        DrawEllipsisIcon(canvas, paint, new SKRect(rect.Right - 31, rect.Top + 8, rect.Right - 13, rect.Top + 22), theme.TextPrimary);
    }

    private static void RenderCommandBarFlyout(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont bodyFont,
        SKFont titleFont,
        SKFont smallFont,
        SKFont iconFont)
    {
        if (!ReadBool(node, "isOpen", fallback: false))
        {
            return;
        }

        var rect = Rect(node);
        FluentDrawingPrimitives.DrawPopupSurface(canvas, paint, rect, theme);
        RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
    }

    private static void RenderMenuFlyout(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont bodyFont, SKFont smallFont)
    {
        if (!ReadBool(node, "isOpen", fallback: false))
        {
            return;
        }

        var rect = Rect(node);
        FluentDrawingPrimitives.DrawPopupSurface(canvas, paint, rect, theme);
        for (var index = 0; index < node.Children.Count; index++)
        {
            var child = node.Children[index];
            if (child.Layout is null)
            {
                continue;
            }

            DrawText(canvas, paint, bodyFont, ReadString(child, "text") ?? ReadControlLabel(child, "Menu item"), (float)child.Layout.X, (float)child.Layout.Y + 19, theme.TextPrimary);
            if (index < node.Children.Count - 1)
            {
                DrawLine(canvas, paint, rect.Left + 12, (float)child.Layout.Y + 31, rect.Right - 12, (float)child.Layout.Y + 31, theme.Stroke);
            }
        }
    }

    private static void RenderMenuBar(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont bodyFont)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, theme.ControlCornerRadius, theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, theme.ControlCornerRadius, theme.SubtleStroke);
        foreach (var child in node.Children)
        {
            RenderMenuBarItem(canvas, child, theme, paint, bodyFont);
        }
    }

    private static void RenderMenuBarItem(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont bodyFont)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, theme.ControlCornerRadius, theme.SubtleSurface);
        DrawText(canvas, paint, bodyFont, ReadString(node, "title") ?? ReadControlLabel(node, "Menu"), rect.Left + 12, rect.Top + 19, theme.TextPrimary);
        if (ReadFloat(node, "itemCount", node.Children.Count) > 0)
        {
            FluentDrawingPrimitives.DrawChevronDown(canvas, paint, rect.Right - 18, rect.Top + 11, theme.TextSecondary);
        }
    }

    private static void RenderExpander(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        SKFont iconFont)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, theme.ContainerCornerRadius, theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, theme.ContainerCornerRadius, theme.Stroke);
        FluentDrawingPrimitives.DrawChevronDown(canvas, paint, rect.Left + 13, rect.Top + 14, theme.TextSecondary);
        DrawText(canvas, paint, bodyFont, ReadString(node, "header") ?? "Expander", rect.Left + 32, rect.Top + 24, theme.TextPrimary);
        if (ReadBool(node, "isExpanded", fallback: false))
        {
            DrawLine(canvas, paint, rect.Left + 8, rect.Top + 34, rect.Right - 8, rect.Top + 34, theme.SubtleStroke);
            RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
        }
    }

    private static void RenderAnnotatedScrollBar(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, theme.ContainerCornerRadius, theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, theme.ContainerCornerRadius, theme.Stroke);
        var rail = new SKRect(rect.Right - 24, rect.Top + 10, rect.Right - 18, rect.Bottom - 10);
        DrawRoundRect(canvas, paint, rail, 3, theme.DisabledSurface);
        DrawRoundRect(canvas, paint, new SKRect(rail.Left, rail.Top + 14, rail.Right, Math.Min(rail.Bottom, rail.Top + 42)), 3, theme.Accent);
        var markerCount = Math.Clamp((int)Math.Round(ReadFloat(node, "markerCount", 3)), 1, 8);
        for (var index = 0; index < markerCount; index++)
        {
            var y = rail.Top + 8 + index * Math.Max(8, (rail.Height - 16) / Math.Max(1, markerCount - 1));
            DrawCircle(canvas, paint, rect.Left + 18, y, 3, theme.Accent);
            DrawLine(canvas, paint, rect.Left + 28, y, rail.Left - 8, y, theme.SubtleStroke);
        }
    }

    private static void RenderSemanticZoom(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        SKFont iconFont)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, theme.ContainerCornerRadius, theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, theme.ContainerCornerRadius, theme.Stroke);
        DrawText(canvas, paint, smallFont, "zoomed in", rect.Left + 10, rect.Top + 17, theme.TextSecondary);
        DrawText(canvas, paint, smallFont, "zoomed out", rect.Left + rect.Width / 2 + 10, rect.Top + 17, theme.TextSecondary);
        DrawLine(canvas, paint, rect.Left + rect.Width / 2, rect.Top + 8, rect.Left + rect.Width / 2, rect.Bottom - 8, theme.SubtleStroke);
        RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
    }

    private static void RenderSplitView(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        SKFont iconFont)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, theme.ContainerCornerRadius, theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, theme.ContainerCornerRadius, theme.Stroke);
        if (ReadBool(node, "isPaneOpen", fallback: false) && node.Children.Count > 0 && node.Children[0].Layout is not null)
        {
            var pane = Rect(node.Children[0]);
            DrawRoundRect(canvas, paint, new SKRect(rect.Left + 4, rect.Top + 4, pane.Right + 8, rect.Bottom - 4), theme.ControlCornerRadius, theme.PaneBackground);
            DrawLine(canvas, paint, pane.Right + 8, rect.Top + 8, pane.Right + 8, rect.Bottom - 8, theme.SubtleStroke);
        }

        RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
    }

    private static void RenderTwoPaneView(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        SKFont iconFont)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, theme.ContainerCornerRadius, theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, theme.ContainerCornerRadius, theme.Stroke);
        DrawLine(canvas, paint, rect.Left + rect.Width / 2, rect.Top + 8, rect.Left + rect.Width / 2, rect.Bottom - 8, theme.SubtleStroke);
        RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
    }

    private static void RenderContentDialog(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont bodyFont, SKFont smallFont)
    {
        if (!ReadBool(node, "isOpen", fallback: false))
        {
            return;
        }

        var rect = Rect(node);
        FluentDrawingPrimitives.DrawPopupSurface(canvas, paint, rect, theme, theme.PopupCornerRadius + 2);
        DrawText(canvas, paint, bodyFont, ReadString(node, "title") ?? "Dialog", rect.Left + 18, rect.Top + 28, theme.TextPrimary);
        var content = node.Children.FirstOrDefault();
        DrawText(canvas, paint, smallFont, ReadText(content) ?? ReadString(node, "primaryButtonText") ?? string.Empty, rect.Left + 18, rect.Top + 54, theme.TextSecondary);
        var button = new SKRect(rect.Right - 96, rect.Bottom - 42, rect.Right - 18, rect.Bottom - 12);
        DrawRoundRect(canvas, paint, button, 6, theme.AccentSoft);
        DrawRoundRectStroke(canvas, paint, button, 6, theme.Accent);
        DrawText(canvas, paint, smallFont, ReadString(node, "primaryButtonText") ?? "OK", button.Left + 18, button.Top + 20, theme.Accent);
    }

    private static void RenderPopupSurface(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont bodyFont, SKFont smallFont)
    {
        if (!ReadBool(node, "isOpen", fallback: false))
        {
            return;
        }

        var rect = Rect(node);
        FluentDrawingPrimitives.DrawPopupSurface(canvas, paint, rect, theme);
        var title = ReadString(node, "title") ?? ReadString(node, "subtitle") ?? SimpleType(node);
        DrawText(canvas, paint, bodyFont, title, rect.Left + 14, rect.Top + 26, theme.TextPrimary);
        var content = node.Children.FirstOrDefault();
        if (content is not null)
        {
            DrawText(canvas, paint, smallFont, ReadText(content) ?? ReadControlLabel(content, string.Empty), rect.Left + 14, rect.Top + 48, theme.TextSecondary);
        }
    }

    private static void RenderChildren(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        SKFont iconFont)
    {
        foreach (var child in node.Children)
        {
            RenderNode(canvas, child, theme, paint, titleFont, bodyFont, smallFont, iconFont);
        }
    }

    private static string ReadContentTitle(UiNode frame, UiNode navigationView)
    {
        var firstText = Flatten(frame).FirstOrDefault(node => SimpleType(node) == "TextBlock");
        if (!string.IsNullOrWhiteSpace(ReadText(firstText)))
        {
            return ReadText(firstText)!;
        }

        var selectedItem = ReadString(navigationView, "selectedItem");
        var selected = navigationView.Children.FirstOrDefault(child => child.Name == selectedItem);
        return selected is null ? "Content" : ReadControlLabel(selected, ToTitle(ReadString(selected, "tag") ?? "Content"));
    }

    private static string ReadControlLabel(UiNode item, string fallback)
    {
        return ReadString(item, "automationName") ??
            ReadString(item, "content") ??
            ReadText(item) ??
            fallback;
    }

    private static IEnumerable<UiNode> Flatten(UiNode root)
    {
        yield return root;
        foreach (var child in root.Children)
        {
            foreach (var nested in Flatten(child))
            {
                yield return nested;
            }
        }
    }

    private static UiNode? FindByName(UiNode root, string name)
    {
        return Flatten(root).FirstOrDefault(node => string.Equals(node.Name, name, StringComparison.Ordinal));
    }

    private static string SimpleType(UiNode node)
    {
        var type = node.Type;
        var dot = type.LastIndexOf('.');
        return dot < 0 ? type : type[(dot + 1)..];
    }

    private static SKRect Rect(UiNode node)
    {
        var layout = node.Layout ?? throw new InvalidOperationException($"Node '{node.Name ?? node.Type}' does not have layout.");
        return new SKRect(
            (float)layout.X,
            (float)layout.Y,
            (float)(layout.X + layout.Width),
            (float)(layout.Y + layout.Height));
    }

    private static string? ReadText(UiNode? node)
    {
        return node is null ? null : ReadString(node, "text");
    }

    private static string? ReadString(UiNode node, string key)
    {
        return node.Properties.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static bool IsBoldText(UiNode node)
    {
        var weight = ReadString(node, "fontWeight");
        return weight is not null &&
            (weight.Contains("Bold", StringComparison.OrdinalIgnoreCase) ||
             weight.Contains("SemiBold", StringComparison.OrdinalIgnoreCase));
    }

    private static string? FindFirstGlyph(UiNode node)
    {
        var icon = Flatten(node).FirstOrDefault(child => SimpleType(child) == "FontIcon");
        return icon is null ? null : ReadString(icon, "glyph");
    }

    private static void DrawAppBarIcon(SKCanvas canvas, SKPaint paint, SKFont iconFont, UiNode node, SKRect rect, SKColor color)
    {
        var label = ReadString(node, "label") ?? ReadControlLabel(node, string.Empty);
        if (label.Contains("Save", StringComparison.OrdinalIgnoreCase))
        {
            DrawSaveIcon(canvas, paint, rect, color);
            return;
        }

        if (label.Contains("Refresh", StringComparison.OrdinalIgnoreCase))
        {
            DrawRefreshIcon(canvas, paint, rect, color);
            return;
        }

        DrawText(canvas, paint, iconFont, FindFirstGlyph(node) ?? "*", rect.Left, rect.Bottom, color);
    }

    private static void DrawSaveIcon(SKCanvas canvas, SKPaint paint, SKRect rect, SKColor color)
    {
        var body = new SKRect(rect.Left + 2, rect.Top + 3, rect.Right - 2, rect.Bottom - 2);
        DrawRoundRectStroke(canvas, paint, body, 1, color, 1.1f);
        DrawLine(canvas, paint, body.Left + 3, body.Top + 1, body.Left + 8, body.Top + 1, color);
        DrawLine(canvas, paint, body.Left + 3, body.Bottom - 3, body.Right - 3, body.Bottom - 3, color);
    }

    private static void DrawRefreshIcon(SKCanvas canvas, SKPaint paint, SKRect rect, SKColor color)
    {
        DrawArc(canvas, paint, new SKRect(rect.Left + 1, rect.Top + 1, rect.Right - 1, rect.Bottom - 1), -45, 285, color, 1.2f);
        using var path = new SKPath();
        path.MoveTo(rect.Right - 2, rect.Top + 5);
        path.LineTo(rect.Right - 2, rect.Top + 1);
        path.LineTo(rect.Right - 6, rect.Top + 2);
        DrawPathStroke(canvas, paint, path, color, 1.2f);
    }

    private static void DrawSearchIcon(SKCanvas canvas, SKPaint paint, SKRect rect, SKColor color)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 1.5f;
        paint.StrokeCap = SKStrokeCap.Round;
        paint.Color = color;
        canvas.DrawCircle(rect.Left + 6, rect.Top + 6, 4.5f, paint);
        canvas.DrawLine(rect.Left + 10, rect.Top + 10, rect.Right - 1, rect.Bottom - 1, paint);
        paint.StrokeCap = SKStrokeCap.Butt;
        paint.Style = SKPaintStyle.Fill;
    }

    private static void DrawCloseIcon(SKCanvas canvas, SKPaint paint, SKRect rect, SKColor color)
    {
        DrawLine(canvas, paint, rect.Left + 2, rect.Top + 2, rect.Right - 2, rect.Bottom - 2, color);
        DrawLine(canvas, paint, rect.Right - 2, rect.Top + 2, rect.Left + 2, rect.Bottom - 2, color);
    }

    private static void DrawPasswordMask(SKCanvas canvas, SKPaint paint, SKRect rect, int count, SKColor color)
    {
        var centerY = rect.Top + rect.Height / 2;
        var x = rect.Left + 13;
        for (var index = 0; index < count; index++)
        {
            DrawCircle(canvas, paint, x + index * 8, centerY, 2.25f, color);
        }
    }

    private static void DrawEllipsisIcon(SKCanvas canvas, SKPaint paint, SKRect rect, SKColor color)
    {
        var centerY = rect.Top + rect.Height / 2;
        DrawCircle(canvas, paint, rect.Left + 4, centerY, 1.5f, color);
        DrawCircle(canvas, paint, rect.Left + 9, centerY, 1.5f, color);
        DrawCircle(canvas, paint, rect.Left + 14, centerY, 1.5f, color);
    }

    private static bool ShouldRenderLayoutSurface(UiNode node)
    {
        return !string.IsNullOrWhiteSpace(node.Name);
    }

    private static void DrawGridColumnSeparators(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKRect rect)
    {
        var columnStarts = node.Children
            .Where(child => ReadFloat(child, "gridColumn", 0) > 0 && child.Layout is not null)
            .Select(child => (float)child.Layout!.X)
            .Distinct()
            .Order()
            .ToArray();
        foreach (var x in columnStarts)
        {
            if (x > rect.Left && x < rect.Right)
            {
                DrawLine(canvas, paint, x - 8, rect.Top + 8, x - 8, rect.Bottom - 8, theme.SubtleStroke);
            }
        }
    }

    private static bool ReadBool(UiNode node, string key, bool fallback)
    {
        return node.Properties.TryGetValue(key, out var value) && bool.TryParse(value?.ToString(), out var boolean)
            ? boolean
            : fallback;
    }

    private static SKColor ReadColor(UiNode node, string key, SKColor fallback)
    {
        if (!node.Properties.TryGetValue(key, out var value))
        {
            return fallback;
        }

        var text = value?.ToString();
        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith('#') || text.Length != 7)
        {
            return fallback;
        }

        return byte.TryParse(text.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red) &&
            byte.TryParse(text.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green) &&
            byte.TryParse(text.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue)
                ? new SKColor(red, green, blue)
                : fallback;
    }

    private static float ReadFloat(UiNode node, string key, float fallback)
    {
        if (!node.Properties.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : fallback;
    }

    private static string ToTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Item";
        }

        var normalized = value.Replace("-", " ", StringComparison.Ordinal);
        return string.Join(
            " ",
            normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(part =>
                char.ToUpperInvariant(part[0]) + (part.Length == 1 ? string.Empty : part[1..])));
    }

    private static string SymbolGlyph(string? symbol)
    {
        return symbol switch
        {
            "Accept" => "\uE73E",
            "Find" => "\uE721",
            "Link" => "\uE71B",
            _ => "\uE10F"
        };
    }

    private static void DrawRect(SKCanvas canvas, SKPaint paint, SKRect rect, SKColor color)
    {
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color;
        canvas.DrawRect(rect, paint);
    }

    private static float ChevronTop(SKRect rect) => rect.Top + (rect.Height - 4) / 2;

    private static SKColor ControlAffordanceColor(SkiaV2Theme theme) => WithAlpha(theme.TextSecondary, 0xcc);

    private static SKColor SeverityFill(string severity, SkiaV2Theme theme)
    {
        return severity switch
        {
            "Success" => new SKColor(0xdf, 0xf6, 0xdd),
            "Warning" => new SKColor(0xff, 0xf4, 0xce),
            "Error" => new SKColor(0xfd, 0xe7, 0xe9),
            _ => theme.AccentSoft
        };
    }

    private static SKColor SeverityStroke(string severity, SkiaV2Theme theme)
    {
        return severity switch
        {
            "Success" => new SKColor(0x9f, 0xd8, 0x9b),
            "Warning" => new SKColor(0xf0, 0xd7, 0x83),
            "Error" => new SKColor(0xf3, 0xb3, 0xb8),
            _ => theme.Stroke
        };
    }

    private static SKColor SplitAffordanceColor(SkiaV2Theme theme, SKColor textColor, bool isChecked)
    {
        return isChecked ? WithAlpha(theme.Surface, 0xcc) : WithAlpha(textColor, 0xcc);
    }

    private static SKColor SplitSeparatorColor(SkiaV2Theme theme, bool isChecked)
    {
        return isChecked ? WithAlpha(theme.Surface, 0x55) : theme.SubtleStroke;
    }

    private static SKColor WithAlpha(SKColor color, byte alpha) => new(color.Red, color.Green, color.Blue, alpha);

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

    private static void DrawLine(SKCanvas canvas, SKPaint paint, float x1, float y1, float x2, float y2, SKColor color)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 1;
        paint.Color = color;
        canvas.DrawLine(x1, y1, x2, y2, paint);
        paint.Style = SKPaintStyle.Fill;
    }

    private static bool DrawNavigationIcon(SKCanvas canvas, SKPaint paint, string? glyph, SKRect rect, SKColor color)
    {
        if (string.IsNullOrEmpty(glyph))
        {
            return false;
        }

        var code = char.ConvertToUtf32(glyph, 0);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 1.6f;
        paint.StrokeCap = SKStrokeCap.Round;
        paint.StrokeJoin = SKStrokeJoin.Round;
        paint.Color = color;
        switch (code)
        {
            case 0xE80F:
                using (var path = new SKPath())
                {
                    path.MoveTo(rect.Left + 3, rect.Top + 11);
                    path.LineTo(rect.MidX, rect.Top + 4);
                    path.LineTo(rect.Right - 3, rect.Top + 11);
                    path.MoveTo(rect.Left + 5, rect.Top + 10);
                    path.LineTo(rect.Left + 5, rect.Bottom - 4);
                    path.LineTo(rect.Right - 5, rect.Bottom - 4);
                    path.LineTo(rect.Right - 5, rect.Top + 10);
                    canvas.DrawPath(path, paint);
                }
                break;
            case 0xE716:
                canvas.DrawRoundRect(new SKRect(rect.Left + 3, rect.Top + 5, rect.Right - 3, rect.Bottom - 6), 4, 4, paint);
                DrawLine(canvas, paint, rect.Left + 8, rect.Bottom - 6, rect.Left + 5, rect.Bottom - 2, color);
                break;
            case 0xE713:
                canvas.DrawCircle(rect.MidX, rect.MidY, 6, paint);
                DrawLine(canvas, paint, rect.MidX, rect.Top + 3, rect.MidX, rect.Top + 7, color);
                DrawLine(canvas, paint, rect.MidX, rect.Bottom - 7, rect.MidX, rect.Bottom - 3, color);
                DrawLine(canvas, paint, rect.Left + 3, rect.MidY, rect.Left + 7, rect.MidY, color);
                DrawLine(canvas, paint, rect.Right - 7, rect.MidY, rect.Right - 3, rect.MidY, color);
                break;
            case 0xE8D7:
                using (var path = new SKPath())
                {
                    path.MoveTo(rect.MidX, rect.Top + 3);
                    path.LineTo(rect.Right - 4, rect.Top + 7);
                    path.LineTo(rect.Right - 6, rect.Bottom - 5);
                    path.LineTo(rect.MidX, rect.Bottom - 2);
                    path.LineTo(rect.Left + 6, rect.Bottom - 5);
                    path.LineTo(rect.Left + 4, rect.Top + 7);
                    path.Close();
                    canvas.DrawPath(path, paint);
                }
                break;
            case 0xE9D2:
                canvas.DrawRoundRect(new SKRect(rect.Left + 4, rect.Top + 8, rect.Right - 4, rect.Bottom - 4), 2, 2, paint);
                DrawLine(canvas, paint, rect.Left + 8, rect.Top + 8, rect.Left + 8, rect.Top + 5, color);
                DrawLine(canvas, paint, rect.Right - 8, rect.Top + 8, rect.Right - 8, rect.Top + 5, color);
                DrawLine(canvas, paint, rect.Left + 8, rect.Top + 5, rect.Right - 8, rect.Top + 5, color);
                break;
            case 0xE77B:
                canvas.DrawCircle(rect.MidX, rect.Top + 8, 4, paint);
                using (var path = new SKPath())
                {
                    path.MoveTo(rect.Left + 5, rect.Bottom - 4);
                    path.CubicTo(rect.Left + 7, rect.Bottom - 10, rect.Right - 7, rect.Bottom - 10, rect.Right - 5, rect.Bottom - 4);
                    canvas.DrawPath(path, paint);
                }
                break;
            default:
                paint.Style = SKPaintStyle.Fill;
                paint.StrokeCap = SKStrokeCap.Butt;
                paint.StrokeJoin = SKStrokeJoin.Miter;
                return false;
        }

        paint.Style = SKPaintStyle.Fill;
        paint.StrokeCap = SKStrokeCap.Butt;
        paint.StrokeJoin = SKStrokeJoin.Miter;
        return true;
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

    private static void DrawText(SKCanvas canvas, SKPaint paint, SKFont font, string text, float x, float y, SKColor color)
    {
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color;
        canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
    }

    private static void DrawTextBoxText(SKCanvas canvas, SKPaint paint, SKFont font, string text, SKRect rect, SKColor color)
    {
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var metrics = WinUITextMetrics.For(font);
        var x = rect.Left + 10;
        var y = metrics.BaselineFor(new SKRect(rect.Left, rect.Top, rect.Right, Math.Min(rect.Bottom, rect.Top + 32)));
        foreach (var line in lines)
        {
            if (y > rect.Bottom - 6)
            {
                break;
            }

            DrawText(canvas, paint, font, line, x, y, color);
            y += metrics.LineHeight + 4;
        }
    }

    private static void DrawCheckMark(SKCanvas canvas, SKPaint paint, SKRect box, SKColor color)
    {
        using var path = new SKPath();
        path.MoveTo(box.Left + 5, box.Top + 11);
        path.LineTo(box.Left + 9, box.Top + 15);
        path.LineTo(box.Right - 5, box.Top + 6);
        DrawPathStroke(canvas, paint, path, color, 2.2f);
    }

    private static void DrawArc(SKCanvas canvas, SKPaint paint, SKRect rect, float startAngle, float sweepAngle, SKColor color, float strokeWidth)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = strokeWidth;
        paint.StrokeCap = SKStrokeCap.Round;
        paint.Color = color;
        canvas.DrawArc(rect, startAngle, sweepAngle, false, paint);
        paint.StrokeCap = SKStrokeCap.Butt;
        paint.Style = SKPaintStyle.Fill;
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

    private static string SeverityGlyph(string severity)
    {
        return severity switch
        {
            "Warning" => "!",
            "Error" => "!",
            _ => "i"
        };
    }

}
