using System.Text;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBlockTests : ScopedTestBase
    {
        [Fact]
        public void DefaultBindingMode_Should_Be_OneWay()
        {
            Assert.Equal(
                BindingMode.OneWay,
                TextBlock.TextProperty.GetMetadata(typeof(TextBlock)).DefaultBindingMode);
        }

        [Fact]
        public void Default_Text_Value_Should_Be_Null()
        {
            var textBlock = new TextBlock();

            Assert.Equal(null, textBlock.Text);
        }

        [Fact]
        public void LetterSpacing_Property_Uses_TextElement_Definition()
        {
            Assert.Same(TextElement.LetterSpacingProperty, TextBlock.LetterSpacingProperty);
        }

        [Fact]
        public void Calling_Measure_Should_Update_TextLayout()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var textBlock = new TestTextBlock { Text = "Hello World" };

                var constraint = textBlock.Constraint;
                Assert.True(double.IsNaN(constraint.Width));
                Assert.True(double.IsNaN(constraint.Height));

                textBlock.Measure(new Size(100, 100));

                var textLayout = textBlock.TextLayout;

                textBlock.Measure(new Size(50, 100));

                Assert.NotEqual(textLayout, textBlock.TextLayout);
            }
        }

        [Fact]
        public void Should_Measure_MinTextWith()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var textBlock = new TextBlock
                {
                    Text = "Hello&#10;שלום&#10;Really really really really long line",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.DetectFromContent,
                    TextWrapping = TextWrapping.Wrap
                };

                textBlock.Measure(new Size(1920, 1080));

                var textLayout = textBlock.TextLayout;

                var constraint = LayoutHelper.RoundLayoutSizeUp(new Size(textLayout.Width, textLayout.Height), 1);

                Assert.Equal(constraint, textBlock.DesiredSize);
            }
        }

        [Fact]
        public void Calling_Arrange_With_Different_Size_Should_Update_Constraint_And_TextLayout()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var textBlock = new TestTextBlock { Text = "Hello World" };

                textBlock.Measure(Size.Infinity);

                var textLayout = textBlock.TextLayout;

                var constraint = LayoutHelper.RoundLayoutSizeUp(new Size(textLayout.WidthIncludingTrailingWhitespace, textLayout.Height), 1);

                textBlock.Arrange(new Rect(constraint));

                //TextLayout is recreated after arrange
                textLayout = textBlock.TextLayout;

                Assert.Equal(constraint, textBlock.Constraint);

                textBlock.Measure(constraint);

                Assert.Equal(textLayout, textBlock.TextLayout);

                constraint += new Size(50, 0);

                textBlock.Arrange(new Rect(constraint));

                Assert.Equal(constraint, textBlock.Constraint);

                //TextLayout is recreated after arrange
                Assert.NotEqual(textLayout, textBlock.TextLayout);
            }
        }

        [Fact]
        public void Calling_Measure_With_Infinite_Space_Should_Set_DesiredSize()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var textBlock = new TestTextBlock { Text = "Hello World" };

                textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                var textLayout = textBlock.TextLayout;

                var constraint = LayoutHelper.RoundLayoutSizeUp(new Size(textLayout.WidthIncludingTrailingWhitespace, textLayout.Height), 1);

                Assert.Equal(constraint, textBlock.DesiredSize);
            }
        }

        [Fact]
        public void Changing_InlinesCollection_Should_Invalidate_Measure()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextBlock();

                target.Measure(Size.Infinity);

                Assert.True(target.IsMeasureValid);

                target.Inlines!.Add(new Run("Hello"));

                Assert.False(target.IsMeasureValid);

                target.Measure(Size.Infinity);

                Assert.True(target.IsMeasureValid);
            }
        }

        [Fact]
        public void Changing_Inlines_Should_Attach_Embedded_Controls_To_Parents()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextBlock();

                var control = new Border();

                var inlineUIContainer = new InlineUIContainer { Child = control };

                target.Inlines = new InlineCollection { inlineUIContainer };

                Assert.Equal(inlineUIContainer, control.Parent);

                Assert.Equal(target, control.VisualParent);
            }
        }

        [Fact]
        public void Can_Call_Measure_Without_InvalidateTextLayout()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextBlock();

                target.Inlines!.Add(new TextBox { Text = "Hello"});

                target.Measure(Size.Infinity);

                target.InvalidateMeasure();

                target.Measure(Size.Infinity);
            }
        }

        [Fact]
        public void Embedded_Control_Should_Keep_Focus()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new TextBlock();

                var root = new TestRoot
                {
                    Child = target
                };

                var textBox = new TextBox { Text = "Hello", Template = TextBoxTests.CreateTemplate() };

                target.Inlines!.Add(textBox);

                target.Measure(Size.Infinity);

                textBox.Focus();

                Assert.Same(textBox, root.FocusManager.GetFocusedElement());

                target.InvalidateMeasure();

                Assert.Same(textBox, root.FocusManager.GetFocusedElement());

                target.Measure(Size.Infinity);

                Assert.Same(textBox, root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Changing_Inlines_Properties_Should_Invalidate_Measure()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextBlock();

                var inline = new Run("Hello");

                target.Inlines!.Add(inline);

                target.Measure(Size.Infinity);

                Assert.True(target.IsMeasureValid);

                inline.Foreground = Brushes.Green;

                Assert.False(target.IsMeasureValid);
            }
        }

        [Fact]
        public void Changing_Inlines_Should_Invalidate_Measure()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextBlock();

                var inlines = new InlineCollection { new Run("Hello") };

                target.Measure(Size.Infinity);

                Assert.True(target.IsMeasureValid);

                target.Inlines = inlines;

                Assert.False(target.IsMeasureValid);
            }
        }

        [Fact]
        public void Changing_Inlines_Should_Reset_Inlines_Parent()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextBlock();

                var run = new Run("Hello");

                target.Inlines!.Add(run);

                target.Measure(Size.Infinity);

                Assert.True(target.IsMeasureValid);

                target.Inlines = null;

                Assert.Null(run.Parent);

                target.Inlines = new InlineCollection { run };

                Assert.Equal(target, run.Parent);
            }
        }

        [Fact]
        public void Changing_InlineHost_Should_Propagate_To_Nested_Inlines()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextBlock();

                var span = new Span { Inlines = new InlineCollection { new Run { Text = "World" } } };

                var inlines = new InlineCollection{ new Run{Text = "Hello "}, span };

                target.Inlines = inlines;

                Assert.Equal(target, span.InlineHost);
            }
        }

        [Fact]
        public void Changing_Inlines_Should_Reset_VisualChildren()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextBlock();

                target.Inlines!.Add(new Border());

                target.Measure(Size.Infinity);

                Assert.NotEmpty(target.VisualChildren);

                target.Inlines = null;

                Assert.Empty(target.VisualChildren);
            }
        }

        [Fact]
        public void Changing_Inlines_Should_Reset_InlineUIContainer_VisualParent_On_Measure()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextBlock();

                var control = new Control();

                var run = new InlineUIContainer(control);

                target.Inlines!.Add(run);

                target.Measure(Size.Infinity);

                Assert.True(target.IsMeasureValid);

                Assert.Equal(target, control.VisualParent);

                target.Inlines = null;

                Assert.Null(run.Parent);

                target.Inlines = new InlineCollection { new Run("Hello World") };

                Assert.Null(run.Parent);

                target.Measure(Size.Infinity);

                Assert.Null(control.VisualParent);
            }
        }

        [Fact]
        public void InlineUIContainer_Child_Should_Be_Arranged()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new TextBlock();

                var button = new Button { Content = "12345678" };

                button.Template = new FuncControlTemplate<Button>((parent, scope) =>
                        new TextBlock
                        {
                            Name = "PART_ContentPresenter",
                            [!TextBlock.TextProperty] = parent[!ContentControl.ContentProperty],
                        }.RegisterInNameScope(scope)
                );

                target.Inlines!.Add("123456");
                target.Inlines.Add(new InlineUIContainer(button));
                target.Inlines.Add("123456");

                target.Measure(Size.Infinity);
                target.Arrange(new Rect(target.DesiredSize));

                Assert.True(button.IsMeasureValid);
                Assert.Equal(58, button.DesiredSize.Width);

                target.Arrange(new Rect(new Size(200, 50)));

                Assert.True(button.IsArrangeValid);

                Assert.Equal(43, button.Bounds.Left);
            }
        }

        [Fact]
        public void InlineUIContainer_Child_Should_Be_Constrained()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new TextBlock();

                GeometryDrawing drawing = new GeometryDrawing();
                drawing.Geometry = new RectangleGeometry(new Rect(0, 0, 500, 500));
                DrawingImage image = new DrawingImage(drawing);

                Image imageControl = new Image { Source = image };
                InlineUIContainer container = new InlineUIContainer(imageControl);

                target.Inlines!.Add(new Run("The child should not be limited by position on line."));
                target.Inlines.Add(container);

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(target.DesiredSize));

                Assert.True(imageControl.IsMeasureValid);
                Assert.Equal(100, imageControl.Bounds.Width);
            }
        }

        [Fact]
        public void Setting_Text_Should_Reset_Inlines()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new TextBlock();

                target.Inlines!.Add(new Run("Hello World"));

                Assert.Equal(null, target.Text);

                Assert.Equal(1, target.Inlines.Count);

                target.Text = "1234";

                Assert.Equal("1234", target.Text);

                Assert.Equal(0, target.Inlines.Count);
            }
        }
        
        [Fact]
        public void Setting_TextDecorations_Should_Update_Inlines()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new TextBlock();

                target.Inlines!.Add(new Run("Hello World"));

                Assert.Equal(1, target.Inlines.Count);

                Assert.Null(target.Inlines[0].TextDecorations);

                var underline = TextDecorations.Underline;

                target.TextDecorations = underline;

                Assert.Equal(underline, target.Inlines[0].TextDecorations);
            }
        }
        
        [Fact]
        public void TextBlock_TextLines_Should_Be_Empty()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var textblock = new TextBlock();
                textblock.Inlines?.Add(new Run("123"));
                textblock.Measure(new Size(200, 200));
                int count = textblock.TextLayout.TextLines[0].TextRuns.Count;
                textblock.Inlines?.Clear();
                textblock.Measure(new Size(200, 200));
                int count1 = textblock.TextLayout.TextLines[0].TextRuns.Count;
                Assert.NotEqual(count, count1);
            }
        }

        [Fact]
        public void TextBlock_With_Infinite_Size_Should_Be_Remeasured_After_TextLayout_Created()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new TextBlock { Text = "" };
            var layout = target.TextLayout;

            Assert.Equal(0.0, layout.MaxWidth);
            Assert.Equal(0.0, layout.MaxHeight);

            target.Text = "foo";
            target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.True(target.DesiredSize.Width > 0);
            Assert.True(target.DesiredSize.Height > 0);
        }

        [Fact]
        public void TextBlock_With_UseLayoutRounding_True_Should_Round_DesiredSize()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new TextBlock { Text = "1980" };

            target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(new Size(28, 15), target.DesiredSize);
        }

        [Fact]
        public void TextBlock_With_UseLayoutRounding_True_Should_Round_Padding_And_DesiredSize()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new TextBlock { Text = "1980", Padding = new(2.25) };

            target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(new Size(32, 19), target.DesiredSize);
        }

        [Fact]
        public void TextBlock_With_UseLayoutRounding_False_Should_Not_Round_DesiredSize()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new TextBlock { Text = "1980", UseLayoutRounding = false };

            target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(new Size(27.954545454545453, 14.522727272727273), target.DesiredSize);
        }

        [Fact]
        public void TextBlock_With_UseLayoutRounding_False_Should_Not_Round_Bounds()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new TextBlock { Text = "1980", UseLayoutRounding = false };

            target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            target.Arrange(new Rect(default, target.DesiredSize));

            Assert.Equal(new Rect(0, 0, 27.954545454545453, 14.522727272727273), target.Bounds);
        }

        [Fact]
        public void TextBlock_With_Fractional_LineHeight_Should_Not_Cull_Last_Line_At_Fractional_Scaling()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new TextBlock
            {
                Text = "first second third",
                FontSize = 16,
                LineHeight = 20.8,
                TextWrapping = TextWrapping.Wrap,
                Width = 50,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            var root = new TestRoot(target)
            {
                LayoutScaling = 1.25,
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            Assert.Equal(3, target.TextLayout.TextLines.Count);
        }

        [Fact]
        public void TextBlock_With_UseLayoutRounding_False_Should_Not_Round_Padding_In_MeasureOverride()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new TextBlock { Text = "1980", UseLayoutRounding = false, Padding = new(2.25) };

            target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(new Size(32.45454545454545, 19.022727272727273), target.DesiredSize);
        }

        [Fact]
        public void TextBlock_With_UseLayoutRounding_False_Should_Not_Round_Padding_In_ArrangeOverride()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new TextBlock { Text = "1980", UseLayoutRounding = false, Padding = new(2.25) };

            target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            target.Arrange(new Rect(default, target.DesiredSize));

            Assert.Equal(new Rect(0, 0, 32.45454545454545, 19.022727272727273), target.Bounds);
        }

        [Fact]
        public void Measure_And_Arrange_Should_Use_WidthIncludingTrailingWhitespace_For_Bounds()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new TextBlock
            {
                Text = "fy",
                FontStyle = FontStyle.Italic,
                FontSize = 48,
                UseLayoutRounding = false,
                Padding = new Thickness(3, 2, 5, 4)
            };

            target.Measure(Size.Infinity);

            var expectedSize =
                new Size(target.TextLayout.WidthIncludingTrailingWhitespace, target.TextLayout.Height)
                    .Inflate(target.Padding);

            Assert.Equal(expectedSize, target.DesiredSize);

            target.Arrange(new Rect(default, target.DesiredSize));

            Assert.Equal(new Rect(default, expectedSize), target.Bounds);
        }

        [Fact]
        public void TextBlock_With_Wrap_MaxLines_CharacterEllipsis_Should_Show_Ellipsis_On_Last_Line()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            const double width = 106;
            var unbounded = new TextBlock
            {
                Text = LongLoremText,
                TextWrapping = TextWrapping.Wrap,
                Width = width,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };

            unbounded.Measure(Size.Infinity);
            unbounded.Arrange(new Rect(0, 0, width, unbounded.DesiredSize.Height));

            Assert.True(unbounded.TextLayout.TextLines.Count > 2);

            var truncated = new TextBlock
            {
                Text = LongLoremText,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 2,
                Width = width,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };

            truncated.Measure(Size.Infinity);
            truncated.Arrange(new Rect(0, 0, width, truncated.DesiredSize.Height));

            Assert.Equal(2, truncated.TextLayout.TextLines.Count);
            Assert.Contains("…", GetLineText(truncated, 1));
        }

        [Fact]
        public void TextBlock_With_Wrap_CharacterEllipsis_And_Height_Limit_Should_Show_Ellipsis_On_Last_Line()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            const double width = 180;

            var unbounded = new TextBlock
            {
                Text = LongLoremText,
                TextWrapping = TextWrapping.Wrap,
                Width = width,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };

            unbounded.Measure(Size.Infinity);
            unbounded.Arrange(new Rect(0, 0, width, unbounded.DesiredSize.Height));

            Assert.True(unbounded.TextLayout.TextLines.Count > 3);

            var lineHeight = unbounded.TextLayout.TextLines[0].Height;
            var constrainedHeight = lineHeight * 1.25;
            var target = new TextBlock
            {
                Text = LongLoremText,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width = width,
                Height = constrainedHeight,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, width, constrainedHeight));

            Assert.True(target.TextLayout.TextLines.Count < unbounded.TextLayout.TextLines.Count);
            Assert.Contains("…", GetLineText(target, target.TextLayout.TextLines.Count - 1));
        }

        [Fact]
        public void TextBlock_With_Wrap_MaxLines_WordEllipsis_Should_Show_Ellipsis_On_Last_Line()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            const double width = 106;
            var unbounded = new TextBlock
            {
                Text = LongLoremText,
                TextWrapping = TextWrapping.Wrap,
                Width = width,
            };

            unbounded.Measure(Size.Infinity);
            unbounded.Arrange(new Rect(0, 0, width, unbounded.DesiredSize.Height));

            Assert.True(unbounded.TextLayout.TextLines.Count > 2);

            var truncated = new TextBlock
            {
                Text = LongLoremText,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.WordEllipsis,
                MaxLines = 2,
                Width = width,
            };

            truncated.Measure(Size.Infinity);
            truncated.Arrange(new Rect(0, 0, width, truncated.DesiredSize.Height));

            Assert.Equal(2, truncated.TextLayout.TextLines.Count);
            Assert.Contains("…", GetLineText(truncated, 1));
        }

        [Fact]
        public void TextBlock_With_Wrap_WordEllipsis_And_Height_Limit_Should_Show_Ellipsis_On_Last_Line()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            const double width = 180;
            var unbounded = new TextBlock
            {
                Text = LongLoremText,
                TextWrapping = TextWrapping.Wrap,
                Width = width,
            };

            unbounded.Measure(Size.Infinity);
            unbounded.Arrange(new Rect(0, 0, width, unbounded.DesiredSize.Height));

            Assert.True(unbounded.TextLayout.TextLines.Count > 3);

            var lineHeight = unbounded.TextLayout.TextLines[0].Height;
            var constrainedHeight = lineHeight * 1.25;
            var target = new TextBlock
            {
                Text = LongLoremText,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.WordEllipsis,
                Width = width,
                Height = constrainedHeight,
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, width, constrainedHeight));

            Assert.True(target.TextLayout.TextLines.Count < unbounded.TextLayout.TextLines.Count);
            Assert.Contains("…", GetLineText(target, target.TextLayout.TextLines.Count - 1));
        }

        [Fact]
        public void TextBlock_With_MaxLines_When_Text_Fully_Fits_Should_Not_Show_Ellipsis()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new TextBlock
            {
                Text = "first line\r\nsecond line",
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 2,
                Width = 200,
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, 200, target.DesiredSize.Height));

            Assert.Equal(2, target.TextLayout.TextLines.Count);
            Assert.DoesNotContain("…", GetLineText(target, 1));
        }

        [Fact]
        public void TextBlock_With_Overflow_And_MaxLines_Should_Not_Produce_Double_Ellipsis()
        {
            // A single very long word that overflows the width on line 1, which is also MaxLines = 1.
            // The overflow path collapses the line first; the MaxLines path must not re-collapse it.
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            const double width = 80;
            var target = new TextBlock
            {
                Text = "AAAAAAAAAAAAAAAAAAAAAAAAA BBBBBBBBBBBBBBBBBBBBBBBBB CCCCCCCCCCCCCCCCCCCCC",
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 1,
                Width = width,
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, width, target.DesiredSize.Height));

            var lastLineText = GetLineText(target, 0);

            // Must contain exactly one ellipsis, not two.
            Assert.Contains("…", lastLineText);
            Assert.False(lastLineText.Contains("……"), $"Double ellipsis found in: {lastLineText}");
        }

        [Fact]
        public void TextBlock_With_Overflow_And_MaxHeight_Should_Not_Produce_Double_Ellipsis()
        {
            // When a line overflows its width and is collapsed by the overflow handler, and then the
            // MaxHeight gate is hit, no second ellipsis must appear. With the IsSplit guard on the
            // MaxHeight path, CollapseForTruncation is not called here at all (the overflowed line
            // ends at a hard paragraph break, so IsSplit=false). The single "…" comes from the
            // overflow handler only.
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            const double width = 20;
            var target = new TextBlock
            {
                Text = "AAAAAAAAAAAAAAAAAAAAAAAAA\r\nsecond line\r\nthird line",
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width = width,
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, width, target.DesiredSize.Height));

            var lineHeight = target.TextLayout.TextLines[0].Height;

            target.Height = lineHeight * 1.25;

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, width, target.Height));

            var lastLineText = GetLineText(target, target.TextLayout.TextLines.Count - 1);

            // Must contain exactly one ellipsis, not two.
            Assert.Contains("…", lastLineText);
            Assert.False(lastLineText.Contains("……"), $"Double ellipsis found in: {lastLineText}");
        }

        [Fact]
        public void TextBlock_With_MaxHeight_And_Hidden_Content_After_NewLine_Should_Not_Show_Ellipsis()
        {
            // When a Height limit hides content that follows a hard paragraph break, no ellipsis is shown
            // on the last visible line — matching WPF and CSS max-height+overflow:hidden behavior.
            // The last visible line ("second line") was not itself trimmed, so no "…" is added.
            // Contrast with MaxLines, where the behavior is intentionally different (see the MaxLines tests).
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var unbounded = new TextBlock
            {
                Text = "first line\r\nsecond line\r\nthird line",
                Width = 200,
            };

            unbounded.Measure(Size.Infinity);
            unbounded.Arrange(new Rect(0, 0, 200, unbounded.DesiredSize.Height));

            Assert.True(unbounded.TextLayout.TextLines.Count >= 3);

            var lineHeight = unbounded.TextLayout.TextLines[0].Height;

            var target = new TextBlock
            {
                Text = "first line\r\nsecond line\r\nthird line",
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width = 200,
                Height = lineHeight * 2,
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, 200, target.Height));

            Assert.Equal(2, target.TextLayout.TextLines.Count);
            Assert.DoesNotContain("…", GetLineText(target, 1));
        }

        [Fact]
        public void TextBlock_With_Rtl_Wrap_MaxLines_CharacterEllipsis_Should_Show_Ellipsis_On_Last_Line()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            const double width = 120;
            var unbounded = new TextBlock
            {
                Text = LongArabicText,
                FlowDirection = FlowDirection.RightToLeft,
                TextWrapping = TextWrapping.Wrap,
                Width = width,
            };

            unbounded.Measure(Size.Infinity);
            unbounded.Arrange(new Rect(0, 0, width, unbounded.DesiredSize.Height));

            Assert.True(unbounded.TextLayout.TextLines.Count > 2);

            var target = new TextBlock
            {
                Text = LongArabicText,
                FlowDirection = FlowDirection.RightToLeft,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 2,
                Width = width,
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, width, target.DesiredSize.Height));

            Assert.Equal(2, target.TextLayout.TextLines.Count);
            Assert.Contains("…", GetLineText(target, 1));
        }

        private static string GetLineText(TextBlock textBlock, int lineIndex)
        {
            var text = new StringBuilder();

            foreach (var run in textBlock.TextLayout.TextLines[lineIndex].TextRuns)
            {
                text.Append(run.Text.ToString());
            }

            return text.ToString();
        }

        private const string LongLoremText =
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
            "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. " +
            "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.";

        private const string LongArabicText =
            "مرحبا بكم في اختبار التخطيط للنصوص الطويلة التي تلتف عبر عدة أسطر للتحقق من سلوك علامة الحذف عند الاقتصاص.";

        private class TestTextBlock : TextBlock
        {
            public Size Constraint => _constraint;
        }
    }
}
