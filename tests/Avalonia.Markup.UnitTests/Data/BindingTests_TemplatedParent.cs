using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class BindingTests_TemplatedParent
    {
        [Fact]
        public void OneWay_Binding_Should_Be_Set_Up()
        {
            var target = new Button
            {
                Template = new FuncControlTemplate<Button>((_, __) =>
                    new ContentPresenter
                    {
                        [!ContentPresenter.ContentProperty] = new Binding
                        {
                            Mode = BindingMode.OneWay,
                            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                            Priority = BindingPriority.TemplatedParent,
                            Path = "Content",
                        },
                    }),
                Content = "foo",
            };

            target.Measure(Size.Infinity);

            var contentPresenter = Assert.IsType<ContentPresenter>(target.GetVisualChildren().Single());
            Assert.Equal("foo", contentPresenter.Content);

            target.Content = "bar";
            Assert.Equal("bar", contentPresenter.Content);

            contentPresenter.Content = "baz";
            Assert.Equal("bar", target.Content);
        }

        [Fact]
        public void TwoWay_Binding_Should_Be_Set_Up()
        {
            var target = new Button
            {
                Template = new FuncControlTemplate<Button>((_, __) =>
                    new ContentPresenter
                    {
                        [!ContentPresenter.ContentProperty] = new Binding
                        {
                            Mode = BindingMode.TwoWay,
                            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                            Priority = BindingPriority.TemplatedParent,
                            Path = "Content",
                        },
                    }),
                Content = "foo",
            };

            target.Measure(Size.Infinity);

            var contentPresenter = Assert.IsType<ContentPresenter>(target.GetVisualChildren().Single());
            Assert.Equal("foo", contentPresenter.Content);

            target.Content = "bar";
            Assert.Equal("bar", contentPresenter.Content);

            target.ClearValue(Button.ContentProperty);
            contentPresenter.Content = "baz";
            Assert.Equal("baz", target.Content);
        }
    }
}
