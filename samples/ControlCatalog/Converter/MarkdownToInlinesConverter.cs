using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls.Documents;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ControlCatalog.Converter
{
    /// <summary>
    /// Turns a small subset of markdown into <see cref="Inline"/>s for a TextBlock: ATX headings,
    /// bullet and numbered lists, <c>**bold**</c>, <c>*italic*</c> and <c>`code`</c>.
    ///
    /// Deliberately not a markdown library. The point for this sample is that the *height* of the
    /// row is a function of the text and of how it wraps, so the virtualizing panel cannot know it
    /// without measuring — the same situation any rich-text row puts it in.
    /// </summary>
    public class MarkdownToInlinesConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string markdown || string.IsNullOrWhiteSpace(markdown))
                return new InlineCollection();

            var inlines = new InlineCollection();
            var lines = markdown.Replace("\r\n", "\n").Split('\n');
            var firstBlock = true;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (line.Length == 0)
                    continue;

                if (!firstBlock)
                    inlines.Add(new LineBreak());
                firstBlock = false;

                if (TryTakePrefix(line, "### ", out var h3))
                {
                    AddSpans(inlines, h3, FontWeight.SemiBold, 1.05);
                }
                else if (TryTakePrefix(line, "## ", out var h2))
                {
                    AddSpans(inlines, h2, FontWeight.Bold, 1.15);
                }
                else if (TryTakePrefix(line, "# ", out var h1))
                {
                    AddSpans(inlines, h1, FontWeight.Bold, 1.3);
                }
                else if (TryTakePrefix(line, "- ", out var bullet) || TryTakePrefix(line, "* ", out bullet))
                {
                    inlines.Add(new Run("  •  "));
                    AddSpans(inlines, bullet, FontWeight.Normal, 1.0);
                }
                else if (TryTakeOrderedPrefix(line, out var number, out var ordered))
                {
                    inlines.Add(new Run($"  {number}.  "));
                    AddSpans(inlines, ordered, FontWeight.Normal, 1.0);
                }
                else
                {
                    AddSpans(inlines, line, FontWeight.Normal, 1.0);
                }
            }

            return inlines;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static bool TryTakePrefix(string line, string prefix, out string rest)
        {
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                rest = line.Substring(prefix.Length);
                return true;
            }

            rest = line;
            return false;
        }

        private static bool TryTakeOrderedPrefix(string line, out int number, out string rest)
        {
            var dot = line.IndexOf(". ", StringComparison.Ordinal);
            if (dot > 0 && int.TryParse(line.Substring(0, dot), out number))
            {
                rest = line.Substring(dot + 2);
                return true;
            }

            number = 0;
            rest = line;
            return false;
        }

        /// <summary>
        /// Splits inline markup into runs. <paramref name="baseWeight"/> and
        /// <paramref name="sizeFactor"/> carry the enclosing block's styling (a heading stays bold
        /// and larger even where it contains emphasis).
        /// </summary>
        private static void AddSpans(InlineCollection inlines, string text, FontWeight baseWeight, double sizeFactor)
        {
            foreach (var (span, kind) in SplitSpans(text))
            {
                var run = new Run(span);

                if (sizeFactor != 1.0)
                    run.FontSize = 14 * sizeFactor;

                run.FontWeight = kind == SpanKind.Bold ? FontWeight.Bold : baseWeight;

                if (kind == SpanKind.Italic)
                    run.FontStyle = FontStyle.Italic;

                if (kind == SpanKind.Code)
                {
                    run.FontFamily = new FontFamily("Consolas, Menlo, monospace");
                    run.Background = new SolidColorBrush(Color.FromArgb(28, 128, 128, 128));
                }

                inlines.Add(run);
            }
        }

        private enum SpanKind { Plain, Bold, Italic, Code }

        /// <summary>
        /// Single pass over the line, splitting on the three inline markers. Unclosed markers are
        /// treated as literal text rather than swallowing the rest of the line.
        /// </summary>
        private static IEnumerable<(string Text, SpanKind Kind)> SplitSpans(string text)
        {
            var i = 0;
            var plainStart = 0;

            while (i < text.Length)
            {
                var (marker, kind) = text[i] switch
                {
                    '*' when i + 1 < text.Length && text[i + 1] == '*' => ("**", SpanKind.Bold),
                    '*' => ("*", SpanKind.Italic),
                    '`' => ("`", SpanKind.Code),
                    _ => (null, SpanKind.Plain),
                };

                if (marker is null)
                {
                    i++;
                    continue;
                }

                var contentStart = i + marker.Length;
                var close = text.IndexOf(marker, contentStart, StringComparison.Ordinal);

                if (close < 0 || close == contentStart)
                {
                    i += marker.Length;
                    continue;
                }

                if (i > plainStart)
                    yield return (text.Substring(plainStart, i - plainStart), SpanKind.Plain);

                yield return (text.Substring(contentStart, close - contentStart), kind);

                i = close + marker.Length;
                plainStart = i;
            }

            if (plainStart < text.Length)
                yield return (text.Substring(plainStart), SpanKind.Plain);
        }
    }
}
