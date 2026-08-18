using Avalonia.Media;
using Avalonia.UnitTests;
using Avalonia.Media.TextFormatting;
using Avalonia.Controls.Documents;
using Xunit;
using System.Collections.Generic;

namespace Avalonia.Controls.UnitTests
{
    public class InlineTests : ScopedTestBase
    {
        [Fact]
        public void Should_Inherit_FontWeight_In_Nested_Inlines()
        {
            var bold = new Bold();
            var span = new Span();
            var run = new Run("Test");
            span.Inlines.Add(run);
            bold.Inlines.Add(span);

            var textRuns = new List<TextRun>();
            bold.BuildTextRun(textRuns, default);

            var runProperties = textRuns[0].Properties;
            Assert.NotNull(runProperties);
            Assert.Equal(FontWeight.Bold, runProperties.Typeface.Weight);
        }

        [Fact]
        public void Should_Inherit_FontStyle_In_Nested_Inlines()
        {
            var italic = new Italic();
            var span = new Span();
            var run = new Run("Test");
            span.Inlines.Add(run);
            italic.Inlines.Add(span);

            var textRuns = new List<TextRun>();
            italic.BuildTextRun(textRuns, default);

            var runProperties = textRuns[0].Properties;
            Assert.NotNull(runProperties);
            Assert.Equal(FontStyle.Italic, runProperties.Typeface.Style);
        }

        [Fact]
        public void Should_Inherit_FontStretch_In_Nested_Inlines()
        {
            var span = new Span();
            var innerSpan = new Span();
            var run = new Run("Test");
            span.FontStretch = FontStretch.Condensed;
            innerSpan.Inlines.Add(run);
            span.Inlines.Add(innerSpan);

            var textRuns = new List<TextRun>();
            span.BuildTextRun(textRuns, default);

            var runProperties = textRuns[0].Properties;
            Assert.NotNull(runProperties);
            Assert.Equal(FontStretch.Condensed, runProperties.Typeface.Stretch);
        }

        [Fact]
        public void Should_Inherit_Background_In_Nested_Inlines()
        {
            var backgroundBrush = Brushes.Red;
            var span = new Span();
            var innerSpan = new Span();
            var run = new Run("Test");

            span.Background = backgroundBrush;
            innerSpan.Inlines.Add(run);
            span.Inlines.Add(innerSpan);

            var textRuns = new List<TextRun>();
            span.BuildTextRun(textRuns, default);

            var runProperties = textRuns[0].Properties;
            Assert.NotNull(runProperties);
            Assert.Equal(backgroundBrush, runProperties.BackgroundBrush);
        }
    }
}
