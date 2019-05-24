// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Data;
using Avalonia.Documents;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
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
        public void Default_Text_Value_Should_Be_Empty_String()
        {
            var textBlock = new TextBlock();

            Assert.Equal(string.Empty, textBlock.Text);
        }

        [Fact]
        public void Default_Inlines_Should_Be_Empty()
        {
            var textBlock = new TextBlock();

            Assert.Empty(textBlock.Inlines);
        }

        [Fact]
        public void Text_Set_To_Null_Should_Return_Null()
        {
            var target = new TextBlock { Text = null };

            Assert.Null(target.Text);
        }

        [Fact]
        public void Text_Set_To_Null_Should_Return_Empty_Inlines()
        {
            var target = new TextBlock { Text = null };

            Assert.Empty(target.Inlines);
        }

        [Fact]
        public void Setting_Text_Should_Add_Single_Run_To_Inlines()
        {
            var target = new TextBlock { Text = "Foo" };

            Assert.Single(target.Inlines);
            Assert.IsType<Run>(target.Inlines[0]);
            Assert.Equal("Foo", ((Run)target.Inlines[0]).Text);
        }

        [Fact]
        public void Setting_Text_To_Empty_String_Should_Clear_Inlines()
        {
            var textBlock = new TextBlock { Text = "Foo" };

            textBlock.Text = string.Empty;

            Assert.Empty(textBlock.Inlines);
        }

        [Fact]
        public void Text_Should_Concatenate_Multiple_Inlines()
        {
            var textBlock = new TextBlock();

            textBlock.Inlines.Add(new Run("Hello "));
            textBlock.Inlines.Add(new Run("World"));

            Assert.Equal("Hello World", textBlock.Text);
        }

        [Fact]
        public void Changing_Text_Should_Invalidate_Measure()
        {
            var textBlock = new TextBlock();

            textBlock.Measure(Size.Infinity);
            textBlock.Text = "Foo";

            Assert.False(textBlock.IsMeasureValid);
        }

        [Fact]
        public void Adding_Run_Should_Invalidate_Measure()
        {
            var textBlock = new TextBlock();

            textBlock.Measure(Size.Infinity);
            textBlock.Inlines.Add(new Run("Foo"));

            Assert.False(textBlock.IsMeasureValid);
        }

        [Fact]
        public void Inlines_Should_Be_LogicalChildren()
        {
            var textBlock = new TextBlock
            {
                Inlines =
                {
                    new Run("Hello "),
                    new Run("World") { FontWeight = FontWeight.Bold },
                },
            };

            Assert.Equal(2, textBlock.GetLogicalChildren().Count());
        }

        [Fact]
        public void Should_Create_FormattedText_With_Span()
        {
            var textBlock = new TextBlock
            {
                Inlines =
                {
                    new Run("Hello "),
                    new Run("World") { FontWeight = FontWeight.Bold },
                },
            };

            var formattedText = textBlock.FormattedText;

            Assert.NotNull(formattedText.Spans);
            Assert.Equal(1, formattedText.Spans.Count);
            Assert.Equal(FontWeight.Bold, formattedText.Spans[0].Typeface.Weight);
        }

        [Fact]
        public void Style_Is_Applied_To_Run()
        {
            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                var textBlock = new TextBlock
                {
                    Styles =
                {
                    new Style(x => x.OfType<Run>().Class("bold"))
                    {
                        Setters =
                        {
                            new Setter(TextElement.FontWeightProperty, FontWeight.Bold),
                        },
                    }
                },
                    Inlines =
                {
                    new Run("Hello "),
                    new Run("World") { Classes = { "bold" } },
                },
                };

                var root = new TestRoot
                {
                    Child = textBlock,
                };

                var formattedText = textBlock.FormattedText;

                Assert.NotNull(formattedText.Spans);
                Assert.Equal(1, formattedText.Spans.Count);
                Assert.Equal(FontWeight.Bold, formattedText.Spans[0].Typeface.Weight);
            }
        }

        [Fact]
        public void Changing_Background_Brush_Color_Should_Invalidate_Visual()
        {
            var target = new TextBlock()
            {
                Background = new SolidColorBrush(Colors.Red),
            };

            var root = new TestRoot(target);
            var renderer = Mock.Get(root.Renderer);
            renderer.ResetCalls();

            ((SolidColorBrush)target.Background).Color = Colors.Green;

            renderer.Verify(x => x.AddDirty(target), Times.Once);
        }

        [Fact]
        public void Changing_Foreground_Brush_Color_Should_Invalidate_Visual()
        {
            var target = new TextBlock()
            {
                Foreground = new SolidColorBrush(Colors.Red),
            };

            var root = new TestRoot(target);
            var renderer = Mock.Get(root.Renderer);
            renderer.ResetCalls();

            ((SolidColorBrush)target.Foreground).Color = Colors.Green;

            renderer.Verify(x => x.AddDirty(target), Times.Once);
        }
    }
}
