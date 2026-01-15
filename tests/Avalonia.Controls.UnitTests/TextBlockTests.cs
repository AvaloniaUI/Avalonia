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

        private class TestTextBlock : TextBlock
        {
            public Size Constraint => _constraint;
        }
    }
}
