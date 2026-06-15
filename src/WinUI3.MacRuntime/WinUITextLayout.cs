using Microsoft.UI.Xaml;

namespace WinUI3.MacRuntime;

public sealed record WinUITextPoint(double X, double Y);

public sealed record WinUITextRect(double X, double Y, double Width, double Height);

public sealed record WinUITextRange(int Start, int Length);

public sealed record WinUITextLine(
    int Index,
    string Text,
    int Start,
    int Length,
    double X,
    double Y,
    double Width,
    double Height,
    double Baseline);

public sealed class WinUITextLayout
{
    private const double DefaultFontSize = 14;
    private const double DefaultLineHeight = 20;
    private const double AverageGlyphWidthRatio = 0.72;

    private WinUITextLayout(
        IReadOnlyList<WinUITextLine> lines,
        WinUITextRect layoutBounds,
        WinUITextRect inkBounds,
        double lineHeight,
        bool isClipped)
    {
        Lines = lines;
        LayoutBounds = layoutBounds;
        InkBounds = inkBounds;
        LineHeight = lineHeight;
        IsClipped = isClipped;
    }

    public IReadOnlyList<WinUITextLine> Lines { get; }

    public WinUITextRect LayoutBounds { get; }

    public WinUITextRect InkBounds { get; }

    public int LineCount => Lines.Count;

    public double LineHeight { get; }

    public double DesiredWidth => LayoutBounds.Width;

    public double DesiredHeight => LayoutBounds.Height;

    public bool IsClipped { get; }

    public static WinUITextLayout Measure(
        string? text,
        TextWrapping wrapping,
        double availableWidth,
        double fontSize = DefaultFontSize,
        double lineHeight = DefaultLineHeight,
        double maxHeight = double.PositiveInfinity)
    {
        var normalized = NormalizeText(text);
        var safeWidth = Math.Max(1, availableWidth);
        var safeFontSize = fontSize > 0 ? fontSize : DefaultFontSize;
        var safeLineHeight = lineHeight > 0 ? lineHeight : DefaultLineHeight;
        var lines = BuildLines(normalized, wrapping, safeWidth, safeFontSize, safeLineHeight);
        if (lines.Count == 0)
        {
            lines.Add(CreateLine(0, string.Empty, 0, 0, safeFontSize, safeLineHeight));
        }

        var desiredHeight = lines.Count * safeLineHeight;
        var desiredWidth = wrapping == TextWrapping.NoWrap
            ? lines.Max(line => line.Width)
            : Math.Min(safeWidth, lines.Max(line => line.Width));
        var clipped = desiredHeight > maxHeight;
        var layoutBounds = new WinUITextRect(0, 0, Math.Round(desiredWidth, 3), Math.Round(desiredHeight, 3));
        var inkBounds = new WinUITextRect(0, 0, Math.Round(lines.Max(line => line.Width), 3), Math.Round(desiredHeight, 3));
        return new WinUITextLayout(lines.ToArray(), layoutBounds, inkBounds, safeLineHeight, clipped);
    }

    public int HitTestPoint(WinUITextPoint point)
    {
        if (Lines.Count == 0)
        {
            return 0;
        }

        var lineIndex = Math.Clamp((int)Math.Floor(point.Y / LineHeight), 0, Lines.Count - 1);
        var line = Lines[lineIndex];
        if (line.Length == 0)
        {
            return line.Start;
        }

        var characterWidth = line.Width / line.Length;
        var offset = Math.Clamp((int)Math.Round(point.X / Math.Max(1, characterWidth)), 0, line.Length);
        return line.Start + offset;
    }

    public WinUITextRect GetCaretRect(int textIndex)
    {
        var safeIndex = Math.Max(0, textIndex);
        var line = Lines.FirstOrDefault(candidate => safeIndex >= candidate.Start && safeIndex <= candidate.Start + candidate.Length) ??
            Lines.Last();
        var offset = Math.Clamp(safeIndex - line.Start, 0, line.Length);
        var characterWidth = line.Length == 0 ? 0 : line.Width / line.Length;
        return new WinUITextRect(
            Math.Round(line.X + offset * characterWidth, 3),
            line.Y,
            1,
            line.Height);
    }

    public WinUITextRange GetLineRange(int lineIndex)
    {
        var line = Lines[Math.Clamp(lineIndex, 0, Lines.Count - 1)];
        return new WinUITextRange(line.Start, line.Length);
    }

    private static List<WinUITextLine> BuildLines(
        string text,
        TextWrapping wrapping,
        double availableWidth,
        double fontSize,
        double lineHeight)
    {
        var lines = new List<WinUITextLine>();
        var paragraphs = text.Split('\n');
        var start = 0;
        foreach (var paragraph in paragraphs)
        {
            if (wrapping == TextWrapping.NoWrap)
            {
                lines.Add(CreateLine(lines.Count, paragraph, start, paragraph.Length, fontSize, lineHeight));
                start += paragraph.Length + 1;
                continue;
            }

            if (wrapping == TextWrapping.WrapWholeWords)
            {
                AddWholeWordLines(lines, paragraph, start, availableWidth, fontSize, lineHeight);
            }
            else
            {
                AddCharacterLines(lines, paragraph, start, availableWidth, fontSize, lineHeight);
            }

            start += paragraph.Length + 1;
        }

        return lines;
    }

    private static void AddWholeWordLines(
        List<WinUITextLine> lines,
        string paragraph,
        int paragraphStart,
        double availableWidth,
        double fontSize,
        double lineHeight)
    {
        var lineStart = 0;
        var lineText = string.Empty;
        var wordStart = 0;
        foreach (var token in SplitWords(paragraph))
        {
            var candidate = lineText.Length == 0 ? token.Text : $"{lineText} {token.Text}";
            if (MeasureWidth(candidate, fontSize) <= availableWidth)
            {
                if (lineText.Length == 0)
                {
                    lineStart = token.Start;
                }

                lineText = candidate;
                wordStart = token.Start + token.Text.Length;
                continue;
            }

            if (lineText.Length > 0)
            {
                lines.Add(CreateLine(lines.Count, lineText, paragraphStart + lineStart, lineText.Length, fontSize, lineHeight));
                lineText = string.Empty;
            }

            if (MeasureWidth(token.Text, fontSize) > availableWidth)
            {
                AddCharacterLines(lines, token.Text, paragraphStart + token.Start, availableWidth, fontSize, lineHeight);
                wordStart = token.Start + token.Text.Length;
            }
            else
            {
                lineStart = token.Start;
                lineText = token.Text;
                wordStart = token.Start + token.Text.Length;
            }
        }

        if (lineText.Length > 0)
        {
            lines.Add(CreateLine(lines.Count, lineText, paragraphStart + lineStart, lineText.Length, fontSize, lineHeight));
        }
        else if (paragraph.Length == 0 && wordStart == 0)
        {
            lines.Add(CreateLine(lines.Count, string.Empty, paragraphStart, 0, fontSize, lineHeight));
        }
    }

    private static void AddCharacterLines(
        List<WinUITextLine> lines,
        string paragraph,
        int paragraphStart,
        double availableWidth,
        double fontSize,
        double lineHeight)
    {
        if (paragraph.Length == 0)
        {
            lines.Add(CreateLine(lines.Count, string.Empty, paragraphStart, 0, fontSize, lineHeight));
            return;
        }

        var maxCharacters = Math.Max(1, (int)Math.Floor(availableWidth / CharacterWidth(fontSize)));
        for (var index = 0; index < paragraph.Length; index += maxCharacters)
        {
            var length = Math.Min(maxCharacters, paragraph.Length - index);
            var line = paragraph.Substring(index, length);
            lines.Add(CreateLine(lines.Count, line, paragraphStart + index, length, fontSize, lineHeight));
        }
    }

    private static IEnumerable<(string Text, int Start)> SplitWords(string paragraph)
    {
        var index = 0;
        while (index < paragraph.Length)
        {
            while (index < paragraph.Length && char.IsWhiteSpace(paragraph[index]))
            {
                index++;
            }

            var start = index;
            while (index < paragraph.Length && !char.IsWhiteSpace(paragraph[index]))
            {
                index++;
            }

            if (index > start)
            {
                yield return (paragraph[start..index], start);
            }
        }
    }

    private static WinUITextLine CreateLine(
        int index,
        string text,
        int start,
        int length,
        double fontSize,
        double lineHeight)
    {
        var y = index * lineHeight;
        return new WinUITextLine(
            index,
            text,
            start,
            length,
            0,
            Math.Round(y, 3),
            Math.Round(MeasureWidth(text, fontSize), 3),
            Math.Round(lineHeight, 3),
            Math.Round(y + lineHeight * 0.78, 3));
    }

    private static string NormalizeText(string? text)
    {
        return (text ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    private static double MeasureWidth(string text, double fontSize)
    {
        return Math.Ceiling(text.Length * CharacterWidth(fontSize));
    }

    private static double CharacterWidth(double fontSize)
    {
        return fontSize * AverageGlyphWidthRatio;
    }
}
