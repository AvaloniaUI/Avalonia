using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters
{
    public class TextPresenter_Tests : ScopedTestBase
    {
        [Fact]
        public void TextPresenter_Can_Contain_Null_With_Password_Char_Set()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextPresenter
                {
                    PasswordChar = '*'
                };

                Assert.NotNull(target.TextLayout);
            }
        }

        [Fact]
        public void TextPresenter_Can_Contain_Null_WithOut_Password_Char_Set()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {

                var target = new TextPresenter();

                Assert.NotNull(target.TextLayout);
            }
        }

        [Fact]
        public void Text_Presenter_Replaces_Formatted_Text_With_Password_Char()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {

                var target = new TextPresenter { PasswordChar = '*', Text = "Test" };

                target.Measure(Size.Infinity);

                Assert.NotNull(target.TextLayout);

                var actual = string.Join(null,
                    target.TextLayout.TextLines.SelectMany(x => x.TextRuns).Select(x => x.Text.ToString()));

                Assert.Equal("****", actual);
            }
        }
        
        [Theory]
        [InlineData(FontStretch.Condensed)]
        [InlineData(FontStretch.Expanded)]
        [InlineData(FontStretch.Normal)]
        [InlineData(FontStretch.ExtraCondensed)]
        [InlineData(FontStretch.SemiCondensed)]
        [InlineData(FontStretch.ExtraExpanded)]
        [InlineData(FontStretch.SemiExpanded)]
        [InlineData(FontStretch.UltraCondensed)]
        [InlineData(FontStretch.UltraExpanded)]
        public void TextPresenter_Should_Use_FontStretch_Property(FontStretch fontStretch)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter { FontStretch = fontStretch, Text = "test" };
                Assert.NotNull(presenter.TextLayout);
                Assert.Equal(1, presenter.TextLayout.TextLines.Count);
                Assert.Equal(1, presenter.TextLayout.TextLines[0].TextRuns.Count);
                Assert.NotNull(presenter.TextLayout.TextLines[0].TextRuns[0].Properties);
                Assert.Equal(fontStretch, presenter.TextLayout.TextLines[0].TextRuns[0].Properties!.Typeface.Stretch);
            }
        }

        [Fact]
        public void Measure_And_Arrange_Should_Use_WidthIncludingTrailingWhitespace_For_Bounds()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    Text = "fy",
                    FontStyle = FontStyle.Italic,
                    FontSize = 48,
                    UseLayoutRounding = false
                };

                presenter.Measure(Size.Infinity);

                var expectedSize = new Size(presenter.TextLayout.WidthIncludingTrailingWhitespace, presenter.TextLayout.Height);

                Assert.Equal(expectedSize, presenter.DesiredSize);

                presenter.Arrange(new Rect(default, presenter.DesiredSize));

                Assert.Equal(new Rect(default, expectedSize), presenter.Bounds);
            }
        }

        [Fact]
        public void Spell_Check_Ranges_Do_Not_Invalidate_Text_Layout()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    Text = "Ths is a sample",
                    SelectionStart = 1,
                    SelectionEnd = 2,
                    SelectionForegroundBrush = Brushes.White,
                    ShowSelectionHighlight = true
                };

                var layout = presenter.TextLayout;

                presenter.SetSpellCheckRanges(new[] { new SpellCheckResult(0, 3) });

                // Adding underlines must preserve shaping and selection formatting.
                Assert.Same(layout, presenter.TextLayout);
                Assert.Equal(new[] { new SpellCheckResult(0, 3) }, presenter.SpellCheckRanges);

                var selectedRun = presenter.TextLayout.TextLines
                    .SelectMany(x => x.TextRuns)
                    .Single(x => x.Text.ToString() == "h");

                Assert.Equal(Brushes.White, selectedRun.Properties!.ForegroundBrush);

                presenter.SetSpellCheckRanges(null);

                Assert.Same(layout, presenter.TextLayout);
                Assert.Null(presenter.SpellCheckRanges);
            }
        }

        [Fact]
        public void Spell_Check_Underline_Is_Built_Per_Visual_Run_And_Line()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    Text = "Ths is a sample",
                    Width = 1000
                };

                presenter.Measure(new Size(1000, 1000));

                presenter.SetSpellCheckRanges(new[] { new SpellCheckResult(0, 3), new SpellCheckResult(9, 6) });

                // Two misspellings on one line: one underline each.
                Assert.Equal(2, presenter.GetSpellCheckUnderlinesForTests().Count);

                // A range that only exists past the end of the text produces nothing.
                presenter.SetSpellCheckRanges(new[] { new SpellCheckResult(40, 3) });
                Assert.Empty(presenter.GetSpellCheckUnderlinesForTests());
            }
        }

        [Fact]
        public void Spell_Check_Underline_Spans_Wrapped_Lines()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    Text = "aaaa bbbb",
                    TextWrapping = TextWrapping.Wrap,
                    Width = 50
                };

                presenter.Measure(new Size(50, 1000));

                Assert.Equal(2, presenter.TextLayout.TextLines.Count);

                // One range covering both words is drawn once per line it touches.
                presenter.SetSpellCheckRanges(new[] { new SpellCheckResult(0, 9) });

                Assert.Equal(2, presenter.GetSpellCheckUnderlinesForTests().Count);
            }
        }

        [Fact]
        public void Spell_Check_Underline_Is_Built_For_Right_To_Left_Text()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    Text = "שלום עולם",
                    FlowDirection = FlowDirection.RightToLeft,
                    Width = 1000
                };

                presenter.Measure(new Size(1000, 1000));
                presenter.SetSpellCheckRanges(new[] { new SpellCheckResult(0, 4) });

                Assert.Single(presenter.GetSpellCheckUnderlinesForTests());
            }
        }

        [Fact]
        public void Spell_Check_Underlines_Survive_Preedit_Text()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    Text = "Ths is a sample",
                    CaretIndex = 4,
                    Width = 1000
                };

                presenter.Measure(new Size(1000, 1000));
                presenter.SetSpellCheckRanges(new[] { new SpellCheckResult(0, 3), new SpellCheckResult(9, 6) });

                // Composition text is inserted at the caret; ranges after it shift, ranges before it stay.
                presenter.PreeditText = "xx";

                Assert.Equal(2, presenter.GetSpellCheckUnderlinesForTests().Count);
            }
        }

        [Fact]
        public void Spell_Check_Underline_Crossing_Preedit_Leaves_A_Gap_For_Composition()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    Text = "abcdef",
                    CaretIndex = 3,
                    Width = 1000,
                    PreeditText = "xx"
                };

                presenter.Measure(new Size(1000, 1000));
                presenter.SetSpellCheckRanges(new[] { new SpellCheckResult(0, 6) });

                // The original word is split around the composition text rather than underlining it.
                Assert.Equal(2, presenter.GetSpellCheckUnderlinesForTests().Count);
            }
        }

        [Fact]
        public void Spell_Check_Range_Finder_Waits_For_Non_Zero_Layout_Bounds()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    Text = "sample",
                    Width = 100
                };
                var ranges = new List<SpellCheckRange>();

                presenter.Measure(new Size(100, 100));

                Assert.False(SpellCheckRangeFinder.TryGetVisibleRanges(presenter, null, presenter.Text, ranges));
                Assert.Empty(ranges);

                presenter.Arrange(new Rect(0, 0, 100, 30));

                Assert.True(SpellCheckRangeFinder.TryGetVisibleRanges(presenter, null, presenter.Text, ranges));
                Assert.NotEmpty(ranges);
            }
        }

        [Fact]
        public void Spell_Check_Range_Finder_Maps_Preedit_Lines_Back_To_Text()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    Text = "ok\nwrod",
                    CaretIndex = 0,
                    PreeditText = "x\n",
                    Width = 1000
                };

                presenter.Measure(new Size(1000, 1000));

                var lines = presenter.TextLayout.TextLines;
                Assert.Equal(3, lines.Count);

                // Show the preedit line and the first source-text line, but not the final source line.
                var visibleHeight = lines[0].Height + lines[1].Height - 0.1;
                presenter.Arrange(new Rect(0, 0, 1000, visibleHeight));

                var ranges = new List<SpellCheckRange>();
                Assert.True(SpellCheckRangeFinder.TryGetVisibleRanges(
                    presenter,
                    null,
                    presenter.Text,
                    ranges));

                Assert.Equal(new SpellCheckRange(0, 3), Assert.Single(ranges));
                Assert.Equal(3, presenter.GetTextPositionFromLayoutPosition(5));
                Assert.Equal(7, presenter.GetTextPositionFromLayoutPosition(9));
            }
        }

        [Fact]
        public void Spell_Check_Range_Finder_Uses_The_Rendered_Vertical_Offset()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    Text = "one\ntwo\nthree",
                    Width = 1000
                };

                presenter.Measure(new Size(1000, double.PositiveInfinity));
                var lines = presenter.TextLayout.TextLines;
                Assert.Equal(3, lines.Count);

                var visibleHeight = System.Math.Max(1, System.Math.Floor(lines[2].Height) - 1);
                presenter.Arrange(new Rect(0, 0, 1000, visibleHeight));
                presenter.VerticalAlignment = Layout.VerticalAlignment.Bottom;
                Assert.True(presenter.GetTextVerticalOffset() < 0);

                var ranges = new List<SpellCheckRange>();
                Assert.True(SpellCheckRangeFinder.TryGetVisibleRanges(
                    presenter,
                    null,
                    presenter.Text,
                    ranges));

                var range = Assert.Single(ranges);
                Assert.Equal((8, 13), (range.Start, range.End));
            }
        }

        [Fact]
        public void Spell_Check_Range_Finder_Accounts_For_Aligned_Line_Origin()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    Text = "misspelled",
                    TextAlignment = TextAlignment.Right,
                    Width = 1000,
                    Margin = new Thickness(500, 0, 0, 0)
                };
                var scrollViewer = new ScrollViewer
                {
                    Content = presenter,
                    HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden,
                    VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden
                };

                scrollViewer.Measure(new Size(100, 100));
                scrollViewer.Arrange(new Rect(0, 0, 100, 100));

                Assert.Single(presenter.TextLayout.TextLines);

                var ranges = new List<SpellCheckRange>();
                Assert.False(SpellCheckRangeFinder.TryGetVisibleRanges(
                    presenter,
                    scrollViewer,
                    presenter.Text,
                    ranges));
                Assert.Empty(ranges);
            }
        }

        [Theory]
        [InlineData(FlowDirection.LeftToRight, TextAlignment.Left)]
        [InlineData(FlowDirection.LeftToRight, TextAlignment.Center)]
        [InlineData(FlowDirection.LeftToRight, TextAlignment.Right)]
        [InlineData(FlowDirection.RightToLeft, TextAlignment.Left)]
        [InlineData(FlowDirection.RightToLeft, TextAlignment.Center)]
        [InlineData(FlowDirection.RightToLeft, TextAlignment.Right)]
        public void Spell_Check_Range_Finder_Includes_Visible_Text_In_Clipped_Mixed_Bidi_Line(
            FlowDirection flowDirection, TextAlignment alignment)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                const string text = "hello שלום עולם goodbye";
                var presenter = new TextPresenter
                {
                    Text = text,
                    FlowDirection = flowDirection,
                    TextAlignment = alignment,
                    FontSize = 16,
                    TextWrapping = TextWrapping.NoWrap
                };

                presenter.Measure(new Size(120, double.PositiveInfinity));
                presenter.Arrange(new Rect(0, 0, 120, 30));

                Assert.True(presenter.TextLayout.TextLines[0].Width > presenter.Bounds.Width);

                var ranges = new List<SpellCheckRange>();
                Assert.True(SpellCheckRangeFinder.TryGetVisibleRanges(
                    presenter,
                    null,
                    text,
                    ranges));

                // Cover every visible glyph without including off-screen bidi runs.
                Assert.True(ranges.Sum(r => r.End - r.Start) < text.Length);
                for (var i = 0; i < text.Length; i++)
                {
                    if (presenter.TextLayout.TextLines[0].GetTextBounds(i, 1)
                        .Any(b => b.Rectangle.Right > 0 && b.Rectangle.Left < 120))
                    {
                        Assert.Contains(ranges, r => r.Start <= i && r.End > i);
                    }
                }
            }
        }

        [Theory]
        [InlineData(FlowDirection.LeftToRight)]
        [InlineData(FlowDirection.RightToLeft)]
        public void Clipped_Long_Mixed_Direction_Line_Only_Checks_Visible_Runs(FlowDirection direction)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var text = string.Concat(Enumerable.Repeat("word ", 12000)) + "אבג";
                var presenter = new TextPresenter
                {
                    Text = text,
                    FlowDirection = direction,
                    TextWrapping = TextWrapping.NoWrap,
                };
                presenter.Measure(new Size(200, 30));
                presenter.Arrange(new Rect(0, 0, 200, 30));
                var ranges = new List<SpellCheckRange>();

                Assert.True(SpellCheckRangeFinder.TryGetVisibleRanges(presenter, null, text, ranges));
                Assert.InRange(ranges.Sum(r => r.End - r.Start), 1, 100);
                for (var i = 1; i < ranges.Count; i++)
                    Assert.True(ranges[i - 1].End < ranges[i].Start);
            }
        }

        public static IEnumerable<object[]> ScrolledBidiPreeditCases()
        {
            foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            foreach (var alignment in new[] { TextAlignment.Left, TextAlignment.Center, TextAlignment.Right })
            foreach (var preedit in new[] { "", "אבג", "Latin ", "אבג\nLatin" })
            foreach (var text in new[]
            {
                "hello שלום עולם goodbye second שלום last",
                "prefix first line\nhello שלום second עולם goodbye last",
                "abc مرحبا بالعالم xyz שלום end",
                "hello שלום 👨‍👩‍👧‍👦 goodbye last"
            })
                yield return new object[] { direction, alignment, preedit, text };
        }

        [Theory]
        [MemberData(nameof(ScrolledBidiPreeditCases))]
        public void Spell_Check_Ranges_Cover_Drawn_Glyphs_After_Bidi_Preedit_And_Scrolling(
            FlowDirection direction, TextAlignment alignment, string preedit, string text)
        {
            using (UnitTestApplication.Start(TestServices.TextServices))
            {
                var presenter = new TextPresenter
                {
                    Text = text, FlowDirection = direction, TextAlignment = alignment,
                    CaretIndex = 6, PreeditText = preedit, TextWrapping = TextWrapping.NoWrap
                };
                var scroll = new ScrollViewer
                {
                    Width = 110, Height = 25, Content = presenter,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    Template = new FuncControlTemplate<ScrollViewer>((owner, scope) =>
                        new ScrollContentPresenter { Name = "PART_ContentPresenter" }.RegisterInNameScope(scope))
                };
                var root = new TestRoot(scroll);
                root.ExecuteInitialLayoutPass();
                Assert.True(scroll.Viewport.Width > 0);
                Assert.True(scroll.Extent.Width > scroll.Viewport.Width);

                for (var fraction = 0; fraction <= 4; fraction++)
                {
                    scroll.Offset = new Vector((scroll.Extent.Width - scroll.Viewport.Width) * fraction / 4,
                        fraction % 2 == 0 ? 0 : scroll.Extent.Height - scroll.Viewport.Height);
                    root.LayoutManager.ExecuteLayoutPass();
                    var ranges = new List<SpellCheckRange>();
                    SpellCheckRangeFinder.TryGetVisibleRanges(presenter, scroll, text, ranges);

                    var viewport = new Rect(scroll.TranslatePoint(default, presenter)!.Value, scroll.Viewport);
                    var y = presenter.GetTextVerticalOffset();
                    foreach (var line in presenter.TextLayout.TextLines)
                    {
                        var top = y;
                        y += line.Height;
                        if (y <= viewport.Top || top >= viewport.Bottom)
                            continue;

                        // Check visual order independently of GetTextBounds.
                        var runX = line.Start;
                        foreach (var run in line.TextRuns)
                        {
                            if (run is ShapedTextRun shaped)
                            {
                                Assert.True(MemoryMarshal.TryGetString(shaped.Text, out _, out var memoryStart, out _));
                                var glyphX = runX;
                                foreach (var glyph in shaped.GlyphRun.GlyphInfos)
                                {
                                    var layoutIndex = memoryStart + glyph.GlyphCluster - shaped.GlyphRun.Metrics.FirstCluster;
                                    var sourceIndex = presenter.GetTextPositionFromLayoutPosition(layoutIndex);
                                    var isPreedit = layoutIndex >= presenter.CaretIndex &&
                                        layoutIndex < presenter.CaretIndex + preedit.Length;
                                    if (!isPreedit && sourceIndex < text.Length && char.IsLetter(text[sourceIndex]) &&
                                        glyph.GlyphAdvance > 0 && glyphX + glyph.GlyphAdvance > viewport.Left + 0.001 &&
                                        glyphX < viewport.Right - 0.001)
                                    {
                                        Assert.Contains(ranges, r => r.Start <= sourceIndex && r.End > sourceIndex);
                                    }
                                    glyphX += glyph.GlyphAdvance;
                                }
                            }

                            if (run is DrawableTextRun drawable)
                                runX += drawable.Size.Width;
                        }
                    }

                    for (var i = 1; i < ranges.Count; i++)
                        Assert.True(ranges[i - 1].End < ranges[i].Start);
                }
                root.Child = null;
            }
        }

        [Fact]
        public void Spell_Check_Range_Finder_Marks_Soft_Wrapped_Word_Boundary()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                const string text = "אנציקלופדיה";
                var presenter = new TextPresenter
                {
                    Text = text,
                    FlowDirection = FlowDirection.RightToLeft,
                    FontSize = 16,
                    TextWrapping = TextWrapping.Wrap
                };

                presenter.Measure(new Size(42, double.PositiveInfinity));
                Assert.True(presenter.TextLayout.TextLines.Count > 1);
                presenter.Arrange(new Rect(0, 0, 42, presenter.TextLayout.TextLines[0].Height - 0.1));

                var ranges = new List<SpellCheckRange>();
                Assert.True(SpellCheckRangeFinder.TryGetVisibleRanges(
                    presenter,
                    null,
                    text,
                    ranges));

                var range = Assert.Single(ranges);
                Assert.True(range.End < text.Length);
                Assert.True(range.EndIsInsideWord);
            }
        }
    }
}
