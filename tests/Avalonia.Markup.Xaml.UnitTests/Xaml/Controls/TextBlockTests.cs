using System;
using Avalonia.Controls;
using Avalonia.Documents;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml.Controls
{
    public class TextBlockTests
    {
        [Fact]
        public void With_Plain_Text()
        {
            var xaml = @"<TextBlock xmlns='https://github.com/avaloniaui'>Hello World!</TextBlock>";

            var target = AvaloniaXamlLoader.Parse<TextBlock>(xaml);

            Assert.NotNull(target);
            Assert.Single(target.Inlines);
            Assert.Equal("Hello World!", target.Text);
        }

        [Fact]
        public void With_Run()
        {
            var xaml = @"<TextBlock xmlns='https://github.com/avaloniaui'>
  <Run>Hello World!</Run>
</TextBlock>";

            var target = AvaloniaXamlLoader.Parse<TextBlock>(xaml);

            Assert.NotNull(target);
            Assert.Single(target.Inlines);
            Assert.Equal("Hello World!", target.Text);
        }

        [Fact]
        public void With_Plain_Text_And_Run()
        {
            var xaml = @"<TextBlock xmlns='https://github.com/avaloniaui'>
  Hello <Run>World</Run>!
</TextBlock>";

            var target = AvaloniaXamlLoader.Parse<TextBlock>(xaml);

            Assert.NotNull(target);
            Assert.Equal(3, target.Inlines.Count);
            Assert.Equal("Hello ", ((Run)target.Inlines[0]).Text);
            Assert.Equal("World", ((Run)target.Inlines[1]).Text);
            Assert.Equal("!", ((Run)target.Inlines[2]).Text);
            Assert.Equal("Hello World!", target.Text);
        }
    }
}
