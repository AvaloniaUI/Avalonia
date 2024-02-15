using System;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBlockTests
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
        public void Changing_InlinesCollection_Should_Invalidate_Measure()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextBlock();

                target.Measure(Size.Infinity);

                Assert.True(target.IsMeasureValid);

                target.Inlines.Add(new Run("Hello"));

                Assert.False(target.IsMeasureValid);

                target.Measure(Size.Infinity);

                Assert.True(target.IsMeasureValid);
            }
        }

        [Fact]
        public void Can_Call_Measure_Without_InvalidateTextLayout()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextBlock();

                target.Inlines.Add(new TextBox { Text = "Hello"});

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

                target.Inlines.Add(textBox);

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

                target.Inlines.Add(inline);

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

                target.Inlines.Add(run);

                target.Measure(Size.Infinity);

                Assert.True(target.IsMeasureValid);

                target.Inlines = null;

                Assert.Null(run.Parent);

                target.Inlines = new InlineCollection { run };

                Assert.Equal(target, run.Parent);
            }
        }

        [Fact]
        public void Changing_Inlines_Should_Reset_VisualChildren()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextBlock();

                target.Inlines.Add(new Border());

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

                target.Inlines.Add(run);

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
        public void InlineUIContainer_Child_Schould_Be_Arranged()
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
                Assert.Equal(80, button.DesiredSize.Width);

                target.Arrange(new Rect(new Size(200, 50)));

                Assert.True(button.IsArrangeValid);

                Assert.Equal(60, button.Bounds.Left);
            }
        }

        [Fact]
        public void Setting_Text_Should_Reset_Inlines()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new TextBlock();

                target.Inlines.Add(new Run("Hello World"));

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

                target.Inlines.Add(new Run("Hello World"));

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
    }
}
