using System;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;
using Moq;
using Xunit;

#nullable enable

namespace Avalonia.Markup.UnitTests.Data
{
    public class BindingTests_TemplatedParent
    {
        [Fact]
        public void OneWay_Binding_Should_Be_Set_Up()
        {
            var target = CreateTarget();
            var binding = new Binding
            {
                Mode = BindingMode.OneWay,
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                Priority = BindingPriority.TemplatedParent,
                Path = "Foo",
            };

            target.Object.Bind(TextBox.TextProperty, binding);

            target.Verify(x => x.Bind(
                TextBox.TextProperty,
                It.IsAny<IObservable<object?>>(),
                BindingPriority.TemplatedParent));
        }

        [Fact]
        public void TwoWay_Binding_Should_Be_Set_Up()
        {
            var target = CreateTarget();
            var binding = new Binding
            {
                Mode = BindingMode.TwoWay,
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                Priority = BindingPriority.TemplatedParent,
                Path = "Foo",
            };

            target.Object.Bind(TextBox.TextProperty, binding);

            target.Verify(x => x.Bind(
                TextBox.TextProperty,
                It.IsAny<IObservable<object?>>(),
                BindingPriority.TemplatedParent));
        }

        private Mock<IControl> CreateTarget(
            ITemplatedControl? templatedParent = null,
            string? text = null)
        {
            var result = new Mock<IControl>();

            result.Setup(x => x.GetValue(Control.TemplatedParentProperty)).Returns(templatedParent);
            result.Setup(x => x.GetValue(Control.TemplatedParentProperty)).Returns(templatedParent);
            result.Setup(x => x.GetValue(TextBox.TextProperty)).Returns(text);
            result.Setup(x => x.GetObservable(TextBlock.TextProperty))
                .Returns(Mock.Of<IObservable<object?>>());
            result.Setup(x => x.Bind(
                It.IsAny<DirectPropertyBase<string>>(),
                It.IsAny<IObservable<object?>>(),
                BindingPriority.TemplatedParent))
                .Returns(Disposable.Empty);
            return result;
        }
    }
}
