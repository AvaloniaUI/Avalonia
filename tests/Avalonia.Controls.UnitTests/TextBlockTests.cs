// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data;
using Avalonia.Media;
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
        public void Default_Text_Value_Should_Be_EmptyString()
        {
            var textBlock = new TextBlock();

            Assert.Equal(
                "",
                textBlock.Text);
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
