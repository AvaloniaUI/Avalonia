using System;
using Moq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;
using Xunit;
using System.Reactive.Disposables;
using Avalonia.Markup.Data;
using Avalonia.Controls.Primitives;

namespace Avalonia.Markup.UnitTests.Data
{
    public class BindingTests_Self
    {
        [Fact]
        public void Binding_To_Property_On_Self_Should_Work()
        {
            var target = new TextBlock
            {
                Tag = "Hello World!",
                [!TextBlock.TextProperty] = new Binding("Tag")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.Self)
                },
            };

            Assert.Equal("Hello World!", target.Text);
        }

        [Fact]
        public void TwoWay_Binding_To_Property_On_Self_Should_Work()
        {
            var target = new TextBlock
            {
                Tag = "Hello World!",
                [!TextBlock.TextProperty] = new Binding("Tag", BindingMode.TwoWay)
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.Self)
                },
            };

            Assert.Equal("Hello World!", target.Text);
            target.Text = "Goodbye cruel world :(";
            Assert.Equal("Goodbye cruel world :(", target.Text);
        }
    }
}
