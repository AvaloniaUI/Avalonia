using System;
using Moq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;
using Xunit;
using System.Reactive.Disposables;
using Avalonia.Markup.Data;

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

        private static Mock<IControl> CreateTarget(
            ITemplatedControl templatedParent = null,
            string text = null)
        {
            var result = new Mock<IControl>();

            result.Setup(x => x.GetValue(Control.TemplatedParentProperty)).Returns(templatedParent);
            result.Setup(x => x.GetValue((AvaloniaProperty)Control.TemplatedParentProperty)).Returns(templatedParent);
            result.Setup(x => x.GetValue((AvaloniaProperty)TextBox.TextProperty)).Returns(text);
            result.Setup(x => x.Bind(It.IsAny<AvaloniaProperty>(), It.IsAny<IObservable<object>>(), It.IsAny<BindingPriority>()))
                .Returns(Disposable.Empty);
            return result;
        }
    }
}
